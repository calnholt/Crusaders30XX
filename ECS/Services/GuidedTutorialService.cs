using System;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Save;
using Crusaders30XX.ECS.Data.Tutorials;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Factories;
using Crusaders30XX.ECS.Systems;

namespace Crusaders30XX.ECS.Services
{
	public static class GuidedTutorialService
	{
		public static bool IsActive(EntityManager entityManager) =>
			GetState(entityManager) != null;

		public static GuidedTutorial GetState(EntityManager entityManager) =>
			entityManager?.GetEntitiesWithComponent<GuidedTutorial>()
				.FirstOrDefault(e => e.IsActive)
				?.GetComponent<GuidedTutorial>();

		public static void Start(World world)
		{
			if (world == null) return;
			Cleanup(world.EntityManager);

			var stateEntity = world.CreateEntity("GuidedTutorial");
			world.AddComponent(stateEntity, new GuidedTutorial());
			world.AddComponent(stateEntity, new DontDestroyOnLoad());

			var deckEntity = EntityFactory.CreateDeck(world);
			world.AddComponent(deckEntity, new StockHand { Battle = TutorialBattle.Gleeber, Turn = 1 });
			world.AddComponent(deckEntity, new DontDestroyOnLoad());

			var player = EntityFactory.CreatePlayer(world);
			player.GetComponent<Player>().DeckEntity = deckEntity;
			player.GetComponent<HP>().Max = 25;
			player.GetComponent<HP>().UnscarredMax = 25;
			player.GetComponent<HP>().Current = 25;
			player.GetComponent<Intellect>().Value = 4;
			player.GetComponent<MaxHandSize>().Value = 4;
			player.GetComponent<EquippedWeapon>().WeaponId = "sword";
			player.GetComponent<EquippedTemperanceAbility>().AbilityId = "unsheath";

			foreach (var equipment in world.EntityManager.GetEntitiesWithComponent<EquippedEquipment>().ToList())
				world.EntityManager.DestroyEntity(equipment.Id);
			foreach (var medal in world.EntityManager.GetEntitiesWithComponent<EquippedMedal>().ToList())
				world.EntityManager.DestroyEntity(medal.Id);

			var queuedEntity = world.CreateEntity("QueuedEvents");
			var queued = new QueuedEvents
			{
				CurrentIndex = -1,
				LocationId = string.Empty,
				QuestIndex = -1,
			};
			queued.Events.Add(new QueuedEvent { EventId = "gleeber" });
			queued.Events.Add(new QueuedEvent { EventId = "sand_corpse" });
			world.AddComponent(queuedEntity, queued);
			world.AddComponent(queuedEntity, new PendingQuestDialog
			{
				DialogId = "guided_tutorial",
				SegmentId = "intro",
				RequestId = Guid.NewGuid(),
				WillShowDialog = true,
			});
			world.AddComponent(queuedEntity, new DontDestroyOnLoad());
			EventManager.Publish(new ShowTransition { Scene = SceneId.Battle, SkipHold = true });
		}

		public static void PrepareStockHand(EntityManager entityManager)
		{
			var state = GetState(entityManager);
			var deckEntity = entityManager?.GetEntitiesWithComponent<StockHand>().FirstOrDefault();
			var deck = deckEntity?.GetComponent<Deck>();
			if (state == null || deck == null || state.StockHandPrepared) return;

			var retainedPledges = deck.Hand
				.Where(card => card.HasComponent<Pledge>())
				.ToList();
			foreach (var card in deck.Cards.ToList())
			{
				if (retainedPledges.Contains(card)) continue;
				entityManager.DestroyEntity(card.Id);
			}

			deck.Cards.Clear();
			deck.DrawPile.Clear();
			deck.DiscardPile.Clear();
			deck.ExhaustPile.Clear();
			deck.Hand.Clear();
			foreach (var retained in retainedPledges)
			{
				deck.Cards.Add(retained);
				deck.Hand.Add(retained);
			}

			var turn = GuidedTutorialDefinitions.GetTurn(state.Battle, state.Turn);
			for (int i = 0; i < turn.StockHand.Count; i++)
			{
				var definition = turn.StockHand[i];
				if (retainedPledges.Any(card =>
					string.Equals(card.GetComponent<CardData>()?.Card?.CardId, definition.CardId, StringComparison.OrdinalIgnoreCase)))
				{
					continue;
				}

				var card = EntityFactory.CreateCardFromDefinition(
					entityManager,
					definition.CardId,
					definition.Color,
					index: i);
				if (card == null) continue;
				deck.Cards.Add(card);
				deck.DrawPile.Add(card);
			}

			var stock = deckEntity.GetComponent<StockHand>();
			stock.Battle = state.Battle;
			stock.Turn = state.Turn;
			state.StockHandPrepared = true;
			RefreshValidPlays(state);
		}

		public static void BeginNextTurn(EntityManager entityManager, int turn)
		{
			var state = GetState(entityManager);
			if (state == null || turn <= state.Turn) return;
			state.Turn = turn;
			state.StockHandPrepared = false;
			state.ActionRequirementsComplete = false;
			state.PlayedCardIds.Clear();
			state.PledgedCardIds.Clear();
			state.BlockedCardIdsThisTurn.Clear();
			state.ConfirmedAttackCountThisTurn = 0;
			PrepareStockHand(entityManager);
		}

		public static void BeginSecondBattle(EntityManager entityManager)
		{
			var state = GetState(entityManager);
			if (state == null) return;
			var playerHp = entityManager.GetEntity("Player")?.GetComponent<HP>();
			int restoredHp = playerHp?.Max ?? 25;
			if (playerHp != null)
				playerHp.Current = restoredHp;
			state.PlayerHp = restoredHp;
			state.Battle = TutorialBattle.SandCorpse;
			state.Turn = 1;
			state.StockHandPrepared = false;
			state.ActionRequirementsComplete = false;
			state.PlayedCardIds.Clear();
			state.PledgedCardIds.Clear();
			state.BlockedCardIdsThisTurn.Clear();
			state.ConfirmedAttackCountThisTurn = 0;
			RefreshValidPlays(state);
		}

		public static void Complete(EntityManager entityManager)
		{
			var state = GetState(entityManager);
			if (state != null) state.IsCompleted = true;
			SaveCache.CompleteGuidedTutorial();
			Cleanup(entityManager);
			EventManager.Publish(new ShowTransition { Scene = SceneId.WayStation, SkipHold = true });
		}

		public static void RefreshValidPlays(GuidedTutorial state)
		{
			if (state == null) return;
			state.ValidPlayCardIds.Clear();
			state.ValidPlayCardIds.AddRange(GuidedTutorialDefinitions.GetValidPlays(state));
		}

		public static void Cleanup(EntityManager entityManager)
		{
			if (entityManager == null) return;
			foreach (var entity in entityManager.GetEntitiesWithComponent<GuidedTutorial>().ToList())
				entityManager.DestroyEntity(entity.Id);
			foreach (var entity in entityManager.GetEntitiesWithComponent<StockHand>().ToList())
			{
				var deck = entity.GetComponent<Deck>();
				foreach (var card in deck?.Cards?.ToList() ?? [])
					entityManager.DestroyEntity(card.Id);
				entityManager.DestroyEntity(entity.Id);
			}
			if (!SaveCache.IsRunActive())
			{
				RunPlayerService.DestroyRunPlayer(entityManager);
				entityManager.DestroyEntity("QueuedEvents");
			}
		}
	}
}
