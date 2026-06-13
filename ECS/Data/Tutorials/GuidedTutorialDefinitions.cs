using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Components;

namespace Crusaders30XX.ECS.Data.Tutorials
{
	public sealed record TutorialCardDefinition(string CardId, CardData.CardColor Color);

	public sealed record TutorialBlockRule(
		IReadOnlyList<string> AllowedCardIds,
		int RequiredCount,
		bool MustUseEveryAllowedCard = false,
		bool MustNotBlock = false,
		string RequirementText = "");

	public sealed record TutorialTurnDefinition(
		IReadOnlyList<TutorialCardDefinition> StockHand,
		IReadOnlyList<string> AttackIds,
		IReadOnlyList<TutorialBlockRule> BlockRules,
		IReadOnlyList<string> RequiredPlays,
		string RequiredPledgeCardId = "");

	public sealed record TutorialBattleDefinition(
		TutorialBattle Battle,
		string EnemyId,
		int EnemyHp,
		IReadOnlyList<TutorialTurnDefinition> Turns);

	public static class GuidedTutorialDefinitions
	{
		public const string CompletionTutorialKey = "guided_tutorial";

		public static readonly IReadOnlyList<string> CoveredTutorialKeys =
		[
			"how_to_win",
			"block_phase_overview",
			"card_block_value",
			"action_phase_overview",
			"action_points",
			"card_damage",
			"weapon",
			"cost",
			"card_colors",
			"courage",
			"temperance",
			"pledge",
			"threat",
		];

		public static readonly IReadOnlyList<TutorialDefinition> GuidedMessages =
		[
			new() { key = "guided_win", text = "Reduce the enemy's HP to zero to win the battle.", targetType = "entity_name", targetId = "Enemy", bubbleOrientation = "bottom" },
			new() { key = "guided_loss", text = "If your HP reaches zero, you lose. Empty draw piles do not reshuffle.", targetType = "entity_name", targetId = "UI_PlayerHudHealth", bubbleOrientation = "right" },
			new() { key = "guided_block", text = "Assign cards from your hand to block incoming damage. A card's blue BLOCK value is how much damage it blocks.", targetType = "ui_region", targetId = "player_hand", bubbleOrientation = "top" },
			new() { key = "guided_ap", text = "You receive one AP each turn. Smite uses one AP.", targetType = "ui_region", targetId = "ap_and_smite_ap", bubbleOrientation = "right" },
			new() { key = "guided_damage", text = "DAMAGE shows how much HP an attack removes. Play Smite.", targetType = "ui_region", targetId = "smite_damage", bubbleOrientation = "top" },
			new() { key = "guided_draw", text = "At the start of each turn, draw until you hold four cards.", targetType = "ui_region", targetId = "player_hand", bubbleOrientation = "top" },
			new() { key = "guided_free", text = "Free Actions do not spend AP. Litany of Wrath is a Free Action. Play Litany of Wrath, then play Smite.", targetType = "entity_name", targetId = "Card_Litany of Wrath_Black_1", bubbleOrientation = "top" },
			new() { key = "guided_cost", text = "Card costs discard other cards from your hand. Gray cost symbols accept any color. Play Absolution OR Litany of Wrath then Reckoning.", targetType = "ui_region", targetId = "absolution_reckoning_costs", bubbleOrientation = "top" },
			new() { key = "guided_red", text = "Blocking with red cards builds Courage.", targetType = "ui_region", targetId = "absolution_and_courage", bubbleOrientation = "right" },
			new() { key = "guided_white", text = "Blocking with white cards builds Temperance.", targetType = "ui_region", targetId = "reckoning_and_temperance", bubbleOrientation = "right" },
			new() { key = "guided_black", text = "Black cards receive one additional BLOCK.", targetType = "ui_region", targetId = "smite_and_litany", bubbleOrientation = "top" },
			new() { key = "guided_intent", text = "Intent pips show the order of incoming enemy attacks.", targetType = "entity_name", targetId = "EnemyIntentPips", bubbleOrientation = "bottom" },
			new() { key = "guided_weapon", text = "Your weapon appears during the Action phase and can be used once each turn. Play Sword.", targetType = "entity_name", targetId = "Weapon", bubbleOrientation = "top" },
			new() { key = "guided_pledge", text = "Pledge one card to retain it for a later turn. Pledged cards cannot block or pay costs.", targetType = "ui_region", targetId = "fervor_and_pledge", bubbleOrientation = "right" },
			new() { key = "guided_fervor", text = "Retain Fervor while building Courage so its bonus damage is ready later.", targetType = "entity_name", targetId = "Card_Fervor_Red_0", bubbleOrientation = "top" },
			new() { key = "guided_lethal", text = "Play Litany of Wrath, then play pledged Fervor for lethal damage.", targetType = "ui_region", targetId = "litany_and_fervor", bubbleOrientation = "top" },
		];

