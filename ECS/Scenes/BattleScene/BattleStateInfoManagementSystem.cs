using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System;
using Crusaders30XX.Diagnostics;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// Clears BattleStateInfo tracking dictionaries at key transitions.
    /// - Clear PhaseTracking whenever sub phase changes
    /// - Clear TurnTracking when Enemy turn starts
    /// - Clear BattleTracking when a new battle starts
    /// - Clear RunTracking when loading Battle scene
    /// </summary>
    [DebugTab("Battle State Info")]
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
            EventManager.Subscribe<ApplyEffect>(OnApplyEffect);
        }

        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return Array.Empty<Entity>();
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

        private void AddToDict(Dictionary<string, int> dict, TrackingEvent e)
        {
          if (dict == null) return;
          dict.TryGetValue(e.Type.ToString(), out int current);
          int next = current + e.Delta;
          if (next == 0)
          {
              if (dict.ContainsKey(e.Type.ToString())) dict.Remove(e.Type.ToString());
          }
          else
          {
              dict[e.Type.ToString()] = next;
          }
        }

        private void AddToAllDictionaries(TrackingEvent e)
        {
          var st = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault().GetComponent<BattleStateInfo>();
          AddToDict(st.RunTracking, e);
          AddToDict(st.BattleTracking, e);
          AddToDict(st.TurnTracking, e);
          AddToDict(st.PhaseTracking, e);
        }

        private void OnModifyCourage(ModifyCourageEvent e)
        {
          OnTrackingEvent(new TrackingEvent { Type = e.Delta > 0 ? TrackingTypeEnum.CourageGained.ToString() : TrackingTypeEnum.CourageLost.ToString(), Delta = Math.Abs(e.Delta) });
        }
        private void OnSetCourageEvent(SetCourageEvent e)
        {
          var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
          var courage = player.GetComponent<Courage>();
          var delta = courage.Amount - e.Amount;
          OnTrackingEvent(new TrackingEvent { Type = delta > 0 ? TrackingTypeEnum.CourageGained.ToString() : TrackingTypeEnum.CourageLost.ToString(), Delta = Math.Abs(delta) });
        }
        private void OnApplyEffect(ApplyEffect e)
        {
          if (!string.IsNullOrEmpty(e.attackId))
          {
            OnTrackingEvent(new TrackingEvent { Type = TrackingTypeEnum.NumberOfAttacksHitPlayer.ToString(), Delta = 1 });
            OnTrackingEvent(new TrackingEvent { Type = e.attackId, Delta = 1 });
          }
        }

        [DebugAction("Print Tracking")]
        private void debug_PrintTracking()
        {
          var st = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault().GetComponent<BattleStateInfo>();
          Console.WriteLine($"[BattleStateInfoManagementSystem] RunTracking: {string.Join(", ", st.RunTracking.Select(kvp => $"{kvp.Key}: {kvp.Value}"))}");
          Console.WriteLine($"[BattleStateInfoManagementSystem] BattleTracking: {string.Join(", ", st.BattleTracking.Select(kvp => $"{kvp.Key}: {kvp.Value}"))}");
          Console.WriteLine($"[BattleStateInfoManagementSystem] TurnTracking: {string.Join(", ", st.TurnTracking.Select(kvp => $"{kvp.Key}: {kvp.Value}"))}");
          Console.WriteLine($"[BattleStateInfoManagementSystem] PhaseTracking: {string.Join(", ", st.PhaseTracking.Select(kvp => $"{kvp.Key}: {kvp.Value}"))}");
        }
    }
}


