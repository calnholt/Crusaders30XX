using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Systems;
using Crusaders30XX.ECS.Factories;
using Crusaders30XX.ECS.Events;
using System;
using Crusaders30XX.Diagnostics;

namespace Crusaders30XX;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private SpriteFont _font;
    
    // ECS System
    private World _world;
    private RenderingSystem _renderingSystem;
    private InputSystem _inputSystem;
    private DeckManagementSystem _deckManagementSystem;
    private CardDisplaySystem _cardDisplaySystem;
    private HandDisplaySystem _handDisplaySystem;
    private CardHighlightSystem _cardHighlightSystem;
    private BattleBackgroundSystem _battleBackgroundSystem;
    private DebugMenuSystem _debugMenuSystem;
    private DebugCommandSystem _debugCommandSystem;
    private DrawPileDisplaySystem _drawPileDisplaySystem;
    private DiscardPileDisplaySystem _discardPileDisplaySystem;
    private CardListModalSystem _cardListModalSystem;
    private PlayerDisplaySystem _playerDisplaySystem;
    private PlayerWispParticleSystem _playerWispParticleSystem;
    private CathedralLightingSystem _cathedralLightingSystem;
    private DesertBackgroundEffectSystem _desertBackgroundEffectSystem;
    private ProfilerSystem _profilerSystem;
    private CourageDisplaySystem _courageDisplaySystem;
    private TemperanceDisplaySystem _temperanceDisplaySystem;
    private StoredBlockDisplaySystem _storedBlockDisplaySystem;
    private CourageManagerSystem _courageManagerSystem;
    private TemperanceManagerSystem _temperanceManagerSystem;
    private TooltipDisplaySystem _tooltipDisplaySystem;
    private HPDisplaySystem _hpDisplaySystem;
    private CardVisualSettingsDebugSystem _cardVisualSettingsDebugSystem;
    private HpManagementSystem _hpManagementSystem;
    private BattlePhaseSystem _battlePhaseSystem;
    private BattlePhaseDisplaySystem _battlePhaseDisplaySystem;
    private EnemyDisplaySystem _enemyDisplaySystem;
    private EnemyTurnStarterSystem _enemyTurnStarterSystem;
    private EnemyIntentPipsSystem _enemyIntentPipsSystem;
    private EnemyAttackDisplaySystem _enemyAttackDisplaySystem;
    private AssignedBlockCardsDisplaySystem _assignedBlockCardsDisplaySystem;
    private EnemyIntentPlanningSystem _enemyIntentPlanningSystem;
    private EnemyAttackProgressManagementSystem _enemyAttackProgressManagementSystem;
    private AttackResolutionSystem _attackResolutionSystem;
    private HandBlockInteractionSystem _handBlockInteractionSystem;
    private StoredBlockManagementSystem _storedBlockManagementSystem;
    private CardZoneSystem _cardZoneSystem;
    private AssignedBlocksToDiscardSystem _assignedBlocksToDiscardSystem;
    private EnemyDamageManagerSystem _enemyDamageManagerSystem;
    private EventQueueSystem _eventQueueSystem;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.AllowUserResizing = true;
        
        // Set window size dynamically, clamped to 1920x1080
        var displayMode = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
        int targetWidth = Math.Min(1920, displayMode.Width);
        int targetHeight = Math.Min(1080, displayMode.Height);
        _graphics.PreferredBackBufferWidth = targetWidth;
        _graphics.PreferredBackBufferHeight = targetHeight;
        // UIScale now lives in CardVisualSettings; initial seeding happens in EntityFactory
        _graphics.ApplyChanges();

        // Clamp user resize to the maximum of 1920x1080
        Window.ClientSizeChanged += (sender, args) =>
        {
            int newWidth = Math.Min(1920, Window.ClientBounds.Width);
            int newHeight = Math.Min(1080, Window.ClientBounds.Height);
            if (newWidth != _graphics.PreferredBackBufferWidth || newHeight != _graphics.PreferredBackBufferHeight)
            {
            _graphics.PreferredBackBufferWidth = newWidth;
            _graphics.PreferredBackBufferHeight = newHeight;
            // Adjust UIScale via CardVisualSettingsDebugSystem if desired
                _graphics.ApplyChanges();
            }
        };
    }

    protected override void Initialize()
    {
        // Initialize ECS World
        _world = new World();
        
        // Create systems that don't need SpriteBatch
        _inputSystem = new InputSystem(_world.EntityManager);
        _deckManagementSystem = new DeckManagementSystem(_world.EntityManager);
        
        // Add systems to world
        _world.AddSystem(_inputSystem);
        _world.AddSystem(_deckManagementSystem);
        
        // Create initial game entities
        CreateInitialGameState();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // Load the NewRocker SpriteFont
        _font = Content.Load<SpriteFont>("NewRocker");


        _cardHighlightSystem = new CardHighlightSystem(_world.EntityManager, GraphicsDevice, _spriteBatch);
        _battleBackgroundSystem = new BattleBackgroundSystem(_world.EntityManager, GraphicsDevice, _spriteBatch, Content);
        _renderingSystem = new RenderingSystem(_world.EntityManager, _spriteBatch, GraphicsDevice);
        _cardDisplaySystem = new CardDisplaySystem(_world.EntityManager, GraphicsDevice, _spriteBatch, _font);
        _handDisplaySystem = new HandDisplaySystem(_world.EntityManager, GraphicsDevice);
        _cardZoneSystem = new CardZoneSystem(_world.EntityManager);
        _debugMenuSystem = new DebugMenuSystem(_world.EntityManager, GraphicsDevice, _spriteBatch, _font, _world.SystemManager);
        _debugCommandSystem = new DebugCommandSystem(_world.EntityManager);
        _drawPileDisplaySystem = new DrawPileDisplaySystem(_world.EntityManager, GraphicsDevice, _spriteBatch, _font);
        _discardPileDisplaySystem = new DiscardPileDisplaySystem(_world.EntityManager, GraphicsDevice, _spriteBatch, _font);
        _cardListModalSystem = new CardListModalSystem(_world.EntityManager, GraphicsDevice, _spriteBatch, _font);
        // Load Crusader portrait texture and create player systems
        var crusaderTexture = Content.Load<Texture2D>("Crusader");
        _playerDisplaySystem = new PlayerDisplaySystem(_world.EntityManager, GraphicsDevice, _spriteBatch, crusaderTexture);
        _cathedralLightingSystem = new CathedralLightingSystem(_world.EntityManager, GraphicsDevice, _spriteBatch);
        _desertBackgroundEffectSystem = new DesertBackgroundEffectSystem(_world.EntityManager, GraphicsDevice, _spriteBatch);
        _playerWispParticleSystem = new PlayerWispParticleSystem(_world.EntityManager, GraphicsDevice, _spriteBatch);
        _courageDisplaySystem = new CourageDisplaySystem(_world.EntityManager, GraphicsDevice, _spriteBatch, _font);
        _temperanceDisplaySystem = new TemperanceDisplaySystem(_world.EntityManager, GraphicsDevice, _spriteBatch, _font);
        _storedBlockDisplaySystem = new StoredBlockDisplaySystem(_world.EntityManager, GraphicsDevice, _spriteBatch, _font);
        _courageManagerSystem = new CourageManagerSystem(_world.EntityManager);
        _temperanceManagerSystem = new TemperanceManagerSystem(_world.EntityManager);
        _tooltipDisplaySystem = new TooltipDisplaySystem(_world.EntityManager, GraphicsDevice, _spriteBatch, _font);
        _hpDisplaySystem = new HPDisplaySystem(_world.EntityManager, GraphicsDevice, _spriteBatch, _font);
        _cardVisualSettingsDebugSystem = new CardVisualSettingsDebugSystem(_world.EntityManager);
        _profilerSystem = new ProfilerSystem(_world.EntityManager, GraphicsDevice, _spriteBatch, _font);
        _hpManagementSystem = new HpManagementSystem(_world.EntityManager);
        _eventQueueSystem = new EventQueueSystem(_world.EntityManager);
        _battlePhaseSystem = new BattlePhaseSystem(_world.EntityManager);
        _battlePhaseSystem.Initialize();
        _battlePhaseDisplaySystem = new BattlePhaseDisplaySystem(_world.EntityManager, GraphicsDevice, _spriteBatch, _font);
        _enemyTurnStarterSystem = new EnemyTurnStarterSystem(_world.EntityManager);
        _enemyDisplaySystem = new EnemyDisplaySystem(_world.EntityManager, GraphicsDevice, _spriteBatch, Content);
        _enemyIntentPipsSystem = new EnemyIntentPipsSystem(_world.EntityManager, GraphicsDevice, _spriteBatch);
        _enemyAttackDisplaySystem = new EnemyAttackDisplaySystem(_world.EntityManager, GraphicsDevice, _spriteBatch, _font);
        _assignedBlockCardsDisplaySystem = new AssignedBlockCardsDisplaySystem(_world.EntityManager, GraphicsDevice, _spriteBatch, _font);
        _enemyIntentPlanningSystem = new EnemyIntentPlanningSystem(_world.EntityManager);
        _enemyAttackProgressManagementSystem = new EnemyAttackProgressManagementSystem(_world.EntityManager);
        // _blockConditionTrackingSystem = new BlockConditionTrackingSystem(_world.EntityManager);
        _attackResolutionSystem = new AttackResolutionSystem(_world.EntityManager);
        _handBlockInteractionSystem = new HandBlockInteractionSystem(_world.EntityManager);
        _storedBlockManagementSystem = new StoredBlockManagementSystem(_world.EntityManager);
        _assignedBlocksToDiscardSystem = new AssignedBlocksToDiscardSystem(_world.EntityManager, GraphicsDevice);
        _enemyDamageManagerSystem = new EnemyDamageManagerSystem(_world.EntityManager);

        
        _world.AddSystem(_cardHighlightSystem);
        // Add systems to world
        _world.AddSystem(_renderingSystem);
        _world.AddSystem(_cardDisplaySystem);
        _world.AddSystem(_handDisplaySystem);
        _world.AddSystem(_cardZoneSystem);
        _world.AddSystem(_handBlockInteractionSystem);
        _world.AddSystem(_eventQueueSystem);
        _world.AddSystem(_debugCommandSystem);
        _world.AddSystem(_drawPileDisplaySystem);
        _world.AddSystem(_discardPileDisplaySystem);
        _world.AddSystem(_cardListModalSystem);
        _world.AddSystem(_playerDisplaySystem);
        _world.AddSystem(_cathedralLightingSystem);
        _world.AddSystem(_desertBackgroundEffectSystem);
        _world.AddSystem(_playerWispParticleSystem);
        _world.AddSystem(_tooltipDisplaySystem);
        _world.AddSystem(_courageDisplaySystem);
        _world.AddSystem(_temperanceDisplaySystem);
        _world.AddSystem(_storedBlockDisplaySystem);
        _world.AddSystem(_courageManagerSystem);
        _world.AddSystem(_temperanceManagerSystem);
        _world.AddSystem(_profilerSystem);
        _world.AddSystem(_debugMenuSystem);
        _world.AddSystem(_battleBackgroundSystem);
        _world.AddSystem(_hpDisplaySystem);
        _world.AddSystem(_cardVisualSettingsDebugSystem);
        _world.AddSystem(_hpManagementSystem);
        _world.AddSystem(_battlePhaseSystem);
        _world.AddSystem(_battlePhaseDisplaySystem);
        _world.AddSystem(_enemyTurnStarterSystem);
        _world.AddSystem(_enemyDisplaySystem);
        _world.AddSystem(_enemyIntentPipsSystem);
        _world.AddSystem(_enemyIntentPlanningSystem);
        _world.AddSystem(_enemyAttackProgressManagementSystem);
        // _world.AddSystem(_blockConditionTrackingSystem);
        _world.AddSystem(_attackResolutionSystem);
        _world.AddSystem(_enemyAttackDisplaySystem);
        _world.AddSystem(_assignedBlockCardsDisplaySystem);
        _world.AddSystem(_assignedBlocksToDiscardSystem);
        _world.AddSystem(_storedBlockManagementSystem);
        _world.AddSystem(_enemyDamageManagerSystem);

        // Set initial location via event which seeds the Battlefield component
        EventManager.Publish(new ChangeBattleLocationEvent { Location = BattleLocation.Desert });

        EventManager.Publish(new DeckShuffleEvent { });
        EventManager.Publish(new RequestDrawCardsEvent { Count = 4 });
        // TODO: use this.Content to load your game content here
    }
    
    private void CreateInitialGameState()
    {
        // Create game state
        EntityFactory.CreateGameState(_world);
        // Create player
        EntityFactory.CreatePlayer(_world);
        // Create deck
        var deckEntity = EntityFactory.CreateDeck(_world);
        // Create demo hand of cards
        var demoHand = EntityFactory.CreateDemoHand(_world);
        // Add cards to deck's draw pile (not hand)
        var deck = deckEntity.GetComponent<Crusaders30XX.ECS.Components.Deck>();
        if (deck != null)
        {
            deck.Cards.AddRange(demoHand);
            deck.DrawPile.AddRange(demoHand); // Add to draw pile instead of hand
        }
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        // Update ECS World (this includes input processing)
        _world.Update(gameTime);
        
        // Update input system AFTER world update to store previous state
        _inputSystem.UpdateInput();

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        _spriteBatch.Begin();

        FrameProfiler.BeginFrame();
        FrameProfiler.Measure("BattleBackgroundSystem.Draw", () => _battleBackgroundSystem.Draw());
        FrameProfiler.Measure("CathedralLightingSystem.Draw", () => _cathedralLightingSystem.Draw());
        FrameProfiler.Measure("DesertBackgroundEffectSystem.Draw", () => _desertBackgroundEffectSystem.Draw());
        FrameProfiler.Measure("RenderingSystem.Draw", () => _renderingSystem.Draw());
        FrameProfiler.Measure("PlayerDisplaySystem.Draw", () => _playerDisplaySystem.Draw());
        FrameProfiler.Measure("EnemyDisplaySystem.Draw", () => _enemyDisplaySystem.Draw());
        FrameProfiler.Measure("EnemyIntentPipsSystem.Draw", () => _enemyIntentPipsSystem.Draw());
        FrameProfiler.Measure("EnemyAttackDisplaySystem.Draw", () => _enemyAttackDisplaySystem.Draw());
        FrameProfiler.Measure("AssignedBlockCardsDisplaySystem.Draw", () => _assignedBlockCardsDisplaySystem.Draw());
        FrameProfiler.Measure("PlayerWispParticleSystem.Draw", () => _playerWispParticleSystem.Draw());
        FrameProfiler.Measure("BattlePhaseDisplaySystem.Draw", () => _battlePhaseDisplaySystem.Draw());
        FrameProfiler.Measure("CourageDisplaySystem.Draw", () => _courageDisplaySystem.Draw());
        FrameProfiler.Measure("TemperanceDisplaySystem.Draw", () => _temperanceDisplaySystem.Draw());
        FrameProfiler.Measure("StoredBlockDisplaySystem.Draw", () => _storedBlockDisplaySystem.Draw());
        FrameProfiler.Measure("HPDisplaySystem.Draw", () => _hpDisplaySystem.Draw());
        FrameProfiler.Measure("HandDisplaySystem.DrawHand", () => _handDisplaySystem.DrawHand());
        FrameProfiler.Measure("DebugMenuSystem.Draw", () => _debugMenuSystem.Draw());
        FrameProfiler.Measure("DrawPileDisplaySystem.Draw", () => _drawPileDisplaySystem.Draw());
        FrameProfiler.Measure("DiscardPileDisplaySystem.Draw", () => _discardPileDisplaySystem.Draw());
        FrameProfiler.Measure("CardListModalSystem.Draw", () => _cardListModalSystem.Draw());
        FrameProfiler.Measure("TooltipDisplaySystem.Draw", () => _tooltipDisplaySystem.Draw());
        FrameProfiler.Measure("ProfilerSystem.Draw", () => _profilerSystem.Draw());
        _spriteBatch.End();

        base.Draw(gameTime);
    }
}
