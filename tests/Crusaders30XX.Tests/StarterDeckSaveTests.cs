using System.Linq;
using Crusaders30XX.ECS.Data.Loadouts;
using Crusaders30XX.ECS.Data.Save;
using Crusaders30XX.ECS.Services;
using Xunit;

namespace Crusaders30XX.Tests;

public class StarterDeckSaveTests
{
	[Fact]
	public void StartingDeckGenerator_builds_full_starting_deck()
	{
		var deck = StartingDeckGeneratorService.Generate(
			StartingDeckGeneratorService.DefaultStarterCardPool,
			12345);

		Assert.Equal(DeckRules.StartingDeckSize, deck.Count);
	}

	[Fact]
	public void StartNewRun_persists_starterCardKeys_matching_loadout()
	{
		SaveCache.DeleteSaveFilesIfPresent();
		SaveCache.StartNewRun();

		var save = SaveCache.GetAll();
		var loadout = save.loadouts?.FirstOrDefault(l => l.id == "loadout_1");

		Assert.NotNull(loadout);
		Assert.Equal(DeckRules.StartingDeckSize, save.starterCardKeys?.Count ?? 0);
		Assert.Equal(loadout.cardIds.Count, save.starterCardKeys.Count);
		foreach (var key in save.starterCardKeys)
		{
			Assert.Contains(key, loadout.cardIds);
		}
	}
}
