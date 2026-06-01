using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Crusaders30XX.ECS.Core;
// duplicate removed
using Crusaders30XX.ECS.Factories;
using Crusaders30XX.ECS.Services;
using Crusaders30XX.ECS.Events;
using System;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.Diagnostics.Snapshots;
using Crusaders30XX.ECS.Components;
using System.Linq;
using Crusaders30XX.ECS.Systems;
using Crusaders30XX.ECS.Singletons;
using Crusaders30XX.ECS.Data.Achievements;
using System.IO;

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
    private CardDisplaySystemV2 _cardDisplaySystemV2;
    private InputSystem _inputSystem;
	private CurrencyDisplaySystem _currencyDisplaySystem;

    private KeyboardState _prevKeyboard;

    private TitleMenuDisplaySystem _titleMenuDisplaySystem;
    private BattleSceneSystem _battleSceneSystem;
    private LocationSceneSystem _locationSceneSystem;
    private ShopSceneSystem _shopSceneSystem;
    private CustomizationRootSystem _customizationRootSystem;
    private CustomizationV2RootSystem _customizationV2RootSystem;
    private AchievementSceneSystem _achievementSceneSystem;
    private TooltipTextDisplaySystem _tooltipTextDisplaySystem;
    private HintTooltipDisplaySystem _hintTooltipDisplaySystem;
    private CardTooltipDisplaySystem _cardTooltipDisplaySystem;
    private ProfilerSystem _profilerSystem;
    // private LocationSelectDisplaySystem _worldMapSystem;
    private PositionTweenSystem _positionTweenSystem;
    private ParallaxLayerSystem _parallaxLayerSystem;
    private UIElementHighlightSystem _uiElementHighlightSystem;
    private CursorSystem _cursorSystem;
    private CursorTrailDisplaySystem _cursorTrailDisplaySystem;
    private HotKeySystem _hotKeySystem;
    private HotKeyProgressRingSystem _hotKeyProgressRingSystem;
    private UIElementBorderDebugSystem _uiElementBorderDebugSystem;
    private DialogDisplaySystem _dialogDisplaySystem;
    private DebugCommandSystem _debugCommandSystem;
    private LocationNameDisplaySystem _locationNameDisplaySystem;
    private QuestStartSystem _questStartSystem;
    private ShopStartSystem _shopStartSystem;
    private TreasureStartSystem _treasureStartSystem;
    private RewardModalDisplaySystem _rewardModalDisplaySystem;
    private DisplaySnapshotHost _snapshotHost;
    private readonly DisplaySnapshotLaunchOptions _snapshotOptions;
#if DEBUG
    private bool _writePerfReportOnExit;
