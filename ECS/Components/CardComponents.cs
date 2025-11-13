using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Data.Locations;
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
        public Dictionary<string, int> RunTracking { get; set; } = new();
        public Dictionary<string, int> BattleTracking { get; set; } = new();
        public Dictionary<string, int> TurnTracking { get; set; } = new();
        public Dictionary<string, int> PhaseTracking { get; set; } = new();

	}

    public enum TrackingTypeEnum
    {
        CourageGained,
        CourageLost,
        NumberOfAttacksHitPlayer,
    }
    
    /// <summary>
    /// Component for positioning and rendering
    /// </summary>
    public class Transform : IComponent
    {
        public Entity Owner { get; set; }
        
        public Vector2 Position { get; set; } = Vector2.Zero;
        public Vector2 BasePosition { get; set; } = Vector2.Zero;
        public float Rotation { get; set; } = 0f;
        public Vector2 Scale { get; set; } = Vector2.One;
        public int ZOrder { get; set; } = 0;
    }
    
    /// <summary>
    /// Parallax configuration for UI/scene entities that should subtly move opposite the cursor.
    /// Movement is computed from the cursor's absolute offset from the screen center.
    /// </summary>
    public class ParallaxLayer : IComponent
    {
        public Entity Owner { get; set; }
        
        // Per-axis multipliers applied to the absolute-from-center cursor delta
        public float MultiplierX { get; set; } = 0.03f;
        public float MultiplierY { get; set; } = 0.03f;
        
        // Maximum magnitude (in pixels) the parallax offset is allowed to reach
        public float MaxOffset { get; set; } = 48f;
        
        // Time constant (seconds) for exponential smoothing toward the target position
        public float SmoothTime { get; set; } = 0.08f;
        
        // When true, the first update will capture the entity's current Transform.Position as BasePosition
        public bool CaptureBaseOnFirstUpdate { get; set; } = true;

        // If true, treat the entity's externally-driven position as the base each frame
        // (e.g., hand layout positions). This avoids fighting other layout systems.
        public bool UpdateBaseFromCurrentEachFrame { get; set; } = true;

        // Internally tracked last applied offset so we can reconstruct the external base
        public Microsoft.Xna.Framework.Vector2 LastAppliedOffset { get; set; } = Vector2.Zero;

        // Last position written by the parallax system; used to detect external layout overrides
        public Microsoft.Xna.Framework.Vector2 LastAppliedPosition { get; set; } = Vector2.Zero;

        // When true, ParallaxLayerSystem will nudge UIElement.Bounds alongside Transform.Position.
        // Disable for UI where bounds are derived from Transform.Position elsewhere each frame.
        public bool AffectsUIBounds { get; set; } = true;

        public static ParallaxLayer GetLocationParallaxLayer()
        {
            return new ParallaxLayer
            {
                MultiplierX = 0.01f,
                MultiplierY = 0.01f,
                MaxOffset = 12f,
                SmoothTime = 0.01f,
                CaptureBaseOnFirstUpdate = false,
                UpdateBaseFromCurrentEachFrame = false,
                AffectsUIBounds = true
            };
        }
        public static ParallaxLayer GetUIParallaxLayer()
        {
            return new ParallaxLayer
            {
                MultiplierX = 0.025f,
                MultiplierY = 0.025f,
                MaxOffset = 48f,
                SmoothTime = 0.08f,
                CaptureBaseOnFirstUpdate = false,
                UpdateBaseFromCurrentEachFrame = false,
                AffectsUIBounds = false
            };
        }

        public static ParallaxLayer GetCharacterParallaxLayer()
        {
            return new ParallaxLayer 
            { 
                MultiplierX = 0.01f, 
                MultiplierY = 0.01f, 
                MaxOffset = 48f, 
                SmoothTime = 0.08f, 
                CaptureBaseOnFirstUpdate = false, 
                UpdateBaseFromCurrentEachFrame = false, 
                AffectsUIBounds = false 
            };
        }
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
        public bool IsInteractable { get; set; } = false;
        public string Tooltip { get; set; } = "";
        public TooltipType TooltipType { get; set; } = TooltipType.Text;
        public TooltipPosition TooltipPosition { get; set; } = TooltipPosition.Above;
        public int TooltipOffsetPx { get; set; } = 6; // gap from element to tooltip
        public UIElementEventType EventType { get; set; } = UIElementEventType.None;
        public UILayerType LayerType { get; set; } = UILayerType.Default;
        public bool IsPreventDefaultClick { get; set; } = false;
        public bool IsHidden { get; set; } = false;
    }

    /// <summary>
    /// Optional hint text for a hovered UI entity. Displayed via HintTooltipDisplaySystem
    /// when the player requests help (e.g., Left Stick click on gamepad).
    /// </summary>
    public class Hint : IComponent
    {
        public Entity Owner { get; set; }
        public string Text { get; set; } = "";
    }

    public enum UILayerType
    {
        Default,
        Overlay,
    }

    public enum TooltipType
    {
        None,
        Quests,
        Card,
        Text,
    }

    public enum UIElementEventType
    {
        None,
        UnassignCardAsBlock,
        AssignCardAsBlock,
        UnassignEquipmentAsBlock,
        AssignEquipmentAsBlock,
        CardListModalClose,
        ActivateEquipment,
        EndTurn,
        ConfirmBlocks,
        PlayCardRequested,
        SelectedCardForCost,
        LocationSelect,
        QuestSelect,
        GoToCustomize,
        NextQuest,
        PreviousQuest,
        CardCustomizationTab,
        HeadCustomizationTab,
        ChestCustomizationTab,
        ArmsCustomizationTab,
        LegsCustomizationTab,
        TemperanceCustomizationTab,
        MedalsCustomizationTab,
        WeaponCustomizationTab,
        ExitCustomization,
        SaveCustomization,
        CancelCustomization,
        UndoCustomization,
        ChangeEquipment,
        AddCardCustomization,
        RemoveCardCustomization,
        ViewDiscard,
        ViewDeck,
			AbandonQuest,
        PayCostCancel,
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
    /// When attached to a hovered UI element, renders the specified card as a tooltip.
    /// </summary>
    public class CardTooltip : IComponent
    {
        public Entity Owner { get; set; }
        public string CardId { get; set; } = "";
        public float TooltipScale { get; set; } = 0.6f;
    }

    /// <summary>
    /// Backups original tooltip configuration so temporary overrides can be restored later.
    /// </summary>
    public class TooltipOverrideBackup : IComponent
    {
        public Entity Owner { get; set; }
        public TooltipType OriginalType { get; set; } = TooltipType.Text;
        public TooltipPosition OriginalPosition { get; set; } = TooltipPosition.Above;
        public int OriginalOffsetPx { get; set; } = 30;
        public bool HadCardTooltip { get; set; } = false;
        public string OriginalCardTooltipId { get; set; } = "";
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
    /// Component for quest tooltip data and fade state.
    /// </summary>
    public class QuestTooltip : IComponent
    {
        public Entity Owner { get; set; }
        
        public string LocationId { get; set; }
        public string Title { get; set; }
	public List<LocationEventDefinition> Events { get; set; }
	public List<TribulationDefinition> Tribulations { get; set; }
		public int RewardGold { get; set; } = 0;
		public bool IsCompleted { get; set; } = false;
	public string PoiType { get; set; } = "Quest";
	public float Alpha01 { get; set; } = 0f;
	public bool TargetVisible { get; set; } = false;
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

    public enum FaceButton
    {
        B,
        X,
        Y,
        Back,         
        Start
    }

    public enum HotKeyPosition
    {
        Top,
        Right,
        Left,
        Below
    }

    /// <summary>
    /// Declares a controller hotkey binding for a UI element. The system will draw
    /// a face-button hint next to the element and trigger its action when pressed.
    /// </summary>
    public class HotKey : IComponent
    {
        public Entity Owner { get; set; }
        public FaceButton Button { get; set; } = FaceButton.Y;
        public bool RequiresHold { get; set; } = false;
        public float HoldDurationSeconds { get; set; } = 1.0f;
        public Entity ParentEntity { get; set; }
        public HotKeyPosition Position { get; set; } = HotKeyPosition.Below;
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
        public System.Collections.Generic.Dictionary<int, string> ConsumedCostByCardId { get; set; } = new();
        public float OpenElapsedSeconds { get; set; } = 0f;
        public int OriginalHandIndex { get; set; } = -1;
        // Tween state for staged card movement
        public Microsoft.Xna.Framework.Vector2 StagedStartPos { get; set; } = Vector2.Zero;
        public float StagedMoveElapsedSeconds { get; set; } = 0f;
        public bool IsReturning { get; set; } = false;
        public float ReturnElapsedSeconds { get; set; } = 0f;
        public float StagedStartRotation { get; set; } = 0f;
        public PayCostOverlayType Type { get; set; } = PayCostOverlayType.ColorDiscard;
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
        HandStaged,
        CostSelected,
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
        public Microsoft.Xna.Framework.Vector2 LastPanelCenter { get; set; } = Vector2.Zero;
    }

    public enum EquipmentZoneType
    {
        Default,
        AssignedBlock
    }

    /// <summary>
    /// Marker for a card currently animating from Hand to Discard due to being played.
    /// Hand layout/draw systems should ignore cards with this component until finalize.
    /// </summary>
    public class AnimatingHandToDiscard : IComponent
    {
        public Entity Owner { get; set; }
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

    /// <summary>
    /// Marks a card that has been selected to pay costs; holds tween state and original hand index.
    /// </summary>
    public class SelectedForPayment : IComponent
    {
        public Entity Owner { get; set; }
        public int OriginalHandIndex { get; set; } = -1;
        public Microsoft.Xna.Framework.Vector2 OriginalHandPos { get; set; } = Vector2.Zero;
        public Microsoft.Xna.Framework.Vector2 StartPos { get; set; } = Vector2.Zero;
        public float StartScale { get; set; } = 1f;
        public Microsoft.Xna.Framework.Vector2 TargetPos { get; set; } = Vector2.Zero;
        public float TargetScale { get; set; } = 0.5f;
        public float MoveElapsed { get; set; } = 0f;
        public float MoveDuration { get; set; } = 0.25f;
        public bool IsReturning { get; set; } = false;
        public float ReturnElapsed { get; set; } = 0f;
        public float ReturnDuration { get; set; } = 0.12f;
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
        Penance,
        Aggression,
        Stealth,
        Poison,
        Shield,
    }
}