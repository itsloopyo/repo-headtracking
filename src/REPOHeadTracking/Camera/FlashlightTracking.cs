using System;
using System.Reflection;
using REPOHeadTracking.Reflection;
using UnityEngine;

namespace REPOHeadTracking.Camera
{
    /// <summary>
    /// Points the player's flashlight where the head is looking instead of where the
    /// body is aiming.
    ///
    /// The rotation is applied in the render phase and taken off again immediately
    /// after, so nothing in the game's Update ever observes a rotated light. That
    /// matters here: FlashlightLightAim raycasts along the light every frame to
    /// publish the aim point other players see, and PhysGrabber's interaction ray
    /// comes off the camera transform. Both keep reading the game's own values.
    ///
    /// Only the light is rotated, never the held flashlight mesh - the player's hand
    /// does not move with their head.
    /// </summary>
    internal sealed class FlashlightTracking
    {
        private static FieldInfo _controllerInstanceField;
        private static FieldInfo _spotlightField;
        private static bool _resolved;

        private Transform _lightTransform;
        private Quaternion _cleanRotation;
        private bool _isRotated;

        /// <summary>
        /// Rotates the light by the same delta the view was rotated by. Call from the
        /// render hook, after the camera's tracked matrix has been written.
        /// </summary>
        public void Apply(Quaternion headDelta)
        {
            if (_isRotated)
                return;

            var light = ResolveLightTransform();
            if (light == null)
                return;

            _lightTransform = light;
            _cleanRotation = light.rotation;
            light.rotation = headDelta * _cleanRotation;
            _isRotated = true;
        }

        /// <summary>
        /// Puts the light back on the game's own rotation. Call after rendering.
        /// </summary>
        public void Restore()
        {
            if (!_isRotated)
                return;

            _isRotated = false;
            if (_lightTransform != null)
                _lightTransform.rotation = _cleanRotation;
        }

        private static Transform ResolveLightTransform()
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
