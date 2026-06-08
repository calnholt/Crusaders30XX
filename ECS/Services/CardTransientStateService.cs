using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;

namespace Crusaders30XX.ECS.Services
{
    public static class CardTransientStateService
    {
        public static bool ClearAssignedBlockHotKey(EntityManager entityManager, Entity card)
        {
            if (entityManager == null || card == null || !card.HasComponent<HotKey>()) return false;

            entityManager.RemoveComponent<HotKey>(card);
            return true;
        }

        public static bool ClearHandVisibilityFilters(EntityManager entityManager, Entity card)
        {
            if (entityManager == null || card == null || !card.HasComponent<FilteredFromHand>()) return false;

            entityManager.RemoveComponent<FilteredFromHand>(card);
            return true;
        }
    }
}
