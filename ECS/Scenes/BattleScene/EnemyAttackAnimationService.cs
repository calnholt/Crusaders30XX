using System;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Systems
{
	internal static class EnemyAttackAnimationService
	{
		/// <summary>
		/// Computes the absorb tween (panel shrinking toward enemy) during EnemyAttack phase.
		/// Returns the interpolated position and remaining panel scale (1 -> 0).
		/// </summary>
		public static (float panelScale, Vector2 approachPos) ComputeAbsorbTween(
			Vector2 center, Vector2 enemyPos, int yOffset, float elapsed, float duration)
		{
			var dur = Math.Max(0.05f, duration);
			float tTween = MathHelper.Clamp(elapsed / dur, 0f, 1f);
			float ease = 1f - (float)Math.Pow(1f - tTween, 3);
			var targetPos = enemyPos + new Vector2(0, yOffset);
			var approachPos = Vector2.Lerp(center, targetPos, ease);
			float panelScale = MathHelper.Lerp(1f, 0f, ease);
			return (panelScale, approachPos);
		}

		/// <summary>
		/// Computes the impact squash/stretch animation returning squash factors and content scale.
		/// </summary>
		public static (float squashX, float squashY, float contentScale) ComputeImpactSquash(
			float elapsed, float duration, float squashXFactor, float squashYFactor, float overshoot)
		{
			float t = Math.Clamp(elapsed / Math.Max(0.0001f, duration), 0f, 1f);
			float back = 1f + overshoot * (float)Math.Pow(1f - t, 3);
			float squashX = MathHelper.Lerp(squashXFactor, 1f, t) * back;
			float squashY = MathHelper.Lerp(squashYFactor, 1f, t) / back;
			float contentScale = Math.Min(squashX, squashY);
			return (squashX, squashY, contentScale);
		}

		/// <summary>
		/// Computes the shake offset during impact.
		/// </summary>
		public static Vector2 ComputeShake(float elapsed, float duration, int amplitude, Random rand)
		{
			if (elapsed >= duration || amplitude <= 0)
				return Vector2.Zero;

			float shakeT = 1f - Math.Clamp(elapsed / Math.Max(0.0001f, duration), 0f, 1f);
			int sx = rand.Next(-amplitude, amplitude + 1);
			int sy = rand.Next(-amplitude, amplitude + 1);
			return new Vector2(sx, sy) * shakeT;
		}

		/// <summary>
		/// Measures the panel size given font, lines, padding, and width constraints.
		/// Returns (width, height) in pixels.
		/// </summary>
		public static (int width, int height) MeasurePanelSize(
			Microsoft.Xna.Framework.Graphics.SpriteFont font,
			System.Collections.Generic.List<(string text, float scale)> lines,
			int padding, int maxWidth, int minWidth, int contentLimit,
			float titleSpacing, float lineSpacing)
		{
			float maxW = 0f;
			float totalH = 0f;
			bool isFirstTitle = true;
			foreach (var (text, lineScale) in lines)
			{
				var parts = Crusaders30XX.ECS.Utils.TextUtils.WrapText(font, text, lineScale, contentLimit);
				foreach (var p in parts)
				{
					var sz = font.MeasureString(p);
					maxW = Math.Max(maxW, sz.X * lineScale);
					float spacing = isFirstTitle ? titleSpacing : lineSpacing;
					totalH += sz.Y * lineScale + spacing;
					if (isFirstTitle) isFirstTitle = false;
				}
			}

			int w = (int)Math.Ceiling(Math.Min(maxW + padding * 2, maxWidth));
			w = Math.Max(w, minWidth);
			int h = (int)Math.Ceiling(totalH) + padding * 2;
			return (w, h);
		}
	}
}
