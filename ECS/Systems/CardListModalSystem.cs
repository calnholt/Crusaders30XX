using System;
using System.Linq;
using System.Collections.Generic;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Config;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// Displays a modal listing an arbitrary set of cards in an alphabetical grid with a close button.
    /// </summary>
    public class CardListModalSystem : Core.System
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font;
        private readonly Texture2D _pixel;

        private const int ModalMargin = 40;
        private const int Padding = 32;
        private static int GridCellW => CardConfig.CARD_WIDTH;
        private static int GridCellH => CardConfig.CARD_HEIGHT;
        private const int GridGap = 12;
        private const float TitleScale = 0.7f;
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
            _spriteBatch.Draw(_pixel, rect, new Color(15, 25, 45) * 0.98f);
            DrawBorder(rect, Color.White, 3);

            int cursorY = rect.Y + Padding;
            _spriteBatch.DrawString(_font, modal.Title ?? "Cards", new Vector2(rect.X + Padding, cursorY), Color.White, 0f, Vector2.Zero, TitleScale, SpriteEffects.None, 0f);
            cursorY += (int)(_font.LineSpacing * TitleScale) + Padding;

            // Close button (top-right)
            var closeRect = new Rectangle(rect.Right - Padding - 28, rect.Y + Padding, 28, 28);
            _spriteBatch.Draw(_pixel, closeRect, new Color(70, 70, 70));
            DrawBorder(closeRect, Color.White, 2);
            var xSize = _font.MeasureString("X") * 0.6f;
            _spriteBatch.DrawString(_font, "X", new Vector2(closeRect.Center.X - xSize.X / 2f, closeRect.Center.Y - xSize.Y / 2f), Color.White, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);

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
            int maxCols = Math.Max(1, (rect.Width - Padding * 2 + GridGap) / (GridCellW + GridGap));
            int col = 0;
            foreach (var cd in cards)
            {
                var cell = new Rectangle(gridX + col * (GridCellW + GridGap), gridY, GridCellW, GridCellH);
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
                    gridY += GridCellH + GridGap;
                }
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
                EntityManager.AddComponent(modal, new CardListModal { IsOpen = true, Title = evt.Title, Cards = evt.Cards ?? new List<Entity>() });
            }
            else
            {
                var cmp = modal.GetComponent<CardListModal>();
                if (cmp != null)
                {
                    cmp.Title = evt.Title;
                    cmp.Cards = evt.Cards ?? new List<Entity>();
                    cmp.IsOpen = true;
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

