using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using BattleTech;
using Harmony;

namespace RogueTechPerfFixes.HarmonyPatches
{
    public static class H_SortMoveCandidatesByInfMapNode_Tick
    {
        private static Assembly _HBSAssembly = Assembly.GetAssembly(typeof(BehaviorNode));

        private static Type _thisType = typeof(H_SortMoveCandidatesByInfMapNode_Tick);

        private static Task<bool> _thinkingTask = null;

        public static void Init()
        {
            MethodInfo original =
                _HBSAssembly.GetType("SortMoveCandidatesByInfMapNode")
                           .GetMethod("Tick", AccessTools.all);

            MethodInfo transpiler =
                typeof(H_SortMoveCandidatesByInfMapNode_Tick)
                    .GetMethod(nameof(Transpiler), AccessTools.all);

            HarmonyInstance.DEBUG = true;
            HarmonyUtils.Harmony.Patch(original, transpiler: new HarmonyMethod(transpiler));
            HarmonyInstance.DEBUG = false;
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codeInstructions)
        {
            /*********************** Before Patch *******************************************/
            //  if (!unit.BehaviorTree.influenceMapEvaluator.RunEvaluationForSeconds(0.02f))
            //  {
            //  	return new BehaviorTreeResults(BehaviorNodeState.Running);
            //  }

            /*********************** After Patch *******************************************/
            //  if (_thinkingTask == null)
            //  {
            //  	_thinkingTask = Task.Run<bool>(() => this.unit.BehaviorTree.influenceMapEvaluator.RunEvaluationForSeconds(2.14748365E+09f));
            //  }
            //  if (!_thinkingTask.IsCompleted)
            //  {
            //  	return new BehaviorTreeResults(BehaviorNodeState.Running);
            //  }
            //  _thinkingTask = null;

            int i = 0;
            int insertionPoint = -1;
            List<CodeInstruction> code = new List<CodeInstruction>(codeInstructions);

            for (; i < code.Count; i++)
            {
                // There is only one opcode that loads float number
                if (code[i].opcode == OpCodes.Ldc_R4)
                {
                    insertionPoint = i - 2;
                    code.RemoveRange(insertionPoint, 4);
                    break;
                }
            }

            // fail-safe
            if (insertionPoint == -1)
                return code;

            code.Insert(
                insertionPoint
                , new CodeInstruction(
                    OpCodes.Call
                    , _thisType.GetMethod(nameof(RunTask), AccessTools.all)));

            for (i = insertionPoint; i < code.Count; i++)
            {
                if (code[i].opcode == OpCodes.Ldarg_0)
                {
                    insertionPoint = i;
                    break;
                }
            }

            code.Insert(++insertionPoint, new CodeInstruction(OpCodes.Ldnull));
            code.Insert(++insertionPoint, new CodeInstruction(OpCodes.Stsfld, _thisType.GetField(nameof(_thinkingTask), AccessTools.all)));

            return code;
        }

        private static bool RunTask(AbstractActor unit)
        {
            if (_thinkingTask == null)
                _thinkingTask = Task.Run(
                    () => unit.BehaviorTree.influenceMapEvaluator.RunEvaluationForSeconds(int.MaxValue));

            return _thinkingTask.IsCompleted;
        }
    }
}
