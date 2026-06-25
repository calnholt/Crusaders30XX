using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Factories;
using Crusaders30XX.ECS.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Plays a climb-map card upgrade cutscene: enter from left, jiggle pulse swap, hold, exit right.
	/// </summary>
	[DebugTab("Climb Upgrade Anim")]
	public class ClimbCardUpgradeDisplaySystem : Core.System
	{
		private enum UpgradeAnimStage
		{
			Enter,
			Pulse,
			Hold,
			Exit,
		}

		private class UpgradeAnim
		{
			public Entity BaseCard;
			public Entity UpgradedCard;
			public bool HasSwapped;
			public bool PulseTriggered;
			public UpgradeAnimStage Stage = UpgradeAnimStage.Enter;
			public float EnterElapsed;
			public float PulseElapsed;
			public float HoldElapsed;
			public float ExitElapsed;
		}

		private const string AnchorEntityName = "Climb_UpgradeAnimAnchor";

		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly Queue<ClimbCardUpgradeAnimationRequested> _queue = new Queue<ClimbCardUpgradeAnimationRequested>();
		private UpgradeAnim _active;
		private Entity _anchor;
		private bool _inputBlockedByThisSystem;

		[DebugEditable(DisplayName = "Enter Duration (s)", Step = 0.01f, Min = 0.01f, Max = 5f)]
		public float EnterDurationSec { get; set; } = 0.1f;

		[DebugEditable(DisplayName = "Exit Duration (s)", Step = 0.01f, Min = 0.01f, Max = 5f)]
		public float ExitDurationSec { get; set; } = 0.1f;

		[DebugEditable(DisplayName = "Hold Duration (s)", Step = 0.01f, Min = 0f, Max = 5f)]
		public float HoldDurationSec { get; set; } = 0.7f;

		[DebugEditable(DisplayName = "Card Scale", Step = 0.01f, Min = 0.01f, Max = 3f)]
		public float CardScale { get; set; } = 1f;

		[DebugEditable(DisplayName = "Center Offset X", Step = 1, Min = -1000, Max = 1000)]
		public int CenterOffsetX { get; set; } = 0;

		[DebugEditable(DisplayName = "Center Offset Y", Step = 1, Min = -1000, Max = 1000)]
		public int CenterOffsetY { get; set; } = 0;

		[DebugEditable(DisplayName = "Left Exit Offset X", Step = 1, Min = -2000, Max = 0)]
		public int LeftExitOffsetX { get; set; } = 0;

		[DebugEditable(DisplayName = "Right Exit Offset X", Step = 1, Min = 0, Max = 2000)]
		public int RightExitOffsetX { get; set; } = 0;

		[DebugEditable(DisplayName = "Vertical Center Offset Y", Step = 1, Min = -1000, Max = 1000)]
		public int VerticalCenterOffsetY { get; set; } = 0;

		[DebugEditable(DisplayName = "Arc Height Enter", Step = 1, Min = -1000, Max = 1000)]
		public int ArcHeightEnter { get; set; } = 120;

		[DebugEditable(DisplayName = "Arc Height Exit", Step = 1, Min = -1000, Max = 1000)]
		public int ArcHeightExit { get; set; } = 120;

		[DebugEditable(DisplayName = "EaseOut Pow", Step = 0.1f, Min = 0.1f, Max = 8f)]
		public float EaseOutPow { get; set; } = 1f;

		[DebugEditable(DisplayName = "EaseIn Pow", Step = 0.1f, Min = 0.1f, Max = 8f)]
		public float EaseInPow { get; set; } = 1f;

		[DebugEditable(DisplayName = "Pulse Duration (s)", Step = 0.01f, Min = 0.05f, Max = 3f)]
		public float PulseDurationSeconds { get; set; } = 0.6f;

		[DebugEditable(DisplayName = "Pulse Scale Amplitude", Step = 0.01f, Min = 0f, Max = 1f)]
		public float PulseScaleAmplitude { get; set; } = 0.2f;

		[DebugEditable(DisplayName = "Jiggle Degrees", Step = 0.5f, Min = 0f, Max = 45f)]
		public float JiggleDegrees { get; set; } = 5f;

		[DebugEditable(DisplayName = "Pulse Frequency Hz", Step = 0.1f, Min = 0.1f, Max = 10f)]
		public float PulseFrequencyHz { get; set; } = 1.7f;

		public ClimbCardUpgradeDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
			: base(entityManager)
		{
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
			EventManager.Subscribe<ClimbCardUpgradeAnimationRequested>(OnAnimationRequested);
			EventManager.Subscribe<DeleteCachesEvent>(_ => ClearAll());
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<SceneState>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			if (!IsClimbScene() || _active == null) return;

			float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
			switch (_active.Stage)
			{
				case UpgradeAnimStage.Enter:
					_active.EnterElapsed += dt;
					if (_active.EnterElapsed >= Math.Max(0.001f, EnterDurationSec))
					{
						_active.Stage = UpgradeAnimStage.Pulse;
						_active.PulseElapsed = 0f;
						TriggerPulse();
					}
					break;

				case UpgradeAnimStage.Pulse:
					_active.PulseElapsed += dt;
					TrySwapAtPulsePeak(_active);
					if (_active.PulseElapsed >= Math.Max(0.01f, PulseDurationSeconds))
					{
						_active.Stage = UpgradeAnimStage.Hold;
						_active.HoldElapsed = 0f;
					}
					break;

				case UpgradeAnimStage.Hold:
					_active.HoldElapsed += dt;
					if (_active.HoldElapsed >= Math.Max(0f, HoldDurationSec))
					{
						_active.Stage = UpgradeAnimStage.Exit;
						_active.ExitElapsed = 0f;
					}
					break;

				case UpgradeAnimStage.Exit:
					_active.ExitElapsed += dt;
					if (_active.ExitElapsed >= Math.Max(0.001f, ExitDurationSec))
					{
						CompleteActiveAnimation();
					}
					break;
			}
		}

		public void Draw()
		{
			if (!IsClimbScene() || _active == null) return;

			Vector2 left = ResolveLeftMiddle();
			Vector2 center = ResolveCenter();
			Vector2 right = ResolveRightMiddle();
			Vector2 pos;
			bool applyJiggle = _active.Stage is UpgradeAnimStage.Pulse or UpgradeAnimStage.Hold;

			switch (_active.Stage)
			{
				case UpgradeAnimStage.Enter:
				{
					float tm = Clamp01(_active.EnterElapsed / Math.Max(0.001f, EnterDurationSec));
					float t = EaseOut(tm, EaseOutPow);
					pos = ArcLerp(left, center, t, ArcHeightEnter);
					break;
				}
				case UpgradeAnimStage.Pulse:
				case UpgradeAnimStage.Hold:
					pos = center;
					break;
				case UpgradeAnimStage.Exit:
				{
					float tm = Clamp01(_active.ExitElapsed / Math.Max(0.001f, ExitDurationSec));
					float t = EaseIn(tm, EaseInPow);
					pos = ArcLerp(center, right, t, ArcHeightExit);
					applyJiggle = false;
					break;
				}
				default:
					return;
			}

			var card = _active.HasSwapped ? _active.UpgradedCard : _active.BaseCard;
			if (card == null) return;

			float renderScale = CardScale;
			float rotation = 0f;
			if (applyJiggle && _anchor != null)
			{
				var anchorTransform = _anchor.GetComponent<Transform>();
				if (anchorTransform != null)
				{
					renderScale *= anchorTransform.Scale.X;
					rotation = anchorTransform.Rotation;
				}
			}

			var cardTransform = card.GetComponent<Transform>();
			if (cardTransform != null) cardTransform.Rotation = rotation;

			EventManager.Publish(new CardRenderScaledRotatedEvent
			{
				Card = card,
				Position = pos,
				Scale = renderScale,
			});
		}

		private void OnAnimationRequested(ClimbCardUpgradeAnimationRequested evt)
		{
			if (!IsClimbScene()) return;
			if (evt == null
				|| string.IsNullOrWhiteSpace(evt.BaseCardKey)
				|| string.IsNullOrWhiteSpace(evt.UpgradedCardKey))
			{
				return;
			}

			_queue.Enqueue(evt);
			if (_active == null) TryStartNext();
		}

		private void TryStartNext()
		{
			if (_active != null || _queue.Count == 0) return;

			var request = _queue.Dequeue();
			var baseCard = CreateDisplayCard(request.BaseCardKey);
			var upgradedCard = CreateDisplayCard(request.UpgradedCardKey);
			if (baseCard == null || upgradedCard == null)
			{
				DestroyDisplayCard(baseCard);
				DestroyDisplayCard(upgradedCard);
				TryStartNext();
				return;
			}

			EnsureAnchor();
			ResetAnchorTransform();

			_active = new UpgradeAnim
			{
				BaseCard = baseCard,
				UpgradedCard = upgradedCard,
				Stage = UpgradeAnimStage.Enter,
			};

			BlockInput();
		}

		private void CompleteActiveAnimation()
		{
			if (_active != null)
			{
				DestroyDisplayCard(_active.BaseCard);
				DestroyDisplayCard(_active.UpgradedCard);
				_active = null;
			}

			if (_queue.Count > 0)
			{
				TryStartNext();
				return;
			}

			UnblockInput();
		}

		private void ClearAll()
		{
			_queue.Clear();
			if (_active != null)
			{
				DestroyDisplayCard(_active.BaseCard);
				DestroyDisplayCard(_active.UpgradedCard);
				_active = null;
			}

			UnblockInput();
		}

		private void BlockInput()
		{
			if (_inputBlockedByThisSystem) return;
			EventManager.Publish(new SetPlayerInputEnabledEvent { Enabled = false });
			_inputBlockedByThisSystem = true;
		}

		private void UnblockInput()
		{
			if (!_inputBlockedByThisSystem) return;
			EventManager.Publish(new SetPlayerInputEnabledEvent { Enabled = true });
			_inputBlockedByThisSystem = false;
		}

		private void TriggerPulse()
		{
			if (_active == null || _active.PulseTriggered || _anchor == null) return;
			_active.PulseTriggered = true;
			EventManager.Publish(new JigglePulseEvent
			{
				Target = _anchor,
				Config = new JigglePulseConfig
				{
					PulseDurationSeconds = PulseDurationSeconds,
					PulseScaleAmplitude = PulseScaleAmplitude,
					JiggleDegrees = JiggleDegrees,
					PulseFrequencyHz = PulseFrequencyHz,
				},
			});
		}

		private void TrySwapAtPulsePeak(UpgradeAnim anim)
		{
			if (anim == null || anim.HasSwapped || anim.PulseElapsed <= 0.01f) return;

			float dur = Math.Max(0.01f, PulseDurationSeconds);
			float norm = MathHelper.Clamp(anim.PulseElapsed / dur, 0f, 1f);
			float env = 1f - norm;
			env *= env;
			float phase = MathHelper.TwoPi * PulseFrequencyHz * anim.PulseElapsed;
			float s = (float)Math.Sin(phase);
			if (s > 0.95f && env > 0.25f)
			{
				anim.HasSwapped = true;
			}
		}

		private Entity CreateDisplayCard(string cardKey)
		{
			if (!RunDeckService.TryParseCardKey(cardKey, out var cardId, out var color, out var isUpgraded)) return null;
			var entity = EntityFactory.CreateCardFromDefinition(
				EntityManager,
				cardId,
				color,
				allowWeapons: false,
				index: 0,
				isUpgraded: isUpgraded);
			if (entity == null) return null;

			var ui = entity.GetComponent<UIElement>();
			if (ui != null)
			{
				ui.IsInteractable = false;
				ui.TooltipType = TooltipType.None;
			}

			return entity;
		}

		private void DestroyDisplayCard(Entity card)
		{
			if (card == null) return;
			EntityManager.DestroyEntity(card.Id);
		}

		private void EnsureAnchor()
		{
			_anchor = EntityManager.GetEntity(AnchorEntityName);
			if (_anchor != null) return;

			_anchor = EntityManager.CreateEntity(AnchorEntityName);
			EntityManager.AddComponent(_anchor, new Transform { Scale = Vector2.One, ZOrder = 10003 });
			EntityManager.AddComponent(_anchor, new OwnedByScene { Scene = SceneId.Climb });
		}

		private void ResetAnchorTransform()
		{
			var transform = _anchor?.GetComponent<Transform>();
			if (transform == null) return;
			transform.Scale = Vector2.One;
			transform.Rotation = 0f;
		}

		private Vector2 ResolveLeftMiddle()
		{
			int cardWidth = CardGeometryService.GetSettings(EntityManager)?.CardWidth ?? CardGeometrySettings.DefaultWidth;
			float vh = Game1.VirtualHeight;
			return new Vector2(
				-cardWidth * 0.5f + LeftExitOffsetX,
				vh * 0.5f + VerticalCenterOffsetY);
		}

		private Vector2 ResolveRightMiddle()
		{
			int cardWidth = CardGeometryService.GetSettings(EntityManager)?.CardWidth ?? CardGeometrySettings.DefaultWidth;
			float vw = Game1.VirtualWidth;
			float vh = Game1.VirtualHeight;
			return new Vector2(
				vw + cardWidth * 0.5f + RightExitOffsetX,
				vh * 0.5f + VerticalCenterOffsetY);
		}

		private Vector2 ResolveCenter()
		{
			float vw = Game1.VirtualWidth;
			float vh = Game1.VirtualHeight;
			return new Vector2(
				vw * 0.5f + CenterOffsetX,
				vh * 0.5f + CenterOffsetY + VerticalCenterOffsetY);
		}

		private bool IsClimbScene()
		{
			return EntityManager.GetEntitiesWithComponent<SceneState>()
				.FirstOrDefault()
				?.GetComponent<SceneState>()
				?.Current == SceneId.Climb;
		}

		private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
		private static float EaseOut(float t, float pow) => 1f - (float)Math.Pow(1f - Clamp01(t), pow);
		private static float EaseIn(float t, float pow) => (float)Math.Pow(Clamp01(t), pow);

		private static Vector2 ArcLerp(Vector2 a, Vector2 b, float t, float arcHeight)
		{
			Vector2 ab = b - a;
			Vector2 n = new Vector2(-ab.Y, ab.X);
			float len = n.Length();
			if (len > 0.0001f) n /= len;
			if (n.Y > 0f) n = -n;
			Vector2 p = a + ab * t;
			float wave = (float)Math.Sin(Math.PI * t);
			return p + n * arcHeight * wave;
		}

		[DebugAction("Play Test Upgrade Animation")]
		private void Debug_PlayTestUpgradeAnimation()
		{
			const string baseKey = "strike|White";
			string upgradedKey = RunDeckService.BuildUpgradedCardKey(baseKey);
			if (string.IsNullOrWhiteSpace(upgradedKey)) return;

			EventManager.Publish(new ClimbCardUpgradeAnimationRequested
			{
				BaseCardKey = baseKey,
				UpgradedCardKey = upgradedKey,
			});
		}
	}
}
