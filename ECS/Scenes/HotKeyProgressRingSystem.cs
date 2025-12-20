using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Crusaders30XX.ECS.Systems
{
    [DebugTab("HotKeys")]
    public class HotKeyProgressRingSystem : Core.System
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private readonly SystemManager _systemManager;
        private GamePadState _prevGamePadState;
        private KeyboardState _prevKeyboardState;
        private Dictionary<Entity, float> _holdProgress = new Dictionary<Entity, float>();
        private Dictionary<Entity, FaceButton> _heldButtons = new Dictionary<Entity, FaceButton>();
        private Texture2D _pixel;

        [DebugEditable(DisplayName = "Progress Ring Radius Multiplier", Step = 0.1f, Min = 1.0f, Max = 3.0f)]
        public float ProgressRingRadius { get; set; } = 1.5f;

        [DebugEditable(DisplayName = "Progress Ring Thickness (px)", Step = 1f, Min = 1f, Max = 10f)]
        public int ProgressRingThickness { get; set; } = 3;

        [DebugEditable(DisplayName = "Progress Ring Color R", Step = 1f, Min = 0f, Max = 255f)]
        public int ProgressRingColorR { get; set; } = 255;

        [DebugEditable(DisplayName = "Progress Ring Color G", Step = 1f, Min = 0f, Max = 255f)]
        public int ProgressRingColorG { get; set; } = 255;

        [DebugEditable(DisplayName = "Progress Ring Color B", Step = 1f, Min = 0f, Max = 255f)]
        public int ProgressRingColorB { get; set; } = 255;

        [DebugEditable(DisplayName = "Progress Ring Start Angle (deg)", Step = 1f, Min = -360f, Max = 360f)]
        public float ProgressRingStartAngle { get; set; } = -90f;

        public HotKeyProgressRingSystem(EntityManager entityManager, GraphicsDevice gd, SpriteBatch sb, SystemManager systemManager)
            : base(entityManager)
        {
            _graphicsDevice = gd;
            _spriteBatch = sb;
            _systemManager = systemManager;
            _pixel = new Texture2D(_graphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });
            _prevKeyboardState = Keyboard.GetState();
            
            EventManager.Subscribe<DeleteCachesEvent>(OnDeleteCaches);
        }

        private void OnDeleteCaches(DeleteCachesEvent evt)
        {
            // No caches to clear
        }

        protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
        {
            // Process globally; we will iterate HotKey components during Update/Draw
            return EntityManager.GetEntitiesWithComponent<SceneState>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
            if (!Game1.WindowIsActive || StateSingleton.IsActive || StateSingleton.PreventClicking) return;
            
            var caps = GamePad.GetCapabilities(PlayerIndex.One);
            var kb = Keyboard.GetState();
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            
            // Check if any overlay UI is present (shared between both branches)
            bool overlayPresent = EntityManager.GetEntitiesWithComponent<UIElement>()
                .Any(e => {
                    var ui = e.GetComponent<UIElement>();
                    return ui != null 
                        && ui.LayerType == UILayerType.Overlay 
                        && ui.IsInteractable 
                        && ui.Bounds.Width > 0 
                        && ui.Bounds.Height > 0;
                });
            
            // List to collect entities that complete hold (shared between both branches)
            var entitiesToComplete = new List<Entity>();
            
            if (!caps.IsConnected)
            {
                _prevGamePadState = default;
                
                // Handle keyboard input when gamepad is not connected
                bool edgeSpace = kb.IsKeyDown(Keys.Space) && !_prevKeyboardState.IsKeyDown(Keys.Space);
                bool releaseSpace = !kb.IsKeyDown(Keys.Space) && _prevKeyboardState.IsKeyDown(Keys.Space);
                
                // Handle spacebar press edge - start tracking hold for FaceButton.X
                if (edgeSpace)
                {
                    FaceButton pressed = FaceButton.X;
                    
                    // Find top-most eligible entity with this hotkey that requires hold
                    var target = EntityManager.GetEntitiesWithComponent<HotKey>()
                        .Select(e => new { E = e, HK = e.GetComponent<HotKey>(), UI = e.GetComponent<UIElement>(), T = e.GetComponent<Transform>(), Btn = e.GetComponent<UIButton>() })
                        .Where(x => x.HK != null && x.HK.IsActive && x.UI != null && x.UI.IsInteractable && x.HK.Button == pressed && x.HK.RequiresHold && (!overlayPresent || x.UI.LayerType == UILayerType.Overlay))
                        .OrderByDescending(x => x.T?.ZOrder ?? 0)
                        .FirstOrDefault();
                    
                    if (target != null)
                    {
                        _holdProgress[target.E] = 0f;
                        _heldButtons[target.E] = pressed;
                    }
                }
                
                // Handle spacebar release edge - cancel tracking
                if (releaseSpace)
                {
                    FaceButton released = FaceButton.X;
                    var toRemove = _heldButtons.Where(kvp => kvp.Value == released).Select(kvp => kvp.Key).ToList();
                    foreach (var ent in toRemove)
                    {
                        _holdProgress.Remove(ent);
                        _heldButtons.Remove(ent);
                    }
                }
                
                // Update hold progress for entities being held
                foreach (var kvp in _holdProgress.ToList())
                {
                    var ent = kvp.Key;
                    var hotKey = ent.GetComponent<HotKey>();
                    var ui = ent.GetComponent<UIElement>();
                    
                    if (hotKey == null || ui == null || !ui.IsInteractable)
                    {
                        _holdProgress.Remove(ent);
                        _heldButtons.Remove(ent);
                        continue;
                    }
                    
                    // Check if spacebar is still pressed (for FaceButton.X)
                    bool stillPressed = false;
                    if (_heldButtons.TryGetValue(ent, out FaceButton btn))
                    {
                        if (btn == FaceButton.X)
                        {
                            stillPressed = kb.IsKeyDown(Keys.Space);
                        }
                    }
                    
                    if (stillPressed)
                    {
                        // Increment hold time
                        _holdProgress[ent] += deltaTime;
                        
                        // Check if hold completed
                        if (_holdProgress[ent] >= hotKey.HoldDurationSeconds)
                        {
                            entitiesToComplete.Add(ent);
                        }
                    }
                    else
                    {
                        // Button released before completion, remove tracking
                        _holdProgress.Remove(ent);
                        _heldButtons.Remove(ent);
                    }
                }
                
                _prevKeyboardState = kb;
            }
            else
            {
                var gp = GamePad.GetState(PlayerIndex.One);

                // Check for button press edges to start hold tracking
                bool edgeY = gp.Buttons.Y == ButtonState.Pressed && _prevGamePadState.Buttons.Y == ButtonState.Released;
                bool edgeB = gp.Buttons.B == ButtonState.Pressed && _prevGamePadState.Buttons.B == ButtonState.Released;
                bool edgeX = gp.Buttons.X == ButtonState.Pressed && _prevGamePadState.Buttons.X == ButtonState.Released;
                bool edgeStart = gp.Buttons.Start == ButtonState.Pressed && _prevGamePadState.Buttons.Start == ButtonState.Released;

                // Check for button release edges to cancel hold tracking
                bool releaseY = gp.Buttons.Y == ButtonState.Released && _prevGamePadState.Buttons.Y == ButtonState.Pressed;
                bool releaseB = gp.Buttons.B == ButtonState.Released && _prevGamePadState.Buttons.B == ButtonState.Pressed;
                bool releaseX = gp.Buttons.X == ButtonState.Released && _prevGamePadState.Buttons.X == ButtonState.Pressed;
                bool releaseStart = gp.Buttons.Start == ButtonState.Released && _prevGamePadState.Buttons.Start == ButtonState.Pressed;

                // Handle button press edges - start tracking hold
                if (edgeY || edgeB || edgeX || edgeStart)
                {
                    FaceButton? pressed = null;
                    if (edgeY) pressed = FaceButton.Y;
                    else if (edgeB) pressed = FaceButton.B;
                    else if (edgeX) pressed = FaceButton.X;
                    else if (edgeStart) pressed = FaceButton.Start;

                    // Find top-most eligible entity with this hotkey that requires hold
                    var target = EntityManager.GetEntitiesWithComponent<HotKey>()
                        .Select(e => new { E = e, HK = e.GetComponent<HotKey>(), UI = e.GetComponent<UIElement>(), T = e.GetComponent<Transform>(), Btn = e.GetComponent<UIButton>() })
                        .Where(x => x.HK != null && x.HK.IsActive && x.UI != null && x.UI.IsInteractable && x.HK.Button == pressed && x.HK.RequiresHold && (!overlayPresent || x.UI.LayerType == UILayerType.Overlay))
                        .OrderByDescending(x => x.T?.ZOrder ?? 0)
                        .FirstOrDefault();

                    if (target != null)
                    {
                        _holdProgress[target.E] = 0f;
                        _heldButtons[target.E] = pressed.Value;
                    }
                }

                // Handle button releases - cancel tracking if button released
                if (releaseY || releaseB || releaseX || releaseStart)
                {
                    FaceButton? released = null;
                    if (releaseY) released = FaceButton.Y;
                    else if (releaseB) released = FaceButton.B;
                    else if (releaseX) released = FaceButton.X;
                    else if (releaseStart) released = FaceButton.Start;

                    // Remove entities that were tracking this button
                    var toRemove = _heldButtons.Where(kv => kv.Value == released.Value).Select(kv => kv.Key).ToList();
                    foreach (var ent in toRemove)
                    {
                        _holdProgress.Remove(ent);
                        _heldButtons.Remove(ent);
                    }
                }

                // Update hold progress for entities being held
                foreach (var kvp in _holdProgress.ToList())
                {
                    var ent = kvp.Key;
                    var hotKey = ent.GetComponent<HotKey>();
                    var ui = ent.GetComponent<UIElement>();
                    
                    if (hotKey == null || ui == null || !ui.IsInteractable)
                    {
                        _holdProgress.Remove(ent);
                        _heldButtons.Remove(ent);
                        continue;
                    }

                    // Check if the button is still pressed
                    bool stillPressed = false;
                    if (_heldButtons.TryGetValue(ent, out FaceButton btn))
                    {
                        switch (btn)
                        {
                            case FaceButton.Y:
                                stillPressed = gp.Buttons.Y == ButtonState.Pressed;
                                break;
                            case FaceButton.B:
                                stillPressed = gp.Buttons.B == ButtonState.Pressed;
                                break;
                            case FaceButton.X:
                                stillPressed = gp.Buttons.X == ButtonState.Pressed;
                                break;
                            case FaceButton.Start:
                                stillPressed = gp.Buttons.Start == ButtonState.Pressed;
                                break;
                        }
                    }

                    if (stillPressed)
                    {
                        // Increment hold time
                        _holdProgress[ent] += deltaTime;
                        
                        // Check if hold completed
                        if (_holdProgress[ent] >= hotKey.HoldDurationSeconds)
                        {
                            entitiesToComplete.Add(ent);
                        }
                    }
                    else
                    {
                        // Button released before completion, remove tracking
                        _holdProgress.Remove(ent);
                        _heldButtons.Remove(ent);
                    }
                }

                _prevGamePadState = gp;
                _prevKeyboardState = kb;
            }
            
            // Complete hold actions - publish event instead of directly triggering (shared between both branches)
            foreach (var ent in entitiesToComplete)
            {
                EventManager.Publish(new HotKeyHoldCompletedEvent { Entity = ent });
                _holdProgress.Remove(ent);
                _heldButtons.Remove(ent);
            }
        }

        public void Draw()
        {
            var caps = GamePad.GetCapabilities(PlayerIndex.One);
            bool gamepadConnected = caps.IsConnected;

            var hotKeySystem = _systemManager.GetSystem<HotKeySystem>();
            if (hotKeySystem == null) return;

            bool overlayPresent = EntityManager.GetEntitiesWithComponent<UIElement>()
                .Any(e => {
                    var ui = e.GetComponent<UIElement>();
                    return ui != null 
                        && ui.LayerType == UILayerType.Overlay 
                        && ui.IsInteractable 
                        && ui.Bounds.Width > 0 
                        && ui.Bounds.Height > 0;
                });

            // Draw progress rings for entities being held
            foreach (var kvp in _holdProgress.ToList())
            {
                var ent = kvp.Key;
                var hotKey = ent.GetComponent<HotKey>();
                var ui = ent.GetComponent<UIElement>();
                var transform = ent.GetComponent<Transform>();

                if (hotKey == null || !hotKey.IsActive || ui == null || !ui.IsInteractable) continue;
                if (overlayPresent && ui.LayerType != UILayerType.Overlay) continue;
                
                // When gamepad is not connected, only draw progress for FaceButton.X (spacebar)
                if (!gamepadConnected && hotKey.Button != FaceButton.X) continue;

                var r = ui.Bounds;
                if (r.Width < 2 || r.Height < 2) continue;

                // Calculate button hint position (same logic as HotKeySystem)
                int hintRadius = hotKeySystem.HintRadius;
                var (cx, cy) = hotKeySystem.CalculateHintPosition(r, hotKey.Position, hintRadius, hotKeySystem.HintGapX, hotKeySystem.HintGapY);

                // Calculate progress
                float progress = System.Math.Min(1f, kvp.Value / hotKey.HoldDurationSeconds);

                // Draw progress arc
                if (progress > 0f)
                {
                    int ringRadius = (int)System.Math.Round(hintRadius * ProgressRingRadius);
                    Color ringColor = new Color(ProgressRingColorR, ProgressRingColorG, ProgressRingColorB);
                    DrawProgressArc(new Vector2(cx, cy), ringRadius, ProgressRingThickness, progress, ProgressRingStartAngle, ringColor);
                }
            }
        }

        private void DrawProgressArc(Vector2 center, int radius, int thickness, float progress, float startAngleDeg, Color color)
        {
            if (progress <= 0f || radius < 1 || thickness < 1) return;

            // Convert start angle to radians
            float startAngleRad = MathHelper.ToRadians(startAngleDeg);
            float endAngleRad = startAngleRad + (progress * MathHelper.TwoPi);

            // Draw arc using many small segments along the mid-radius to create a solid ring
            // Increase segment density based on radius and progress for a smoother result
            int baseSegments = (int)System.Math.Round(progress * radius * 4f);
            int segments = System.Math.Max(64, baseSegments);
            float angleStep = (endAngleRad - startAngleRad) / segments;

            // Use a mid radius so the thickness extends both inward and outward
            float midRadius = radius - (thickness * 0.5f);

            for (int i = 0; i < segments; i++)
            {
                float angle1 = startAngleRad + angleStep * i;
                // Slightly overlap segments to avoid tiny gaps
                float angle2 = startAngleRad + angleStep * (i + 1.05f);
                if (angle2 > endAngleRad)
                {
                    angle2 = endAngleRad;
                }

                Vector2 mid1 = center + new Vector2(
                    System.MathF.Cos(angle1) * midRadius,
                    System.MathF.Sin(angle1) * midRadius
                );
                Vector2 mid2 = center + new Vector2(
                    System.MathF.Cos(angle2) * midRadius,
                    System.MathF.Sin(angle2) * midRadius
                );

                // Draw a single thick, anti-aliased stroke for this segment
                DrawLineAA(mid1, mid2, color, thickness);
            }
        }

        /// <summary>
        /// Draws an anti-aliased line by layering several slightly offset strokes
        /// with decreasing alpha to create a soft edge.
        /// </summary>
        private void DrawLineAA(Vector2 a, Vector2 b, Color color, int thickness)
        {
            // For very thin lines, fall back to the basic draw
            if (thickness <= 1)
            {
                DrawLine(a, b, color, thickness);
                return;
            }

            const int featherSteps = 2; // total strokes = 2 * featherSteps + 1

            // Precompute direction/normal for offsets
            float dx = b.X - a.X;
            float dy = b.Y - a.Y;
            float len = System.MathF.Sqrt(dx * dx + dy * dy);
            if (len <= 0.0001f)
            {
                return;
            }

            Vector2 normal = new Vector2(-dy / len, dx / len);

            for (int i = -featherSteps; i <= featherSteps; i++)
            {
                float offset = i * 0.5f;
                float weight = 1f - (System.Math.Abs(i) / (float)(featherSteps + 1));
                byte alpha = (byte)(color.A * weight);
                Color c = new Color(color.R, color.G, color.B, alpha);

                Vector2 offsetVec = normal * offset;
                Vector2 aaStart = a + offsetVec;
                Vector2 aaEnd = b + offsetVec;

                DrawLine(aaStart, aaEnd, c, thickness);
            }
        }

        private void DrawLine(Vector2 a, Vector2 b, Color color, int thickness)
        {
            float dx = b.X - a.X;
            float dy = b.Y - a.Y;
            float len = System.MathF.Max(1f, System.MathF.Sqrt(dx * dx + dy * dy));
            float ang = System.MathF.Atan2(dy, dx);
            _spriteBatch.Draw(_pixel, position: a, sourceRectangle: null, color: color, rotation: ang, origin: Vector2.Zero, scale: new Vector2(len, thickness), effects: SpriteEffects.None, layerDepth: 0f);
        }

    }
}
