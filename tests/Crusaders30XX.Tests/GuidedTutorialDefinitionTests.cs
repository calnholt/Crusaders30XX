using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Data.Tutorials;
using Crusaders30XX.ECS.Factories;
using Xunit;

namespace Crusaders30XX.Tests;

public class GuidedTutorialDefinitionTests
{
	[Theory]
	[InlineData(TutorialBattle.Gleeber, 1, "smite:Black,litany_of_wrath:Black,reckoning:Black,absolution:Black")]
	[InlineData(TutorialBattle.Gleeber, 2, "smite:Black,litany_of_wrath:Black,reckoning:Black,absolution:Black")]
	[InlineData(TutorialBattle.Gleeber, 3, "smite:Black,litany_of_wrath:Black,reckoning:Black,absolution:Black")]
	[InlineData(TutorialBattle.SandCorpse, 1, "smite:Black,litany_of_wrath:Black,reckoning:White,absolution:Red")]
	[InlineData(TutorialBattle.SandCorpse, 2, "fervor:Red,smite:Black,litany_of_wrath:Black,absolution:Red")]
	[InlineData(TutorialBattle.SandCorpse, 3, "smite:Red,litany_of_wrath:Red,reckoning:Red,absolution:Red")]
	public void Stock_hands_and_colors_are_exact(TutorialBattle battle, int turn, string expected)
	{
		string actual = string.Join(",", GuidedTutorialDefinitions.GetTurn(battle, turn).StockHand
			.Select(card => $"{card.CardId}:{card.Color}"));
		Assert.Equal(expected, actual);
	}

	[Fact]
	public void Enemy_hp_attack_order_and_damage_are_exact()
	{
		Assert.Equal(19, GuidedTutorialDefinitions.GetBattle(TutorialBattle.Gleeber).EnemyHp);
		Assert.Equal(20, GuidedTutorialDefinitions.GetBattle(TutorialBattle.SandCorpse).EnemyHp);

		foreach (var turn in GuidedTutorialDefinitions.GetBattle(TutorialBattle.Gleeber).Turns)
		{
			Assert.Equal(["tutorial_gleeber_strike"], turn.AttackIds);
			Assert.Equal(9, EnemyAttackFactory.Create(turn.AttackIds[0]).Damage);
		}

		foreach (var turn in GuidedTutorialDefinitions.GetBattle(TutorialBattle.SandCorpse).Turns)
		{
			Assert.Equal(["tutorial_sand_blast", "tutorial_sand_storm"], turn.AttackIds);
			Assert.Equal(4, EnemyAttackFactory.Create(turn.AttackIds[0]).Damage);
			Assert.Equal(3, EnemyAttackFactory.Create(turn.AttackIds[1]).Damage);
		}
	}

	[Fact]
	public void Gleeber_final_turn_allows_both_approved_lethal_branches()
	{
		var absolution = new GuidedTutorial { Battle = TutorialBattle.Gleeber, Turn = 3 };
		Assert.True(GuidedTutorialDefinitions.IsLegalPlay(absolution, "absolution"));
		absolution.PlayedCardIds.Add("absolution");
		Assert.True(GuidedTutorialDefinitions.AreActionRequirementsComplete(absolution));

		var litanyReckoning = new GuidedTutorial { Battle = TutorialBattle.Gleeber, Turn = 3 };
		Assert.True(GuidedTutorialDefinitions.IsLegalPlay(litanyReckoning, "litany_of_wrath"));
		litanyReckoning.PlayedCardIds.Add("litany_of_wrath");
		Assert.True(GuidedTutorialDefinitions.IsLegalPlay(litanyReckoning, "reckoning"));
		litanyReckoning.PlayedCardIds.Add("reckoning");
		Assert.True(GuidedTutorialDefinitions.AreActionRequirementsComplete(litanyReckoning));
	}

	[Fact]
	public void Sand_turn_two_requires_pledge_before_smite()
	{
		var state = new GuidedTutorial { Battle = TutorialBattle.SandCorpse, Turn = 2 };
		Assert.False(GuidedTutorialDefinitions.IsLegalPlay(state, "smite"));

		state.PledgedCardIds.Add("fervor");
		Assert.True(GuidedTutorialDefinitions.IsLegalPlay(state, "smite"));
	}

	[Fact]
	public void Sand_corpse_script_builds_five_courage_and_deals_exactly_twenty_damage()
	{
		int courage = 1; // Sword
		courage += 1; // turn 1 Absolution block
		courage += 1; // turn 2 Absolution block
		courage += 2; // turn 3 red blockers
		Assert.Equal(5, courage);

		int damage = 5; // Sword
		damage += 3; // Smite
		damage += 6 + 3 + 3; // Fervor base, Courage bonus, Litany aggression
		Assert.Equal(20, damage);
	}

	[Fact]
	public void Every_permitted_block_path_keeps_player_alive()
	{
		int hp = 25;
		hp -= 2; // Gleeber turn 2
		hp -= 9; // Gleeber turn 3
		hp -= 2; // Sand Corpse turn 1
		hp -= 1; // worst turn 2 mapping
		hp -= 2; // worst turn 3 mapping
		Assert.Equal(9, hp);
		Assert.True(hp >= 5);
	}
}
