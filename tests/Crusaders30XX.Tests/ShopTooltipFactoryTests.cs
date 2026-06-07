using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Locations;
using Crusaders30XX.ECS.Factories;
using Xunit;

namespace Crusaders30XX.Tests;

public class ShopTooltipFactoryTests
{
	[Fact]
	public void Run_shop_card_uses_standard_card_tooltip_above()
	{
		var entityManager = new EntityManager();
		var normalCard = EntityFactory.CreateCardFromDefinition(
			entityManager,
			"strike",
			CardData.CardColor.Red);
		var shopCard = CreateRunShopItem(entityManager, new RunMapShopItem
		{
			cardId = "strike",
			color = "Red",
		});

		var normalUi = normalCard.GetComponent<UIElement>();
		var shopUi = shopCard.GetComponent<UIElement>();

		Assert.Equal(normalUi.Tooltip, shopUi.Tooltip);
		Assert.Equal(normalUi.TooltipType, shopUi.TooltipType);
		Assert.Equal(normalUi.TooltipOffsetPx, shopUi.TooltipOffsetPx);
		Assert.Equal(TooltipPosition.Above, shopUi.TooltipPosition);
		Assert.Null(shopCard.GetComponent<CardTooltip>());
	}

	[Fact]
	public void Run_shop_card_preserves_definition_card_tooltip_defaults()
	{
		var entityManager = new EntityManager();
		var shopCard = CreateRunShopItem(entityManager, new RunMapShopItem
		{
			cardId = "kunai",
			color = "Red",
		});

		var ui = shopCard.GetComponent<UIElement>();
		var tooltip = shopCard.GetComponent<CardTooltip>();

		Assert.Equal(TooltipType.Card, ui.TooltipType);
		Assert.Equal(TooltipPosition.Above, ui.TooltipPosition);
		Assert.Equal("kunai", tooltip.CardId);
		Assert.Equal(0.6f, tooltip.TooltipScale);
		Assert.Equal(CardData.CardColor.Red, tooltip.CardColor);
	}

	[Fact]
	public void Run_shop_medal_uses_standard_medal_tooltip_above()
	{
		var entityManager = new EntityManager();
		var shopMedal = CreateRunShopItem(entityManager, new RunMapShopItem
		{
			itemType = RunMapShopItem.ItemTypeMedal,
			cardId = "st_luke",
		});

		var medal = MedalFactory.Create("st_luke");
		var ui = shopMedal.GetComponent<UIElement>();

		Assert.Equal($"{medal.Name}\n\n{medal.Text}", ui.Tooltip);
		Assert.Equal(TooltipType.Text, ui.TooltipType);
		Assert.Equal(TooltipPosition.Above, ui.TooltipPosition);
	}

	private static Entity CreateRunShopItem(EntityManager entityManager, RunMapShopItem item)
	{
		var shop = new RunMapShop
		{
			id = "tooltip_test_shop",
			items = [item],
		};

		return EntityFactory.CreateForSaleFromRunShop(entityManager, shop).Single();
	}
}
