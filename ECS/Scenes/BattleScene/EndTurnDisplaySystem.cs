using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Crusaders30XX.Diagnostics;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// Displays an "End Turn" button during the player's Action phase.
    /// When clicked, transitions to Block phase. Button mirrors EnemyAttackDisplaySystem styling.
    /// </summary>
    [DebugTab("End Turn UI")]
    public class EndTurnDisplaySystem : Core.System
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font;
        private readonly Texture2D _pixel;

        // Visuals similar to EnemyAttackDisplaySystem confirm button
        [DebugEditable(DisplayName = "Button Width", Step = 1, Min = 80, Max = 600)]
        public int ButtonWidth { get; set; } = 220;

        [DebugEditable(DisplayName = "Button Height", Step = 1, Min = 24, Max = 200)]
        public int ButtonHeight { get; set; } = 64;

        [DebugEditable(DisplayName = "Button Offset Y", Step = 5, Min = -2000, Max = 2000)]
        public int ButtonOffsetY { get; set; } = 440;

        [DebugEditable(DisplayName = "Button Z", Step = 100, Min = 0, Max = 20000)]
        public int ButtonZ { get; set; } = 4000;

        [DebugEditable(DisplayName = "Text Scale", Step = 0.05f, Min = 0.2f, Max = 2.5f)]
        public float ButtonTextScale { get; set; } = 0.2f;

        public EndTurnDisplaySystem(EntityManager entityManager, GraphicsDevice gd, SpriteBatch sb, SpriteFont font) : base(entityManager)
        {
            _graphicsDevice = gd;
            _spriteBatch = sb;
            _font = font;
            _pixel = new Texture2D(gd, 1, 1);
            _pixel.SetData(new[] { Color.White });

            // Hook debug command to support button click via input system
            EventManager.Subscribe<DebugCommandEvent>(evt =>
            {
                if (evt.Command == "EndTurn")
                {
                    System.Console.WriteLine("[EndTurnDisplaySystem] DebugCommand EndTurn received");
                    OnEndTurnPressed();
                }
            });
        }

        protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
        {
            // Singletons; no per-entity iteration needed
            return System.Array.Empty<Entity>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

        private void OnEndTurnPressed()
        {
            EventQueue.EnqueueRule(new EventQueueBridge.QueuedPublish<ChangeBattlePhaseEvent>(
                "Rule.ChangePhase.PlayerEnd",
                new ChangeBattlePhaseEvent { Current = SubPhase.PlayerEnd }
            ));
            EventQueue.EnqueueRule(new EventQueueBridge.QueuedPublish<ChangeBattlePhaseEvent>(
                "Rule.ChangePhase.EnemyStart",
                new ChangeBattlePhaseEvent { Current = SubPhase.EnemyStart }
            ));
            EventQueue.EnqueueRule(new EventQueueBridge.QueuedPublish<ChangeBattlePhaseEvent>(
                "Rule.ChangePhase.PreBlock",
                new ChangeBattlePhaseEvent { Current = SubPhase.PreBlock }
            ));
            EventQueue.EnqueueRule(new EventQueueBridge.QueuedPublish<ChangeBattlePhaseEvent>(
                "Rule.ChangePhase.Block",
                new ChangeBattlePhaseEvent { Current = SubPhase.Block }
            ));
        }

        [DebugAction("Trigger End Turn Now")]
        public void Debug_EndTurnNow()
        {
            OnEndTurnPressed();
        }

        private Rectangle GetButtonRect(Viewport vp)
        {
            int x = (int)(vp.Width * 0.5f - ButtonWidth * 0.5f);
            int y = vp.Height - ButtonHeight - 40 - ButtonOffsetY; // above bottom UI band
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
            // Only show in Action phase
            var phaseEntity = EntityManager.GetEntitiesWithComponent<PhaseState>().FirstOrDefault();
            if (phaseEntity == null) return;
            var phase = phaseEntity.GetComponent<PhaseState>();
            if (phase.Sub != SubPhase.Action) return;

            var vp = _graphicsDevice.Viewport;
            var btnRect = GetButtonRect(vp);

            // Draw button
            _spriteBatch.Draw(_pixel, btnRect, new Color(40, 120, 40, 220));
            DrawRect(btnRect, Color.White, 2);
            string label = "End Turn";
            var size = _font.MeasureString(label) * ButtonTextScale;
            var posText = new Vector2(btnRect.Center.X - size.X / 2f, btnRect.Center.Y - size.Y / 2f);
            _spriteBatch.DrawString(_font, label, posText, Color.White, 0f, Vector2.Zero, ButtonTextScale, SpriteEffects.None, 0f);

            // Ensure a clickable UI entity exists and stays in sync
            var endBtn = EntityManager.GetEntitiesWithComponent<UIButton>().FirstOrDefault(e => e.GetComponent<UIButton>().Command == "EndTurn");
            if (endBtn == null)
            {
                endBtn = EntityManager.CreateEntity("UIButton_EndTurn");
                EntityManager.AddComponent(endBtn, new UIButton { Label = "End Turn", Command = "EndTurn" });
                EntityManager.AddComponent(endBtn, new Transform { Position = new Vector2(btnRect.X, btnRect.Y), ZOrder = ButtonZ });
                EntityManager.AddComponent(endBtn, new UIElement { Bounds = btnRect, IsInteractable = true });
            }
            else
            {
                var ui = endBtn.GetComponent<UIElement>();
                var tr = endBtn.GetComponent<Transform>();
                if (ui != null)
                {
                    ui.Bounds = btnRect;
                    ui.IsInteractable = true; // keep clickable in case other systems disabled
                }
                if (tr != null)
                {
                    tr.ZOrder = ButtonZ;
                    tr.Position = new Vector2(btnRect.X, btnRect.Y);
                }
            }
        }
    }
}


