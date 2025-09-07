using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Rendering;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Displays equipped equipment icons (head/chest/arms/legs) on the left side of the screen,
	/// grouped by type in vertical order: Head, Chest, Arms, Legs. Multiple items of the same
	/// type are drawn in a row for that type.
	/// </summary>
	[DebugTab("Equipment Display")] 
	public class EquipmentDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly ContentManager _content;
		private SpriteFont _font;
		private readonly Dictionary<string, Texture2D> _iconCache = new();
		private readonly Dictionary<(int w, int h, int r), Texture2D> _roundedRectCache = new();
		private readonly Dictionary<int, Entity> _tooltipByEquipEntityId = new();

		// Layout constants (pixels)
		[DebugEditable(DisplayName = "Left Margin", Step = 2, Min = 0, Max = 2000)]
		public int LeftMargin { get; set; } = 8;
		[DebugEditable(DisplayName = "Top Margin", Step = 2, Min = 0, Max = 2000)]
		public int TopMargin { get; set; } = 120;
		[DebugEditable(DisplayName = "Icon Size", Step = 1, Min = 8, Max = 512)]
		public int IconSize { get; set; } = 60;
		[DebugEditable(DisplayName = "Column Gap", Step = 1, Min = 0, Max = 128)]
		public int ColGap { get; set; } = 8;
		[DebugEditable(DisplayName = "Row Gap", Step = 1, Min = 0, Max = 128)]
		public int RowGap { get; set; } = 12;
		[DebugEditable(DisplayName = "Background Corner Radius", Step = 1, Min = 0, Max = 64)]
		public int BgCornerRadius { get; set; } = 23;
		[DebugEditable(DisplayName = "Background Border Thickness", Step = 1, Min = 0, Max = 16)]
		public int BgBorderThickness { get; set; } = 0;
		[DebugEditable(DisplayName = "Background Padding", Step = 1, Min = 0, Max = 64)]
		public int BgPadding { get; set; } = 28;
		[DebugEditable(DisplayName = "Icon Padding X", Step = 1, Min = 0, Max = 256)]
		public int IconPaddingX { get; set; } = 8;
		[DebugEditable(DisplayName = "Icon Padding Y", Step = 1, Min = 0, Max = 256)]
		public int IconPaddingY { get; set; } = 8;
		[DebugEditable(DisplayName = "Block Text Scale", Step = 0.05f, Min = 0.2f, Max = 3f)]
		public float BlockTextScale { get; set; } = 0.75f;
		[DebugEditable(DisplayName = "Shield Icon Height", Step = 1, Min = 8, Max = 128)]
		public int ShieldIconHeight { get; set; } = 36;
		[DebugEditable(DisplayName = "Shield Gap", Step = 1, Min = 0, Max = 64)]
		public int ShieldGap { get; set; } = 2;
		[DebugEditable(DisplayName = "Background Opacity", Step = 0.05f, Min = 0f, Max = 1f)]
		public float BgOpacity { get; set; } = 0.75f;
		[DebugEditable(DisplayName = "Shield Offset X", Step = 1, Min = -200, Max = 200)]
		public int ShieldOffsetX { get; set; } = 0;
		[DebugEditable(DisplayName = "Shield Offset Y", Step = 1, Min = -200, Max = 200)]
		public int ShieldOffsetY { get; set; } = -2;

		public EquipmentDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, ContentManager content, SpriteFont font)
			: base(entityManager)
		{
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
			_content = content;
			_font = font;
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<Player>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		public void Draw()
		{
			var player = GetRelevantEntities().FirstOrDefault();
			if (player == null) return;

			// Gather equipment for this player
			var equipment = EntityManager.GetEntitiesWithComponent<EquippedEquipment>()
				.Where(e => e.GetComponent<EquippedEquipment>().EquippedOwner == player)
				.Select(e => e.GetComponent<EquippedEquipment>())
				.ToList();

			if (equipment.Count == 0) return;

			// Group and order types
			string[] order = new[] { "Head", "Chest", "Arms", "Legs" };
			int y = TopMargin;
			foreach (var type in order)
			{
				var items = equipment.Where(eq => string.Equals(eq.EquipmentType, type, StringComparison.OrdinalIgnoreCase)).ToList();
				if (items.Count == 0) continue;
				// Draw items in a row
				int x = LeftMargin;
				foreach (var item in items)
				{
					// Resolve equipment definition for visuals and tooltip
					int bgW = IconSize + BgPadding * 2;
					int bgH = IconSize + BgPadding * 2;
					var bgRect = new Rectangle(x, y, bgW, bgH);
					var fillColor = ResolveFillColor(item);
					DrawRoundedBackground(bgRect, fillColor);

					// Draw icon fixed to the top-left of the rounded square
					var tex = GetOrLoadIcon(type);
					if (tex != null)
					{
						var iconRect = new Rectangle(bgRect.X + IconPaddingX, bgRect.Y + IconPaddingY, IconSize, IconSize);
						_spriteBatch.Draw(tex, iconRect, Color.White);
					}

					// Draw block value and shield icon at bottom-left
					DrawBlockAndShield(item, bgRect, fillColor);

					// Create/update tooltip hover rect
					UpdateTooltip(item, bgRect);

					x += bgW + ColGap;
				}
				y += (IconSize + BgPadding * 2) + RowGap;
			}
		}

		private void DrawRoundedBackground(Rectangle rect, Color fill)
		{
			int w = rect.Width;
			int h = rect.Height;
			int rOuter = Math.Max(0, BgCornerRadius);
			int rInner = Math.Max(0, BgCornerRadius - BgBorderThickness);
			var outer = GetRoundedRectTexture(w, h, rOuter);
			var inner = GetRoundedRectTexture(Math.Max(1, w - BgBorderThickness * 2), Math.Max(1, h - BgBorderThickness * 2), rInner);
			var center = new Vector2(rect.X + w / 2f, rect.Y + h / 2f);
			if (BgBorderThickness > 0)
			{
				_spriteBatch.Draw(outer, center, null, Color.Black, 0f, new Vector2(outer.Width / 2f, outer.Height / 2f), 1f, SpriteEffects.None, 0f);
			}
			byte a = (byte)(MathHelper.Clamp(BgOpacity, 0f, 1f) * 255f);
			var fillWithAlpha = new Color(fill.R, fill.G, fill.B, a);
			_spriteBatch.Draw(inner, center, null, fillWithAlpha, 0f, new Vector2(inner.Width / 2f, inner.Height / 2f), 1f, SpriteEffects.None, 0f);
		}

		private Texture2D GetRoundedRectTexture(int width, int height, int radius)
		{
			var key = (width, height, radius);
			if (_roundedRectCache.TryGetValue(key, out var tex) && tex != null) return tex;
			var texture = RoundedRectTextureFactory.CreateRoundedRect(_graphicsDevice, width, height, radius);
			_roundedRectCache[key] = texture;
			return texture;
		}

		private void DrawBlockAndShield(EquippedEquipment item, Rectangle bgRect, Color fillColor)
		{
			if (_font == null) return;
			int block = 0;
			try
			{
				if (Crusaders30XX.ECS.Data.Equipment.EquipmentDefinitionCache.TryGet(item.EquipmentId, out var def))
				{
					// def.block may be string or numeric in JSON; handle both by parsing to int
					if (def != null && def.block != null)
					{
						int parsed;
						if (int.TryParse(def.block.ToString(), out parsed)) block = parsed;
					}
				}
			}
			catch { }
			if (block <= 0) return;

			var textColor = GetTextColorForFill(fillColor);
			string blockText = block.ToString();
			var textSize = _font.MeasureString(blockText) * BlockTextScale;
			float marginX = 8f;
			float marginY = 8f;
			float baselineY = bgRect.Y + bgRect.Height - marginY;
			float numberX = bgRect.X + marginX;
			float numberY = baselineY - textSize.Y;
			_spriteBatch.DrawString(_font, blockText, new Vector2(numberX, numberY), textColor, 0f, Vector2.Zero, BlockTextScale, SpriteEffects.None, 0f);

			var shield = SafeLoadTexture("shield");
			if (shield != null)
			{
				float iconH = Math.Max(8, ShieldIconHeight);
				float iconW = shield.Height > 0 ? iconH * (shield.Width / (float)shield.Height) : iconH;
				float gap = Math.Max(0, ShieldGap);
				float iconX = numberX + textSize.X + gap + ShieldOffsetX;
				float iconY = baselineY - iconH + ShieldOffsetY;
				_spriteBatch.Draw(shield, new Rectangle((int)iconX, (int)iconY, (int)iconW, (int)iconH), Color.White);
			}
		}

		private void UpdateTooltip(EquippedEquipment item, Rectangle rect)
		{
			if (!_tooltipByEquipEntityId.TryGetValue(item.Owner.Id, out var uiEntity) || uiEntity == null)
			{
				uiEntity = EntityManager.CreateEntity($"UI_EquipTooltip_{item.Owner.Id}");
				EntityManager.AddComponent(uiEntity, new Transform { Position = new Vector2(rect.X, rect.Y), ZOrder = 10001 });
				EntityManager.AddComponent(uiEntity, new UIElement { Bounds = rect, IsInteractable = true, Tooltip = BuildTooltipText(item) });
				_tooltipByEquipEntityId[item.Owner.Id] = uiEntity;
			}
			else
			{
				var tr = uiEntity.GetComponent<Transform>();
				if (tr != null) tr.Position = new Vector2(rect.X, rect.Y);
				var ui = uiEntity.GetComponent<UIElement>();
				if (ui != null)
				{
					ui.Bounds = rect;
					ui.Tooltip = BuildTooltipText(item);
				}
			}
		}

		private string BuildTooltipText(EquippedEquipment item)
		{
			try
			{
				if (Crusaders30XX.ECS.Data.Equipment.EquipmentDefinitionCache.TryGet(item.EquipmentId, out var def) && def != null)
				{
					string name = string.IsNullOrWhiteSpace(def.name) ? item.EquipmentId : def.name;
					var parts = new List<string>();
					if (def.abilities != null)
					{
						foreach (var a in def.abilities)
						{
							if (!string.IsNullOrWhiteSpace(a.text)) parts.Add(a.text);
						}
					}
					string abilities = string.Join("\n", parts);
					return string.IsNullOrEmpty(abilities) ? name : (name + "\n\n" + abilities);
				}
			}
			catch { }
			return item.EquipmentId ?? string.Empty;
		}

		private Texture2D SafeLoadTexture(string asset)
		{
			try { return _content.Load<Texture2D>(asset); } catch { return null; }
		}

		private Color ResolveFillColor(EquippedEquipment item)
		{
			try
			{
				if (Crusaders30XX.ECS.Data.Equipment.EquipmentDefinitionCache.TryGet(item.EquipmentId, out var def) && def != null)
				{
					string c = def.color?.Trim()?.ToLowerInvariant();
					switch (c)
					{
						case "red": return Color.DarkRed;
						case "white": return Color.White;
						case "black": return Color.Black;
						default: return Color.Gray;
					}
				}
			}
			catch { }
			return Color.Gray;
		}

		private Color GetTextColorForFill(Color fill)
		{
			// Match card logic: black text on white fill, white otherwise
			if (fill == Color.White) return Color.Black;
			return Color.White;
		}

		private Texture2D GetOrLoadIcon(string type)
		{
			string key = type.ToLowerInvariant();
			if (_iconCache.TryGetValue(key, out var t) && t != null) return t;
			string assetName = key; // assumes head.png, chest.png, arms.png, legs.png in Content root
			try
			{
				var tex = _content.Load<Texture2D>(assetName);
				_iconCache[key] = tex;
				return tex;
			}
			catch
			{
				System.Console.WriteLine($"[EquipmentDisplaySystem] Missing icon for type '{type}' (expected content asset '{assetName}')");
				_iconCache[key] = null;
				return null;
			}
		}
	}
}


