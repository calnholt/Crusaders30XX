

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Services;

namespace Crusaders30XX.ECS.Objects.EnemyAttacks
{
  public class EnemyAttackBase
  {
    public string Id { get; set; }
    public string Name { get; set; }
    public int Damage { get; set; }
    public int AdditionalDamage { get; set; } = 0;
    public ConditionType ConditionType { get; set; } = ConditionType.None;
    public BlockingRestrictionType BlockingRestrictionType { get; set; } = BlockingRestrictionType.None;
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
    public int AmbushPercentage { get; set; } = 0;
    public bool IsTextConditionFulfilled { get; set; } = true;
    public bool IsQuestOneBattle { get; set; } = false;
    public int Channel { get; set; } = 0;
    public static EntityManager EntityManager { get; set; }

    #nullable enable annotations
    public Action<EntityManager>? OnAttackReveal { get; protected set; }
    public Action<EntityManager>? OnAttackHit { get; protected set; }
    public Action<EntityManager>? OnBlocksConfirmed { get; protected set; }
    public Action<EntityManager, Entity>? OnBlockProcessed { get; protected set; }
    public Action<EntityManager>? OnBlockAssigned { get; protected set; }
    public Action<EntityManager>? OnChannelApplied { get; protected set; }
    public Func<EntityManager, bool>? ProgressOverride { get; protected set; }

    public void Initialize(EntityManager entityManager)    
    {
      EntityManager = entityManager;
      IsQuestOneBattle = GetComponentHelper.IsLastBattleOfQuest(EntityManager);
      GetComponentHelper.GetAppliedPassives(EntityManager, "Enemy").Passives.TryGetValue(AppliedPassiveType.Channel, out int count);
      Channel = count;
      OnChannelApplied?.Invoke(entityManager);
    }
  }


  public enum ConditionType
  {
    OnHit,
    OnBlockedByAtLeast1Card,
    OnBlockedByAtLeast2Cards,
    MustBeBlockedByAtLeast1Card,
    MustBeBlockedByAtLeast2Cards,
    MustBeBlockedByExactly1Card,
    MustBeBlockedByExactly2Cards,
    None,
  }

  public enum BlockingRestrictionType
  {
    None,
    OnlyRed,
    OnlyBlack,
    OnlyWhite,
    NotRed,
    NotBlack,
    NotWhite,
  }
}