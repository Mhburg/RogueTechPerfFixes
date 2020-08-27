using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleTech;
using Harmony;

namespace RogueTechPerfFixes.HarmonyPatches
{
    [HarmonyPatch(typeof(EffectManager), nameof(EffectManager.GetAllEffectsTargeting))]
    public static class H_EffectManager_GetAllEffectsTargeting
    {
        private const int _initCapacity = 16;

        private static long _counter = 0;

        public static bool Prefix(List<Effect> ___effects, object target, ref List<Effect> __result)
        {

            List<Effect> list = new List<Effect>(_initCapacity);
            for (int i = 0; i < ___effects.Count; i++)
            {
                if (___effects[i].CheckEffectTarget(target))
                {
                    list.Add(___effects[i]);
                }
            }

            __result = list;
            return true;
        }
    }
}
