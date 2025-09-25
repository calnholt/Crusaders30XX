using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
	public class CustomizationBackgroundSystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly ContentManager _content;
		private Texture2D _background;

		public CustomizationBackgroundSystem(EntityManager em, GraphicsDevice gd, SpriteBatch sb, ContentManager content) : base(em)
		{
			_graphicsDevice = gd;
			_spriteBatch = sb;
			_content = content;
			TryLoad();
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<SceneState>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			// no-op; draw only
		}

		public void Draw()
		{
			var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
			if (scene == null || scene.Current != SceneId.Customization) return;
			if (_background == null) return;

			int vw = _graphicsDevice.Viewport.Width;
			int vh = _graphicsDevice.Viewport.Height;
			var dest = new Rectangle(0, 0, vw, vh);
			_spriteBatch.Draw(_background, dest, Color.White);
		}

		private void TryLoad()
		{
			try
			{
				_background = _content.Load<Texture2D>("customization");
			}
			catch
			{
				try { _background = _content.Load<Texture2D>("Customization"); }
				catch { _background = null; }
			}
		}
	}
}


