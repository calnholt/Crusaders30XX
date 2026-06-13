using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Factories;
using Crusaders30XX.ECS.Services;
using Crusaders30XX.ECS.Systems;
using Microsoft.Xna.Framework;
using Xunit;

namespace Crusaders30XX.Tests;

public class GuidedTutorialInputGateTests
{
	[Fact]
	public void Wrong_play_pledge_confirmation_and_end_turn_publish_message_without_state_mutation()
	{
		EventManager.Clear();
		var manager = new EntityManager();
		var stateEntity = manager.CreateEntity("GuidedTutorial");
		var state = new GuidedTutorial { Battle = TutorialBattle.SandCorpse, Turn = 2 };
		manager.AddComponent(stateEntity, state);
		var wrongCard = EntityFactory.CreateCardFromDefinition(manager, "absolution", CardData.CardColor.Red);
		int messages = 0;
		EventManager.Subscribe<CantPlayCardMessage>(_ => messages++);

		Assert.False(BattleInputGate.TryAllowTutorialAction(manager, TutorialAction.PlayCard, wrongCard));
		Assert.False(BattleInputGate.TryAllowTutorialAction(manager, TutorialAction.PledgeCard, wrongCard));
		Assert.False(BattleInputGate.TryAllowTutorialAction(manager, TutorialAction.ConfirmBlocks));
		Assert.False(BattleInputGate.TryAllowTutorialAction(manager, TutorialAction.EndTurn));

		Assert.Equal(4, messages);
		Assert.Empty(state.PlayedCardIds);
		Assert.Empty(state.PledgedCardIds);
		Assert.Empty(state.BlockedCardIdsThisTurn);
		Assert.Equal(0, state.ConfirmedAttackCountThisTurn);
	}

	[Fact]
	public void Sand_turn_three_accepts_exactly_two_distinct_approved_blockers_across_attacks()
	{
		EventManager.Clear();
		var manager = new EntityManager();
		var stateEntity = manager.CreateEntity("GuidedTutorial");
		var state = new GuidedTutorial
		{
			Battle = TutorialBattle.SandCorpse,
			Turn = 3,
			ConfirmedAttackCountThisTurn = 1,
		};
		state.BlockedCardIdsThisTurn.Add("smite");
		manager.AddComponent(stateEntity, state);
		AddAssigned(manager, "reckoning");

		Assert.True(BattleInputGate.TryAllowTutorialAction(manager, TutorialAction.ConfirmBlocks));
	}

	[Fact]
	public void Gleeber_turn_three_requires_no_blockers()
	{
		EventManager.Clear();
		var manager = new EntityManager();
		var stateEntity = manager.CreateEntity("GuidedTutorial");
		manager.AddComponent(stateEntity, new GuidedTutorial
		{
			Battle = TutorialBattle.Gleeber,
			Turn = 3,
		});

		Assert.True(BattleInputGate.IsTutorialActionAllowed(
			manager,
			TutorialAction.ConfirmBlocks));

		AddAssigned(manager, "reckoning");

		Assert.False(BattleInputGate.IsTutorialActionAllowed(
			manager,
			TutorialAction.ConfirmBlocks));
	}

	[Theory]
	[InlineData(2, 0, "litany_of_wrath", true)]
	[InlineData(2, 0, "absolution", false)]
	[InlineData(2, 1, "absolution", true)]
	[InlineData(2, 1, "litany_of_wrath", false)]
	[InlineData(3, 0, "smite", true)]
	[InlineData(3, 0, "reckoning", false)]
	[InlineData(3, 1, "reckoning", true)]
	[InlineData(3, 1, "absolution", false)]
	public void Sand_attacks_require_the_exact_authored_blocker(
		int turn,
		int confirmedAttackCount,
		string cardId,
		bool expected)
	{
		EventManager.Clear();
		var manager = new EntityManager();
		var stateEntity = manager.CreateEntity("GuidedTutorial");
		var state = new GuidedTutorial
		{
			Battle = TutorialBattle.SandCorpse,
			Turn = turn,
			ConfirmedAttackCountThisTurn = confirmedAttackCount,
		};
		if (confirmedAttackCount == 1)
			state.BlockedCardIdsThisTurn.Add(turn == 2 ? "litany_of_wrath" : "smite");
		manager.AddComponent(stateEntity, state);
		AddAssigned(manager, cardId);

		Assert.Equal(
			expected,
			BattleInputGate.IsTutorialActionAllowed(manager, TutorialAction.ConfirmBlocks));
	}

	[Fact]
	public void Confirmation_validity_is_side_effect_free_and_tracks_requirement_state()
	{
		EventManager.Clear();
		var manager = new EntityManager();
		var stateEntity = manager.CreateEntity("GuidedTutorial");
		manager.AddComponent(stateEntity, new GuidedTutorial
		{
			Battle = TutorialBattle.Gleeber,
			Turn = 2,
		});
		int messages = 0;
		EventManager.Subscribe<CantPlayCardMessage>(_ => messages++);

		Assert.False(BattleInputGate.IsTutorialActionAllowed(manager, TutorialAction.ConfirmBlocks));
		Assert.Equal(0, messages);

		AddAssigned(manager, "reckoning");
		AddAssigned(manager, "absolution");

		Assert.True(BattleInputGate.IsTutorialActionAllowed(manager, TutorialAction.ConfirmBlocks));
		Assert.Equal(0, messages);
		Assert.Equal(new Color(255, 150, 150, 255), EnemyAttackDisplaySystem.GetConditionTextColor(false));
		Assert.Equal(Color.White, EnemyAttackDisplaySystem.GetConditionTextColor(true));
	}

	private static void AddAssigned(EntityManager manager, string cardId)
	{
		var card = EntityFactory.CreateCardFromDefinition(manager, cardId, CardData.CardColor.Red);
		manager.AddComponent(card, new AssignedBlockCard { ContextId = "ctx" });
	}
}
