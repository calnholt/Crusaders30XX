using System;
using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Singletons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("Quit Quest")]
	public class QuitCurrentQuestDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly SpriteFont _font;
		private readonly Texture2D _pixel;

		private enum OverlayState
		{
			Hidden,
			FadingIn,
			Visible,
			FadingOut
		}

		[DebugEditable(DisplayName = "Text Scale", Step = 0.05f, Min = 0.05f, Max = 3.0f)]
		public float TextScale { get; set; } = 0.22f;

		[DebugEditable(DisplayName = "Overlay Max Alpha (0-255)", Step = 1f, Min = 0f, Max = 255f)]
		public int OverlayMaxAlpha { get; set; } = 170;

		[DebugEditable(DisplayName = "Fade In (sec)", Step = 0.05f, Min = 0.05f, Max = 5f)]
		public float FadeInSec { get; set; } = 0.18f;

		[DebugEditable(DisplayName = "Fade Out (sec)", Step = 0.05f, Min = 0.05f, Max = 5f)]
		public float FadeOutSec { get; set; } = 0.16f;

		[DebugEditable(DisplayName = "Z-Order", Step = 100f, Min = 0f, Max = 50000f)]
		public int OverlayZ { get; set; } = 26000;

		private OverlayState _state = OverlayState.Hidden;
		private float _alpha01 = 0f; // 0..1

		// Input edge tracking
		private GamePadState _prevGamePad;
		private KeyboardState _prevKeyboard;

		// Entities
		private Entity _overlayTextEntity;
		private Entity _confirmParentEntity;

		// Cached last drawn text rect
		private Rectangle _textRect;

		public QuitCurrentQuestDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
			: base(entityManager)
		{
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
			_font = FontSingleton.TitleFont;
			_pixel = new Texture2D(graphicsDevice, 1, 1);
			_pixel.SetData(new[] { Color.White });
			_prevKeyboard = Keyboard.GetState();
      EventManager.Subscribe<DeleteCachesEvent>(OnDeleteCaches);
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			// Process globally; we manage our own state and entities
			return EntityManager.GetEntitiesWithComponent<SceneState>();
		}

		private void OnDeleteCaches(DeleteCachesEvent evt)
		{
			_overlayTextEntity = null;
			_confirmParentEntity = null;
      _state = OverlayState.Hidden;
      _alpha01 = 0f;
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			// Only active when window is active and not during transitions
			if (!Game1.WindowIsActive || StateSingleton.IsActive) return;

			// Restrict to Battle scene for now
			var scene = entity.GetComponent<SceneState>();
			if (scene == null || scene.Current != SceneId.Battle) return;

			var kb = Keyboard.GetState();
			var caps = GamePad.GetCapabilities(PlayerIndex.One);
			var gp = caps.IsConnected ? GamePad.GetState(PlayerIndex.One) : default;

			bool edgeEsc = kb.IsKeyDown(Keys.Escape) && !_prevKeyboard.IsKeyDown(Keys.Escape);
			bool edgeBackHw = caps.IsConnected && gp.Buttons.Back == ButtonState.Pressed && _prevGamePad.Buttons.Back == ButtonState.Released;

			// Determine if any other overlay UI is present (avoid opening over modals)
			bool anyOverlayUi = EntityManager.GetEntitiesWithComponent<UIElement>()
				.Any(ea => {
					var ui = ea.GetComponent<UIElement>();
					return ui != null
						&& ui.LayerType == UILayerType.Overlay
						&& ui.IsInteractable
						&& ui.Bounds.Width > 0
						&& ui.Bounds.Height > 0;
				});

			switch (_state)
			{
				case OverlayState.Hidden:
				{
					// Open via ESC or controller Back (hardware) if no overlay is already present
					if (!anyOverlayUi && (edgeEsc || edgeBackHw))
					{
						_state = OverlayState.FadingIn;
						_alpha01 = 0f;
						EnsureEntities();
						SyncEntitiesActive(true);
					}
					break;
				}
				case OverlayState.FadingIn:
				{
					float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
					_alpha01 += (FadeInSec > 0.001f) ? dt / Math.Max(0.001f, FadeInSec) : 1f;
					if (_alpha01 >= 1f)
					{
						_alpha01 = 1f;
						_state = OverlayState.Visible;
					}
					// Allow cancel during fade-in (Back only)
					if (edgeBackHw)
					{
						_state = OverlayState.FadingOut;
					}
					break;
				}
				case OverlayState.Visible:
				{
					// Cancel via hardware Back only
					if (edgeBackHw)
					{
						_state = OverlayState.FadingOut;
					}
					break;
				}
				case OverlayState.FadingOut:
				{
					float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
					_alpha01 -= (FadeOutSec > 0.001f) ? dt / Math.Max(0.001f, FadeOutSec) : 1f;
					if (_alpha01 <= 0f)
					{
						_alpha01 = 0f;
						_state = OverlayState.Hidden;
						SyncEntitiesActive(false);
					}
					break;
				}
			}

			_prevGamePad = gp;
			_prevKeyboard = kb;
		}

		private void EnsureEntities()
		{
			// Confirm parent (off-screen) -> triggers AbandonQuest transition
			if (_confirmParentEntity == null)
			{
				_confirmParentEntity = EntityManager.CreateEntity("QuitQuest_ConfirmParent");
				EntityManager.AddComponent(_confirmParentEntity, new Transform { Position = new Vector2(-10000, -10000), ZOrder = OverlayZ });
				EntityManager.AddComponent(_confirmParentEntity, new UIElement { Bounds = Rectangle.Empty, IsInteractable = false, LayerType = UILayerType.Overlay, EventType = UIElementEventType.AbandonQuest });
			}

			// Centered text entity with Start hotkey (parented to confirm)
			if (_overlayTextEntity == null)
			{
				_overlayTextEntity = EntityManager.CreateEntity("QuitQuest_Text");
				EntityManager.AddComponent(_overlayTextEntity, new Transform { Position = Vector2.Zero, ZOrder = OverlayZ });
				EntityManager.AddComponent(_overlayTextEntity, new UIElement { Bounds = Rectangle.Empty, IsInteractable = false, LayerType = UILayerType.Overlay, Tooltip = "" });
				EntityManager.AddComponent(_overlayTextEntity, new HotKey { Button = FaceButton.X, ParentEntity = _confirmParentEntity, Position = HotKeyPosition.Below, RequiresHold = true });
			}
		}

		private void SyncEntitiesActive(bool active)
		{
			// When inactive, hide hotkey hints by disabling interaction and zeroing bounds
			var uiText = _overlayTextEntity?.GetComponent<UIElement>();
			var uiParent = _confirmParentEntity?.GetComponent<UIElement>();
			if (uiText != null)
			{
				uiText.IsInteractable = active;
				uiText.Bounds = active ? _textRect : Rectangle.Empty;
			}
			if (uiParent != null)
			{
				// Keep parent off-screen; interaction handled through child hotkey parenting
				uiParent.IsInteractable = active;
			}
		}

		public void Draw()
		{
			if (_state == OverlayState.Hidden) return;

			int w = _graphicsDevice.Viewport.Width;
			int h = _graphicsDevice.Viewport.Height;

			// Fullscreen dim
			int alpha = Math.Max(0, Math.Min(255, (int)Math.Round(OverlayMaxAlpha * _alpha01)));
			_spriteBatch.Draw(_pixel, new Rectangle(0, 0, w, h), new Color(0, 0, 0, alpha));

			// Centered text
			string text = "Abandon quest?";
			var size = _font.MeasureString(text) * TextScale;
			var pos = new Vector2((w - size.X) / 2f, (h - size.Y) / 2f);
			_spriteBatch.DrawString(_font, text, pos, Color.White, 0f, Vector2.Zero, TextScale, SpriteEffects.None, 0f);

			// Update entity transforms and bounds so hotkey hints anchor to text
			_textRect = new Rectangle((int)Math.Round(pos.X), (int)Math.Round(pos.Y), (int)Math.Ceiling(size.X), (int)Math.Ceiling(size.Y));

			// Set hotkey to X for controller; avoid keyboard badge by switching to Start when no gamepad
			var caps = GamePad.GetCapabilities(PlayerIndex.One);
			bool gamepadConnected = caps.IsConnected;

			if (_overlayTextEntity != null)
			{
				var t = _overlayTextEntity.GetComponent<Transform>();
				var ui = _overlayTextEntity.GetComponent<UIElement>();
				var hk = _overlayTextEntity.GetComponent<HotKey>();
				if (t != null) t.ZOrder = OverlayZ;
				if (t != null) t.Position = new Vector2(_textRect.X, _textRect.Y);
				if (ui != null && _state != OverlayState.Hidden)
				{
					ui.Bounds = _textRect;
					ui.IsInteractable = true;
				}
				if (hk != null)
				{
					hk.Button = gamepadConnected ? FaceButton.X : FaceButton.Start;
				}
			}

			if (_confirmParentEntity != null)
			{
				var t = _confirmParentEntity.GetComponent<Transform>();
				var ui = _confirmParentEntity.GetComponent<UIElement>();
				if (t != null) t.ZOrder = OverlayZ;
				if (t != null) t.Position = new Vector2(-10000, -10000);
				if (ui != null && _state != OverlayState.Hidden)
				{
					ui.IsInteractable = true;
				}
			}
		}
	}
}

