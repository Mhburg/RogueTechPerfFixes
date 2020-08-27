using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BattleTech;
using Harmony;

namespace RogueTechPerfFixes.HarmonyPatches
{
    [HarmonyPatch(typeof(BresenhamLineUtil), nameof(BresenhamLineUtil.BresenhamLine))]
    public static class H_BresenhamLine
    {
        private static ThreadLocal<List<Point>> _localWorkingSet = new ThreadLocal<List<Point>>(() => new List<Point>(4));

        private static FieldInfo _threadLocalList = typeof(H_BresenhamLine).GetField(nameof(_localWorkingSet), AccessTools.all);

        private static MethodInfo _valueList = typeof(ThreadLocal<List<Point>>).GetProperty(nameof(_localWorkingSet.Value), AccessTools.all).GetGetMethod();

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codeInstructions)
        {
            List<CodeInstruction> code = new List<CodeInstruction>(codeInstructions);

            code.RemoveRange(0, 3);

            code.Insert(0, new CodeInstruction(OpCodes.Ldsfld, _threadLocalList));
            code.Insert(1, new CodeInstruction(OpCodes.Call, _valueList));
            code.Insert(2, new CodeInstruction(OpCodes.Stloc_0));
            code.Insert(3, new CodeInstruction(OpCodes.Ldloc_0));

            return code;
        }
    }
}
