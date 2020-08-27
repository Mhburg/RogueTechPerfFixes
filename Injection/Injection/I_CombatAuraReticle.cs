using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleTech.UI;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Mono.Collections.Generic;

namespace RogueTechPerfFixes.Injection
{
    public class I_CombatAuraReticle : IInjector
    {
        private const string _targetType = "BattleTech.UI.CombatAuraReticle";

        private static FieldDefinition _counter;

        private static FieldDefinition _interval;

        private const int _intervalValue = 10;

        #region Implementation of IInjector

        public void Inject(Dictionary<string, TypeDefinition> typeTable, ModuleDefinition module)
        {
            if (!Mod.Settings.Patch.Vanilla)
                return;

            if (typeTable.TryGetValue(_targetType, out TypeDefinition type))
            {
                InjectField(type, module);
                InitField(type);
                InjectIL(type);
            }
            else
            {
                RTPFLogger.LogCritical($"Can't find target type: {_targetType}\n");
            }
        }

        #endregion

        private static void InjectField(TypeDefinition type, ModuleDefinition module)
        {
            TypeReference unsignedInt = module.ImportReference(typeof(uint));

            _counter = new FieldDefinition(
                    "_counter"
                    , FieldAttributes.Private
                    , unsignedInt);

            _interval = new FieldDefinition(
                "_updateInterval"
                , FieldAttributes.Private | FieldAttributes.Static
                , unsignedInt);

            type.Fields.Add(_counter);
            type.Fields.Add(_interval);
        }

        private static void InitField(TypeDefinition type)
        {
            MethodDefinition staticCtor = type.GetStaticConstructor();
            ILProcessor ilProcessor = staticCtor.Body.GetILProcessor();
            Instruction ctorStart = staticCtor.Body.Instructions[0];

            ilProcessor.InsertBefore(ctorStart, Instruction.Create(OpCodes.Ldc_I4, _intervalValue));
            ilProcessor.InsertBefore(ctorStart, Instruction.Create(OpCodes.Stsfld, _interval));
        }

        private static void InjectIL(TypeDefinition type)
        {
            const string targetMethod = "LateUpdate";

            MethodDefinition method =
                type.GetMethods().FirstOrDefault(m => m.Name == targetMethod);

            if (method == null)
            {
                RTPFLogger.LogCritical($"Can't find method: {targetMethod}\n");
                return;
            }

            ILProcessor ilProcessor = method.Body.GetILProcessor();
            Instruction methodStart = method.Body.Instructions[0];

            List<Instruction> newInstructions = CreateInstructions(ilProcessor, methodStart);
            newInstructions.Reverse();

            foreach (Instruction instruction in newInstructions)
            {
                ilProcessor.InsertBefore(method.Body.Instructions[0], instruction);
            }
        }

        private static List<Instruction> CreateInstructions(ILProcessor ilProcessor, Instruction branchTarget)
        {
            List<Instruction> instructions = new List<Instruction>()
            {
                // int remainder = _counter % _interval;
                ilProcessor.Create(OpCodes.Ldarg_0),
                ilProcessor.Create(OpCodes.Ldfld, _counter),
                ilProcessor.Create(OpCodes.Ldsfld, _interval),
                ilProcessor.Create(OpCodes.Rem_Un),

                // _counter++;
                ilProcessor.Create(OpCodes.Ldarg_0),
                ilProcessor.Create(OpCodes.Ldarg_0),
                ilProcessor.Create(OpCodes.Ldfld, _counter),
                ilProcessor.Create(OpCodes.Ldc_I4_1),
                ilProcessor.Create(OpCodes.Add),
                ilProcessor.Create(OpCodes.Stfld, _counter),

                // if (equal) goto branchTarget;
                ilProcessor.Create(OpCodes.Brfalse, branchTarget),

                // return;
                ilProcessor.Create(OpCodes.Ret),
            };

            return instructions;
        }
    }
}
