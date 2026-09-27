using System.IO;
using BepInEx.Configuration;
using CameraUnlock.Core.Config;

namespace REPOHeadTracking.Legacy
{
    /// <summary>
    /// Frozen: the Bind calls the last build before the canonical config ran on the plugin's
    /// <see cref="ConfigFile"/>, each definition's section, key, type, default, description and
    /// acceptable values unchanged. Reads BepInEx\config\com.cameraunlock.repo.headtracking.cfg
    /// exactly as that build did and writes nothing. Never edit this file.
    /// </summary>
    internal static class LegacyConfigReader
    {
        public const string General = "General";
        public const string UI = "UI";
        public const string Keybindings = "Keybindings";
        public const string Network = "Network";
        public const string Sensitivity = "Sensitivity";
        public const string Smoothing = "Smoothing";
        public const string Position = "Position";

        /// <summary>Every section and key <see cref="Read"/> binds, in the order it binds them.</summary>
        public static readonly LegacyKey[] Keys =
        {
            new LegacyKey(General, "EnabledOnStartup"),
            new LegacyKey(General, "ShowStartupNotification"),
            new LegacyKey(General, "WorldSpaceYaw"),
            new LegacyKey(General, "FlashlightFollowsHead"),
            new LegacyKey(General, "FlashlightMultiplier"),
            new LegacyKey(UI, "ShowConnectionNotifications"),
            new LegacyKey(Keybindings, "ToggleKey"),
            new LegacyKey(Keybindings, "CycleTrackingModeKey"),
            new LegacyKey(Keybindings, "YawModeKey"),
            new LegacyKey(Network, "UDPPort"),
            new LegacyKey(Sensitivity, "YawSensitivity"),
            new LegacyKey(Sensitivity, "PitchSensitivity"),
            new LegacyKey(Sensitivity, "RollSensitivity"),
            new LegacyKey(Sensitivity, "InvertYaw"),
            new LegacyKey(Sensitivity, "InvertPitch"),
            new LegacyKey(Sensitivity, "InvertRoll"),
            new LegacyKey(Smoothing, "LocalSmoothing"),
            new LegacyKey(Smoothing, "RemoteSmoothing"),
            new LegacyKey(Position, "PositionEnabled"),
            new LegacyKey(Position, "PositionSensitivityX"),
            new LegacyKey(Position, "PositionSensitivityY"),
            new LegacyKey(Position, "PositionSensitivityZ"),
            new LegacyKey(Position, "PositionLimitX"),
            new LegacyKey(Position, "PositionLimitY"),
            new LegacyKey(Position, "PositionLimitZ"),
            new LegacyKey(Position, "PositionLimitZBack"),
            new LegacyKey(Position, "TrackerPivotForward"),
        };

