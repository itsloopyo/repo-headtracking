using System;
using System.Collections.Generic;
using System.IO;
using BepInEx.Configuration;
using CameraUnlock.Core.Config;
using CameraUnlock.Core.Data;
using CameraUnlock.Core.Effects;
using CameraUnlock.Core.Input;
using REPOHeadTracking.Legacy;
using UnityEngine;

namespace REPOHeadTracking.Config
{
    /// <summary>
    /// The legacy import: the frozen reader on the plugin's own ConfigFile, then a map, field by
    /// field, from what it read into <see cref="REPOConfig"/>. The owner runs it once, while
    /// BepInEx\config\CameraUnlock.ini is absent, on
    /// BepInEx\config\com.cameraunlock.repo.headtracking.cfg, which it never writes.
    /// </summary>
    internal static class LegacyMigration
    {
        // What the last build before the canonical config shipped, and so what a pose-shaping
        // value is compared with.
        private static readonly LegacyConfig Shipped = new LegacyConfig();

        public static LegacyImport<REPOConfig> Import(ConfigFile pluginConfig)
        {
            return new LegacyImport<REPOConfig>((input, config) => Run(pluginConfig, input, config), LegacyConfigReader.Keys);
        }

        private static ImportResult Run(ConfigFile pluginConfig, LegacyImportInput input, REPOConfig config)
        {
            bool exists = File.Exists(input.Path);
            LegacyConfig legacy;
            try
            {
                legacy = LegacyConfigReader.Read(pluginConfig);
            }
            catch (ArgumentException e)
            {
                // BepInEx refuses a key it cannot name. The plugin's own ConfigFile already read
                // the file once without throwing, so the file changed in between.
                return ImportResult.Refused("BepInEx cannot read it: " + e.Message);
            }
            finally
            {
                // The frozen reader bound its entries on the plugin's ConfigFile. Unbound, they are
                // not listed by ConfigurationManager, where they would do nothing.
                pluginConfig.Clear();
            }

            var dropped = new List<DroppedValue>();
            var poseShaping = new List<PoseShapingValue>();
            LegacyFollowsDefaultsIni follows = Map(legacy, config, dropped, poseShaping);
            return exists
                ? ImportResult.Imported(dropped, poseShaping, follows.Concepts)
                : ImportResult.Absent(dropped, poseShaping, follows.Concepts);
        }

