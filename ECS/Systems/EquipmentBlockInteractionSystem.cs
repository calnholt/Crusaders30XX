using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

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
			// Only in Block or Action phase
			var phase = EntityManager.GetEntitiesWithComponent<PhaseState>().FirstOrDefault()?.GetComponent<PhaseState>();
			if (phase == null) return;
			bool isBlockPhase = phase.Sub == SubPhase.Block;
			bool isActionPhase = phase.Sub == SubPhase.Action;
			if (!isBlockPhase && !isActionPhase) return;
			// Need current context during Block phase
			var enemy = EntityManager.GetEntitiesWithComponent<AttackIntent>().FirstOrDefault();
			var ctx = enemy?.GetComponent<AttackIntent>()?.Planned?.FirstOrDefault()?.ContextId;
			var mouse = Mouse.GetState();
			bool click = mouse.LeftButton == ButtonState.Pressed && _prev.LeftButton == ButtonState.Released;
			if (!click) { _prev = mouse; return; }

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
				if (!ui.Bounds.Contains(mouse.Position)) continue;
				// Prevent use if destroyed
				var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
				var usedState = player?.GetComponent<EquipmentUsedState>();
				if (usedState != null && usedState.DestroyedEquipmentIds.Contains(comp.EquipmentId)) { _prev = mouse; return; }
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
					if (total > 0 && used >= total) { _prev = mouse; return; }
				}

				if (isBlockPhase)
				{
					if (string.IsNullOrEmpty(ctx)) { _prev = mouse; return; }
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
					if (blockVal <= 0) { _prev = mouse; return; }
					EventManager.Publish(new BlockAssignmentAdded { ContextId = ctx, Card = eqEntity, DeltaBlock = blockVal, Color = color });
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
						abc = new AssignedBlockCard { ContextId = ctx, BlockAmount = blockVal, AssignedAtTicks = System.DateTime.UtcNow.Ticks, StartPos = t?.Position ?? Vector2.Zero, CurrentPos = t?.Position ?? Vector2.Zero, TargetPos = t?.Position ?? Vector2.Zero, StartScale = t?.Scale.X ?? 1f, TargetScale = 0.35f, Phase = AssignedBlockCard.PhaseState.Pullback, Elapsed = 0f, IsEquipment = true, ColorKey = NormalizeColorKey(color), Tooltip = BuildEquipmentTooltip(comp), DisplayBgColor = ResolveEquipmentBgColor(color), DisplayFgColor = ResolveFgForBg(ResolveEquipmentBgColor(color)), ReturnTargetPos = Vector2.Zero, EquipmentType = comp.EquipmentType };
						EntityManager.AddComponent(eqEntity, abc);
					}
					else
					{
						abc.ContextId = ctx; abc.BlockAmount = blockVal; abc.AssignedAtTicks = System.DateTime.UtcNow.Ticks; abc.Phase = AssignedBlockCard.PhaseState.Pullback; abc.Elapsed = 0f; abc.IsEquipment = true; abc.ColorKey = NormalizeColorKey(color); abc.Tooltip = BuildEquipmentTooltip(comp); abc.DisplayBgColor = ResolveEquipmentBgColor(color); abc.DisplayFgColor = ResolveFgForBg(abc.DisplayBgColor); abc.ReturnTargetPos = Vector2.Zero; abc.EquipmentType = comp.EquipmentType;
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
								if (!act.isFreeAction) { EventManager.Publish(new ModifyActionPointsEvent { Delta = -1 }); }
								EquipmentAbilityService.ActivateByEquipmentId(EntityManager, comp.EquipmentId);
							}
						}
					}
					catch { }
					break;
				}
			}

			_prev = mouse;
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


