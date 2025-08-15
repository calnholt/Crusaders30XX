using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.Diagnostics;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("HP Management")]
	public class HpManagementSystem : Core.System
	{
		public HpManagementSystem(EntityManager entityManager) : base(entityManager)
		{
			EventManager.Subscribe<ModifyHpEvent>(OnModifyHp);
			EventManager.Subscribe<SetHpEvent>(OnSetHp);
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<HP>();
		}

		protected override void UpdateEntity(Entity entity, Microsoft.Xna.Framework.GameTime gameTime) { }

		private void OnModifyHp(ModifyHpEvent e)
		{
			var target = ResolveTarget(e.Target);
			if (target == null) return;
			var hp = target.GetComponent<HP>();
			if (hp == null) return;
			int nv = hp.Current + e.Delta;
			hp.Current = System.Math.Max(0, System.Math.Min(hp.Max, nv));
		}

		private void OnSetHp(SetHpEvent e)
		{
			var target = ResolveTarget(e.Target);
			if (target == null) return;
			var hp = target.GetComponent<HP>();
			if (hp == null) return;
			hp.Current = System.Math.Max(0, System.Math.Min(hp.Max, e.Value));
		}

		private Entity ResolveTarget(Entity explicitTarget)
		{
			if (explicitTarget != null) return explicitTarget;
			// Default: first Player with HP
			var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
			if (player != null && player.HasComponent<HP>()) return player;
			return EntityManager.GetEntitiesWithComponent<HP>().FirstOrDefault();
		}

		// Debug action: Lose X HP
		[DebugActionInt("Lose HP", Step = 1, Min = 1, Max = 999, Default = 10)]
		public void Debug_LoseHp(int amount)
		{
			EventManager.Publish(new ModifyHpEvent { Delta = -System.Math.Abs(amount) });
		}

		// Debug action: Heal X HP
		[DebugActionInt("Heal HP", Step = 1, Min = 1, Max = 999, Default = 10)]
		public void Debug_HealHp(int amount)
		{
			EventManager.Publish(new ModifyHpEvent { Delta = System.Math.Abs(amount) });
		}
	}
}


