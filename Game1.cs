using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Systems;
using Crusaders30XX.ECS.Factories;
using Crusaders30XX.ECS.Events;
using System;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using System.Linq;

namespace Crusaders30XX;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private SpriteFont _font;
    
    
    // ECS System
    private World _world;
    // private RenderingSystem _renderingSystem;
    // private InputSystem _inputSystem;
    // private DeckManagementSystem _deckManagementSystem;
    // private CardDisplaySystem _cardDisplaySystem;
    // private HandDisplaySystem _handDisplaySystem;
    // private CardHighlightSystem _cardHighlightSystem;
    // private BattleBackgroundSystem _battleBackgroundSystem;
    // private DebugMenuSystem _debugMenuSystem;
    // private DebugCommandSystem _debugCommandSystem;
    // private DrawPileDisplaySystem _drawPileDisplaySystem;
    // private DiscardPileDisplaySystem _discardPileDisplaySystem;
    // private CardListModalSystem _cardListModalSystem;
    // private PlayerDisplaySystem _playerDisplaySystem;
    // private PlayerWispParticleSystem _playerWispParticleSystem;
    // private CathedralLightingSystem _cathedralLightingSystem;
    // private DesertBackgroundEffectSystem _desertBackgroundEffectSystem;
    // private ProfilerSystem _profilerSystem;
    // private CourageDisplaySystem _courageDisplaySystem;
    // private ActionPointDisplaySystem _actionPointDisplaySystem;
    // private TemperanceDisplaySystem _temperanceDisplaySystem;
    // private StoredBlockDisplaySystem _storedBlockDisplaySystem;
    // private CourageManagerSystem _courageManagerSystem;
    // private ActionPointManagementSystem _actionPointManagementSystem;
    // private TemperanceManagerSystem _temperanceManagerSystem;
    // private TooltipDisplaySystem _tooltipDisplaySystem;
    // private HPDisplaySystem _hpDisplaySystem;
    // private CardVisualSettingsDebugSystem _cardVisualSettingsDebugSystem;
    // private HpManagementSystem _hpManagementSystem;
    // private BattlePhaseSystem _battlePhaseSystem;
    // private BattlePhaseDisplaySystem _battlePhaseDisplaySystem;
    // private EnemyDisplaySystem _enemyDisplaySystem;
    // private EnemyIntentPipsSystem _enemyIntentPipsSystem;
    // private EnemyAttackDisplaySystem _enemyAttackDisplaySystem;
    // private AssignedBlockCardsDisplaySystem _assignedBlockCardsDisplaySystem;
    // private EnemyIntentPlanningSystem _enemyIntentPlanningSystem;
    // private EnemyAttackProgressManagementSystem _enemyAttackProgressManagementSystem;
    // private StunnedOverlaySystem _stunnedOverlaySystem;
    // private AttackResolutionSystem _attackResolutionSystem;
    // private HandBlockInteractionSystem _handBlockInteractionSystem;
    // private StoredBlockManagementSystem _storedBlockManagementSystem;
    // private CardZoneSystem _cardZoneSystem;
    // private AssignedBlocksToDiscardSystem _assignedBlocksToDiscardSystem;
    // private EnemyDamageManagerSystem _enemyDamageManagerSystem;
    // private EventQueueSystem _eventQueueSystem;
    // private CardPlaySystem _cardPlaySystem;
    // private EndTurnDisplaySystem _endTurnDisplaySystem;
    // private PayCostOverlaySystem _payCostOverlaySystem;
    // private DrawHandSystem _battlePhaseDrawSystem;
    // private PhaseCoordinatorSystem _phaseCoordinatorSystem;
    // private EnemyStunAutoSkipSystem _enemyStunAutoSkipSystem;

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
        // _inputSystem = new InputSystem(_world.EntityManager);
        // _deckManagementSystem = new DeckManagementSystem(_world.EntityManager);
        
        // Add systems to world
        // _world.AddSystem(_inputSystem);
        // _world.AddSystem(_deckManagementSystem);
        
        // Menu/Battle scene systems manage initialization

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // Load the NewRocker SpriteFont
        _font = Content.Load<SpriteFont>("NewRocker");

        // Seed a SceneState entity
        var sceneEntity = _world.EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault();
        if (sceneEntity == null)
        {
            sceneEntity = _world.CreateEntity("SceneState");
            _world.AddComponent(sceneEntity, new SceneState { Current = SceneId.Menu });
        }
        // Add parent scene systems only
        var menuSceneSystem = new MenuSceneSystem(_world.EntityManager, GraphicsDevice, _spriteBatch, Content, _font);
        var battleSceneSystem = new BattleSceneSystem(_world.EntityManager, _world.SystemManager, _world, GraphicsDevice, _spriteBatch, Content, _font);
        _world.AddSystem(menuSceneSystem);
        _world.AddSystem(battleSceneSystem);

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
        var deck = deckEntity.GetComponent<Deck>();
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

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        _spriteBatch.Begin();
        // Delegate drawing to active parent systems
        var menuScene = _world.SystemManager.GetSystem<MenuSceneSystem>();
        var battleScene = _world.SystemManager.GetSystem<BattleSceneSystem>();
        menuScene?.Draw();
        battleScene?.Draw();
        _spriteBatch.End();

        base.Draw(gameTime);
    }
}
