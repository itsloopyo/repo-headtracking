using System;
using CameraUnlock.Core.Data;
using CameraUnlock.Core.Processing;
using CameraUnlock.Core.Protocol;
using CameraUnlock.Core.Unity.Tracking;
using REPOHeadTracking.Config;

namespace REPOHeadTracking.Core
{
    /// <summary>
    /// The assembled tracking pipeline: the UDP source at one end and the camera
    /// controller at the other. The processors and interpolators between them are owned
    /// by the controller from construction on, so only these two ends are exposed.
    /// </summary>
    internal sealed class TrackingPipeline
    {
        // R.E.P.O.'s camera pitches the opposite way to the tracker's convention. Shipped as
        // InvertPitch = true before the canonical config and folded into the code, so the view
        // moves as it did at that default.
        private static readonly SensitivitySettings RotationAxes = new SensitivitySettings(1f, 1f, 1f, invertPitch: true);

        // The neck pivot the mod shipped. The tracker is authoritative over the rest of the pose,
        // but this distance is the mod's own, so it stays in code rather than in the config.
        private const float TrackerPivotForward = 0.08f;

        public OpenTrackReceiver Receiver { get; }
        public ViewMatrixTrackingController Controller { get; }

        private TrackingPipeline(OpenTrackReceiver receiver, ViewMatrixTrackingController controller)
        {
            Receiver = receiver;
            Controller = controller;
        }

        /// <summary>
        /// Wires the pipeline up from the config's tuning values. Runtime toggles
        /// (yaw mode, tracking mode) are the caller's to seed, and neither the receiver
        /// nor the controller is started here - the caller decides when tracking goes live.
        /// </summary>
        public static TrackingPipeline Build(REPOConfig config, Action<string> log)
        {
            var receiver = new OpenTrackReceiver { Log = log };

            var processor = new TrackingProcessor
            {
                LocalSmoothing = config.LocalSmoothing,
                RemoteSmoothing = config.RemoteSmoothing,
                Sensitivity = RotationAxes,
                Deadzone = DeadzoneSettings.None
            };

            PositionSettings limits = config.Position;
            var positionProcessor = new PositionProcessor
            {
                Settings = new PositionSettings(
                    1f, 1f, 1f,
                    limits.LimitX, limits.LimitY, limits.LimitYDown, limits.LimitZ, limits.LimitZBack,
                    config.LocalSmoothing, config.RemoteSmoothing,
                    invertX: true, invertY: false, invertZ: false),
                TrackerPivotForward = TrackerPivotForward
            };

            var controller = new ViewMatrixTrackingController(
                receiver, processor, new PoseInterpolator(),
                positionProcessor, new PositionInterpolator());

            return new TrackingPipeline(receiver, controller);
        }
    }
}
