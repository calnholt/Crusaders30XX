using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Crusaders30XX.Diagnostics;
using System.Collections.Generic;
using Crusaders30XX.ECS.Data.Equipment;

namespace Crusaders30XX.ECS.Systems
{
    [DebugTab("Assigned Blocks -> Discard")]
    public class AssignedBlocksToDiscardSystem : Core.System
    {
        private readonly GraphicsDevice _graphics;

        [DebugEditable(DisplayName = "Start Delay Between Cards (s)", Step = 0.02f, Min = 0f, Max = 2f)]
		public float StartDelayBetweenCardsSeconds { get; set; } = 0.1f;

		[DebugEditable(DisplayName = "Flight Duration (s)", Step = 0.02f, Min = 0.05f, Max = 2f)]
		public float FlightDurationSeconds { get; set; } = 0.21f;

		[DebugEditable(DisplayName = "Arc Height (px)", Step = 2, Min = 0, Max = 600)]
		public int ArcHeightPx { get; set; } = 180;

		[DebugEditable(DisplayName = "End Scale", Step = 0.02f, Min = 0.1f, Max = 1.2f)]
		public float EndScale { get; set; } = 0.14f;

		[DebugEditable(DisplayName = "Auto Advance To Action (s)", Step = 0.05f, Min = 0f, Max = 5f)]
		public float AutoAdvanceSeconds { get; set; } = 0.3f;

        private float _phaseElapsed;

        public AssignedBlocksToDiscardSystem(EntityManager em, GraphicsDevice gd) : base(em)
        {
            _graphics = gd;
            EventManager.Subscribe<DebugCommandEvent>(OnDebugCommand);
        }

        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<CardToDiscardFlight>();
        }

        public override void Update(GameTime gameTime)
        {
            var phaseStateEntity = EntityManager.GetEntitiesWithComponent<PhaseState>().FirstOrDefault();
            if (phaseStateEntity == null) return;
            var phase = phaseStateEntity.GetComponent<PhaseState>();
            if (phase.Sub != SubPhase.EnemyAttack)
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

            // Do not auto-advance phase here; queued rules orchestrate transitions
            if (!GetRelevantEntities().Any())
            {
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
                // Move to discard zone or restore equipment
                flight.Completed = true;
                var isEquipment = entity.GetComponent<EquippedEquipment>() != null;
                if (isEquipment)
                {
                    // For equipment, just clear assignment and return to Default zone (no discard)
                    var zone = entity.GetComponent<EquipmentZone>();
                    if (zone == null) { zone = new EquipmentZone(); EntityManager.AddComponent(entity, zone); }
                    zone.Zone = EquipmentZoneType.Default;
                    // Update usage count for this equipment id
                    var eqComp = entity.GetComponent<EquippedEquipment>();
                    if (eqComp != null && !string.IsNullOrEmpty(eqComp.EquipmentId))
                    {
                        EventManager.Publish(new EquipmentUseResolved { EquipmentId = eqComp.EquipmentId, Delta = 1 });
                    }
                    // Mirror card resolution rewards: red equipment grants Courage, white grants Temperance
                    try
                    {
                        var eq = entity.GetComponent<EquippedEquipment>();
                        if (eq != null && EquipmentDefinitionCache.TryGet(eq.EquipmentId, out var def) && def != null)
                        {
                            string c = (def.color ?? string.Empty).Trim().ToLowerInvariant();
                            if (c == "red" || c == "r")
                            {
                                EventManager.Publish(new ModifyCourageEvent { Delta = 1 });
                            }
                            else if (c == "white" || c == "w")
                            {
                                EventManager.Publish(new ModifyTemperanceEvent { Delta = 1 });
                            }
                        }
                    }
                    catch { }
                    EntityManager.RemoveComponent<CardToDiscardFlight>(entity);
                    EntityManager.RemoveComponent<AssignedBlockCard>(entity);
                    // Remove lingering hotkey from equipment returning to panel
                    var hk = entity.GetComponent<HotKey>();
                    if (hk != null) { EntityManager.RemoveComponent<HotKey>(entity); }
                    var uiE = entity.GetComponent<UIElement>();
                    if (uiE != null) { uiE.IsHovered = false; uiE.IsInteractable = true; uiE.Tooltip = string.Empty; uiE.EventType = UIElementEventType.None; }
                }
                else
                {
                    var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
                    // If the card exhausts on block, route to Exhaust instead of Discard and remove the marker
                    var exhaustOnBlock = entity.GetComponent<ExhaustOnBlock>();
                    var destination = (exhaustOnBlock != null) ? CardZoneType.ExhaustPile : CardZoneType.DiscardPile;
                    if (exhaustOnBlock != null)
                    {
                        EntityManager.RemoveComponent<ExhaustOnBlock>(entity);
                    }
                    BlockCardResolveService.Resolve(entity);
                    EventManager.Publish(new CardMoveRequested
                    {
                        Card = entity,
                        Deck = deckEntity,
                        Destination = destination,
                        ContextId = flight.ContextId,
                        Reason = destination == CardZoneType.ExhaustPile ? "AssignedBlockToExhaust" : "AssignedBlockToDiscard"
                    });
                    // Remove animation component
                    EntityManager.RemoveComponent<CardToDiscardFlight>(entity);
                    // Clear AssignedBlockCard if still present
                    EntityManager.RemoveComponent<AssignedBlockCard>(entity);
                    // Clear any tooltip/hovers applied while assigned as block
                    var ui = entity.GetComponent<UIElement>();
                    if (ui != null)
                    {
                        ui.Tooltip = string.Empty;
                        ui.IsHovered = false;
                    }
                }
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
            var discardTarget = new Vector2(margin + panelW * 0.5f, h - margin - panelH * 0.5f);

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

                // For equipment, fly back to its saved return target (left panel), else fly to discard
                Vector2 targetPos = abc.IsEquipment && abc.ReturnTargetPos != Vector2.Zero ? abc.ReturnTargetPos : discardTarget;
                var flight = new CardToDiscardFlight
                {
                    StartPos = abc.CurrentPos,
                    TargetPos = targetPos,
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


