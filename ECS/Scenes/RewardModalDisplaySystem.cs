using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Data.Save;
using System.Text.Json.Nodes;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Factories;
using Crusaders30XX.ECS.Objects.Equipment;
using Crusaders30XX.ECS.Singletons;
using Crusaders30XX.ECS.Rendering;
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
		private readonly List<Entity> _rewardCardEntities = new();
		private readonly List<DeckRewardOptionView> _deckRewardOptionViews = new();
		private readonly List<Entity> _deckRewardLaneEntities = new();
		private Entity _deckRewardSkipButton;
		private Entity _rewardMedalEntity;
		private Entity _rewardEquipmentEntity;
		private readonly Dictionary<string, Texture2D> _equipmentIconCache = new();
		private QuestRewardLayout _layout;

		private bool _layoutValid;
		private bool _textMetricsValid;
		private bool _drawInBattleOrSnapshot;
		private int _cachedVw;
		private int _cachedVh;
		private bool _cachedShowGold;
		private bool _cachedShowCard;
		private bool _cachedShowMedal;
		private bool _cachedShowEquipment;
		private int _cachedRewardGold;
		private int _cachedRewardCardCount;
		private LayoutSignature _layoutSignature;
		private CachedTextMetrics _textMetrics;
		private readonly HorizontalGradientRuleCache _gradientRuleCache;

		private static readonly Color LeftColTint = new Color(0, 0, 0) * 0.35f;
		private static readonly Color ColumnDivider = new Color(255, 255, 255) * 0.15f;
		private static readonly Color GoldLabelColor = new Color(160, 128, 48);
		private static readonly Color GoldAmountColor = new Color(232, 200, 74);
		private static readonly Color StageLabelColor = new Color(200, 192, 184);

		private const string GoldLabelText = "GOLD";
		private const string StageLabelText = "REWARD";
		private const string QuestStageLabelText = "CHOOSE YOUR REWARD";
		private const string ProceedLabelText = "Proceed";
		private const string SkipRewardLabelText = "Skip Reward";
		private const int MaxRewardCardChoices = 2;

		[DebugEditable(DisplayName = "Z Order", Step = 10, Min = 0, Max = 100000)]
		public int ZOrder { get; set; } = 52000;

		[DebugEditable(DisplayName = "Modal Width", Step = 10, Min = 200, Max = 1600)]
		public int ModalWidth { get; set; } = 920;
		[DebugEditable(DisplayName = "Quest Reward Modal Height", Step = 10, Min = 200, Max = 1200)]
		public int QuestRewardModalHeight { get; set; } = 1030;
		[DebugEditable(DisplayName = "Treasure Chest Modal Height", Step = 10, Min = 200, Max = 1200)]
		public int TreasureChestModalHeight { get; set; } = 520;
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
		[DebugEditable(DisplayName = "Card Choice Gap", Step = 2, Min = 0, Max = 240)]
		public int CardChoiceGap { get; set; } = 32;
		[DebugEditable(DisplayName = "Card Select Anim (s)", Step = 0.01f, Min = 0.05f, Max = 2f)]
		public float CardSelectionAnimationSeconds { get; set; } = 0.55f;

		[DebugEditable(DisplayName = "Medal Preview Size", Step = 2, Min = 40, Max = 400)]
		public int MedalPreviewSize { get; set; } = 180;
		[DebugEditable(DisplayName = "Equipment Name Scale", Step = 0.01f, Min = 0.05f, Max = 0.5f)]
		public float EquipmentNameScale { get; set; } = 0.14f;

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

		[DebugEditable(DisplayName = "Deck Lane Card Scale", Step = 0.01f, Min = 0.1f, Max = 1f)]
		public float DeckLaneCardScale { get; set; } = 0.64f;
		[DebugEditable(DisplayName = "Deck Lane Gap", Step = 1, Min = 0, Max = 40)]
		public int DeckLaneGap { get; set; } = 6;
		[DebugEditable(DisplayName = "Deck Lane Pair Width", Step = 5, Min = 200, Max = 700)]
		public int DeckLanePairWidth { get; set; } = 555;
		[DebugEditable(DisplayName = "Deck Lane Meta Width", Step = 5, Min = 40, Max = 180)]
		public int DeckLaneMetaWidth { get; set; } = 86;

		private struct DeckRewardOptionView
		{
			public Entity Lane;
			public Entity OutgoingCard;
			public Entity IncomingCard;
		}

		private struct DeckRewardOfferLayout
		{
			public Rectangle Modal;
			public Rectangle Content;
			public Rectangle Masthead;
			public Rectangle Stage;
			public Rectangle Footer;
			public Rectangle SkipButton;
			public Rectangle[] Lanes;
			public Vector2[] OutgoingCardCenters;
			public Vector2[] IncomingCardCenters;
			public Vector2[] ArrowCenters;
		}

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
			public Vector2[] CardChoiceCenters;
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
			public int QuestRewardModalHeight;
			public int TreasureChestModalHeight;
			public bool IsTreasureChest;
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
			public int CardChoiceGap;
			public int FooterPadding;
			public int ButtonWidth;
			public int ButtonHeight;
			public float ButtonTextScale;
			public int CardWidth;
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
			_gradientRuleCache = new HorizontalGradientRuleCache(gd);
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
			EventManager.Subscribe<LoadSceneEvent>(OnLoadScene);
			EventManager.Subscribe<DeleteCachesEvent>(OnDeleteCaches);
		}

		private void OnLoadScene(LoadSceneEvent e)
		{
			if (e.Scene != SceneId.Location) return;

			var state = EntityManager.GetEntity("QuestRewardOverlay")?.GetComponent<QuestRewardOverlayState>();
			if (state != null && state.DismissInProgress)
			{
				CloseOverlay(state);
				return;
			}

			if (state?.IsOpen == true) return;

			var pendingOffer = SaveCache.GetPendingDeckRewardOffer();
			if (pendingOffer?.options == null || pendingOffer.options.Count == 0) return;

			OpenQuestReward(new ShowQuestRewardOverlay
			{
				RewardGold = pendingOffer.rewardGold,
				HasCardReward = true,
				DeckRewardOffer = pendingOffer
			});
		}

		private void OnDeleteCaches(DeleteCachesEvent _)
		{
			InvalidateCaches();
			_gradientRuleCache.DisposeAll();
			DestroyRewardCards();
			DestroyDeckRewardControls();
			DestroyRewardMedal();
			DestroyRewardEquipment();
		}

		private void InvalidateCaches()
		{
			_layoutValid = false;
			_textMetricsValid = false;
		}

		private static bool IsTreasureChestOverlay(QuestRewardOverlayState state) =>
			string.Equals(state?.TitleLine1, "Treasure", System.StringComparison.Ordinal);

		private LayoutSignature CaptureLayoutSignature()
		{
			var overlayState = EntityManager.GetEntity("QuestRewardOverlay")?.GetComponent<QuestRewardOverlayState>();
			var settings = EntityManager.GetEntitiesWithComponent<CardVisualSettings>().FirstOrDefault()?.GetComponent<CardVisualSettings>();
			return new LayoutSignature
			{
				ModalWidth = ModalWidth,
				QuestRewardModalHeight = QuestRewardModalHeight,
				TreasureChestModalHeight = TreasureChestModalHeight,
				IsTreasureChest = IsTreasureChestOverlay(overlayState),
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
				CardChoiceGap = CardChoiceGap,
				FooterPadding = FooterPadding,
				ButtonWidth = ButtonWidth,
				ButtonHeight = ButtonHeight,
				ButtonTextScale = ButtonTextScale,
				CardWidth = settings?.CardWidth ?? 250,
				CardHeight = settings?.CardHeight ?? 340,
				CardOffsetYExtra = settings?.CardOffsetYExtra ?? 0
			};
		}

		private bool NeedsLayoutRebuild(int vw, int vh, bool showGold, bool showCard, bool showMedal, bool showEquipment, int rewardGold, int rewardCardCount)
		{
			if (!_layoutValid) return true;
			if (vw != _cachedVw || vh != _cachedVh) return true;
			if (showGold != _cachedShowGold || showCard != _cachedShowCard || showMedal != _cachedShowMedal || showEquipment != _cachedShowEquipment || rewardGold != _cachedRewardGold || rewardCardCount != _cachedRewardCardCount) return true;
			var sig = CaptureLayoutSignature();
			return !sig.Equals(_layoutSignature);
		}

		private void EnsureLayout(int vw, int vh, bool showGold, bool showCard, bool showMedal, bool showEquipment, int rewardGold, int rewardCardCount, SceneState scene)
		{
			if (!NeedsLayoutRebuild(vw, vh, showGold, showCard, showMedal, showEquipment, rewardGold, rewardCardCount)) return;

			_cachedVw = vw;
			_cachedVh = vh;
			_cachedShowGold = showGold;
			_cachedShowCard = showCard;
			_cachedShowMedal = showMedal;
			_cachedShowEquipment = showEquipment;
			_cachedRewardGold = rewardGold;
			_cachedRewardCardCount = rewardCardCount;
			_layoutSignature = CaptureLayoutSignature();
			_drawInBattleOrSnapshot = scene != null
				&& (scene.Current == SceneId.Battle
					|| scene.Current == SceneId.Location
					|| scene.Current == SceneId.Snapshot);

			_layout = ComputeLayout(vw, vh, showGold, showCard, showMedal || showEquipment, rewardCardCount, _layoutSignature);
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
				string stageLabel = GetStageLabelText();
				var stageSize = _bodyFont.MeasureString(stageLabel) * StageLabelScale;
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
			InputContextService.EnsureContext(
				EntityManager,
				overlayEntity,
				"overlay.quest-reward",
				720,
				state.IsOpen);

			ui.IsInteractable = state.IsOpen;
			ui.LayerType = state.IsOpen ? UILayerType.Overlay : UILayerType.Default;
			ui.Bounds = state.IsOpen
				? new Rectangle(0, 0, Game1.VirtualWidth, Game1.VirtualHeight)
				: new Rectangle(0, 0, 0, 0);

			if (!state.IsOpen)
			{
				HideProceedButton();
				StateSingleton.PreventClicking = false;
				return;
			}

			var scene = sceneEntity.GetComponent<SceneState>();
			StateSingleton.PreventClicking = scene != null && scene.Current == SceneId.Location;

			int vw = Game1.VirtualWidth;
			int vh = Game1.VirtualHeight;
			bool showGold = state.RewardGold > 0;
			int rewardCardCount = _rewardCardEntities.Count;
			bool showCard = state.HasCardReward && rewardCardCount > 0;
			bool showMedal = state.HasMedalReward && !string.IsNullOrEmpty(state.RewardMedalId);
			bool showEquipment = state.HasEquipmentReward && !string.IsNullOrEmpty(state.RewardEquipmentId);
			EnsureLayout(vw, vh, showGold, showCard, showMedal, showEquipment, state.RewardGold, rewardCardCount, scene);

			var overlayT = overlayEntity.GetComponent<Transform>();
			if (overlayT != null) overlayT.ZOrder = ZOrder;

			if (state.HasDeckRewardOffer)
			{
				HideProceedButton();
				UpdateDeckRewardOfferControls(state, scene);
				return;
			}

			if (showCard)
			{
				HideProceedButton();
				SyncRewardCardHitboxes(state);
				UpdateRewardCardSelection(state, gameTime);
				return;
			}

			if (showMedal && _rewardMedalEntity != null)
			{
				var medalT = _rewardMedalEntity.GetComponent<Transform>();
				var medalUi = _rewardMedalEntity.GetComponent<UIElement>();
				if (medalT != null) medalT.ZOrder = ZOrder + 1;
				if (medalUi != null) medalUi.Bounds = _layout.MedalPreviewRect;
			}
			else if (_rewardMedalEntity != null)
			{
				var medalUi = _rewardMedalEntity.GetComponent<UIElement>();
				if (medalUi != null) medalUi.Bounds = Rectangle.Empty;
			}

			if (showEquipment && _rewardEquipmentEntity != null)
			{
				var equipT = _rewardEquipmentEntity.GetComponent<Transform>();
				var equipUi = _rewardEquipmentEntity.GetComponent<UIElement>();
				if (equipT != null) equipT.ZOrder = ZOrder + 1;
				if (equipUi != null) equipUi.Bounds = _layout.MedalPreviewRect;
			}
			else if (_rewardEquipmentEntity != null)
			{
				var equipUi = _rewardEquipmentEntity.GetComponent<UIElement>();
				if (equipUi != null) equipUi.Bounds = Rectangle.Empty;
			}

			var btn = EnsureProceedButton();
			var btnUi = btn?.GetComponent<UIElement>();
			if (btnUi != null)
			{
				btnUi.Bounds = _layout.ProceedButton;
				btnUi.IsInteractable = !state.DismissInProgress;
				btnUi.IsHidden = false;
				btnUi.LayerType = UILayerType.Overlay;
				var btnHotKey = btn.GetComponent<HotKey>();
				if (btnHotKey != null) btnHotKey.IsActive = !state.DismissInProgress;
				if (state.DismissInProgress)
				{
					btnUi.IsClicked = false;
					return;
				}
				if (btnUi.IsClicked)
				{
					btnUi.IsClicked = false;
					bool dismissToLocation = state.DismissToLocation;
					if (dismissToLocation)
					{
						state.DismissInProgress = true;
						btnUi.IsInteractable = false;
						if (btnHotKey != null) btnHotKey.IsActive = false;
						EventManager.Publish(new ShowTransition { Scene = SceneId.Location });
					}
					else
					{
						CloseOverlay(state);
					}
				}
			}
		}

		private void OpenQuestReward(ShowQuestRewardOverlay e)
		{
			EnsureOverlayEntity();
			var st = EntityManager.GetEntity("QuestRewardOverlay").GetComponent<QuestRewardOverlayState>();

			DestroyRewardCards();
			DestroyDeckRewardControls();
			DestroyRewardMedal();
			DestroyRewardEquipment();
			InvalidateCaches();
			if (!string.IsNullOrEmpty(e?.Message)) st.Message = e.Message;
			st.TitleLine1 = string.IsNullOrWhiteSpace(e?.TitleLine1) ? TitleLine1 : e.TitleLine1;
			st.TitleLine2 = string.IsNullOrWhiteSpace(e?.TitleLine2) ? TitleLine2 : e.TitleLine2;
			st.RewardGold = e?.RewardGold ?? 0;
			st.HasCardReward = e?.HasCardReward ?? false;
			st.RewardCardKey = e?.RewardCardKey ?? string.Empty;
			st.RewardCardKeys = NormalizeRewardCardKeys(e);
			st.DeckRewardOffer = CloneDeckRewardOffer(e?.DeckRewardOffer);
			st.HasMedalReward = false;
			st.RewardMedalId = string.Empty;
			st.HasEquipmentReward = false;
			st.RewardEquipmentId = string.Empty;
			st.DismissToLocation = true;
			st.DismissInProgress = false;
			st.CardSelectionInProgress = false;
			st.SelectedRewardCardIndex = -1;
			st.CardSelectionElapsedSeconds = 0f;
			st.IsOpen = true;

			if (st.HasDeckRewardOffer)
			{
				CreateDeckRewardOfferViews(st.DeckRewardOffer);
				st.HasCardReward = _deckRewardOptionViews.Count > 0;
			}
			else if (st.HasCardReward)
			{
				foreach (var cardKey in st.RewardCardKeys.Take(MaxRewardCardChoices))
				{
					var rewardCard = CreateRewardCard(cardKey);
					if (rewardCard != null) _rewardCardEntities.Add(rewardCard);
				}
				st.HasCardReward = _rewardCardEntities.Count > 0;
			}
		}

		private void OpenTreasureChest(TreasureChestOpened e)
		{
			EnsureOverlayEntity();
			var st = EntityManager.GetEntity("QuestRewardOverlay").GetComponent<QuestRewardOverlayState>();

			DestroyRewardCards();
			DestroyDeckRewardControls();
			DestroyRewardMedal();
			DestroyRewardEquipment();
			InvalidateCaches();
			st.Message = string.Empty;
			st.TitleLine1 = "Treasure";
			st.TitleLine2 = "Unlocked!";
			st.RewardGold = e?.RewardGold ?? 0;
			st.HasCardReward = false;
			st.RewardCardKey = string.Empty;
			st.RewardCardKeys = new List<string>();
			st.DeckRewardOffer = null;
			st.HasMedalReward = !string.IsNullOrWhiteSpace(e?.RewardMedalId);
			st.RewardMedalId = e?.RewardMedalId ?? string.Empty;
			st.HasEquipmentReward = !string.IsNullOrWhiteSpace(e?.RewardEquipmentId);
			st.RewardEquipmentId = e?.RewardEquipmentId ?? string.Empty;
			st.DismissToLocation = false;
			st.DismissInProgress = false;
			st.CardSelectionInProgress = false;
			st.SelectedRewardCardIndex = -1;
			st.CardSelectionElapsedSeconds = 0f;
			st.IsOpen = true;

			if (st.HasMedalReward && !string.IsNullOrEmpty(st.RewardMedalId))
			{
				_rewardMedalEntity = CreateRewardMedalHitbox(st.RewardMedalId);
			}
			else if (st.HasEquipmentReward && !string.IsNullOrEmpty(st.RewardEquipmentId))
			{
				_rewardEquipmentEntity = CreateRewardEquipmentHitbox(st.RewardEquipmentId);
			}
		}

		public void Open(
			string message = null,
			int rewardGold = 0,
			bool hasCardReward = false,
			string rewardCardKey = null,
			List<string> rewardCardKeys = null,
			DeckRewardOfferSave deckRewardOffer = null)
		{
			OpenQuestReward(new ShowQuestRewardOverlay
			{
				Message = message,
				RewardGold = rewardGold,
				HasCardReward = hasCardReward,
				RewardCardKey = rewardCardKey,
				RewardCardKeys = rewardCardKeys ?? new List<string>(),
				DeckRewardOffer = deckRewardOffer
			});
		}

		public static bool IsOverlayOpen(EntityManager entityManager)
		{
			var st = entityManager.GetEntity("QuestRewardOverlay")?.GetComponent<QuestRewardOverlayState>();
			return st != null && st.IsOpen;
		}

		private static List<string> NormalizeRewardCardKeys(ShowQuestRewardOverlay e)
		{
			var keys = new List<string>();
			if (e?.RewardCardKeys != null)
			{
				foreach (var key in e.RewardCardKeys)
				{
					if (!string.IsNullOrWhiteSpace(key)) keys.Add(key);
				}
			}
			if (keys.Count == 0 && !string.IsNullOrWhiteSpace(e?.RewardCardKey))
			{
				keys.Add(e.RewardCardKey);
			}
			return keys
				.Distinct(System.StringComparer.OrdinalIgnoreCase)
				.Take(MaxRewardCardChoices)
				.ToList();
		}

		private static DeckRewardOfferSave CloneDeckRewardOffer(DeckRewardOfferSave offer)
		{
			if (offer == null) return null;
			var clone = new DeckRewardOfferSave
			{
				rewardGold = offer.rewardGold,
				options = new List<DeckRewardOfferOptionSave>()
			};
			if (offer.options == null) return clone;
			foreach (var option in offer.options)
			{
				if (option == null) continue;
				clone.options.Add(new DeckRewardOfferOptionSave
				{
					kind = option.kind ?? string.Empty,
					loadoutIndex = option.loadoutIndex,
					outgoingCardKey = option.outgoingCardKey ?? string.Empty,
					incomingCardKey = option.incomingCardKey ?? string.Empty,
					upgradedCardKey = option.upgradedCardKey ?? string.Empty,
				});
			}
			return clone;
		}

		public void Draw()
		{
			if (_titleFont == null || !IsOverlayOpen(EntityManager)) return;
			var e = EntityManager.GetEntity("QuestRewardOverlay");
			var st = e.GetComponent<QuestRewardOverlayState>();

			int vw = Game1.VirtualWidth;
			int vh = Game1.VirtualHeight;
			bool showGold = st.RewardGold > 0;
			int rewardCardCount = _rewardCardEntities.Count;
			bool showCard = st.HasCardReward && rewardCardCount > 0;
			bool showMedal = st.HasMedalReward && !string.IsNullOrEmpty(st.RewardMedalId);
			bool showEquipment = st.HasEquipmentReward && !string.IsNullOrEmpty(st.RewardEquipmentId);

			if (st.HasDeckRewardOffer)
			{
				var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
				bool canDraw = scene != null
					&& (scene.Current == SceneId.Battle
						|| scene.Current == SceneId.Location
						|| scene.Current == SceneId.Snapshot);
				if (canDraw)
				{
					DrawDeckRewardOffer(st, vw, vh);
				}
				return;
			}

			if (!_layoutValid || !_textMetricsValid)
			{
				var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
				EnsureLayout(vw, vh, showGold, showCard, showMedal, showEquipment, st.RewardGold, rewardCardCount, scene);
			}

			if (!_drawInBattleOrSnapshot) return;

			ModalOverlayChrome.DrawDim(_spriteBatch, _pixel, vw, vh, DimAlpha);
			ModalOverlayChrome.DrawDropShadow(_spriteBatch, _pixel, _layout.Modal, DropShadowOffsetY, ModalOverlayPalette.DropShadow);

			_spriteBatch.Draw(_pixel, _layout.Modal, ModalOverlayPalette.ModalFill);
			_spriteBatch.Draw(_pixel, _layout.LeftColumn, LeftColTint);
			if (_layout.ShowRightColumn)
			{
				_spriteBatch.Draw(_pixel, _layout.Divider, ColumnDivider);
			}
			if (_layout.Footer.Height > 0)
			{
				_spriteBatch.Draw(_pixel, _layout.Footer, ModalOverlayPalette.FooterFill);
				_spriteBatch.Draw(_pixel, new Rectangle(_layout.Footer.X, _layout.Footer.Y, _layout.Footer.Width, 1), ModalOverlayPalette.FooterBorderTop);
			}
			ModalOverlayChrome.DrawInsetHighlight(_spriteBatch, _pixel, _layout.Content);
			ModalOverlayChrome.DrawBorder(_spriteBatch, _pixel, _layout.Modal, ModalOverlayPalette.PanelBorder, BorderThickness);

			// 4. Left column text
			DrawLeftColumn();

			// 5. Right column: label above card
			if (_layout.ShowRightColumn)
			{
				DrawStageLabel();
				DrawRightColumnCard(showCard);
				DrawRightColumnMedal(showMedal, st.RewardMedalId);
				DrawRightColumnEquipment(showEquipment, st.RewardEquipmentId);
			}

			if (!showCard)
			{
				var btn = EntityManager.GetEntity("QuestRewardProceedButton");
				bool hovered = btn?.GetComponent<UIElement>()?.IsHovered ?? false;
				DrawProceedButton(hovered);
			}
		}

		private QuestRewardLayout ComputeLayout(int vw, int vh, bool showGold, bool showCard, bool showMedal, int rewardCardCount, LayoutSignature sig)
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

			int modalH = System.Math.Max(200, sig.IsTreasureChest ? sig.TreasureChestModalHeight : sig.QuestRewardModalHeight);
			int border = System.Math.Max(1, sig.BorderThickness);
			int modalX = (vw - modalW) / 2;
			int modalY = (vh - modalH) / 2;

			var modal = new Rectangle(modalX, modalY, modalW, modalH);
			var content = new Rectangle(
				modal.X + border,
				modal.Y + border,
				System.Math.Max(1, modal.Width - border * 2),
				System.Math.Max(1, modal.Height - border * 2));

			int footerH = showCard ? 0 : sig.FooterPadding * 2 + System.Math.Max(30, sig.ButtonHeight);
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
			Vector2[] cardChoiceCenters = System.Array.Empty<Vector2>();
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
				if (showCard)
				{
					int cardCount = System.Math.Max(1, rewardCardCount);
					cardChoiceCenters = new Vector2[cardCount];
					if (cardCount == 1)
					{
						cardChoiceCenters[0] = cardCenter;
					}
					else
					{
						float cardW = sig.CardWidth * sig.CardPreviewScale;
						float totalW = cardW * cardCount + sig.CardChoiceGap * (cardCount - 1);
						float startX = cardCenter.X - totalW / 2f + cardW / 2f;
						for (int i = 0; i < cardCount; i++)
						{
							cardChoiceCenters[i] = new Vector2(
								startX + i * (cardW + sig.CardChoiceGap),
								cardCenter.Y);
						}
					}
				}

				if (showMedal)
				{
					int medalSize = System.Math.Max(40, MedalPreviewSize);
					int medalX = rightInner.X + (rightInner.Width - medalSize) / 2;
					int medalY = (int)(cardTop + sig.CardPreviewOffsetY);
					medalPreviewRect = new Rectangle(medalX, medalY, medalSize, medalSize);
				}
			}

			var proceedButton = Rectangle.Empty;
			if (!showCard)
			{
				int bw = System.Math.Max(60, sig.ButtonWidth);
				int bh = System.Math.Max(30, sig.ButtonHeight);
				int bx = content.X + (content.Width - bw) / 2;
				int by = footer.Y + sig.FooterPadding;
				proceedButton = new Rectangle(bx, by, bw, bh);
			}

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
				CardChoiceCenters = cardChoiceCenters,
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

			_spriteBatch.DrawString(_titleFont, line1, m.TitleLine1Pos, ModalOverlayPalette.TitleColor, 0f, Vector2.Zero, TitleScale, SpriteEffects.None, 0f);
			_spriteBatch.DrawString(_titleFont, line2, m.TitleLine2Pos, ModalOverlayPalette.TitleColor, 0f, Vector2.Zero, TitleScale, SpriteEffects.None, 0f);

			int centerX = _layout.LeftInner.Center.X;
			_gradientRuleCache.DrawRule(_spriteBatch, centerX, m.RuleY, RedRuleWidth, RedRuleHeight);

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
			if (!showCard || _rewardCardEntities.Count == 0) return;
			var st = EntityManager.GetEntity("QuestRewardOverlay")?.GetComponent<QuestRewardOverlayState>();
			for (int i = 0; i < _rewardCardEntities.Count; i++)
			{
				var card = _rewardCardEntities[i];
				if (card == null || !card.IsActive) continue;
				float scale = GetRewardCardDisplayScale(i, st);
				if (scale <= 0.001f) continue;
				EventManager.Publish(new CardRenderScaledRotatedEvent
				{
					Card = card,
					Position = GetRewardCardCenter(i),
					Scale = scale
				});
			}
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
			_spriteBatch.DrawString(_bodyFont, GetStageLabelText(),
				_textMetrics.StageLabelPos,
				StageLabelColor, 0f, Vector2.Zero, StageLabelScale, SpriteEffects.None, 0f);
		}

		private string GetStageLabelText()
		{
			var state = EntityManager.GetEntity("QuestRewardOverlay")?.GetComponent<QuestRewardOverlayState>();
			return state?.DismissToLocation == true ? QuestStageLabelText : StageLabelText;
		}

		private void DrawProceedButton(bool hovered)
		{
			ModalOverlayChrome.DrawActionButton(
				_spriteBatch,
				_pixel,
				_layout.ProceedButton,
				hovered,
				BorderThickness,
				_titleFont,
				ProceedLabelText,
				_textMetrics.ProceedTextPos,
				ButtonTextScale,
				Color.White);
		}

		private void DrawGoldGlow(string text, Vector2 pos, float scale)
		{
			var glow = GoldAmountColor * 0.35f;
			_spriteBatch.DrawString(_titleFont, text, pos + new Vector2(-2, 0), glow, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
			_spriteBatch.DrawString(_titleFont, text, pos + new Vector2(2, 0), glow, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
			_spriteBatch.DrawString(_titleFont, text, pos + new Vector2(0, -2), glow, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
			_spriteBatch.DrawString(_titleFont, text, pos + new Vector2(0, 2), glow, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
		}

		private void DrawDeckRewardOffer(QuestRewardOverlayState state, int vw, int vh)
		{
			if (state?.DeckRewardOffer?.options == null) return;
			var layout = ComputeDeckRewardOfferLayout(vw, vh, state.DeckRewardOffer.options.Count);

			ModalOverlayChrome.DrawDim(_spriteBatch, _pixel, vw, vh, DimAlpha);
			ModalOverlayChrome.DrawDropShadow(_spriteBatch, _pixel, layout.Modal, DropShadowOffsetY, ModalOverlayPalette.DropShadow);
			_spriteBatch.Draw(_pixel, layout.Modal, ModalOverlayPalette.ModalFill);
			_spriteBatch.Draw(_pixel, layout.Masthead, new Color(0, 0, 0) * 0.20f);
			_spriteBatch.Draw(_pixel, layout.Footer, ModalOverlayPalette.FooterFill);
			_spriteBatch.Draw(_pixel, new Rectangle(layout.Masthead.X, layout.Masthead.Bottom, layout.Masthead.Width, 1), ModalOverlayPalette.FooterBorderTop);
			_spriteBatch.Draw(_pixel, new Rectangle(layout.Footer.X, layout.Footer.Y, layout.Footer.Width, 1), ModalOverlayPalette.FooterBorderTop);
			ModalOverlayChrome.DrawInsetHighlight(_spriteBatch, _pixel, layout.Content);
			ModalOverlayChrome.DrawBorder(_spriteBatch, _pixel, layout.Modal, ModalOverlayPalette.PanelBorder, BorderThickness);

			DrawDeckRewardMasthead(layout, state.RewardGold);
			DrawDeckRewardStageLabel(layout);

			for (int i = 0; i < state.DeckRewardOffer.options.Count && i < layout.Lanes.Length; i++)
			{
				var option = state.DeckRewardOffer.options[i];
				if (option == null) continue;
				bool isUpgrade = string.Equals(option.kind, DeckRewardOfferKinds.Upgrade, System.StringComparison.OrdinalIgnoreCase);
				bool hovered = i < _deckRewardOptionViews.Count
					&& (_deckRewardOptionViews[i].Lane?.GetComponent<UIElement>()?.IsHovered ?? false);
				DrawDeckRewardLane(layout, option, i, isUpgrade, hovered);

				if (i < _deckRewardOptionViews.Count)
				{
					var view = _deckRewardOptionViews[i];
					EventManager.Publish(new CardRenderScaledRotatedEvent
					{
						Card = view.OutgoingCard,
						Position = layout.OutgoingCardCenters[i],
						Scale = DeckLaneCardScale
					});
					EventManager.Publish(new CardRenderScaledRotatedEvent
					{
						Card = view.IncomingCard,
						Position = layout.IncomingCardCenters[i],
						Scale = DeckLaneCardScale
					});
				}
			}

			var skipUi = _deckRewardSkipButton?.GetComponent<UIElement>();
			bool skipHovered = skipUi?.IsHovered ?? false;
			var skipSize = _bodyFont?.MeasureString(SkipRewardLabelText) * 0.15f ?? Vector2.Zero;
			var skipPos = new Vector2(
				layout.SkipButton.Center.X - skipSize.X / 2f,
				layout.SkipButton.Center.Y - skipSize.Y / 2f);
			ModalOverlayChrome.DrawActionButton(
				_spriteBatch,
				_pixel,
				layout.SkipButton,
				skipHovered,
				BorderThickness,
				_bodyFont,
				SkipRewardLabelText,
				skipPos,
				0.15f,
				StageLabelColor);
		}

		private void DrawDeckRewardMasthead(DeckRewardOfferLayout layout, int rewardGold)
		{
			DrawCenteredString(_titleFont, "Quest Complete", new Vector2(layout.Masthead.Center.X, layout.Masthead.Y + 16), 0.24f, ModalOverlayPalette.TitleColor);
			_gradientRuleCache.DrawRule(_spriteBatch, layout.Masthead.Center.X, layout.Masthead.Y + 56, 64, 2);

			string goldText = rewardGold > 0 ? $"Reward +{rewardGold:N0}" : "Reward";
			Vector2 rowCenter = new Vector2(layout.Masthead.Center.X, layout.Masthead.Y + 72);
			if (_bodyFont != null)
			{
				var goldSize = _bodyFont.MeasureString(goldText) * 0.12f;
				var prompt = "Pick one reward";
				var promptSize = _bodyFont.MeasureString(prompt) * 0.10f;
				float totalW = goldSize.X + 24f + promptSize.X;
				float x = rowCenter.X - totalW / 2f;
				_spriteBatch.DrawString(_bodyFont, goldText, new Vector2(x, rowCenter.Y), GoldAmountColor, 0f, Vector2.Zero, 0.12f, SpriteEffects.None, 0f);
				int dividerX = (int)(x + goldSize.X + 12f);
				_spriteBatch.Draw(_pixel, new Rectangle(dividerX, (int)rowCenter.Y, 1, 16), ColumnDivider);
				_spriteBatch.DrawString(_bodyFont, prompt, new Vector2(dividerX + 12f, rowCenter.Y + 1f), StageLabelColor, 0f, Vector2.Zero, 0.10f, SpriteEffects.None, 0f);
			}
		}

		private void DrawDeckRewardStageLabel(DeckRewardOfferLayout layout)
		{
			DrawCenteredString(_bodyFont, "Deck Reward", new Vector2(layout.Stage.Center.X, layout.Stage.Y + 14), StageLabelScale, StageLabelColor);
		}

		private void DrawDeckRewardLane(DeckRewardOfferLayout layout, DeckRewardOfferOptionSave option, int index, bool isUpgrade, bool hovered)
		{
			var lane = layout.Lanes[index];
			var fill = isUpgrade
				? (hovered ? new Color(46, 38, 4) * 0.88f : new Color(22, 18, 2) * 0.88f)
				: (hovered ? new Color(50, 0, 0) * 0.86f : new Color(12, 0, 0) * 0.80f);
			var border = isUpgrade
				? (hovered ? GoldAmountColor * 0.65f : GoldAmountColor * 0.28f)
				: (hovered ? new Color(196, 30, 58) * 0.70f : ColumnDivider);
			_spriteBatch.Draw(_pixel, lane, fill);
			_spriteBatch.Draw(_pixel, new Rectangle(lane.X, lane.Y, 3, lane.Height), isUpgrade ? GoldAmountColor * 0.55f : new Color(196, 30, 58) * 0.55f);
			ModalOverlayChrome.DrawBorder(_spriteBatch, _pixel, lane, border, 1);

			string laneNum = (index + 1).ToString("00");
			string tag = isUpgrade ? "Upgrade" : "Exchange";
			float metaX = lane.X + 14;
			DrawString(_titleFont, laneNum, new Vector2(metaX, lane.Center.Y - 28), 0.18f, ModalOverlayPalette.TitleColor);
			DrawString(_bodyFont, tag, new Vector2(metaX, lane.Center.Y + 4), 0.08f, isUpgrade ? GoldLabelColor : StageLabelColor);

			string leftLabel = isUpgrade ? "Current" : "Remove";
			string rightLabel = isUpgrade ? "Upgraded" : "Gain";
			DrawCenteredString(_bodyFont, leftLabel, new Vector2(layout.OutgoingCardCenters[index].X, lane.Y + 8), 0.08f, StageLabelColor);
			DrawCenteredString(_bodyFont, rightLabel, new Vector2(layout.IncomingCardCenters[index].X, lane.Y + 8), 0.08f, isUpgrade ? GoldLabelColor : StageLabelColor);

			string arrow = isUpgrade ? "^" : ">>";
			string arrowLabel = isUpgrade ? "Upgrade" : "Trade";
			DrawCenteredString(_titleFont, arrow, layout.ArrowCenters[index] + new Vector2(0, -18), isUpgrade ? 0.24f : 0.20f, isUpgrade ? GoldAmountColor : new Color(196, 30, 58));
			DrawCenteredString(_bodyFont, arrowLabel, layout.ArrowCenters[index] + new Vector2(0, 20), 0.09f, isUpgrade ? GoldLabelColor : StageLabelColor);
		}

		private void DrawString(SpriteFont font, string text, Vector2 position, float scale, Color color)
		{
			if (font == null || string.IsNullOrEmpty(text)) return;
			_spriteBatch.DrawString(font, text, position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
		}

		private void DrawCenteredString(SpriteFont font, string text, Vector2 centerTop, float scale, Color color)
		{
			if (font == null || string.IsNullOrEmpty(text)) return;
			var size = font.MeasureString(text) * scale;
			var pos = new Vector2(centerTop.X - size.X / 2f, centerTop.Y);
			_spriteBatch.DrawString(font, text, pos, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
		}

		private void CloseOverlay(QuestRewardOverlayState state)
		{
			state.IsOpen = false;
			state.DismissInProgress = false;
			state.RewardGold = 0;
			state.HasCardReward = false;
			state.RewardCardKey = string.Empty;
			state.RewardCardKeys = new List<string>();
			state.DeckRewardOffer = null;
			state.HasMedalReward = false;
			state.RewardMedalId = string.Empty;
			state.HasEquipmentReward = false;
			state.RewardEquipmentId = string.Empty;
			state.CardSelectionInProgress = false;
			state.SelectedRewardCardIndex = -1;
			state.CardSelectionElapsedSeconds = 0f;
			StateSingleton.PreventClicking = false;
			HideProceedButton();
			DestroyRewardCards();
			DestroyDeckRewardControls();
			DestroyRewardMedal();
			DestroyRewardEquipment();
			InvalidateCaches();
		}

		private void HideProceedButton()
		{
			var btn = EntityManager.GetEntity("QuestRewardProceedButton");
			if (btn == null) return;

			var ui = btn.GetComponent<UIElement>();
			if (ui != null)
			{
				ui.Bounds = Rectangle.Empty;
				ui.IsInteractable = false;
				ui.IsHidden = true;
				ui.IsClicked = false;
				ui.LayerType = UILayerType.Overlay;
			}

			var hotKey = btn.GetComponent<HotKey>();
			if (hotKey != null) hotKey.IsActive = false;
		}

		private void DestroyRewardCards()
		{
			foreach (var card in _rewardCardEntities.ToList())
			{
				if (card != null)
				{
					EntityManager.DestroyEntity(card.Id);
				}
			}
			_rewardCardEntities.Clear();
			_deckRewardOptionViews.Clear();
		}

		private void DestroyDeckRewardControls()
		{
			foreach (var lane in _deckRewardLaneEntities.ToList())
			{
				if (lane != null) EntityManager.DestroyEntity(lane.Id);
			}
			_deckRewardLaneEntities.Clear();

			if (_deckRewardSkipButton != null)
			{
				EntityManager.DestroyEntity(_deckRewardSkipButton.Id);
				_deckRewardSkipButton = null;
			}
		}

		private void CreateDeckRewardOfferViews(DeckRewardOfferSave offer)
		{
			if (offer?.options == null) return;
			for (int i = 0; i < offer.options.Count; i++)
			{
				var option = offer.options[i];
				if (option == null) continue;

				string incomingKey = string.Equals(option.kind, DeckRewardOfferKinds.Upgrade, System.StringComparison.OrdinalIgnoreCase)
					? option.upgradedCardKey
					: option.incomingCardKey;
				var outgoing = CreateRewardCard(option.outgoingCardKey);
				var incoming = CreateRewardCard(incomingKey);
				if (outgoing == null || incoming == null)
				{
					if (outgoing != null) EntityManager.DestroyEntity(outgoing.Id);
					if (incoming != null) EntityManager.DestroyEntity(incoming.Id);
					continue;
				}

				_rewardCardEntities.Add(outgoing);
				_rewardCardEntities.Add(incoming);
				var lane = EnsureDeckRewardLaneEntity(i);
				_deckRewardOptionViews.Add(new DeckRewardOptionView
				{
					Lane = lane,
					OutgoingCard = outgoing,
					IncomingCard = incoming
				});
			}
		}

		private Entity EnsureDeckRewardLaneEntity(int index)
		{
			while (_deckRewardLaneEntities.Count <= index)
			{
				int next = _deckRewardLaneEntities.Count;
				var ent = EntityManager.CreateEntity($"QuestRewardDeckLane_{next}");
				EntityManager.AddComponent(ent, new Transform { Position = Vector2.Zero, ZOrder = ZOrder + 3 + next });
				EntityManager.AddComponent(ent, new UIElement
				{
					Bounds = Rectangle.Empty,
					IsInteractable = false,
					EventType = UIElementEventType.None,
					LayerType = UILayerType.Overlay
				});
				EntityManager.AddComponent(ent, ParallaxLayer.GetUIParallaxLayer());
				EntityManager.AddComponent(ent, new DontDestroyOnLoad());
				InputContextService.EnsureMember(EntityManager, ent, "overlay.quest-reward");
				_deckRewardLaneEntities.Add(ent);
			}
			return _deckRewardLaneEntities[index];
		}

		private Entity EnsureDeckRewardSkipButton()
		{
			if (_deckRewardSkipButton != null && _deckRewardSkipButton.IsActive) return _deckRewardSkipButton;

			_deckRewardSkipButton = EntityManager.CreateEntity("QuestRewardSkipButton");
			EntityManager.AddComponent(_deckRewardSkipButton, new Transform { Position = Vector2.Zero, ZOrder = ZOrder + 4 });
			EntityManager.AddComponent(_deckRewardSkipButton, new UIElement
			{
				Bounds = Rectangle.Empty,
				IsInteractable = false,
				EventType = UIElementEventType.None,
				LayerType = UILayerType.Overlay
			});
			EntityManager.AddComponent(_deckRewardSkipButton, new HotKey { Button = FaceButton.Y, IsActive = false });
			EntityManager.AddComponent(_deckRewardSkipButton, ParallaxLayer.GetUIParallaxLayer());
			EntityManager.AddComponent(_deckRewardSkipButton, new DontDestroyOnLoad());
			InputContextService.EnsureMember(EntityManager, _deckRewardSkipButton, "overlay.quest-reward");
			return _deckRewardSkipButton;
		}

		private void UpdateDeckRewardOfferControls(QuestRewardOverlayState state, SceneState scene)
		{
			if (state?.DeckRewardOffer?.options == null) return;
			var layout = ComputeDeckRewardOfferLayout(Game1.VirtualWidth, Game1.VirtualHeight, state.DeckRewardOffer.options.Count);

			for (int i = 0; i < _deckRewardOptionViews.Count; i++)
			{
				var view = _deckRewardOptionViews[i];
				var laneUi = view.Lane?.GetComponent<UIElement>();
				if (laneUi != null)
				{
					laneUi.Bounds = i < layout.Lanes.Length ? layout.Lanes[i] : Rectangle.Empty;
					laneUi.IsInteractable = state.IsOpen && !state.DismissInProgress;
					laneUi.LayerType = UILayerType.Overlay;
					if (laneUi.IsClicked)
					{
						laneUi.IsClicked = false;
						if (QuestCardRewardService.ApplyPendingOfferOption(i))
						{
							CompleteDeckRewardOfferResolution(state, scene);
							return;
						}
					}
				}

				PreparePreviewCard(view.OutgoingCard, i * 2, state);
				PreparePreviewCard(view.IncomingCard, i * 2 + 1, state);
			}

			var skip = EnsureDeckRewardSkipButton();
			var skipUi = skip.GetComponent<UIElement>();
			if (skipUi != null)
			{
				skipUi.Bounds = layout.SkipButton;
				skipUi.IsInteractable = state.IsOpen && !state.DismissInProgress;
				skipUi.LayerType = UILayerType.Overlay;
				if (skipUi.IsClicked)
				{
					skipUi.IsClicked = false;
					QuestCardRewardService.SkipPendingOffer();
					CompleteDeckRewardOfferResolution(state, scene);
				}
			}
			var hotKey = skip.GetComponent<HotKey>();
			if (hotKey != null) hotKey.IsActive = state.IsOpen && !state.DismissInProgress;
		}

		private void PreparePreviewCard(Entity card, int zOffset, QuestRewardOverlayState state)
		{
			if (card == null) return;
			var ui = card.GetComponent<UIElement>();
			if (ui != null)
			{
				ui.Bounds = Rectangle.Empty;
				ui.IsInteractable = false;
				ui.IsClicked = false;
				ui.LayerType = UILayerType.Overlay;
			}
			var transform = card.GetComponent<Transform>();
			if (transform != null)
			{
				transform.ZOrder = ZOrder + 1 + zOffset;
			}
			InputContextService.EnsureMember(EntityManager, card, "overlay.quest-reward");
		}

		private void CompleteDeckRewardOfferResolution(QuestRewardOverlayState state, SceneState scene)
		{
			if (state == null) return;
			if (state.DismissToLocation && scene?.Current == SceneId.Battle)
			{
				state.DismissInProgress = true;
				DisableDeckRewardControls();
				EventManager.Publish(new ShowTransition { Scene = SceneId.Location });
				return;
			}

			CloseOverlay(state);
		}

		private void DisableDeckRewardControls()
		{
			foreach (var lane in _deckRewardLaneEntities)
			{
				var ui = lane?.GetComponent<UIElement>();
				if (ui == null) continue;
				ui.IsInteractable = false;
				ui.IsClicked = false;
			}
			var skipUi = _deckRewardSkipButton?.GetComponent<UIElement>();
			if (skipUi != null)
			{
				skipUi.IsInteractable = false;
				skipUi.IsClicked = false;
			}
			var hotKey = _deckRewardSkipButton?.GetComponent<HotKey>();
			if (hotKey != null) hotKey.IsActive = false;
		}

		private DeckRewardOfferLayout ComputeDeckRewardOfferLayout(int vw, int vh, int laneCount)
		{
			int modalW = System.Math.Max(600, ModalWidth);
			int modalH = System.Math.Max(560, QuestRewardModalHeight);
			int modalX = (vw - modalW) / 2;
			int modalY = (vh - modalH) / 2;
			var modal = new Rectangle(modalX, modalY, modalW, modalH);
			var content = new Rectangle(modal.X + BorderThickness, modal.Y + BorderThickness, modal.Width - BorderThickness * 2, modal.Height - BorderThickness * 2);
			var masthead = new Rectangle(content.X, content.Y, content.Width, 104);
			var footer = new Rectangle(content.X, content.Bottom - 88, content.Width, 88);
			var stage = new Rectangle(content.X, masthead.Bottom, content.Width, footer.Y - masthead.Bottom);

			int count = System.Math.Max(0, laneCount);
			var lanes = new Rectangle[count];
			var outgoing = new Vector2[count];
			var incoming = new Vector2[count];
			var arrows = new Vector2[count];
			if (count > 0)
			{
				int gap = System.Math.Max(0, DeckLaneGap);
				int lanesX = stage.X + 20;
				int lanesY = stage.Y + 52;
				int lanesW = stage.Width - 40;
				int lanesH = System.Math.Max(1, stage.Bottom - 12 - lanesY);
				int laneH = System.Math.Max(64, (lanesH - gap * (count - 1)) / count);
				float cardSeparation = System.Math.Max(120, DeckLanePairWidth) / 2f;
				for (int i = 0; i < count; i++)
				{
					lanes[i] = new Rectangle(lanesX, lanesY + i * (laneH + gap), lanesW, laneH);
					float pairCenterX = lanes[i].Center.X + DeckLaneMetaWidth / 2f;
					float centerY = lanes[i].Center.Y + 12;
					outgoing[i] = new Vector2(pairCenterX - cardSeparation / 2f, centerY);
					incoming[i] = new Vector2(pairCenterX + cardSeparation / 2f, centerY);
					arrows[i] = new Vector2(pairCenterX, lanes[i].Center.Y + 8);
				}
			}

			int buttonW = System.Math.Max(120, ButtonWidth - 40);
			int buttonH = System.Math.Max(40, ButtonHeight - 8);
			var skip = new Rectangle(content.Center.X - buttonW / 2, footer.Y + (footer.Height - buttonH) / 2, buttonW, buttonH);

			return new DeckRewardOfferLayout
			{
				Modal = modal,
				Content = content,
				Masthead = masthead,
				Stage = stage,
				Footer = footer,
				SkipButton = skip,
				Lanes = lanes,
				OutgoingCardCenters = outgoing,
				IncomingCardCenters = incoming,
				ArrowCenters = arrows
			};
		}

		private void DestroyRewardMedal()
		{
			if (_rewardMedalEntity == null)
			{
				var orphan = EntityManager.GetEntity("QuestRewardMedalHitbox");
				if (orphan != null) EntityManager.DestroyEntity(orphan.Id);
				return;
			}
			EntityManager.DestroyEntity(_rewardMedalEntity.Id);
			_rewardMedalEntity = null;
		}

		private void DestroyRewardEquipment()
		{
			if (_rewardEquipmentEntity == null)
			{
				var orphan = EntityManager.GetEntity("QuestRewardEquipmentHitbox");
				if (orphan != null) EntityManager.DestroyEntity(orphan.Id);
				return;
			}
			EntityManager.DestroyEntity(_rewardEquipmentEntity.Id);
			_rewardEquipmentEntity = null;
		}

		private void SyncRewardCardHitboxes(QuestRewardOverlayState state)
		{
			for (int i = 0; i < _rewardCardEntities.Count; i++)
			{
				var card = _rewardCardEntities[i];
				if (card == null) continue;
				var ui = card.GetComponent<UIElement>();
				var t = card.GetComponent<Transform>();
				if (ui == null) continue;
				if (t != null) t.ZOrder = ZOrder + 1 + i;
				float scale = GetRewardCardDisplayScale(i, state);
				ui.Bounds = scale > 0.001f
					? GetCardVisualRectScaled(GetRewardCardCenter(i), scale)
					: Rectangle.Empty;
				ui.IsInteractable = state != null && state.IsOpen && !state.CardSelectionInProgress && !state.DismissInProgress;
				ui.LayerType = UILayerType.Overlay;
				InputContextService.EnsureMember(
					EntityManager,
					card,
					"overlay.quest-reward");
			}
		}

		private void UpdateRewardCardSelection(QuestRewardOverlayState state, GameTime gameTime)
		{
			if (state == null) return;

			if (state.CardSelectionInProgress)
			{
				state.CardSelectionElapsedSeconds += (float)gameTime.ElapsedGameTime.TotalSeconds;
				if (state.CardSelectionElapsedSeconds >= System.Math.Max(0.05f, CardSelectionAnimationSeconds) && !state.DismissInProgress)
				{
					state.DismissInProgress = true;
					if (state.DismissToLocation)
					{
						EventManager.Publish(new ShowTransition { Scene = SceneId.Location });
					}
					else
					{
						CloseOverlay(state);
					}
				}
				return;
			}

			for (int i = 0; i < _rewardCardEntities.Count; i++)
			{
				var card = _rewardCardEntities[i];
				if (card == null) continue;
				var ui = card.GetComponent<UIElement>();
				if (ui == null) continue;

				if (ui.IsHovered)
				{
					var hk = card.GetComponent<HotKey>();
					if (hk == null)
					{
						EntityManager.AddComponent(card, new HotKey { Button = FaceButton.X, RequiresHold = true, Position = HotKeyPosition.Below });
					}
					else
					{
						hk.Button = FaceButton.X;
						hk.RequiresHold = true;
						hk.Position = HotKeyPosition.Below;
						hk.IsActive = true;
					}
				}
				else
				{
					var hk = card.GetComponent<HotKey>();
					if (hk != null) EntityManager.RemoveComponent<HotKey>(card);
				}

				if (!ui.IsClicked) continue;
				ui.IsClicked = false;
				SelectRewardCard(state, i);
				break;
			}
		}

		private void SelectRewardCard(QuestRewardOverlayState state, int selectedIndex)
		{
			if (state == null || state.CardSelectionInProgress) return;
			if (selectedIndex < 0 || selectedIndex >= state.RewardCardKeys.Count) return;

			string selectedKey = state.RewardCardKeys[selectedIndex];
			var grant = QuestCardRewardService.GrantCard(selectedKey);
			if (!grant.Granted) return;

			state.CardSelectionInProgress = true;
			state.SelectedRewardCardIndex = selectedIndex;
			state.RewardCardKey = selectedKey;
			state.CardSelectionElapsedSeconds = 0f;

			for (int i = 0; i < _rewardCardEntities.Count; i++)
			{
				var card = _rewardCardEntities[i];
				if (card == null) continue;
				var ui = card.GetComponent<UIElement>();
				if (ui != null)
				{
					ui.IsClicked = false;
					ui.IsInteractable = false;
				}
				var hk = card.GetComponent<HotKey>();
				if (hk != null) EntityManager.RemoveComponent<HotKey>(card);
			}

			var selected = _rewardCardEntities.ElementAtOrDefault(selectedIndex);
			if (selected != null)
			{
				EventManager.Publish(new JigglePulseEvent
				{
					Target = selected,
					Config = new JigglePulseConfig
					{
						PulseDurationSeconds = System.Math.Max(0.05f, CardSelectionAnimationSeconds),
						PulseScaleAmplitude = 0.12f,
						JiggleDegrees = 7f,
						PulseFrequencyHz = 6f
					}
				});
			}
		}

		private Vector2 GetRewardCardCenter(int index)
		{
			if (_layout.CardChoiceCenters != null && index >= 0 && index < _layout.CardChoiceCenters.Length)
			{
				return _layout.CardChoiceCenters[index];
			}
			return _layout.CardCenter;
		}

		private float GetRewardCardDisplayScale(int index, QuestRewardOverlayState state)
		{
			float scale = CardPreviewScale;
			if (state != null && state.CardSelectionInProgress && state.SelectedRewardCardIndex >= 0 && index != state.SelectedRewardCardIndex)
			{
				float t = MathHelper.Clamp(state.CardSelectionElapsedSeconds / System.Math.Max(0.05f, CardSelectionAnimationSeconds), 0f, 1f);
				float shrink = 1f - SmoothStep(t);
				scale *= shrink;
			}

			var transform = index >= 0 && index < _rewardCardEntities.Count
				? _rewardCardEntities[index]?.GetComponent<Transform>()
				: null;
			if (transform != null)
			{
				scale *= transform.Scale.X;
			}
			return scale;
		}

		private static float SmoothStep(float t)
		{
			t = MathHelper.Clamp(t, 0f, 1f);
			return t * t * (3f - 2f * t);
		}

		private Rectangle GetCardVisualRectScaled(Vector2 position, float scale)
		{
			var settings = EntityManager.GetEntitiesWithComponent<CardVisualSettings>().FirstOrDefault()?.GetComponent<CardVisualSettings>();
			if (settings == null)
			{
				int w = (int)System.Math.Round(200 * scale);
				int h = (int)System.Math.Round(300 * scale);
				return new Rectangle((int)position.X - w / 2, (int)position.Y - h / 2, w, h);
			}
			int rw = (int)System.Math.Round(settings.CardWidth * scale);
			int rh = (int)System.Math.Round(settings.CardHeight * scale);
			int offsetY = (int)System.Math.Round(settings.CardOffsetYExtra * scale);
			return new Rectangle(
				(int)position.X - rw / 2,
				(int)position.Y - (rh / 2 + offsetY),
				rw,
				rh);
		}

		private Entity CreateRewardCard(string cardKey)
		{
			if (!RunDeckService.TryParseCardKey(cardKey, out var cardId, out var color, out var isUpgraded)) return null;
			var created = EntityFactory.CreateCardFromDefinition(
				EntityManager,
				cardId,
				color,
				suppressStatDeltaDisplay: true,
				isUpgraded: isUpgraded);
			if (created == null) return null;

			var ui = created.GetComponent<UIElement>();
			var t = created.GetComponent<Transform>();
			if (ui != null)
			{
				ui.IsInteractable = true;
				ui.EventType = UIElementEventType.None;
				ui.LayerType = UILayerType.Overlay;
			}
			if (t != null) t.ZOrder = ZOrder + 1;
			return created;
		}

		private Entity CreateRewardMedalHitbox(string medalId)
		{
			var medal = MedalFactory.Create(medalId);
			if (medal == null) return null;

			var ent = EntityManager.CreateEntity("QuestRewardMedalHitbox");
			EntityManager.AddComponent(ent, new Transform { Position = Vector2.Zero, ZOrder = ZOrder + 1 });
			EntityManager.AddComponent(ent, new UIElement
			{
				Bounds = Rectangle.Empty,
				IsInteractable = false,
				Tooltip = $"{medal.Name}\n\n{medal.Text}",
				TooltipPosition = TooltipPosition.Below,
				LayerType = UILayerType.Overlay
			});
			EntityManager.AddComponent(ent, ParallaxLayer.GetUIParallaxLayer());
			EntityManager.AddComponent(ent, new DontDestroyOnLoad());
			InputContextService.EnsureMember(
				EntityManager,
				ent,
				"overlay.quest-reward");
			return ent;
		}

		private Entity CreateRewardEquipmentHitbox(string equipmentId)
		{
			var equipment = EquipmentFactory.Create(equipmentId);
			if (equipment == null) return null;

			var ent = EntityManager.CreateEntity("QuestRewardEquipmentHitbox");
			EntityManager.AddComponent(ent, new Transform { Position = Vector2.Zero, ZOrder = ZOrder + 1 });
			EntityManager.AddComponent(ent, new UIElement
			{
				Bounds = Rectangle.Empty,
				IsInteractable = true,
				Tooltip = EquipmentService.GetTooltipText(equipment, EquipmentTooltipType.Shop),
				TooltipPosition = TooltipPosition.Below,
				LayerType = UILayerType.Overlay
			});
			EntityManager.AddComponent(ent, ParallaxLayer.GetUIParallaxLayer());
			EntityManager.AddComponent(ent, new DontDestroyOnLoad());
			InputContextService.EnsureMember(
				EntityManager,
				ent,
				"overlay.quest-reward");
			return ent;
		}

		private void DrawRightColumnEquipment(bool showEquipment, string equipmentId)
		{
			if (!showEquipment || string.IsNullOrWhiteSpace(equipmentId)) return;

			var equipment = EquipmentFactory.Create(equipmentId);
			if (equipment == null) return;

			var r = _layout.MedalPreviewRect;
			if (r.Width <= 0 || r.Height <= 0) return;

			var icon = GetEquipmentSlotIcon(equipment.Slot);
			if (icon != null)
			{
				int iconSize = System.Math.Min(r.Width, r.Height);
				var iconRect = new Rectangle(
					r.X + (r.Width - iconSize) / 2,
					r.Y,
					iconSize,
					iconSize);
				_spriteBatch.Draw(icon, iconRect, Color.White);
			}

			if (_bodyFont == null || string.IsNullOrWhiteSpace(equipment.Name)) return;

			Vector2 nameSize = _bodyFont.MeasureString(equipment.Name) * EquipmentNameScale;
			float nameX = r.X + r.Width / 2f - nameSize.X / 2f;
			float nameY = r.Bottom + 8f;
			_spriteBatch.DrawString(
				_bodyFont,
				equipment.Name,
				new Vector2(nameX, nameY),
				Color.White,
				0f,
				Vector2.Zero,
				EquipmentNameScale,
				SpriteEffects.None,
				0f);
		}

		private Texture2D GetEquipmentSlotIcon(EquipmentSlot slot)
		{
			string key = slot.ToString().ToLowerInvariant();
			if (_equipmentIconCache.TryGetValue(key, out var cached)) return cached;
			try
			{
				cached = _content?.Load<Texture2D>(key);
			}
			catch
			{
				cached = null;
			}
			_equipmentIconCache[key] = cached;
			return cached;
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
				InputContextService.EnsureContext(
					EntityManager,
					e,
					"overlay.quest-reward",
					720,
					false);
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
				EntityManager.AddComponent(ent, new Transform { Position = Vector2.Zero, ZOrder = ZOrder + 2 });
				EntityManager.AddComponent(ent, new UIElement { Bounds = Rectangle.Empty, IsInteractable = false, IsHidden = true, LayerType = UILayerType.Overlay });
				EntityManager.AddComponent(ent, new HotKey { Button = FaceButton.Y, IsActive = false });
				InputContextService.EnsureMember(
					EntityManager,
					ent,
					"overlay.quest-reward");
				EntityManager.AddComponent(ent, ParallaxLayer.GetUIParallaxLayer());
			}
			else
			{
				var t = ent.GetComponent<Transform>();
				if (t != null) t.ZOrder = ZOrder + 2;
				var ui = ent.GetComponent<UIElement>();
				if (ui != null) ui.LayerType = UILayerType.Overlay;
			}
			return ent;
		}
	}
}
