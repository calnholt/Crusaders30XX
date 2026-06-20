using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Loadouts;
using Crusaders30XX.ECS.Data.Save;
using Crusaders30XX.ECS.Objects.Cards;
using Crusaders30XX.ECS.Services;
using Xunit;

namespace Crusaders30XX.Tests;

public class QuestCardRewardServiceTests
{
	private static readonly string[] ExpectedSharedIncomingPool =
	{
		"strike",
		"crusade",
		"zealous_vow",
		"tempest",
		"shield_of_faith",
		"increase_faith",
		"renounce_and_hone",
		"steel_the_spirit",
		"iron_covenant",
		"whirlwind",
		"pouch_of_kunai",
		"ravage",
		"reap",
		"relentless_strike",
		"serpent_crush",
		"stalwart",
		"temper_the_blade",
		"vindicate",
		"vanguards_promise",
		"steadfast_resolve",
		"exaltation",
		"deus_vult",
		"carpe_diem",
		"crimson_rite",
		"consecrate",
		"ark_of_the_covenant",
		"dowse_with_holy_water",
		"fury"
	};

	[Fact]
	public void GenerateDeckRewardOffer_prioritizes_starter_exchange_targets()
	{
		var deckKeys = new List<string>
		{
			"fervor|Red",
			"smite|White",
			"reckoning|Black",
			"seize|Red"
		};

		var offer = QuestCardRewardService.GenerateDeckRewardOffer(deckKeys, "sword", 20);

		Assert.True(offer.options.Count >= 2);
		Assert.Equal("smite|White", offer.options[0].outgoingCardKey);
		Assert.Equal("reckoning|Black", offer.options[1].outgoingCardKey);
		Assert.All(offer.options.Where(o => o.kind == DeckRewardOfferKinds.Exchange), option =>
		{
			Assert.NotEqual(
				RunDeckService.TryParseCardKey(option.outgoingCardKey, out var outgoingId, out _) ? outgoingId : string.Empty,
				RunDeckService.TryParseCardKey(option.incomingCardKey, out var incomingId, out _) ? incomingId : string.Empty,
				StringComparer.OrdinalIgnoreCase);
		});
	}

	[Fact]
	public void Incoming_exchange_pool_is_exact_shared_list_for_sword_and_dagger()
	{
		Assert.Equal(
			ExpectedSharedIncomingPool.Order(StringComparer.OrdinalIgnoreCase),
			QuestCardRewardService.GetEligibleRewardCardIdsForTests(Array.Empty<string>(), "sword")
				.Order(StringComparer.OrdinalIgnoreCase));

		Assert.Equal(
			ExpectedSharedIncomingPool.Order(StringComparer.OrdinalIgnoreCase),
			QuestCardRewardService.GetEligibleRewardCardIdsForTests(Array.Empty<string>(), "dagger")
				.Order(StringComparer.OrdinalIgnoreCase));
	}

	[Fact]
	public void Incoming_exchange_pool_adds_only_hammer_specific_cards_for_hammer()
	{
		var expected = ExpectedSharedIncomingPool
			.Concat(new[] { "unburdened_strike", "battering_blow" })
			.Order(StringComparer.OrdinalIgnoreCase);

		Assert.Equal(
			expected,
			QuestCardRewardService.GetEligibleRewardCardIdsForTests(Array.Empty<string>(), "hammer")
				.Order(StringComparer.OrdinalIgnoreCase));
	}

	[Fact]
	public void Incoming_exchange_pool_normalizes_exhaltation_to_existing_exaltation()
	{
		var pool = QuestCardRewardService.GetEligibleRewardCardIdsForTests(Array.Empty<string>(), "sword");

		Assert.Contains("exaltation", pool, StringComparer.OrdinalIgnoreCase);
		Assert.DoesNotContain("exhaltation", pool, StringComparer.OrdinalIgnoreCase);
	}

