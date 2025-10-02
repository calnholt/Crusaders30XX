using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Data.Attacks;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// Manages MarkedForSpecificDiscard components based on the current enemy attack context.
    /// Extracted from EnemyAttackDisplaySystem.
    /// </summary>
    public class MarkedForSpecificDiscardSystem : Core.System
    {
		private string _lastContextId = null;
		private readonly System.Random _random = new System.Random();
        public MarkedForSpecificDiscardSystem(EntityManager entityManager) : base(entityManager)
        {
            // When the phase leaves Block/EnemyAttack, clear marks
            EventManager.Subscribe<ChangeBattlePhaseEvent>(evt =>
            {
                if (evt.Current != SubPhase.Block && evt.Current != SubPhase.EnemyAttack)
                {
                    ClearExistingSpecificDiscardMarks();
					_lastContextId = null;
                }
            });
        }

        protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<AttackIntent>();
        }

        protected override void UpdateEntity(Entity entity, Microsoft.Xna.Framework.GameTime gameTime)
        {
            var intent = entity.GetComponent<AttackIntent>();
			if (intent == null || intent.Planned.Count == 0) { ClearExistingSpecificDiscardMarks(); _lastContextId = null; return; }

            var ctx = intent.Planned[0].ContextId;
			if (_lastContextId != ctx)
			{
				// New attack context: clear previous marks once and remember this context
				ClearExistingSpecificDiscardMarks();
				_lastContextId = ctx;
			}
            TryPreselectSpecificDiscards(ctx);
        }

        private AttackDefinition LoadAttackDefinition(string id)
        {
            AttackDefinitionCache.TryGet(id, out var def);
            return def;
        }

		private void TryPreselectSpecificDiscards(string contextId)
        {
            try
            {
                var enemy = GetRelevantEntities().FirstOrDefault();
                var intent = enemy?.GetComponent<AttackIntent>();
                var pa = intent?.Planned?.FirstOrDefault(x => x.ContextId == contextId);
                if (pa == null) return;
                var def = LoadAttackDefinition(pa.AttackId);
                if (def == null || def.effectsOnNotBlocked == null) return;
                int amount = def.effectsOnNotBlocked.Where(e => e.type == "DiscardSpecificCard").Sum(e => e.amount);
                if (amount <= 0) { ClearExistingSpecificDiscardMarks(); return; }
				// If already selected for this context, persist selections (do not reseed on block assignments)
				bool alreadySelected = EntityManager.GetEntitiesWithComponent<MarkedForSpecificDiscard>()
					.Any(e => e.GetComponent<MarkedForSpecificDiscard>()?.ContextId == contextId);
				if (alreadySelected) return;

				// Select random cards from hand (excluding weapon card)
                var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
                var deck = deckEntity?.GetComponent<Deck>();
                if (deck == null) return;
                // Identify weapon to exclude
                Entity weapon = null;
                try
                {
                    var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
                    weapon = player?.GetComponent<EquippedWeapon>()?.SpawnedEntity;
                }
                catch { }
				var candidates = deck.Hand.Where(c => !ReferenceEquals(c, weapon)).ToList();
				int pick = System.Math.Min(amount, candidates.Count);
				if (pick <= 0) return;
				// Shuffle candidates and take the first N
				var selected = candidates.OrderBy(_ => _random.Next()).Take(pick).ToList();
				foreach (var card in selected)
				{
					var mark = card.GetComponent<MarkedForSpecificDiscard>();
					if (mark == null)
					{
						mark = new MarkedForSpecificDiscard { ContextId = contextId };
						EntityManager.AddComponent(card, mark);
					}
					else
					{
						mark.ContextId = contextId;
					}
				}
            }
            catch { }
        }

        private void ClearExistingSpecificDiscardMarks()
        {
            try
            {
                foreach (var e in EntityManager.GetEntitiesWithComponent<MarkedForSpecificDiscard>())
                {
                    EntityManager.RemoveComponent<MarkedForSpecificDiscard>(e);
                }
            }
            catch { }
        }
    }
}


