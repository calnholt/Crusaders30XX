using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("Location Fog Display")]
	public class FogDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly ContentManager _content;
		private CircularMaskOverlay _overlay;
		private float _timeSeconds;

		[DebugEditable(DisplayName = "Mask Radius (px)", Step = 5f, Min = 10f, Max = 1000f)]
		public float RadiusPx { get; set; } = 300f;

		[DebugEditable(DisplayName = "Feather (px)", Step = 1f, Min = 0f, Max = 64f)]
		public float FeatherPx { get; set; } = 23f;

		[DebugEditable(DisplayName = "Warp Amount (px)", Step = 1f, Min = 0f, Max = 64f)]
		public float WarpAmountPx { get; set; } = 24f;

		[DebugEditable(DisplayName = "Warp Speed", Step = 0.05f, Min = 0f, Max = 3f)]
		public float WarpSpeed { get; set; } = 0.1f;

		[DebugEditable(DisplayName = "Noise Scale", Step = 0.0005f, Min = 0.0005f, Max = 0.02f)]
		public float NoiseScale { get; set; } = 0.02f;

		[DebugEditable(DisplayName = "Ease Speed", Step = 0.05f, Min = 0f, Max = 3f)]
		public float EaseSpeed { get; set; } = 0.2f;

		[DebugEditable(DisplayName = "Global Alpha Min", Step = 0.01f, Min = 0f, Max = 1f)]
		public float GlobalAlphaMin { get; set; } = 0.8f;

		[DebugEditable(DisplayName = "Global Alpha Max", Step = 0.01f, Min = 0f, Max = 1f)]
		public float GlobalAlphaMax { get; set; } = 1f;

		[DebugEditable(DisplayName = "Death Contrast", Step = 0.05f, Min = 0.5f, Max = 3f)]
		public float DeathContrast { get; set; } = 0.5f;

		[DebugEditable(DisplayName = "Lifeless Desaturate Mix", Step = 0.05f, Min = 0f, Max = 1f)]
		public float LifelessDesaturateMix { get; set; } = 0.1f;

		[DebugEditable(DisplayName = "Lifeless Darken Mul", Step = 0.05f, Min = 0f, Max = 1f)]
		public float LifelessDarkenMul { get; set; } = 1f;

		public FogDisplaySystem(EntityManager em, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, ContentManager content)
			: base(em)
		{
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
			_content = content;
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<SceneState>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
		}
		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);
			_timeSeconds += MathHelper.Max(0f, (float)gameTime.ElapsedGameTime.TotalSeconds);
		}

		public void Draw()
		{
			var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
			if (scene == null || scene.Current != SceneId.Location) return;

			EnsureOverlayLoaded();
			if (_overlay == null || !_overlay.IsAvailable) return;

			var centers = EntityManager
				.GetEntitiesWithComponent<PointOfInterest>()
				.Select(p => new { Position = p, RevealRadius = e.GetComponent<PointOfInterest>().RevealRadius })
				.ToList();

			if (centers.Count == 0) return;

			_overlay.CentersPx = centers;
			_overlay.RadiusPx = centers.Select(c => c.RevealRadius).ToList();
			_overlay.FeatherPx = FeatherPx;
			_overlay.WarpAmountPx = WarpAmountPx;
			_overlay.WarpSpeed = WarpSpeed;
			_overlay.NoiseScale = NoiseScale;
			_overlay.EaseSpeed = EaseSpeed;
			_overlay.GlobalAlphaMin = GlobalAlphaMin;
			_overlay.GlobalAlphaMax = GlobalAlphaMax;
			_overlay.DeathContrast = DeathContrast;
			_overlay.LifelessDesaturateMix = LifelessDesaturateMix;
			_overlay.LifelessDarkenMul = LifelessDarkenMul;
			_overlay.TimeSeconds = _timeSeconds;
			// Anchor distortion to world Y
			var cam = EntityManager.GetEntity("LocationCamera")?.GetComponent<LocationCameraState>();
			_overlay.CameraOriginYPx = cam?.Origin.Y ?? 0f;

			// Save current SpriteBatch device states and temporarily end the batch
			var savedBlend = _graphicsDevice.BlendState;
			var savedSampler = _graphicsDevice.SamplerStates[0];
			var savedDepth = _graphicsDevice.DepthStencilState;
			var savedRasterizer = _graphicsDevice.RasterizerState;
			_spriteBatch.End();

			// Draw overlay with its own begin/end using the effect
			_overlay.Begin(_spriteBatch);
			_overlay.Draw(_spriteBatch);
			_overlay.End(_spriteBatch);

			// Restore the previous SpriteBatch with saved states for subsequent draws
			_spriteBatch.Begin(
				SpriteSortMode.Immediate,
				savedBlend,
				savedSampler,
				savedDepth,
				savedRasterizer
			);
		}

		public void DrawComposite(Texture2D sceneTexture)
		{
			var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
			if (scene == null || scene.Current != SceneId.Location) return;

			EnsureOverlayLoaded();
			if (_overlay == null || !_overlay.IsAvailable) return;
			if (sceneTexture == null) return;

			var centers = EntityManager
				.GetEntitiesWithComponent<PointOfInterest>()
				.Select(e => e.GetComponent<Transform>())
				.Where(t => t != null)
				.Select(t => t.Position)
				.ToList();

			if (centers.Count == 0) return;

			_overlay.CentersPx = centers;
			_overlay.RadiusPx = RadiusPx;
			_overlay.FeatherPx = FeatherPx;
			_overlay.WarpAmountPx = WarpAmountPx;
			_overlay.WarpSpeed = WarpSpeed;
			_overlay.NoiseScale = NoiseScale;
			_overlay.EaseSpeed = EaseSpeed;
			_overlay.GlobalAlphaMin = GlobalAlphaMin;
			_overlay.GlobalAlphaMax = GlobalAlphaMax;
			_overlay.DeathContrast = DeathContrast;
			_overlay.LifelessDesaturateMix = LifelessDesaturateMix;
			_overlay.LifelessDarkenMul = LifelessDarkenMul;
			_overlay.TimeSeconds = _timeSeconds;

			// Save and end current SpriteBatch
			var savedBlend = _graphicsDevice.BlendState;
			var savedSampler = _graphicsDevice.SamplerStates[0];
			var savedDepth = _graphicsDevice.DepthStencilState;
			var savedRasterizer = _graphicsDevice.RasterizerState;
			_spriteBatch.End();

			// Composite the captured scene texture via the mask effect
			_overlay.Begin(_spriteBatch);
			_overlay.Draw(_spriteBatch, sceneTexture);
			_overlay.End(_spriteBatch);

			// Restore previous SpriteBatch
			_spriteBatch.Begin(
				SpriteSortMode.Immediate,
				savedBlend,
				savedSampler,
				savedDepth,
				savedRasterizer
			);
		}

		private void EnsureOverlayLoaded()
		{
			if (_overlay != null) return;
			Effect fx = null;
			try
			{
				fx = _content.Load<Effect>("Shaders/CircularMask");
			}
			catch { }
			_overlay = new CircularMaskOverlay(_graphicsDevice, fx);
		}
	}
}


