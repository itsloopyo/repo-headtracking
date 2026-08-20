using System;
using CameraUnlock.Core.Unity.Extensions;
using REPOHeadTracking.Config;
using UnityEngine;

namespace REPOHeadTracking.Core
{
    internal sealed class InputHandler
    {
        private readonly ConfigManager _config;

        public event Action OnTogglePressed;
        public event Action OnCycleTrackingModePressed;
        public event Action OnToggleYawModePressed;

        public InputHandler(ConfigManager config)
        {
            _config = config;
        }

        /// <summary>
        /// The bound keys and their chord alternatives, for the startup notification.
        /// Lives here so the key-to-chord pairing is stated next to the dispatch that
        /// implements it, rather than restated by whoever draws the notification.
        /// </summary>
        public string HotkeySummary =>
            $"[{_config.ToggleKey.Value}/Ctrl+Shift+{ChordHotkeys.ToggleLetter}] Toggle, " +
            $"[{_config.CycleTrackingModeKey.Value}/Ctrl+Shift+{ChordHotkeys.PositionLetter}] Cycle Mode, " +
            $"[{_config.YawModeKey.Value}/Ctrl+Shift+{ChordHotkeys.FourthToggleLetter}] Yaw";

        public void CheckInput()
        {
            // Common case: nothing pressed this frame. Skip the per-key probes.
            if (!Input.anyKeyDown)
                return;

            Dispatch(_config.ToggleKey.Value, ChordHotkeys.ToggleLetter, OnTogglePressed);
            Dispatch(_config.CycleTrackingModeKey.Value, ChordHotkeys.PositionLetter, OnCycleTrackingModePressed);
            Dispatch(_config.YawModeKey.Value, ChordHotkeys.FourthToggleLetter, OnToggleYawModePressed);
        }

        private static void Dispatch(KeyCode primary, KeyCode chordLetter, Action handler)
        {
            if (ChordHotkeys.IsActionPressed(primary, chordLetter))
                handler?.Invoke();
        }
    }
}
