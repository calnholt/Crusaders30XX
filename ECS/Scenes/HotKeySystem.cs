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
    public class HotKeySystem : Core.System
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font;
        private GamePadState _prevGamePadState;
        private Texture2D _circleTexSmall;

        [DebugEditable(DisplayName = "Hint Radius (px)", Step = 1f, Min = 4f, Max = 64f)]
        public int HintRadius { get; set; } = 20;

        [DebugEditable(DisplayName = "Hint X Gap (px)", Step = 1f, Min = -64f, Max = 128f)]
        public int HintGapX { get; set; } = 8;

        [DebugEditable(DisplayName = "Hint Y Gap (px)", Step = 1f, Min = -64f, Max = 128f)]
        public int HintGapY { get; set; } = 8;

        [DebugEditable(DisplayName = "Text Scale", Step = 0.05f, Min = 0.05f, Max = 2.5f)]
        public float TextScale { get; set; } = 0.15f;

        public HotKeySystem(EntityManager entityManager, GraphicsDevice gd, SpriteBatch sb, SpriteFont font)
            : base(entityManager)
        {
            _graphicsDevice = gd;
            _spriteBatch = sb;
            _font = font;
            _circleTexSmall = PrimitiveTextureFactory.GetAntiAliasedCircle(_graphicsDevice, HintRadius);
        }

        protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
        {
            // Process globally; we will iterate HotKey components during Update/Draw
            return EntityManager.GetEntitiesWithComponent<SceneState>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
            // Poll controller once per frame
            if (!Game1.WindowIsActive || TransitionStateSingleton.IsActive) return;
            var gp = GamePad.GetState(PlayerIndex.One);
            bool edgeY = gp.Buttons.Y == ButtonState.Pressed && _prevGamePadState.Buttons.Y == ButtonState.Released;
            bool edgeB = gp.Buttons.B == ButtonState.Pressed && _prevGamePadState.Buttons.B == ButtonState.Released;
            bool edgeX = gp.Buttons.X == ButtonState.Pressed && _prevGamePadState.Buttons.X == ButtonState.Released;

            if (edgeY || edgeB || edgeX)
            {
                FaceButton? pressed = null;
                if (edgeY) pressed = FaceButton.Y;
                else if (edgeB) pressed = FaceButton.B;
                else if (edgeX) pressed = FaceButton.X;

                // If any overlay UI is present, suppress default-layer hotkeys
                bool overlayPresent = EntityManager.GetEntitiesWithComponent<UIElement>()
                    .Any(e => {
                        var ui = e.GetComponent<UIElement>();
                        return ui != null && ui.LayerType == UILayerType.Overlay;
                    });

                // Choose top-most eligible entity with this hotkey by ZOrder
                var target = EntityManager.GetEntitiesWithComponent<HotKey>()
                    .Select(e => new { E = e, HK = e.GetComponent<HotKey>(), UI = e.GetComponent<UIElement>(), T = e.GetComponent<Transform>(), Btn = e.GetComponent<UIButton>() })
                    .Where(x => x.HK != null && x.UI != null && x.UI.IsInteractable && x.HK.Button == pressed && (!overlayPresent || x.UI.LayerType == UILayerType.Overlay))
                    .OrderByDescending(x => x.T?.ZOrder ?? 0)
                    .FirstOrDefault();
                if (target != null)
                {
                    if (target.Btn != null && !string.IsNullOrEmpty(target.Btn.Command))
                    {
                        EventManager.Publish(new DebugCommandEvent { Command = target.Btn.Command });
                    }
                    else if (target.UI.EventType != UIElementEventType.None)
                    {
                        UIElementEventDelegateService.HandleEvent(target.UI.EventType, target.E);
                    }
                    else
                    {
                        target.UI.IsClicked = true;
                    }
                }
            }

            _prevGamePadState = gp;
        }

        public void Draw()
        {
            // Draw hints for interactable HotKey UI elements
            bool overlayPresent = EntityManager.GetEntitiesWithComponent<UIElement>()
                .Any(e => {
                    var ui = e.GetComponent<UIElement>();
                    return ui != null && ui.LayerType == UILayerType.Overlay;
                });

            var items = EntityManager.GetEntitiesWithComponent<HotKey>()
                .Select(e => new { E = e, HK = e.GetComponent<HotKey>(), UI = e.GetComponent<UIElement>(), T = e.GetComponent<Transform>() })
                .Where(x => x.HK != null && x.UI != null && x.UI.IsInteractable && (!overlayPresent || x.UI.LayerType == UILayerType.Overlay))
                .OrderByDescending(x => x.T?.ZOrder ?? 0)
                .ToList();

            // Ensure radius texture matches current setting
            _circleTexSmall = PrimitiveTextureFactory.GetAntiAliasedCircle(_graphicsDevice, System.Math.Max(2, HintRadius));

            foreach (var x in items)
            {
                var r = x.UI.Bounds;
                if (r.Width < 2 || r.Height < 2) continue;
                int cx = r.Center.X;
                int cy = r.Bottom + System.Math.Max(-64, HintGapY) + (int)System.Math.Round(HintRadius * 1.1f);
                var pos = new Vector2(cx - HintRadius, cy - HintRadius);
                var color = GetFaceButtonColor(x.HK.Button);

                _spriteBatch.Draw(_circleTexSmall, pos, color);
                string letter = GetFaceButtonLetter(x.HK.Button);
                var size = _font.MeasureString(letter) * TextScale;
                var textPos = new Vector2(cx - size.X / 2f, cy - size.Y / 2f - 2f);
                _spriteBatch.DrawString(_font, letter, textPos, Color.Black, 0f, Vector2.Zero, TextScale, SpriteEffects.None, 0f);
            }
        }

        private static string GetFaceButtonLetter(FaceButton b)
        {
            switch (b)
            {
                case FaceButton.B: return "B";
                case FaceButton.X: return "X";
                case FaceButton.Y: return "Y";
                default: return "?";
            }
        }

        private static Color GetFaceButtonColor(FaceButton b)
        {
            switch (b)
            {
                case FaceButton.B: return new Color(220, 50, 50);
                case FaceButton.X: return new Color(60, 120, 220);
                case FaceButton.Y: return new Color(220, 200, 60);
                default: return Color.White;
            }
        }
    }
}


