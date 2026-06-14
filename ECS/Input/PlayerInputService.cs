using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;

namespace Crusaders30XX.ECS.Input
{
    public static class PlayerInputService
    {
        public static PlayerInputFrame GetFrame(EntityManager entityManager)
        {
            return entityManager
                .GetEntitiesWithComponent<PlayerInputState>()
                .FirstOrDefault()
                ?.GetComponent<PlayerInputState>()
                ?.Frame ?? default;
        }
    }
}
