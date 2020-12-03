using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;

namespace RogueTechPerfFixes.Injection
{
    public interface IInjector
    {
        void Inject(Dictionary<string, TypeDefinition> typeTable, ModuleDefinition module);
    }
}
