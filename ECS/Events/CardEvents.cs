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
    /// Event fired when entering the Action phase to initialize action points for the player.
    /// Carry additional data in the future if needed.
    /// </summary>
    public class StartPlayerTurn
    {
        public int StartingActionPoints { get; set; } = 1;
    }
} 