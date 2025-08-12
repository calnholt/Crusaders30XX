using System;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.Diagnostics;
using System.Reflection;
using Microsoft.Xna.Framework.Input;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// System to render a simple toggleable debug menu and its buttons
    /// </summary>
    public class DebugMenuSystem : Core.System
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font;
        private readonly SystemManager _systemManager;
        private Texture2D _pixel;
        private MouseState _prevMouse;
        private DateTime _lastDrawTime = DateTime.UtcNow;

        private class HoldState
        {
            public Rectangle Rect;
            public Func<object> Getter;
            public Action<object> Setter;
            public Type Type;
            public DebugEditableAttribute Attr;
            public float Step;
            public float Sign; // +1 or -1
            public double HeldSeconds;
            public double RepeatAccumulator;
        }

        private HoldState _hold;

        public DebugMenuSystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, SpriteFont font, SystemManager systemManager)
            : base(entityManager)
        {
            _graphicsDevice = graphicsDevice;
            _spriteBatch = spriteBatch;
            _font = font;
            _systemManager = systemManager;
            _pixel = new Texture2D(graphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });
            _prevMouse = Mouse.GetState();
        }

        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<DebugMenu>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
            // No periodic updates needed
        }

        public void Draw()
        {
            var menuEntity = GetRelevantEntities().FirstOrDefault();
            if (menuEntity == null) return;

            var menu = menuEntity.GetComponent<DebugMenu>();
            var transform = menuEntity.GetComponent<Transform>();
            var ui = menuEntity.GetComponent<UIElement>();

            if (menu == null || transform == null || ui == null) return;
            if (!menu.IsOpen) return;

            var mouse = Mouse.GetState();
            var now = DateTime.UtcNow;
            double dt = (now - _lastDrawTime).TotalSeconds;
            _lastDrawTime = now;
            bool click = mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released;

            // Layout constants
            int viewportW = _graphicsDevice.Viewport.Width;
            int margin = 20;
            int panelWidth = 420;
            int padding = 12;
            int spacing = 8;
            int rowH = 36;
            int buttonH = 22;
            float titleScale = 0.6f;
            float textScale = 0.55f;

            int panelX = viewportW - panelWidth - margin;
            int panelY = margin + 60;

            // Build tabs from annotated systems
            var systems = GetAnnotatedSystems(_systemManager.GetAllSystems()).OrderBy(t => t.name).ToList();
            if (systems.Count == 0)
            {
                var panelRectEmpty = new Rectangle(panelX, panelY, panelWidth, 80);
                ui.Bounds = panelRectEmpty;
                DrawFilledRect(panelRectEmpty, new Color(15, 30, 55) * 0.95f);
                DrawRect(panelRectEmpty, Color.White, 2);
                _prevMouse = mouse;
                return;
            }
            if (menu.ActiveTabIndex < 0 || menu.ActiveTabIndex >= systems.Count)
                menu.ActiveTabIndex = 0;

            // First pass: measure height
            int measureY = panelY + padding;
            if (_font != null)
            {
                measureY += (int)(_font.LineSpacing * titleScale) + spacing; // title height
            }

            // Tab bar height
            int tabH = 36;
            int tabBarY = measureY;
            measureY += tabH + spacing;

            // Active tab fields count
            var active = systems[menu.ActiveTabIndex];
            var members = GetEditableMembers(active.sys);
            int fieldsCount = members.Count;
            measureY += fieldsCount * (rowH + spacing);

            // Buttons section (existing UIButtons)
            var buttons = EntityManager.GetEntitiesWithComponent<UIButton>().ToList();
            if (_font != null && buttons.Count > 0)
            {
                measureY += (int)(_font.LineSpacing * textScale) + spacing; // header
                measureY += buttons.Count * (buttonH + spacing);
            }

            int panelHeight = measureY - panelY + padding;
            var panelRect = new Rectangle(panelX, panelY, panelWidth, panelHeight);
            ui.Bounds = panelRect;

            // Draw panel and title
            DrawFilledRect(panelRect, new Color(15, 30, 55) * 0.95f);
            DrawRect(panelRect, Color.White, 2);

            int cursorY = panelY + padding;
            if (_font != null)
            {
                DrawStringScaled("Debug Menu", new Vector2(panelX + padding, cursorY), Color.White, titleScale);
                cursorY += (int)(_font.LineSpacing * titleScale) + spacing;
            }

            // Draw tabs
            int tabX = panelX + padding;
            int tabY = cursorY;
            int tabH2 = 36;
            for (int i = 0; i < systems.Count; i++)
            {
                string tab = systems[i].name;
                int tabW = (int)(_font.MeasureString(tab).X * textScale) + 24;
                var rect = new Rectangle(tabX, tabY, tabW, tabH2);
                var bg = (i == menu.ActiveTabIndex) ? new Color(60, 90, 140) : new Color(50, 50, 50);
                DrawFilledRect(rect, bg);
                DrawRect(rect, Color.White, 1);
                DrawStringScaled(tab, new Vector2(rect.X + 10, rect.Y + 4), Color.White, textScale);

                if (click && rect.Contains(mouse.Position))
                    menu.ActiveTabIndex = i;

                tabX += tabW + 6;
            }
            cursorY = tabY + tabH2 + spacing;

            // Fields for active tab
            foreach (var m in members)
            {
                var (label, getter, setter, type, attr) = m;
                string display = string.IsNullOrWhiteSpace(attr.DisplayName) ? label : attr.DisplayName;
                object val = getter();

                var rowRect = new Rectangle(panelX + padding, cursorY, panelWidth - padding * 2, rowH);
                DrawFilledRect(rowRect, new Color(30, 30, 30));
                DrawRect(rowRect, Color.White, 1);
                int right = rowRect.Right - 8;

                if (type == typeof(bool))
                {
                    string vs = ((bool)val) ? "ON" : "OFF";
                    var txtSize = _font.MeasureString(vs) * textScale;
                    var valRect = new Rectangle(right - (int)txtSize.X - 10, rowRect.Y + 3, (int)txtSize.X + 10, rowH - 6);
                    // Label clipped to available space
                    int labelMaxWidth = Math.Max(0, valRect.X - 6 - (rowRect.X + 8));
                    DrawStringClippedScaled(display, new Vector2(rowRect.X + 8, rowRect.Y + 4), Color.LightGray, labelMaxWidth, textScale);
                    DrawFilledRect(valRect, new Color(70, 70, 70));
                    DrawRect(valRect, Color.White, 1);
                    DrawStringScaled(vs, new Vector2(valRect.X + 5, valRect.Y + 2), Color.White, textScale);
                    if (click && valRect.Contains(mouse.Position)) setter(!(bool)val);
                }
                else if (type == typeof(int) || type == typeof(float) || type == typeof(byte))
                {
                    int btnW = 22;
                    var plusRect = new Rectangle(right - btnW, rowRect.Y + 3, btnW, rowH - 6);
                    var valRect = new Rectangle(plusRect.X - 100, rowRect.Y + 3, 100, rowH - 6);
                    var minusRect = new Rectangle(valRect.X - btnW, rowRect.Y + 3, btnW, rowH - 6);

                    // Label clipped to available space
                    int labelMaxWidth = Math.Max(0, minusRect.X - 6 - (rowRect.X + 8));
                    DrawStringClippedScaled(display, new Vector2(rowRect.X + 8, rowRect.Y + 4), Color.LightGray, labelMaxWidth, textScale);

                    DrawFilledRect(minusRect, new Color(70, 70, 70));
                    DrawRect(minusRect, Color.White, 1);
                    DrawStringScaled("-", new Vector2(minusRect.X + 7, minusRect.Y + 2), Color.White, textScale);

                    DrawFilledRect(valRect, new Color(50, 50, 50));
                    DrawRect(valRect, Color.White, 1);
                    string vs = type == typeof(float) ? $"{Convert.ToSingle(val):0.###}" : Convert.ToInt32(val).ToString();
                    DrawStringScaled(vs, new Vector2(valRect.X + 6, valRect.Y + 2), Color.White, textScale);

                    DrawFilledRect(plusRect, new Color(70, 70, 70));
                    DrawRect(plusRect, Color.White, 1);
                    DrawStringScaled("+", new Vector2(plusRect.X + 6, plusRect.Y + 2), Color.White, textScale);

                    float step = attr.Step <= 0f ? 1f : attr.Step;
                    if (click)
                    {
                        if (minusRect.Contains(mouse.Position))
                        {
                            ApplyNumericDelta(type, setter, val, -step, attr);
                            _hold = new HoldState { Rect = minusRect, Getter = getter, Setter = setter, Type = type, Attr = attr, Step = step, Sign = -1f, HeldSeconds = 0, RepeatAccumulator = 0 };
                        }
                        else if (plusRect.Contains(mouse.Position))
                        {
                            ApplyNumericDelta(type, setter, val, +step, attr);
                            _hold = new HoldState { Rect = plusRect, Getter = getter, Setter = setter, Type = type, Attr = attr, Step = step, Sign = +1f, HeldSeconds = 0, RepeatAccumulator = 0 };
                        }
                    }
                    // Holding logic
                    if (mouse.LeftButton == ButtonState.Pressed && _hold != null)
                    {
                        // Refresh current rect positions to keep tracking in case layout shifts slightly
                        if ((_hold.Rect == minusRect || _hold.Rect == plusRect) && _hold.Rect.Contains(mouse.Position))
                        {
                            _hold.HeldSeconds += dt;
                            _hold.RepeatAccumulator += dt;
                            double initialDelay = 0.4;
                            if (_hold.HeldSeconds >= initialDelay)
                            {
                                double interval = _hold.HeldSeconds > 2.0 ? 0.02 : (_hold.HeldSeconds > 1.0 ? 0.05 : 0.1);
                                while (_hold.RepeatAccumulator >= interval)
                                {
                                    var cur = _hold.Getter();
                                    ApplyNumericDelta(_hold.Type, _hold.Setter, cur, _hold.Step * _hold.Sign, _hold.Attr);
                                    _hold.RepeatAccumulator -= interval;
                                }
                            }
                        }
                        else if (!_hold.Rect.Contains(mouse.Position))
                        {
                            _hold = null; // moved off the button
                        }
                    }
                    else
                    {
                        _hold = null; // released
                    }
                }
                else
                {
                    string vs = val?.ToString() ?? "null";
                    var txtSize = _font.MeasureString(vs) * textScale;
                    var valRect = new Rectangle(right - (int)txtSize.X - 10, rowRect.Y + 3, (int)txtSize.X + 10, rowH - 6);
                    int labelMaxWidth = Math.Max(0, valRect.X - 6 - (rowRect.X + 8));
                    DrawStringClippedScaled(display, new Vector2(rowRect.X + 8, rowRect.Y + 4), Color.LightGray, labelMaxWidth, textScale);
                    DrawFilledRect(valRect, new Color(50, 50, 50));
                    DrawRect(valRect, Color.White, 1);
                    DrawStringScaled(vs, new Vector2(valRect.X + 5, valRect.Y + 2), Color.White, textScale);
                }

                cursorY += rowH + spacing;
            }

            // Buttons section (existing)
            var uiButtons = EntityManager.GetEntitiesWithComponent<UIButton>().ToList();
            if (_font != null && uiButtons.Count > 0)
            {
                DrawStringScaled("Buttons", new Vector2(panelX + padding, cursorY), Color.LightGreen, textScale);
                cursorY += (int)(_font.LineSpacing * textScale) + spacing;

                foreach (var e in uiButtons)
                {
                    var btn = e.GetComponent<UIButton>();
                    var btnUI = e.GetComponent<UIElement>();
                    if (btn == null || btnUI == null) continue;

                    var rect = new Rectangle(panelX + padding, cursorY, panelWidth - padding * 2, buttonH);
                    btnUI.Bounds = rect;

                    var bgColor = btnUI.IsHovered ? new Color(120, 120, 120) : new Color(70, 70, 70);
                    DrawFilledRect(rect, bgColor);
                    DrawRect(rect, Color.White, 1);

                    if (_font != null && !string.IsNullOrEmpty(btn.Label))
                    {
                        var size = _font.MeasureString(btn.Label) * textScale;
                        int textX = rect.X + (int)((rect.Width - size.X) / 2f);
                        int textY = rect.Y + (int)((rect.Height - size.Y) / 2f);
                        DrawStringScaled(btn.Label, new Vector2(textX, textY), Color.White, textScale);
                    }
                    cursorY += buttonH + spacing;
                }
            }

            _prevMouse = mouse;
        }

        private static IEnumerable<(string name, Core.System sys)> GetAnnotatedSystems(IEnumerable<Core.System> systems)
        {
            foreach (var s in systems)
            {
                var attr = s.GetType().GetCustomAttribute<DebugTabAttribute>();
                if (attr != null)
                {
                    yield return (string.IsNullOrWhiteSpace(attr.Name) ? s.GetType().Name : attr.Name, s);
                }
            }
        }

        private static List<(string label, Func<object> get, Action<object> set, Type type, DebugEditableAttribute attr)>
            GetEditableMembers(Core.System system)
        {
            var list = new List<(string, Func<object>, Action<object>, Type, DebugEditableAttribute)>();
            var t = system.GetType();

            foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var attr = p.GetCustomAttribute<DebugEditableAttribute>();
                if (attr == null) continue;
                if (!p.CanRead || !p.CanWrite) continue;
                var localP = p;
                list.Add((
                    localP.Name,
                    () => localP.GetValue(system),
                    v => localP.SetValue(system, v),
                    localP.PropertyType,
                    attr
                ));
            }

            foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                var attr = f.GetCustomAttribute<DebugEditableAttribute>();
                if (attr == null) continue;
                var localF = f;
                list.Add((
                    localF.Name,
                    () => localF.GetValue(system),
                    v => localF.SetValue(system, v),
                    localF.FieldType,
                    attr
                ));
            }

            return list;
        }

        private static void ApplyNumericDelta(Type type, Action<object> setter, object current, float delta, DebugEditableAttribute attr)
        {
            if (type == typeof(int))
            {
                int v = Convert.ToInt32(current);
                int nv = v + (int)Math.Round(delta);
                nv = ClampWithAttr(nv, attr);
                setter(nv);
            }
            else if (type == typeof(float))
            {
                float v = Convert.ToSingle(current);
                float nv = v + delta;
                nv = ClampWithAttr(nv, attr);
                setter(nv);
            }
            else if (type == typeof(byte))
            {
                int v = Convert.ToInt32(current);
                int nv = v + (int)Math.Round(delta);
                nv = Math.Max(0, Math.Min(255, nv));
                if (!float.IsNaN(attr.Min)) nv = Math.Max(nv, (int)attr.Min);
                if (!float.IsNaN(attr.Max)) nv = Math.Min(nv, (int)attr.Max);
                setter((byte)nv);
            }
        }

        private static int ClampWithAttr(int v, DebugEditableAttribute attr)
        {
            if (!float.IsNaN(attr.Min)) v = Math.Max(v, (int)attr.Min);
            if (!float.IsNaN(attr.Max)) v = Math.Min(v, (int)attr.Max);
            return v;
        }

        private static float ClampWithAttr(float v, DebugEditableAttribute attr)
        {
            if (!float.IsNaN(attr.Min)) v = Math.Max(v, attr.Min);
            if (!float.IsNaN(attr.Max)) v = Math.Min(v, attr.Max);
            return v;
        }

        private void DrawFilledRect(Rectangle rect, Color color)
        {
            _spriteBatch.Draw(_pixel, rect, color);
        }

        private void DrawRect(Rectangle rect, Color color, int thickness)
        {
            // top
            _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            // bottom
            _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
            // left
            _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            // right
            _spriteBatch.Draw(_pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
        }

        private void DrawStringScaled(string text, Vector2 position, Color color, float scale)
        {
            if (_font == null || string.IsNullOrEmpty(text)) return;
            _spriteBatch.DrawString(_font, text, position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        private void DrawStringClippedScaled(string text, Vector2 position, Color color, int maxWidth, float scale)
        {
            if (_font == null || string.IsNullOrEmpty(text)) return;
            float width = _font.MeasureString(text).X * scale;
            if (width <= maxWidth)
            {
                _spriteBatch.DrawString(_font, text, position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                return;
            }
            const string ellipsis = "...";
            float ellipsisWidth = _font.MeasureString(ellipsis).X * scale;
            string s = text;
            while (s.Length > 0 && _font.MeasureString(s).X * scale + ellipsisWidth > maxWidth)
            {
                s = s.Substring(0, s.Length - 1);
            }
            _spriteBatch.DrawString(_font, s + ellipsis, position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
    }
}

