using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Crusaders30XX.Diagnostics;
using System.Collections.Generic;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("How To Play Overlay")] 
	public class HowToPlayOverlaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly SpriteFont _font;
		private Texture2D _pixel;
		private float _scrollY = 0f;
		private int _prevWheel = 0;
		private bool _wasOpen = false;
		private bool _prevLeftDown = false;

		[DebugEditable(DisplayName = "Backdrop Alpha (0-255)", Step = 5, Min = 0, Max = 255)]
		public int BackdropAlpha { get; set; } = 140;

		[DebugEditable(DisplayName = "Panel Width", Step = 10, Min = 100, Max = 2000)]
		public int PanelWidth { get; set; } = 900;

		[DebugEditable(DisplayName = "Panel Height", Step = 10, Min = 100, Max = 2000)]
		public int PanelHeight { get; set; } = 620;

		[DebugEditable(DisplayName = "Text Scale", Step = 0.05f, Min = 0.3f, Max = 2f)]
		public float TextScale { get; set; } = 0.175f;

		public HowToPlayOverlaySystem(EntityManager em, GraphicsDevice gd, SpriteBatch sb, SpriteFont font) : base(em)
		{
			_graphicsDevice = gd;
			_spriteBatch = sb;
			_font = font;
			_pixel = new Texture2D(gd, 1, 1);
			_pixel.SetData(new[] { Color.White });
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<HowToPlayOverlay>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			// Close on click anywhere; handle mouse wheel scrolling when open
			var state = entity.GetComponent<HowToPlayOverlay>();
			if (state == null) return;
			var mouse = Microsoft.Xna.Framework.Input.Mouse.GetState();
			if (state.IsOpen)
			{
				if (!_wasOpen) { _scrollY = 0f; _prevLeftDown = mouse.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed; }
				int wheel = mouse.ScrollWheelValue;
				int delta = wheel - _prevWheel;
				if (delta != 0)
				{
					_scrollY = System.Math.Max(0f, _scrollY - delta * 0.25f);
				}
				bool leftNow = mouse.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed;
				if (leftNow && !_prevLeftDown)
				{
					state.IsOpen = false;
				}
				_prevLeftDown = leftNow;
			}
			_prevWheel = mouse.ScrollWheelValue;
			_wasOpen = state.IsOpen;
		}

		// For contexts where this system isn't registered in the SystemManager, allow manual input handling
		public void UpdateFromMenu()
		{
			var e = GetRelevantEntities().FirstOrDefault();
			if (e == null) return;
			UpdateEntity(e, new GameTime());
		}

		public void Draw()
		{
			var st = GetRelevantEntities().FirstOrDefault()?.GetComponent<HowToPlayOverlay>();
			if (st == null || !st.IsOpen || _font == null) return;

			int vw = _graphicsDevice.Viewport.Width;
			int vh = _graphicsDevice.Viewport.Height;
			// Backdrop
			var back = new Color(0, 0, 0, System.Math.Clamp(BackdropAlpha, 0, 255));
			_spriteBatch.Draw(_pixel, new Rectangle(0, 0, vw, vh), back);

			// Panel centered
			int w = System.Math.Min(PanelWidth, vw - 40);
			int h = System.Math.Min(PanelHeight, vh - 40);
			var rect = new Rectangle((vw - w) / 2, (vh - h) / 2, w, h);
			_spriteBatch.Draw(_pixel, rect, Color.Black);

			// Title
			string title = "How To Play";
			var tsize = _font.MeasureString(title) * 0.9f;
			var tpos = new Vector2(rect.X + (w - tsize.X) / 2f, rect.Y + 16);
			_spriteBatch.DrawString(_font, title, tpos + new Vector2(1, 1), Color.Black, 0f, Vector2.Zero, 0.225f, SpriteEffects.None, 0f);
			_spriteBatch.DrawString(_font, title, tpos, Color.White, 0f, Vector2.Zero, 0.225f, SpriteEffects.None, 0f);

			// Reserve bottom space for close hint (outside scrollable content)
			string hint = "Click anywhere to close";
			var hsize = _font.MeasureString(hint) * 0.6f;
			int bottomReserved = (int)System.Math.Ceiling(hsize.Y + 20); // keep hint outside scroll area

			// Placeholder body text (wrapped within panel) with scrolling
			string body = st.Text ?? "Placeholder: How to play text goes here.";
			int padding = 24;
			int contentW = System.Math.Max(10, w - padding * 2);
			var lines = WrapText(body, TextScale, contentW).ToList();
			float lineStep = _font.LineSpacing * TextScale;
			float contentH = lines.Count * lineStep;
			float viewTop = rect.Y + 60;
			float viewBottom = rect.Bottom - bottomReserved;
			float maxScroll = System.Math.Max(0f, contentH - (viewBottom - viewTop));
			if (_scrollY > maxScroll) _scrollY = maxScroll;
			float lineY = viewTop - _scrollY;
			for (int i = 0; i < lines.Count; i++)
			{
				string line = lines[i];
				var lsize = _font.MeasureString(line) * TextScale;
				var lpos = new Vector2(rect.X + padding, lineY);
				// cull lines outside the view
				if (lineY + lsize.Y >= viewTop && lineY <= viewBottom)
				{
					_spriteBatch.DrawString(_font, line, lpos + new Vector2(1, 1), Color.Black, 0f, Vector2.Zero, TextScale, SpriteEffects.None, 0f);
					_spriteBatch.DrawString(_font, line, lpos, Color.White, 0f, Vector2.Zero, TextScale, SpriteEffects.None, 0f);
				}
				lineY += lineStep;
			}

			// Optional scrollbar
			if (maxScroll > 0.5f)
			{
				int barW = 6;
				int trackX = rect.Right - barW - 6;
				int trackY = (int)viewTop;
				int trackH = (int)(viewBottom - viewTop);
				// track
				_spriteBatch.Draw(_pixel, new Rectangle(trackX, trackY, barW, trackH), new Color(255, 255, 255, 30));
				// thumb size proportional to view/content
				float ratio = (viewBottom - viewTop) / contentH;
				int thumbH = System.Math.Max(12, (int)(trackH * ratio));
				int thumbY = trackY + (int)((trackH - thumbH) * (_scrollY / maxScroll));
				_spriteBatch.Draw(_pixel, new Rectangle(trackX, thumbY, barW, thumbH), new Color(255, 255, 255, 120));
			}

			// Close hint
			var hpos = new Vector2(rect.X + (w - hsize.X) / 2f, rect.Bottom - hsize.Y - 12);
			_spriteBatch.DrawString(_font, hint, hpos, Color.Gray, 0f, Vector2.Zero, 0.15f, SpriteEffects.None, 0f);
		}

		private IEnumerable<string> WrapText(string text, float scale, int maxWidth)
		{
			if (string.IsNullOrEmpty(text)) yield break;
			text = text.Replace("\r", string.Empty);
			var paragraphs = text.Split('\n');
			foreach (var para in paragraphs)
			{
				string current = string.Empty;
				var words = para.Split(' ');
				foreach (var raw in words)
				{
					var word = raw;
					string test = string.IsNullOrEmpty(current) ? word : current + " " + word;
					var size = _font.MeasureString(test) * scale;
					if (size.X <= maxWidth)
					{
						current = test;
					}
					else
					{
						if (!string.IsNullOrEmpty(current)) { yield return current; }
						// If single word longer than maxWidth, hard-break it
						if ((_font.MeasureString(word) * scale).X > maxWidth)
						{
							string remainder = word;
							while ((_font.MeasureString(remainder) * scale).X > maxWidth && remainder.Length > 1)
							{
								int cut = System.Math.Max(1, (int)(remainder.Length * 0.8));
								string chunk = remainder.Substring(0, cut);
								while ((_font.MeasureString(chunk) * scale).X > maxWidth && cut > 1) { cut--; chunk = remainder.Substring(0, cut); }
								yield return chunk;
								remainder = remainder.Substring(cut);
							}
							current = remainder;
						}
						else
						{
							current = word;
						}
					}
				}
				if (!string.IsNullOrEmpty(current)) yield return current;
				// blank line between paragraphs
				yield return string.Empty;
			}
		}
	}
}


