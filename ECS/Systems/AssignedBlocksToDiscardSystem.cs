using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Crusaders30XX.Diagnostics;

namespace Crusaders30XX.ECS.Systems
{
    [DebugTab("Assigned Blocks -> Discard")]
    public class AssignedBlocksToDiscardSystem : Core.System
    {
        private readonly GraphicsDevice _graphics;

        [DebugEditable(DisplayName = "Start Delay Between Cards (s)", Step = 0.02f, Min = 0f, Max = 2f)]
        public float StartDelayBetweenCardsSeconds { get; set; } = 0.12f;

        [DebugEditable(DisplayName = "Flight Duration (s)", Step = 0.02f, Min = 0.05f, Max = 2f)]
        public float FlightDurationSeconds { get; set; } = 0.35f;

        [DebugEditable(DisplayName = "Arc Height (px)", Step = 2, Min = 0, Max = 600)]
        public int ArcHeightPx { get; set; } = 80;

        [DebugEditable(DisplayName = "End Scale", Step = 0.02f, Min = 0.1f, Max = 1.2f)]
        public float EndScale { get; set; } = 0.3f;

        [DebugEditable(DisplayName = "Auto Advance To Action (s)", Step = 0.05f, Min = 0f, Max = 5f)]
        public float AutoAdvanceSeconds { get; set; } = 0.6f;

        private float _phaseElapsed;

        public AssignedBlocksToDiscardSystem(EntityManager em, GraphicsDevice gd) : base(em)
        {
            _graphics = gd;
            EventManager.Subscribe<DebugCommandEvent>(OnDebugCommand);
        }

        protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<CardToDiscardFlight>();
        }

        public override void Update(GameTime gameTime)
        {
            var state = EntityManager.GetEntitiesWithComponent<BattlePhaseState>().FirstOrDefault()?.GetComponent<BattlePhaseState>();
            if (state == null || state.Phase != BattlePhase.ProcessEnemyAttack)
            {
                base.Update(gameTime);
                return;
            }

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _phaseElapsed += dt;

            // Snapshot to avoid modifying the underlying collection during iteration
            var flights = GetRelevantEntities().ToList();
            for (int i = 0; i < flights.Count; i++)
            {
                UpdateEntity(flights[i], gameTime);
            }

            // If no flights remain and enough time passed, advance to Action
            if (!GetRelevantEntities().Any() && AutoAdvanceSeconds > 0f && _phaseElapsed >= AutoAdvanceSeconds)
            {
                EventManager.Publish(new ChangeBattlePhaseEvent { Next = BattlePhase.Action });
                _phaseElapsed = 0f;
            }
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
            var flight = entity.GetComponent<CardToDiscardFlight>();
            var t = entity.GetComponent<Transform>();
            if (flight == null || t == null) return;
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            flight.ElapsedSeconds += dt;
            if (!flight.Started)
            {
                if (flight.ElapsedSeconds >= flight.StartDelaySeconds)
                {
                    flight.Started = true;
                    flight.ElapsedSeconds = 0f;
                }
                return;
            }

            float dur = System.Math.Max(0.001f, flight.DurationSeconds);
            float p = MathHelper.Clamp(flight.ElapsedSeconds / dur, 0f, 1f);
            // simple quadratic bezier-like arc using a parabola height
            Vector2 pos = Vector2.Lerp(flight.StartPos, flight.TargetPos, p);
            float arc = flight.ArcHeightPx * 4f * p * (1f - p); // up then down
            pos.Y -= arc;
            t.Position = pos;
            float scl = MathHelper.Lerp(flight.StartScale, flight.EndScale, p);
            t.Scale = new Vector2(scl, scl);
            // Mirror motion to AssignedBlockCard so existing display system renders the motion
            var abc = entity.GetComponent<AssignedBlockCard>();
            if (abc != null)
            {
                abc.CurrentPos = pos;
                abc.CurrentScale = scl;
            }

            if (p >= 1f && !flight.Completed)
            {
                // Move to discard zone
                var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
                flight.Completed = true;
                EventManager.Publish(new CardMoveRequested
                {
                    Card = entity,
                    Deck = deckEntity,
                    Destination = CardZoneType.DiscardPile,
                    ContextId = flight.ContextId,
                    Reason = "AssignedBlockToDiscard"
                });
                // Remove animation component
                EntityManager.RemoveComponent<CardToDiscardFlight>(entity);
                // Clear AssignedBlockCard if still present
                EntityManager.RemoveComponent<AssignedBlockCard>(entity);
            }
        }

        private void OnDebugCommand(DebugCommandEvent evt)
        {
            if (evt.Command != "AnimateAssignedBlocksToDiscard") return;
            // Kick off flights for all assigned block cards in the current context
            var enemy = EntityManager.GetEntitiesWithComponent<AttackIntent>().FirstOrDefault();
            var ctx = enemy?.GetComponent<AttackIntent>()?.Planned?.FirstOrDefault()?.ContextId;
            if (string.IsNullOrEmpty(ctx)) return;

            var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
            if (deckEntity == null) return;

            // Compute discard target rect center from DiscardPileDisplaySystem layout
            int h = _graphics.Viewport.Height;
            int panelW = 60; // defaults in DiscardPileDisplaySystem
            int panelH = 80;
            int margin = 20;
            var target = new Vector2(margin + panelW * 0.5f, h - margin - panelH * 0.5f);

            var assigned = EntityManager.GetEntitiesWithComponent<AssignedBlockCard>()
                .Where(e => e.GetComponent<AssignedBlockCard>()?.ContextId == ctx)
                .OrderBy(e => e.GetComponent<AssignedBlockCard>().AssignedAtTicks)
                .ToList();

            for (int i = 0; i < assigned.Count; i++)
            {
                var card = assigned[i];
                var abc = card.GetComponent<AssignedBlockCard>();
                var t = card.GetComponent<Transform>();
                if (t == null) { t = new Transform(); EntityManager.AddComponent(card, t); t.Position = abc.CurrentPos; t.Scale = new Vector2(abc.CurrentScale, abc.CurrentScale); }

                var flight = new CardToDiscardFlight
                {
                    StartPos = abc.CurrentPos,
                    TargetPos = target,
                    StartDelaySeconds = StartDelayBetweenCardsSeconds * i,
                    DurationSeconds = FlightDurationSeconds,
                    ArcHeightPx = ArcHeightPx,
                    StartScale = abc.CurrentScale,
                    EndScale = EndScale,
                    ContextId = ctx
                };
                EntityManager.AddComponent(card, flight);
            }

            _phaseElapsed = 0f; // reset auto-advance timer
        }
    }
}


