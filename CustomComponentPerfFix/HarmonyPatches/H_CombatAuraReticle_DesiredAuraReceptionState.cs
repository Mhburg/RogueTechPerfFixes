using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BattleTech;
using BattleTech.UI;
using Harmony;

namespace RogueTechPerfFixes.HarmonyPatches
{
    [HarmonyPatch(typeof(CombatAuraReticle), "get_DesiredAuraProjectionState")]
    public static class H_CombatAuraReticle_DesiredAuraReceptionState
    {
        private const int _updateInterval = 15;

        internal static readonly Dictionary<AbstractActor, AuraState> AuraTable = new Dictionary<AbstractActor, AuraState>();

        public static bool Prefix(AbstractActor ___owner, ref ButtonState __result)
        {
            if (!AuraTable.TryGetValue(___owner, out AuraState auraState))
                AuraTable[___owner] = auraState = new AuraState();

            if (auraState.LastUpdate++ % _updateInterval == 0)
                return true;

            __result = auraState.LastState;
            return false;
        }

        public static void Postfix(AbstractActor ___owner, ButtonState __result)
        {
            AuraTable[___owner].LastState = __result;
        }

        internal class AuraState
        {
            public ulong LastUpdate { get; set; } = 0;

            public ButtonState LastState { get; set; } = ButtonState.Disabled;
        }
    }
}
