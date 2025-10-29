using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("Quest Reward Modal")] 
	public class QuestRewardModalDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly SpriteFont _font;
		private readonly Texture2D _pixel;

		[DebugEditable(DisplayName = "Z Order", Step = 10, Min = 0, Max = 100000)]
		public int ZOrder { get; set; } = 52000;

		[DebugEditable(DisplayName = "Panel Width", Step = 10, Min = 100, Max = 1600)]
		public int PanelWidth { get; set; } = 720;
		[DebugEditable(DisplayName = "Panel Height", Step = 10, Min = 80, Max = 1200)]
		public int PanelHeight { get; set; } = 280;
		[DebugEditable(DisplayName = "Panel Alpha", Step = 5, Min = 0, Max = 255)]
		public int PanelAlpha { get; set; } = 200;
		[DebugEditable(DisplayName = "Text Scale", Step = 0.05f, Min = 0.1f, Max = 2f)]
		public float TextScale { get; set; } = 0.4f;

		[DebugEditable(DisplayName = "Button Width", Step = 5, Min = 60, Max = 800)]
		public int ButtonWidth { get; set; } = 220;
		[DebugEditable(DisplayName = "Button Height", Step = 5, Min = 30, Max = 300)]
		public int ButtonHeight { get; set; } = 64;
		[DebugEditable(DisplayName = "Button Text Scale", Step = 0.05f, Min = 0.1f, Max = 2f)]
		public float ButtonTextScale { get; set; } = 0.35f;

		public QuestRewardModalDisplaySystem(EntityManager entityManager, GraphicsDevice gd, SpriteBatch sb, SpriteFont font) : base(entityManager)
		{
			_graphicsDevice = gd;
			_spriteBatch = sb;
			_font = font;
			_pixel = new Texture2D(gd, 1, 1);
			_pixel.SetData(new[] { Color.White });
			EventManager.Subscribe<ShowQuestRewardOverlay>(_ => Open());
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<SceneState>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			EnsureOverlayEntity();
			var overlayEntity = EntityManager.GetEntity("QuestRewardOverlay");
			var ui = overlayEntity?.GetComponent<UIElement>();
			var state = overlayEntity?.GetComponent<QuestRewardOverlayState>();
			if (ui == null || state == null) return;

			ui.IsInteractable = state.IsOpen;
			ui.Bounds = state.IsOpen
				? new Rectangle(0, 0, _graphicsDevice.Viewport.Width, _graphicsDevice.Viewport.Height)
				: new Rectangle(0, 0, 0, 0);

			var btn = EnsureProceedButton();
			var btnUi = btn?.GetComponent<UIElement>();
			if (btnUi != null)
			{
				btnUi.IsInteractable = state.IsOpen;
				if (state.IsOpen && btnUi.IsClicked)
				{
					btnUi.IsClicked = false;
					state.IsOpen = false;
					EventManager.Publish(new ShowTransition { Scene = SceneId.WorldMap });
				}
			}
		}

		public void Open()
		{
			EnsureOverlayEntity();
			var e = EntityManager.GetEntity("QuestRewardOverlay");
			var st = e.GetComponent<QuestRewardOverlayState>();
			st.IsOpen = true;
		}

		public void Draw()
		{
			if (_font == null) return;
			var e = EntityManager.GetEntity("QuestRewardOverlay");
			var st = e?.GetComponent<QuestRewardOverlayState>();
			if (st == null || !st.IsOpen) return;

			int vw = _graphicsDevice.Viewport.Width;
			int vh = _graphicsDevice.Viewport.Height;

			int panelW = System.Math.Max(100, PanelWidth);
			int panelH = System.Math.Max(80, PanelHeight);
			var panelRect = new Rectangle((vw - panelW) / 2, (vh - panelH) / 2, panelW, panelH);

			// Dim background slightly for focus
			_spriteBatch.Draw(_pixel, new Rectangle(0, 0, vw, vh), new Color(0, 0, 0, 100));
			// Panel background
			_spriteBatch.Draw(_pixel, panelRect, new Color(0, 0, 0, System.Math.Clamp(PanelAlpha, 0, 255)));
			// Border
			_spriteBatch.Draw(_pixel, new Rectangle(panelRect.X, panelRect.Y, panelRect.Width, 2), Color.White);
			_spriteBatch.Draw(_pixel, new Rectangle(panelRect.X, panelRect.Bottom - 2, panelRect.Width, 2), Color.White);
			_spriteBatch.Draw(_pixel, new Rectangle(panelRect.X, panelRect.Y, 2, panelRect.Height), Color.White);
			_spriteBatch.Draw(_pixel, new Rectangle(panelRect.Right - 2, panelRect.Y, 2, panelRect.Height), Color.White);

			// Title text
			string title = st.Message ?? "Quest Complete!";
			var titleSize = _font.MeasureString(title) * TextScale;
			var titlePos = new Vector2(panelRect.Center.X - titleSize.X / 2f, panelRect.Y + System.Math.Max(8, (int)(panelRect.Height * 0.18f)) - titleSize.Y / 2f);
			_spriteBatch.DrawString(_font, title, titlePos, Color.White, 0f, Vector2.Zero, TextScale, SpriteEffects.None, 0f);

			// Proceed button
			var btn = EntityManager.GetEntity("QuestRewardProceedButton");
			if (btn != null)
			{
				var btnUi = btn.GetComponent<UIElement>();
				var t = btn.GetComponent<Transform>();
				if (btnUi != null && t != null)
				{
					int bw = System.Math.Max(60, ButtonWidth);
					int bh = System.Math.Max(30, ButtonHeight);
					int bx = panelRect.Center.X - bw / 2;
					int by = panelRect.Bottom - bh - System.Math.Max(12, (int)(panelRect.Height * 0.12f));
					var drawRect = new Rectangle(bx, by, bw, bh);
					// Background
					_spriteBatch.Draw(_pixel, drawRect, new Color(40, 40, 40, 220));
					// Border
					_spriteBatch.Draw(_pixel, new Rectangle(drawRect.X, drawRect.Y, drawRect.Width, 2), Color.White);
					_spriteBatch.Draw(_pixel, new Rectangle(drawRect.X, drawRect.Bottom - 2, drawRect.Width, 2), Color.White);
					_spriteBatch.Draw(_pixel, new Rectangle(drawRect.X, drawRect.Y, 2, drawRect.Height), Color.White);
					_spriteBatch.Draw(_pixel, new Rectangle(drawRect.Right - 2, drawRect.Y, 2, drawRect.Height), Color.White);
					// Label
					string label = "Proceed";
					var size = _font.MeasureString(label) * ButtonTextScale;
					var posText = new Vector2(drawRect.Center.X - size.X / 2f, drawRect.Center.Y - size.Y / 2f);
					_spriteBatch.DrawString(_font, label, posText, Color.White, 0f, Vector2.Zero, ButtonTextScale, SpriteEffects.None, 0f);
					// Sync bounds
					btnUi.Bounds = drawRect;
				}
			}
		}

		private void EnsureOverlayEntity()
		{
			var e = EntityManager.GetEntity("QuestRewardOverlay");
			if (e == null)
			{
				e = EntityManager.CreateEntity("QuestRewardOverlay");
				var t = new Transform { Position = Vector2.Zero, ZOrder = ZOrder };
				var ui = new UIElement { Bounds = new Rectangle(0, 0, _graphicsDevice.Viewport.Width, _graphicsDevice.Viewport.Height), IsInteractable = false, LayerType = UILayerType.Overlay };
				EntityManager.AddComponent(e, t);
				EntityManager.AddComponent(e, ui);
				EntityManager.AddComponent(e, new QuestRewardOverlayState());
				EntityManager.AddComponent(e, ParallaxLayer.GetUIParallaxLayer());
				EntityManager.AddComponent(e, new DontDestroyOnLoad());
			}
			else
			{
				var t = e.GetComponent<Transform>();
				if (t != null) t.ZOrder = ZOrder;
			}
		}

		private Entity EnsureProceedButton()
		{
			var ent = EntityManager.GetEntity("QuestRewardProceedButton");
			if (ent == null)
			{
				ent = EntityManager.CreateEntity("QuestRewardProceedButton");
				int vw = _graphicsDevice.Viewport.Width;
				int vh = _graphicsDevice.Viewport.Height;
				int bw = System.Math.Max(60, ButtonWidth);
				int bh = System.Math.Max(30, ButtonHeight);
				int bx = (vw - bw) / 2;
				int by = (vh + PanelHeight) / 2 - System.Math.Max(12, (int)(PanelHeight * 0.12f)) - bh;
				EntityManager.AddComponent(ent, new Transform { BasePosition = new Vector2(bx, by), Position = new Vector2(bx, by), ZOrder = ZOrder + 1 });
				EntityManager.AddComponent(ent, new UIElement { Bounds = new Rectangle(bx, by, bw, bh), IsInteractable = false, LayerType = UILayerType.Overlay, Tooltip = "Proceed" });
				EntityManager.AddComponent(ent, new UIButton { Label = "Proceed" });
				EntityManager.AddComponent(ent, new HotKey { Button = FaceButton.Y });
				EntityManager.AddComponent(ent, ParallaxLayer.GetUIParallaxLayer());
			}
			else
			{
				var t = ent.GetComponent<Transform>();
				if (t != null) t.ZOrder = ZOrder + 1;
				var ui = ent.GetComponent<UIElement>();
				if (ui != null) ui.LayerType = UILayerType.Overlay;
			}
			return ent;
		}
	}
}


