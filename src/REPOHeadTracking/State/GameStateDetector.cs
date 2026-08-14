using System;
using System.Reflection;
using REPOHeadTracking.Reflection;
using UnityEngine;

namespace REPOHeadTracking.State
{
    /// <summary>
    /// Detects gameplay vs menus/loading/paused for R.E.P.O.
    ///
    /// R.E.P.O. exposes its run state on GameDirector.instance.currentState (an enum).
    /// Active gameplay is the "Main" state; everything else (Load/Start/Outro/Result/
    /// End/Death) suppresses tracking.
    ///
    /// The state alone is not enough: GameDirector runs in the menu levels too and settles
    /// on Main there, so the main menu, lobby menu and splash screen all have to be excluded
    /// separately - SemiFunc.MenuLevel() is the game's own check for exactly those three.
    ///
    /// The escape menu changes neither, so it is caught through the cursor: R.E.P.O. draws
    /// its own pointer and keeps Cursor.visible false at all times, unlocking the cursor
    /// whenever a menu wants pointer input. So lockState alone marks a menu overlay - a
    /// Cursor.visible check would never fire.
    ///
    /// The map (held TAB by default) takes over the view, so it suppresses tracking too.
    /// Map.Instance.Active is the game's own flag for it, which beats polling the key -
    /// it follows the player's keybind and any other route that opens or closes the map.
    ///
    /// Looked up by reflection so the build stays game-DLL-agnostic (CI compiles against
    /// Unity stubs only). If GameDirector cannot be resolved yet, we report Loading.
    /// </summary>
    internal sealed class GameStateDetector
    {
        private const float CheckIntervalSeconds = 0.1f;
        private const string GameplayStateName = "Main";

        private GameState _currentState = GameState.Unknown;
        private float _lastCheckTime;

        private bool _reflectionReady;
        private FieldInfo _instanceField;
        private FieldInfo _currentStateField;
        private MethodInfo _menuLevelMethod;
        private FieldInfo _mapInstanceField;
        private FieldInfo _mapActiveField;
        private object _mainStateValue;

        public event Action<GameState> StateChanged;

        public bool IsGameplayActive => _currentState == GameState.Gameplay;

        /// <summary>
        /// Publishes the opening state. Subscribe to <see cref="StateChanged"/> first if
        /// the caller wants to see it.
        /// </summary>
        public void Initialize()
        {
            UpdateState();
        }

        public void Update()
        {
            // Unscaled: a game that pauses by zeroing timeScale would freeze Time.time,
            // and the detector would then never see the state change back out of it.
            if (Time.unscaledTime - _lastCheckTime < CheckIntervalSeconds)
                return;

            _lastCheckTime = Time.unscaledTime;
            UpdateState();
        }

        private void UpdateState()
        {
            var newState = DetectState();
            if (newState != _currentState)
            {
                _currentState = newState;
                StateChanged?.Invoke(newState);
            }
        }

        private GameState DetectState()
        {
            if (!_reflectionReady && !ResolveReflection())
                return GameState.Loading;

            object instance = _instanceField.GetValue(null);
            if (instance == null)
                return GameState.Loading;

            // Safe to call now: a live GameDirector means the level (and RunManager,
            // which MenuLevel reads) is up.
            if ((bool)_menuLevelMethod.Invoke(null, null))
                return GameState.MainMenu;

            object state = _currentStateField.GetValue(instance);
            if (!_mainStateValue.Equals(state))
                return GameState.Loading;

            if (Cursor.lockState == CursorLockMode.None || IsMapOpen())
                return GameState.Paused;

            return GameState.Gameplay;
        }

        /// <summary>
        /// True while the map overlay is up. Map.Instance is null in levels that have no
        /// map (the menus, and before the level's map is built), which is not the map
        /// being open.
        /// </summary>
        private bool IsMapOpen()
        {
            var map = _mapInstanceField.GetValue(null) as MonoBehaviour;
            if (map == null)
                return false;

            return (bool)_mapActiveField.GetValue(map);
        }

        private bool ResolveReflection()
        {
            if (!TryResolveGameDirector(out Type stateEnum))
                return false;

            if (!TryResolveMenuLevel())
                return false;

            if (!TryResolveMap())
                return false;

            // Reported like every other unresolved member rather than thrown: this runs
            // from Update, so a game build that renamed the state would otherwise throw
            // ten times a second forever instead of settling on Loading.
            if (Array.IndexOf(Enum.GetNames(stateEnum), GameplayStateName) < 0)
                return false;

            _mainStateValue = Enum.Parse(stateEnum, GameplayStateName);
            _reflectionReady = true;
            return true;
        }

        private bool TryResolveGameDirector(out Type stateEnum)
        {
            stateEnum = null;

            Type gameDirector = GameTypes.Find("GameDirector");
            if (gameDirector == null)
                return false;

            _instanceField = gameDirector.GetField("instance",
                BindingFlags.Public | BindingFlags.Static);
            _currentStateField = gameDirector.GetField("currentState",
                BindingFlags.Public | BindingFlags.Instance);

            if (_instanceField == null || _currentStateField == null)
                return false;

            stateEnum = _currentStateField.FieldType;
            return stateEnum.IsEnum;
        }

        private bool TryResolveMenuLevel()
        {
            Type semiFunc = GameTypes.Find("SemiFunc");
            if (semiFunc == null)
                return false;

            _menuLevelMethod = semiFunc.GetMethod("MenuLevel",
                BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);

            return _menuLevelMethod != null && _menuLevelMethod.ReturnType == typeof(bool);
        }

        private bool TryResolveMap()
        {
            Type mapType = GameTypes.Find("Map");
            if (mapType == null)
                return false;

            _mapInstanceField = mapType.GetField("Instance",
                BindingFlags.Public | BindingFlags.Static);
            _mapActiveField = mapType.GetField("Active",
                BindingFlags.Public | BindingFlags.Instance);

            return _mapInstanceField != null && _mapActiveField != null &&
                   _mapActiveField.FieldType == typeof(bool);
        }
    }
}
