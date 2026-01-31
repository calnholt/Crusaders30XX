using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Crusaders30XX.Diagnostics;
using System;
using Crusaders30XX.ECS.Factories;
using Crusaders30XX.ECS.Services;
using System.Collections.Generic;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// Processes start-of-turn applied passives for player and enemy.
    /// Currently supports Burn: deals 1 damage to the owner per stack at start of their turn.
    /// </summary>
    public class AppliedPassivesManagementSystem : Core.System
    {
        public static readonly float Duration = 0.5f;
        public AppliedPassivesManagementSystem(EntityManager entityManager) : base(entityManager)
        {
            EventManager.Subscribe<ChangeBattlePhaseEvent>(OnChangeBattlePhase);
            EventManager.Subscribe<ApplyPassiveEvent>(OnApplyPassive);
            EventManager.Subscribe<ApplyEffect>(OnApplyEffect);
            EventManager.Subscribe<RemovePassive>(OnRemovePassive);
            EventManager.Subscribe<UpdatePassive>(OnUpdatePassive);
            EventManager.Subscribe<LoadSceneEvent>(OnLoadScene);
            EventManager.Subscribe<RemoveAllPassives>(OnRemoveAllPassives);
        }

        private void OnApplyEffect(ApplyEffect effect)
        {
            var typeName = effect.EffectType ?? string.Empty;
            if (!Enum.TryParse<AppliedPassiveType>(typeName, true, out var passiveType)) return;
            EventManager.Publish(new ApplyPassiveEvent { Delta = effect.Amount, Target = effect.Target, Type = passiveType });
            switch (passiveType)
            {
                case AppliedPassiveType.Aegis:
                    EventManager.Publish(new PlaySfxEvent { Track = SfxTrack.GainAegis, Volume = 0.5f });
                    break;
            }
        }

        protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
        {
            // No per-frame updates; event-driven
            return Array.Empty<Entity>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

        private void OnChangeBattlePhase(ChangeBattlePhaseEvent evt)
        {
            var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
            var enemy = EntityManager.GetEntitiesWithComponent<Enemy>().FirstOrDefault();
            if (evt == null) return;
            if (evt.Current == SubPhase.StartBattle)
            {
                var enemyBase = enemy.GetComponent<Enemy>();
                if (enemyBase != null && enemyBase.EnemyBase != null && enemyBase.EnemyBase.OnStartOfBattle != null)
                {
                    Console.WriteLine($"[AppliedPassivesManagementSystem] OnChangeBattlePhase - OnStartOfBattle - {enemyBase.EnemyBase.Id}");
                    EventManager.Publish(new StartDebuffAnimation { TargetIsPlayer = false });
                    enemyBase.EnemyBase.OnStartOfBattle(EntityManager);
                }
                EnemyShieldsMaintenance(enemy);
                ConvertPenanceToScar(player);
            }
            if (evt.Current == SubPhase.PlayerEnd)
            {
                RemoveTurnPassives(player);
            }
            else if (evt.Current == SubPhase.EnemyStart)
            {
                EnemyShieldsMaintenance(enemy);
                ApplyStartOfTurnPassives(enemy);
            }
            else if (evt.Current == SubPhase.EnemyEnd)
            {
                RemoveTurnPassives(enemy);
            }
            else if (evt.Current == SubPhase.PlayerStart)
            {
                ApplyStartOfTurnPassives(player);
            }
            else if (evt.Current == SubPhase.PreBlock)
            {
                ApplyStartOfPreBlockPassives(enemy);
            }
        }

        private void OnLoadScene(LoadSceneEvent @event)
        {
            // this is bad, make a direct event call to clean
            if (@event.Scene != SceneId.Battle) return;
            var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
            var ap = player.GetComponent<AppliedPassives>();
            if (ap == null) return;
            foreach (var passive in ap.Passives)
            {
                if (GetQuestPassives().Contains(passive.Key)) continue;
                EventManager.Publish(new RemovePassive { Owner = player, Type = passive.Key });
            }
        }

        private void EnemyShieldsMaintenance(Entity enemyEntity)
        {
            var enemy = enemyEntity.GetComponent<Enemy>();
            var ap = enemyEntity.GetComponent<AppliedPassives>();
            if (ap.Passives.TryGetValue(AppliedPassiveType.Shield, out int shieldAmount) && shieldAmount > 0)
            {
                EventManager.Publish(new ApplyPassiveEvent { Target = enemyEntity, Type = AppliedPassiveType.Shield, Delta = 1 });
            }
        }

        private void ConvertPenanceToScar(Entity player)
        {
            var ap = player.GetComponent<AppliedPassives>();
            if (ap == null) return;
            if (ap.Passives.TryGetValue(AppliedPassiveType.Penance, out int penanceStacks) && penanceStacks > 0)
            {
                EventQueueBridge.EnqueueTriggerAction("AppliedPassivesManagementSystem.ApplyStartOfTurnPassives.Inferno", () =>
                {
                    EventManager.Publish(new ApplyPassiveEvent { Target = player, Type = AppliedPassiveType.Scar, Delta = penanceStacks });
                    EventManager.Publish(new RemovePassive { Owner = player, Type = AppliedPassiveType.Penance });
                }, Duration);
            }
        }
        private void ApplyStartOfTurnPassives(Entity owner)
        {
            var ap = owner.GetComponent<AppliedPassives>();
            if (ap == null) return;
            if (ap == null || ap.Passives == null || ap.Passives.Count == 0) return;
            var hasInferno = ap.Passives.TryGetValue(AppliedPassiveType.Inferno, out int infernoStacks);
            if (hasInferno && infernoStacks > 0)
            {
                EventQueueBridge.EnqueueTriggerAction("AppliedPassivesManagementSystem.ApplyStartOfTurnPassives.Inferno", () =>
                {
                    EventManager.Publish(new ApplyPassiveEvent { Target = owner, Type = AppliedPassiveType.Burn, Delta = infernoStacks });
                    EventManager.Publish(new PassiveTriggered { Owner = owner, Type = AppliedPassiveType.Inferno });
                }, Duration);
            }
            if ((ap.Passives.TryGetValue(AppliedPassiveType.Burn, out int burnStacks) || hasInferno) && (burnStacks > 0 || infernoStacks > 0))
            {
                Console.WriteLine($"[AppliedPassivesManagementSystem] ApplyStartOfTurnPassives.Burn - {burnStacks} + {infernoStacks}");
                EventQueueBridge.EnqueueTriggerAction("AppliedPassivesManagementSystem.ApplyStartOfTurnPassives.Burn", () =>
                {
                    EventManager.Publish(new ModifyHpRequestEvent { Source = owner, Target = owner, Delta = -(burnStacks + infernoStacks), DamageType = ModifyTypeEnum.Effect });
                    EventManager.Publish(new PassiveTriggered { Owner = owner, Type = AppliedPassiveType.Burn });
                }, Duration);
            }
            if (ap.Passives.TryGetValue(AppliedPassiveType.Webbing, out int webbingStacks) && webbingStacks > 0)
            {
                EventQueueBridge.EnqueueTriggerAction("AppliedPassivesManagementSystem.ApplyStartOfTurnPassives.Webbing", () =>
                {
                    EventManager.Publish(new ApplyPassiveEvent { Target = owner, Type = AppliedPassiveType.Slow, Delta = webbingStacks });
                    EventManager.Publish(new PassiveTriggered { Owner = owner, Type = AppliedPassiveType.Webbing });
                }, Duration);
            }
            if (ap.Passives.TryGetValue(AppliedPassiveType.Bleed, out int bleedStacks) && bleedStacks > 0)
            {
                EventQueueBridge.EnqueueTriggerAction("AppliedPassivesManagementSystem.ApplyStartOfTurnPassives.Bleed", () =>
                {
                    EventManager.Publish(new ModifyHpRequestEvent { Source = owner, Target = owner, Delta = -1, DamageType = ModifyTypeEnum.Effect });
                    EventManager.Publish(new PassiveTriggered { Owner = owner, Type = AppliedPassiveType.Bleed });
                    EventManager.Publish(new UpdatePassive { Owner = owner, Type = AppliedPassiveType.Bleed, Delta = -1 });
                }, Duration);
            }
        }

        private void ApplyStartOfPreBlockPassives(Entity enemy)
        {
            var ap = enemy.GetComponent<AppliedPassives>();
            if (ap == null || ap.Passives == null || ap.Passives.Count == 0) return;

            if (ap.Passives.TryGetValue(AppliedPassiveType.Stun, out int stunStacks) && stunStacks > 0)
            {
                var intent = enemy.GetComponent<AttackIntent>();
                if (intent == null || intent.Planned == null || intent.Planned.Count == 0) return;
                var count = stunStacks > intent.Planned.Count ? intent.Planned.Count : stunStacks;
                for (int i = 0; i < count; i++)
                {
                    EventQueueBridge.EnqueueTriggerAction("AppliedPassivesManagementSystem.ApplyStartOfTurnPassives.Stun", () =>
                    {
                        EventManager.Publish(new ShowStunnedOverlay { ContextId = enemy.GetComponent<AttackIntent>()?.Planned?.FirstOrDefault()?.ContextId });
                        EventManager.Publish(new PassiveTriggered { Owner = enemy, Type = AppliedPassiveType.Stun });
                        EventManager.Publish(new UpdatePassive { Owner = enemy, Type = AppliedPassiveType.Stun, Delta = -1 });
                        var ctx = intent.Planned[0].ContextId;
                        intent.Planned.RemoveAt(0);
                        if (intent.Planned.Count == 0)
                        {
                            EventQueue.Clear();
                            EventQueue.EnqueueRule(new EventQueueBridge.QueuedPublish<ChangeBattlePhaseEvent>(
                                "Rule.ChangePhase.EnemyEnd",
                                new ChangeBattlePhaseEvent { Current = SubPhase.EnemyEnd }
                            ));
                            EventQueue.EnqueueRule(new EventQueueBridge.QueuedPublish<ChangeBattlePhaseEvent>(
                                "Rule.ChangePhase.PlayerStart",
                                new ChangeBattlePhaseEvent { Current = SubPhase.PlayerStart }
                            ));
                            EventQueue.EnqueueRule(new EventQueueBridge.QueuedPublish<ChangeBattlePhaseEvent>(
                                "Rule.ChangePhase.Action",
                                new ChangeBattlePhaseEvent { Current = SubPhase.Action }
                            ));
                        }
                    }, Duration);
                }
            }

            if (ap.Passives.TryGetValue(AppliedPassiveType.Aggression, out int aggressionStacks) && aggressionStacks > 0)
            {
                var attackDef = GetComponentHelper.GetPlannedAttack(EntityManager);
                if (attackDef == null) return;
                attackDef.Damage += aggressionStacks;
                attackDef.AdditionalDamage += aggressionStacks;
                Console.WriteLine($"[AppliedPassivesManagementSystem] ApplyStartOfPreBlockPassives.Aggression - {attackDef.Damage}");
                EventManager.Publish(new PassiveTriggered { Owner = enemy, Type = AppliedPassiveType.Aggression });
                TimerScheduler.Schedule(Duration, () =>
                {
                    EventManager.Publish(new RemovePassive { Owner = enemy, Type = AppliedPassiveType.Aggression });
                });
            }
            if (ap.Passives.TryGetValue(AppliedPassiveType.Power, out int powerStacks) && powerStacks > 0)
            {
                var attackDef = GetComponentHelper.GetPlannedAttack(EntityManager);
                if (attackDef == null) return;
                attackDef.Damage += powerStacks;
                attackDef.AdditionalDamage += powerStacks;
                Console.WriteLine($"[AppliedPassivesManagementSystem] ApplyStartOfPreBlockPassives.Power - {attackDef.Damage}");
                EventManager.Publish(new PassiveTriggered { Owner = enemy, Type = AppliedPassiveType.Power });
            }
        }

        private void OnApplyPassive(ApplyPassiveEvent e)
        {
            if (e == null || e.Target == null) return;
            var ap = e.Target.GetComponent<AppliedPassives>();
            if (ap == null)
            {
                // Create if missing on the target entity
                EntityManager.AddComponent(e.Target, new AppliedPassives());
                ap = e.Target.GetComponent<AppliedPassives>();
            }
            if (ap == null) return;

            ap.Passives.TryGetValue(e.Type, out int current);
            int next = current + e.Delta;
            Console.WriteLine($"[AppliedPassivesManagementSystem] OnApplyPassive - {e.Type} - {current} -> {next}");
            if (next <= 0)
            {
                // remove from dictionary if zero or less
                if (ap.Passives.ContainsKey(e.Type)) ap.Passives.Remove(e.Type);
            }
            else
            {
                ap.Passives[e.Type] = next;
            }

            switch (e.Type)
            {
                case AppliedPassiveType.Frostbite:
                    if (next >= PassiveTooltipTextService.FrostbiteThreshold)
                    {
                        EventManager.Publish(new FrostbiteTriggered { Target = e.Target });
                        EventManager.Publish(new ModifyHpRequestEvent { Source = e.Target, Target = e.Target, Delta = -PassiveTooltipTextService.FrostbiteDamage, DamageType = ModifyTypeEnum.Effect });
                        EventManager.Publish(new UpdatePassive { Owner = e.Target, Type = AppliedPassiveType.Frostbite, Delta = -PassiveTooltipTextService.FrostbiteDamage });
                    }
                    break;
                default:
                    break;
            }
        }

        private void OnRemovePassive(RemovePassive e)
        {
            Console.WriteLine($"[AppliedPassivesManagementSystem] OnRemovePassive - {e.Type} - {e.Owner.Name}");
            if (e == null || e.Owner == null) return;
            var ap = e.Owner.GetComponent<AppliedPassives>();
            if (ap == null || ap.Passives == null) return;
            if (ap.Passives.ContainsKey(e.Type))
            {
                ap.Passives.Remove(e.Type);
            }
        }

        private void OnUpdatePassive(UpdatePassive e)
        {
            if (e == null || e.Owner == null) return;
            var ap = e.Owner.GetComponent<AppliedPassives>();
            if (ap == null || ap.Passives == null) return;
            ap.Passives[e.Type] = ap.Passives[e.Type] + e.Delta;
            if (ap.Passives[e.Type] <= 0)
            {
                ap.Passives.Remove(e.Type);
            }
        }

        private void OnRemoveAllPassives(RemoveAllPassives e)
        {
            if (e == null || e.Owner == null) return;
            var ap = e.Owner.GetComponent<AppliedPassives>();
            if (ap == null || ap.Passives == null) return;
            ap.Passives.Clear();
        }

        private void RemoveTurnPassives(Entity owner)
        {
            EventManager.Publish(new RemovePassive { Owner = owner, Type = AppliedPassiveType.DowseWithHolyWater });
            EventManager.Publish(new RemovePassive { Owner = owner, Type = AppliedPassiveType.Aggression });
            var ap = owner.GetComponent<AppliedPassives>();
            if (ap == null || ap.Passives == null || ap.Passives.Count == 0) return;
            if (ap.Passives.TryGetValue(AppliedPassiveType.Silenced, out int silencedStacks) && silencedStacks > 0)
            {
                EventManager.Publish(new PassiveTriggered { Owner = owner, Type = AppliedPassiveType.Silenced });
                EventManager.Publish(new UpdatePassive { Owner = owner, Type = AppliedPassiveType.Silenced, Delta = -1 });
            }
        }

        public static HashSet<AppliedPassiveType> GetTurnPassives()
        {
            return new HashSet<AppliedPassiveType>
            {
                AppliedPassiveType.DowseWithHolyWater,
                AppliedPassiveType.Aggression
            };
        }

        public static HashSet<AppliedPassiveType> GetBattlePassives()
        {
            return new HashSet<AppliedPassiveType>
            {
                AppliedPassiveType.Stun,
                AppliedPassiveType.Burn,
                AppliedPassiveType.Power,
                AppliedPassiveType.Armor,
                AppliedPassiveType.Wounded,
                AppliedPassiveType.Inferno,
                AppliedPassiveType.Stealth,
                AppliedPassiveType.Poison,
                AppliedPassiveType.Siphon,
                AppliedPassiveType.Thorns,
                AppliedPassiveType.Rage,
                AppliedPassiveType.Intimidated,
                AppliedPassiveType.MindFog,
                AppliedPassiveType.Channel,
                AppliedPassiveType.Windchill,
                AppliedPassiveType.SubZero,
                AppliedPassiveType.Aegis,
                AppliedPassiveType.Anathema,
            };
        }
        public static HashSet<AppliedPassiveType> GetQuestPassives()
        {
            return new HashSet<AppliedPassiveType>
            {
                AppliedPassiveType.Shackled,
                AppliedPassiveType.Webbing,
                AppliedPassiveType.Penance,
                AppliedPassiveType.Fear,
                AppliedPassiveType.Bleed,
                AppliedPassiveType.Frostbite,
                AppliedPassiveType.Enflamed,
                AppliedPassiveType.Scar,
                AppliedPassiveType.Sealed,
                AppliedPassiveType.Silenced,
            };
        }
    }
}


