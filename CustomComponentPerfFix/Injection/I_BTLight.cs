﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Mono.Collections.Generic;

namespace RogueTechPerfFixes.Injection
{
    public class I_BTLight : IInjector
    {
        private const string _targetType = "BattleTech.Rendering.BTLight";

        private static FieldDefinition InstanceId;

        private static Instruction _getInstanceID;

        #region Implementation of IInjector

        public void Inject(Dictionary<string, TypeDefinition> typeTable, ModuleDefinition module)
        {
            if (typeTable.TryGetValue(_targetType, out TypeDefinition type))
            {
                _getInstanceID = Instruction.Create(
                    OpCodes.Callvirt
                    , type.Module.ImportReference(typeof(UnityEngine.Object).GetMethod(nameof(UnityEngine.Object.GetInstanceID))));

                InjectField(type, module);
                InitField(type);
                InjectIL(type);
            }
            else
            {
                File.AppendAllText(CecilManager.CecilLog, $"Can't find target type: {_targetType}\n");
            }
        }

        #endregion

        private static void InjectField(TypeDefinition type, ModuleDefinition module)
        {
            TypeReference intReference = module.ImportReference(typeof(int));

            InstanceId = new FieldDefinition(
                nameof(InstanceId)
                , FieldAttributes.Public
                , intReference);

            type.Fields.Add(InstanceId);

            //MethodDefinition method = new MethodDefinition("GetInstanctIdLazy", MethodAttributes.Public, intReference);
            //ILProcessor ilProcessor = method.Body.GetILProcessor();
            //ilProcessor.Emit(OpCodes.Ldarg_0);
            //ilProcessor.Emit(OpCodes.Ldfld, InstanceId);
            //ilProcessor.Emit(OpCodes.Ldc_I4_0);
            //ilProcessor.Emit(OpCodes.Brtrue);
            //ilProcessor.Emit(OpCodes.Ldarg_0);
            //ilProcessor.Emit(_getInstanceID.OpCode, _getInstanceID.Operand as MethodReference);
        }

        private static void InitField(TypeDefinition type)
        {
            List<MethodDefinition> consturctors = type.GetConstructors().ToList();
            if (consturctors.Count == 0)
            {
                File.AppendAllText(CecilManager.CecilLog, $"Can't find constructor for BTLight\n");
                throw new NotImplementedException("Can't find constructor for BTLight");
            }

            foreach (MethodDefinition consturctor in consturctors)
            {
                ILProcessor ilProcessor = consturctor.Body.GetILProcessor();
                Instruction ctorEnd = consturctor.Body.Instructions.Last();

                ilProcessor.InsertBefore(
                    ctorEnd
                    , Instruction.Create(OpCodes.Ldarg_0));

                ilProcessor.InsertBefore(
                    ctorEnd
                    , Instruction.Create(OpCodes.Ldarg_0));

                ilProcessor.InsertBefore(ctorEnd, _getInstanceID);

                ilProcessor.InsertBefore(
                    ctorEnd
                    , Instruction.Create(OpCodes.Stfld, InstanceId));
            }
        }

        private static void InjectIL(TypeDefinition type)
        {
            MethodDefinition method = type.GetMethods().FirstOrDefault(m => m.Name == "CompareTo");
            if (method == null)
            {
                File.AppendAllText(CecilManager.CecilLog, $"Can't find target method: BTLight.CompareTo\n");
            }

            Instruction loadField = Instruction.Create(OpCodes.Ldfld, InstanceId);

            List<int> loadFieldPosition = new List<int>(2);
            for (int i = 0; i < method.Body.Instructions.Count; i++)
            {
                Instruction instruction = method.Body.Instructions[i];

                if (instruction.Operand is MethodReference reference1
                        && _getInstanceID.Operand is MethodReference reference2
                        && reference1.FullName == reference2.FullName)
                {
                    loadFieldPosition.Add(i);
                }
            }

            if (loadFieldPosition.Count != 2)
            {
                File.AppendAllText(CecilManager.CecilLog, $"Can't patch BTLight.CompareTo\n");
            }

            foreach (int i in loadFieldPosition)
                method.Body.Instructions[i] = loadField;
        }
    }
}
