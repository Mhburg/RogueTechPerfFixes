using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleTech;
using Harmony;

namespace RogueTechPerfFixes
{
    /// <summary>
    /// Pool all requests to rebuild the visibility cache into one place.
    /// </summary>
    public class VisibilityCacheGate : ActionSemaphore
    {
        private static VisibilityCacheGate cacheGate = new VisibilityCacheGate();

        private readonly HashSet<AbstractActor> selfCacheActors = new HashSet<AbstractActor>();

        private readonly HashSet<AbstractActor> biCacheActors = new HashSet<AbstractActor>();

        private delegate void CheckForAlertDelegate(VisibilityCache cache);

        private static CheckForAlertDelegate CheckForAlert =
            (CheckForAlertDelegate)Delegate.CreateDelegate(
                typeof(CheckForAlertDelegate), typeof(VisibilityCache).GetMethod("checkForAlert", AccessTools.all));

        private VisibilityCacheGate()
            : base(null, null)
        {
            shouldTakeaction = () => counter == 0;
            actionToTake = () =>
            {
                CombatGameState combatGameState = UnityGameInstance.BattleTechGame.Combat;
                List<ICombatant> combatants = combatGameState.GetAllLivingCombatants();

                foreach (AbstractActor actor in selfCacheActors.ToList())
                {
                    if (biCacheActors.Contains(actor))
                    {
                        selfCacheActors.Remove(actor);
                    }
                }

                foreach (AbstractActor actor in selfCacheActors)
                {
                    actor.RebuildVisibilityCache(combatants);
                }

                foreach (AbstractActor actor in biCacheActors)
                {
                    actor.UpdateVisibilityCache(combatants);
                }

                selfCacheActors.Clear();
                biCacheActors.Clear();
            };
        }

        public static bool Active => cacheGate.counter > 0;

        public static int GetCounter => cacheGate.counter;

        public static void EnterGate()
        {
            cacheGate.Enter();
        }

        public static void ExitGate()
        {
            cacheGate.Exit();
        }

        public static void ExitAll()
        {
            cacheGate.ResetHard();
        }

        public static void Reset()
        {
            cacheGate.ResetSemaphore();
        }

        public static void AddActorToRefresh(AbstractActor actor)
        {
            cacheGate.selfCacheActors.Add(actor);
        }

        public static void AddActorToRefreshReciprocal(AbstractActor actor)
        {
            cacheGate.biCacheActors.Add(actor);
        }

        #region Overrides of ActionSemaphore

        public override void ResetSemaphore()
        {
            base.ResetSemaphore();
            selfCacheActors.Clear();
        }

        #endregion

        private static void RebuildSharedCache(List<SharedVisibilityCache> list, List<ICombatant> combatatns)
        {
            for (int j = 0; j < list.Count; j++)
            {
                list[j].RebuildCache(combatatns);
                if (list[j].ReportVisibilityToParent)
                {
                    list.Add(list[j].ParentCache);
                }
            }
        }
    }
}
