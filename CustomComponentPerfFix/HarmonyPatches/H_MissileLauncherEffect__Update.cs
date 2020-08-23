using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleTech.Rendering;
using Harmony;

namespace RogueTechPerfFixes.HarmonyPatches
{
    [HarmonyPatch(typeof(MissileLauncherEffect), "Update")]
    public static class H_MissileLauncherEffect__Update
    {
        [HarmonyPriority(900)]
        public static void Prefix()
        {
            BTLightController.InBatchProcess = true;
        }

        public static void Postfix()
        {
            BTLightController.InBatchProcess = false;
            if (BTLightController.LightAdded)
            {
                H_BTLightController.Lights().Sort();
                BTLightController.LightAdded = false;
            }
        }
    }
}
