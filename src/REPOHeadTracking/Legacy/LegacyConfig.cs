using UnityEngine;

namespace REPOHeadTracking.Legacy
{
    /// <summary>
    /// Frozen: every setting the last build before the canonical config read from
    /// BepInEx\config\com.cameraunlock.repo.headtracking.cfg, with that build's defaults.
    /// A default the runtime config later moves changes only what a new file holds, never what
    /// an old file without the key means. Never edit this file.
    /// </summary>
    internal sealed class LegacyConfig
    {
        // General
        public bool EnabledOnStartup = true;
        public bool ShowStartupNotification = true;
        public bool WorldSpaceYaw = true;
        public bool FlashlightFollowsHead = true;
        public float FlashlightMultiplier = 1.5f;

        // UI
        public bool ShowConnectionNotifications = true;

        // Keybindings
        public KeyCode ToggleKey = KeyCode.End;
        public KeyCode CycleTrackingModeKey = KeyCode.PageUp;
        public KeyCode YawModeKey = KeyCode.PageDown;

        // Network
        public int UDPPort = 4242;

        // Sensitivity
        public float YawSensitivity = 1.0f;
        public float PitchSensitivity = 1.0f;
        public float RollSensitivity = 1.0f;
        public bool InvertYaw = false;
        public bool InvertPitch = true;
        public bool InvertRoll = false;

        // Smoothing
        public float LocalSmoothing = 0.0f;
        public float RemoteSmoothing = 0.15f;

        // Position
        public bool PositionEnabled = true;
        public float PositionSensitivityX = 1.0f;
        public float PositionSensitivityY = 1.0f;
        public float PositionSensitivityZ = 1.0f;
        public float PositionLimitX = 0.30f;
        public float PositionLimitY = 0.20f;
        public float PositionLimitZ = 0.40f;
        public float PositionLimitZBack = 0.10f;
        public float TrackerPivotForward = 0.08f;
    }
}
