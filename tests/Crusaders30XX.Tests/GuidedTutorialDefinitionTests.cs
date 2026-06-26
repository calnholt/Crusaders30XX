using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Tutorials;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Factories;
using Crusaders30XX.ECS.Services;
using Crusaders30XX.ECS.Systems;
using Microsoft.Xna.Framework;
using Xunit;

namespace Crusaders30XX.Tests;

public class GuidedTutorialDefinitionTests
{
	[Theory]
	[InlineData(1, "colorless_3_block:Black,smite:Black")]
	[InlineData(2, "colorless_3_block:Black,litany_of_wrath:Black,smite:Black")]
	[InlineData(3, "hold_the_line:Black,litany_of_wrath:Black,smite:Black,reckoning:Black")]
	[InlineData(4, "absolution:Black,litany_of_wrath:Black,smite:Black,reckoning:Black")]
	[InlineData(5, "mantlet:Black,mantlet:Black,mantlet:Black,smite:Black")]
	[InlineData(6, "stab:Black,hold_the_line:Red,hold_the_line:White,smite:Black")]
	[InlineData(7, "hold_the_line:White,hold_the_line:White,smite:Black,smite:Black")]
	public void Stock_hands_are_exact(int section, string expected)
	{
		string actual = string.Join(",", GuidedTutorialDefinitions.GetTurn(section, 1).StockHand
			.Select(card => $"{card.CardId}:{card.Color}"));
		Assert.Equal(expected, actual);
	}

	[Fact]
	public void Section_8_has_two_turns()
	{
		Assert.Equal(2, GuidedTutorialDefinitions.GetTurnCount(8));

		string turn1 = string.Join(",", GuidedTutorialDefinitions.GetTurn(8, 1).StockHand
			.Select(card => $"{card.CardId}:{card.Color}"));
		Assert.Equal("courageous:Black,smite:Red,smite:Black,fervor:Black", turn1);

		string turn2 = string.Join(",", GuidedTutorialDefinitions.GetTurn(8, 2).StockHand
			.Select(card => $"{card.CardId}:{card.Color}"));
		Assert.Equal("litany_of_wrath:Red,absolution:Black,reckoning:Black,smite:Red", turn2);
	}

	[Fact]
	public void Enemy_hp_and_teach_flags_are_exact()
	{
		Assert.Equal(3, GuidedTutorialDefinitions.GetSection(1).EnemyHp);
		Assert.True(GuidedTutorialDefinitions.GetSection(1).IsTeachSection);

		Assert.Equal(6, GuidedTutorialDefinitions.GetSection(2).EnemyHp);
		Assert.False(GuidedTutorialDefinitions.GetSection(2).IsTeachSection);

		Assert.Equal(8, GuidedTutorialDefinitions.GetSection(3).EnemyHp);

		Assert.Equal(10, GuidedTutorialDefinitions.GetSection(4).EnemyHp);
		Assert.Equal(9, GuidedTutorialDefinitions.GetSection(4).PlayerHp);

		Assert.Equal(6, GuidedTutorialDefinitions.GetSection(5).EnemyHp);
		Assert.True(GuidedTutorialDefinitions.GetSection(5).IsTeachSection);

		Assert.Equal(12, GuidedTutorialDefinitions.GetSection(8).EnemyHp);
		Assert.True(GuidedTutorialDefinitions.GetSection(8).ShowDrawPile);
	}

	[Fact]
	public void Section_8_has_pending_dialog()
	{
		Assert.Equal("last_of_them", GuidedTutorialDefinitions.GetSection(8).PendingDialogKey);
	}

	[Fact]
	public void Section_3_has_catch_breath_dialog()
	{
		Assert.Equal("catch_breath", GuidedTutorialDefinitions.GetSection(3).PendingDialogKey);
	}

	[Fact]
	public void Section_4_has_sword_retrieved_dialog()
	{
		Assert.Equal("sword_retrieved", GuidedTutorialDefinitions.GetSection(4).PendingDialogKey);
	}

	[Fact]
	public void Attack_ids_map_to_correct_damage_values()
	{
		Assert.Equal(9, EnemyAttackFactory.Create("tutorial_gleeber_strike_9").Damage);
		Assert.Equal(8, EnemyAttackFactory.Create("tutorial_gleeber_strike_8").Damage);
		Assert.Equal(6, EnemyAttackFactory.Create("tutorial_gleeber_strike_6").Damage);
		Assert.Equal(5, EnemyAttackFactory.Create("tutorial_gleeber_strike_5").Damage);
		Assert.Equal(3, EnemyAttackFactory.Create("tutorial_gleeber_strike_3").Damage);
	}

