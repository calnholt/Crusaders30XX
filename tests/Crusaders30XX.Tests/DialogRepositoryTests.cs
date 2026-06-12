using Crusaders30XX.ECS.Data.Dialog;
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
}
