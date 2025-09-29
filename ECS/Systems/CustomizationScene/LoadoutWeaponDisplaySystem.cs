using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Data.Equipment;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Crusaders30XX.ECS.Factories;
using Crusaders30XX.ECS.Data.Cards;

namespace Crusaders30XX.ECS.Systems
{
	public class LoadoutWeaponDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly LoadoutDeckPanelSystem _deckPanel;
		private readonly CustomizeEquipmentDisplaySystem _customizeEquipmentDisplaySystem;
		private readonly World _world;
		private int _entityId = 0;

		public int RowHeight { get; set; } = 120;
		public int LeftPadding { get; set; } = 10;
		public int SidePadding { get; set; } = 10;
		public int TopOffsetFromHeader { get; set; } = 0;

		public LoadoutWeaponDisplaySystem(EntityManager em, World world, GraphicsDevice gd, SpriteBatch sb, LoadoutDeckPanelSystem deckPanel, CustomizeEquipmentDisplaySystem customizeEquipmentDisplaySystem) : base(em)
		{
			_graphicsDevice = gd;
			_spriteBatch = sb;
			_deckPanel = deckPanel;
			_customizeEquipmentDisplaySystem = customizeEquipmentDisplaySystem;
			_world = world;
			EventManager.Subscribe<ShowTransition>(_ => ClearEntity());
			EventManager.Subscribe<SetCustomizationTab>(_ => ClearEntity());
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<SceneState>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			// No interaction on the loadout side for weapon; selection occurs from Available side
		}

		public void Draw()
		{
			var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
			if (scene == null || scene.Current != SceneId.Customization) return;
			var st = EntityManager.GetEntitiesWithComponent<CustomizationState>().FirstOrDefault()?.GetComponent<CustomizationState>();
			if (st == null || st.SelectedTab != CustomizationTabType.Weapon) return;

			string equippedId = st.WorkingWeaponId;
			if (string.IsNullOrEmpty(equippedId)) return;
			if (!CardDefinitionCache.TryGet(equippedId, out var def) || def == null) return;

			int vw = _graphicsDevice.Viewport.Width;
			int cardW = EntityManager.GetEntitiesWithComponent<CardVisualSettings>().First().GetComponent<CardVisualSettings>().CardWidth;
			int cardH = EntityManager.GetEntitiesWithComponent<CardVisualSettings>().First().GetComponent<CardVisualSettings>().CardHeight;
			int panelX = vw - _deckPanel.PanelWidth;
			int panelY = 0;
			int x = panelX + (_deckPanel.PanelWidth / 2);
			int y = panelY + _deckPanel.HeaderHeight + _deckPanel.TopMargin + (int)(cardH * _deckPanel.CardScale / 2) - st.RightScroll;
			var created = EnsureEntity(def);
			EventManager.Publish(new CardRenderScaledEvent { Card = created, Position = new Vector2(x, y), Scale = _deckPanel.CardScale });
		}

		private Entity EnsureEntity(CardDefinition def)
		{
			string name = def.name ?? def.id;
			string keyName = $"Card_{name}_Yellow";
			var existing = EntityManager.GetEntity(keyName);
			if (existing != null) return existing;
			var created = EntityFactory.CreateCardFromDefinition(_world, def.id, CardData.CardColor.Yellow, true);
			if (created != null)
			{
				_entityId = created.Id;
			}
			return created;
		}

		private void ClearEntity()
		{
			if (_entityId != 0)
			{
				EntityManager.DestroyEntity(_entityId);
				_entityId = 0;
			}
		}
	}
}