	[Fact]
	public void Teach_section_messages_are_correct()
	{
		Assert.Equal(
			["teach_win", "teach_loss", "teach_enemy_attack"],
			GuidedTutorialDefinitions.GetMessageKeys(1, 1, SubPhase.Block, 0));

		Assert.Empty(GuidedTutorialDefinitions.GetMessageKeys(2, 1, SubPhase.Block, 0));

		Assert.Equal(
			["teach_black_block"],
			GuidedTutorialDefinitions.GetMessageKeys(5, 1, SubPhase.Block, 0));
		Assert.Equal(
			["teach_weapon"],
			GuidedTutorialDefinitions.GetMessageKeys(5, 1, SubPhase.Action, 0));

		Assert.Equal(
			["teach_red_courage", "teach_courage_hud"],
			GuidedTutorialDefinitions.GetMessageKeys(6, 1, SubPhase.Block, 0));

		Assert.Equal(
			["teach_white_temperance", "teach_temperance_hud"],
			GuidedTutorialDefinitions.GetMessageKeys(7, 1, SubPhase.Block, 0));

		Assert.Equal(
			["teach_intent_pips", "teach_pledge"],
			GuidedTutorialDefinitions.GetMessageKeys(8, 1, SubPhase.Block, 0));
	}

	[Fact]
	public void Teach_messages_have_correct_targets()
	{
		var win = GuidedTutorialDefinitions.GuidedMessages.Single(msg => msg.key == "teach_win");
		Assert.Equal("entity_name", win.targetType);
		Assert.Equal("Enemy", win.targetId);

		var loss = GuidedTutorialDefinitions.GuidedMessages.Single(msg => msg.key == "teach_loss");
		Assert.Equal(PlayerHudLayoutSystem.HealthEntityName, loss.targetId);

		var courageHud = GuidedTutorialDefinitions.GuidedMessages.Single(msg => msg.key == "teach_courage_hud");
		Assert.Equal(PlayerHudLayoutSystem.CourageEntityName, courageHud.targetId);

		var temperanceHud = GuidedTutorialDefinitions.GuidedMessages.Single(msg => msg.key == "teach_temperance_hud");
		Assert.Equal(PlayerHudLayoutSystem.TemperanceEntityName, temperanceHud.targetId);

		var intent = GuidedTutorialDefinitions.GuidedMessages.Single(msg => msg.key == "teach_intent_pips");
		Assert.Equal("EnemyIntentPips", intent.targetId);

		var pledge = GuidedTutorialDefinitions.GuidedMessages.Single(msg => msg.key == "teach_pledge");
		Assert.Equal("UI_PlayerHudPledge", pledge.targetId);
	}

	[Theory]
	[InlineData("top", HotKeyPosition.Top)]
	[InlineData("bottom", HotKeyPosition.Below)]
	[InlineData("left", HotKeyPosition.Left)]
	[InlineData("right", HotKeyPosition.Right)]
	[InlineData(null, HotKeyPosition.Top)]
	[InlineData("unknown", HotKeyPosition.Top)]
	public void Tutorial_bubble_orientation_maps_to_hotkey_position(string orientation, HotKeyPosition expected)
	{
		Assert.Equal(expected, TutorialDisplaySystem.MapBubbleOrientationToHotKeyPosition(orientation));
	}

	[Fact]
	public void Tutorial_targets_player_hud_health_and_full_hand_bounds()
	{
		var loss = GuidedTutorialDefinitions.GuidedMessages.Single(message => message.key == "teach_loss");
		Assert.Equal(PlayerHudLayoutSystem.HealthEntityName, loss.targetId);

		var bounds = TutorialManager.UnionBounds(
		[
			new Rectangle(100, 400, 120, 180),
			new Rectangle(200, 360, 120, 180),
			new Rectangle(300, 420, 120, 180),
		]);
		Assert.Equal(new Rectangle(100, 360, 320, 240), bounds);
	}

