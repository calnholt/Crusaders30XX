using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Save;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Factories;
using Crusaders30XX.ECS.Objects.Events;
using Crusaders30XX.ECS.Rendering;
using Crusaders30XX.ECS.Singletons;
using Crusaders30XX.ECS.Utils;
using Crusaders30XX.ECS.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("Narrative Event Modal")]
	public class NarrativeEventModalDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly SpriteFont _titleFont = FontSingleton.TitleFont;
		private readonly SpriteFont _bodyFont = FontSingleton.ChakraPetchFont;
		private readonly Texture2D _pixel;
		private readonly HorizontalGradientRuleCache _gradientRuleCache;

		private EventBase _activeEvent;
		private int _snapshotVisibleOptionCap;
		private bool _forceSnapshotDraw;
		private NarrativeEventLayout _layout;
		private CachedTextMetrics _textMetrics;
		private readonly List<(int OptionIndex, string Text)> _visibleOptions = new();

		private bool _layoutValid;
		private bool _textMetricsValid;
		private bool _drawOnLocationOrSnapshot;
		private int _cachedVw;
		private int _cachedVh;
		private int _cachedVisibleCount;

		private struct NarrativeEventLayout
		{
			public Rectangle Modal;
			public Rectangle Content;
			public Rectangle Body;
			public Rectangle BodyInner;
			public Rectangle Footer;
			public int RuleY;
			public Rectangle[] OptionButtons;
		}

		private struct CachedTextMetrics
		{
			public Vector2 TitlePos;
			public Vector2 TitleSize;
			public List<string> BodyLines;
			public List<Vector2> BodyLinePositions;
			public List<(string Label, Vector2 Pos, float Scale)> OptionLabels;
		}

		[DebugEditable(DisplayName = "Z Order", Step = 10, Min = 0, Max = 100000)]
		public int ZOrder { get; set; } = 52010;

		[DebugEditable(DisplayName = "Modal Width", Step = 10, Min = 200, Max = 1600)]
		public int ModalWidth { get; set; } = 920;
		[DebugEditable(DisplayName = "Modal Height", Step = 10, Min = 200, Max = 1200)]
		public int ModalHeight { get; set; } = 520;
		[DebugEditable(DisplayName = "Border Thickness", Step = 1, Min = 1, Max = 8)]
		public int BorderThickness { get; set; } = 2;
		[DebugEditable(DisplayName = "Dim Alpha", Step = 5, Min = 0, Max = 255)]
		public int DimAlpha { get; set; } = 140;
		[DebugEditable(DisplayName = "Drop Shadow Offset Y", Step = 1, Min = 0, Max = 40)]
		public int DropShadowOffsetY { get; set; } = 16;

		[DebugEditable(DisplayName = "Body Padding Top", Step = 2, Min = 0, Max = 120)]
		public int BodyPaddingTop { get; set; } = 40;
		[DebugEditable(DisplayName = "Body Padding X", Step = 2, Min = 0, Max = 80)]
		public int BodyPaddingX { get; set; } = 40;
		[DebugEditable(DisplayName = "Body Padding Bottom", Step = 2, Min = 0, Max = 120)]
		public int BodyPaddingBottom { get; set; } = 28;
		[DebugEditable(DisplayName = "Body Stack Gap", Step = 2, Min = 0, Max = 80)]
		public int BodyStackGap { get; set; } = 20;

		[DebugEditable(DisplayName = "Title Scale", Step = 0.01f, Min = 0.1f, Max = 2f)]
		public float TitleScale { get; set; } = 0.281f;
		[DebugEditable(DisplayName = "Body Text Scale", Step = 0.01f, Min = 0.05f, Max = 1f)]
		public float BodyTextScale { get; set; } = 0.172f;
		[DebugEditable(DisplayName = "Title Offset Y", Step = 1, Min = -80, Max = 80)]
		public int TitleOffsetY { get; set; } = 0;
		[DebugEditable(DisplayName = "Body Offset Y", Step = 1, Min = -80, Max = 80)]
		public int BodyOffsetY { get; set; } = 0;

		[DebugEditable(DisplayName = "Red Rule Width", Step = 2, Min = 20, Max = 200)]
		public int RedRuleWidth { get; set; } = 80;
		[DebugEditable(DisplayName = "Red Rule Height", Step = 1, Min = 1, Max = 12)]
		public int RedRuleHeight { get; set; } = 3;

		[DebugEditable(DisplayName = "Footer Padding", Step = 2, Min = 0, Max = 60)]
		public int FooterPadding { get; set; } = 20;
		[DebugEditable(DisplayName = "Option Gap", Step = 2, Min = 0, Max = 60)]
		public int OptionGap { get; set; } = 20;
		[DebugEditable(DisplayName = "Option Min Height", Step = 2, Min = 30, Max = 120)]
		public int OptionMinHeight { get; set; } = 64;
		[DebugEditable(DisplayName = "Option Text Scale", Step = 0.01f, Min = 0.05f, Max = 1f)]
		public float OptionTextScale { get; set; } = 0.133f;

		public NarrativeEventModalDisplaySystem(EntityManager entityManager, GraphicsDevice gd, SpriteBatch sb, ContentManager content) : base(entityManager)
		{
			_graphicsDevice = gd;
			_spriteBatch = sb;
			_pixel = new Texture2D(gd, 1, 1);
			_pixel.SetData(new[] { Color.White });
			_gradientRuleCache = new HorizontalGradientRuleCache(gd);

			EventManager.Subscribe<ShowNarrativeEventOverlay>(OnShowNarrativeEventOverlay);
			EventManager.Subscribe<NarrativeEventOverlayClosedEvent>(OnNarrativeEventOverlayClosed);
			EventManager.Subscribe<DeleteCachesEvent>(_ =>
			{
				InvalidateCaches();
				_gradientRuleCache.DisposeAll();
			});
		}

		private void OnShowNarrativeEventOverlay(ShowNarrativeEventOverlay e)
		{
			if (e == null || IsOverlayOpen(EntityManager)) return;
			Open(e.RunMapEventId, e.EventTypeId, snapshotVisibleOptionCap: 0);
		}

		private void OnNarrativeEventOverlayClosed(NarrativeEventOverlayClosedEvent e)
		{
			ClimbEventService.TryCompletePendingEvent(EntityManager, e?.EventTypeId);
			CloseOverlay();
		}

		public void OpenForSnapshot(string eventTypeId, int visibleOptionCount = 0)
		{
			_forceSnapshotDraw = true;
			Open(string.Empty, eventTypeId, visibleOptionCount);
		}

		private void Open(string runMapEventId, string eventTypeId, int snapshotVisibleOptionCap)
		{
			if (string.IsNullOrWhiteSpace(eventTypeId)) return;

			var narrativeEvent = EventFactory.Create(eventTypeId);
			if (narrativeEvent == null)
			{
				System.Diagnostics.Debug.WriteLine($"[NarrativeEventModalDisplaySystem] Unknown event type: {eventTypeId}");
				return;
			}

			EnsureOverlayEntity();
			var st = EntityManager.GetEntity("NarrativeEventOverlay").GetComponent<NarrativeEventOverlayState>();
			st.RunMapEventId = runMapEventId ?? string.Empty;
			st.EventTypeId = eventTypeId;
			st.IsOpen = true;

			_activeEvent = narrativeEvent;
			_activeEvent.Initialize(EntityManager);
			_snapshotVisibleOptionCap = snapshotVisibleOptionCap;
			InvalidateCaches();
		}

		public static bool IsOverlayOpen(EntityManager entityManager)
		{
			var st = entityManager.GetEntity("NarrativeEventOverlay")?.GetComponent<NarrativeEventOverlayState>();
			return st != null && st.IsOpen;
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<SceneState>();
		}

		protected override void UpdateEntity(Entity sceneEntity, GameTime gameTime)
		{
			var overlayEntity = EntityManager.GetEntity("NarrativeEventOverlay");
			if (overlayEntity == null) return;
			var state = overlayEntity.GetComponent<NarrativeEventOverlayState>();
			if (state == null) return;
			InputContextService.EnsureContext(
				EntityManager,
				overlayEntity,
				"overlay.narrative-event",
				730,
				state.IsOpen);

			if (!state.IsOpen)
			{
				if (!_forceSnapshotDraw)
					StateSingleton.PreventClicking = false;
				return;
			}

			var scene = sceneEntity.GetComponent<SceneState>();
			if (!_forceSnapshotDraw)
				StateSingleton.PreventClicking = scene != null && (scene.Current == SceneId.Location || scene.Current == SceneId.Climb);

			int vw = Game1.VirtualWidth;
			int vh = Game1.VirtualHeight;
			int visibleCount = CountVisibleOptions(_activeEvent, _snapshotVisibleOptionCap);
			EnsureLayout(vw, vh, visibleCount, scene);

			var overlayT = overlayEntity.GetComponent<Transform>();
			if (overlayT != null) overlayT.ZOrder = ZOrder;

			SyncOptionButtons(state, visibleCount);
		}

		private void SyncOptionButtons(NarrativeEventOverlayState state, int visibleCount)
		{
			for (int slot = 0; slot < 3; slot++)
			{
				int buttonNum = slot + 1;
				var btn = EnsureOptionButton(buttonNum);
				var btnUi = btn?.GetComponent<UIElement>();
				if (btnUi == null) continue;

				if (slot < visibleCount && slot < _visibleOptions.Count)
				{
					var entry = _visibleOptions[slot];
					btnUi.Bounds = _layout.OptionButtons != null && slot < _layout.OptionButtons.Length
						? _layout.OptionButtons[slot]
						: Rectangle.Empty;
					btnUi.IsInteractable = true;
					btnUi.IsHidden = false;

					if (btnUi.IsClicked)
					{
						btnUi.IsClicked = false;
						ResolveOption(state, entry.OptionIndex);
						return;
					}
				}
				else
				{
					btnUi.Bounds = Rectangle.Empty;
					btnUi.IsInteractable = false;
					btnUi.IsHidden = true;
				}

				var btnT = btn.GetComponent<Transform>();
				if (btnT != null) btnT.ZOrder = ZOrder + 2;
			}
		}

		private void ResolveOption(NarrativeEventOverlayState state, int optionIndex)
		{
			if (_activeEvent == null || !state.IsOpen) return;

			switch (optionIndex)
			{
				case 1: _activeEvent.OnOption1(EntityManager); break;
				case 2: _activeEvent.OnOption2(EntityManager); break;
				case 3: _activeEvent.OnOption3(EntityManager); break;
			}

			if (!string.IsNullOrWhiteSpace(state.RunMapEventId))
				SaveCache.TryCompleteRunMapEvent(state.RunMapEventId);

			EventManager.Publish(new NarrativeEventOverlayClosedEvent
			{
				RunMapEventId = state.RunMapEventId,
				EventTypeId = state.EventTypeId,
				OptionIndex = optionIndex
			});
		}

		public void Draw()
		{
			if (_titleFont == null || !IsOverlayOpen(EntityManager)) return;

			int vw = Game1.VirtualWidth;
			int vh = Game1.VirtualHeight;
			int visibleCount = CountVisibleOptions(_activeEvent, _snapshotVisibleOptionCap);

			if (!_layoutValid || !_textMetricsValid)
			{
				var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
				EnsureLayout(vw, vh, visibleCount, scene);
			}

			if (!_drawOnLocationOrSnapshot && !_forceSnapshotDraw) return;

			ModalOverlayChrome.DrawDim(_spriteBatch, _pixel, vw, vh, DimAlpha);
			ModalOverlayChrome.DrawDropShadow(_spriteBatch, _pixel, _layout.Modal, DropShadowOffsetY, ModalOverlayPalette.DropShadow);
			ModalOverlayChrome.DrawModalRegions(_spriteBatch, _pixel, _layout.Modal, _layout.Content, _layout.Footer, BorderThickness);

			DrawBodyColumn();
			DrawOptionButtons();
		}

		private void DrawBodyColumn()
		{
			var m = _textMetrics;
			if (_activeEvent == null) return;

			_spriteBatch.DrawString(_titleFont, _activeEvent.Title, m.TitlePos, ModalOverlayPalette.TitleColor,
				0f, Vector2.Zero, TitleScale, SpriteEffects.None, 0f);

			int centerX = _layout.BodyInner.Center.X;
			_gradientRuleCache.DrawRule(_spriteBatch, centerX, _layout.RuleY, RedRuleWidth, RedRuleHeight);

			if (_bodyFont == null || m.BodyLines == null) return;
			for (int i = 0; i < m.BodyLines.Count; i++)
			{
				_spriteBatch.DrawString(_bodyFont, m.BodyLines[i], m.BodyLinePositions[i], ModalOverlayPalette.BodyTextColor,
					0f, Vector2.Zero, BodyTextScale, SpriteEffects.None, 0f);
			}
		}

		private void DrawOptionButtons()
		{
			for (int i = 0; i < _visibleOptions.Count; i++)
			{
				var entry = _visibleOptions[i];
				int buttonNum = i + 1;
				var btn = EntityManager.GetEntity($"NarrativeEventOptionButton{buttonNum}");
				bool hovered = btn?.GetComponent<UIElement>()?.IsHovered ?? false;

				var rect = _layout.OptionButtons != null && i < _layout.OptionButtons.Length
					? _layout.OptionButtons[i]
					: Rectangle.Empty;

				string label = i < _textMetrics.OptionLabels.Count ? _textMetrics.OptionLabels[i].Label : entry.Text;
				Vector2 pos = i < _textMetrics.OptionLabels.Count ? _textMetrics.OptionLabels[i].Pos : Vector2.Zero;
				float scale = i < _textMetrics.OptionLabels.Count ? _textMetrics.OptionLabels[i].Scale : OptionTextScale;

				ModalOverlayChrome.DrawActionButton(
					_spriteBatch,
					_pixel,
					rect,
					hovered,
					BorderThickness,
					_bodyFont,
					label,
					pos,
					scale,
					Color.White);
			}
		}

		private void EnsureLayout(int vw, int vh, int visibleCount, SceneState scene)
		{
			if (_layoutValid && _textMetricsValid && vw == _cachedVw && vh == _cachedVh && visibleCount == _cachedVisibleCount)
			{
				return;
			}

			_cachedVw = vw;
			_cachedVh = vh;
			_cachedVisibleCount = visibleCount;
			_drawOnLocationOrSnapshot = _forceSnapshotDraw
				|| (scene != null && (scene.Current == SceneId.Location || scene.Current == SceneId.Climb || scene.Current == SceneId.Snapshot));

			int footerH = ComputeFooterHeight(visibleCount);
			var shell = ModalShellLayout.ComputeCentered(vw, vh, ModalWidth, ModalHeight, BorderThickness, footerH);

			var bodyInner = new Rectangle(
				shell.Body.X + BodyPaddingX,
				shell.Body.Y + BodyPaddingTop,
				System.Math.Max(1, shell.Body.Width - BodyPaddingX * 2),
				System.Math.Max(1, shell.Body.Height - BodyPaddingTop - BodyPaddingBottom));

			BuildVisibleOptionList(_activeEvent, _snapshotVisibleOptionCap);

			_layout = new NarrativeEventLayout
			{
				Modal = shell.Modal,
				Content = shell.Content,
				Body = shell.Body,
				BodyInner = bodyInner,
				Footer = shell.Footer
			};

			RebuildTextMetrics();
			_layoutValid = true;
			_textMetricsValid = true;
		}

		private void RebuildTextMetrics()
		{
			var metrics = new CachedTextMetrics
			{
				BodyLines = new List<string>(),
				BodyLinePositions = new List<Vector2>(),
				OptionLabels = new List<(string, Vector2, float)>()
			};

			string title = TextUtils.FilterUnsupportedGlyphs(_titleFont, _activeEvent?.Title ?? string.Empty);
			metrics.TitleSize = _titleFont.MeasureString(title) * TitleScale;
			metrics.TitlePos = new Vector2(
				_layout.BodyInner.Center.X - metrics.TitleSize.X / 2f,
				_layout.BodyInner.Y + TitleOffsetY);

			float cursorY = metrics.TitlePos.Y + metrics.TitleSize.Y + BodyStackGap;
			_layout.RuleY = (int)cursorY;
			cursorY += RedRuleHeight + BodyStackGap + BodyOffsetY;

			if (_bodyFont != null && _activeEvent != null)
			{
				string body = TextUtils.FilterUnsupportedGlyphs(_bodyFont, _activeEvent.EventText ?? string.Empty);
				metrics.BodyLines = TextUtils.WrapText(_bodyFont, body, BodyTextScale, _layout.BodyInner.Width);
				foreach (var line in metrics.BodyLines)
				{
					metrics.BodyLinePositions.Add(new Vector2(_layout.BodyInner.X, cursorY));
					cursorY += _bodyFont.MeasureString(line).Y * BodyTextScale;
				}
			}

			int footerPad = FooterPadding;
			int optionH = System.Math.Max(30, OptionMinHeight);
			int optionW = System.Math.Max(1, _layout.Footer.Width - footerPad * 2);
			int optionX = _layout.Footer.X + footerPad;
			var optionButtons = new Rectangle[3];

			for (int i = 0; i < _visibleOptions.Count; i++)
			{
				var entry = _visibleOptions[i];
				int y = _layout.Footer.Y + footerPad + i * (optionH + OptionGap);
				var rect = new Rectangle(optionX, y, optionW, optionH);
				optionButtons[i] = rect;

				string label = ClipOptionLabel(entry.Text, optionW);
				var labelSize = _bodyFont.MeasureString(label) * OptionTextScale;
				var pos = new Vector2(
					rect.Center.X - labelSize.X / 2f,
					rect.Center.Y - labelSize.Y / 2f);
				metrics.OptionLabels.Add((label, pos, OptionTextScale));
			}

			_layout.OptionButtons = optionButtons;
			_textMetrics = metrics;
		}

		private void BuildVisibleOptionList(EventBase evt, int snapshotCap)
		{
			_visibleOptions.Clear();
			if (evt == null) return;

			if (!string.IsNullOrWhiteSpace(evt.Option1Text)) _visibleOptions.Add((1, evt.Option1Text));
			if (!string.IsNullOrWhiteSpace(evt.Option2Text)) _visibleOptions.Add((2, evt.Option2Text));
			if (!string.IsNullOrWhiteSpace(evt.Option3Text)) _visibleOptions.Add((3, evt.Option3Text));

			if (snapshotCap > 0 && _visibleOptions.Count > snapshotCap)
				_visibleOptions.RemoveRange(snapshotCap, _visibleOptions.Count - snapshotCap);
		}

		private static int CountVisibleOptions(EventBase evt, int snapshotCap)
		{
			if (evt == null) return 0;
			int count = 0;
			if (!string.IsNullOrWhiteSpace(evt.Option1Text)) count++;
			if (!string.IsNullOrWhiteSpace(evt.Option2Text)) count++;
			if (!string.IsNullOrWhiteSpace(evt.Option3Text)) count++;
			if (snapshotCap > 0) count = System.Math.Min(count, snapshotCap);
			return count;
		}

		private int ComputeFooterHeight(int visibleCount)
		{
			int pad = FooterPadding;
			int optionH = System.Math.Max(30, OptionMinHeight);
			if (visibleCount <= 0) return pad * 2 + optionH;
			return pad * 2 + visibleCount * optionH + System.Math.Max(0, visibleCount - 1) * OptionGap;
		}

		private string ClipOptionLabel(string text, int maxWidthPx)
		{
			if (_bodyFont == null || string.IsNullOrEmpty(text)) return string.Empty;
			string safe = TextUtils.FilterUnsupportedGlyphs(_bodyFont, text);
			if (_bodyFont.MeasureString(safe).X * OptionTextScale <= maxWidthPx) return safe;

			string ellipsis = "...";
			for (int len = safe.Length; len > 0; len--)
			{
				string candidate = safe.Substring(0, len) + ellipsis;
				if (_bodyFont.MeasureString(candidate).X * OptionTextScale <= maxWidthPx)
					return candidate;
			}
			return ellipsis;
		}

		private void CloseOverlay()
		{
			var overlayEntity = EntityManager.GetEntity("NarrativeEventOverlay");
			var st = overlayEntity?.GetComponent<NarrativeEventOverlayState>();
			if (st == null) return;

			st.IsOpen = false;
			st.RunMapEventId = string.Empty;
			st.EventTypeId = string.Empty;
			_activeEvent = null;
			_snapshotVisibleOptionCap = 0;
			_forceSnapshotDraw = false;
			StateSingleton.PreventClicking = false;
			var context = overlayEntity.GetComponent<InputContext>();
			if (context != null)
			{
				context.IsActive = false;
			}

			for (int i = 1; i <= 3; i++)
			{
				var btnUi = EntityManager.GetEntity($"NarrativeEventOptionButton{i}")?.GetComponent<UIElement>();
				if (btnUi != null)
				{
					btnUi.Bounds = Rectangle.Empty;
					btnUi.IsInteractable = false;
					btnUi.IsHidden = true;
				}
			}
			_visibleOptions.Clear();

			InvalidateCaches();
		}

		private void InvalidateCaches()
		{
			_layoutValid = false;
			_textMetricsValid = false;
		}

		private void EnsureOverlayEntity()
		{
			var e = EntityManager.GetEntity("NarrativeEventOverlay");
			if (e == null)
			{
				e = EntityManager.CreateEntity("NarrativeEventOverlay");
				EntityManager.AddComponent(e, new Transform { Position = Vector2.Zero, ZOrder = ZOrder });
				EntityManager.AddComponent(e, new NarrativeEventOverlayState());
				InputContextService.EnsureContext(
					EntityManager,
					e,
					"overlay.narrative-event",
					730,
					false);
				EntityManager.AddComponent(e, ParallaxLayer.GetUIParallaxLayer());
				EntityManager.AddComponent(e, new DontDestroyOnLoad());
			}
			else
			{
				if (e.GetComponent<UIElement>() != null)
				{
					EntityManager.RemoveComponent<UIElement>(e);
				}
				var t = e.GetComponent<Transform>();
				if (t != null) t.ZOrder = ZOrder;
			}
		}

		private Entity EnsureOptionButton(int index)
		{
			string name = $"NarrativeEventOptionButton{index}";
			var ent = EntityManager.GetEntity(name);
			if (ent == null)
			{
				ent = EntityManager.CreateEntity(name);
				EntityManager.AddComponent(ent, new Transform { Position = Vector2.Zero, ZOrder = ZOrder + 2 });
				EntityManager.AddComponent(ent, new UIElement
				{
					Bounds = Rectangle.Empty,
					IsInteractable = false,
					IsHidden = true,
					LayerType = UILayerType.Overlay
				});
				EntityManager.AddComponent(ent, ParallaxLayer.GetUIParallaxLayer());
				InputContextService.EnsureMember(
					EntityManager,
					ent,
					"overlay.narrative-event");
				EntityManager.AddComponent(ent, new DontDestroyOnLoad());
			}
			return ent;
		}
	}
}
