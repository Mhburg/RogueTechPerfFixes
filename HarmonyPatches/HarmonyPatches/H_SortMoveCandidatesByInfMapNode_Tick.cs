using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using BattleTech;
using Harmony;
using HBS.Logging;
using RogueTechPerfFixes.Injection;

namespace RogueTechPerfFixes.HarmonyPatches
{
    public static class H_SortMoveCandidatesByInfMapNode_Tick
    {
        private const string TARGET_TYPE_NAME = "SortMoveCandidatesByInfMapNode";

        private static Assembly _HBSAssembly = Assembly.GetAssembly(typeof(BehaviorNode));

        private static MethodInfo _targetMethod;

        private static Type _comparer;

        private static MethodInfo _drawDebugLines;

        private static HarmonyUtils.RefGetter<object, bool> _ownTaskField;

        private static HarmonyUtils.RefGetter<Task<bool>> _thinkTaskField;

        private static bool _taskRunning = false;

        public static void Init()
        {
            Type origType = _HBSAssembly.GetType(TARGET_TYPE_NAME);

            try
            {
                _ownTaskField =
                    HarmonyUtils.CreateInstanceFieldRef<bool>(origType,
                        I_SortMoveCandidatesByInfMapNode.OwnTaskTypeName);
                _thinkTaskField = HarmonyUtils.CreateStaticFieldRef<Task<bool>>(origType,
                    I_SortMoveCandidatesByInfMapNode.ThinkTaskTypeName);

                _comparer = origType.GetNestedType("AccumulatorComparer", AccessTools.all);

                _drawDebugLines = origType.GetMethod("drawDebugLines", AccessTools.all);

                _targetMethod = origType.GetMethod("Tick", AccessTools.all);
                MethodInfo prefix =
                    typeof(H_SortMoveCandidatesByInfMapNode_Tick)
                        .GetMethod(nameof(Prefix), AccessTools.all);

                HarmonyUtils.Harmony.Patch(_targetMethod, new HarmonyMethod(prefix));
            }
            catch (Exception e)
            {
                Utils.Logger.LogError($"{Utils.LOG_HEADER} Failed to patch {TARGET_TYPE_NAME}", e);
            }
        }

        public static bool Prefix(ref BehaviorTreeResults __result, AbstractActor ___unit, BehaviorNode __instance)
        {
            __result = AltTick(___unit, __instance);
            return false;
        }

        public static BehaviorTreeResults AltTick(AbstractActor unit, BehaviorNode instance)
        {
            if (unit.BehaviorTree.movementCandidateLocations.Count == 0)
            {
                unit.BehaviorTree.influenceMapEvaluator.Reset();
                return new BehaviorTreeResults(BehaviorNodeState.Failure);
            }

            ref Task<bool> thinkTask = ref _thinkTaskField();
            ref bool ownTask = ref _ownTaskField(instance);
            if (thinkTask == null)
            {
                thinkTask = Task.Run(() => unit.BehaviorTree.influenceMapEvaluator.RunEvaluationForSeconds(float.MaxValue));
                ownTask = true;
            }

            if (!thinkTask.IsCompleted)
                return new BehaviorTreeResults(BehaviorNodeState.Running);

            if (!ownTask)
                return new BehaviorTreeResults(BehaviorNodeState.Running);

            if (thinkTask.Exception != null)
            {
                Utils.Logger.LogError($"{Utils.LOG_HEADER} Think task has run into exceptions", thinkTask.Exception.Flatten());
                thinkTask = null;
                ownTask = false;
                return new BehaviorTreeResults(BehaviorNodeState.Failure);
            }
            else if (!thinkTask.Result)
            {
                Utils.Logger.LogError($"{Utils.LOG_HEADER} Think task fails without exception");
                thinkTask = null;
                ownTask = false;
                return new BehaviorTreeResults(BehaviorNodeState.Failure);
            }

            thinkTask = null;
            ownTask = false;

            unit.BehaviorTree.influenceMapEvaluator.Reset();
            IComparer<WorkspaceEvaluationEntry> comparer = (IComparer<WorkspaceEvaluationEntry>)Activator.CreateInstance(_comparer);
            unit.BehaviorTree.influenceMapEvaluator.WorkspaceEvaluationEntries.Sort(
                0, unit.BehaviorTree.influenceMapEvaluator.firstFreeWorkspaceEvaluationEntryIndex, comparer);
            _drawDebugLines.Invoke(instance, null);
            return new BehaviorTreeResults(BehaviorNodeState.Success);
        }
    }
}
