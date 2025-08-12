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
        private float _scrollOffset = 0f; // vertical scroll for panel content

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
            // Constrain panel height to viewport with a bottom margin
            int bottomMargin = 40;
            int maxPanelHeight = Math.Max(120, _graphicsDevice.Viewport.Height - bottomMargin - panelY);
            bool needScroll = panelHeight > maxPanelHeight;
            int displayHeight = needScroll ? maxPanelHeight : panelHeight;
            var panelRect = new Rectangle(panelX, panelY, panelWidth, displayHeight);
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

            // Dropdown for tabs
            // Create/find a dropdown entity to manage selection
            var ddEntity = EntityManager.GetEntitiesWithComponent<UIDropdown>().FirstOrDefault(e => e.GetComponent<UIDropdown>().Owner == e);
            if (ddEntity == null)
            {
                ddEntity = EntityManager.CreateEntity("DebugMenu_TabDropdown");
                var dd = new UIDropdown { Items = systems.Select(s => s.name).ToList(), SelectedIndex = Math.Clamp(menu.ActiveTabIndex, 0, systems.Count - 1), RowHeight = 28, TextScale = textScale };
                var ddBounds = new Rectangle(panelX + padding, cursorY, panelWidth - padding * 2, 36);
                EntityManager.AddComponent(ddEntity, dd);
                EntityManager.AddComponent(ddEntity, new UIElement { Bounds = ddBounds, IsInteractable = true });
                // Set very high Z so dropdown (and especially its options) stays on top of other UI
                EntityManager.AddComponent(ddEntity, new Transform { Position = new Vector2(ddBounds.X, ddBounds.Y), ZOrder = 50000 });
            }
            else
            {
                var dd = ddEntity.GetComponent<UIDropdown>();
                var uiDD = ddEntity.GetComponent<UIElement>();
                var tDD = ddEntity.GetComponent<Transform>();
                dd.Items = systems.Select(s => s.name).ToList();
                dd.SelectedIndex = Math.Clamp(menu.ActiveTabIndex, 0, systems.Count - 1);
                uiDD.Bounds = new Rectangle(panelX + padding, cursorY, panelWidth - padding * 2, 36);
                if (tDD != null)
                {
                    // Boost ZOrder while open to ensure options render above everything and capture hover
                    tDD.ZOrder = dd.IsOpen ? 60000 : 50000;
                }
            }
            var ddCurrent = ddEntity.GetComponent<UIDropdown>();
            var ddUI = ddEntity.GetComponent<UIElement>();

            // Draw dropdown closed state (the bar)
            DrawFilledRect(ddUI.Bounds, new Color(35, 35, 35));
            DrawRect(ddUI.Bounds, Color.White, 1);
            string ddLabel = (ddCurrent.SelectedIndex >= 0 && ddCurrent.SelectedIndex < ddCurrent.Items.Count) ? ddCurrent.Items[ddCurrent.SelectedIndex] : "";
            DrawStringScaled(ddLabel, new Vector2(ddUI.Bounds.X + 8, ddUI.Bounds.Y + 4), Color.White, textScale);

            if (click && ddUI.Bounds.Contains(mouse.Position))
            {
                ddCurrent.IsOpen = !ddCurrent.IsOpen;
            }

            // Defer drawing the open list until AFTER fields so it overlays everything
            bool drawListAfter = ddCurrent.IsOpen;
            int deferredItemCount = ddCurrent.Items.Count;
            int deferredRowH = ddCurrent.RowHeight;
            // Open the dropdown list upwards so it doesn't cover the action buttons below
            var deferredListRect = new Rectangle(ddUI.Bounds.X, ddUI.Bounds.Y - deferredItemCount * deferredRowH, ddUI.Bounds.Width, deferredItemCount * deferredRowH);

            if (ddCurrent.SelectedIndex != menu.ActiveTabIndex)
            {
                menu.ActiveTabIndex = Math.Clamp(ddCurrent.SelectedIndex, 0, systems.Count - 1);
            }
            int dropdownHeight = ddUI.Bounds.Height;
            cursorY += dropdownHeight + spacing;

            // Scroll handling for content below the dropdown
            int headerHeight = cursorY - panelY; // title + dropdown + spacing consumed so far
            int scrollAreaHeight = Math.Max(0, displayHeight - headerHeight - padding);
            float maxScroll = Math.Max(0, (panelHeight - headerHeight - padding) - scrollAreaHeight);
            if (!needScroll)
            {
                _scrollOffset = 0f;
            }
            else
            {
                int wheelDelta = mouse.ScrollWheelValue - _prevMouse.ScrollWheelValue; // + when scrolled up
                float deltaPixels = -(wheelDelta / 120f) * 48f; // ~48px per notch
                _scrollOffset = MathHelper.Clamp(_scrollOffset + deltaPixels, 0f, maxScroll);
            }
            int yOffset = -(int)Math.Round(_scrollOffset);

            // Visible region bounds for culling
            int visibleTop = panelY + headerHeight;
            int visibleBottom = panelY + displayHeight - padding;

            // Fields for active tab (scrollable)
            foreach (var m in members)
            {
                var (label, getter, setter, type, attr) = m;
                string display = string.IsNullOrWhiteSpace(attr.DisplayName) ? label : attr.DisplayName;
                object val = getter();

                var rowRect = new Rectangle(panelX + padding, cursorY + yOffset, panelWidth - padding * 2, rowH);
                if (rowRect.Bottom < visibleTop || rowRect.Y > visibleBottom)
                {
                    cursorY += rowH + spacing;
                    continue;
                }
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

            // Buttons section (existing) - also scrollable
            var uiButtons = EntityManager.GetEntitiesWithComponent<UIButton>().ToList();
            if (_font != null && uiButtons.Count > 0)
            {
                int headerY = cursorY + yOffset;
                if (headerY + (int)(_font.LineSpacing * textScale) >= visibleTop && headerY <= visibleBottom)
                {
                    DrawStringScaled("Buttons", new Vector2(panelX + padding, headerY), Color.LightGreen, textScale);
                }
                cursorY += (int)(_font.LineSpacing * textScale) + spacing;

                foreach (var e in uiButtons)
                {
                    var btn = e.GetComponent<UIButton>();
                    var btnUI = e.GetComponent<UIElement>();
                    if (btn == null || btnUI == null) continue;

                    var rect = new Rectangle(panelX + padding, cursorY + yOffset, panelWidth - padding * 2, buttonH);
                    if (rect.Bottom < visibleTop || rect.Y > visibleBottom)
                    {
                        cursorY += buttonH + spacing;
                        continue;
                    }
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

            // Finally, if dropdown is open, draw its list on top of everything else in the panel
            if (drawListAfter)
            {
                DrawFilledRect(deferredListRect, new Color(25, 25, 25));
                DrawRect(deferredListRect, Color.White, 1);
                for (int i = 0; i < deferredItemCount; i++)
                {
                    var itemRect = new Rectangle(deferredListRect.X, deferredListRect.Y + i * deferredRowH, deferredListRect.Width, deferredRowH);
                    bool hover = itemRect.Contains(mouse.Position);
                    if (hover) DrawFilledRect(itemRect, new Color(60, 60, 60));
                    DrawStringScaled(systems[i].name, new Vector2(itemRect.X + 8, itemRect.Y + 4), Color.White, textScale);
                    DrawRect(itemRect, Color.White, 1);
                    if (click && hover)
                    {
                        var dd = ddEntity.GetComponent<UIDropdown>();
                        dd.SelectedIndex = i;
                        dd.IsOpen = false;
                        menu.ActiveTabIndex = i;
                    }
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

