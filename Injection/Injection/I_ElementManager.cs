using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BattleTech.Rendering.UI;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Mono.Collections.Generic;
using RogueTechPerfFixes;
using RogueTechPerfFixes.Injection;
using UnityEngine.Rendering;

namespace Injection.Injection
{
    public class I_ElementManager : IInjector
    {
        private const string _targetType = "BattleTech.Rendering.UI.ElementManager";

        #region Implementation of IInjector

        public void Inject(Dictionary<string, TypeDefinition> typeTable, ModuleDefinition module)
        {
            if (!Mod.Settings.Patch.Vanilla)
                return;

            if (typeTable.TryGetValue(_targetType, out TypeDefinition type))
            {
                InjectIL(type, module);
                return;
            }

            CecilManager.WriteError($"Can't find type: {_targetType}");
        }

        #endregion

        private void InjectIL(TypeDefinition type, ModuleDefinition module)
        {
            MethodDefinition method = 
                type.GetMethods()
                    .FirstOrDefault(m => m.Name == nameof(ElementManager.RefreshCommandBuffer));

            FieldReference _buffer = type.Fields.FirstOrDefault(f => f.Name == "_uiCommandBuffer");
            MethodReference _init = module.ImportReference(
                typeof(I_ElementManager)
                    .GetMethod(nameof(InitBuffer), BindingFlags.Static | BindingFlags.NonPublic));

            Collection<Instruction> instructions = method.Body.Instructions;
            instructions[1].OpCode = OpCodes.Ldfld;
            instructions[1].Operand = _buffer;
            //Instruction branchTarget = Instruction.Create(OpCodes.Ldarg_0);
            //instructions.RemoveAt(1);
            //instructions.Insert(1, Instruction.Create(OpCodes.Ldfld, _buffer));
            //instructions.Insert(2, Instruction.Create(OpCodes.Brtrue, branchTarget));
            //instructions.Insert(3, Instruction.Create(OpCodes.Ldarg_0));
            //instructions.Insert(4, Instruction.Create(OpCodes.Ldflda, _buffer));
            //instructions.Insert(5, Instruction.Create(OpCodes.Call, _init));
            //instructions.Insert(6, branchTarget);
            //instructions.Insert(7, Instruction.Create(OpCodes.Ldfld, _buffer));
        }

        private static void InitBuffer(ref CommandBuffer buffer)
        {
            if (buffer == null)
            {
                buffer = new CommandBuffer();
                buffer.name = "UI Command Buffer";
            }
        }
    }
}
