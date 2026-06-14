using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Input;
using Crusaders30XX.ECS.Systems;
using Microsoft.Xna.Framework;
using Xunit;
using EcsSystem = Crusaders30XX.ECS.Core.System;

namespace Crusaders30XX.Tests;

public class PlayerInputArchitectureTests
{
    [Fact]
    public void System_manager_executes_declared_phases_in_order()
    {
        var entityManager = new EntityManager();
        var manager = new SystemManager(entityManager);
        var calls = new List<string>();

        manager.AddSystem(new RecordingSystem(entityManager, "gameplay", calls));
        manager.AddSystem(new RecordingSystem(entityManager, "input", calls), SystemUpdatePhase.Input);
        manager.AddSystem(new RecordingSystem(entityManager, "presentation", calls), SystemUpdatePhase.Presentation);
        manager.AddSystem(new RecordingSystem(entityManager, "interaction", calls), SystemUpdatePhase.Interaction);
        manager.AddLateSystem(new RecordingSystem(entityManager, "late", calls));

        manager.Update(new GameTime());
        manager.LateUpdate(new GameTime());

        Assert.Equal(
            new[] { "input", "interaction", "gameplay", "presentation", "late" },
            calls);
        Assert.Equal(5, manager.GetAllSystems().Count());
    }

    [Fact]
    public void Input_frame_button_masks_are_immutable_and_report_edges()
    {
        PlayerButtonMask primary = PlayerInputFrame.Mask(PlayerButton.Primary);
        var pressed = Frame(
            down: primary,
            pressed: primary);
        var held = Frame(down: primary);
        var released = Frame(released: primary);

        Assert.True(pressed.IsDown(PlayerButton.Primary));
        Assert.True(pressed.WasPressed(PlayerButton.Primary));
        Assert.False(held.WasPressed(PlayerButton.Primary));
        Assert.True(released.WasReleased(PlayerButton.Primary));

        PlayerInputFrame copy = pressed with { Sequence = 2 };
        Assert.True(pressed.WasPressed(PlayerButton.Primary));
        Assert.Equal(1, pressed.Sequence);
        Assert.Equal(2, copy.Sequence);
    }

    [Fact]
    public void Player_input_system_publishes_device_transitions_and_top_cursor_target()
    {
        EventManager.Clear();
        var entityManager = new EntityManager();
        Entity scene = entityManager.CreateEntity("Scene");
        entityManager.AddComponent(scene, new SceneState());

        Entity lower = CreateUi(entityManager, "Lower", 10, new Rectangle(0, 0, 100, 100));
        Entity upper = CreateUi(entityManager, "Upper", 20, new Rectangle(0, 0, 100, 100));
        var source = new FakeInputSource(
            Frame(sequence: 1, pointer: new Vector2(50, 50)),
            Frame(
                sequence: 2,
                pointer: new Vector2(50, 50),
                device: PlayerInputDevice.Gamepad,
                previousDevice: PlayerInputDevice.KeyboardMouse,
                gamepadConnected: true));
        var events = new List<PlayerInputFrame>();
        EventManager.Subscribe<PlayerInputEvent>(e => events.Add(e.Frame));
        var system = new PlayerInputSystem(entityManager, source);

        system.Update(new GameTime());
        PlayerInputState state = entityManager
            .GetEntitiesWithComponent<PlayerInputState>()
            .Single()
            .GetComponent<PlayerInputState>();
        Assert.Same(upper, state.CursorTarget.Entity);
        Assert.NotSame(lower, state.CursorTarget.Entity);

        system.Update(new GameTime());
        Assert.Equal(2, events.Count);
        Assert.True(events[1].DeviceChanged);
        Assert.Equal(PlayerInputDevice.Gamepad, events[1].Device);
        EventManager.Clear();
    }

    [Fact]
    public void Context_resolution_uses_priority_and_diagnostic_region_override()
    {
        var entityManager = new EntityManager();
        Entity overlayRoot = entityManager.CreateEntity("Overlay");
        entityManager.AddComponent(overlayRoot, new InputContext
        {
            Id = "overlay.test",
            Priority = 700,
            IsActive = true,
        });

        Entity diagnosticRoot = entityManager.CreateEntity("Diagnostic");
        entityManager.AddComponent(diagnosticRoot, new InputContext
        {
            Id = "diagnostic.test",
            Priority = 900,
            IsActive = true,
            IsDiagnostic = true,
        });
        Entity diagnosticControl = CreateUi(
            entityManager,
            "DiagnosticControl",
            100,
            new Rectangle(10, 10, 50, 50));
        entityManager.AddComponent(diagnosticControl, new InputContextMember
        {
            ContextId = "diagnostic.test",
        });

        Assert.Equal(
            "diagnostic.test",
            InputContextResolver.ResolveCursorContext(entityManager, new Vector2(20, 20)));
        Assert.Equal(
            "overlay.test",
            InputContextResolver.ResolveCursorContext(entityManager, new Vector2(200, 200)));
        Assert.Equal(
            "overlay.test",
            InputContextResolver.ResolveCommandContext(entityManager));
    }

