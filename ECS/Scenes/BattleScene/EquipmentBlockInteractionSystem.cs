using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Handles clicking equipped equipment during Block or Action sub-phases.
	/// In Block: assigns as block. In Action: activates if ability exists.
	/// </summary>
	public class EquipmentBlockInteractionSystem : Core.System
	{
		public EquipmentBlockInteractionSystem(EntityManager entityManager) : base(entityManager) { }

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<EquippedEquipment>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		private MouseState _prev;

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);
			// Ensure all AssignedBlockCard entities have a UIElement so clicks can be detected elsewhere
			var assigned = EntityManager.GetEntitiesWithComponent<AssignedBlockCard>();
			foreach (var e in assigned)
			{
				var uiAbc = e.GetComponent<UIElement>();
				if (uiAbc == null)
				{
					uiAbc = new UIElement { IsInteractable = true };
					EntityManager.AddComponent(e, uiAbc);
				}
				// Best-effort bounds sync for assigned cards (precise sync happens in AssignedBlockCardsDisplaySystem)
				var abc = e.GetComponent<AssignedBlockCard>();
				if (abc != null)
				{
					const int defaultCardDrawWidth = 80;
					const int defaultCardDrawHeight = 110;
					int cw = (int)(defaultCardDrawWidth * abc.CurrentScale);
					int ch = (int)(defaultCardDrawHeight * abc.CurrentScale);
					var rectNow = new Microsoft.Xna.Framework.Rectangle((int)(abc.CurrentPos.X - cw / 2f), (int)(abc.CurrentPos.Y - ch / 2f), System.Math.Max(1, cw), System.Math.Max(1, ch));
					uiAbc.Bounds = rectNow;
					// Ensure clicks on assigned equipment route to unassign handler via delegate path
					uiAbc.EventType = UIElementEventType.UnassignCardAsBlock;
					uiAbc.IsInteractable = true;
				}
			}
			// Only in Block or Action phase
			var phase = EntityManager.GetEntitiesWithComponent<PhaseState>().FirstOrDefault()?.GetComponent<PhaseState>();
			if (phase == null) return;
			bool isBlockPhase = phase.Sub == SubPhase.Block;
			bool isActionPhase = phase.Sub == SubPhase.Action;
			if (!isBlockPhase && !isActionPhase) return;
			// Need current context during Block phase
			var enemy = EntityManager.GetEntitiesWithComponent<AttackIntent>().FirstOrDefault();
			var ctx = enemy?.GetComponent<AttackIntent>()?.Planned?.FirstOrDefault()?.ContextId;
			// Clicks now come from UIElement.IsClicked on equipment UI elements

			// Iterate equipment visible in panel (Default zone), topmost first (Z desc just in case)
			var equipEntities = EntityManager.GetEntitiesWithComponent<EquippedEquipment>()
				.Where(e => (e.GetComponent<EquipmentZone>()?.Zone ?? EquipmentZoneType.Default) == EquipmentZoneType.Default)
				.OrderByDescending(e => e.GetComponent<Transform>()?.ZOrder ?? 0)
				.ToList();
			foreach (var eqEntity in equipEntities)
			{
				var ui = eqEntity.GetComponent<UIElement>();
				var comp = eqEntity.GetComponent<EquippedEquipment>();
				if (ui == null || comp == null) continue;
				if (!ui.IsInteractable || !ui.IsClicked) continue;
				// Prevent use if destroyed
				var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
				var usedState = player?.GetComponent<EquipmentUsedState>();
				if (usedState != null && usedState.DestroyedEquipmentIds.Contains(comp.EquipmentId)) { return; }
				// Prevent block use if out of uses during Block phase
				if (isBlockPhase)
				{
					int total = 0; int used = 0;
					try
					{
						if (Crusaders30XX.ECS.Data.Equipment.EquipmentDefinitionCache.TryGet(comp.EquipmentId, out var defChk) && defChk != null) total = System.Math.Max(0, defChk.blockUses);
						if (usedState != null && usedState.UsesByEquipmentId.TryGetValue(comp.EquipmentId, out var u)) used = u;
					}
					catch { }
					if (total > 0 && used >= total) { return; }
				}

				if (isBlockPhase)
				{
					if (string.IsNullOrEmpty(ctx)) { return; }
					// Existing assign as block behavior
					// Lookup block value and color from definition
					int blockVal = 0;
					string color = "White";
					try
					{
						if (Crusaders30XX.ECS.Data.Equipment.EquipmentDefinitionCache.TryGet(comp.EquipmentId, out var def) && def != null)
						{
							blockVal = System.Math.Max(0, def.block);
							color = (def.color ?? "White");
						}
					}
					catch { }
					if (blockVal <= 0) { return; }
					
					var zone = eqEntity.GetComponent<EquipmentZone>();
					if (zone == null)
					{
						zone = new EquipmentZone { Zone = EquipmentZoneType.AssignedBlock };
						EntityManager.AddComponent(eqEntity, zone);
					}
					else
					{
						zone.Zone = EquipmentZoneType.AssignedBlock;
					}
					// Create AssignedBlockCard animation state
					var t = eqEntity.GetComponent<Transform>();
					var abc = eqEntity.GetComponent<AssignedBlockCard>();
					if (abc == null)
					{
						var equipZone = eqEntity.GetComponent<EquipmentZone>();
						var uiElem = eqEntity.GetComponent<UIElement>();
						var uiCenter = uiElem != null ? new Vector2(uiElem.Bounds.X + uiElem.Bounds.Width * 0.5f, uiElem.Bounds.Y + uiElem.Bounds.Height * 0.5f) : (t?.Position ?? Vector2.Zero);
						var returnPos = (equipZone != null && equipZone.LastPanelCenter != Vector2.Zero) ? equipZone.LastPanelCenter : uiCenter;
						abc = new AssignedBlockCard { ContextId = ctx, BlockAmount = blockVal, AssignedAtTicks = System.DateTime.UtcNow.Ticks, StartPos = t?.Position ?? Vector2.Zero, CurrentPos = t?.Position ?? Vector2.Zero, TargetPos = t?.Position ?? Vector2.Zero, StartScale = t?.Scale.X ?? 1f, TargetScale = 0.35f, Phase = AssignedBlockCard.PhaseState.Pullback, Elapsed = 0f, IsEquipment = true, ColorKey = NormalizeColorKey(color), Tooltip = BuildEquipmentTooltip(comp), DisplayBgColor = ResolveEquipmentBgColor(color), DisplayFgColor = ResolveFgForBg(ResolveEquipmentBgColor(color)), ReturnTargetPos = returnPos, EquipmentType = comp.EquipmentType };
						EntityManager.AddComponent(eqEntity, abc);
					}
					else
					{
						var equipZone = eqEntity.GetComponent<EquipmentZone>();
						var uiElem = eqEntity.GetComponent<UIElement>();
						var uiCenter = uiElem != null ? new Vector2(uiElem.Bounds.X + uiElem.Bounds.Width * 0.5f, uiElem.Bounds.Y + uiElem.Bounds.Height * 0.5f) : (t?.Position ?? Vector2.Zero);
						var returnPos = (equipZone != null && equipZone.LastPanelCenter != Vector2.Zero) ? equipZone.LastPanelCenter : uiCenter;
						abc.ContextId = ctx; abc.BlockAmount = blockVal; abc.AssignedAtTicks = System.DateTime.UtcNow.Ticks; abc.Phase = AssignedBlockCard.PhaseState.Pullback; abc.Elapsed = 0f; abc.IsEquipment = true; abc.ColorKey = NormalizeColorKey(color); abc.Tooltip = BuildEquipmentTooltip(comp); abc.DisplayBgColor = ResolveEquipmentBgColor(color); abc.DisplayFgColor = ResolveFgForBg(abc.DisplayBgColor); abc.ReturnTargetPos = returnPos; abc.EquipmentType = comp.EquipmentType;
					}
					EventManager.Publish(new BlockAssignmentAdded { ContextId = ctx, Card = eqEntity, DeltaBlock = blockVal, Color = color });
					// Mark the assigned equipment to unassign via delegate when clicked on its assigned tile
					{
						var uiAssigned = eqEntity.GetComponent<UIElement>();
						if (uiAssigned == null)
						{
							uiAssigned = new UIElement { IsInteractable = true };
							EntityManager.AddComponent(eqEntity, uiAssigned);
						}
						uiAssigned.EventType = UIElementEventType.UnassignCardAsBlock;
						uiAssigned.IsInteractable = true;
						// Ensure no lingering hotkey when initially assigning equipment (will be given to newest idle later)
						var hk = eqEntity.GetComponent<HotKey>();
						if (hk != null) { EntityManager.RemoveComponent<HotKey>(eqEntity); }
					}
					break;
				}

				if (isActionPhase)
				{
					try
					{
						if (Crusaders30XX.ECS.Data.Equipment.EquipmentDefinitionCache.TryGet(comp.EquipmentId, out var def) && def != null && def.abilities != null)
						{
							var act = def.abilities.FirstOrDefault(a => a.type == "Activate");
							if (act != null)
							{
								// prevent re-activation in the same turn
							if (usedState != null && usedState.ActivatedThisTurn.Contains(comp.EquipmentId)) { return; }
								if (!act.isFreeAction) { EventManager.Publish(new ModifyActionPointsEvent { Delta = -1 }); }
								EquipmentAbilityService.ActivateByEquipmentId(EntityManager, comp.EquipmentId);
								EventManager.Publish(new EquipmentActivated { EquipmentId = comp.EquipmentId });
							}
						}
					}
					catch { }
					break;
				}
			}

			// No raw mouse state tracking needed anymore
		}
		private static string NormalizeColorKey(string c)
		{
			if (string.IsNullOrWhiteSpace(c)) return "White";
			switch (c.Trim().ToLowerInvariant())
			{
				case "r": case "red": return "Red";
				case "w": case "white": return "White";
				case "b": case "black": return "Black";
				default: return char.ToUpperInvariant(c[0]) + c.Substring(1);
			}
		}

		private static Microsoft.Xna.Framework.Color ResolveEquipmentBgColor(string c)
		{
			switch ((c ?? "").Trim().ToLowerInvariant())
			{
				case "red": return Microsoft.Xna.Framework.Color.DarkRed;
				case "black": return Microsoft.Xna.Framework.Color.Black;
				case "white": return Microsoft.Xna.Framework.Color.White;
				default: return Microsoft.Xna.Framework.Color.Gray;
			}
		}

		private static Microsoft.Xna.Framework.Color ResolveFgForBg(Microsoft.Xna.Framework.Color bg)
		{
			return (bg == Microsoft.Xna.Framework.Color.White) ? Microsoft.Xna.Framework.Color.Black : Microsoft.Xna.Framework.Color.White;
		}

		private static string BuildEquipmentTooltip(EquippedEquipment item)
		{
			try
			{
				if (Crusaders30XX.ECS.Data.Equipment.EquipmentDefinitionCache.TryGet(item.EquipmentId, out var def) && def != null)
				{
					string name = string.IsNullOrWhiteSpace(def.name) ? item.EquipmentId : def.name;
					return name;
				}
			}
			catch { }
			return item.EquipmentId ?? string.Empty;
		}
	}
}


