using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleTech.UI;
using Harmony;

namespace RogueTechPerfFixes.HarmonyPatches
{
    [HarmonyPatch(typeof(Briefing), "InitializeContract")]
    public static class H_Briefing_InitializeContract
    {
        public static void Postfix()
        {
            H_CombatAuraReticle_DesiredAuraReceptionState.AuraTable.Clear();
        }
    }
}
