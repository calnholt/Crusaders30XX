using System;
using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.Diagnostics;
using System.Collections.Generic;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Debug-only system that exposes CardVisualSettings fields for live editing via the debug menu.
	/// </summary>
	[DebugTab("Card Visual Settings")]
	public class CardVisualSettingsDebugSystem : Core.System
	{
		// Local baselines (formerly in CardConfig)
		private const int BASE_CARD_WIDTH = 250;
		private const int BASE_CARD_HEIGHT = 350;
		private const int BASE_CARD_GAP = -20;
		private const int BASE_CARD_BORDER_THICKNESS = 3;
		private const int BASE_CARD_CORNER_RADIUS = 18;
		private const int BASE_HIGHLIGHT_BORDER_THICKNESS = 5;
		private const int BASE_TEXT_MARGIN_X = 16;
		private const int BASE_TEXT_MARGIN_Y = 16;
		private const float BASE_NAME_SCALE = 0.04375f;
		private const float BASE_COST_SCALE = 0.0375f;
		private const float BASE_DESCRIPTION_SCALE = 0.025f;
		private const float BASE_BLOCK_SCALE = 0.125f;
		private const float BASE_BLOCK_NUMBER_SCALE = 0.05625f;
		private const int BASE_BLOCK_NUMBER_MARGIN_X = 14;
		private const int BASE_BLOCK_NUMBER_MARGIN_Y = 12;

		public CardVisualSettingsDebugSystem(EntityManager entityManager) : base(entityManager)
		{
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			return Array.Empty<Entity>();
		}

		protected override void UpdateEntity(Entity entity, Microsoft.Xna.Framework.GameTime gameTime) { }

		private CardVisualSettings EnsureSettings()
		{
			var e = EntityManager.GetEntitiesWithComponent<CardVisualSettings>().FirstOrDefault();
			if (e == null)
			{
				e = EntityManager.CreateEntity("CardVisualSettings");
				var s = new CardVisualSettings
				{
					UIScale = 1f,
					CardWidth = BASE_CARD_WIDTH,
					CardHeight = BASE_CARD_HEIGHT,
					CardOffsetYExtra = 25,
					CardGap = BASE_CARD_GAP,
					CardBorderThickness = BASE_CARD_BORDER_THICKNESS,
					CardCornerRadius = BASE_CARD_CORNER_RADIUS,
					HighlightBorderThickness = BASE_HIGHLIGHT_BORDER_THICKNESS,
					TextMarginX = BASE_TEXT_MARGIN_X,
					TextMarginY = BASE_TEXT_MARGIN_Y,
					NameScale = BASE_NAME_SCALE,
					CostScale = BASE_COST_SCALE,
					DescriptionScale = BASE_DESCRIPTION_SCALE,
					BlockScale = BASE_BLOCK_SCALE,
					BlockNumberScale = BASE_BLOCK_NUMBER_SCALE,
					BlockNumberMarginX = BASE_BLOCK_NUMBER_MARGIN_X,
					BlockNumberMarginY = BASE_BLOCK_NUMBER_MARGIN_Y
				};
				EntityManager.AddComponent(e, s);
				return s;
			}
			return e.GetComponent<CardVisualSettings>();
		}

		[DebugEditable(DisplayName = "UI Scale", Step = 0.05f, Min = 0.1f, Max = 3f)]
		public float UIScale { get => EnsureSettings().UIScale; set => EnsureSettings().UIScale = Math.Max(0.1f, value); }

		[DebugEditable(DisplayName = "Card Width", Step = 1, Min = 10, Max = 2000)]
		public int CardWidth { get => EnsureSettings().CardWidth; set => EnsureSettings().CardWidth = Math.Max(10, value); }

		[DebugEditable(DisplayName = "Card Height", Step = 1, Min = 10, Max = 2000)]
		public int CardHeight { get => EnsureSettings().CardHeight; set => EnsureSettings().CardHeight = Math.Max(10, value); }

		[DebugEditable(DisplayName = "Card Offset Y Extra", Step = 1, Min = -500, Max = 500)]
		public int CardOffsetYExtra { get => EnsureSettings().CardOffsetYExtra; set => EnsureSettings().CardOffsetYExtra = value; }

		[DebugEditable(DisplayName = "Card Gap", Step = 1, Min = -500, Max = 500)]
		public int CardGap { get => EnsureSettings().CardGap; set => EnsureSettings().CardGap = value; }

		[DebugEditable(DisplayName = "Border Thickness", Step = 1, Min = 0, Max = 64)]
		public int BorderThickness { get => EnsureSettings().CardBorderThickness; set => EnsureSettings().CardBorderThickness = Math.Max(0, value); }

		[DebugEditable(DisplayName = "Corner Radius", Step = 1, Min = 0, Max = 64)]
		public int CornerRadius { get => EnsureSettings().CardCornerRadius; set => EnsureSettings().CardCornerRadius = Math.Max(0, value); }

		[DebugEditable(DisplayName = "Highlight Border Thickness", Step = 1, Min = 0, Max = 64)]
		public int HighlightBorderThickness { get => EnsureSettings().HighlightBorderThickness; set => EnsureSettings().HighlightBorderThickness = Math.Max(0, value); }

		[DebugEditable(DisplayName = "Text Margin X", Step = 1, Min = 0, Max = 500)]
		public int TextMarginX { get => EnsureSettings().TextMarginX; set => EnsureSettings().TextMarginX = Math.Max(0, value); }

		[DebugEditable(DisplayName = "Text Margin Y", Step = 1, Min = 0, Max = 500)]
		public int TextMarginY { get => EnsureSettings().TextMarginY; set => EnsureSettings().TextMarginY = Math.Max(0, value); }

		[DebugEditable(DisplayName = "Name Scale", Step = 0.05f, Min = 0.1f, Max = 3f)]
		public float NameScale { get => EnsureSettings().NameScale; set => EnsureSettings().NameScale = Math.Max(0.05f, value); }

		[DebugEditable(DisplayName = "Cost Scale", Step = 0.05f, Min = 0.1f, Max = 3f)]
		public float CostScale { get => EnsureSettings().CostScale; set => EnsureSettings().CostScale = Math.Max(0.05f, value); }

		[DebugEditable(DisplayName = "Description Scale", Step = 0.01f, Min = 0.1f, Max = 3f)]
		public float DescriptionScale { get => EnsureSettings().DescriptionScale; set => EnsureSettings().DescriptionScale = Math.Max(0.05f, value); }

		[DebugEditable(DisplayName = "Block Scale", Step = 0.05f, Min = 0.1f, Max = 3f)]
		public float BlockScale { get => EnsureSettings().BlockScale; set => EnsureSettings().BlockScale = Math.Max(0.05f, value); }

		[DebugEditable(DisplayName = "Block Number Scale", Step = 0.05f, Min = 0.1f, Max = 3f)]
		public float BlockNumberScale { get => EnsureSettings().BlockNumberScale; set => EnsureSettings().BlockNumberScale = Math.Max(0.05f, value); }

		[DebugEditable(DisplayName = "Block Number Margin X", Step = 1, Min = 0, Max = 500)]
		public int BlockNumberMarginX { get => EnsureSettings().BlockNumberMarginX; set => EnsureSettings().BlockNumberMarginX = Math.Max(0, value); }

		[DebugEditable(DisplayName = "Block Number Margin Y", Step = 1, Min = 0, Max = 500)]
		public int BlockNumberMarginY { get => EnsureSettings().BlockNumberMarginY; set => EnsureSettings().BlockNumberMarginY = Math.Max(0, value); }

		[DebugAction("Apply Defaults (250x350 etc.)")]
		public void ApplyDefaults()
		{
			var s = EnsureSettings();
			s.UIScale = 1f;
			s.CardWidth = 250;
			s.CardHeight = 350;
			s.CardOffsetYExtra = 25;
			s.CardGap = -20;
			s.CardBorderThickness = 3;
			s.CardCornerRadius = 18;
			s.HighlightBorderThickness = 5;
			s.TextMarginX = 16;
			s.TextMarginY = 16;
			s.NameScale = 0.175f;
			s.CostScale = 0.6f;
			s.DescriptionScale = 0.1f;
			s.BlockScale = 0.5f;
			s.BlockNumberScale = 0.225f;
			s.BlockNumberMarginX = 14;
			s.BlockNumberMarginY = 12;
		}
	}
}


