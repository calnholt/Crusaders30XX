using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Loadouts;
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

		private Entity _tabDeckEntity;
		private Entity _tabLoadoutEntity;
		private Entity _exitButtonEntity;

		public DeckV2InvalidDeckDialogSystem InvalidDeckDialogSystem { get; set; }

		[DebugEditable(DisplayName = "Header Height", Step = 2, Min = 32, Max = 100)]
		public int HeaderHeight { get; set; } = 56;

		[DebugEditable(DisplayName = "Tab Width", Step = 4, Min = 80, Max = 300)]
		public int TabWidth { get; set; } = 250;

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
			EventManager.Subscribe<LoadSceneEvent>(OnSceneLoad);
			EventManager.Subscribe<HotKeySelectEvent>(OnHotKeySelect);
		}

		private void OnSceneLoad(LoadSceneEvent evt)
		{
			if (evt.Scene != SceneId.CustomizationV2) return;
			EnsureTabEntities();
			EnsureExitEntity();
		}

		private void EnsureTabEntities()
		{
			if (_tabDeckEntity == null || EntityManager.GetEntity(_tabDeckEntity.Id) == null)
			{
				int tabX = TabLeftPad;
				var deckRect = new Rectangle(tabX, 0, TabWidth, HeaderHeight);
				_tabDeckEntity = EntityManager.CreateEntity("CV2_TabDeck");
				EntityManager.AddComponent(_tabDeckEntity, new Transform { Position = new Vector2(deckRect.X, deckRect.Y), ZOrder = 9000 });
				EntityManager.AddComponent(_tabDeckEntity, new UIElement { Bounds = deckRect, IsInteractable = true });
				EntityManager.AddComponent(_tabDeckEntity, new HotKey { Button = FaceButton.LB, Position = HotKeyPosition.Below });
			}

			if (_tabLoadoutEntity == null || EntityManager.GetEntity(_tabLoadoutEntity.Id) == null)
			{
				int tabX = TabLeftPad + TabWidth + TabGap;
				var loadoutRect = new Rectangle(tabX, 0, TabWidth, HeaderHeight);
				_tabLoadoutEntity = EntityManager.CreateEntity("CV2_TabLoadout");
				EntityManager.AddComponent(_tabLoadoutEntity, new Transform { Position = new Vector2(loadoutRect.X, loadoutRect.Y), ZOrder = 9000 });
				EntityManager.AddComponent(_tabLoadoutEntity, new UIElement { Bounds = loadoutRect, IsInteractable = true });
				EntityManager.AddComponent(_tabLoadoutEntity, new HotKey { Button = FaceButton.RB, Position = HotKeyPosition.Below });
			}
		}

		private void EnsureExitEntity()
		{
			if (_exitButtonEntity == null || EntityManager.GetEntity(_exitButtonEntity.Id) == null)
			{
				string exitText = "EXIT";
				var exitSize = _font.MeasureString(exitText) * ExitTextScale;
				int exitX = Game1.VirtualWidth - ExitRightPad - (int)exitSize.X;
				var exitRect = new Rectangle(exitX - 40, 0, (int)exitSize.X + 40, HeaderHeight);
				_exitButtonEntity = EntityManager.CreateEntity("CV2_ExitButton");
				EntityManager.AddComponent(_exitButtonEntity, new Transform { Position = new Vector2(exitRect.X, exitRect.Y), ZOrder = 9000 });
				EntityManager.AddComponent(_exitButtonEntity, new UIElement { Bounds = exitRect, IsInteractable = true });
				EntityManager.AddComponent(_exitButtonEntity, new HotKey { Button = FaceButton.B, Position = HotKeyPosition.Left });
			}
		}

		private void OnHotKeySelect(HotKeySelectEvent evt)
		{
			if (evt.Entity == _tabDeckEntity)
			{
				EventManager.Publish(new SwitchCustomizationV2Tab { Tab = CustomizationV2TabType.Deck });
			}
			else if (evt.Entity == _tabLoadoutEntity)
			{
				EventManager.Publish(new SwitchCustomizationV2Tab { Tab = CustomizationV2TabType.Loadout });
			}
			else if (evt.Entity == _exitButtonEntity)
			{
				HandleExitClick();
			}
		}

		private void HandleExitClick()
		{
			if (InvalidDeckDialogSystem != null && InvalidDeckDialogSystem.IsDialogOpen) return;

			var nav = EntityManager.GetEntitiesWithComponent<CustomizationV2NavigationState>().FirstOrDefault()?.GetComponent<CustomizationV2NavigationState>();
			if (nav == null) return;

			EventManager.Publish(new ShowTransition { Scene = SceneId.Location, SkipHold = true });
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
			if (InvalidDeckDialogSystem != null && InvalidDeckDialogSystem.IsDialogOpen) return;

			var nav = EntityManager.GetEntitiesWithComponent<CustomizationV2NavigationState>().FirstOrDefault()?.GetComponent<CustomizationV2NavigationState>();
			if (nav == null) return;

			// Update tab entity bounds in case layout properties changed
			UpdateTabEntityBounds();

			// Handle tab clicks via UIElement
			if (_tabDeckEntity != null)
			{
				var ui = _tabDeckEntity.GetComponent<UIElement>();
				if (ui != null && ui.IsClicked && nav.ActiveTab != CustomizationV2TabType.Deck)
				{
					EventManager.Publish(new SwitchCustomizationV2Tab { Tab = CustomizationV2TabType.Deck });
				}
			}
			if (_tabLoadoutEntity != null)
			{
				var ui = _tabLoadoutEntity.GetComponent<UIElement>();
				if (ui != null && ui.IsClicked && nav.ActiveTab != CustomizationV2TabType.Loadout)
				{
					EventManager.Publish(new SwitchCustomizationV2Tab { Tab = CustomizationV2TabType.Loadout });
				}
			}

			// Handle exit click via UIElement
			if (_exitButtonEntity != null)
			{
				var ui = _exitButtonEntity.GetComponent<UIElement>();
				if (ui != null && ui.IsClicked)
				{
					HandleExitClick();
				}
			}
		}

		private void UpdateTabEntityBounds()
		{
			if (_tabDeckEntity != null)
			{
				var ui = _tabDeckEntity.GetComponent<UIElement>();
				if (ui != null) ui.Bounds = new Rectangle(TabLeftPad, 0, TabWidth, HeaderHeight);
			}
			if (_tabLoadoutEntity != null)
			{
				var ui = _tabLoadoutEntity.GetComponent<UIElement>();
				if (ui != null) ui.Bounds = new Rectangle(TabLeftPad + TabWidth + TabGap, 0, TabWidth, HeaderHeight);
			}
			if (_exitButtonEntity != null)
			{
				var ui = _exitButtonEntity.GetComponent<UIElement>();
				if (ui != null)
				{
					string exitText = "EXIT";
					var exitSize = _font.MeasureString(exitText) * ExitTextScale;
					int exitX = Game1.VirtualWidth - ExitRightPad - (int)exitSize.X;
					ui.Bounds = new Rectangle(exitX - 40, 0, (int)exitSize.X + 40, HeaderHeight);
				}
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
			float exitTextX = vw - ExitRightPad - exitSize.X;
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
