using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Crusaders30XX.ECS.Singletons;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("Title Menu")]
	public class TitleMenuDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private Texture2D _pixel;
		private float _t;
		private MouseState _prevMouse;

		[DebugEditable(DisplayName = "Title Text", Step = 1)]
		public string TitleText { get; set; } = "Crusader 30XX";

		[DebugEditable(DisplayName = "Target Width %", Step = 0.05f, Min = 0.1f, Max = 1f)]
		public float TargetWidthPercent { get; set; } = 0.6f;

		[DebugEditable(DisplayName = "Min Text Scale", Step = 0.05f, Min = 0.05f, Max = 2f)]
		public float MinTextScale { get; set; } = 0.2f;

		[DebugEditable(DisplayName = "Max Text Scale", Step = 0.05f, Min = 0.1f, Max = 5f)]
		public float MaxTextScale { get; set; } = 2.0f;

		[DebugEditable(DisplayName = "Fade In Duration (s)", Step = 0.1f, Min = 0.1f, Max = 10f)]
		public float FadeInDurationSeconds { get; set; } = 1.5f;

		[DebugEditable(DisplayName = "Text Offset X", Step = 2, Min = -2000, Max = 2000)]
		public int TextOffsetX { get; set; } = 0;

		[DebugEditable(DisplayName = "Text Offset Y", Step = 2, Min = -2000, Max = 2000)]
		public int TextOffsetY { get; set; } = 0;

		[DebugEditable(DisplayName = "Background Alpha", Step = 0.05f, Min = 0f, Max = 1f)]
		public float BackgroundAlpha { get; set; } = 1f;

		public TitleMenuDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
			: base(entityManager)
		{
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
			_pixel = new Texture2D(graphicsDevice, 1, 1);
			_pixel.SetData(new[] { Color.White });
			_prevMouse = Mouse.GetState();
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<SceneState>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			var scene = entity.GetComponent<SceneState>();
			if (scene == null || scene.Current != SceneId.TitleMenu)
			{
				// Reset timer when not active so fade restarts upon returning
				_t = 0f;
				_prevMouse = Mouse.GetState();
				// Disable title menu click area if it exists
				var existing = EntityManager.GetEntity("TitleMenu_ClickArea");
				var uiExisting = existing?.GetComponent<UIElement>();
				if (uiExisting != null) uiExisting.IsInteractable = false;
				return;
			}

			float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
			_t += dt;

			// Ensure a full-screen interactable UI element exists for click handling (immediate, no fade gating)
			int w = _graphicsDevice.Viewport.Width;
			int h = _graphicsDevice.Viewport.Height;
			var clickArea = EntityManager.GetEntity("TitleMenu_ClickArea");
			if (clickArea == null)
			{
				clickArea = EntityManager.CreateEntity("TitleMenu_ClickArea");
				EntityManager.AddComponent(clickArea, new Transform { Position = new Vector2(0, 0), ZOrder = 10000 });
				EntityManager.AddComponent(clickArea, new UIElement { Bounds = new Rectangle(0, 0, w, h), IsInteractable = true });
			}
			else
			{
				var t = clickArea.GetComponent<Transform>();
				if (t != null) { t.Position = new Vector2(0, 0); t.ZOrder = 10000; }
				var ui = clickArea.GetComponent<UIElement>();
				if (ui == null)
				{
					EntityManager.AddComponent(clickArea, new UIElement { Bounds = new Rectangle(0, 0, w, h), IsInteractable = true });
				}
				else
				{
					ui.Bounds = new Rectangle(0, 0, w, h);
					ui.IsInteractable = true;
				}
			}

			// Use UIElement click flag instead of raw mouse. Trigger transition immediately when clicked.
			var uiClick = clickArea.GetComponent<UIElement>();
			if (uiClick != null && uiClick.IsClicked && !StateSingleton.IsActive)
			{
				EventManager.Publish(new ShowTransition { Scene = SceneId.Location });
			}

			_prevMouse = Mouse.GetState();
		}

		public void Draw()
		{
			var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
			if (scene == null || scene.Current != SceneId.TitleMenu) return;

			int w = _graphicsDevice.Viewport.Width;
			int h = _graphicsDevice.Viewport.Height;

			// Fill black background
			_spriteBatch.Draw(_pixel, new Rectangle(0, 0, w, h), new Color(0f, 0f, 0f, MathHelper.Clamp(BackgroundAlpha, 0f, 1f)));

			// Compute scale from viewport width
			string text = TitleText ?? string.Empty;
			var baseSize = FontSingleton.TitleFont.MeasureString(text);
			float targetWidth = MathHelper.Clamp(TargetWidthPercent, 0.1f, 1f) * w;
			float scale = baseSize.X > 0.001f ? targetWidth / baseSize.X : 1f;
			scale = MathHelper.Clamp(scale, System.Math.Max(0.01f, MinTextScale), System.Math.Max(MinTextScale, MaxTextScale));

			// Fade alpha from 0 -> 1
			float denom = System.Math.Max(0.0001f, FadeInDurationSeconds);
			float t01 = MathHelper.Clamp(_t / denom, 0f, 1f);
			var color = Color.White * t01;

			// Center text
			var size = baseSize * scale;
			var pos = new Vector2(w / 2f - size.X / 2f + TextOffsetX, h / 2f - size.Y / 2f + TextOffsetY);
			_spriteBatch.DrawString(FontSingleton.TitleFont, text, pos, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
		}
	}
}


