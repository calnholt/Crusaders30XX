using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Crusaders30XX.ECS.Components
{
    /// <summary>
    /// Component that holds the basic data for a card
    /// </summary>
    public class CardData : IComponent
    {
        public Entity Owner { get; set; }
        
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public int Cost { get; set; } = 0;
        public CardType Type { get; set; } = CardType.Attack;
        public CardRarity Rarity { get; set; } = CardRarity.Common;
        public string ImagePath { get; set; } = "";
        
        // New properties for the card system
        public CardColor Color { get; set; } = CardColor.White;
        public CostType CardCostType { get; set; } = CostType.NoCost;
        public int BlockValue { get; set; } = 0;
        
        public enum CardType
        {
            Attack,
            Skill,
            Power,
            Curse,
            Status
        }
        
        public enum CardRarity
        {
            Common,
            Uncommon,
            Rare,
            Legendary
        }
        
        public enum CardColor
        {
            White,
            Red,
            Black
        }
        
        public enum CostType
        {
            NoCost,
            Red,
            White,
            Black
        }
    }

    /// <summary>
    /// Component for entities that represent a deck
    /// </summary>
    public class Deck : IComponent
    {
        public Entity Owner { get; set; }
        
        public List<Entity> Cards { get; set; } = new();
        public List<Entity> DrawPile { get; set; } = new();
        public List<Entity> DiscardPile { get; set; } = new();
        public List<Entity> ExhaustPile { get; set; } = new();
        public List<Entity> Hand { get; set; } = new();
    }
    
    
    /// <summary>
    /// Component for the player entity
    /// </summary>
    public class Player : IComponent
    {
        public Entity Owner { get; set; }
        
        public int MaxHealth { get; set; } = 100;
        public int CurrentHealth { get; set; } = 100;
        public int MaxEnergy { get; set; } = 3;
        public int Block { get; set; } = 0;
        
        public Entity DeckEntity { get; set; }
        public Entity HandEntity { get; set; }
    }
    
    /// <summary>
    /// Component for enemies
    /// </summary>
    public class Enemy : IComponent
    {
        public Entity Owner { get; set; }
        
        public EnemyType Type { get; set; } = EnemyType.Demon;
        public string Id { get; set; } = "demon";

        public string Name { get; set; } = "";
        public int MaxHealth { get; set; } = 50;
        public int CurrentHealth { get; set; } = 50;
        public int Block { get; set; } = 0;
    }

    public enum EnemyType
    {
        Demon
    }
    
    /// <summary>
    /// Optional resource component representing Courage for an entity (e.g., the player)
    /// </summary>
    public class Courage : IComponent
    {
        public Entity Owner { get; set; }
        
        public int Amount { get; set; } = 0;
    }

    /// <summary>
    /// Per-turn resource representing the player's available action points during the Action phase.
    /// </summary>
    public class ActionPoints : IComponent
    {
        public Entity Owner { get; set; }

        public int Current { get; set; } = 0;
    }
    
    /// <summary>
    /// Optional resource component representing Temperance for an entity (e.g., the player)
    /// </summary>
    public class Temperance : IComponent
    {
        public Entity Owner { get; set; }
        
        public int Amount { get; set; } = 0;
    }

    /// <summary>
    /// Resource component representing Stored Block for an entity (e.g., the player)
    /// </summary>
    public class StoredBlock : IComponent
    {
        public Entity Owner { get; set; }

        public int Amount { get; set; } = 0;
    }

    /// <summary>
    /// Player stat for how many cards to draw at start of Block phase.
    /// </summary>
    public class Intellect : IComponent
    {
        public Entity Owner { get; set; }
        public int Value { get; set; } = 0;
    }

    /// <summary>
    /// Player stat for maximum cards allowed in hand.
    /// A separate component allows UI and effects to modify it independently
    /// from the Deck's internal limit; a sync system keeps Deck.MaxHandSize aligned.
    /// </summary>
    public class MaxHandSize : IComponent
    {
        public Entity Owner { get; set; }
        public int Value { get; set; } = 5;
    }
    
    /// <summary>
    /// Component for positioning and rendering
    /// </summary>
    public class Transform : IComponent
    {
        public Entity Owner { get; set; }
        
        public Vector2 Position { get; set; } = Vector2.Zero;
        public float Rotation { get; set; } = 0f;
        public Vector2 Scale { get; set; } = Vector2.One;
        public int ZOrder { get; set; } = 0;
    }
    
    /// <summary>
    /// Component for rendering sprites
    /// </summary>
    public class Sprite : IComponent
    {
        public Entity Owner { get; set; }
        
        public string TexturePath { get; set; } = "";
        public Rectangle? SourceRectangle { get; set; } = null;
        public Color Tint { get; set; } = Color.White;
        public bool IsVisible { get; set; } = true;
    }
    
    /// <summary>
    /// Component for UI elements
    /// </summary>
    public class UIElement : IComponent
    {
        public Entity Owner { get; set; }
        
        public Rectangle Bounds { get; set; }
        public bool IsHovered { get; set; } = false;
        public bool IsClicked { get; set; } = false;
        public bool IsInteractable { get; set; } = true;
        public string Tooltip { get; set; } = "";
        public TooltipPosition TooltipPosition { get; set; } = TooltipPosition.Above;
    }
    
    public enum TooltipPosition
    {
        Above,
        Below,
        Right,
        Left
    }
    
    /// <summary>
    /// Marker for the courage hover area used to show a tooltip.
    /// </summary>
    public class CourageTooltipAnchor : IComponent
    {
        public Entity Owner { get; set; }
    }
    
    /// <summary>
    /// Marker for the temperance hover area used to show a tooltip.
    /// </summary>
    public class TemperanceTooltipAnchor : IComponent
    {
        public Entity Owner { get; set; }
    }
    
    /// <summary>
    /// Marker for the stored block hover area used to show a tooltip.
    /// </summary>
    public class StoredBlockTooltipAnchor : IComponent
    {
        public Entity Owner { get; set; }
    }
    
    /// <summary>
    /// Component for animation
    /// </summary>
    public class Animation : IComponent
    {
        public Entity Owner { get; set; }
        
        public float Duration { get; set; } = 1.0f;
        public float CurrentTime { get; set; } = 0.0f;
        public bool IsPlaying { get; set; } = false;
        public bool IsLooping { get; set; } = false;
        public AnimationType Type { get; set; } = AnimationType.Fade;
        
        public enum AnimationType
        {
            Fade,
            Scale,
            Move,
            Rotate
        }
    }


    /// <summary>
    /// Shared portrait metadata for any entity drawn as a character/portrait.
    /// Systems set TextureWidth/Height once the texture is known and keep CurrentScale updated.
    /// </summary>
    public class PortraitInfo : IComponent
    {
        public Entity Owner { get; set; }
        public int TextureWidth { get; set; }
        public int TextureHeight { get; set; }
        public float CurrentScale { get; set; } = 1f; // actual draw scale this frame (may breathe)
        public float BaseScale { get; set; } = 1f;    // stable baseline scale for layout (no breathing)
    }

    /// <summary>
    /// State for the on-screen profiler overlay
    /// </summary>
    public class ProfilerOverlay : IComponent
    {
        public Entity Owner { get; set; }
        public bool IsOpen { get; set; } = false;
    }

    /// <summary>
    /// Marker component for the clickable draw pile UI area
    /// </summary>
    public class DrawPileClickable : IComponent
    {
        public Entity Owner { get; set; }
    }

    /// <summary>
    /// Marker component for the clickable discard pile UI area
    /// </summary>
    public class DiscardPileClickable : IComponent
    {
        public Entity Owner { get; set; }
    }

    /// <summary>
    /// Component representing a generic card list modal
    /// </summary>
    public class CardListModal : IComponent
    {
        public Entity Owner { get; set; }
        public bool IsOpen { get; set; } = false;
        public string Title { get; set; } = "";
        public List<Entity> Cards { get; set; } = new();
    }

    /// <summary>
    /// Marker for the modal close button (X) of the card list modal
    /// </summary>
    public class CardListModalClose : IComponent
    {
        public Entity Owner { get; set; }
    }

    /// <summary>
    /// Component for a simple in-game debug menu state
    /// </summary>
    public class DebugMenu : IComponent
    {
        public Entity Owner { get; set; }
        public bool IsOpen { get; set; } = false;
        public int ActiveTabIndex { get; set; } = 0;
        // Persisted panel position and init flag for draggable debug menu
        public int PanelX { get; set; } = 0;
        public int PanelY { get; set; } = 0;
        public bool IsPositionSet { get; set; } = false;
    }

    /// <summary>
    /// Component for a generic UI button
    /// </summary>
    public class UIButton : IComponent
    {
        public Entity Owner { get; set; }
        public string Label { get; set; } = "";
        public string Command { get; set; } = ""; // e.g., "DrawCard"
    }
    
    /// <summary>
    /// Component for a generic dropdown UI control used by the debug menu
    /// </summary>
    public class UIDropdown : IComponent
    {
        public Entity Owner { get; set; }
        public List<string> Items { get; set; } = new();
        public int SelectedIndex { get; set; } = 0;
        public bool IsOpen { get; set; } = false;
        public int RowHeight { get; set; } = 28;
        public float TextScale { get; set; } = 0.55f;
    }
    
    /// <summary>
    /// Component for game state management
    /// </summary>
    public class GameState : IComponent
    {
        public Entity Owner { get; set; }
        
        public GamePhase CurrentPhase { get; set; } = GamePhase.MainMenu;
        public int TurnNumber { get; set; } = 1;
        public bool IsPlayerTurn { get; set; } = true;
        public bool IsGameOver { get; set; } = false;
        
        public enum GamePhase
        {
            MainMenu,
            Map,
            Combat,
            Shop,
            Event,
            Victory,
            Defeat
        }
    }

    /// <summary>
    /// Singleton-like world component describing the current battlefield location.
    /// Other systems read this instead of subscribing to an event.
    /// </summary>
    public class Battlefield : IComponent
    {
        public Entity Owner { get; set; }
        public BattleLocation Location { get; set; } = BattleLocation.Desert;
    }

    /// <summary>
    /// Enumerates the high-level phases of a battle.
    /// </summary>
    public enum BattlePhase
    {
        StartOfBattle,
        Block,
        Action,
        ProcessEnemyAttack
    }

    /// <summary>
    /// Singleton-like world component that tracks the current battle phase.
    /// </summary>
    public class BattlePhaseState : IComponent
    {
        public Entity Owner { get; set; }
        public BattlePhase Phase { get; set; } = BattlePhase.StartOfBattle;
    }

    /// <summary>
    /// Tracks battle metadata such as the current enemy turn number.
    /// TurnNumber starts at 0 and increments when transitioning to Block for a new enemy turn.
    /// </summary>
    public class BattleInfo : IComponent
    {
        public Entity Owner { get; set; }
        public int TurnNumber { get; set; } = 0;
    }

    /// <summary>
    /// Generic hit points component
    /// </summary>
    public class HP : IComponent
    {
        public Entity Owner { get; set; }
        public int Max { get; set; } = 100;
        public int Current { get; set; } = 100;
    }

    /// <summary>
    /// Shared visual settings for cards; systems read/write this singleton component to stay in sync.
    /// </summary>
    public class CardVisualSettings : IComponent
    {
        public Entity Owner { get; set; }
        public float UIScale { get; set; }
        public int CardWidth { get; set; }
        public int CardHeight { get; set; }
        public int CardOffsetYExtra { get; set; }
        public int CardGap { get; set; }
        public int CardBorderThickness { get; set; }
        public int CardCornerRadius { get; set; }
        public int HighlightBorderThickness { get; set; }
        public int TextMarginX { get; set; }
        public int TextMarginY { get; set; }
        public float NameScale { get; set; }
        public float CostScale { get; set; }
        public float DescriptionScale { get; set; }
        public float BlockScale { get; set; }
        public float BlockNumberScale { get; set; }
        public int BlockNumberMarginX { get; set; }
        public int BlockNumberMarginY { get; set; }
    }

    /// <summary>
    /// Enumerates the zone a card currently belongs to. Useful for transitioning toward a fully component-driven zone model.
    /// </summary>
    public enum CardZoneType
    {
        DrawPile,
        Hand,
        DiscardPile,
        ExhaustPile,
        AssignedBlock
    }

} 

// New component used to animate assigned block cards flying to the discard pile
namespace Crusaders30XX.ECS.Components
{
    public class CardToDiscardFlight : IComponent
    {
        public Entity Owner { get; set; }
        public Microsoft.Xna.Framework.Vector2 StartPos { get; set; }
        public Microsoft.Xna.Framework.Vector2 TargetPos { get; set; }
        public float StartDelaySeconds { get; set; }
        public float DurationSeconds { get; set; }
        public float ArcHeightPx { get; set; }
        public float ElapsedSeconds { get; set; }
        public bool Started { get; set; }
        public float StartScale { get; set; } = 0.35f;
        public float EndScale { get; set; } = 0.3f;
        public string ContextId { get; set; }
        public bool Completed { get; set; }
    }
}