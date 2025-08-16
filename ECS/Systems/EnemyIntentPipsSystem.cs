using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("Enemy Intent Pips")] 
	public class EnemyIntentPipsSystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private Texture2D _pixel;

		[DebugEditable(DisplayName = "Pip Radius", Step = 1, Min = 2, Max = 64)]
		public int PipRadius { get; set; } = 8;
		[DebugEditable(DisplayName = "Pip Gap", Step = 1, Min = 0, Max = 64)]
		public int PipGap { get; set; } = 10;
		[DebugEditable(DisplayName = "Offset Y", Step = 2, Min = -400, Max = 400)]
		public int OffsetY { get; set; } = -160;
		[DebugEditable(DisplayName = "Row Gap", Step = 1, Min = 0, Max = 64)]
		public int RowGap { get; set; } = 14;

		public EnemyIntentPipsSystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
			: base(entityManager)
		{
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
			_pixel = new Texture2D(graphicsDevice, 1, 1);
			_pixel.SetData(new[] { Color.White });
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<Enemy>().Where(e => e.HasComponent<AttackIntent>());
		}

		protected override void UpdateEntity(Entity entity, Microsoft.Xna.Framework.GameTime gameTime) { }

		public void Draw()
		{
			foreach (var e in GetRelevantEntities())
			{
				var t = e.GetComponent<Transform>();
				var info = e.GetComponent<PortraitInfo>();
				var intent = e.GetComponent<AttackIntent>();
				if (t == null || info == null || intent == null) continue;
				int count = intent.Planned.Count;
				if (count <= 0) continue;

				// Compute row center above enemy
				var center = new Vector2(t.Position.X, t.Position.Y + OffsetY);
				int diameter = PipRadius * 2;
				int totalWidth = count * diameter + (count - 1) * PipGap;
				int startX = (int)System.Math.Round(center.X - totalWidth / 2f);

				// Determine the next-to-resolve step (min ResolveStep)
				int minStep = intent.Planned.Min(p => p.ResolveStep);

				for (int i = 0; i < count; i++)
				{
					var pa = intent.Planned[i];
					int x = startX + i * (diameter + PipGap) + PipRadius;
					int y = (int)center.Y;
					bool isNext = pa.ResolveStep == minStep;
					DrawCircle(new Vector2(x, y), PipRadius, isNext ? Color.Yellow : Color.LightGray, 2);
				}

				// Next turn preview row (smaller/faded) if present
				var next = e.GetComponent<NextTurnAttackIntent>();
				if (next != null && next.Planned.Count > 0)
				{
					int nCount = next.Planned.Count;
					int nDiameter = System.Math.Max(2, (int)System.Math.Round(PipRadius * 0.75f)) * 2;
					int nRadius = nDiameter / 2;
					int nTotalWidth = nCount * nDiameter + (nCount - 1) * PipGap;
					int nStartX = (int)System.Math.Round(center.X - nTotalWidth / 2f);
					int y2 = (int)center.Y + RowGap + nRadius + PipRadius;
					for (int i = 0; i < nCount; i++)
					{
						int x2 = nStartX + i * (nDiameter + PipGap) + nRadius;
						DrawCircle(new Vector2(x2, y2), nRadius, new Color(200, 200, 200, 180), 2);
					}
				}
			}
		}

		private void DrawCircle(Vector2 center, int radius, Color color, int thickness)
		{
			// Midpoint circle rasterization approximation using rectangles (fast + simple)
			int steps = System.Math.Max(12, radius * 6);
			for (int i = 0; i < steps; i++)
			{
				float a0 = MathHelper.TwoPi * (i / (float)steps);
				float a1 = MathHelper.TwoPi * ((i + 1) / (float)steps);
				var p0 = new Vector2(center.X + radius * System.MathF.Cos(a0), center.Y + radius * System.MathF.Sin(a0));
				var p1 = new Vector2(center.X + radius * System.MathF.Cos(a1), center.Y + radius * System.MathF.Sin(a1));
				DrawLine(p0, p1, color, thickness);
			}
		}

		private void DrawLine(Vector2 a, Vector2 b, Color color, int thickness)
		{
			float dx = b.X - a.X; float dy = b.Y - a.Y;
			float len = System.MathF.Max(1f, System.MathF.Sqrt(dx * dx + dy * dy));
			float ang = System.MathF.Atan2(dy, dx);
			_spriteBatch.Draw(_pixel, position: a, sourceRectangle: null, color: color, rotation: ang, origin: Vector2.Zero, scale: new Vector2(len, thickness), effects: SpriteEffects.None, layerDepth: 0f);
		}
	}
}


