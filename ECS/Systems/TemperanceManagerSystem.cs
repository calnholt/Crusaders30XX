using System;
using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// Manages the player's Temperance resource by listening to events and applying changes.
    /// </summary>
    public class TemperanceManagerSystem : Core.System
    {
        public TemperanceManagerSystem(EntityManager entityManager) : base(entityManager)
        {
            EventManager.Subscribe<CardMoved>(OnCardMoved);
        }

        protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
        {
            return Array.Empty<Entity>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

        private void OnCardMoved(CardMoved evt)
        {
            // When assigned blocks land in discard, grant Temperance for white cards
            if (evt.To == CardZoneType.DiscardPile && evt.From == CardZoneType.AssignedBlock) {
              var data = evt.Card.GetComponent<CardData>();
              if (data == null || data.Color != CardData.CardColor.White) return;
              var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
              if (player == null) return;
              var t = player.GetComponent<Temperance>();
              if (t == null) { t = new Temperance(); EntityManager.AddComponent(player, t); }
              t.Amount = Math.Max(0, t.Amount + 1);
            }
        }
    }
}


