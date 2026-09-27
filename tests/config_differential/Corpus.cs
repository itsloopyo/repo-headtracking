using System.Collections.Generic;
using System.IO;
using CameraUnlock.Core.Config;
using CameraUnlock.Core.Config.Testing;
using REPOHeadTracking.Legacy;

namespace REPOHeadTracking.Tests.ConfigDifferential
{
    /// <summary>
    /// The differential test's inputs: no file, an empty file, the first-run output of the one
    /// published build (the rolling dev pre-release; there is no v* release and no predecessor
    /// repo), and core's mutation corpus over it. That build shipped no config file and no
    /// launcher seed of one. tests/config_differential/provenance.tsv says where each came from.
    /// </summary>
    internal static class Corpus
    {
        public static readonly string[] PublishedTags = { "dev" };

        private static readonly string[] None = new string[0];

        /// <summary>
        /// One descriptor per key the frozen reader binds: an alternate value BepInEx reads, and
        /// one value outside each AcceptableValueRange, which BepInEx clamps. No legacy row names
        /// a chord: ChordHotkeys added the Ctrl+Shift letter in code.
        /// </summary>
        public static readonly MutationKey[] Keys =
        {
            Bool(LegacyConfigReader.General, "EnabledOnStartup", "false"),
            Bool(LegacyConfigReader.General, "ShowStartupNotification", "false"),
            Bool(LegacyConfigReader.General, "WorldSpaceYaw", "false"),
            Bool(LegacyConfigReader.General, "FlashlightFollowsHead", "false"),
            Number(LegacyConfigReader.General, "FlashlightMultiplier", "1", "-0.1", "5.5"),
            Bool(LegacyConfigReader.UI, "ShowConnectionNotifications", "false"),
            Hotkey(LegacyConfigReader.Keybindings, "ToggleKey", "F8"),
            Hotkey(LegacyConfigReader.Keybindings, "CycleTrackingModeKey", "F10"),
            Hotkey(LegacyConfigReader.Keybindings, "YawModeKey", "F7"),
            Number(LegacyConfigReader.Network, "UDPPort", "5555", "1023", "65536"),
            Number(LegacyConfigReader.Sensitivity, "YawSensitivity", "2", "0.05", "3.5"),
            Number(LegacyConfigReader.Sensitivity, "PitchSensitivity", "2", "0.05", "3.5"),
            Number(LegacyConfigReader.Sensitivity, "RollSensitivity", "2", "-0.1", "3.5"),
            Bool(LegacyConfigReader.Sensitivity, "InvertYaw", "true"),
            Bool(LegacyConfigReader.Sensitivity, "InvertPitch", "false"),
            Bool(LegacyConfigReader.Sensitivity, "InvertRoll", "true"),
            Number(LegacyConfigReader.Smoothing, "LocalSmoothing", "0.5", "-0.1", "1.1"),
            Number(LegacyConfigReader.Smoothing, "RemoteSmoothing", "0.3", "-0.1", "1.1"),
            Bool(LegacyConfigReader.Position, "PositionEnabled", "false"),
            Number(LegacyConfigReader.Position, "PositionSensitivityX", "2", "-0.1", "5.5"),
            Number(LegacyConfigReader.Position, "PositionSensitivityY", "2", "-0.1", "5.5"),
            Number(LegacyConfigReader.Position, "PositionSensitivityZ", "2", "-0.1", "5.5"),
            Number(LegacyConfigReader.Position, "PositionLimitX", "0.25", "0.005", "0.6"),
            Number(LegacyConfigReader.Position, "PositionLimitY", "0.25", "0.005", "0.6"),
            Number(LegacyConfigReader.Position, "PositionLimitZ", "0.25", "0.005", "0.6"),
            Number(LegacyConfigReader.Position, "PositionLimitZBack", "0.25", "0.005", "0.6"),
            Number(LegacyConfigReader.Position, "TrackerPivotForward", "0.1", "-0.01", "0.25"),
        };

        /// <summary>Every input as (name, bytes); null bytes is no file.</summary>
        public static IEnumerable<KeyValuePair<string, byte[]>> Inputs()
        {
            yield return new KeyValuePair<string, byte[]>("no file", null);
            yield return new KeyValuePair<string, byte[]>("empty file", new byte[0]);
            foreach (string tag in PublishedTags)
            {
                yield return new KeyValuePair<string, byte[]>(tag + " first run", FirstRun(tag));
            }
            foreach (IniMutation m in IniMutations.Generate(FirstRun("dev"), LegacyConfigReader.Keys, Keys))
            {
                yield return new KeyValuePair<string, byte[]>("corpus: " + m.Name, m.Bytes);
            }
        }

        /// <summary>The .cfg a published build wrote at its first start, extracted from its release DLL once.</summary>
        public static byte[] FirstRun(string tag)
        {
            return File.ReadAllBytes(Path.Combine(RepoPaths.Root, "tests", "config_differential", "data", "first-run", tag + ".cfg"));
        }

        private static MutationKey Number(string section, string key, string alternate, params string[] outOfRange)
        {
            return new MutationKey(section, key, alternate, outOfRange, false, new ChordSwitch[0]);
        }

        private static MutationKey Bool(string section, string key, string alternate)
        {
            return new MutationKey(section, key, alternate, None, false, new ChordSwitch[0]);
        }

        private static MutationKey Hotkey(string section, string key, string alternate)
        {
            return new MutationKey(section, key, alternate, None, true, new ChordSwitch[0]);
        }
    }
}
