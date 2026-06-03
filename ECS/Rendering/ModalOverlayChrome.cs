using System.Collections.Generic;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Rendering
{
	public static class ModalOverlayPalette
	{
		public static readonly Color ModalFill = new Color(8, 8, 8) * 0.92f;
		public static readonly Color PanelBorder = new Color(255, 255, 255) * 0.85f;
		public static readonly Color InsetHighlight = new Color(255, 255, 255) * 0.08f;
		public static readonly Color FooterFill = new Color(0, 0, 0) * 0.25f;
		public static readonly Color FooterBorderTop = new Color(255, 255, 255) * 0.12f;
		public static readonly Color TitleColor = Color.White;
		public static readonly Color BodyTextColor = new Color(240, 236, 230);
		public static readonly Color RedRuleCenter = new Color(196, 30, 58);
		public static readonly Color ButtonFill = new Color(30, 30, 30);
		public static readonly Color ButtonFillHover = new Color(160, 0, 0);
		public static readonly Color ButtonBorder = Color.White;
		public static readonly Color ButtonBorderHover = new Color(196, 30, 58);
		public static readonly Color DropShadow = new Color(0, 0, 0) * 0.75f;
	}

	public struct ModalShellLayout
	{
		public Rectangle Modal;
		public Rectangle Content;
		public Rectangle Body;
		public Rectangle Footer;

		public static ModalShellLayout ComputeCentered(
			int vw,
			int vh,
			int modalW,
			int modalH,
			int borderThickness,
			int footerHeight)
		{
			int border = System.Math.Max(1, borderThickness);
			int modalX = (vw - modalW) / 2;
			int modalY = (vh - modalH) / 2;
			var modal = new Rectangle(modalX, modalY, modalW, modalH);
			var content = new Rectangle(
				modal.X + border,
				modal.Y + border,
				System.Math.Max(1, modal.Width - border * 2),
				System.Math.Max(1, modal.Height - border * 2));

			int footerH = System.Math.Max(1, footerHeight);
			int bodyH = System.Math.Max(1, content.Height - footerH);
			var footer = new Rectangle(content.X, content.Y + bodyH, content.Width, footerH);
			var body = new Rectangle(content.X, content.Y, content.Width, bodyH);

			return new ModalShellLayout
			{
				Modal = modal,
				Content = content,
				Body = body,
				Footer = footer
			};
		}
	}

	public static class ModalOverlayChrome
	{
		public static void DrawDim(SpriteBatch spriteBatch, Texture2D pixel, int vw, int vh, int dimAlpha)
		{
			spriteBatch.Draw(pixel, new Rectangle(0, 0, vw, vh), new Color(0, 0, 0, System.Math.Clamp(dimAlpha, 0, 255)));
		}

		public static void DrawDropShadow(SpriteBatch spriteBatch, Texture2D pixel, Rectangle modalRect, int offsetY, Color shadowColor)
		{
			int shadowY = System.Math.Max(0, offsetY);
			int shadowH = System.Math.Max(1, modalRect.Height - shadowY);
			var shadow = new Rectangle(modalRect.X, modalRect.Y + shadowY, modalRect.Width, shadowH);
			spriteBatch.Draw(pixel, shadow, shadowColor);
		}

		public static void DrawModalRegions(
			SpriteBatch spriteBatch,
			Texture2D pixel,
			Rectangle modal,
			Rectangle content,
			Rectangle footer,
			int borderThickness)
		{
			spriteBatch.Draw(pixel, modal, ModalOverlayPalette.ModalFill);
			if (footer.Width > 0 && footer.Height > 0)
			{
				spriteBatch.Draw(pixel, footer, ModalOverlayPalette.FooterFill);
				spriteBatch.Draw(pixel, new Rectangle(footer.X, footer.Y, footer.Width, 1), ModalOverlayPalette.FooterBorderTop);
			}
			DrawInsetHighlight(spriteBatch, pixel, content);
			DrawBorder(spriteBatch, pixel, modal, ModalOverlayPalette.PanelBorder, borderThickness);
		}

		public static void DrawBorder(SpriteBatch spriteBatch, Texture2D pixel, Rectangle r, Color color, int thickness)
		{
			int t = System.Math.Max(1, thickness);
			spriteBatch.Draw(pixel, new Rectangle(r.X, r.Y, r.Width, t), color);
			spriteBatch.Draw(pixel, new Rectangle(r.X, r.Bottom - t, r.Width, t), color);
			spriteBatch.Draw(pixel, new Rectangle(r.X, r.Y, t, r.Height), color);
			spriteBatch.Draw(pixel, new Rectangle(r.Right - t, r.Y, t, r.Height), color);
		}

		public static void DrawInsetHighlight(SpriteBatch spriteBatch, Texture2D pixel, Rectangle contentRect)
		{
			if (contentRect.Width <= 0 || contentRect.Height <= 0) return;
			DrawBorder(spriteBatch, pixel, contentRect, ModalOverlayPalette.InsetHighlight, 1);
		}

		public static void DrawActionButton(
			SpriteBatch spriteBatch,
			Texture2D pixel,
			Rectangle rect,
			bool hovered,
			int borderThickness,
			SpriteFont font,
			string label,
			Vector2 textPos,
			float textScale,
			Color textColor)
		{
			var fill = hovered ? ModalOverlayPalette.ButtonFillHover : ModalOverlayPalette.ButtonFill;
			var border = hovered ? ModalOverlayPalette.ButtonBorderHover : ModalOverlayPalette.ButtonBorder;
			spriteBatch.Draw(pixel, rect, fill);
			DrawBorder(spriteBatch, pixel, rect, border, borderThickness);
			if (font != null && !string.IsNullOrEmpty(label))
			{
				spriteBatch.DrawString(font, label, textPos, textColor, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);
			}
		}
	}

	public sealed class HorizontalGradientRuleCache
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly Dictionary<(int w, int h), Texture2D> _cache = new();

		public HorizontalGradientRuleCache(GraphicsDevice graphicsDevice)
		{
			_graphicsDevice = graphicsDevice;
			EventManager.Subscribe<DeleteCachesEvent>(_ => DisposeAll());
		}

		public void DrawRule(SpriteBatch spriteBatch, int centerX, int centerY, int width, int height)
		{
			if (width < 1) width = 1;
			if (height < 1) height = 1;
			var tex = GetOrCreate(width, height);
			int half = width / 2;
			spriteBatch.Draw(tex, new Rectangle(centerX - half, centerY, width, height), Color.White);
		}

		private Texture2D GetOrCreate(int width, int height)
		{
			var key = (width, height);
			if (_cache.TryGetValue(key, out var existing) && existing != null && !existing.IsDisposed)
				return existing;

			const int strips = 9;
			int stripW = System.Math.Max(1, width / strips);
			var data = new Color[width * height];
			for (int i = 0; i < strips; i++)
			{
				float t = i / (float)(strips - 1);
				float dist = System.Math.Abs(t - 0.5f) * 2f;
				float alpha = 1f - dist;
				var c = ModalOverlayPalette.RedRuleCenter * alpha;
				int x0 = i * stripW;
				for (int px = 0; px < stripW && x0 + px < width; px++)
				{
					for (int y = 0; y < height; y++)
						data[y * width + x0 + px] = c;
				}
			}

			var tex = new Texture2D(_graphicsDevice, width, height);
			tex.SetData(data);
			_cache[key] = tex;
			return tex;
		}

		public void DisposeAll()
		{
			foreach (var kv in _cache)
			{
				try { kv.Value?.Dispose(); } catch { }
			}
			_cache.Clear();
		}
	}
}
