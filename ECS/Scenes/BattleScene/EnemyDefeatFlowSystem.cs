using System;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace Crusaders30XX.ECS.Systems
{
	public class EnemyDefeatFlowSystem : Core.System
	{
		private readonly ContentManager _content;

		private Guid? _pendingBurstId;
		private Entity _pendingEnemy;
		private bool _pendingIsPreview;

		public EnemyDefeatFlowSystem(EntityManager entityManager, ContentManager content) : base(entityManager)
		{
			_content = content;
			EventManager.Subscribe<BeginDefeatPresentationEvent>(OnBeginDefeatPresentation);
			EventManager.Subscribe<PixelBurstAnimationCompleted>(OnPixelBurstCompleted);
			EventManager.Subscribe<DeleteCachesEvent>(_ => OnCachesCleared());
			EventManager.Subscribe<StartBattleRequested>(_ => OnCachesCleared());
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return Array.Empty<Entity>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		private void OnBeginDefeatPresentation(BeginDefeatPresentationEvent evt)
		{
			if (evt?.Enemy == null) return;
			if (!evt.IsPreview && evt.Enemy.HasComponent<SuppressPortraitRender>()) return;

			if (!evt.IsPreview)
			{
				EventQueue.Clear();
			}

			if (_pendingBurstId.HasValue) return;

			SetDefeatPresentationActive(true);
			EnsureSuppressPortrait(evt.Enemy);

			if (!PortraitPixelBurstRequestBuilder.TryBuild(EntityManager, _content, evt.Enemy, evt.IsPreview, out var request))
			{
				FinishWithoutBurst(evt.Enemy, evt.IsPreview);
				return;
			}

			_pendingBurstId = request.BurstId;
			_pendingEnemy = evt.Enemy;
			_pendingIsPreview = evt.IsPreview;
			EventManager.Publish(request);
		}

		private void OnPixelBurstCompleted(PixelBurstAnimationCompleted evt)
		{
			if (evt == null || !_pendingBurstId.HasValue || evt.BurstId != _pendingBurstId.Value) return;

			var enemy = _pendingEnemy;
			var isPreview = _pendingIsPreview;
			ResetPending();

			if (isPreview)
			{
				EndDefeatPresentationAndRestorePortrait(enemy);
				return;
			}

			CompleteRealDefeat(enemy);
		}

		private void CompleteRealDefeat(Entity enemy)
		{
			EndDefeatPresentationInputFreeze();

			LoggingService.Append("EnemyDefeatFlowSystem.CompleteRealDefeat", new System.Text.Json.Nodes.JsonObject
			{
				["enemyId"] = enemy?.Id ?? -1
			});

			EventManager.Publish(new EnemyKilledEvent { Enemy = enemy });

			var enemyId = enemy?.GetComponent<Enemy>()?.EnemyBase?.Id;
			if (string.Equals(enemyId, "fallen_shepherd", StringComparison.OrdinalIgnoreCase))
			{
				EventManager.Publish(new ShowTransition { Scene = SceneId.WayStation, EndRunOnLoad = true });
				return;
			}

			var queuedEntity = EntityManager.GetEntity("QueuedEvents");
			var queued = queuedEntity?.GetComponent<QueuedEvents>();
			if (queued != null
				&& queued.Events != null
				&& queued.Events.Count > 0
				&& queued.CurrentIndex >= 0
				&& queued.CurrentIndex == queued.Events.Count - 1)
			{
				LoggingService.Append("EnemyDefeatFlowSystem.QuestComplete", new System.Text.Json.Nodes.JsonObject
				{
					["message"] = "attempting to save quest completion"
				});
				var completion = QuestCompleteService.SaveIfCompletedHighest(EntityManager);
				EventManager.Publish(new ShowQuestRewardOverlay
				{
					Message = "Quest Complete!",
					RewardGold = completion.IsNewlyCompleted ? completion.RewardGold : 0,
					HasCardReward = completion.HasCardReward,
					RewardCardKey = completion.RewardCardKey,
					RewardCardKeys = completion.RewardCardKeys
				});
				EventManager.Publish(new ChangeMusicTrack { Track = MusicTrack.QuestComplete });
			}
			else
			{
				EventManager.Publish(new ShowTransition { Scene = SceneId.Battle });
			}
		}

		private void FinishWithoutBurst(Entity enemy, bool isPreview)
		{
			ResetPending();
			if (isPreview)
			{
				EndDefeatPresentationAndRestorePortrait(enemy);
				return;
			}

			CompleteRealDefeat(enemy);
		}

		private void EndDefeatPresentationInputFreeze()
		{
			SetDefeatPresentationActive(false);
		}

		private void EndDefeatPresentationAndRestorePortrait(Entity enemy)
		{
			EndDefeatPresentationInputFreeze();
			RestorePortraitAfterPreview(enemy);
		}

		private void RestorePortraitAfterPreview(Entity enemy)
		{
			if (enemy != null && enemy.HasComponent<SuppressPortraitRender>())
			{
				EntityManager.RemoveComponent<SuppressPortraitRender>(enemy);
			}
		}

		private static void EnsureSuppressPortrait(Entity enemy)
		{
			if (!enemy.HasComponent<SuppressPortraitRender>())
			{
				enemy.AddComponent(new SuppressPortraitRender());
			}
		}

		private void SetDefeatPresentationActive(bool active)
		{
			var phaseEntity = EntityManager.GetEntitiesWithComponent<PhaseState>().FirstOrDefault();
			var phase = phaseEntity?.GetComponent<PhaseState>();
			if (phase != null)
			{
				phase.DefeatPresentationActive = active;
			}
		}

		private void OnCachesCleared()
		{
			ResetPending();
			SetDefeatPresentationActive(false);
		}

		private void ResetPending()
		{
			_pendingBurstId = null;
			_pendingEnemy = null;
			_pendingIsPreview = false;
		}
	}
}
