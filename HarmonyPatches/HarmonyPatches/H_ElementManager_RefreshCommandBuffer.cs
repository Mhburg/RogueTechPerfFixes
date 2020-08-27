using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using BattleTech.Rendering.UI;
using Harmony;

namespace RogueTechPerfFixes.HarmonyPatches
{
    [HarmonyPatch(typeof(ElementManager), nameof(ElementManager.RefreshCommandBuffer))]
    public static class H_ElementManager_RefreshCommandBuffer
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codeInstructions)
        {
            List<CodeInstruction> code = new List<CodeInstruction>(codeInstructions);

            foreach (CodeInstruction instruction in code)
            {
                if (instruction.opcode == OpCodes.Call)
                {
                    instruction.opcode = OpCodes.Ldfld;
                    instruction.operand = typeof(ElementManager).GetField("_uiCommandBuffer", AccessTools.all);
                    break;
                }
            }

            return code;
        }
    }
}
