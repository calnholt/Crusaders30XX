using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Rendering;
using Crusaders30XX.ECS.Singletons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("CV2 Browse Arrows")]
	public class BrowseArrowDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly SpriteFont _headingFont = FontSingleton.TitleFont;
		private Texture2D _pixel;

		[DebugEditable(DisplayName = "Arrow Size", Step = 2, Min = 10, Max = 40)]
		public int ArrowSize { get; set; } = 20;

		[DebugEditable(DisplayName = "Arrow Offset X", Step = 4, Min = 40, Max = 200)]
		public int ArrowOffsetX { get; set; } = 120;

		[DebugEditable(DisplayName = "Arrow Offset Y", Step = 4, Min = -60, Max = 60)]
		public int ArrowOffsetY { get; set; } = 40;

		[DebugEditable(DisplayName = "Arrow Alpha", Step = 5, Min = 0, Max = 255)]
		public int ArrowAlpha { get; set; } = 180;

		[DebugEditable(DisplayName = "Arrow R", Step = 1, Min = 0, Max = 255)]
		public int ArrowR { get; set; } = 196;

		[DebugEditable(DisplayName = "Arrow G", Step = 1, Min = 0, Max = 255)]
		public int ArrowG { get; set; } = 30;

		[DebugEditable(DisplayName = "Arrow B", Step = 1, Min = 0, Max = 255)]
		public int ArrowB { get; set; } = 58;

		[DebugEditable(DisplayName = "Key Hint Scale", Step = 0.01f, Min = 0.03f, Max = 0.15f)]
		public float KeyHintScale { get; set; } = 0.07f;

		[DebugEditable(DisplayName = "Key Hint Offset Y", Step = 2, Min = 0, Max = 40)]
		public int KeyHintOffsetY { get; set; } = 24;

		public WheelLayoutSystem LayoutSystem { get; set; }
		public BrowseStateSystem BrowseSystem { get; set; }

		public BrowseArrowDisplaySystem(EntityManager em, GraphicsDevice gd, SpriteBatch sb) : base(em)
		{
			_graphicsDevice = gd;
			_spriteBatch = sb;
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<SceneState>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		public void Draw()
		{
			var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
			if (scene == null || scene.Current != SceneId.CustomizationV2) return;

			var nav = EntityManager.GetEntitiesWithComponent<CustomizationV2NavigationState>().FirstOrDefault()?.GetComponent<CustomizationV2NavigationState>();
			if (nav == null || nav.ActiveTab != CustomizationV2TabType.Loadout) return;

			var loadout = EntityManager.GetEntitiesWithComponent<CustomizationV2LoadoutState>().FirstOrDefault()?.GetComponent<CustomizationV2LoadoutState>();
			if (loadout == null || loadout.HoveredSegmentIndex < 0) return;

			if (LayoutSystem == null || BrowseSystem == null) return;
			if (BrowseSystem.GetBrowseCount() <= 1) return;

			if (_pixel == null)
			{
				_pixel = new Texture2D(_graphicsDevice, 1, 1);
				_pixel.SetData(new[] { Color.White });
			}

			var center = LayoutSystem.GetWheelCenter();
			var arrowColor = new Color(ArrowR, ArrowG, ArrowB, ArrowAlpha);

			// Left arrow
			DrawArrow(center.X - ArrowOffsetX, center.Y + ArrowOffsetY, -1, arrowColor);

			// Right arrow
			DrawArrow(center.X + ArrowOffsetX, center.Y + ArrowOffsetY, 1, arrowColor);

			// Key hints
			DrawKeyHint(center.X - ArrowOffsetX, center.Y + ArrowOffsetY + KeyHintOffsetY, "A");
			DrawKeyHint(center.X + ArrowOffsetX, center.Y + ArrowOffsetY + KeyHintOffsetY, "D");
		}

		private void DrawArrow(float x, float y, int direction, Color color)
		{
			var triangle = PrimitiveTextureFactory.GetEquilateralTriangle(_graphicsDevice, ArrowSize);
			float rotation = direction > 0 ? MathHelper.PiOver2 : -MathHelper.PiOver2;
			var origin = new Vector2(triangle.Width / 2f, triangle.Height / 2f);
			_spriteBatch.Draw(triangle, new Vector2(x, y), null, color, rotation, origin, 1f, SpriteEffects.None, 0f);
		}

		private void DrawKeyHint(float x, float y, string key)
		{
			var size = _headingFont.MeasureString(key) * KeyHintScale;
			float kx = x - size.X / 2f;
			_spriteBatch.DrawString(_headingFont, key, new Vector2(kx, y), new Color(102, 102, 102), 0f, Vector2.Zero, KeyHintScale, SpriteEffects.None, 0f);
		}
	}
}
