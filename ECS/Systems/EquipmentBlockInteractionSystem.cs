using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Handles clicking equipped equipment during Block sub-phase to assign it as block.
	/// Publishes BlockAssignmentAdded and marks the equipment as AssignedBlock in EquipmentZone.
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
			// Only in Block phase
			var phase = EntityManager.GetEntitiesWithComponent<PhaseState>().FirstOrDefault()?.GetComponent<PhaseState>();
			if (phase == null || phase.Sub != SubPhase.Block) return;
			// Need current context
			var enemy = EntityManager.GetEntitiesWithComponent<AttackIntent>().FirstOrDefault();
			var ctx = enemy?.GetComponent<AttackIntent>()?.Planned?.FirstOrDefault()?.ContextId;
			if (string.IsNullOrEmpty(ctx)) return;

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
				if (!ui.IsInteractable) continue;
				if (!ui.Bounds.Contains(mouse.Position)) continue;

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

				// Publish assignment and mark zone state
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

				// Create AssignedBlockCard animation state so AssignedBlockCardsDisplaySystem can animate/display it
				var t = eqEntity.GetComponent<Transform>();
				var returnCenter = new Vector2(ui.Bounds.X + ui.Bounds.Width * 0.5f, ui.Bounds.Y + ui.Bounds.Height * 0.5f);
				var abc = eqEntity.GetComponent<AssignedBlockCard>();
				if (abc == null)
				{
					abc = new AssignedBlockCard
					{
						ContextId = ctx,
						BlockAmount = blockVal,
						AssignedAtTicks = System.DateTime.UtcNow.Ticks,
						StartPos = t?.Position ?? Vector2.Zero,
						CurrentPos = t?.Position ?? Vector2.Zero,
						TargetPos = t?.Position ?? Vector2.Zero,
						StartScale = t?.Scale.X ?? 1f,
						TargetScale = 0.35f,
						Phase = AssignedBlockCard.PhaseState.Pullback,
						Elapsed = 0f,
						IsEquipment = true,
						ColorKey = NormalizeColorKey(color),
						Tooltip = BuildEquipmentTooltip(comp),
						DisplayBgColor = ResolveEquipmentBgColor(color),
						DisplayFgColor = ResolveFgForBg(ResolveEquipmentBgColor(color)),
						ReturnTargetPos = returnCenter,
						EquipmentType = comp.EquipmentType
					};
					EntityManager.AddComponent(eqEntity, abc);
				}
				else
				{
					abc.ContextId = ctx;
					abc.BlockAmount = blockVal;
					abc.AssignedAtTicks = System.DateTime.UtcNow.Ticks;
					abc.Phase = AssignedBlockCard.PhaseState.Pullback;
					abc.Elapsed = 0f;
					abc.IsEquipment = true;
					abc.ColorKey = NormalizeColorKey(color);
					abc.Tooltip = BuildEquipmentTooltip(comp);
					abc.DisplayBgColor = ResolveEquipmentBgColor(color);
					abc.DisplayFgColor = ResolveFgForBg(abc.DisplayBgColor);
					abc.ReturnTargetPos = returnCenter;
					abc.EquipmentType = comp.EquipmentType;
				}

				// Prevent UI hover-interaction while assigned
				ui.IsInteractable = false;
				ui.IsHovered = false;
				break;
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


