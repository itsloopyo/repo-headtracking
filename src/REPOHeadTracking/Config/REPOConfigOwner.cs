using System;
using BepInEx.Configuration;
using CameraUnlock.Core.Config;

namespace REPOHeadTracking.Config
{
    internal static class REPOConfigOwner
    {
        public const string FileName = "CameraUnlock.ini";

        /// <summary>
        /// The owner of BepInEx\config\CameraUnlock.ini: the plugin's own .cfg beside it is the
        /// legacy file, imported through the plugin's ConfigFile while CameraUnlock.ini is absent.
        /// The mod passes <see cref="DefaultsFile.PerUser"/>, a test a scratch file.
        /// </summary>
        public static ConfigOwnerOptions<REPOConfig> Options(ConfigFile pluginConfig, DefaultsFile defaults, Action<string> statusSink)
        {
            string legacyPath = pluginConfig.ConfigFilePath;
            return new ConfigOwnerOptions<REPOConfig>
            {
                Path = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(legacyPath), FileName),
                Table = REPOConfig.Table(),
                Header = new RenderHeader(REPOConfig.DisplayName),
                Import = LegacyMigration.Import(pluginConfig),
                LegacySourcePath = legacyPath,
                Defaults = defaults,
                StatusSink = statusSink,
            };
        }
    }
}
