using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Crusaders30XX.Diagnostics;
using System;
using Crusaders30XX.ECS.Singletons;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// Displays the "Choose card to pledge" prompt and "Skip Pledge" button during the Pledge subphase.
    /// Clicking skip or selecting a card proceeds to the next phase.
    /// </summary>
    [DebugTab("Skip Pledge UI")]
    public class SkipPledgeDisplaySystem : Core.System
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font = FontSingleton.ContentFont;
        private readonly SpriteFont _titleFont = FontSingleton.TitleFont;
        private readonly Texture2D _pixel;
        private bool _isVisible = false;

        [DebugEditable(DisplayName = "Button Width", Step = 1, Min = 80, Max = 600)]
        public int ButtonWidth { get; set; } = 180;

        [DebugEditable(DisplayName = "Button Height", Step = 1, Min = 24, Max = 200)]
        public int ButtonHeight { get; set; } = 50;

        [DebugEditable(DisplayName = "Button Offset Y", Step = 5, Min = -2000, Max = 2000)]
        public int ButtonOffsetY { get; set; } = 550;

        [DebugEditable(DisplayName = "Button Z", Step = 100, Min = 0, Max = 20000)]
        public int ButtonZ { get; set; } = 4000;

        [DebugEditable(DisplayName = "Button Text Scale", Step = 0.05f, Min = 0.2f, Max = 2.5f)]
        public float ButtonTextScale { get; set; } = 0.18f;

        [DebugEditable(DisplayName = "Prompt Text Scale", Step = 0.05f, Min = 0.1f, Max = 1.0f)]
        public float PromptTextScale { get; set; } = 0.25f;

        [DebugEditable(DisplayName = "Prompt Offset Y", Step = 5, Min = -500, Max = 500)]
        public int PromptOffsetY { get; set; } = -375;

        public SkipPledgeDisplaySystem(EntityManager entityManager, GraphicsDevice gd, SpriteBatch sb) : base(entityManager)
        {
            _graphicsDevice = gd;
            _spriteBatch = sb;
            _pixel = new Texture2D(gd, 1, 1);
            _pixel.SetData(new[] { Color.White });

            // Handle button click via debug command
            EventManager.Subscribe<DebugCommandEvent>(evt =>
            {
                if (evt.Command == "SkipPledge")
                {
                    Console.WriteLine("[SkipPledgeDisplaySystem] SkipPledge command received");
                    OnSkipPledgePressed();
                }
            });
            
            EventManager.Subscribe<ChangeBattlePhaseEvent>(OnChangeBattlePhaseEvent);
            EventManager.Subscribe<LoadSceneEvent>(_ => HideAll());
        }

        protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
        {
            return System.Array.Empty<Entity>();
        }

        public void OnChangeBattlePhaseEvent(ChangeBattlePhaseEvent evt)
        { 
            _isVisible = evt.Current == SubPhase.Pledge;
            
            var ui = EntityManager.GetEntity("UIButton_SkipPledge")?.GetComponent<UIElement>();
            if (ui != null)
            {
                ui.IsHidden = !_isVisible;
                ui.IsInteractable = _isVisible;
            }
        }

        public void HideAll()
        {
            _isVisible = false;
            var ui = EntityManager.GetEntity("UIButton_SkipPledge")?.GetComponent<UIElement>();
            if (ui != null)
            {
                ui.IsHidden = true;
                ui.IsInteractable = false;
            }
        }

        private void OnSkipPledgePressed()
        {
            _isVisible = false;
            var ui = EntityManager.GetEntity("UIButton_SkipPledge")?.GetComponent<UIElement>();
            if (ui != null)
            {
                ui.IsHidden = true;
                ui.IsInteractable = false;
            }
            EventManager.Publish(new SkipPledgeRequested());
        }

        private Rectangle GetButtonRect()
        {
            int width = Game1.VirtualWidth;
            int height = Game1.VirtualHeight;
            int x = (int)(width * 0.5f - ButtonWidth * 0.5f);
            int y = height - ButtonHeight - 40 - ButtonOffsetY;
            return new Rectangle(x, y, ButtonWidth, ButtonHeight);
        }

        private void DrawRect(Rectangle r, Color color, int thickness)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(r.X, r.Y, r.Width, thickness), color);
            _spriteBatch.Draw(_pixel, new Rectangle(r.X, r.Bottom - thickness, r.Width, thickness), color);
            _spriteBatch.Draw(_pixel, new Rectangle(r.X, r.Y, thickness, r.Height), color);
            _spriteBatch.Draw(_pixel, new Rectangle(r.Right - thickness, r.Y, thickness, r.Height), color);
        }

        public void Draw()
        {
            var btnRect = GetButtonRect();
            var skipBtn = EntityManager.GetEntitiesWithComponent<UIButton>().FirstOrDefault(e => e.GetComponent<UIButton>().Command == "SkipPledge");
            var ui = EntityManager.GetEntity("UIButton_SkipPledge")?.GetComponent<UIElement>();
            
            if (skipBtn == null)
            {
                skipBtn = EntityManager.CreateEntity("UIButton_SkipPledge");
                EntityManager.AddComponent(skipBtn, new UIButton { Label = "Skip Pledge", Command = "SkipPledge" });
                EntityManager.AddComponent(skipBtn, new Transform { BasePosition = new Vector2(btnRect.X, btnRect.Y), Position = new Vector2(btnRect.X, btnRect.Y), ZOrder = ButtonZ });
                EntityManager.AddComponent(skipBtn, new UIElement { Bounds = btnRect, IsInteractable = false, IsHidden = true, EventType = UIElementEventType.SkipPledge });
                EntityManager.AddComponent(skipBtn, new HotKey { Button = FaceButton.B });
                EntityManager.AddComponent(skipBtn, ParallaxLayer.GetUIParallaxLayer());
            }
            else
            {
                var tr = skipBtn.GetComponent<Transform>();
                if (tr != null)
                {
                    tr.ZOrder = ButtonZ;
                    tr.BasePosition = new Vector2(btnRect.X, btnRect.Y);
                    tr.Position = new Vector2(btnRect.X, btnRect.Y);
                }
            }
            
            if (!_isVisible) return;
            
            int screenWidth = Game1.VirtualWidth;
            int screenHeight = Game1.VirtualHeight;
            
            // Draw "Choose card to pledge" prompt at top center
            string promptText = "Choose card to pledge";
            var promptSize = _titleFont.MeasureString(promptText) * PromptTextScale;
            var promptPos = new Vector2(
                screenWidth / 2f - promptSize.X / 2f,
                screenHeight / 2f + PromptOffsetY
            );
            _spriteBatch.DrawString(_titleFont, promptText, promptPos, Color.White, 0f, Vector2.Zero, PromptTextScale, SpriteEffects.None, 0f);
            
            // Draw the skip button
            ui = EntityManager.GetEntity("UIButton_SkipPledge")?.GetComponent<UIElement>();
            if (ui == null || ui.IsHidden) return;
            
            // Draw using the entity's current Transform.Position (which includes parallax offset)
            var t = skipBtn?.GetComponent<Transform>();
            Vector2 drawPos = (t != null) ? t.Position : new Vector2(btnRect.X, btnRect.Y);
            var drawRect = new Rectangle((int)drawPos.X, (int)drawPos.Y, btnRect.Width, btnRect.Height);

            // Gray/neutral color for skip button
            _spriteBatch.Draw(_pixel, drawRect, new Color(80, 80, 80, 220));
            DrawRect(drawRect, Color.White, 2);
            string label = "Skip Pledge";
            var size = _font.MeasureString(label) * ButtonTextScale;
            var posText = new Vector2(drawRect.Center.X - size.X / 2f, drawRect.Center.Y - size.Y / 2f);
            _spriteBatch.DrawString(_font, label, posText, Color.White, 0f, Vector2.Zero, ButtonTextScale, SpriteEffects.None, 0f);

            // Keep UI bounds aligned with drawn rect
            var skipBtnUi = skipBtn?.GetComponent<UIElement>();
            if (skipBtnUi != null)
            {
                skipBtnUi.Bounds = drawRect;
            }
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
            // No per-entity update needed
        }
    }
}
