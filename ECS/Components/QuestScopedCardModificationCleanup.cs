using System;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Systems;

namespace Crusaders30XX.ECS.Components
{
    /// <summary>
    /// Removes quest-scoped ModifiedDamage/ModifiedBlock when the quest node completes.
    /// </summary>
    public class QuestScopedCardModificationCleanup : IComponent, IDisposable
    {
        public Entity Owner { get; set; }
        public string ModificationReason { get; set; } = "";
        public bool UseBlock { get; set; }
        private EntityManager _entityManager;
        private bool _disposed;

        public void Initialize(EntityManager entityManager, Entity card)
        {
            Owner = card;
            _entityManager = entityManager;
            EventManager.Subscribe<ShowQuestRewardOverlay>(OnQuestComplete);
        }

        private void OnQuestComplete(ShowQuestRewardOverlay evt)
        {
            if (_disposed || Owner == null) return;
            if (UseBlock)
            {
                BlockValueService.RemoveModification(Owner, ModificationReason);
            }
            else
            {
                AttackDamageValueService.RemoveModification(Owner, ModificationReason);
            }
            Dispose();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            EventManager.Unsubscribe<ShowQuestRewardOverlay>(OnQuestComplete);
        }
    }
}
