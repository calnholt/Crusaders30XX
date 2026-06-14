using System;
using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Singletons;
using Crusaders30XX.ECS.Services;
using Crusaders30XX.ECS.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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

		// Entities
		private Entity _overlayTextEntity;
		private Entity _confirmParentEntity;
		private Entity _overlayBlockerEntity;

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
			EventManager.Subscribe<DeleteCachesEvent>(OnDeleteCaches);
			EventManager.Subscribe<RunEndSequenceRequested>(_ => DismissOverlay());
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
			_overlayBlockerEntity = null;
			DismissOverlay();
		}

		private void DismissOverlay()
		{
			_state = OverlayState.Hidden;
			_alpha01 = 0f;
			SyncEntitiesActive(false);
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			// Only active when window is active and not during transitions
			if (!Game1.WindowIsActive || StateSingleton.IsActive) return;

			// Restrict to Battle scene for now
			var scene = entity.GetComponent<SceneState>();
			if (scene == null || scene.Current != SceneId.Battle) return;
			if (GuidedTutorialService.IsActive(EntityManager))
			{
				DismissOverlay();
				return;
			}

			PlayerInputFrame input = PlayerInputService.GetFrame(EntityManager);
			bool edgeEsc = input.WasPressed(PlayerButton.Escape);
			bool edgeBackHw = input.WasPressed(PlayerButton.Back);

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
					// Allow cancel during fade-in (ESC or Back)
					if (edgeEsc || edgeBackHw)
					{
						_state = OverlayState.FadingOut;
					}
					break;
				}
				case OverlayState.Visible:
				{
					// Cancel via ESC or hardware Back
					if (edgeEsc || edgeBackHw)
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

			if (_state != OverlayState.Hidden)
			{
				UpdateEntityLayout();
				SyncEntitiesActive(true);
			}
		}

		private void EnsureEntities()
		{
			// Confirm parent (off-screen) -> triggers AbandonQuest transition
			if (_confirmParentEntity == null)
			{
				_confirmParentEntity = EntityManager.CreateEntity("QuitQuest_ConfirmParent");
				EntityManager.AddComponent(_confirmParentEntity, new Transform { Position = new Vector2(-10000, -10000), ZOrder = OverlayZ });
				EntityManager.AddComponent(_confirmParentEntity, new UIElement { Bounds = Rectangle.Empty, IsInteractable = false, LayerType = UILayerType.Overlay, EventType = UIElementEventType.AbandonQuest });
				InputContextService.EnsureMember(
					EntityManager,
					_confirmParentEntity,
					"overlay.quit-quest");
			}

			// Centered text entity with Start hotkey (parented to confirm)
			if (_overlayTextEntity == null)
			{
				_overlayTextEntity = EntityManager.CreateEntity("QuitQuest_Text");
				EntityManager.AddComponent(_overlayTextEntity, new Transform { Position = Vector2.Zero, ZOrder = OverlayZ });
				EntityManager.AddComponent(_overlayTextEntity, new UIElement { Bounds = Rectangle.Empty, IsInteractable = false, LayerType = UILayerType.Overlay, Tooltip = "" });
				EntityManager.AddComponent(_overlayTextEntity, new HotKey { Button = FaceButton.X, ParentEntity = _confirmParentEntity, Position = HotKeyPosition.Below, RequiresHold = true });
				InputContextService.EnsureMember(
					EntityManager,
					_overlayTextEntity,
					"overlay.quit-quest");
			}

			// Fullscreen blocker that swallows clicks behind the overlay
			if (_overlayBlockerEntity == null)
			{
				_overlayBlockerEntity = EntityManager.CreateEntity("QuitQuest_OverlayBlocker");
				EntityManager.AddComponent(_overlayBlockerEntity, new Transform { Position = Vector2.Zero, ZOrder = OverlayZ - 1 });
				EntityManager.AddComponent(_overlayBlockerEntity, new UIElement
				{
					Bounds = Rectangle.Empty,
					IsInteractable = false,
					LayerType = UILayerType.Overlay,
					EventType = UIElementEventType.None,
					IsPreventDefaultClick = true,
					IsHidden = false,
				});
				InputContextService.EnsureContext(
					EntityManager,
					_overlayBlockerEntity,
					"overlay.quit-quest",
					760,
					true);
			}
		}

		private void SyncEntitiesActive(bool active)
		{
			// When inactive, hide hotkey hints by disabling interaction and zeroing bounds
			var uiText = _overlayTextEntity?.GetComponent<UIElement>();
			var uiParent = _confirmParentEntity?.GetComponent<UIElement>();
			var uiBlocker = _overlayBlockerEntity?.GetComponent<UIElement>();
			var tBlocker = _overlayBlockerEntity?.GetComponent<Transform>();
			var context = _overlayBlockerEntity?.GetComponent<InputContext>();
			if (context != null) context.IsActive = active;
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

			// Block all clicks behind the quit overlay while it is active
			if (uiBlocker != null && tBlocker != null)
			{
				tBlocker.ZOrder = OverlayZ - 1;
				if (active)
				{
					uiBlocker.IsInteractable = true;
					uiBlocker.IsHidden = false;
					uiBlocker.Bounds = new Rectangle(0, 0, Game1.VirtualWidth, Game1.VirtualHeight);
				}
				else
				{
					uiBlocker.IsInteractable = false;
					uiBlocker.Bounds = Rectangle.Empty;
					uiBlocker.IsHidden = true;
				}
			}
		}

		public void Draw()
		{
			if (_state == OverlayState.Hidden) return;

			int w = Game1.VirtualWidth;
			int h = Game1.VirtualHeight;

			// Fullscreen dim
			int alpha = Math.Max(0, Math.Min(255, (int)Math.Round(OverlayMaxAlpha * _alpha01)));
			_spriteBatch.Draw(_pixel, new Rectangle(0, 0, w, h), new Color(0, 0, 0, alpha));

			// Centered text
			string text = "Abandon run?";
			var size = _font.MeasureString(text) * TextScale;
			var pos = new Vector2((w - size.X) / 2f, (h - size.Y) / 2f);
			_spriteBatch.DrawString(_font, text, pos, Color.White, 0f, Vector2.Zero, TextScale, SpriteEffects.None, 0f);
		}

		private void UpdateEntityLayout()
		{
			string text = "Abandon run?";
			var size = _font.MeasureString(text) * TextScale;
			var pos = new Vector2(
				(Game1.VirtualWidth - size.X) / 2f,
				(Game1.VirtualHeight - size.Y) / 2f);
			_textRect = new Rectangle((int)Math.Round(pos.X), (int)Math.Round(pos.Y), (int)Math.Ceiling(size.X), (int)Math.Ceiling(size.Y));

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
					hk.Button = FaceButton.X;
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

			// Keep blocker Z-order in sync with debug-edited OverlayZ
			if (_overlayBlockerEntity != null)
			{
				var t = _overlayBlockerEntity.GetComponent<Transform>();
				if (t != null) t.ZOrder = OverlayZ - 1;
			}
		}
	}
}