	[Fact]
	public void Generated_exchange_incoming_cards_are_only_from_shared_and_weapon_specific_pools()
	{
		var deckKeys = new List<string>
		{
			"smite|White",
			"reckoning|Black",
			"fervor|Red"
		};
		var swordAllowed = new HashSet<string>(ExpectedSharedIncomingPool, StringComparer.OrdinalIgnoreCase);
		var hammerAllowed = new HashSet<string>(
			ExpectedSharedIncomingPool.Concat(new[] { "unburdened_strike", "battering_blow" }),
			StringComparer.OrdinalIgnoreCase);

		for (int i = 0; i < 100; i++)
		{
			AssertExchangeIncomingPool(deckKeys, "sword", swordAllowed);
			AssertExchangeIncomingPool(deckKeys, "dagger", swordAllowed);
			AssertExchangeIncomingPool(deckKeys, "hammer", hammerAllowed);
		}
	}

	[Fact]
	public void GenerateDeckRewardOffer_excludes_upgraded_cards_from_targeting()
	{
		var deckKeys = new List<string>
		{
			"smite|White|Upgraded",
			"reckoning|Black|Upgraded",
			"fervor|Red"
		};

		var offer = QuestCardRewardService.GenerateDeckRewardOffer(deckKeys, "sword", 20);

		Assert.DoesNotContain(offer.options, o => o.outgoingCardKey.Contains("Upgraded", StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public void ApplyPendingOfferOption_replaces_card_at_same_loadout_position_and_allows_copy_limit_bypass()
	{
		SaveCache.DeleteSaveFilesIfPresent();
		SaveCache.StartNewRun();
		var loadout = SaveCache.GetLoadout(RunDeckService.PrimaryLoadoutId);
		loadout.cards = Entries(
			"smite|White",
			"fervor|Red",
			"fervor|White");
		loadout.cards[0].restrictions.Add(RunScopedStateService.RestrictionFrozen);
		SaveCache.SaveLoadout(loadout);
		var outgoingEntryId = loadout.cards[0].entryId;

		SaveCache.SetPendingDeckRewardOffer(new DeckRewardOfferSave
		{
			rewardGold = 20,
			options = new List<DeckRewardOfferOptionSave>
			{
				new()
				{
					kind = DeckRewardOfferKinds.Exchange,
					loadoutIndex = 0,
					outgoingEntryId = outgoingEntryId,
					outgoingCardKey = "smite|White",
					incomingCardKey = "fervor|Red"
				}
			}
		});

		Assert.True(QuestCardRewardService.ApplyPendingOfferOption(0));

		var after = SaveCache.GetLoadout(RunDeckService.PrimaryLoadoutId).cards;
		Assert.Equal("fervor|Red", after[0].cardKey);
		Assert.NotEqual(outgoingEntryId, after[0].entryId);
		Assert.False(after[0].isStarter);
		Assert.True(after[0].countsAsTraded);
		Assert.Empty(after[0].restrictions);
		Assert.Equal(3, DeckRules.CountCardIdInDeck(after.Select(entry => entry.cardKey).ToList(), "fervor"));
		Assert.Null(SaveCache.GetPendingDeckRewardOffer());
	}

	[Fact]
	public void ApplyPendingOfferOption_upgrade_persists_upgraded_key_without_stat_mutation()
	{
		SaveCache.DeleteSaveFilesIfPresent();
		SaveCache.StartNewRun();
		var loadout = SaveCache.GetLoadout(RunDeckService.PrimaryLoadoutId);
		loadout.cards = Entries("smite|White");
		loadout.cards[0].countsAsTraded = true;
		loadout.cards[0].restrictions.Add(RunScopedStateService.RestrictionFrozen);
		SaveCache.SaveLoadout(loadout);
		var original = loadout.cards[0];

		SaveCache.SetPendingDeckRewardOffer(new DeckRewardOfferSave
		{
			options = new List<DeckRewardOfferOptionSave>
			{
				new()
				{
					kind = DeckRewardOfferKinds.Upgrade,
					loadoutIndex = 0,
					outgoingEntryId = original.entryId,
					outgoingCardKey = "smite|White",
					upgradedCardKey = "smite|White|Upgraded"
				}
			}
		});

		Assert.True(QuestCardRewardService.ApplyPendingOfferOption(0));

		var upgradedEntry = SaveCache.GetLoadout(RunDeckService.PrimaryLoadoutId).cards.Single();
		Assert.Equal(original.entryId, upgradedEntry.entryId);
		Assert.Equal("smite|White|Upgraded", upgradedEntry.cardKey);
		Assert.True(upgradedEntry.isStarter);
		Assert.True(upgradedEntry.countsAsTraded);
		Assert.Contains(RunScopedStateService.RestrictionFrozen, upgradedEntry.restrictions);
		var baseCard = Crusaders30XX.ECS.Factories.CardFactory.Create("smite");
		var upgradedCard = Crusaders30XX.ECS.Factories.CardFactory.Create("smite");
		upgradedCard.IsUpgraded = true;
		Assert.Equal(baseCard.Damage, upgradedCard.Damage);
		Assert.EndsWith("+", upgradedCard.DisplayName);
	}

	[Fact]
	public void SkipPendingOffer_clears_offer_without_deck_mutation()
	{
		SaveCache.DeleteSaveFilesIfPresent();
		SaveCache.StartNewRun();
		var before = SaveCache.GetLoadout(RunDeckService.PrimaryLoadoutId).cards
			.Select(entry => (entry.entryId, entry.cardKey))
			.ToList();
		SaveCache.SetPendingDeckRewardOffer(new DeckRewardOfferSave
		{
			options = new List<DeckRewardOfferOptionSave>
			{
				new()
				{
					kind = DeckRewardOfferKinds.Exchange,
					loadoutIndex = 0,
					outgoingEntryId = before[0].entryId,
					outgoingCardKey = before[0].cardKey,
					incomingCardKey = "fervor|Red"
				}
			}
		});

		QuestCardRewardService.SkipPendingOffer();

		Assert.Null(SaveCache.GetPendingDeckRewardOffer());
		Assert.Equal(
			before,
			SaveCache.GetLoadout(RunDeckService.PrimaryLoadoutId).cards
				.Select(entry => (entry.entryId, entry.cardKey))
				.ToList());
	}

	[Fact]
	public void PendingOffer_persists_across_save_reload()
	{
		SaveCache.DeleteSaveFilesIfPresent();
		SaveCache.StartNewRun();
		var outgoing = SaveCache.GetLoadout(RunDeckService.PrimaryLoadoutId).cards[0];
		SaveCache.SetPendingDeckRewardOffer(new DeckRewardOfferSave
		{
			rewardGold = 20,
			options = new List<DeckRewardOfferOptionSave>
			{
				new()
				{
					kind = DeckRewardOfferKinds.Upgrade,
					loadoutIndex = 0,
					outgoingEntryId = outgoing.entryId,
					outgoingCardKey = outgoing.cardKey,
					upgradedCardKey = RunDeckService.BuildUpgradedCardKey(outgoing.cardKey),
				}
			}
		});

		SaveCache.Reload();

		var pending = SaveCache.GetPendingDeckRewardOffer();
		Assert.NotNull(pending);
		Assert.Equal(20, pending.rewardGold);
		Assert.Single(pending.options);
		Assert.Equal(outgoing.entryId, pending.options[0].outgoingEntryId);
		Assert.Equal(RunDeckService.BuildUpgradedCardKey(outgoing.cardKey), pending.options[0].upgradedCardKey);
	}

	[Fact]
	public void CardKeyParser_accepts_legacy_and_upgraded_keys()
	{
		Assert.True(RunDeckService.TryParseCardKey("smite|white", out var legacyId, out var legacyColor, out var legacyUpgraded));
		Assert.Equal("smite", legacyId);
		Assert.Equal(Crusaders30XX.ECS.Components.CardData.CardColor.White, legacyColor);
		Assert.False(legacyUpgraded);

		Assert.True(RunDeckService.TryParseCardKey("smite|Red|Upgraded", out var upgradedId, out var upgradedColor, out var upgraded));
		Assert.Equal("smite", upgradedId);
		Assert.Equal(Crusaders30XX.ECS.Components.CardData.CardColor.Red, upgradedColor);
		Assert.True(upgraded);
	}

	[Fact]
	public void OnUpgrade_spawn_path_runs_when_upgraded_card_initializes()
	{
		TestOnUpgradeCard.ResetCounts();
		var entityManager = new EntityManager();
		var entity = entityManager.CreateEntity("TestOnUpgradeCard");
		var card = new TestOnUpgradeCard { IsUpgraded = true };
		card.Initialize(entityManager, entity);

		Assert.Equal(1, TestOnUpgradeCard.SpawnInvokeCount);
		Assert.Equal(0, TestOnUpgradeCard.ApplyInvokeCount);
	}

	[Fact]
	public void OnUpgrade_apply_path_runs_when_upgrade_confirmed()
	{
		TestOnUpgradeCard.ResetCounts();
		CardUpgradeService.InvokeUpgradeConfirmedOnCard(new TestOnUpgradeCard());

		Assert.Equal(0, TestOnUpgradeCard.SpawnInvokeCount);
		Assert.Equal(1, TestOnUpgradeCard.ApplyInvokeCount);
	}

	[Fact]
	public void ApplyPendingOfferOption_upgrade_invokes_upgrade_confirmed_hook()
	{
		CardUpgradeService.UpgradeConfirmedInvokeCountForTests = 0;
		SaveCache.DeleteSaveFilesIfPresent();
		SaveCache.StartNewRun();
		var loadout = SaveCache.GetLoadout(RunDeckService.PrimaryLoadoutId);
		loadout.cards = Entries("smite|White");
		SaveCache.SaveLoadout(loadout);
		var outgoingEntryId = loadout.cards[0].entryId;

		SaveCache.SetPendingDeckRewardOffer(new DeckRewardOfferSave
		{
			options = new List<DeckRewardOfferOptionSave>
			{
				new()
				{
					kind = DeckRewardOfferKinds.Upgrade,
					loadoutIndex = 0,
					outgoingEntryId = outgoingEntryId,
					outgoingCardKey = "smite|White",
					upgradedCardKey = "smite|White|Upgraded"
				}
			}
		});

		Assert.True(QuestCardRewardService.ApplyPendingOfferOption(0));
		var upgraded = SaveCache.GetLoadout(RunDeckService.PrimaryLoadoutId).cards.Single();
		Assert.Equal(outgoingEntryId, upgraded.entryId);
		Assert.Equal("smite|White|Upgraded", upgraded.cardKey);
		Assert.Equal(1, CardUpgradeService.UpgradeConfirmedInvokeCountForTests);
	}

	[Fact]
	public void IsInDefaultStarterPool_recognizes_all_default_starter_cards()
	{
		foreach (var cardId in StartingDeckGeneratorService.DefaultStarterCardPool)
		{
			Assert.True(StartingDeckGeneratorService.IsInDefaultStarterPool(cardId));
		}
	}

	private static void AssertExchangeIncomingPool(
		IReadOnlyList<string> deckKeys,
		string weaponId,
		HashSet<string> allowed)
	{
		var offer = QuestCardRewardService.GenerateDeckRewardOffer(deckKeys, weaponId, 20);
		foreach (var option in offer.options.Where(o => o.kind == DeckRewardOfferKinds.Exchange))
		{
			Assert.True(
				RunDeckService.TryParseCardKey(option.incomingCardKey, out var incomingId, out _),
				$"Invalid incoming key {option.incomingCardKey}");
			Assert.Contains(incomingId, allowed);
		}
	}

	private static List<LoadoutCardEntry> Entries(params string[] cardKeys)
	{
		return cardKeys.Select((cardKey, index) => new LoadoutCardEntry
		{
			entryId = $"test_card_{index}",
			cardKey = cardKey,
			isStarter = true,
			countsAsTraded = false,
			restrictions = new List<string>(),
		}).ToList();
	}

	private sealed class TestOnUpgradeCard : CardBase
	{
		public static int SpawnInvokeCount;
		public static int ApplyInvokeCount;

		public TestOnUpgradeCard()
		{
			CardId = "test_on_upgrade_card";
			OnUpgrade = (entityManager, card) =>
			{
				if (card != null)
					SpawnInvokeCount++;
				else
					ApplyInvokeCount++;
			};
		}

		public static void ResetCounts()
		{
			SpawnInvokeCount = 0;
			ApplyInvokeCount = 0;
		}
	}
}
