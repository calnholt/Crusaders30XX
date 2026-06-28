using System.Collections.Generic;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Loadouts;
using Crusaders30XX.ECS.Data.Save;
using Crusaders30XX.ECS.Factories;
using Crusaders30XX.ECS.Objects.Cards;
using Crusaders30XX.ECS.Services;
using Crusaders30XX.ECS.Systems;
using Xunit;

namespace Crusaders30XX.Tests;

public sealed class RewardModalDisplaySystemTests
{
	private static readonly string[] AllPreviewRestrictions =
	{
		RunScopedStateService.RestrictionFrozen,
		RunScopedStateService.RestrictionSealed,
		RunScopedStateService.RestrictionBrittle,
		RunScopedStateService.RestrictionCursed,
	};

	[Fact]
	public void ApplyDeckRewardPreviewRestrictions_outgoing_card_hydrates_saved_restrictions()
	{
		var entityManager = new EntityManager();
		string entryId = SeedRestrictedEntry("smite|White", AllPreviewRestrictions);
		var option = new DeckRewardOfferOptionSave
		{
			kind = DeckRewardOfferKinds.Exchange,
			outgoingEntryId = entryId,
			outgoingCardKey = "smite|White",
			incomingCardKey = "fervor|Red",
		};
		var outgoing = CreatePreviewCard(entityManager, option.outgoingCardKey);

		RewardModalDisplaySystem.ApplyDeckRewardPreviewRestrictions(entityManager, outgoing, option, forIncomingCard: false);

		AssertHasAllPreviewRestrictions(outgoing);
	}

	[Fact]
	public void ApplyDeckRewardPreviewRestrictions_upgrade_incoming_card_copies_outgoing_restrictions()
	{
		var entityManager = new EntityManager();
		string entryId = SeedRestrictedEntry("smite|White", AllPreviewRestrictions);
		var option = new DeckRewardOfferOptionSave
		{
			kind = DeckRewardOfferKinds.Upgrade,
			outgoingEntryId = entryId,
			outgoingCardKey = "smite|White",
			upgradedCardKey = "smite|White|Upgraded",
		};
		var incoming = CreatePreviewCard(entityManager, option.upgradedCardKey);

		RewardModalDisplaySystem.ApplyDeckRewardPreviewRestrictions(entityManager, incoming, option, forIncomingCard: true);

		AssertHasAllPreviewRestrictions(incoming);
	}

	[Fact]
	public void ApplyDeckRewardPreviewRestrictions_exchange_incoming_card_copies_outgoing_restrictions()
	{
		var entityManager = new EntityManager();
		string entryId = SeedRestrictedEntry("smite|White", AllPreviewRestrictions);
		var option = new DeckRewardOfferOptionSave
		{
			kind = DeckRewardOfferKinds.Exchange,
			outgoingEntryId = entryId,
			outgoingCardKey = "smite|White",
			incomingCardKey = "fervor|Red",
		};
		var outgoing = CreatePreviewCard(entityManager, option.outgoingCardKey);
		var incoming = CreatePreviewCard(entityManager, option.incomingCardKey);

		RewardModalDisplaySystem.ApplyDeckRewardPreviewRestrictions(entityManager, outgoing, option, forIncomingCard: false);
		RewardModalDisplaySystem.ApplyDeckRewardPreviewRestrictions(entityManager, incoming, option, forIncomingCard: true);

		AssertHasAllPreviewRestrictions(outgoing);
		AssertHasAllPreviewRestrictions(incoming);
	}

	private static string SeedRestrictedEntry(string cardKey, IEnumerable<string> restrictions)
	{
		SaveCache.DeleteSaveFilesIfPresent();
		SaveCache.StartNewRun();
		var loadout = SaveCache.GetLoadout(RunDeckService.PrimaryLoadoutId);
		loadout.cards = new List<LoadoutCardEntry>
		{
			new()
			{
				entryId = "reward_modal_test_entry",
				cardKey = cardKey,
				isStarter = true,
				countsAsTraded = false,
				restrictions = new List<string>(),
			}
		};
		foreach (var restriction in restrictions)
		{
			loadout.cards[0].restrictions.Add(restriction);
		}
		SaveCache.SaveLoadout(loadout);
		return loadout.cards[0].entryId;
	}

	private static Entity CreatePreviewCard(EntityManager entityManager, string cardKey)
	{
		if (!RunDeckService.TryParseCardKey(cardKey, out var cardId, out var color, out var isUpgraded)) return null;
		return EntityFactory.CreateCardFromDefinition(
			entityManager,
			cardId,
			color,
			suppressStatDeltaDisplay: true,
			isUpgraded: isUpgraded);
	}

	private static void AssertHasAllPreviewRestrictions(Entity card)
	{
		Assert.NotNull(card);
		Assert.True(card.HasComponent<Frozen>());
		Assert.True(card.HasComponent<Sealed>());
		Assert.True(card.HasComponent<Brittle>());
		Assert.True(card.HasComponent<Cursed>());
		Assert.Equal(Curse.CardIdValue, card.GetComponent<CardData>()?.Card?.CardId);
	}

}
