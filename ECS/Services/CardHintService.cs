using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Cards;
using Crusaders30XX.ECS.Data.Locations;
using Crusaders30XX.ECS.Events;
using System;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Crusaders30XX.ECS.Services
{
	/// <summary>
	/// Service that applies tribulation effects to the player based on quest ID.
	/// Effects are keyed entirely by quest ID - no string parsing needed.
	/// </summary>
	public static class CardHintService
	{
		/// <summary>
		/// Applies tribulation effects for the given quest ID.
		/// Looks up quest data and applies effects based on hardcoded quest ID matching.
		/// </summary>
		public static string GetCardHint(CardDefinition def, CardData.CardColor color)
		{
			StringBuilder sb = new StringBuilder();
      if (def.isWeapon) {
        sb.Append("Weapons are a special type of card that can play once each action phase. They cannot be used to block enemy attacks, do not count towards your maximum hand size, and cannot be discarded to pay the costs of other cards in your hand.\n\n");
        return sb.ToString();
      }
      sb.Append("Cards have different uses! They can be used to play for their effect on your action phase, can be used to block enemy attacks, or be discarded to pay the costs of other cards in your hand.\n\n");
      switch (color)
      {
        case CardData.CardColor.White:
          sb.Append("When a white card is used to block an enemy attack, you gain 1 temperance.\n\n");
          break;
        case CardData.CardColor.Red:
          sb.Append("When a red card is used to block an enemy attack, you gain 1 courage.\n\n");
          break;
        case CardData.CardColor.Black:
          sb.Append("Black cards have +1 block value from their red and white counterparts.\n\n");
          break;
      }
      if (def.isFreeAction) {
        sb.Append("Free action cards do not consume an action point.\n\n");
      }
      else {
        sb.Append("This card requires one action point to play.\n\n");
      }
      if (def.cost.Length > 0) {
        sb.Append("This card requires discarding the following costs to play: ");
        foreach (string cost in def.cost) {
          sb.Append($"{cost.ToLower()} ");
        }
        sb.Append(".\n\n");
        if (def.cost.Contains("Any")) {
          sb.Append("(You can discard ANY color of card to satisfy an \"any\" cost.)\n\n");
        }
      }
      return sb.ToString();
		}
	}
}

