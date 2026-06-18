using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Save;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Factories;
using Crusaders30XX.ECS.Services;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Systems
{
	public class ClimbColumnLayoutSystem : Core.System
	{
		private const string ShopColumnName = "Climb_Column_Shop";
		private const string EncounterColumnName = "Climb_Column_Encounter";
		private const string EventColumnName = "Climb_Column_Event";

		public ClimbColumnLayoutSystem(EntityManager entityManager)
			: base(entityManager)
		{
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
			SyncColumn(ShopColumnName, ClimbColumnKind.Shop, "Shop", "Spend resources before refresh", columns.Shop);
			SyncColumn(EncounterColumnName, ClimbColumnKind.Encounter, "Encounters", "Fight foes for red, white, and black", columns.Encounter);
			SyncColumn(EventColumnName, ClimbColumnKind.Event, "Events", "Timed windows and hidden stories", columns.Events, showEvents);
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
			SetBounds(entity, bounds, visible, UIElementEventType.None, 1500);
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
			SetBounds(entity, rect, !presentation.IsUnavailable && !presentation.IsSold && presentation.IsAffordable, UIElementEventType.ClimbShopSlotSelect, 1600);
			var ui = entity.GetComponent<UIElement>();
			if (ui != null)
			{
				ui.Tooltip = !presentation.IsUnavailable && !presentation.IsSold && !presentation.IsAffordable
					? "Unavailable"
					: string.Empty;
			}
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
			SetBounds(entity, rect, !presentation.IsUnavailable, UIElementEventType.ClimbEncounterSlotSelect, 1600);
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
			SetBounds(entity, rect, visible && !presentation.IsUnavailable, UIElementEventType.ClimbEventSlotSelect, 1600, hidden: !visible);
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
			int height = Game1.VirtualHeight - top - 48;
			int colW = 486;
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
			return new Rectangle(inner.X, inner.Y + 59 + index * (58 + ClimbColumnDisplaySystem.SlotGapValue), inner.Width, 58);
		}

		private static Rectangle ComputeEncounterSlotRect(Rectangle inner, int index)
		{
			return new Rectangle(inner.X, inner.Y + 59 + index * (210 + ClimbColumnDisplaySystem.SlotGapValue), inner.Width, 210);
		}

		private static Rectangle ComputeEventSlotRect(Rectangle inner, int index)
		{
			return new Rectangle(inner.X, inner.Y + 59 + index * (52 + ClimbColumnDisplaySystem.SlotGapValue), inner.Width, 52);
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
