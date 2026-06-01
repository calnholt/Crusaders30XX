using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("Pixel Burst")]
	public class PixelBurstDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;

		[DebugEditable(DisplayName = "Pixel Step", Step = 1, Min = 1, Max = 8)]
		public int PixelStep { get; set; } = 3;

		[DebugEditable(DisplayName = "Alpha Threshold", Step = 1, Min = 0, Max = 255)]
		public int AlphaThreshold { get; set; } = 10;

		[DebugEditable(DisplayName = "Max Particles", Step = 100, Min = 100, Max = 12000)]
		public int MaxParticles { get; set; } = 4000;

		[DebugEditable(DisplayName = "Blast Radius (px)", Step = 5f, Min = 10f, Max = 600f)]
		public float BlastRadius { get; set; } = 180f;

		[DebugEditable(DisplayName = "Speed Min", Step = 5f, Min = 0f, Max = 2000f)]
		public float SpeedMin { get; set; } = 120f;

		[DebugEditable(DisplayName = "Speed Max", Step = 5f, Min = 0f, Max = 2000f)]
		public float SpeedMax { get; set; } = 420f;

		[DebugEditable(DisplayName = "Gravity Y", Step = 10f, Min = -4000f, Max = 4000f)]
		public float GravityY { get; set; } = 420f;

		[DebugEditable(DisplayName = "Drag", Step = 0.01f, Min = 0.8f, Max = 1f)]
		public float Drag { get; set; } = 0.98f;

		[DebugEditable(DisplayName = "Max Burst Duration (s)", Step = 0.05f, Min = 0.1f, Max = 10f)]
		public float MaxBurstDurationSeconds { get; set; } = 2.5f;

		[DebugEditable(DisplayName = "Lifetime Min (s)", Step = 0.05f, Min = 0.05f, Max = 5f)]
		public float LifetimeMin { get; set; } = 0.5f;

		[DebugEditable(DisplayName = "Lifetime Max (s)", Step = 0.05f, Min = 0.05f, Max = 5f)]
		public float LifetimeMax { get; set; } = 1.2f;

		[DebugEditable(DisplayName = "Particle Size Min", Step = 0.5f, Min = 0.5f, Max = 16f)]
		public float ParticleSizeMin { get; set; } = 2f;

		[DebugEditable(DisplayName = "Particle Size Max", Step = 0.5f, Min = 0.5f, Max = 16f)]
		public float ParticleSizeMax { get; set; } = 4f;

		[DebugEditable(DisplayName = "Fade Power", Step = 0.05f, Min = 0.1f, Max = 4f)]
		public float FadePower { get; set; } = 0.75f;

		[DebugEditable(DisplayName = "Outward Bias", Step = 0.05f, Min = 0f, Max = 1f)]
		public float OutwardBias { get; set; } = 0.85f;

		[DebugEditable(DisplayName = "Velocity Jitter", Step = 0.05f, Min = 0f, Max = 1f)]
		public float VelocityJitter { get; set; } = 0.25f;

		private struct Particle
		{
			public Vector2 Position;
			public Vector2 Velocity;
			public float Age;
			public float Lifetime;
			public float Size;
			public Color Color;
			public Vector2 SpawnPosition;
			public float BlastRadiusLimit;
		}

		private class ActiveBurst
		{
			public Guid BurstId;
			public int SourceEntityId;
			public bool IsPreview;
			public float Age;
			public readonly List<Particle> Particles = new();
		}

		private readonly List<ActiveBurst> _bursts = new();
		private Texture2D _pixel;
		private static readonly Random _rng = new();
		private static readonly Vector2 PixelOrigin = new(0.5f, 0.5f);

		public PixelBurstDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
			: base(entityManager)
		{
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
			EventManager.Subscribe<PixelBurstAnimationRequested>(OnPixelBurstRequested);
			EventManager.Subscribe<DeleteCachesEvent>(_ => ClearAll());
		}

		protected override IEnumerable<Entity> GetRelevantEntities() => Array.Empty<Entity>();

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		public override void Update(GameTime gameTime)
		{
			float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
			if (dt <= 0f)
			{
				base.Update(gameTime);
				return;
			}

			for (int b = _bursts.Count - 1; b >= 0; b--)
			{
				var burst = _bursts[b];
				burst.Age += dt;
				bool timedOut = burst.Age >= MaxBurstDurationSeconds;

				var particles = burst.Particles;
				int writeIndex = 0;
				for (int readIndex = 0; readIndex < particles.Count; readIndex++)
				{
					var p = particles[readIndex];
					bool integrate = PortraitPixelBurstLayout.ShouldIntegrateParticle(p.Age);
					p.Age += dt;
					if (integrate)
					{
						p.Velocity.Y += GravityY * dt;
						p.Velocity *= Drag;
						p.Position += p.Velocity * dt;
						ClampTravelFromSpawn(ref p);
					}

					if (p.Age < p.Lifetime)
					{
						particles[writeIndex++] = p;
					}
				}
				if (writeIndex < particles.Count)
				{
					particles.RemoveRange(writeIndex, particles.Count - writeIndex);
				}

				if (timedOut || burst.Particles.Count == 0)
				{
					EventManager.Publish(new PixelBurstAnimationCompleted
					{
						BurstId = burst.BurstId,
						SourceEntityId = burst.SourceEntityId,
						IsPreview = burst.IsPreview
					});
					_bursts.RemoveAt(b);
				}
			}

			base.Update(gameTime);
		}

		public void Draw()
		{
			EnsurePixel();
			if (_bursts.Count == 0) return;

			foreach (var burst in _bursts)
			{
				for (int i = 0; i < burst.Particles.Count; i++)
				{
					var p = burst.Particles[i];
					float t = MathHelper.Clamp(p.Age / Math.Max(0.0001f, p.Lifetime), 0f, 1f);
					float alpha = MathF.Pow(1f - t, FadePower);
					var color = p.Color * alpha;
					_spriteBatch.Draw(
						_pixel,
						p.Position,
						null,
						color,
						0f,
						PixelOrigin,
						p.Size,
						SpriteEffects.None,
						0f);
				}
			}
		}

		private void OnPixelBurstRequested(PixelBurstAnimationRequested evt)
		{
			if (evt?.Texture == null) return;

			var spawns = PortraitPixelBurstSampler.Sample(
				evt.Texture,
				evt.Center,
				evt.DrawTopLeft,
				evt.DrawScale,
				PixelStep,
				AlphaThreshold,
				MaxParticles,
				BlastRadius,
				SpeedMin,
				SpeedMax,
				OutwardBias,
				VelocityJitter,
				LifetimeMin,
				LifetimeMax,
				ParticleSizeMin,
				ParticleSizeMax,
				_rng);

			var burst = new ActiveBurst
			{
				BurstId = evt.BurstId == Guid.Empty ? Guid.NewGuid() : evt.BurstId,
				SourceEntityId = evt.SourceEntityId,
				IsPreview = evt.IsPreview,
				Age = 0f
			};
			burst.Particles.Capacity = spawns.Count;

			for (int i = 0; i < spawns.Count; i++)
			{
				var s = spawns[i];
				burst.Particles.Add(new Particle
				{
					Position = s.Position,
					Velocity = s.Velocity,
					Age = 0f,
					Lifetime = s.Lifetime,
					Size = s.Size,
					Color = s.Color,
					SpawnPosition = s.Position,
					BlastRadiusLimit = s.BlastRadius
				});
			}

			_bursts.Add(burst);
			LogBurstSpawnSample(evt, burst);

			if (burst.Particles.Count == 0)
			{
				EventManager.Publish(new PixelBurstAnimationCompleted
				{
					BurstId = burst.BurstId,
					SourceEntityId = burst.SourceEntityId,
					IsPreview = burst.IsPreview
				});
				_bursts.Remove(burst);
			}
		}

		private static void ClampTravelFromSpawn(ref Particle p)
		{
			p.Position = PortraitPixelBurstLayout.ClampTravelFromSpawn(
				p.Position,
				p.SpawnPosition,
				p.BlastRadiusLimit);
		}

		private static void LogBurstSpawnSample(PixelBurstAnimationRequested evt, ActiveBurst burst)
		{
			const int maxSamples = 16;
			int count = burst.Particles.Count;
			var samples = new JsonArray();
			if (count > 0)
			{
				int step = System.Math.Max(1, count / maxSamples);
				for (int i = 0; i < count && samples.Count < maxSamples; i += step)
				{
					var p = burst.Particles[i].Position;
					samples.Add(new JsonObject
					{
						["index"] = i,
						["x"] = System.Math.Round(p.X, 2),
						["y"] = System.Math.Round(p.Y, 2)
					});
				}
			}

			float minX = 0f, maxX = 0f, minY = 0f, maxY = 0f;
			if (count > 0)
			{
				minX = maxX = burst.Particles[0].Position.X;
				minY = maxY = burst.Particles[0].Position.Y;
				for (int i = 1; i < count; i++)
				{
					var p = burst.Particles[i].Position;
					minX = System.Math.Min(minX, p.X);
					maxX = System.Math.Max(maxX, p.X);
					minY = System.Math.Min(minY, p.Y);
					maxY = System.Math.Max(maxY, p.Y);
				}
			}

			LoggingService.Append("PixelBurstDisplaySystem.OnPixelBurstRequested", new JsonObject
			{
				["burstId"] = burst.BurstId.ToString(),
				["sourceEntityId"] = burst.SourceEntityId,
				["isPreview"] = burst.IsPreview,
				["particleCount"] = count,
				["textureSize"] = $"{evt.Texture.Width}x{evt.Texture.Height}",
				["centerX"] = System.Math.Round(evt.Center.X, 2),
				["centerY"] = System.Math.Round(evt.Center.Y, 2),
				["drawTopLeftX"] = System.Math.Round(evt.DrawTopLeft.X, 2),
				["drawTopLeftY"] = System.Math.Round(evt.DrawTopLeft.Y, 2),
				["drawScaleX"] = System.Math.Round(evt.DrawScale.X, 4),
				["drawScaleY"] = System.Math.Round(evt.DrawScale.Y, 4),
				["boundsMinX"] = System.Math.Round(minX, 2),
				["boundsMaxX"] = System.Math.Round(maxX, 2),
				["boundsMinY"] = System.Math.Round(minY, 2),
				["boundsMaxY"] = System.Math.Round(maxY, 2),
				["spawnSamples"] = samples
			});
		}

		private void ClearAll()
		{
			_bursts.Clear();
			PortraitPixelBurstSampler.ClearCache();
		}

		private void EnsurePixel()
		{
			if (_pixel != null) return;
			_pixel = new Texture2D(_graphicsDevice, 1, 1);
			_pixel.SetData(new[] { Color.White });
		}

		[DebugAction("Test enemy pixel burst")]
		public void Debug_TestEnemyPixelBurst()
		{
			var enemy = EntityManager.GetEntity("Enemy");
			if (enemy == null) return;
			EventManager.Publish(new BeginDefeatPresentationEvent { Enemy = enemy, IsPreview = true });
		}
	}
}
