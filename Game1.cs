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
    private RasterizerState _spriteRasterizer;
    private DebugMenuSystem _debugMenuSystem;
    private EntityListOverlaySystem _entityListOverlaySystem;
    private TransitionDisplaySystem _transitionDisplaySystem;
    private CardDisplaySystem _cardDisplaySystem;
    private RenderingSystem _renderingSystem;
    private InputSystem _inputSystem;

    private KeyboardState _prevKeyboard;

    private InternalQueueEventsSceneSystem _menuSceneSystem;
    private TitleMenuDisplaySystem _titleMenuDisplaySystem;
    private BattleSceneSystem _battleSceneSystem;
    private CustomizationRootSystem _customizationRootSystem;
    private TooltipDisplaySystem _tooltipDisplaySystem;
    private ProfilerSystem _profilerSystem;
    private LocationSelectDisplaySystem _worldMapSystem;
    private QuestSelectDisplaySystem _questSelectSystem;
    
    // ECS System
    private World _world;

    public static bool WindowIsActive { get; private set; } = true;

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
        _spriteRasterizer = new RasterizerState { ScissorTestEnable = true, CullMode = CullMode.None };

        // Load the NewRocker SpriteFont
        _font = Content.Load<SpriteFont>("NewRocker");

        // Seed a SceneState entity
        var sceneEntity = _world.EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault();
        if (sceneEntity == null)
        {
            sceneEntity = _world.CreateEntity("SceneState");
            _world.AddComponent(sceneEntity, new SceneState { Current = SceneId.TitleMenu });
        }
        EntityFactory.CreateCardVisualSettings(_world);
        // Add parent scene systems only
        _titleMenuDisplaySystem = new TitleMenuDisplaySystem(_world.EntityManager, GraphicsDevice, _spriteBatch, _font);
        _menuSceneSystem = new InternalQueueEventsSceneSystem(_world.EntityManager, GraphicsDevice, _spriteBatch, Content, _font);
        _battleSceneSystem = new BattleSceneSystem(_world.EntityManager, _world.SystemManager, _world, GraphicsDevice, _spriteBatch, Content, _font);
        _customizationRootSystem = new CustomizationRootSystem(_world.EntityManager, _world.SystemManager, _world, GraphicsDevice, _spriteBatch, Content, _font);
        _debugMenuSystem = new DebugMenuSystem(_world.EntityManager, GraphicsDevice, _spriteBatch, _font, _world.SystemManager);
        _entityListOverlaySystem = new EntityListOverlaySystem(_world.EntityManager, GraphicsDevice, _spriteBatch, _font);
        _transitionDisplaySystem = new TransitionDisplaySystem(_world.EntityManager, GraphicsDevice, _spriteBatch, _font);
        _cardDisplaySystem = new CardDisplaySystem(_world.EntityManager, GraphicsDevice, _spriteBatch, _font, Content);
        _renderingSystem = new RenderingSystem(_world.EntityManager, _spriteBatch, GraphicsDevice);
        _inputSystem = new InputSystem(_world.EntityManager);
        _tooltipDisplaySystem = new TooltipDisplaySystem(_world.EntityManager, GraphicsDevice, _spriteBatch, _font);
        _profilerSystem = new ProfilerSystem(_world.EntityManager, GraphicsDevice, _spriteBatch, _font);
        _worldMapSystem = new LocationSelectDisplaySystem(_world.EntityManager, GraphicsDevice, _spriteBatch, Content, _font);
        _questSelectSystem = new QuestSelectDisplaySystem(_world.EntityManager, GraphicsDevice, _spriteBatch, Content, _font);
        _world.AddSystem(_titleMenuDisplaySystem);
        _world.AddSystem(_menuSceneSystem);
        _world.AddSystem(_battleSceneSystem);
        _world.AddSystem(_customizationRootSystem);
        _world.AddSystem(new TimerSchedulerSystem(_world.EntityManager));
        _world.AddSystem(_debugMenuSystem);
        _world.AddSystem(_entityListOverlaySystem);
        _world.AddSystem(_transitionDisplaySystem);
        _world.AddSystem(_cardDisplaySystem);
        _world.AddSystem(_renderingSystem);
        _world.AddSystem(_inputSystem);
        _world.AddSystem(_tooltipDisplaySystem);
        _world.AddSystem(_profilerSystem);
        _world.AddSystem(_worldMapSystem);
        _world.AddSystem(_questSelectSystem);
        // Global music manager
        _world.AddSystem(new MusicManagerSystem(_world.EntityManager, Content));

        // Mark persistent entities
        _world.AddComponent(sceneEntity, new DontDestroyOnLoad());
        var cvsEntity = _world.EntityManager.GetEntity("CardVisualSettings");
        if (cvsEntity != null)
        {
            _world.AddComponent(cvsEntity, new DontDestroyOnLoad());
        }
        // TODO: use this.Content to load your game content here
    }
    
    protected override void Update(GameTime gameTime)
    {
        WindowIsActive = IsActive;
        var kb = Keyboard.GetState();
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || (kb.IsKeyDown(Keys.Escape) && kb.IsKeyDown(Keys.LeftShift)))
            Exit();

        // Global debug menu toggle so it's available in the main menu too
        if (kb.IsKeyDown(Keys.D) && !_prevKeyboard.IsKeyDown(Keys.D))
        {
            ToggleDebugMenu();
        }

        // Entity list overlay toggle: Shift + W
        if ((kb.IsKeyDown(Keys.LeftShift) || kb.IsKeyDown(Keys.RightShift)) && kb.IsKeyDown(Keys.W) && !_prevKeyboard.IsKeyDown(Keys.W))
        {
            ToggleEntityListOverlay();
        }

        // Update ECS World (this includes input processing)
        _world.Update(gameTime);

        _prevKeyboard = kb;
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, _spriteRasterizer);
        // Delegate drawing to active parent systems
        var scene = _world.EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault().GetComponent<SceneState>();
        switch(scene.Current)
        {
            case SceneId.TitleMenu:
            {
                FrameProfiler.Measure("TitleMenuDisplaySystem.Draw", _titleMenuDisplaySystem.Draw);
                break;
            }
            case SceneId.Internal_QueueEventsMenu:
            {
                FrameProfiler.Measure("MenuSceneSystem.Draw", _menuSceneSystem.Draw);
                break;
            }
            case SceneId.Customization:
            {
                FrameProfiler.Measure("CustomizationRootSystem.Draw", _customizationRootSystem.Draw);
                break;
            }
            case SceneId.Battle:
            {
                FrameProfiler.Measure("BattleSceneSystem.Draw", _battleSceneSystem.Draw);
                break;
            }
            case SceneId.WorldMap:
            {
                // Draw location tiles with crisp pixel sampling
                _spriteBatch.End();
                _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, _spriteRasterizer);
                FrameProfiler.Measure("LocationSelectDisplaySystem.Draw", _worldMapSystem.Draw);
                FrameProfiler.Measure("QuestSelectDisplaySystem.Draw", _questSelectSystem.Draw);
                _spriteBatch.End();
                // Resume default sampling for overlays and other UI
                _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, _spriteRasterizer);
                break;
            }
        }
        FrameProfiler.Measure("TooltipDisplaySystem.Draw", _tooltipDisplaySystem.Draw);
        FrameProfiler.Measure("ProfilerSystem.Draw", _profilerSystem.Draw);
        FrameProfiler.Measure("DebugMenuSystem.Draw", _debugMenuSystem.Draw);
        FrameProfiler.Measure("EntityListOverlaySystem.Draw", _entityListOverlaySystem.Draw);
        FrameProfiler.Measure("TransitionDisplaySystem.Draw", _transitionDisplaySystem.Draw);
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void ToggleDebugMenu()
    {
        var em = _world.EntityManager;
        var menuEntity = em.GetEntitiesWithComponent<DebugMenu>().FirstOrDefault();
        if (menuEntity == null)
        {
            menuEntity = _world.CreateEntity("DebugMenu");
            _world.AddComponent(menuEntity, new DebugMenu { IsOpen = true });
            _world.AddComponent(menuEntity, new Transform { Position = new Vector2(1800, 200), ZOrder = 5000 });
            _world.AddComponent(menuEntity, new UIElement { Bounds = new Rectangle(1750, 150, 150, 300), IsInteractable = true });
            _world.AddComponent(menuEntity, new DontDestroyOnLoad());
        }
        else
        {
            var menu = menuEntity.GetComponent<DebugMenu>();
            menu.IsOpen = !menu.IsOpen;
            foreach (var e in em.GetEntitiesWithComponent<UIButton>())
            {
                var ui = e.GetComponent<UIElement>();
                if (ui != null) ui.IsInteractable = menu.IsOpen;
            }
        }
    }

    private void ToggleEntityListOverlay()
    {
        var em = _world.EntityManager;
        var overlayEntity = em.GetEntitiesWithComponent<EntityListOverlay>().FirstOrDefault();
        if (overlayEntity == null)
        {
            overlayEntity = _world.CreateEntity("EntityListOverlay");
            _world.AddComponent(overlayEntity, new EntityListOverlay { IsOpen = true });
            _world.AddComponent(overlayEntity, new DontDestroyOnLoad());
        }
        else
        {
            var o = overlayEntity.GetComponent<EntityListOverlay>();
            o.IsOpen = !o.IsOpen;
        }
    }
}
