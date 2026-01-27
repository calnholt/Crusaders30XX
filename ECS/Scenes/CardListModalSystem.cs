using System;
using System.Linq;
using System.Collections.Generic;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Crusaders30XX.Diagnostics;
using Microsoft.Xna.Framework.Input;
using Crusaders30XX.ECS.Singletons;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// Displays a modal listing an arbitrary set of cards in an alphabetical grid with a close button.
    /// </summary>
    [DebugTab("Card List Modal")]
    public class CardListModalSystem : Core.System
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font;
        private readonly Texture2D _pixel;
        private readonly RasterizerState _scissorRasterizer;
        private int? _lastWheel;
        [DebugEditable(DisplayName = "Modal Margin", Step = 1, Min = 0, Max = 200)]
        public int ModalMargin { get; set; } = 40;
        [DebugEditable(DisplayName = "Padding", Step = 1, Min = 0, Max = 200)]
        public int Padding { get; set; } = 32;
        [DebugEditable(DisplayName = "Close Size", Step = 1, Min = 0, Max = 200)]
        public int CloseSize { get; set; } = 40;
        [DebugEditable(DisplayName = "Scroll Step", Step = 1, Min = 0, Max = 200)]
        public int ScrollStep { get; set; } = 100;
        private int GridCellW
        {
            get
            {
                var e = EntityManager.GetEntitiesWithComponent<CardVisualSettings>().FirstOrDefault();
                var s = e?.GetComponent<CardVisualSettings>();
                return s?.CardWidth ?? 250;
            }
        }
        private int GridCellH
        {
            get
            {
                var e = EntityManager.GetEntitiesWithComponent<CardVisualSettings>().FirstOrDefault();
                var s = e?.GetComponent<CardVisualSettings>();
                return s?.CardHeight ?? 350;
            }
        }
        private const int GridGap = 12;
        private const float TitleScale = 0.175f;
        private const float CardScale = 1.0f;

        [DebugEditable(DisplayName = "Gamepad Scroll Speed (px/s)", Step = 50, Min = 100, Max = 6000)]
        public float GamepadScrollSpeed { get; set; } = 1400f;
        [DebugEditable(DisplayName = "Right Stick Deadzone", Step = 0.01f, Min = 0f, Max = 0.5f)]
        public float RightStickDeadzone { get; set; } = 0.15f;
        [DebugEditable(DisplayName = "Speed Exponent", Step = 0.1f, Min = 0.1f, Max = 5f)]
        public float SpeedExponent { get; set; } = 1.2f;
        [DebugEditable(DisplayName = "Max Multiplier", Step = 0.1f, Min = 1f, Max = 10f)]
        public float MaxMultiplier { get; set; } = 3f;

        public CardListModalSystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
            : base(entityManager)
        {
            _graphicsDevice = graphicsDevice;
            _spriteBatch = spriteBatch;
            _font = FontSingleton.TitleFont;
            _pixel = new Texture2D(graphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });
            _scissorRasterizer = new RasterizerState { ScissorTestEnable = true, CullMode = CullMode.None };

            EventManager.Subscribe<OpenCardListModalEvent>(OpenModal);
            EventManager.Subscribe<CloseCardListModalEvent>(_ => CloseModal());
        }

        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<CardListModal>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime) { }
        
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            // Ensure modal cards are top-most and use zero rotation for accurate hover detection
            var modalEntity = GetRelevantEntities().FirstOrDefault();
            if (modalEntity == null) return;
            var modal = modalEntity.GetComponent<CardListModal>();
            if (modal == null || !modal.IsOpen || modal.Cards == null) return;
            // Gamepad right-stick scrolling
            if (Game1.WindowIsActive && !StateSingleton.IsActive)
            {
                var gp = GamePad.GetState(PlayerIndex.One);
                if (gp.IsConnected)
                {
                    var stick = gp.ThumbSticks.Right; // X: right+, Y: up+
                    float mag = stick.Length();
                    if (mag >= RightStickDeadzone)
                    {
                        int w = Game1.VirtualWidth;
                        int h = Game1.VirtualHeight;
                        var rect = new Rectangle(ModalMargin, ModalMargin, w - ModalMargin * 2, h - ModalMargin * 2);

                        int cursorY = rect.Y + Padding;
                        cursorY += (int)(_font.LineSpacing * TitleScale) + Padding;

                        var settingsEntity = EntityManager.GetEntitiesWithComponent<CardVisualSettings>().FirstOrDefault();
                        var cvs = settingsEntity != null ? settingsEntity.GetComponent<CardVisualSettings>() : null;
                        int topNudge = Math.Max(0, cvs?.CardOffsetYExtra ?? 0);

                        int maxCols = Math.Max(1, (rect.Width - Padding * 2 + GridGap) / (GridCellW + GridGap));
                        int rows = (((modal.Cards?.Count) ?? 0) + maxCols - 1) / maxCols;
                        int contentHeight = Math.Max(0, rows * (GridCellH + GridGap) - GridGap + topNudge);
                        int visibleHeight = rect.Bottom - cursorY - Padding;
                        int maxScroll = Math.Max(0, contentHeight - visibleHeight);

                        Vector2 dir = (mag > 0f) ? (stick / mag) : Vector2.Zero;
                        float normalized = MathHelper.Clamp((mag - RightStickDeadzone) / (1f - RightStickDeadzone), 0f, 1f);
                        float speedMultiplier = MathHelper.Clamp((float)System.Math.Pow(normalized, SpeedExponent) * MaxMultiplier, 0f, 10f);
                        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
                        float delta = -(dir.Y) * GamepadScrollSpeed * speedMultiplier * dt;
                        if (System.Math.Abs(delta) > 0.01f)
                        {
                            float next = MathHelper.Clamp(modal.ScrollOffset + delta, 0, maxScroll);
                            modal.ScrollOffset = (int)System.Math.Round(next);
                        }
                    }
                }
            }
            foreach (var card in modal.Cards)
            {
                var t = card.GetComponent<Transform>();
                if (t != null)
                {
                    t.ZOrder = 15000;
                    t.Rotation = 0f;
                }
                var ui = card.GetComponent<UIElement>();
                if (ui != null)
                {
                    ui.IsInteractable = true;
                }
            }
        }

        public void Draw()
        {
            var modalEntity = GetRelevantEntities().FirstOrDefault();
            if (modalEntity == null) return;
            var modal = modalEntity.GetComponent<CardListModal>();
            if (modal == null || !modal.IsOpen) return;

            int w = Game1.VirtualWidth;
            int h = Game1.VirtualHeight;
            var rect = new Rectangle(ModalMargin, ModalMargin, w - ModalMargin * 2, h - ModalMargin * 2);

            // Dim background overlay
            _spriteBatch.Draw(_pixel, new Rectangle(0, 0, w, h), new Color(0, 0, 0, 180));
            // Panel
            var panelColor = new Color(15, 25, 45) * 0.98f;
            _spriteBatch.Draw(_pixel, rect, panelColor);
            DrawBorder(rect, Color.White, 3);

            int cursorY = rect.Y + Padding;
            _spriteBatch.DrawString(_font, modal.Title ?? "Cards", new Vector2(rect.X + Padding, cursorY), Color.White, 0f, Vector2.Zero, TitleScale, SpriteEffects.None, 0f);
            cursorY += (int)(_font.LineSpacing * TitleScale) + Padding;

            // Fetch and sort provided cards
            var cards = (modal.Cards ?? new List<Entity>())
                .Select(e => e.GetComponent<CardData>())
                .Where(cd => cd != null)
                .OrderBy(cd => cd.Card.CardId)
                .ToList();

            // Grid within rect
            int gridX = rect.X + Padding;
            int gridY = cursorY;
            // Nudge first row down by the card's internal top offset so its top isn't clipped
            var settingsEntity = EntityManager.GetEntitiesWithComponent<CardVisualSettings>().FirstOrDefault();
            var cvs = settingsEntity != null ? settingsEntity.GetComponent<CardVisualSettings>() : null;
            int topNudge = Math.Max(0, cvs?.CardOffsetYExtra ?? 0);
            gridY += topNudge;
            int maxCols = Math.Max(1, (rect.Width - Padding * 2 + GridGap) / (GridCellW + GridGap));
            int col = 0;

            // Calculate content height and clamp scroll
            int rows = (cards.Count + maxCols - 1) / maxCols;
            int contentHeight = Math.Max(0, rows * (GridCellH + GridGap) - GridGap + topNudge);
            int visibleHeight = rect.Bottom - cursorY - Padding;
            int maxScroll = Math.Max(0, contentHeight - visibleHeight);

            // Handle mouse wheel scrolling within content area
            var mouse = Microsoft.Xna.Framework.Input.Mouse.GetState();
            var contentRect = new Rectangle(rect.X + Padding, cursorY, rect.Width - Padding * 2, visibleHeight);
            if (contentRect.Contains(mouse.Position))
            {
                int delta = mouse.ScrollWheelValue;
                if (_lastWheel.HasValue)
                {
                    int diff = delta - _lastWheel.Value;
                    if (diff != 0)
                    {
                        modal.ScrollOffset -= Math.Sign(diff) * ScrollStep;
                        if (modal.ScrollOffset < 0) modal.ScrollOffset = 0;
                        if (modal.ScrollOffset > maxScroll) modal.ScrollOffset = maxScroll;
                    }
                }
                _lastWheel = delta;
            }
            else
            {
                _lastWheel = mouse.ScrollWheelValue;
            }

            // End current batch to establish new state with scissor clipping
            var prevScissor = _graphicsDevice.ScissorRectangle;
            _spriteBatch.End();

            // Begin new batch with scissor-enabled rasterizer
            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, null, _scissorRasterizer);

            // Set scissor rectangle for clipping
            var clipRect = new Rectangle(rect.X + Padding, cursorY, rect.Width - Padding * 2, visibleHeight);
            _graphicsDevice.ScissorRectangle = clipRect;

            int startY = gridY - modal.ScrollOffset;
            foreach (var cd in cards)
            {
                var cell = new Rectangle(gridX + col * (GridCellW + GridGap), startY, GridCellW, GridCellH);
                // Render the actual card scaled in the cell center
                var cardEntity = cd.Owner;
                Vector2 center = new Vector2(cell.Center.X, cell.Center.Y);
                EventManager.Publish(new CardRenderScaledEvent
                {
                    Card = cardEntity,
                    Position = center,
                    Scale = CardScale
                });

                col++;
                if (col >= maxCols)
                {
                    col = 0;
                    startY += GridCellH + GridGap;
                }
            }

            // End clipped batch and resume normal drawing
            _spriteBatch.End();
            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, null, _scissorRasterizer);

            // Restore scissor to full screen
            _graphicsDevice.ScissorRectangle = prevScissor;

            // Draw close button (top-right)
            var closeRect = new Rectangle(rect.Right - Padding - CloseSize, rect.Y + Padding, CloseSize, CloseSize);
            _spriteBatch.Draw(_pixel, closeRect, new Color(70, 70, 70));
            DrawBorder(closeRect, Color.White, 2);
            // Center the X label precisely in the button
            string xLabel = "X";
            float xScale = 0.15f;
            var xMeasure = _font.MeasureString(xLabel) * xScale;
            Vector2 xPos = new Vector2(closeRect.Center.X - xMeasure.X / 2f, closeRect.Center.Y - xMeasure.Y / 2f);
            _spriteBatch.DrawString(_font, xLabel, xPos, Color.White, 0f, Vector2.Zero, xScale, SpriteEffects.None, 0f);

            // Sync a clickable close entity
            var closeBtn = EntityManager.GetEntitiesWithComponent<CardListModalClose>().FirstOrDefault();
            if (closeBtn == null)
            {
                closeBtn = EntityManager.CreateEntity("CardListModal_Close");
                EntityManager.AddComponent(closeBtn, new Transform { Position = new Vector2(closeRect.X, closeRect.Y), ZOrder = 20000 });
                EntityManager.AddComponent(closeBtn, new UIElement { Bounds = closeRect, IsInteractable = true, Tooltip = "Close", LayerType = UILayerType.Overlay, EventType = UIElementEventType.CardListModalClose });
                EntityManager.AddComponent(closeBtn, new HotKey { Button = FaceButton.B });
                EntityManager.AddComponent(closeBtn, new CardListModalClose());
            }
            else
            {
                var ui = closeBtn.GetComponent<UIElement>();
                if (ui != null) ui.Bounds = closeRect;
            }
        }

        private void DrawBorder(Rectangle r, Color color, int thickness)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(r.X, r.Y, r.Width, thickness), color);
            _spriteBatch.Draw(_pixel, new Rectangle(r.X, r.Bottom - thickness, r.Width, thickness), color);
            _spriteBatch.Draw(_pixel, new Rectangle(r.X, r.Y, thickness, r.Height), color);
            _spriteBatch.Draw(_pixel, new Rectangle(r.Right - thickness, r.Y, thickness, r.Height), color);
        }

        private void OpenModal(OpenCardListModalEvent evt)
        {
            var modal = EntityManager.GetEntitiesWithComponent<CardListModal>().FirstOrDefault();
            if (modal == null)
            {
                modal = EntityManager.CreateEntity("CardListModal");
                EntityManager.AddComponent(modal, new CardListModal { IsOpen = true, Title = evt.Title, Cards = evt.Cards ?? new List<Entity>(), ScrollOffset = 0 });
            }
            else
            {
                var cmp = modal.GetComponent<CardListModal>();
                if (cmp != null)
                {
                    cmp.Title = evt.Title;
                    cmp.Cards = evt.Cards ?? new List<Entity>();
                    cmp.IsOpen = true;
                    cmp.ScrollOffset = 0;
                }
            }
        }

        private void CloseModal()
        {
            var modal = EntityManager.GetEntitiesWithComponent<CardListModal>().FirstOrDefault();
            if (modal == null) return;
            var cmp = modal.GetComponent<CardListModal>();
            if (cmp != null)
            {
                cmp.IsOpen = false;
                // Clear UI hover/click/bounds for any cards that were displayed in the modal grid
                var cards = cmp.Cards ?? new List<Entity>();
                foreach (var card in cards)
                {
                    var ui = card.GetComponent<UIElement>();
                    if (ui != null)
                    {
                        ui.IsHovered = false;
                        ui.IsClicked = false;
                        ui.Bounds = new Rectangle(0, 0, 0, 0);
                    }
                }
            }
            // Destroy any lingering close button entities
            var closeButtons = EntityManager.GetEntitiesWithComponent<CardListModalClose>().ToList();
            foreach (var btn in closeButtons)
            {
                EntityManager.DestroyEntity(btn.Id);
            }
            // Reset wheel so a future open starts fresh
            _lastWheel = null;
        }
    }
}

