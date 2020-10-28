using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Harmony;

namespace RogueTechPerfFixes
{
    public class HarmonyPatcher
    {
        private readonly string _assemblyAbsPath;

        private readonly string _id;

        public HarmonyPatcher(string assemblyAbsPath, string id)
        {
            _assemblyAbsPath = assemblyAbsPath;
            _id = id;
        }

        #region Overrides of Object

        public override string ToString()
        {
            return _id;
        }

        #endregion

        public void Patch()
        {
            try
            {
                HarmonyUtils.Harmony.PatchAll(Assembly.LoadFrom(_assemblyAbsPath));
            }
            catch (Exception e)
            {
                RTPFLogger.LogCritical(e.ToString());
            }
        }
    }
}
