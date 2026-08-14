using System;
using CameraUnlock.Core.Protocol;
using CameraUnlock.Core.Tracking;
using CameraUnlock.Core.Unity.Tracking;
using CameraUnlock.Core.Unity.UI;
using REPOHeadTracking.Camera;
using REPOHeadTracking.Config;
using REPOHeadTracking.State;
using UnityEngine;

namespace REPOHeadTracking.Core
{
    /// <summary>
    /// Owns the whole head-tracking runtime and drives it from Unity's per-frame
    /// callbacks. Lives on its own DontDestroyOnLoad GameObject rather than on the
    /// BepInEx manager object, which R.E.P.O. tears down during its first scene load
    /// (see <see cref="REPOHeadTrackingPlugin"/>).
    /// </summary>
    internal sealed class HeadTrackingHost : MonoBehaviour
    {
        private const float StartupNotificationSeconds = 4f;
        private const float StatusNotificationSeconds = 1.5f;
        private const float PortBusyNotificationSeconds = 8f;

        private static readonly int TrackingModeCount = Enum.GetValues(typeof(TrackingMode)).Length;

        public static HeadTrackingHost Instance { get; private set; }

        public bool TrackingEnabled { get; private set; }

        private ConfigManager _config;
        private OpenTrackReceiver _receiver;
        private ViewMatrixTrackingController _cameraController;
        private GameStateDetector _gameStateDetector;
        private InputHandler _inputHandler;
        private NotificationUI _notificationUI;
        private AnchoredOffsetCompensator _crosshair;
        private FlashlightRenderHook _flashlightHook;
        private bool _wasReceiving;
        private TrackingMode _trackingMode;
        private bool _initialized;

        private static BepInEx.Logging.ManualLogSource Log => REPOHeadTrackingPlugin.Log;

        private void Awake()
        {
            Instance = this;
            _config = REPOHeadTrackingPlugin.Settings;

            BuildTracking();
            BuildGameStateDetector();
            BuildInput();
            BuildUI();
            BuildFlashlight();

            bool listening = _receiver.Start(_config.UDPPort.Value);
            TrackingEnabled = _config.EnabledOnStartup.Value;
            _initialized = true;

            AnnounceStartup(listening);
        }

        private void BuildTracking()
        {
            var pipeline = TrackingPipeline.Build(_config, msg => Log.LogInfo(msg));
            _receiver = pipeline.Receiver;
            _cameraController = pipeline.Controller;

            _cameraController.WorldSpaceYaw = _config.WorldSpaceYaw.Value;

            // ProcessFrame consumes the tracker app's recenter request itself and has
            // already recentered by the time this fires, so this only reports it.
            // Polling the receiver here as well would race it: the request is claimed
            // with a single Interlocked.Exchange, so exactly one of the two consumers
            // sees a given CENTER press and the feedback would come and go at random.
            _cameraController.OnRemoteRecenter = ReportRecentered;

            // Seed the mode from config so the first cycle press transitions away
            // from the current mode rather than back to it.
            SetTrackingMode(_config.PositionEnabled.Value
                ? TrackingMode.RotationAndPosition
                : TrackingMode.RotationOnly);
            _cameraController.Enable();
        }

        private void BuildGameStateDetector()
        {
            _gameStateDetector = new GameStateDetector();
            _gameStateDetector.StateChanged += OnGameStateChanged;
            _gameStateDetector.Initialize();
        }

        private void BuildInput()
        {
            _inputHandler = new InputHandler(_config);
            _inputHandler.OnTogglePressed += HandleToggle;
            _inputHandler.OnRecenterPressed += HandleRecenter;
            _inputHandler.OnCycleTrackingModePressed += HandleCycleTrackingMode;
            _inputHandler.OnToggleYawModePressed += HandleToggleYawMode;
        }

        private void BuildUI()
        {
            _notificationUI = new NotificationUI();
            _crosshair = new AnchoredOffsetCompensator(GameCrosshair.Resolve);
        }

        private void BuildFlashlight()
        {
            if (!_config.FlashlightFollowsHead.Value)
                return;

            // Attached after the controller's own render hook (BuildTracking runs first),
            // so by the time it fires the camera carries the tracked matrix this frame's
            // world is rendered with.
            _flashlightHook = new FlashlightRenderHook(
                _cameraController, () => _gameStateDetector.IsGameplayActive);
            _flashlightHook.Attach();
        }

        private void AnnounceStartup(bool listening)
        {
            Log.LogInfo($"Head tracking runtime started. Tracking {(TrackingEnabled ? "enabled" : "disabled")}");

            int port = _config.UDPPort.Value;

            if (listening)
            {
                Log.LogInfo($"Listening on UDP port {port}");

                if (_config.ShowStartupNotification.Value)
                {
                    string status = TrackingEnabled ? "Head Tracking: ON" : "Head Tracking: OFF";
                    _notificationUI.ShowNotification($"{status}\n{_inputHandler.HotkeySummary}", StartupNotificationSeconds);
                }
            }
            else if (_config.ShowConnectionNotifications.Value)
            {
                // The receiver logs the failed bind and keeps polling the port, so this
                // resolves itself the moment the other app lets go - the player just
                // needs to know on screen why nothing is moving until then.
                _notificationUI.ShowNotification(
                    $"UDP port {port} is in use by another app.\nClose it and head tracking starts on its own.",
                    NotificationType.Warning,
                    PortBusyNotificationSeconds);
            }
        }

