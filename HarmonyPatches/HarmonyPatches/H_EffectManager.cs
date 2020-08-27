using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleTech;
using Harmony;
using LowVisibility.Object;

namespace RogueTechPerfFixes.HarmonyPatches
{
    public static class H_EffectManager
    {
        #region Memoization of effects for targets

        private readonly static Dictionary<object, List<Effect>> _cache = new Dictionary<object, List<Effect>>();

        [HarmonyPatch(typeof(EffectManager), nameof(EffectManager.AddEffect))]
        public static class H_EffectManager_AddEffect
        {
            public static bool Prepare()
            {
                return Mod.Settings.Patch.Vanilla;
            }

            public static void Postfix(Effect effect)
            {
                if (!_cache.TryGetValue(effect.Target, out List<Effect> effects))
                {
                    _cache[effect.Target] = effects = new List<Effect>();
                }

                effects.Add(effect);
            }
        }

        [HarmonyPatch(typeof(EffectManager), nameof(EffectManager.CancelEffect))]
        public static class H_EffectManager_CancelEffect
        {
            public static bool Prepare()
            {
                return Mod.Settings.Patch.Vanilla;
            }

            public static void Postfix(Effect e)
            {
                if (_cache.TryGetValue(e.Target, out List<Effect> effects))
                {
                    effects.Remove(e);
                }
            }
        }

        [HarmonyPatch(typeof(EffectManager), nameof(EffectManager.EffectComplete))]
        public static class H_EffectManager_Effectcomplete
        {
            public static bool Prepare()
            {
                return Mod.Settings.Patch.Vanilla;
            }

            public static void Postfix(Effect e)
            {
                if (_cache.TryGetValue(e.Target, out List<Effect> effects))
                {
                    effects.Remove(e);
                }
            }
        }

        [HarmonyPatch(typeof(EffectManager), nameof(EffectManager.GetAllEffectsTargeting))]
        public static class H_EffectManager_GetAllEffectsTargeting
        {
            public static bool Prepare()
            {
                return Mod.Settings.Patch.Vanilla;
            }

            public static bool Prefix(object target, ref List<Effect> __result)
            {
                if (_cache.TryGetValue(target, out List<Effect> effects))
                {
                    __result = new List<Effect>(effects);
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(CombatGameState), "_Init")]
        public static class H_CombatGameState_Init
        {
            public static bool Prepare()
            {
                return Mod.Settings.Patch.Vanilla;
            }

            public static void Postfix()
            {
                _cache.Clear();
            }
        }

        [HarmonyPatch(typeof(CombatGameState), nameof(CombatGameState.OnCombatGameDestroyed))]
        public static class H_CombatGaemState_OnCombatGameDestroyed
        {
            public static bool Prepare()
            {
                return Mod.Settings.Patch.Vanilla;
            }

            public static void Postfix()
            {
                _cache.Clear();
            }
        }

        [HarmonyPatch(typeof(EffectManager), nameof(EffectManager.Hydrate))]
        public static class H_EffectManager_Hydrate
        {
            public static bool Prepare()
            {
                return Mod.Settings.Patch.Vanilla;
            }

            public static void Postfix(List<Effect> ___effects)
            {
                _cache.Clear();
                foreach (Effect effect in ___effects)
                {
                    (_cache[effect.Target] = new List<Effect>()).Add(effect);
                }
            }
        }

        #endregion

        #region Pool refreshing work on visibility cache

        [HarmonyPatch(typeof(EffectManager), nameof(EffectManager.OnRoundEnd))]
        public static class H_OnRoundEnd
        {
            private static int _counter = 0;

            public static bool GateActive { get; private set; }

            public static bool Prepare()
            {
                return Mod.Settings.Patch.LowVisibility;
            }

            public static void Prefix()
            {
                VisibilityCacheGate.EnterGate();
                GateActive = true;

                _counter = VisibilityCacheGate.GetCounter;
                RTPFLogger.Debug?.Write($"Enter visibility cache gate in {typeof(H_OnRoundEnd).FullName}:{nameof(Prefix)}\n");

            }

            public static void Postfix()
            {
                VisibilityCacheGate.ExitGate();

                GateActive = false;

                Utils.CheckExitCounter($"Fewer calls made to ExitGate() when reaches {typeof(H_OnRoundEnd).FullName}:{nameof(Postfix)}.\n", _counter);
                RTPFLogger.Debug?.Write($"Exit visibility cache gate in {typeof(H_OnRoundEnd).FullName}:{nameof(Postfix)}\n");
            }
        }

        #endregion
    }
}
