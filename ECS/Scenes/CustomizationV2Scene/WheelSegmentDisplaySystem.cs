using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Rendering;
using Crusaders30XX.ECS.Singletons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("CV2 Segments")]
	public class WheelSegmentDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly SpriteFont _font = FontSingleton.TitleFont;
		private Texture2D _pixel;

		[DebugEditable(DisplayName = "Default BG R", Step = 1, Min = 0, Max = 255)]
		public int DefaultBgR { get; set; } = 42;

		[DebugEditable(DisplayName = "Default BG G", Step = 1, Min = 0, Max = 255)]
		public int DefaultBgG { get; set; } = 42;

		[DebugEditable(DisplayName = "Default BG B", Step = 1, Min = 0, Max = 255)]
		public int DefaultBgB { get; set; } = 42;

		[DebugEditable(DisplayName = "Hover BG R", Step = 1, Min = 0, Max = 255)]
		public int HoverBgR { get; set; } = 26;

		[DebugEditable(DisplayName = "Hover BG G", Step = 1, Min = 0, Max = 255)]
		public int HoverBgG { get; set; } = 26;

		[DebugEditable(DisplayName = "Hover BG B", Step = 1, Min = 0, Max = 255)]
		public int HoverBgB { get; set; } = 26;

		[DebugEditable(DisplayName = "Active BG R", Step = 1, Min = 0, Max = 255)]
		public int ActiveBgR { get; set; } = 160;

		[DebugEditable(DisplayName = "Active BG G", Step = 1, Min = 0, Max = 255)]
		public int ActiveBgG { get; set; } = 0;

		[DebugEditable(DisplayName = "Active BG B", Step = 1, Min = 0, Max = 255)]
		public int ActiveBgB { get; set; } = 0;

		[DebugEditable(DisplayName = "Label Scale", Step = 0.01f, Min = 0.03f, Max = 0.2f)]
		public float LabelScale { get; set; } = 0.12f;

		[DebugEditable(DisplayName = "Name Scale", Step = 0.01f, Min = 0.05f, Max = 0.3f)]
		public float NameScale { get; set; } = 0.15f;

		[DebugEditable(DisplayName = "Skew %", Step = 1, Min = 0, Max = 20)]
		public float SkewPercent { get; set; } = 28f;

		[DebugEditable(DisplayName = "Segment Width", Step = 4, Min = 60, Max = 1000)]
		public int SegmentWidth { get; set; } = 300;

		[DebugEditable(DisplayName = "Segment Height", Step = 2, Min = 30, Max = 1000)]
		public int SegmentHeight { get; set; } = 100;

		public WheelLayoutSystem LayoutSystem { get; set; }

		public WheelSegmentDisplaySystem(EntityManager em, GraphicsDevice gd, SpriteBatch sb) : base(em)
		{
			_graphicsDevice = gd;
			_spriteBatch = sb;
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<WheelSegment>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		public void Draw()
		{
			var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
			if (scene == null || scene.Current != SceneId.CustomizationV2) return;

			var nav = EntityManager.GetEntitiesWithComponent<CustomizationV2NavigationState>().FirstOrDefault()?.GetComponent<CustomizationV2NavigationState>();
			if (nav == null || nav.ActiveTab != CustomizationV2TabType.Loadout) return;

			var loadout = EntityManager.GetEntitiesWithComponent<CustomizationV2LoadoutState>().FirstOrDefault()?.GetComponent<CustomizationV2LoadoutState>();

			if (_pixel == null)
			{
				_pixel = new Texture2D(_graphicsDevice, 1, 1);
				_pixel.SetData(new[] { Color.White });
			}

			var segments = EntityManager.GetEntitiesWithComponent<WheelSegment>();
			foreach (var ent in segments)
			{
				var seg = ent.GetComponent<WheelSegment>();
				var tr = ent.GetComponent<Transform>();
				var ui = ent.GetComponent<UIElement>();
				if (seg == null || tr == null) continue;

				int hoveredIdx = loadout?.HoveredSegmentIndex ?? -1;
				bool isActive = hoveredIdx == seg.SegmentIndex;
				bool isHovered = ui?.IsHovered ?? false;

				Color bgColor;
				if (isActive) bgColor = new Color(ActiveBgR, ActiveBgG, ActiveBgB);
				else if (isHovered) bgColor = new Color(HoverBgR, HoverBgG, HoverBgB);
				else bgColor = new Color(DefaultBgR, DefaultBgG, DefaultBgB);

				// Draw parallelogram segment
				int x = (int)(tr.Position.X - SegmentWidth / 2f);
				int y = (int)(tr.Position.Y - SegmentHeight / 2f);
				var mask = PrimitiveTextureFactory.GetParallelogramMask(_graphicsDevice, SegmentWidth, SegmentHeight, SkewPercent);
				_spriteBatch.Draw(mask, new Rectangle(x, y, SegmentWidth, SegmentHeight), bgColor);

				// Slot label
				string label = WheelLayoutSystem.GetSlotLabel(seg.SlotType);
				var labelSize = _font.MeasureString(label) * LabelScale;
				float lx = tr.Position.X - labelSize.X / 2f;
				float ly = y + 6;
				var labelColor = isActive ? new Color(255, 255, 255, 180) : new Color(136, 136, 136);
				_spriteBatch.DrawString(_font, label, new Vector2(lx, ly), labelColor, 0f, Vector2.Zero, LabelScale, SpriteEffects.None, 0f);

				// Equipped item name
				string itemName = GetEquippedItemName(seg.SlotType, loadout);
				if (!string.IsNullOrEmpty(itemName))
				{
					var nameSize = _font.MeasureString(itemName) * NameScale;
					float nx = tr.Position.X - nameSize.X / 2f;
					float ny = ly + labelSize.Y + 2;
					_spriteBatch.DrawString(_font, itemName, new Vector2(nx, ny), Color.White, 0f, Vector2.Zero, NameScale, SpriteEffects.None, 0f);
				}
			}
		}

		private string GetEquippedItemName(WheelSlotType slot, CustomizationV2LoadoutState st)
		{
			if (st == null) return "-";
			string id = slot switch
			{
				WheelSlotType.Weapon => st.WorkingWeaponId,
				WheelSlotType.Head => st.WorkingHeadId,
				WheelSlotType.Chest => st.WorkingChestId,
				WheelSlotType.Arms => st.WorkingArmsId,
				WheelSlotType.Legs => st.WorkingLegsId,
				WheelSlotType.Temperance => st.WorkingTemperanceId,
				WheelSlotType.Medal1 => st.WorkingMedalIds?.Count > 0 ? st.WorkingMedalIds[0] : "",
				WheelSlotType.Medal2 => st.WorkingMedalIds?.Count > 1 ? st.WorkingMedalIds[1] : "",
				WheelSlotType.Medal3 => st.WorkingMedalIds?.Count > 2 ? st.WorkingMedalIds[2] : "",
				_ => ""
			};
			if (string.IsNullOrEmpty(id)) return "-";
			// Try to get a human-readable name
			var eq = Factories.EquipmentFactory.Create(id);
			if (eq != null) return eq.Name ?? id;
			var medal = Factories.MedalFactory.Create(id);
			if (medal != null) return medal.Name ?? id;
			if (Data.Temperance.TemperanceAbilityDefinitionCache.TryGet(id, out var temp) && temp != null)
				return temp.name ?? id;
			var card = Factories.CardFactory.Create(id);
			if (card != null) return card.Name ?? id;
			return id;
		}
	}
}
