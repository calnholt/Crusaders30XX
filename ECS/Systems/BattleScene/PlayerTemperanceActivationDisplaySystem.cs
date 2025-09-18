using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Crusaders30XX.Diagnostics;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// Listens for TriggerTemperance and plays a white mask + expanding ripple over the player's sprite.
    /// </summary>
    [DebugTab("Temperance FX")]
    public class PlayerTemperanceActivationDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly Texture2D _crusaderTexture;
		private float _elapsed;
		private bool _active;
		private float _animTime;
		[DebugEditable(DisplayName = "Duration Seconds", Step = 0.05f, Min = 0.1f, Max = 3f)]
		public float DurationSeconds { get; set; } = 0.6f;

		[DebugEditable(DisplayName = "Ripple Max Scale", Step = 0.05f, Min = 1f, Max = 3f)]
		public float RippleMaxScale { get; set; } = 1.6f;

		[DebugEditable(DisplayName = "Ripple Min Alpha", Step = 0.05f, Min = 0f, Max = 1f)]
		public float RippleMinAlpha { get; set; } = 0f;

		[DebugEditable(DisplayName = "Mask Max Alpha", Step = 0.05f, Min = 0f, Max = 1f)]
		public float MaskMaxAlpha { get; set; } = 1f;

		[DebugEditable(DisplayName = "Offset X", Step = 1, Min = -1000, Max = 1000)]
		public int OffsetX { get; set; } = 0;

		[DebugEditable(DisplayName = "Offset Y", Step = 1, Min = -1000, Max = 1000)]
		public int OffsetY { get; set; } = 0;


		public PlayerTemperanceActivationDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, Texture2D crusaderTexture) : base(entityManager)
		{
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
			_crusaderTexture = crusaderTexture;
			EventManager.Subscribe<TriggerTemperance>(_ => { _active = true; _animTime = 0f; });
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return System.Array.Empty<Entity>();
		}

		public override void Update(GameTime gameTime)
		{
			_elapsed += (float)gameTime.ElapsedGameTime.TotalSeconds;
			if (_active)
			{
				_animTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
				if (_animTime >= DurationSeconds) { _active = false; _animTime = 0f; }
			}
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		public void Draw()
		{
			if (!_active) return;
			var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
			if (player == null) return;
			var t = player.GetComponent<Transform>();
			var pinfo = player.GetComponent<PortraitInfo>();
			if (t == null || pinfo == null || _crusaderTexture == null) return;

			float progress = MathHelper.Clamp(_animTime / System.Math.Max(0.0001f, DurationSeconds), 0f, 1f);
			float rippleScale = MathHelper.Lerp(1f, RippleMaxScale, progress);
			float alpha = MathHelper.Lerp(MaskMaxAlpha, RippleMinAlpha, progress);

			// Compute current draw parameters matching PlayerDisplaySystem (position + anim offsets, scale + anim multiplier)
			var origin = new Vector2(_crusaderTexture.Width / 2f, _crusaderTexture.Height / 2f);
			var anim = player.GetComponent<PlayerAnimationState>();
			var position = t.Position + (anim?.DrawOffset ?? Vector2.Zero) + new Vector2(OffsetX, OffsetY);
			var scaleVec = new Vector2(pinfo.CurrentScale, pinfo.CurrentScale);
			if (anim != null)
			{
				scaleVec.X *= anim.ScaleMultiplier.X;
				scaleVec.Y *= anim.ScaleMultiplier.Y;
			}

			// White sprite mask (uses sprite alpha)
			_spriteBatch.Draw(
				_crusaderTexture,
				position,
				sourceRectangle: null,
				color: Color.White * alpha,
				rotation: 0f,
				origin: origin,
				scale: scaleVec,
				effects: SpriteEffects.None,
				layerDepth: 0f
			);

			// Expanding ripple as scaled-up white-tinted sprite
			var rippleScaleVec = new Vector2(scaleVec.X * rippleScale, scaleVec.Y * rippleScale);
			_spriteBatch.Draw(
				_crusaderTexture,
				position,
				sourceRectangle: null,
				color: Color.White * alpha,
				rotation: 0f,
				origin: origin,
				scale: rippleScaleVec,
				effects: SpriteEffects.None,
				layerDepth: 0f
			);
		}

		[DebugAction("Simulate Temperance Trigger")]
		public void Debug_SimulateTemperanceTrigger()
		{
			EventManager.Publish(new TriggerTemperance { Owner = EntityManager.GetEntity("Player"), AbilityId = "debug" });
		}

	}
}


