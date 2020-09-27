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
using UnityEngine;
using FieldAttributes = Mono.Cecil.FieldAttributes;

namespace RogueTechPerfFixes.Injection
{
    public class I_DOTweenAnimation : IInjector
    {
        private const string _targetType = "DG.Tweening.DOTweenAnimation";

        private static Instruction _getInstanceId;

        private static FieldDefinition TargetId;

        private static FieldDefinition HasTargetId;

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

            CecilManager.WriteError($"Can't find target type: {_targetType}");
        }

        #endregion

        private static void InjectField(TypeDefinition type, ModuleDefinition module)
        {
            TypeReference intReference = module.ImportReference(typeof(int));
            TypeReference booleanReference = module.ImportReference(typeof(bool));

            TargetId = new FieldDefinition(
                nameof(TargetId)
                , FieldAttributes.Public
                , intReference);

            HasTargetId = new FieldDefinition(
                nameof(HasTargetId)
                , FieldAttributes.Public
                , booleanReference);

            type.Fields.Add(TargetId);
            type.Fields.Add(HasTargetId);
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
                    , Instruction.Create(OpCodes.Stfld, TargetId));
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
            module.ImportReference(typeof(Component));

            FieldReference tween = module.ImportReference(typeof(ABSAnimationComponent).GetField(nameof(ABSAnimationComponent.tween)));
            FieldReference target = module.ImportReference(typeof(DOTweenAnimation).GetField(nameof(DOTweenAnimation.target)));
            FieldReference tweenId = module.ImportReference(typeof(Tween).GetField(nameof(Tween.id)));

            MethodReference notEqualTo = module.ImportReference(
                typeof(UnityEngine.Object).GetMethod("op_Inequality", BindingFlags.Public | BindingFlags.Static));

            Instruction loadTargetId = Instruction.Create(OpCodes.Ldfld, TargetId);
            Instruction loadHasTargetId = Instruction.Create(OpCodes.Ldfld, HasTargetId);
            Instruction loadTarget = Instruction.Create(OpCodes.Ldfld, target);
            Instruction loadTween = Instruction.Create(OpCodes.Ldfld, tween);
            Instruction setId = Instruction.Create(OpCodes.Stfld, tweenId);
            Instruction branchLoad = Instruction.Create(OpCodes.Ldarg_0);
            Instruction branchReturn = Instruction.Create(OpCodes.Ret);
            Collection<Instruction> instructions = createTween.Body.Instructions;

            // if (this.HasTargetId) return;
            instructions[instructions.Count - 1].OpCode = OpCodes.Ldarg_0;
            instructions.Add(loadHasTargetId);
            instructions.Add(Instruction.Create(OpCodes.Brtrue, branchReturn));

            // if (this.target == null) return;
            instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            instructions.Add(loadTarget);
            instructions.Add(Instruction.Create(OpCodes.Ldnull));
            instructions.Add(Instruction.Create(OpCodes.Call, notEqualTo));
            instructions.Add(Instruction.Create(OpCodes.Brfalse, branchReturn));

            // this.TargetId = this.target.GetInstanceID();
            instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            instructions.Add(loadTarget);
            instructions.Add(_getInstanceId);
            instructions.Add(Instruction.Create(OpCodes.Stfld, TargetId));

            // this.HasTargetId = true;
            instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1));
            instructions.Add(Instruction.Create(OpCodes.Stfld, HasTargetId));

            // this.tween.id = this.InstanceId;
            //instructions.Add(branchLoad);
            //instructions.Add(loadTween);
            //instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            //instructions.Add(loadTargetId);
            //instructions.Add(Instruction.Create(OpCodes.Box, module.ImportReference(typeof(int))));
            //instructions.Add(setId);

            // return;
            instructions.Add(branchReturn);

            createTween.Body.OptimizeMacros();
        }
    }
}
