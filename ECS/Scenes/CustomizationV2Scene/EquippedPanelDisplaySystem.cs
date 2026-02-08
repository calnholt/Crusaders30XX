using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Singletons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("CV2 Equipped")]
	public class EquippedPanelDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly SpriteFont _headingFont = FontSingleton.TitleFont;
		private readonly SpriteFont _contentFont = FontSingleton.ContentFont;
		private Texture2D _pixel;

		[DebugEditable(DisplayName = "Panel Width", Step = 4, Min = 160, Max = 400)]
		public int PanelWidth { get; set; } = 240;

		[DebugEditable(DisplayName = "Panel BG R", Step = 1, Min = 0, Max = 255)]
		public int PanelBgR { get; set; } = 26;

		[DebugEditable(DisplayName = "Panel BG G", Step = 1, Min = 0, Max = 255)]
		public int PanelBgG { get; set; } = 26;

		[DebugEditable(DisplayName = "Panel BG B", Step = 1, Min = 0, Max = 255)]
		public int PanelBgB { get; set; } = 26;

		[DebugEditable(DisplayName = "Row Height", Step = 2, Min = 40, Max = 100)]
		public int RowHeight { get; set; } = 72;

		[DebugEditable(DisplayName = "Row Pad X", Step = 2, Min = 4, Max = 30)]
		public int RowPadX { get; set; } = 16;

		[DebugEditable(DisplayName = "Row Pad Y", Step = 2, Min = 2, Max = 16)]
		public int RowPadY { get; set; } = 8;

		[DebugEditable(DisplayName = "Label Scale", Step = 0.01f, Min = 0.03f, Max = 0.2f)]
		public float LabelScale { get; set; } = 0.07f;

		[DebugEditable(DisplayName = "Name Scale", Step = 0.01f, Min = 0.05f, Max = 0.2f)]
		public float NameScale { get; set; } = 0.10f;

		[DebugEditable(DisplayName = "Stat Scale", Step = 0.01f, Min = 0.03f, Max = 0.15f)]
		public float StatScale { get; set; } = 0.08f;

		[DebugEditable(DisplayName = "Top Margin", Step = 2, Min = 40, Max = 200)]
		public int TopMargin { get; set; } = 80;

		[DebugEditable(DisplayName = "Title Scale", Step = 0.01f, Min = 0.05f, Max = 0.3f)]
		public float TitleScale { get; set; } = 0.10f;

		[DebugEditable(DisplayName = "Highlight BG R", Step = 1, Min = 0, Max = 255)]
		public int HighlightBgR { get; set; } = 60;

		[DebugEditable(DisplayName = "Highlight BG G", Step = 1, Min = 0, Max = 255)]
		public int HighlightBgG { get; set; } = 10;

		[DebugEditable(DisplayName = "Highlight BG B", Step = 1, Min = 0, Max = 255)]
		public int HighlightBgB { get; set; } = 10;

		public EquippedPanelDisplaySystem(EntityManager em, GraphicsDevice gd, SpriteBatch sb) : base(em)
		{
			_graphicsDevice = gd;
			_spriteBatch = sb;
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<SceneState>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		public void Draw()
		{
			var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
			if (scene == null || scene.Current != SceneId.CustomizationV2) return;

			var nav = EntityManager.GetEntitiesWithComponent<CustomizationV2NavigationState>().FirstOrDefault()?.GetComponent<CustomizationV2NavigationState>();
			if (nav == null || nav.ActiveTab != CustomizationV2TabType.Loadout) return;

			var loadout = EntityManager.GetEntitiesWithComponent<CustomizationV2LoadoutState>().FirstOrDefault()?.GetComponent<CustomizationV2LoadoutState>();
			if (loadout == null) return;

			if (_pixel == null)
			{
				_pixel = new Texture2D(_graphicsDevice, 1, 1);
				_pixel.SetData(new[] { Color.White });
			}

			int vw = Game1.VirtualWidth;
			int vh = Game1.VirtualHeight;
			int panelX = vw - PanelWidth;

			// Panel background
			_spriteBatch.Draw(_pixel, new Rectangle(panelX, 0, PanelWidth, vh), new Color(PanelBgR, PanelBgG, PanelBgB));
			// Left border
			_spriteBatch.Draw(_pixel, new Rectangle(panelX, 0, 1, vh), new Color(51, 51, 51));

			// Title
			string title = "EQUIPPED";
			var titleSize = _headingFont.MeasureString(title) * TitleScale;
			float titleX = panelX + RowPadX;
			float titleY = TopMargin;
			_spriteBatch.DrawString(_headingFont, title, new Vector2(titleX, titleY), new Color(102, 102, 102), 0f, Vector2.Zero, TitleScale, SpriteEffects.None, 0f);

			// Divider under title
			int divY = (int)(titleY + titleSize.Y + 8);
			_spriteBatch.Draw(_pixel, new Rectangle(panelX + RowPadX, divY, PanelWidth - RowPadX * 2, 1), new Color(51, 51, 51));

			// Draw rows for each slot
			var slots = new[]
			{
				WheelSlotType.Weapon, WheelSlotType.Head, WheelSlotType.Chest,
				WheelSlotType.Arms, WheelSlotType.Legs, WheelSlotType.Temperance,
				WheelSlotType.Medal1, WheelSlotType.Medal2, WheelSlotType.Medal3
			};

			int rowY = divY + 12;
			for (int i = 0; i < slots.Length; i++)
			{
				var slot = slots[i];
				bool isHighlighted = loadout.HoveredSegmentIndex == i;
				string label = WheelLayoutSystem.GetSlotLabel(slot);
				string itemName = GetEquippedItemName(slot, loadout);
				string stat = GetStatLine(slot, loadout);

				DrawRow(panelX, rowY, label, itemName, stat, isHighlighted);

				// Row divider
				_spriteBatch.Draw(_pixel, new Rectangle(panelX + RowPadX, rowY + RowHeight, PanelWidth - RowPadX * 2, 1), new Color(255, 255, 255, 8));
				rowY += RowHeight;
			}
		}

		private void DrawRow(int panelX, int y, string label, string name, string stat, bool highlighted)
		{
			if (highlighted)
			{
				_spriteBatch.Draw(_pixel, new Rectangle(panelX, y, PanelWidth, RowHeight), new Color(HighlightBgR, HighlightBgG, HighlightBgB));
				// Left accent
				_spriteBatch.Draw(_pixel, new Rectangle(panelX, y, 2, RowHeight), new Color(196, 30, 58));
			}

			float textX = panelX + RowPadX;
			float textY = y + RowPadY;

			// Slot label
			_spriteBatch.DrawString(_headingFont, label, new Vector2(textX, textY), new Color(85, 85, 85), 0f, Vector2.Zero, LabelScale, SpriteEffects.None, 0f);
			var labelSize = _headingFont.MeasureString(label) * LabelScale;
			textY += labelSize.Y + 2;

			// Item name
			if (!string.IsNullOrEmpty(name))
			{
				_spriteBatch.DrawString(_contentFont, name, new Vector2(textX, textY), new Color(240, 240, 240), 0f, Vector2.Zero, NameScale, SpriteEffects.None, 0f);
				var nameSize = _contentFont.MeasureString(name) * NameScale;

				// Stat on the right
				if (!string.IsNullOrEmpty(stat))
				{
					var statSize = _contentFont.MeasureString(stat) * StatScale;
					float statX = panelX + PanelWidth - RowPadX - statSize.X;
					_spriteBatch.DrawString(_contentFont, stat, new Vector2(statX, textY), new Color(196, 30, 58), 0f, Vector2.Zero, StatScale, SpriteEffects.None, 0f);
				}
			}
		}

		private string GetEquippedItemName(WheelSlotType slot, CustomizationV2LoadoutState st)
		{
			string id = GetEquippedId(slot, st);
			if (string.IsNullOrEmpty(id)) return "-";
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

		private string GetStatLine(WheelSlotType slot, CustomizationV2LoadoutState st)
		{
			string id = GetEquippedId(slot, st);
			if (string.IsNullOrEmpty(id)) return "";
			var eq = Factories.EquipmentFactory.Create(id);
			if (eq != null) return $"BLK: {eq.Block} x{eq.Uses}";
			return "";
		}

		private static string GetEquippedId(WheelSlotType slot, CustomizationV2LoadoutState st) => slot switch
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
	}
}
