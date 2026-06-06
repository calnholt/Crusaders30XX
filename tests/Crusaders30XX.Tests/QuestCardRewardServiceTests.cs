using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Data.Save;
using Crusaders30XX.ECS.Services;
using Xunit;

namespace Crusaders30XX.Tests;

public class QuestCardRewardServiceTests
{
	[Fact]
	public void GetEligibleRewardCardIds_excludes_default_starter_pool()
	{
		var starterPool = new HashSet<string>(
			StartingDeckGeneratorService.DefaultStarterCardPool,
			StringComparer.OrdinalIgnoreCase);

		var eligible = QuestCardRewardService.GetEligibleRewardCardIdsForTests(Array.Empty<string>());

		Assert.NotEmpty(eligible);
		foreach (var cardId in eligible)
		{
			Assert.False(
				starterPool.Contains(cardId),
				$"eligible quest reward included starter pool card {cardId}");
		}
	}

	[Fact]
	public void GenerateRandomCardChoices_returns_distinct_card_ids()
	{
		var choices = QuestCardRewardService.GenerateRandomCardChoices(Array.Empty<string>(), 2);

		Assert.Equal(2, choices.Count);
		Assert.Equal(2, choices.Select(c => c.CardId).Distinct(StringComparer.OrdinalIgnoreCase).Count());
	}

	[Fact]
	public void GenerateRandomCardChoices_does_not_include_existing_exact_deck_keys()
	{
		var deckKeys = new List<string> { "smite|Red" };

		var choices = QuestCardRewardService.GenerateRandomCardChoices(deckKeys, 20);

		Assert.DoesNotContain(choices, c => string.Equals(c.CardKey, "smite|Red", StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public void GenerateRandomCardChoices_does_not_mutate_loadout()
	{
		SaveCache.DeleteSaveFilesIfPresent();
		SaveCache.StartNewRun();
		var before = SaveCache.GetLoadout(RunDeckService.PrimaryLoadoutId).cardIds.ToList();

		QuestCardRewardService.GenerateRandomCardChoices();

		var after = SaveCache.GetLoadout(RunDeckService.PrimaryLoadoutId).cardIds.ToList();
		Assert.Equal(before, after);
	}

	[Fact]
	public void GrantCard_adds_selected_key_once()
	{
		SaveCache.DeleteSaveFilesIfPresent();
		SaveCache.StartNewRun();
		const string cardKey = "smite|Red";
		var beforeCount = SaveCache.GetLoadout(RunDeckService.PrimaryLoadoutId).cardIds.Count(k => string.Equals(k, cardKey, StringComparison.OrdinalIgnoreCase));

		var result = QuestCardRewardService.GrantCard(cardKey);

		var afterCount = SaveCache.GetLoadout(RunDeckService.PrimaryLoadoutId).cardIds.Count(k => string.Equals(k, cardKey, StringComparison.OrdinalIgnoreCase));
		Assert.True(result.Granted);
		Assert.Equal(beforeCount + 1, afterCount);
	}

	[Fact]
	public void IsInDefaultStarterPool_recognizes_all_default_starter_cards()
	{
		foreach (var cardId in StartingDeckGeneratorService.DefaultStarterCardPool)
		{
			Assert.True(StartingDeckGeneratorService.IsInDefaultStarterPool(cardId));
		}
	}
}
