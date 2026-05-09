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
            float num = Mathf.Clamp(numVanilla + gloc, minF, maxF);
            float lerp = Mathf.Clamp01(GlocCameraPlugin.CockpitFovLerp.Value);
            cam.mainCamera.fieldOfView = Mathf.Lerp(_mainCameraFovAtPrefix, num, lerp);
            cam.cockpitCamRender.fieldOfView = cam.mainCamera.fieldOfView;

            if (PlayerSettings.useTrackIR)
                return;
            if (GlocCameraPlugin.DollyMaxMeters.Value <= 0.0001f)
                return;

            float dolly = GlocCameraDriver.GetSmoothedDollyZ();
            if (Mathf.Abs(dolly) < 1e-6f)
                return;

            var lp = cam.transform.localPosition;
            cam.transform.localPosition = new Vector3(lp.x, lp.y, dolly);
        }
    }
}
