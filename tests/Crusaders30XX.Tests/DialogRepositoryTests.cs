using System;
using System.IO;
using Crusaders30XX.ECS.Data.Dialog;
using Xunit;

namespace Crusaders30XX.Tests;

public class DialogRepositoryTests
{
	[Fact]
	public void LoadFromFolder_supports_legacy_lines_and_named_segments()
	{
		string folder = Path.Combine(Path.GetTempPath(), $"crusaders-dialog-{Guid.NewGuid():N}");
		Directory.CreateDirectory(folder);
		try
		{
			File.WriteAllText(Path.Combine(folder, "legacy.json"),
				"""{"lines":[{"actor":"Angel","message":"Legacy"}]}""");
			File.WriteAllText(Path.Combine(folder, "segmented.json"),
				"""{"segments":{"intro":[{"actor":"Fallen Shepherd","message":"..."}],"victory":[{"actor":"Fallen Shepherd","message":"..."}]}}""");

			var definitions = DialogRepository.LoadFromFolder(folder);

			Assert.Equal("Legacy", definitions["legacy"].ResolveSegment(string.Empty)[0].message);
			Assert.Equal("Fallen Shepherd", definitions["segmented"].ResolveSegment("intro")[0].actor);
			Assert.Single(definitions["segmented"].ResolveSegment("victory"));
			Assert.Empty(definitions["segmented"].ResolveSegment("missing"));
		}
		finally
		{
			Directory.Delete(folder, recursive: true);
		}
	}
}
