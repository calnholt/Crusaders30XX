using Crusaders30XX.ECS.Core;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Crusaders30XX.ECS.Components;

namespace Crusaders30XX.ECS.Events
{
    /// <summary>
    /// Event published when a card should be rendered
    /// </summary>
    public class CardRenderEvent
    {
        public Entity Card { get; set; }
        public Vector2 Position { get; set; }
        public bool IsInHand { get; set; }
    }

    /// <summary>
    /// Event published to render a card at a position with a uniform scale factor
    /// </summary>
    public class CardRenderScaledEvent
    {
        public Entity Card { get; set; }
        public Vector2 Position { get; set; }
        public float Scale { get; set; } = 1f;
    }

    /// <summary>
    /// Event to render a card at a position with a uniform scale factor, preserving the card's current rotation.
    /// </summary>
    public class CardRenderScaledRotatedEvent
    {
        public Entity Card { get; set; }
        public Vector2 Position { get; set; }
        public float Scale { get; set; } = 1f;
    }

    /// <summary>
    /// Event published just before rendering a specific card to allow a highlight to draw beneath it
    /// </summary>
    public class CardHighlightRenderEvent
    {
        public Entity Card { get; set; }
    }
    
    /// <summary>
    /// Event published when cards in hand need to be positioned
    /// </summary>
    public class HandLayoutEvent
    {
        public System.Collections.Generic.List<Entity> CardsInHand { get; set; }
        public float ScreenWidth { get; set; }
        public float ScreenHeight { get; set; }
    }

    /// <summary>
    /// Event published when a debug UI button is clicked
    /// </summary>
    public class DebugCommandEvent
    {
        public string Command { get; set; }
    }

    /// <summary>
    /// Increases or decreases the player's Courage by Delta
    /// </summary>
    public class ModifyCourageEvent
    {
        public int Delta { get; set; } = 0;
    }

    /// <summary>
    /// Event to request discarding the hand, shuffling, and drawing a fresh hand
    /// </summary>
    public class RedrawHandEvent
    {
        public int DrawCount { get; set; } = 4;
    }

    public class OpenCardListModalEvent
    {
        public string Title { get; set; }
        public List<Entity> Cards { get; set; }
    }

    public class CloseCardListModalEvent { }
    
    /// <summary>
    /// Event published when deck shuffling and drawing is requested
    /// </summary>
    public class DeckShuffleDrawEvent
    {
        public Entity Deck { get; set; }
        public int DrawCount { get; set; }
    }
        /// <summary>
    /// Event published when deck shuffling is requested
    /// </summary>
    public class DeckShuffleEvent
    {
        public Entity Deck { get; set; }
    }
    
    /// <summary>
    /// Event published when cards are drawn from deck
    /// </summary>
    public class CardsDrawnEvent
    {
        public Entity Deck { get; set; }
        public System.Collections.Generic.List<Entity> DrawnCards { get; set; }
    }

    /// <summary>
    /// Event to request drawing N cards from the current deck without reshuffling
    /// </summary>
    public class RequestDrawCardsEvent
    {
        public int Count { get; set; } = 1;
    }

    /// <summary>
    /// Event to request moving a card into a destination zone. A single authoritative system should handle this.
    /// </summary>
    public class CardMoveRequested
    {
        public Entity Card { get; set; }
        public Entity Deck { get; set; }
        public CardZoneType Destination { get; set; }
        public string ContextId { get; set; }
        public string Reason { get; set; }
    }

    /// <summary>
    /// Event published after a card has been moved between zones.
    /// </summary>
    public class CardMoved
    {
        public Entity Card { get; set; }
        public Entity Deck { get; set; }
        public CardZoneType From { get; set; }
        public CardZoneType To { get; set; }
        public string ContextId { get; set; }
    }

    /// <summary>
    /// Request to play a card during the Action phase.
    /// The handling system should validate phase/zone and resolve effects.
    /// </summary>
    public class PlayCardRequested
    {
        public Entity Card { get; set; }
        // When true, downstream systems should not prompt for resource/cost payment.
        public bool CostsPaid { get; set; } = false;
    }

    /// <summary>
    /// Increase or decrease the player's Action Points (AP) by Delta.
    /// Clamped at zero; no explicit max is enforced here.
    /// </summary>
    public class ModifyActionPointsEvent
    {
        public int Delta { get; set; } = 0;
    }

    /// <summary>
    /// Opens the pay-cost overlay for a specific card with required costs.
    /// </summary>
    public class OpenPayCostOverlayEvent
    {
        public Entity CardToPlay { get; set; }
        public System.Collections.Generic.List<string> RequiredCosts { get; set; } = new(); // values: "Red","White","Black","Any"
    }

    /// <summary>
    /// Closes the pay-cost overlay if open.
    /// </summary>
    public class ClosePayCostOverlayEvent { }

    /// <summary>
    /// Emitted by input when a card is clicked while the pay-cost overlay is open.
    /// </summary>
    public class PayCostCandidateClicked
    {
        public Entity Card { get; set; }
    }

    /// <summary>
    /// User requested to cancel paying costs and abort the play.
    /// </summary>
    public class PayCostCancelRequested { }

    /// <summary>
    /// Emitted when cost payment has been satisfied and the selected cards have been discarded.
    /// Re-dispatches play of the original card with CostsPaid=true.
    /// </summary>
    public class PayCostSatisfied
    {
        public Entity CardToPlay { get; set; }
        public System.Collections.Generic.List<Entity> PaymentCards { get; set; } = new();
    }

    /// <summary>
    /// UI message indicating why a card cannot be played.
    /// Systems can subscribe to display a transient on-screen notification.
    /// </summary>
    public class CantPlayCardMessage
    {
        public string Message { get; set; } = "";
    }

} 