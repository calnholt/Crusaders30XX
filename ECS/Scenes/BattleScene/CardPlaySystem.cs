using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System;
using Crusaders30XX.ECS.Factories;
using Crusaders30XX.ECS.Objects.Cards;
using Crusaders30XX.ECS.Services;
using Crusaders30XX.ECS.Objects.EnemyAttacks;
using Crusaders30XX.ECS.Data.Save;

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
            Console.WriteLine("[CardPlaySystem] Subscribed to PlayCardRequested");
            EventManager.Subscribe<PayCostSatisfied>(OnPayCostSatisfied);
        }

        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return Array.Empty<Entity>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

        /// <summary>
        /// Counts distinct ways to satisfy cost requirements (order-independent multisets).
        /// Returns the count of distinct solutions, stopping early if count > 1 for optimization.
        /// </summary>
        private int CountDistinctSolutions(List<string> requiredCosts, List<Entity> handCards)
        {
            // Safety check: exclude Yellow cards
            var validCards = handCards.Where(e => 
            {
                var cd = e.GetComponent<CardData>();
                return cd != null && cd.Color != CardData.CardColor.Yellow;
            }).ToList();

            if (requiredCosts.Count == 0)
                return validCards.Count == 0 ? 1 : 0; // Empty requirement means no cards needed

            int count = 0;
            HashSet<string> seenSolutions = new HashSet<string>();

            // Recursive helper to count solutions
            void CountSolutionsRecursive(List<string> remainingCosts, List<Entity> availableCards, HashSet<Entity> usedCards, List<Entity> currentSolution)
            {
                // Early exit optimization: if we already found more than 1 solution, stop
                if (count > 1) return;

                if (remainingCosts.Count == 0)
                {
                    // Found a valid solution - create a canonical representation (sorted by entity ID)
                    var solutionKey = string.Join(",", currentSolution.OrderBy(e => e.Id).Select(e => e.Id.ToString()));
                    if (!seenSolutions.Contains(solutionKey))
                    {
                        seenSolutions.Add(solutionKey);
                        count++;
                    }
                    return;
                }

                string nextCost = remainingCosts[0];
                var remainingCostsAfter = remainingCosts.Skip(1).ToList();

                // Find all cards that can satisfy this cost requirement
                var candidates = availableCards.Where(card => 
                {
                    if (usedCards.Contains(card)) return false;
                    var cd = card.GetComponent<CardData>();
                    if (cd == null || cd.Color == CardData.CardColor.Yellow) return false;

                    if (nextCost == "Any") return true;
                    if (nextCost == "Red" && cd.Color == CardData.CardColor.Red) return true;
                    if (nextCost == "White" && cd.Color == CardData.CardColor.White) return true;
                    if (nextCost == "Black" && cd.Color == CardData.CardColor.Black) return true;
                    return false;
                }).ToList();

                // Try each candidate for this requirement
                foreach (var candidate in candidates)
                {
                    var newUsedCards = new HashSet<Entity>(usedCards) { candidate };
                    var newSolution = new List<Entity>(currentSolution) { candidate };
                    CountSolutionsRecursive(remainingCostsAfter, availableCards, newUsedCards, newSolution);
                    
                    // Early exit optimization
                    if (count > 1) return;
                }
            }

            CountSolutionsRecursive(requiredCosts, validCards, new HashSet<Entity>(), new List<Entity>());
            return count;
        }

        /// <summary>
        /// Finds the first valid solution for satisfying cost requirements.
        /// Assumes count == 1, so returns the single solution.
        /// </summary>
        private List<Entity> FindFirstSolution(List<string> requiredCosts, List<Entity> handCards)
        {
            // Safety check: exclude Yellow cards
            var validCards = handCards.Where(e => 
            {
                var cd = e.GetComponent<CardData>();
                return cd != null && cd.Color != CardData.CardColor.Yellow;
            }).ToList();

            if (requiredCosts.Count == 0)
                return new List<Entity>();

            List<Entity> solution = null;

            // Recursive helper to find first solution
            bool FindSolutionRecursive(List<string> remainingCosts, List<Entity> availableCards, HashSet<Entity> usedCards, List<Entity> currentSolution)
            {
                if (remainingCosts.Count == 0)
                {
                    solution = new List<Entity>(currentSolution);
                    return true; // Found solution
                }

                string nextCost = remainingCosts[0];
                var remainingCostsAfter = remainingCosts.Skip(1).ToList();

                // Find all cards that can satisfy this cost requirement
                var candidates = availableCards.Where(card => 
                {
                    if (usedCards.Contains(card)) return false;
                    var cd = card.GetComponent<CardData>();
                    if (cd == null || cd.Color == CardData.CardColor.Yellow) return false;

                    if (nextCost == "Any") return true;
                    if (nextCost == "Red" && cd.Color == CardData.CardColor.Red) return true;
                    if (nextCost == "White" && cd.Color == CardData.CardColor.White) return true;
                    if (nextCost == "Black" && cd.Color == CardData.CardColor.Black) return true;
                    return false;
                }).ToList();

                // Try each candidate for this requirement
                foreach (var candidate in candidates)
                {
                    var newUsedCards = new HashSet<Entity>(usedCards) { candidate };
                    var newSolution = new List<Entity>(currentSolution) { candidate };
                    if (FindSolutionRecursive(remainingCostsAfter, availableCards, newUsedCards, newSolution))
                    {
                        return true; // Found solution
                    }
                }

                return false; // No solution found
            }

            FindSolutionRecursive(requiredCosts, validCards, new HashSet<Entity>(), new List<Entity>());
            return solution ?? new List<Entity>();
        }

        /// <summary>
        /// Ensures the LastPaymentCache entity exists and returns it.
        /// </summary>
        private LastPaymentCache EnsurePaymentCacheExists()
        {
            var e = EntityManager.GetEntitiesWithComponent<LastPaymentCache>().FirstOrDefault();
            if (e == null)
            {
                e = EntityManager.CreateEntity("LastPaymentCache");
                EntityManager.AddComponent(e, new LastPaymentCache());
            }
            return e.GetComponent<LastPaymentCache>();
        }

        private void OnPlayCardRequested(PlayCardRequested evt)
        {
            if (evt?.Card == null) return;

            // Only in Action phase
            var phase = EntityManager.GetEntitiesWithComponent<PhaseState>().FirstOrDefault().GetComponent<PhaseState>();
            if (phase.Sub != SubPhase.Action) { Console.WriteLine($"[CardPlaySystem] Ignored play, phase={phase}"); return; }

            var data = evt.Card.GetComponent<CardData>();
            if (data == null) return;

            // Use CardId directly for lookup
            string id = data.Card.CardId ?? string.Empty;
            if (string.IsNullOrEmpty(id)) return;
            var card = CardFactory.Create(id);
            if (card == null) return;


            if (data.Card.Type == CardType.Relic)
            {
                EventManager.Publish(new CantPlayCardMessage { Message = "Relics can only be discarded to pay for costs!" });
                return;
            }

            // Weapons can only be played during Action phase (already gated) and cannot be used to pay costs of other cards
            bool isWeapon = card.IsWeapon;

            // Gate by Action Points unless the card is a free action
            var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
            var ap = player?.GetComponent<ActionPoints>();
            bool isFree = card.IsFreeAction;
            if (!isFree)
            {
                int currentAp = ap?.Current ?? 0;
                if (currentAp <= 0)
                {
                    EventManager.Publish(new CantPlayCardMessage { Message = "Not enough action points!" });
                    return; // cannot play without AP
                }
            }

            // Evaluate any additional costs/requirements tied to the card id
            if (card.CanPlay(EntityManager, evt.Card) == false)
            {
                Console.WriteLine($"[CardPlaySystem] Additional cost check failed for id={card.CardId}; aborting play");
                return;
            }

            // If costs are not yet paid, determine if payment is needed and either auto-resolve or open overlay
            if (!evt.CostsPaid)
            {
                var deckEntityForCost = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
                var deck = deckEntityForCost?.GetComponent<Deck>();
                var cardId = evt.Card.GetComponent<CardData>().Card.CardId;
                // gonna cloodge this in for now
                if (card.SpecialAction == "SelectOneCardFromHand")
                {
                    EventManager.Publish(new OpenPayCostOverlayEvent { CardToPlay = evt.Card, RequiredCosts = ["Any"], Type = PayCostOverlayType.SelectOneCard });
                    return;
                }
                var requiredCosts = card.Cost.ToList();
                if (requiredCosts.Count > 0 && deck != null)
                {
                    // Build hand color multiset excluding the card being played
                    var handOthers = deck.Hand.Where(c => c != evt.Card).ToList();
                    // Exclude weapons from being considered as payment candidates
                    List<Entity> handNonWeapons = new List<Entity>();
                    foreach (var e in handOthers)
                    {
                        var cdOther = e.GetComponent<CardData>();
                        if (cdOther == null)
                        {
                            handNonWeapons.Add(e);
                            continue;
                        }
                        try
                        {
                            string oid = cdOther.Card.CardId ?? string.Empty;
                            var ocard = CardFactory.Create(oid);
                            if (ocard != null)
                            {
                                if (!ocard.IsWeapon)
                                {
                                    handNonWeapons.Add(e);
                                }
                            }
                            else
                            {
                                // If no definition found, treat as non-weapon to avoid false negatives
                                handNonWeapons.Add(e);
                            }
                        }
                        catch
                        {
                            handNonWeapons.Add(e);
                        }
                    }

                    // Exclude Yellow cards - they cannot be discarded/used to pay costs
                    handNonWeapons = handNonWeapons.Where(e => e.GetComponent<CardData>()?.Color != CardData.CardColor.Yellow).ToList();

                    // Exclude pledged cards - they cannot be used to pay costs
                    handNonWeapons = handNonWeapons.Where(e => e.GetComponent<Pledge>() == null).ToList();

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

                    bool canSatisfy = CanSatisfy(requiredCosts, handNonWeapons, out _);

                    if (!canSatisfy)
                    {
                        Console.WriteLine("[CardPlaySystem] Cannot satisfy cost requirements; aborting play");
                        EventManager.Publish(new CantPlayCardMessage { Message = "Can't pay card's cost!" });
                        return;
                    }

                    // Count distinct ways to satisfy the cost (order-independent)
                    int solutionCount = CountDistinctSolutions(requiredCosts, handNonWeapons);

                    if (solutionCount == 0)
                    {
                        Console.WriteLine("[CardPlaySystem] Cannot satisfy cost requirements; aborting play");
                        EventManager.Publish(new CantPlayCardMessage { Message = "Can't pay card's cost!" });
                        return;
                    }
                    else if (solutionCount == 1)
                    {
                        // Exactly one way to satisfy cost - auto-pay
                        var solution = FindFirstSolution(requiredCosts, handNonWeapons);
                        Console.WriteLine($"[CardPlaySystem] Auto-paying cost with {solution.Count} card(s)");
                        foreach (var c in solution)
                        {
                            EventManager.Publish(new CardMoveRequested { Card = c, Deck = deckEntityForCost, Destination = CardZoneType.DiscardPile, Reason = "AutoPayCost" });
                            var cardData = c.GetComponent<CardData>();
                            if (cardData != null && cardData.Card.OnDiscardedForCost != null)
                            {
                                cardData.Card.OnDiscardedForCost(EntityManager, c);
                            }
                            // Award mastery points for Relic cards discarded for cost
                            if (cardData != null && cardData.Card.Type == CardType.Relic)
                            {
                                SaveCache.AddMasteryPoints(cardData.Card.CardId, 1);
                            }
                        }
                        
                        // Populate payment cache so card effects can reference what was paid
                        var cache = EnsurePaymentCacheExists();
                        cache.CardPlayed = evt.Card;
                        cache.PaymentCards = new List<Entity>(solution);
                        cache.HasData = true;
                        
                        EventManager.Publish(new PlayCardRequested { Card = evt.Card, CostsPaid = true });
                        return;
                    }
                    else
                    {
                        // Multiple ways to satisfy cost - show overlay for player choice
                        Console.WriteLine($"[CardPlaySystem] {solutionCount} distinct ways to satisfy cost; opening pay-cost overlay");
                        EventManager.Publish(new OpenPayCostOverlayEvent { CardToPlay = evt.Card, RequiredCosts = requiredCosts, Type = PayCostOverlayType.ColorDiscard });
                        return;
                    }
                }
            }

            // If this card has an attack animation, enqueue a player attack animation sequence that will play serially
            if (string.Equals(card.Animation, "Attack", StringComparison.OrdinalIgnoreCase))
            {
                EventQueue.EnqueueRule(new QueuedStartPlayerAttackAnimation());
                EventQueue.EnqueueRule(new QueuedWaitPlayerImpactEvent());
            }
            else if (string.Equals(card.Animation, "Buff", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"[CardPlaySystem] Buff");
                EventQueue.EnqueueRule(new QueuedStartBuffAnimation(true));
                EventQueue.EnqueueRule(new QueuedWaitBuffComplete(true));
            }

            card.OnPlay?.Invoke(EntityManager, evt.Card);
            EventManager.Publish(new TrackingEvent { Type = card.CardId, Delta = 1 });

            // Award mastery points for Attack and Prayer cards on play
            if (card.Type == CardType.Attack || card.Type == CardType.Prayer)
            {
                SaveCache.AddMasteryPoints(card.CardId, 1);
            }

            // Move the played card to discard unless it's a weapon (weapons leave hand but do not go to discard)
            var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
            var destination = CardZoneType.DiscardPile;

            // Consume 1 AP if not a free action
            if (!isFree)
            {
                EventManager.Publish(new ModifyActionPointsEvent { Delta = -1 });
                Console.WriteLine("[CardPlaySystem] Consumed 1 AP");
            }

            if (deckEntity != null)
            {
                if (isWeapon)
                {
                    // Remove from hand without adding to discard/exhaust; stays out until re-added by phase rules
                    // CardZoneSystem will remove from lists when destination not specified; emulate by not re-adding
                    var deck = deckEntity.GetComponent<Deck>();
                    deck?.Hand.Remove(evt.Card);
                    Console.WriteLine("[CardPlaySystem] Weapon used; removed from hand without discarding");
                    EntityManager.DestroyEntity(evt.Card.Id);
                    return;
                }
                else {
                    if (evt.Card.GetComponent<MarkedForReturnToDeck>() != null)
                    {
                        destination = CardZoneType.DrawPile;
                        EventManager.Publish(new DeckShuffleEvent { Deck = deckEntity });
                        Console.WriteLine("[CardPlaySystem] Card returned to deck");
                        EntityManager.RemoveComponent<MarkedForReturnToDeck>(evt.Card);
                    }
                    if (evt.Card.GetComponent<MarkedForExhaust>() != null)
                    {
                        destination = CardZoneType.ExhaustPile;
                        Console.WriteLine("[CardPlaySystem] Card exhausted");
                        EntityManager.RemoveComponent<MarkedForExhaust>(evt.Card);
                    }
                    if (evt.Card.GetComponent<Frozen>() != null)
                    {
                        EventManager.Publish(new ApplyPassiveEvent { Target = player, Type = AppliedPassiveType.Frostbite, Delta = 1 });
                        destination = Random.Shared.Next(0, 100) < 50 ? CardZoneType.ExhaustPile : destination;
                        Console.WriteLine($"[CardPlaySystem] Card frozen; moving to {destination}");
                    }
                }
                EventManager.Publish(new CardMoveRequested { Card = evt.Card, Deck = deckEntity, Destination = destination, Reason = "PlayCard" });
            }
        }

        private void OnPayCostSatisfied(PayCostSatisfied evt)
        {
            if (evt?.CardToPlay == null) return;
            // Once costs are paid, proceed to resolve effect by re-publishing play with CostsPaid
            EventManager.Publish(new PlayCardRequested { Card = evt.CardToPlay, CostsPaid = true, PaymentCards = evt.PaymentCards });
        }

        
    }
}


