using CameraUnlock.Core.Config;

namespace REPOHeadTracking.Config
{
    /// <summary>
    /// Everything the mod reads from BepInEx\config\CameraUnlock.ini. Unity-free, so the test
    /// project compiles it and holds the committed file to it.
    /// </summary>
    public sealed class REPOConfig : HeadTrackingConfigData
    {
        public const string DisplayName = "R.E.P.O.";

        public bool ShowStartupNotification { get; set; } = true;

        public bool ShowConnectionNotifications { get; set; } = true;

        public static ConfigTable<REPOConfig> Table()
        {
            return HeadTrackingConfigTable.Create<REPOConfig>(
                    ConfigConcepts.UdpPort,
                    ConfigConcepts.EnableOnStartup,
                    ConfigConcepts.WorldSpaceYaw,
                    ConfigConcepts.RotationEnabled,
                    ConfigConcepts.LocalSmoothing,
                    ConfigConcepts.RemoteSmoothing,
                    ConfigConcepts.PositionEnabled,
                    ConfigConcepts.PositionLimitX,
                    ConfigConcepts.PositionLimitY,
                    ConfigConcepts.PositionLimitYDown,
                    ConfigConcepts.PositionLimitZ,
                    ConfigConcepts.PositionLimitZBack,
                    ConfigConcepts.ToggleKey,
                    ConfigConcepts.CycleTrackingModeKey,
                    ConfigConcepts.YawModeKey,
                    ConfigConcepts.LightFollowsHead,
                    ConfigConcepts.LightMultiplier)
                .Select(ConfigConcepts.WorldSpaceYaw).Writable()
                .Select(ConfigConcepts.RotationEnabled).Writable()
                .Select(ConfigConcepts.PositionEnabled).Writable()
                .Local("Notifications", "ShowStartupNotification", c => c.ShowStartupNotification,
                    (c, v) => c.ShowStartupNotification = v, new BoolCodec(),
                    "true: show head tracking's state and hotkeys on screen when the game starts.")
                .Local("Notifications", "ShowConnectionNotifications", c => c.ShowConnectionNotifications,
                    (c, v) => c.ShowConnectionNotifications = v, new BoolCodec(),
                    "true: show on screen when the tracker's data starts or stops arriving, and when\n" +
                    "the UDP port is in use by another app.");
        }
    }
}
