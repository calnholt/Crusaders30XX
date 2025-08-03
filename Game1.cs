using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Systems;
using Crusaders30XX.ECS.Factories;

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
    private CombatSystem _combatSystem;
    private HandDisplaySystem _handDisplaySystem;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        
        // Set window size
        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 720;
        _graphics.ApplyChanges();
    }

    protected override void Initialize()
    {
        // Initialize ECS World
        _world = new World();
        
        // Create systems that don't need SpriteBatch
        _inputSystem = new InputSystem(_world.EntityManager);
        _deckManagementSystem = new DeckManagementSystem(_world.EntityManager);
        _combatSystem = new CombatSystem(_world.EntityManager);
        
        // Add systems to world
        _world.AddSystem(_inputSystem);
        _world.AddSystem(_deckManagementSystem);
        _world.AddSystem(_combatSystem);
        
        // Create initial game entities
        CreateInitialGameState();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // Load the NewRocker SpriteFont
        _font = Content.Load<SpriteFont>("NewRocker");

        // Create systems that need SpriteBatch
        _renderingSystem = new RenderingSystem(_world.EntityManager, _spriteBatch, GraphicsDevice);
        _handDisplaySystem = new HandDisplaySystem(_world.EntityManager, GraphicsDevice, _spriteBatch, _font, _deckManagementSystem);
        
        // Add systems to world
        _world.AddSystem(_renderingSystem);
        _world.AddSystem(_handDisplaySystem);

        // Trigger initial deck shuffle and draw
        _handDisplaySystem.TriggerDeckShuffleAndDraw(4);

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
        
        // Create some enemies
        var enemy1 = EntityFactory.CreateEnemy(_world, "Goblin", 30, new Vector2(800, 200));
        var enemy2 = EntityFactory.CreateEnemy(_world, "Orc", 50, new Vector2(900, 200));
        
        // Create UI elements
        var endTurnButton = EntityFactory.CreateUIElement(_world, "EndTurnButton", 
            new Rectangle(1100, 600, 150, 50), "button");
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        // Update ECS World
        _world.Update(gameTime);
        
        // Update input system
        _inputSystem.UpdateInput();

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        _spriteBatch.Begin();
        
        // Draw ECS World
        _renderingSystem.Draw();
        
        // Draw hand of cards
        _handDisplaySystem.DrawHand();
        
        // Draw text using the NewRocker font
        if (_font != null)
        {
            _spriteBatch.DrawString(_font, "Crusaders 30XX", new Vector2(50, 50), Color.White);
            _spriteBatch.DrawString(_font, "NewRocker Font Demo", new Vector2(50, 100), Color.Yellow);
        }
        
        _spriteBatch.End();

        base.Draw(gameTime);
    }
}
