using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Events;
using System.Collections.Generic;
using System;
using Crusaders30XX.ECS.Utils;
using Crusaders30XX.ECS.Singletons;
using Crusaders30XX.ECS.Objects.EnemyAttacks;
using Microsoft.Xna.Framework.Content;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Skeleton banner that displays current attack name, base damage (sum of on-hit Damage effects),
	/// and a simple list of leaf blocking conditions. Shown when there is a current planned attack.
	/// </summary>
	[DebugTab("Enemy Attack Display")]
	public class EnemyAttackDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly ContentManager _content;
		private readonly SpriteFont _titleFont = FontSingleton.TitleFont;
		private readonly SpriteFont _contentFont = FontSingleton.ContentFont;
		private readonly Texture2D _pixel;
		private readonly Texture2D _enemyAttackCornerBlTexture;
		private readonly Texture2D _enemyAttackCornerBrTexture;
		private readonly Texture2D _enemyAttackTopTexture;
		private readonly Texture2D _enemyAttackSkullTexture;
	private readonly System.Collections.Generic.Dictionary<string, Entity> _effectTooltipUiByKey = new();
	private Entity _attackTextTooltipEntity = null;
	private Rectangle _bannerRect = Rectangle.Empty;

	// Animation state
	private string _lastContextId = null;
		private float _shakeElapsedSeconds = 0f;

		// Impact animation flow (spawn centered -> impact)
		private bool _impactActive = false;
		private float _squashElapsedSeconds = 0f;
		private float _flashElapsedSeconds = 0f;
		private float _craterElapsedSeconds = 0f;

		// Prevent repeated confirm presses for the same attack context
		private readonly HashSet<string> _confirmedForContext = [];
		private bool _showBanner = false;

		private struct DebrisParticle
		{
			public Vector2 Position;
			public Vector2 Velocity;
			public float Age;
			public float Lifetime;
			public float Size;
			public Color Color;
		}
		private readonly List<DebrisParticle> _debris = new();
		private static readonly Random _rand = new();

		// Absorb tween (panel -> enemy)
		[DebugEditable(DisplayName = "Absorb Duration (s)", Step = 0.02f, Min = 0.05f, Max = 3f)]
		public float AbsorbDurationSeconds { get; set; } = 0.4f;
		[DebugEditable(DisplayName = "Absorb Target Y Offset", Step = 2, Min = -400, Max = 400)]
		public int AbsorbTargetYOffset { get; set; } = -40;
		private float _absorbElapsedSeconds = 0f;
		private bool _absorbCompleteFired = false;

		[DebugEditable(DisplayName = "Center Offset X", Step = 2, Min = -1000, Max = 1000)]
		public int OffsetX { get; set; } = 0;

		[DebugEditable(DisplayName = "Center Offset Y", Step = 2, Min = -400, Max = 400)]
		public int OffsetY { get; set; } = -300;

		[DebugEditable(DisplayName = "Panel Padding", Step = 1, Min = 4, Max = 40)]
		public int PanelPadding { get; set; } = 30;

		[DebugEditable(DisplayName = "Border Thickness", Step = 1, Min = 1, Max = 8)]
		public int BorderThickness { get; set; } = 0;

		[DebugEditable(DisplayName = "Background Alpha", Step = 5, Min = 0, Max = 255)]
		public int BackgroundAlpha { get; set; } = 160;

		[DebugEditable(DisplayName = "Title Scale", Step = 0.05f, Min = 0.05f, Max = 2.5f)]
		public float TitleScale { get; set; } = 0.2f;

		[DebugEditable(DisplayName = "Text Scale", Step = 0.05f, Min = 0.01f, Max = 2.5f)]
		public float TextScale { get; set; } = 0.138f;

		[DebugEditable(DisplayName = "Panel Max Width % of Screen", Step = 0.05f, Min = 0.1f, Max = 1f)]
		public float PanelMaxWidthPercent { get; set; } = 0.3f;

		[DebugEditable(DisplayName = "Panel Min Width % of Screen", Step = 0.01f, Min = 0f, Max = 1f)]
		public float PanelMinWidthPercent { get; set; } = 0.2f;

		[DebugEditable(DisplayName = "Line Spacing Extra", Step = 1, Min = 0, Max = 20)]
		public int LineSpacingExtra { get; set; } = 3;

		[DebugEditable(DisplayName = "Title Spacing Extra", Step = 1, Min = 0, Max = 120)]
		public int TitleSpacingExtra { get; set; } = 80;

		[DebugEditable(DisplayName = "Corner Ornament Scale", Step = 0.01f, Min = 0.1f, Max = 4f)]
		public float CornerOrnamentScale { get; set; } = 0.24f;

		[DebugEditable(DisplayName = "Corner Left Offset X", Step = 1, Min = -400, Max = 400)]
		public int CornerLeftOffsetX { get; set; } = -5;

		[DebugEditable(DisplayName = "Corner Left Offset Y", Step = 1, Min = -400, Max = 400)]
		public int CornerLeftOffsetY { get; set; } = 5;

		[DebugEditable(DisplayName = "Corner Right Offset X", Step = 1, Min = -400, Max = 400)]
		public int CornerRightOffsetX { get; set; } = 5;

		[DebugEditable(DisplayName = "Corner Right Offset Y", Step = 1, Min = -400, Max = 400)]
		public int CornerRightOffsetY { get; set; } = 5;

		[DebugEditable(DisplayName = "Top Ornament Scale", Step = 0.01f, Min = 0.1f, Max = 4f)]
		public float TopOrnamentScale { get; set; } = 0.37f;

		[DebugEditable(DisplayName = "Top Ornament Offset Y", Step = 1, Min = -400, Max = 400)]
		public int TopOrnamentOffsetY { get; set; } = 22;

		[DebugEditable(DisplayName = "Skull Scale", Step = 0.01f, Min = 0.1f, Max = 4f)]
		public float SkullScale { get; set; } = 0.15f;

		[DebugEditable(DisplayName = "Skull Vertical Offset", Step = 2, Min = -400, Max = 200)]
		public int SkullVerticalOffset { get; set; } = 30;

		// Impact animation tuning

		[DebugEditable(DisplayName = "Overshoot Intensity", Step = 0.05f, Min = 0f, Max = 3f)]
		public float OvershootIntensity { get; set; } = 0.8f; // higher = more overshoot in back-ease

		[DebugEditable(DisplayName = "Shake Duration (s)", Step = 0.05f, Min = 0f, Max = 1.5f)]
		public float ShakeDurationSeconds { get; set; } = 0.25f;

		[DebugEditable(DisplayName = "Shake Amplitude (px)", Step = 1, Min = 0, Max = 50)]
		public int ShakeAmplitudePx { get; set; } = 9;

		// (Approach phase removed)

		// Impact squash/flash/crater
		[DebugEditable(DisplayName = "Squash Duration (s)", Step = 0.02f, Min = 0.05f, Max = 1f)]
		public float SquashDurationSeconds { get; set; } = 0.2f;

		[DebugEditable(DisplayName = "Squash X Factor", Step = 0.05f, Min = 1f, Max = 2.5f)]
		public float SquashXFactor { get; set; } = 1.25f;

		[DebugEditable(DisplayName = "Squash Y Factor", Step = 0.05f, Min = 0.3f, Max = 1f)]
		public float SquashYFactor { get; set; } = 0.8f;

		[DebugEditable(DisplayName = "Impact Flash Duration (s)", Step = 0.02f, Min = 0f, Max = 1f)]
		public float FlashDurationSeconds { get; set; } = 0.12f;

		[DebugEditable(DisplayName = "Impact Flash Max Alpha", Step = 5, Min = 0, Max = 255)]
		public int FlashMaxAlpha { get; set; } = 180;

		[DebugEditable(DisplayName = "Crater Duration (s)", Step = 0.02f, Min = 0f, Max = 1.5f)]
		public float CraterDurationSeconds { get; set; } = 0.45f;

		[DebugEditable(DisplayName = "Crater Max Expand (px)", Step = 2, Min = 0, Max = 200)]
		public int CraterMaxExpandPx { get; set; } = 24;

		[DebugEditable(DisplayName = "Crater Max Alpha", Step = 5, Min = 0, Max = 255)]
		public int CraterMaxAlpha { get; set; } = 120;

		// Confirm button tuning
		[DebugEditable(DisplayName = "Confirm Button Offset Y", Step = 2, Min = -600, Max = 600)]
		public int ConfirmButtonOffsetY { get; set; } = 8;
		[DebugEditable(DisplayName = "Confirm Button Width", Step = 2, Min = 20, Max = 600)]
		public int ConfirmButtonWidth { get; set; } = 154;
		[DebugEditable(DisplayName = "Confirm Button Height", Step = 2, Min = 16, Max = 200)]
		public int ConfirmButtonHeight { get; set; } = 42;
		[DebugEditable(DisplayName = "Confirm Button Text Scale", Step = 0.05f, Min = 0.3f, Max = 3f)]
		public float ConfirmButtonTextScale { get; set; } = 0.175f;
		[DebugEditable(DisplayName = "Confirm Button Z", Step = 10, Min = -100000, Max = 100000)]
		public int ConfirmButtonZ { get; set; } = 20000;

		// Debris
		[DebugEditable(DisplayName = "Debris Count", Step = 1, Min = 0, Max = 100)]
		public int DebrisCount { get; set; } = 100;

		[DebugEditable(DisplayName = "Debris Speed Min", Step = 5, Min = 0, Max = 600)]
		public int DebrisSpeedMin { get; set; } = 210;

		[DebugEditable(DisplayName = "Debris Speed Max", Step = 5, Min = 0, Max = 800)]
		public int DebrisSpeedMax { get; set; } = 420;

		[DebugEditable(DisplayName = "Debris Lifetime (s)", Step = 0.05f, Min = 0f, Max = 2f)]
		public float DebrisLifetimeSeconds { get; set; } = 0.8f;

		public EnemyAttackDisplaySystem(EntityManager em, GraphicsDevice gd, SpriteBatch sb, ContentManager content) : base(em)
		{
			_graphicsDevice = gd;
			_spriteBatch = sb;
			_content = content;
			_pixel = new Texture2D(gd, 1, 1);
			_pixel.SetData(new[] { Color.White });
			_enemyAttackCornerBlTexture = TryLoadDecorationTexture("enemy_attack_bl");
			_enemyAttackCornerBrTexture = TryLoadDecorationTexture("enemy_attack_br");
			_enemyAttackTopTexture = TryLoadDecorationTexture("enemy_attack_top");
			_enemyAttackSkullTexture = TryLoadDecorationTexture("enemy_attack_skull");
			EventManager.Subscribe<DebugCommandEvent>(evt =>
			{
				if (evt.Command == "ConfirmEnemyAttack")
				{
                    Console.WriteLine("[EnemyAttackDisplaySystem] DebugCommand ConfirmEnemyAttack received");
					OnConfirmPressed();
				}
			});

		// Clear any transient visuals when leaving Enemy phases
		EventManager.Subscribe<ChangeBattlePhaseEvent>(evt =>
		{
			if (evt.Current != SubPhase.Block && evt.Current != SubPhase.EnemyAttack)
			{
				_impactActive = false;
				_absorbElapsedSeconds = 0f;
				_absorbCompleteFired = false;
				_lastContextId = null;
				_debris.Clear();
				_showBanner = false;
				// Cleanup tooltip entity when leaving enemy phases
				if (_attackTextTooltipEntity != null)
				{
					EntityManager.DestroyEntity(_attackTextTooltipEntity.Id);
					_attackTextTooltipEntity = null;
				}
			}
			if (evt.Current == SubPhase.Block && evt.Previous != SubPhase.Block)
			{
				_showBanner = false;
			}
		});

			EventManager.Subscribe<TriggerEnemyAttackDisplayEvent>(evt =>
			{
				_showBanner = true;
				// Spawn centered and trigger immediate impact sequence
				_impactActive = true;
				_squashElapsedSeconds = 0f;
				_flashElapsedSeconds = 0f;
				_craterElapsedSeconds = 0f;
				_shakeElapsedSeconds = 0f;
				_debris.Clear();
				SpawnDebris();
				EventManager.Publish(new PlaySfxEvent { Track = SfxTrack.EnemyAttackIntro });
				// Publish rectangular shockwave event
				int vx = Game1.VirtualWidth;
				int vy = Game1.VirtualHeight;
				float percent = Math.Clamp(PanelMaxWidthPercent, 0.1f, 1f);
				int panelW = (int)Math.Round(vx * percent);
				int panelH = (int)Math.Round(vy * 0.25f); // Rough estimate for panel height
				EventManager.Publish(new RectangularShockwaveEvent
				{
					BoundsCenterPx = new Vector2(vx / 2f + OffsetX, vy / 2f + OffsetY),
					BoundsSizePx = new Vector2(panelW, panelH),
					DurationSec = 0.5f,
					MaxRadiusPx = 130f,
					RippleWidthPx = 6f,
					Strength = 0.02f,
					ChromaticAberrationAmp = 0.003f,
					ChromaticAberrationFreq = 2f,
					ShadingIntensity = 0.15f
				});
			});
		}

		private Texture2D TryLoadDecorationTexture(string assetName)
		{
			if (_content == null || string.IsNullOrWhiteSpace(assetName)) return null;
			try { return _content.Load<Texture2D>(assetName); }
			catch { return null; }
		}

		private void CreateConfirmButton()
		{
			var primaryBtn = EntityManager.CreateEntity("UIButton_ConfirmEnemyAttack");
			EntityManager.AddComponent(primaryBtn, new Transform{});
			EntityManager.AddComponent(primaryBtn, new UIElement { IsInteractable = true, EventType = UIElementEventType.ConfirmBlocks });
			EntityManager.AddComponent(primaryBtn, new HotKey { Button = FaceButton.Y });
			EntityManager.AddComponent(primaryBtn, ParallaxLayer.GetUIParallaxLayer());
		}


		private void CleanupEffectTooltips(System.Collections.Generic.HashSet<string> presentKeys)
		{
			var keys = _effectTooltipUiByKey.Keys.ToList();
			foreach (var k in keys)
			{
				if (!presentKeys.Contains(k))
				{
					if (_effectTooltipUiByKey.TryGetValue(k, out var e) && e != null)
					{
						EntityManager.DestroyEntity(e.Id);
					}
					_effectTooltipUiByKey.Remove(k);
				}
			}
		}

		private void OnConfirmPressed()
		{
			// Determine current context id first
			var enemy = GetRelevantEntities().FirstOrDefault();
			var intent = enemy?.GetComponent<AttackIntent>();
			var ctx = intent?.Planned?.FirstOrDefault()?.ContextId;
			if (string.IsNullOrEmpty(ctx)) return;
			// Lock confirm for this context immediately to avoid double presses
			_confirmedForContext.Add(ctx);
			var confirmBtn = EntityManager.GetEntity("UIButton_ConfirmEnemyAttack");
			if (confirmBtn != null)
			{
				var ui = confirmBtn.GetComponent<UIElement>();
				if (ui != null)
				{
					ui.IsInteractable = false;
					ui.Bounds = new Rectangle(0, 0, 0, 0);
				}
			}
			EventManager.Publish(new ChangeBattlePhaseEvent { Current = SubPhase.EnemyAttack, Previous = SubPhase.Block });
			// Enqueue: Discard assigned blocks as the first step
			// Defer resolution/phase to coordinator; enqueue the standard sequence
			EventQueue.EnqueueRule(new QueuedDiscardAssignedBlocksEvent(EntityManager, ctx));
			EventQueue.EnqueueRule(new QueuedResolveAttackEvent(ctx));
			EventQueue.EnqueueRule(new QueuedWaitAbsorbEvent(ctx));
			EventQueue.EnqueueRule(new QueuedStartEnemyAttackAnimation(ctx));
			EventQueue.EnqueueRule(new QueuedWaitImpactEvent(ctx));
			EventQueue.EnqueueRule(new QueuedAdvanceToNextPlannedAttackEvent(EntityManager, ctx));
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<AttackIntent>();
		}

		[DebugAction("Replay Impact Animation")]
		public void Debug_ReplayImpactAnimation()
		{
			// Trigger a fresh impact sequence even if one is currently playing
			_impactActive = true;
			_squashElapsedSeconds = 0f;
			_flashElapsedSeconds = 0f;
			_craterElapsedSeconds = 0f;
			_shakeElapsedSeconds = 0f;
			_debris.Clear();
			SpawnDebris();
			// Publish rectangular shockwave event
			int vx = Game1.VirtualWidth;
			int vy = Game1.VirtualHeight;
			float percent = Math.Clamp(PanelMaxWidthPercent, 0.1f, 1f);
			int panelW = (int)Math.Round(vx * percent);
			int panelH = (int)Math.Round(vy * 0.25f);
			EventManager.Publish(new RectangularShockwaveEvent
			{
				BoundsCenterPx = new Vector2(vx / 2f + OffsetX, vy / 2f + OffsetY),
				BoundsSizePx = new Vector2(panelW, panelH),
				DurationSec = 0.5f,
				MaxRadiusPx = 1300f,
				RippleWidthPx = 60f,
				Strength = 10.2f,
				ChromaticAberrationAmp = 0.3f,
				ChromaticAberrationFreq = 20f,
				ShadingIntensity = 1.15f
			});
		}

		protected override void UpdateEntity(Entity entity, Microsoft.Xna.Framework.GameTime gameTime)
		{
			if (EntityManager.GetEntity("UIButton_ConfirmEnemyAttack") == null) {
				CreateConfirmButton();
			}
			var phaseNow = EntityManager.GetEntitiesWithComponent<PhaseState>().FirstOrDefault().GetComponent<PhaseState>().Sub;
			if (phaseNow == SubPhase.Block) { 
				_absorbElapsedSeconds = 0f;
				_absorbCompleteFired = false;
			}
		var intent = entity.GetComponent<AttackIntent>();
		if (intent == null || intent.Planned.Count == 0)
		{
			_impactActive = false;
			_impactActive = false;
			_lastContextId = null;
			_debris.Clear();
			// Cleanup tooltip entity when no attack is planned
			if (_attackTextTooltipEntity != null)
			{
				EntityManager.DestroyEntity(_attackTextTooltipEntity.Id);
				_attackTextTooltipEntity = null;
			}
			return;
		}

			var currentContextId = intent.Planned[0].ContextId;
			if (_lastContextId != currentContextId)
			{
				_lastContextId = currentContextId;
				// New context: reset confirm lock for previous and ensure button can show again
				_confirmedForContext.RemoveWhere(id => id != currentContextId);
				// Specific discard preselection is handled by MarkedForSpecificDiscardSystem
			}

			float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
			if (_impactActive)
			{
				_squashElapsedSeconds += dt;
				_flashElapsedSeconds += dt;
				_craterElapsedSeconds += dt;
				_shakeElapsedSeconds += dt;
				UpdateDebris(dt);
			}
			// Update absorb tween timer based on battle phase
			if (phaseNow == SubPhase.EnemyAttack)
			{
				_absorbElapsedSeconds += dt;
				if (_absorbElapsedSeconds >= AbsorbDurationSeconds)
				{
					EventManager.Publish(new EnemyAbsorbComplete { ContextId = intent.Planned[0].ContextId });
					_absorbCompleteFired = true;
				}
			}

			// Calculate and store banner rect for TutorialDisplaySystem to query
			if (_showBanner && _contentFont != null)
			{
				var pa = intent.Planned[0];
				var def = pa.AttackDefinition;
				if (def != null)
				{
					_bannerRect = CalculateBannerRect(entity, phaseNow, def);
					UpdateAnchorBounds(_bannerRect);
				}
			}
		}

		public void Draw()
		{
			var enemy = GetRelevantEntities().FirstOrDefault();
			var intent = enemy?.GetComponent<AttackIntent>();
			if (intent == null || intent.Planned.Count == 0 || _contentFont == null) return;
			// Only render during enemy phases (Block / EnemyAttack)
			var phaseNowForDraw = EntityManager.GetEntitiesWithComponent<PhaseState>().FirstOrDefault().GetComponent<PhaseState>().Sub;
			if (phaseNowForDraw != SubPhase.Block && phaseNowForDraw != SubPhase.EnemyAttack) return;
			if (!_showBanner) return;
			// Gate display during ambush intro
			var ambushState = EntityManager.GetEntitiesWithComponent<AmbushState>().FirstOrDefault()?.GetComponent<AmbushState>();
			if (ambushState != null && ambushState.IsActive && ambushState.IntroActive) return;
			if (_absorbCompleteFired) return;

			var pa = intent.Planned[0];
			var def = pa.AttackDefinition;
			if (def == null) return;

			// Compose lines: Name, Damage (final + prevented breakdown), and Leaf conditions (with live status)
			var lines = new System.Collections.Generic.List<(string text, float scale, Color color)>();
			lines.Add((def.Name, TitleScale, Color.White));
			var progress = FindEnemyAttackProgress(pa.ContextId);
			if (progress != null)
			{
				lines.Add(($"{def.Text}", TextScale, progress.IsConditionMet ? Color.White : new Color(255, 150, 150, 255)));
			}
			// Measure and draw a simple panel in the center
			int pad = Math.Max(0, PanelPadding);
			int vx = Game1.VirtualWidth;
			int vy = Game1.VirtualHeight;
			float percent = Math.Clamp(PanelMaxWidthPercent, 0.1f, 1f);
			int maxPanelWidthPx = (int)Math.Round(vx * percent);
			float minPercent = Math.Clamp(PanelMinWidthPercent, 0f, 1f);
			int minPanelWidthPx = (int)Math.Round(vx * minPercent);

			int contentWidthLimitPx = Math.Max(50, maxPanelWidthPx - pad * 2);
			// wrappedLines: centerTitle == true for attack name lines, false otherwise
			var wrappedLines = new System.Collections.Generic.List<(string text, float scale, Color color, bool centerTitle)>();
			for (int i = 0; i < lines.Count; i++)
			{
				var (text, lineScale, color) = lines[i];
				bool centerTitle = (i == 0); // Center the attack name; other lines stay left-aligned
				var parts = TextUtils.WrapText(_contentFont, text, lineScale, contentWidthLimitPx);
				foreach (var p in parts)
				{
					wrappedLines.Add((p, lineScale, color, centerTitle));
				}
			}
			float maxW = 0f;
			float totalH = 0f;
			bool isFirstTitleForHeight = true;
			foreach (var (text, lineScale, _, centerTitle) in wrappedLines)
			{
				var sz = _contentFont.MeasureString(text);
				maxW = Math.Max(maxW, sz.X * lineScale);
				float spacing = (isFirstTitleForHeight && centerTitle) ? TitleSpacingExtra : LineSpacingExtra;
				totalH += sz.Y * lineScale + spacing;
				if (centerTitle) isFirstTitleForHeight = false;
			}
			int w = (int)Math.Ceiling(Math.Min(maxW + pad * 2, maxPanelWidthPx));
			w = Math.Max(w, minPanelWidthPx);
			int h = (int)Math.Ceiling(totalH) + pad * 2;

			// Derive panel center from viewport center plus the current parallax offset of the banner anchor
			var anchorEntity = EntityManager.GetEntitiesWithComponent<Crusaders30XX.ECS.Components.EnemyAttackBannerAnchor>().FirstOrDefault();
			if (anchorEntity == null)
			{
				anchorEntity = EntityManager.CreateEntity("EnemyAttackBannerAnchor");
				EntityManager.AddComponent(anchorEntity, new Crusaders30XX.ECS.Components.EnemyAttackBannerAnchor());
				EntityManager.AddComponent(anchorEntity, new Transform());
				var parallaxLayer = ParallaxLayer.GetUIParallaxLayer();
				parallaxLayer.MultiplierX = 0.045f;
				parallaxLayer.MultiplierY = 0.045f;
				EntityManager.AddComponent(anchorEntity, parallaxLayer);
				// Provide UI bounds for other systems to align against the live banner rect
				EntityManager.AddComponent(anchorEntity, new UIElement { Bounds = new Rectangle(0, 0, 1, 1), IsInteractable = false });
			}
			var anchorTransform = anchorEntity.GetComponent<Transform>();
			Vector2 parallaxOffset = Vector2.Zero;
			if (anchorTransform != null)
			{
				parallaxOffset = anchorTransform.Position - anchorTransform.BasePosition;
			}
			var centerBase = new Vector2(vx / 2f + OffsetX, vy / 2f + OffsetY);
			var center = centerBase + parallaxOffset;
			Vector2 approachPos = center;
			float panelScale = 1f;
			// During processing, tween panel toward enemy center and scale down to 0
			var phaseNow = EntityManager.GetEntitiesWithComponent<PhaseState>().FirstOrDefault().GetComponent<PhaseState>().Sub;
			if (phaseNow == SubPhase.EnemyAttack)
			{
				var enemyT = enemy?.GetComponent<Transform>();
				var dur = Math.Max(0.05f, AbsorbDurationSeconds);
				float tTween = MathHelper.Clamp(_absorbElapsedSeconds / dur, 0f, 1f);
				float ease = 1f - (float)Math.Pow(1f - tTween, 3);
				var targetPos = enemyT.Position + new Vector2(0, AbsorbTargetYOffset);
				approachPos = Vector2.Lerp(center, targetPos, ease);
				panelScale = MathHelper.Lerp(1f, 0f, ease);
			}
			int bgAlpha = Math.Clamp(BackgroundAlpha, 0, 255);

			// Impact phase visuals: squash/stretch + shake/flash/crater
			Vector2 shake = Vector2.Zero;
			float squashX = 1f;
			float squashY = 1f;
			float contentScale = 1f;
			if (_impactActive)
			{
				float t = Math.Clamp(_squashElapsedSeconds / Math.Max(0.0001f, SquashDurationSeconds), 0f, 1f);
				// easeOutBack-like return towards 1 with overshoot feel
				float back = 1f + (OvershootIntensity) * (float)Math.Pow(1f - t, 3);
				squashX = MathHelper.Lerp(SquashXFactor, 1f, t) * back;
				squashY = MathHelper.Lerp(SquashYFactor, 1f, t) / back;
				// Scale content with the squash so text remains inside the panel
				contentScale = Math.Min(squashX, squashY);
				if (_shakeElapsedSeconds < ShakeDurationSeconds && ShakeAmplitudePx > 0)
				{
					float shakeT = 1f - Math.Clamp(_shakeElapsedSeconds / Math.Max(0.0001f, ShakeDurationSeconds), 0f, 1f);
					int sx = _rand.Next(-ShakeAmplitudePx, ShakeAmplitudePx + 1);
					int sy = _rand.Next(-ShakeAmplitudePx, ShakeAmplitudePx + 1);
					shake = new Vector2(sx, sy) * shakeT;
				}
			}

			int drawW = (int)Math.Round(w * panelScale * squashX);
			int drawH = (int)Math.Round(h * panelScale * squashY);
			// Anchor from top (X is still centered horizontally)
			var rect = new Rectangle((int)(approachPos.X - drawW / 2f + shake.X), (int)(approachPos.Y + shake.Y), drawW, drawH);
			_spriteBatch.Draw(_pixel, rect, new Color(20, 20, 20, bgAlpha));
			DrawRect(rect, Color.White, Math.Max(0, BorderThickness));
			DrawAttackDecorations(rect, panelScale, squashX, squashY);

			// Keep the anchor's UI bounds synced to the drawn banner rectangle for accurate positioning elsewhere
			{
				var anchorUi = anchorEntity.GetComponent<UIElement>();
				if (anchorUi == null)
				{
					anchorUi = new UIElement { Bounds = rect, IsInteractable = false };
					EntityManager.AddComponent(anchorEntity, anchorUi);
				}
				else
				{
					anchorUi.Bounds = rect;
					anchorUi.IsInteractable = false;
				}
			}

			// Impact flash overlay
			if (_impactActive && _flashElapsedSeconds < FlashDurationSeconds && FlashMaxAlpha > 0)
			{
				float ft = 1f - Math.Clamp(_flashElapsedSeconds / Math.Max(0.0001f, FlashDurationSeconds), 0f, 1f);
				int fa = (int)(FlashMaxAlpha * ft);
				_spriteBatch.Draw(_pixel, rect, new Color(255, 255, 255, Math.Clamp(fa, 0, 255)));
			}

			// Crater (darkened expanding rect)
			if (_impactActive && _craterElapsedSeconds < CraterDurationSeconds && CraterMaxAlpha > 0)
			{
				float ct = Math.Clamp(_craterElapsedSeconds / Math.Max(0.0001f, CraterDurationSeconds), 0f, 1f);
				int cexp = (int)Math.Round(CraterMaxExpandPx * ct);
				int ca = (int)Math.Round(CraterMaxAlpha * (1f - ct));
				var craterRect = new Rectangle(rect.X - cexp, rect.Y - cexp, rect.Width + cexp * 2, rect.Height + cexp * 2);
				_spriteBatch.Draw(_pixel, craterRect, new Color(10, 10, 10, Math.Clamp(ca, 0, 255)));
			}

			// Debris
			if (_impactActive && _debris.Count > 0)
			{
				var debrisBase = new Vector2(rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f);
				for (int i = 0; i < _debris.Count; i++)
				{
					var d = _debris[i];
					if (d.Age <= d.Lifetime)
					{
						int ds = (int)Math.Max(1, d.Size);
						var p = new Rectangle((int)(debrisBase.X + d.Position.X + shake.X), (int)(debrisBase.Y + d.Position.Y + shake.Y), ds, ds);
						_spriteBatch.Draw(_pixel, p, d.Color);
					}
				}
			}

		// Content
		float y = rect.Y + pad * panelScale * contentScale;

		// Build per-effect hover UI for the "On hit" summary, positioned below the banner
		var presentKeys = new System.Collections.Generic.HashSet<string>();
		bool isFirstTitleLine = true;
		
		// Track bounds of def.Text lines (non-title lines) for tooltip
		float defTextMinX = float.MaxValue;
		float defTextMinY = float.MaxValue;
		float defTextMaxX = float.MinValue;
		float defTextMaxY = float.MinValue;
		bool hasDefTextLines = false;
		
		foreach (var (text, baseScale, color, centerTitle) in wrappedLines)
		{
			float s = baseScale * panelScale * contentScale;
			var sz = _contentFont.MeasureString(text);
			float textWidth = sz.X * s;
			float textHeight = sz.Y * s;
			float x = centerTitle
				? rect.X + (rect.Width - textWidth) / 2f
				: rect.X + pad * panelScale * contentScale;
			_spriteBatch.DrawString(_contentFont, text, new Vector2(x, y), color, 0f, Vector2.Zero, s, SpriteEffects.None, 0f);
			
			// Track bounds for non-title lines (these are the def.Text lines)
			if (!centerTitle)
			{
				defTextMinX = Math.Min(defTextMinX, x);
				defTextMinY = Math.Min(defTextMinY, y);
				defTextMaxX = Math.Max(defTextMaxX, x + textWidth);
				defTextMaxY = Math.Max(defTextMaxY, y + textHeight);
				hasDefTextLines = true;
			}
			
			float extraSpacing = (isFirstTitleLine && centerTitle) 
				? TitleSpacingExtra 
				: LineSpacingExtra;
			y += sz.Y * s + extraSpacing * panelScale * contentScale;
			if (centerTitle) isFirstTitleLine = false;
		}
		
		// Create or update tooltip entity for def.Text if keywords are present
		string keywordTooltip = KeywordTooltipTextService.GetTooltip(def.Text);
		if (!string.IsNullOrEmpty(keywordTooltip) && hasDefTextLines)
		{
			var defTextBounds = new Rectangle(
				(int)Math.Floor(defTextMinX),
				(int)Math.Floor(defTextMinY),
				(int)Math.Ceiling(defTextMaxX - defTextMinX),
				(int)Math.Ceiling(defTextMaxY - defTextMinY)
			);
			
			if (_attackTextTooltipEntity == null)
			{
				_attackTextTooltipEntity = EntityManager.CreateEntity("UI_EnemyAttackTextTooltip");
				EntityManager.AddComponent(_attackTextTooltipEntity, new Transform { Position = new Vector2(defTextBounds.X, defTextBounds.Y), ZOrder = 10001 });
				EntityManager.AddComponent(_attackTextTooltipEntity, new UIElement 
				{ 
					Bounds = defTextBounds, 
					IsInteractable = false, 
					Tooltip = keywordTooltip, 
					TooltipType = TooltipType.Text,
					TooltipPosition = TooltipPosition.Below 
				});
			}
			else
			{
				var tr = _attackTextTooltipEntity.GetComponent<Transform>();
				if (tr != null)
				{
					tr.Position = new Vector2(defTextBounds.X, defTextBounds.Y);
					tr.ZOrder = 10001;
				}
				var ui = _attackTextTooltipEntity.GetComponent<UIElement>();
				if (ui != null)
				{
					ui.Bounds = defTextBounds;
					ui.Tooltip = keywordTooltip;
					ui.TooltipType = TooltipType.Text;
					ui.TooltipPosition = TooltipPosition.Below;
					ui.IsInteractable = false;
				}
			}
		}
		else
		{
			// No keywords found or no def.Text lines - cleanup tooltip entity if it exists
			if (_attackTextTooltipEntity != null)
			{
				EntityManager.DestroyEntity(_attackTextTooltipEntity.Id);
				_attackTextTooltipEntity = null;
			}
		}

		// Confirm button below panel (only show in Block phase)
		Entity primaryBtn = EntityManager.GetEntity("UIButton_ConfirmEnemyAttack");
		var isInteractable = primaryBtn?.GetComponent<UIElement>()?.IsInteractable ?? false;
		bool isAnimating = IsAnyBlockAssignmentAnimating();
		bool showConfirm = phaseNow == SubPhase.Block && !_confirmedForContext.Contains(pa.ContextId) && isInteractable && !isAnimating;
			
			// Manage hotkey IsActive flag based on animation state
			if (primaryBtn != null)
			{
				var hotkey = primaryBtn.GetComponent<HotKey>();
				if (hotkey != null)
				{
					hotkey.IsActive = !isAnimating && phaseNow == SubPhase.Block && !_confirmedForContext.Contains(pa.ContextId);
				}
			}
			
			if (showConfirm)
			{
				var btnRect = new Rectangle(
					(int)(rect.X + rect.Width / 2f - ConfirmButtonWidth / 2f),
					rect.Bottom + ConfirmButtonOffsetY,
					ConfirmButtonWidth,
					ConfirmButtonHeight
				);
				_spriteBatch.Draw(_pixel, btnRect, new Color(40, 120, 40, 220));
				DrawRect(btnRect, Color.White, 2);
				if (_contentFont != null)
				{
					string label = "Confirm";
					var size = _contentFont.MeasureString(label) * ConfirmButtonTextScale;
					var posText = new Vector2(btnRect.Center.X - size.X / 2f, btnRect.Center.Y - size.Y / 2f);
					_spriteBatch.DrawString(_contentFont, label, posText, Color.White, 0f, Vector2.Zero, ConfirmButtonTextScale, SpriteEffects.None, 0f);
				}

				var ui = primaryBtn.GetComponent<UIElement>();
				var tr = primaryBtn.GetComponent<Transform>();
				if (ui != null) { ui.Bounds = btnRect; }
				if (tr != null) { tr.ZOrder = ConfirmButtonZ; tr.BasePosition = new Vector2(btnRect.X, btnRect.Y); tr.Position = new Vector2(btnRect.X, btnRect.Y); }
				
			}


			// Cleanup any stale per-effect tooltip UIs not present this frame
			CleanupEffectTooltips(presentKeys);

			// Update banner anchor transform at center-bottom of the base (non-parallax) rect
			if (anchorTransform != null)
			{
				anchorTransform.BasePosition = new Vector2(centerBase.X, centerBase.Y + drawH / 2f);
				// Keep current parallax offset; Position will be derived by Parallax system if applicable
				if (anchorTransform.Position == Vector2.Zero)
				{
					anchorTransform.Position = anchorTransform.BasePosition;
				}
				anchorTransform.Scale = Vector2.One;
				anchorTransform.Rotation = 0f;
			}
		}

	private bool IsAnyBlockAssignmentAnimating()
	{
		var assignedCards = EntityManager.GetEntitiesWithComponent<AssignedBlockCard>();
		foreach (var entity in assignedCards)
		{
			var abc = entity.GetComponent<AssignedBlockCard>();
			if (abc != null && abc.Phase != AssignedBlockCard.PhaseState.Idle)
			{
				return true;
			}
		}
		return false;
	}

	private Rectangle CalculateBannerRect(Entity enemy, SubPhase phaseNow, EnemyAttackBase def)
	{
		int pad = Math.Max(0, PanelPadding);
		int vx = Game1.VirtualWidth;
		int vy = Game1.VirtualHeight;
		float percent = Math.Clamp(PanelMaxWidthPercent, 0.1f, 1f);
		int maxPanelWidthPx = (int)Math.Round(vx * percent);
		float minPercent = Math.Clamp(PanelMinWidthPercent, 0f, 1f);
		int minPanelWidthPx = (int)Math.Round(vx * minPercent);
		int contentWidthLimitPx = Math.Max(50, maxPanelWidthPx - pad * 2);

		// Measure content to determine panel size
		var lines = new List<(string text, float scale)>();
		lines.Add((def.Name, TitleScale));
		lines.Add((def.Text, TextScale));

		float maxW = 0f;
		float totalH = 0f;
		bool isFirstTitle = true;
		foreach (var (text, lineScale) in lines)
		{
			var parts = TextUtils.WrapText(_contentFont, text, lineScale, contentWidthLimitPx);
			foreach (var p in parts)
			{
				var sz = _contentFont.MeasureString(p);
				maxW = Math.Max(maxW, sz.X * lineScale);
				float spacing = isFirstTitle ? TitleSpacingExtra : LineSpacingExtra;
				totalH += sz.Y * lineScale + spacing;
				if (isFirstTitle) isFirstTitle = false;
			}
		}

		int w = (int)Math.Ceiling(Math.Min(maxW + pad * 2, maxPanelWidthPx));
		w = Math.Max(w, minPanelWidthPx);
		int h = (int)Math.Ceiling(totalH) + pad * 2;

		// Calculate animation effects
		float panelScale = 1f;
		float squashX = 1f;
		float squashY = 1f;

		if (phaseNow == SubPhase.EnemyAttack)
		{
			var dur = Math.Max(0.05f, AbsorbDurationSeconds);
			float tTween = MathHelper.Clamp(_absorbElapsedSeconds / dur, 0f, 1f);
			float ease = 1f - (float)Math.Pow(1f - tTween, 3);
			panelScale = MathHelper.Lerp(1f, 0f, ease);
		}

		if (_impactActive)
		{
			float t = Math.Clamp(_squashElapsedSeconds / Math.Max(0.0001f, SquashDurationSeconds), 0f, 1f);
			float back = 1f + (OvershootIntensity) * (float)Math.Pow(1f - t, 3);
			squashX = MathHelper.Lerp(SquashXFactor, 1f, t) * back;
			squashY = MathHelper.Lerp(SquashYFactor, 1f, t) / back;
		}

		int drawW = (int)Math.Round(w * panelScale * squashX);
		int drawH = (int)Math.Round(h * panelScale * squashY);

		// Calculate position
		var anchorEntity = EntityManager.GetEntitiesWithComponent<EnemyAttackBannerAnchor>().FirstOrDefault();
		var anchorTransform = anchorEntity?.GetComponent<Transform>();
		Vector2 parallaxOffset = Vector2.Zero;
		if (anchorTransform != null)
		{
			parallaxOffset = anchorTransform.Position - anchorTransform.BasePosition;
		}

		var centerBase = new Vector2(vx / 2f + OffsetX, vy / 2f + OffsetY);
		var center = centerBase + parallaxOffset;
		Vector2 approachPos = center;

		if (phaseNow == SubPhase.EnemyAttack)
		{
			var enemyT = enemy?.GetComponent<Transform>();
			if (enemyT != null)
			{
				var dur = Math.Max(0.05f, AbsorbDurationSeconds);
				float tTween = MathHelper.Clamp(_absorbElapsedSeconds / dur, 0f, 1f);
				float ease = 1f - (float)Math.Pow(1f - tTween, 3);
				var targetPos = enemyT.Position + new Vector2(0, AbsorbTargetYOffset);
				approachPos = Vector2.Lerp(center, targetPos, ease);
			}
		}

		// Note: Shake effect is not included here for stable bounds that TutorialDisplaySystem can use
		// The visual shake is applied separately in Draw()
		return new Rectangle(
			(int)(approachPos.X - drawW / 2f),
			(int)(approachPos.Y),
			drawW,
			drawH
		);
	}

	private void UpdateAnchorBounds(Rectangle bannerRect)
	{
		var anchorEntity = EntityManager.GetEntitiesWithComponent<EnemyAttackBannerAnchor>().FirstOrDefault();
		if (anchorEntity == null) return;

		var anchorUi = anchorEntity.GetComponent<UIElement>();
		if (anchorUi == null)
		{
			anchorUi = new UIElement { Bounds = bannerRect, IsInteractable = false };
			EntityManager.AddComponent(anchorEntity, anchorUi);
		}
		else
		{
			anchorUi.Bounds = bannerRect;
			anchorUi.IsInteractable = false;
		}
	}

		private EnemyAttackProgress FindEnemyAttackProgress(string contextId)
		{
			foreach (var e in EntityManager.GetEntitiesWithComponent<EnemyAttackProgress>())
			{
				var p = e.GetComponent<EnemyAttackProgress>();
				if (p != null && p.ContextId == contextId) return p;
			}
			return null;
		}


		private void DrawAttackDecorations(Rectangle rect, float panelScale, float panelSquashX, float panelSquashY)
		{
			if (rect.Width <= 0 || rect.Height <= 0) return;

			float scaleXFactor = Math.Max(0f, panelScale * panelSquashX);
			float scaleYFactor = Math.Max(0f, panelScale * panelSquashY);

			float cornerScale = Math.Max(0.01f, CornerOrnamentScale);
			if (_enemyAttackCornerBlTexture != null)
			{
				var originBl = new Vector2(0f, _enemyAttackCornerBlTexture.Height);
				var posBl = new Vector2(rect.Left + CornerLeftOffsetX, rect.Bottom + CornerLeftOffsetY);
				var scaleBl = new Vector2(cornerScale * scaleXFactor, cornerScale * scaleYFactor);
				_spriteBatch.Draw(_enemyAttackCornerBlTexture, posBl, null, Color.White, 0f, originBl, scaleBl, SpriteEffects.None, 0f);
			}

			if (_enemyAttackCornerBrTexture != null)
			{
				var originBr = new Vector2(_enemyAttackCornerBrTexture.Width, _enemyAttackCornerBrTexture.Height);
				var posBr = new Vector2(rect.Right + CornerRightOffsetX, rect.Bottom + CornerRightOffsetY);
				var scaleBr = new Vector2(cornerScale * scaleXFactor, cornerScale * scaleYFactor);
				_spriteBatch.Draw(_enemyAttackCornerBrTexture, posBr, null, Color.White, 0f, originBr, scaleBr, SpriteEffects.None, 0f);
			}

			float topScale = Math.Max(0.01f, TopOrnamentScale);
			if (_enemyAttackTopTexture != null)
			{
				var originTop = new Vector2(_enemyAttackTopTexture.Width / 2f, _enemyAttackTopTexture.Height);
				var posTop = new Vector2(rect.Left + rect.Width / 2f, rect.Top + TopOrnamentOffsetY);
				var topScaleVec = new Vector2(topScale * scaleXFactor, topScale * scaleYFactor);
				_spriteBatch.Draw(_enemyAttackTopTexture, posTop, null, Color.White, 0f, originTop, topScaleVec, SpriteEffects.None, 0f);
			}

			float skullScale = Math.Max(0.01f, SkullScale);
			if (_enemyAttackSkullTexture != null)
			{
				var originSkull = new Vector2(_enemyAttackSkullTexture.Width / 2f, _enemyAttackSkullTexture.Height);
				var skullPos = new Vector2(rect.Left + rect.Width / 2f, rect.Top + SkullVerticalOffset);
				var skullScaleVec = new Vector2(skullScale * scaleXFactor, skullScale * scaleYFactor);
				_spriteBatch.Draw(_enemyAttackSkullTexture, skullPos, null, Color.White, 0f, originSkull, skullScaleVec, SpriteEffects.None, 0f);
			}
		}

		private void DrawRect(Rectangle rect, Color color, int thickness)
		{
			_spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
			_spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
			_spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
			_spriteBatch.Draw(_pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
		}

		private void SpawnDebris()
		{
			_debris.Clear();
			var rand = _rand;
			for (int i = 0; i < DebrisCount; i++)
			{
				float ang = (float)(rand.NextDouble() * Math.PI * 2);
				float spd = rand.Next(DebrisSpeedMin, DebrisSpeedMax + 1);
				var vel = new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang)) * spd;
				_debris.Add(new DebrisParticle
				{
					Position = Vector2.Zero, // will be positioned at draw time around the rect center
					Velocity = vel,
					Age = 0f,
					Lifetime = DebrisLifetimeSeconds * (0.6f + (float)rand.NextDouble() * 0.8f),
					Size = 2 + (float)rand.NextDouble() * 3f,
					Color = new Color(230, 230, 230, 200)
				});
			}
		}

		private void UpdateDebris(float dt)
		{
			for (int i = 0; i < _debris.Count; i++)
			{
				var d = _debris[i];
				d.Age += dt;
				d.Position += d.Velocity * dt;
				float lifeT = Math.Clamp(d.Age / Math.Max(0.0001f, d.Lifetime), 0f, 1f);
				int a = (int)(200 * (1f - lifeT));
				d.Color = new Color(d.Color.R, d.Color.G, d.Color.B, Math.Clamp(a, 0, 255));
				_debris[i] = d;
			}
		}

	}
}


