using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Maintains the BattleInfo singleton. Increments TurnNumber when transitioning to Block phase
	/// after the player's Action phase ends.
	/// </summary>
	public class BattleInfoSystem : Core.System
	{
		public BattleInfoSystem(EntityManager em) : base(em)
		{
			EventManager.Subscribe<ChangeBattlePhaseEvent>(OnChangePhase);
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<BattleInfo>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		private BattleInfo GetOrCreateBattleInfo()
		{
			var e = EntityManager.GetEntitiesWithComponent<BattleInfo>().FirstOrDefault();
			if (e != null) return e.GetComponent<BattleInfo>();
			var world = EntityManager.CreateEntity("BattleInfo");
			var info = new BattleInfo { TurnNumber = 1 };
			EntityManager.AddComponent(world, info);
			return info;
		}

		private void OnChangePhase(ChangeBattlePhaseEvent evt)
		{
			if (evt.Previous == BattlePhase.Action)
			{
				var info = GetOrCreateBattleInfo();
				info.TurnNumber++;
				System.Console.WriteLine($"[BattleInfoSystem] Advancing to enemy turn {info.TurnNumber}");
			}
		}
	}
}



