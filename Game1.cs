using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Systems;
using Crusaders30XX.ECS.Factories;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Config;
using System;

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
    private ProfilerSystem _profilerSystem;
    private CourageDisplaySystem _courageDisplaySystem;
    private TemperanceDisplaySystem _temperanceDisplaySystem;
    private StoredBlockDisplaySystem _storedBlockDisplaySystem;
    private CourageManagerSystem _courageManagerSystem;
    private HPDisplaySystem _hpDisplaySystem;

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
        CardConfig.SetScaleFromViewportHeight(targetHeight);
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
            CardConfig.SetScaleFromViewportHeight(newHeight);
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
        _debugMenuSystem = new DebugMenuSystem(_world.EntityManager, GraphicsDevice, _spriteBatch, _font, _world.SystemManager);
        _debugCommandSystem = new DebugCommandSystem(_world.EntityManager);
        _drawPileDisplaySystem = new DrawPileDisplaySystem(_world.EntityManager, GraphicsDevice, _spriteBatch, _font);
        _discardPileDisplaySystem = new DiscardPileDisplaySystem(_world.EntityManager, GraphicsDevice, _spriteBatch, _font);
        _cardListModalSystem = new CardListModalSystem(_world.EntityManager, GraphicsDevice, _spriteBatch, _font);
        // Load Crusader portrait texture and create player systems
        var crusaderTexture = Content.Load<Texture2D>("Crusader");
        _playerDisplaySystem = new PlayerDisplaySystem(_world.EntityManager, GraphicsDevice, _spriteBatch, crusaderTexture);
        _cathedralLightingSystem = new CathedralLightingSystem(_world.EntityManager, GraphicsDevice, _spriteBatch);
        _playerWispParticleSystem = new PlayerWispParticleSystem(_world.EntityManager, GraphicsDevice, _spriteBatch);
        _courageDisplaySystem = new CourageDisplaySystem(_world.EntityManager, GraphicsDevice, _spriteBatch, _font);
        _temperanceDisplaySystem = new TemperanceDisplaySystem(_world.EntityManager, GraphicsDevice, _spriteBatch, _font);
        _storedBlockDisplaySystem = new StoredBlockDisplaySystem(_world.EntityManager, GraphicsDevice, _spriteBatch, _font);
        _courageManagerSystem = new CourageManagerSystem(_world.EntityManager);
        _hpDisplaySystem = new HPDisplaySystem(_world.EntityManager, GraphicsDevice, _spriteBatch, _font);
        _profilerSystem = new ProfilerSystem(_world.EntityManager, GraphicsDevice, _spriteBatch, _font);

        
        _world.AddSystem(_cardHighlightSystem);
        // Add systems to world
        _world.AddSystem(_renderingSystem);
        _world.AddSystem(_cardDisplaySystem);
        _world.AddSystem(_handDisplaySystem);
        _world.AddSystem(_debugCommandSystem);
        _world.AddSystem(_drawPileDisplaySystem);
        _world.AddSystem(_discardPileDisplaySystem);
        _world.AddSystem(_cardListModalSystem);
        _world.AddSystem(_playerDisplaySystem);
        _world.AddSystem(_cathedralLightingSystem);
        _world.AddSystem(_playerWispParticleSystem);
        _world.AddSystem(_courageDisplaySystem);
        _world.AddSystem(_temperanceDisplaySystem);
        _world.AddSystem(_storedBlockDisplaySystem);
        _world.AddSystem(_courageManagerSystem);
        _world.AddSystem(_profilerSystem);
        _world.AddSystem(_debugMenuSystem);
        _world.AddSystem(_battleBackgroundSystem);
        _world.AddSystem(_hpDisplaySystem);

        // Set initial location via event which seeds the Battlefield component
        EventManager.Publish(new ChangeBattleLocationEvent { Location = BattleLocation.Cathedral });

        EventManager.Publish(new RequestDrawCardsEvent { Count = 4 });
        // TODO: use this.Content to load your game content here
    }
    
    private void CreateInitialGameState()
    {
        // Create game state
        var gameStateEntity = EntityFactory.CreateGameState(_world);
        
        // Create player
        var playerEntity = EntityFactory.CreatePlayer(_world);
        
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

        // Begin profiling frame
        Crusaders30XX.Diagnostics.FrameProfiler.BeginFrame();
        
        // Draw background first
        Crusaders30XX.Diagnostics.FrameProfiler.Measure("BattleBackgroundSystem.Draw", () => _battleBackgroundSystem.Draw());

        // Cathedral lighting beams (under foreground elements)
        Crusaders30XX.Diagnostics.FrameProfiler.Measure("CathedralLightingSystem.Draw", () => _cathedralLightingSystem.Draw());

        // Draw ECS World
        Crusaders30XX.Diagnostics.FrameProfiler.Measure("RenderingSystem.Draw", () => _renderingSystem.Draw());

        // Draw player portrait (middle-left)
        Crusaders30XX.Diagnostics.FrameProfiler.Measure("PlayerDisplaySystem.Draw", () => _playerDisplaySystem.Draw());

        // Draw wisps around the portrait
        Crusaders30XX.Diagnostics.FrameProfiler.Measure("PlayerWispParticleSystem.Draw", () => _playerWispParticleSystem.Draw());

        // Draw courage badge below the portrait
        Crusaders30XX.Diagnostics.FrameProfiler.Measure("CourageDisplaySystem.Draw", () => _courageDisplaySystem.Draw());
        // Draw temperance badge next to courage
        Crusaders30XX.Diagnostics.FrameProfiler.Measure("TemperanceDisplaySystem.Draw", () => _temperanceDisplaySystem.Draw());
        // Draw stored block badge next to temperance
        Crusaders30XX.Diagnostics.FrameProfiler.Measure("StoredBlockDisplaySystem.Draw", () => _storedBlockDisplaySystem.Draw());

        // Draw HP bar under player
        Crusaders30XX.Diagnostics.FrameProfiler.Measure("HPDisplaySystem.Draw", () => _hpDisplaySystem.Draw());

        // Draw hand of cards on top of highlights
        Crusaders30XX.Diagnostics.FrameProfiler.Measure("HandDisplaySystem.DrawHand", () => _handDisplaySystem.DrawHand());

        // Draw debug menu if open
        Crusaders30XX.Diagnostics.FrameProfiler.Measure("DebugMenuSystem.Draw", () => _debugMenuSystem.Draw());

        // Draw draw pile count (bottom-right)
        Crusaders30XX.Diagnostics.FrameProfiler.Measure("DrawPileDisplaySystem.Draw", () => _drawPileDisplaySystem.Draw());

        // Draw discard pile count (bottom-left)
        Crusaders30XX.Diagnostics.FrameProfiler.Measure("DiscardPileDisplaySystem.Draw", () => _discardPileDisplaySystem.Draw());

        // Draw card list modal if open
        Crusaders30XX.Diagnostics.FrameProfiler.Measure("CardListModalSystem.Draw", () => _cardListModalSystem.Draw());
        
        // Draw profiler overlay last
        Crusaders30XX.Diagnostics.FrameProfiler.Measure("ProfilerSystem.Draw", () => _profilerSystem.Draw());

        _spriteBatch.End();

        base.Draw(gameTime);
    }
}
