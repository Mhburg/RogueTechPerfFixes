using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using RogueTechPerfFixes;
using RogueTechPerfFixes.DataManager;
using RogueTechPerfFixes.Injection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Injection.Injection
{
    class I_AmmunitionDefLoadRequest : IInjector
    {
        private const string _baseType = "BattleTech.Data.DataManager";
        private const string _targetType = "BattleTech.Data.DataManager/AmmunitionDefLoadRequest";

        #region Implementation of IInjector

        public void Inject(Dictionary<string, TypeDefinition> typeTable, ModuleDefinition module)
        {
            if (!Mod.Settings.Patch.Vanilla)
                return;

            if (typeTable.TryGetValue(_baseType, out TypeDefinition type))
            {
                //InjectField(type, module);
                //InitField(type);
                CecilManager.WriteLog($"Found baseType: {_baseType}");

                foreach (TypeDefinition nestedType in type.NestedTypes)
                {
                    if (_targetType.Equals(nestedType.FullName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        CecilManager.WriteLog($"Found target nestedType: {nestedType.FullName}");
                        InjectIL(nestedType, module);
                    }
                }
            }
            else
            {
                CecilManager.WriteError($"Can't find target type: {_targetType}");
            }
        }

        #endregion
        private static void InjectIL(TypeDefinition type, ModuleDefinition module)
        {
            const string targetMethod = "Load";

            TypeDefinition baseType = type.BaseType.Resolve().BaseType.Resolve();
            foreach (MethodDefinition methodDef in baseType.GetMethods())
            {
                CecilManager.WriteLog($" Method: {methodDef.Name}");
            }

            MethodDefinition method =
                baseType.GetMethods().FirstOrDefault(m => m.Name == targetMethod);

            if (method == null)
            {
                CecilManager.WriteError($"Can't find method: {targetMethod}\n");
                return;
            }

            ILProcessor ilProcessor = method.Body.GetILProcessor();

            TypeReference asyncJsonLoadRequestTR = module.ImportReference(typeof(AsyncJsonLoadRequest));
            VariableDefinition asyncJsonLoadRequestVD = new VariableDefinition(asyncJsonLoadRequestTR);
            method.Body.Variables.Add(asyncJsonLoadRequestVD);

            for (int i = 0; i < method.Body.Instructions.Count - 1; i++)
            {
                Instruction instruction = method.Body.Instructions[i];
                if (instruction.OpCode == OpCodes.Callvirt &&
                    instruction.Operand != null &&
                    instruction.Operand.GetType().FullName.StartsWith("HBS.Data.DataLoader::LoadResource"))
                {
                    CecilManager.WriteLog($"Found injection point: {instruction.Operand.GetType().FullName}\n");
                    method.Body.Instructions[i] = ilProcessor.Create(OpCodes.Call, method);

                    // Look for preceeding methods at -7, -8

                    if (i - 7 > 0 &&
                        method.Body.Instructions[i - 7].OpCode == OpCodes.Ldfld)
                    {
                        CecilManager.WriteLog($" WIPING LDFLD");
                        method.Body.Instructions[i - 7].Operand = OpCodes.Nop;
                        method.Body.Instructions[i - 7].Operand = null;
                    }
                    else
                        CecilManager.WriteError($" NOT LDFLD - SHIT GONNA BREAK");

                    if (i - 8 > 0 &&
                        method.Body.Instructions[i - 8].OpCode == OpCodes.Ldfld)
                    {
                        CecilManager.WriteLog($" WIPING LDFLD");
                        method.Body.Instructions[i - 8].Operand = OpCodes.Nop;
                        method.Body.Instructions[i - 8].Operand = null;
                    }
                    else
                        CecilManager.WriteError($" NOT LDFLD - SHIT GONNA BREAK");

                }
            }


            //Instruction methodStart = method.Body.Instructions[0];

            //List<Instruction> newInstructions = CreateInstructions(ilProcessor, methodStart);
            //newInstructions.Reverse();

            //foreach (Instruction instruction in newInstructions)
            //{
            //    ilProcessor.InsertBefore(method.Body.Instructions[0], instruction);
            //}
        }

        //private static List<Instruction> CreateInstructions(ILProcessor ilProcessor, Instruction branchTarget)
        //{
        //    List<Instruction> instructions = new List<Instruction>()
        //    {
        //        // int remainder = _counter % _interval;
        //        ilProcessor.Create(OpCodes.Ldarg_0),
        //        ilProcessor.Create(OpCodes.Ldfld, _counter),
        //        ilProcessor.Create(OpCodes.Ldsfld, _interval),
        //        ilProcessor.Create(OpCodes.Rem_Un),

        //        // _counter++;
        //        ilProcessor.Create(OpCodes.Ldarg_0),
        //        ilProcessor.Create(OpCodes.Ldarg_0),
        //        ilProcessor.Create(OpCodes.Ldfld, _counter),
        //        ilProcessor.Create(OpCodes.Ldc_I4_1),
        //        ilProcessor.Create(OpCodes.Add),
        //        ilProcessor.Create(OpCodes.Stfld, _counter),

        //        // if (equal) goto branchTarget;
        //        ilProcessor.Create(OpCodes.Brfalse, branchTarget),

        //        // return;
        //        ilProcessor.Create(OpCodes.Ret),
        //    };

        //    return instructions;
        //}
    
    }
}
