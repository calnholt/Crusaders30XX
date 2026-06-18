using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Save;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Factories;
using Crusaders30XX.ECS.Services;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("Climb Column Layout")]
	public class ClimbColumnLayoutSystem : Core.System
	{
		private const string ShopColumnName = "Climb_Column_Shop";
		private const string EncounterColumnName = "Climb_Column_Encounter";
		private const string EventColumnName = "Climb_Column_Event";

		[DebugEditable(DisplayName = "Columns Bottom Padding", Step = 1, Min = 0, Max = 200)]
		public int ColumnsBottomPadding { get; set; } = 48;
		[DebugEditable(DisplayName = "Column Width", Step = 1, Min = 200, Max = 800)]
		public int ColumnWidth { get; set; } = 486;
		[DebugEditable(DisplayName = "Slot List Top Offset", Step = 1, Min = 0, Max = 120)]
		public int SlotListTopOffset { get; set; } = 59;
		[DebugEditable(DisplayName = "Shop Slot Height", Step = 1, Min = 32, Max = 160)]
		public int ShopSlotHeight { get; set; } = 104;
		[DebugEditable(DisplayName = "Encounter Slot Height", Step = 1, Min = 80, Max = 400)]
		public int EncounterSlotHeight { get; set; } = 217;
		[DebugEditable(DisplayName = "Event Slot Height", Step = 1, Min = 32, Max = 160)]
		public int EventSlotHeight { get; set; } = 52;
		[DebugEditable(DisplayName = "Column Z Order", Step = 1, Min = 0, Max = 5000)]
		public int ColumnZOrder { get; set; } = 1500;
		[DebugEditable(DisplayName = "Slot Z Order", Step = 1, Min = 0, Max = 5000)]
		public int SlotZOrder { get; set; } = 1600;
		[DebugEditable(DisplayName = "Shop Tooltip Offset Px", Step = 1, Min = 0, Max = 120)]
		public int ShopTooltipOffsetPx { get; set; } = 30;

		internal static int ColumnsBottomPaddingValue { get; private set; } = 48;
		internal static int ColumnWidthValue { get; private set; } = 486;
		internal static int SlotListTopOffsetValue { get; private set; } = 59;
		internal static int ShopSlotHeightValue { get; private set; } = 104;
		internal static int EncounterSlotHeightValue { get; private set; } = 217;
		internal static int EventSlotHeightValue { get; private set; } = 52;
		internal static int ColumnZOrderValue { get; private set; } = 1500;
		internal static int SlotZOrderValue { get; private set; } = 1600;
		internal static int ShopTooltipOffsetPxValue { get; private set; } = 30;

		public ClimbColumnLayoutSystem(EntityManager entityManager)
			: base(entityManager)
		{
		}

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);
			ColumnsBottomPaddingValue = ColumnsBottomPadding;
			ColumnWidthValue = ColumnWidth;
			SlotListTopOffsetValue = SlotListTopOffset;
			ShopSlotHeightValue = ShopSlotHeight;
			EncounterSlotHeightValue = EncounterSlotHeight;
			EventSlotHeightValue = EventSlotHeight;
			ColumnZOrderValue = ColumnZOrder;
			SlotZOrderValue = SlotZOrder;
			ShopTooltipOffsetPxValue = ShopTooltipOffsetPx;
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<SceneState>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			var scene = entity.GetComponent<SceneState>();
			if (scene?.Current != SceneId.Climb)
			{
				ClimbSceneSystem.DeactivateClimbUiEntities(EntityManager);
				return;
			}

			var climb = SaveCache.GetClimbState();
			bool showEvents = climb?.eventSlots?.Any(slot => ClimbRuleService.IsEventVisible(slot, climb.time)) == true;
			var columns = ComputeColumnsLayout(showEvents);
			SyncColumn(ShopColumnName, ClimbColumnKind.Shop, "Shop", "Spend resources before the shop refreshes", columns.Shop);
			SyncColumn(EncounterColumnName, ClimbColumnKind.Encounter, "Encounters", "Fight foes for red, white, and black resources", columns.Encounter);
			SyncColumn(EventColumnName, ClimbColumnKind.Event, "Events", "Timed windows hide medals, foes, and events", columns.Events, showEvents);
			SyncSlots(climb, columns, showEvents);
		}

		private void SyncSlots(ClimbSaveState climb, ClimbColumnsLayout columns, bool showEvents)
		{
			for (int i = 0; i < ClimbRuleService.ShopSlotCount; i++)
			{
				var slot = climb?.shopSlots != null && i < climb.shopSlots.Count ? climb.shopSlots[i] : null;
				var rect = ComputeShopSlotRect(columns.ShopInner, i);
				SyncShopSlot(i, slot, rect);
			}

			for (int i = 0; i < ClimbRuleService.EncounterSlotCount; i++)
			{
				var slot = climb?.encounterSlots != null && i < climb.encounterSlots.Count ? climb.encounterSlots[i] : null;
				var rect = ComputeEncounterSlotRect(columns.EncounterInner, i);
				SyncEncounterSlot(i, slot, rect);
			}

			var activeEvents = showEvents
				? climb.eventSlots.Where(slot => ClimbRuleService.IsEventVisible(slot, climb.time)).Take(ClimbRuleService.EventSlotCount).ToList()
				: new List<ClimbEventSlotSave>();
			for (int i = 0; i < ClimbRuleService.EventSlotCount; i++)
			{
				var slot = i < activeEvents.Count ? activeEvents[i] : null;
				var rect = ComputeEventSlotRect(columns.EventInner, i);
				SyncEventSlot(i, slot, rect, showEvents && slot != null);
			}
		}

		private void SyncColumn(string name, ClimbColumnKind kind, string title, string subtitle, Rectangle bounds, bool visible = true)
		{
			var entity = EnsureEntity(name);
			var column = entity.GetComponent<ClimbColumnPresentation>();
			if (column == null)
			{
				column = new ClimbColumnPresentation();
				EntityManager.AddComponent(entity, column);
			}
			column.Kind = kind;
			column.Title = title;
			column.Subtitle = subtitle;
			column.IsVisible = visible;
			column.InnerBounds = new Rectangle(bounds.X + ClimbColumnDisplaySystem.ColumnPaddingValue, bounds.Y + ClimbColumnDisplaySystem.ColumnPaddingValue, Math.Max(0, bounds.Width - ClimbColumnDisplaySystem.ColumnPaddingValue * 2), Math.Max(0, bounds.Height - ClimbColumnDisplaySystem.ColumnPaddingValue * 2));
			SetBounds(entity, bounds, visible, UIElementEventType.None, ColumnZOrderValue);
		}

		private void SyncShopSlot(int index, ClimbShopSlotSave slot, Rectangle rect)
		{
			var entity = EnsureEntity($"Climb_ShopSlot_{index}");
			var presentation = EnsureSlotPresentation(entity);
			presentation.Kind = ClimbSlotKind.Shop;
			presentation.SlotIndex = index;
			presentation.SlotId = slot?.id ?? $"shop_{index}";
			presentation.Title = ClimbSceneDrawHelpers.ResolveShopTitle(slot);
			presentation.Label = ClimbSceneDrawHelpers.ResolveShopLabel(slot);
			presentation.Meta = "PRICE";
			presentation.GeneratedAtTime = Math.Max(0, slot?.generatedAtTime ?? 0);
			presentation.Duration = 0;
			presentation.Cost = Clone(slot?.cost);
			presentation.Reward = new ClimbResourceSave { red = 0, white = 0, black = 0 };
			presentation.TimeCost = Math.Max(0, slot?.timeCost ?? 0);
			presentation.IsSold = slot?.isSold == true;
			presentation.IsCompleted = false;
			presentation.IsUnavailable = slot == null || string.Equals(slot.kind, ClimbShopSlotKinds.Empty, StringComparison.OrdinalIgnoreCase);
			presentation.IsAffordable = presentation.IsUnavailable || ClimbRuleService.CanAfford(SaveCache.GetClimbState()?.resources, slot?.cost);
			var action = entity.GetComponent<ClimbShopSlotAction>();
			if (action == null) EntityManager.AddComponent(entity, new ClimbShopSlotAction { SlotIndex = index });
			else action.SlotIndex = index;
			SetBounds(entity, rect, !presentation.IsUnavailable && !presentation.IsSold, UIElementEventType.ClimbShopSlotSelect, SlotZOrderValue);
			SyncShopTooltip(entity, slot, presentation);
		}

		private void SyncEncounterSlot(int index, ClimbEncounterSlotSave slot, Rectangle rect)
		{
			var entity = EnsureEntity($"Climb_EncounterSlot_{index}");
			var presentation = EnsureSlotPresentation(entity);
			var enemy = EnemyFactory.Create(slot?.enemyId);
			presentation.Kind = ClimbSlotKind.Encounter;
			presentation.SlotIndex = index;
			presentation.SlotId = slot?.id ?? $"encounter_{index}";
			presentation.Title = enemy?.Name ?? "Encounter";
			presentation.Label = slot?.isFinal == true ? "Final" : "Fight";
			presentation.Meta = "GAIN";
			presentation.GeneratedAtTime = Math.Max(0, slot?.generatedAtTime ?? 0);
			presentation.Duration = Math.Max(0, slot?.duration ?? 0);
			presentation.Cost = new ClimbResourceSave { red = 0, white = 0, black = 0 };
			presentation.Reward = Clone(slot?.rewardResources);
			presentation.TimeCost = Math.Max(0, slot?.timeCost ?? 0);
			presentation.IsSold = false;
			presentation.IsCompleted = slot?.isCompleted == true;
			presentation.IsUnavailable = slot == null || slot.isCompleted || string.IsNullOrWhiteSpace(slot.enemyId);
			presentation.IsAffordable = true;
			presentation.IsFinal = slot?.isFinal == true;
			presentation.PortraitAsset = EnemyPortraitContent.ToAssetName(slot?.enemyId ?? string.Empty);
			var action = entity.GetComponent<ClimbEncounterSlotAction>();
			if (action == null) EntityManager.AddComponent(entity, new ClimbEncounterSlotAction { SlotId = presentation.SlotId });
			else action.SlotId = presentation.SlotId;
			SetBounds(entity, rect, !presentation.IsUnavailable, UIElementEventType.ClimbEncounterSlotSelect, SlotZOrderValue);
		}

		private void SyncEventSlot(int index, ClimbEventSlotSave slot, Rectangle rect, bool visible)
		{
			var entity = EnsureEntity($"Climb_EventSlot_{index}");
			var presentation = EnsureSlotPresentation(entity);
			presentation.Kind = ClimbSlotKind.Event;
			presentation.SlotIndex = index;
			presentation.SlotId = slot?.id ?? $"event_{index}";
			presentation.Title = "Event";
			presentation.Label = slot == null ? string.Empty : $"T{slot.visibleStartTime}-{slot.visibleEndTime}";
			presentation.Meta = "TIME";
			presentation.GeneratedAtTime = Math.Max(0, slot?.generatedAtTime ?? 0);
			presentation.Duration = slot == null ? 0 : Math.Max(0, slot.visibleEndTime - ClimbRuleService.ClampTime(SaveCache.GetClimbState()?.time ?? 0));
			presentation.Cost = new ClimbResourceSave { red = 0, white = 0, black = 0 };
			presentation.Reward = new ClimbResourceSave { red = 0, white = 0, black = 0 };
			presentation.TimeCost = Math.Max(0, slot?.timeCost ?? 0);
			presentation.IsSold = false;
			presentation.IsCompleted = slot?.isCompleted == true;
			presentation.IsUnavailable = !visible || slot == null || slot.isCompleted;
			presentation.IsAffordable = true;
			var action = entity.GetComponent<ClimbEventSlotAction>();
			if (action == null) EntityManager.AddComponent(entity, new ClimbEventSlotAction { SlotId = presentation.SlotId });
			else action.SlotId = presentation.SlotId;
			SetBounds(entity, rect, visible && !presentation.IsUnavailable, UIElementEventType.ClimbEventSlotSelect, SlotZOrderValue, hidden: !visible);
		}

		private void SyncShopTooltip(Entity entity, ClimbShopSlotSave slot, ClimbSlotPresentation presentation)
		{
			var ui = entity.GetComponent<UIElement>();
			if (ui == null) return;

			ui.Tooltip = string.Empty;
			ui.TooltipType = TooltipType.Text;
			if (slot == null || presentation.IsUnavailable || presentation.IsSold)
			{
				ClearShopTooltip(entity, ui);
				return;
			}

			ui.TooltipPosition = TooltipPosition.Above;
			ui.TooltipOffsetPx = ShopTooltipOffsetPxValue;

			if (string.Equals(slot.kind, ClimbShopSlotKinds.Medal, StringComparison.OrdinalIgnoreCase))
			{
				RemoveCardTooltip(entity);
				RemoveEquipmentTooltip(entity);
				var medal = MedalFactory.Create(slot.itemId);
				ui.Tooltip = medal == null ? string.Empty : $"{medal.Name}\n\n{medal.Text}";
				ui.TooltipType = TooltipType.Text;
				return;
			}

			if (string.Equals(slot.kind, ClimbShopSlotKinds.Equipment, StringComparison.OrdinalIgnoreCase))
			{
				RemoveCardTooltip(entity);
				SyncEquipmentTooltip(entity, slot.itemId, ui);
				return;
			}

			if (string.Equals(slot.kind, ClimbShopSlotKinds.Upgrade, StringComparison.OrdinalIgnoreCase)
				|| string.Equals(slot.kind, ClimbShopSlotKinds.Replacement, StringComparison.OrdinalIgnoreCase))
			{
				RemoveCardTooltip(entity);
				RemoveEquipmentTooltip(entity);
				if (!RunDeckService.TryParseCardKey(slot.cardKey, out var cardId, out var color, out bool isUpgraded)) return;
				EntityManager.AddComponent(entity, new CardTooltip
				{
					CardId = cardId,
					CardColor = color,
					IsUpgraded = isUpgraded,
				});
				ui.TooltipType = TooltipType.Card;
			}
		}

		private void SyncEquipmentTooltip(Entity entity, string equipmentId, UIElement ui)
		{
			if (string.IsNullOrWhiteSpace(equipmentId)) return;
			var source = entity.GetComponent<ClimbShopTooltipSource>();
			var equipped = entity.GetComponent<EquippedEquipment>();
			if (source == null
				|| equipped == null
				|| !string.Equals(source.EquipmentId, equipmentId, StringComparison.OrdinalIgnoreCase))
			{
				equipped?.Dispose();
				if (source != null) EntityManager.RemoveComponent<ClimbShopTooltipSource>(entity);
				if (equipped != null) EntityManager.RemoveComponent<EquippedEquipment>(entity);
				if (entity.GetComponent<EquipmentZone>() != null) EntityManager.RemoveComponent<EquipmentZone>(entity);

				var equipment = EquipmentFactory.Create(equipmentId);
				if (equipment == null) return;
				equipment.Initialize(EntityManager, entity);
				EntityManager.AddComponent(entity, new ClimbShopTooltipSource { EquipmentId = equipmentId });
				EntityManager.AddComponent(entity, new EquippedEquipment { Equipment = equipment });
				EntityManager.AddComponent(entity, new EquipmentZone { Zone = EquipmentZoneType.Default });
			}

			ui.TooltipType = TooltipType.Equipment;
		}

		private void ClearShopTooltip(Entity entity, UIElement ui)
		{
			ui.Tooltip = string.Empty;
			ui.TooltipType = TooltipType.Text;

			RemoveCardTooltip(entity);
			RemoveEquipmentTooltip(entity);
		}

		private void RemoveCardTooltip(Entity entity)
		{
			if (entity.GetComponent<CardTooltip>() != null) EntityManager.RemoveComponent<CardTooltip>(entity);
		}

		private void RemoveEquipmentTooltip(Entity entity)
		{
			var equipped = entity.GetComponent<EquippedEquipment>();
			equipped?.Dispose();
			if (equipped != null) EntityManager.RemoveComponent<EquippedEquipment>(entity);
			if (entity.GetComponent<EquipmentZone>() != null) EntityManager.RemoveComponent<EquipmentZone>(entity);
			if (entity.GetComponent<ClimbShopTooltipSource>() != null) EntityManager.RemoveComponent<ClimbShopTooltipSource>(entity);
		}

		private ClimbSlotPresentation EnsureSlotPresentation(Entity entity)
		{
			var presentation = entity.GetComponent<ClimbSlotPresentation>();
			if (presentation == null)
			{
				presentation = new ClimbSlotPresentation();
				EntityManager.AddComponent(entity, presentation);
			}
			return presentation;
		}

		private Entity EnsureEntity(string name)
		{
			var entity = EntityManager.GetEntity(name);
			if (entity == null)
			{
				entity = EntityManager.CreateEntity(name);
				EntityManager.AddComponent(entity, new Transform());
				if (entity.GetComponent<OwnedByScene>() == null)
				{
					EntityManager.AddComponent(entity, new OwnedByScene { Scene = SceneId.Climb });
				}
			}
			return entity;
		}

		private void SetBounds(Entity entity, Rectangle rect, bool interactable, UIElementEventType eventType, int zOrder, bool hidden = false)
		{
			var transform = entity.GetComponent<Transform>();
			if (transform != null)
			{
				transform.Position = new Vector2(rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f);
				transform.ZOrder = zOrder;
			}

			var preview = EntityManager.GetEntity(ClimbHeaderLayoutSystem.RootName)?.GetComponent<ClimbPreviewState>();
			var slot = entity.GetComponent<ClimbSlotPresentation>();
			bool blockedByPreview = preview?.IsActive == true
				&& slot != null
				&& !string.Equals(slot.SlotId, preview.SourceSlotId, StringComparison.OrdinalIgnoreCase)
				&& preview.WouldVanishSlotIds.Contains(slot.SlotId);

			var ui = entity.GetComponent<UIElement>();
			if (ui == null)
			{
				EntityManager.AddComponent(entity, new UIElement { Bounds = rect, IsInteractable = interactable && !blockedByPreview, EventType = eventType, IsHidden = hidden });
			}
			else
			{
				ui.Bounds = rect;
				ui.IsInteractable = interactable && !blockedByPreview;
				ui.EventType = eventType;
				ui.IsHidden = hidden;
			}
		}

		internal static ClimbColumnsLayout ComputeColumnsLayout(bool showEvents)
		{
			int top = ClimbColumnDisplaySystem.ColumnsTopValue;
			int maxWidth = ClimbColumnDisplaySystem.ColumnsMaxWidthValue;
			int gap = ClimbColumnDisplaySystem.ColumnsGapValue;
			int height = Game1.VirtualHeight - top - ColumnsBottomPaddingValue;
			int colW = ColumnWidthValue;
			int groupW = showEvents ? colW * 3 + gap * 2 : colW * 2 + gap;
			int x = (Game1.VirtualWidth - Math.Min(maxWidth, groupW)) / 2;
			if (showEvents && groupW <= maxWidth)
			{
				x = (Game1.VirtualWidth - groupW) / 2;
			}
			else if (!showEvents)
			{
				x = (Game1.VirtualWidth - groupW) / 2;
			}

			var shop = new Rectangle(x, top, colW, height);
			var encounter = new Rectangle(shop.Right + gap, top, colW, height);
			var events = new Rectangle(encounter.Right + gap, top, colW, height);
			int pad = ClimbColumnDisplaySystem.ColumnPaddingValue;
			return new ClimbColumnsLayout
			{
				Shop = shop,
				Encounter = encounter,
				Events = events,
				ShopInner = new Rectangle(shop.X + pad, shop.Y + pad, shop.Width - pad * 2, shop.Height - pad * 2),
				EncounterInner = new Rectangle(encounter.X + pad, encounter.Y + pad, encounter.Width - pad * 2, encounter.Height - pad * 2),
				EventInner = new Rectangle(events.X + pad, events.Y + pad, events.Width - pad * 2, events.Height - pad * 2),
			};
		}

		private static Rectangle ComputeShopSlotRect(Rectangle inner, int index)
		{
			return new Rectangle(inner.X, inner.Y + SlotListTopOffsetValue + index * (ShopSlotHeightValue + ClimbColumnDisplaySystem.SlotGapValue), inner.Width, ShopSlotHeightValue);
		}

		private static Rectangle ComputeEncounterSlotRect(Rectangle inner, int index)
		{
			return new Rectangle(inner.X, inner.Y + SlotListTopOffsetValue + index * (EncounterSlotHeightValue + ClimbColumnDisplaySystem.SlotGapValue), inner.Width, EncounterSlotHeightValue);
		}

		private static Rectangle ComputeEventSlotRect(Rectangle inner, int index)
		{
			return new Rectangle(inner.X, inner.Y + SlotListTopOffsetValue + index * (EventSlotHeightValue + ClimbColumnDisplaySystem.SlotGapValue), inner.Width, EventSlotHeightValue);
		}

		private static ClimbResourceSave Clone(ClimbResourceSave resources)
		{
			return new ClimbResourceSave
			{
				red = Math.Max(0, resources?.red ?? 0),
				white = Math.Max(0, resources?.white ?? 0),
				black = Math.Max(0, resources?.black ?? 0),
			};
		}
	}

	public struct ClimbColumnsLayout
	{
		public Rectangle Shop;
		public Rectangle Encounter;
		public Rectangle Events;
		public Rectangle ShopInner;
		public Rectangle EncounterInner;
		public Rectangle EventInner;
	}
}