#endif
    
    // ECS System
    private World _world;

    // Shockwave integration
    private ShockwaveDisplaySystem _shockwaveSystem;
    private RectangularShockwaveDisplaySystem _rectangularShockwaveSystem;
    private PoisonDamageDisplaySystem _poisonSystem;
    private AlertDisplaySystem _alertDisplaySystem;
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
    
    public Game1(DisplaySnapshotLaunchOptions snapshotOptions = null)
    {
        _snapshotOptions = snapshotOptions;
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
        LoggingService.Initialize();
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

        // Initialize Achievement system
        AchievementManager.Initialize(_world.EntityManager);

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
        _battleSceneSystem = new BattleSceneSystem(_world.EntityManager, _world.SystemManager, _world, GraphicsDevice, _spriteBatch, Content);
        _locationSceneSystem = new LocationSceneSystem(_world.EntityManager, _world.SystemManager, _world, GraphicsDevice, _spriteBatch, Content);
        _shopSceneSystem = new ShopSceneSystem(_world.EntityManager, _world.SystemManager, _world, GraphicsDevice, _spriteBatch, Content);
        _customizationRootSystem = new CustomizationRootSystem(_world.EntityManager, _world.SystemManager, _world, GraphicsDevice, _spriteBatch, Content);
        _customizationV2RootSystem = new CustomizationV2RootSystem(_world.EntityManager, _world.SystemManager, _world, GraphicsDevice, _spriteBatch);
        _achievementSceneSystem = new AchievementSceneSystem(_world.EntityManager, _world.SystemManager, _world, GraphicsDevice, _spriteBatch, Content);
        _debugMenuSystem = new DebugMenuSystem(_world.EntityManager, GraphicsDevice, _spriteBatch, _world.SystemManager);
        _entityListOverlaySystem = new EntityListOverlaySystem(_world.EntityManager, GraphicsDevice, _spriteBatch);
        _transitionDisplaySystem = new TransitionDisplaySystem(_world.EntityManager, GraphicsDevice, _spriteBatch);
        _cardDisplaySystem = new CardDisplaySystem(_world.EntityManager, GraphicsDevice, _spriteBatch, Content);
        _cardDisplaySystemV2 = new CardDisplaySystemV2(_world.EntityManager, GraphicsDevice, _spriteBatch, Content);
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
        _cursorTrailDisplaySystem = new CursorTrailDisplaySystem(_world.EntityManager, GraphicsDevice, _spriteBatch, Content);
        _hotKeySystem = new HotKeySystem(_world.EntityManager, GraphicsDevice, _spriteBatch);
        _hotKeyProgressRingSystem = new HotKeyProgressRingSystem(_world.EntityManager, GraphicsDevice, _spriteBatch, _world.SystemManager);
        _positionTweenSystem = new PositionTweenSystem(_world.EntityManager);
        _parallaxLayerSystem = new ParallaxLayerSystem(_world.EntityManager, GraphicsDevice);
        _uiElementBorderDebugSystem = new UIElementBorderDebugSystem(_world.EntityManager, GraphicsDevice, _spriteBatch);
        _uiElementHighlightSystem = new UIElementHighlightSystem(_world.EntityManager, GraphicsDevice, _spriteBatch);
        _debugCommandSystem = new DebugCommandSystem(_world.EntityManager);
        _world.AddSystem(_titleMenuDisplaySystem);
        _world.AddSystem(_battleSceneSystem);
        _world.AddSystem(_locationSceneSystem);
        _world.AddSystem(_shopSceneSystem);
        _world.AddSystem(_customizationRootSystem);
        _world.AddSystem(_customizationV2RootSystem);
        _world.AddSystem(_achievementSceneSystem);
        _world.AddSystem(new TimerSchedulerSystem(_world.EntityManager));
        _world.AddSystem(_debugMenuSystem);
        _world.AddSystem(_entityListOverlaySystem);
        _world.AddSystem(_transitionDisplaySystem);
        _world.AddSystem(_cardDisplaySystem);
        _world.AddSystem(_cardDisplaySystemV2);
        _world.AddSystem(_dialogDisplaySystem);
        _world.AddSystem(_inputSystem);
        _world.AddSystem(_tooltipTextDisplaySystem);
        _world.AddSystem(_hintTooltipDisplaySystem);
        _world.AddSystem(_cardTooltipDisplaySystem);
        _world.AddSystem(_profilerSystem);
        _world.AddSystem(_locationNameDisplaySystem);
        // _world.AddSystem(_worldMapSystem);
        _world.AddSystem(_cursorSystem);
        _world.AddSystem(_cursorTrailDisplaySystem);
        _world.AddSystem(_hotKeySystem);
        _world.AddSystem(_hotKeyProgressRingSystem);
        _world.AddLateSystem(_positionTweenSystem);
        _world.AddLateSystem(_parallaxLayerSystem);
        _world.AddSystem(_uiElementBorderDebugSystem);
        _world.AddSystem(_debugCommandSystem);
        _questStartSystem = new QuestStartSystem(_world.EntityManager);
        _world.AddSystem(_questStartSystem);
        _shopStartSystem = new ShopStartSystem(_world.EntityManager);
        _world.AddSystem(_shopStartSystem);
        _treasureStartSystem = new TreasureStartSystem(_world.EntityManager);
        _world.AddSystem(_treasureStartSystem);
        _rewardModalDisplaySystem = new RewardModalDisplaySystem(_world.EntityManager, GraphicsDevice, _spriteBatch, Content);
        _world.AddSystem(_rewardModalDisplaySystem);
        _world.AddSystem(new RunDeckLifecycleSystem(_world.EntityManager));
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
        _alertDisplaySystem = new AlertDisplaySystem(_world.EntityManager, GraphicsDevice, _spriteBatch);
        _world.AddSystem(_alertDisplaySystem);

        // Mark persistent entities
        _world.AddComponent(sceneEntity, new DontDestroyOnLoad());
        var cvsEntity = _world.EntityManager.GetEntity("CardVisualSettings");
        if (cvsEntity != null)
        {
            _world.AddComponent(cvsEntity, new DontDestroyOnLoad());
        }
        // Allocate render targets
        AllocateRenderTargets();

        _snapshotHost = DisplaySnapshotHost.TryCreate(_snapshotOptions, this, GraphicsDevice, Content);
        _snapshotHost?.OnGameReady(_world, sceneEntity, _spriteBatch);
    }
    
    protected override void Update(GameTime gameTime)
    {
        var kb = Keyboard.GetState();
        if (kb.IsKeyDown(Keys.Escape) && (kb.IsKeyDown(Keys.LeftShift) || kb.IsKeyDown(Keys.RightShift)))
        {
#if DEBUG
            _writePerfReportOnExit = true;
#endif
            Exit();
            return;
        }

        FrameProfiler.BeginGameFrame(gameTime, _snapshotHost?.IsActive == true);

#if DEBUG
        FrameProfiler.MeasureInclusive("Game1.Update", () => RunUpdate(gameTime, kb));
#else
        RunUpdate(gameTime, kb);
#endif
    }

    private void RunUpdate(GameTime gameTime, KeyboardState kb)
    {
        LoggingService.Tick();
        WindowIsActive = IsActive;
        var gp = GamePad.GetState(PlayerIndex.One);
        IsMouseVisible = gp.IsConnected;

        if (_snapshotHost?.IsActive != true)
        {
            if (kb.IsKeyDown(Keys.F11) && !_prevKeyboard.IsKeyDown(Keys.F11))
            {
                ToggleFullScreen();
            }

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

            if ((kb.IsKeyDown(Keys.LeftShift) || kb.IsKeyDown(Keys.RightShift)) && kb.IsKeyDown(Keys.W) && !_prevKeyboard.IsKeyDown(Keys.W))
            {
                ToggleEntityListOverlay();
            }

            if ((kb.IsKeyDown(Keys.LeftShift) || kb.IsKeyDown(Keys.RightShift)) && kb.IsKeyDown(Keys.K) && !_prevKeyboard.IsKeyDown(Keys.K))
            {
                _debugCommandSystem.Debug_PlayerDealDamage(999);
            }

            if (kb.IsKeyDown(Keys.P) && !_prevKeyboard.IsKeyDown(Keys.P))
            {
                var e = _world.EntityManager.GetEntitiesWithComponent<ProfilerOverlay>().FirstOrDefault();
                if (e == null)
                {
                    e = _world.EntityManager.CreateEntity("ProfilerOverlay");
                    _world.EntityManager.AddComponent(e, new ProfilerOverlay { IsOpen = true });
                }
                else
                {
                    var p = e.GetComponent<ProfilerOverlay>();
                    p.IsOpen = !p.IsOpen;
                }
            }
        }

        _world.Update(gameTime);

#if DEBUG
        var sceneEntity = _world.EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault();
        var sceneId = sceneEntity?.GetComponent<SceneState>()?.Current ?? SceneId.None;
        FrameProfiler.SetActiveScene(sceneId);
#endif

        _prevKeyboard = kb;
        base.Update(gameTime);
    }

    public RasterizerState GetRasterizerState() => _spriteRasterizer;

    protected override void Draw(GameTime gameTime)
    {
#if DEBUG
        FrameProfiler.MeasureInclusive("Game1.Draw", () => DrawGame(gameTime));
        FrameProfiler.EndGameFrame(gameTime);
#else
        DrawGame(gameTime);
#endif
    }

    private void DrawGame(GameTime gameTime)
    {
#if DEBUG
        FrameProfiler.Measure("Game1.Draw.SceneSetupAndDrawScene", DrawSceneSetup);
        Texture2D finalTexture = _sceneRt;
        bool shouldPresent = false;
        FrameProfiler.Measure("Game1.Draw.ShaderComposite", () =>
        {
            shouldPresent = TryCompositeDrawEffects(out finalTexture);
        });
        if (!shouldPresent)
        {
            return;
        }
        FrameProfiler.Measure("Game1.Draw.Present", () => PresentToBackbuffer(finalTexture, gameTime));
#else
        DrawSceneSetup();
        if (!TryCompositeDrawEffects(out Texture2D finalTexture))
        {
            return;
        }
        PresentToBackbuffer(finalTexture, gameTime);
#endif
    }

    private void DrawSceneSetup()
    {
        EnsureRenderTargetsMatchVirtual();
        GraphicsDevice.SetRenderTarget(_sceneRt);
        GraphicsDevice.Clear(_snapshotHost?.IsActive == true ? Color.Black : Color.CornflowerBlue);
        DrawScene();
    }

    private bool TryCompositeDrawEffects(out Texture2D finalTexture)
    {
        bool hasPoison = _poisonSystem != null && _poisonSystem.HasActivePoison;
        bool hasCircularWaves = _shockwaveSystem != null && _shockwaveSystem.HasActiveWaves;
        bool hasRectangularWaves = _rectangularShockwaveSystem != null && _rectangularShockwaveSystem.HasActiveWaves;

        finalTexture = _sceneRt;

        if (ShaderRuntimeOptions.ShadersEnabled && (hasPoison || hasCircularWaves || hasRectangularWaves))
        {
            if (hasPoison)
            {
                RenderTarget2D next = (hasCircularWaves || hasRectangularWaves) ? _ppB : _ppA;
                _poisonSystem.Composite(finalTexture, _ppA, next);
                finalTexture = next;
            }

            if (hasCircularWaves)
            {
                RenderTarget2D dest = (finalTexture == _ppA) ? _ppB : _ppA;
                _shockwaveSystem.Composite(finalTexture, _ppA, _ppB, dest);
                finalTexture = dest;
            }

            if (hasRectangularWaves)
            {
                RenderTarget2D dest = (finalTexture == _ppA) ? _ppB : _ppA;
                _rectangularShockwaveSystem.Composite(finalTexture, _ppA, _ppB, dest);
                finalTexture = dest;
            }
        }

        if (_snapshotHost?.IsActive == true && _snapshotHost.TickAfterDraw(_sceneRt))
        {
            return false;
        }

        return true;
    }

    private void PresentToBackbuffer(Texture2D finalTexture, GameTime gameTime)
    {
        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.Clear(Color.Black);

        _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp);
        _spriteBatch.Draw(finalTexture, RenderDestination, Color.White);
        _spriteBatch.End();

        base.Draw(gameTime);
    }

