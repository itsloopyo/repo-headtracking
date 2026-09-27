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

        // The table's hotkey codec has read every list of a loaded file. Only a legacy import the
        // owner deferred, over a key with no name, hands one over that does not parse; that
        // action then has no keys this session.
        private static KeyBinding[] Parse(string row, string text)
        {
            KeyBinding[] bindings;
            string error;
            if (KeyBindings.TryParse(text, out bindings, out error)) return bindings;
            REPOHeadTrackingPlugin.Log.LogError(row + "=" + text + " is not a hotkey list (" + error + "), so it has no keys this session.");
            return new KeyBinding[0];
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
