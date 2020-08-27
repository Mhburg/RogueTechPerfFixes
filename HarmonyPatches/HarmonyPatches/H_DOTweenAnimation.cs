using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleTech.UI;
using DG.Tweening;
using DG.Tweening.Core;
using Harmony;

namespace RogueTechPerfFixes.HarmonyPatches
{
    public static class H_DOTweenAnimation
    {
        [HarmonyPatch(typeof(DOTweenAnimation), nameof(DOTweenAnimation.DOKill))]
        public static class H_DoKill
        {
            public static bool Prepare()
            {
                return Mod.Settings.Patch.Vanilla;
            }

            public static bool Prefix(DOTweenAnimation __instance)
            {
                Tween tween = __instance.tween;
                if (tween != null)
                {
                    DOTween.Kill(tween.id != null ? __instance.tween.id : __instance.tween);
                    __instance.tween = null;
                }

                return false;
            }
        }
    }
}
