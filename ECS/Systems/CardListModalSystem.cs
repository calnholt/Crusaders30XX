using System;
using System.Linq;
using System.Collections.Generic;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Crusaders30XX.Diagnostics;

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

        public CardListModalSystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, SpriteFont font)
            : base(entityManager)
        {
            _graphicsDevice = graphicsDevice;
            _spriteBatch = spriteBatch;
            _font = font;
            _pixel = new Texture2D(graphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });

            EventManager.Subscribe<OpenCardListModalEvent>(OpenModal);
            EventManager.Subscribe<CloseCardListModalEvent>(_ => CloseModal());
        }

        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<CardListModal>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

        public void Draw()
        {
            var modalEntity = GetRelevantEntities().FirstOrDefault();
            if (modalEntity == null) return;
            var modal = modalEntity.GetComponent<CardListModal>();
            if (modal == null || !modal.IsOpen) return;

            int w = _graphicsDevice.Viewport.Width;
            int h = _graphicsDevice.Viewport.Height;
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

            // Close button (top-right)
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
                EntityManager.AddComponent(closeBtn, new UIElement { Bounds = closeRect, IsInteractable = true, Tooltip = "Close" });
                EntityManager.AddComponent(closeBtn, new CardListModalClose());
            }
            else
            {
                var ui = closeBtn.GetComponent<UIElement>();
                if (ui != null) ui.Bounds = closeRect;
            }

            // Fetch and sort provided cards
            var cards = (modal.Cards ?? new List<Entity>())
                .Select(e => e.GetComponent<CardData>())
                .Where(cd => cd != null)
                .OrderBy(cd => cd.Name)
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

            // Clip drawing to the content area with scissor rectangle
            var prevScissor = _graphicsDevice.ScissorRectangle;
            var prevState = _graphicsDevice.RasterizerState;
            var clipRect = new Rectangle(rect.X + Padding, cursorY, rect.Width - Padding * 2, visibleHeight);
            _graphicsDevice.ScissorRectangle = clipRect;
            _graphicsDevice.RasterizerState = new RasterizerState { ScissorTestEnable = true };

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

            // Mask overflow (belt-and-suspenders in case scissor is disabled)
            var topMask = new Rectangle(rect.X + Padding, rect.Y + Padding, rect.Width - Padding * 2, Math.Max(0, contentRect.Y - (rect.Y + Padding)));
            var botMask = new Rectangle(rect.X + Padding, contentRect.Bottom, rect.Width - Padding * 2, Math.Max(0, (rect.Bottom - Padding) - contentRect.Bottom));
            if (topMask.Height > 0) _spriteBatch.Draw(_pixel, topMask, panelColor);
            if (botMask.Height > 0) _spriteBatch.Draw(_pixel, botMask, panelColor);

            // Restore scissor state
            _graphicsDevice.RasterizerState = prevState;
            _graphicsDevice.ScissorRectangle = prevScissor;
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
            if (cmp != null) cmp.IsOpen = false;
        }
    }
}

