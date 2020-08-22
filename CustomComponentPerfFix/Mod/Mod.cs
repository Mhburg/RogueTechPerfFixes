using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Harmony;
using Newtonsoft.Json;

namespace RogueTechPerfFixes
{
    public static class Mod
    {
        public static Settings Settings;

        public static void Init(string modDirectory, string settingsJSON)
        {
            try
            {
                RTPFLogger.InitCriticalLogger(modDirectory);
                Settings = JsonConvert.DeserializeObject<Settings>(settingsJSON);
            }
            catch (Exception e)
            {
                RTPFLogger.LogCritical(e.ToString());
            }

            HarmonyUtils.Harmony.PatchAll();
        }
    }
}
