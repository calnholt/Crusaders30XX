using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Crusaders30XX.ECS.Singletons;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Displays a simple game-over overlay when the player dies: shows red centered text,
	/// then fades the screen to black and returns to the main menu.
	/// </summary>
	[DebugTab("Game Over Overlay")]
	public class GameOverOverlayDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly SpriteFont _font = FontSingleton.TitleFont;
		private readonly Texture2D _pixel;

		// Configurable timings
		[DebugEditable(DisplayName = "Overlay Fade In (s)", Step = 0.05f, Min = 0.01f, Max = 5f)]
		public float OverlayFadeInSeconds { get; set; } = 0.35f;

		[DebugEditable(DisplayName = "Text Fade Out (s)", Step = 0.05f, Min = 0.05f, Max = 10f)]
		public float TextFadeOutSeconds { get; set; } = 2.75f;

		[DebugEditable(DisplayName = "Text Scale", Step = 0.05f, Min = 0.2f, Max = 3f)]
		public float TextScale { get; set; } = 0.625f;

		[DebugEditable(DisplayName = "Text Offset X", Step = 1f, Min = -1000f, Max = 1000f)]
		public float TextOffsetX { get; set; } = 0f;

		[DebugEditable(DisplayName = "Text Offset Y", Step = 1f, Min = -1000f, Max = 1000f)]
		public float TextOffsetY { get; set; } = 0f;

		public string MessageText { get; set; } = "Thy Will Be Done";

		private bool _active;
		private float _elapsed;
		private bool _sceneSwitched;

		public GameOverOverlayDisplaySystem(EntityManager em, GraphicsDevice gd, SpriteBatch sb) : base(em)
		{
			_graphicsDevice = gd;
			_spriteBatch = sb;
			_pixel = new Texture2D(gd, 1, 1);
			_pixel.SetData(new[] { Color.White });
			EventManager.Subscribe<PlayerDied>(OnPlayerDied);
			EventManager.Subscribe<DeleteCachesEvent>(OnDeleteCachesEvent);
		}

		private void OnDeleteCachesEvent(DeleteCachesEvent evt)
		{
			_active = false;
			_elapsed = 0f;
			_sceneSwitched = false;
		}

		private void OnPlayerDied(PlayerDied evt)
		{
			if (_active) return;
			_active = true;
			_elapsed = 0f;
			_sceneSwitched = false;
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			// Tie updates to scene presence; we only need one entity to tick per frame
			return EntityManager.GetEntitiesWithComponent<SceneState>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			if (!_active) return;
			_elapsed += (float)gameTime.ElapsedGameTime.TotalSeconds;
			float total = System.Math.Max(0.0001f, OverlayFadeInSeconds) + System.Math.Max(0.0001f, TextFadeOutSeconds);
			if (!_sceneSwitched && _elapsed >= total)
			{
				// Switch back to menu
				EventManager.Publish(new ShowTransition { Scene = SceneId.Location });
				_sceneSwitched = true;
				_active = false; // stop drawing after switch
			}
		}

		public void Draw()
		{
			if (!_active || _font == null) return;
			int w = _graphicsDevice.Viewport.Width;
			int h = _graphicsDevice.Viewport.Height;

			// Phase timings
			float tOverlay = MathHelper.Clamp(_elapsed / System.Math.Max(0.0001f, OverlayFadeInSeconds), 0f, 1f);
			float tTextFade = MathHelper.Clamp((_elapsed - OverlayFadeInSeconds) / System.Math.Max(0.0001f, TextFadeOutSeconds), 0f, 1f);

			// 1) Draw background overlay first; it reaches full opacity and stays black
			_spriteBatch.Draw(_pixel, new Rectangle(0, 0, w, h), new Color(0f, 0f, 0f, tOverlay));

			// 2) Draw centered text on top; fade its color from red to black after overlay completes
			var text = MessageText ?? string.Empty;
			var baseSize = _font.MeasureString(text);
			float scale = System.Math.Max(0.01f, TextScale);
			var size = baseSize * scale;
			var pos = new Vector2(w / 2f - size.X / 2f + TextOffsetX, h / 2f - size.Y / 2f + TextOffsetY);
			// Lerp text color from Red -> Black over tTextFade (alpha stays 1)
			byte r = (byte)(255 * (1f - tTextFade));
			var textColor = new Color(r, 0, 0);
			var shadow = Color.Black * (0.6f * (1f - tTextFade));
			_spriteBatch.DrawString(_font, text, pos + new Vector2(2, 2), shadow, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
			_spriteBatch.DrawString(_font, text, pos, textColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
		}
	}
}


