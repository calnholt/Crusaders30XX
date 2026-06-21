using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Services;
using Crusaders30XX.ECS.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Singletons;
using Crusaders30XX.ECS.Rendering;

namespace Crusaders30XX.ECS.Systems
{
	internal class TutorialRetryDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly SpriteFont _font = FontSingleton.ContentFont;
		private Texture2D _cachedButtonTexture;
		private string _cachedButtonText;
		private float _holdElapsed;
		private bool _holding;

		[DebugEditable(DisplayName = "Button Width", Step = 1, Min = 40, Max = 400)]
		public int ButtonWidth { get; set; } = 140;

		[DebugEditable(DisplayName = "Button Height", Step = 1, Min = 20, Max = 120)]
		public int ButtonHeight { get; set; } = 48;

		[DebugEditable(DisplayName = "Button X", Step = 5, Min = 0, Max = 2000)]
		public int ButtonX { get; set; } = 16;

		[DebugEditable(DisplayName = "Button Y", Step = 5, Min = 0, Max = 2000)]
		public int ButtonY { get; set; } = 16;

		[DebugEditable(DisplayName = "Button Z", Step = 100, Min = 0, Max = 20000)]
		public int ButtonZ { get; set; } = 5000;

		[DebugEditable(DisplayName = "Text Scale", Step = 0.05f, Min = 0.1f, Max = 2.5f)]
		public float ButtonTextScale { get; set; } = 0.15f;

		[DebugEditable(DisplayName = "Hold Duration (sec)", Step = 0.05f, Min = 0.1f, Max = 5f)]
		public float HoldDuration { get; set; } = 0.75f;

		public TutorialRetryDisplaySystem(EntityManager entityManager, GraphicsDevice gd, SpriteBatch sb) : base(entityManager)
		{
			_graphicsDevice = gd;
			_spriteBatch = sb;
			EventManager.Subscribe<LoadSceneEvent>(_ => DestroyButton());
		}

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);
			if (!GuidedTutorialService.IsActive(EntityManager)) return;
			if (!Game1.WindowIsActive || StateSingleton.IsActive) return;

			EnsureButton();

			// Manual hold for Escape/Back (bypasses all input gating)
			PlayerInputFrame frame = PlayerInputService.GetFrame(EntityManager);
			bool escDown = frame.IsDown(PlayerButton.Escape);
			bool backDown = frame.IsDown(PlayerButton.Back);

			if (escDown || backDown)
			{
				if (!_holding)
				{
					_holding = true;
					_holdElapsed = 0f;
				}
				_holdElapsed += (float)gameTime.ElapsedGameTime.TotalSeconds;
				if (_holdElapsed >= HoldDuration)
				{
					_holding = false;
					_holdElapsed = 0f;
					GuidedTutorialService.RestartSection(EntityManager);
				}
			}
			else
			{
				_holding = false;
				_holdElapsed = 0f;
			}

			// IsClicked for mouse clicks (via UIInteractionSystem) and HotKey hold (via HotKeySystem)
			var retryBtn = EntityManager.GetEntity("UIButton_TutorialRetry");
			if (retryBtn?.GetComponent<UIElement>()?.IsClicked == true)
			{
				retryBtn.GetComponent<UIElement>().IsClicked = false;
				GuidedTutorialService.RestartSection(EntityManager);
			}
		}

		public void Draw()
		{
			if (!GuidedTutorialService.IsActive(EntityManager))
			{
				DestroyButton();
				return;
			}

			EnsureButton();

			string label = "Retry";
			if (_cachedButtonTexture == null || _cachedButtonText != label)
			{
				_cachedButtonTexture?.Dispose();
				_cachedButtonTexture = ButtonTextureFactory.Create(
					_graphicsDevice, label, Color.White, Color.DarkRed);
				_cachedButtonText = label;
			}

			var drawRect = GetButtonRect();
			_spriteBatch.Draw(_cachedButtonTexture,
				new Rectangle(drawRect.X, drawRect.Y, drawRect.Width, drawRect.Height),
				Color.White);
		}

		private void EnsureButton()
		{
			var btnRect = GetButtonRect();
			var retryBtn = EntityManager.GetEntity("UIButton_TutorialRetry");
			if (retryBtn == null)
			{
				retryBtn = EntityManager.CreateEntity("UIButton_TutorialRetry");
				EntityManager.AddComponent(retryBtn, new Transform { Position = new Vector2(btnRect.X, btnRect.Y), ZOrder = ButtonZ });
				EntityManager.AddComponent(retryBtn, new UIElement { Bounds = btnRect, IsInteractable = true, IsHidden = false });
				EntityManager.AddComponent(retryBtn, new HotKey { Button = FaceButton.Back, RequiresHold = true, Position = HotKeyPosition.Below });
				EntityManager.AddComponent(retryBtn, new TutorialInteractionPermitted());
			}
			else
			{
				var ui = retryBtn.GetComponent<UIElement>();
				if (ui != null) ui.Bounds = btnRect;

				var t = retryBtn.GetComponent<Transform>();
				if (t != null) t.Position = new Vector2(btnRect.X, btnRect.Y);
			}

			// Sync context between "gameplay" and "overlay.tutorial" based on tutorial overlay state
			bool tutorialOverlayActive = EntityManager.GetEntitiesWithComponent<InputContext>()
				.Any(e =>
				{
					var c = e.GetComponent<InputContext>();
					return c != null && c.Id == "overlay.tutorial" && c.IsActive;
				});
			string targetContext = tutorialOverlayActive ? "overlay.tutorial" : InputContextIds.Gameplay;
			var member = retryBtn.GetComponent<InputContextMember>();
			if (member == null || member.ContextId != targetContext)
			{
				InputContextService.EnsureMember(EntityManager, retryBtn, targetContext);
			}
		}

		private Rectangle GetButtonRect()
		{
			return new Rectangle(ButtonX, ButtonY, ButtonWidth, ButtonHeight);
		}

		private void DestroyButton()
		{
			var btn = EntityManager.GetEntity("UIButton_TutorialRetry");
			if (btn != null)
				EntityManager.DestroyEntity(btn.Id);
			_cachedButtonTexture?.Dispose();
			_cachedButtonTexture = null;
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities() => System.Array.Empty<Entity>();
		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }
	}
}
