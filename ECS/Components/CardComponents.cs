using Crusaders30XX.ECS.Core;
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
    /// Component for cards that are currently in play
    /// </summary>
    public class CardInPlay : IComponent
    {
        public Entity Owner { get; set; }
        
        public bool IsExhausted { get; set; } = false;
        public bool IsUpgraded { get; set; } = false;
        public int EnergyCost { get; set; } = 0;
        public bool IsPlayable { get; set; } = true;
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
        
        public int MaxHandSize { get; set; } = 10;
        public int DrawPerTurn { get; set; } = 5;
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
        public int CurrentEnergy { get; set; } = 3;
        public int Block { get; set; } = 0;
        public int Gold { get; set; } = 0;
        
        public Entity DeckEntity { get; set; }
        public Entity HandEntity { get; set; }
    }
    
    /// <summary>
    /// Component for enemies
    /// </summary>
    public class Enemy : IComponent
    {
        public Entity Owner { get; set; }
        
        public string Name { get; set; } = "";
        public int MaxHealth { get; set; } = 50;
        public int CurrentHealth { get; set; } = 50;
        public int Block { get; set; } = 0;
        public List<Entity> Intentions { get; set; } = new();
        public int IntentIndex { get; set; } = 0;
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
} 