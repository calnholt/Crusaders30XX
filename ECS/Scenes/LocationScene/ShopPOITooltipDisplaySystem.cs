using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Locations;
using Crusaders30XX.ECS.Rendering;
using Crusaders30XX.ECS.Singletons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("Shop POI Tooltip")]
	public class ShopPOITooltipDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly SpriteFont _font = FontSingleton.TitleFont;
		private const string TooltipEntityName = "UI_ShopTooltip";
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

		// 8 independent angle controls (deg)
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

		public ShopPOITooltipDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
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

			// Find top-most hovered Shop POI
			var hovered = EntityManager.GetEntitiesWithComponent<UIElement>()
				.Select(e => new { E = e, UI = e.GetComponent<UIElement>(), T = e.GetComponent<Transform>(), P = e.GetComponent<PointOfInterest>() })
				.Where(x => x.UI != null && x.UI.IsHovered && x.P != null && x.P.Type == PointOfInterestType.Shop)
				.OrderByDescending(x => x.T?.ZOrder ?? 0)
				.FirstOrDefault();

			if (hovered == null)
			{
				if (_tooltipEntity != null)
				{
					EntityManager.DestroyEntity(_tooltipEntity.Id);
					_tooltipEntity = null;
				}
				return;
			}

			// Determine title
			string title = "Shop";
			if (!string.IsNullOrEmpty(hovered.P?.Id))
			{
				var all = LocationDefinitionCache.GetAll();
				foreach (var kv in all)
				{
					var def = kv.Value;
					if (def?.pointsOfInterest == null) continue;
					var found = def.pointsOfInterest.FirstOrDefault(p => p?.id == hovered.P.Id);
					if (found != null && !string.IsNullOrWhiteSpace(found.name))
					{
						title = found.name;
						break;
					}
				}
			}

			// Compute rect to the left or right of the hovered icon, based on available space (similar to quest tooltip)
			int pad = System.Math.Max(0, Padding);
			var size = _font.MeasureString(title) * TextScale;
			int width = (int)System.Math.Ceiling(size.X) + pad * 2 + System.Math.Max(0, LeftSideOffset);
			int height = System.Math.Max(24, TrapezoidHeight);
			var r = hovered.UI.Bounds;
			int viewportW = _graphicsDevice.Viewport.Width;
			int viewportH = _graphicsDevice.Viewport.Height;

			// Prefer the side with more room; default preference by screen halves
			int rightSpace = viewportW - (r.Right + Gap);
			int leftSpace = r.Left - Gap;
			bool canPlaceRight = rightSpace >= width;
			bool canPlaceLeft = leftSpace >= width;
			bool preferRight = r.Center.X < viewportW / 2;
			bool placeRight = canPlaceRight || (!canPlaceLeft && preferRight);
			if (!canPlaceRight && canPlaceLeft) placeRight = false;

			int rx = placeRight ? (r.Right + Gap) : (r.Left - Gap - width);
			int ry = r.Y + (r.Height - height) / 2;

			// Screen clamp
			rx = System.Math.Max(0, System.Math.Min(rx, viewportW - width));
			ry = System.Math.Max(0, System.Math.Min(ry, viewportH - height));
			var rect = new Microsoft.Xna.Framework.Rectangle(rx, ry, width, height);

			if (_tooltipEntity == null)
			{
				_tooltipEntity = EntityManager.CreateEntity(TooltipEntityName);
				EntityManager.AddComponent(_tooltipEntity, new Transform { Position = new Vector2(rect.X, rect.Y), ZOrder = 10001 });
				EntityManager.AddComponent(_tooltipEntity, new UIElement { Bounds = rect, IsInteractable = true });
				// Add a hold-to-enter hotkey attached to the hovered POI
				EntityManager.AddComponent(_tooltipEntity, new HotKey { Button = FaceButton.X, RequiresHold = true, ParentEntity = hovered.E, Position = HotKeyPosition.Below });
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
			}

			// Stash the title on the entity via Hint for easy retrieval during Draw
			var hint = _tooltipEntity.GetComponent<Hint>();
			if (hint == null)
			{
				EntityManager.AddComponent(_tooltipEntity, new Hint { Text = title });
			}
			else
			{
				hint.Text = title;
			}
		}

		public void Draw()
		{
			var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
			if (scene == null || scene.Current != SceneId.Location) return;
			if (_tooltipEntity == null) return;

			var ui = _tooltipEntity.GetComponent<UIElement>();
			var t = _tooltipEntity.GetComponent<Transform>();
			var hint = _tooltipEntity.GetComponent<Hint>();
			if (ui == null || t == null || hint == null) return;

			string title = string.IsNullOrEmpty(hint.Text) ? "Shop" : hint.Text;
			int pad = System.Math.Max(0, Padding);
			var rect = ui.Bounds;

			// Average paired angles to the factory's 4 inputs
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

			// Draw text centered
			var size = _font.MeasureString(title) * TextScale;
			var pos = new Vector2(rect.X + (rect.Width - size.X) / 2f, rect.Y + (rect.Height - size.Y) / 2f);
			_spriteBatch.DrawString(_font, title, pos, Color.White, 0f, Vector2.Zero, TextScale, SpriteEffects.None, 0f);
		}
	}
}


