using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Data.Cards;
using Crusaders30XX.ECS.Factories;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;
using System;

namespace Crusaders30XX.ECS.Systems
{
	public class AvailableWeaponDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly CardLibraryPanelSystem _libraryPanel;
		private readonly CustomizeEquipmentDisplaySystem _customizeEquipmentDisplaySystem;
		private readonly World _world;
		private readonly Dictionary<string, int> _createdCardIds = new();
		private MouseState _prevMouse;

		public int RowHeight { get; set; } = 120;
		public int ItemSpacing { get; set; } = 10;
		public int LeftPadding { get; set; } = 10;
		public int SidePadding { get; set; } = 10;
		public int TopOffsetFromHeader { get; set; } = 0;

		public AvailableWeaponDisplaySystem(EntityManager em, World world, GraphicsDevice gd, SpriteBatch sb, CardLibraryPanelSystem libraryPanel, CustomizeEquipmentDisplaySystem customizeEquipmentDisplaySystem) : base(em)
		{
			_world = world;
			_graphicsDevice = gd;
			_spriteBatch = sb;
			_libraryPanel = libraryPanel;
			_customizeEquipmentDisplaySystem = customizeEquipmentDisplaySystem;
			_prevMouse = Mouse.GetState();
			EventManager.Subscribe<ShowTransition>(_ => ClearCards());
			EventManager.Subscribe<SetCustomizationTab>(_ => ClearCards());
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<SceneState>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			if (TransitionStateSingleton.IsActive) return;
			var scene = entity.GetComponent<SceneState>();
			if (scene == null || scene.Current != SceneId.Customization) return;
			var st = EntityManager.GetEntitiesWithComponent<CustomizationState>().FirstOrDefault()?.GetComponent<CustomizationState>();
			if (st == null || st.SelectedTab != CustomizationTabType.Weapon) return;

			string equippedId = st.WorkingWeaponId ?? string.Empty;

			var defs = CardDefinitionCache.GetAll().Values
				.Where(d => d.isWeapon)
				.Where(d => (d.id ?? string.Empty) != equippedId)
				.OrderBy(d => ((d.name ?? d.id) ?? string.Empty).ToLowerInvariant())
				.ToList();

			int cardW = EntityManager.GetEntitiesWithComponent<CardVisualSettings>().First().GetComponent<CardVisualSettings>().CardWidth;
			int cardH = EntityManager.GetEntitiesWithComponent<CardVisualSettings>().First().GetComponent<CardVisualSettings>().CardHeight;
			int colW = (int)(cardW * _libraryPanel.CardScale) + 20;
			int col = Math.Max(1, _libraryPanel.Columns);
			var mouse = Mouse.GetState();
			bool click = mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released;
			for (int i = 0; i < defs.Count; i++)
			{
				var d = defs[i];
				int r = i / col;
				int c = i % col;
				int x = 0 + c * colW + (colW / 2);
				int y = 0 + _libraryPanel.HeaderHeight + _libraryPanel.TopMargin + r * ((int)(cardH * _libraryPanel.CardScale) + _libraryPanel.RowGap) + (int)(cardH * _libraryPanel.CardScale / 2) - st.LeftScroll;
				var rect = new Rectangle(x - (int)(cardW * _libraryPanel.CardScale / 2), y - (int)(cardH * _libraryPanel.CardScale / 2), (int)(cardW * _libraryPanel.CardScale), (int)(cardH * _libraryPanel.CardScale));
				if (click && rect.Contains(mouse.Position))
				{
					EventManager.Publish(new UpdateEquipmentLoadoutRequested { Slot = CustomizationTabType.Weapon, EquipmentId = d.id });
				}
			}
			_prevMouse = mouse;
		}

		public void Draw()
		{
			if (TransitionStateSingleton.IsActive) return;
			var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
			if (scene == null || scene.Current != SceneId.Customization) return;
			var st = EntityManager.GetEntitiesWithComponent<CustomizationState>().FirstOrDefault()?.GetComponent<CustomizationState>();
			if (st == null || st.SelectedTab != CustomizationTabType.Weapon) return;

			string equippedId = st.WorkingWeaponId ?? string.Empty;

			var defs = CardDefinitionCache.GetAll().Values
				.Where(d => d.isWeapon)
				.Where(d => (d.id ?? string.Empty) != equippedId)
				.OrderBy(d => ((d.name ?? d.id) ?? string.Empty).ToLowerInvariant())
				.ToList();



			int cardW = EntityManager.GetEntitiesWithComponent<CardVisualSettings>().First().GetComponent<CardVisualSettings>().CardWidth;
			int cardH = EntityManager.GetEntitiesWithComponent<CardVisualSettings>().First().GetComponent<CardVisualSettings>().CardHeight;
			int colW = (int)(cardW * _libraryPanel.CardScale) + 20;
			int col = Math.Max(1, _libraryPanel.Columns);
			for (int i = 0; i < defs.Count; i++)
			{
				var d = defs[i];
				int r = i / col;
				int c = i % col;
				int x = 0 + c * colW + (colW / 2);
				int y = 0 + _libraryPanel.HeaderHeight + _libraryPanel.TopMargin + r * ((int)(cardH * _libraryPanel.CardScale) + _libraryPanel.RowGap) + (int)(cardH * _libraryPanel.CardScale / 2) - st.LeftScroll;
			var tempCard = EnsureTempCard(d);
				if (tempCard != null)
				{
					EventManager.Publish(new CardRenderScaledEvent { Card = tempCard, Position = new Vector2(x, y), Scale = _libraryPanel.CardScale });
				}
			}
		}

		private Entity EnsureTempCard(CardDefinition def)
		{
			string name = def.name ?? def.id;
			string keyName = $"Card_{name}_Yellow";
			var existing = EntityManager.GetEntity(keyName);
			if (existing != null) return existing;
			var created = EntityFactory.CreateCardFromDefinition(EntityManager, def.id, CardData.CardColor.Yellow, true);
			if (created != null)
			{
				_createdCardIds[keyName] = created.Id;
			}
			return created;
		}

		private void ClearCards()
		{
			foreach (var id in _createdCardIds.Values)
			{
				EntityManager.DestroyEntity(id);
			}
			_createdCardIds.Clear();
		}
	}
}


