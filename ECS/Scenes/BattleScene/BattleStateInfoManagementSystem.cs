using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// Clears BattleStateInfo tracking dictionaries at key transitions.
    /// - Clear PhaseTracking whenever sub phase changes
    /// - Clear TurnTracking when Enemy turn starts
    /// - Clear BattleTracking when a new battle starts
    /// - Clear RunTracking when loading Battle scene
    /// </summary>
    public class BattleStateInfoManagementSystem : Core.System
    {
        public BattleStateInfoManagementSystem(EntityManager entityManager) : base(entityManager)
        {
            EventManager.Subscribe<ChangeBattlePhaseEvent>(OnChangeBattlePhase);
            EventManager.Subscribe<StartBattleRequested>(_ => ClearBattleTracking());
            EventManager.Subscribe<LoadSceneEvent>(OnLoadScene);
            EventManager.Subscribe<TrackingEvent>(OnTrackingEvent);
            EventManager.Subscribe<ModifyCourageEvent>(OnModifyCourage);
            EventManager.Subscribe<SetCourageEvent>(OnSetCourageEvent);
        }

        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return System.Array.Empty<Entity>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

        private void OnChangeBattlePhase(ChangeBattlePhaseEvent evt)
        {
            var st = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault().GetComponent<BattleStateInfo>();
            // Clear per-subphase tracking whenever sub phase changes
            st.PhaseTracking?.Clear();

            // When enemy turn starts, clear per-turn tracking
            if (evt.Current == SubPhase.EnemyStart)
            {
                st.TurnTracking?.Clear();
            }
        }

        private void ClearBattleTracking()
        {
            var st = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault().GetComponent<BattleStateInfo>();
            st.BattleTracking?.Clear();
        }

        private void OnLoadScene(LoadSceneEvent e)
        {
            var st = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault().GetComponent<BattleStateInfo>();
            st.RunTracking?.Clear();
        }

        private void OnTrackingEvent(TrackingEvent e)
        {
          AddToAllDictionaries(e);
        }

        private void AddToDict(Dictionary<TrackingTypeEnum, int> dict, TrackingEvent e)
        {
          if (dict == null) return;
          dict.TryGetValue(e.Type, out int current);
          int next = current + e.Delta;
          if (next == 0)
          {
              if (dict.ContainsKey(e.Type)) dict.Remove(e.Type);
          }
          else
          {
              dict[e.Type] = next;
          }
        }

        private void AddToAllDictionaries(TrackingEvent e)
        {
          var st = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault().GetComponent<BattleStateInfo>();
          AddToDict(st.RunTracking, e);
          AddToDict(st.BattleTracking, e);
          AddToDict(st.TurnTracking, e);
          AddToDict(st.PhaseTracking, e);
          Console.WriteLine($"[BattleStateInfoManagementSystem] AddToAllDictionaries - {e.Type} {e.Delta}");
        }

        private void OnModifyCourage(ModifyCourageEvent e)
        {
          var st = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault().GetComponent<BattleStateInfo>();
          OnTrackingEvent(new TrackingEvent { Type = e.Delta > 0 ? TrackingTypeEnum.CourageGained : TrackingTypeEnum.CourageLost, Delta = Math.Abs(e.Delta) });
        }
        private void OnSetCourageEvent(SetCourageEvent e)
        {
          var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
          var st = player.GetComponent<BattleStateInfo>();
          var courage = player.GetComponent<Courage>();
          var delta = courage.Amount - e.Amount;
          OnTrackingEvent(new TrackingEvent { Type = delta > 0 ? TrackingTypeEnum.CourageGained : TrackingTypeEnum.CourageLost, Delta = Math.Abs(delta) });
        }
    }
}


