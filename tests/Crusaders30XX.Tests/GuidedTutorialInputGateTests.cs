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
