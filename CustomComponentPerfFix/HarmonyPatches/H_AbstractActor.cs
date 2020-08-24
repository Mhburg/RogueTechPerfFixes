using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleTech;
using Harmony;

namespace RogueTechPerfFixes.HarmonyPatches
{
    [HarmonyPatch(typeof(AbstractActor))]
    [HarmonyPatch(nameof(AbstractActor.IsFuryInspired))]
    [HarmonyPatch(MethodType.Getter)]
    public static class H_AbstractActor
    {
        public static bool Prefix(ref bool __result)
        {
            // If not in skirmish, return false immediately.
            if (UnityGameInstance.BattleTechGame.Simulation != null)
            {
                __result = false;
                return false;
            }

            return true;
        }
    }
}
