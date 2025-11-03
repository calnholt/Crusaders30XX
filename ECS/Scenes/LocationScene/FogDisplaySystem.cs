using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System.Numerics;

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
		public float FeatherPx { get; set; } = 64f;

		[DebugEditable(DisplayName = "Warp Amount (px)", Step = 1f, Min = 0f, Max = 64f)]
		public float WarpAmountPx { get; set; } = 24f;

		[DebugEditable(DisplayName = "Warp Speed", Step = 0.05f, Min = 0f, Max = 3f)]
		public float WarpSpeed { get; set; } = 0.5f;

		[DebugEditable(DisplayName = "Noise Scale", Step = 0.0005f, Min = 0.0005f, Max = 0.5f)]
		public float NoiseScale { get; set; } = 0.2f;

		[DebugEditable(DisplayName = "Ease Speed", Step = 0.05f, Min = 0f, Max = 3f)]
		public float EaseSpeed { get; set; } = 0.45f;

		[DebugEditable(DisplayName = "Global Alpha Min", Step = 0.01f, Min = 0f, Max = 1f)]
		public float GlobalAlphaMin { get; set; } = 0.8f;

		[DebugEditable(DisplayName = "Global Alpha Max", Step = 0.01f, Min = 0f, Max = 1f)]
		public float GlobalAlphaMax { get; set; } = 1f;

		[DebugEditable(DisplayName = "Death Contrast", Step = 0.05f, Min = 0.5f, Max = 3f)]
		public float DeathContrast { get; set; } = 3f;

		[DebugEditable(DisplayName = "Lifeless Desaturate Mix", Step = 0.05f, Min = 0f, Max = 1f)]
		public float LifelessDesaturateMix { get; set; } = 0f;

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

			// Build circles: unlockers (revealed or completed) use DisplayRadius; adjacent-but-unrevealed use UnrevealedRadius
			// Hellrift POIs are always visible and act as unlockers
			var list = EntityManager
				.GetEntitiesWithComponent<PointOfInterest>()
				.Select(e => new { E = e, P = e.GetComponent<PointOfInterest>(), T = e.GetComponent<Transform>() })
				.Where(x => x.P != null && x.T != null)
				.ToList();
			var unlockers = list.Where(x => x.P.IsRevealed || x.P.IsCompleted || x.P.Type == "Hellrift").ToList();
			var unlockerIds = new System.Collections.Generic.HashSet<int>(unlockers.Select(x => x.E.Id));
			var centers = new System.Collections.Generic.List<Microsoft.Xna.Framework.Vector2>();
			var radii = new System.Collections.Generic.List<float>();
			var cam = EntityManager.GetEntity("LocationCamera")?.GetComponent<LocationCameraState>();
			float mapScale = cam?.MapScale ?? 1.0f;
			foreach (var u in unlockers)
			{
				centers.Add(new Microsoft.Xna.Framework.Vector2(u.T.Position.X, u.T.Position.Y));
				var drawRadius = (u.P.DisplayRadius > 0f) ? u.P.DisplayRadius : (u.P.IsCompleted ? u.P.RevealRadius : u.P.UnrevealedRadius);
				radii.Add(drawRadius * mapScale);
			}
			foreach (var x in list)
			{
				if (unlockerIds.Contains(x.E.Id)) continue;
				foreach (var u in unlockers)
				{
					// Scale world positions by map scale for distance checks
					float dx = (x.P.WorldPosition.X - u.P.WorldPosition.X) * mapScale;
					float dy = (x.P.WorldPosition.Y - u.P.WorldPosition.Y) * mapScale;
					float r = (u.P.DisplayRadius > 0f) ? u.P.DisplayRadius : (u.P.IsCompleted ? u.P.RevealRadius : u.P.UnrevealedRadius);
					r *= mapScale; // Scale radius for distance check
					if ((dx * dx) + (dy * dy) <= (r * r))
					{
						centers.Add(new Microsoft.Xna.Framework.Vector2(x.T.Position.X, x.T.Position.Y));
						radii.Add((float)x.P.UnrevealedRadius * mapScale);
						break;
					}
				}
			}

			if (centers.Count == 0) return;

			_overlay.CentersPx = centers;
			_overlay.RadiusPx = radii;
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
			if (sceneTexture == null) return;

			// Build circles: unlockers (revealed or completed) use RevealRadius; adjacent-but-unrevealed use UnrevealedRadius
			// Hellrift POIs are always visible and act as unlockers
			var list = EntityManager
				.GetEntitiesWithComponent<PointOfInterest>()
				.Select(e => new { E = e, P = e.GetComponent<PointOfInterest>(), T = e.GetComponent<Transform>() })
				.Where(x => x.P != null && x.T != null)
				.ToList();
			var unlockers = list.Where(x => x.P.IsRevealed || x.P.IsCompleted || x.P.Type == "Hellrift").ToList();
			var unlockerIds = new System.Collections.Generic.HashSet<int>(unlockers.Select(x => x.E.Id));
			var centers = new System.Collections.Generic.List<Microsoft.Xna.Framework.Vector2>();
			var radii = new System.Collections.Generic.List<float>();
			var cam = EntityManager.GetEntity("LocationCamera")?.GetComponent<LocationCameraState>();
			float mapScale = cam?.MapScale ?? 1.0f;
			foreach (var u in unlockers)
			{
				centers.Add(new Microsoft.Xna.Framework.Vector2(u.T.Position.X, u.T.Position.Y));
				var drawRadius = (u.P.DisplayRadius > 0f) ? u.P.DisplayRadius : (u.P.IsCompleted ? u.P.RevealRadius : u.P.UnrevealedRadius);
				radii.Add(drawRadius * mapScale);
			}
			foreach (var x in list)
			{
				if (unlockerIds.Contains(x.E.Id)) continue;
				foreach (var u in unlockers)
				{
					// Scale world positions by map scale for distance checks
					float dx = (x.P.WorldPosition.X - u.P.WorldPosition.X) * mapScale;
					float dy = (x.P.WorldPosition.Y - u.P.WorldPosition.Y) * mapScale;
					float r = (u.P.DisplayRadius > 0f) ? u.P.DisplayRadius : (u.P.IsCompleted ? u.P.RevealRadius : u.P.UnrevealedRadius);
					r *= mapScale; // Scale radius for distance check
					if ((dx * dx) + (dy * dy) <= (r * r))
					{
						centers.Add(new Microsoft.Xna.Framework.Vector2(x.T.Position.X, x.T.Position.Y));
						radii.Add((float)x.P.UnrevealedRadius * mapScale);
						break;
					}
				}
			}

			// Configure overlay params (will be used if masking is available)
			if (_overlay != null)
			{
				// Anchor distortion to world Y (match Draw path)
				_overlay.CameraOriginYPx = cam?.Origin.Y ?? 0f;
				_overlay.CentersPx = centers;
				_overlay.RadiusPx = radii;
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
			}

			// Save and end current SpriteBatch
			var savedBlend = _graphicsDevice.BlendState;
			var savedSampler = _graphicsDevice.SamplerStates[0];
			var savedDepth = _graphicsDevice.DepthStencilState;
			var savedRasterizer = _graphicsDevice.RasterizerState;
			_spriteBatch.End();

			// Decide whether we can apply masking
			bool canMask = (_overlay != null && _overlay.IsAvailable && centers.Count > 0);
			if (canMask)
			{
				// Composite the captured scene texture via the mask effect
				_overlay.Begin(_spriteBatch);
				_overlay.Draw(_spriteBatch, sceneTexture);
				_overlay.End(_spriteBatch);
			}
			else
			{
				// Fallback: present the captured scene without fog so we never show a black screen
				_spriteBatch.Begin(
					SpriteSortMode.Immediate,
					savedBlend,
					savedSampler,
					savedDepth,
					savedRasterizer
				);
				_spriteBatch.Draw(sceneTexture, _graphicsDevice.Viewport.Bounds, Color.White);
				_spriteBatch.End();
			}

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


