using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Achievements;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Rendering;
using Crusaders30XX.ECS.Singletons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Displays the Achievement button in the Location scene (top-right corner, left of Customize button).
	/// </summary>
	[DebugTab("Achievement Button")]
	public class AchievementButtonDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly SpriteFont _font = FontSingleton.ContentFont;
		private Texture2D _cachedTexture;
		private string _cachedText;
		private Texture2D _badgeCircleCache;
		private float _pulseTimer;

		[DebugEditable(DisplayName = "Button Margin", Step = 1, Min = 0, Max = 120)]
		public int ButtonMargin { get; set; } = 16;

		[DebugEditable(DisplayName = "Badge Size", Step = 2, Min = 8, Max = 128)]
		public int BadgeSize { get; set; } = 40;

		[DebugEditable(DisplayName = "Badge Excl Scale", Step = 0.1f, Min = 0.1f, Max = 10.0f)]
		public float BadgeExclamationScale { get; set; } = 1.1f;

		[DebugEditable(DisplayName = "Badge Pulse Speed", Step = 0.1f, Min = 0.0f, Max = 10.0f)]
		public float BadgePulseSpeed { get; set; } = 2.0f;

		[DebugEditable(DisplayName = "Badge Pulse Intensity", Step = 0.01f, Min = 0.0f, Max = 1.0f)]
		public float BadgePulseIntensity { get; set; } = 0.1f;

		[DebugEditable(DisplayName = "Badge Offset X", Step = 1, Min = -50, Max = 50)]
		public int BadgeOffsetX { get; set; } = 2;

		[DebugEditable(DisplayName = "Badge Offset Y", Step = 1, Min = -50, Max = 50)]
		public int BadgeOffsetY { get; set; } = -50;

		public AchievementButtonDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch) : base(entityManager)
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
			if (scene == null || scene.Current != SceneId.Location) return;

			_pulseTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

			// Regenerate texture when text changes
			string text = "Achievements";
			if (_cachedTexture == null || _cachedText != text)
			{
				_cachedTexture?.Dispose();
				_cachedTexture = ButtonTextureFactory.Create(_graphicsDevice, text, Color.Black, Color.White);
				_cachedText = text;
			}

			// Ensure button entity exists and is positioned correctly
			int vw = Game1.VirtualWidth;
			int vh = Game1.VirtualHeight;
			EnsureButtonEntity(vw, vh);

			// Block interactions during scene transition
			if (StateSingleton.IsActive)
			{
				return;
			}

			// Handle click
			var buttonEnt = EntityManager.GetEntity("Location_AchievementButton");
			var buttonUI = buttonEnt?.GetComponent<UIElement>();
			if (buttonUI != null && buttonUI.IsClicked)
			{
				EventManager.Publish(new ShowTransition { Scene = SceneId.Achievement });
			}
		}

		public void Draw()
		{
			if (_cachedTexture == null) return;

			var buttonEnt = EntityManager.GetEntity("Location_AchievementButton");
			var buttonUI = buttonEnt?.GetComponent<UIElement>();
			if (buttonUI == null) return;

			var rect = buttonUI.Bounds;
			if (rect.Width <= 0 || rect.Height <= 0) return;

			// Draw the texture centered within the UIElement bounds
			var texX = rect.X + (rect.Width - _cachedTexture.Width) / 2;
			var texY = rect.Y + (rect.Height - _cachedTexture.Height) / 2;
			_spriteBatch.Draw(_cachedTexture, new Rectangle(texX, texY, _cachedTexture.Width, _cachedTexture.Height), Color.White);

			// Draw notification badge if there are unseen achievements
			if (AchievementManager.GetUnseenCount() > 0)
			{
				int badgeSize = System.Math.Max(8, BadgeSize);
				int radius = badgeSize / 2;
				if (_badgeCircleCache == null || _badgeCircleCache.Width != badgeSize)
				{
					_badgeCircleCache = RoundedRectTextureFactory.CreateRoundedRect(_graphicsDevice, badgeSize, badgeSize, radius);
				}

				// Pulse animation
				float pulse = (float)System.Math.Sin(_pulseTimer * BadgePulseSpeed) * BadgePulseIntensity + 1.0f;

				// Position at bottom-right of the button
				Vector2 badgeCenter = new Vector2(rect.Right + BadgeOffsetX, rect.Bottom + BadgeOffsetY);
				Vector2 origin = new Vector2(radius);

				// Draw dark red circle
				_spriteBatch.Draw(_badgeCircleCache, badgeCenter, null, Color.DarkRed, 0f, origin, pulse, SpriteEffects.None, 0f);

				// Draw white '!'
				string exclamation = "!";
				float exclamationScale = 0.2f * BadgeExclamationScale;
				Vector2 exclSize = _font.MeasureString(exclamation);
				Vector2 exclOrigin = exclSize / 2f;

				_spriteBatch.DrawString(_font, exclamation, badgeCenter, Color.White, 0f, exclOrigin, exclamationScale * pulse, SpriteEffects.None, 0f);
			}
		}

		private void EnsureButtonEntity(int viewportW, int viewportH)
		{
			int btnW = System.Math.Max(40, _cachedTexture.Width);
			int btnH = System.Math.Max(24, _cachedTexture.Height);
			int margin = System.Math.Max(0, ButtonMargin);
			// Position to the left of the Customize button (top-right)
			var rect = new Rectangle(viewportW - btnW * 2 - margin * 2, margin, btnW, btnH);
			var position = new Vector2(rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f);

			var ent = EntityManager.GetEntity("Location_AchievementButton");
			if (ent == null)
			{
				ent = EntityManager.CreateEntity("Location_AchievementButton");
				EntityManager.AddComponent(ent, new Transform { Position = position, BasePosition = position, ZOrder = 10000 });
				EntityManager.AddComponent(ent, new UIElement { Bounds = rect, IsInteractable = true });
				EntityManager.AddComponent(ent, new HotKey { Button = FaceButton.B, Position = HotKeyPosition.Below, RequiresHold = true });
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
					EntityManager.AddComponent(ent, new HotKey { Button = FaceButton.B, Position = HotKeyPosition.Below });
				}
				else
				{
					hotKey.Button = FaceButton.B;
				}
			}
		}
	}
}
