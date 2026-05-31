using Crusaders30XX.ECS.Core;

namespace Crusaders30XX.ECS.Objects.Medals
{
    /// <summary>
    /// Passive: enemy HP uses effective deck size minus 4 (see EntityFactory.CreateEnemyFromId).
    /// </summary>
    public class StClare : MedalBase
    {
        public const string MedalId = "st_clare";

        public StClare()
        {
            Id = MedalId;
            Name = "St. Clare of Assisi";
            Text = "Enemies have less HP (deck size - 4).";
        }

        public override void Initialize(EntityManager entityManager, Entity medalEntity)
        {
            EntityManager = entityManager;
            MedalEntity = medalEntity;
        }

        public override void Dispose()
        {
        }
    }
}
