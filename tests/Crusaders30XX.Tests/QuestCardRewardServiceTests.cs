using System;
using System.Collections.Generic;
using System.Linq;
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
	public void IsInDefaultStarterPool_recognizes_all_default_starter_cards()
	{
		foreach (var cardId in StartingDeckGeneratorService.DefaultStarterCardPool)
		{
			Assert.True(StartingDeckGeneratorService.IsInDefaultStarterPool(cardId));
		}
	}
}
