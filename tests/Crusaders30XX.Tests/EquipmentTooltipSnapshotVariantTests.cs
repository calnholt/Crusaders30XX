using Crusaders30XX.Diagnostics.Snapshots;
using Crusaders30XX.Diagnostics.Snapshots.Fixtures;
using Xunit;

namespace Crusaders30XX.Tests;

public class EquipmentTooltipSnapshotVariantTests
{
	[Theory]
	[InlineData("active", EquipmentTooltipSnapshotVariantId.Active, "active", "helm_of_seeing", false)]
	[InlineData("passive", EquipmentTooltipSnapshotVariantId.Passive, "passive", "knightly_grieves", false)]
	[InlineData("out-of-uses", EquipmentTooltipSnapshotVariantId.OutOfUses, "out-of-uses", "helm_of_seeing", true)]
	public void Parses_supported_variants(
		string token,
		EquipmentTooltipSnapshotVariantId expectedId,
		string expectedSlug,
		string expectedEquipmentId,
		bool expectedExhausted)
	{
		var variant = EquipmentTooltipSnapshotVariant.Parse([token]);

		Assert.Equal(expectedId, variant.Id);
		Assert.Equal(expectedSlug, variant.FileSlug);
		Assert.Equal(expectedEquipmentId, variant.EquipmentId);
		Assert.Equal(expectedExhausted, variant.Exhausted);
	}

	[Fact]
	public void Defaults_to_active()
	{
		var variant = EquipmentTooltipSnapshotVariant.Parse([]);

		Assert.Equal(EquipmentTooltipSnapshotVariantId.Active, variant.Id);
		Assert.Equal("active", variant.FileSlug);
	}

	[Fact]
	public void Rejects_unknown_or_extra_arguments()
	{
		Assert.Throws<DisplaySnapshotSetupException>(
			() => EquipmentTooltipSnapshotVariant.Parse(["unknown"]));
		Assert.Throws<DisplaySnapshotSetupException>(
			() => EquipmentTooltipSnapshotVariant.Parse(["active", "extra"]));
	}
}
