using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Locations;
using Crusaders30XX.ECS.Data.Save;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Services;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// System for handling quest initialization when a POI is selected.
    /// Subscribes to QuestSelectRequested and builds QueuedEvents for the quest.
    /// </summary>
    public class QuestStartSystem : Core.System
    {
        public QuestStartSystem(EntityManager entityManager) : base(entityManager)
        {
            EventManager.Subscribe<QuestSelectRequested>(OnQuestSelectRequested);
        }

        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            yield break;
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
            // Event-driven only; no per-entity update needed
        }

        private void OnQuestSelectRequested(QuestSelectRequested e)
        {
            var poi = e.Entity.GetComponent<PointOfInterest>();
            if (poi != null)
            {
                TryStartQuestFromPoi(poi);
            }
        }

        private void TryStartQuestFromPoi(PointOfInterest poi)
        {
            if (poi == null || string.IsNullOrEmpty(poi.Id)) return;

            // Check if this is a Dungeon POI
            if (poi.Type == PointOfInterestType.Dungeon)
            {
                StartDungeonEncounter(poi);
                return;
            }

            // Find location and quest index containing this POI
            string locationId = null;
            int questIndex = -1;
            var all = LocationDefinitionCache.GetAll();
            foreach (var kv in all)
            {
                var loc = kv.Value;
                if (loc?.pointsOfInterest == null) continue;
                for (int i = 0; i < loc.pointsOfInterest.Count; i++)
                {
                    if (string.Equals(loc.pointsOfInterest[i].id, poi.Id, System.StringComparison.OrdinalIgnoreCase))
                    {
                        locationId = kv.Key;
                        questIndex = i;
                        break;
                    }
                }
                if (questIndex >= 0) break;
            }
            if (string.IsNullOrEmpty(locationId) || questIndex < 0) return;

            // Build queued events from this quest
            var qeEntity = EntityManager.GetEntitiesWithComponent<QueuedEvents>().FirstOrDefault();
            if (qeEntity == null)
            {
                qeEntity = EntityManager.CreateEntity("QueuedEvents");
                EntityManager.AddComponent(qeEntity, new QueuedEvents());
                EntityManager.AddComponent(qeEntity, new DontDestroyOnLoad());
            }
            var qe = qeEntity.GetComponent<QueuedEvents>();
            // Ensure any previous dungeon loadout is removed for a normal quest
            EntityManager.RemoveComponent<DungeonLoadout>(qeEntity);
            qe.CurrentIndex = -1;
            qe.Events.Clear();
            qe.LocationId = locationId;
            qe.QuestIndex = questIndex;

            if (LocationDefinitionCache.TryGet(locationId, out var def) && def != null)
            {
                if (questIndex >= 0 && questIndex < (def.pointsOfInterest?.Count ?? 0))
                {
                    var questDefs = def.pointsOfInterest[questIndex];
                    foreach (var q in questDefs.events)
                    {
                        var type = ParseQueuedEventType(q?.type);
                        if (!string.IsNullOrEmpty(q?.id))
                        {
                            var queuedEvent = new QueuedEvent { EventId = q.id, EventType = type };
                            if (q.modifications != null)
                            {
                                queuedEvent.Modifications = new List<EnemyModification>(q.modifications);
                            }
                            qe.Events.Add(queuedEvent);
                        }
                    }
                }
            }

            if (qe.Events.Count > 0)
            {
                // Announce selection and transition to Battle
                SaveCache.SetLastLocation(poi.Id);
                EventManager.Publish(new QuestSelected { LocationId = locationId, QuestIndex = questIndex, QuestId = poi.Id });
                EventManager.Publish(new ShowTransition { Scene = SceneId.Battle });
            }
        }

        private void StartDungeonEncounter(PointOfInterest poi)
        {
            // Generate 3 random enemies
            var enemyIds = DungeonEncounterGeneratorService.GenerateRandomEnemyEncounter(3);
            
            // Find location and quest index containing this POI
            string locationId = null;
            int questIndex = -1;
            var all = LocationDefinitionCache.GetAll();
            foreach (var kv in all)
            {
                var loc = kv.Value;
                if (loc?.pointsOfInterest == null) continue;
                for (int i = 0; i < loc.pointsOfInterest.Count; i++)
                {
                    if (string.Equals(loc.pointsOfInterest[i].id, poi.Id, System.StringComparison.OrdinalIgnoreCase))
                    {
                        locationId = kv.Key;
                        questIndex = i;
                        break;
                    }
                }
                if (questIndex >= 0) break;
            }
            if (string.IsNullOrEmpty(locationId) || questIndex < 0) return;
            
            // Find or create QueuedEvents entity
            var qeEntity = EntityManager.GetEntitiesWithComponent<QueuedEvents>().FirstOrDefault();
            if (qeEntity == null)
            {
                qeEntity = EntityManager.CreateEntity("QueuedEvents");
                EntityManager.AddComponent(qeEntity, new QueuedEvents());
                EntityManager.AddComponent(qeEntity, new DontDestroyOnLoad());
            }
            
            var qe = qeEntity.GetComponent<QueuedEvents>();
            qe.CurrentIndex = -1;
            qe.Events.Clear();
            qe.LocationId = locationId;
            qe.QuestIndex = questIndex;

            // Generate and attach randomized dungeon loadout
            var dungeonLoadout = DungeonLoadoutGeneratorService.GenerateRandomLoadout();
            EntityManager.AddComponent(qeEntity, new DungeonLoadout { Loadout = dungeonLoadout });
            
            // Add enemy events
            foreach (var enemyId in enemyIds)
            {
                qe.Events.Add(new QueuedEvent 
                { 
                    EventId = enemyId, 
                    EventType = QueuedEventType.Enemy 
                });
            }
            
            // Start battle
            SaveCache.SetLastLocation(poi.Id);
            EventManager.Publish(new QuestSelected 
            { 
                LocationId = locationId, 
                QuestIndex = questIndex, 
                QuestId = poi.Id 
            });
            EventManager.Publish(new ShowTransition { Scene = SceneId.Battle });
        }

        private static QueuedEventType ParseQueuedEventType(string type)
        {
            if (string.IsNullOrEmpty(type)) return QueuedEventType.Enemy;
            switch (type.ToLowerInvariant())
            {
                case "enemy": return QueuedEventType.Enemy;
                case "event": return QueuedEventType.Event;
                case "shop": return QueuedEventType.Shop;
                case "church": return QueuedEventType.Church;
                default: return QueuedEventType.Enemy;
            }
        }
    }
}






