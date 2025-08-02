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
    
    // ECS System
    private World _world;
    private RenderingSystem _renderingSystem;
    private InputSystem _inputSystem;
    private DeckManagementSystem _deckManagementSystem;
    private CombatSystem _combatSystem;

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
        
        // Create systems
        _renderingSystem = new RenderingSystem(_world.EntityManager, _spriteBatch, GraphicsDevice);
        _inputSystem = new InputSystem(_world.EntityManager);
        _deckManagementSystem = new DeckManagementSystem(_world.EntityManager);
        _combatSystem = new CombatSystem(_world.EntityManager);
        
        // Add systems to world
        _world.AddSystem(_renderingSystem);
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
        
        // Create some basic cards
        var strikeCard = EntityFactory.CreateAttackCard(_world, "Strike", 6);
        var defendCard = EntityFactory.CreateSkillCard(_world, "Defend", "Gain 5 block", 1);
        var bashCard = EntityFactory.CreateAttackCard(_world, "Bash", 8, 2);
        
        // Add cards to deck
        var deck = deckEntity.GetComponent<Crusaders30XX.ECS.Components.Deck>();
        if (deck != null)
        {
            deck.Cards.Add(strikeCard);
            deck.Cards.Add(strikeCard);
            deck.Cards.Add(strikeCard);
            deck.Cards.Add(strikeCard);
            deck.Cards.Add(defendCard);
            deck.Cards.Add(defendCard);
            deck.Cards.Add(defendCard);
            deck.Cards.Add(defendCard);
            deck.Cards.Add(bashCard);
            
            // Add all cards to draw pile
            deck.DrawPile.AddRange(deck.Cards);
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
        
        _spriteBatch.End();

        base.Draw(gameTime);
    }
}
