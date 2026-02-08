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
		private CursorStateEvent _cursorEvent;
		private Texture2D _pixel;
		private bool _isDialogOpen;
		private int _pendingDeckCount;

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
			EventManager.Subscribe<CursorStateEvent>(e => _cursorEvent = e);
		}

		public void ShowDialog(int deckCount)
		{
			_isDialogOpen = true;
			_pendingDeckCount = deckCount;
		}

		public bool IsDialogOpen => _isDialogOpen;

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<SceneState>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			if (!_isDialogOpen) return;
			if (_cursorEvent == null || !_cursorEvent.IsAPressedEdge) return;

			var click = new Point((int)Math.Round(_cursorEvent.Position.X), (int)Math.Round(_cursorEvent.Position.Y));
			int vw = Game1.VirtualWidth;
			int vh = Game1.VirtualHeight;
			int dx = (vw - DialogWidth) / 2;
			int dy = (vh - DialogHeight) / 2;

			// Exit Anyway button
			int btnY = dy + DialogHeight - ButtonHeight - 20;
			int totalBtnW = ButtonWidth * 2 + ButtonGap;
			int btnStartX = dx + (DialogWidth - totalBtnW) / 2;
			var exitRect = new Rectangle(btnStartX, btnY, ButtonWidth, ButtonHeight);
			var cancelRect = new Rectangle(btnStartX + ButtonWidth + ButtonGap, btnY, ButtonWidth, ButtonHeight);

			if (exitRect.Contains(click))
			{
				_isDialogOpen = false;
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
			else if (cancelRect.Contains(click))
			{
				_isDialogOpen = false;
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
			string body = $"Your deck has {_pendingDeckCount}/{DeckRules.RequiredDeckSize} cards.\nExit anyway? Changes won't be saved.";
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
