using Crusaders30XX.ECS.Factories;
using Crusaders30XX.ECS.Services;
using Xunit;

namespace Crusaders30XX.Tests;

public class EquipmentServiceTests
{
	[Fact]
	public void Simple_tooltip_orders_rules_before_flavor_and_includes_free_action()
	{
		var equipment = EquipmentFactory.Create("helm_of_seeing");
		equipment.FlavorText = "A lens polished under a red moon.";

		string tooltip = EquipmentService.GetTooltipText(
			equipment,
			EquipmentTooltipType.Shop);

		Assert.True(tooltip.IndexOf(equipment.Text) < tooltip.IndexOf(equipment.FlavorText));
		Assert.Contains("Block: 2 | Uses: 1", tooltip);
		Assert.EndsWith("Free Action", tooltip);
	}
}
