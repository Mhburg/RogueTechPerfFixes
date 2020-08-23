using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BattleTech.Rendering;
using Harmony;
using RogueTechPerfFixes.Injection;

namespace RogueTechPerfFixes.HarmonyPatches
{
    public static class H_BTLightController
    {
        public static HarmonyUtils.RefGetter<List<BTLight>> Lights = HarmonyUtils.CreateStaticFieldRef<List<BTLight>>(typeof(BTLightController), "lightList");

        [HarmonyPatch(typeof(BTLightController), nameof(BTLightController.AddLight))]
        public static class H_AddLight
        {
            /// <summary>
            /// Check whether <see cref="BTLightController.InBatchProcess"/> is injected.
            /// </summary>
            /// <returns> Returns true, if the field is found in the class. </returns>
            public static bool Prepare()
            {
                return I_BTLightController.Init;
            }

            public static bool Prefix(BTLight light, List<BTLight> ___lightList)
            {
                if (BTLightController.InBatchProcess)
                {
                    if (!light.isInLightList)
                    {
                        ___lightList.Add(light);
                        light.isInLightList = true;
                    }

                    return false;
                }

                return true;
            }
        }
    }
}
