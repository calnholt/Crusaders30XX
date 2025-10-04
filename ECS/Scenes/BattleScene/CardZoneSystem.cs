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
            EventManager.Subscribe<CardMoved>(OnCardMoved);
            System.Console.WriteLine("[CardZoneSystem] Subscribed to CardMoveRequested");
        }

        protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
        {
            return System.Array.Empty<Entity>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

        private void OnCardMoved(CardMoved evt)
        {
            if (evt.From == CardZoneType.AssignedBlock && evt.To == CardZoneType.DiscardPile)
            {
                CardBlockService.Resolve(evt.Card, EntityManager);
            }
        }

        private void OnCardMoveRequested(CardMoveRequested evt)
        {
            System.Console.WriteLine($"[CardZoneSystem] CardMoveRequested card={evt.Card?.Id} to={evt.Destination} reason={evt.Reason}");
            if (evt == null || evt.Card == null) return;

            var deckEntity = evt.Deck ?? EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
            var deck = deckEntity?.GetComponent<Deck>();
            if (deck == null) return;

            var from = GetZoneOf(deck, evt.Card);

            // Remove from all known deck lists first to avoid duplicates
            deck.DrawPile.Remove(evt.Card);
            deck.Hand.Remove(evt.Card);
            deck.DiscardPile.Remove(evt.Card);
            deck.ExhaustPile.Remove(evt.Card);

            switch (evt.Destination)
            {
                case CardZoneType.Hand:
                {
                    // When returning to hand, remove assignment component and ensure we don't duplicate in hand
                    var abc = evt.Card.GetComponent<AssignedBlockCard>();
                    if (abc != null)
                    {
                        EntityManager.RemoveComponent<AssignedBlockCard>(evt.Card);
                    }
                    if (!deck.Hand.Contains(evt.Card))
                    {
                        deck.Hand.Add(evt.Card);
                    }
                    // Make card interactable in hand
                    var uiH = evt.Card.GetComponent<UIElement>();
                    if (uiH != null)
                    {
                        uiH.IsInteractable = true;
                        uiH.IsHovered = false;
                        uiH.IsClicked = false;
                    }
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
                        var cd = evt.Card.GetComponent<CardData>();
                        var bg = ResolveCardBgColor(cd?.Color ?? CardData.CardColor.White);
                        var fg = ResolveFgForBg(bg);
                        abc = new AssignedBlockCard
                        {
                            ContextId = evt.ContextId,
                            BlockAmount = BlockValueService.GetBlockValue(evt.Card),
                            StartPos = t?.Position ?? Vector2.Zero,
                            CurrentPos = t?.Position ?? Vector2.Zero,
                            TargetPos = t?.Position ?? Vector2.Zero,
                            StartScale = t?.Scale.X ?? 1f,
                            TargetScale = 0.35f,
                            Phase = AssignedBlockCard.PhaseState.Pullback,
                            Elapsed = 0f,
                            AssignedAtTicks = System.DateTime.UtcNow.Ticks,
                            IsEquipment = false,
                            ColorKey = NormalizeColorKey(cd?.Color.ToString() ?? "White"),
                            Tooltip = ResolveCardName(cd),
                            DisplayBgColor = bg,
                            DisplayFgColor = fg
                        };
                        EntityManager.AddComponent(evt.Card, abc);
                    }
                    // Ensure the card is not still in hand to prevent later clicks from finding it
                    deck.Hand.Remove(evt.Card);
                    // Make assigned block cards non-interactable as cards (interactions happen via assigned UI)
                    var uiA = evt.Card.GetComponent<UIElement>();
                    if (uiA != null)
                    {
                        uiA.IsInteractable = false;
                        uiA.IsHovered = false;
                        uiA.IsClicked = false;
                    }
                    break;
                }
                case CardZoneType.DiscardPile:
                {
                    deck.DiscardPile.Add(evt.Card);
                    // Disable interactions for discarded cards
                    var uiD = evt.Card.GetComponent<UIElement>();
                    if (uiD != null)
                    {
                        uiD.IsInteractable = false;
                        uiD.IsHovered = false;
                        uiD.IsClicked = false;
                    }
                    // Push discarded cards behind UI
                    var t = evt.Card.GetComponent<Transform>();
                    if (t != null) t.ZOrder = -10000;
                    break;
                }
                case CardZoneType.DrawPile:
                {
                    deck.DrawPile.Add(evt.Card);
                    // Not interactable in draw pile
                    var uiDP = evt.Card.GetComponent<UIElement>();
                    if (uiDP != null)
                    {
                        uiDP.IsInteractable = false;
                        uiDP.IsHovered = false;
                        uiDP.IsClicked = false;
                    }
                    // Reset transform so highlight hit-test uses proper defaults when re-drawn
                    var tdp = evt.Card.GetComponent<Transform>();
                    if (tdp != null)
                    {
                        tdp.Rotation = 0f;
                        if (tdp.Position == Vector2.Zero) { tdp.Position = Vector2.Zero; }
                        tdp.Scale = Vector2.One;
                    }
                    break;
                }
                case CardZoneType.ExhaustPile:
                {
                    deck.ExhaustPile.Add(evt.Card);
                    // Not interactable in exhaust pile
                    var uiE = evt.Card.GetComponent<UIElement>();
                    if (uiE != null)
                    {
                        uiE.IsInteractable = false;
                        uiE.IsHovered = false;
                        uiE.IsClicked = false;
                    }
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
            System.Console.WriteLine($"[CardZoneSystem] CardMoved from={from} to={evt.Destination}");
        }

        private static string NormalizeColorKey(string c)
        {
            if (string.IsNullOrWhiteSpace(c)) return "White";
            switch (c.Trim().ToLowerInvariant())
            {
                case "r": case "red": return "Red";
                case "w": case "white": return "White";
                case "b": case "black": return "Black";
                default: return char.ToUpperInvariant(c[0]) + c.Substring(1);
            }
        }

        private static Microsoft.Xna.Framework.Color ResolveCardBgColor(CardData.CardColor color)
        {
            switch (color)
            {
                case CardData.CardColor.Red: return Microsoft.Xna.Framework.Color.DarkRed;
                case CardData.CardColor.Black: return Microsoft.Xna.Framework.Color.Black;
                case CardData.CardColor.White:
                default: return Microsoft.Xna.Framework.Color.White;
            }
        }

        private static Microsoft.Xna.Framework.Color ResolveFgForBg(Microsoft.Xna.Framework.Color bg)
        {
            return (bg == Microsoft.Xna.Framework.Color.White) ? Microsoft.Xna.Framework.Color.Black : Microsoft.Xna.Framework.Color.White;
        }

        private static string ResolveCardName(CardData cd)
        {
            if (cd == null) return string.Empty;
            try
            {
                if (Crusaders30XX.ECS.Data.Cards.CardDefinitionCache.TryGet(cd.CardId ?? string.Empty, out var def) && def != null)
                {
                    return def.name ?? def.id ?? cd.CardId ?? string.Empty;
                }
            }
            catch { }
            return cd.CardId ?? string.Empty;
        }

        private static CardZoneType GetZoneOf(Deck deck, Entity card)
        {
            if (deck.Hand.Contains(card)) return CardZoneType.Hand;
            if (deck.DrawPile.Contains(card)) return CardZoneType.DrawPile;
            if (deck.DiscardPile.Contains(card)) return CardZoneType.DiscardPile;
            if (deck.ExhaustPile.Contains(card)) return CardZoneType.ExhaustPile;
            return CardZoneType.ExhaustPile;
        }
    }
}


