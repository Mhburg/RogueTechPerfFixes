using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RogueTechPerfFixes
{
    /// <summary>
    /// This class encapsulates what is needed to improve performance for an dll.
    /// </summary>
    public class PerformanceFixer
    {
        public bool Enable { get; private set; }

        public HarmonyPatcher HarmonyPatcher { get; private set; }

        public CecilInjector CecilInjector { get; private set; }
    }
}
