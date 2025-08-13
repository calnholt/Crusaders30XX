using System;
using System.Collections.Generic;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Renders animated cathedral light beams when the current battle location is Cathedral.
	/// Draws after the background and under foreground elements.
	/// </summary>
	[DebugTab("CathedralLighting")] 
	public class CathedralLightingSystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;

		private bool _isActive;
		private float _elapsedSeconds;
		private Texture2D _beamTexture; // soft-edged rectangular gradient
		private readonly Random _random = new Random();

		private readonly List<Beam> _beams = new();

		// --- Runtime adjustable settings (debug menu) ---
		private int _numberOfBeams = 6;
		[DebugEditable(DisplayName = "Beams", Step = 1f, Min = 1f, Max = 24f)]
		public int NumberOfBeams { get => _numberOfBeams; set => _numberOfBeams = Math.Max(1, value); }

		private float _beamBaseThicknessPx = 140f;
		[DebugEditable(DisplayName = "Beam Thickness (px)", Step = 1f, Min = 4f, Max = 2000f)]
		public float BeamBaseThicknessPx { get => _beamBaseThicknessPx; set => _beamBaseThicknessPx = MathHelper.Clamp(value, 4f, 2000f); }

		private float _beamSpreadPx = 420f;
		[DebugEditable(DisplayName = "Beam Spread (px)", Step = 5f, Min = 0f, Max = 5000f)]
		public float BeamSpreadPx { get => _beamSpreadPx; set => _beamSpreadPx = MathHelper.Clamp(value, 0f, 5000f); }

		private float _baseAlpha = 0.55f;
		[DebugEditable(DisplayName = "Base Alpha", Step = 0.01f, Min = 0f, Max = 1f)]
		public float BaseAlpha { get => _baseAlpha; set => _baseAlpha = MathHelper.Clamp(value, 0f, 1f); }

		private float _variationAmount = 0.75f;
		[DebugEditable(DisplayName = "Cloud Variation", Step = 0.01f, Min = 0f, Max = 2f)]
		public float VariationAmount { get => _variationAmount; set => _variationAmount = MathHelper.Clamp(value, 0f, 2f); }

		private float _cloudSpeedHz1 = 0.10f;
		private float _cloudSpeedHz2 = 0.23f;
		[DebugEditable(DisplayName = "Cloud Speed 1 (Hz)", Step = 0.01f, Min = 0f, Max = 2f)]
		public float CloudSpeedHz1 { get => _cloudSpeedHz1; set => _cloudSpeedHz1 = MathHelper.Clamp(value, 0f, 2f); }
		[DebugEditable(DisplayName = "Cloud Speed 2 (Hz)", Step = 0.01f, Min = 0f, Max = 2f)]
		public float CloudSpeedHz2 { get => _cloudSpeedHz2; set => _cloudSpeedHz2 = MathHelper.Clamp(value, 0f, 2f); }

		// Slight excess to ensure full coverage across the diagonal
		private float _lengthOverscan = 1.15f;
		[DebugEditable(DisplayName = "Length Overscan", Step = 0.01f, Min = 1f, Max = 2f)]
		public float LengthOverscan { get => _lengthOverscan; set => _lengthOverscan = MathHelper.Clamp(value, 1f, 2f); }

		private struct Beam
		{
			public float OffsetPx;           // perpendicular offset from top-right anchor along normal
			public float ThicknessJitter;    // multiplicative jitter [0.85..1.15]
			public float Phase1;             // radians for cloud layer 1
			public float Phase2;             // radians for cloud layer 2
			public float IntensityBias;      // baseline multiplier per-beam
		}

		public CathedralLightingSystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
			: base(entityManager)
		{
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
			EventManager.Subscribe<ChangeBattleLocationEvent>(OnChangeLocation);
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			// Presentation-only
			return Array.Empty<Entity>();
		}

		public override void Update(GameTime gameTime)
		{
			_elapsedSeconds += (float)gameTime.ElapsedGameTime.TotalSeconds;
			EnsureBeamTexture();
			EnsureBeamsInitialized();
			base.Update(gameTime);
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		public void Draw()
		{
			if (!_isActive || _beamTexture == null || _numberOfBeams <= 0) return;

			int viewportW = _graphicsDevice.Viewport.Width;
			int viewportH = _graphicsDevice.Viewport.Height;

			// Diagonal from top-right to bottom-left
			var start = new Vector2(viewportW, 0f);
			var dir = new Vector2(-viewportW, viewportH);
			float diagLength = dir.Length() * _lengthOverscan;
			dir.Normalize();
			var normal = new Vector2(-dir.Y, dir.X); // 90° cw
			float rotation = MathF.Atan2(dir.Y, dir.X);

			float texW = _beamTexture.Width;
			float texH = _beamTexture.Height;
			var origin = new Vector2(0f, texH / 2f); // left-center so X-scale stretches along the beam

			for (int i = 0; i < _beams.Count; i++)
			{
				var b = _beams[i];
				float t = _elapsedSeconds;
				float layer1 = 0.5f + 0.5f * MathF.Sin(MathHelper.TwoPi * _cloudSpeedHz1 * t + b.Phase1);
				float layer2 = 0.5f + 0.5f * MathF.Sin(MathHelper.TwoPi * _cloudSpeedHz2 * t + b.Phase2 + b.OffsetPx * 0.01f);
				float clouds = layer1 * layer2; // simple 2-layer interference
				float intensity = _baseAlpha * (b.IntensityBias * (0.5f + 0.5f * (1f - _variationAmount + _variationAmount * clouds)));
				intensity = MathHelper.Clamp(intensity, 0f, 1f);

				float thickness = _beamBaseThicknessPx * b.ThicknessJitter * (0.9f + 0.2f * clouds);
				var position = start + normal * b.OffsetPx;
				var scale = new Vector2(diagLength / texW, thickness / texH);

				// warm sunlight color
				var color = new Color(255, 245, 210) * intensity;
				_spriteBatch.Draw(_beamTexture, position, null, color, rotation, origin, scale, SpriteEffects.None, 0f);
			}
		}

		private void OnChangeLocation(ChangeBattleLocationEvent evt)
		{
			if (evt == null) return;
			if (evt.Location.HasValue)
			{
				_isActive = evt.Location.Value == BattleLocation.Cathedral;
			}
			else if (!string.IsNullOrWhiteSpace(evt.TexturePath))
			{
				// Fallback: activate if texture name suggests cathedral
				_isActive = evt.TexturePath.IndexOf("cathedral", StringComparison.OrdinalIgnoreCase) >= 0;
			}
		}

		private void EnsureBeamsInitialized()
		{
			if (_beams.Count == _numberOfBeams) return;
			_beams.Clear();
			if (_numberOfBeams <= 0) return;

			for (int i = 0; i < _numberOfBeams; i++)
			{
				float u = _numberOfBeams == 1 ? 0.5f : (i / (float)(_numberOfBeams - 1)); // [0..1]
				float offset = MathHelper.Lerp(-_beamSpreadPx, _beamSpreadPx, u);
				// add subtle jitter to avoid perfect uniformity
				offset += (float)(_random.NextDouble() * 40.0 - 20.0);
				float thicknessJitter = 0.9f + 0.2f * (float)_random.NextDouble();
				float phase1 = MathHelper.TwoPi * (float)_random.NextDouble();
				float phase2 = MathHelper.TwoPi * (float)_random.NextDouble();
				float bias = 0.85f + 0.3f * (float)_random.NextDouble();

				_beams.Add(new Beam
				{
					OffsetPx = offset,
					ThicknessJitter = thicknessJitter,
					Phase1 = phase1,
					Phase2 = phase2,
					IntensityBias = bias
				});
			}
		}

		private void EnsureBeamTexture()
		{
			if (_beamTexture != null) return;

			int texWidth = 512;  // length axis
			int texHeight = 64;  // thickness axis
			_beamTexture = new Texture2D(_graphicsDevice, texWidth, texHeight, false, SurfaceFormat.Color);
			var data = new Color[texWidth * texHeight];

			for (int y = 0; y < texHeight; y++)
			{
				float v = (y + 0.5f) / texHeight; // 0..1
				float dy = MathF.Abs(v - 0.5f) / 0.5f; // 0 at center, 1 at edges
				// Soft edge across thickness using smooth step
				float edge = 1f - (dy * dy * (3f - 2f * dy));

				for (int x = 0; x < texWidth; x++)
				{
					float u = (x + 0.5f) / texWidth; // along length
					// Gentle head fade-in and tail fade-out to avoid harsh caps
					float head = u; // 0..1
					float tail = 1f - u;
					float cap = MathF.Min(1f, MathF.Min(head * 2.5f, tail * 1.2f));
					float a = MathHelper.Clamp(edge * cap, 0f, 1f);
					// store as premultiplied-friendly white; tint applied at draw
					data[y * texWidth + x] = Color.FromNonPremultiplied(255, 255, 255, (int)(a * 255));
				}
			}

			_beamTexture.SetData(data);
		}
	}
}