    [Fact]
    public void Hotkey_hold_completes_once_and_cancels_when_ineligible()
    {
        var entityManager = new EntityManager();
        Entity entity = entityManager.CreateEntity("Hold");
        var tracker = new HotKeyHoldTracker();

        tracker.Start(entity, FaceButton.X);
        Assert.False(tracker.Advance(entity, 0.4f, 0.75f, true));
        Assert.True(tracker.Advance(entity, 0.4f, 0.75f, true));
        Assert.False(tracker.Advance(entity, 1f, 0.75f, true));

        tracker.Start(entity, FaceButton.X);
        Assert.False(tracker.Advance(entity, 0.2f, 0.75f, false));
        Assert.Empty(tracker.Progress);
    }

    [Fact]
    public void MonoGame_input_is_referenced_only_by_the_hardware_adapter()
    {
        string root = FindRepositoryRoot();
        string adapter = Path.GetFullPath(
            Path.Combine(root, "ECS", "Input", "MonoGamePlayerInputAdapter.cs"));
        string[] forbidden =
        {
            "Microsoft.Xna.Framework.Input",
            "Keyboard.GetState",
            "Mouse.GetState",
            "GamePad.GetState",
            "GamePad.GetCapabilities",
            "GamePad.SetVibration",
        };

        var violations = Directory
            .EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}tests{Path.DirectorySeparatorChar}"))
            .Where(path => Path.GetFullPath(path) != adapter)
            .Where(path =>
            {
                string text = File.ReadAllText(path);
                return forbidden.Any(text.Contains);
            })
            .Select(path => Path.GetRelativePath(root, path))
            .ToList();

        Assert.Empty(violations);
    }

    private static Entity CreateUi(
        EntityManager entityManager,
        string name,
        int zOrder,
        Rectangle bounds)
    {
        Entity entity = entityManager.CreateEntity(name);
        entityManager.AddComponent(entity, new Transform
        {
            Position = Vector2.Zero,
            ZOrder = zOrder,
        });
        entityManager.AddComponent(entity, new UIElement
        {
            Bounds = bounds,
            IsInteractable = true,
        });
        return entity;
    }

    private static PlayerInputFrame Frame(
        long sequence = 1,
        Vector2 pointer = default,
        PlayerInputDevice device = PlayerInputDevice.KeyboardMouse,
        PlayerInputDevice previousDevice = PlayerInputDevice.KeyboardMouse,
        bool gamepadConnected = false,
        PlayerButtonMask down = PlayerButtonMask.None,
        PlayerButtonMask pressed = PlayerButtonMask.None,
        PlayerButtonMask released = PlayerButtonMask.None)
    {
        return new PlayerInputFrame(
            sequence,
            true,
            gamepadConnected,
            device,
            previousDevice,
            GamepadGlyphStyle.Xbox,
            pointer,
            Vector2.Zero,
            0f,
            Vector2.Zero,
            Vector2.Zero,
            0f,
            0f,
            down,
            pressed,
            released);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo directory = new(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Crusaders30XX.csproj")))
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate repository root.");
    }

    private sealed class FakeInputSource : IPlayerInputSource
    {
        private readonly Queue<PlayerInputFrame> _frames;

        public FakeInputSource(params PlayerInputFrame[] frames)
        {
            _frames = new Queue<PlayerInputFrame>(frames);
        }

        public PlayerInputFrame Capture(
            bool isWindowActive,
            Rectangle renderDestination,
            int virtualWidth,
            int virtualHeight)
        {
            return _frames.Dequeue();
        }

        public void SetVibration(float lowFrequency, float highFrequency)
        {
        }
    }

    private sealed class RecordingSystem : EcsSystem
    {
        private readonly string _name;
        private readonly List<string> _calls;

        public RecordingSystem(
            EntityManager entityManager,
            string name,
            List<string> calls)
            : base(entityManager)
        {
            _name = name;
            _calls = calls;
        }

        public override void Update(GameTime gameTime)
        {
            _calls.Add(_name);
        }

        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return Enumerable.Empty<Entity>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
        }
    }
}
