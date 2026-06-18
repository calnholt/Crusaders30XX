using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("Climb Background")]
	public class ClimbBackgroundDisplaySystem : Core.System
	{
		private readonly SpriteBatch _spriteBatch;
		private readonly Texture2D _pixel;
		private Texture2D _background;

		[DebugEditable(DisplayName = "Background Dim Alpha", Step = 0.01f, Min = 0f, Max = 1f)]
		public float BackgroundDimAlpha { get; set; } = 0.22f;

		[DebugEditable(DisplayName = "Background Anchor Y", Step = 1, Min = -400, Max = 400)]
		public int BackgroundAnchorY { get; set; } = 0;

		public ClimbBackgroundDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, ContentManager content)
			: base(entityManager)
		{
			_spriteBatch = spriteBatch;
			_pixel = new Texture2D(graphicsDevice, 1, 1);
			_pixel.SetData(new[] { Color.White });
			try
			{
				_background = content.Load<Texture2D>("desert_background_location");
			}
			catch (Exception)
			{
				_background = null;
			}
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			yield break;
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
		}

		public void Draw()
		{
			if (!IsClimbScene()) return;

			var dest = new Rectangle(0, 0, Game1.VirtualWidth, Game1.VirtualHeight);
			if (_background != null)
			{
				float scale = Math.Max(dest.Width / (float)_background.Width, dest.Height / (float)_background.Height);
				int w = (int)Math.Ceiling(_background.Width * scale);
				int h = (int)Math.Ceiling(_background.Height * scale);
				var bgDest = new Rectangle((dest.Width - w) / 2, BackgroundAnchorY, w, h);
				_spriteBatch.Draw(_background, bgDest, Color.White);
			}
			else
			{
				_spriteBatch.Draw(_pixel, dest, ClimbSceneDrawHelpers.Black1);
			}

			_spriteBatch.Draw(_pixel, dest, Color.Black * MathHelper.Clamp(BackgroundDimAlpha, 0f, 1f));
		}

		private bool IsClimbScene()
		{
			return EntityManager.GetEntitiesWithComponent<SceneState>()
				.FirstOrDefault()
				?.GetComponent<SceneState>()
				?.Current == SceneId.Climb;
		}
	}
}