        /// <summary>
        /// Reads the settings the way the published build did, through BepInEx's own parser and
        /// clamping. BepInEx read the .cfg once already, in the ConfigFile constructor, so this
        /// turns saving off, reads the file again and then binds. Returns the defaults when the
        /// file is absent, as that build ran on a first start.
        /// </summary>
        public static LegacyConfig Read(ConfigFile config)
        {
            config.SaveOnConfigSet = false;
            if (File.Exists(config.ConfigFilePath))
            {
                config.Reload();
            }

            var c = new LegacyConfig();

            c.EnabledOnStartup = config.Bind(
                General, "EnabledOnStartup", c.EnabledOnStartup,
                "Whether head tracking is enabled when the game starts").Value;

            c.ShowStartupNotification = config.Bind(
                General, "ShowStartupNotification", c.ShowStartupNotification,
                "Whether to show a notification when the plugin initializes").Value;

            c.WorldSpaceYaw = config.Bind(
                General, "WorldSpaceYaw", c.WorldSpaceYaw,
                "Yaw mode: true = horizon-locked yaw (default), false = camera-local").Value;

            c.FlashlightFollowsHead = config.Bind(
                General, "FlashlightFollowsHead", c.FlashlightFollowsHead,
                "Point the flashlight where you are looking rather than where you are aiming").Value;

            c.FlashlightMultiplier = config.Bind(
                General, "FlashlightMultiplier", c.FlashlightMultiplier,
                new ConfigDescription(
                    "How far the flashlight turns relative to your head",
                    new AcceptableValueRange<float>(0f, 5.0f))).Value;

            c.ShowConnectionNotifications = config.Bind(
                UI, "ShowConnectionNotifications", c.ShowConnectionNotifications,
                "Whether to show notifications when OpenTrack connection is lost or restored").Value;

            c.ToggleKey = config.Bind(
                Keybindings, "ToggleKey", c.ToggleKey,
                "Key to toggle head tracking on/off").Value;

            c.CycleTrackingModeKey = config.Bind(
                Keybindings, "CycleTrackingModeKey", c.CycleTrackingModeKey,
                "Key to cycle tracking mode (normal -> rotation only -> position only -> normal)").Value;

            c.YawModeKey = config.Bind(
                Keybindings, "YawModeKey", c.YawModeKey,
                "Key to toggle world-locked vs camera-local yaw").Value;

            c.UDPPort = config.Bind(
                Network, "UDPPort", c.UDPPort,
                new ConfigDescription(
                    "UDP port to listen for OpenTrack data",
                    new AcceptableValueRange<int>(1024, 65535))).Value;

            c.YawSensitivity = config.Bind(
                Sensitivity, "YawSensitivity", c.YawSensitivity,
                new ConfigDescription(
                    "Multiplier for horizontal head rotation (left/right)",
                    new AcceptableValueRange<float>(0.1f, 3.0f))).Value;

            c.PitchSensitivity = config.Bind(
                Sensitivity, "PitchSensitivity", c.PitchSensitivity,
                new ConfigDescription(
                    "Multiplier for vertical head rotation (up/down)",
                    new AcceptableValueRange<float>(0.1f, 3.0f))).Value;

            c.RollSensitivity = config.Bind(
                Sensitivity, "RollSensitivity", c.RollSensitivity,
                new ConfigDescription(
                    "Multiplier for head tilt (ear to shoulder)",
                    new AcceptableValueRange<float>(0.0f, 3.0f))).Value;

            c.InvertYaw = config.Bind(
                Sensitivity, "InvertYaw", c.InvertYaw,
                "Invert horizontal (left/right) head rotation").Value;

            c.InvertPitch = config.Bind(
                Sensitivity, "InvertPitch", c.InvertPitch,
                "Invert vertical (up/down) head rotation").Value;

            c.InvertRoll = config.Bind(
                Sensitivity, "InvertRoll", c.InvertRoll,
                "Invert head tilt (roll)").Value;

            c.LocalSmoothing = config.Bind(
                Smoothing, "LocalSmoothing", c.LocalSmoothing,
                new ConfigDescription(
                    "Smoothing applied when the tracker runs on this machine (loopback). " +
                    "0 = no smoothing, 1 = heavy. Covers rotation and position.",
                    new AcceptableValueRange<float>(0f, 1f))).Value;

            c.RemoteSmoothing = config.Bind(
                Smoothing, "RemoteSmoothing", c.RemoteSmoothing,
                new ConfigDescription(
                    "Smoothing applied when the tracker is a remote device on the network. " +
                    "0 = no smoothing, 1 = heavy. Covers rotation and position.",
                    new AcceptableValueRange<float>(0f, 1f))).Value;

            c.PositionEnabled = config.Bind(
                Position, "PositionEnabled", c.PositionEnabled,
                "Enable positional tracking (lean in/out/side-to-side)").Value;

            c.PositionSensitivityX = config.Bind(
                Position, "PositionSensitivityX", c.PositionSensitivityX,
                new ConfigDescription(
                    "Multiplier for lateral (left/right) position",
                    new AcceptableValueRange<float>(0f, 5.0f))).Value;

            c.PositionSensitivityY = config.Bind(
                Position, "PositionSensitivityY", c.PositionSensitivityY,
                new ConfigDescription(
                    "Multiplier for vertical (up/down) position",
                    new AcceptableValueRange<float>(0f, 5.0f))).Value;

            c.PositionSensitivityZ = config.Bind(
                Position, "PositionSensitivityZ", c.PositionSensitivityZ,
                new ConfigDescription(
                    "Multiplier for depth (forward/back) position",
                    new AcceptableValueRange<float>(0f, 5.0f))).Value;

            c.PositionLimitX = config.Bind(
                Position, "PositionLimitX", c.PositionLimitX,
                new ConfigDescription(
                    "Maximum lateral displacement in meters",
                    new AcceptableValueRange<float>(0.01f, 0.5f))).Value;

            c.PositionLimitY = config.Bind(
                Position, "PositionLimitY", c.PositionLimitY,
                new ConfigDescription(
                    "Maximum vertical displacement in meters",
                    new AcceptableValueRange<float>(0.01f, 0.5f))).Value;

            c.PositionLimitZ = config.Bind(
                Position, "PositionLimitZ", c.PositionLimitZ,
                new ConfigDescription(
                    "Maximum forward displacement in meters",
                    new AcceptableValueRange<float>(0.01f, 0.5f))).Value;

            c.PositionLimitZBack = config.Bind(
                Position, "PositionLimitZBack", c.PositionLimitZBack,
                new ConfigDescription(
                    "Maximum backward displacement in meters",
                    new AcceptableValueRange<float>(0.01f, 0.5f))).Value;

            c.TrackerPivotForward = config.Bind(
                Position, "TrackerPivotForward", c.TrackerPivotForward,
                new ConfigDescription(
                    "Distance from pivot point to tracker face point. " +
                    "Compensates lateral arc from head yaw in position data.",
                    new AcceptableValueRange<float>(0f, 0.20f))).Value;

            return c;
        }
    }
}
