using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleTech;
using Harmony;

namespace RogueTechPerfFixes.HarmonyPatches
{
    public static class H_AttackDirector
    {
        private static int _counter = 0;

        /// <summary>
        /// Along with patch to <see cref="AttackDirector.OnAttackSequenceEnd"/>, they together pool all work
        /// of refreshing the visibility cache into one place.
        /// </summary>
        [HarmonyPatch(typeof(AttackDirector), nameof(AttackDirector.OnAttackSequenceBegin))]
        public static class H_OnAttackSequenceBegin
        {
            public static void Postfix()
            {
                LowVisibility.Object.VisibilityCacheGate.EnterGate();
                _counter = LowVisibility.Object.VisibilityCacheGate.GetCounter;
                RTPFLogger.Debug?.Write($"Enter visibility cache gate in {typeof(H_OnAttackSequenceBegin).FullName}:{nameof(Postfix)}\n");
            }
        }

        [HarmonyPatch(typeof(AttackDirector), nameof(AttackDirector.OnAttackSequenceEnd))]
        public static class H_OnAttackSequenceEnd
        {
            public static void Postfix()
            {
                LowVisibility.Object.VisibilityCacheGate.ExitGate();

                Utils.CheckExitCounter($"Fewer calls made to ExitGate() when reaches AttackDirector.OnAttackSequenceEnd().\n", _counter);
                RTPFLogger.Debug?.Write($"Exit visibility cache gate in {typeof(H_OnAttackSequenceEnd).FullName}:{nameof(Postfix)}\n");
            }
        }
    }
}
