using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleTech;
using CustomUnits;
using Harmony;

namespace RogueTechPerfFixes.HarmonyPatches
{
    [HarmonyPatch(typeof(TerraiWaterHelper), nameof(TerraiWaterHelper.HasWater))]
    public static class H_TerraiWaterHelper_HasWater
    {
        public static bool Prefix(MapTerrainDataCell cell, ref bool __result)
        {
            return !CacheManager.WatchCache.TryGetValue(cell, out __result);
        }

        public static void Postfix(MapTerrainDataCell cell, ref bool __result)
        {
            CacheManager.WatchCache[cell] = __result;
        }
    }
}
