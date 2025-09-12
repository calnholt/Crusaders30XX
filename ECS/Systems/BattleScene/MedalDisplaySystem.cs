using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System.Collections.Generic;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Data.Medals;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("Medal Display")]
	public class MedalDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly ContentManager _content;
		private SpriteFont _font;
		private Texture2D _medalTex;
		private Texture2D _roundedCache;
		private int _roundedW, _roundedH, _roundedR;

		// Layout/debug controls
		[DebugEditable(DisplayName = "Left Margin", Step = 2, Min = 0, Max = 2000)]
		public int LeftMargin { get; set; } = 10;
		[DebugEditable(DisplayName = "Top Margin", Step = 2, Min = 0, Max = 2000)]
		public int TopMargin { get; set; } = 10;
		[DebugEditable(DisplayName = "Icon Size", Step = 1, Min = 8, Max = 512)]
		public int IconSize { get; set; } = 48;
		[DebugEditable(DisplayName = "Spacing X", Step = 1, Min = 0, Max = 256)]
		public int SpacingX { get; set; } = 10;
		[DebugEditable(DisplayName = "Background Corner Radius", Step = 1, Min = 0, Max = 64)]
		public int BgCornerRadius { get; set; } = 16;
		[DebugEditable(DisplayName = "Background Padding", Step = 1, Min = 0, Max = 64)]
		public int BgPadding { get; set; } = 8;
		[DebugEditable(DisplayName = "Background Opacity", Step = 0.05f, Min = 0f, Max = 1f)]
		public float BgOpacity { get; set; } = 0.75f;

		public MedalDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, ContentManager content, SpriteFont font)
			: base(entityManager)
		{
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
			_content = content;
			_font = font;
			TryLoadAssets();
		}

		private void TryLoadAssets()
		{
			try { _medalTex = _content.Load<Texture2D>("medal"); } catch { _medalTex = null; }
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<Player>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		public void Draw()
		{
			var player = GetRelevantEntities().FirstOrDefault();
			if (player == null) return;
			var medals = EntityManager.GetEntitiesWithComponent<EquippedMedal>()
				.Where(e => e.GetComponent<EquippedMedal>().EquippedOwner == player)
				.Select(e => e.GetComponent<EquippedMedal>())
				.ToList();
			if (medals.Count == 0) return;

			int x = LeftMargin;
			int y = TopMargin;
			foreach (var m in medals)
			{
				int bgW = IconSize + BgPadding * 2;
				int bgH = IconSize + BgPadding * 2;
				var rect = new Rectangle(x, y, bgW, bgH);
				byte a = (byte)Microsoft.Xna.Framework.MathHelper.Clamp(BgOpacity * 255f, 0f, 255f);
				DrawRoundedBackground(rect, new Color((byte)0, (byte)0, (byte)0, a));
				UpdateTooltipForMedal(m, rect);
				DrawMedalIcon(rect);
				x += bgW + SpacingX;
			}
		}

		private void DrawMedalIcon(Rectangle bgRect)
		{
			if (_medalTex == null) return;
			// Fit medal into IconSize preserving aspect ratio
			int targetW = IconSize;
			int targetH = IconSize;
			if (_medalTex.Width > 0 && _medalTex.Height > 0)
			{
				float aspect = _medalTex.Width / (float)_medalTex.Height;
				if (aspect >= 1f) { targetW = IconSize; targetH = (int)System.Math.Round(IconSize / aspect); }
				else { targetH = IconSize; targetW = (int)System.Math.Round(IconSize * aspect); }
			}
			int innerX = bgRect.X + BgPadding;
			int innerY = bgRect.Y + BgPadding;
			int drawX = innerX + (IconSize - targetW) / 2;
			int drawY = innerY + (IconSize - targetH) / 2;
			_spriteBatch.Draw(_medalTex, new Rectangle(drawX, drawY, targetW, targetH), Color.White);
		}

		private void UpdateTooltipForMedal(EquippedMedal medal, Rectangle rect)
		{
			var ui = medal.Owner.GetComponent<UIElement>();
			if (ui == null)
			{
				ui = new UIElement { IsInteractable = true };
				EntityManager.AddComponent(medal.Owner, ui);
			}
			ui.Bounds = rect;
			ui.IsInteractable = true;
			var mouse = Microsoft.Xna.Framework.Input.Mouse.GetState();
			ui.IsHovered = rect.Contains(mouse.Position);
			ui.Tooltip = BuildMedalTooltip(medal);
			var t = medal.Owner.GetComponent<Transform>();
			if (t == null)
			{
				t = new Transform { Position = new Vector2(rect.X, rect.Y), ZOrder = 10001 };
				EntityManager.AddComponent(medal.Owner, t);
			}
			else
			{
				t.Position = new Vector2(rect.X, rect.Y);
				t.ZOrder = 10001;
			}
		}

		private string BuildMedalTooltip(EquippedMedal medal)
		{
			if (string.IsNullOrWhiteSpace(medal.MedalId)) return string.Empty;
			if (MedalDefinitionCache.TryGet(medal.MedalId, out var def) && def != null)
			{
				string name = string.IsNullOrWhiteSpace(def.name) ? medal.MedalId : def.name;
				string txt = def.text ?? string.Empty;
				return string.IsNullOrWhiteSpace(txt) ? name : (name + "\n\n" + txt);
			}
			return medal.MedalId;
		}

		private void DrawRoundedBackground(Rectangle rect, Color fill)
		{
			int w = rect.Width;
			int h = rect.Height;
			int r = System.Math.Max(0, BgCornerRadius);
			bool rebuild = _roundedCache == null || _roundedW != w || _roundedH != h || _roundedR != r;
			if (rebuild)
			{
				_roundedCache?.Dispose();
				_roundedCache = Crusaders30XX.ECS.Rendering.RoundedRectTextureFactory.CreateRoundedRect(_graphicsDevice, w, h, r);
				_roundedW = w; _roundedH = h; _roundedR = r;
			}
			var center = new Vector2(rect.X + w / 2f, rect.Y + h / 2f);
			_spriteBatch.Draw(_roundedCache, center, null, fill, 0f, new Vector2(_roundedCache.Width / 2f, _roundedCache.Height / 2f), 1f, SpriteEffects.None, 0f);
		}
	}
}


