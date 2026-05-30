using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Loadouts;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Singletons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("DeckV2 Dialog")]
	public class DeckV2InvalidDeckDialogSystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly SpriteFont _headingFont = FontSingleton.TitleFont;
		private readonly SpriteFont _contentFont = FontSingleton.ContentFont;
		private Texture2D _pixel;
		private bool _isDialogOpen;
		private int _pendingDeckCount;
		private Entity _overlayBlockerEntity;
		private Entity _exitButtonEntity;
		private Entity _cancelButtonEntity;

		[DebugEditable(DisplayName = "Dialog Width", Step = 4, Min = 200, Max = 800)]
		public int DialogWidth { get; set; } = 480;

		[DebugEditable(DisplayName = "Dialog Height", Step = 4, Min = 100, Max = 400)]
		public int DialogHeight { get; set; } = 200;

		[DebugEditable(DisplayName = "Dialog BG R", Step = 1, Min = 0, Max = 255)]
		public int DialogBgR { get; set; } = 26;

		[DebugEditable(DisplayName = "Dialog BG G", Step = 1, Min = 0, Max = 255)]
		public int DialogBgG { get; set; } = 26;

		[DebugEditable(DisplayName = "Dialog BG B", Step = 1, Min = 0, Max = 255)]
		public int DialogBgB { get; set; } = 26;

		[DebugEditable(DisplayName = "Title Scale", Step = 0.01f, Min = 0.05f, Max = 0.4f)]
		public float TitleScale { get; set; } = 0.16f;

		[DebugEditable(DisplayName = "Body Scale", Step = 0.01f, Min = 0.05f, Max = 0.3f)]
		public float BodyScale { get; set; } = 0.11f;

		[DebugEditable(DisplayName = "Button Width", Step = 4, Min = 60, Max = 200)]
		public int ButtonWidth { get; set; } = 120;

		[DebugEditable(DisplayName = "Button Height", Step = 2, Min = 24, Max = 60)]
		public int ButtonHeight { get; set; } = 40;

		[DebugEditable(DisplayName = "Button Text Scale", Step = 0.01f, Min = 0.05f, Max = 0.3f)]
		public float ButtonTextScale { get; set; } = 0.11f;

		[DebugEditable(DisplayName = "Button Gap", Step = 2, Min = 4, Max = 40)]
		public int ButtonGap { get; set; } = 16;

		public DeckV2InvalidDeckDialogSystem(EntityManager em, GraphicsDevice gd, SpriteBatch sb) : base(em)
		{
			_graphicsDevice = gd;
			_spriteBatch = sb;
		}

		public void ShowDialog(int deckCount)
		{
			_isDialogOpen = true;
			_pendingDeckCount = deckCount;
			CreateDialogEntities();
		}

		private void CreateDialogEntities()
		{
			int vw = Game1.VirtualWidth;
			int vh = Game1.VirtualHeight;

			// Fullscreen overlay blocker
			_overlayBlockerEntity = EntityManager.CreateEntity("DeckV2Dialog_Blocker");
			EntityManager.AddComponent(_overlayBlockerEntity, new Transform { Position = Vector2.Zero, ZOrder = 19000 });
			EntityManager.AddComponent(_overlayBlockerEntity, new UIElement
			{
				Bounds = new Rectangle(0, 0, vw, vh),
				IsInteractable = true,
				LayerType = UILayerType.Overlay,
				IsPreventDefaultClick = true,
				IsHidden = false
			});

			// Button positions
			int dx = (vw - DialogWidth) / 2;
			int dy = (vh - DialogHeight) / 2;
			int btnY = dy + DialogHeight - ButtonHeight - 20;
			int totalBtnW = ButtonWidth * 2 + ButtonGap;
			int btnStartX = dx + (DialogWidth - totalBtnW) / 2;

			// Exit Anyway button entity
			var exitRect = new Rectangle(btnStartX, btnY, ButtonWidth, ButtonHeight);
			_exitButtonEntity = EntityManager.CreateEntity("DeckV2Dialog_Exit");
			EntityManager.AddComponent(_exitButtonEntity, new Transform { Position = new Vector2(exitRect.X, exitRect.Y), ZOrder = 19001 });
			EntityManager.AddComponent(_exitButtonEntity, new UIElement
			{
				Bounds = exitRect,
				IsInteractable = true,
				LayerType = UILayerType.Overlay
			});
			EntityManager.AddComponent(_exitButtonEntity, new HotKey { Button = FaceButton.B, Position = HotKeyPosition.Below });

			// Cancel button entity
			var cancelRect = new Rectangle(btnStartX + ButtonWidth + ButtonGap, btnY, ButtonWidth, ButtonHeight);
			_cancelButtonEntity = EntityManager.CreateEntity("DeckV2Dialog_Cancel");
			EntityManager.AddComponent(_cancelButtonEntity, new Transform { Position = new Vector2(cancelRect.X, cancelRect.Y), ZOrder = 19001 });
			EntityManager.AddComponent(_cancelButtonEntity, new UIElement
			{
				Bounds = cancelRect,
				IsInteractable = true,
				LayerType = UILayerType.Overlay
			});
			EntityManager.AddComponent(_cancelButtonEntity, new HotKey { Button = FaceButton.X, Position = HotKeyPosition.Below });
		}

		private void DestroyDialogEntities()
		{
			if (_overlayBlockerEntity != null) { EntityManager.DestroyEntity(_overlayBlockerEntity.Id); _overlayBlockerEntity = null; }
			if (_exitButtonEntity != null) { EntityManager.DestroyEntity(_exitButtonEntity.Id); _exitButtonEntity = null; }
			if (_cancelButtonEntity != null) { EntityManager.DestroyEntity(_cancelButtonEntity.Id); _cancelButtonEntity = null; }
		}

		public bool IsDialogOpen => _isDialogOpen;

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<SceneState>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			if (!_isDialogOpen) return;

			// Check exit button click via UIElement
			if (_exitButtonEntity != null)
			{
				var ui = _exitButtonEntity.GetComponent<UIElement>();
				if (ui != null && ui.IsClicked)
				{
					_isDialogOpen = false;
					DestroyDialogEntities();
					EventManager.Publish(new ShowTransition { Scene = SceneId.Location, SkipHold = true });
					TimerScheduler.Schedule(0.8f, () =>
					{
						var st = EntityManager.GetEntitiesWithComponent<CustomizationV2DeckState>().FirstOrDefault();
						if (st != null) EntityManager.DestroyEntity(st.Id);
						var nav = EntityManager.GetEntitiesWithComponent<CustomizationV2NavigationState>().FirstOrDefault();
						if (nav != null) EntityManager.DestroyEntity(nav.Id);
						var loadout = EntityManager.GetEntitiesWithComponent<CustomizationV2LoadoutState>().FirstOrDefault();
						if (loadout != null) EntityManager.DestroyEntity(loadout.Id);
					});
				}
			}

			// Check cancel button click via UIElement
			if (_cancelButtonEntity != null)
			{
				var ui = _cancelButtonEntity.GetComponent<UIElement>();
				if (ui != null && ui.IsClicked)
				{
					_isDialogOpen = false;
					DestroyDialogEntities();
				}
			}
		}

		public void Draw()
		{
			if (!_isDialogOpen) return;

			if (_pixel == null)
			{
				_pixel = new Texture2D(_graphicsDevice, 1, 1);
				_pixel.SetData(new[] { Color.White });
			}

			int vw = Game1.VirtualWidth;
			int vh = Game1.VirtualHeight;

			// Dim overlay
			_spriteBatch.Draw(_pixel, new Rectangle(0, 0, vw, vh), new Color(0, 0, 0, 180));

			// Dialog box
			int dx = (vw - DialogWidth) / 2;
			int dy = (vh - DialogHeight) / 2;
			_spriteBatch.Draw(_pixel, new Rectangle(dx, dy, DialogWidth, DialogHeight), new Color(DialogBgR, DialogBgG, DialogBgB));

			// Border
			DrawRectBorder(new Rectangle(dx, dy, DialogWidth, DialogHeight), 2, new Color(160, 0, 0));

			// Title
			string title = "INVALID DECK";
			var titleSize = _headingFont.MeasureString(title) * TitleScale;
			float tx = dx + (DialogWidth - titleSize.X) / 2f;
			float ty = dy + 20;
			_spriteBatch.DrawString(_headingFont, title, new Vector2(tx, ty), new Color(196, 30, 58), 0f, Vector2.Zero, TitleScale, SpriteEffects.None, 0f);

			// Body
			string body = $"Your deck has {_pendingDeckCount} cards.\nExit anyway? Changes won't be saved.";
			var bodySize = _contentFont.MeasureString(body) * BodyScale;
			float bx = dx + (DialogWidth - bodySize.X) / 2f;
			float by = ty + titleSize.Y + 16;
			_spriteBatch.DrawString(_contentFont, body, new Vector2(bx, by), new Color(200, 200, 200), 0f, Vector2.Zero, BodyScale, SpriteEffects.None, 0f);

			// Buttons
			int btnY = dy + DialogHeight - ButtonHeight - 20;
			int totalBtnW = ButtonWidth * 2 + ButtonGap;
			int btnStartX = dx + (DialogWidth - totalBtnW) / 2;

			// Exit Anyway
			var exitRect = new Rectangle(btnStartX, btnY, ButtonWidth, ButtonHeight);
			_spriteBatch.Draw(_pixel, exitRect, new Color(160, 0, 0));
			DrawCenteredText("EXIT", exitRect, _headingFont, ButtonTextScale, Color.White);

			// Cancel
			var cancelRect = new Rectangle(btnStartX + ButtonWidth + ButtonGap, btnY, ButtonWidth, ButtonHeight);
			_spriteBatch.Draw(_pixel, cancelRect, new Color(42, 42, 42));
			DrawCenteredText("CANCEL", cancelRect, _headingFont, ButtonTextScale, Color.White);
		}

		private void DrawCenteredText(string text, Rectangle rect, SpriteFont font, float scale, Color color)
		{
			var size = font.MeasureString(text) * scale;
			float x = rect.X + (rect.Width - size.X) / 2f;
			float y = rect.Y + (rect.Height - size.Y) / 2f;
			_spriteBatch.DrawString(font, text, new Vector2(x, y), color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
		}

		private void DrawRectBorder(Rectangle rect, int thickness, Color color)
		{
			_spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
			_spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
			_spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
			_spriteBatch.Draw(_pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
		}
	}
}
