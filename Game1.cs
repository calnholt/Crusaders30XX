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
