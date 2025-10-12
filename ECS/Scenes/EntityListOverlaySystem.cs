using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Diagnostics;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// Draws a simple window listing all active entities in the world.
    /// </summary>
    public class EntityListOverlaySystem : Core.System
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font;
        private Texture2D _pixel;
        private MouseState _prevMouse;

        public EntityListOverlaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, SpriteFont font)
            : base(entityManager)
        {
            _graphicsDevice = graphicsDevice;
            _spriteBatch = spriteBatch;
            _font = font;
            _pixel = new Texture2D(_graphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });
            _prevMouse = Mouse.GetState();
        }

        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<EntityListOverlay>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
            // no-op
        }

        public void Draw()
        {
			var overlayEntity = GetRelevantEntities().FirstOrDefault();
			if (overlayEntity == null) return;
			var overlay = overlayEntity.GetComponent<EntityListOverlay>();
			var ui = overlayEntity.GetComponent<UIElement>();
			if (ui != null) ui.IsInteractable = overlay != null && overlay.IsOpen;
			if (overlay == null || !overlay.IsOpen) return;

            var mouse = Mouse.GetState();
            bool click = mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released;
            bool wheelMoved = mouse.ScrollWheelValue != _prevMouse.ScrollWheelValue;
            if (wheelMoved)
            {
                float delta = (mouse.ScrollWheelValue - _prevMouse.ScrollWheelValue) / 120f; // notches
                overlay.ScrollOffset = Math.Max(0f, overlay.ScrollOffset - delta * overlay.RowHeight * 2f);
            }

            // Panel
            int x = overlay.PanelX;
            int y = overlay.PanelY;
            int w = Math.Min(overlay.PanelWidth, _graphicsDevice.Viewport.Width - x - 10);
            int h = Math.Min(overlay.PanelHeight, _graphicsDevice.Viewport.Height - y - 10);
            var panelRect = new Rectangle(x, y, w, h);
            Fill(panelRect, new Color(10, 10, 10, 200));
            Stroke(panelRect, Color.White, 1);

            int cursorY = y + overlay.Padding;
            // Title
            string title = "Entities";
            if (_font != null)
            {
                _spriteBatch.DrawString(_font, title, new Vector2(x + overlay.Padding, cursorY), Color.White, 0f, Vector2.Zero, overlay.TextScale, SpriteEffects.None, 0f);
                // Copy button at top-right
                int btnW = 120;
                int btnH = overlay.RowHeight;
                var copyRect = new Rectangle(x + w - overlay.Padding - btnW, y + overlay.Padding, btnW, btnH);
                bool hoverCopy = copyRect.Contains(mouse.Position);
                var copyBg = hoverCopy ? new Color(120, 120, 120) : new Color(70, 70, 70);
                Fill(copyRect, copyBg);
                Stroke(copyRect, Color.White, 1);
                _spriteBatch.DrawString(_font, "Copy", new Vector2(copyRect.X + 10, copyRect.Y + 3), Color.White, 0f, Vector2.Zero, overlay.TextScale, SpriteEffects.None, 0f);
                if (click && hoverCopy)
                {
                    string export = BuildEntityListExport();
                    TryCopyToClipboard(export);
                }
                cursorY += (int)Math.Round(_font.LineSpacing * overlay.TextScale) + overlay.Padding;
            }

            // Header row
            string hdr = "ID   Name";
            _spriteBatch.DrawString(_font, hdr, new Vector2(x + overlay.Padding, cursorY), Color.LightGreen, 0f, Vector2.Zero, overlay.TextScale, SpriteEffects.None, 0f);
            cursorY += overlay.RowHeight;

            int visibleTop = y + overlay.Padding;
            int visibleBottom = y + h - overlay.Padding;

            // Entities sorted by Id
            var all = EntityManager.GetAllEntities().OrderBy(e => e.Id).ToList();
            int startIndex = (int)(overlay.ScrollOffset / overlay.RowHeight);
            int maxRows = Math.Max(0, (visibleBottom - cursorY) / overlay.RowHeight) + 1;
            for (int i = 0; i < maxRows; i++)
            {
                int idx = startIndex + i;
                if (idx < 0 || idx >= all.Count) break;
                int rowY = cursorY + i * overlay.RowHeight - (int)(overlay.ScrollOffset % overlay.RowHeight);
                if (rowY + overlay.RowHeight < visibleTop || rowY > visibleBottom) continue;
                var e = all[idx];
                string line = $"{e.Id,3}   {e.Name}";
                _spriteBatch.DrawString(_font, line, new Vector2(x + overlay.Padding, rowY), Color.White, 0f, Vector2.Zero, overlay.TextScale, SpriteEffects.None, 0f);
            }

            _prevMouse = mouse;
        }

        private string BuildEntityListExport()
        {
            var all = EntityManager.GetAllEntities().OrderBy(e => e.Id).ToList();
            var lines = new List<string>(all.Count + 1);
            lines.Add("ID\tName");
            foreach (var e in all)
            {
                lines.Add($"{e.Id}\t{e.Name}");
            }
            return string.Join(Environment.NewLine, lines);
        }

        private static void TryCopyToClipboard(string text)
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    var psi = new ProcessStartInfo("cmd.exe", "/c clip")
                    {
                        UseShellExecute = false,
                        RedirectStandardInput = true,
                        CreateNoWindow = true
                    };
                    using var p = Process.Start(psi);
                    if (p != null)
                    {
                        p.StandardInput.Write(text);
                        p.StandardInput.Close();
                        p.WaitForExit(2000);
                    }
                }
                else if (OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst())
                {
                    var psi = new ProcessStartInfo("/usr/bin/pbcopy")
                    {
                        UseShellExecute = false,
                        RedirectStandardInput = true,
                        CreateNoWindow = true
                    };
                    using var p = Process.Start(psi);
                    if (p != null)
                    {
                        p.StandardInput.Write(text);
                        p.StandardInput.Close();
                        p.WaitForExit(2000);
                    }
                }
                else if (OperatingSystem.IsLinux())
                {
                    bool copied = false;
                    try
                    {
                        var psi = new ProcessStartInfo("xclip", "-selection clipboard")
                        {
                            UseShellExecute = false,
                            RedirectStandardInput = true,
                            CreateNoWindow = true
                        };
                        using var p = Process.Start(psi);
                        if (p != null)
                        {
                            p.StandardInput.Write(text);
                            p.StandardInput.Close();
                            p.WaitForExit(2000);
                            copied = p.ExitCode == 0;
                        }
                    }
                    catch { }
                    if (!copied)
                    {
                        try
                        {
                            var psi2 = new ProcessStartInfo("xsel", "--clipboard --input")
                            {
                                UseShellExecute = false,
                                RedirectStandardInput = true,
                                CreateNoWindow = true
                            };
                            using var p2 = Process.Start(psi2);
                            if (p2 != null)
                            {
                                p2.StandardInput.Write(text);
                                p2.StandardInput.Close();
                                p2.WaitForExit(2000);
                                copied = p2.ExitCode == 0;
                            }
                        }
                        catch { }
                    }
                    if (!copied)
                    {
                        Console.WriteLine("[Clipboard] xclip/xsel not available. Export below:\n" + text);
                    }
                }
                else
                {
                    Console.WriteLine("[Clipboard] Copy not supported on this OS. Export below:\n" + text);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Clipboard] Failed: " + ex.Message + "\n" + text);
            }
        }

        private void Fill(Rectangle rect, Color color)
        {
            _spriteBatch.Draw(_pixel, rect, color);
        }

        private void Stroke(Rectangle rect, Color color, int thickness)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
            _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            _spriteBatch.Draw(_pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
        }
    }
}


