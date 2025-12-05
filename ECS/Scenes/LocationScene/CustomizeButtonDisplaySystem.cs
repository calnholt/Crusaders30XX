using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Rendering;
using Crusaders30XX.ECS.Singletons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("Customize Button")]
	public class CustomizeButtonDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly SpriteFont _font = FontSingleton.ContentFont;
		private Texture2D _roundedRectCache;
		private Texture2D _pixel;

		[DebugEditable(DisplayName = "Button Width", Step = 2, Min = 40, Max = 800)]
		public int ButtonWidth { get; set; } = 200;

		[DebugEditable(DisplayName = "Button Height", Step = 2, Min = 24, Max = 300)]
		public int ButtonHeight { get; set; } = 56;

		[DebugEditable(DisplayName = "Button Margin", Step = 1, Min = 0, Max = 120)]
		public int ButtonMargin { get; set; } = 16;

		[DebugEditable(DisplayName = "Button Padding", Step = 1, Min = 0, Max = 64)]
		public int ButtonPadding { get; set; } = 8;

		[DebugEditable(DisplayName = "Text Scale", Step = 0.05f, Min = 0.1f, Max = 2.0f)]
		public float TextScale { get; set; } = 0.2f;

		[DebugEditable(DisplayName = "Corner Radius", Step = 1, Min = 0, Max = 64)]
		public int CornerRadius { get; set; } = 12;

		public CustomizeButtonDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch) : base(entityManager)
		{
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
			_pixel = new Texture2D(_graphicsDevice, 1, 1);
			_pixel.SetData(new[] { Color.White });
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<SceneState>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			var scene = entity.GetComponent<SceneState>();
			if (scene == null || (scene.Current != SceneId.Location && scene.Current != SceneId.Shop)) return;

			// Block interactions during scene transition
			if (StateSingleton.IsActive)
			{
				return;
			}

			int vw = Game1.VirtualWidth;
			int vh = Game1.VirtualHeight;

			// Ensure button entity exists and is positioned correctly
			EnsureButtonEntity(vw, vh);

			// Handle click
			var buttonEnt = EntityManager.GetEntity("Location_CustomizeButton");
			var buttonUI = buttonEnt?.GetComponent<UIElement>();
			if (buttonUI != null && buttonUI.IsClicked)
			{
				if (scene.Current == SceneId.Shop)
				{
					EventManager.Publish(new ShowTransition { Scene = SceneId.Location });
				}
				else
				{
					EventManager.Publish(new ShowTransition { Scene = SceneId.Customization });
				}
			}
		}

		public void Draw()
		{
			var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
			if (scene == null || (scene.Current != SceneId.Location && scene.Current != SceneId.Shop)) return;

			int vw = Game1.VirtualWidth;
			int vh = Game1.VirtualHeight;

			// Ensure button entity exists
			EnsureButtonEntity(vw, vh);

			var buttonEnt = EntityManager.GetEntity("Location_CustomizeButton");
			var buttonUI = buttonEnt?.GetComponent<UIElement>();
			if (buttonUI == null) return;

			var rect = buttonUI.Bounds;
			if (rect.Width <= 0 || rect.Height <= 0) return;

			// Draw background
			if (CornerRadius > 0)
			{
				var tex = _roundedRectCache;
				if (tex == null || tex.Width != rect.Width || tex.Height != rect.Height)
				{
					int radius = System.Math.Max(0, CornerRadius);
					tex = RoundedRectTextureFactory.CreateRoundedRect(_graphicsDevice, rect.Width, rect.Height, radius);
					_roundedRectCache = tex;
				}
				_spriteBatch.Draw(tex, rect, Color.Black);
			}
			else
			{
				_spriteBatch.Draw(_pixel, rect, Color.Black);
			}

			// Draw text
			string text = (scene.Current == SceneId.Shop) ? "Leave" : "Customize";
			var size = _font.MeasureString(text) * TextScale;
			var textRect = new Rectangle(rect.X + ButtonPadding, rect.Y + ButtonPadding, rect.Width - ButtonPadding * 2, rect.Height - ButtonPadding * 2);
			var pos = new Vector2(textRect.X + (textRect.Width - size.X) / 2f, textRect.Y + (textRect.Height - size.Y) / 2f);
			_spriteBatch.DrawString(_font, text, pos, Color.White, 0f, Vector2.Zero, TextScale, SpriteEffects.None, 0f);
		}

		private void EnsureButtonEntity(int viewportW, int viewportH)
		{
			int btnW = System.Math.Max(40, ButtonWidth);
			int btnH = System.Math.Max(24, ButtonHeight);
			int margin = System.Math.Max(0, ButtonMargin);
			var rect = new Rectangle(viewportW - btnW - margin, margin, btnW, btnH);
			var position = new Vector2(rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f);

			var ent = EntityManager.GetEntity("Location_CustomizeButton");
			if (ent == null)
			{
				ent = EntityManager.CreateEntity("Location_CustomizeButton");
				EntityManager.AddComponent(ent, new Transform { Position = position, BasePosition = position, ZOrder = 10000 });
				EntityManager.AddComponent(ent, new UIElement { Bounds = rect, IsInteractable = true });
				EntityManager.AddComponent(ent, new HotKey { Button = FaceButton.Y, Position = HotKeyPosition.Below, RequiresHold = true });
				var layer = ParallaxLayer.GetUIParallaxLayer();
				layer.AffectsUIBounds = true;
				EntityManager.AddComponent(ent, layer);
			}
			else
			{
				var transform = ent.GetComponent<Transform>();
				if (transform != null)
				{
					transform.Position = position;
					transform.BasePosition = position;
					transform.ZOrder = 10000;
				}
				else
				{
					EntityManager.AddComponent(ent, new Transform { Position = position, BasePosition = position, ZOrder = 10000 });
				}

				var ui = ent.GetComponent<UIElement>();
				if (ui == null)
				{
					EntityManager.AddComponent(ent, new UIElement { Bounds = rect, IsInteractable = true });
				}
				else
				{
					ui.Bounds = rect;
					ui.IsInteractable = true;
				}

				var hotKey = ent.GetComponent<HotKey>();
				if (hotKey == null)
				{
					EntityManager.AddComponent(ent, new HotKey { Button = FaceButton.Y, Position = HotKeyPosition.Below });
				}
			}
		}
	}
}
