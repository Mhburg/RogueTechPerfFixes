using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Harmony;

namespace RogueTechPerfFixes
{
    public static class HarmonyUtils
    {
        public const string HarmonyId = "NotooShabby.RogueTechPerfFixes";

        public static HarmonyInstance Harmony = HarmonyInstance.Create(HarmonyId);

        public delegate ref U RefGetter<U>();

        public delegate ref U RefGetter<in T, U>(T obj);

        /// <summary>
        /// Create a pointer for instance field <paramref name="s_field"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="s_field"></param>
        /// <returns> Pointer to <paramref name="s_field"/></returns>
        /// <remarks> Source: https://stackoverflow.com/a/45046664/13073994 </remarks>
        public static RefGetter<T, U> CreateInstanceFieldRef<T, U>(String s_field)
        {
            const BindingFlags bf = BindingFlags.NonPublic |
                                    BindingFlags.Instance |
                                    BindingFlags.DeclaredOnly;

            var fi = typeof(T).GetField(s_field, bf);
            if (fi == null)
                throw new MissingFieldException(typeof(T).Name, s_field);

            var s_name = "__refget_" + typeof(T).Name + "_fi_" + fi.Name;

            // workaround for using ref-return with DynamicMethod:
            //   a.) initialize with dummy return value
            var dm = new DynamicMethod(s_name, typeof(U), new[] { typeof(T) }, typeof(T), true);

            //   b.) replace with desired 'ByRef' return value
            dm.GetType().GetField("returnType", bf).SetValue(dm, typeof(U).MakeByRefType());

            var il = dm.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldflda, fi);
            il.Emit(OpCodes.Ret);

            return (RefGetter<T, U>)dm.CreateDelegate(typeof(RefGetter<T, U>));
        }

        /// <summary>
        /// Create a pointer for instance field <paramref name="s_field"/> of <paramref name="type"/>.
        /// </summary>
        /// <typeparam name="U"></typeparam>
        /// <param name="s_field"></param>
        /// <returns> Pointer to <paramref name="s_field"/></returns>
        /// <remarks> Source: https://stackoverflow.com/a/45046664/13073994 </remarks>
        public static RefGetter<object, U> CreateInstanceFieldRef<U>(Type type, String s_field)
        {
            const BindingFlags bf = BindingFlags.NonPublic |
                                    BindingFlags.Instance |
                                    BindingFlags.DeclaredOnly;

            var fi = type.GetField(s_field, bf);
            if (fi == null)
                throw new MissingFieldException(type.Name, s_field);

            var s_name = "__refget_" + type.Name + "_fi_" + fi.Name;

            // workaround for using ref-return with DynamicMethod:
            //   a.) initialize with dummy return value
            var dm = new DynamicMethod(s_name, typeof(U), new[] { typeof(object) }, type, true);

            //   b.) replace with desired 'ByRef' return value
            dm.GetType().GetField("returnType", bf).SetValue(dm, typeof(U).MakeByRefType());

            var il = dm.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldflda, fi);
            il.Emit(OpCodes.Ret);

            return (RefGetter<object, U>)dm.CreateDelegate(typeof(RefGetter<object, U>));
        }

        /// <summary>
        /// Create a pointer for static field <paramref name="s_field"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="s_field"></param>
        /// <returns> Pointer to <paramref name="s_field"/></returns>
        /// <remarks> Source: https://stackoverflow.com/a/45046664/13073994 </remarks>
        public static RefGetter<U> CreateStaticFieldRef<T, U>(String s_field)
        {
            const BindingFlags bf = BindingFlags.NonPublic |
                                    BindingFlags.Static |
                                    BindingFlags.DeclaredOnly;

            var fi = typeof(T).GetField(s_field, bf);
            if (fi == null)
                throw new MissingFieldException(typeof(T).Name, s_field);

            var s_name = "__refget_" + typeof(T).Name + "_fi_" + fi.Name;

            // workaround for using ref-return with DynamicMethod:
            //   a.) initialize with dummy return value
            var dm = new DynamicMethod(s_name, typeof(U), null, typeof(T), true);

            //   b.) replace with desired 'ByRef' return value
            dm.GetType().GetField("returnType", AccessTools.all).SetValue(dm, typeof(U).MakeByRefType());

            var il = dm.GetILGenerator();
            il.Emit(OpCodes.Ldsflda, fi);
            il.Emit(OpCodes.Ret);

            return (RefGetter<U>)dm.CreateDelegate(typeof(RefGetter<U>));
        }

        /// <summary>
        /// Create a pointer for static field <paramref name="s_field"/>
        /// </summary>
        /// <typeparam name="U"></typeparam>
        /// <param name="s_field"></param>
        /// <returns> Pointer to <paramref name="s_field"/></returns>
        /// <remarks> Source: https://stackoverflow.com/a/45046664/13073994 </remarks>
        public static RefGetter<U> CreateStaticFieldRef<U>(Type type, String s_field)
        {
            const BindingFlags bf = BindingFlags.NonPublic |
                                    BindingFlags.Static |
                                    BindingFlags.DeclaredOnly;

            var fi = type.GetField(s_field, bf);
            if (fi == null)
                throw new MissingFieldException(type.Name, s_field);

            var s_name = "__refget_" + type.Name + "_fi_" + fi.Name;

            // workaround for using ref-return with DynamicMethod:
            //   a.) initialize with dummy return value
            var dm = new DynamicMethod(s_name, typeof(U), null, type, true);

            //   b.) replace with desired 'ByRef' return value
            dm.GetType().GetField("returnType", AccessTools.all).SetValue(dm, typeof(U).MakeByRefType());

            var il = dm.GetILGenerator();
            il.Emit(OpCodes.Ldsflda, fi);
            il.Emit(OpCodes.Ret);

            return (RefGetter<U>)dm.CreateDelegate(typeof(RefGetter<U>));
        }

        public static bool Compare(this CodeInstruction codeInstruction, CodeInstruction pattern)
        {
            if (codeInstruction.opcode != pattern.opcode
                || (pattern.opcode == OpCodes.Ldloc_S
                    && (int)pattern.operand != (codeInstruction.operand as LocalVariableInfo)?.LocalIndex)
                || (pattern.opcode == OpCodes.Callvirt
                    && pattern.operand != null
                    && pattern.operand != codeInstruction.operand))
                return false;

            return true;
        }

        public static bool MatchPattern(this List<CodeInstruction> instructions, List<CodeInstruction> pattern, int index, Action action = null)
        {
            for (int i = index, j = 0; j < pattern.Count; i++, j++)
            {
                if (!instructions[i].Compare(pattern[j]))
                {
                    return false;
                }
            }

            action?.Invoke();
            return true;
        }
    }
}
