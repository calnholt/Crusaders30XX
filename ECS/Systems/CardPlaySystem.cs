using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Data.Cards;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// Handles simple action-phase card plays from JSON definitions.
    /// Currently supports Strike-like damage with optional Courage threshold bonus.
    /// </summary>
    public class CardPlaySystem : Core.System
    {
        public CardPlaySystem(EntityManager entityManager) : base(entityManager)
        {
            EventManager.Subscribe<PlayCardRequested>(OnPlayCardRequested);
        }

        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return Array.Empty<Entity>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

        private void OnPlayCardRequested(PlayCardRequested evt)
        {
            if (evt?.Card == null) return;

            // Only in Action phase
            var phase = EntityManager.GetEntitiesWithComponent<BattlePhaseState>().FirstOrDefault()?.GetComponent<BattlePhaseState>()?.Phase ?? BattlePhase.StartOfBattle;
            if (phase != BattlePhase.Action) return;

            var data = evt.Card.GetComponent<CardData>();
            if (data == null) return;

            // Lookup by normalized name -> id (e.g., "Strike 3" -> "strike_3")
            string id = (data.Name ?? string.Empty).Trim().ToLowerInvariant().Replace(' ', '_');
            if (string.IsNullOrEmpty(id)) return;
            if (!CardDefinitionCache.TryGet(id, out var def))
            {
                // Fallback: try raw name without spaces (compat)
                string alt = (data.Name ?? string.Empty).Trim().ToLowerInvariant();
                if (!CardDefinitionCache.TryGet(alt, out def)) return;
            }

            int totalDamage = ComputeDamageFromId(def.id);

            // Apply damage to first enemy
            var enemy = EntityManager.GetEntitiesWithComponent<Enemy>().FirstOrDefault();
            if (enemy != null && totalDamage != 0)
            {
                EventManager.Publish(new ModifyHpEvent { Target = enemy, Delta = -System.Math.Abs(totalDamage) });
            }

            // Move the played card to discard
            var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
            if (deckEntity != null)
            {
                EventManager.Publish(new CardMoveRequested { Card = evt.Card, Deck = deckEntity, Destination = CardZoneType.DiscardPile, Reason = "PlayCard" });
            }
        }

        private int ComputeDamageFromId(string id)
        {
            if (string.IsNullOrEmpty(id)) return 0;
            var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
            int courage = player?.GetComponent<Courage>()?.Amount ?? 0;
            switch (id)
            {
                case "strike": return 10;
                case "strike_2": return courage >= 2 ? (8 + 4) : 8;
                case "strike_3": return courage >= 3 ? (7 + 6) : 7;
                case "strike_4": return courage >= 4 ? (6 + 8) : 6;
                case "strike_5": return courage >= 5 ? (5 + 10) : 5;
                case "strike_6": return courage >= 6 ? (4 + 12) : 4;
                case "strike_7": return courage >= 7 ? (3 + 14) : 3;
                case "strike_8": return courage >= 8 ? (2 + 16) : 2;
                case "strike_9": return courage >= 9 ? (1 + 18) : 1;
                case "strike_10": return courage >= 10 ? (0 + 20) : 0;
                default: return 0;
            }
        }
    }
}


