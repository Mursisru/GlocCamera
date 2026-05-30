using System.Collections.Generic;
using UnityEngine;

namespace GlocCamera_Engine
{
    /// <summary>
    /// Night cockpit lighting: fill spot is **spawned** on the cockpit camera with configurable camera-local
    /// pose, then **immediately reparented** to <c>cockpit.transform</c> with <c>worldPositionStays</c> so its
    /// world pose is frozen relative to the cockpit (no pan drift). **J** / **K** hotkeys toggle armed state and
    /// wide cone mode. Optional boost of external <see cref="Light"/>.
    /// </summary>
    internal static class GlocCockpitLightingDriver
    {
        private const string FillObjectName = "GlocCamera_InstrumentFlashlight";

        private struct ExternalLightRecord
        {
            public Light Light;
            public Vector2 BaseIntensityRange;
        }

        private static Aircraft _boundAircraft;
        private static readonly List<ExternalLightRecord> ExternalLights = new List<ExternalLightRecord>(32);
        private static GameObject _fillRoot;
        private static Light _fillLight;
        private static bool _fillPoseFrozenToCockpit;
        private static bool _fillUserArmed = true;
        private static bool _fillWideMode;

        internal static void ForceRestore()
        {
            RestoreExternalLights();
            DestroyFill();
            if (_boundAircraft != null)
                DestroyStrayModObjectsOnAircraft(_boundAircraft);
            _boundAircraft = null;
        }

        /// <summary>Removes legacy mod-spawned lights/particles left on the airframe (e.g. nose gear experiments).</summary>
        internal static void DestroyStrayModObjectsOnAircraft(Aircraft ac)
        {
            if (ac?.transform == null)
                return;

            foreach (Transform t in ac.transform.GetComponentsInChildren<Transform>(true))
            {
                if (t == null || t.gameObject == null)
                    continue;
                if (_fillRoot != null && t.gameObject == _fillRoot)
                    continue;
                if (!t.name.StartsWith("GlocCamera_"))
                    continue;
                Object.Destroy(t.gameObject);
            }
        }

        internal static void Tick(CameraStateManager cam)
        {
            if (!GlocCameraPlugin.Enabled.Value || !GlocCameraPlugin.LightingEnabled.Value)
            {
                if (_boundAircraft != null)
                    ForceRestore();
                return;
            }

            if (cam == null || cam.currentState != cam.cockpitState)
            {
                if (_boundAircraft != null)
                    ForceRestore();
                return;
            }

            if (!GlocFrameContext.HasLocalAircraft)
            {
                if (_boundAircraft != null)
                    ForceRestore();
                return;
            }

            Aircraft local = GlocFrameContext.LocalAircraft;
            if (!GlocFrameContext.IsLocalPilotReady(cam))
            {
                if (_boundAircraft != null)
                    ForceRestore();
                return;
            }

            if (local.cockpit == null || local.cockpit.transform == null)
                return;

            if (cam.transform == null)
                return;

            if (!GameManager.flightControlsEnabled)
                return;

            if (_boundAircraft != local)
            {
                ForceRestore();
                DestroyStrayModObjectsOnAircraft(local);
                BindAircraft(local);
            }

            ProcessFillHotkeys();

            float night01 = GlocNightFactor.ComputeNight01();
            bool nightOk = night01 >= Mathf.Clamp01(GlocCameraPlugin.LightingFillNightMin01.Value);
            bool fillOn = GlocCameraPlugin.LightingFillEnabled.Value && _fillUserArmed && nightOk;
            EnsureFill(local, cam.transform);
            if (_fillLight != null)
            {
                _fillLight.enabled = fillOn;
                if (fillOn)
                {
                    _fillLight.intensity = GlocCameraPlugin.LightingFillIntensity.Value;
                    _fillLight.range = GlocCameraPlugin.LightingFillRange.Value;
                    float outer = _fillWideMode
                        ? GlocCameraPlugin.LightingFillSpotAngleWide.Value
                        : GlocCameraPlugin.LightingFillSpotAngle.Value;
                    float inner = _fillWideMode
                        ? GlocCameraPlugin.LightingFillInnerSpotAngleWide.Value
                        : GlocCameraPlugin.LightingFillInnerSpotAngle.Value;
                    _fillLight.spotAngle = outer;
                    _fillLight.innerSpotAngle = Mathf.Clamp(inner, 1f, Mathf.Max(2f, outer - 0.5f));
                    if (!string.IsNullOrEmpty(GlocCameraPlugin.LightingFillColorHex.Value)
                        && ColorUtility.TryParseHtmlString(GlocCameraPlugin.LightingFillColorHex.Value, out Color c))
                        _fillLight.color = c;
                }
            }

            ApplyExternalLightMultipliers();
        }

        private static void BindAircraft(Aircraft ac)
        {
            _boundAircraft = ac;
            ExternalLights.Clear();

            string[] keys = SplitSubstrings(GlocCameraPlugin.LightingExternalNameSubstrings.Value);
            if (keys.Length == 0)
                return;

            foreach (Light lt in ac.transform.GetComponentsInChildren<Light>(true))
            {
                if (lt == null || lt.gameObject == null)
                    continue;
                if (lt.gameObject.name == FillObjectName)
                    continue;

                string path = BuildTransformPath(lt.transform, ac.transform).ToLowerInvariant();
                if (path.Contains("cockpit") && !PathMatchesAnyKey(path, keys))
                    continue;
                if (!PathMatchesAnyKey(path, keys))
                    continue;

                ExternalLights.Add(new ExternalLightRecord
                {
                    Light = lt,
                    BaseIntensityRange = new Vector2(lt.intensity, lt.range),
                });
            }
        }

