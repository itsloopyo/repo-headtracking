using System;
using CameraUnlock.Core.Unity.Tracking;
using UnityEngine;

namespace REPOHeadTracking.Camera
{
    /// <summary>
    /// Drives <see cref="FlashlightTracking"/> off Unity's render callbacks, so the light
    /// is turned only for the span of the main camera's render pass and is back on the
    /// game's own rotation before anything else looks at it.
    ///
    /// <see cref="Attach"/> must come after the tracking controller has registered its own
    /// render hook, so that by the time these fire the camera already carries the tracked
    /// matrix this frame's world is rendered with.
    /// </summary>
    internal sealed class FlashlightRenderHook
    {
        /// <summary>
        /// How far the light turns relative to the view. A player who turns their head
        /// keeps their eyes on what they turned towards, so their gaze sits past the
        /// centre of the screen; matching the light to the view alone leaves it short of
        /// what they are actually looking at.
        /// </summary>
        private const float HeadRotationScale = 1.5f;

        private readonly ViewMatrixTrackingController _controller;
        private readonly Func<bool> _isGameplayActive;
        private readonly FlashlightTracking _flashlight = new FlashlightTracking();

        public FlashlightRenderHook(ViewMatrixTrackingController controller, Func<bool> isGameplayActive)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            _isGameplayActive = isGameplayActive ?? throw new ArgumentNullException(nameof(isGameplayActive));
        }

        public void Attach()
        {
            UnityEngine.Camera.onPreCull += RotateForRender;
            UnityEngine.Camera.onPostRender += RestoreAfterRender;
        }

        public void Detach()
        {
            UnityEngine.Camera.onPreCull -= RotateForRender;
            UnityEngine.Camera.onPostRender -= RestoreAfterRender;
            _flashlight.Restore();
        }

        private void RotateForRender(UnityEngine.Camera cam)
        {
            if (cam != _controller.MainCamera)
                return;

            if (!_controller.IsApplyingTracking || !_isGameplayActive())
                return;

            _flashlight.Apply(GetHeadRotationDelta(cam));
        }

        private void RestoreAfterRender(UnityEngine.Camera cam)
        {
            if (cam == _controller.MainCamera)
                _flashlight.Restore();
        }

        /// <summary>
        /// The world-space rotation the view was turned by this frame, read back from the
        /// matrix the controller wrote rather than recomposed from the tracking angles -
        /// so it matches whatever composition was applied, world-yaw or camera-local.
        ///
        /// A Unity view matrix holds the camera's world basis transposed, looking down
        /// -Z: row 0 is right, row 1 is up, row 2 is negated forward.
        /// </summary>
        private static Quaternion GetHeadRotationDelta(UnityEngine.Camera cam)
        {
            Matrix4x4 view = cam.worldToCameraMatrix;
            var forward = new Vector3(-view.m20, -view.m21, -view.m22);
            var up = new Vector3(view.m10, view.m11, view.m12);

            Quaternion tracked = Quaternion.LookRotation(forward, up);
            Quaternion delta = tracked * Quaternion.Inverse(cam.transform.rotation);
            return Quaternion.SlerpUnclamped(Quaternion.identity, delta, HeadRotationScale);
        }
    }
}
