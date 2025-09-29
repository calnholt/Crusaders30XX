using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Factories;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Crusaders30XX.Diagnostics;
using System;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("Battle Scene System")]
	public class BattleSceneSystem : Core.System
	{
		private readonly SystemManager _systemManager;
		private readonly World _world;
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly ContentManager _content;
		private readonly SpriteFont _font;
		private bool _isFirstLoad = true;

		// Battle systems (logic and draw). Only present while in Battle
	
		private DeckManagementSystem _deckManagementSystem;

		private HandDisplaySystem _handDisplaySystem;
		private CardHighlightSystem _cardHighlightSystem;
		private BattleBackgroundSystem _battleBackgroundSystem;
		private DebugCommandSystem _debugCommandSystem;
		private DrawPileDisplaySystem _drawPileDisplaySystem;
		private DiscardPileDisplaySystem _discardPileDisplaySystem;
		private CardListModalSystem _cardListModalSystem;
		private PlayerDisplaySystem _playerDisplaySystem;
		private PlayerWispParticleSystem _playerWispParticleSystem;
		private PlayerTemperanceActivationDisplaySystem _playerTemperanceActivationDisplaySystem;
		private PlayerAnimationSystem _playerAnimationSystem;
		private CathedralLightingSystem _cathedralLightingSystem;
		private DesertBackgroundEffectSystem _desertBackgroundEffectSystem;
		private CourageDisplaySystem _courageDisplaySystem;
		private ActionPointDisplaySystem _actionPointDisplaySystem;
		private TemperanceDisplaySystem _temperanceDisplaySystem;
		private CourageManagerSystem _courageManagerSystem;
		private ActionPointManagementSystem _actionPointManagementSystem;
		private TemperanceManagerSystem _temperanceManagerSystem;
		private HPDisplaySystem _hpDisplaySystem;
		private AppliedPassivesDisplaySystem _appliedPassivesDisplaySystem;
		private CardVisualSettingsDebugSystem _cardVisualSettingsDebugSystem;
		private HpManagementSystem _hpManagementSystem;
		private BattlePhaseDisplaySystem _battlePhaseDisplaySystem;
		private EnemyDisplaySystem _enemyDisplaySystem;
		private GuardianAngelDisplaySystem _guardianAngelDisplaySystem;
		private EnemyIntentPipsSystem _enemyIntentPipsSystem;
		private EnemyAttackDisplaySystem _enemyAttackDisplaySystem;
		private AmbushDisplaySystem _ambushDisplaySystem;
		private QueuedEventsDisplaySystem _queuedEventsDisplaySystem;
		private DamageModificationDisplaySystem _damageModificationDisplaySystem;
		private CardPlayedAnimationSystem _cardPlayedAnimationSystem;
		private AssignedBlockCardsDisplaySystem _assignedBlockCardsDisplaySystem;
		private EnemyIntentPlanningSystem _enemyIntentPlanningSystem;
		private EnemyAttackProgressManagementSystem _enemyAttackProgressManagementSystem;
		private MarkedForSpecificDiscardSystem _markedForSpecificDiscardSystem;
		private StunnedOverlaySystem _stunnedOverlaySystem;
		private AttackResolutionSystem _attackResolutionSystem;
		private HandBlockInteractionSystem _handBlockInteractionSystem;
		private CardZoneSystem _cardZoneSystem;
		private AssignedBlocksToDiscardSystem _assignedBlocksToDiscardSystem;
		private EnemyDamageManagerSystem _enemyDamageManagerSystem;
		private EventQueueSystem _eventQueueSystem;
		private CardPlaySystem _cardPlaySystem;
		private EndTurnDisplaySystem _endTurnDisplaySystem;
		private DrawHandSystem _battlePhaseDrawSystem;
		private PhaseCoordinatorSystem _phaseCoordinatorSystem;
		private PayCostOverlaySystem _payCostOverlaySystem;
		private CantPlayCardMessageSystem _cantPlayCardMessageSystem;
		private GameOverOverlayDisplaySystem _gameOverOverlayDisplaySystem;
		private WeaponManagementSystem _weaponManagementSystem;
		private EquipmentManagerSystem _equipmentManagerSystem;
		private MedalManagerSystem _medalManagerSystem;
		private MedalDisplaySystem _medalDisplaySystem;
		private EquippedWeaponDisplaySystem _equippedWeaponDisplaySystem;
		private EquipmentDisplaySystem _equipmentDisplaySystem;
		private EquipmentUsedManagementSystem _equipmentUsedManagementSystem;
		private HighlightSettingsSystem _equipmentHighlightSettingsDebugSystem;
		private EquipmentBlockInteractionSystem _equipmentBlockInteractionSystem;
	private AppliedPassivesManagementSystem _appliedPassivesManagementSystem;
	private BattleStateInfoManagementSystem _battleStateInfoManagementSystem;
	private DiscardSpecificCardHighlightSystem _discardSpecificCardHighlightSystem;
	private IntimidateManagementSystem _intimidateManagementSystem;
	private IntimidateDisplaySystem _intimidateDisplaySystem;

		public BattleSceneSystem(EntityManager em, SystemManager sm, World world, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, ContentManager content, SpriteFont font) : base(em)
		{
			_systemManager = sm;
			_world = world;
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
			_content = content;
			_font = font;
			EventManager.Subscribe<StartBattleRequested>(_ => StartBattle());
			EventManager.Subscribe<LoadSceneEvent>(_ => {
				if (_.Scene != SceneId.Battle) return;
				if (_isFirstLoad) CreateBattleSceneEntities();
				StartBattle();
			});
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
			FrameProfiler.Measure("BattleBackgroundSystem.Draw", _battleBackgroundSystem.Draw);
			FrameProfiler.Measure("CathedralLightingSystem.Draw", _cathedralLightingSystem.Draw);
			FrameProfiler.Measure("DesertBackgroundEffectSystem.Draw", _desertBackgroundEffectSystem.Draw);
			FrameProfiler.Measure("PlayerDisplaySystem.Draw", _playerDisplaySystem.Draw);
			FrameProfiler.Measure("GuardianAngelDisplaySystem.Draw", _guardianAngelDisplaySystem.Draw);
			FrameProfiler.Measure("EnemyDisplaySystem.Draw", _enemyDisplaySystem.Draw);
			FrameProfiler.Measure("EnemyIntentPipsSystem.Draw", _enemyIntentPipsSystem.Draw);
			FrameProfiler.Measure("EnemyAttackDisplaySystem.Draw", _enemyAttackDisplaySystem.Draw);
			FrameProfiler.Measure("AmbushDisplaySystem.Draw", _ambushDisplaySystem.Draw);
			FrameProfiler.Measure("QueuedEventsDisplaySystem.Draw", _queuedEventsDisplaySystem.Draw);
			FrameProfiler.Measure("DamageModificationDisplaySystem.Draw", _damageModificationDisplaySystem.Draw);
			FrameProfiler.Measure("StunnedOverlaySystem.Draw", _stunnedOverlaySystem.Draw);
			FrameProfiler.Measure("EndTurnDisplaySystem.Draw", _endTurnDisplaySystem.Draw);
			FrameProfiler.Measure("AssignedBlockCardsDisplaySystem.Draw", _assignedBlockCardsDisplaySystem.Draw);
			FrameProfiler.Measure("PlayerWispParticleSystem.Draw", _playerWispParticleSystem.Draw);
			FrameProfiler.Measure("PlayerTemperanceActivationDisplaySystem.Draw", _playerTemperanceActivationDisplaySystem.Draw);
			FrameProfiler.Measure("BattlePhaseDisplaySystem.Draw", _battlePhaseDisplaySystem.Draw);
			FrameProfiler.Measure("CourageDisplaySystem.Draw", _courageDisplaySystem.Draw);
			FrameProfiler.Measure("TemperanceDisplaySystem.Draw", _temperanceDisplaySystem.Draw);
			FrameProfiler.Measure("ActionPointDisplaySystem.Draw", _actionPointDisplaySystem.Draw);
			FrameProfiler.Measure("HPDisplaySystem.Draw", _hpDisplaySystem.Draw);
			FrameProfiler.Measure("AppliedPassivesDisplaySystem.Draw", _appliedPassivesDisplaySystem.Draw);
			FrameProfiler.Measure("PayCostOverlaySystem.DrawBackdrop", _payCostOverlaySystem.DrawBackdrop);
			FrameProfiler.Measure("HandDisplaySystem.DrawHand", _handDisplaySystem.DrawHand);
			FrameProfiler.Measure("CardPlayedAnimationSystem.Draw", _cardPlayedAnimationSystem.Draw);
			FrameProfiler.Measure("EquipmentDisplaySystem.Draw", _equipmentDisplaySystem.Draw);
			FrameProfiler.Measure("EquippedWeaponDisplaySystem.Draw", _equippedWeaponDisplaySystem.Draw);
			FrameProfiler.Measure("MedalDisplaySystem.Draw", _medalDisplaySystem.Draw);
			FrameProfiler.Measure("DrawPileDisplaySystem.Draw", _drawPileDisplaySystem.Draw);
			FrameProfiler.Measure("DiscardPileDisplaySystem.Draw", _discardPileDisplaySystem.Draw);
			FrameProfiler.Measure("CardListModalSystem.Draw", _cardListModalSystem.Draw);
		FrameProfiler.Measure("PayCostOverlaySystem.DrawForeground", _payCostOverlaySystem.DrawForeground);
		FrameProfiler.Measure("CantPlayCardMessageSystem.Draw", _cantPlayCardMessageSystem.Draw);
		FrameProfiler.Measure("DiscardSpecificCardHighlightSystem.Draw", _discardSpecificCardHighlightSystem.Draw);
		FrameProfiler.Measure("IntimidateDisplaySystem.Draw", _intimidateDisplaySystem.Draw);
		if (_gameOverOverlayDisplaySystem != null) FrameProfiler.Measure("GameOverOverlayDisplaySystem.Draw", _gameOverOverlayDisplaySystem.Draw);
		}

		private void CreateBattleSceneEntities() {
			var sceneEntity = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault();
			if (sceneEntity == null)
			{
				sceneEntity = EntityManager.CreateEntity("SceneState");
				EntityManager.AddComponent(sceneEntity, new SceneState { Current = SceneId.Menu });
			}
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
			EventManager.Publish(new ChangeBattleLocationEvent { Location = BattleLocation.Desert });
			EventManager.Publish(new DeckShuffleEvent { });
				_isFirstLoad = false;
		}

		private void ResetEntitiesAfterBattle() {
			var player = EntityManager.GetEntity("Player");
			player.GetComponent<HP>().Current = 30;
			player.GetComponent<HP>().Max = 30;
			EventManager.Publish(new SetTemperanceEvent{ Amount = 0 });
			var equipmentUsedState = player.GetComponent<EquipmentUsedState>();
			equipmentUsedState.ActivatedThisTurn.Clear();
			equipmentUsedState.DestroyedEquipmentIds.Clear();
			equipmentUsedState.UsesByEquipmentId.Clear();
			var queuedEntity = EntityManager.GetEntity("QueuedEvents");
			var queued = queuedEntity.GetComponent<QueuedEvents>();
			queued.CurrentIndex = -1;
			queued.Events.Clear();
		}

		public void StartBattle() 
		{
			var queuedEntity = EntityManager.GetEntity("QueuedEvents");
			var queued = queuedEntity.GetComponent<QueuedEvents>();
			if (queued.CurrentIndex == 0) 
			{
				EventManager.Publish(new ChangeMusicTrack { Track = MusicTrack.Battle });
			}
			// all battles are done, go to menu
			if (queued.Events.Count == queued.CurrentIndex + 1)
			{
				var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault().GetComponent<SceneState>();
				ResetEntitiesAfterBattle();
				scene.Current = SceneId.Menu;
				return;
			};
			EventManager.Publish(new SetCourageEvent{ Amount = 0 });
			// TODO: should handle through events rather than directly but im lazy right now
			var player = EntityManager.GetEntity("Player");
			var battleStateInfo = player.GetComponent<BattleStateInfo>();
			battleStateInfo.EquipmentTriggeredThisBattle.Clear();
			// Initialize/Reset per-battle applied passives on player
			var playerPassives = player.GetComponent<AppliedPassives>();
			if (playerPassives == null)
			{
				_world.AddComponent(player, new AppliedPassives());
			}
			else
			{
				playerPassives.Passives.Clear();
			}
			EntityManager.DestroyEntity("Enemy");
			Console.WriteLine($"queued.Events.Count: {queued.Events.Count}, queued.CurrentIndex: {queued.CurrentIndex}");
			var nextEnemy = EntityFactory.CreateEnemyFromId(_world, queued.Events[++queued.CurrentIndex].EventId);
			EventManager.Publish(new ResetDeckEvent { });
			var phaseState = EntityManager.GetEntity("PhaseState").GetComponent<PhaseState>();
			phaseState.TurnNumber = 0;
			EntityManager.GetEntity("SceneState").GetComponent<SceneState>().Current = SceneId.Battle;
			EventQueueBridge.EnqueueTriggerAction("BattleSceneSystem.StartBattle", () => {
				EventManager.Publish(new ChangeBattlePhaseEvent { Current = SubPhase.StartBattle, Previous = SubPhase.StartBattle });
				EventQueue.EnqueueRule(new EventQueueBridge.QueuedPublish<ChangeBattlePhaseEvent>(
					"Rule.ChangePhase.EnemyStart",
					new ChangeBattlePhaseEvent { Current = SubPhase.EnemyStart }
				));
				EventQueue.EnqueueRule(new EventQueueBridge.QueuedPublish<ChangeBattlePhaseEvent>(
					"Rule.ChangePhase.PreBlock",
					new ChangeBattlePhaseEvent { Current = SubPhase.PreBlock }
				));
				EventQueue.EnqueueRule(new EventQueueBridge.QueuedPublish<ChangeBattlePhaseEvent>(
					"Rule.ChangePhase.Block",
					new ChangeBattlePhaseEvent { Current = SubPhase.Block }
				));
			}, 2f);
		}
		
		[DebugAction("Next Battle")]
		public void Debug_NextBattle() 
		{
			EventManager.Publish(new StartBattleRequested {  });
		}
		private void AddBattleSystems()
		{
			// Construct
			_deckManagementSystem = new DeckManagementSystem(_world.EntityManager);
			_cardHighlightSystem = new CardHighlightSystem(_world.EntityManager, _graphicsDevice, _spriteBatch);
			_battleBackgroundSystem = new BattleBackgroundSystem(_world.EntityManager, _graphicsDevice, _spriteBatch, _content);
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
			_playerTemperanceActivationDisplaySystem = new PlayerTemperanceActivationDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch, crusaderTexture);
			_playerAnimationSystem = new PlayerAnimationSystem(_world.EntityManager);
			_courageDisplaySystem = new CourageDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch, _font);
			_actionPointDisplaySystem = new ActionPointDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch, _font);
			_temperanceDisplaySystem = new TemperanceDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch, _font);
			_courageManagerSystem = new CourageManagerSystem(_world.EntityManager);
			_actionPointManagementSystem = new ActionPointManagementSystem(_world.EntityManager);
			_temperanceManagerSystem = new TemperanceManagerSystem(_world.EntityManager);
			_hpDisplaySystem = new HPDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch, _font);
			_appliedPassivesDisplaySystem = new AppliedPassivesDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch, _font);
			_cardVisualSettingsDebugSystem = new CardVisualSettingsDebugSystem(_world.EntityManager);
			_hpManagementSystem = new HpManagementSystem(_world.EntityManager);
			_eventQueueSystem = new EventQueueSystem(_world.EntityManager);
			_battlePhaseDisplaySystem = new BattlePhaseDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch, _font);
			_enemyDisplaySystem = new EnemyDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch, _content);
			_guardianAngelDisplaySystem = new GuardianAngelDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch, _content);
			_enemyIntentPipsSystem = new EnemyIntentPipsSystem(_world.EntityManager, _graphicsDevice, _spriteBatch);
			_enemyAttackDisplaySystem = new EnemyAttackDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch, _font);
			_ambushDisplaySystem = new AmbushDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch, _font);
			_queuedEventsDisplaySystem = new QueuedEventsDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch, _content);
			_damageModificationDisplaySystem = new DamageModificationDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch, _font);
			_cardPlayedAnimationSystem = new CardPlayedAnimationSystem(_world.EntityManager, _graphicsDevice, _spriteBatch);
			_endTurnDisplaySystem = new EndTurnDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch, _font);
			_assignedBlockCardsDisplaySystem = new AssignedBlockCardsDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch, _font, _content);
			_payCostOverlaySystem = new PayCostOverlaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch, _font);
			_cantPlayCardMessageSystem = new CantPlayCardMessageSystem(_world.EntityManager, _graphicsDevice, _spriteBatch, _font);
			_gameOverOverlayDisplaySystem = new GameOverOverlayDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch, _font);
			_enemyIntentPlanningSystem = new EnemyIntentPlanningSystem(_world.EntityManager);
			_enemyAttackProgressManagementSystem = new EnemyAttackProgressManagementSystem(_world.EntityManager);
			_markedForSpecificDiscardSystem = new MarkedForSpecificDiscardSystem(_world.EntityManager);
			_stunnedOverlaySystem = new StunnedOverlaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch, _font);
			_attackResolutionSystem = new AttackResolutionSystem(_world.EntityManager);
			_handBlockInteractionSystem = new HandBlockInteractionSystem(_world.EntityManager);
			_assignedBlocksToDiscardSystem = new AssignedBlocksToDiscardSystem(_world.EntityManager, _graphicsDevice);
			_enemyDamageManagerSystem = new EnemyDamageManagerSystem(_world.EntityManager);
			_cardPlaySystem = new CardPlaySystem(_world.EntityManager);
			_battlePhaseDrawSystem = new DrawHandSystem(_world.EntityManager);
			_phaseCoordinatorSystem = new PhaseCoordinatorSystem(_world.EntityManager);
			_weaponManagementSystem = new WeaponManagementSystem(_world.EntityManager);
			_equipmentManagerSystem = new EquipmentManagerSystem(_world.EntityManager);
			_medalManagerSystem = new MedalManagerSystem(_world.EntityManager);
			_equipmentDisplaySystem = new EquipmentDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch, _content, _font);
			_equippedWeaponDisplaySystem = new EquippedWeaponDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch, _content);
			_medalDisplaySystem = new MedalDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch, _content, _font);
			_equipmentUsedManagementSystem = new EquipmentUsedManagementSystem(_world.EntityManager);
			_equipmentHighlightSettingsDebugSystem = new HighlightSettingsSystem(_world.EntityManager);
			_equipmentBlockInteractionSystem = new EquipmentBlockInteractionSystem(_world.EntityManager);
		_appliedPassivesManagementSystem = new AppliedPassivesManagementSystem(_world.EntityManager);
		_battleStateInfoManagementSystem = new BattleStateInfoManagementSystem(_world.EntityManager);
		_discardSpecificCardHighlightSystem = new DiscardSpecificCardHighlightSystem(_world.EntityManager, _graphicsDevice, _spriteBatch);
		_cardZoneSystem = new CardZoneSystem(_world.EntityManager);
		_intimidateManagementSystem = new IntimidateManagementSystem(_world.EntityManager);
		_intimidateDisplaySystem = new IntimidateDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch, _font);

			// Register
			_world.AddSystem(_deckManagementSystem);
			_world.AddSystem(_handDisplaySystem);
			_world.AddSystem(_cardZoneSystem);
			_world.AddSystem(_handBlockInteractionSystem);
			_world.AddSystem(_eventQueueSystem);
			_world.AddSystem(_debugCommandSystem);
			_world.AddSystem(_drawPileDisplaySystem);
			_world.AddSystem(_discardPileDisplaySystem);
			_world.AddSystem(_cardListModalSystem);
			_world.AddSystem(_playerDisplaySystem);
			_world.AddSystem(_guardianAngelDisplaySystem);
			_world.AddSystem(_cathedralLightingSystem);
			_world.AddSystem(_desertBackgroundEffectSystem);
			_world.AddSystem(_playerWispParticleSystem);
			_world.AddSystem(_playerAnimationSystem);
			_world.AddSystem(_playerTemperanceActivationDisplaySystem);
			_world.AddSystem(_courageDisplaySystem);
			_world.AddSystem(_temperanceDisplaySystem);
			_world.AddSystem(_actionPointDisplaySystem);
			_world.AddSystem(_courageManagerSystem);
			_world.AddSystem(_temperanceManagerSystem);
			_world.AddSystem(_actionPointManagementSystem);
			_world.AddSystem(_battleBackgroundSystem);
			_world.AddSystem(_hpDisplaySystem);
			_world.AddSystem(_appliedPassivesDisplaySystem);
			_world.AddSystem(_cardVisualSettingsDebugSystem);
			_world.AddSystem(_hpManagementSystem);
			_world.AddSystem(_battlePhaseDisplaySystem);
			_world.AddSystem(_enemyDisplaySystem);
			_world.AddSystem(_enemyIntentPipsSystem);
			_world.AddSystem(_enemyIntentPlanningSystem);
			_world.AddSystem(_enemyAttackProgressManagementSystem);
			_world.AddSystem(_markedForSpecificDiscardSystem);
			_world.AddSystem(_stunnedOverlaySystem);
			_world.AddSystem(_attackResolutionSystem);
			_world.AddSystem(_enemyAttackDisplaySystem);
			_world.AddSystem(_ambushDisplaySystem);
			_world.AddSystem(_queuedEventsDisplaySystem);
			_world.AddSystem(_damageModificationDisplaySystem);
			_world.AddSystem(_cardPlayedAnimationSystem);
			_world.AddSystem(_endTurnDisplaySystem);
			_world.AddSystem(_assignedBlockCardsDisplaySystem);
			_world.AddSystem(_assignedBlocksToDiscardSystem);
			_world.AddSystem(_enemyDamageManagerSystem);
			_world.AddSystem(_cardPlaySystem);
			_world.AddSystem(_battlePhaseDrawSystem);
			_world.AddSystem(_phaseCoordinatorSystem);
			_world.AddSystem(_weaponManagementSystem);
			_world.AddSystem(_equipmentManagerSystem);
			_world.AddSystem(_medalManagerSystem);
			_world.AddSystem(_equipmentDisplaySystem);
			_world.AddSystem(_equippedWeaponDisplaySystem);
			_world.AddSystem(_medalDisplaySystem);
			_world.AddSystem(_equipmentUsedManagementSystem);
			_world.AddSystem(_equipmentHighlightSettingsDebugSystem);
			_world.AddSystem(_equipmentBlockInteractionSystem);
		_world.AddSystem(_appliedPassivesManagementSystem);
		_world.AddSystem(_battleStateInfoManagementSystem);
		_world.AddSystem(_payCostOverlaySystem);
		_world.AddSystem(_cardHighlightSystem);
		_world.AddSystem(_cantPlayCardMessageSystem);
		_world.AddSystem(_gameOverOverlayDisplaySystem);
		_world.AddSystem(_discardSpecificCardHighlightSystem);
		_world.AddSystem(_intimidateManagementSystem);
		_world.AddSystem(_intimidateDisplaySystem);
		}

	}
}


