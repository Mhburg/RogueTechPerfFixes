using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RogueTechPerfFixes
{
    public class Settings
    {
        public bool LogError = true;

        public bool LogDebug = false;

        public bool LogWarning = false;

        public PatchOption Patch = new PatchOption();

        public class PatchOption
        {
            public bool Vanilla = true;

            public bool LowVisibility = true;

            public bool CustomActivatableEquipment = true;

            public bool CustomUnit = true;
        }
    }
}
