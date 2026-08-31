using System;
using System.Reflection;
using REPOHeadTracking.Reflection;
using UnityEngine;

namespace REPOHeadTracking.Camera
{
    /// <summary>
    /// Resolves the player's flashlight beam - the spotlight the game's
    /// FlashlightController holds, reached through its Instance singleton.
    ///
    /// The light alone, never the held flashlight mesh: the player's hand does not
    /// move with their head.
    ///
    /// The light is turned only for the span of the render pass and put straight back
    /// (CameraUnlock.Core.Unity.Effects.HeadFollowLight), which is what keeps the rest of
    /// the game reading the direction it aimed the beam itself. Two things here depend on
    /// that: FlashlightLightAim raycasts along the light every frame to publish the aim
    /// point other players see, and PhysGrabber's interaction ray comes off the camera
    /// transform.
    ///
    /// Looked up by reflection so the build stays game-DLL-agnostic (CI compiles
    /// against Unity stubs only).
    /// </summary>
    internal static class GameFlashlight
    {
        private static FieldInfo _controllerInstanceField;
        private static FieldInfo _spotlightField;
        private static bool _resolved;

        /// <summary>
        /// The beam's transform, or null when there is no flashlight right now (boot,
        /// menus, level load, a player who has not picked one up).
        /// </summary>
        public static Transform Resolve()
        {
            if (!_resolved && !ResolveReflection())
                return null;

            var controller = _controllerInstanceField.GetValue(null) as MonoBehaviour;
            if (controller == null)
                return null;

            // Typed as Component rather than Light: the field is a UnityEngine.Light, but
            // only its transform is needed and the build's Unity stubs have no Light.
            var spotlight = _spotlightField.GetValue(controller) as Component;
            if (spotlight == null)
                return null;

            return spotlight.transform;
        }

        private static bool ResolveReflection()
        {
            Type controllerType = GameTypes.Find("FlashlightController");
            if (controllerType == null)
                return false;

            _controllerInstanceField = controllerType.GetField("Instance",
                BindingFlags.Public | BindingFlags.Static);
            _spotlightField = controllerType.GetField("spotlight",
                BindingFlags.Public | BindingFlags.Instance);

            if (_controllerInstanceField == null || _spotlightField == null)
                return false;

            _resolved = true;
            return true;
        }
    }
}