        private void Update()
        {
            if (!_initialized) return;
            _inputHandler.CheckInput();
            _gameStateDetector.Update();
            _notificationUI.Update();
            MonitorConnectionState();
        }

        private void LateUpdate()
        {
            if (!_initialized) return;
            _cameraController.ProcessFrame(TrackingEnabled && _gameStateDetector.IsGameplayActive);
            UpdateCrosshair();
        }

        /// <summary>
        /// Slides the game's crosshair to where the clean aim direction lands in the
        /// head-tracked view, so it stays on the point the player is actually aiming at.
        ///
        /// R.E.P.O.'s HUD is a world-space canvas and the world renders through a render
        /// texture whose aspect is independent of the window, so the offset has to go
        /// through the canvas rect - screen pixels are meaningless here.
        /// </summary>
        private void UpdateCrosshair()
        {
            Vector2 ndc;
            if (_gameStateDetector.IsGameplayActive && _cameraController.TryGetAimNdcOffset(out ndc))
            {
                _crosshair.ApplyNdcOffset(ndc);
            }
            else
            {
                _crosshair.Restore();
            }
        }

        private void OnGUI()
        {
            _notificationUI?.Draw();
        }

        private void OnDestroy()
        {
            Log.LogInfo("Head tracking runtime stopping...");

            if (_inputHandler != null)
            {
                _inputHandler.OnTogglePressed -= HandleToggle;
                _inputHandler.OnRecenterPressed -= HandleRecenter;
                _inputHandler.OnCycleTrackingModePressed -= HandleCycleTrackingMode;
                _inputHandler.OnToggleYawModePressed -= HandleToggleYawMode;
            }
            if (_gameStateDetector != null)
            {
                _gameStateDetector.StateChanged -= OnGameStateChanged;
            }

            _flashlightHook?.Detach();
            _crosshair?.Restore();
            _cameraController?.Disable();
            _receiver?.Dispose();

            Instance = null;
        }

        private void MonitorConnectionState()
        {
            bool isReceiving = _receiver.IsReceiving;
            if (isReceiving == _wasReceiving)
                return;

            if (_config.ShowConnectionNotifications.Value)
            {
                if (isReceiving)
                {
                    _notificationUI.ShowConnectionEstablished();
                    Log.LogInfo("OpenTrack connection established");
                }
                else
                {
                    _notificationUI.ShowConnectionLost();
                    Log.LogInfo("OpenTrack connection lost");
                }
            }
            _wasReceiving = isReceiving;
        }

        private void HandleToggle()
        {
            TrackingEnabled = !TrackingEnabled;
            if (TrackingEnabled)
            {
                _cameraController.OnTrackingEnabled();
                _notificationUI.ShowTrackingEnabled();
                Log.LogInfo("Head tracking enabled");
            }
            else
            {
                _cameraController.OnTrackingDisabled();
                _notificationUI.ShowTrackingDisabled();
                Log.LogInfo("Head tracking disabled");
            }
        }

        private void HandleRecenter()
        {
            _cameraController.Recenter();
            ReportRecentered();
        }

        private void ReportRecentered()
        {
            _notificationUI.ShowRecentered();
            Log.LogInfo("Head tracking recentered");
        }

        private void HandleCycleTrackingMode()
        {
            SetTrackingMode((TrackingMode)(((int)_trackingMode + 1) % TrackingModeCount));

            string label = "Tracking: " + _trackingMode.Description();
            _notificationUI.ShowNotification(label, NotificationType.Info, StatusNotificationSeconds);
            Log.LogInfo(label);
        }

        private void SetTrackingMode(TrackingMode mode)
        {
            _trackingMode = mode;
            _cameraController.RotationEnabled = mode != TrackingMode.PositionOnly;
            _cameraController.PositionEnabled = mode != TrackingMode.RotationOnly;
        }

        private void HandleToggleYawMode()
        {
            _cameraController.WorldSpaceYaw = !_cameraController.WorldSpaceYaw;
            _notificationUI.ShowNotification(
                _cameraController.WorldSpaceYaw ? "Yaw: World-locked" : "Yaw: Camera-local",
                NotificationType.Info,
                StatusNotificationSeconds);
            Log.LogInfo($"Yaw mode: {(_cameraController.WorldSpaceYaw ? "world-locked" : "camera-local")}");
        }

        private void OnGameStateChanged(GameState newState)
        {
            Log.LogInfo($"Game state: {newState}");

            if (newState != GameState.Gameplay)
                _cameraController.ResetState();
            else if (TrackingEnabled)
                _cameraController.OnTrackingEnabled();
        }
    }
}