        private static void ApplyExternalLightMultipliers()
        {
            float iMul = Mathf.Max(0.01f, GlocCameraPlugin.LightingExternalIntensityMul.Value);
            float rMul = Mathf.Max(0.01f, GlocCameraPlugin.LightingExternalRangeMul.Value);
            for (int i = 0; i < ExternalLights.Count; i++)
            {
                var rec = ExternalLights[i];
                if (rec.Light == null)
                    continue;
                rec.Light.intensity = rec.BaseIntensityRange.x * iMul;
                rec.Light.range = rec.BaseIntensityRange.y * rMul;
            }
        }

        private static void RestoreExternalLights()
        {
            for (int i = 0; i < ExternalLights.Count; i++)
            {
                var rec = ExternalLights[i];
                if (rec.Light == null)
                    continue;
                rec.Light.intensity = rec.BaseIntensityRange.x;
                rec.Light.range = rec.BaseIntensityRange.y;
            }
            ExternalLights.Clear();
        }

        private static void DestroyFill()
        {
            if (_fillRoot != null)
            {
                Object.Destroy(_fillRoot);
                _fillRoot = null;
                _fillLight = null;
            }
            _fillPoseFrozenToCockpit = false;
            _fillUserArmed = true;
            _fillWideMode = false;
        }

        private static bool FillHotkeysAllowed()
        {
            if (Cursor.visible)
                return false;
            if (DynamicMap.mapMaximized)
                return false;
            return !RadialMenuMain.IsInUse();
        }

        private static void ProcessFillHotkeys()
        {
            if (!GlocCameraPlugin.LightingFillEnabled.Value)
                return;
            if (!FillHotkeysAllowed())
                return;
            if (GlocCameraPlugin.LightingFillToggleHotkey.Value.IsDown())
                _fillUserArmed = !_fillUserArmed;
            if (GlocCameraPlugin.LightingFillWideModeHotkey.Value.IsDown())
                _fillWideMode = !_fillWideMode;
        }

        private static void EnsureFill(Aircraft ac, Transform cockpitCameraTransform)
        {
            if (!GlocCameraPlugin.LightingFillEnabled.Value)
            {
                DestroyFill();
                return;
            }

            Transform cockpitRoot = ac.cockpit?.transform;
            if (cockpitRoot == null || cockpitCameraTransform == null)
                return;

            if (_fillRoot != null)
            {
                if (_fillPoseFrozenToCockpit && _fillRoot.transform.parent == cockpitRoot)
                    return;

                if (!_fillPoseFrozenToCockpit && _fillRoot.transform.parent == cockpitCameraTransform)
                {
                    FreezeFillToCockpit(cockpitRoot);
                    return;
                }

                DestroyFill();
            }

            DestroyStrayModObjectsOnAircraft(ac);
            _fillRoot = new GameObject(FillObjectName);
            _fillRoot.transform.SetParent(cockpitCameraTransform, false);
            _fillRoot.transform.localPosition = GlocCameraPlugin.LightingFillLocalPosition.Value;
            _fillRoot.transform.localEulerAngles = new Vector3(
                GlocCameraPlugin.LightingFillPitchDegrees.Value,
                GlocCameraPlugin.LightingFillYawDegrees.Value,
                GlocCameraPlugin.LightingFillRollDegrees.Value);

            _fillLight = _fillRoot.AddComponent<Light>();
            _fillLight.type = LightType.Spot;
            _fillLight.shadows = LightShadows.None;
            _fillLight.renderingLayerMask = -1;
            _fillLight.renderMode = LightRenderMode.ForcePixel;
            if (!string.IsNullOrEmpty(GlocCameraPlugin.LightingFillColorHex.Value)
                && ColorUtility.TryParseHtmlString(GlocCameraPlugin.LightingFillColorHex.Value, out Color c))
                _fillLight.color = c;

            FreezeFillToCockpit(cockpitRoot);
        }

        private static void FreezeFillToCockpit(Transform cockpitRoot)
        {
            if (_fillRoot == null || cockpitRoot == null)
                return;
            _fillRoot.transform.SetParent(cockpitRoot, worldPositionStays: true);
            _fillPoseFrozenToCockpit = true;
        }

        private static string BuildTransformPath(Transform t, Transform root)
        {
            var parts = new List<string>();
            Transform x = t;
            int guard = 0;
            while (x != null && x != root && guard++ < 64)
            {
                parts.Add(x.name);
                x = x.parent;
            }
            parts.Reverse();
            return string.Join("/", parts.ToArray());
        }

        private static string[] SplitSubstrings(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return System.Array.Empty<string>();
            string[] parts = raw.Split(new[] { ';', ',', '|' }, System.StringSplitOptions.RemoveEmptyEntries);
            var list = new List<string>();
            foreach (string p in parts)
            {
                string t = p.Trim();
                if (t.Length > 0)
                    list.Add(t.ToLowerInvariant());
            }
            return list.ToArray();
        }

        private static bool PathMatchesAnyKey(string pathLower, string[] keysLower)
        {
            foreach (string k in keysLower)
            {
                if (pathLower.Contains(k))
                    return true;
            }
            return false;
        }
    }
}
