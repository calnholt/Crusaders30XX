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
            EventManager.Subscribe<PayCostSatisfied>(OnPayCostSatisfied);
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

            // Evaluate any additional costs/requirements tied to the card id
            if (!EvaluateAdditionalCostService.CanPay(EntityManager, def.id))
            {
                System.Console.WriteLine($"[CardPlaySystem] Additional cost check failed for id={def.id}; aborting play");
                return;
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

            // If costs are not yet paid, determine if payment is needed and either auto-resolve or open overlay
            if (!evt.CostsPaid)
            {
                var requiredCosts = (def.cost ?? System.Array.Empty<string>()).ToList();
                if (requiredCosts.Count > 0)
                {
                    // Build hand color multiset excluding the card being played
                    var deckEntityForCost = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
                    var deck = deckEntityForCost?.GetComponent<Deck>();
                    if (deck == null) return;
                    var handOthers = deck.Hand.Where(c => c != evt.Card).ToList();

                    // Helper to attempt greedy satisfaction of remaining requirements
                    bool CanSatisfy(List<string> req, List<Entity> candidates, out List<Entity> picks)
                    {
                        picks = new List<Entity>();
                        var remaining = new List<string>(req);
                        // Prefer matching specific colors first
                        foreach (var e in candidates)
                        {
                            if (remaining.Count == 0) break;
                            var cd = e.GetComponent<CardData>();
                            if (cd == null) continue;
                            int idx = remaining.FindIndex(r =>
                                (r == "Red" && cd.Color == CardData.CardColor.Red) ||
                                (r == "White" && cd.Color == CardData.CardColor.White) ||
                                (r == "Black" && cd.Color == CardData.CardColor.Black));
                            if (idx >= 0)
                            {
                                picks.Add(e);
                                remaining.RemoveAt(idx);
                            }
                        }
                        // Then satisfy Any with remaining cards
                        foreach (var e in candidates)
                        {
                            if (remaining.Count == 0) break;
                            if (!picks.Contains(e))
                            {
                                int idx = remaining.FindIndex(r => r == "Any");
                                if (idx >= 0)
                                {
                                    picks.Add(e);
                                    remaining.RemoveAt(idx);
                                }
                            }
                        }
                        return remaining.Count == 0;
                    }

                    if (CanSatisfy(requiredCosts, handOthers, out var autoPicks))
                    {
                        // Deterministic auto-pick when each specific color has exactly the needed count, and Any is exact
                        int needRed = requiredCosts.Count(c => c == "Red");
                        int needWhite = requiredCosts.Count(c => c == "White");
                        int needBlack = requiredCosts.Count(c => c == "Black");
                        int needAny = requiredCosts.Count(c => c == "Any");
                        var redCards = handOthers.Where(e => e.GetComponent<CardData>()?.Color == CardData.CardColor.Red).ToList();
                        var whiteCards = handOthers.Where(e => e.GetComponent<CardData>()?.Color == CardData.CardColor.White).ToList();
                        var blackCards = handOthers.Where(e => e.GetComponent<CardData>()?.Color == CardData.CardColor.Black).ToList();
                        var deterministic = new List<Entity>();
                        bool specificExact = (needRed == redCards.Count || needRed == 0)
                                           && (needWhite == whiteCards.Count || needWhite == 0)
                                           && (needBlack == blackCards.Count || needBlack == 0);
                        if (specificExact)
                        {
                            if (needRed == redCards.Count) deterministic.AddRange(redCards);
                            if (needWhite == whiteCards.Count) deterministic.AddRange(whiteCards);
                            if (needBlack == blackCards.Count) deterministic.AddRange(blackCards);
                            var remaining = handOthers.Except(deterministic).ToList();
                            if (needAny == 0 || remaining.Count == needAny)
                            {
                                if (needAny > 0) deterministic.AddRange(remaining);
                                if (deterministic.Count == requiredCosts.Count)
                                {
                                    // Auto-discard deterministic set and continue
                                    foreach (var p in deterministic)
                                    {
                                        EventManager.Publish(new CardMoveRequested { Card = p, Deck = deckEntityForCost, Destination = CardZoneType.DiscardPile, Reason = "AutoPayCost" });
                                    }
                                    EventManager.Publish(new PlayCardRequested { Card = evt.Card, CostsPaid = true });
                                    return;
                                }
                            }
                        }
                        if (autoPicks.Count == 1 && handOthers.Count == 1)
                        {
                            // Single deterministic discard -> auto pay and continue
                            System.Console.WriteLine("[CardPlaySystem] Auto-paying cost with only available card");
                            EventManager.Publish(new CardMoveRequested { Card = autoPicks[0], Deck = deckEntityForCost, Destination = CardZoneType.DiscardPile, Reason = "AutoPayCost" });
                            // Re-dispatch play with CostsPaid=true
                            EventManager.Publish(new PlayCardRequested { Card = evt.Card, CostsPaid = true });
                            return;
                        }
                        else if (requiredCosts.All(c => c == "Any") && autoPicks.Count == requiredCosts.Count && handOthers.Count == autoPicks.Count)
                        {
                            // Exact number of cards equal to Any requirement -> auto pay all
                            foreach (var p in autoPicks)
                            {
                                EventManager.Publish(new CardMoveRequested { Card = p, Deck = deckEntityForCost, Destination = CardZoneType.DiscardPile, Reason = "AutoPayCost" });
                            }
                            EventManager.Publish(new PlayCardRequested { Card = evt.Card, CostsPaid = true });
                            return;
                        }
                        else
                        {
                            // Open overlay to let player choose among options
                            System.Console.WriteLine("[CardPlaySystem] Opening pay-cost overlay");
                            EventManager.Publish(new OpenPayCostOverlayEvent { CardToPlay = evt.Card, RequiredCosts = requiredCosts });
                            return;
                        }
                    }
                    else
                    {
                        System.Console.WriteLine("[CardPlaySystem] Cannot satisfy cost requirements; aborting play");
                        return;
                    }
                }
            }

            // Delegate per-card effects to service
            CardPlayService.Resolve(EntityManager, def.id, data.Name);

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

        private void OnPayCostSatisfied(PayCostSatisfied evt)
        {
            if (evt?.CardToPlay == null) return;
            // Once costs are paid, proceed to resolve effect by re-publishing play with CostsPaid
            EventManager.Publish(new PlayCardRequested { Card = evt.CardToPlay, CostsPaid = true });
        }

        
    }
}