        /// <summary>
        /// Sets every field from the legacy values and returns the rows left to Defaults.ini: each
        /// global row whose legacy setting holds what the last build shipped (owner rule of
        /// 2026-09-26).
        /// </summary>
        public static LegacyFollowsDefaultsIni Map(LegacyConfig legacy, REPOConfig config, ICollection<DroppedValue> dropped,
            ICollection<PoseShapingValue> poseShaping)
        {
            config.UdpPort = legacy.UDPPort;
            config.EnableOnStartup = legacy.EnabledOnStartup;
            config.WorldSpaceYaw = legacy.WorldSpaceYaw;
            // The published build always started with rotation on; PositionEnabled chose between
            // the rotation-and-position and rotation-only modes at startup, and the cycle key
            // walked all three whatever it said.
            config.RotationEnabled = true;
            config.PositionEnabled = legacy.PositionEnabled;
            config.LocalSmoothing = legacy.LocalSmoothing;
            config.RemoteSmoothing = legacy.RemoteSmoothing;
            // PositionLimitY bounded the view both up and down.
            PositionSettings p = config.Position;
            config.Position = new PositionSettings(
                p.SensitivityX, p.SensitivityY, p.SensitivityZ,
                legacy.PositionLimitX, legacy.PositionLimitY, legacy.PositionLimitY, legacy.PositionLimitZ, legacy.PositionLimitZBack,
                legacy.LocalSmoothing, legacy.RemoteSmoothing,
                p.InvertX, p.InvertY, p.InvertZ);
            config.Light = new HeadFollowLightSettings
            {
                FollowsHead = legacy.FlashlightFollowsHead,
                Multiplier = legacy.FlashlightMultiplier,
            };
            config.ShowStartupNotification = legacy.ShowStartupNotification;
            config.ShowConnectionNotifications = legacy.ShowConnectionNotifications;

            config.ToggleKeyName = KeyList(legacy.ToggleKey, KeyCode.Y, "ToggleKey", dropped);
            config.CycleTrackingModeKeyName = KeyList(legacy.CycleTrackingModeKey, KeyCode.G, "CycleTrackingModeKey", dropped);
            config.YawModeKeyName = KeyList(legacy.YawModeKey, KeyCode.H, "YawModeKey", dropped);

            const string s = LegacyConfigReader.Sensitivity;
            const string pos = LegacyConfigReader.Position;
            LegacyPoseShaping.Record(legacy.YawSensitivity, Shipped.YawSensitivity, s, "YawSensitivity", poseShaping, dropped);
            LegacyPoseShaping.Record(legacy.PitchSensitivity, Shipped.PitchSensitivity, s, "PitchSensitivity", poseShaping, dropped);
            LegacyPoseShaping.Record(legacy.RollSensitivity, Shipped.RollSensitivity, s, "RollSensitivity", poseShaping, dropped);
            LegacyPoseShaping.Record(legacy.InvertYaw, Shipped.InvertYaw, s, "InvertYaw", poseShaping, dropped);
            LegacyPoseShaping.Record(legacy.InvertPitch, Shipped.InvertPitch, s, "InvertPitch", poseShaping, dropped);
            LegacyPoseShaping.Record(legacy.InvertRoll, Shipped.InvertRoll, s, "InvertRoll", poseShaping, dropped);
            LegacyPoseShaping.Record(legacy.PositionSensitivityX, Shipped.PositionSensitivityX, pos, "PositionSensitivityX", poseShaping, dropped);
            LegacyPoseShaping.Record(legacy.PositionSensitivityY, Shipped.PositionSensitivityY, pos, "PositionSensitivityY", poseShaping, dropped);
            LegacyPoseShaping.Record(legacy.PositionSensitivityZ, Shipped.PositionSensitivityZ, pos, "PositionSensitivityZ", poseShaping, dropped);
            LegacyTrackerPivot.Record(legacy.TrackerPivotForward, Shipped.TrackerPivotForward, pos, "TrackerPivotForward", dropped);

            var follows = new LegacyFollowsDefaultsIni();
            follows.Setting(ConfigConcepts.UdpPort, legacy.UDPPort, Shipped.UDPPort);
            follows.Setting(ConfigConcepts.EnableOnStartup, legacy.EnabledOnStartup, Shipped.EnabledOnStartup);
            follows.Setting(ConfigConcepts.WorldSpaceYaw, legacy.WorldSpaceYaw, Shipped.WorldSpaceYaw);
            follows.TrackingMode(legacy.PositionEnabled, Shipped.PositionEnabled);
            follows.Setting(ConfigConcepts.LocalSmoothing, legacy.LocalSmoothing, Shipped.LocalSmoothing);
            follows.Setting(ConfigConcepts.RemoteSmoothing, legacy.RemoteSmoothing, Shipped.RemoteSmoothing);
            follows.Setting(ConfigConcepts.PositionLimitX, legacy.PositionLimitX, Shipped.PositionLimitX);
            follows.Setting(ConfigConcepts.PositionLimitY, legacy.PositionLimitY, Shipped.PositionLimitY);
            follows.Setting(ConfigConcepts.PositionLimitYDown, legacy.PositionLimitY, Shipped.PositionLimitY);
            follows.Setting(ConfigConcepts.PositionLimitZ, legacy.PositionLimitZ, Shipped.PositionLimitZ);
            follows.Setting(ConfigConcepts.PositionLimitZBack, legacy.PositionLimitZBack, Shipped.PositionLimitZBack);
            // The Ctrl+Shift letter was fixed in code, so a hotkey is unchanged exactly where its key is.
            follows.Setting(ConfigConcepts.ToggleKey, legacy.ToggleKey, Shipped.ToggleKey);
            follows.Setting(ConfigConcepts.CycleTrackingModeKey, legacy.CycleTrackingModeKey, Shipped.CycleTrackingModeKey);
            follows.Setting(ConfigConcepts.YawModeKey, legacy.YawModeKey, Shipped.YawModeKey);
            follows.Setting(ConfigConcepts.LightFollowsHead, legacy.FlashlightFollowsHead, Shipped.FlashlightFollowsHead);
            follows.Setting(ConfigConcepts.LightMultiplier, legacy.FlashlightMultiplier, Shipped.FlashlightMultiplier);
            return follows;
        }

        /// <summary>
        /// A legacy hotkey as a key list: the key the player set, through core's N3 (a Ctrl, Shift
        /// or Alt key alone unbinds and is logged), then the Ctrl+Shift letter ChordHotkeys polled
        /// beside it. A KeyCode with no name in core's key list (a number BepInEx read into the
        /// enum) keeps its text, which the owner cannot write, so the import is deferred rather
        /// than the key changed.
        /// </summary>
        private static string KeyList(KeyCode primary, KeyCode chordLetter, string legacyKey, ICollection<DroppedValue> dropped)
        {
            var items = new List<string>();
            string plain;
            try
            {
                plain = LegacyNormalisations.KeyCodeToBindings((int)primary, LegacyConfigReader.Keybindings, legacyKey, dropped);
            }
            catch (ArgumentException)
            {
                plain = primary.ToString();
            }
            if (plain.Length > 0) items.Add(plain);
            items.Add(KeyBindings.Format(new[] { new KeyBinding(KeyModifiers.Ctrl | KeyModifiers.Shift, (int)chordLetter) }));
            return string.Join(", ", items.ToArray());
        }
    }
}