		private static readonly TutorialBattleDefinition Gleeber = new(
			TutorialBattle.Gleeber,
			"gleeber",
			19,
			[
				Turn(
					[Black("smite"), Black("litany_of_wrath"), Black("reckoning"), Black("absolution")],
					["tutorial_gleeber_strike"],
					[Rule(["litany_of_wrath", "reckoning", "absolution"], 3, true, text: "Must be blocked by Litany of Wrath, Reckoning, and Absolution.")],
					["smite"]),
				Turn(
					[Black("smite"), Black("litany_of_wrath"), Black("reckoning"), Black("absolution")],
					["tutorial_gleeber_strike"],
					[Rule(["reckoning", "absolution"], 2, true, text: "Must be blocked by Reckoning and Absolution.")],
					["litany_of_wrath", "smite"]),
				Turn(
					[Black("smite"), Black("litany_of_wrath"), Black("reckoning"), Black("absolution")],
					["tutorial_gleeber_strike"],
					[new TutorialBlockRule([], 0, MustNotBlock: true, RequirementText: "Can't be blocked by any cards.")],
					[]),
			]);

		private static readonly TutorialBattleDefinition SandCorpse = new(
			TutorialBattle.SandCorpse,
			"sand_corpse",
			20,
			[
				Turn(
					[Black("smite"), Black("litany_of_wrath"), White("reckoning"), Red("absolution")],
					["tutorial_sand_blast", "tutorial_sand_storm"],
					[
						Rule(["absolution"], 1, true, text: "Must be blocked by Absolution."),
						Rule(["reckoning"], 1, true, text: "Must be blocked by Reckoning."),
					],
					["sword"]),
				Turn(
					[Red("fervor"), Black("smite"), Black("litany_of_wrath"), Red("absolution")],
					["tutorial_sand_blast", "tutorial_sand_storm"],
					[
						Rule(["litany_of_wrath"], 1, true, text: "Must be blocked by Litany of Wrath."),
						Rule(["absolution"], 1, true, text: "Must be blocked by Absolution."),
					],
					["smite"],
					"fervor"),
				Turn(
					[Red("smite"), Red("litany_of_wrath"), Red("reckoning"), Red("absolution")],
					["tutorial_sand_blast", "tutorial_sand_storm"],
					[
						Rule(["smite"], 1, true, text: "Must be blocked by Smite."),
						Rule(["reckoning"], 1, true, text: "Must be blocked by Reckoning."),
					],
					["litany_of_wrath", "fervor"]),
			]);

		public static IReadOnlyList<string> GetMessageKeys(
			TutorialBattle battle,
			int turn,
			SubPhase phase,
			int confirmedAttackCount)
		{
			if (battle == TutorialBattle.Gleeber)
			{
				if (turn == 1 && phase == SubPhase.Block)
					return ["guided_win", "guided_loss", "guided_block"];
				if (turn == 1 && phase == SubPhase.Action)
					return ["guided_ap", "guided_damage"];
				if (turn == 2 && phase == SubPhase.Block)
					return ["guided_draw"];
				if (turn == 2 && phase == SubPhase.Action)
					return ["guided_free"];
				if (turn == 3 && phase == SubPhase.Action)
					return ["guided_cost"];
				return Array.Empty<string>();
			}

			if (turn == 1 && phase == SubPhase.Block && confirmedAttackCount == 0)
				return ["guided_red", "guided_white", "guided_black", "guided_intent"];
			if (turn == 1 && phase == SubPhase.Action)
				return ["guided_weapon"];
			if (turn == 2 && phase == SubPhase.Action)
				return ["guided_pledge", "guided_fervor"];
			if (turn == 3 && phase == SubPhase.Action)
				return ["guided_lethal"];
			return Array.Empty<string>();
		}

