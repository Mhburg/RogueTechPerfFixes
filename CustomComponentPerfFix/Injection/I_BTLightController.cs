using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace RogueTechPerfFixes.Injection
{
    public class I_BTLightController : IInjector
    {
        private const string _targetType = "BattleTech.Rendering.BTLightController";

        private static FieldDefinition InBatchProcess;

        public static bool Init { get; private set; } = false;

        #region Implementation of IInjector

        public void Inject(Dictionary<string, TypeDefinition> typeTable, ModuleDefinition module)
        {
            if (typeTable.TryGetValue(_targetType, out TypeDefinition type))
            {
                InjectField(type, module);
                Init = true;
            }
        }

        #endregion

        private static void InjectField(TypeDefinition type, ModuleDefinition module)
        {
            TypeReference boolReference = module.ImportReference(typeof(bool));

            InBatchProcess = new FieldDefinition(
                nameof(InBatchProcess)
                , FieldAttributes.Public | FieldAttributes.Static
                , boolReference);

            type.Fields.Add(InBatchProcess);
        }
    }
}
