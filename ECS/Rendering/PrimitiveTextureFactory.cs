using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Rendering
{
	/// <summary>
	/// Factory for primitive textures (e.g., anti-aliased circles) with caching.
	/// </summary>
	public static class PrimitiveTextureFactory
	{
		// Cache AA circles by GraphicsDevice pointer and radius
		private static readonly Dictionary<(int deviceId, int radius), Texture2D> _aaCircleCache = new();

		public static Texture2D GetAntiAliasedCircle(GraphicsDevice device, int radius)
		{
			if (radius < 1) radius = 1;
			int deviceId = device?.GetHashCode() ?? 0;
			var key = (deviceId, radius);
			if (_aaCircleCache.TryGetValue(key, out var existing) && existing != null) return existing;
			int d = radius * 2;
			var circle = new Texture2D(device, d, d);
			var data = new Color[d * d];
			// Subpixel AA: inner and outer radii for linear falloff
			float rInner = radius - 0.5f;
			float rOuter = radius + 0.5f;
			float r2Inner = rInner * rInner;
			float r2Outer = rOuter * rOuter;
			for (int y = 0; y < d; y++)
			{
				float dy = y - radius + 0.5f;
				for (int x = 0; x < d; x++)
				{
					float dx = x - radius + 0.5f;
					float dist2 = dx * dx + dy * dy;
					float alpha;
					if (dist2 <= r2Inner) alpha = 1f;
					else if (dist2 >= r2Outer) alpha = 0f;
					else
					{
						float dist = (float)System.Math.Sqrt(dist2);
						alpha = 1f - (dist - rInner) / (rOuter - rInner);
					}
					byte A = (byte)MathHelper.Clamp((int)System.Math.Round(alpha * 255f), 0, 255);
					data[y * d + x] = Color.FromNonPremultiplied(255, 255, 255, A);
				}
			}
			circle.SetData(data);
			_aaCircleCache[key] = circle;
			return circle;
		}
	}
}


