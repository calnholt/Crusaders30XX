using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Save;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("Climb Columns")]
	public class ClimbColumnDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly ContentManager _content;
		private readonly Texture2D _pixel;
		private readonly Dictionary<string, Texture2D> _textureCache = new();

		[DebugEditable(DisplayName = "Columns Top", Step = 1, Min = 80, Max = 300)]
		public int ColumnsTop { get; set; } = 114;
		[DebugEditable(DisplayName = "Columns Max Width", Step = 1, Min = 800, Max = 1900)]
		public int ColumnsMaxWidth { get; set; } = 1500;
		[DebugEditable(DisplayName = "Columns Gap", Step = 1, Min = 0, Max = 80)]
		public int ColumnsGap { get; set; } = 20;
		[DebugEditable(DisplayName = "Columns Padding X", Step = 1, Min = 0, Max = 120)]
		public int ColumnsPaddingX { get; set; } = 32;
		[DebugEditable(DisplayName = "Column Padding", Step = 1, Min = 0, Max = 80)]
		public int ColumnPadding { get; set; } = 16;
		[DebugEditable(DisplayName = "Column Radius", Step = 1, Min = 0, Max = 24)]
		public int ColumnRadius { get; set; } = 4;
		[DebugEditable(DisplayName = "Column Title Font Scale", Step = 0.01f, Min = 0.05f, Max = 0.5f)]
		public float ColumnTitleFontScale { get; set; } = 0.15f;
		[DebugEditable(DisplayName = "Column Subtitle Font Scale", Step = 0.01f, Min = 0.03f, Max = 0.3f)]
		public float ColumnSubtitleFontScale { get; set; } = 0.09f;
		[DebugEditable(DisplayName = "Slot Gap", Step = 1, Min = 0, Max = 40)]
		public int SlotGap { get; set; } = 6;
		[DebugEditable(DisplayName = "Slot Ring Padding", Step = 1, Min = 0, Max = 40)]
		public int SlotRingPadding { get; set; } = 10;
		[DebugEditable(DisplayName = "Compact Padding X", Step = 1, Min = 0, Max = 40)]
		public int CompactPaddingX { get; set; } = 10;
		[DebugEditable(DisplayName = "Compact Padding Y", Step = 1, Min = 0, Max = 40)]
		public int CompactPaddingY { get; set; } = 8;
		[DebugEditable(DisplayName = "Compact Radius", Step = 1, Min = 0, Max = 24)]
		public int CompactRadius { get; set; } = 3;
		[DebugEditable(DisplayName = "Compact Title Font Scale", Step = 0.01f, Min = 0.04f, Max = 0.3f)]
		public float CompactTitleFontScale { get; set; } = 0.10f;
		[DebugEditable(DisplayName = "Compact Badge Font Scale", Step = 0.01f, Min = 0.03f, Max = 0.25f)]
		public float CompactBadgeFontScale { get; set; } = 0.07f;
		[DebugEditable(DisplayName = "Compact Meta Font Scale", Step = 0.01f, Min = 0.03f, Max = 0.25f)]
		public float CompactMetaFontScale { get; set; } = 0.08f;
		[DebugEditable(DisplayName = "Enemy Portrait Height", Step = 1, Min = 60, Max = 260)]
		public int EnemyPortraitHeight { get; set; } = 148;
		[DebugEditable(DisplayName = "Meta Block Min Height", Step = 1, Min = 20, Max = 100)]
		public int MetaBlockMinHeight { get; set; } = 36;
		[DebugEditable(DisplayName = "Compact Resource Icon Size", Step = 1, Min = 6, Max = 36)]
		public int CompactResourceIconSize { get; set; } = 12;
		[DebugEditable(DisplayName = "Compact Hourglass Width", Step = 1, Min = 4, Max = 32)]
		public int CompactHourglassWidth { get; set; } = 10;
		[DebugEditable(DisplayName = "Compact Hourglass Height", Step = 1, Min = 4, Max = 40)]
		public int CompactHourglassHeight { get; set; } = 14;

		internal static int ColumnsTopValue { get; private set; } = 114;
		internal static int ColumnsMaxWidthValue { get; private set; } = 1500;
		internal static int ColumnsGapValue { get; private set; } = 20;
		internal static int ColumnPaddingValue { get; private set; } = 16;
		internal static int SlotGapValue { get; private set; } = 6;

		public ClimbColumnDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, ContentManager content)
			: base(entityManager)
		{
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
			_content = content;
			_pixel = new Texture2D(graphicsDevice, 1, 1);
			_pixel.SetData(new[] { Color.White });
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			yield break;
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
		}

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);
			ColumnsTopValue = ColumnsTop;
			ColumnsMaxWidthValue = ColumnsMaxWidth;
			ColumnsGapValue = ColumnsGap;
			ColumnPaddingValue = ColumnPadding;
			SlotGapValue = SlotGap;
			if (IsClimbScene())
			{
				foreach (var slot in EntityManager.GetEntitiesWithComponent<ClimbSlotPresentation>()
					.Select(e => e.GetComponent<ClimbSlotPresentation>())
					.Where(slot => slot != null && !string.IsNullOrWhiteSpace(slot.PortraitAsset)))
				{
					EnsureTexture(slot.PortraitAsset);
				}
			}
		}

		public void Draw()
		{
			if (!IsClimbScene()) return;
			var preview = GetPreview();
			foreach (var entity in EntityManager.GetEntitiesWithComponent<ClimbColumnPresentation>())
			{
				DrawColumn(entity);
			}
			foreach (var entity in EntityManager.GetEntitiesWithComponent<ClimbSlotPresentation>())
			{
				DrawSlot(entity, preview);
			}
		}

		private void DrawColumn(Entity entity)
		{
			var ui = entity.GetComponent<UIElement>();
			var column = entity.GetComponent<ClimbColumnPresentation>();
			if (ui == null || column == null || ui.IsHidden || !column.IsVisible) return;

			var bounds = ui.Bounds;
			(Color top, Color bottom, Color border) = column.Kind switch
			{
				ClimbColumnKind.Shop => (Color.White * 0.10f, new Color(8, 8, 8) * 0.92f, Color.White * 0.55f),
				ClimbColumnKind.Event => (Color.Black * 0.35f, new Color(8, 8, 8) * 0.92f, Color.Black * 0.75f),
				_ => (ClimbSceneDrawHelpers.Red3 * 0.10f, new Color(8, 8, 8) * 0.92f, ClimbSceneDrawHelpers.Red3 * 0.45f),
			};
			ClimbSceneDrawHelpers.DrawVerticalGradient(_spriteBatch, _pixel, bounds, top, bottom, 16);
			ClimbSceneDrawHelpers.DrawBorder(_spriteBatch, _pixel, bounds, border, 2);

			var inner = column.InnerBounds;
			var titleColor = column.Kind == ClimbColumnKind.Encounter ? ClimbSceneDrawHelpers.White1 : ClimbSceneDrawHelpers.White2;
			ClimbSceneDrawHelpers.DrawText(_spriteBatch, column.Title, new Vector2(inner.X, inner.Y), ColumnTitleFontScale, titleColor);
			_spriteBatch.Draw(_pixel, new Rectangle(inner.X, inner.Y + 31, inner.Width, 2), column.Kind == ClimbColumnKind.Encounter ? ClimbSceneDrawHelpers.Red3 : border);
			ClimbSceneDrawHelpers.DrawText(_spriteBatch, column.Subtitle, new Vector2(inner.X, inner.Y + 37), ColumnSubtitleFontScale, ClimbSceneDrawHelpers.White2);
		}

		private void DrawSlot(Entity entity, ClimbPreviewState preview)
		{
			var ui = entity.GetComponent<UIElement>();
			var slot = entity.GetComponent<ClimbSlotPresentation>();
			if (ui == null || slot == null || ui.IsHidden || ui.Bounds.Width <= 0) return;

			bool source = preview?.IsActive == true && string.Equals(preview.SourceSlotId, slot.SlotId, StringComparison.OrdinalIgnoreCase);
			bool wouldVanish = preview?.IsActive == true && preview.WouldVanishSlotIds.Contains(slot.SlotId) && !source;
			bool affordableAfterPreview = slot.Kind == ClimbSlotKind.Shop && preview?.IsActive == true && preview.AffordableShopSlotIds.Contains(slot.SlotId);

			var ring = Inflate(ui.Bounds, SlotRingPadding);
			if (ui.IsHovered || source)
			{
				_spriteBatch.Draw(_pixel, ring, Color.White * 0.04f);
			}

			var rect = ui.Bounds;
			Color border = ResolveBorder(slot, ui, source, affordableAfterPreview);
			Color fill = new Color(8, 8, 8) * 0.92f;
			if (slot.IsUnavailable) fill *= 0.78f;
			_spriteBatch.Draw(_pixel, rect, fill);
			ClimbSceneDrawHelpers.DrawBorder(_spriteBatch, _pixel, rect, border, 2);

			if (slot.Kind == ClimbSlotKind.Encounter)
			{
				DrawEncounterSlot(rect, slot);
			}
			else if (slot.Kind == ClimbSlotKind.Event)
			{
				DrawEventSlot(rect, slot);
			}
			else
			{
				DrawShopSlot(rect, slot, affordableAfterPreview);
			}

			if (slot.IsSold)
			{
				DrawOverlay(rect, "SOLD", Color.Black * 0.55f, ClimbSceneDrawHelpers.White2);
			}
			else if (wouldVanish)
			{
				float pulse = 0.35f + 0.25f * (float)Math.Sin(DateTime.UtcNow.TimeOfDay.TotalSeconds * 3.5);
				_spriteBatch.Draw(_pixel, rect, ClimbSceneDrawHelpers.RedDim * pulse);
				ClimbSceneDrawHelpers.DrawBorder(_spriteBatch, _pixel, rect, ClimbSceneDrawHelpers.Red2, 2);
			}
			else if (slot.IsUnavailable)
			{
				_spriteBatch.Draw(_pixel, rect, Color.Black * 0.35f);
			}
		}

		private void DrawShopSlot(Rectangle rect, ClimbSlotPresentation slot, bool affordableAfterPreview)
		{
			var titlePos = new Vector2(rect.X + CompactPaddingX, rect.Y + CompactPaddingY);
			ClimbSceneDrawHelpers.DrawText(_spriteBatch, Trim(slot.Title, 30), titlePos, CompactTitleFontScale, ClimbSceneDrawHelpers.White1);
			ClimbSceneDrawHelpers.DrawText(_spriteBatch, slot.Label, new Vector2(rect.Right - 78, rect.Y + CompactPaddingY + 1), CompactBadgeFontScale, ClimbSceneDrawHelpers.White3);
			Color metaColor = affordableAfterPreview || !slot.IsAffordable ? ClimbSceneDrawHelpers.Red2 : ClimbSceneDrawHelpers.White2;
			DrawResourceLine(new Vector2(rect.X + CompactPaddingX, rect.Y + rect.Height - 24), slot.Cost, metaColor);
			if (slot.TimeCost > 0)
			{
				DrawTimeBlock(new Rectangle(rect.Right - 64, rect.Y + rect.Height - 28, 48, 24), slot.TimeCost);
			}
		}

		private void DrawEncounterSlot(Rectangle rect, ClimbSlotPresentation slot)
		{
			var portrait = new Rectangle(rect.X, rect.Y, rect.Width, Math.Min(EnemyPortraitHeight, rect.Height - 60));
			var texture = GetTexture(slot.PortraitAsset);
			if (texture != null)
			{
				_spriteBatch.Draw(texture, portrait, Color.White);
			}
			else
			{
				_spriteBatch.Draw(_pixel, portrait, ClimbSceneDrawHelpers.Red3 * 0.18f);
				ClimbSceneDrawHelpers.DrawText(_spriteBatch, Trim(slot.Title, 26), new Vector2(portrait.X + 14, portrait.Center.Y - 12), CompactTitleFontScale, ClimbSceneDrawHelpers.White1);
			}

			var detail = new Rectangle(rect.X, portrait.Bottom, rect.Width, rect.Bottom - portrait.Bottom);
			_spriteBatch.Draw(_pixel, detail, Color.Black * 0.45f);
			ClimbSceneDrawHelpers.DrawText(_spriteBatch, Trim(slot.Title, 28), new Vector2(detail.X + CompactPaddingX, detail.Y + 8), CompactTitleFontScale, ClimbSceneDrawHelpers.White1);
			DrawResourceLine(new Vector2(detail.X + CompactPaddingX, detail.Y + 34), slot.Reward, ClimbSceneDrawHelpers.White2);
			DrawTimeBlock(new Rectangle(detail.Right - 64, detail.Y + 26, 48, 24), slot.TimeCost);
		}

		private void DrawEventSlot(Rectangle rect, ClimbSlotPresentation slot)
		{
			ClimbSceneDrawHelpers.DrawText(_spriteBatch, "?", new Vector2(rect.X + 14, rect.Y + 10), 0.18f, ClimbSceneDrawHelpers.White2);
			ClimbSceneDrawHelpers.DrawText(_spriteBatch, slot.Title, new Vector2(rect.X + 48, rect.Y + 8), CompactTitleFontScale, ClimbSceneDrawHelpers.White1);
			ClimbSceneDrawHelpers.DrawText(_spriteBatch, slot.Label, new Vector2(rect.X + 48, rect.Y + 28), CompactBadgeFontScale, ClimbSceneDrawHelpers.White3);
			DrawTimeBlock(new Rectangle(rect.Right - 64, rect.Y + 14, 48, 24), slot.TimeCost);
		}

		private void DrawResourceLine(Vector2 position, ClimbResourceSave resources, Color color)
		{
			resources ??= new ClimbResourceSave { red = 0, white = 0, black = 0 };
			int x = (int)position.X;
			DrawSingleResource(ref x, (int)position.Y, ClimbResourceType.Red, resources.red, ClimbSceneDrawHelpers.Red2);
			DrawSingleResource(ref x, (int)position.Y, ClimbResourceType.White, resources.white, ClimbSceneDrawHelpers.White1);
			DrawSingleResource(ref x, (int)position.Y, ClimbResourceType.Black, resources.black, ClimbSceneDrawHelpers.White3);
			if (resources.red == 0 && resources.white == 0 && resources.black == 0)
			{
				ClimbSceneDrawHelpers.DrawText(_spriteBatch, "None", position, CompactMetaFontScale, color);
			}
		}

		private void DrawSingleResource(ref int x, int y, ClimbResourceType type, int amount, Color color)
		{
			if (amount <= 0) return;
			ClimbSceneDrawHelpers.DrawResourceIcon(_spriteBatch, _graphicsDevice, _pixel, new Vector2(x, y), type, CompactResourceIconSize, color);
			ClimbSceneDrawHelpers.DrawText(_spriteBatch, amount.ToString(), new Vector2(x + CompactResourceIconSize + 4, y - 2), CompactMetaFontScale, ClimbSceneDrawHelpers.White1);
			x += 42;
		}

		private void DrawTimeBlock(Rectangle rect, int time)
		{
			_spriteBatch.Draw(_pixel, rect, Color.White * 0.05f);
			ClimbSceneDrawHelpers.DrawBorder(_spriteBatch, _pixel, rect, Color.White * 0.20f, 1);
			var icon = new Rectangle(rect.X + 6, rect.Y + 5, CompactHourglassWidth, CompactHourglassHeight);
			ClimbSceneDrawHelpers.DrawHourglassIcon(_spriteBatch, _pixel, icon, ClimbSceneDrawHelpers.White3, ClimbSceneDrawHelpers.White2, true);
			ClimbSceneDrawHelpers.DrawText(_spriteBatch, time.ToString(), new Vector2(icon.Right + 6, rect.Y + 4), CompactMetaFontScale, ClimbSceneDrawHelpers.White1);
		}

		private void DrawOverlay(Rectangle rect, string text, Color fill, Color textColor)
		{
			_spriteBatch.Draw(_pixel, rect, fill);
			ClimbSceneDrawHelpers.DrawText(_spriteBatch, text, new Vector2(rect.Center.X - 22, rect.Center.Y - 8), 0.10f, textColor);
		}

		private Color ResolveBorder(ClimbSlotPresentation slot, UIElement ui, bool source, bool affordableAfterPreview)
		{
			if (source) return slot.Kind == ClimbSlotKind.Encounter ? ClimbSceneDrawHelpers.Red2 : ClimbSceneDrawHelpers.White1;
			if (affordableAfterPreview) return ClimbSceneDrawHelpers.Red2;
			if (slot.Kind == ClimbSlotKind.Shop && !slot.IsAffordable) return ClimbSceneDrawHelpers.RedDim;
			if (slot.IsUnavailable) return Color.White * 0.22f;
			if (ui.IsHovered) return ClimbSceneDrawHelpers.Red3;
			return slot.Kind switch
			{
				ClimbSlotKind.Encounter => ClimbSceneDrawHelpers.Red3 * 0.45f,
				ClimbSlotKind.Event => Color.Black * 0.75f,
				_ => Color.White * 0.35f,
			};
		}

		private Texture2D GetTexture(string asset)
		{
			if (string.IsNullOrWhiteSpace(asset)) return null;
			return _textureCache.TryGetValue(asset, out var cached) ? cached : null;
		}

		private void EnsureTexture(string asset)
		{
			if (string.IsNullOrWhiteSpace(asset) || _textureCache.ContainsKey(asset)) return;
			try
			{
				_textureCache[asset] = _content.Load<Texture2D>(asset);
			}
			catch
			{
				_textureCache[asset] = null;
			}
		}

		private ClimbPreviewState GetPreview()
		{
			return EntityManager.GetEntity(ClimbHeaderLayoutSystem.RootName)?.GetComponent<ClimbPreviewState>();
		}

		private bool IsClimbScene()
		{
			return EntityManager.GetEntitiesWithComponent<SceneState>()
				.FirstOrDefault()
				?.GetComponent<SceneState>()
				?.Current == SceneId.Climb;
		}

		private static Rectangle Inflate(Rectangle rect, int amount)
		{
			return new Rectangle(rect.X - amount, rect.Y - amount, rect.Width + amount * 2, rect.Height + amount * 2);
		}

		private static string Trim(string value, int max)
		{
			value = ClimbSceneDrawHelpers.ToAscii(value ?? string.Empty);
			if (value.Length <= max) return value;
			return value.Substring(0, Math.Max(0, max - 3)) + "...";
		}
	}
}
