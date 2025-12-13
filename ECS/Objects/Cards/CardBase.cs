using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class CardBase
    {
        public string CardId { get; set; }
        public string Name { get; set; } = "";
        public int Damage { get; set; } = 0;
        public int Block { get; set; } = 0;
        public List<string> Cost { get; set; } = [];
        
        private string _text = "";
        public string Text
        {
            get => _text;
            set
            {
                _text = value;
                
                if (!string.IsNullOrEmpty(_text))
                {
                    var valuesList = new List<int>();
                    var pattern = @"\{(\d+)\}";
                    var matches = Regex.Matches(_text, pattern);

                    foreach (Match match in matches)
                    {
                        if (int.TryParse(match.Groups[1].Value, out int parsedValue))
                        {
                            valuesList.Add(parsedValue);
                        }
                    }

                    ValuesParse = valuesList.ToArray();

                    string resolved = _text;
                    foreach (Match match in matches)
                    {
                        resolved = resolved.Replace(match.Value, match.Groups[1].Value);
                    }
                    _text = resolved;
                }
            }
        }
        
        public string Animation { get; set; } = "";
        public string Type { get; set; } = "";
        public string Target { get; set; } = "";
        public int[] ValuesParse { get; set; } = [];
        public bool IsFreeAction { get; set; } = false;
        public bool ExhaustsOnPlay { get; set; } = false;
        public bool ExhaustsOnEndTurn { get; set; } = false;
        public bool CanAddToLoadout { get; set; } = true;
        public bool IsToken { get; set; } = false;
        public bool IsBlockCard { get; set; } = false;
        public bool IsWeapon { get; set; } = false;
        public string Tooltip { get; set; } = "";
        public string CardTooltip { get; set; } = "";
        public string SpecialAction { get; set; } = "";
        public string OriginalText { get; set; } = "";


#nullable enable annotations
        public Action<EntityManager, Entity>? OnPlay { get; protected set; }
        public Action<EntityManager, Entity>? OnBlock { get; protected set; }
        public Func<EntityManager, Entity, bool>? CanPlay { get; protected set; } = (a, b) => true;
        public Func<EntityManager, Entity, int>? GetConditionalDamage { get; protected set; } = (a, b) => 0;

        public string GetCardHint(CardData.CardColor color)
        {
            StringBuilder sb = new StringBuilder();
            if (IsWeapon)
            {
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
            if (IsFreeAction)
            {
                sb.Append("Free action cards do not consume an action point.\n\n");
            }
            else
            {
                sb.Append("This card requires one action point to play.\n\n");
            }
            if (Cost.Count > 0)
            {
                sb.Append("This card requires discarding the following costs to play: ");
                foreach (string cost in Cost)
                {
                    sb.Append($"{cost.ToLower()} ");
                }
                sb.Append(".\n\n");
                if (Cost.Contains("Any"))
                {
                    sb.Append("(You can discard ANY color of card to satisfy an \"any\" cost.)\n\n");
                }
            }
            return sb.ToString();
        }
    }
}