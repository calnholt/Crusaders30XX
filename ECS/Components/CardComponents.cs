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
        
        public string CardId { get; set; } = ""; // id of CardDefinition
        
        // Instance-specific properties for the card entity
        public CardColor Color { get; set; } = CardColor.White;
        
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
            Black,
            Yellow
        }
        
        public enum CostType
        {
            NoCost,
            Red,
            White,
            Black,
            Any
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
        public int MaxHealth { get; set; } = 40;
        public int CurrentHealth { get; set; } = 40;
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
    /// Player's currently equipped Temperance ability (by id). At most one should be equipped.
    /// </summary>
    public class EquippedTemperanceAbility : IComponent
    {
        public Entity Owner { get; set; }
        public string AbilityId { get; set; } = "";
    }

    /// <summary>
    /// Player's currently equipped weapon by id (e.g., "hammer"). Weapon is not in the deck.
    /// </summary>
    public class EquippedWeapon : IComponent
    {
        public Entity Owner { get; set; }
        public string WeaponId { get; set; } = "";
        public Entity SpawnedEntity { get; set; }
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
	/// A single equipped item. Multiple instances may exist, each referencing the owning player.
	/// </summary>
	public class EquippedEquipment : IComponent
	{
		public Entity Owner { get; set; }
		public Entity EquippedOwner { get; set; } // player entity that owns this equipment
		public string EquipmentId { get; set; } = ""; // id of equipment definition
		public string EquipmentType { get; set; } = ""; // Head | Chest | Arms | Legs | etc
	}

	/// <summary>
	/// Per-battle state: counters and once-per-battle flags for triggers.
	/// </summary>
	public class BattleStateInfo : IComponent
	{
		public Entity Owner { get; set; }
		public HashSet<string> EquipmentTriggeredThisBattle { get; set; } = new();
        public Dictionary<TrackingTypeEnum, int> RunTracking { get; set; } = new();
        public Dictionary<TrackingTypeEnum, int> BattleTracking { get; set; } = new();
        public Dictionary<TrackingTypeEnum, int> TurnTracking { get; set; } = new();
        public Dictionary<TrackingTypeEnum, int> PhaseTracking { get; set; } = new();

	}

    public enum TrackingTypeEnum
    {
        CourageGained,
        CourageLost
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
        public int TooltipOffsetPx { get; set; } = 6; // gap from element to tooltip
    }

    /// <summary>
    /// Anchor published by HPDisplaySystem describing the last drawn HP bar rectangle for an entity.
    /// Used by other UI systems to align elements relative to the HP bar.
    /// </summary>
    public class HPBarAnchor : IComponent
    {
        public Entity Owner { get; set; }
        public Microsoft.Xna.Framework.Rectangle Rect { get; set; }
    }
    
    public enum TooltipPosition
    {
        Above,
        Below,
        Right,
        Left
    }

    /// <summary>
    /// Marks a card in hand that has been preselected to be discarded by an enemy effect
    /// for a specific attack context.
    /// </summary>
    public class MarkedForSpecificDiscard : IComponent
    {
        public Entity Owner { get; set; }
        public string ContextId { get; set; }
    }
    
    /// <summary>
    /// Marks a card as intimidated. Intimidated cards cannot be used to block during the block phase.
    /// </summary>
    public class Intimidated : IComponent
    {
        public Entity Owner { get; set; }
    }

    /// <summary>
    /// Marks a card as frozen. Frozen cards lose frozen when used to block. Frozen cards cannot be played during action phase
    /// </summary>
    public class Frozen : IComponent
    {
        public Entity Owner { get; set; }
    }

    /// <summary>
    /// Marks a card as having its block value modified.
    /// </summary>
    public class ModifiedBlock : IComponent
    {
        public Entity Owner { get; set; }
        public int Delta { get; set; } = 0;
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
        public int ScrollOffset { get; set; } = 0;
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
    /// State for a simple "How To Play" overlay.
    /// </summary>
    public class HowToPlayOverlay : IComponent
    {
        public Entity Owner { get; set; }
        public bool IsOpen { get; set; } = false;
        public string Text { get; set; } = "Each battle starts with the block phase. During this phase you can prevent damage and effects by assigning cards from you hand or equipment from your hero to the attack. When you block with white cards, you gain Temperance. When you block with red cards, you gain Courage. When you block with black cards, you gain no resource (higher block value instead). Above the enemy are little circles which represent the number of attacks they will make, and the ones below are for the enemy's next turn.\n\nAfter the enemy turn is your action phase. You have one Action Point (AP) per action phase that can be spent to play cards. Some cards require no AP. Some cards have a cost, denoted by the circles underneath their name. You must discard that many cards of the matching color from you hand to play these cards.\n\nYou have an equipped Sword, which you always have access to on your turn.\n\nAt the end of your turn, you will draw 4 cards, up to your maximum hand size of 5.";
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
    /// State for the pay-cost overlay flow.
    /// </summary>
    public class PayCostOverlayState : IComponent
    {
        public Entity Owner { get; set; }
        public bool IsOpen { get; set; } = false;
        public Entity CardToPlay { get; set; }
        public List<string> RequiredCosts { get; set; } = new(); // "Red","White","Black","Any"
        public List<Entity> SelectedCards { get; set; } = new();
        public float OpenElapsedSeconds { get; set; } = 0f;
        public int OriginalHandIndex { get; set; } = -1;
        // Tween state for staged card movement
        public Microsoft.Xna.Framework.Vector2 StagedStartPos { get; set; } = Microsoft.Xna.Framework.Vector2.Zero;
        public float StagedMoveElapsedSeconds { get; set; } = 0f;
        public bool IsReturning { get; set; } = false;
        public float ReturnElapsedSeconds { get; set; } = 0f;
        public float StagedStartRotation { get; set; } = 0f;
    }

    /// <summary>
    /// Marker for the cancel button inside the pay-cost overlay.
    /// </summary>
    public class PayCostCancelButton : IComponent
    {
        public Entity Owner { get; set; }
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


    // New phase model with main/sub-phases
    public enum MainPhase
    {
        StartBattle,
        EnemyTurn,
        PlayerTurn
    }

    public enum SubPhase
    {
        StartBattle,
        // Enemy sub-phases
        EnemyStart,
        PreBlock,
        Block,
        EnemyAttack,
        EnemyEnd,
        // Player sub-phases
        PlayerStart,
        Action,
        PlayerEnd
    }

    public class PhaseState : IComponent
    {
        public Entity Owner { get; set; }
        public MainPhase Main { get; set; } = MainPhase.StartBattle;
        public SubPhase Sub { get; set; } = SubPhase.StartBattle;
        public int TurnNumber { get; set; } = 0; // enemy turn counter
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
        public int Max { get; set; } = 40;
        public int Current { get; set; } = 40;
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
    /// Shared highlight visual settings for equipment panel items.
    /// </summary>
    public class EquipmentHighlightSettings : IComponent
    {
        public Entity Owner { get; set; }
        public int GlowLayers { get; set; } = 40;
        public float GlowSpread { get; set; } = 0.006f;
        public float GlowSpreadSpeed { get; set; } = 2.5f;
        public float GlowSpreadAmplitude { get; set; } = 0.22f;
        public float MaxAlpha { get; set; } = 0.06f;
        public float GlowPulseSpeed { get; set; } = 2.0f;
        public float GlowEasingPower { get; set; } = 0.8f;
        public float GlowMinIntensity { get; set; } = 0.30f;
        public float GlowMaxIntensity { get; set; } = 0.8f;
        public int CornerRadius { get; set; } = 23;
        public int HighlightBorderThickness { get; set; } = 4;
        public int GlowColorR { get; set; } = 0;
        public int GlowColorG { get; set; } = 0;
        public int GlowColorB { get; set; } = 0;
    }

    /// <summary>
    /// Tracks how many times each equipment id has been used to block this battle.
    /// Attach to the player entity.
    /// </summary>
    public class EquipmentUsedState : IComponent
    {
        public Entity Owner { get; set; }
        public Dictionary<string, int> UsesByEquipmentId { get; set; } = new Dictionary<string, int>();
        public System.Collections.Generic.HashSet<string> DestroyedEquipmentIds { get; set; } = new System.Collections.Generic.HashSet<string>();
        public System.Collections.Generic.HashSet<string> ActivatedThisTurn { get; set; } = new System.Collections.Generic.HashSet<string>();
    }

    /// <summary>
    /// A single equipped medal. Multiple instances may exist, each referencing the owning player.
    /// </summary>
    public class EquippedMedal : IComponent
    {
        public Entity Owner { get; set; }
        public Entity EquippedOwner { get; set; } // player entity that owns this medal
        public string MedalId { get; set; } = ""; // id of medal definition
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
    /// <summary>
    /// Zone state for an equipment entity: either visible in the left panel (Default)
    /// or currently assigned as block (hidden in panel, shown via AssignedBlockCardsDisplaySystem).
    /// </summary>
    public class EquipmentZone : IComponent
    {
        public Entity Owner { get; set; }
        public EquipmentZoneType Zone { get; set; } = EquipmentZoneType.Default;
        public Microsoft.Xna.Framework.Vector2 LastPanelCenter { get; set; } = Microsoft.Xna.Framework.Vector2.Zero;
    }

    public enum EquipmentZoneType
    {
        Default,
        AssignedBlock
    }

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

    public class AppliedPassives : IComponent
    {
        public Entity Owner { get; set; }
        public Dictionary<AppliedPassiveType, int> Passives { get; set; } = new Dictionary<AppliedPassiveType, int>();
    }

    public enum AppliedPassiveType
    {
        Burn,
        Power,
        DowseWithHolyWater,
        Slow,
        Aegis,
        Stun,
        Armor,
        Wounded,
        Webbing,
        Inferno,
        Penance
    }
}