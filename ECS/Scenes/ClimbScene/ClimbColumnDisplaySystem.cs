using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Save;
using Crusaders30XX.ECS.Services;
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
		private float _vanishPreviewAlpha;

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
		public int SlotGap { get; set; } = 16;
		[DebugEditable(DisplayName = "Slot Ring Padding", Step = 1, Min = 0, Max = 40)]
		public int SlotRingPadding { get; set; } = 10;
		[DebugEditable(DisplayName = "Compact Padding X", Step = 1, Min = 0, Max = 40)]
		public int CompactPaddingX { get; set; } = 14;
		[DebugEditable(DisplayName = "Compact Padding Y", Step = 1, Min = 0, Max = 40)]
		public int CompactPaddingY { get; set; } = 16;
		[DebugEditable(DisplayName = "Compact Radius", Step = 1, Min = 0, Max = 24)]
		public int CompactRadius { get; set; } = 3;
		[DebugEditable(DisplayName = "Compact Title Font Scale", Step = 0.01f, Min = 0.04f, Max = 0.3f)]
		public float CompactTitleFontScale { get; set; } = 0.142f;
		[DebugEditable(DisplayName = "Compact Badge Font Scale", Step = 0.01f, Min = 0.03f, Max = 0.25f)]
		public float CompactBadgeFontScale { get; set; } = 0.108f;
		[DebugEditable(DisplayName = "Compact Meta Font Scale", Step = 0.01f, Min = 0.03f, Max = 0.25f)]
		public float CompactMetaFontScale { get; set; } = 0.075f;
		[DebugEditable(DisplayName = "Compact Meta Label Font Scale", Step = 0.01f, Min = 0.03f, Max = 0.25f)]
		public float CompactMetaLabelFontScale { get; set; } = 0.11f;
		[DebugEditable(DisplayName = "Enemy Portrait Height", Step = 1, Min = 60, Max = 260)]
		public int EnemyPortraitHeight { get; set; } = 148;
		[DebugEditable(DisplayName = "Meta Block Min Height", Step = 1, Min = 20, Max = 100)]
		public int MetaBlockMinHeight { get; set; } = 60;
		[DebugEditable(DisplayName = "Meta Block Border Alpha", Step = 0.01f, Min = 0f, Max = 1f)]
		public float MetaBlockBorderAlpha { get; set; } = 0.45f;
		[DebugEditable(DisplayName = "Meta Block Fill Alpha", Step = 0.01f, Min = 0f, Max = 1f)]
		public float MetaBlockFillAlpha { get; set; } = 0.08f;
		[DebugEditable(DisplayName = "Compact Resource Icon Size", Step = 1, Min = 6, Max = 36)]
		public int CompactResourceIconSize { get; set; } = 14;
		[DebugEditable(DisplayName = "Compact Hourglass Width", Step = 1, Min = 4, Max = 32)]
		public int CompactHourglassWidth { get; set; } = 18;
		[DebugEditable(DisplayName = "Compact Hourglass Height", Step = 1, Min = 4, Max = 40)]
		public int CompactHourglassHeight { get; set; } = 19;
		[DebugEditable(DisplayName = "Event Glyph Size", Step = 1, Min = 16, Max = 48)]
		public int EventGlyphSize { get; set; } = 28;
		[DebugEditable(DisplayName = "Shop Title Icon Size", Step = 1, Min = 8, Max = 32)]
		public int ShopTitleIconSize { get; set; } = 18;
		[DebugEditable(DisplayName = "Preview Glow Alpha", Step = 0.01f, Min = 0f, Max = 1f)]
		public float PreviewGlowAlpha { get; set; } = 0.30f;
		[DebugEditable(DisplayName = "Preview Source Offset Y", Step = 1, Min = -8, Max = 8)]
		public int PreviewSourceOffsetY { get; set; } = -1;
		[DebugEditable(DisplayName = "Empty Hourglass Alpha", Step = 0.01f, Min = 0f, Max = 1f)]
		public float EmptyHourglassAlpha { get; set; } = 0.34f;
		[DebugEditable(DisplayName = "Hourglass Red Glow Alpha", Step = 0.01f, Min = 0f, Max = 1f)]
		public float HourglassRedGlowAlpha { get; set; } = 0.65f;
		[DebugEditable(DisplayName = "Hourglass White Meter Glow Alpha", Step = 0.01f, Min = 0f, Max = 1f)]
		public float HourglassWhiteMeterGlowAlpha { get; set; } = 0.55f;
		[DebugEditable(DisplayName = "Hourglass Glow Radius", Step = 1, Min = 1, Max = 8)]
		public int HourglassGlowRadius { get; set; } = 2;
		[DebugEditable(DisplayName = "Column Gradient Strips", Step = 1, Min = 1, Max = 64)]
		public int ColumnGradientStrips { get; set; } = 16;
		[DebugEditable(DisplayName = "Column Border Thickness", Step = 1, Min = 1, Max = 6)]
		public int ColumnBorderThickness { get; set; } = 2;
		[DebugEditable(DisplayName = "Shop Column Top Alpha", Step = 0.01f, Min = 0f, Max = 1f)]
		public float ShopColumnTopAlpha { get; set; } = 0.10f;
		[DebugEditable(DisplayName = "Shop Column Border Alpha", Step = 0.01f, Min = 0f, Max = 1f)]
		public float ShopColumnBorderAlpha { get; set; } = 0.55f;
		[DebugEditable(DisplayName = "Event Column Top Alpha", Step = 0.01f, Min = 0f, Max = 1f)]
		public float EventColumnTopAlpha { get; set; } = 0.35f;
		[DebugEditable(DisplayName = "Event Column Border Alpha", Step = 0.01f, Min = 0f, Max = 1f)]
		public float EventColumnBorderAlpha { get; set; } = 0.75f;
		[DebugEditable(DisplayName = "Encounter Column Top Alpha", Step = 0.01f, Min = 0f, Max = 1f)]
		public float EncounterColumnTopAlpha { get; set; } = 0.10f;
		[DebugEditable(DisplayName = "Encounter Column Border Alpha", Step = 0.01f, Min = 0f, Max = 1f)]
		public float EncounterColumnBorderAlpha { get; set; } = 0.45f;
		[DebugEditable(DisplayName = "Shop Title Icon Y Offset", Step = 1, Min = -8, Max = 16)]
		public int ShopTitleIconYOffset { get; set; } = 2;
		[DebugEditable(DisplayName = "Shop Title Icon Gap", Step = 1, Min = 0, Max = 24)]
		public int ShopTitleIconGap { get; set; } = 8;
		[DebugEditable(DisplayName = "Column Underline Y Offset", Step = 1, Min = 0, Max = 80)]
		public int ColumnUnderlineYOffset { get; set; } = 31;
		[DebugEditable(DisplayName = "Column Underline Height", Step = 1, Min = 1, Max = 8)]
		public int ColumnUnderlineHeight { get; set; } = 2;
		[DebugEditable(DisplayName = "Column Subtitle Y Offset", Step = 1, Min = 0, Max = 80)]
		public int ColumnSubtitleYOffset { get; set; } = 37;
		[DebugEditable(DisplayName = "Event Underline Alpha", Step = 0.01f, Min = 0f, Max = 1f)]
		public float EventUnderlineAlpha { get; set; } = 0.75f;
		[DebugEditable(DisplayName = "Slot Hover Ring Alpha", Step = 0.01f, Min = 0f, Max = 1f)]
		public float SlotHoverRingAlpha { get; set; } = 0.04f;
		[DebugEditable(DisplayName = "Slot Unavailable Fill Multiplier", Step = 0.01f, Min = 0f, Max = 1f)]
		public float SlotUnavailableFillMultiplier { get; set; } = 0.78f;
		[DebugEditable(DisplayName = "Slot Unaffordable Fill Multiplier", Step = 0.01f, Min = 0f, Max = 1f)]
		public float SlotUnaffordableFillMultiplier { get; set; } = 0.84f;
		[DebugEditable(DisplayName = "Slot Border Thickness", Step = 1, Min = 1, Max = 6)]
		public int SlotBorderThickness { get; set; } = 2;
		[DebugEditable(DisplayName = "Preview Glow Inflate", Step = 1, Min = 0, Max = 16)]
		public int PreviewGlowInflate { get; set; } = 4;
		[DebugEditable(DisplayName = "Event Preview Glow Multiplier", Step = 0.01f, Min = 0f, Max = 1f)]
		public float EventPreviewGlowMultiplier { get; set; } = 0.5f;
		[DebugEditable(DisplayName = "Vanish Overlay Base Alpha", Step = 0.01f, Min = 0f, Max = 1f)]
		public float VanishOverlayBaseAlpha { get; set; } = 0.18f;
		[DebugEditable(DisplayName = "Vanish Overlay Pulse Amplitude", Step = 0.01f, Min = 0f, Max = 1f)]
		public float VanishOverlayPulseAmplitude { get; set; } = 0.12f;
		[DebugEditable(DisplayName = "Vanish Pulse Period Seconds", Step = 0.1f, Min = 0.5f, Max = 6f)]
		public float VanishPulsePeriodSeconds { get; set; } = 2f;
		[DebugEditable(DisplayName = "Vanish Fade Seconds", Step = 0.01f, Min = 0f, Max = 2f)]
		public float VanishFadeSeconds { get; set; } = 0.22f;
		[DebugEditable(DisplayName = "Vanish Border Alpha", Step = 0.01f, Min = 0f, Max = 1f)]
		public float VanishBorderAlpha { get; set; } = 0.45f;
		[DebugEditable(DisplayName = "Unaffordable Overlay Alpha", Step = 0.01f, Min = 0f, Max = 1f)]
		public float UnaffordableOverlayAlpha { get; set; } = 0.22f;
		[DebugEditable(DisplayName = "Unavailable Overlay Alpha", Step = 0.01f, Min = 0f, Max = 1f)]
		public float UnavailableOverlayAlpha { get; set; } = 0.35f;
		[DebugEditable(DisplayName = "Shop Time Block Width", Step = 1, Min = 40, Max = 160)]
		public int ShopTimeBlockWidth { get; set; } = 95;
		[DebugEditable(DisplayName = "Encounter Time Block Width", Step = 1, Min = 40, Max = 160)]
		public int EncounterTimeBlockWidth { get; set; } = 125;
		[DebugEditable(DisplayName = "Event Time Block Width", Step = 1, Min = 40, Max = 160)]
		public int EventTimeBlockWidth { get; set; } = 76;
		[DebugEditable(DisplayName = "Meta Block Gap", Step = 1, Min = 0, Max = 24)]
		public int MetaBlockGap { get; set; } = 8;
		[DebugEditable(DisplayName = "Compact Badge Y Offset", Step = 1, Min = -8, Max = 8)]
		public int CompactBadgeYOffset { get; set; } = 1;
		[DebugEditable(DisplayName = "Shop Title Max Length", Step = 1, Min = 8, Max = 80)]
		public int ShopTitleMaxLength { get; set; } = 30;
		[DebugEditable(DisplayName = "Portrait Fallback Padding X", Step = 1, Min = 0, Max = 40)]
		public int PortraitFallbackPaddingX { get; set; } = 14;
		[DebugEditable(DisplayName = "Portrait Fallback Text Offset Y", Step = 1, Min = -24, Max = 24)]
		public int PortraitFallbackTextOffsetY { get; set; } = 12;
		[DebugEditable(DisplayName = "Encounter Title Max Length", Step = 1, Min = 8, Max = 80)]
		public int EncounterTitleMaxLength { get; set; } = 26;
		[DebugEditable(DisplayName = "Portrait Bottom Line Height", Step = 1, Min = 1, Max = 8)]
		public int PortraitBottomLineHeight { get; set; } = 2;
		[DebugEditable(DisplayName = "Portrait Bottom Line Alpha", Step = 0.01f, Min = 0f, Max = 1f)]
		public float PortraitBottomLineAlpha { get; set; } = 0.45f;
		[DebugEditable(DisplayName = "Event Glyph Title Scale", Step = 0.01f, Min = 0.03f, Max = 0.4f)]
		public float EventGlyphTitleScale { get; set; } = 0.18f;
		[DebugEditable(DisplayName = "Event Glyph Text Gap", Step = 1, Min = 0, Max = 32)]
		public int EventGlyphTextGap { get; set; } = 10;
		[DebugEditable(DisplayName = "Muted Meta Border Alpha", Step = 0.01f, Min = 0f, Max = 1f)]
		public float MutedMetaBorderAlpha { get; set; } = 0.20f;
		[DebugEditable(DisplayName = "Muted Meta Fill Alpha", Step = 0.01f, Min = 0f, Max = 1f)]
		public float MutedMetaFillAlpha { get; set; } = 0.04f;
		[DebugEditable(DisplayName = "Resource Icon Text Gap", Step = 1, Min = 0, Max = 16)]
		public int ResourceIconTextGap { get; set; } = 4;
		[DebugEditable(DisplayName = "Resource Amount Text Y Offset", Step = 1, Min = -8, Max = 8)]
		public int ResourceAmountTextYOffset { get; set; } = -2;
		[DebugEditable(DisplayName = "Resource Group Width", Step = 1, Min = 16, Max = 80)]
		public int ResourceGroupWidth { get; set; } = 38;
		[DebugEditable(DisplayName = "Hourglass Row Gap", Step = 1, Min = 0, Max = 12)]
		public int HourglassRowGap { get; set; } = 3;
		[DebugEditable(DisplayName = "Time Block Plus Offset X", Step = 1, Min = 0, Max = 16)]
		public int TimeBlockPlusOffsetX { get; set; } = 2;
		[DebugEditable(DisplayName = "Time Block Plus Offset Y", Step = 1, Min = -8, Max = 8)]
		public int TimeBlockPlusOffsetY { get; set; } = -2;
		[DebugEditable(DisplayName = "Time Block Hourglass Offset X", Step = 1, Min = 0, Max = 32)]
		public int TimeBlockHourglassOffsetX { get; set; } = 14;
		[DebugEditable(DisplayName = "Time Block Cost Row Offset Y", Step = 1, Min = 0, Max = 16)]
		public int TimeBlockCostRowOffsetY { get; set; } = 2;
		[DebugEditable(DisplayName = "Time Block Duration Row Bottom Padding", Step = 1, Min = 0, Max = 16)]
		public int TimeBlockDurationRowBottomPadding { get; set; } = 2;
		[DebugEditable(DisplayName = "Overlay Font Scale", Step = 0.01f, Min = 0.03f, Max = 0.4f)]
		public float OverlayFontScale { get; set; } = 0.10f;
		[DebugEditable(DisplayName = "Overlay Text Offset X", Step = 1, Min = 0, Max = 80)]
		public int OverlayTextOffsetX { get; set; } = 22;
		[DebugEditable(DisplayName = "Overlay Text Offset Y", Step = 1, Min = -24, Max = 24)]
		public int OverlayTextOffsetY { get; set; } = 8;
		[DebugEditable(DisplayName = "Source Border Event Alpha", Step = 0.01f, Min = 0f, Max = 1f)]
		public float SourceBorderEventAlpha { get; set; } = 0.75f;
		[DebugEditable(DisplayName = "Disabled Border Alpha", Step = 0.01f, Min = 0f, Max = 1f)]
		public float DisabledBorderAlpha { get; set; } = 0.22f;
		[DebugEditable(DisplayName = "Encounter Border Alpha", Step = 0.01f, Min = 0f, Max = 1f)]
		public float EncounterBorderAlpha { get; set; } = 0.45f;
		[DebugEditable(DisplayName = "Event Border Alpha", Step = 0.01f, Min = 0f, Max = 1f)]
		public float EventBorderAlpha { get; set; } = 0.55f;
		[DebugEditable(DisplayName = "Shop Border Alpha", Step = 0.01f, Min = 0f, Max = 1f)]
		public float ShopBorderAlpha { get; set; } = 0.35f;

		internal static int ColumnsTopValue { get; private set; } = 114;
		internal static int ColumnsMaxWidthValue { get; private set; } = 1500;
		internal static int ColumnsGapValue { get; private set; } = 20;
		internal static int ColumnPaddingValue { get; private set; } = 16;
		internal static int SlotGapValue { get; private set; } = 16;

		public ClimbColumnDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, ContentManager content)
			: base(entityManager)
		{
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
			_content = content;
			_pixel = new Texture2D(graphicsDevice, 1, 1);
			_pixel.SetData(new[] { Color.White });
			ClimbSceneDrawHelpers.EnsureHourglassTextures(content);
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
				UpdateVanishPreviewFade(gameTime);
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
				ClimbColumnKind.Shop => (Color.White * ShopColumnTopAlpha, ClimbSceneDrawHelpers.CardFill, Color.White * ShopColumnBorderAlpha),
				ClimbColumnKind.Event => (Color.Black * EventColumnTopAlpha, ClimbSceneDrawHelpers.CardFill, Color.Black * EventColumnBorderAlpha),
				_ => (ClimbSceneDrawHelpers.Red3 * EncounterColumnTopAlpha, ClimbSceneDrawHelpers.CardFill, ClimbSceneDrawHelpers.Red3 * EncounterColumnBorderAlpha),
			};
			ClimbSceneDrawHelpers.DrawVerticalGradient(_spriteBatch, _pixel, bounds, top, bottom, ColumnGradientStrips);
			ClimbSceneDrawHelpers.DrawBorder(_spriteBatch, _pixel, bounds, border, ColumnBorderThickness);

			var inner = column.InnerBounds;
			var titleColor = column.Kind == ClimbColumnKind.Encounter ? ClimbSceneDrawHelpers.White1 : ClimbSceneDrawHelpers.White2;
			if (column.Kind == ClimbColumnKind.Shop)
			{
				titleColor = ClimbSceneDrawHelpers.White1;
			}

			int titleX = inner.X;
			if (column.Kind == ClimbColumnKind.Shop)
			{
				var iconRect = new Rectangle(inner.X, inner.Y + ShopTitleIconYOffset, ShopTitleIconSize, ShopTitleIconSize);
				ClimbSceneDrawHelpers.DrawShopTitleIcon(_spriteBatch, _pixel, iconRect, ClimbSceneDrawHelpers.White1);
				titleX += ShopTitleIconSize + ShopTitleIconGap;
			}

			ClimbSceneDrawHelpers.DrawTitleText(_spriteBatch, column.Title, new Vector2(titleX, inner.Y), ColumnTitleFontScale, titleColor);
			Color underline = column.Kind switch
			{
				ClimbColumnKind.Shop => ClimbSceneDrawHelpers.White1,
				ClimbColumnKind.Event => Color.Black * EventUnderlineAlpha,
				_ => ClimbSceneDrawHelpers.Red3,
			};
			_spriteBatch.Draw(_pixel, new Rectangle(inner.X, inner.Y + ColumnUnderlineYOffset, inner.Width, ColumnUnderlineHeight), underline);
			ClimbSceneDrawHelpers.DrawBodyText(
				_spriteBatch,
				ClimbSceneDrawHelpers.ToUpperAscii(column.Subtitle),
				new Vector2(inner.X, inner.Y + ColumnSubtitleYOffset),
				ColumnSubtitleFontScale,
				ClimbSceneDrawHelpers.White1);
		}

		private void DrawSlot(Entity entity, ClimbPreviewState preview)
		{
			var ui = entity.GetComponent<UIElement>();
			var slot = entity.GetComponent<ClimbSlotPresentation>();
			if (ui == null || slot == null || ui.IsHidden || ui.Bounds.Width <= 0) return;

			bool source = preview?.IsActive == true && string.Equals(preview.SourceSlotId, slot.SlotId, StringComparison.OrdinalIgnoreCase);
			bool wouldVanish = preview?.IsActive == true && preview.WouldVanishSlotIds.Contains(slot.SlotId) && !source;
			int offsetY = source ? PreviewSourceOffsetY : 0;

			var ring = Inflate(ui.Bounds, SlotRingPadding);
			ring.Y += offsetY;
			if (ui.IsHovered || source)
			{
				_spriteBatch.Draw(_pixel, ring, Color.White * SlotHoverRingAlpha);
			}

			var rect = ui.Bounds;
			rect.Y += offsetY;
			if (source)
			{
				DrawPreviewSourceGlow(rect, slot);
			}

			Color border = ResolveBorder(slot, ui, source);
			Color fill = ClimbSceneDrawHelpers.CardFill;
			if (slot.IsUnavailable) fill *= SlotUnavailableFillMultiplier;
			else if (slot.Kind == ClimbSlotKind.Shop && !slot.IsAffordable) fill *= SlotUnaffordableFillMultiplier;
			_spriteBatch.Draw(_pixel, rect, fill);
			ClimbSceneDrawHelpers.DrawBorder(_spriteBatch, _pixel, rect, border, SlotBorderThickness);

			if (slot.Kind == ClimbSlotKind.Encounter)
			{
				DrawEncounterSlot(rect, slot, preview, source);
			}
			else if (slot.Kind == ClimbSlotKind.Event)
			{
				DrawEventSlot(rect, slot);
			}
			else
			{
				DrawShopSlot(rect, slot);
			}

			if (wouldVanish)
			{
				float pulse = ClimbSceneDrawHelpers.PreviewVanishPulseAlpha(VanishPulsePeriodSeconds);
				float pulseStrength = VanishOverlayBaseAlpha + VanishOverlayPulseAmplitude * pulse;
				float overlayAlpha = _vanishPreviewAlpha * pulseStrength;
				float borderAlpha = _vanishPreviewAlpha * VanishBorderAlpha * pulseStrength;
				if (overlayAlpha > 0.001f)
				{
					_spriteBatch.Draw(_pixel, rect, ClimbSceneDrawHelpers.RedDim * overlayAlpha);
				}
				if (borderAlpha > 0.001f)
				{
					ClimbSceneDrawHelpers.DrawBorder(_spriteBatch, _pixel, rect, ClimbSceneDrawHelpers.Red2 * borderAlpha, SlotBorderThickness);
				}
			}
			else if (slot.Kind == ClimbSlotKind.Shop && !slot.IsAffordable)
			{
				_spriteBatch.Draw(_pixel, rect, Color.Black * UnaffordableOverlayAlpha);
			}
			else if (slot.IsUnavailable)
			{
				_spriteBatch.Draw(_pixel, rect, Color.Black * UnavailableOverlayAlpha);
			}
		}

		private void DrawPreviewSourceGlow(Rectangle rect, ClimbSlotPresentation slot)
		{
			var glow = Inflate(rect, PreviewGlowInflate);
			Color glowColor = slot.Kind switch
			{
				ClimbSlotKind.Encounter => ClimbSceneDrawHelpers.RedGlow * PreviewGlowAlpha,
				ClimbSlotKind.Event => Color.Black * (PreviewGlowAlpha * EventPreviewGlowMultiplier),
				_ => Color.White * PreviewGlowAlpha,
			};
			_spriteBatch.Draw(_pixel, glow, glowColor);
		}

		private void DrawShopSlot(Rectangle rect, ClimbSlotPresentation slot)
		{
			var titlePos = new Vector2(rect.X + CompactPaddingX, rect.Y + CompactPaddingY);
			ClimbSceneDrawHelpers.DrawBodyText(_spriteBatch, Trim(slot.Title, ShopTitleMaxLength), titlePos, CompactTitleFontScale, ClimbSceneDrawHelpers.White1);
			var badgeSize = ClimbSceneDrawHelpers.MeasureBodyText(ClimbSceneDrawHelpers.ToUpperAscii(slot.Label), CompactBadgeFontScale);
			ClimbSceneDrawHelpers.DrawBodyText(
				_spriteBatch,
				ClimbSceneDrawHelpers.ToUpperAscii(slot.Label),
				new Vector2(rect.Right - CompactPaddingX - badgeSize.X, rect.Y + CompactPaddingY + CompactBadgeYOffset),
				CompactBadgeFontScale,
				ClimbSceneDrawHelpers.White3);

			int metaY = rect.Bottom - CompactPaddingY - MetaBlockMinHeight;
			var timeRect = new Rectangle(rect.Right - CompactPaddingX - ShopTimeBlockWidth, metaY, ShopTimeBlockWidth, MetaBlockMinHeight);
			var costRect = new Rectangle(rect.X + CompactPaddingX, metaY, timeRect.X - MetaBlockGap - rect.X - CompactPaddingX, MetaBlockMinHeight);
			DrawCostMetaBlock(costRect, slot.Cost, slot.Kind == ClimbSlotKind.Shop && !slot.IsAffordable);
			DrawTimeBlock(timeRect, slot.TimeCost);
		}

		private void DrawEncounterSlot(Rectangle rect, ClimbSlotPresentation slot, ClimbPreviewState preview, bool source)
		{
			var portrait = new Rectangle(rect.X, rect.Y, rect.Width, Math.Min(EnemyPortraitHeight, rect.Height - MetaBlockMinHeight - CompactPaddingY * 2));
			ClimbSceneDrawHelpers.DrawRadialPortraitGradient(_spriteBatch, _pixel, portrait);
			var texture = GetTexture(slot.PortraitAsset);
			if (texture != null)
			{
				ClimbSceneDrawHelpers.DrawPortraitCropped(_spriteBatch, texture, portrait);
			}
			else
			{
				ClimbSceneDrawHelpers.DrawBodyText(_spriteBatch, Trim(slot.Title, EncounterTitleMaxLength), new Vector2(portrait.X + PortraitFallbackPaddingX, portrait.Center.Y - PortraitFallbackTextOffsetY), CompactTitleFontScale, ClimbSceneDrawHelpers.White1);
			}
			_spriteBatch.Draw(_pixel, new Rectangle(portrait.X, portrait.Bottom - PortraitBottomLineHeight, portrait.Width, PortraitBottomLineHeight), ClimbSceneDrawHelpers.Red3 * PortraitBottomLineAlpha);

			int metaY = rect.Bottom - CompactPaddingY - MetaBlockMinHeight;
			var timeRect = new Rectangle(rect.Right - CompactPaddingX - EncounterTimeBlockWidth, metaY, EncounterTimeBlockWidth, MetaBlockMinHeight);
			var rewardRect = new Rectangle(rect.X + CompactPaddingX, metaY, timeRect.X - MetaBlockGap - rect.X - CompactPaddingX, MetaBlockMinHeight);
			DrawRewardMetaBlock(rewardRect, slot.Reward);
			int remaining = GetEncounterRemainingDuration(slot, SaveCache.GetClimbState()?.time ?? 0);
			int activeRemaining = remaining;
			if (preview?.IsActive == true && !source)
			{
				activeRemaining = GetEncounterRemainingDuration(slot, preview.ProjectedUsedTime);
			}
			DrawTimeBlock(timeRect, slot.TimeCost, remaining, activeRemaining);
		}

		private void DrawEventSlot(Rectangle rect, ClimbSlotPresentation slot)
		{
			int glyphY = rect.Y + (rect.Height - EventGlyphSize) / 2;
			var glyphRect = new Rectangle(rect.X + CompactPaddingX, glyphY, EventGlyphSize, EventGlyphSize);
			ClimbSceneDrawHelpers.DrawGlyphBox(_spriteBatch, _pixel, glyphRect, EventGlyphTitleScale);
			ClimbSceneDrawHelpers.DrawBodyText(_spriteBatch, "Event", new Vector2(glyphRect.Right + EventGlyphTextGap, rect.Y + CompactPaddingY), CompactTitleFontScale, ClimbSceneDrawHelpers.White1);

			int metaY = rect.Y + (rect.Height - MetaBlockMinHeight) / 2;
			var timeRect = new Rectangle(rect.Right - CompactPaddingX - EventTimeBlockWidth, metaY, EventTimeBlockWidth, MetaBlockMinHeight);
			DrawTimeBlock(timeRect, slot.TimeCost);
		}

		private void DrawCostMetaBlock(Rectangle rect, ClimbResourceSave resources, bool muted)
		{
			ClimbSceneDrawHelpers.DrawMetaBlock(
				_spriteBatch,
				_pixel,
				rect,
				"PRICE",
				CompactMetaLabelFontScale,
				muted ? MutedMetaBorderAlpha : MetaBlockBorderAlpha,
				muted ? MutedMetaFillAlpha : MetaBlockFillAlpha,
				(sb, content) => DrawResourceLine(sb, content, resources, muted ? ClimbSceneDrawHelpers.White3 : ClimbSceneDrawHelpers.White2, compact: true));
		}

		private void DrawRewardMetaBlock(Rectangle rect, ClimbResourceSave resources)
		{
			ClimbSceneDrawHelpers.DrawMetaBlock(
				_spriteBatch,
				_pixel,
				rect,
				"GAIN",
				CompactMetaLabelFontScale,
				MetaBlockBorderAlpha,
				MetaBlockFillAlpha,
				(sb, content) => DrawResourceLine(sb, content, resources, ClimbSceneDrawHelpers.White2, compact: true));
		}

		private void DrawResourceLine(SpriteBatch spriteBatch, Rectangle content, ClimbResourceSave resources, Color color, bool compact = false)
		{
			resources ??= new ClimbResourceSave { red = 0, white = 0, black = 0 };
			int x = content.X;
			int y = content.Y + Math.Max(0, (content.Height - CompactResourceIconSize) / 2);
			DrawSingleResource(spriteBatch, ref x, y, ClimbResourceType.Red, resources.red, ClimbSceneDrawHelpers.Red3, compact);
			DrawSingleResource(spriteBatch, ref x, y, ClimbResourceType.White, resources.white, ClimbSceneDrawHelpers.White1, compact);
			DrawSingleResource(spriteBatch, ref x, y, ClimbResourceType.Black, resources.black, ClimbSceneDrawHelpers.White3, compact);
			if (resources.red == 0 && resources.white == 0 && resources.black == 0)
			{
				ClimbSceneDrawHelpers.DrawBodyText(spriteBatch, "None", new Vector2(content.X, y + ResourceAmountTextYOffset), CompactMetaFontScale, color);
			}
		}

		private void DrawSingleResource(SpriteBatch spriteBatch, ref int x, int y, ClimbResourceType type, int amount, Color color, bool compact)
		{
			if (amount <= 0) return;
			int iconSize = compact ? CompactResourceIconSize : CompactResourceIconSize;
			ClimbSceneDrawHelpers.DrawResourceIcon(spriteBatch, _graphicsDevice, _pixel, new Vector2(x, y), type, iconSize, color, compact: compact);
			ClimbSceneDrawHelpers.DrawBodyText(spriteBatch, amount.ToString(), new Vector2(x + iconSize + ResourceIconTextGap, y + ResourceAmountTextYOffset), CompactMetaFontScale, ClimbSceneDrawHelpers.White1);
			x += ResourceGroupWidth;
		}

		private void DrawTimeBlock(Rectangle rect, int time, int durationRemaining = -1, int activeDurationRemaining = -1)
		{
			ClimbSceneDrawHelpers.DrawMetaBlock(
				_spriteBatch,
				_pixel,
				rect,
				string.Empty,
				CompactMetaLabelFontScale,
				MetaBlockBorderAlpha,
				MetaBlockFillAlpha,
				(sb, content) =>
				{
					if (durationRemaining >= 0)
					{
						int costY = content.Y + TimeBlockCostRowOffsetY;
						ClimbSceneDrawHelpers.DrawBodyText(sb, "+", new Vector2(content.X + TimeBlockPlusOffsetX, costY + TimeBlockPlusOffsetY), CompactMetaFontScale, ClimbSceneDrawHelpers.White1);
						DrawHourglassRow(sb, content.X + TimeBlockHourglassOffsetX, costY, time, time, ClimbSceneDrawHelpers.White3, ClimbSceneDrawHelpers.White2, HourglassIconStyle.WhiteCost, HourglassIconStyle.WhiteFaded);
						int active = activeDurationRemaining < 0 ? durationRemaining : activeDurationRemaining;
						DrawHourglassRow(sb, content.X + TimeBlockPlusOffsetX, content.Bottom - CompactHourglassHeight - TimeBlockDurationRowBottomPadding, durationRemaining, active, ClimbSceneDrawHelpers.Red2, ClimbSceneDrawHelpers.Red2, HourglassIconStyle.Red, HourglassIconStyle.RedFaded);
					}
					else
					{
						int costY = content.Y + Math.Max(0, (content.Height - CompactHourglassHeight) / 2);
						ClimbSceneDrawHelpers.DrawBodyText(sb, "+", new Vector2(content.X + TimeBlockPlusOffsetX, costY + TimeBlockPlusOffsetY), CompactMetaFontScale, ClimbSceneDrawHelpers.White1);
						DrawHourglassRow(sb, content.X + TimeBlockHourglassOffsetX, costY, time, time, ClimbSceneDrawHelpers.White3, ClimbSceneDrawHelpers.White2, HourglassIconStyle.WhiteCost, HourglassIconStyle.WhiteFaded);
					}
				});
		}

		private void DrawHourglassRow(SpriteBatch spriteBatch, int x, int y, int count, int activeCount, Color frame, Color sand, HourglassIconStyle activeStyle, HourglassIconStyle fadedStyle)
		{
			count = Math.Max(0, count);
			activeCount = Math.Clamp(activeCount, 0, count);
			var glow = new HourglassGlowTuning
			{
				RedGlowAlpha = HourglassRedGlowAlpha,
				WhiteMeterGlowAlpha = HourglassWhiteMeterGlowAlpha,
				GlowRadius = HourglassGlowRadius,
			};
			for (int i = 0; i < count; i++)
			{
				bool active = i < activeCount;
				float alpha = active ? 1f : EmptyHourglassAlpha;
				var icon = new Rectangle(
					x + i * (CompactHourglassWidth + HourglassRowGap),
					y,
					CompactHourglassWidth,
					CompactHourglassHeight);
				ClimbSceneDrawHelpers.DrawHourglassIcon(
					spriteBatch,
					icon,
					active ? activeStyle : fadedStyle,
					frame,
					sand,
					active,
					alpha,
					glow);
			}
		}

		private void DrawOverlay(Rectangle rect, string text, Color fill, Color textColor)
		{
			_spriteBatch.Draw(_pixel, rect, fill);
			ClimbSceneDrawHelpers.DrawBodyText(_spriteBatch, text, new Vector2(rect.Center.X - OverlayTextOffsetX, rect.Center.Y - OverlayTextOffsetY), OverlayFontScale, textColor);
		}

		private Color ResolveBorder(ClimbSlotPresentation slot, UIElement ui, bool source)
		{
			if (source)
			{
				return slot.Kind switch
				{
					ClimbSlotKind.Encounter => ClimbSceneDrawHelpers.Red3,
					ClimbSlotKind.Event => Color.Black * SourceBorderEventAlpha,
					_ => ClimbSceneDrawHelpers.White1,
				};
			}
			if (slot.Kind == ClimbSlotKind.Shop && !slot.IsAffordable) return Color.White * DisabledBorderAlpha;
			if (slot.IsUnavailable) return Color.White * DisabledBorderAlpha;
			if (ui.IsHovered) return ClimbSceneDrawHelpers.Red3;
			return slot.Kind switch
			{
				ClimbSlotKind.Encounter => ClimbSceneDrawHelpers.Red3 * EncounterBorderAlpha,
				ClimbSlotKind.Event => Color.Black * EventBorderAlpha,
				_ => Color.White * ShopBorderAlpha,
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

		private static int GetEncounterRemainingDuration(ClimbSlotPresentation slot, int time)
		{
			if (slot == null || slot.Duration <= 0) return 0;
			int expiresAt = slot.GeneratedAtTime + slot.Duration;
			return Math.Clamp(expiresAt - ClimbRuleService.ClampTime(time), 0, slot.Duration);
		}

		private void UpdateVanishPreviewFade(GameTime gameTime)
		{
			var preview = GetPreview();
			bool hasVanishTargets = preview?.IsActive == true
				&& preview.WouldVanishSlotIds.Any(id => !string.Equals(id, preview.SourceSlotId, StringComparison.OrdinalIgnoreCase));
			float target = hasVanishTargets ? 1f : 0f;
			float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
			float delta = VanishFadeSeconds <= 0f ? 1f : elapsed / VanishFadeSeconds;
			_vanishPreviewAlpha = MathHelper.Clamp(
				_vanishPreviewAlpha + (target > _vanishPreviewAlpha ? delta : -delta),
				0f,
				1f);
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
