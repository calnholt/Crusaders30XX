using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("CV2 Wheel Layout")]
	public class WheelLayoutSystem : Core.System
	{
		private readonly List<Entity> _segmentEntities = new();
		private bool _segmentsCreated;

		[DebugEditable(DisplayName = "Wheel Center X", Step = 4, Min = 100, Max = 1200)]
		public int WheelCenterX { get; set; } = 832;

		[DebugEditable(DisplayName = "Wheel Center Y", Step = 4, Min = 100, Max = 900)]
		public int WheelCenterY { get; set; } = 564;

		[DebugEditable(DisplayName = "Wheel Radius", Step = 4, Min = 100, Max = 700)]
		public int WheelRadius { get; set; } = 400;

		[DebugEditable(DisplayName = "Segment Width", Step = 4, Min = 60, Max = 200)]
		public int SegmentWidth { get; set; } = 140;

		[DebugEditable(DisplayName = "Segment Height", Step = 2, Min = 30, Max = 80)]
		public int SegmentHeight { get; set; } = 52;

		[DebugEditable(DisplayName = "Angle Spacing", Step = 1, Min = 20, Max = 60)]
		public float AngleSpacing { get; set; } = 39f;

		[DebugEditable(DisplayName = "Start Angle", Step = 5, Min = 0, Max = 360)]
		public float StartAngle { get; set; } = 285f;

		private static readonly WheelSlotType[] SlotOrder =
		{
			WheelSlotType.Weapon,
			WheelSlotType.Head,
			WheelSlotType.Chest,
			WheelSlotType.Arms,
			WheelSlotType.Legs,
			WheelSlotType.Temperance,
			WheelSlotType.Medal1,
			WheelSlotType.Medal2,
			WheelSlotType.Medal3
		};

		public WheelLayoutSystem(EntityManager em) : base(em)
		{
			EventManager.Subscribe<ShowTransition>(_ => CleanupEntities());
			EventManager.Subscribe<SwitchCustomizationV2Tab>(_ => { });
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

			EnsureSegmentEntities();
			UpdateSegmentPositions();
		}

		private void EnsureSegmentEntities()
		{
			if (_segmentsCreated) return;
			_segmentsCreated = true;

			for (int i = 0; i < SlotOrder.Length; i++)
			{
				var ent = EntityManager.CreateEntity($"CV2_WheelSegment_{i}");
				EntityManager.AddComponent(ent, new WheelSegment { SlotType = SlotOrder[i], SegmentIndex = i });
				EntityManager.AddComponent(ent, new Transform { Position = Vector2.Zero, ZOrder = 50000 });
				EntityManager.AddComponent(ent, new UIElement
				{
					Bounds = Rectangle.Empty,
					IsInteractable = true,
					IsPreventDefaultClick = true
				});
				_segmentEntities.Add(ent);
			}
		}

		private void UpdateSegmentPositions()
		{
			for (int i = 0; i < _segmentEntities.Count; i++)
			{
				var ent = _segmentEntities[i];
				if (!ent.IsActive) continue;

				var layout = ComputeSegmentLayout(i);
				var tr = ent.GetComponent<Transform>();
				if (tr != null) tr.Position = layout.position;

				var ui = ent.GetComponent<UIElement>();
				if (ui != null)
				{
					ui.Bounds = new Rectangle(
						(int)(layout.position.X - SegmentWidth / 2f),
						(int)(layout.position.Y - SegmentHeight / 2f),
						SegmentWidth,
						SegmentHeight
					);
				}
			}
		}

		public (Vector2 position, float angleDeg) ComputeSegmentLayout(int index)
		{
			float angleDeg = StartAngle + index * AngleSpacing;
			float angleRad = MathHelper.ToRadians(angleDeg);
			float x = WheelCenterX + WheelRadius * (float)Math.Cos(angleRad);
			float y = WheelCenterY + WheelRadius * (float)Math.Sin(angleRad);
			return (new Vector2(x, y), angleDeg);
		}

		public Vector2 GetWheelCenter() => new Vector2(WheelCenterX, WheelCenterY);

		public static WheelSlotType GetSlotType(int index)
		{
			if (index >= 0 && index < SlotOrder.Length) return SlotOrder[index];
			return WheelSlotType.Weapon;
		}

		public static string GetSlotLabel(WheelSlotType slot) => slot switch
		{
			WheelSlotType.Weapon => "WEAPON",
			WheelSlotType.Head => "HEAD",
			WheelSlotType.Chest => "CHEST",
			WheelSlotType.Arms => "ARMS",
			WheelSlotType.Legs => "LEGS",
			WheelSlotType.Temperance => "TEMPERANCE",
			WheelSlotType.Medal1 => "MEDAL 1",
			WheelSlotType.Medal2 => "MEDAL 2",
			WheelSlotType.Medal3 => "MEDAL 3",
			_ => ""
		};

		private void CleanupEntities()
		{
			foreach (var ent in _segmentEntities)
			{
				EntityManager.DestroyEntity(ent.Id);
			}
			_segmentEntities.Clear();
			_segmentsCreated = false;
		}
	}
}
