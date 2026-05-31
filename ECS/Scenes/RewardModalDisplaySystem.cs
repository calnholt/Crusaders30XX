using System.Collections.Generic;
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
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("Reward Modal")]
	public class RewardModalDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly ContentManager _content;
		private readonly SpriteFont _titleFont = FontSingleton.TitleFont;
		private readonly SpriteFont _bodyFont = FontSingleton.ChakraPetchFont;
		private readonly Texture2D _pixel;
		private Entity _rewardCardEntity;
		private QuestRewardLayout _layout;

		private bool _layoutValid;
		private bool _textMetricsValid;
		private bool _drawInBattleOrSnapshot;
		private int _cachedVw;
		private int _cachedVh;
		private bool _cachedShowGold;
		private bool _cachedShowCard;
		private bool _cachedShowMedal;
		private int _cachedRewardGold;
		private LayoutSignature _layoutSignature;
		private CachedTextMetrics _textMetrics;
		private readonly Dictionary<(int w, int h), Texture2D> _gradientRuleCache = new();

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

		private const string GoldLabelText = "GOLD";
		private const string StageLabelText = "REWARD";
		private const string ProceedLabelText = "Proceed";

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

		[DebugEditable(DisplayName = "Medal Preview Size", Step = 2, Min = 40, Max = 400)]
		public int MedalPreviewSize { get; set; } = 180;

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
			public Rectangle MedalPreviewRect;
			public float StageLabelHeight;
			public bool ShowRightColumn;
			public bool ShowGold;
			public bool ShowCard;
			public bool ShowMedal;
		}

		private struct LayoutSignature
		{
			public int ModalWidth;
			public int ModalHeight;
			public int LeftColWidth;
			public int GoldOnlyModalWidth;
			public int BorderThickness;
			public int LeftPaddingTop;
			public int LeftPaddingBottom;
			public int LeftPaddingX;
			public int LeftColGap;
			public string TitleLine1;
			public string TitleLine2;
			public float TitleScale;
			public float GoldLabelScale;
			public float GoldAmountScale;
			public float StageLabelScale;
			public int RedRuleWidth;
			public int RedRuleHeight;
			public int RightPaddingTop;
			public int RightPaddingX;
			public int RightPaddingBottom;
			public int StageLabelGap;
			public float CardPreviewScale;
			public int CardPreviewOffsetX;
			public int CardPreviewOffsetY;
			public int FooterPadding;
			public int ButtonWidth;
			public int ButtonHeight;
			public float ButtonTextScale;
			public int CardHeight;
			public int CardOffsetYExtra;
		}

		private struct CachedTextMetrics
		{
			public Vector2 TitleLine1Size;
			public Vector2 TitleLine2Size;
			public Vector2 TitleLine1Pos;
			public Vector2 TitleLine2Pos;
			public int RuleY;
			public Vector2 GoldLabelSize;
			public float GoldLabelHeight;
			public Vector2 GoldLabelPos;
			public string GoldAmountText;
			public Vector2 GoldAmountSize;
			public Vector2 GoldAmountPos;
			public bool HasGoldBlock;
			public Vector2 StageLabelPos;
			public Vector2 ProceedTextPos;
			public Vector2 ProceedTextSize;
		}

		public RewardModalDisplaySystem(EntityManager entityManager, GraphicsDevice gd, SpriteBatch sb, ContentManager content) : base(entityManager)
		{
			_graphicsDevice = gd;
			_spriteBatch = sb;
			_content = content;
			_pixel = new Texture2D(gd, 1, 1);
			_pixel.SetData(new[] { Color.White });
			EventManager.Subscribe<ShowQuestRewardOverlay>(e => {
				LoggingService.Append("RewardModalDisplaySystem.OnShowQuestRewardOverlay", new JsonObject {
					{ "Message", e.Message },
					{ "RewardGold", e.RewardGold },
					{ "HasCardReward", e.HasCardReward },
					{ "RewardCardKey", e.RewardCardKey ?? string.Empty }
				});
				OpenQuestReward(e);
			});
			EventManager.Subscribe<TreasureChestOpened>(e => {
				LoggingService.Append("RewardModalDisplaySystem.OnTreasureChestOpened", new JsonObject {
					{ "RewardGold", e.RewardGold },
					{ "RewardMedalId", e.RewardMedalId ?? string.Empty }
				});
				OpenTreasureChest(e);
			});
			EventManager.Subscribe<DeleteCachesEvent>(OnDeleteCaches);
		}

		private void OnDeleteCaches(DeleteCachesEvent _)
		{
			InvalidateCaches();
			DisposeGradientCache();
			DestroyRewardCard();
		}

		private void InvalidateCaches()
		{
			_layoutValid = false;
			_textMetricsValid = false;
		}

		private void DisposeGradientCache()
		{
			foreach (var kv in _gradientRuleCache)
			{
				try { kv.Value?.Dispose(); } catch { }
			}
			_gradientRuleCache.Clear();
		}

		private LayoutSignature CaptureLayoutSignature()
		{
			var overlayState = EntityManager.GetEntity("QuestRewardOverlay")?.GetComponent<QuestRewardOverlayState>();
			var settings = EntityManager.GetEntitiesWithComponent<CardVisualSettings>().FirstOrDefault()?.GetComponent<CardVisualSettings>();
			return new LayoutSignature
			{
				ModalWidth = ModalWidth,
				ModalHeight = ModalHeight,
				LeftColWidth = LeftColWidth,
				GoldOnlyModalWidth = GoldOnlyModalWidth,
				BorderThickness = BorderThickness,
				LeftPaddingTop = LeftPaddingTop,
				LeftPaddingBottom = LeftPaddingBottom,
				LeftPaddingX = LeftPaddingX,
				LeftColGap = LeftColGap,
				TitleLine1 = overlayState?.TitleLine1 ?? TitleLine1 ?? "Quest",
				TitleLine2 = overlayState?.TitleLine2 ?? TitleLine2 ?? "Complete!",
				TitleScale = TitleScale,
				GoldLabelScale = GoldLabelScale,
				GoldAmountScale = GoldAmountScale,
				StageLabelScale = StageLabelScale,
				RedRuleWidth = RedRuleWidth,
				RedRuleHeight = RedRuleHeight,
				RightPaddingTop = RightPaddingTop,
				RightPaddingX = RightPaddingX,
				RightPaddingBottom = RightPaddingBottom,
				StageLabelGap = StageLabelGap,
				CardPreviewScale = CardPreviewScale,
				CardPreviewOffsetX = CardPreviewOffsetX,
				CardPreviewOffsetY = CardPreviewOffsetY,
				FooterPadding = FooterPadding,
				ButtonWidth = ButtonWidth,
				ButtonHeight = ButtonHeight,
				ButtonTextScale = ButtonTextScale,
				CardHeight = settings?.CardHeight ?? 340,
				CardOffsetYExtra = settings?.CardOffsetYExtra ?? 0
			};
		}

		private bool NeedsLayoutRebuild(int vw, int vh, bool showGold, bool showCard, bool showMedal, int rewardGold)
		{
			if (!_layoutValid) return true;
			if (vw != _cachedVw || vh != _cachedVh) return true;
			if (showGold != _cachedShowGold || showCard != _cachedShowCard || showMedal != _cachedShowMedal || rewardGold != _cachedRewardGold) return true;
			var sig = CaptureLayoutSignature();
			return !sig.Equals(_layoutSignature);
		}

		private void EnsureLayout(int vw, int vh, bool showGold, bool showCard, bool showMedal, int rewardGold, SceneState scene)
		{
			if (!NeedsLayoutRebuild(vw, vh, showGold, showCard, showMedal, rewardGold)) return;

			_cachedVw = vw;
			_cachedVh = vh;
			_cachedShowGold = showGold;
			_cachedShowCard = showCard;
			_cachedShowMedal = showMedal;
			_cachedRewardGold = rewardGold;
			_layoutSignature = CaptureLayoutSignature();
			_drawInBattleOrSnapshot = scene != null
				&& (scene.Current == SceneId.Battle
					|| scene.Current == SceneId.Location
					|| scene.Current == SceneId.Snapshot);

			_layout = ComputeLayout(vw, vh, showGold, showCard, showMedal, _layoutSignature);
			RebuildTextMetrics(rewardGold, showGold);
			_layoutValid = true;
			_textMetricsValid = true;
		}

		private void RebuildTextMetrics(int rewardGold, bool showGold)
		{
			int centerX = _layout.LeftInner.Center.X;
			float cursorY = _layout.LeftInner.Y;

			string line1 = _layoutSignature.TitleLine1;
			string line2 = _layoutSignature.TitleLine2;

			var title1Size = _titleFont.MeasureString(line1) * TitleScale;
			var title2Size = _titleFont.MeasureString(line2) * TitleScale;
			var title1Pos = new Vector2(centerX - title1Size.X / 2f, cursorY);
			cursorY += title1Size.Y;
			var title2Pos = new Vector2(centerX - title2Size.X / 2f, cursorY);
			cursorY += title2Size.Y + LeftColGap;

			int ruleY = (int)cursorY;

			var metrics = new CachedTextMetrics
			{
				TitleLine1Size = title1Size,
				TitleLine2Size = title2Size,
				TitleLine1Pos = title1Pos,
				TitleLine2Pos = title2Pos,
				RuleY = ruleY,
				HasGoldBlock = false
			};

			if (showGold && rewardGold > 0)
			{
				float goldBlockTop = cursorY + RedRuleHeight + LeftColGap;
				float goldBlockBottom = _layout.LeftInner.Bottom;
				float goldBlockH = System.Math.Max(1f, goldBlockBottom - goldBlockTop);

				float labelH = _bodyFont != null ? _bodyFont.MeasureString(GoldLabelText).Y * GoldLabelScale : 14f;
				string goldAmount = $"+{rewardGold:N0}";
				var amountSize = _titleFont.MeasureString(goldAmount) * GoldAmountScale;
				float totalGoldH = labelH + 4f + amountSize.Y;
				float goldStartY = goldBlockTop + (goldBlockH - totalGoldH) / 2f;

				Vector2 labelSize = Vector2.Zero;
				Vector2 labelPos = Vector2.Zero;
				if (_bodyFont != null)
				{
					labelSize = _bodyFont.MeasureString(GoldLabelText) * GoldLabelScale;
					labelPos = new Vector2(centerX - labelSize.X / 2f, goldStartY);
				}

				metrics.HasGoldBlock = true;
				metrics.GoldLabelSize = labelSize;
				metrics.GoldLabelHeight = labelH;
				metrics.GoldLabelPos = labelPos;
				metrics.GoldAmountText = goldAmount;
				metrics.GoldAmountSize = amountSize;
				metrics.GoldAmountPos = new Vector2(centerX - amountSize.X / 2f, goldStartY + labelH + 4f);
			}

			if (_bodyFont != null && _layout.ShowRightColumn)
			{
				var stageSize = _bodyFont.MeasureString(StageLabelText) * StageLabelScale;
				float labelX = _layout.RightInner.X + (_layout.RightInner.Width - stageSize.X) / 2f;
				float labelY = _layout.RightColumn.Y + RightPaddingTop;
				metrics.StageLabelPos = new Vector2(labelX, labelY);
			}

			var proceedSize = _titleFont.MeasureString(ProceedLabelText) * ButtonTextScale;
			var r = _layout.ProceedButton;
			metrics.ProceedTextSize = proceedSize;
			metrics.ProceedTextPos = new Vector2(r.Center.X - proceedSize.X / 2f, r.Center.Y - proceedSize.Y / 2f);

			_textMetrics = metrics;
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<SceneState>();
		}

		protected override void UpdateEntity(Entity sceneEntity, GameTime gameTime)
		{
			var overlayEntity = EntityManager.GetEntity("QuestRewardOverlay");
			if (overlayEntity == null) return;
			var ui = overlayEntity.GetComponent<UIElement>();
			var state = overlayEntity.GetComponent<QuestRewardOverlayState>();
			if (ui == null || state == null) return;

			ui.IsInteractable = state.IsOpen;
			ui.LayerType = state.IsOpen ? UILayerType.Overlay : UILayerType.Default;
			ui.Bounds = state.IsOpen
				? new Rectangle(0, 0, Game1.VirtualWidth, Game1.VirtualHeight)
				: new Rectangle(0, 0, 0, 0);

			if (!state.IsOpen)
			{
				StateSingleton.PreventClicking = false;
				return;
			}

			var scene = sceneEntity.GetComponent<SceneState>();
			StateSingleton.PreventClicking = scene != null && scene.Current == SceneId.Location;

			int vw = Game1.VirtualWidth;
			int vh = Game1.VirtualHeight;
			bool showGold = state.RewardGold > 0;
			bool showCard = state.HasCardReward && _rewardCardEntity != null;
			bool showMedal = state.HasMedalReward && !string.IsNullOrEmpty(state.RewardMedalId);
			EnsureLayout(vw, vh, showGold, showCard, showMedal, state.RewardGold, scene);

			var btn = EnsureProceedButton();
			var btnUi = btn?.GetComponent<UIElement>();
			if (btnUi != null)
			{
				btnUi.Bounds = _layout.ProceedButton;
				btnUi.IsInteractable = true;
				if (btnUi.IsClicked)
				{
					btnUi.IsClicked = false;
					bool dismissToLocation = state.DismissToLocation;
					CloseOverlay(state);
					if (dismissToLocation)
					{
						EventManager.Publish(new ShowTransition { Scene = SceneId.Location });
					}
				}
			}
		}

		private void OpenQuestReward(ShowQuestRewardOverlay e)
		{
			EnsureOverlayEntity();
			var st = EntityManager.GetEntity("QuestRewardOverlay").GetComponent<QuestRewardOverlayState>();

			DestroyRewardCard();
			InvalidateCaches();
			if (!string.IsNullOrEmpty(e?.Message)) st.Message = e.Message;
			st.TitleLine1 = string.IsNullOrWhiteSpace(e?.TitleLine1) ? TitleLine1 : e.TitleLine1;
			st.TitleLine2 = string.IsNullOrWhiteSpace(e?.TitleLine2) ? TitleLine2 : e.TitleLine2;
			st.RewardGold = e?.RewardGold ?? 0;
			st.HasCardReward = e?.HasCardReward ?? false;
			st.RewardCardKey = e?.RewardCardKey ?? string.Empty;
			st.HasMedalReward = false;
			st.RewardMedalId = string.Empty;
			st.DismissToLocation = true;
			st.IsOpen = true;

			if (st.HasCardReward && !string.IsNullOrEmpty(st.RewardCardKey))
			{
				_rewardCardEntity = CreateRewardCard(st.RewardCardKey);
			}
		}

		private void OpenTreasureChest(TreasureChestOpened e)
		{
			EnsureOverlayEntity();
			var st = EntityManager.GetEntity("QuestRewardOverlay").GetComponent<QuestRewardOverlayState>();

			DestroyRewardCard();
			InvalidateCaches();
			st.Message = string.Empty;
			st.TitleLine1 = "Treasure";
			st.TitleLine2 = "Unlocked!";
			st.RewardGold = e?.RewardGold ?? 0;
			st.HasCardReward = false;
			st.RewardCardKey = string.Empty;
			st.HasMedalReward = !string.IsNullOrWhiteSpace(e?.RewardMedalId);
			st.RewardMedalId = e?.RewardMedalId ?? string.Empty;
			st.DismissToLocation = false;
			st.IsOpen = true;
		}

		public void Open(string message = null, int rewardGold = 0, bool hasCardReward = false, string rewardCardKey = null)
		{
			OpenQuestReward(new ShowQuestRewardOverlay
			{
				Message = message,
				RewardGold = rewardGold,
				HasCardReward = hasCardReward,
				RewardCardKey = rewardCardKey,
			});
		}

		public void Draw()
		{
			if (_titleFont == null) return;
			var e = EntityManager.GetEntity("QuestRewardOverlay");
			var st = e?.GetComponent<QuestRewardOverlayState>();
			if (st == null || !st.IsOpen) return;

			int vw = Game1.VirtualWidth;
			int vh = Game1.VirtualHeight;
			bool showGold = st.RewardGold > 0;
			bool showCard = st.HasCardReward && _rewardCardEntity != null;
			bool showMedal = st.HasMedalReward && !string.IsNullOrEmpty(st.RewardMedalId);

			if (!_layoutValid || !_textMetricsValid)
			{
				var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
				EnsureLayout(vw, vh, showGold, showCard, showMedal, st.RewardGold, scene);
			}

			if (!_drawInBattleOrSnapshot) return;

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
			DrawLeftColumn();

			// 5. Right column: label above card
			if (_layout.ShowRightColumn)
			{
				DrawStageLabel();
				DrawRightColumnCard(showCard);
				DrawRightColumnMedal(showMedal, st.RewardMedalId);
			}

			// 6. Proceed button
			var btn = EntityManager.GetEntity("QuestRewardProceedButton");
			bool hovered = btn?.GetComponent<UIElement>()?.IsHovered ?? false;
			DrawProceedButton(hovered);
		}

		private QuestRewardLayout ComputeLayout(int vw, int vh, bool showGold, bool showCard, bool showMedal, LayoutSignature sig)
		{
			int modalW = sig.ModalWidth;
			if (showGold && !showCard && !showMedal)
			{
				modalW = sig.GoldOnlyModalWidth;
			}
			else if (!showGold && !showCard && !showMedal)
			{
				modalW = System.Math.Max(sig.LeftColWidth + sig.BorderThickness * 2, 320);
			}

			int modalH = System.Math.Max(200, sig.ModalHeight);
			int border = System.Math.Max(1, sig.BorderThickness);
			int modalX = (vw - modalW) / 2;
			int modalY = (vh - modalH) / 2;

			var modal = new Rectangle(modalX, modalY, modalW, modalH);
			var content = new Rectangle(
				modal.X + border,
				modal.Y + border,
				System.Math.Max(1, modal.Width - border * 2),
				System.Math.Max(1, modal.Height - border * 2));

			int footerH = sig.FooterPadding * 2 + System.Math.Max(30, sig.ButtonHeight);
			int bodyH = System.Math.Max(1, content.Height - footerH);
			var footer = new Rectangle(content.X, content.Y + bodyH, content.Width, footerH);
			var body = new Rectangle(content.X, content.Y, content.Width, bodyH);

			bool showRightColumn = showCard || showMedal;
			int leftW = showRightColumn ? System.Math.Min(sig.LeftColWidth, body.Width) : body.Width;
			var leftColumn = new Rectangle(body.X, body.Y, leftW, body.Height);
			var leftInner = new Rectangle(
				leftColumn.X + sig.LeftPaddingX,
				leftColumn.Y + sig.LeftPaddingTop,
				System.Math.Max(1, leftColumn.Width - sig.LeftPaddingX * 2),
				System.Math.Max(1, leftColumn.Height - sig.LeftPaddingTop - sig.LeftPaddingBottom));

			Rectangle divider = Rectangle.Empty;
			Rectangle rightColumn = Rectangle.Empty;
			Rectangle rightInner = Rectangle.Empty;
			Vector2 cardCenter = Vector2.Zero;
			Rectangle medalPreviewRect = Rectangle.Empty;
			float stageLabelH = 0f;

			if (showRightColumn)
			{
				int rightX = body.X + leftW;
				int rightW = System.Math.Max(1, body.Width - leftW);
				divider = new Rectangle(rightX - 1, body.Y, 1, body.Height);
				rightColumn = new Rectangle(rightX, body.Y, rightW, body.Height);
				rightInner = new Rectangle(
					rightColumn.X + sig.RightPaddingX,
					rightColumn.Y + sig.RightPaddingTop,
					System.Math.Max(1, rightColumn.Width - sig.RightPaddingX * 2),
					System.Math.Max(1, rightColumn.Height - sig.RightPaddingTop - sig.RightPaddingBottom));

				float labelTop = rightColumn.Y + sig.RightPaddingTop;
				stageLabelH = _bodyFont != null
					? _bodyFont.MeasureString(StageLabelText).Y * sig.StageLabelScale
					: 16f;
				float labelBottom = labelTop + stageLabelH;
				float cardTop = labelBottom + sig.StageLabelGap;
				float cardHalfH = sig.CardHeight * sig.CardPreviewScale / 2f + sig.CardOffsetYExtra * sig.CardPreviewScale;
				cardCenter = new Vector2(
					rightInner.X + rightInner.Width / 2f + sig.CardPreviewOffsetX,
					cardTop + cardHalfH + sig.CardPreviewOffsetY);

				if (showMedal)
				{
					int medalSize = System.Math.Max(40, MedalPreviewSize);
					int medalX = rightInner.X + (rightInner.Width - medalSize) / 2;
					int medalY = (int)(cardTop + sig.CardPreviewOffsetY);
					medalPreviewRect = new Rectangle(medalX, medalY, medalSize, medalSize);
				}
			}

			int bw = System.Math.Max(60, sig.ButtonWidth);
			int bh = System.Math.Max(30, sig.ButtonHeight);
			int bx = content.X + (content.Width - bw) / 2;
			int by = footer.Y + sig.FooterPadding;
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
				MedalPreviewRect = medalPreviewRect,
				StageLabelHeight = stageLabelH,
				ShowRightColumn = showRightColumn,
				ShowGold = showGold,
				ShowCard = showCard,
				ShowMedal = showMedal
			};
		}

		private void DrawLeftColumn()
		{
			var m = _textMetrics;
			string line1 = _layoutSignature.TitleLine1;
			string line2 = _layoutSignature.TitleLine2;

			_spriteBatch.DrawString(_titleFont, line1, m.TitleLine1Pos, TitleColor, 0f, Vector2.Zero, TitleScale, SpriteEffects.None, 0f);
			_spriteBatch.DrawString(_titleFont, line2, m.TitleLine2Pos, TitleColor, 0f, Vector2.Zero, TitleScale, SpriteEffects.None, 0f);

			int centerX = _layout.LeftInner.Center.X;
			DrawHorizontalGradientRule(centerX, m.RuleY, RedRuleWidth, RedRuleHeight);

			if (!m.HasGoldBlock) return;

			if (_bodyFont != null)
			{
				_spriteBatch.DrawString(_bodyFont, GoldLabelText, m.GoldLabelPos,
					GoldLabelColor, 0f, Vector2.Zero, GoldLabelScale, SpriteEffects.None, 0f);
			}

			DrawGoldGlow(m.GoldAmountText, m.GoldAmountPos, GoldAmountScale);
			_spriteBatch.DrawString(_titleFont, m.GoldAmountText, m.GoldAmountPos, GoldAmountColor, 0f, Vector2.Zero, GoldAmountScale, SpriteEffects.None, 0f);
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

		private void DrawRightColumnMedal(bool showMedal, string medalId)
		{
			if (!showMedal || string.IsNullOrWhiteSpace(medalId)) return;

			var r = _layout.MedalPreviewRect;
			if (r.Width <= 0 || r.Height <= 0) return;

			var center = new Vector2(r.Center.X, r.Center.Y);
			int iconSize = System.Math.Min(r.Width, r.Height);
			MedalIconRenderService.DrawMedalIcon(
				_spriteBatch,
				_graphicsDevice,
				_titleFont,
				center,
				iconSize,
				medalId,
				_content);
		}

		private void DrawStageLabel()
		{
			if (_bodyFont == null || !_layout.ShowRightColumn) return;
			_spriteBatch.DrawString(_bodyFont, StageLabelText,
				_textMetrics.StageLabelPos,
				StageLabelColor, 0f, Vector2.Zero, StageLabelScale, SpriteEffects.None, 0f);
		}

		private void DrawProceedButton(bool hovered)
		{
			var r = _layout.ProceedButton;
			var fill = hovered ? ButtonFillHover : ButtonFill;
			var border = hovered ? ButtonBorderHover : ButtonBorder;
			_spriteBatch.Draw(_pixel, r, fill);
			DrawBorder(r, border, BorderThickness);

			_spriteBatch.DrawString(_titleFont, ProceedLabelText, _textMetrics.ProceedTextPos, Color.White, 0f, Vector2.Zero, ButtonTextScale, SpriteEffects.None, 0f);
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

		private Texture2D GetGradientRuleTexture(int width, int height)
		{
			if (width < 1) width = 1;
			if (height < 1) height = 1;
			var key = (width, height);
			if (_gradientRuleCache.TryGetValue(key, out var existing) && existing != null && !existing.IsDisposed)
				return existing;

			const int strips = 9;
			int stripW = System.Math.Max(1, width / strips);
			var data = new Color[width * height];
			for (int i = 0; i < strips; i++)
			{
				float t = i / (float)(strips - 1);
				float dist = System.Math.Abs(t - 0.5f) * 2f;
				float alpha = 1f - dist;
				var c = RedRuleCenter * alpha;
				int x0 = i * stripW;
				for (int px = 0; px < stripW && x0 + px < width; px++)
				{
					for (int y = 0; y < height; y++)
						data[y * width + x0 + px] = c;
				}
			}

			var tex = new Texture2D(_graphicsDevice, width, height);
			tex.SetData(data);
			_gradientRuleCache[key] = tex;
			return tex;
		}

		private void DrawHorizontalGradientRule(int centerX, int centerY, int width, int height)
		{
			var tex = GetGradientRuleTexture(width, height);
			int half = width / 2;
			_spriteBatch.Draw(tex, new Rectangle(centerX - half, centerY, width, height), Color.White);
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
			state.HasMedalReward = false;
			state.RewardMedalId = string.Empty;
			StateSingleton.PreventClicking = false;
			DestroyRewardCard();
			InvalidateCaches();
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
