using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Factories;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace Crusaders30XX.ECS.Systems
{
	public class BattleSceneSystem : Core.System
	{
		private readonly SystemManager _systemManager;
		private readonly World _world;
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly ContentManager _content;
		private readonly SpriteFont _font;

		// Battle systems (logic and draw). Only present while in Battle
		private InputSystem _inputSystem;
		private DeckManagementSystem _deckManagementSystem;
		private RenderingSystem _renderingSystem;
		private CardDisplaySystem _cardDisplaySystem;
		private HandDisplaySystem _handDisplaySystem;
		private CardHighlightSystem _cardHighlightSystem;
		private BattleBackgroundSystem _battleBackgroundSystem;
		private DebugCommandSystem _debugCommandSystem;
		private DrawPileDisplaySystem _drawPileDisplaySystem;
		private DiscardPileDisplaySystem _discardPileDisplaySystem;
		private CardListModalSystem _cardListModalSystem;
		private PlayerDisplaySystem _playerDisplaySystem;
		private PlayerWispParticleSystem _playerWispParticleSystem;
		private PlayerAnimationSystem _playerAnimationSystem;
		private CathedralLightingSystem _cathedralLightingSystem;
		private DesertBackgroundEffectSystem _desertBackgroundEffectSystem;
		private ProfilerSystem _profilerSystem;
		private CourageDisplaySystem _courageDisplaySystem;
		private ActionPointDisplaySystem _actionPointDisplaySystem;
		private TemperanceDisplaySystem _temperanceDisplaySystem;
		private StoredBlockDisplaySystem _storedBlockDisplaySystem;
		private CourageManagerSystem _courageManagerSystem;
		private ActionPointManagementSystem _actionPointManagementSystem;
		private TemperanceManagerSystem _temperanceManagerSystem;
		private TooltipDisplaySystem _tooltipDisplaySystem;
		private HPDisplaySystem _hpDisplaySystem;
		private CardVisualSettingsDebugSystem _cardVisualSettingsDebugSystem;
		private HpManagementSystem _hpManagementSystem;
		private BattlePhaseSystem _battlePhaseSystem;
		private BattlePhaseDisplaySystem _battlePhaseDisplaySystem;
		private EnemyDisplaySystem _enemyDisplaySystem;
		private EnemyIntentPipsSystem _enemyIntentPipsSystem;
		private EnemyAttackDisplaySystem _enemyAttackDisplaySystem;
		private CardPlayedAnimationSystem _cardPlayedAnimationSystem;
		private AssignedBlockCardsDisplaySystem _assignedBlockCardsDisplaySystem;
		private EnemyIntentPlanningSystem _enemyIntentPlanningSystem;
		private EnemyAttackProgressManagementSystem _enemyAttackProgressManagementSystem;
		private StunnedOverlaySystem _stunnedOverlaySystem;
		private AttackResolutionSystem _attackResolutionSystem;
		private HandBlockInteractionSystem _handBlockInteractionSystem;
		private StoredBlockManagementSystem _storedBlockManagementSystem;
		private CardZoneSystem _cardZoneSystem;
		private AssignedBlocksToDiscardSystem _assignedBlocksToDiscardSystem;
		private EnemyDamageManagerSystem _enemyDamageManagerSystem;
		private EventQueueSystem _eventQueueSystem;
		private CardPlaySystem _cardPlaySystem;
		private EndTurnDisplaySystem _endTurnDisplaySystem;
		private DrawHandSystem _battlePhaseDrawSystem;
		private PhaseCoordinatorSystem _phaseCoordinatorSystem;
		private EnemyStunAutoSkipSystem _enemyStunAutoSkipSystem;
		private PayCostOverlaySystem _payCostOverlaySystem;
		private CantPlayCardMessageSystem _cantPlayCardMessageSystem;
		private GameOverOverlayDisplaySystem _gameOverOverlayDisplaySystem;
		private WeaponManagementSystem _weaponManagementSystem;
		private EquipmentManagerSystem _equipmentManagerSystem;
		private EquipmentDisplaySystem _equipmentDisplaySystem;
		private EquipmentUsedManagementSystem _equipmentUsedManagementSystem;
		private HighlightSettingsSystem _equipmentHighlightSettingsDebugSystem;
		private EquipmentBlockInteractionSystem _equipmentBlockInteractionSystem;

		public BattleSceneSystem(EntityManager em, SystemManager sm, World world, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, ContentManager content, SpriteFont font) : base(em)
		{
			_systemManager = sm;
			_world = world;
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
			_content = content;
			_font = font;
			EventManager.Subscribe<StartBattleRequested>(_ => StartBattle());
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<SceneState>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			// no-op per-frame
		}

		public void Draw()
		{
			var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
			if (scene == null || scene.Current != SceneId.Battle) return;
			// Draw in the same order as previously in Game1
			_battleBackgroundSystem.Draw();
			_cathedralLightingSystem.Draw();
			_desertBackgroundEffectSystem.Draw();
			_renderingSystem.Draw();
			_playerDisplaySystem.Draw();
			_enemyDisplaySystem.Draw();
			_enemyIntentPipsSystem.Draw();
			_enemyAttackDisplaySystem.Draw();
			_stunnedOverlaySystem.Draw();
			_endTurnDisplaySystem.Draw();
			_assignedBlockCardsDisplaySystem.Draw();
			_playerWispParticleSystem.Draw();
			_battlePhaseDisplaySystem.Draw();
			_courageDisplaySystem.Draw();
			_temperanceDisplaySystem.Draw();
			_actionPointDisplaySystem.Draw();
			_storedBlockDisplaySystem.Draw();
			_hpDisplaySystem.Draw();
			_payCostOverlaySystem.DrawBackdrop();
			_handDisplaySystem.DrawHand();
			_cardPlayedAnimationSystem.Draw();
			_equipmentDisplaySystem.Draw();
			_drawPileDisplaySystem.Draw();
			_discardPileDisplaySystem.Draw();
			_cardListModalSystem.Draw();
			_tooltipDisplaySystem.Draw();
			_profilerSystem.Draw();
			_payCostOverlaySystem.DrawForeground();
			_cantPlayCardMessageSystem.Draw();
			_gameOverOverlayDisplaySystem?.Draw();
		}

		private void StartBattle()
		{
			var sceneEntity = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault();
			if (sceneEntity == null)
			{
				sceneEntity = EntityManager.CreateEntity("SceneState");
				EntityManager.AddComponent(sceneEntity, new SceneState { Current = SceneId.Menu });
			}
			var scene = sceneEntity.GetComponent<SceneState>();
			if (scene.Current == SceneId.Battle) return;

			EntityFactory.CreateGameState(_world);
			EntityFactory.CreatePlayer(_world);
			var deckEntity = EntityFactory.CreateDeck(_world);
			var demoHand = EntityFactory.CreateDemoHand(_world);
			var deck = deckEntity.GetComponent<Deck>();
			if (deck != null)
			{
				deck.Cards.AddRange(demoHand);
				deck.DrawPile.AddRange(demoHand);
			}
			AddBattleSystems();
			// Spawn selected enemy (index 0) if any queued; otherwise default handled by CreateGameState
			var queued = _world.EntityManager.GetEntitiesWithComponent<QueuedEnemies>().FirstOrDefault()?.GetComponent<QueuedEnemies>();
			if (queued != null && queued.EnemyIds.Count > 0)
			{
				// Remove any default enemy created during CreateGameState
				var existingEnemies = _world.EntityManager.GetEntitiesWithComponent<Enemy>().ToList();
				foreach (var e in existingEnemies)
				{
					_world.EntityManager.DestroyEntity(e.Id);
				}
				var id0 = queued.EnemyIds[0];
				EntityFactory.CreateEnemyFromId(_world, id0);
			}
			EventManager.Publish(new ChangeBattleLocationEvent { Location = BattleLocation.Desert });
			EventManager.Publish(new DeckShuffleEvent { });

			scene.Current = SceneId.Battle;
		}

		private void AddBattleSystems()
		{
			// Construct
			_inputSystem = new InputSystem(_world.EntityManager);
			_deckManagementSystem = new DeckManagementSystem(_world.EntityManager);
			_cardHighlightSystem = new CardHighlightSystem(_world.EntityManager, _graphicsDevice, _spriteBatch);
			_battleBackgroundSystem = new BattleBackgroundSystem(_world.EntityManager, _graphicsDevice, _spriteBatch, _content);
			_renderingSystem = new RenderingSystem(_world.EntityManager, _spriteBatch, _graphicsDevice);
			_cardDisplaySystem = new CardDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch, _font, _content);
			_handDisplaySystem = new HandDisplaySystem(_world.EntityManager, _graphicsDevice);
			_cardZoneSystem = new CardZoneSystem(_world.EntityManager);
			_debugCommandSystem = new DebugCommandSystem(_world.EntityManager);
			_drawPileDisplaySystem = new DrawPileDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch, _font);
			_discardPileDisplaySystem = new DiscardPileDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch, _font);
			_cardListModalSystem = new CardListModalSystem(_world.EntityManager, _graphicsDevice, _spriteBatch, _font);
			var crusaderTexture = _content.Load<Texture2D>("Crusader");
			_playerDisplaySystem = new PlayerDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch, crusaderTexture);
			_cathedralLightingSystem = new CathedralLightingSystem(_world.EntityManager, _graphicsDevice, _spriteBatch);
			_desertBackgroundEffectSystem = new DesertBackgroundEffectSystem(_world.EntityManager, _graphicsDevice, _spriteBatch);
			_playerWispParticleSystem = new PlayerWispParticleSystem(_world.EntityManager, _graphicsDevice, _spriteBatch);
			_playerAnimationSystem = new PlayerAnimationSystem(_world.EntityManager);
			_courageDisplaySystem = new CourageDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch, _font);
			_actionPointDisplaySystem = new ActionPointDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch);
			_temperanceDisplaySystem = new TemperanceDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch, _font);
			_storedBlockDisplaySystem = new StoredBlockDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch, _font);
			_courageManagerSystem = new CourageManagerSystem(_world.EntityManager);
			_actionPointManagementSystem = new ActionPointManagementSystem(_world.EntityManager);
			_temperanceManagerSystem = new TemperanceManagerSystem(_world.EntityManager);
			_tooltipDisplaySystem = new TooltipDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch, _font);
			_hpDisplaySystem = new HPDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch, _font);
			_cardVisualSettingsDebugSystem = new CardVisualSettingsDebugSystem(_world.EntityManager);
			_profilerSystem = new ProfilerSystem(_world.EntityManager, _graphicsDevice, _spriteBatch, _font);
			_hpManagementSystem = new HpManagementSystem(_world.EntityManager);
			_eventQueueSystem = new EventQueueSystem(_world.EntityManager);
			_battlePhaseSystem = new BattlePhaseSystem(_world.EntityManager);
			_battlePhaseDisplaySystem = new BattlePhaseDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch, _font);
			_enemyDisplaySystem = new EnemyDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch, _content);
			_enemyIntentPipsSystem = new EnemyIntentPipsSystem(_world.EntityManager, _graphicsDevice, _spriteBatch);
			_enemyAttackDisplaySystem = new EnemyAttackDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch, _font);
			_cardPlayedAnimationSystem = new CardPlayedAnimationSystem(_world.EntityManager, _graphicsDevice, _spriteBatch);
			_endTurnDisplaySystem = new EndTurnDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch, _font);
			_assignedBlockCardsDisplaySystem = new AssignedBlockCardsDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch, _font, _content);
			_payCostOverlaySystem = new PayCostOverlaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch, _font);
			_cantPlayCardMessageSystem = new CantPlayCardMessageSystem(_world.EntityManager, _graphicsDevice, _spriteBatch, _font);
			_gameOverOverlayDisplaySystem = new GameOverOverlayDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch, _font);
			_enemyIntentPlanningSystem = new EnemyIntentPlanningSystem(_world.EntityManager);
			_enemyAttackProgressManagementSystem = new EnemyAttackProgressManagementSystem(_world.EntityManager);
			_stunnedOverlaySystem = new StunnedOverlaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch, _font);
			_attackResolutionSystem = new AttackResolutionSystem(_world.EntityManager);
			_handBlockInteractionSystem = new HandBlockInteractionSystem(_world.EntityManager);
			_storedBlockManagementSystem = new StoredBlockManagementSystem(_world.EntityManager);
			_assignedBlocksToDiscardSystem = new AssignedBlocksToDiscardSystem(_world.EntityManager, _graphicsDevice);
			_enemyDamageManagerSystem = new EnemyDamageManagerSystem(_world.EntityManager);
			_cardPlaySystem = new CardPlaySystem(_world.EntityManager);
			_battlePhaseDrawSystem = new DrawHandSystem(_world.EntityManager);
			_phaseCoordinatorSystem = new PhaseCoordinatorSystem(_world.EntityManager);
			_enemyStunAutoSkipSystem = new EnemyStunAutoSkipSystem(_world.EntityManager);
			_weaponManagementSystem = new WeaponManagementSystem(_world.EntityManager);
			_equipmentManagerSystem = new EquipmentManagerSystem(_world.EntityManager);
			_equipmentDisplaySystem = new EquipmentDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch, _content, _font);
			_equipmentUsedManagementSystem = new EquipmentUsedManagementSystem(_world.EntityManager);
			_equipmentHighlightSettingsDebugSystem = new HighlightSettingsSystem(_world.EntityManager);
			_equipmentBlockInteractionSystem = new EquipmentBlockInteractionSystem(_world.EntityManager);
			_cardZoneSystem = new CardZoneSystem(_world.EntityManager);

			// Register
			_world.AddSystem(_inputSystem);
			_world.AddSystem(_deckManagementSystem);
			_world.AddSystem(_enemyStunAutoSkipSystem);
			_world.AddSystem(_renderingSystem);
			_world.AddSystem(_cardDisplaySystem);
			_world.AddSystem(_handDisplaySystem);
			_world.AddSystem(_cardZoneSystem);
			_world.AddSystem(_handBlockInteractionSystem);
			_world.AddSystem(_eventQueueSystem);
			_world.AddSystem(_debugCommandSystem);
			_world.AddSystem(_drawPileDisplaySystem);
			_world.AddSystem(_discardPileDisplaySystem);
			_world.AddSystem(_cardListModalSystem);
			_world.AddSystem(_playerDisplaySystem);
			_world.AddSystem(_cathedralLightingSystem);
			_world.AddSystem(_desertBackgroundEffectSystem);
			_world.AddSystem(_playerWispParticleSystem);
			_world.AddSystem(_playerAnimationSystem);
			_world.AddSystem(_tooltipDisplaySystem);
			_world.AddSystem(_courageDisplaySystem);
			_world.AddSystem(_temperanceDisplaySystem);
			_world.AddSystem(_actionPointDisplaySystem);
			_world.AddSystem(_storedBlockDisplaySystem);
			_world.AddSystem(_courageManagerSystem);
			_world.AddSystem(_temperanceManagerSystem);
			_world.AddSystem(_actionPointManagementSystem);
			_world.AddSystem(_profilerSystem);
			_world.AddSystem(_battleBackgroundSystem);
			_world.AddSystem(_hpDisplaySystem);
			_world.AddSystem(_cardVisualSettingsDebugSystem);
			_world.AddSystem(_hpManagementSystem);
			_world.AddSystem(_battlePhaseSystem);
			_world.AddSystem(_battlePhaseDisplaySystem);
			_world.AddSystem(_enemyDisplaySystem);
			_world.AddSystem(_enemyIntentPipsSystem);
			_world.AddSystem(_enemyIntentPlanningSystem);
			_world.AddSystem(_enemyAttackProgressManagementSystem);
			_world.AddSystem(_stunnedOverlaySystem);
			_world.AddSystem(_attackResolutionSystem);
			_world.AddSystem(_enemyAttackDisplaySystem);
			_world.AddSystem(_cardPlayedAnimationSystem);
			_world.AddSystem(_endTurnDisplaySystem);
			_world.AddSystem(_assignedBlockCardsDisplaySystem);
			_world.AddSystem(_assignedBlocksToDiscardSystem);
			_world.AddSystem(_storedBlockManagementSystem);
			_world.AddSystem(_enemyDamageManagerSystem);
			_world.AddSystem(_cardPlaySystem);
			_world.AddSystem(_battlePhaseDrawSystem);
			_world.AddSystem(_phaseCoordinatorSystem);
			_world.AddSystem(_weaponManagementSystem);
			_world.AddSystem(_equipmentManagerSystem);
			_world.AddSystem(_equipmentDisplaySystem);
			_world.AddSystem(_equipmentUsedManagementSystem);
			_world.AddSystem(_equipmentHighlightSettingsDebugSystem);
			_world.AddSystem(_equipmentBlockInteractionSystem);
			_world.AddSystem(_payCostOverlaySystem);
			_world.AddSystem(_cardHighlightSystem);
			_world.AddSystem(_cantPlayCardMessageSystem);
			_world.AddSystem(_gameOverOverlayDisplaySystem);
		}

	}
}


