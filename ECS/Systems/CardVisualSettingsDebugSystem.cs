using System;
using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.Diagnostics;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Debug-only system that exposes CardVisualSettings fields for live editing via the debug menu.
	/// </summary>
	[DebugTab("Card Visual Settings")]
	public class CardVisualSettingsDebugSystem : Core.System
	{
		public CardVisualSettingsDebugSystem(EntityManager entityManager) : base(entityManager)
		{
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
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
					CardWidth = Crusaders30XX.ECS.Config.CardConfig.CARD_WIDTH,
					CardHeight = Crusaders30XX.ECS.Config.CardConfig.CARD_HEIGHT,
					CardGap = Crusaders30XX.ECS.Config.CardConfig.CARD_GAP,
					CardBorderThickness = Crusaders30XX.ECS.Config.CardConfig.CARD_BORDER_THICKNESS,
					CardCornerRadius = Crusaders30XX.ECS.Config.CardConfig.CARD_CORNER_RADIUS,
					HighlightBorderThickness = Crusaders30XX.ECS.Config.CardConfig.HIGHLIGHT_BORDER_THICKNESS,
					TextMarginX = Crusaders30XX.ECS.Config.CardConfig.TEXT_MARGIN_X,
					TextMarginY = Crusaders30XX.ECS.Config.CardConfig.TEXT_MARGIN_Y,
					NameScale = Crusaders30XX.ECS.Config.CardConfig.NAME_SCALE,
					CostScale = Crusaders30XX.ECS.Config.CardConfig.COST_SCALE,
					DescriptionScale = Crusaders30XX.ECS.Config.CardConfig.DESCRIPTION_SCALE,
					BlockScale = Crusaders30XX.ECS.Config.CardConfig.BLOCK_SCALE,
					BlockNumberScale = Crusaders30XX.ECS.Config.CardConfig.BLOCK_NUMBER_SCALE,
					BlockNumberMarginX = Crusaders30XX.ECS.Config.CardConfig.BLOCK_NUMBER_MARGIN_X,
					BlockNumberMarginY = Crusaders30XX.ECS.Config.CardConfig.BLOCK_NUMBER_MARGIN_Y
				};
				EntityManager.AddComponent(e, s);
				return s;
			}
			return e.GetComponent<CardVisualSettings>();
		}

		[DebugEditable(DisplayName = "Card Width", Step = 1, Min = 10, Max = 2000)]
		public int CardWidth { get => EnsureSettings().CardWidth; set => EnsureSettings().CardWidth = Math.Max(10, value); }

		[DebugEditable(DisplayName = "Card Height", Step = 1, Min = 10, Max = 2000)]
		public int CardHeight { get => EnsureSettings().CardHeight; set => EnsureSettings().CardHeight = Math.Max(10, value); }

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

		[DebugEditable(DisplayName = "Description Scale", Step = 0.05f, Min = 0.1f, Max = 3f)]
		public float DescriptionScale { get => EnsureSettings().DescriptionScale; set => EnsureSettings().DescriptionScale = Math.Max(0.05f, value); }

		[DebugEditable(DisplayName = "Block Scale", Step = 0.05f, Min = 0.1f, Max = 3f)]
		public float BlockScale { get => EnsureSettings().BlockScale; set => EnsureSettings().BlockScale = Math.Max(0.05f, value); }

		[DebugEditable(DisplayName = "Block Number Scale", Step = 0.05f, Min = 0.1f, Max = 3f)]
		public float BlockNumberScale { get => EnsureSettings().BlockNumberScale; set => EnsureSettings().BlockNumberScale = Math.Max(0.05f, value); }

		[DebugEditable(DisplayName = "Block Number Margin X", Step = 1, Min = 0, Max = 500)]
		public int BlockNumberMarginX { get => EnsureSettings().BlockNumberMarginX; set => EnsureSettings().BlockNumberMarginX = Math.Max(0, value); }

		[DebugEditable(DisplayName = "Block Number Margin Y", Step = 1, Min = 0, Max = 500)]
		public int BlockNumberMarginY { get => EnsureSettings().BlockNumberMarginY; set => EnsureSettings().BlockNumberMarginY = Math.Max(0, value); }
	}
}


