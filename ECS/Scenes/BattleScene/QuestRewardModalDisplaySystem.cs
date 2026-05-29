using System.Linq;
using System.Text.Json.Nodes;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Factories;
using Crusaders30XX.ECS.Singletons;
using Crusaders30XX.ECS.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("Quest Reward Modal")]
	public class QuestRewardModalDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly SpriteFont _titleFont = FontSingleton.TitleFont;
		private readonly SpriteFont _bodyFont = FontSingleton.ChakraPetchFont;
		private readonly Texture2D _pixel;
		private Entity _rewardCardEntity;
		private QuestRewardLayout _layout;

		private static readonly Color ModalFill = new Color(8, 8, 8) * 0.92f;
		private static readonly Color PanelBorder = new Color(255, 255, 255) * 0.85f;
		private static readonly Color LeftColTint = new Color(0, 0, 0) * 0.35f;
		private static readonly Color ColumnDivider = new Color(255, 255, 255) * 0.15f;
		private static readonly Color FooterFill = new Color(0, 0, 0) * 0.25f;
		private static readonly Color FooterBorderTop = new Color(255, 255, 255) * 0.12f;
		private static readonly Color InsetHighlight = new Color(255, 255, 255) * 0.08f;
		private static readonly Color TitleColor = Color.White;
		private static readonly Color GoldLabelColor = new Color(160, 128, 48);
		private static readonly Color GoldAmountColor = new Color(232, 200, 74);
		private static readonly Color StageLabelColor = new Color(200, 192, 184);
		private static readonly Color RedRuleCenter = new Color(196, 30, 58);
		private static readonly Color ButtonFill = new Color(30, 30, 30);
		private static readonly Color ButtonFillHover = new Color(160, 0, 0);
		private static readonly Color ButtonBorder = Color.White;
		private static readonly Color ButtonBorderHover = new Color(196, 30, 58);
		private static readonly Color DropShadow = new Color(0, 0, 0) * 0.75f;

		[DebugEditable(DisplayName = "Z Order", Step = 10, Min = 0, Max = 100000)]
		public int ZOrder { get; set; } = 52000;

		[DebugEditable(DisplayName = "Modal Width", Step = 10, Min = 200, Max = 1600)]
		public int ModalWidth { get; set; } = 920;
		[DebugEditable(DisplayName = "Modal Height", Step = 10, Min = 200, Max = 1200)]
		public int ModalHeight { get; set; } = 520;
		[DebugEditable(DisplayName = "Left Col Width", Step = 10, Min = 120, Max = 600)]
		public int LeftColWidth { get; set; } = 280;
		[DebugEditable(DisplayName = "Gold Only Modal Width", Step = 10, Min = 200, Max = 800)]
		public int GoldOnlyModalWidth { get; set; } = 400;
		[DebugEditable(DisplayName = "Border Thickness", Step = 1, Min = 1, Max = 8)]
		public int BorderThickness { get; set; } = 2;
		[DebugEditable(DisplayName = "Dim Alpha", Step = 5, Min = 0, Max = 255)]
		public int DimAlpha { get; set; } = 140;

		[DebugEditable(DisplayName = "Left Padding Top", Step = 2, Min = 0, Max = 120)]
		public int LeftPaddingTop { get; set; } = 40;
		[DebugEditable(DisplayName = "Left Padding Bottom", Step = 2, Min = 0, Max = 120)]
		public int LeftPaddingBottom { get; set; } = 40;
		[DebugEditable(DisplayName = "Left Padding X", Step = 2, Min = 0, Max = 80)]
		public int LeftPaddingX { get; set; } = 44;
		[DebugEditable(DisplayName = "Left Col Gap", Step = 2, Min = 0, Max = 80)]
		public int LeftColGap { get; set; } = 32;

		[DebugEditable(DisplayName = "Title Line 1")]
		public string TitleLine1 { get; set; } = "Quest";
		[DebugEditable(DisplayName = "Title Line 2")]
		public string TitleLine2 { get; set; } = "Complete!";
		[DebugEditable(DisplayName = "Title Scale", Step = 0.01f, Min = 0.1f, Max = 2f)]
		public float TitleScale { get; set; } = 0.33f;
		[DebugEditable(DisplayName = "Gold Label Scale", Step = 0.01f, Min = 0.05f, Max = 1f)]
		public float GoldLabelScale { get; set; } = 0.14f;
		[DebugEditable(DisplayName = "Gold Amount Scale", Step = 0.01f, Min = 0.1f, Max = 2f)]
		public float GoldAmountScale { get; set; } = 0.42f;
		[DebugEditable(DisplayName = "Stage Label Scale", Step = 0.01f, Min = 0.05f, Max = 1f)]
		public float StageLabelScale { get; set; } = 0.14f;

		[DebugEditable(DisplayName = "Red Rule Width", Step = 2, Min = 20, Max = 200)]
		public int RedRuleWidth { get; set; } = 80;
		[DebugEditable(DisplayName = "Red Rule Height", Step = 1, Min = 1, Max = 12)]
		public int RedRuleHeight { get; set; } = 3;

		[DebugEditable(DisplayName = "Right Padding Top", Step = 2, Min = 0, Max = 120)]
		public int RightPaddingTop { get; set; } = 48;
		[DebugEditable(DisplayName = "Right Padding X", Step = 2, Min = 0, Max = 80)]
		public int RightPaddingX { get; set; } = 40;
		[DebugEditable(DisplayName = "Right Padding Bottom", Step = 2, Min = 0, Max = 120)]
		public int RightPaddingBottom { get; set; } = 32;
		[DebugEditable(DisplayName = "Stage Label Gap", Step = 2, Min = 0, Max = 120)]
		public int StageLabelGap { get; set; } = 36;

		[DebugEditable(DisplayName = "Card Preview Scale", Step = 0.01f, Min = 0.1f, Max = 2f)]
		public float CardPreviewScale { get; set; } = 0.74f;
		[DebugEditable(DisplayName = "Card Preview Offset X", Step = 2, Min = -400, Max = 400)]
		public int CardPreviewOffsetX { get; set; } = 0;
		[DebugEditable(DisplayName = "Card Preview Offset Y", Step = 2, Min = -400, Max = 400)]
		public int CardPreviewOffsetY { get; set; } = -20;

		[DebugEditable(DisplayName = "Drop Shadow Offset Y", Step = 1, Min = 0, Max = 40)]
		public int DropShadowOffsetY { get; set; } = 16;

		[DebugEditable(DisplayName = "Footer Padding", Step = 2, Min = 0, Max = 60)]
		public int FooterPadding { get; set; } = 20;
		[DebugEditable(DisplayName = "Button Width", Step = 5, Min = 60, Max = 800)]
		public int ButtonWidth { get; set; } = 220;
		[DebugEditable(DisplayName = "Button Height", Step = 5, Min = 30, Max = 300)]
		public int ButtonHeight { get; set; } = 64;
		[DebugEditable(DisplayName = "Button Text Scale", Step = 0.01f, Min = 0.1f, Max = 2f)]
		public float ButtonTextScale { get; set; } = 0.28f;

		private struct QuestRewardLayout
		{
			public Rectangle Modal;
			public Rectangle Content;
			public Rectangle LeftColumn;
			public Rectangle LeftInner;
			public Rectangle Divider;
			public Rectangle RightColumn;
			public Rectangle RightInner;
			public Rectangle Footer;
			public Rectangle ProceedButton;
			public Vector2 CardCenter;
			public float StageLabelHeight;
			public bool ShowRightColumn;
			public bool ShowGold;
			public bool ShowCard;
		}

		public QuestRewardModalDisplaySystem(EntityManager entityManager, GraphicsDevice gd, SpriteBatch sb) : base(entityManager)
		{
			_graphicsDevice = gd;
			_spriteBatch = sb;
			_pixel = new Texture2D(gd, 1, 1);
			_pixel.SetData(new[] { Color.White });
			EventManager.Subscribe<ShowQuestRewardOverlay>(e => {
				LoggingService.Append("QuestRewardModalDisplaySystem.OnShowQuestRewardOverlay", new JsonObject {
					{ "Message", e.Message },
					{ "RewardGold", e.RewardGold },
					{ "HasCardReward", e.HasCardReward },
					{ "RewardCardKey", e.RewardCardKey ?? string.Empty }
				});
				Open(e.Message, e.RewardGold, e.HasCardReward, e.RewardCardKey);
			});
			EventManager.Subscribe<DeleteCachesEvent>(_ => DestroyRewardCard());
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<SceneState>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			var overlayEntity = EntityManager.GetEntity("QuestRewardOverlay");
			if (overlayEntity == null) return;
			var ui = overlayEntity?.GetComponent<UIElement>();
			var state = overlayEntity?.GetComponent<QuestRewardOverlayState>();
			if (ui == null || state == null) return;

			ui.IsInteractable = state.IsOpen;
			ui.LayerType = state.IsOpen ? UILayerType.Overlay : UILayerType.Default;
			ui.Bounds = state.IsOpen
				? new Rectangle(0, 0, Game1.VirtualWidth, Game1.VirtualHeight)
				: new Rectangle(0, 0, 0, 0);

			if (!state.IsOpen) return;

			int vw = Game1.VirtualWidth;
			int vh = Game1.VirtualHeight;
			_layout = ComputeLayout(vw, vh, state.RewardGold > 0, state.HasCardReward);

			var btn = EnsureProceedButton();
			var btnUi = btn?.GetComponent<UIElement>();
			if (btnUi != null)
			{
				btnUi.Bounds = _layout.ProceedButton;
				btnUi.IsInteractable = true;
				if (btnUi.IsClicked)
				{
					btnUi.IsClicked = false;
					CloseOverlay(state);
					EventManager.Publish(new ShowTransition { Scene = SceneId.Location });
				}
			}
		}

		public void Open(string message = null, int rewardGold = 0, bool hasCardReward = false, string rewardCardKey = null)
		{
			EnsureOverlayEntity();
			var e = EntityManager.GetEntity("QuestRewardOverlay");
			var st = e.GetComponent<QuestRewardOverlayState>();

			DestroyRewardCard();
			if (!string.IsNullOrEmpty(message)) st.Message = message;
			st.RewardGold = rewardGold;
			st.HasCardReward = hasCardReward;
			st.RewardCardKey = rewardCardKey ?? string.Empty;
			st.IsOpen = true;

			if (hasCardReward && !string.IsNullOrEmpty(rewardCardKey))
			{
				_rewardCardEntity = CreateRewardCard(rewardCardKey);
			}
		}

		public void Draw()
		{
			if (_titleFont == null) return;
			var e = EntityManager.GetEntity("QuestRewardOverlay");
			var st = e?.GetComponent<QuestRewardOverlayState>();
			if (st == null || !st.IsOpen) return;
			var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
			if (scene == null || scene.Current != SceneId.Battle) return;

			int vw = Game1.VirtualWidth;
			int vh = Game1.VirtualHeight;
			bool showGold = st.RewardGold > 0;
			bool showCard = st.HasCardReward && _rewardCardEntity != null;
			_layout = ComputeLayout(vw, vh, showGold, showCard);

			// 1. Dim
			_spriteBatch.Draw(_pixel, new Rectangle(0, 0, vw, vh), new Color(0, 0, 0, System.Math.Clamp(DimAlpha, 0, 255)));

			// 2. Drop shadow (offset down but clipped to modal bottom — no bleed past border)
			int shadowY = System.Math.Max(0, DropShadowOffsetY);
			int shadowH = System.Math.Max(1, _layout.Modal.Height - shadowY);
			var shadow = new Rectangle(_layout.Modal.X, _layout.Modal.Y + shadowY, _layout.Modal.Width, shadowH);
			_spriteBatch.Draw(_pixel, shadow, DropShadow);

			// 3. Modal shell + regions inside content
			_spriteBatch.Draw(_pixel, _layout.Modal, ModalFill);
			_spriteBatch.Draw(_pixel, _layout.LeftColumn, LeftColTint);
			if (_layout.ShowRightColumn)
			{
				_spriteBatch.Draw(_pixel, _layout.Divider, ColumnDivider);
			}
			_spriteBatch.Draw(_pixel, _layout.Footer, FooterFill);
			_spriteBatch.Draw(_pixel, new Rectangle(_layout.Footer.X, _layout.Footer.Y, _layout.Footer.Width, 1), FooterBorderTop);
			DrawInsetHighlight(_layout.Content);
			DrawBorder(_layout.Modal, PanelBorder, BorderThickness);

			// 4. Left column text
			DrawLeftColumn(st);

			// 5. Right column: label above card
			if (_layout.ShowRightColumn)
			{
				DrawStageLabel();
				DrawRightColumnCard(showCard);
			}

			// 6. Proceed button
			var btn = EntityManager.GetEntity("QuestRewardProceedButton");
			bool hovered = btn?.GetComponent<UIElement>()?.IsHovered ?? false;
			DrawProceedButton(hovered);
		}

		private QuestRewardLayout ComputeLayout(int vw, int vh, bool showGold, bool showCard)
		{
			int modalW = ModalWidth;
			if (showGold && !showCard)
			{
				modalW = GoldOnlyModalWidth;
			}
			else if (!showGold && !showCard)
			{
				modalW = System.Math.Max(LeftColWidth + BorderThickness * 2, 320);
			}

			int modalH = System.Math.Max(200, ModalHeight);
			int border = System.Math.Max(1, BorderThickness);
			int modalX = (vw - modalW) / 2;
			int modalY = (vh - modalH) / 2;

			var modal = new Rectangle(modalX, modalY, modalW, modalH);
			var content = new Rectangle(
				modal.X + border,
				modal.Y + border,
				System.Math.Max(1, modal.Width - border * 2),
				System.Math.Max(1, modal.Height - border * 2));

			int footerH = FooterPadding * 2 + System.Math.Max(30, ButtonHeight);
			int bodyH = System.Math.Max(1, content.Height - footerH);
			var footer = new Rectangle(content.X, content.Y + bodyH, content.Width, footerH);
			var body = new Rectangle(content.X, content.Y, content.Width, bodyH);

			bool showRightColumn = showCard;
			int leftW = showRightColumn ? System.Math.Min(LeftColWidth, body.Width) : body.Width;
			var leftColumn = new Rectangle(body.X, body.Y, leftW, body.Height);
			var leftInner = new Rectangle(
				leftColumn.X + LeftPaddingX,
				leftColumn.Y + LeftPaddingTop,
				System.Math.Max(1, leftColumn.Width - LeftPaddingX * 2),
				System.Math.Max(1, leftColumn.Height - LeftPaddingTop - LeftPaddingBottom));

			Rectangle divider = Rectangle.Empty;
			Rectangle rightColumn = Rectangle.Empty;
			Rectangle rightInner = Rectangle.Empty;
			Vector2 cardCenter = Vector2.Zero;
			float stageLabelH = 0f;

			if (showRightColumn)
			{
				int rightX = body.X + leftW;
				int rightW = System.Math.Max(1, body.Width - leftW);
				divider = new Rectangle(rightX - 1, body.Y, 1, body.Height);
				rightColumn = new Rectangle(rightX, body.Y, rightW, body.Height);
				rightInner = new Rectangle(
					rightColumn.X + RightPaddingX,
					rightColumn.Y + RightPaddingTop,
					System.Math.Max(1, rightColumn.Width - RightPaddingX * 2),
					System.Math.Max(1, rightColumn.Height - RightPaddingTop - RightPaddingBottom));

				float labelTop = rightColumn.Y + RightPaddingTop;
				stageLabelH = GetStageLabelHeight();
				float labelBottom = labelTop + stageLabelH;
				float cardTop = labelBottom + StageLabelGap;
				float cardHalfH = GetScaledCardHalfHeight();
				cardCenter = new Vector2(
					rightInner.X + rightInner.Width / 2f + CardPreviewOffsetX,
					cardTop + cardHalfH + CardPreviewOffsetY);
			}

			int bw = System.Math.Max(60, ButtonWidth);
			int bh = System.Math.Max(30, ButtonHeight);
			int bx = content.X + (content.Width - bw) / 2;
			int by = footer.Y + FooterPadding;
			var proceedButton = new Rectangle(bx, by, bw, bh);

			return new QuestRewardLayout
			{
				Modal = modal,
				Content = content,
				LeftColumn = leftColumn,
				LeftInner = leftInner,
				Divider = divider,
				RightColumn = rightColumn,
				RightInner = rightInner,
				Footer = footer,
				ProceedButton = proceedButton,
				CardCenter = cardCenter,
				StageLabelHeight = stageLabelH,
				ShowRightColumn = showRightColumn,
				ShowGold = showGold,
				ShowCard = showCard
			};
		}

		private float GetStageLabelHeight()
		{
			if (_bodyFont == null) return 16f;
			return _bodyFont.MeasureString("REWARD").Y * StageLabelScale;
		}

		private float GetScaledCardHalfHeight()
		{
			var settings = EntityManager.GetEntitiesWithComponent<CardVisualSettings>().FirstOrDefault()?.GetComponent<CardVisualSettings>();
			float cardH = settings?.CardHeight ?? 340;
			float offsetY = settings?.CardOffsetYExtra ?? 0f;
			float scale = CardPreviewScale;
			return cardH * scale / 2f + offsetY * scale;
		}

		private void DrawLeftColumn(QuestRewardOverlayState state)
		{
			int centerX = _layout.LeftInner.Center.X;
			float cursorY = _layout.LeftInner.Y;

			string line1 = TitleLine1 ?? "Quest";
			string line2 = TitleLine2 ?? "Complete!";
			var size1 = _titleFont.MeasureString(line1) * TitleScale;
			var size2 = _titleFont.MeasureString(line2) * TitleScale;
			_spriteBatch.DrawString(_titleFont, line1, new Vector2(centerX - size1.X / 2f, cursorY), TitleColor, 0f, Vector2.Zero, TitleScale, SpriteEffects.None, 0f);
			cursorY += size1.Y;
			_spriteBatch.DrawString(_titleFont, line2, new Vector2(centerX - size2.X / 2f, cursorY), TitleColor, 0f, Vector2.Zero, TitleScale, SpriteEffects.None, 0f);
			cursorY += size2.Y + LeftColGap;

			int ruleY = (int)cursorY;
			DrawHorizontalGradientRule(centerX, ruleY, RedRuleWidth, RedRuleHeight, RedRuleCenter);
			cursorY += RedRuleHeight + LeftColGap;

			if (_layout.ShowGold && state.RewardGold > 0)
			{
				float goldBlockTop = cursorY;
				float goldBlockBottom = _layout.LeftInner.Bottom;
				float goldBlockH = System.Math.Max(1f, goldBlockBottom - goldBlockTop);

				string goldLabel = "GOLD";
				string goldAmount = $"+{state.RewardGold:N0}";
				float labelH = _bodyFont != null ? _bodyFont.MeasureString(goldLabel).Y * GoldLabelScale : 14f;
				float amountH = _titleFont.MeasureString(goldAmount).Y * GoldAmountScale;
				float totalGoldH = labelH + 4f + amountH;
				float goldStartY = goldBlockTop + (goldBlockH - totalGoldH) / 2f;

				if (_bodyFont != null)
				{
					var labelSize = _bodyFont.MeasureString(goldLabel) * GoldLabelScale;
					_spriteBatch.DrawString(_bodyFont, goldLabel,
						new Vector2(centerX - labelSize.X / 2f, goldStartY),
						GoldLabelColor, 0f, Vector2.Zero, GoldLabelScale, SpriteEffects.None, 0f);
				}

				var amountSize = _titleFont.MeasureString(goldAmount) * GoldAmountScale;
				var amountPos = new Vector2(centerX - amountSize.X / 2f, goldStartY + labelH + 4f);
				DrawGoldGlow(goldAmount, amountPos, GoldAmountScale);
				_spriteBatch.DrawString(_titleFont, goldAmount, amountPos, GoldAmountColor, 0f, Vector2.Zero, GoldAmountScale, SpriteEffects.None, 0f);
			}
		}

		private void DrawRightColumnCard(bool showCard)
		{
			if (!showCard || _rewardCardEntity == null) return;
			EventManager.Publish(new CardRenderScaledEvent
			{
				Card = _rewardCardEntity,
				Position = _layout.CardCenter,
				Scale = CardPreviewScale
			});
		}

		private void DrawStageLabel()
		{
			if (_bodyFont == null) return;

			const string stageLabel = "REWARD";
			var labelSize = _bodyFont.MeasureString(stageLabel) * StageLabelScale;
			float labelX = _layout.RightInner.X + (_layout.RightInner.Width - labelSize.X) / 2f;
			float labelY = _layout.RightColumn.Y + RightPaddingTop;
			_spriteBatch.DrawString(_bodyFont, stageLabel,
				new Vector2(labelX, labelY),
				StageLabelColor, 0f, Vector2.Zero, StageLabelScale, SpriteEffects.None, 0f);
		}

		private void DrawProceedButton(bool hovered)
		{
			var r = _layout.ProceedButton;
			var fill = hovered ? ButtonFillHover : ButtonFill;
			var border = hovered ? ButtonBorderHover : ButtonBorder;
			_spriteBatch.Draw(_pixel, r, fill);
			DrawBorder(r, border, BorderThickness);

			string label = "Proceed";
			var size = _titleFont.MeasureString(label) * ButtonTextScale;
			var pos = new Vector2(r.Center.X - size.X / 2f, r.Center.Y - size.Y / 2f);
			_spriteBatch.DrawString(_titleFont, label, pos, Color.White, 0f, Vector2.Zero, ButtonTextScale, SpriteEffects.None, 0f);
		}

		private void DrawBorder(Rectangle r, Color color, int thickness)
		{
			int t = System.Math.Max(1, thickness);
			_spriteBatch.Draw(_pixel, new Rectangle(r.X, r.Y, r.Width, t), color);
			_spriteBatch.Draw(_pixel, new Rectangle(r.X, r.Bottom - t, r.Width, t), color);
			_spriteBatch.Draw(_pixel, new Rectangle(r.X, r.Y, t, r.Height), color);
			_spriteBatch.Draw(_pixel, new Rectangle(r.Right - t, r.Y, t, r.Height), color);
		}

		private void DrawInsetHighlight(Rectangle contentRect)
		{
			if (contentRect.Width <= 0 || contentRect.Height <= 0) return;
			DrawBorder(contentRect, InsetHighlight, 1);
		}

		private void DrawHorizontalGradientRule(int centerX, int centerY, int width, int height, Color centerColor)
		{
			int half = width / 2;
			int strips = 9;
			for (int i = 0; i < strips; i++)
			{
				float t = i / (float)(strips - 1);
				float dist = System.Math.Abs(t - 0.5f) * 2f;
				float alpha = 1f - dist;
				int stripW = System.Math.Max(1, width / strips);
				int x = centerX - half + i * stripW;
				_spriteBatch.Draw(_pixel, new Rectangle(x, centerY, stripW, height), centerColor * alpha);
			}
		}

		private void DrawGoldGlow(string text, Vector2 pos, float scale)
		{
			var glow = GoldAmountColor * 0.35f;
			_spriteBatch.DrawString(_titleFont, text, pos + new Vector2(-2, 0), glow, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
			_spriteBatch.DrawString(_titleFont, text, pos + new Vector2(2, 0), glow, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
			_spriteBatch.DrawString(_titleFont, text, pos + new Vector2(0, -2), glow, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
			_spriteBatch.DrawString(_titleFont, text, pos + new Vector2(0, 2), glow, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
		}

		private void CloseOverlay(QuestRewardOverlayState state)
		{
			state.IsOpen = false;
			state.RewardGold = 0;
			state.HasCardReward = false;
			state.RewardCardKey = string.Empty;
			DestroyRewardCard();
		}

		private void DestroyRewardCard()
		{
			if (_rewardCardEntity == null) return;
			EntityManager.DestroyEntity(_rewardCardEntity.Id);
			_rewardCardEntity = null;
		}

		private Entity CreateRewardCard(string cardKey)
		{
			var parts = cardKey.Split('|');
			if (parts.Length < 2) return null;
			string cardId = parts[0];
			var color = ParseColor(parts[1]);
			var created = EntityFactory.CreateCardFromDefinition(EntityManager, cardId, color);
			if (created == null) return null;

			var ui = created.GetComponent<UIElement>();
			if (ui != null)
			{
				ui.IsInteractable = false;
				ui.TooltipType = TooltipType.None;
				ui.Tooltip = string.Empty;
			}
			return created;
		}

		private static CardData.CardColor ParseColor(string color)
		{
			if (string.IsNullOrEmpty(color)) return CardData.CardColor.White;
			switch (color.Trim().ToLowerInvariant())
			{
				case "red": return CardData.CardColor.Red;
				case "black": return CardData.CardColor.Black;
				default: return CardData.CardColor.White;
			}
		}

		private void EnsureOverlayEntity()
		{
			var e = EntityManager.GetEntity("QuestRewardOverlay");
			if (e == null)
			{
				e = EntityManager.CreateEntity("QuestRewardOverlay");
				var t = new Transform { Position = Vector2.Zero, ZOrder = ZOrder };
				var ui = new UIElement { Bounds = new Rectangle(0, 0, Game1.VirtualWidth, Game1.VirtualHeight), IsInteractable = false, LayerType = UILayerType.Overlay };
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
				EntityManager.AddComponent(ent, new Transform { Position = Vector2.Zero, ZOrder = ZOrder + 1 });
				EntityManager.AddComponent(ent, new UIElement { Bounds = Rectangle.Empty, IsInteractable = false, LayerType = UILayerType.Overlay });
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
