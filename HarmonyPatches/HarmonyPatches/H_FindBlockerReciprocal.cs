using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BattleTech;
using Harmony;
using UnityEngine;

namespace RogueTechPerfFixes.HarmonyPatches
{
    [Obsolete]
    [HarmonyPatch(typeof(PathNodeGrid), nameof(PathNodeGrid.FindBlockerReciprocal))]
    public static class H_FindBlockerReciprocal
    {
        private static volatile bool _taskResult = false;

        [HarmonyPriority(0)]
        public static bool Prefix(PathNodeGrid __instance, Vector3 from, Vector3 to, ref bool __result)
        {
            int done = 0;
            Task task = Task.Run(() =>
            {
                _taskResult = __instance.FindBlockerBetween(from, to);
                Interlocked.Increment(ref done);
            });

            bool result = __instance.FindBlockerBetween(to, from);

            while (done == 0)
            {
                if (task.IsFaulted)
                {
                    _taskResult = false;
                    Utils.Logger.LogError(
                        $"{Utils.LOG_HEADER}FindBlockerBetween runs into errors",
                        task.Exception is AggregateException agg ? agg.Flatten() : task.Exception);
                    break;
                }

                Interlocked.CompareExchange(ref done, 1, 1);
            }

            __result = result && _taskResult;
            return false;
        }
    }
}
