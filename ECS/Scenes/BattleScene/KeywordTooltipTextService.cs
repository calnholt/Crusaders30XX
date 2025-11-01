using Crusaders30XX.ECS.Data.Cards;
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
			if (i >= 0) matches.Add((i, "Stun - Skip the next attack."));
			i = lowerText.IndexOf("inferno");
			if (i >= 0) matches.Add((i, "Inferno - At the start of the turn, gain 1 burn."));
			i = lowerText.IndexOf("slow");
			if (i >= 0) matches.Add((i, "Slow - Ambush attacks are 1 second faster."));
			i = lowerText.IndexOf("aegis");
			if (i >= 0) matches.Add((i, "Aegis - Prevent the next 1 damage from any source."));
			i = lowerText.IndexOf("burn");
			if (i >= 0) matches.Add((i, "Burn - At the start of the turn, take 1 damage."));
			i = lowerText.IndexOf("aggression");
			if (i >= 0) matches.Add((i, "Aggression - Your next attack this turn gains +1 damage."));
			i = lowerText.IndexOf("power");
			if (i >= 0) matches.Add((i, "Power - Your attacks deal +1 damage."));

			if (matches.Count == 0) return string.Empty;
			matches.Sort((a, b) => a.Index.CompareTo(b.Index));
			var parts = new string[matches.Count];
			for (int j = 0; j < matches.Count; j++) parts[j] = matches[j].Tooltip;
			return string.Join("\n", parts);
		}
	}
}


