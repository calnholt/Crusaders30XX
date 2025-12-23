using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Crusaders30XX.ECS.Core;
// duplicate removed
using Crusaders30XX.ECS.Factories;
using Crusaders30XX.ECS.Events;
using System;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using System.Linq;
using Crusaders30XX.ECS.Systems;
using Crusaders30XX.ECS.Singletons;

namespace Crusaders30XX;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private RasterizerState _spriteRasterizer;
    private DebugMenuSystem _debugMenuSystem;
    private EntityListOverlaySystem _entityListOverlaySystem;
    private TransitionDisplaySystem _transitionDisplaySystem;
    private CardDisplaySystem _cardDisplaySystem;
    private InputSystem _inputSystem;
	private CurrencyDisplaySystem _currencyDisplaySystem;

    private KeyboardState _prevKeyboard;

    private InternalQueueEventsSceneSystem _menuSceneSystem;
    private TitleMenuDisplaySystem _titleMenuDisplaySystem;
    private BattleSceneSystem _battleSceneSystem;
    private LocationSceneSystem _locationSceneSystem;
    private ShopSceneSystem _shopSceneSystem;
    private CustomizationRootSystem _customizationRootSystem;
    private TooltipTextDisplaySystem _tooltipTextDisplaySystem;
    private HintTooltipDisplaySystem _hintTooltipDisplaySystem;
    private CardTooltipDisplaySystem _cardTooltipDisplaySystem;
    private ProfilerSystem _profilerSystem;
    // private LocationSelectDisplaySystem _worldMapSystem;
    private ParallaxLayerSystem _parallaxLayerSystem;
    private UIElementHighlightSystem _uiElementHighlightSystem;
    private CursorSystem _cursorSystem;
    private HotKeySystem _hotKeySystem;
    private HotKeyProgressRingSystem _hotKeyProgressRingSystem;
    private UIElementBorderDebugSystem _uiElementBorderDebugSystem;
    private DialogDisplaySystem _dialogDisplaySystem;
    private DebugCommandSystem _debugCommandSystem;
    private LocationNameDisplaySystem _locationNameDisplaySystem;
    
    // ECS System
    private World _world;

    // Shockwave integration
    private ShockwaveDisplaySystem _shockwaveSystem;
    private RectangularShockwaveDisplaySystem _rectangularShockwaveSystem;
    private PoisonDamageDisplaySystem _poisonSystem;
    private RenderTarget2D _sceneRt;
    private RenderTarget2D _ppA;
    private RenderTarget2D _ppB;

	// Alpha-weighted additive blending so source alpha modulates brightness
	private static readonly BlendState AdditiveAlphaOne = new BlendState
	{
		ColorSourceBlend = Blend.SourceAlpha,
		ColorDestinationBlend = Blend.One,
		ColorBlendFunction = BlendFunction.Add,
		AlphaSourceBlend = Blend.One,
		AlphaDestinationBlend = Blend.One,
		AlphaBlendFunction = BlendFunction.Add
	};

    public static bool WindowIsActive { get; private set; } = true;
    public static int VirtualWidth = 1920;
    public static int VirtualHeight = 1080;
    public static Rectangle RenderDestination { get; private set; }
    
    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        Window.AllowUserResizing = true;
        
        // Initial setup - will be adjusted by CalculateRenderDestination
        _graphics.PreferredBackBufferWidth = VirtualWidth;
        _graphics.PreferredBackBufferHeight = VirtualHeight;
        
        _graphics.ApplyChanges();

        Window.ClientSizeChanged += (sender, args) =>
        {
            if (_graphics.PreferredBackBufferWidth != Window.ClientBounds.Width ||
                _graphics.PreferredBackBufferHeight != Window.ClientBounds.Height)
            {
                _graphics.PreferredBackBufferWidth = Window.ClientBounds.Width;
                _graphics.PreferredBackBufferHeight = Window.ClientBounds.Height;
                _graphics.ApplyChanges();
                
                CalculateRenderDestination();
            }
        };
    }

    protected override void Initialize()
    {
        CalculateRenderDestination();
        // Initialize ECS World
        _world = new World();
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _spriteRasterizer = new RasterizerState { ScissorTestEnable = true, CullMode = CullMode.None };

        // Initialize FontSingleton with both fonts
        FontSingleton.Initialize(Content);

        // Seed a SceneState entity
        var sceneEntity = _world.EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault();
        if (sceneEntity == null)
        {
            sceneEntity = _world.CreateEntity("SceneState");
            _world.AddComponent(sceneEntity, new SceneState { Current = SceneId.TitleMenu });
        }
        EntityFactory.CreateCardVisualSettings(_world);
        // Add parent scene systems only
        _titleMenuDisplaySystem = new TitleMenuDisplaySystem(_world.EntityManager, GraphicsDevice, _spriteBatch);
        _menuSceneSystem = new InternalQueueEventsSceneSystem(_world.EntityManager, GraphicsDevice, _spriteBatch, Content);
        _battleSceneSystem = new BattleSceneSystem(_world.EntityManager, _world.SystemManager, _world, GraphicsDevice, _spriteBatch, Content);
        _locationSceneSystem = new LocationSceneSystem(_world.EntityManager, _world.SystemManager, _world, GraphicsDevice, _spriteBatch, Content);
        _shopSceneSystem = new ShopSceneSystem(_world.EntityManager, _world.SystemManager, _world, GraphicsDevice, _spriteBatch, Content);
        _customizationRootSystem = new CustomizationRootSystem(_world.EntityManager, _world.SystemManager, _world, GraphicsDevice, _spriteBatch, Content);
        _debugMenuSystem = new DebugMenuSystem(_world.EntityManager, GraphicsDevice, _spriteBatch, _world.SystemManager);
        _entityListOverlaySystem = new EntityListOverlaySystem(_world.EntityManager, GraphicsDevice, _spriteBatch);
        _transitionDisplaySystem = new TransitionDisplaySystem(_world.EntityManager, GraphicsDevice, _spriteBatch);
        _cardDisplaySystem = new CardDisplaySystem(_world.EntityManager, GraphicsDevice, _spriteBatch, Content);
        _dialogDisplaySystem = new DialogDisplaySystem(_world.EntityManager, GraphicsDevice, _spriteBatch, Content);
        _inputSystem = new InputSystem(_world.EntityManager);
        _tooltipTextDisplaySystem = new TooltipTextDisplaySystem(_world.EntityManager, GraphicsDevice, _spriteBatch);
        _hintTooltipDisplaySystem = new HintTooltipDisplaySystem(_world.EntityManager, GraphicsDevice, _spriteBatch, Content);
        _cardTooltipDisplaySystem = new CardTooltipDisplaySystem(_world.EntityManager, GraphicsDevice, _spriteBatch);
        _locationNameDisplaySystem = new LocationNameDisplaySystem(_world.EntityManager, GraphicsDevice, _spriteBatch);
		_currencyDisplaySystem = new CurrencyDisplaySystem(GraphicsDevice, _spriteBatch, Content);
        _profilerSystem = new ProfilerSystem(_world.EntityManager, GraphicsDevice, _spriteBatch);
        // _worldMapSystem = new LocationSelectDisplaySystem(_world.EntityManager, GraphicsDevice, _spriteBatch, Content);
        _cursorSystem = new CursorSystem(_world.EntityManager, GraphicsDevice, _spriteBatch, Content);
        _hotKeySystem = new HotKeySystem(_world.EntityManager, GraphicsDevice, _spriteBatch);
        _hotKeyProgressRingSystem = new HotKeyProgressRingSystem(_world.EntityManager, GraphicsDevice, _spriteBatch, _world.SystemManager);
        _parallaxLayerSystem = new ParallaxLayerSystem(_world.EntityManager, GraphicsDevice);
        _uiElementBorderDebugSystem = new UIElementBorderDebugSystem(_world.EntityManager, GraphicsDevice, _spriteBatch);
        _uiElementHighlightSystem = new UIElementHighlightSystem(_world.EntityManager, GraphicsDevice, _spriteBatch);
        _debugCommandSystem = new DebugCommandSystem(_world.EntityManager);
        _world.AddSystem(_titleMenuDisplaySystem);
        _world.AddSystem(_menuSceneSystem);
        _world.AddSystem(_battleSceneSystem);
        _world.AddSystem(_locationSceneSystem);
        _world.AddSystem(_shopSceneSystem);
        _world.AddSystem(_customizationRootSystem);
        _world.AddSystem(new TimerSchedulerSystem(_world.EntityManager));
        _world.AddSystem(_debugMenuSystem);
        _world.AddSystem(_entityListOverlaySystem);
        _world.AddSystem(_transitionDisplaySystem);
        _world.AddSystem(_cardDisplaySystem);
        _world.AddSystem(_dialogDisplaySystem);
        _world.AddSystem(_inputSystem);
        _world.AddSystem(_tooltipTextDisplaySystem);
        _world.AddSystem(_hintTooltipDisplaySystem);
        _world.AddSystem(_cardTooltipDisplaySystem);
        _world.AddSystem(_profilerSystem);
        _world.AddSystem(_locationNameDisplaySystem);
        // _world.AddSystem(_worldMapSystem);
        _world.AddSystem(_cursorSystem);
        _world.AddSystem(_hotKeySystem);
        _world.AddSystem(_hotKeyProgressRingSystem);
        _world.AddSystem(_parallaxLayerSystem);
        _world.AddSystem(_uiElementBorderDebugSystem);
        _world.AddSystem(_debugCommandSystem);
        // Global music manager
        _world.AddSystem(new MusicManagerSystem(_world.EntityManager, Content));
        // Global sound effect manager
        _world.AddSystem(new SoundEffectManagerSystem(_world.EntityManager, Content));
        _shockwaveSystem = new ShockwaveDisplaySystem(_world.EntityManager, GraphicsDevice, _spriteBatch, Content);
        _world.AddSystem(_shockwaveSystem);
        _rectangularShockwaveSystem = new RectangularShockwaveDisplaySystem(_world.EntityManager, GraphicsDevice, _spriteBatch, Content);
        _world.AddSystem(_rectangularShockwaveSystem);
        _poisonSystem = new PoisonDamageDisplaySystem(_world.EntityManager, GraphicsDevice, _spriteBatch, Content);
        _world.AddSystem(_poisonSystem);
        _world.AddSystem(new CursorEmptySelectDisplaySystem(_world.EntityManager, GraphicsDevice));
        _world.AddSystem(new UISelectDisplaySystem(_world.EntityManager, GraphicsDevice));
        _world.AddSystem(new JigglePulseDisplaySystem(_world.EntityManager));

        // Mark persistent entities
        _world.AddComponent(sceneEntity, new DontDestroyOnLoad());
        var cvsEntity = _world.EntityManager.GetEntity("CardVisualSettings");
        if (cvsEntity != null)
        {
            _world.AddComponent(cvsEntity, new DontDestroyOnLoad());
        }
        // Allocate render targets
        AllocateRenderTargets();
        // TODO: use this.Content to load your game content here
    }
    
    protected override void Update(GameTime gameTime)
    {
        WindowIsActive = IsActive;
        // Hide system mouse cursor when no gamepad is connected (custom cursor will be drawn)
        var gp = GamePad.GetState(PlayerIndex.One);
        IsMouseVisible = gp.IsConnected;
        var kb = Keyboard.GetState();
        if ((kb.IsKeyDown(Keys.Escape) && kb.IsKeyDown(Keys.LeftShift)))
            Exit();

        if (kb.IsKeyDown(Keys.F11) && !_prevKeyboard.IsKeyDown(Keys.F11))
        {
            ToggleFullScreen();
        }

        // Global debug menu toggle so it's available in the main menu too
        if (kb.IsKeyDown(Keys.D) && !_prevKeyboard.IsKeyDown(Keys.D) && kb.IsKeyDown(Keys.LeftShift))
        {
            ToggleDebugMenu();
            var debugMenu = _world.EntityManager.GetEntitiesWithComponent<DebugMenu>().FirstOrDefault();
            if (debugMenu != null)
            {
                debugMenu.GetComponent<UIElement>().IsInteractable = !debugMenu.GetComponent<UIElement>().IsInteractable;
                Console.WriteLine($"Debug menu interactable: {debugMenu.GetComponent<UIElement>().IsInteractable}");
            }
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

    public RasterizerState GetRasterizerState() => _spriteRasterizer;

    protected override void Draw(GameTime gameTime)
    {
        // Draw to virtual RT first
        EnsureRenderTargetsMatchVirtual();
        
        GraphicsDevice.SetRenderTarget(_sceneRt);
        GraphicsDevice.Clear(Color.CornflowerBlue);

        DrawScene();

        // Process effects
        bool hasPoison = _poisonSystem != null && _poisonSystem.HasActivePoison;
        bool hasCircularWaves = _shockwaveSystem != null && _shockwaveSystem.HasActiveWaves;
        bool hasRectangularWaves = _rectangularShockwaveSystem != null && _rectangularShockwaveSystem.HasActiveWaves;
        
        Texture2D finalTexture = _sceneRt;

        if (hasPoison || hasCircularWaves || hasRectangularWaves)
        {
            // Composite effects in order: Poison → Circular Shockwaves → Rectangular Shockwaves
            
            // Apply poison first if active
            if (hasPoison)
            {
                RenderTarget2D next = (hasCircularWaves || hasRectangularWaves) ? _ppB : _ppA;
                _poisonSystem.Composite(finalTexture, _ppA, next);
                finalTexture = next;
            }
            
            // Apply circular shockwaves second if any
            if (hasCircularWaves)
            {
                RenderTarget2D dest = (finalTexture == _ppA) ? _ppB : _ppA;
                _shockwaveSystem.Composite(finalTexture, _ppA, _ppB, dest);
                finalTexture = dest;
            }
            
            // Apply rectangular shockwaves on top if any
            if (hasRectangularWaves)
            {
                RenderTarget2D dest = (finalTexture == _ppA) ? _ppB : _ppA;
                _rectangularShockwaveSystem.Composite(finalTexture, _ppA, _ppB, dest);
                finalTexture = dest;
            }
        }

        // Final draw to backbuffer with letterboxing
        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.Clear(Color.Black);
        
        _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp);
        _spriteBatch.Draw(finalTexture, RenderDestination, Color.White);
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void DrawScene()
    {
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
                // Additive trail pass for card move trails
                _spriteBatch.End();
				_spriteBatch.Begin(SpriteSortMode.Immediate, AdditiveAlphaOne, SamplerState.AnisotropicClamp, DepthStencilState.None, _spriteRasterizer);
                _battleSceneSystem.DrawAdditive();
                _spriteBatch.End();
                // Resume normal alpha-blend UI drawing state
                _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, _spriteRasterizer);
                break;
            }
            case SceneId.Location:
            {
                FrameProfiler.Measure("LocationSceneSystem.Draw", _locationSceneSystem.Draw);
				FrameProfiler.Measure("CurrencyDisplaySystem.Draw", _currencyDisplaySystem.Draw);
                break;
            }
            case SceneId.Shop:
            {
                FrameProfiler.Measure("ShopSceneSystem.Draw", _shopSceneSystem.Draw);
				FrameProfiler.Measure("CurrencyDisplaySystem.Draw", _currencyDisplaySystem.Draw);
                break;
            }
            case SceneId.WorldMap:
            {
                // Draw location tiles with crisp pixel sampling
                _spriteBatch.End();
                _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, _spriteRasterizer);
                FrameProfiler.Measure("UIElementHighlightSystem.Draw", _uiElementHighlightSystem.Draw);
                // FrameProfiler.Measure("LocationSelectDisplaySystem.Draw", _worldMapSystem.Draw);
                _spriteBatch.End();
                // Resume default sampling for overlays and other UI
                _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, _spriteRasterizer);
                
                break;
            }
        }
        FrameProfiler.Measure("HotKeySystem.Draw", _hotKeySystem.Draw);
        FrameProfiler.Measure("HotKeyProgressRingSystem.Draw", _hotKeyProgressRingSystem.Draw);
        FrameProfiler.Measure("LocationNameDisplaySystem.Draw", _locationNameDisplaySystem.Draw);
        FrameProfiler.Measure("TooltipDisplaySystem.Draw", _tooltipTextDisplaySystem.Draw);
        FrameProfiler.Measure("HintTooltipDisplaySystem.Draw", _hintTooltipDisplaySystem.Draw);
        FrameProfiler.Measure("CardTooltipDisplaySystem.Draw", _cardTooltipDisplaySystem.Draw);
        FrameProfiler.Measure("ProfilerSystem.Draw", _profilerSystem.Draw);
        FrameProfiler.Measure("DebugMenuSystem.Draw", _debugMenuSystem.Draw);
        FrameProfiler.Measure("EntityListOverlaySystem.Draw", _entityListOverlaySystem.Draw);
        FrameProfiler.Measure("DialogDisplaySystem.Draw", _dialogDisplaySystem.Draw);
        FrameProfiler.Measure("TransitionDisplaySystem.Draw", _transitionDisplaySystem.Draw);
        FrameProfiler.Measure("UIElementBorderDebugSystem.Draw", _uiElementBorderDebugSystem.Draw);
        FrameProfiler.Measure("WorldMapCursorSystem.Draw", _cursorSystem.Draw);
        _spriteBatch.End();
    }

	protected override void UnloadContent()
	{
		try { _currencyDisplaySystem?.Dispose(); } catch { }
		base.UnloadContent();
	}

    private void AllocateRenderTargets()
    {
        _sceneRt = new RenderTarget2D(GraphicsDevice, VirtualWidth, VirtualHeight, false, SurfaceFormat.Color, DepthFormat.None);
        _ppA = new RenderTarget2D(GraphicsDevice, VirtualWidth, VirtualHeight, false, SurfaceFormat.Color, DepthFormat.None);
        _ppB = new RenderTarget2D(GraphicsDevice, VirtualWidth, VirtualHeight, false, SurfaceFormat.Color, DepthFormat.None);
    }

    private void EnsureRenderTargetsMatchVirtual()
    {
        if (_sceneRt == null || _sceneRt.Width != VirtualWidth || _sceneRt.Height != VirtualHeight)
        {
            _sceneRt?.Dispose();
            _ppA?.Dispose();
            _ppB?.Dispose();
            AllocateRenderTargets();
        }
    }
    
    private void CalculateRenderDestination()
    {
        Point screenSize = new Point(GraphicsDevice.PresentationParameters.BackBufferWidth, GraphicsDevice.PresentationParameters.BackBufferHeight);
        float screenAspect = (float)screenSize.X / screenSize.Y;
        float virtualAspect = (float)VirtualWidth / VirtualHeight;

        int width, height;
        if (screenAspect > virtualAspect)
        {
            // Screen is wider than virtual: pillarbox
            height = screenSize.Y;
            width = (int)(height * virtualAspect);
        }
        else
        {
            // Screen is taller than virtual (or equal): letterbox
            width = screenSize.X;
            height = (int)(width / virtualAspect);
        }

        int x = (screenSize.X - width) / 2;
        int y = (screenSize.Y - height) / 2;
        RenderDestination = new Rectangle(x, y, width, height);
    }

    private void ToggleFullScreen()
    {
        if (!_graphics.IsFullScreen)
        {
            // Borderless full screen: set to desktop resolution to cover taskbar and top bar
            _graphics.HardwareModeSwitch = false;
            _graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            _graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            _graphics.IsFullScreen = true;
        }
        else
        {
            // Windowed mode: reset to virtual resolution
            _graphics.IsFullScreen = false;
            _graphics.PreferredBackBufferWidth = VirtualWidth;
            _graphics.PreferredBackBufferHeight = VirtualHeight;
        }
        _graphics.ApplyChanges();
        
        // Recalculate rendering destination to stretch and letterbox the 1920x1080 content
        CalculateRenderDestination();
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
