using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// When entering Block phase, draw up to player Intellect, respecting max hand size.
    /// If hand has N cards, draw max(0, Intellect - N) but never beyond max hand size.
    /// </summary>
    public class BlockPhaseDrawSystem : Core.System
    {
        private BattlePhase _lastPhase = BattlePhase.StartOfBattle;
        public BlockPhaseDrawSystem(EntityManager entityManager) : base(entityManager)
        {
            var s = entityManager.GetEntitiesWithComponent<BattlePhaseState>().FirstOrDefault()?.GetComponent<BattlePhaseState>();
            if (s != null) _lastPhase = s.Phase;
        }

        protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
        {
            return System.Array.Empty<Entity>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            var state = EntityManager.GetEntitiesWithComponent<BattlePhaseState>().FirstOrDefault()?.GetComponent<BattlePhaseState>();
            if (state == null) return;
            var current = state.Phase;
            if (current == BattlePhase.Block && _lastPhase != BattlePhase.Block)
            {
                // Do not draw when transitioning from ProcessEnemyAttack or StartOfBattle into Block
                if (_lastPhase != BattlePhase.ProcessEnemyAttack && _lastPhase != BattlePhase.StartOfBattle)
                {
                    var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
                    if (player != null)
                    {
                        int intellect = player.GetComponent<Intellect>()?.Value ?? 0;
                        var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
                        var deck = deckEntity?.GetComponent<Deck>();
                        if (deck != null)
                        {
                            int currentHand = deck.Hand.Count;
                            int spaceLeft = System.Math.Max(0, deck.MaxHandSize - currentHand);
                            int toDraw = System.Math.Min(spaceLeft, System.Math.Max(0, intellect));
                            if (toDraw > 0)
                            {
                                EventManager.Publish(new RequestDrawCardsEvent { Count = toDraw });
                            }
                        }
                    }
                }
            }
            _lastPhase = current;
        }
    }
}


