using System.Collections.Generic;

namespace Crusaders30XX.ECS.Systems
{
	internal static class KeywordTooltipTextService
	{
		public static string GetTooltip(string text)
		{
			if (text == null) return string.Empty;
			if (string.IsNullOrWhiteSpace(text)) return string.Empty;
			var lowerText = text.ToLowerInvariant();
			var matches = new List<(int Index, string Tooltip)>();

			int i;
			i = lowerText.IndexOf("stun");
			if (i >= 0) matches.Add((i, "X Stun - Skip the next X attack(s)."));
			i = lowerText.IndexOf("inferno");
			if (i >= 0) matches.Add((i, "X Inferno- At the start of the turn, gain X burn."));
			i = lowerText.IndexOf("slow");
			if (i >= 0) matches.Add((i, "X Slow - Ambush attacks are X second faster."));
			i = lowerText.IndexOf("aegis");
			if (i >= 0) matches.Add((i, " X Aegis - Prevent the next X damage from any source."));
			i = lowerText.IndexOf("burn");
			if (i >= 0) matches.Add((i, "X Burn - At the start of the turn, take X damage."));
			i = lowerText.IndexOf("aggression");
			if (i >= 0) matches.Add((i, "X Aggression - Your next attack this turn gains +X damage."));
			i = lowerText.IndexOf("power");
			if (i >= 0) matches.Add((i, "X Power - Your attacks deal +X damage."));
			i = lowerText.IndexOf("penance");
			if (i >= 0) matches.Add((i, "X Penance - Your attacks deal -X less damage. At the start of the next battle, these are converted to scars."));
			var showScar = lowerText.IndexOf("scar ") >= 0 || lowerText.IndexOf("scars") >= 0 || lowerText.IndexOf("scars ") >= 0 || lowerText.IndexOf("scar.") >= 0;
			if (showScar) matches.Add((i, "X Scar - Lose X max HP for the rest of the quest."));
			i = lowerText.IndexOf("fear");
			if (i >= 0) matches.Add((i, "X Fear - Attacks have a (X*10)% chance to become ambush attacks this quest."));
			i = lowerText.IndexOf("wounded");
			if (i >= 0) matches.Add((i, "X Wounded - Take X more damage from all sources this battle."));
			i = lowerText.IndexOf("armor");
			if (i >= 0) matches.Add((i, "X Armor - Take X less damage from attacks this battle."));
			i = lowerText.IndexOf("bleed");
			if (i >= 0) matches.Add((i, "X Bleed - While you have bleed, lose 1 HP at the start of your turn then remove one bleed. Lasts for the rest of the quest."));
			i = lowerText.IndexOf("mill");
			if (i >= 0) matches.Add((i, "Mill X - Discard the top X cards of your deck."));
			i = lowerText.IndexOf("frostbite");
			if (i >= 0) matches.Add((i, $"X Frostbite - When you have 3 stacks of frostbite, take {PassiveTooltipTextService.FrostbiteDamage} damage and lose 3 frostbite."));
			i = lowerText.IndexOf("frozen");
			if (i >= 0) matches.Add((i, "Frozen - When you play a frozen card, gain 1 frostbite and there's a 50% chance it's exhausted. Remove frozen by blocking with it."));
			if (matches.Count == 0) return string.Empty;
			matches.Sort((a, b) => a.Index.CompareTo(b.Index));
			var parts = new string[matches.Count];
			for (int j = 0; j < matches.Count; j++) parts[j] = matches[j].Tooltip;
			return string.Join("\n", parts);
		}
	}
}


