using System.Collections.Generic;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Save;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Services;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Systems
{
	public class TreasureStartSystem : Core.System
	{
		public TreasureStartSystem(EntityManager entityManager) : base(entityManager)
		{
			EventManager.Subscribe<HotKeyHoldCompletedEvent>(OnHotKeyHoldCompleted);
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			yield break;
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		private void OnHotKeyHoldCompleted(HotKeyHoldCompletedEvent evt)
		{
			var ent = evt?.Entity;
			if (ent == null) return;

			var hotKey = ent.GetComponent<HotKey>();
			Entity treasureEntity = ent;
			if (hotKey?.ParentEntity != null)
			{
				treasureEntity = hotKey.ParentEntity;
			}

			var poi = treasureEntity.GetComponent<PointOfInterest>();
			if (poi == null || poi.Type != PointOfInterestType.Treasure) return;

			string treasureId = !string.IsNullOrEmpty(poi.TreasureId) ? poi.TreasureId : poi.Id;
			if (string.IsNullOrEmpty(treasureId)) return;

			if (!SaveCache.TryGetRunTreasure(treasureId, out var treasure, out _)) return;
			if (!RunMapTreasureService.IsEnterable(treasure, SaveCache.GetRunMapNodes())) return;

			if (!SaveCache.TryClaimRunMapTreasure(treasureId, EntityManager, out int rewardGold, out string rewardMedalId))
			{
				return;
			}

			RunMedalService.AcquireAndEquip(EntityManager, rewardMedalId);

			EventManager.Publish(new TreasureChestOpened
			{
				RewardGold = rewardGold,
				RewardMedalId = rewardMedalId,
			});
		}
	}
}
