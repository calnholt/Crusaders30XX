using System;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Data.Locations;
using Crusaders30XX.ECS.Data.Save;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// System for handling input and UI interactions
    /// </summary>
    public class InputSystem : Core.System
    {
        private MouseState _previousMouseState;
        private KeyboardState _previousKeyboardState;
        private CursorStateEvent _cursorEvent;

        public InputSystem(EntityManager entityManager) : base(entityManager)
        {
            _previousMouseState = Mouse.GetState();
            _previousKeyboardState = Keyboard.GetState();
            EventManager.Subscribe<CursorStateEvent>(OnCursorEvent);
            EventManager.Subscribe<QuestSelectRequested>(OnQuestSelectRequested);
        }
        
        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<UIElement>();
        }
        
        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
            // Defer hover resolution to a single pass so only the top-most UI under mouse is hovered
        }

        public override void Update(GameTime gameTime)
        {
            if (!IsActive) return;

            // If the game window is not active, ignore inputs and keep previous states in sync
            if (!Game1.WindowIsActive)
            {
                _previousMouseState = Mouse.GetState();
                _previousKeyboardState = Keyboard.GetState();
                return;
            }

            var mouseState = Mouse.GetState();
            var mousePosition = mouseState.Position;
            // Coalesce pointer position: prefer controller cursor position if present
            bool hasCursor = _cursorEvent != null;
            var pointerVec = hasCursor ? _cursorEvent.Position : new Vector2(mousePosition.X, mousePosition.Y);
            var pointerPoint = new Point((int)Math.Round(pointerVec.X), (int)Math.Round(pointerVec.Y));
            var keyboardState = Keyboard.GetState();

            // Debug menu toggle is handled globally in Game1 to be scene-independent

            // Toggle profiler overlay on P key press (edge-triggered)
            if (keyboardState.IsKeyDown(Keys.P) && !_previousKeyboardState.IsKeyDown(Keys.P))
            {
                ToggleProfilerOverlay();
            }

            // Collect all interactable UI elements
            var uiEntities = GetRelevantEntities()
                .Select(e => new { E = e, UI = e.GetComponent<UIElement>(), T = e.GetComponent<Transform>(), IsCard = e.GetComponent<CardData>() != null })
                .Where(x => x.UI != null && x.UI.IsInteractable)
                .ToList();


            // If a modal is open, restrict interactions to modal UI only
            var modalEntity = EntityManager.GetEntitiesWithComponent<CardListModal>().FirstOrDefault();
            bool isModalOpen = false;
            if (modalEntity != null)
            {
                var modal = modalEntity.GetComponent<CardListModal>();
                isModalOpen = modal != null && modal.IsOpen;
            }
            if (isModalOpen)
            {
                uiEntities = uiEntities
                    .Where(x => x.E.GetComponent<CardListModalClose>() != null || x.IsCard)
                    .ToList();
            }

            // If pay-cost overlay is open, restrict interactions to: cancel button and hand cards
            var payEntity = EntityManager.GetEntitiesWithComponent<PayCostOverlayState>().FirstOrDefault();
            bool isPayOpen = false;
            if (payEntity != null)
            {
                var st = payEntity.GetComponent<PayCostOverlayState>();
                isPayOpen = st != null && st.IsOpen;
            }
            if (isPayOpen)
            {
                // While pay-cost overlay is open, allow cancel button, HAND cards, and selected-for-payment cards
                var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
                var deck = deckEntity?.GetComponent<Deck>();
                uiEntities = uiEntities
                    .Where(x => x.E.GetComponent<PayCostCancelButton>() != null
                             || (x.IsCard && ((deck != null && deck.Hand.Contains(x.E)) || x.E.GetComponent<SelectedForPayment>() != null)))
                    .ToList();
            }

            // During Action phase (normal play), only hand cards should be clickable among card UIs
            var phaseEntity = EntityManager.GetEntitiesWithComponent<PhaseState>().FirstOrDefault();
            var phaseState = phaseEntity?.GetComponent<PhaseState>();
            if (!isPayOpen && phaseState != null && phaseState.Sub == SubPhase.Action)
            {
                var deckEntity2 = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
                var deck2 = deckEntity2?.GetComponent<Deck>();
                if (deck2 != null)
                {
                    uiEntities = uiEntities
                        .Where(x => !x.IsCard || deck2.Hand.Contains(x.E))
                        .ToList();
                }
            }

            // Reset hover flags
            foreach (var x in uiEntities)
            {
                x.UI.IsHovered = false;
            }

            // Find top-most under mouse by ZOrder
            // Use UI.Bounds for non-card UI, rotated-hit for cards; ignore very small bounds
            var underMouse = uiEntities
                .Where(x =>
                {
                    // Reject degenerate bounds
                    if (x.UI.Bounds.Width < 2 || x.UI.Bounds.Height < 2) return false;
                    return IsUnderMouse(x, pointerPoint);
                })
                .OrderByDescending(x => x.T?.ZOrder ?? 0)
                .ToList();
            var top = underMouse.FirstOrDefault();

            if (top != null && !StateSingleton.PreventClicking)
            {
                top.UI.IsHovered = true;

                // Handle click on the top-most only
                bool mouseEdge = mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released;
                // Ignore cursor events sourced from mouse to avoid double-clicking (since we handle mouseEdge locally)
                bool controllerEdge = _cursorEvent != null && _cursorEvent.IsAPressedEdge && _cursorEvent.Source != InputMethod.Mouse;
                if (controllerEdge)
                {
                    // Prefer top entity from controller event if available, otherwise use top under coalesced pointer
                    var target = _cursorEvent.TopEntity ?? top?.E;
                    var ui = target?.GetComponent<UIElement>();
                    if (ui != null)
                    {
                        ui.IsClicked = true;
                        HandleUIClick(target);
                    }
                }
                else if (mouseEdge)
                {
                    top.UI.IsClicked = true;
                    HandleUIClick(top.E);
                }
                else
                {
                    top.UI.IsClicked = false;
                }
            }

            // Optionally debug hover changes per card (limited to top)
            var cardData = top?.E.GetComponent<CardData>();
            if (cardData != null)
            {
                // You can log if needed
            }

            _previousMouseState = mouseState;
            _previousKeyboardState = keyboardState;
            // Clear cursor event after consumption to avoid reuse next frame
            _cursorEvent = null;
        }

        private bool IsUnderMouse(dynamic x, Point mousePosition)
        {
			if (!x.IsCard)
			{
				// Fallback to AABB for non-card UI
				return x.UI.Bounds.Contains(mousePosition);
			}

			// Rotated-rect hit test for cards using UI bounds (already scaled/positioned)
			var transform = x.T as Transform;
			var ui = x.UI as UIElement;
			if (ui == null) return false;

			var r = ui.Bounds;
			if (r.Width < 2 || r.Height < 2) return false;

			Vector2 center = new Vector2(r.X + r.Width / 2f, r.Y + r.Height / 2f);
			float rotation = transform?.Rotation ?? 0f;
			float cos = (float)Math.Cos(rotation);
			float sin = (float)Math.Sin(rotation);

			Vector2 m = new Vector2(mousePosition.X, mousePosition.Y);
			Vector2 d = m - center;
			// rotate mouse into card local space (inverse rotation)
			float localX = d.X * cos + d.Y * sin;
			float localY = -d.X * sin + d.Y * cos;

			float halfW = r.Width / 2f;
			float halfH = r.Height / 2f;
			return (localX >= -halfW && localX <= halfW && localY >= -halfH && localY <= halfH);
        }
        
        private void HandleUIClick(Entity entity)
        {
            // Handle different types of UI clicks
            var cardData = entity.GetComponent<CardData>();
            if (cardData != null)
            {
                // Handle card click
                // If pay-cost overlay is open, treat card click as candidate for payment instead of play
                var payStateEntity = EntityManager.GetEntitiesWithComponent<PayCostOverlayState>().FirstOrDefault();
                var payState = payStateEntity?.GetComponent<PayCostOverlayState>();
                if (payState != null && payState.IsOpen)
                {
                    EventManager.Publish(new PayCostCandidateClicked { Card = entity });
                }
                else
                {
                    HandleCardClick(entity);
                }
            }

            // // Location scene: clicking a Point Of Interest should initiate the quest
            // var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
            // if (scene != null && scene.Current == SceneId.Location)
            // {
            //     var poi = entity.GetComponent<PointOfInterest>();
            //     if (poi != null)
            //     {
            //         TryStartQuestFromPoi(poi);
            //     }
            // }

            var button = entity.GetComponent<UIButton>();
            if (button != null)
            {
                // Publish debug command event based on button command
                EventManager.Publish(new DebugCommandEvent { Command = button.Command });
            }

            var drawPileClickable = entity.GetComponent<DrawPileClickable>();
            if (drawPileClickable != null)
            {
                // Open generic modal with draw pile content
                var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
                var deck = deckEntity?.GetComponent<Deck>();
                if (deck != null)
                {
                    EventManager.Publish(new OpenCardListModalEvent { Title = "Draw Pile", Cards = deck.DrawPile.ToList() });
                }
            }

            var discardPileClickable = entity.GetComponent<DiscardPileClickable>();
            if (discardPileClickable != null)
            {
                // Open generic modal with discard pile content
                var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
                var deck = deckEntity?.GetComponent<Deck>();
                if (deck != null)
                {
                    EventManager.Publish(new OpenCardListModalEvent { Title = "Discard Pile", Cards = deck.DiscardPile.ToList() });
                }
            }
        }
        
        private void HandleCardClick(Entity entity)
        {
            var cardData = entity.GetComponent<CardData>();
            var phase = EntityManager.GetEntitiesWithComponent<PhaseState>().FirstOrDefault()?.GetComponent<PhaseState>();
            Console.WriteLine($"[InputSystem] Card clicked id={cardData?.CardId} phase={phase?.Sub.ToString() ?? "None"}");
            if (phase == null) return;
            if (phase.Sub == SubPhase.Action)
            {
                var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
                var deck = deckEntity?.GetComponent<Deck>();
                if (deck == null)
                {
                    Console.WriteLine("[InputSystem] No deck found; ignoring card click");
                    return;
                }
                if (!deck.Hand.Contains(entity))
                {
                    Console.WriteLine("[InputSystem] Clicked card is not in hand; ignoring");
                    return;
                }
                EventManager.Publish(new PlayCardRequested { Card = entity });
            }
        }

        private void OnQuestSelectRequested(QuestSelectRequested e)
        {
            var poi = e.Entity.GetComponent<PointOfInterest>();
            if (poi != null)
            {
                TryStartQuestFromPoi(poi);
            }
        }
        private void TryStartQuestFromPoi(PointOfInterest poi)
        {
            if (poi == null || string.IsNullOrEmpty(poi.Id)) return;

            // Find location and quest index containing this POI
            string locationId = null;
            int questIndex = -1;
            var all = LocationDefinitionCache.GetAll();
            foreach (var kv in all)
            {
                var loc = kv.Value;
                if (loc?.pointsOfInterest == null) continue;
                for (int i = 0; i < loc.pointsOfInterest.Count; i++)
                {
                    if (string.Equals(loc.pointsOfInterest[i].id, poi.Id, System.StringComparison.OrdinalIgnoreCase))
                    {
                        locationId = kv.Key;
                        questIndex = i;
                        break;
                    }
                }
                if (questIndex >= 0) break;
            }
            if (string.IsNullOrEmpty(locationId) || questIndex < 0) return;

            // Build queued events from this quest
            var qeEntity = EntityManager.GetEntitiesWithComponent<QueuedEvents>().FirstOrDefault();
            if (qeEntity == null)
            {
                qeEntity = EntityManager.CreateEntity("QueuedEvents");
                EntityManager.AddComponent(qeEntity, new QueuedEvents());
                EntityManager.AddComponent(qeEntity, new DontDestroyOnLoad());
            }
            var qe = qeEntity.GetComponent<QueuedEvents>();
            qe.CurrentIndex = -1;
            qe.Events.Clear();
            qe.LocationId = locationId;
            qe.QuestIndex = questIndex;

            if (LocationDefinitionCache.TryGet(locationId, out var def) && def != null)
            {
                if (questIndex >= 0 && questIndex < (def.pointsOfInterest?.Count ?? 0))
                {
                    var questDefs = def.pointsOfInterest[questIndex];
                    foreach (var q in questDefs.events)
                    {
                        var type = ParseQueuedEventType(q?.type);
                        if (!string.IsNullOrEmpty(q?.id))
                        {
                            var queuedEvent = new QueuedEvent { EventId = q.id, EventType = type };
                            if (q.modifications != null)
                            {
                                queuedEvent.Modifications = new List<EnemyModification>(q.modifications);
                            }
                            qe.Events.Add(queuedEvent);
                        }
                    }
                }
            }

            if (qe.Events.Count > 0)
            {
                // Announce selection and transition to Battle
                SaveCache.SetLastLocation(poi.Id);
                EventManager.Publish(new QuestSelected { LocationId = locationId, QuestIndex = questIndex });
                EventManager.Publish(new ShowTransition { Scene = SceneId.Battle });
            }
        }

        private static QueuedEventType ParseQueuedEventType(string type)
        {
            if (string.IsNullOrEmpty(type)) return QueuedEventType.Enemy;
            switch (type.ToLowerInvariant())
            {
                case "enemy": return QueuedEventType.Enemy;
                case "event": return QueuedEventType.Event;
                case "shop": return QueuedEventType.Shop;
                case "church": return QueuedEventType.Church;
                default: return QueuedEventType.Enemy;
            }
        }
        
        public void UpdateInput()
        {
            _previousMouseState = Mouse.GetState();
            _previousKeyboardState = Keyboard.GetState();
        }

        // Debug menu toggle removed from here; see Game1.ToggleDebugMenu()

        private void ToggleProfilerOverlay()
        {
            var e = EntityManager.GetEntitiesWithComponent<ProfilerOverlay>().FirstOrDefault();
            if (e == null)
            {
                e = EntityManager.CreateEntity("ProfilerOverlay");
                EntityManager.AddComponent(e, new ProfilerOverlay { IsOpen = true });
            }
            else
            {
                var p = e.GetComponent<ProfilerOverlay>();
                p.IsOpen = !p.IsOpen;
            }
        }
        private void OnCursorEvent(CursorStateEvent e)
        {
            _cursorEvent = e;
        }
    }
} 