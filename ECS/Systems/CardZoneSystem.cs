using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// Single authority for moving cards between zones (hand, discard, draw, exhaust, assigned block).
    /// Other systems should publish CardMoveRequested events instead of mutating Deck lists directly.
    /// </summary>
    public class CardZoneSystem : Core.System
    {
        public CardZoneSystem(EntityManager entityManager) : base(entityManager)
        {
            EventManager.Subscribe<CardMoveRequested>(OnCardMoveRequested);
        }

        protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
        {
            return System.Array.Empty<Entity>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

        private void OnCardMoveRequested(CardMoveRequested evt)
        {
            if (evt == null || evt.Card == null) return;

            var deckEntity = evt.Deck ?? EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
            var deck = deckEntity?.GetComponent<Deck>();
            if (deck == null) return;

            var from = GetZoneOf(deck, evt.Card);

            // Remove from all known deck lists first
            deck.DrawPile.Remove(evt.Card);
            deck.Hand.Remove(evt.Card);
            deck.DiscardPile.Remove(evt.Card);
            deck.ExhaustPile.Remove(evt.Card);

            switch (evt.Destination)
            {
                case CardZoneType.Hand:
                {
                    var abc = evt.Card.GetComponent<AssignedBlockCard>();
                    if (abc != null)
                    {
                        EntityManager.RemoveComponent<AssignedBlockCard>(evt.Card);
                    }
                    deck.Hand.Add(evt.Card);
                    var t = evt.Card.GetComponent<Transform>();
                    if (t != null && t.Position == Vector2.Zero)
                    {
                        t.Position = Vector2.Zero;
                        t.Rotation = 0f;
                        t.Scale = Vector2.One;
                    }
                    break;
                }
                case CardZoneType.AssignedBlock:
                {
                    var t = evt.Card.GetComponent<Transform>();
                    var abc = evt.Card.GetComponent<AssignedBlockCard>();
                    if (abc == null)
                    {
                        abc = new AssignedBlockCard
                        {
                            ContextId = evt.ContextId,
                            BlockAmount = System.Math.Max(1, evt.Card.GetComponent<CardData>()?.BlockValue ?? 1),
                            StartPos = t?.Position ?? Vector2.Zero,
                            CurrentPos = t?.Position ?? Vector2.Zero,
                            TargetPos = t?.Position ?? Vector2.Zero,
                            StartScale = t?.Scale.X ?? 1f,
                            TargetScale = 0.35f,
                            Phase = AssignedBlockCard.PhaseState.Pullback,
                            Elapsed = 0f,
                            AssignedAtTicks = System.DateTime.UtcNow.Ticks
                        };
                        EntityManager.AddComponent(evt.Card, abc);
                    }
                    break;
                }
                case CardZoneType.DiscardPile:
                {
                    deck.DiscardPile.Add(evt.Card);
                    break;
                }
                case CardZoneType.DrawPile:
                {
                    deck.DrawPile.Add(evt.Card);
                    break;
                }
                case CardZoneType.ExhaustPile:
                {
                    deck.ExhaustPile.Add(evt.Card);
                    break;
                }
            }

            EventManager.Publish(new CardMoved
            {
                Card = evt.Card,
                Deck = deckEntity,
                From = from,
                To = evt.Destination,
                ContextId = evt.ContextId
            });
        }

        private static CardZoneType GetZoneOf(Deck deck, Entity card)
        {
            if (deck.Hand.Contains(card)) return CardZoneType.Hand;
            if (deck.DrawPile.Contains(card)) return CardZoneType.DrawPile;
            if (deck.DiscardPile.Contains(card)) return CardZoneType.DiscardPile;
            if (deck.ExhaustPile.Contains(card)) return CardZoneType.ExhaustPile;
            return CardZoneType.AssignedBlock;
        }
    }
}


