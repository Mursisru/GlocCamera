using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace GlocCamera_Engine
{
    [HarmonyPatch(typeof(CameraCockpitState), nameof(CameraCockpitState.UpdateState))]
    internal static class GlocCameraPatches
    {
        private static readonly FieldInfo FovAdjustment = AccessTools.Field(typeof(CameraCockpitState), "FOVAdjustment");
        private static readonly FieldInfo MinFov = AccessTools.Field(typeof(CameraCockpitState), "minFOV");
        private static readonly FieldInfo MaxFov = AccessTools.Field(typeof(CameraCockpitState), "maxFOV");
        private static readonly FieldInfo PilotField = AccessTools.Field(typeof(CameraCockpitState), "pilot");

        private static float _mainCameraFovAtPrefix;

        [HarmonyPrefix]
        private static void Prefix(CameraStateManager cam)
        {
            if (cam?.mainCamera == null)
                return;
            _mainCameraFovAtPrefix = cam.mainCamera.fieldOfView;
        }

        [HarmonyPostfix]
        private static void Postfix(CameraCockpitState __instance, CameraStateManager cam)
        {
            if (!GlocCameraPlugin.Enabled.Value || cam?.mainCamera == null || cam.cockpitCamRender == null)
                return;
            if (cam.currentState != cam.cockpitState)
                return;

            var pilot = PilotField.GetValue(__instance) as Pilot;
            if (pilot == null || pilot.dead)
                return;
            if (!GameManager.flightControlsEnabled)
                return;

            float fovAdj = (float)FovAdjustment.GetValue(__instance);
            float minF = (float)MinFov.GetValue(__instance);
            float maxF = (float)MaxFov.GetValue(__instance);
            float numVanilla = Mathf.Clamp(cam.desiredFOV + fovAdj, minF, maxF);
            float gloc = GlocCameraDriver.GetSmoothedFovDelta();
            bool applyFovExtras = !PlayerSettings.useTrackIR || GlocCameraPlugin.CockpitViewApplyFramingWithTrackIR.Value;
            float fovBias = applyFovExtras ? GlocCameraPlugin.CockpitViewFovBiasDegrees.Value : 0f;
            float num = Mathf.Clamp(numVanilla + gloc + fovBias, minF, maxF);
            float lerp = Mathf.Clamp01(GlocCameraPlugin.CockpitFovLerp.Value);
            cam.mainCamera.fieldOfView = Mathf.Lerp(_mainCameraFovAtPrefix, num, lerp);
            cam.cockpitCamRender.fieldOfView = cam.mainCamera.fieldOfView;

            bool framingPosTrack = PlayerSettings.useTrackIR && GlocCameraPlugin.CockpitViewApplyFramingWithTrackIR.Value;
            if (PlayerSettings.useTrackIR && !framingPosTrack)
                return;

            bool useTrackIr = PlayerSettings.useTrackIR;
            bool applyApex = GlocCameraPlugin.ApexViewEnabled.Value && (!useTrackIr || GlocCameraPlugin.ApexViewWithTrackIR.Value);
            Vector3 apex = applyApex ? GlocApexViewDriver.GetSmoothedLocalOffset() : Vector3.zero;

            var vo = new Vector3(
                GlocCameraPlugin.CockpitViewOffsetLocalX.Value + apex.x,
                GlocCameraPlugin.CockpitViewOffsetLocalY.Value + apex.y,
                GlocCameraPlugin.CockpitViewOffsetLocalZ.Value + apex.z);

            if (PlayerSettings.useTrackIR)
            {
                if (vo.sqrMagnitude < 1e-12f)
                    return;

                var lp = cam.transform.localPosition;
                const float TxMin = -0.25f;
                const float TxMax = 0.25f;
                const float TyMin = -0.15f;
                const float TyMax = 0.15f;
                const float TzMin = -0.1f;
                const float TzMax = 0.45f;
                cam.transform.localPosition = new Vector3(
                    Mathf.Clamp(lp.x + vo.x, TxMin, TxMax),
                    Mathf.Clamp(lp.y + vo.y, TyMin, TyMax),
                    Mathf.Clamp(lp.z + vo.z, TzMin, TzMax));
                return;
            }

            float dolly = GlocCameraPlugin.DollyMaxMeters.Value <= 0.0001f ? 0f : GlocCameraDriver.GetSmoothedDollyZ();
            if (Mathf.Abs(dolly) < 1e-6f && vo.sqrMagnitude < 1e-12f)
                return;

            var lp2 = cam.transform.localPosition;
            cam.transform.localPosition = new Vector3(lp2.x + vo.x, lp2.y + vo.y, dolly + vo.z);
        }
    }
}
