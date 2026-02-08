using System.Linq;
using System.Collections.Generic;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
	public class CustomizationV2RootSystem : Core.System
	{
		private readonly World _world;
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;

		// Shared systems
		private CustomizationV2BackgroundSystem _backgroundSystem;
		private CustomizationV2HeaderSystem _headerSystem;

		// Deck systems
		private DeckV2SceneSystem _deckSceneSystem;
		private DeckV2StatsBarSystem _statsBarSystem;
		private DeckV2AvailableGridSystem _availableGridSystem;
		private DeckV2DeckListSystem _deckListSystem;
		private DeckV2AutoSaveSystem _autoSaveSystem;
		private DeckV2InvalidDeckDialogSystem _invalidDeckDialogSystem;

		// Loadout systems
		private CustomizationV2LoadoutStateSystem _loadoutStateSystem;
		private WheelLayoutSystem _wheelLayoutSystem;
		private WheelSegmentDisplaySystem _wheelSegmentDisplaySystem;
		private WheelDecorativeRingDisplaySystem _wheelRingDisplaySystem;
		private WheelInteractionSystem _wheelInteractionSystem;
		private CenterHubDisplaySystem _centerHubDisplaySystem;
		private ConnectorChevronDisplaySystem _connectorChevronDisplaySystem;
		private BrowseStateSystem _browseStateSystem;
		private EquipSystem _equipSystem;
		private EquippedPanelDisplaySystem _equippedPanelDisplaySystem;
		private BrowseArrowDisplaySystem _browseArrowDisplaySystem;

		public CustomizationV2RootSystem(EntityManager em, SystemManager sm, World world, GraphicsDevice gd, SpriteBatch sb) : base(em)
		{
			_world = world;
			_graphicsDevice = gd;
			_spriteBatch = sb;

			// Shared systems
			_backgroundSystem = new CustomizationV2BackgroundSystem(em, gd, sb);
			_headerSystem = new CustomizationV2HeaderSystem(em, gd, sb);

			// Deck systems
			_deckSceneSystem = new DeckV2SceneSystem(em);
			_statsBarSystem = new DeckV2StatsBarSystem(em, gd, sb);
			_availableGridSystem = new DeckV2AvailableGridSystem(em, world, gd, sb);
			_deckListSystem = new DeckV2DeckListSystem(em, gd, sb);
			_autoSaveSystem = new DeckV2AutoSaveSystem(em);
			_invalidDeckDialogSystem = new DeckV2InvalidDeckDialogSystem(em, gd, sb);

			// Loadout systems
			_loadoutStateSystem = new CustomizationV2LoadoutStateSystem(em);
			_wheelLayoutSystem = new WheelLayoutSystem(em);
			_wheelSegmentDisplaySystem = new WheelSegmentDisplaySystem(em, gd, sb);
			_wheelRingDisplaySystem = new WheelDecorativeRingDisplaySystem(em, gd, sb);
			_wheelInteractionSystem = new WheelInteractionSystem(em);
			_browseStateSystem = new BrowseStateSystem(em);
			_equipSystem = new EquipSystem(em, _browseStateSystem);
			_centerHubDisplaySystem = new CenterHubDisplaySystem(em, gd, sb);
			_connectorChevronDisplaySystem = new ConnectorChevronDisplaySystem(em, gd, sb);
			_equippedPanelDisplaySystem = new EquippedPanelDisplaySystem(em, gd, sb);
			_browseArrowDisplaySystem = new BrowseArrowDisplaySystem(em, gd, sb);

			// Wire up cross-system references
			_statsBarSystem.HeaderSystem = _headerSystem;
			_availableGridSystem.HeaderSystem = _headerSystem;
			_availableGridSystem.StatsBarSystem = _statsBarSystem;
			_deckListSystem.HeaderSystem = _headerSystem;
			_deckListSystem.StatsBarSystem = _statsBarSystem;
			_headerSystem.InvalidDeckDialogSystem = _invalidDeckDialogSystem;

			_wheelSegmentDisplaySystem.LayoutSystem = _wheelLayoutSystem;
			_wheelRingDisplaySystem.LayoutSystem = _wheelLayoutSystem;
			_connectorChevronDisplaySystem.LayoutSystem = _wheelLayoutSystem;
			_centerHubDisplaySystem.LayoutSystem = _wheelLayoutSystem;
			_centerHubDisplaySystem.BrowseSystem = _browseStateSystem;
			_browseArrowDisplaySystem.LayoutSystem = _wheelLayoutSystem;
			_browseArrowDisplaySystem.BrowseSystem = _browseStateSystem;

			// Register all systems with the world
			world.AddSystem(_backgroundSystem);
			world.AddSystem(_headerSystem);

			world.AddSystem(_deckSceneSystem);
			world.AddSystem(_statsBarSystem);
			world.AddSystem(_availableGridSystem);
			world.AddSystem(_deckListSystem);
			world.AddSystem(_autoSaveSystem);
			world.AddSystem(_invalidDeckDialogSystem);

			world.AddSystem(_loadoutStateSystem);
			world.AddSystem(_wheelLayoutSystem);
			world.AddSystem(_wheelSegmentDisplaySystem);
			world.AddSystem(_wheelRingDisplaySystem);
			world.AddSystem(_wheelInteractionSystem);
			world.AddSystem(_browseStateSystem);
			world.AddSystem(_equipSystem);
			world.AddSystem(_centerHubDisplaySystem);
			world.AddSystem(_connectorChevronDisplaySystem);
			world.AddSystem(_equippedPanelDisplaySystem);
			world.AddSystem(_browseArrowDisplaySystem);

			// Subscribe to scene load
			EventManager.Subscribe<LoadSceneEvent>(evt =>
			{
				if (evt.Scene == SceneId.CustomizationV2)
				{
					EventManager.Publish(new ChangeMusicTrack { Track = MusicTrack.Customize });

					var navEntity = EntityManager.CreateEntity("CustomizationV2Nav");
					EntityManager.AddComponent(navEntity, new CustomizationV2NavigationState());

					SetTabActivation(CustomizationV2TabType.Deck);
				}
			});

			// Subscribe to tab switching to activate/deactivate systems
			EventManager.Subscribe<SwitchCustomizationV2Tab>(OnTabSwitch);
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<SceneState>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			var scene = entity.GetComponent<SceneState>();
			if (scene == null || scene.Current != SceneId.CustomizationV2) return;

			// Ensure correct tab activation state
			var nav = EntityManager.GetEntitiesWithComponent<CustomizationV2NavigationState>().FirstOrDefault()?.GetComponent<CustomizationV2NavigationState>();
			if (nav != null)
			{
				SetTabActivation(nav.ActiveTab);
			}
		}

		private void OnTabSwitch(SwitchCustomizationV2Tab evt)
		{
			var nav = EntityManager.GetEntitiesWithComponent<CustomizationV2NavigationState>()
				.FirstOrDefault()?.GetComponent<CustomizationV2NavigationState>();
			if (nav != null)
				nav.ActiveTab = evt.Tab;
			SetTabActivation(evt.Tab);
		}

		private void SetTabActivation(CustomizationV2TabType activeTab)
		{
			bool isDeck = activeTab == CustomizationV2TabType.Deck;
			bool isLoadout = activeTab == CustomizationV2TabType.Loadout;

			// Deck systems
			_deckSceneSystem.SetActive(isDeck);
			_statsBarSystem.SetActive(isDeck);
			_availableGridSystem.SetActive(isDeck);
			_deckListSystem.SetActive(isDeck);
			_autoSaveSystem.SetActive(isDeck);
			_invalidDeckDialogSystem.SetActive(isDeck);

			// Loadout systems
			_loadoutStateSystem.SetActive(isLoadout);
			_wheelLayoutSystem.SetActive(isLoadout);
			_wheelSegmentDisplaySystem.SetActive(isLoadout);
			_wheelRingDisplaySystem.SetActive(isLoadout);
			_wheelInteractionSystem.SetActive(isLoadout);
			_browseStateSystem.SetActive(isLoadout);
			_equipSystem.SetActive(isLoadout);
			_centerHubDisplaySystem.SetActive(isLoadout);
			_connectorChevronDisplaySystem.SetActive(isLoadout);
			_equippedPanelDisplaySystem.SetActive(isLoadout);
			_browseArrowDisplaySystem.SetActive(isLoadout);
		}

		public void Draw()
		{
			var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
			if (scene == null || scene.Current != SceneId.CustomizationV2) return;

			var nav = EntityManager.GetEntitiesWithComponent<CustomizationV2NavigationState>().FirstOrDefault()?.GetComponent<CustomizationV2NavigationState>();

			// Background (always)
			FrameProfiler.Measure("CV2Background.Draw", _backgroundSystem.Draw);

			// Header (always)
			FrameProfiler.Measure("CV2Header.Draw", _headerSystem.Draw);

			if (nav != null && nav.ActiveTab == CustomizationV2TabType.Deck)
			{
				// Deck screen draw order
				FrameProfiler.Measure("DeckV2StatsBar.Draw", _statsBarSystem.Draw);
				FrameProfiler.Measure("DeckV2AvailableGrid.Draw", _availableGridSystem.Draw);
				FrameProfiler.Measure("DeckV2DeckList.Draw", _deckListSystem.Draw);
				FrameProfiler.Measure("DeckV2InvalidDialog.Draw", _invalidDeckDialogSystem.Draw);
			}
			else if (nav != null && nav.ActiveTab == CustomizationV2TabType.Loadout)
			{
				// Loadout screen draw order
				// FrameProfiler.Measure("WheelRings.Draw", _wheelRingDisplaySystem.Draw);
				FrameProfiler.Measure("WheelSegments.Draw", _wheelSegmentDisplaySystem.Draw);
				FrameProfiler.Measure("ConnectorChevron.Draw", _connectorChevronDisplaySystem.Draw);
				FrameProfiler.Measure("CenterHub.Draw", _centerHubDisplaySystem.Draw);
				FrameProfiler.Measure("BrowseArrows.Draw", _browseArrowDisplaySystem.Draw);
				FrameProfiler.Measure("EquippedPanel.Draw", _equippedPanelDisplaySystem.Draw);
			}
		}
	}
}
