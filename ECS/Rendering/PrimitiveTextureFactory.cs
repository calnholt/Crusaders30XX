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
		// Cache rounded squares by device, size, and corner radius
		private static readonly Dictionary<(int deviceId, int size, int radius), Texture2D> _roundedSquareCache = new();
		// Cache equilateral triangles by device and size
		private static readonly Dictionary<(int deviceId, int size), Texture2D> _triangleCache = new();
		// Cache trapezoids by device and all parameters
		private static readonly Dictionary<(int deviceId, float width, float height, float leftOffset, float topAngle, float rightAngle, float bottomAngle, float leftAngle), Texture2D> _trapezoidCache = new();

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

		public static Texture2D GetRoundedSquare(GraphicsDevice device, int size, int radius)
		{
			if (size < 1) size = 1;
			if (radius < 0) radius = 0;
			int deviceId = device?.GetHashCode() ?? 0;
			var key = (deviceId, size, radius);
			if (_roundedSquareCache.TryGetValue(key, out var existing) && existing != null) return existing;
			var tex = RoundedRectTextureFactory.CreateRoundedRect(device, size, size, radius);
			_roundedSquareCache[key] = tex;
			return tex;
		}

		public static Texture2D GetEquilateralTriangle(GraphicsDevice device, int size)
		{
			if (size < 1) size = 1;
			int deviceId = device?.GetHashCode() ?? 0;
			var key = (deviceId, size);
			if (_triangleCache.TryGetValue(key, out var existing) && existing != null) return existing;

			var tex = new Texture2D(device, size, size);
			var data = new Color[size * size];

			// Define an equilateral triangle centered horizontally; slight margins for better AA
			float s = size - 2f; // side length with 1px margin
			float h = 0.8660254f * s; // sqrt(3)/2 * s
			float apexY = (size - h) * 0.5f;
			var v0 = new Vector2(size * 0.5f, apexY);           // apex (top)
			var v1 = new Vector2(v0.X - s * 0.5f, apexY + h);  // left base
			var v2 = new Vector2(v0.X + s * 0.5f, apexY + h);  // right base

			// Barycentric inside test
			for (int y = 0; y < size; y++)
			{
				float py = y + 0.5f;
				for (int x = 0; x < size; x++)
				{
					float px = x + 0.5f;
					var p = new Vector2(px, py);
					float denom = (v1.Y - v2.Y) * (v0.X - v2.X) + (v2.X - v1.X) * (v0.Y - v2.Y);
					if (System.Math.Abs(denom) < 1e-5f)
					{
						data[y * size + x] = Color.Transparent;
						continue;
					}
					float w0 = ((v1.Y - v2.Y) * (p.X - v2.X) + (v2.X - v1.X) * (p.Y - v2.Y)) / denom;
					float w1 = ((v2.Y - v0.Y) * (p.X - v2.X) + (v0.X - v2.X) * (p.Y - v2.Y)) / denom;
					float w2 = 1f - w0 - w1;
					bool inside = (w0 >= 0f) && (w1 >= 0f) && (w2 >= 0f);
					if (inside)
					{
						data[y * size + x] = Color.FromNonPremultiplied(255, 255, 255, 255);
					}
					else
					{
						data[y * size + x] = Color.Transparent;
					}
				}
			}

			tex.SetData(data);
			_triangleCache[key] = tex;
			return tex;
		}

		public static Texture2D GetAntialiasedTrapezoid(
			GraphicsDevice device,
			float width,
			float height,
			float leftSideOffset,
			float topEdgeAngleDegrees,
			float rightEdgeAngleDegrees,
			float bottomEdgeAngleDegrees,
			float leftEdgeAngleDegrees)
		{
			if (width < 1) width = 1;
			if (height < 1) height = 1;
			int deviceId = device?.GetHashCode() ?? 0;
			
			// Use rounded values for cache key to avoid floating point precision issues
			var key = (
				deviceId,
				(float)System.Math.Round(width, 2),
				(float)System.Math.Round(height, 2),
				(float)System.Math.Round(leftSideOffset, 2),
				(float)System.Math.Round(topEdgeAngleDegrees, 2),
				(float)System.Math.Round(rightEdgeAngleDegrees, 2),
				(float)System.Math.Round(bottomEdgeAngleDegrees, 2),
				(float)System.Math.Round(leftEdgeAngleDegrees, 2)
			);
			
			if (_trapezoidCache.TryGetValue(key, out var existing) && existing != null)
			{
				return existing;
			}

			// Render at higher resolution for antialiasing, then scale down
			const int superSampleFactor = 2; // Render at 2x resolution
			int texWidth = (int)System.Math.Ceiling(width * superSampleFactor);
			int texHeight = (int)System.Math.Ceiling(height * superSampleFactor);
			if (texWidth < 1) texWidth = 1;
			if (texHeight < 1) texHeight = 1;

			var tex = new Texture2D(device, texWidth, texHeight);
			var data = new Color[texWidth * texHeight];

			// Convert angles to radians
			float topAngleRad = MathHelper.ToRadians(topEdgeAngleDegrees);
			float rightAngleRad = MathHelper.ToRadians(rightEdgeAngleDegrees);
			float bottomAngleRad = MathHelper.ToRadians(bottomEdgeAngleDegrees);
			float leftAngleRad = MathHelper.ToRadians(leftEdgeAngleDegrees);

			// Scale parameters for supersampling
			float scaledLeftXBase = System.Math.Max(0f, leftSideOffset * superSampleFactor);
			float scaledBottomYBase = (texHeight - 1f);
			float scaledWidth = texWidth;
			float scaledHeight = texHeight;

			// Calculate edge positions at each Y level
			float leftXAtTop = scaledLeftXBase;
			float leftXAtBottom = scaledLeftXBase + (float)System.Math.Tan(leftAngleRad) * scaledHeight;
			float rightXAtTop = scaledWidth - 1f;
			float rightXAtBottom = scaledWidth - 1f + (float)System.Math.Tan(rightAngleRad) * scaledHeight;

			// Antialiasing threshold - distance in pixels for smooth edge
			const float aaThreshold = 1.0f;

			// For each pixel, check if inside trapezoid with antialiasing
			for (int y = 0; y < texHeight; y++)
			{
				float py = y + 0.5f; // Center of pixel
				
				// Interpolate left and right edges based on Y position
				float t = (texHeight <= 1) ? 0f : (y / (float)(texHeight - 1)); // 0 at top, 1 at bottom
				float leftXAtY = MathHelper.Lerp(leftXAtTop, leftXAtBottom, t);
				float rightXAtY = MathHelper.Lerp(rightXAtTop, rightXAtBottom, t);

				for (int x = 0; x < texWidth; x++)
				{
					int idx = y * texWidth + x;
					float px = x + 0.5f; // Center of pixel
					
					// Calculate distances to edges for antialiasing
					float distToLeft = px - leftXAtY;
					float distToRight = rightXAtY - px;
					
					// Top edge equation: y = tan(topAngleRad) * (x - leftXAtTop)
					float topEdgeYAtX = (float)System.Math.Tan(topAngleRad) * (px - leftXAtTop);
					float distToTop = py - topEdgeYAtX;
					
					// Bottom edge equation: y = bottomYBase + tan(bottomAngleRad) * (x - leftXAtBottom)
					float bottomEdgeYAtX = scaledBottomYBase + (float)System.Math.Tan(bottomAngleRad) * (px - leftXAtBottom);
					float distToBottom = bottomEdgeYAtX - py;

					// Check if pixel is inside trapezoid
					bool insideX = distToLeft >= 0 && distToRight >= 0;
					bool insideY = distToTop >= 0 && distToBottom >= 0;
					bool inside = insideX && insideY;

					if (!inside)
					{
						data[idx] = Color.Transparent;
						continue;
					}

					// Calculate alpha for antialiasing based on distance to nearest edge
					float minDist = System.Math.Min(System.Math.Min(distToLeft, distToRight), System.Math.Min(distToTop, distToBottom));
					float alpha = 1.0f;
					
					if (minDist < aaThreshold)
					{
						// Smooth transition at edges
						alpha = MathHelper.Clamp(minDist / aaThreshold, 0f, 1f);
					}

					// Set pixel with alpha for antialiasing
					byte alphaByte = (byte)MathHelper.Clamp((int)System.Math.Round(alpha * 255f), 0, 255);
					data[idx] = Color.FromNonPremultiplied(0, 0, 0, alphaByte); // Black with alpha
				}
			}

			tex.SetData(data);
			_trapezoidCache[key] = tex;
			return tex;
		}
	}
}


