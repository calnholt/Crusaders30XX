using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("CV2 Background")]
	public class CustomizationV2BackgroundSystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private Texture2D _pixel;

		[DebugEditable(DisplayName = "BG Red", Step = 1, Min = 0, Max = 255)]
		public int BgR { get; set; } = 10;

		[DebugEditable(DisplayName = "BG Green", Step = 1, Min = 0, Max = 255)]
		public int BgG { get; set; } = 10;

		[DebugEditable(DisplayName = "BG Blue", Step = 1, Min = 0, Max = 255)]
		public int BgB { get; set; } = 10;

		public CustomizationV2BackgroundSystem(EntityManager em, GraphicsDevice gd, SpriteBatch sb) : base(em)
		{
			_graphicsDevice = gd;
			_spriteBatch = sb;
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<SceneState>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		public void Draw()
		{
			var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
			if (scene == null || scene.Current != SceneId.CustomizationV2) return;

			if (_pixel == null)
			{
				_pixel = new Texture2D(_graphicsDevice, 1, 1);
				_pixel.SetData(new[] { Color.White });
			}

			var bgColor = new Color(BgR, BgG, BgB);
			_spriteBatch.Draw(_pixel, new Rectangle(0, 0, Game1.VirtualWidth, Game1.VirtualHeight), bgColor);
		}
	}
}