	[Fact]
	public void Named_entity_target_resolves_parent_transformed_hud_bounds()
	{
		EventManager.Clear();
		try
		{
			var manager = new EntityManager();
			var root = manager.CreateEntity("HUD");
			manager.AddComponent(root, new Transform { Position = new Vector2(400, 500) });
			var health = manager.CreateEntity(PlayerHudLayoutSystem.HealthEntityName);
			manager.AddComponent(health, new Transform { Position = new Vector2(20, 30) });
			manager.AddComponent(health, new ParentTransform { Parent = root });
			manager.AddComponent(health, new UIElement
			{
				Bounds = new Rectangle(0, 0, 300, 36),
			});
			var tutorialManager = new TutorialManager(manager);

			Assert.Equal(
				new Rectangle(420, 530, 300, 36),
				tutorialManager.GetEntityBounds(PlayerHudLayoutSystem.HealthEntityName));
		}
		finally
		{
			EventManager.Clear();
		}
	}

	[Fact]
	public void Enemy_start_advances_turn_for_section_8_without_repreparing_hand()
	{
		EventManager.Clear();
		try
		{
			var manager = new EntityManager();
			var stateEntity = manager.CreateEntity("GuidedTutorial");
			var state = new GuidedTutorial
			{
				Section = 8,
				TurnWithinSection = 1,
				StockHandPrepared = true,
			};
			manager.AddComponent(stateEntity, state);

			var deckEntity = manager.CreateEntity("Deck");
			var deck = new Deck();
			manager.AddComponent(deckEntity, deck);
			manager.AddComponent(deckEntity, new StockHand
			{
				Section = 8,
				TurnWithinSection = 1,
			});

			var phaseEntity = manager.CreateEntity("PhaseState");
			manager.AddComponent(phaseEntity, new PhaseState
			{
				Sub = SubPhase.PlayerEnd,
				TurnNumber = 1,
			});

			_ = new GuidedTutorialDirectorSystem(manager);
			EventManager.Publish(new ChangeBattlePhaseEvent { Current = SubPhase.EnemyStart });

			Assert.Equal(2, state.TurnWithinSection);
			Assert.True(state.StockHandPrepared);
		}
		finally
		{
			EventManager.Clear();
		}
	}

	[Fact]
	public void Restart_section_sets_flag_and_resets_turn()
	{
		var manager = new EntityManager();
		var stateEntity = manager.CreateEntity("GuidedTutorial");
		var state = new GuidedTutorial
		{
			Section = 3,
			TurnWithinSection = 2,
			StockHandPrepared = true,
		};
		manager.AddComponent(stateEntity, state);

		GuidedTutorialService.RestartSection(manager);

		Assert.True(state.IsRestart);
		Assert.Equal(1, state.TurnWithinSection);
		Assert.False(state.StockHandPrepared);
	}

	[Fact]
	public void Advance_to_next_section_snapshots_courage_and_temperance()
	{
		EventManager.Clear();
		try
		{
			var manager = new EntityManager();
			var stateEntity = manager.CreateEntity("GuidedTutorial");
			var state = new GuidedTutorial
			{
				Section = 1,
				TurnWithinSection = 1,
				PlayerHp = 1,
			};
			manager.AddComponent(stateEntity, state);

			var player = manager.CreateEntity("Player");
			manager.AddComponent(player, new Courage { Amount = 2 });
			manager.AddComponent(player, new Temperance { Amount = 1 });
			manager.AddComponent(player, new HP { Current = 5, Max = 25 });

			GuidedTutorialService.AdvanceToNextSection(manager);

			Assert.Equal(2, state.Section);
			Assert.Equal(1, state.TurnWithinSection);
			Assert.False(state.StockHandPrepared);
			Assert.False(state.IsRestart);
			Assert.Equal(2, state.BaselineCourage);
			Assert.Equal(1, state.BaselineTemperance);
			Assert.Equal(1, state.PlayerHp); // Section 2 has player HP 1
		}
		finally
		{
			EventManager.Clear();
		}
	}

	[Fact]
	public void Covered_tutorial_keys_include_all_teach_keys()
	{
		Assert.Contains("teach_win", GuidedTutorialDefinitions.CoveredTutorialKeys);
		Assert.Contains("teach_loss", GuidedTutorialDefinitions.CoveredTutorialKeys);
		Assert.Contains("teach_black_block", GuidedTutorialDefinitions.CoveredTutorialKeys);
		Assert.Contains("teach_red_courage", GuidedTutorialDefinitions.CoveredTutorialKeys);
		Assert.Contains("teach_white_temperance", GuidedTutorialDefinitions.CoveredTutorialKeys);
		Assert.Contains("teach_intent_pips", GuidedTutorialDefinitions.CoveredTutorialKeys);
		Assert.Contains("teach_pledge", GuidedTutorialDefinitions.CoveredTutorialKeys);
	}
}
