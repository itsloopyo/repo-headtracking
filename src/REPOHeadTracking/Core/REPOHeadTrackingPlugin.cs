using BepInEx;
using BepInEx.Logging;
using REPOHeadTracking.Config;
using REPOHeadTracking.Legacy;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace REPOHeadTracking.Core
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInProcess("REPO.exe")]
    public class REPOHeadTrackingPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.cameraunlock.repo.headtracking";
        public const string PluginName = "R.E.P.O. Head Tracking";
        public const string PluginVersion = "0.0.0";

        internal static ManualLogSource Log { get; private set; }
        internal static ConfigManager Settings { get; private set; }

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo($"{PluginName} v{PluginVersion} initializing...");

            // The frozen reader binds every definition with saving off and writes nothing. The
            // runtime binds the same definitions after it and writes the file once, as the first
            // Bind with saving on used to.
            LegacyConfigReader.Read(Config);
            Config.SaveOnConfigSet = true;
            Settings = new ConfigManager();
            Settings.Initialize(Config);
            Config.Save();

            // R.E.P.O. destroys BepInEx's manager GameObject during the first scene
            // load, which takes this component (and every Update/LateUpdate it would
            // have received) with it. Run the tracking from a GameObject of our own,
            // created on the first scene load so DontDestroyOnLoad actually sticks,
            // and put it back if a later load takes that out too. The sceneLoaded
            // subscription is a static event, so it survives this component dying.
            SceneManager.sceneLoaded += (scene, mode) => EnsureHost();
        }

        private static void EnsureHost()
        {
            if (HeadTrackingHost.Instance != null)
                return;

            var host = new GameObject("REPOHeadTrackingHost");
            Object.DontDestroyOnLoad(host);
            host.AddComponent<HeadTrackingHost>();
        }
    }
}
