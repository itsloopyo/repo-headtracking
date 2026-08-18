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
        public static TrackingPipeline Build(ConfigManager config, Action<string> log)
        {
            var receiver = new OpenTrackReceiver { Log = log };

            var processor = new TrackingProcessor
            {
                LocalSmoothing = config.LocalSmoothing.Value,
                RemoteSmoothing = config.RemoteSmoothing.Value,
                Sensitivity = new SensitivitySettings(
                    config.YawSensitivity.Value,
                    config.PitchSensitivity.Value,
                    config.RollSensitivity.Value,
                    invertYaw: config.InvertYaw.Value,
                    invertPitch: config.InvertPitch.Value,
                    invertRoll: config.InvertRoll.Value),
                Deadzone = DeadzoneSettings.None
            };

            var positionProcessor = new PositionProcessor
            {
                Settings = PositionSettings.Symmetric(
                    config.PositionSensitivityX.Value,
                    config.PositionSensitivityY.Value,
                    config.PositionSensitivityZ.Value,
                    config.PositionLimitX.Value,
                    config.PositionLimitY.Value,
                    config.PositionLimitZ.Value,
                    config.PositionLimitZBack.Value,
                    localSmoothing: config.LocalSmoothing.Value,
                    remoteSmoothing: config.RemoteSmoothing.Value,
                    invertX: true, invertY: false, invertZ: false),
                TrackerPivotForward = config.TrackerPivotForward.Value
            };

            var controller = new ViewMatrixTrackingController(
                receiver, processor, new PoseInterpolator(),
                positionProcessor, new PositionInterpolator());

            return new TrackingPipeline(receiver, controller);
        }
    }
}
