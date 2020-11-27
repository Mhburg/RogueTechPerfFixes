using BattleTech;
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
    class I_DataManager : IInjector
    {
        private const string _targetType = "BattleTech.Data.DataManager";

        #region Implementation of IInjector

        public void Inject(Dictionary<string, TypeDefinition> typeTable, ModuleDefinition module)
        {
            if (!Mod.Settings.Patch.Vanilla)
                return;

            if (typeTable.TryGetValue(_targetType, out TypeDefinition type))
            {
                CecilManager.WriteError($"Injecting IL for targetType: {_targetType}");
                InjectIL(type, module);
            }
            else
            {
                CecilManager.WriteError($"Can't find target type: {_targetType}");
            }
        }

        #endregion
        private static void InjectIL(TypeDefinition type, ModuleDefinition module)
        {
            // internal DataManager.FileLoadRequest CreateFileRequest(BattleTechResourceType resourceType, string identifier, PrewarmRequest prewarm, bool allowRequestStacking)
            const string targetMethod = "CreateFileRequest";

            // From class -> JsonLoadRequest -> StringDataLoadRequest
            MethodDefinition method =
                type.GetMethods().FirstOrDefault(m => m.Name == targetMethod);

            if (method == null)
            {
                CecilManager.WriteError($"Can't find method: {targetMethod}\n");
                return;
            }

            ILProcessor ilProcessor = method.Body.GetILProcessor();
            Instruction methodStart = method.Body.Instructions[0];

            List<Instruction> newInstructions = CreateInstructions(ilProcessor, methodStart, module);
            newInstructions.Reverse();

            foreach (Instruction instruction in newInstructions)
            {
                ilProcessor.InsertBefore(method.Body.Instructions[0], instruction);
            }
        }

        private static List<Instruction> CreateInstructions(ILProcessor ilProcessor, Instruction branchTarget, ModuleDefinition module)
        {
            // Create an importable reference to our logger
            TypeReference ajlr_TR = module.ImportReference(typeof(AsyncJsonLoadRequest));
            TypeReference void_TR = module.ImportReference(typeof(void));

            MethodReference ajlr_lr_MR = new MethodReference("LogLoadRequest", void_TR, ajlr_TR);

            TypeReference battleTechResourceType_TR = module.ImportReference(typeof(BattleTechResourceType));
            ajlr_lr_MR.Parameters.Add(new ParameterDefinition(battleTechResourceType_TR));

            TypeReference string_TR = module.ImportReference(typeof(string));
            ajlr_lr_MR.Parameters.Add(new ParameterDefinition(string_TR));

            TypeReference bool_TR = module.ImportReference(typeof(bool));
            ajlr_lr_MR.Parameters.Add(new ParameterDefinition(bool_TR));

            MethodReference ajlr_lr_Imported_MR = module.ImportReference(ajlr_lr_MR);

            List<Instruction> instructions = new List<Instruction>()
            {
                // Add all params to stack
                ilProcessor.Create(OpCodes.Ldarg, 1),
                ilProcessor.Create(OpCodes.Ldarg, 2),
                ilProcessor.Create(OpCodes.Ldarg, 4),
                ilProcessor.Create(OpCodes.Call, ajlr_lr_Imported_MR)
            };

            return instructions;
        }

    }
}
