using System.Collections.Generic;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Save;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Services;
using Crusaders30XX.ECS.Singletons;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Systems
{
	public class ShopStartSystem : Core.System
	{
		public ShopStartSystem(EntityManager entityManager) : base(entityManager)
		{
			EventManager.Subscribe<OpenRunShopRequested>(OnOpenRunShopRequested);
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
			Entity shopEntity = ent;
			if (hotKey?.ParentEntity != null)
			{
				shopEntity = hotKey.ParentEntity;
			}

			var poi = shopEntity.GetComponent<PointOfInterest>();
			if (poi == null || poi.Type != PointOfInterestType.Shop) return;

			OnOpenRunShopRequested(new OpenRunShopRequested { Entity = shopEntity, ShopId = poi.ShopId });
		}

		private void OnOpenRunShopRequested(OpenRunShopRequested e)
		{
			if (e?.Entity == null) return;

			var poi = e.Entity.GetComponent<PointOfInterest>();
			if (poi == null || poi.Type != PointOfInterestType.Shop) return;

			string shopId = !string.IsNullOrEmpty(e.ShopId) ? e.ShopId : poi.ShopId;
			if (string.IsNullOrEmpty(shopId)) return;

			if (!SaveCache.TryGetRunShop(shopId, out var shop, out _)) return;
			if (!RunMapShopService.IsEnterable(shop, SaveCache.GetRunMapNodes())) return;

			StateSingleton.ActiveRunShopId = shopId;
			string title = string.IsNullOrWhiteSpace(shop.displayName) ? "Shop" : shop.displayName;
			EventManager.Publish(new SetShopTitle
			{
				Title = title,
				ShopId = shopId,
				BackgroundAsset = shop.backgroundAsset ?? string.Empty,
			});
			EventManager.Publish(new ShowTransition { Scene = SceneId.Shop, SkipHold = true });
		}
	}
}
