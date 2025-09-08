using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Handles player portrait animations (e.g., attack lunge) and publishes impact signals.
	/// Keeps UI stable by writing only to PlayerAnimationState.DrawOffset.
	/// </summary>
	public class PlayerAnimationSystem : Core.System
	{
		private readonly Vector2 _attackOffset = new Vector2(80f, -20f);

		public PlayerAnimationSystem(EntityManager entityManager) : base(entityManager)
		{
			EventManager.Subscribe<StartPlayerAttackAnimation>(_ =>
			{
				var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
				if (player == null) return;
				var anim = player.GetComponent<PlayerAnimationState>();
				if (anim == null)
				{
					anim = new PlayerAnimationState();
					EntityManager.AddComponent(player, anim);
				}
				anim.AttackAnimTimer = anim.AttackAnimDuration;
				var enemy = EntityManager.GetEntitiesWithComponent<Enemy>().FirstOrDefault();
				anim.AttackTargetPos = enemy?.GetComponent<Transform>()?.Position ?? Vector2.Zero;
			});
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<PlayerAnimationState>();
		}

		protected override void UpdateEntity(Entity entity, Microsoft.Xna.Framework.GameTime gameTime)
		{
			var anim = entity.GetComponent<PlayerAnimationState>();
			var t = entity.GetComponent<Transform>();
			if (anim == null || t == null) return;
			float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
			anim.DrawOffset = Vector2.Zero;
			if (anim.AttackAnimTimer > 0f)
			{
				anim.AttackAnimTimer = System.Math.Max(0f, anim.AttackAnimTimer - dt);
				float ta = 1f - (anim.AttackAnimTimer / System.Math.Max(0.0001f, anim.AttackAnimDuration));
				float outPhase = System.Math.Min(0.5f, ta) * 2f;
				float backPhase = System.Math.Max(0f, ta - 0.5f) * 2f;
				var basePos = t.Position;
				Vector2 outPos = anim.AttackTargetPos + _attackOffset;
				Vector2 mid = Vector2.Lerp(basePos, outPos, 1f - (float)System.Math.Pow(1f - outPhase, 3));
				var animPos = Vector2.Lerp(mid, basePos, backPhase);
				anim.DrawOffset = animPos - basePos;
				if (anim.AttackAnimTimer == 0f)
				{
					EventManager.Publish(new PlayerAttackImpactNow());
				}
			}
		}
	}
}


