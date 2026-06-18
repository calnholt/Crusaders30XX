using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Loadouts;
using Crusaders30XX.ECS.Data.Save;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Services;
using Crusaders30XX.ECS.Systems;
using Xunit;

namespace Crusaders30XX.Tests;

public class ClimbShopServiceTests : IDisposable
{
	public ClimbShopServiceTests()
	{
		EventManager.Clear();
	}

	public void Dispose()
	{
		EventManager.Clear();
	}

	[Fact]
	public void Medal_purchase_spends_resources_adds_medal_and_marks_slot_sold()
	{
		PrepareRun(new List<string> { "smite|White" });
		var state = BaseState();
		state.shopSlots[0] = ShopSlot(ClimbShopSlotKinds.Medal, itemId: "st_luke");
		SaveCache.SaveClimbState(state);

		Assert.True(ClimbShopService.TryPurchaseSlot(0));

		var loadout = SaveCache.GetLoadout(RunDeckService.PrimaryLoadoutId);
		var after = SaveCache.GetClimbState();
		Assert.Contains("st_luke", loadout.medalIds);
		Assert.True(after.shopSlots[0].isSold);
		Assert.Equal(2, after.resources.red);
		Assert.Equal(1, after.time);
	}

	[Fact]
	public void Equipment_purchase_equips_item_and_marks_slot_sold()
	{
		PrepareRun(new List<string> { "smite|White" });
		var state = BaseState();
		state.shopSlots[0] = ShopSlot(ClimbShopSlotKinds.Equipment, itemId: "knightly_chest");
		SaveCache.SaveClimbState(state);

		Assert.True(ClimbShopService.TryPurchaseSlot(0));

		var loadout = SaveCache.GetLoadout(RunDeckService.PrimaryLoadoutId);
		Assert.Equal("knightly_chest", loadout.chestId);
		Assert.True(SaveCache.GetClimbState().shopSlots[0].isSold);
	}

	[Fact]
	public void Upgrade_purchase_replaces_specific_deck_entry()
	{
		PrepareRun(new List<string> { "smite|White", "fervor|Red" });
		var state = BaseState();
		state.shopSlots[0] = ShopSlot(ClimbShopSlotKinds.Upgrade, cardKey: "smite|White|Upgraded", deckIndex: 0);
		SaveCache.SaveClimbState(state);

		Assert.True(ClimbShopService.TryPurchaseSlot(0));

		Assert.Equal("smite|White|Upgraded", SaveCache.GetLoadout(RunDeckService.PrimaryLoadoutId).cardIds[0]);
		Assert.True(SaveCache.GetClimbState().shopSlots[0].isSold);
	}

	[Fact]
	public void Replacement_open_and_cancel_do_not_spend_or_mutate_deck()
	{
		PrepareRun(new List<string> { "smite|White", "fervor|Red" });
		var state = BaseState();
		state.shopSlots[0] = ShopSlot(ClimbShopSlotKinds.Replacement, cardKey: "strike|Red");
		SaveCache.SaveClimbState(state);

		Assert.True(ClimbShopService.TryOpenReplacementOffer(0));
		ClimbShopService.CancelReplacementOffer();

		var after = SaveCache.GetClimbState();
		Assert.Null(after.pendingReplacementOffer);
		Assert.Equal(3, after.resources.red);
		Assert.Equal(0, after.time);
		Assert.Equal(new[] { "smite|White", "fervor|Red" }, SaveCache.GetLoadout(RunDeckService.PrimaryLoadoutId).cardIds);
	}

	[Fact]
	public void Replacement_final_selection_spends_replaces_non_weapon_card_and_sells_slot()
	{
		PrepareRun(new List<string> { "smite|White", "fervor|Red" });
		var state = BaseState();
		state.shopSlots[0] = ShopSlot(ClimbShopSlotKinds.Replacement, cardKey: "strike|Red");
		SaveCache.SaveClimbState(state);
		Assert.True(ClimbShopService.TryOpenReplacementOffer(0));

		Assert.True(ClimbShopService.TryFinalizeReplacement(1));

		var loadout = SaveCache.GetLoadout(RunDeckService.PrimaryLoadoutId);
		var after = SaveCache.GetClimbState();
		Assert.Equal("strike|Red", loadout.cardIds[1]);
		Assert.Equal(2, after.resources.red);
		Assert.Equal(1, after.time);
		Assert.True(after.shopSlots[0].isSold);
		Assert.Null(after.pendingReplacementOffer);
	}