		public static string ResolveMessageText(
			string key,
			string defaultText,
			bool isGamepadConnected)
		{
			if (!string.Equals(key, "guided_pledge", StringComparison.Ordinal))
				return defaultText ?? string.Empty;

			string input = isGamepadConnected ? "X" : "Spacebar";
			return $"{defaultText} Press {input} while hovering over Fervor.";
		}

		public static TutorialBattleDefinition GetBattle(TutorialBattle battle) =>
			battle == TutorialBattle.SandCorpse ? SandCorpse : Gleeber;

		public static TutorialTurnDefinition GetTurn(TutorialBattle battle, int turn)
		{
			var definition = GetBattle(battle);
			int index = Math.Clamp(turn - 1, 0, definition.Turns.Count - 1);
			return definition.Turns[index];
		}

		public static bool IsLegalPlay(GuidedTutorial state, string cardId)
		{
			if (state == null || string.IsNullOrWhiteSpace(cardId)) return false;
			return GetValidPlays(state).Contains(cardId);
		}

		public static IReadOnlyList<string> GetValidPlays(GuidedTutorial state)
		{
			if (state == null) return Array.Empty<string>();
			var required = GetTurn(state.Battle, state.Turn).RequiredPlays;
			if (state.Battle == TutorialBattle.Gleeber && state.Turn == 3)
			{
				if (state.PlayedCardIds.Count == 0)
					return ["absolution", "litany_of_wrath"];
				return state.PlayedCardIds.SequenceEqual(["litany_of_wrath"])
					? ["reckoning"]
					: Array.Empty<string>();
			}

			if (state.Battle == TutorialBattle.SandCorpse
				&& state.Turn == 2
				&& !state.PledgedCardIds.Contains("fervor"))
			{
				return Array.Empty<string>();
			}

			int index = state.PlayedCardIds.Count;
			return index < required.Count ? [required[index]] : Array.Empty<string>();
		}

		public static bool AreActionRequirementsComplete(GuidedTutorial state)
		{
			if (state == null) return true;
			if (state.Battle == TutorialBattle.Gleeber && state.Turn == 3)
			{
				return state.PlayedCardIds.SequenceEqual(["absolution"])
					|| state.PlayedCardIds.SequenceEqual(["litany_of_wrath", "reckoning"]);
			}

			var turn = GetTurn(state.Battle, state.Turn);
			return state.PlayedCardIds.SequenceEqual(turn.RequiredPlays)
				&& (string.IsNullOrEmpty(turn.RequiredPledgeCardId)
					|| state.PledgedCardIds.Contains(turn.RequiredPledgeCardId));
		}

		private static TutorialTurnDefinition Turn(
			IReadOnlyList<TutorialCardDefinition> hand,
			IReadOnlyList<string> attacks,
			IReadOnlyList<TutorialBlockRule> rules,
			IReadOnlyList<string> plays,
			string pledge = "") =>
			new(hand, attacks, rules, plays, pledge);

		private static TutorialBlockRule Rule(
			IReadOnlyList<string> ids,
			int count,
			bool every = false,
			bool none = false,
			string text = "") =>
			new(ids, count, every, none, text);

		private static TutorialCardDefinition Red(string id) => new(id, CardData.CardColor.Red);
		private static TutorialCardDefinition White(string id) => new(id, CardData.CardColor.White);
		private static TutorialCardDefinition Black(string id) => new(id, CardData.CardColor.Black);
	}
}
