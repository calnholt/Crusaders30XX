using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Crusaders30XX.ECS.Core;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class CardBase
    {
        public string CardId { get; set; }
        public string Name { get; set; } = "";
        public int Damage { get; set; } = 0;
        public int Block { get; set; } = 0;
        public int Cost { get; set; } = 0;
        public string Text { get; set; } = "";
        public string Animation { get; set; } = "";
        public string Type { get; set; } = "";
        public string Target { get; set; } = "";
        public int[] ValuesParse { get; set; } = [];
        public bool IsFreeAction { get; set; } = false;
        public bool ExhaustsOnPlay { get; set; } = false;
        public bool ExhaustsOnEndTurn { get; set; } = false;
        public bool CanAddToLoadout { get; set; } = true;
        public bool IsToken { get; set; } = false;

        #nullable enable annotations    
        public Action<EntityManager, Entity>? OnPlay { get; protected set; }
        public Func<EntityManager, Entity, bool>? CanPlay { get; protected set; } = (a,b) => true;
        public Func<EntityManager, Entity, int>? GetConditionalDamage { get; protected set; } = (a,b) => 0;

        public CardBase()
        {
            if (!string.IsNullOrEmpty(Text))
                {
                    var valuesList = new List<int>();
                    var pattern = @"\{(\d+)\}";
                    var matches = Regex.Matches(Text, pattern);
                    
                    foreach (Match match in matches)
                    {
                        if (int.TryParse(match.Groups[1].Value, out int value))
                        {
                            valuesList.Add(value);
                        }
                    }
                    
                    ValuesParse = valuesList.ToArray();
                    
                    string resolved = Text;
                    foreach (Match match in matches)
                    {
                        resolved = resolved.Replace(match.Value, match.Groups[1].Value);
                    }
                    Text = resolved;
                }
        }

    }
}