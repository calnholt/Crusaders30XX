using System.Collections.Generic;
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
	[DebugTab("CV2 Header")]
	public class CustomizationV2HeaderSystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly SpriteFont _font = FontSingleton.TitleFont;
		private readonly SpriteFont _contentFont = FontSingleton.ContentFont;
		private Texture2D _pixel;
		private CursorStateEvent _cursorEvent;

		[DebugEditable(DisplayName = "Header Height", Step = 2, Min = 32, Max = 100)]
		public int HeaderHeight { get; set; } = 56;

		[DebugEditable(DisplayName = "Tab Width", Step = 4, Min = 80, Max = 300)]
		public int TabWidth { get; set; } = 160;

		[DebugEditable(DisplayName = "Tab Gap", Step = 1, Min = 0, Max = 20)]
		public int TabGap { get; set; } = 4;

		[DebugEditable(DisplayName = "Tab Skew %", Step = 1, Min = 0, Max = 20)]
		public float TabSkewPercent { get; set; } = 12f;

		[DebugEditable(DisplayName = "Tab Text Scale", Step = 0.01f, Min = 0.05f, Max = 0.5f)]
		public float TabTextScale { get; set; } = 0.16f;

		[DebugEditable(DisplayName = "Header BG R", Step = 1, Min = 0, Max = 255)]
		public int HeaderBgR { get; set; } = 26;

		[DebugEditable(DisplayName = "Header BG G", Step = 1, Min = 0, Max = 255)]
		public int HeaderBgG { get; set; } = 26;

		[DebugEditable(DisplayName = "Header BG B", Step = 1, Min = 0, Max = 255)]
		public int HeaderBgB { get; set; } = 26;

		[DebugEditable(DisplayName = "Active Tab R", Step = 1, Min = 0, Max = 255)]
		public int ActiveTabR { get; set; } = 160;

		[DebugEditable(DisplayName = "Active Tab G", Step = 1, Min = 0, Max = 255)]
		public int ActiveTabG { get; set; } = 0;

		[DebugEditable(DisplayName = "Active Tab B", Step = 1, Min = 0, Max = 255)]
		public int ActiveTabB { get; set; } = 0;

		[DebugEditable(DisplayName = "Inactive Tab R", Step = 1, Min = 0, Max = 255)]
		public int InactiveTabR { get; set; } = 42;

		[DebugEditable(DisplayName = "Inactive Tab G", Step = 1, Min = 0, Max = 255)]
		public int InactiveTabG { get; set; } = 42;

		[DebugEditable(DisplayName = "Inactive Tab B", Step = 1, Min = 0, Max = 255)]
		public int InactiveTabB { get; set; } = 42;

		[DebugEditable(DisplayName = "Border Accent R", Step = 1, Min = 0, Max = 255)]
		public int BorderR { get; set; } = 160;

		[DebugEditable(DisplayName = "Border Accent G", Step = 1, Min = 0, Max = 255)]
		public int BorderG { get; set; } = 0;

		[DebugEditable(DisplayName = "Border Accent B", Step = 1, Min = 0, Max = 255)]
		public int BorderB { get; set; } = 0;

		[DebugEditable(DisplayName = "Border Height", Step = 1, Min = 0, Max = 6)]
		public int BorderHeight { get; set; } = 2;

		[DebugEditable(DisplayName = "Exit Text Scale", Step = 0.01f, Min = 0.05f, Max = 0.5f)]
		public float ExitTextScale { get; set; } = 0.15f;

		[DebugEditable(DisplayName = "Tab Left Pad", Step = 2, Min = 0, Max = 100)]
		public int TabLeftPad { get; set; } = 20;

		[DebugEditable(DisplayName = "Exit Right Pad", Step = 2, Min = 0, Max = 100)]
		public int ExitRightPad { get; set; } = 24;

		[DebugEditable(DisplayName = "Key Hint Size", Step = 1, Min = 16, Max = 40)]
		public int KeyHintSize { get; set; } = 24;

		public CustomizationV2HeaderSystem(EntityManager em, GraphicsDevice gd, SpriteBatch sb) : base(em)
		{
			_graphicsDevice = gd;
			_spriteBatch = sb;
			EventManager.Subscribe<CursorStateEvent>(e => _cursorEvent = e);
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<SceneState>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			var scene = entity.GetComponent<SceneState>();
			if (scene == null || scene.Current != SceneId.CustomizationV2) return;
			if (StateSingleton.IsActive) return;

			var nav = EntityManager.GetEntitiesWithComponent<CustomizationV2NavigationState>().FirstOrDefault()?.GetComponent<CustomizationV2NavigationState>();
			if (nav == null) return;

			if (_cursorEvent == null || !_cursorEvent.IsAPressedEdge) return;
			var click = new Point((int)System.Math.Round(_cursorEvent.Position.X), (int)System.Math.Round(_cursorEvent.Position.Y));

			// Check tab clicks
			int tabX = TabLeftPad;
			var tabs = new[] { CustomizationV2TabType.Deck, CustomizationV2TabType.Loadout };
			foreach (var tab in tabs)
			{
				var tabRect = new Rectangle(tabX, 0, TabWidth, HeaderHeight);
				if (tabRect.Contains(click) && nav.ActiveTab != tab)
				{
					EventManager.Publish(new SwitchCustomizationV2Tab { Tab = tab });
				}
				tabX += TabWidth + TabGap;
			}

			// Check exit click
			string exitText = "EXIT";
			var exitSize = _font.MeasureString(exitText) * ExitTextScale;
			int exitX = Game1.VirtualWidth - ExitRightPad - (int)exitSize.X - KeyHintSize - 10;
			var exitRect = new Rectangle(exitX, 0, (int)exitSize.X + KeyHintSize + 10, HeaderHeight);
			if (exitRect.Contains(click))
			{
				EventManager.Publish(new ShowTransition { Scene = SceneId.Location, SkipHold = true });
			}
		}

		public void Draw()
		{
			var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
			if (scene == null || scene.Current != SceneId.CustomizationV2) return;

			var nav = EntityManager.GetEntitiesWithComponent<CustomizationV2NavigationState>().FirstOrDefault()?.GetComponent<CustomizationV2NavigationState>();
			if (nav == null) return;

			if (_pixel == null)
			{
				_pixel = new Texture2D(_graphicsDevice, 1, 1);
				_pixel.SetData(new[] { Color.White });
			}

			int vw = Game1.VirtualWidth;

			// Header background
			var headerBg = new Color(HeaderBgR, HeaderBgG, HeaderBgB);
			_spriteBatch.Draw(_pixel, new Rectangle(0, 0, vw, HeaderHeight), headerBg);

			// Bottom border accent
			var borderColor = new Color(BorderR, BorderG, BorderB);
			_spriteBatch.Draw(_pixel, new Rectangle(0, HeaderHeight - BorderHeight, vw, BorderHeight), borderColor);

			// Draw tabs
			var activeColor = new Color(ActiveTabR, ActiveTabG, ActiveTabB);
			var inactiveColor = new Color(InactiveTabR, InactiveTabG, InactiveTabB);
			var tabs = new[] { ("DECK", CustomizationV2TabType.Deck), ("LOADOUT", CustomizationV2TabType.Loadout) };
			int tabX = TabLeftPad;

			foreach (var (label, tabType) in tabs)
			{
				bool isActive = nav.ActiveTab == tabType;
				var tabColor = isActive ? activeColor : inactiveColor;
				var mask = PrimitiveTextureFactory.GetParallelogramMask(_graphicsDevice, TabWidth, HeaderHeight, TabSkewPercent);
				_spriteBatch.Draw(mask, new Rectangle(tabX, 0, TabWidth, HeaderHeight), tabColor);

				var textSize = _font.MeasureString(label) * TabTextScale;
				float textX = tabX + (TabWidth - textSize.X) / 2f;
				float textY = (HeaderHeight - textSize.Y) / 2f;
				var textColor = isActive ? Color.White : new Color(224, 224, 224);
				_spriteBatch.DrawString(_font, label, new Vector2(textX, textY), textColor, 0f, Vector2.Zero, TabTextScale, SpriteEffects.None, 0f);

				tabX += TabWidth + TabGap;
			}

			// Draw EXIT button on the right
			DrawExitButton(vw);
		}

		private void DrawExitButton(int vw)
		{
			string exitText = "EXIT";
			var exitSize = _font.MeasureString(exitText) * ExitTextScale;

			// Key hint badge
			int hintX = vw - ExitRightPad - KeyHintSize;
			int hintY = (HeaderHeight - KeyHintSize) / 2;
			var hintRect = new Rectangle(hintX, hintY, KeyHintSize, KeyHintSize);
			_spriteBatch.Draw(_pixel, hintRect, new Color(10, 10, 10));

			// Hint border
			DrawRectBorder(hintRect, 1, new Color(51, 51, 51));

			// ESC text in hint
			string escText = "ESC";
			var escSize = _contentFont.MeasureString(escText) * 0.08f;
			float escX = hintRect.X + (hintRect.Width - escSize.X) / 2f;
			float escY = hintRect.Y + (hintRect.Height - escSize.Y) / 2f;
			_spriteBatch.DrawString(_contentFont, escText, new Vector2(escX, escY), new Color(102, 102, 102), 0f, Vector2.Zero, 0.08f, SpriteEffects.None, 0f);

			// EXIT text
			float exitTextX = hintX - 10 - exitSize.X;
			float exitTextY = (HeaderHeight - exitSize.Y) / 2f;
			_spriteBatch.DrawString(_font, exitText, new Vector2(exitTextX, exitTextY), new Color(224, 224, 224), 0f, Vector2.Zero, ExitTextScale, SpriteEffects.None, 0f);
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