	[Fact]
	public void Replacement_shop_action_opens_selectable_modal_with_only_eligible_non_weapon_deck_cards()
	{
		EventManager.Clear();
		PrepareRun(new List<string> { "smite|White", "hammer|White", "kunai|White", "fervor|Red" });
		var state = BaseState();
		state.shopSlots[0] = ShopSlot(ClimbShopSlotKinds.Replacement, cardKey: "strike|Red");
		SaveCache.SaveClimbState(state);
		var entityManager = new EntityManager();
		var sceneEntity = entityManager.CreateEntity("Scene");
		entityManager.AddComponent(sceneEntity, new SceneState { Current = SceneId.Climb });
		var slotEntity = entityManager.CreateEntity("ClimbShopSlot");
		entityManager.AddComponent(slotEntity, new ClimbShopSlotAction { SlotIndex = 0 });
		OpenCardListModalEvent opened = null;
		EventManager.Subscribe<OpenCardListModalEvent>(evt => opened = evt);

		UIElementEventDelegateService.HandleEvent(UIElementEventType.ClimbShopSlotSelect, slotEntity, entityManager);

		Assert.NotNull(opened);
		Assert.True(opened.IsSelectable);
		Assert.Equal(CardListSelectionContexts.ClimbReplacement, opened.SelectionContext);
		var openedKeys = opened.Cards
			.Select(c => c.GetComponent<RunDeckCard>()?.CardKey)
			.Where(k => !string.IsNullOrWhiteSpace(k))
			.ToList();
		Assert.Equal(new[] { "smite|White", "fervor|Red" }, openedKeys);
		Assert.All(opened.Cards, card =>
		{
			var metadata = card.GetComponent<CardListModalSelectionMetadata>();
			Assert.NotNull(metadata);
			Assert.Equal(CardListSelectionContexts.ClimbReplacement, metadata.SelectionContext);
		});
		Assert.NotNull(SaveCache.GetClimbState().pendingReplacementOffer);
		Assert.Equal(3, SaveCache.GetClimbState().resources.red);
		Assert.Equal(0, SaveCache.GetClimbState().time);
	}

	[Fact]
	public void Replacement_modal_selection_finalizes_pending_climb_replacement()
	{
		EventManager.Clear();
		PrepareRun(new List<string> { "smite|White", "fervor|Red" });
		var state = BaseState();
		state.shopSlots[0] = ShopSlot(ClimbShopSlotKinds.Replacement, cardKey: "strike|Red");
		SaveCache.SaveClimbState(state);
		Assert.True(ClimbShopService.TryOpenReplacementOffer(0));
		var entityManager = new EntityManager();
		var selectedCard = entityManager.CreateEntity("SelectedCard");
		entityManager.AddComponent(selectedCard, new CardListModalSelectionMetadata
		{
			SelectionContext = CardListSelectionContexts.ClimbReplacement,
			CardKey = "fervor|Red",
			SourceIndex = 1,
		});

		Assert.True(CardListModalSystem.TryFinalizeClimbReplacementSelection(selectedCard, entityManager));

		var loadout = SaveCache.GetLoadout(RunDeckService.PrimaryLoadoutId);
		var after = SaveCache.GetClimbState();
		Assert.Equal("strike|Red", loadout.cardIds[1]);
		Assert.Equal(2, after.resources.red);
		Assert.Equal(1, after.time);
		Assert.True(after.shopSlots[0].isSold);
		Assert.Null(after.pendingReplacementOffer);
	}

	[Fact]
	public void Invalid_card_shop_offer_clears_until_refresh()
	{
		var loadout = new LoadoutDefinition
		{
			id = RunDeckService.PrimaryLoadoutId,
			cardIds = new List<string> { "smite|White|Upgraded" },
			medalIds = new List<string>(),
		};
		var state = BaseState();
		state.shopSlots[0] = ShopSlot(ClimbShopSlotKinds.Upgrade, cardKey: "smite|White|Upgraded", deckIndex: 0);

		Assert.True(ClimbShopService.ClearInvalidOffers(state, loadout));
		Assert.Equal(ClimbShopSlotKinds.Empty, state.shopSlots[0].kind);
	}

	private static void PrepareRun(List<string> cards)
	{
		SaveCache.DeleteSaveFilesIfPresent();
		SaveCache.StartNewRun();
		var loadout = SaveCache.GetLoadout(RunDeckService.PrimaryLoadoutId);
		loadout.cardIds = cards;
		loadout.medalIds = new List<string>();
		loadout.weaponId = "sword";
		loadout.headId = string.Empty;
		loadout.chestId = string.Empty;
		loadout.armsId = string.Empty;
		loadout.legsId = string.Empty;
		SaveCache.SaveLoadout(loadout);
	}

	private static ClimbSaveState BaseState()
	{
		var state = SaveCache.GetClimbState();
		state.time = 0;
		state.resources = new ClimbResourceSave { red = 3, white = 3, black = 3 };
		state.shopSlots = Enumerable.Range(0, ClimbRuleService.ShopSlotCount)
			.Select(i => ShopSlot(ClimbShopSlotKinds.Empty))
			.ToList();
		return state;
	}

	private static ClimbShopSlotSave ShopSlot(
		string kind,
		string itemId = "",
		string cardKey = "",
		int deckIndex = -1)
	{
		return new ClimbShopSlotSave
		{
			id = "slot",
			kind = kind,
			itemId = itemId,
			cardKey = cardKey,
			deckIndex = deckIndex,
			cost = new ClimbResourceSave { red = 1, white = 0, black = 0 },
			timeCost = string.Equals(kind, ClimbShopSlotKinds.Empty, StringComparison.OrdinalIgnoreCase) ? 0 : 1,
		};
	}
}
