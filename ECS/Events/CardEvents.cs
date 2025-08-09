using Crusaders30XX.ECS.Core;
using Microsoft.Xna.Framework;

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
    /// Event to request discarding the hand, shuffling, and drawing a fresh hand
    /// </summary>
    public class RedrawHandEvent
    {
        public int DrawCount { get; set; } = 4;
    }

    public class OpenDrawPileModalEvent { }
    public class CloseDrawPileModalEvent { }
    
    /// <summary>
    /// Event published when deck shuffling and drawing is requested
    /// </summary>
    public class DeckShuffleDrawEvent
    {
        public Entity Deck { get; set; }
        public int DrawCount { get; set; }
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
} 