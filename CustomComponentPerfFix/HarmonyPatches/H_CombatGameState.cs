using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleTech;
using BattleTech.UI;
using Harmony;

namespace RogueTechPerfFixes.HarmonyPatches
{
    [HarmonyPatch(typeof(CombatGameState))]
    [HarmonyPatch("_Init")]
    public static class H_CombatGameState_init
    {
        public static void Postfix()
        {
            //CacheManager.WatchCache.Clear();
        }

    }

    [HarmonyPatch(typeof(CombatGameState), nameof(CombatGameState.OnCombatGameDestroyed))]
    public static class H_CombatGameState_OnCombatGameDestroyed
    {
        public static void Postfix()
        {
            Utils.Logger.LogError($"{Utils.LOG_HEADER} Total time spent: {H_FindBlockerBetween.Time} on {H_FindBlockerBetween.Counter} calls, average : {H_FindBlockerBetween.Time / H_FindBlockerBetween.Counter}");
            Utils.Logger.LogError($"{Utils.LOG_HEADER}Slowest time: {H_FindBlockerBetween.Slowest}, Fastest time: {H_FindBlockerBetween.Fastest}");
            GC.Collect();
        }
    }
}
