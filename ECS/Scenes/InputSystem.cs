using System;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

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

            if (!Game1.WindowIsActive)
            {
                _previousMouseState = Mouse.GetState();
                _previousKeyboardState = Keyboard.GetState();
                return;
            }

            var mouseState = Mouse.GetState();
            var mousePosition = mouseState.Position;
            bool hasCursor = _cursorEvent != null;
            Vector2 pointerVec;
            if (hasCursor)
            {
                pointerVec = _cursorEvent.Position;
            }
            else
            {
                var dest = Game1.RenderDestination;
                float scaleX = (float)dest.Width / Game1.VirtualWidth;
                float scaleY = (float)dest.Height / Game1.VirtualHeight;
                if (scaleX <= 0.001f) scaleX = 1f;
                if (scaleY <= 0.001f) scaleY = 1f;
                float virtX = (mousePosition.X - dest.X) / scaleX;
                float virtY = (mousePosition.Y - dest.Y) / scaleY;
                pointerVec = new Vector2(virtX, virtY);
            }
            var pointerPoint = new Point((int)Math.Round(pointerVec.X), (int)Math.Round(pointerVec.Y));
            var keyboardState = Keyboard.GetState();

            // Collect all UI elements, reset hover/click on all so suppressed entities never keep stale state
            var allUiEntities = GetRelevantEntities()
                .Select(e => new { E = e, UI = e.GetComponent<UIElement>(), T = e.GetComponent<Transform>(), IsCard = e.GetComponent<CardData>() != null })
                .Where(x => x.UI != null)
                .ToList();

            foreach (var x in allUiEntities)
            {
                x.UI.IsHovered = false;
                x.UI.IsClicked = false;
            }

            // Restrict hover/click detection to interactable entities only
            var uiEntities = allUiEntities.Where(x => x.UI.IsInteractable).ToList();

            // Determine the top entity under cursor
            dynamic top = null;
            if (_cursorEvent != null && _cursorEvent.TopEntity != null)
            {
                var topEntity = _cursorEvent.TopEntity;
                var topUI = topEntity.GetComponent<UIElement>();
                var topT = topEntity.GetComponent<Transform>();
                var topIsCard = topEntity.GetComponent<CardData>() != null;
                if (uiEntities.Any(x => x.E == topEntity))
                {
                    top = new { E = topEntity, UI = topUI, T = topT, IsCard = topIsCard };
                }
            }
            else
            {
                var underMouse = uiEntities
                    .Where(x =>
                    {
                        Rectangle bounds = TransformResolverService.ResolveUIBounds(EntityManager, x.E, x.UI);
                        if (bounds.Width < 2 || bounds.Height < 2) return false;
                        return IsUnderMouse(x, pointerPoint);
                    })
                    .OrderByDescending(x => x.T?.ZOrder ?? 0)
                    .ToList();
                top = underMouse.FirstOrDefault();
            }

            // Detect click intent before guards so dropped clicks can be diagnosed
            bool mouseEdge = mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released;
            bool controllerEdge = _cursorEvent != null && _cursorEvent.IsAPressedEdge && _cursorEvent.Source != InputMethod.Mouse;
            bool isPrimaryClickAttempt = mouseEdge || controllerEdge;
            bool isSecondaryClickAttempt = _cursorEvent?.IsSecondaryPressedEdge == true;
            bool isClickAttempt = isPrimaryClickAttempt || isSecondaryClickAttempt;

            if (isClickAttempt && top == null)
            {
                var diagLog = new JsonObject
                {
                    ["source"] = mouseEdge ? "mouse" : "controller",
                    ["preventClicking"] = StateSingleton.PreventClicking,
                    ["isTutorialActive"] = StateSingleton.IsTutorialActive,
                };
                if (_cursorEvent?.TopEntity != null)
                {
                    var cursorTop = _cursorEvent.TopEntity;
                    var cursorTopUI = cursorTop.GetComponent<UIElement>();
                    diagLog["cursorTopEntityId"] = cursorTop.Id;
                    diagLog["cursorTopEntityName"] = cursorTop.Name;
                    diagLog["cursorTopInUiEntities"] = uiEntities.Any(x => x.E == cursorTop);
                    if (cursorTopUI != null)
                    {
                        Rectangle bounds = TransformResolverService.ResolveUIBounds(EntityManager, cursorTop, cursorTopUI);
                        diagLog["isInteractable"] = cursorTopUI.IsInteractable;
                        diagLog["isHidden"] = cursorTopUI.IsHidden;
                        diagLog["eventType"] = cursorTopUI.EventType.ToString();
                        diagLog["suppressCount"] = cursorTopUI.SuppressCount;
                        diagLog["bounds"] = $"x:{bounds.X} y:{bounds.Y} w:{bounds.Width} h:{bounds.Height}";
                    }
                    else
                    {
                        diagLog["hasUIElement"] = false;
                    }
                }
                else
                {
                    diagLog["cursorTopEntity"] = "null";
                }
                LoggingService.Append("InputSystem_ClickDropped", diagLog);
            }
            else if (isClickAttempt && top != null && (StateSingleton.PreventClicking || StateSingleton.IsTutorialActive))
            {
                LoggingService.Append("InputSystem_ClickBlocked", new JsonObject
                {
                    ["entityId"] = ((Entity)top.E).Id,
                    ["entityName"] = ((Entity)top.E).Name,
                    ["preventClicking"] = StateSingleton.PreventClicking,
                    ["isTutorialActive"] = StateSingleton.IsTutorialActive
                });
            }

            if (top != null && !StateSingleton.PreventClicking && !StateSingleton.IsTutorialActive)
            {
                top.UI.IsHovered = true;

                if (isClickAttempt)
                {
                    var uiElement = ((Entity)top.E).GetComponent<UIElement>();
                    var eventType = isPrimaryClickAttempt
                        ? uiElement?.EventType ?? UIElementEventType.None
                        : uiElement?.SecondaryEventType ?? UIElementEventType.None;
                    if (isPrimaryClickAttempt)
                    {
                        top.UI.IsClicked = true;
                    }
                    LoggingService.Append("InputSystem_Click", new JsonObject
                    {
                        ["entityId"] = ((Entity)top.E).Id,
                        ["button"] = isPrimaryClickAttempt ? "primary" : "secondary",
                        ["eventType"] = eventType.ToString(),
                        ["isInteractable"] = uiElement?.IsInteractable ?? false,
                        ["suppressCount"] = uiElement?.SuppressCount ?? 0,
                    });
                    if (uiElement != null && eventType != UIElementEventType.None)
                    {
                        UIElementEventDelegateService.HandleEvent(eventType, top.E, EntityManager);
                    }
                    else
                    {
                        LoggingService.Append("InputSystem_ClickNoEvent", new JsonObject
                        {
                            ["entityId"] = ((Entity)top.E).Id,
                            ["reason"] = uiElement == null ? "NoUIElement" : "EventTypeNone",
                        });
                    }
                }
            }

            _previousMouseState = mouseState;
            _previousKeyboardState = keyboardState;
            _cursorEvent = null;
        }

        private bool IsUnderMouse(dynamic x, Point mousePosition)
        {
			var entity = x.E as Entity;
			var ui = x.UI as UIElement;
			if (entity == null || ui == null) return false;
			Rectangle resolvedBounds = TransformResolverService.ResolveUIBounds(EntityManager, entity, ui);

			if (!x.IsCard)
			{
				// Fallback to AABB for non-card UI
				return resolvedBounds.Contains(mousePosition);
			}

			// Rotated-rect hit test for cards using UI bounds (already scaled/positioned)
			var transform = x.T as Transform;
			var r = resolvedBounds;
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
        
        public void UpdateInput()
        {
            _previousMouseState = Mouse.GetState();
            _previousKeyboardState = Keyboard.GetState();
        }

        private void OnCursorEvent(CursorStateEvent e)
        {
            _cursorEvent = e;
        }
    }
}
