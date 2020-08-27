using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Configuration;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Harmony;

namespace RogueTechPerfFixes
{
    public static class Mod
    {
        private const string HARMONY_PATCH_PATH = "HarmonyPatches.dll";

        private static Settings _settings;

        public static Settings Settings
        {
            get
            {
                if (_settings == null)
                    _settings = JsonConvert.DeserializeObject<Settings>(GetSetting());

                return _settings;
            }

            set => _settings = value;
        }

        public static Version Version { get; } = Assembly.GetExecutingAssembly().GetName().Version;

        public static void Init(string modDirectory, string settingsJSON)
        {
            try
            {
                RTPFLogger.InitCriticalLogger(modDirectory);
                Settings = JsonConvert.DeserializeObject<Settings>(settingsJSON);
                HarmonyUtils.Harmony.PatchAll(
                    Assembly.LoadFrom(Path.Combine(
                        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), HARMONY_PATCH_PATH)));
            }
            catch (Exception e)
            {
                RTPFLogger.LogCritical(e.ToString());
            }

        }

        private static string GetSetting()
        {
            return File.ReadAllText(
                Path.Combine(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                    , "mod.json"));
        }
    }
}
