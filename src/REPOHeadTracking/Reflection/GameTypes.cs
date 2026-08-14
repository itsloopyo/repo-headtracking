using System;

namespace REPOHeadTracking.Reflection
{
    /// <summary>
    /// Resolves R.E.P.O.'s own types by name out of the loaded assemblies.
    ///
    /// Every game member this mod touches is reached this way rather than through a
    /// compile-time reference, so the build stays game-DLL-agnostic (CI compiles
    /// against Unity stubs only).
    /// </summary>
    internal static class GameTypes
    {
        /// <summary>
        /// The type with this name from any loaded assembly, or null when nothing has
        /// loaded it yet - which is the normal state during boot, before the game's
        /// assembly is in play.
        /// </summary>
        public static Type Find(string name)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = asm.GetType(name, throwOnError: false);
                if (type != null)
                    return type;
            }
            return null;
        }
    }
}
