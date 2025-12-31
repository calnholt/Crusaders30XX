using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Crusaders30XX.Diagnostics;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("Poison")]
	public class PoisonSystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private Texture2D _whitePixel;

		[DebugEditable(DisplayName = "Seconds Per Tick", Step = 1f, Min = 1f, Max = 600f)]
		public float SecondsPerTick { get; set; } = 60f;

		[DebugEditable(DisplayName = "Meter Width", Step = 1, Min = 1, Max = 400)]
		public int MeterWidth { get; set; } = 100;

		[DebugEditable(DisplayName = "Meter Height", Step = 1, Min = 1, Max = 50)]
		public int MeterHeight { get; set; } = 4;

		[DebugEditable(DisplayName = "Offset X", Step = 1, Min = -500, Max = 500)]
		public int OffsetX { get; set; } = 0;

		[DebugEditable(DisplayName = "Offset Y", Step = 1, Min = -500, Max = 500)]
		public int OffsetY { get; set; } = 4;

		[DebugEditable(DisplayName = "Background Alpha", Step = 1, Min = 0, Max = 255)]
		public int BgA { get; set; } = 160;

		[DebugEditable(DisplayName = "Fill R", Step = 1, Min = 0, Max = 255)]
		public int FillR { get; set; } = 220;
		[DebugEditable(DisplayName = "Fill G", Step = 1, Min = 0, Max = 255)]
		public int FillG { get; set; } = 40;
		[DebugEditable(DisplayName = "Fill B", Step = 1, Min = 0, Max = 255)]
		public int FillB { get; set; } = 40;
		[DebugEditable(DisplayName = "Fill A", Step = 1, Min = 0, Max = 255)]
		public int FillA { get; set; } = 220;

		[DebugEditable(DisplayName = "Z-Order", Step = 1, Min = 0, Max = 20000)]
		public int ZOrder { get; set; } = 10002;

		private float _timerRemaining;
		private bool _wasPoisonedLastFrame;
		private bool _paused = false;

		public PoisonSystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch) : base(entityManager)
		{
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
			_timerRemaining = SecondsPerTick;

			EventManager.Subscribe<UpdatePassive>(OnUpdatePassive);
			EventManager.Subscribe<PassiveTriggered>(_ => { /* no-op; kept for potential future feedback hooks */ });
			EventManager.Subscribe<TutorialStartedEvent>(OnTutorialStarted);
			EventManager.Subscribe<TutorialCompletedEvent>(OnTutorialCompleted);

		}

	private void OnTutorialStarted(TutorialStartedEvent e)
	{
			_paused = true;
	}

	private void OnTutorialCompleted(TutorialCompletedEvent e)
	{
		_paused = false;
	}
		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			// Run once per frame by anchoring to scene state
			return EntityManager.GetEntitiesWithComponent<SceneState>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			if (_paused) return;
			var player = EntityManager.GetEntity("Player");
			if (player == null) return;
			bool isPoisoned = IsPoisoned(player);

			if (!isPoisoned)
			{
				// Reset the timer when poison is not active
				_timerRemaining = SecondsPerTick;
				_wasPoisonedLastFrame = false;
				return;
			}

			// Start full timer when poison becomes active
			if (!_wasPoisonedLastFrame)
			{
				_timerRemaining = SecondsPerTick;
				_wasPoisonedLastFrame = true;
			}

			float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
			if (dt <= 0f) return;
			_timerRemaining -= dt;
			if (_timerRemaining <= 0f)
			{
				// Apply -1 HP and reset timer
				EventManager.Publish(new ModifyHpRequestEvent
				{
					Source = player,
					Target = player,
					Delta = -1,
					DamageType = ModifyTypeEnum.Effect
				});
				EventManager.Publish(new PassiveTriggered { Owner = player, Type = AppliedPassiveType.Poison });
				EventManager.Publish(new PoisonDamageEvent { DurationSec = .5f });
				_timerRemaining = SecondsPerTick;
			}
		}

		public void Draw()
		{
			var player = EntityManager.GetEntity("Player");
			if (player == null) { DestroyMeterIfExists(); return; }

			bool isPoisoned = IsPoisoned(player);
			if (!isPoisoned) { DestroyMeterIfExists(); return; }

			var anchorName = $"UI_PassiveTooltip_{player.Id}_{AppliedPassiveType.Poison}";
			var anchor = EntityManager.GetEntity(anchorName);
			var ui = anchor?.GetComponent<UIElement>();
			if (ui == null) { DestroyMeterIfExists(); return; }

			EnsureWhitePixel();
			EnsureMeterEntity(player);

			// Position meter under the anchor chip
			int x = ui.Bounds.X + OffsetX;
			int y = ui.Bounds.Bottom + OffsetY;

			// Update transform position for the meter entity (anchor top-left)
			var meterEntity = EntityManager.GetEntity($"UI_PoisonMeter_{player.Id}");
			var meterTr = meterEntity?.GetComponent<Transform>();
			if (meterTr != null)
			{
				meterTr.Position = new Vector2(x, y);
				meterTr.ZOrder = ZOrder;
			}

			// Background (black with BgA alpha)
			var bgRect = new Rectangle(x, y, MeterWidth, MeterHeight);
			var bgColor = Color.FromNonPremultiplied(0, 0, 0, (byte)MathHelper.Clamp(BgA, 0, 255));
			_spriteBatch.Draw(_whitePixel, bgRect, bgColor);

			// Fill (red) representing time remaining
			float clampedRemaining = MathHelper.Clamp(_timerRemaining, 0f, MathHelper.Max(0.0001f, SecondsPerTick));
			float pct = MathHelper.Clamp(clampedRemaining / MathHelper.Max(0.0001f, SecondsPerTick), 0f, 1f);
			int fillW = (int)System.Math.Round(MeterWidth * pct);
			if (fillW > 0)
			{
				var fillRect = new Rectangle(x, y, fillW, MeterHeight);
				var fillColor = Color.FromNonPremultiplied(
					(int)MathHelper.Clamp(FillR, 0, 255),
					(int)MathHelper.Clamp(FillG, 0, 255),
					(int)MathHelper.Clamp(FillB, 0, 255),
					(byte)MathHelper.Clamp(FillA, 0, 255)
				);
				_spriteBatch.Draw(_whitePixel, fillRect, fillColor);
			}
		}

		private void EnsureWhitePixel()
		{
			if (_whitePixel != null && !_whitePixel.IsDisposed) return;
			_whitePixel = new Texture2D(_graphicsDevice, 1, 1);
			_whitePixel.SetData(new[] { Color.White });
		}

		private bool IsPoisoned(Entity player)
		{
			var ap = player?.GetComponent<AppliedPassives>();
			if (ap == null || ap.Passives == null) return false;
			return ap.Passives.TryGetValue(AppliedPassiveType.Poison, out var stacks) && stacks > 0;
		}

		private void OnUpdatePassive(UpdatePassive e)
		{
			if (e == null || e.Owner == null) return;
			var player = EntityManager.GetEntity("Player");
			if (player == null || e.Owner.Id != player.Id) return;
			if (e.Type != AppliedPassiveType.Poison) return;
			// If poison cleared, reset timer; if applied, start fresh countdown
			if (!IsPoisoned(player))
			{
				_timerRemaining = SecondsPerTick;
				_wasPoisonedLastFrame = false;
			}
		}

		private void EnsureMeterEntity(Entity player)
		{
			var name = $"UI_PoisonMeter_{player.Id}";
			var e = EntityManager.GetEntity(name);
			if (e == null)
			{
				e = EntityManager.CreateEntity(name);
				EntityManager.AddComponent(e, new Transform { Position = Vector2.Zero, ZOrder = ZOrder });
			}
			else
			{
				var tr = e.GetComponent<Transform>();
				if (tr != null) tr.ZOrder = ZOrder;
			}
		}

		private void DestroyMeterIfExists()
		{
			var player = EntityManager.GetEntity("Player");
			if (player == null) return;
			var name = $"UI_PoisonMeter_{player.Id}";
			var e = EntityManager.GetEntity(name);
			if (e != null)
			{
				EntityManager.DestroyEntity(e.Id);
			}
		}
	}
}
