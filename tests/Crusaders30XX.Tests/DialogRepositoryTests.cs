using Crusaders30XX.ECS.Data.Dialog;
using Crusaders30XX.ECS.Systems;
using Xunit;

namespace Crusaders30XX.Tests;

public class DialogCatalogTests
{
	[Fact]
	public void Catalog_contains_desert_dialogue_and_fallen_shepherd_segments()
	{
		Assert.True(DialogCatalog.TryGet("desert_1", out var desert));
		Assert.NotEmpty(desert.ResolveSegment(string.Empty));
		Assert.True(DialogCatalog.TryGet("fallen_shepherd", out var shepherd));
		Assert.Equal("Fallen Shepherd", shepherd.ResolveSegment("intro")[0].actor);
		Assert.Single(shepherd.ResolveSegment("victory"));
		Assert.Empty(shepherd.ResolveSegment("missing"));
	}

	[Fact]
	public void Guided_tutorial_dialogue_uses_supplied_lines_and_remiel_portrait()
	{
		Assert.True(DialogCatalog.TryGet("guided_tutorial", out var tutorial));

		var intro = tutorial.ResolveSegment("intro");
		Assert.Equal(8, intro.Count);
		Assert.Equal("Remiel", intro[0].actor);
		Assert.Equal("There you are, eyes open. Now I need you awake a great deal faster, because we have a problem brewing about thirty feet to your left.", intro[0].message);
		Assert.Equal("Crusader", intro[7].actor);
		Assert.Equal("Then it does not need explaining. It needs to be put down.", intro[7].message);

		var victory = tutorial.ResolveSegment("gleeber_victory");
		Assert.Equal(2, victory.Count);
		Assert.Equal("One down. One more to go. And now I have my sword. Keep up.", victory[0].message);
		Assert.Equal("Remiel", victory[1].actor);
		Assert.Equal("Trying!", victory[1].message);

		Assert.Equal("guardian_angel", DialogDisplaySystem.ResolvePortraitAssetName("Remiel"));
		Assert.Equal("crusader_sword", DialogDisplaySystem.ResolvePortraitAssetName("Crusader"));
	}
}
