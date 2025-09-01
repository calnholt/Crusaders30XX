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
            System.Console.WriteLine("[CardPlaySystem] Subscribed to PlayCardRequested");
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
            var phase = EntityManager.GetEntitiesWithComponent<PhaseState>().FirstOrDefault().GetComponent<PhaseState>();
            if (phase.Sub != SubPhase.Action) { System.Console.WriteLine($"[CardPlaySystem] Ignored play, phase={phase}"); return; }

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

            // Gate by Action Points unless the card is a free action
            var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
            var ap = player?.GetComponent<ActionPoints>();
            bool isFree = def.isFreeAction;
            if (!isFree)
            {
                int currentAp = ap?.Current ?? 0;
                if (currentAp <= 0)
                {
                    System.Console.WriteLine("[CardPlaySystem] Ignored play, AP=0");
                    return; // cannot play without AP
                }
            }

            // Publish explicit effects per card id
            var enemy = EntityManager.GetEntitiesWithComponent<Enemy>().FirstOrDefault();
            int courage = player?.GetComponent<Courage>()?.Amount ?? 0;
            System.Console.WriteLine($"[CardPlaySystem] Resolving card id={def.id} name={data.Name}");
            switch (def.id)
            {
                case "strike":
                {
                    EventManager.Publish(new ModifyHpEvent { Target = enemy, Delta = -10 });
                    System.Console.WriteLine("[CardPlaySystem] Applied strike -10");
                    break;
                }
                case "strike_2":
                {
                    int dmg = courage >= 2 ? 12 : 8; // 8 base, +4 bonus
                    EventManager.Publish(new ModifyHpEvent { Target = enemy, Delta = -dmg });
                    break;
                }
                case "strike_3":
                {
                    int dmg = courage >= 3 ? 13 : 7; // 7 base, +6 bonus
                    EventManager.Publish(new ModifyHpEvent { Target = enemy, Delta = -dmg });
                    break;
                }
                case "strike_4":
                {
                    int dmg = courage >= 4 ? 14 : 6; // 6 base, +8 bonus
                    EventManager.Publish(new ModifyHpEvent { Target = enemy, Delta = -dmg });
                    break;
                }
                case "strike_5":
                {
                    int dmg = courage >= 5 ? 15 : 5; // 5 base, +10 bonus
                    EventManager.Publish(new ModifyHpEvent { Target = enemy, Delta = -dmg });
                    break;
                }
                case "strike_6":
                {
                    int dmg = courage >= 6 ? 16 : 4; // 4 base, +12 bonus
                    EventManager.Publish(new ModifyHpEvent { Target = enemy, Delta = -dmg });
                    break;
                }
                case "strike_7":
                {
                    int dmg = courage >= 7 ? 17 : 3; // 3 base, +14 bonus
                    EventManager.Publish(new ModifyHpEvent { Target = enemy, Delta = -dmg });
                    break;
                }
                case "strike_8":
                {
                    int dmg = courage >= 8 ? 18 : 2; // 2 base, +16 bonus
                    EventManager.Publish(new ModifyHpEvent { Target = enemy, Delta = -dmg });
                    break;
                }
                case "strike_9":
                {
                    int dmg = courage >= 9 ? 19 : 1; // 1 base, +18 bonus
                    EventManager.Publish(new ModifyHpEvent { Target = enemy, Delta = -dmg });
                    break;
                }
                case "strike_10":
                {
                    int dmg = courage >= 10 ? 20 : 0; // 0 base, +20 bonus
                    if (dmg > 0) EventManager.Publish(new ModifyHpEvent { Target = enemy, Delta = -dmg });
                    break;
                }
                case "stun":
                {
                    if (enemy != null)
                    {
                        var stun = enemy.GetComponent<Stun>();
                        if (stun == null)
                        {
                            stun = new Stun { Stacks = 0 };
                            EntityManager.AddComponent(enemy, stun);
                        }
                        stun.Stacks += 1;
                        System.Console.WriteLine($"[CardPlaySystem] Applied stun. Stacks={stun.Stacks}");
                    }
                    break;
                }
                default:
                    System.Console.WriteLine($"[CardPlaySystem] No effect for id={def.id}");
                    break;
            }

            // Move the played card to discard
            var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
            if (deckEntity != null)
            {
                EventManager.Publish(new CardMoveRequested { Card = evt.Card, Deck = deckEntity, Destination = CardZoneType.DiscardPile, Reason = "PlayCard" });
                System.Console.WriteLine("[CardPlaySystem] Requested move to DiscardPile");
            }

            // Consume 1 AP if not a free action
            if (!isFree)
            {
                EventManager.Publish(new ModifyActionPointsEvent { Delta = -1 });
                System.Console.WriteLine("[CardPlaySystem] Consumed 1 AP");
            }
        }

        
    }
}


