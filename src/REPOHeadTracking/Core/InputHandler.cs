using System;
using CameraUnlock.Core.Input;
using CameraUnlock.Core.Unity.Extensions;
using REPOHeadTracking.Config;
using UnityEngine;

namespace REPOHeadTracking.Core
{
    /// <summary>
    /// Polls the hotkey lists from CameraUnlock.ini. Every entry of a list fires its action, the
    /// Ctrl+Shift chords included.
    /// </summary>
    internal sealed class InputHandler
    {
        private readonly KeyBinding[] _toggleKeys;
        private readonly KeyBinding[] _cycleTrackingModeKeys;
        private readonly KeyBinding[] _yawModeKeys;

        public event Action OnTogglePressed;
        public event Action OnCycleTrackingModePressed;
        public event Action OnToggleYawModePressed;

        /// <summary>The bound keys, for the startup notification.</summary>
        public string HotkeySummary { get; }

        public InputHandler(REPOConfig config)
        {
            _toggleKeys = Parse("ToggleKey", config.ToggleKeyName);
            _cycleTrackingModeKeys = Parse("CycleTrackingModeKey", config.CycleTrackingModeKeyName);
            _yawModeKeys = Parse("YawModeKey", config.YawModeKeyName);
            HotkeySummary =
                $"[{config.ToggleKeyName}] Toggle, " +
                $"[{config.CycleTrackingModeKeyName}] Cycle Mode, " +
                $"[{config.YawModeKeyName}] Yaw";
        }

        // The table's hotkey codec has read every list of a loaded file, and the legacy import
        // writes only key lists, so a list that does not parse is a bug.
        private static KeyBinding[] Parse(string row, string text)
        {
            KeyBinding[] bindings;
            string error;
            if (!KeyBindings.TryParse(text, out bindings, out error))
                throw new InvalidOperationException("[Hotkeys] " + row + "=" + text + ": " + error);
            return bindings;
        }

        public void CheckInput()
        {
            // Common case: nothing pressed this frame. Skip the per-key probes.
            if (!Input.anyKeyDown)
                return;

            if (KeyBindingInput.IsTriggered(_toggleKeys)) OnTogglePressed?.Invoke();
            if (KeyBindingInput.IsTriggered(_cycleTrackingModeKeys)) OnCycleTrackingModePressed?.Invoke();
            if (KeyBindingInput.IsTriggered(_yawModeKeys)) OnToggleYawModePressed?.Invoke();
        }
    }
}
