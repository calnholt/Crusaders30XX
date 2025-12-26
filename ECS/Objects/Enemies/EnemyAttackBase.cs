

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Crusaders30XX.ECS.Core;

namespace Crusaders30XX.ECS.Objects.EnemyAttacks
{
  public class EnemyAttackBase
  {
    public string Id { get; set; }
    public string Name { get; set; }
    public int Damage { get; set; }
    public ConditionType ConditionType { get; set; } = ConditionType.None;
    public int[] ValuesParse { get; set; } = [];
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
                var pattern = @"\[(\d+)\]";
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

    #nullable enable annotations
    public Action<EntityManager>? OnAttackReveal { get; protected set; }
    public Action<EntityManager>? OnAttackHit { get; protected set; }
    public Action<EntityManager>? OnBlocksConfirmed { get; protected set; }
    public Action<EntityManager, Entity>? OnBlockProcessed { get; protected set; }
    public Action<EntityManager>? OnBlockAssigned { get; protected set; }
    public Func<EntityManager, bool>? ProgressOverride { get; protected set; }
  }


  public enum ConditionType
  {
    OnHit,
    OnBlockedBy1Card,
    OnBlockedBy2Cards,
    None,
  }
}