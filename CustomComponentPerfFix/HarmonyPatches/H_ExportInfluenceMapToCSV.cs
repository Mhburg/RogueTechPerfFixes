using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleTech;
using Harmony;

namespace RogueTechPerfFixes.HarmonyPatches
{
    [HarmonyPatch(typeof(InfluenceMapEvaluator), nameof(InfluenceMapEvaluator.ExportInfluenceMapToCSV))]
    public static class H_ExportInfluenceMapToCSV
    {
        /// <summary>
        /// Skip running an expansive logging method.
        /// </summary>
        /// <returns></returns>
        public static bool Prefix()
        {
            return false;
        }
    }
}
