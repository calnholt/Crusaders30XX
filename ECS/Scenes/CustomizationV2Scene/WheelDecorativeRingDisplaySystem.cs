using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("CV2 Rings")]
	public class WheelDecorativeRingDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private Texture2D _pixel;

		[DebugEditable(DisplayName = "Outer Ring Radius", Step = 4, Min = 100, Max = 400)]
		public int OuterRingRadius { get; set; } = 220;

		[DebugEditable(DisplayName = "Outer Ring Border", Step = 1, Min = 1, Max = 4)]
		public int OuterRingBorder { get; set; } = 1;

		[DebugEditable(DisplayName = "Outer Ring R", Step = 1, Min = 0, Max = 255)]
		public int OuterRingR { get; set; } = 160;

		[DebugEditable(DisplayName = "Outer Ring G", Step = 1, Min = 0, Max = 255)]
		public int OuterRingG { get; set; } = 0;

		[DebugEditable(DisplayName = "Outer Ring B", Step = 1, Min = 0, Max = 255)]
		public int OuterRingB { get; set; } = 0;

		[DebugEditable(DisplayName = "Outer Ring Alpha", Step = 5, Min = 0, Max = 255)]
		public int OuterRingAlpha { get; set; } = 38;

		[DebugEditable(DisplayName = "Inner Ring Radius", Step = 4, Min = 50, Max = 300)]
		public int InnerRingRadius { get; set; } = 155;

		[DebugEditable(DisplayName = "Inner Ring Alpha", Step = 5, Min = 0, Max = 255)]
		public int InnerRingAlpha { get; set; } = 10;

		[DebugEditable(DisplayName = "Dash Length", Step = 1, Min = 2, Max = 20)]
		public int DashLength { get; set; } = 8;

		[DebugEditable(DisplayName = "Gap Length", Step = 1, Min = 2, Max = 20)]
		public int GapLength { get; set; } = 6;

		public WheelLayoutSystem LayoutSystem { get; set; }

		public WheelDecorativeRingDisplaySystem(EntityManager em, GraphicsDevice gd, SpriteBatch sb) : base(em)
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

			if (_pixel == null)
			{
				_pixel = new Texture2D(_graphicsDevice, 1, 1);
				_pixel.SetData(new[] { Color.White });
			}

			var center = LayoutSystem?.GetWheelCenter() ?? new Vector2(480, 540);

			// Outer ring - circle outline
			DrawCircleOutline(center, OuterRingRadius, OuterRingBorder, new Color(OuterRingR, OuterRingG, OuterRingB, OuterRingAlpha));

			// Inner dashed ring
			DrawDashedCircle(center, InnerRingRadius, new Color(255, 255, 255, InnerRingAlpha));
		}

		private void DrawCircleOutline(Vector2 center, int radius, int thickness, Color color)
		{
			var outerCircle = PrimitiveTextureFactory.GetAntiAliasedCircle(_graphicsDevice, radius + thickness);
			var innerCircle = PrimitiveTextureFactory.GetAntiAliasedCircle(_graphicsDevice, radius);

			int outerD = (radius + thickness) * 2;
			int innerD = radius * 2;

			_spriteBatch.Draw(outerCircle, new Rectangle((int)(center.X - radius - thickness), (int)(center.Y - radius - thickness), outerD, outerD), color);
			// Punch out inner with background color
			var bgColor = new Color(10, 10, 10);
			_spriteBatch.Draw(innerCircle, new Rectangle((int)(center.X - radius), (int)(center.Y - radius), innerD, innerD), bgColor);
		}

		private void DrawDashedCircle(Vector2 center, int radius, Color color)
		{
			float circumference = MathF.PI * 2 * radius;
			int segments = (int)(circumference / (DashLength + GapLength));
			float angleStep = MathF.PI * 2f / segments;
			float dashAngle = (float)DashLength / radius;

			for (int i = 0; i < segments; i++)
			{
				float startAngle = i * angleStep;
				float endAngle = startAngle + dashAngle;

				for (float a = startAngle; a < endAngle; a += 0.02f)
				{
					float x = center.X + radius * MathF.Cos(a);
					float y = center.Y + radius * MathF.Sin(a);
					_spriteBatch.Draw(_pixel, new Rectangle((int)x, (int)y, 1, 1), color);
				}
			}
		}
	}
}
