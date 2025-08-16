using System;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Applies effects published by AttackResolutionSystem. For now, supports Damage and ApplyStatus (log-only).
	/// </summary>
	public class EffectApplicationSystem : Core.System
	{
		public EffectApplicationSystem(EntityManager entityManager) : base(entityManager)
		{
			EventManager.Subscribe<ApplyEffect>(OnApplyEffect);
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return Array.Empty<Entity>();
		}

		protected override void UpdateEntity(Entity entity, Microsoft.Xna.Framework.GameTime gameTime) { }

		private void OnApplyEffect(ApplyEffect e)
		{
			string type = e.EffectType ?? string.Empty;
			switch (type)
			{
				case "Damage":
					Crusaders30XX.ECS.Core.EventManager.Publish(new ModifyHpEvent { Target = e.Target, Delta = -System.Math.Max(0, e.Amount) });
					break;
				case "ApplyStatus":
					System.Console.WriteLine($"[Effect] ApplyStatus status={e.Status} stacks={e.Stacks} to target={(e.Target != null ? e.Target.Name : "(default)")}");
					break;
				default:
					System.Console.WriteLine($"[Effect] Unsupported effect type '{type}'");
					break;
			}
		}
	}
}


