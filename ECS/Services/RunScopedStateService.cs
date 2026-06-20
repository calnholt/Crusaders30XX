using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Save;
using Crusaders30XX.ECS.Systems;
using Crusaders30XX.Diagnostics;

namespace Crusaders30XX.ECS.Services
{
	public static class RunScopedStateService
	{
		public const string RestrictionFrozen = "Frozen";
		public const string RestrictionSealed = "Sealed";
		public const string RestrictionBrittle = "Brittle";
		public const string RestrictionColorless = "Colorless";

		public static void HydrateRunLongPassivesOntoPlayer(Entity player)
		{
			if (TestFightRuntime.IsActive) return;
			if (player == null) return;
			var ap = player.GetComponent<AppliedPassives>();
			if (ap == null) return;
			if (ap.Passives == null) ap.Passives = new Dictionary<AppliedPassiveType, int>();

			foreach (var type in AppliedPassivesManagementSystem.GetRunLongPassives())
			{
				if (ap.Passives.ContainsKey(type)) ap.Passives.Remove(type);
			}

			foreach (var kv in SaveCache.GetRunLongPassivesSnapshot())
			{
				if (!Enum.TryParse<AppliedPassiveType>(kv.Key, true, out var type)) continue;
				if (!AppliedPassivesManagementSystem.GetRunLongPassives().Contains(type)) continue;
				if (kv.Value > 0) ap.Passives[type] = kv.Value;
			}
		}

		public static void SyncRunLongPassivesFromPlayer(Entity player)
		{
			if (TestFightRuntime.IsActive) return;
			if (player == null || !player.HasComponent<Player>()) return;
			var ap = player.GetComponent<AppliedPassives>();
			foreach (var type in AppliedPassivesManagementSystem.GetRunLongPassives())
			{
				int stacks = 0;
				if (ap?.Passives != null && ap.Passives.TryGetValue(type, out var value))
				{
					stacks = value;
				}
				SaveCache.SetRunLongPassive(type.ToString(), stacks);
			}
		}

		public static void HydrateRunCardRestrictions(EntityManager entityManager)
		{
			if (TestFightRuntime.IsActive) return;
			foreach (var card in entityManager.GetEntitiesWithComponent<RunDeckCard>())
			{
				if (!card.IsActive) continue;
				var entryId = card.GetComponent<RunDeckCard>()?.EntryId;
				if (string.IsNullOrWhiteSpace(entryId)) continue;
				ApplySavedRestrictionsToCard(entityManager, card, entryId);
			}
		}

		public static void SyncCardRestrictionsFromComponents(Entity card)
		{
			if (TestFightRuntime.IsActive) return;
			if (card == null) return;
			var entryId = card.GetComponent<RunDeckCard>()?.EntryId;
			if (string.IsNullOrWhiteSpace(entryId)) return;
			var names = new List<string>();
			if (card.HasComponent<Frozen>()) names.Add(RestrictionFrozen);
			if (card.HasComponent<Sealed>()) names.Add(RestrictionSealed);
			if (card.HasComponent<Brittle>()) names.Add(RestrictionBrittle);
			if (card.HasComponent<Colorless>()) names.Add(RestrictionColorless);
			SaveCache.SetRunDeckEntryRestrictions(RunDeckService.PrimaryLoadoutId, entryId, names);
		}

		public static void ClearRunCardRestrictionComponents(EntityManager entityManager)
		{
			foreach (var card in entityManager.GetEntitiesWithComponent<RunDeckCard>().ToList())
			{
				if (card == null || !card.IsActive) continue;
				StripRestrictionComponents(entityManager, card);
			}
		}

		private static void ApplySavedRestrictionsToCard(EntityManager entityManager, Entity card, string entryId)
		{
			foreach (var restriction in SaveCache.GetRunDeckEntryRestrictions(RunDeckService.PrimaryLoadoutId, entryId))
			{
				ApplyRestrictionComponent(entityManager, card, restriction);
			}
		}

		private static void ApplyRestrictionComponent(EntityManager entityManager, Entity card, string restrictionName)
		{
			switch (restrictionName)
			{
				case RestrictionFrozen:
					if (card.GetComponent<Frozen>() == null)
					{
						if (entityManager != null) entityManager.AddComponent(card, new Frozen { Owner = card });
					}
					break;
				case RestrictionSealed:
					if (card.GetComponent<Sealed>() == null)
					{
						if (entityManager != null) entityManager.AddComponent(card, new Sealed { Owner = card, Seals = 1 });
					}
					break;
				case RestrictionBrittle:
					if (card.GetComponent<Brittle>() == null)
					{
						if (entityManager != null) entityManager.AddComponent(card, new Brittle { Owner = card });
					}
					break;
				case RestrictionColorless:
					if (card.GetComponent<Colorless>() == null)
					{
						if (entityManager != null) entityManager.AddComponent(card, new Colorless { Owner = card });
					}
					break;
			}
		}

		private static void StripRestrictionComponents(EntityManager entityManager, Entity card)
		{
			if (card.HasComponent<Frozen>()) entityManager.RemoveComponent<Frozen>(card);
			if (card.HasComponent<Shackle>()) entityManager.RemoveComponent<Shackle>(card);
			if (card.HasComponent<Sealed>()) entityManager.RemoveComponent<Sealed>(card);
			if (card.HasComponent<Brittle>()) entityManager.RemoveComponent<Brittle>(card);
			if (card.HasComponent<Colorless>()) entityManager.RemoveComponent<Colorless>(card);
		}
	}
}
