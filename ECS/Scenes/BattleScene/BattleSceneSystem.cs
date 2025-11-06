using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Factories;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Data.Locations;
using Crusaders30XX.ECS.Data.Save;
using Crusaders30XX.ECS.Services;
 
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
		private bool _firstLoad = true;

		// Battle systems (logic and draw). Only present while in Battle
	
		private DeckManagementSystem _deckManagementSystem;

		private HandDisplaySystem _handDisplaySystem;
		private BattleBackgroundSystem _battleBackgroundSystem;
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
		private PoisonSystem _poisonSystem;
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
		private FrozenCardManagementSystem _frozenCardManagementSystem;
		private FrozenCardDisplaySystem _frozenCardDisplaySystem;
		private UIElementHighlightSystem _uiElementHighlightSystem;
		private QuestRewardModalDisplaySystem _questRewardModalDisplaySystem;
		private TribulationManagerSystem _tribulationManagerSystem;
		private QuestTribulationDisplaySystem _questTribulationDisplaySystem;


		public BattleSceneSystem(EntityManager em, SystemManager sm, World world, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, ContentManager content) : base(em)
		{
			_systemManager = sm;
			_world = world;
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
			_content = content;
			EventManager.Subscribe<StartBattleRequested>(_ => 
			{
				Console.WriteLine("[BattleSceneSystem] StartBattleRequested");
				InitBattle();
			});
			EventManager.Subscribe<LoadSceneEvent>(_ => {
				Console.WriteLine("[BattleSceneSystem] LoadSceneEvent");
				if (_.Scene != SceneId.Battle) return;
				if (EntityManager.GetEntity("Player") == null) 
				{
					Console.WriteLine("[BattleSceneSystem] LoadSceneEvent 2");
					AddBattleSystems();
					// Always create base battle entities (background, player, deck) so the scene renders during dialog
					Console.WriteLine("[BattleSceneSystem] LoadSceneEvent 3 (CreateBattleSceneEntities)");
					CreateBattleSceneEntities();
					// If a dialog is pending for this quest, wait for DialogEnded before starting battle
					bool willShowDialog = EntityManager.GetEntitiesWithComponent<QueuedEvents>().FirstOrDefault()?.GetComponent<PendingQuestDialog>()?.WillShowDialog ?? false;
					if (!willShowDialog)
					{
						InitBattle();
						EnqueueBattleRules(false);
					}
				};
			});
			EventManager.Subscribe<DialogEnded>(_ => 
			{
				Console.WriteLine("[BattleSceneSystem] DialogEnded");
				if (EntityManager.GetEntity("Player") == null) 
				{
					CreateBattleSceneEntities();
				}
				InitBattle();
				EnqueueBattleRules(true);
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
			FrameProfiler.Measure("BattleBackgroundSystem.Draw", _battleBackgroundSystem.Draw);
			FrameProfiler.Measure("CathedralLightingSystem.Draw", _cathedralLightingSystem.Draw);
			FrameProfiler.Measure("DesertBackgroundEffectSystem.Draw", _desertBackgroundEffectSystem.Draw);
			// If there will be dialog to show for the quest, skip drawing battle UI (overlay is drawn globally)
			bool willShowDialog = EntityManager.GetEntitiesWithComponent<QueuedEvents>().FirstOrDefault()?.GetComponent<PendingQuestDialog>()?.WillShowDialog ?? false;
			bool rewardOpen = EntityManager.GetEntitiesWithComponent<QuestRewardOverlayState>().FirstOrDefault()?.GetComponent<QuestRewardOverlayState>()?.IsOpen ?? false;
			if (willShowDialog) return;
			if (rewardOpen)
			{
				FrameProfiler.Measure("QuestRewardModalDisplaySystem.Draw", _questRewardModalDisplaySystem.Draw);
				return;
			}
			FrameProfiler.Measure("PlayerDisplaySystem.Draw", _playerDisplaySystem.Draw);
			FrameProfiler.Measure("GuardianAngelDisplaySystem.Draw", _guardianAngelDisplaySystem.Draw);
			FrameProfiler.Measure("EnemyDisplaySystem.Draw", _enemyDisplaySystem.Draw);
			FrameProfiler.Measure("EnemyIntentPipsSystem.Draw", _enemyIntentPipsSystem.Draw);
			FrameProfiler.Measure("AmbushDisplaySystem.Draw", _ambushDisplaySystem.Draw);
			FrameProfiler.Measure("QueuedEventsDisplaySystem.Draw", _queuedEventsDisplaySystem.Draw);
			FrameProfiler.Measure("DamageModificationDisplaySystem.Draw", _damageModificationDisplaySystem.Draw);
			FrameProfiler.Measure("StunnedOverlaySystem.Draw", _stunnedOverlaySystem.Draw);
			FrameProfiler.Measure("AssignedBlockCardsDisplaySystem.Draw", _assignedBlockCardsDisplaySystem.Draw);
			FrameProfiler.Measure("PlayerWispParticleSystem.Draw", _playerWispParticleSystem.Draw);
			FrameProfiler.Measure("PlayerTemperanceActivationDisplaySystem.Draw", _playerTemperanceActivationDisplaySystem.Draw);
			FrameProfiler.Measure("BattlePhaseDisplaySystem.Draw", _battlePhaseDisplaySystem.Draw);
			FrameProfiler.Measure("CourageDisplaySystem.Draw", _courageDisplaySystem.Draw);
			FrameProfiler.Measure("QuestTribulationDisplaySystem.Draw", _questTribulationDisplaySystem.Draw);
			FrameProfiler.Measure("TemperanceDisplaySystem.Draw", _temperanceDisplaySystem.Draw);
			FrameProfiler.Measure("ActionPointDisplaySystem.Draw", _actionPointDisplaySystem.Draw);
			FrameProfiler.Measure("HPDisplaySystem.Draw", _hpDisplaySystem.Draw);
			FrameProfiler.Measure("AppliedPassivesDisplaySystem.Draw", _appliedPassivesDisplaySystem.Draw);
			FrameProfiler.Measure("PoisonSystem.Draw", _poisonSystem.Draw);
			FrameProfiler.Measure("PayCostOverlaySystem.DrawBackdrop", _payCostOverlaySystem.DrawBackdrop);
			FrameProfiler.Measure("UIElementHighlightSystem.Draw", _uiElementHighlightSystem.Draw);
			FrameProfiler.Measure("EnemyAttackDisplaySystem.Draw", _enemyAttackDisplaySystem.Draw);
			FrameProfiler.Measure("EndTurnDisplaySystem.Draw", _endTurnDisplaySystem.Draw);
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
			FrameProfiler.Measure("QuestRewardModalDisplaySystem.Draw", _questRewardModalDisplaySystem.Draw);
		if (_gameOverOverlayDisplaySystem != null) FrameProfiler.Measure("GameOverOverlayDisplaySystem.Draw", _gameOverOverlayDisplaySystem.Draw);
		}

		private void CreateBattleSceneEntities() {
			var sceneEntity = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault();
			if (sceneEntity == null)
			{
				sceneEntity = EntityManager.CreateEntity("SceneState");
				EntityManager.AddComponent(sceneEntity, new SceneState { Current = SceneId.Internal_QueueEventsMenu });
			}
			EntityFactory.CreateGameState(_world);
			EntityFactory.CreatePlayer(_world);
			var deckEntity = EntityFactory.CreateDeck(_world);
			var deckCards = EntityFactory.CreateDeckFromLoadout(EntityManager);
			var deck = deckEntity.GetComponent<Deck>();
			if (deck != null)
			{
				deck.Cards.AddRange(deckCards);
				deck.DrawPile.AddRange(deckCards);
			}
			// Create tribulations for the current quest
			var queuedEntity = EntityManager.GetEntity("QueuedEvents");
			var queued = queuedEntity.GetComponent<QueuedEvents>();
			if (!string.IsNullOrEmpty(queued.LocationId))
			{
				TribulationQuestService.CreateTribulationsForQuest(EntityManager, queued.LocationId, queued.QuestIndex);
			}
			EventManager.Publish(new ChangeBattleLocationEvent { Location = BattleLocation.Desert });
			EventManager.Publish(new DeckShuffleEvent { });
		}

		public void InitBattle() 
		{
			var deck = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault().GetComponent<Deck>();
			if (deck.Cards.Count == 0)
			{
				var deckCards = EntityFactory.CreateDeckFromLoadout(EntityManager);
				deck.Cards.AddRange(deckCards);
				deck.DrawPile.AddRange(deckCards);
			}
			var queuedEntity = EntityManager.GetEntity("QueuedEvents");
			var queued = queuedEntity.GetComponent<QueuedEvents>();
			EventManager.Publish(new SetCourageEvent{ Amount = 0 });
			// Dialog is now handled globally; do not open here
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
			EntityManager.DestroyEntity("Enemy");
			Console.WriteLine($"queued.Events.Count: {queued.Events.Count}, queued.CurrentIndex: {queued.CurrentIndex}");
			var nextEvent = queued.Events[++queued.CurrentIndex];
			var nextEnemy = EntityFactory.CreateEnemyFromId(_world, nextEvent.EventId, nextEvent.Modifications);
			EventManager.Publish(new ResetDeckEvent { });
			var phaseState = EntityManager.GetEntity("PhaseState").GetComponent<PhaseState>();
			phaseState.TurnNumber = 0;
		}

		public void EnqueueBattleRules(bool isFollowingDialog) 
		{
			Console.WriteLine("[BattleSceneSystem] EnqueueBattleRules");
			if (isFollowingDialog)
			{
				EventQueue.EnqueueRule(new EventQueueBridge.QueuedPublish<ChangeBattlePhaseEvent>(
					"Rule.ChangePhase.EnemyStart",
					new ChangeBattlePhaseEvent { Current = SubPhase.StartBattle, Previous = SubPhase.StartBattle }
				));				
				EventQueueBridge.EnqueueTriggerAction("BattleSceneSystem.StartBattle", () => {
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
				return;
			}
			EventQueueBridge.EnqueueTriggerAction("BattleSceneSystem.StartBattle", () => {
				EventQueue.EnqueueRule(new EventQueueBridge.QueuedPublish<ChangeBattlePhaseEvent>(
					"Rule.ChangePhase.EnemyStart",
					new ChangeBattlePhaseEvent { Current = SubPhase.StartBattle, Previous = SubPhase.StartBattle }
				));
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
			if (!_firstLoad) return;
			_firstLoad = false;
			// Construct
				EventManager.Publish(new ChangeMusicTrack { Track = MusicTrack.Battle });

			_deckManagementSystem = new DeckManagementSystem(_world.EntityManager);
			_battleBackgroundSystem = new BattleBackgroundSystem(_world.EntityManager, _graphicsDevice, _spriteBatch, _content);
			_handDisplaySystem = new HandDisplaySystem(_world.EntityManager, _graphicsDevice);
			_cardZoneSystem = new CardZoneSystem(_world.EntityManager);
			_drawPileDisplaySystem = new DrawPileDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch);
			_discardPileDisplaySystem = new DiscardPileDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch);
			_cardListModalSystem = new CardListModalSystem(_world.EntityManager, _graphicsDevice, _spriteBatch);
			var crusaderTexture = _content.Load<Texture2D>("Crusader");
			_playerDisplaySystem = new PlayerDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch, crusaderTexture);
			_cathedralLightingSystem = new CathedralLightingSystem(_world.EntityManager, _graphicsDevice, _spriteBatch);
			_desertBackgroundEffectSystem = new DesertBackgroundEffectSystem(_world.EntityManager, _graphicsDevice, _spriteBatch);
			_playerWispParticleSystem = new PlayerWispParticleSystem(_world.EntityManager, _graphicsDevice, _spriteBatch);
			_playerTemperanceActivationDisplaySystem = new PlayerTemperanceActivationDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch, crusaderTexture);
			_playerAnimationSystem = new PlayerAnimationSystem(_world.EntityManager);
			_courageDisplaySystem = new CourageDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch);
			_actionPointDisplaySystem = new ActionPointDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch);
			_temperanceDisplaySystem = new TemperanceDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch);
			_courageManagerSystem = new CourageManagerSystem(_world.EntityManager);
			_actionPointManagementSystem = new ActionPointManagementSystem(_world.EntityManager);
			_temperanceManagerSystem = new TemperanceManagerSystem(_world.EntityManager);
			_hpDisplaySystem = new HPDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch);
			_appliedPassivesDisplaySystem = new AppliedPassivesDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch);
			_poisonSystem = new PoisonSystem(_world.EntityManager, _graphicsDevice, _spriteBatch);
			_cardVisualSettingsDebugSystem = new CardVisualSettingsDebugSystem(_world.EntityManager);
			_hpManagementSystem = new HpManagementSystem(_world.EntityManager);
			_eventQueueSystem = new EventQueueSystem(_world.EntityManager);
			_battlePhaseDisplaySystem = new BattlePhaseDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch);
			_enemyDisplaySystem = new EnemyDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch, _content);
			_guardianAngelDisplaySystem = new GuardianAngelDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch, _content);
			_enemyIntentPipsSystem = new EnemyIntentPipsSystem(_world.EntityManager, _graphicsDevice, _spriteBatch);
			_enemyAttackDisplaySystem = new EnemyAttackDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch);
			_ambushDisplaySystem = new AmbushDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch);
			_queuedEventsDisplaySystem = new QueuedEventsDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch, _content);
			_damageModificationDisplaySystem = new DamageModificationDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch);
			_cardPlayedAnimationSystem = new CardPlayedAnimationSystem(_world.EntityManager, _graphicsDevice, _spriteBatch);
			_endTurnDisplaySystem = new EndTurnDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch);
			_assignedBlockCardsDisplaySystem = new AssignedBlockCardsDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch, _content);
			_payCostOverlaySystem = new PayCostOverlaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch);
			_cantPlayCardMessageSystem = new CantPlayCardMessageSystem(_world.EntityManager, _graphicsDevice, _spriteBatch);
			_gameOverOverlayDisplaySystem = new GameOverOverlayDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch);
			_enemyIntentPlanningSystem = new EnemyIntentPlanningSystem(_world.EntityManager);
			_enemyAttackProgressManagementSystem = new EnemyAttackProgressManagementSystem(_world.EntityManager);
			_markedForSpecificDiscardSystem = new MarkedForSpecificDiscardSystem(_world.EntityManager);
			_stunnedOverlaySystem = new StunnedOverlaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch);
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
			_tribulationManagerSystem = new TribulationManagerSystem(_world.EntityManager);
			_equipmentDisplaySystem = new EquipmentDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch, _content);
			_equippedWeaponDisplaySystem = new EquippedWeaponDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch, _content);
			_medalDisplaySystem = new MedalDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch, _content);
			_questTribulationDisplaySystem = new QuestTribulationDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch, _content);
			_equipmentUsedManagementSystem = new EquipmentUsedManagementSystem(_world.EntityManager);
			_equipmentHighlightSettingsDebugSystem = new HighlightSettingsSystem(_world.EntityManager);
			_equipmentBlockInteractionSystem = new EquipmentBlockInteractionSystem(_world.EntityManager);
			_appliedPassivesManagementSystem = new AppliedPassivesManagementSystem(_world.EntityManager);
			_battleStateInfoManagementSystem = new BattleStateInfoManagementSystem(_world.EntityManager);
			_discardSpecificCardHighlightSystem = new DiscardSpecificCardHighlightSystem(_world.EntityManager, _graphicsDevice, _spriteBatch);
			_cardZoneSystem = new CardZoneSystem(_world.EntityManager);
			_intimidateManagementSystem = new IntimidateManagementSystem(_world.EntityManager);
			_intimidateDisplaySystem = new IntimidateDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch);
			_frozenCardManagementSystem = new FrozenCardManagementSystem(_world.EntityManager);
			var frostTexture = _content.Load<Texture2D>("frost");
			_frozenCardDisplaySystem = new FrozenCardDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch, frostTexture);
			_uiElementHighlightSystem = new UIElementHighlightSystem(_world.EntityManager, _graphicsDevice, _spriteBatch);
			_questRewardModalDisplaySystem = new QuestRewardModalDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch);
			// Register
			_world.AddSystem(_deckManagementSystem);
			_world.AddSystem(_handDisplaySystem);
			_world.AddSystem(_cardZoneSystem);
			_world.AddSystem(_handBlockInteractionSystem);
			_world.AddSystem(_eventQueueSystem);
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
			_world.AddSystem(_poisonSystem);
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
			_world.AddSystem(_tribulationManagerSystem);
			_world.AddSystem(_equipmentDisplaySystem);
			_world.AddSystem(_equippedWeaponDisplaySystem);
			_world.AddSystem(_medalDisplaySystem);
			_world.AddSystem(_questTribulationDisplaySystem);
			_world.AddSystem(_equipmentUsedManagementSystem);
			_world.AddSystem(_equipmentHighlightSettingsDebugSystem);
			_world.AddSystem(_equipmentBlockInteractionSystem);
			_world.AddSystem(_appliedPassivesManagementSystem);
			_world.AddSystem(_battleStateInfoManagementSystem);
			_world.AddSystem(_payCostOverlaySystem);
			_world.AddSystem(_cantPlayCardMessageSystem);
			_world.AddSystem(_gameOverOverlayDisplaySystem);
			_world.AddSystem(_discardSpecificCardHighlightSystem);
			_world.AddSystem(_intimidateManagementSystem);
			_world.AddSystem(_intimidateDisplaySystem);
			_world.AddSystem(_frozenCardManagementSystem);
			_world.AddSystem(_frozenCardDisplaySystem);
			_world.AddSystem(_questRewardModalDisplaySystem);
		}

	}
}


