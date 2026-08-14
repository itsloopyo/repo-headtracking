using System;
using System.Reflection;
using REPOHeadTracking.Reflection;
using UnityEngine;

namespace REPOHeadTracking.Camera
{
    /// <summary>
    /// Resolves R.E.P.O.'s own crosshair - the uGUI Image driven by the game's Aim
    /// component, reached through its Aim.instance singleton.
    ///
    /// Aim only ever writes localScale, localRotation, sprite and colour, never
    /// position, so offsetting its anchoredPosition does not fight the game.
    ///
    /// Looked up by reflection so the build stays game-DLL-agnostic (CI compiles
    /// against Unity stubs only).
    /// </summary>
    internal static class GameCrosshair
    {
        private static FieldInfo _instanceField;
        private static bool _resolved;

        /// <summary>
        /// The crosshair GameObject, or null when the game UI is not up yet
        /// (boot, menus, level load).
        /// </summary>
        public static GameObject Resolve()
        {
            if (!_resolved && !ResolveReflection())
                return null;

            var aim = _instanceField.GetValue(null) as MonoBehaviour;
            if (aim == null)
                return null;

            return aim.gameObject;
        }

        private static bool ResolveReflection()
        {
            Type aimType = GameTypes.Find("Aim");
            if (aimType == null)
                return false;

            _instanceField = aimType.GetField("instance",
                BindingFlags.Public | BindingFlags.Static);
            if (_instanceField == null)
                return false;

            _resolved = true;
            return true;
        }
    }
}
