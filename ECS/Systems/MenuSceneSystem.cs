using System;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Crusaders30XX.ECS.Systems
{
	public class MenuSceneSystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly ContentManager _content;
		private readonly SpriteFont _font;
		private Texture2D _demonTexture;
		private MouseState _prevMouse;

		public MenuSceneSystem(EntityManager em, GraphicsDevice gd, SpriteBatch sb, ContentManager content, SpriteFont font) : base(em)
		{
			_graphicsDevice = gd;
			_spriteBatch = sb;
			_content = content;
			_font = font;
			_prevMouse = Mouse.GetState();
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<SceneState>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			var scene = entity.GetComponent<SceneState>();
			if (scene == null || scene.Current != SceneId.Menu) return;
			var mouse = Mouse.GetState();
			bool click = mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released;
			if (click && IsMouseOverDemon(mouse.Position))
			{
				EventManager.Publish(new StartBattleRequested());
			}
			_prevMouse = mouse;
		}

		public void Draw()
		{
			var state = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
			if (state == null || state.Current != SceneId.Menu) return;
			EnsureTexture();
			if (_demonTexture == null || _font == null) return;
			int viewportW = _graphicsDevice.Viewport.Width;
			int viewportH = _graphicsDevice.Viewport.Height;
			float coverage = 0.40f;
			float desiredHeight = coverage * viewportH;
			float scale = desiredHeight / _demonTexture.Height;
			var origin = new Vector2(_demonTexture.Width / 2f, _demonTexture.Height / 2f);
			var center = new Vector2(viewportW / 2f, viewportH / 2f);
			_spriteBatch.Draw(_demonTexture, center, null, Color.White, 0f, origin, scale, SpriteEffects.None, 0f);
			var text = "Click the Demon to Fight";
			var size = _font.MeasureString(text);
			var textPos = new Vector2(center.X - size.X / 2f, center.Y + desiredHeight / 2f + 20f);
			_spriteBatch.DrawString(_font, text, textPos, Color.White);
		}

		private void EnsureTexture()
		{
			if (_demonTexture == null)
			{
				_demonTexture = _content.Load<Texture2D>("Demon");
			}
		}

		private bool IsMouseOverDemon(Point mouse)
		{
			EnsureTexture();
			if (_demonTexture == null) return false;
			int viewportW = _graphicsDevice.Viewport.Width;
			int viewportH = _graphicsDevice.Viewport.Height;
			float coverage = 0.40f;
			float desiredHeight = coverage * viewportH;
			float scale = desiredHeight / _demonTexture.Height;
			var center = new Vector2(viewportW / 2f, viewportH / 2f);
			var width = _demonTexture.Width * scale;
			var height = _demonTexture.Height * scale;
			var rect = new Rectangle((int)(center.X - width / 2f), (int)(center.Y - height / 2f), (int)width, (int)height);
			return rect.Contains(mouse);
		}
	}
}


