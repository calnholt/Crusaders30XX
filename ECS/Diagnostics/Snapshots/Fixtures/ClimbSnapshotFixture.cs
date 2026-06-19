using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Loadouts;
using Crusaders30XX.ECS.Data.Save;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Services;
using Crusaders30XX.ECS.Systems;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.Diagnostics.Snapshots.Fixtures
{
	public enum ClimbSnapshotVariant
	{
		NoEvents,
		ActiveEvents,
		HoverPreview,
		SoldShopSlot,
		EncounterRewardModal,
		ReplacementModal,
	}

	public sealed class ClimbSnapshotFixture : IDisplaySnapshotFixture
	{
		private const int SnapshotSeed = 30030;
		private readonly ClimbSnapshotVariant _variant;
		private ClimbSceneSystem _climbScene;
		private ClimbHeaderLayoutSystem _headerLayout;
		private ClimbColumnLayoutSystem _columnLayout;
		private RewardModalDisplaySystem _rewardModal;
		private CardListModalSystem _cardListModal;
		private bool _modalOpened;

		public ClimbSnapshotFixture(ClimbSnapshotVariant variant)
		{
			_variant = variant;
		}

		public string Id => _variant switch
		{
			ClimbSnapshotVariant.NoEvents => "climb-no-events",
			ClimbSnapshotVariant.ActiveEvents => "climb-active-events",
			ClimbSnapshotVariant.HoverPreview => "climb-hover-preview",
			ClimbSnapshotVariant.SoldShopSlot => "climb-sold-shop-slot",
			ClimbSnapshotVariant.EncounterRewardModal => "climb-encounter-reward-modal",
			ClimbSnapshotVariant.ReplacementModal => "climb-replacement-modal",
			_ => "climb",
		};

		public int WarmupFrames => _variant == ClimbSnapshotVariant.ReplacementModal ? 4 : 3;
		public string OutputFileName => Id;

		public void Setup(DisplaySnapshotContext ctx, string[] args)
		{
			ConfigureSave();
			SetScene(ctx, SceneId.Climb);
			EventManager.Publish(new LoadSceneEvent
			{
				Scene = SceneId.Climb,
				PreviousScene = SceneId.Snapshot,
			});

			_climbScene = ctx.World.GetSystem<ClimbSceneSystem>();
			_headerLayout = ctx.World.GetSystem<ClimbHeaderLayoutSystem>();
			_columnLayout = ctx.World.GetSystem<ClimbColumnLayoutSystem>();
			_rewardModal = ctx.World.GetSystem<RewardModalDisplaySystem>();
			_cardListModal = ctx.World.GetSystem<CardListModalSystem>();

			if (_climbScene == null || _headerLayout == null || _columnLayout == null)
			{
				throw new DisplaySnapshotSetupException("Climb scene systems were not registered.");
			}
		}

		public void Draw(DisplaySnapshotContext ctx)
		{
			if (_variant == ClimbSnapshotVariant.HoverPreview)
			{
				ForceHoverPreview(ctx.World.EntityManager);
			}

			if (_variant == ClimbSnapshotVariant.EncounterRewardModal)
			{
				OpenRewardModal();
			}
			else if (_variant == ClimbSnapshotVariant.ReplacementModal)
			{
				OpenReplacementModal(ctx.World.EntityManager);
			}

			_climbScene.Draw();

			if (_variant == ClimbSnapshotVariant.EncounterRewardModal)
			{
				_rewardModal?.Draw();
			}
			else if (_variant == ClimbSnapshotVariant.ReplacementModal)
			{
				_cardListModal?.Draw();
			}
		}

		private void ConfigureSave()
		{
			SaveCache.StartNewRun();
			var save = SaveCache.GetAll();
			save.isRunActive = true;
			save.runMapSeed = SnapshotSeed;
			save.pendingBattleNodeId = string.Empty;
			save.pendingDeckRewardOffer = null;

			var loadout = SaveCache.GetLoadout(RunDeckService.PrimaryLoadoutId)
				?? new LoadoutDefinition { id = RunDeckService.PrimaryLoadoutId, name = "Deck" };
			loadout.cardIds = new List<string>
			{
				"strike|White",
				"smite|White",
				"fervor|Red",
				"reckoning|White",
				"unburdened_strike|Black",
				"hold_the_line|White",
			};
			loadout.weaponId = "sword";
			loadout.temperanceId = "angelic_aura";
			loadout.chestId = string.Empty;
			loadout.legsId = string.Empty;
			loadout.armsId = string.Empty;
			loadout.headId = string.Empty;
			loadout.medalIds = new List<string>();
			SaveCache.SaveLoadout(loadout);

			SaveCache.SaveClimbState(BuildClimbState());
		}

		private ClimbSaveState BuildClimbState()
		{
			int time = _variant == ClimbSnapshotVariant.HoverPreview ? 6 : 5;
			return new ClimbSaveState
			{
				time = time,
				resources = new ClimbResourceSave { red = 2, white = 1, black = 1 },
				shopSlots = BuildShopSlots(time),
				encounterSlots = BuildEncounterSlots(time),
				eventSlots = BuildEventSlots(time),
				shownMedalIds = new List<string>(),
				shownEquipmentIds = new List<string>(),
				shownEventTypeIds = new List<string>(),
				nextEventSlotId = 2,
				pendingReplacementOffer = _variant == ClimbSnapshotVariant.ReplacementModal
					? new ClimbReplacementOfferSave
					{
						shopSlotIndex = 3,
						incomingCardKey = "zealous_vow|Red",
						cost = new ClimbResourceSave { red = 1, white = 1, black = 0 },
					}
					: null,
				pendingEncounterReward = null,
			};
		}

		private List<ClimbShopSlotSave> BuildShopSlots(int time)
		{
			return new List<ClimbShopSlotSave>
			{
				new()
				{
					id = "shop_medal",
					kind = ClimbShopSlotKinds.Medal,
					itemId = "st_luke",
					cost = new ClimbResourceSave { red = 1, white = 0, black = 0 },
					timeCost = 1,
					generatedAtTime = time,
				},
				new()
				{
					id = "shop_equipment",
					kind = ClimbShopSlotKinds.Equipment,
					itemId = "knightly_helm",
					cost = new ClimbResourceSave { red = 0, white = 1, black = 0 },
					timeCost = 2,
					isSold = _variant == ClimbSnapshotVariant.SoldShopSlot,
					generatedAtTime = time,
				},
				new()
				{
					id = "shop_upgrade",
					kind = ClimbShopSlotKinds.Upgrade,
					cardKey = "smite|White|Upgraded",
					deckIndex = 1,
					cost = new ClimbResourceSave { red = 1, white = 1, black = 0 },
					timeCost = 2,
					generatedAtTime = time,
				},
				new()
				{
					id = "shop_replacement",
					kind = ClimbShopSlotKinds.Replacement,
					cardKey = "zealous_vow|Red",
					cost = new ClimbResourceSave { red = 1, white = 1, black = 0 },
					timeCost = 3,
					generatedAtTime = time,
				},
			};
		}

		private List<ClimbEncounterSlotSave> BuildEncounterSlots(int time)
		{
			return new List<ClimbEncounterSlotSave>
			{
				new()
				{
					id = "encounter_0",
					enemyId = "skeleton",
					generatedAtTime = time,
					duration = 4,
					timeCost = _variant == ClimbSnapshotVariant.HoverPreview ? 2 : 3,
					rewardResources = _variant == ClimbSnapshotVariant.HoverPreview
						? new ClimbResourceSave { red = 1, white = 1, black = 0 }
						: new ClimbResourceSave { red = 1, white = 1, black = 1 },
					hasDeckReward = true,
				},
				new()
				{
					id = "encounter_1",
					enemyId = "demon",
					generatedAtTime = time,
					duration = 3,
					timeCost = 1,
					rewardResources = new ClimbResourceSave { red = 0, white = 1, black = 0 },
					hasDeckReward = true,
				},
				new()
				{
					id = "encounter_2",
					enemyId = "cactus",
					generatedAtTime = time,
					duration = 5,
					timeCost = 3,
					rewardResources = new ClimbResourceSave { red = 1, white = 1, black = 1 },
					hasDeckReward = true,
				},
			};
		}

		private List<ClimbEventSlotSave> BuildEventSlots(int time)
		{
			if (_variant == ClimbSnapshotVariant.NoEvents)
			{
				return new List<ClimbEventSlotSave>();
			}

			return new List<ClimbEventSlotSave>
			{
				new()
				{
					id = "event_0",
					eventTypeId = "icebound_tithe",
					generatedAtTime = 0,
					visibleStartTime = Math.Max(0, time - 1),
					visibleEndTime = time + 3,
					timeCost = 2,
					seen = true,
				},
				new()
				{
					id = "event_1",
					eventTypeId = "pruned_vocation",
					generatedAtTime = 1,
					visibleStartTime = time,
					visibleEndTime = time + 4,
					timeCost = 3,
					seen = true,
				},
			};
		}

		private void ForceHoverPreview(EntityManager entityManager)
		{
			var target = entityManager.GetEntitiesWithComponent<ClimbSlotPresentation>()
				.FirstOrDefault(e => string.Equals(
					e.GetComponent<ClimbSlotPresentation>()?.SlotId,
					"encounter_0",
					StringComparison.OrdinalIgnoreCase));
			if (target == null) return;

			foreach (var ui in entityManager.GetEntitiesWithComponent<UIElement>()
				.Select(e => e.GetComponent<UIElement>())
				.Where(ui => ui != null))
			{
				ui.IsHovered = false;
			}

			var targetUi = target.GetComponent<UIElement>();
			if (targetUi != null)
			{
				targetUi.IsHovered = true;
			}

			var time = new GameTime(TimeSpan.FromSeconds(1d), TimeSpan.FromSeconds(1d / 60d));
			_headerLayout?.Update(time);
			_columnLayout?.Update(time);
		}

		private void OpenRewardModal()
		{
			if (_modalOpened) return;
			_modalOpened = true;
			if (_rewardModal == null)
			{
				throw new DisplaySnapshotSetupException("Reward modal system was not registered.");
			}

			_rewardModal.OpenEncounterRewardForSnapshot(
				new ClimbResourceSave { red = 2, white = 1, black = 1 });
		}

		private void OpenReplacementModal(EntityManager entityManager)
		{
			if (_modalOpened) return;
			_modalOpened = true;
			if (_cardListModal == null)
			{
				throw new DisplaySnapshotSetupException("Card list modal system was not registered.");
			}

			var deck = RunDeckService.EnsureRunDeck(entityManager)?.GetComponent<Deck>();
			var cards = new List<Entity>();
			var loadout = SaveCache.GetLoadout(RunDeckService.PrimaryLoadoutId);
			for (int i = 0; i < (loadout?.cardIds?.Count ?? 0); i++)
			{
				string key = loadout.cardIds[i];
				if (!ClimbShopService.IsReplacementEligible(key)) continue;
				var card = deck?.Cards.FirstOrDefault(e =>
					string.Equals(e.GetComponent<RunDeckCard>()?.CardKey, key, StringComparison.OrdinalIgnoreCase));
				if (card == null) continue;
				if (card.GetComponent<CardListModalSelectionMetadata>() == null)
				{
					entityManager.AddComponent(card, new CardListModalSelectionMetadata
					{
						SelectionContext = CardListSelectionContexts.ClimbReplacement,
						CardKey = key,
						SourceIndex = i,
					});
				}
				cards.Add(card);
			}

			if (cards.Count == 0)
			{
				throw new DisplaySnapshotSetupException("No eligible replacement cards were created.");
			}

			_cardListModal.OpenForSnapshot(
				"Choose Replacement",
				cards,
				isSelectable: true,
				selectionContext: CardListSelectionContexts.ClimbReplacement);
			_cardListModal.Update(new GameTime(TimeSpan.FromSeconds(1d), TimeSpan.FromSeconds(1d / 60d)));
		}

		private static void SetScene(DisplaySnapshotContext ctx, SceneId sceneId)
		{
			var scene = ctx.SceneEntity.GetComponent<SceneState>();
			if (scene == null)
			{
				ctx.World.AddComponent(ctx.SceneEntity, new SceneState { Current = sceneId });
				return;
			}
			scene.Current = sceneId;
		}
	}
}
