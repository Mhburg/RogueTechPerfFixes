using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleTech;
using Harmony;

namespace RogueTechPerfFixes.HarmonyPatches
{
    public static class H_ActorMovementSequence
    {
        private static bool _hasEntered = false;

        private static int _counter = 0;

        [HarmonyPatch(typeof(ActorMovementSequence), nameof(ActorMovementSequence.Update))]
        public static class H_Update
        {
            public static bool Prepare()
            {
                return Mod.Settings.Patch.LowVisibility;
            }

            public static void Prefix()
            {
                if (!_hasEntered)
                {
                    _hasEntered = true;
                    VisibilityCacheGate.EnterGate();
                    _counter = VisibilityCacheGate.GetCounter;
                    RTPFLogger.Debug?.Write($"Enter visibility cache gate in {typeof(H_Update).FullName}:{nameof(Prefix)}\n");
                }
            }
        }

        [HarmonyPatch(typeof(ActorMovementSequence), "CompleteMove")]
        public static class H_CompleteMove
        {
            public static bool Prepare()
            {
                return Mod.Settings.Patch.LowVisibility;
            }

            public static void Postfix()
            {
                _hasEntered = false;
                VisibilityCacheGate.ExitGate();

                Utils.CheckExitCounter($"Fewer calls made to ExitGate() when reaches ActorMovementSequence.CompleteMove().\n", _counter);
                RTPFLogger.Debug?.Write($"Exit visibility cache gate in {typeof(H_CompleteMove).FullName}: {nameof(Postfix)}\n");
            }
        }
    }
}
