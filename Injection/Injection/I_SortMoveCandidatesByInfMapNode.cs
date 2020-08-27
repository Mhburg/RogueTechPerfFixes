using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace RogueTechPerfFixes.Injection
{
    [Obsolete]
    public class I_SortMoveCandidatesByInfMapNode : IInjector
    {
        private const string _targetType = "SortMoveCandidatesByInfMapNode";

        public const string ThinkTaskTypeName = "thinkTask";

        public const string OwnTaskTypeName = "ownTask";

        #region Implementation of IInjector

        public void Inject(Dictionary<string, TypeDefinition> typeTable, ModuleDefinition module)
        {
            if (typeTable.TryGetValue(_targetType, out TypeDefinition type))
            {
                TypeReference taskReference = module.ImportReference(typeof(Task<bool>));
                TypeReference booleanReference = module.ImportReference(typeof(bool));

                type.Fields.Add(
                    new FieldDefinition(
                        ThinkTaskTypeName
                        , FieldAttributes.Private | FieldAttributes.Static
                        , taskReference));

                type.Fields.Add(
                    new FieldDefinition(
                        OwnTaskTypeName
                        , FieldAttributes.Private
                        , booleanReference));
            }
        }

        #endregion
    }
}