#if DEBUG
    private void MeasureInclusiveSceneDraw(string name, Action draw) =>
        FrameProfiler.MeasureInclusive(name, draw);
#else
    private void MeasureInclusiveSceneDraw(string name, Action draw) => draw();
#endif

    private void DrawScene()
    {
        _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, _spriteRasterizer);

        if (_snapshotHost?.IsActive == true)
        {
            _snapshotHost.DrawScene(_spriteBatch);
            _spriteBatch.End();
            return;
        }

        // Delegate drawing to active parent systems
        var scene = _world.EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault().GetComponent<SceneState>();
        switch(scene.Current)
        {
            case SceneId.TitleMenu:
            {
                FrameProfiler.Measure("TitleMenuDisplaySystem.Draw", _titleMenuDisplaySystem.Draw);
                break;
            }
            case SceneId.Customization:
            {
                MeasureInclusiveSceneDraw("CustomizationRootSystem.Draw", _customizationRootSystem.Draw);
                break;
            }
            case SceneId.CustomizationV2:
            {
                MeasureInclusiveSceneDraw("CustomizationV2RootSystem.Draw", _customizationV2RootSystem.Draw);
                break;
            }
            case SceneId.Battle:
            {
                MeasureInclusiveSceneDraw("BattleSceneSystem.Draw", _battleSceneSystem.Draw);
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
                MeasureInclusiveSceneDraw("LocationSceneSystem.Draw", _locationSceneSystem.Draw);
				FrameProfiler.Measure("CurrencyDisplaySystem.Draw", _currencyDisplaySystem.Draw);
                break;
            }
            case SceneId.Shop:
            {
                MeasureInclusiveSceneDraw("ShopSceneSystem.Draw", _shopSceneSystem.Draw);
				FrameProfiler.Measure("CurrencyDisplaySystem.Draw", _currencyDisplaySystem.Draw);
                break;
            }
            case SceneId.Achievement:
            {
                MeasureInclusiveSceneDraw("AchievementSceneSystem.Draw", _achievementSceneSystem.Draw);
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
        FrameProfiler.Measure("RewardModalDisplaySystem.Draw", _rewardModalDisplaySystem.Draw);
        FrameProfiler.Measure("TooltipDisplaySystem.Draw", _tooltipTextDisplaySystem.Draw);
        FrameProfiler.Measure("HintTooltipDisplaySystem.Draw", _hintTooltipDisplaySystem.Draw);
        FrameProfiler.Measure("CardTooltipDisplaySystem.Draw", _cardTooltipDisplaySystem.Draw);
        FrameProfiler.Measure("AlertDisplaySystem.Draw", _alertDisplaySystem.Draw);
        FrameProfiler.Measure("ProfilerSystem.Draw", _profilerSystem.Draw);
        FrameProfiler.Measure("DebugMenuSystem.Draw", _debugMenuSystem.Draw);
        FrameProfiler.Measure("EntityListOverlaySystem.Draw", _entityListOverlaySystem.Draw);
        FrameProfiler.Measure("DialogDisplaySystem.Draw", _dialogDisplaySystem.Draw);
        FrameProfiler.Measure("TransitionDisplaySystem.Draw", _transitionDisplaySystem.Draw);
        FrameProfiler.Measure("UIElementBorderDebugSystem.Draw", _uiElementBorderDebugSystem.Draw);
        // Cursor blur trail (additive pass before cursor) — skip in card debug mode
        _spriteBatch.End();
        if (_snapshotHost?.ShouldSkipGlobalOverlays != true)
        {
            if (ShaderRuntimeOptions.ShadersEnabled)
            {
                _cursorTrailDisplaySystem.DrawTrail(_sceneRt);
            }

            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, _spriteRasterizer);
            FrameProfiler.Measure("WorldMapCursorSystem.Draw", _cursorSystem.Draw);
            _spriteBatch.End();
        }
    }

	protected override void UnloadContent()
	{
#if DEBUG
		if (_writePerfReportOnExit)
		{
			try
			{
				var sceneEntity = _world.EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault();
				var sceneAtQuit = sceneEntity?.GetComponent<SceneState>()?.Current ?? SceneId.None;
				FrameProfiler.WriteReport("logs/performance-report.txt", sceneAtQuit, ShaderRuntimeOptions.ShadersEnabled);
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"[FrameProfiler] Report failed: {ex.Message}");
			}
		}
#endif
		LoggingService.Flush();
		try { _currencyDisplaySystem?.Dispose(); } catch { }
		base.UnloadContent();
	}

    private void AllocateRenderTargets()
    {
        _sceneRt = new RenderTarget2D(GraphicsDevice, VirtualWidth, VirtualHeight, false,
            SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
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
            // All interactive UIElements are suppressed/restored together via their suppress state
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
