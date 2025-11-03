using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("POI Radius Debug")]
	public class POIRadiusDebugDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly Dictionary<(int radius, int thickness), Texture2D> _ringCache = new();

		[DebugEditable(DisplayName = "Enabled", Step = 1)]
		public bool Enabled { get; set; } = false;

		[DebugEditable(DisplayName = "Show Reveal Radius", Step = 1)]
		public bool ShowRevealRadius { get; set; } = false;

		[DebugEditable(DisplayName = "Show Unrevealed Radius", Step = 1)]
		public bool ShowUnrevealedRadius { get; set; } = false;

		[DebugEditable(DisplayName = "Ring Thickness", Step = 1, Min = 1, Max = 20)]
		public int RingThickness { get; set; } = 2;

		public POIRadiusDebugDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
			: base(entityManager)
		{
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;

			EventManager.Subscribe<DeleteCachesEvent>(_ => OnDeleteCaches());
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<PointOfInterest>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			// draw-only system
		}

		public void Draw()
		{
			if (!Enabled) return;

			var vp = _graphicsDevice.Viewport;
			int screenW = vp.Width;
			int screenH = vp.Height;
			int thickness = RingThickness < 1 ? 1 : RingThickness;
			if (!ShowRevealRadius && !ShowUnrevealedRadius) return;

			var cam = EntityManager.GetEntity("LocationCamera")?.GetComponent<LocationCameraState>();
			float mapScale = cam?.MapScale ?? 1.0f;

			var list = EntityManager
				.GetEntitiesWithComponent<PointOfInterest>()
				.Select(e => new { E = e, P = e.GetComponent<PointOfInterest>(), T = e.GetComponent<Transform>() })
				.Where(x => x.P != null && x.T != null)
				.ToList();

			foreach (var x in list)
			{
				var cx = x.T.Position.X;
				var cy = x.T.Position.Y;

				int rReveal = x.P.RevealRadius;
				if (ShowRevealRadius && rReveal > 0)
				{
					float scaledRadius = rReveal * mapScale;
					// simple viewport cull
					if (!(cx + scaledRadius < 0 || cy + scaledRadius < 0 || cx - scaledRadius > screenW || cy - scaledRadius > screenH))
					{
						DrawRing(new Vector2(cx, cy), (int)scaledRadius, Color.Red, thickness);
					}
				}

				int rUnrevealed = x.P.UnrevealedRadius;
				if (ShowUnrevealedRadius && rUnrevealed > 0)
				{
					float scaledRadius = rUnrevealed * mapScale;
					if (!(cx + scaledRadius < 0 || cy + scaledRadius < 0 || cx - scaledRadius > screenW || cy - scaledRadius > screenH))
					{
						DrawRing(new Vector2(cx, cy), (int)scaledRadius, Color.Blue, thickness);
					}
				}
			}
		}

		private void OnDeleteCaches()
		{
			foreach (var kv in _ringCache)
			{
				try { kv.Value?.Dispose(); } catch { }
			}
			_ringCache.Clear();
		}

		private Texture2D GetRingTexture(int radius, int thickness)
		{
			if (radius < 1) radius = 1;
			if (thickness < 1) thickness = 1;
			var key = (radius, thickness);
			if (_ringCache.TryGetValue(key, out var existing) && existing != null && !existing.IsDisposed) return existing;

			int d = radius * 2;
			var tex = new Texture2D(_graphicsDevice, d, d);
			var data = new Color[d * d];

			// Anti-aliased ring via difference of two AA discs (outer - inner)
			float outerRadius = radius - 0.5f;
			float innerRadius = Math.Max(0f, radius - thickness) + 0.5f;
			float smooth = 1.0f; // pixel-wide smoothing band

			for (int y = 0; y < d; y++)
			{
				float dy = y - radius + 0.5f;
				for (int x = 0; x < d; x++)
				{
					float dx = x - radius + 0.5f;
					float dist = MathF.Sqrt(dx * dx + dy * dy);

					float outerAlpha;
					if (dist <= outerRadius - smooth) outerAlpha = 1f;
					else if (dist >= outerRadius + smooth) outerAlpha = 0f;
					else outerAlpha = 0.5f + 0.5f * (outerRadius - dist) / smooth;

					float innerAlpha;
					if (dist <= innerRadius - smooth) innerAlpha = 1f;
					else if (dist >= innerRadius + smooth) innerAlpha = 0f;
					else innerAlpha = 0.5f + 0.5f * (innerRadius - dist) / smooth;

					float ringAlpha = outerAlpha - innerAlpha;
					if (ringAlpha < 0f) ringAlpha = 0f;
					if (ringAlpha > 1f) ringAlpha = 1f;

					byte A = (byte)MathHelper.Clamp((int)Math.Round(ringAlpha * 255f), 0, 255);
					data[y * d + x] = Color.FromNonPremultiplied(255, 255, 255, A);
				}
			}

			tex.SetData(data);
			_ringCache[key] = tex;
			return tex;
		}

		private void DrawRing(Vector2 center, int radius, Color color, int thickness)
		{
			var tex = GetRingTexture(radius, thickness);
			_spriteBatch.Draw(tex, center, sourceRectangle: null, color: color, rotation: 0f, origin: new Vector2(radius, radius), scale: 1f, effects: SpriteEffects.None, layerDepth: 0f);
		}
	}
}


