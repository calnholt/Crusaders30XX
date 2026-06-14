using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Input;
using Crusaders30XX.ECS.Services;
using Crusaders30XX.ECS.Singletons;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Systems
{
    [DebugTab("Player Input")]
    public class PlayerInputSystem : Core.System
    {
        private readonly IPlayerInputSource _inputSource;
        private PlayerInputState _state;
        private Vector2 _cursorPosition;
        private int _lastViewportWidth = -1;
        private int _lastViewportHeight = -1;
        private float _rumbleTimeRemaining;
        private Entity _previousHoverTarget;
        private bool _inputEnabled = true;

        [DebugEditable(DisplayName = "Cursor Radius (px)", Step = 1f, Min = 2f, Max = 256f)]
        public int CursorRadius { get; set; } = 26;

        [DebugEditable(DisplayName = "Base Speed (px/s)", Step = 10f, Min = 50f, Max = 4000f)]
        public float BaseSpeed { get; set; } = 1450f;

        [DebugEditable(DisplayName = "Analog Deadzone", Step = 0.01f, Min = 0f, Max = 0.5f)]
        public float Deadzone { get; set; } = 0.08f;

        [DebugEditable(DisplayName = "Speed Exponent", Step = 0.05f, Min = 0.25f, Max = 3f)]
        public float SpeedExponent { get; set; } = 1f;

        [DebugEditable(DisplayName = "Max Multiplier", Step = 0.1f, Min = 0.5f, Max = 5f)]
        public float MaxMultiplier { get; set; } = 1f;

        [DebugEditable(DisplayName = "RT Speed Multiplier", Step = 0.1f, Min = 0.1f, Max = 10f)]
        public float TriggerSpeedMultiplier { get; set; } = 2f;

        [DebugEditable(DisplayName = "Rumble Duration (s)", Step = 0.01f, Min = 0f, Max = 1f)]
        public float RumbleDurationSeconds { get; set; } = 0.04f;

        [DebugEditable(DisplayName = "Rumble Low Intensity", Step = 0.05f, Min = 0f, Max = 1f)]
        public float RumbleLow { get; set; } = 0.3f;

        [DebugEditable(DisplayName = "Rumble High Intensity", Step = 0.05f, Min = 0f, Max = 1f)]
        public float RumbleHigh { get; set; } = 0.2f;

        public PlayerInputSystem(EntityManager entityManager, IPlayerInputSource inputSource)
            : base(entityManager)
        {
            _inputSource = inputSource;
            EventManager.Subscribe<SetPlayerInputEnabledEvent>(e => _inputEnabled = e.Enabled);
        }

        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<SceneState>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
            EnsureState();
            EnsureCursorPosition();

            PlayerInputFrame frame = _inputSource.Capture(
                Game1.WindowIsActive,
                Game1.RenderDestination,
                Game1.VirtualWidth,
                Game1.VirtualHeight);

            if (frame.Device == PlayerInputDevice.Gamepad)
            {
                UpdateGamepadCursor(frame, gameTime);
                frame = frame with
                {
                    PointerDelta = _cursorPosition - _state.Frame.PointerPosition,
                    PointerPosition = _cursorPosition,
                };
            }
            else
            {
                _cursorPosition = frame.PointerPosition;
            }

            bool interactionBlocked = !_inputEnabled
                || !frame.IsWindowActive
                || StateSingleton.IsActive;
            CursorTarget target = interactionBlocked
                ? CursorTarget.None
                : ResolveCursorTarget(frame.PointerPosition);

            _state.Frame = frame;
            _state.CursorTarget = target;
            _state.IsCursorInteractionEnabled = !interactionBlocked;

            UpdateRumble(target, frame, gameTime);
            PublishCommands(frame);
            EventManager.Publish(new PlayerInputEvent { Frame = frame });
            EventManager.Publish(new CursorStateEvent
            {
                Position = frame.PointerPosition,
                IsAPressed = frame.IsDown(PlayerButton.Primary),
                IsAPressedEdge = frame.WasPressed(PlayerButton.Primary),
                IsSecondaryPressed = frame.IsDown(PlayerButton.Secondary),
                IsSecondaryPressedEdge = frame.WasPressed(PlayerButton.Secondary),
                Coverage = target.Coverage,
                TopEntity = target.Entity,
                Source = frame.Device,
                ScrollDelta = frame.ScrollDelta,
                ScrollStickY = MathF.Abs(frame.RightStick.Y) > 0.15f ? frame.RightStick.Y : 0f,
            });
        }

        private void EnsureState()
        {
            if (_state != null) return;
            var stateEntity = EntityManager.GetEntity("PlayerInput");
            if (stateEntity == null)
            {
                stateEntity = EntityManager.CreateEntity("PlayerInput");
            }

            _state = stateEntity.GetComponent<PlayerInputState>();
            if (_state == null)
            {
                _state = new PlayerInputState();
                EntityManager.AddComponent(stateEntity, _state);
            }
        }

        private void EnsureCursorPosition()
        {
            if (_lastViewportWidth == Game1.VirtualWidth
                && _lastViewportHeight == Game1.VirtualHeight)
            {
                return;
            }

            _lastViewportWidth = Game1.VirtualWidth;
            _lastViewportHeight = Game1.VirtualHeight;
            _cursorPosition = new Vector2(
                Game1.VirtualWidth / 2f,
                Game1.VirtualHeight / 2f);
        }

        private void UpdateGamepadCursor(PlayerInputFrame frame, GameTime gameTime)
        {
            Vector2 stick = frame.LeftStick;
            float magnitude = stick.Length();
            if (magnitude < Deadzone) return;

            Vector2 direction = stick / magnitude;
            float normalized = MathHelper.Clamp(
                (magnitude - Deadzone) / Math.Max(0.001f, 1f - Deadzone),
                0f,
                1f);
            float multiplier = MathHelper.Clamp(
                MathF.Pow(normalized, SpeedExponent) * MaxMultiplier,
                0f,
                10f);
            if (frame.RightTrigger > 0.1f)
            {
                multiplier *= TriggerSpeedMultiplier;
            }

            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _cursorPosition += new Vector2(direction.X, -direction.Y)
                * BaseSpeed
                * multiplier
                * elapsed;
            _cursorPosition.X = MathHelper.Clamp(_cursorPosition.X, 0f, Game1.VirtualWidth);
            _cursorPosition.Y = MathHelper.Clamp(_cursorPosition.Y, 0f, Game1.VirtualHeight);
        }

        private CursorTarget ResolveCursorTarget(Vector2 position)
        {
            string contextId = InputContextResolver.ResolveCursorContext(
                EntityManager,
                position);
            var candidate = EntityManager.GetEntitiesWithComponent<UIElement>()
                .Select(entity => new
                {
                    Entity = entity,
                    UI = entity.GetComponent<UIElement>(),
                    Transform = entity.GetComponent<Transform>(),
                })
                .Where(item => item.UI != null
                    && !item.UI.IsHidden
                    && InputContextResolver.IsMember(item.Entity, contextId))
                .Where(item =>
                {
                    Rectangle bounds = TransformResolverService.ResolveUIBounds(
                        EntityManager,
                        item.Entity,
                        item.UI);
                    return bounds.Width >= 2
                        && bounds.Height >= 2
                        && ContainsPoint(bounds, item.Transform?.Rotation ?? 0f, position);
                })
                .OrderByDescending(item => item.Transform?.ZOrder ?? 0)
                .FirstOrDefault();

            if (candidate == null) return CursorTarget.None;
            var kind = contextId == InputContextIds.Diagnostic
                ? CursorTargetKind.Diagnostic
                : CursorTargetKind.UI;
            return new CursorTarget(candidate.Entity, kind, 1f);
        }

        private static bool ContainsPoint(Rectangle bounds, float rotation, Vector2 point)
        {
            if (MathF.Abs(rotation) < 0.001f)
            {
                return bounds.Contains(point);
            }

            Vector2 center = new(
                bounds.X + bounds.Width / 2f,
                bounds.Y + bounds.Height / 2f);
            Vector2 delta = point - center;
            float cos = MathF.Cos(rotation);
            float sin = MathF.Sin(rotation);
            float localX = delta.X * cos + delta.Y * sin;
            float localY = -delta.X * sin + delta.Y * cos;
            return MathF.Abs(localX) <= bounds.Width / 2f
                && MathF.Abs(localY) <= bounds.Height / 2f;
        }

        private void UpdateRumble(
            CursorTarget target,
            PlayerInputFrame frame,
            GameTime gameTime)
        {
            if (frame.Device != PlayerInputDevice.Gamepad)
            {
                if (_rumbleTimeRemaining > 0f)
                {
                    _rumbleTimeRemaining = 0f;
                    _inputSource.SetVibration(0f, 0f);
                }
                _previousHoverTarget = target.Entity;
                return;
            }

            if (target.Entity != _previousHoverTarget
                && target.Entity?.GetComponent<UIElement>()?.IsInteractable == true)
            {
                _rumbleTimeRemaining = RumbleDurationSeconds;
                _inputSource.SetVibration(RumbleLow, RumbleHigh);
            }

            if (_rumbleTimeRemaining > 0f)
            {
                _rumbleTimeRemaining -= (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (_rumbleTimeRemaining <= 0f)
                {
                    _inputSource.SetVibration(0f, 0f);
                }
            }

            _previousHoverTarget = target.Entity;
        }

        private static void PublishCommands(PlayerInputFrame frame)
        {
            Publish(frame, PlayerButton.Cancel, PlayerCommand.Cancel);
            Publish(frame, PlayerButton.LeftStick, PlayerCommand.ShowHint);
            Publish(frame, PlayerButton.F11, PlayerCommand.ToggleFullScreen);
            PublishModified(frame, PlayerButton.DebugMenu, PlayerCommand.ToggleDebugMenu);
            PublishModified(frame, PlayerButton.EntityList, PlayerCommand.ToggleEntityList);
            PublishModified(frame, PlayerButton.DebugDamage, PlayerCommand.DealDebugDamage);
            Publish(frame, PlayerButton.Profiler, PlayerCommand.ToggleProfiler);
            if (frame.WasPressed(PlayerButton.Quit) && frame.IsDown(PlayerButton.Shift))
            {
                EventManager.Publish(new PlayerCommandEvent
                {
                    Command = PlayerCommand.QuitApplication,
                    Source = frame.Device,
                });
            }
        }

        private static void Publish(
            PlayerInputFrame frame,
            PlayerButton button,
            PlayerCommand command)
        {
            if (!frame.WasPressed(button)) return;
            EventManager.Publish(new PlayerCommandEvent
            {
                Command = command,
                Source = frame.Device,
            });
        }

        private static void PublishModified(
            PlayerInputFrame frame,
            PlayerButton button,
            PlayerCommand command)
        {
            if (!frame.WasPressed(button) || !frame.IsDown(PlayerButton.Shift)) return;
            EventManager.Publish(new PlayerCommandEvent
            {
                Command = command,
                Source = frame.Device,
            });
        }
    }
}
