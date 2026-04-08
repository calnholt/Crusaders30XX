using System;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Services;
using Microsoft.Xna.Framework;
using Crusaders30XX.Diagnostics;

namespace Crusaders30XX.ECS.Scenes.BattleScene
{
	[DebugTab("Sanguine Curse")]
	public class SanguineCurseSystem : Core.System
	{
		private const int SanguineCurseThreshold = 7;
		private const int SanguineCursePenance = 1;

		private int _currentDamage = 0;
		private bool _hasTriggeredThisTurn = false;

		public SanguineCurseSystem(EntityManager entityManager) : base(entityManager)
		{
			EventManager.Subscribe<ModifyHpEvent>(OnModifyHp);
			EventManager.Subscribe<ChangeBattlePhaseEvent>(OnChangeBattlePhase);
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<SceneState>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			// Update the PassiveMeterComponent on the tooltip entity each frame
			var enemy = EntityManager.GetEntity("Enemy");
			if (enemy == null) return;

			if (!HasSanguineCurse(enemy))
			{
				RemoveMeterComponent(enemy);
				return;
			}

			UpdateMeterComponent(enemy);
		}

		private void OnChangeBattlePhase(ChangeBattlePhaseEvent evt)
		{
			LoggingService.Append("SanguineCurseSystem.OnChangeBattlePhase", new System.Text.Json.Nodes.JsonObject
			{
				["phase"] = evt.Current.ToString()
			});
			if (evt.Current == SubPhase.EnemyStart)
			{
				_currentDamage = 0;
				_hasTriggeredThisTurn = false;
			}
		}

		private void OnModifyHp(ModifyHpEvent evt)
		{
			LoggingService.Append("SanguineCurseSystem.OnModifyHp", new System.Text.Json.Nodes.JsonObject
			{
				["delta"] = evt.Delta,
				["currentDamage"] = _currentDamage,
				["targetId"] = evt.Target?.Id ?? -1
			});
			if (_hasTriggeredThisTurn) return;
			// Only track damage dealt to enemy
			if (evt.Delta >= 0) return;
			var enemy = EntityManager.GetEntity("Enemy");
			if (enemy == null || evt.Target != enemy) return;

			// Check if enemy has SanguineCurse
			if (!HasSanguineCurse(enemy)) return;

			// Calculate actual damage (already accounts for Armor/Wounded in the HP system)
			int actualDamage = Math.Abs(evt.Delta);
			_currentDamage += actualDamage;

			if (_currentDamage >= SanguineCurseThreshold)
			{
				EventManager.Publish(new ApplyPassiveEvent
				{
					Target = EntityManager.GetEntity("Player"),
					Type = AppliedPassiveType.Penance,
					Delta = SanguineCursePenance
				});
				_currentDamage = 0;
				_hasTriggeredThisTurn = false;
			}
		}

		private bool HasSanguineCurse(Entity enemy)
		{
			var ap = enemy?.GetComponent<AppliedPassives>();
			if (ap == null || ap.Passives == null) return false;
			return ap.Passives.TryGetValue(AppliedPassiveType.SanguineCurse, out var stacks) && stacks > 0;
		}

		private void UpdateMeterComponent(Entity enemy)
		{
			var anchorName = $"UI_PassiveTooltip_{enemy.Id}_{AppliedPassiveType.SanguineCurse}";
			var anchor = EntityManager.GetEntity(anchorName);
			if (anchor == null) return;

			var meter = anchor.GetComponent<PassiveMeterComponent>();
			if (meter == null)
			{
				meter = new PassiveMeterComponent
				{
					CurrentValue = _currentDamage,
					MaxValue = SanguineCurseThreshold,
					Direction = PassiveMeterDirection.FillUp,
					IsActive = true
				};
				EntityManager.AddComponent(anchor, meter);
			}
			else
			{
				meter.CurrentValue = _currentDamage;
				meter.MaxValue = SanguineCurseThreshold;
				meter.Direction = PassiveMeterDirection.FillUp;
				meter.IsActive = true;
			}
		}

		private void RemoveMeterComponent(Entity enemy)
		{
			var anchorName = $"UI_PassiveTooltip_{enemy.Id}_{AppliedPassiveType.SanguineCurse}";
			var anchor = EntityManager.GetEntity(anchorName);
			if (anchor != null && anchor.HasComponent<PassiveMeterComponent>())
			{
				EntityManager.RemoveComponent<PassiveMeterComponent>(anchor);
			}
		}
	}
}
