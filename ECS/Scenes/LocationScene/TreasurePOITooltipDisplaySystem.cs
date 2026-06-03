using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Save;
using Crusaders30XX.ECS.Rendering;
using Crusaders30XX.ECS.Services;
using Crusaders30XX.ECS.Singletons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("Treasure POI Tooltip")]
	public class TreasurePOITooltipDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly SpriteFont _font = FontSingleton.TitleFont;
		private const string TooltipEntityName = "UI_TreasureTooltip";
		private const string TreasureTitle = "Open Treasure";
		private const string EventTitle = "Event";
		private Entity _tooltipEntity;

		[DebugEditable(DisplayName = "Padding", Step = 1, Min = 0, Max = 40)]
		public int Padding { get; set; } = 10;
		[DebugEditable(DisplayName = "Gap", Step = 1, Min = 0, Max = 120)]
		public int Gap { get; set; } = 18;
		[DebugEditable(DisplayName = "Text Scale", Step = 0.05f, Min = 0.1f, Max = 2.0f)]
		public float TextScale { get; set; } = 0.25f;
		[DebugEditable(DisplayName = "Trapezoid Height", Step = 2, Min = 20, Max = 300)]
		public int TrapezoidHeight { get; set; } = 64;
		[DebugEditable(DisplayName = "Left Side Offset", Step = 1, Min = 0, Max = 120)]
		public int LeftSideOffset { get; set; } = 16;

		[DebugEditable(DisplayName = "Top-Left Angle", Step = 1f, Min = -45f, Max = 45f)]
		public float TopLeftAngleDegrees { get; set; } = 2f;
		[DebugEditable(DisplayName = "Top-Right Angle", Step = 1f, Min = -45f, Max = 45f)]
		public float TopRightAngleDegrees { get; set; } = 2f;
		[DebugEditable(DisplayName = "Right-Top Angle", Step = 1f, Min = -45f, Max = 45f)]
		public float RightTopAngleDegrees { get; set; } = -26f;
		[DebugEditable(DisplayName = "Right-Bottom Angle", Step = 1f, Min = -45f, Max = 45f)]
		public float RightBottomAngleDegrees { get; set; } = -26f;
		[DebugEditable(DisplayName = "Bottom-Right Angle", Step = 1f, Min = -45f, Max = 45f)]
		public float BottomRightAngleDegrees { get; set; } = -2f;
		[DebugEditable(DisplayName = "Bottom-Left Angle", Step = 1f, Min = -45f, Max = 45f)]
		public float BottomLeftAngleDegrees { get; set; } = -2f;
		[DebugEditable(DisplayName = "Left-Bottom Angle", Step = 1f, Min = -45f, Max = 45f)]
		public float LeftBottomAngleDegrees { get; set; } = 9f;
		[DebugEditable(DisplayName = "Left-Top Angle", Step = 1f, Min = -45f, Max = 45f)]
		public float LeftTopAngleDegrees { get; set; } = 9f;

		public TreasurePOITooltipDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
			: base(entityManager)
		{
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<SceneState>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			var scene = entity.GetComponent<SceneState>();
			if (scene == null || scene.Current != SceneId.Location) return;
			if (StateSingleton.IsActive || StateSingleton.PreventClicking) return;

			var runNodes = SaveCache.GetRunMapNodes();
			var hovered = EntityManager.GetEntitiesWithComponent<UIElement>()
				.Select(e => new { E = e, UI = e.GetComponent<UIElement>(), T = e.GetComponent<Transform>(), P = e.GetComponent<PointOfInterest>() })
				.Where(x => x.UI != null && !x.UI.IsHidden && x.UI.IsHovered && x.P != null
					&& (x.P.Type == PointOfInterestType.Treasure || x.P.Type == PointOfInterestType.Event))
				.OrderByDescending(x => x.T?.ZOrder ?? 0)
				.FirstOrDefault();

			if (hovered == null)
			{
				DestroyTooltip();
				return;
			}

			string tooltipTitle;
			if (hovered.P.Type == PointOfInterestType.Event)
			{
				if (string.IsNullOrEmpty(hovered.P.EventId))
				{
					DestroyTooltip();
					return;
				}

				if (!SaveCache.TryGetRunEvent(hovered.P.EventId, out var mapEvent, out _) ||
					!RunMapEventService.IsEnterable(mapEvent, runNodes))
				{
					DestroyTooltip();
					return;
				}

				tooltipTitle = EventTitle;
			}
			else
			{
				if (string.IsNullOrEmpty(hovered.P.TreasureId))
				{
					DestroyTooltip();
					return;
				}

				if (!SaveCache.TryGetRunTreasure(hovered.P.TreasureId, out var treasure, out _) ||
					!RunMapTreasureService.IsEnterable(treasure, runNodes))
				{
					DestroyTooltip();
					return;
				}

				tooltipTitle = TreasureTitle;
			}

			int pad = System.Math.Max(0, Padding);
			var size = _font.MeasureString(tooltipTitle) * TextScale;
			int width = (int)System.Math.Ceiling(size.X) + pad * 2 + System.Math.Max(0, LeftSideOffset);
			int height = System.Math.Max(24, TrapezoidHeight);
			var r = hovered.UI.Bounds;
			int viewportW = Game1.VirtualWidth;
			int viewportH = Game1.VirtualHeight;

			int rightSpace = viewportW - (r.Right + Gap);
			int leftSpace = r.Left - Gap;
			bool canPlaceRight = rightSpace >= width;
			bool canPlaceLeft = leftSpace >= width;
			bool preferRight = r.Center.X < viewportW / 2;
			bool placeRight = canPlaceRight || (!canPlaceLeft && preferRight);
			if (!canPlaceRight && canPlaceLeft) placeRight = false;

			int rx = placeRight ? (r.Right + Gap + 35) : (r.Left - Gap - width - 35);
			int ry = r.Y + (r.Height - height) / 2;
			rx = System.Math.Max(0, System.Math.Min(rx, viewportW - width));
			ry = System.Math.Max(0, System.Math.Min(ry, viewportH - height));
			var rect = new Rectangle(rx, ry, width, height);

			if (_tooltipEntity == null)
			{
				_tooltipEntity = EntityManager.CreateEntity(TooltipEntityName);
				EntityManager.AddComponent(_tooltipEntity, new Transform { Position = new Vector2(rect.X, rect.Y), ZOrder = 10001 });
				EntityManager.AddComponent(_tooltipEntity, new UIElement { Bounds = rect, IsInteractable = true, TooltipOffsetPx = 30 });
				EntityManager.AddComponent(_tooltipEntity, new HotKey
				{
					Button = FaceButton.X,
					RequiresHold = true,
					ParentEntity = hovered.E,
					Position = HotKeyPosition.Below
				});
				EntityManager.AddComponent(_tooltipEntity, new Hint { Text = tooltipTitle });
			}
			else
			{
				var t = _tooltipEntity.GetComponent<Transform>();
				if (t != null)
				{
					t.Position = new Vector2(rect.X, rect.Y);
					t.ZOrder = 10001;
				}
				var ui = _tooltipEntity.GetComponent<UIElement>();
				if (ui != null) ui.Bounds = rect;
				var hk = _tooltipEntity.GetComponent<HotKey>();
				if (hk != null) hk.ParentEntity = hovered.E;
				var hint = _tooltipEntity.GetComponent<Hint>();
				if (hint != null) hint.Text = tooltipTitle;
			}
		}

		private void DestroyTooltip()
		{
			if (_tooltipEntity == null) return;
			EntityManager.DestroyEntity(_tooltipEntity.Id);
			_tooltipEntity = null;
		}

		public void Draw()
		{
			var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
			if (scene == null || scene.Current != SceneId.Location) return;
			if (_tooltipEntity == null) return;

			var ui = _tooltipEntity.GetComponent<UIElement>();
			var hint = _tooltipEntity.GetComponent<Hint>();
			if (ui == null || hint == null) return;

			string title = string.IsNullOrEmpty(hint.Text) ? TreasureTitle : hint.Text;
			var rect = ui.Bounds;

			float top = (TopLeftAngleDegrees + TopRightAngleDegrees) * 0.5f;
			float right = (RightTopAngleDegrees + RightBottomAngleDegrees) * 0.5f;
			float bottom = (BottomLeftAngleDegrees + BottomRightAngleDegrees) * 0.5f;
			float left = (LeftTopAngleDegrees + LeftBottomAngleDegrees) * 0.5f;

			var trap = PrimitiveTextureFactory.GetAntialiasedTrapezoid(
				_graphicsDevice,
				rect.Width,
				rect.Height,
				LeftSideOffset,
				top,
				right,
				bottom,
				left
			);
			_spriteBatch.Draw(trap, rect, Color.White);

			var size = _font.MeasureString(title) * TextScale;
			var pos = new Vector2(rect.X + (rect.Width - size.X) / 2f, rect.Y + (rect.Height - size.Y) / 2f);
			_spriteBatch.DrawString(_font, title, pos, Color.White, 0f, Vector2.Zero, TextScale, SpriteEffects.None, 0f);
		}
	}
}
