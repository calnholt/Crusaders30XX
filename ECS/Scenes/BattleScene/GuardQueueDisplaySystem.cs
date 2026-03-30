using System;
using System.Collections.Generic;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Singletons;
using Crusaders30XX.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("Guard Queue Display")]
	public class GuardQueueDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private SpriteFont _font;
		private readonly Dictionary<(int radius, int thickness), Texture2D> _ringCache = new();

		// Animation state
		private readonly Dictionary<int, BreakAnim> _breakAnims = new();
		private readonly List<GainAnim> _gainAnims = new();
		private readonly Dictionary<int, float> _pipScales = new(); // queue index → gain scale, recomputed each frame
		private int _nextAnimId = 0;

		[DebugEditable(DisplayName = "Offset Y", Step = 2, Min = -400, Max = 400)]
		public int OffsetY { get; set; } = -260;

		[DebugEditable(DisplayName = "Pip Radius", Step = 1, Min = 4, Max = 32)]
		public int PipRadius { get; set; } = 12;

		[DebugEditable(DisplayName = "Pip Gap", Step = 1, Min = 0, Max = 32)]
		public int PipGap { get; set; } = 8;

		[DebugEditable(DisplayName = "Pip Thickness", Step = 1, Min = 1, Max = 8)]
		public int PipThickness { get; set; } = 3;

		[DebugEditable(DisplayName = "Font Scale", Step = 0.05f, Min = 0.1f, Max = 2f)]
		public float FontScale { get; set; } = 0.5f;

		[DebugEditable(DisplayName = "Break Duration", Step = 0.05f, Min = 0.1f, Max = 1f)]
		public float BreakDuration { get; set; } = 0.3f;

		[DebugEditable(DisplayName = "Break Max Scale", Step = 0.1f, Min = 1f, Max = 3f)]
		public float BreakMaxScale { get; set; } = 1.5f;

		[DebugEditable(DisplayName = "Gain Duration", Step = 0.05f, Min = 0.1f, Max = 1f)]
		public float GainDuration { get; set; } = 0.25f;

		[DebugEditable(DisplayName = "Gain Overshoot Scale", Step = 0.05f, Min = 1f, Max = 2f)]
		public float GainOvershootScale { get; set; } = 1.15f;

		[DebugEditable(DisplayName = "Gain Overshoot Peak", Step = 0.05f, Min = 0.1f, Max = 0.9f)]
		public float GainOvershootPeak { get; set; } = 0.6f;

		[DebugEditable(DisplayName = "Pip Color R", Step = 5, Min = 0, Max = 255)]
		public int PipColorR { get; set; } = 100;

		[DebugEditable(DisplayName = "Pip Color G", Step = 5, Min = 0, Max = 255)]
		public int PipColorG { get; set; } = 180;

		[DebugEditable(DisplayName = "Pip Color B", Step = 5, Min = 0, Max = 255)]
		public int PipColorB { get; set; } = 255;

		public GuardQueueDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
			: base(entityManager)
		{
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
			EventManager.Subscribe<GuardConsumedEvent>(OnGuardConsumed);
			EventManager.Subscribe<GuardGainedEvent>(OnGuardGained);
			EventManager.Subscribe<DeleteCachesEvent>(OnDeleteCaches);
			EventManager.Subscribe<LoadSceneEvent>(OnLoadScene);
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<GuardQueue>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

			// Advance break animations, remove completed ones
			var completedBreaks = new List<int>();
			foreach (var kv in _breakAnims)
			{
				kv.Value.Elapsed += dt;
				if (kv.Value.Elapsed >= BreakDuration)
					completedBreaks.Add(kv.Key);
			}
			foreach (var id in completedBreaks)
				_breakAnims.Remove(id);

			// Advance gain animations, remove completed ones
			for (int i = _gainAnims.Count - 1; i >= 0; i--)
			{
				_gainAnims[i].Elapsed += dt;
				if (_gainAnims[i].Elapsed >= GainDuration)
					_gainAnims.RemoveAt(i);
			}

			// Precompute pip scale for each queue index from active gain animations
			_pipScales.Clear();
			foreach (var anim in _gainAnims)
			{
				float progress = Math.Clamp(anim.Elapsed / GainDuration, 0f, 1f);
				float scale = progress < GainOvershootPeak
					? MathHelper.Lerp(0f, GainOvershootScale, progress / GainOvershootPeak)
					: MathHelper.Lerp(GainOvershootScale, 1f, (progress - GainOvershootPeak) / (1f - GainOvershootPeak));
				_pipScales[anim.QueueIndex] = scale;
			}
		}

		public void Draw()
		{
			// Lazy-load font
			if (_font == null)
			{
				_font = FontSingleton.ContentFont;
				if (_font == null) return;
			}

			var enemy = EntityManager.GetEntity("Enemy");
			if (enemy == null) return;
			var gq = enemy.GetComponent<GuardQueue>();
			if ((gq == null || gq.Queue.Count == 0) && _breakAnims.Count == 0) return;

			var t = enemy.GetComponent<Transform>();
			if (t == null) return;

			var pipColor = new Color(PipColorR, PipColorG, PipColorB);
			DrawActivePips(gq, t, pipColor);
			DrawBreakAnimations(pipColor);
		}

		// Render the live guard pips centered above the enemy
		private void DrawActivePips(GuardQueue gq, Transform t, Color pipColor)
		{
			if (gq == null) return;
			int count = gq.Queue.Count;
			if (count == 0) return;

			int diameter = PipRadius * 2;
			int totalWidth = count * diameter + Math.Max(0, count - 1) * PipGap;
			float centerX = t.Position.X;
			float centerY = t.Position.Y + OffsetY;
			int startX = (int)Math.Round(centerX - totalWidth / 2f);

			for (int i = 0; i < count; i++)
			{
				int value = gq.Queue[i];
				int x = startX + i * (diameter + PipGap) + PipRadius;

				// Apply gain animation scale if this pip was just added (precomputed in UpdateEntity)
				float scale = _pipScales.TryGetValue(i, out var pipScale) ? pipScale : 1f;

				DrawGuardPip(new Vector2(x, (int)centerY), PipRadius, pipColor, scale, 1f, value);
			}
		}

		// Render fading/scaling pips for consumed guards
		private void DrawBreakAnimations(Color pipColor)
		{
			foreach (var kv in _breakAnims)
			{
				var anim = kv.Value;
				float progress = Math.Clamp(anim.Elapsed / BreakDuration, 0f, 1f);
				float breakScale = MathHelper.Lerp(1f, BreakMaxScale, progress);
				float breakAlpha = MathHelper.Lerp(1f, 0f, progress);
				DrawGuardPip(anim.Position, PipRadius, pipColor, breakScale, breakAlpha, anim.Value);
			}
		}

		private void DrawGuardPip(Vector2 center, int radius, Color color, float scale, float alpha, int value)
		{
			var drawColor = color * alpha;
			int scaledRadius = Math.Max(1, (int)Math.Round(radius * scale));

			// Ring outline
			var tex = GetRingTexture(scaledRadius, PipThickness);
			_spriteBatch.Draw(tex, center, sourceRectangle: null, color: drawColor,
				rotation: 0f, origin: new Vector2(scaledRadius, scaledRadius),
				scale: 1f, effects: SpriteEffects.None, layerDepth: 0f);

			// Centered value text
			string text = value.ToString();
			var textSize = _font.MeasureString(text) * FontScale * scale;
			var textPos = center - textSize / 2f;
			_spriteBatch.DrawString(_font, text, textPos, drawColor,
				0f, Vector2.Zero, FontScale * scale, SpriteEffects.None, 0f);
		}

		private void OnGuardConsumed(GuardConsumedEvent e)
		{
			// Compute the position of the consumed (front) pip before removal
			var enemy = EntityManager.GetEntity("Enemy");
			if (enemy == null) return;
			var t = enemy.GetComponent<Transform>();
			if (t == null) return;

			int totalCount = e.RemainingCount + 1;
			int diameter = PipRadius * 2;
			int totalWidth = totalCount * diameter + Math.Max(0, totalCount - 1) * PipGap;
			float centerX = t.Position.X;
			float centerY = t.Position.Y + OffsetY;
			int startX = (int)Math.Round(centerX - totalWidth / 2f);
			int x = startX + PipRadius; // first pip

			_breakAnims[_nextAnimId++] = new BreakAnim
			{
				Position = new Vector2(x, (int)centerY),
				Value = e.GuardValue,
				Elapsed = 0f
			};
		}

		private void OnGuardGained(GuardGainedEvent e)
		{
			var enemy = EntityManager.GetEntity("Enemy");
			if (enemy == null) return;
			var gq = enemy.GetComponent<GuardQueue>();
			if (gq == null) return;

			_gainAnims.Add(new GainAnim
			{
				QueueIndex = gq.Queue.Count - 1,
				Elapsed = 0f
			});
		}

		private void OnDeleteCaches(DeleteCachesEvent evt)
		{
			foreach (var kv in _ringCache)
			{
				try { kv.Value?.Dispose(); } catch { }
			}
			_ringCache.Clear();
		}

		private void OnLoadScene(LoadSceneEvent e)
		{
			_breakAnims.Clear();
			_gainAnims.Clear();
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

			float outerRadius = radius - 0.5f;
			float innerRadius = Math.Max(0f, radius - thickness) + 0.5f;
			float smooth = 1.0f;

			for (int y = 0; y < d; y++)
			{
				float dy = y - radius + 0.5f;
				for (int x = 0; x < d; x++)
				{
					float dx = x - radius + 0.5f;
					float dist = MathF.Sqrt(dx * dx + dy * dy);

					float outerAlpha = SmoothEdge(dist, outerRadius, smooth);
					float innerAlpha = SmoothEdge(dist, innerRadius, smooth);
					float ringAlpha = Math.Clamp(outerAlpha - innerAlpha, 0f, 1f);

					byte A = (byte)MathHelper.Clamp((int)Math.Round(ringAlpha * 255f), 0, 255);
					data[y * d + x] = Color.FromNonPremultiplied(255, 255, 255, A);
				}
			}

			tex.SetData(data);
			_ringCache[key] = tex;
			return tex;
		}

		private static float SmoothEdge(float dist, float edgeRadius, float smooth)
		{
			if (dist <= edgeRadius - smooth) return 1f;
			if (dist >= edgeRadius + smooth) return 0f;
			return 0.5f + 0.5f * (edgeRadius - dist) / smooth;
		}

		private class BreakAnim
		{
			public Vector2 Position;
			public int Value;
			public float Elapsed;
		}

		private class GainAnim
		{
			public int QueueIndex;
			public float Elapsed;
		}
	}
}
