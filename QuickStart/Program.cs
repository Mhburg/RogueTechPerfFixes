using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using RogueTechPerfFixes;
using RogueTechPerfFixes.Injection;

namespace QuickStart
{
    class Program
    {
        private static string _probePath = Path.GetDirectoryName(CecilManager.VanillaAssemblyPath);

        static void Main(string[] args)
        {
            CecilManager.Init();
        }
    }
}
