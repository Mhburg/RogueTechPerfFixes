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
    // Changes DataManager.LoadResource(string path, Action<string> handler) to support async loads of files.
    //   This significantly improves the load time as IO is waits together instead of blocking.
    //   We target AmmunitionDefLoadRequest as it's the first name that inherits from StringDataLoadRequest
    class I_StringDataLoadRequest : IInjector
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

            // From class -> JsonLoadRequest -> StringDataLoadRequest
            TypeDefinition baseType = type.BaseType.Resolve().BaseType.Resolve();
            MethodDefinition method =
                baseType.GetMethods().FirstOrDefault(m => m.Name == targetMethod);

            if (method == null)
            {
                CecilManager.WriteError($"Can't find method: {targetMethod}\n");
                return;
            }

            ILProcessor ilProcessor = method.Body.GetILProcessor();

            // Add a enew reference to an importable method call for AsyncJsonLoadRequest.Load()
            TypeReference ajlr_TR = module.ImportReference(typeof(AsyncJsonLoadRequest));
            TypeReference taskTR = module.ImportReference(typeof(Task));

            MethodReference ajlr_lr_MR = new MethodReference("LoadResource", taskTR, ajlr_TR);
            TypeReference stringTR = module.ImportReference(typeof(string));
            ajlr_lr_MR.Parameters.Add(new ParameterDefinition(stringTR));

            TypeReference actionStringTR = module.ImportReference(typeof(Action<string>));
            ajlr_lr_MR.Parameters.Add(new ParameterDefinition(actionStringTR));
            ajlr_lr_MR.ReturnType = taskTR;

            MethodReference ajlr_lr_Imported_MR = module.ImportReference(ajlr_lr_MR);

            // Walk the instructions to find the target. Don't mutate as we go, so we can use insertAfter later.
            int targetIdx = -1;
            for (int i = 0; i < method.Body.Instructions.Count - 1; i++)
            {
                Instruction instruction = method.Body.Instructions[i];
                if (instruction.OpCode == OpCodes.Callvirt &&
                    instruction.Operand is MethodDefinition methodDef)
                {
                    //CecilManager.WriteLog($"Found methodDef: {methodDef.FullName}");

                    if (methodDef.FullName.StartsWith("System.Void HBS.Data.DataLoader::LoadResource"))
                    {
                        CecilManager.WriteLog($"Found injection point: {methodDef.FullName}\n");
                        targetIdx = i;
                    }
                }
            }
            if (targetIdx != -1)
            {
                // Replace callvirt for dataManager.dataLoader.LoadResource with call to AsyncJsonLoadRequest
                method.Body.Instructions[targetIdx] = ilProcessor.Create(OpCodes.Call, ajlr_lr_Imported_MR);

                // Elminate references to dataLoader (no longer used)
                method.Body.Instructions[targetIdx - 9].OpCode = OpCodes.Nop;
                method.Body.Instructions[targetIdx - 9].Operand = null;

                method.Body.Instructions[targetIdx - 8].OpCode = OpCodes.Nop;
                method.Body.Instructions[targetIdx - 8].Operand = null;

                method.Body.Instructions[targetIdx - 7].OpCode = OpCodes.Nop;
                method.Body.Instructions[targetIdx - 7].Operand = null;

                // Add a pop to remove the async Task (to eliminate dnSpy decompile err)
                Instruction popInst = ilProcessor.Create(OpCodes.Pop);
                ilProcessor.InsertAfter(method.Body.Instructions[targetIdx], popInst);
            }

        }

    }
}
