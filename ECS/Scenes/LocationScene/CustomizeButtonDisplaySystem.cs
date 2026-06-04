using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
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
		private Texture2D _cachedTexture;
		private string _cachedText;

		[DebugEditable(DisplayName = "Button Margin", Step = 1, Min = 0, Max = 120)]
		public int ButtonMargin { get; set; } = 16;

		public CustomizeButtonDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch) : base(entityManager)
		{
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<SceneState>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			var scene = entity.GetComponent<SceneState>();
			if (scene == null || scene.Current != SceneId.Shop) return;

			// Regenerate texture when text changes
			if (_cachedTexture == null || _cachedText != "Leave")
			{
				_cachedTexture?.Dispose();
				_cachedTexture = ButtonTextureFactory.Create(_graphicsDevice, "Leave", Color.Black, Color.White);
				_cachedText = "Leave";
			}

			// Ensure button entity exists and is positioned correctly
			int vw = Game1.VirtualWidth;
			int vh = Game1.VirtualHeight;
			EnsureButtonEntity(vw, vh, UIElementEventType.LeaveShop);
		}

		public void Draw()
		{
			if (_cachedTexture == null) return;

			var buttonEnt = EntityManager.GetEntity("Location_CustomizeButton");
			var buttonUI = buttonEnt?.GetComponent<UIElement>();
			if (buttonUI == null) return;

			var rect = buttonUI.Bounds;
			if (rect.Width <= 0 || rect.Height <= 0) return;

			// Draw the texture centered within the UIElement bounds
			var texX = rect.X + (rect.Width - _cachedTexture.Width) / 2;
			var texY = rect.Y + (rect.Height - _cachedTexture.Height) / 2;
			_spriteBatch.Draw(_cachedTexture, new Rectangle(texX, texY, _cachedTexture.Width, _cachedTexture.Height), Color.White);
		}

		private void EnsureButtonEntity(int viewportW, int viewportH, UIElementEventType eventType)
		{
			int btnW = System.Math.Max(40, _cachedTexture.Width);
			int btnH = System.Math.Max(24, _cachedTexture.Height);
			int margin = System.Math.Max(0, ButtonMargin);
			var rect = new Rectangle(viewportW - btnW - margin, margin, btnW, btnH);
			var position = new Vector2(rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f);

			var ent = EntityManager.GetEntity("Location_CustomizeButton");
			if (ent == null)
			{
				ent = EntityManager.CreateEntity("Location_CustomizeButton");
				EntityManager.AddComponent(ent, new Transform { Position = position, ZOrder = 10000 });
				EntityManager.AddComponent(ent, new UIElement { Bounds = rect, IsInteractable = true, EventType = eventType });
				EntityManager.AddComponent(ent, new HotKey { Button = FaceButton.Y, Position = HotKeyPosition.Below, RequiresHold = true });
				EntityManager.AddComponent(ent, ParallaxLayer.GetUIParallaxLayer());
			}
			else
			{
				var transform = ent.GetComponent<Transform>();
				if (transform != null)
				{
					transform.Position = position;
					transform.ZOrder = 10000;
				}
				else
				{
					EntityManager.AddComponent(ent, new Transform { Position = position, ZOrder = 10000 });
				}

				var ui = ent.GetComponent<UIElement>();
				if (ui == null)
				{
					EntityManager.AddComponent(ent, new UIElement { Bounds = rect, IsInteractable = true, EventType = eventType });
				}
				else
				{
					ui.Bounds = rect;
					ui.IsInteractable = true;
					ui.EventType = eventType;
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
