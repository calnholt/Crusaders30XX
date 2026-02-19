using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Singletons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("CV2 Interaction")]
	public class WheelInteractionSystem : Core.System
	{
		private CursorStateEvent _cursorEvent;
		private KeyboardState _prevKeyboard;
		private GamePadState _prevGamePadState;

		public WheelInteractionSystem(EntityManager em) : base(em)
		{
			EventManager.Subscribe<CursorStateEvent>(e => _cursorEvent = e);
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<SceneState>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			var scene = entity.GetComponent<SceneState>();
			if (scene == null || scene.Current != SceneId.CustomizationV2) return;

			var nav = EntityManager.GetEntitiesWithComponent<CustomizationV2NavigationState>().FirstOrDefault()?.GetComponent<CustomizationV2NavigationState>();
			if (nav == null || nav.ActiveTab != CustomizationV2TabType.Loadout) return;
			if (StateSingleton.IsActive) return;

			var loadout = EntityManager.GetEntitiesWithComponent<CustomizationV2LoadoutState>().FirstOrDefault()?.GetComponent<CustomizationV2LoadoutState>();
			if (loadout == null) return;

			var kb = Keyboard.GetState();

			// Hover-to-select segment
			{
				bool anySegmentHovered = false;
				var segments = EntityManager.GetEntitiesWithComponent<WheelSegment>();
				foreach (var seg in segments)
				{
					var ui = seg.GetComponent<UIElement>();
					var ws = seg.GetComponent<WheelSegment>();
					if (ui != null && ws != null && ui.IsHovered)
					{
						anySegmentHovered = true;
						if (loadout.HoveredSegmentIndex != ws.SegmentIndex)
						{
							loadout.HoveredSegmentIndex = ws.SegmentIndex;
							EventManager.Publish(new WheelSegmentSelected { SegmentIndex = ws.SegmentIndex, SlotType = ws.SlotType });
						}
						break;
					}
				}

				// If no segment hovered and cursor isn't over the center hub, deselect
				if (!anySegmentHovered)
				{
					var hub = EntityManager.GetEntitiesWithComponent<CenterHub>().FirstOrDefault();
					var hubUi = hub?.GetComponent<UIElement>();
					bool overHub = hubUi != null && hubUi.IsHovered;
					if (!overHub && loadout.HoveredSegmentIndex >= 0)
					{
						loadout.HoveredSegmentIndex = -1;
					}
				}
			}

			// Click on hovered segment or center hub = equip browsed item
			if (_cursorEvent != null && _cursorEvent.IsAPressedEdge && loadout.HoveredSegmentIndex >= 0)
			{
				var segments = EntityManager.GetEntitiesWithComponent<WheelSegment>();
				foreach (var seg in segments)
				{
					var ui = seg.GetComponent<UIElement>();
					var ws = seg.GetComponent<WheelSegment>();
					if (ui != null && ws != null && ui.IsHovered)
					{
						EventManager.Publish(new EquipBrowsedItemRequested { SlotType = ws.SlotType });
						break;
					}
				}

				var hub = EntityManager.GetEntitiesWithComponent<CenterHub>().FirstOrDefault();
				var hubUi = hub?.GetComponent<UIElement>();
				if (hubUi != null && hubUi.IsHovered)
				{
					var slot = WheelLayoutSystem.GetSlotType(loadout.HoveredSegmentIndex);
					EventManager.Publish(new EquipBrowsedItemRequested { SlotType = slot });
				}
			}

			// A/D or Arrow keys for browse
			if (loadout.HoveredSegmentIndex >= 0)
			{
				if ((kb.IsKeyDown(Keys.A) && !_prevKeyboard.IsKeyDown(Keys.A)) ||
					(kb.IsKeyDown(Keys.Left) && !_prevKeyboard.IsKeyDown(Keys.Left)))
				{
					EventManager.Publish(new BrowseItemRequested { Direction = -1 });
				}
				if ((kb.IsKeyDown(Keys.D) && !_prevKeyboard.IsKeyDown(Keys.D)) ||
					(kb.IsKeyDown(Keys.Right) && !_prevKeyboard.IsKeyDown(Keys.Right)))
				{
					EventManager.Publish(new BrowseItemRequested { Direction = 1 });
				}

				// Space to equip
				if (kb.IsKeyDown(Keys.Space) && !_prevKeyboard.IsKeyDown(Keys.Space))
				{
					var slot = WheelLayoutSystem.GetSlotType(loadout.HoveredSegmentIndex);
					EventManager.Publish(new EquipBrowsedItemRequested { SlotType = slot });
				}

				// Escape to deselect
				if (kb.IsKeyDown(Keys.Escape) && !_prevKeyboard.IsKeyDown(Keys.Escape))
				{
					loadout.HoveredSegmentIndex = -1;
				}
			}

			// Gamepad support
			var gp = GamePad.GetState(PlayerIndex.One);
			if (gp.IsConnected)
			{
				// DPad left/right for browse
				bool dpadLeft = gp.DPad.Left == ButtonState.Pressed && _prevGamePadState.DPad.Left == ButtonState.Released;
				bool dpadRight = gp.DPad.Right == ButtonState.Pressed && _prevGamePadState.DPad.Right == ButtonState.Released;

				if (dpadLeft && loadout.HoveredSegmentIndex >= 0)
				{
					EventManager.Publish(new BrowseItemRequested { Direction = -1 });
				}
				if (dpadRight && loadout.HoveredSegmentIndex >= 0)
				{
					EventManager.Publish(new BrowseItemRequested { Direction = 1 });
				}
			}

			_prevGamePadState = gp;
			_prevKeyboard = kb;
		}
	}
}
