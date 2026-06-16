using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Events;
using System.Collections.Generic;
using System;
using Crusaders30XX.ECS.Singletons;
using Crusaders30XX.ECS.Rendering;
using Microsoft.Xna.Framework.Content;
using Crusaders30XX.ECS.Services;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Skeleton banner that displays current attack name, base damage (sum of on-hit Damage effects),
	/// and a simple list of leaf blocking conditions. Shown when there is a current planned attack.
	/// </summary>
	[DebugTab("Enemy Attack Display")]
	public partial class EnemyAttackDisplaySystem : Core.System
	{
		// Graphics & rendering
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly ContentManager _content;
		private readonly SpriteFont _titleFont = FontSingleton.TitleFont;
		private readonly SpriteFont _contentFont = FontSingleton.ContentFont;
		private readonly SpriteFont _bodyFont = FontSingleton.ChakraPetchFont;
		private readonly Texture2D _pixel;
		private readonly Texture2D _enemyAttackCornerBlTexture;
		private readonly Texture2D _enemyAttackCornerBrTexture;
		private readonly Texture2D _enemyAttackTopTexture;
		private readonly Texture2D _enemyAttackSkullTexture;

		// Confirm button texture cache
		private Texture2D _cachedConfirmTexture;
		private string _cachedConfirmText;

		// Tooltip state
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

		// Absorb tween (panel -> enemy)
		[DebugEditable(DisplayName = "Absorb Duration (s)", Step = 0.02f, Min = 0.05f, Max = 3f)]
		public float AbsorbDurationSeconds { get; set; } = 0.4f;
		[DebugEditable(DisplayName = "Absorb Target Y Offset", Step = 2, Min = -400, Max = 400)]
		public int AbsorbTargetYOffset { get; set; } = -40;
		private float _absorbElapsedSeconds = 0f;
		private bool _absorbCompleteFired = false;

		// Panel position
		[DebugEditable(DisplayName = "Center Offset X", Step = 2, Min = -1000, Max = 1000)]
		public int OffsetX { get; set; } = 0;

		[DebugEditable(DisplayName = "Center Offset Y", Step = 2, Min = -400, Max = 400)]
		public int OffsetY { get; set; } = -300;

		// Panel sizing
		[DebugEditable(DisplayName = "Panel Padding", Step = 1, Min = 4, Max = 40)]
		public int PanelPadding { get; set; } = 30;

		[DebugEditable(DisplayName = "Border Thickness", Step = 1, Min = 1, Max = 8)]
		public int BorderThickness { get; set; } = 0;

		[DebugEditable(DisplayName = "Background Alpha", Step = 5, Min = 0, Max = 255)]
		public int BackgroundAlpha { get; set; } = 160;

		// Text
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

		// Decorations
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
		public float OvershootIntensity { get; set; } = 0.8f;

		[DebugEditable(DisplayName = "Shake Duration (s)", Step = 0.05f, Min = 0f, Max = 1.5f)]
		public float ShakeDurationSeconds { get; set; } = 0.25f;

		[DebugEditable(DisplayName = "Shake Amplitude (px)", Step = 1, Min = 0, Max = 50)]
		public int ShakeAmplitudePx { get; set; } = 9;

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

			EventManager.Subscribe<ConfirmBlocksRequested>(_ =>
			{
                LoggingService.Append("EnemyAttackDisplaySystem.OnConfirmBlocksRequested", new System.Text.Json.Nodes.JsonObject { ["event"] = "ConfirmBlocksRequested" });
				OnConfirmPressed();
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
				int panelH = (int)Math.Round(vy * 0.25f);
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
			var parallaxLayer = ParallaxLayer.GetUIParallaxLayer();
			parallaxLayer.MultiplierX = 0.045f;
			parallaxLayer.MultiplierY = 0.045f;
			EntityManager.AddComponent(primaryBtn, parallaxLayer);
		}

		private void OnConfirmPressed()
		{
			if (BattleInputGate.IsBattleInputFrozen(EntityManager)) return;
			if (!BattleInputGate.TryAllowTutorialAction(EntityManager, TutorialAction.ConfirmBlocks)) return;
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
			// Defer resolution/phase to coordinator; enqueue the standard sequence
			EventQueue.EnqueueRule(new QueuedDiscardAssignedBlocksEvent(EntityManager, ctx));
			EventQueue.EnqueueRule(new QueuedResolveAttackEvent(ctx));
			EventQueue.EnqueueRule(new QueuedWaitAbsorbEvent(ctx));
			EventQueue.EnqueueRule(new QueuedStartEnemyAttackAnimation(ctx));
			EventQueue.EnqueueRule(new QueuedWaitImpactEvent(ctx));
			EventQueue.EnqueueRule(new QueuedAdvanceToNextPlannedAttackEvent(EntityManager, ctx));
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
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

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
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
			}
			UpdateConfirmAvailability(phaseNow, currentContextId);

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
			// Note: anchor Bounds are written in Draw (UpdateAnchorEntity) with parallax-adjusted
			// values — do NOT overwrite them here, or downstream systems lose parallax.
			if (_showBanner && _contentFont != null && _bodyFont != null)
			{
				var pa = intent.Planned[0];
				var def = pa.AttackDefinition;
				if (def != null)
				{
					_bannerRect = CalculateBannerRect(entity, phaseNow, def);
				}
			}

			// Ensure banner anchor entity exists (unconditional so BuildDrawContext finds it)
			var anchorEntity = EntityManager.GetEntitiesWithComponent<EnemyAttackBannerAnchor>().FirstOrDefault();
			if (anchorEntity == null)
			{
				anchorEntity = EntityManager.CreateEntity("EnemyAttackBannerAnchor");
				EntityManager.AddComponent(anchorEntity, new EnemyAttackBannerAnchor());
				EntityManager.AddComponent(anchorEntity, new Transform());
				var parallaxLayer = ParallaxLayer.GetUIParallaxLayer();
				parallaxLayer.MultiplierX = 0.045f;
				parallaxLayer.MultiplierY = 0.045f;
				EntityManager.AddComponent(anchorEntity, parallaxLayer);
				EntityManager.AddComponent(anchorEntity, new UIElement { Bounds = new Rectangle(0, 0, 1, 1), IsInteractable = false });
			}

			// Only write Positions when banner rect is valid (banner is visible)
			if (_bannerRect != Rectangle.Empty)
			{
				var anchorTransform = anchorEntity.GetComponent<Transform>();
				if (anchorTransform != null)
				{
					var centerBase = new Vector2(Game1.VirtualWidth / 2f + OffsetX, Game1.VirtualHeight / 2f + OffsetY);
					anchorTransform.Position = centerBase;
				}

				// Write confirm button Position so parallax can adjust it
				var confirmBtn = EntityManager.GetEntity("UIButton_ConfirmEnemyAttack");
				if (confirmBtn != null)
				{
					var btnTransform = confirmBtn.GetComponent<Transform>();
					if (btnTransform != null)
					{
						btnTransform.Position = new Vector2(
							_bannerRect.X + _bannerRect.Width / 2f - ConfirmButtonWidth / 2f,
							_bannerRect.Bottom + ConfirmButtonOffsetY
						);
						btnTransform.ZOrder = ConfirmButtonZ;
					}
				}
			}
		}

		private void UpdateConfirmAvailability(SubPhase phaseNow, string contextId)
		{
			var confirmButton = EntityManager.GetEntity("UIButton_ConfirmEnemyAttack");
			var ui = confirmButton?.GetComponent<UIElement>();
			var hotkey = confirmButton?.GetComponent<HotKey>();
			if (ui == null) return;

			bool tutorialRequirementMet = BattleInputGate.IsTutorialActionAllowed(
				EntityManager,
				TutorialAction.ConfirmBlocks);
			bool available = phaseNow == SubPhase.Block
				&& !_confirmedForContext.Contains(contextId)
				&& !IsAnyBlockAssignmentAnimating()
				&& tutorialRequirementMet;

			ui.IsInteractable = available;
			if (!available) ui.Bounds = Rectangle.Empty;
			if (hotkey != null)
			{
				hotkey.IsActive = available && ui.IsInteractable;
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
	}
}
