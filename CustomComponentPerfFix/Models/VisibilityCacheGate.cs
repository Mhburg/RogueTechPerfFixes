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

        private readonly HashSet<AbstractActor> actors = new HashSet<AbstractActor>();

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
                List<ICombatant> uniDirectionalList = new List<ICombatant>();
                List<ICombatant> biDirectionalList = new List<ICombatant>();
                List<SharedVisibilityCache> sharedCache = new List<SharedVisibilityCache>();

                foreach (ICombatant combatant in combatants)
                {
                    if (combatant is AbstractActor actor)
                    {
                        if (actors.Contains(actor))
                        {
                            uniDirectionalList.Add(actor);
                        }
                        else
                        {
                            biDirectionalList.Add(actor);
                        }

                        if (actor.VisibilityCache.ReportVisibilityToParent && !sharedCache.Contains(actor.VisibilityCache.ParentCache))
                        {
                            sharedCache.Add(actor.VisibilityCache.ParentCache);
                        }
                    }
                }

                foreach (AbstractActor actor in actors)
                {
                    actor.VisibilityCache?.UpdateCacheReciprocal(biDirectionalList);
                    actor.VisibilityCache?.RebuildCache(uniDirectionalList);
                }

                RebuildSharedCache(sharedCache, combatants);

                if (!biDirectionalList.Any())
                {
                    List<TurnActor> turnActors = combatGameState.TurnDirector.TurnActors;
                    List<AITeam> aiTeams = turnActors.OfType<AITeam>().ToList();
                    foreach (AITeam aiTeam in aiTeams)
                    {
                        if (aiTeam.miscCombatants.FirstOrDefault() is AbstractActor actor)
                        {
                            CheckForAlert(actor.VisibilityCache);
                        }
                    }
                }

                actors.Clear();
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
            cacheGate.actors.Add(actor);
        }

        #region Overrides of ActionSemaphore

        public override void ResetSemaphore()
        {
            base.ResetSemaphore();
            actors.Clear();
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
