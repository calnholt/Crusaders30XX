using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.Diagnostics;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("Stored Block Management")]
	public class StoredBlockManagementSystem : Core.System
	{
		public StoredBlockManagementSystem(EntityManager entityManager) : base(entityManager)
		{
			EventManager.Subscribe<ModifyStoredBlock>(OnModifyStoredBlock);
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			// Stateless; nothing to iterate in Update
			return System.Array.Empty<Entity>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		private void OnModifyStoredBlock(ModifyStoredBlock e)
		{
			var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
			if (player == null) return;
			var stored = player.GetComponent<StoredBlock>();
			if (stored == null)
			{
				stored = new StoredBlock();
				EntityManager.AddComponent(player, stored);
			}
			long next = (long)stored.Amount + e.Delta;
			stored.Amount = next < 0 ? 0 : (int)next;
		}

		[DebugActionInt("Modify Stored Block", Step = 1, Min = -999, Max = 999, Default = 5)]
		public void Debug_ModifyStoredBlock(int delta)
		{
			EventManager.Publish(new ModifyStoredBlock { Delta = delta });
		}
	}
}


