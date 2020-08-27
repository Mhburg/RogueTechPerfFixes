using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DG.Tweening;
using DG.Tweening.Core;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Mono.Collections.Generic;
using FieldAttributes = Mono.Cecil.FieldAttributes;

namespace RogueTechPerfFixes.Injection
{
    public class I_DOTweenAnimation : IInjector
    {
        private const string _targetType = "DG.Tweening.DOTweenAnimation";

        private static Instruction _getInstanceId;

        private static FieldDefinition InstanceId;

        private static FieldDefinition HasInstanceId;

        #region Implementation of IInjector

        public void Inject(Dictionary<string, TypeDefinition> typeTable, ModuleDefinition module)
        {
            if (!Mod.Settings.Patch.Vanilla)
                return;

            if (typeTable.TryGetValue(_targetType, out TypeDefinition type))
            {
                _getInstanceId = Instruction.Create(
                    OpCodes.Callvirt
                    , type.Module.ImportReference(typeof(UnityEngine.Object).GetMethod(nameof(UnityEngine.Object.GetInstanceID))));

                InjectField(type, module);
                //InitField(type, module);
                InjectIL(type, module);
                return;
            }

            CecilManager.WriteLog($"Can't find target type: {_targetType}");
        }

        #endregion

        private static void InjectField(TypeDefinition type, ModuleDefinition module)
        {
            TypeReference intReference = module.ImportReference(typeof(int));
            TypeReference booleanReference = module.ImportReference(typeof(bool));

            InstanceId = new FieldDefinition(
                nameof(InstanceId)
                , FieldAttributes.Public
                , intReference);

            HasInstanceId = new FieldDefinition(
                nameof(HasInstanceId)
                , FieldAttributes.Public
                , booleanReference);

            type.Fields.Add(InstanceId);
            type.Fields.Add(HasInstanceId);
        }

        private static void InitField(TypeDefinition type, ModuleDefinition module)
        {
            List<MethodDefinition> consturctors = type.GetConstructors().ToList();
            if (consturctors.Count == 0)
            {
                CecilManager.WriteLog($"Can't find constructor for {nameof(_targetType)}\n");
                return;
            }

            foreach (MethodDefinition constructor in consturctors)
            {
                if (constructor.IsStatic)
                    continue;

                ILProcessor ilProcessor = constructor.Body.GetILProcessor();
                Instruction ctorEnd = constructor.Body.Instructions.Last();

                // this.InstanceId = this.GetInstanceID();
                ilProcessor.InsertBefore(
                    ctorEnd
                    , Instruction.Create(OpCodes.Ldarg_0));

                ilProcessor.InsertBefore(
                    ctorEnd
                    , Instruction.Create(OpCodes.Ldarg_0));

                ilProcessor.InsertBefore(ctorEnd, _getInstanceId);

                ilProcessor.InsertBefore(
                    ctorEnd
                    , Instruction.Create(OpCodes.Stfld, InstanceId));
            }
        }

        private static void InjectIL(TypeDefinition type, ModuleDefinition module)
        {
            MethodDefinition createTween = type.GetMethods().FirstOrDefault(m => m.Name == "CreateTween");
            if (createTween == null)
            {
                CecilManager.WriteError($"Can't find target method: CreateTween\n");
                return;
            }

            module.ImportReference(typeof(Tween));
            module.ImportReference(typeof(ABSAnimationComponent));
            FieldReference tween = module.ImportReference(typeof(ABSAnimationComponent).GetField(nameof(ABSAnimationComponent.tween)));
            if (tween == null)
            {
                CecilManager.WriteError($"Can't find target field: tween");
                return;
            }

            FieldReference tweenId = module.ImportReference(typeof(Tween).GetField(nameof(Tween.id)));

            Instruction loadId = Instruction.Create(OpCodes.Ldfld, InstanceId);
            Instruction loadTween = Instruction.Create(OpCodes.Ldfld, tween);
            Instruction setId = Instruction.Create(OpCodes.Stfld, tweenId);
            Collection<Instruction> instructions = createTween.Body.Instructions;
            Instruction branchLoad = Instruction.Create(OpCodes.Ldarg_0);

            // if (this.HasInstanceId) jump to branchLoad
            instructions[instructions.Count - 1].OpCode = OpCodes.Ldarg_0;
            instructions.Add(Instruction.Create(OpCodes.Ldfld, HasInstanceId));
            instructions.Add(Instruction.Create(OpCodes.Brtrue, branchLoad));

            // this.InstanceId = this.GetInstanceID();
            instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            instructions.Add(_getInstanceId);
            instructions.Add(Instruction.Create(OpCodes.Stfld, InstanceId));

            // this.HasInstanceId = true;
            instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1));
            instructions.Add(Instruction.Create(OpCodes.Stfld, HasInstanceId));

            // this.tween.id = this.InstanceId;
            instructions.Add(branchLoad);
            instructions.Add(loadTween);
            instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            instructions.Add(loadId);
            instructions.Add(Instruction.Create(OpCodes.Box, module.ImportReference(typeof(int))));
            instructions.Add(setId);

            // return;
            instructions.Add(Instruction.Create(OpCodes.Ret));
        }
    }
}
