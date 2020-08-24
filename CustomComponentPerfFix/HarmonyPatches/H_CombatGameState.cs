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
    public static class H_CombatGameState
    {
        private const string ACTIVE_GATE = "{0} has active visibility cache gate.\n";

        [HarmonyPatch(typeof(CombatGameState), nameof(CombatGameState.Update))]
        public static class H_Update
        {
            public static void Postfix()
            {
                bool error = false;

                if (H_AuraActorBody.GateActive)
                {
                    error = true;
                    RTPFLogger.Error?.Write(string.Format(ACTIVE_GATE, nameof(H_AuraActorBody)));
                }

                if (H_EffectManager.H_OnRoundEnd.GateActive)
                {
                    error = true;
                    RTPFLogger.Error?.Write(string.Format(ACTIVE_GATE, nameof(H_EffectManager.H_OnRoundEnd)));
                }

                if (error)
                    LowVisibility.Object.VisibilityCacheGate.ExitAll();
            }
        }
    }
}
