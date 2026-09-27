using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using CameraUnlock.Core.Config;
using REPOHeadTracking.Config;
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
        internal static REPOConfig Settings { get; private set; }

        /// <summary>
        /// The one-line messages the config owner has for the player, shown on screen once the
        /// runtime's notification UI is up.
        /// </summary>
        internal static readonly List<string> ConfigMessages = new List<string>();

        private static ConfigOwner<REPOConfig> _configOwner;

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo($"{PluginName} v{PluginVersion} initializing...");

            LoadConfig();

            // R.E.P.O. destroys BepInEx's manager GameObject during the first scene
            // load, which takes this component (and every Update/LateUpdate it would
            // have received) with it. Run the tracking from a GameObject of our own,
            // created on the first scene load so DontDestroyOnLoad actually sticks,
            // and put it back if a later load takes that out too. The sceneLoaded
            // subscription is a static event, so it survives this component dying.
            SceneManager.sceneLoaded += (scene, mode) => EnsureHost();
        }

        /// <summary>
        /// The settings live in BepInEx\config\CameraUnlock.ini, read and written by core's config
        /// owner, with rows set to default following the player's Defaults.ini. Nothing is bound
        /// through BepInEx's ConfigFile at runtime, so ConfigurationManager does not list them.
        /// The plugin's .cfg, which earlier builds read, is imported once while CameraUnlock.ini is
        /// absent and never written.
        /// </summary>
        private void LoadConfig()
        {
            _configOwner = new ConfigOwner<REPOConfig>(
                REPOConfigOwner.Options(Config, DefaultsFile.PerUser(), message =>
                {
                    Log.LogWarning(message);
                    ConfigMessages.Add(message);
                }));
            ConfigLoadResult<REPOConfig> loaded = _configOwner.Load();
            bool usable = loaded.Status == ConfigLoadStatus.Canonical
                          || loaded.Status == ConfigLoadStatus.Migrated
                          || loaded.Status == ConfigLoadStatus.Created;
            WriteConfigLog(loaded.Log, loaded.Diagnostics, usable);
            Log.LogInfo("Config: " + loaded.Status);

            // The published build did not load at all on a .cfg BepInEx refused to read.
            if (loaded.Status == ConfigLoadStatus.LegacyRefused)
            {
                throw new InvalidOperationException(loaded.Reason);
            }
            Settings = loaded.Config;
        }

        // The owner writes each diagnostic as "<path>: <description>" among lines that only report
        // what it did, so the complaints are picked out by their text.
        private static void WriteConfigLog(IEnumerable<string> lines, IEnumerable<CanonicalDiagnostic> diagnostics, bool usable)
        {
            var complaints = new HashSet<string>();
            foreach (CanonicalDiagnostic diagnostic in diagnostics) complaints.Add(diagnostic.Describe());
            foreach (string line in lines)
            {
                bool complaint = false;
                foreach (string c in complaints)
                {
                    if (line.EndsWith(c, StringComparison.Ordinal)) complaint = true;
                }
                if (usable && !complaint) Log.LogInfo(line);
                else Log.LogWarning(line);
            }
        }

        /// <summary>
        /// Called after a toggle has applied its new value. A save that fails is logged and the
        /// session keeps the new value.
        /// </summary>
        internal static void SaveConfig(Action<REPOConfig> change)
        {
            ConfigSaveResult saved = _configOwner.Save(change);
            if (saved.Status == ConfigSaveStatus.Saved)
            {
                foreach (string line in saved.Log) Log.LogInfo(line);
                return;
            }
            foreach (string line in saved.Log) Log.LogWarning(line);
            Log.LogWarning("Config not saved (" + saved.Status + "): " + saved.Reason + " The change applies to this session only.");
        }

        private static void EnsureHost()
        {
            if (HeadTrackingHost.Instance != null)
                return;

            var host = new GameObject("REPOHeadTrackingHost");
            UnityEngine.Object.DontDestroyOnLoad(host);
            host.AddComponent<HeadTrackingHost>();
        }
    }
}
