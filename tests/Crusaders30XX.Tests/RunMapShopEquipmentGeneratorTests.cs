using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Data.Locations;
using Crusaders30XX.ECS.Factories;
using Crusaders30XX.ECS.Services;
using Xunit;

namespace Crusaders30XX.Tests;

public class RunMapShopEquipmentGeneratorTests
{
	[Fact]
	public void Generate_places_exactly_one_equipment_offer_not_on_medal_shop()
	{
		const int attempts = 24;
		for (int i = 0; i < attempts; i++)
		{
			var (seed, nodes) = LocationMapGeneratorService.Generate();
			var shops = RunMapShopGeneratorService.Generate(seed, nodes);

			int equipmentOffers = 0;
			int medalOffers = 0;
			string equipmentId = null;
			foreach (var shop in shops)
			{
				if (shop?.items == null) continue;
				bool hasEquipment = shop.items.Any(item => item?.IsEquipment == true);
				bool hasMedal = shop.items.Any(item => item?.IsMedal == true);
				if (hasEquipment)
				{
					equipmentOffers++;
					equipmentId = shop.items.First(item => item.IsEquipment).cardId;
					Assert.False(hasMedal, $"seed {seed} shop {shop.id} has both medal and equipment");
				}
				if (hasMedal) medalOffers++;
			}

			Assert.Equal(1, equipmentOffers);
			Assert.Equal(1, medalOffers);
			Assert.False(string.IsNullOrWhiteSpace(equipmentId));
		}
	}

	[Fact]
	public void Generate_shop_and_treasure_equipment_use_different_slots_at_claim()
	{
		const int attempts = 24;
		for (int i = 0; i < attempts; i++)
		{
			var (seed, nodes) = LocationMapGeneratorService.Generate();
			var shops = RunMapShopGeneratorService.Generate(seed, nodes);
			var treasures = RunMapTreasureGeneratorService.Generate(seed, nodes, shops);

			string shopEquipmentId = shops
				.SelectMany(s => s.items ?? new List<RunMapShopItem>())
				.First(item => item.IsEquipment)
				.cardId;
			var shopSlot = EquipmentFactory.Create(shopEquipmentId).Slot;

			var equipmentChest = treasures.Single(t => t.grantsEquipmentReward);
			var rng = new System.Random((seed ^ 0x71EA5A71) + int.Parse(equipmentChest.id.Split('_')[1]));
			string chestEquipmentId = RunMapEquipmentPoolService.PickRandomEquipment(
				rng,
				loadout: null,
				shops,
				excludeShopOffers: true);
			var chestSlot = EquipmentFactory.Create(chestEquipmentId).Slot;

			Assert.NotEqual(shopSlot, chestSlot);
			Assert.NotEqual(shopEquipmentId, chestEquipmentId);
		}
	}
}
