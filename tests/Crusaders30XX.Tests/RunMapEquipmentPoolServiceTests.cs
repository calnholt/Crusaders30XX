using System.Collections.Generic;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Data.Loadouts;
using Crusaders30XX.ECS.Data.Locations;
using Crusaders30XX.ECS.Factories;
using Crusaders30XX.ECS.Services;
using Xunit;

namespace Crusaders30XX.Tests;

public class RunMapEquipmentPoolServiceTests
{
	[Fact]
	public void BuildEligibleEquipmentIds_excludes_equipped_ids()
	{
		var loadout = new LoadoutDefinition
		{
			id = "loadout_1",
			headId = "helm_of_seeing",
		};

		var pool = RunMapEquipmentPoolService.BuildEligibleEquipmentIds(loadout, shops: null, excludeShopOffers: false);

		Assert.DoesNotContain("helm_of_seeing", pool);
		Assert.Contains("knightly_helm", pool);
	}

	[Fact]
	public void BuildEligibleEquipmentIds_excludes_shop_offers_when_requested()
	{
		var shops = new List<RunMapShop>
		{
			new RunMapShop
			{
				id = "shop_0",
				items =
				[
					new RunMapShopItem
					{
						itemType = RunMapShopItem.ItemTypeEquipment,
						cardId = "purging_bracers",
					},
				],
			},
		};

		var pool = RunMapEquipmentPoolService.BuildEligibleEquipmentIds(
			loadout: null,
			shops,
			excludeShopOffers: true);

		Assert.DoesNotContain("purging_bracers", pool);
	}

	[Fact]
	public void BuildEligibleEquipmentIds_excludes_requested_slot_types()
	{
		var pool = RunMapEquipmentPoolService.BuildEligibleEquipmentIds(
			loadout: null,
			shops: null,
			excludeShopOffers: false,
			excludedSlots: new[] { EquipmentSlot.Arms });

		Assert.DoesNotContain("purging_bracers", pool);
		Assert.DoesNotContain("knightly_gauntlets", pool);
		Assert.Contains("helm_of_seeing", pool);
	}

	[Fact]
	public void PickRandomEquipment_for_treasure_excludes_shop_slot_type()
	{
		var shops = new List<RunMapShop>
		{
			new RunMapShop
			{
				id = "shop_1",
				items =
				[
					new RunMapShopItem
					{
						itemType = RunMapShopItem.ItemTypeEquipment,
						cardId = "purging_bracers",
					},
				],
			},
		};

		var shopEquipment = EquipmentFactory.Create("purging_bracers");
		var rng = new System.Random(42);
		for (int i = 0; i < 32; i++)
		{
			string picked = RunMapEquipmentPoolService.PickRandomEquipment(
				rng,
				loadout: null,
				shops,
				excludeShopOffers: true);
			var pickedEquipment = EquipmentFactory.Create(picked);
			Assert.NotEqual(shopEquipment.Slot, pickedEquipment.Slot);
			Assert.NotEqual("purging_bracers", picked);
		}
	}

	[Fact]
	public void ApplyEquipmentToLoadout_writes_correct_slot_field()
	{
		var loadout = new LoadoutDefinition { id = "loadout_1" };

		RunMapEquipmentPoolService.ApplyEquipmentToLoadout(loadout, "knightly_chest");

		Assert.Equal("knightly_chest", loadout.chestId);
	}
}
