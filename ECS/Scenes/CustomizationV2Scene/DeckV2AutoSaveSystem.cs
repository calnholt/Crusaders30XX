using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Loadouts;
using Crusaders30XX.ECS.Data.Save;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Factories;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("DeckV2 Save")]
	public class DeckV2AutoSaveSystem : Core.System
	{
		public DeckV2AutoSaveSystem(EntityManager em) : base(em)
		{
			EventManager.Subscribe<AddCardToLoadoutRequested>(OnAddRequested);
			EventManager.Subscribe<RemoveCardFromLoadoutRequested>(OnRemoveRequested);
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<SceneState>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		private void OnAddRequested(AddCardToLoadoutRequested evt)
		{
			if (evt == null || string.IsNullOrEmpty(evt.CardKey)) return;
			var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
			if (scene == null || scene.Current != SceneId.CustomizationV2) return;

			var deck = EntityManager.GetEntitiesWithComponent<CustomizationV2DeckState>().FirstOrDefault()?.GetComponent<CustomizationV2DeckState>();
			if (deck == null) return;

			// Enforce copy limit: max 2 copies per card name
			string baseId = evt.CardKey.Split('|')[0].ToLowerInvariant();
			var card = CardFactory.Create(baseId);
			string cardName = (card?.Name ?? baseId).ToLowerInvariant();

			int existingCopies = 0;
			foreach (var key in deck.DeckCardKeys)
			{
				string existingBaseId = key.Split('|')[0].ToLowerInvariant();
				var existingCard = CardFactory.Create(existingBaseId);
				string existingName = (existingCard?.Name ?? existingBaseId).ToLowerInvariant();
				if (existingName == cardName) existingCopies++;
			}

			if (existingCopies >= 2)
			{
				Console.WriteLine($"[DeckV2] Cannot add: already have {existingCopies} copies of '{cardName}'.");
				return;
			}

			deck.DeckCardKeys.Add(evt.CardKey);
			EventManager.Publish(new DeckV2CardAdded { CardKey = evt.CardKey });
			TrySaveToDisk(deck);
			EventManager.Publish(new DeckV2DeckChanged());
		}

		private void OnRemoveRequested(RemoveCardFromLoadoutRequested evt)
		{
			if (evt == null || string.IsNullOrEmpty(evt.CardKey)) return;
			var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
			if (scene == null || scene.Current != SceneId.CustomizationV2) return;

			var deck = EntityManager.GetEntitiesWithComponent<CustomizationV2DeckState>().FirstOrDefault()?.GetComponent<CustomizationV2DeckState>();
			if (deck == null) return;

			if (evt.Index.HasValue)
			{
				int i = evt.Index.Value;
				if (i >= 0 && i < deck.DeckCardKeys.Count && deck.DeckCardKeys[i] == evt.CardKey)
				{
					deck.DeckCardKeys.RemoveAt(i);
				}
				else
				{
					int idx = deck.DeckCardKeys.IndexOf(evt.CardKey);
					if (idx >= 0) deck.DeckCardKeys.RemoveAt(idx);
				}
			}
			else
			{
				int idx = deck.DeckCardKeys.IndexOf(evt.CardKey);
				if (idx >= 0) deck.DeckCardKeys.RemoveAt(idx);
			}

			EventManager.Publish(new DeckV2CardRemoved { CardKey = evt.CardKey });
			TrySaveToDisk(deck);
			EventManager.Publish(new DeckV2DeckChanged());
		}

		private void TrySaveToDisk(CustomizationV2DeckState deck)
		{
			if (deck.DeckCardKeys.Count != DeckRules.RequiredDeckSize) return;
			if (!IsWithinNameCopyLimit(deck)) return;

			if (!LoadoutDefinitionCache.TryGet("loadout_1", out var def) || def == null)
			{
				def = new LoadoutDefinition { id = "loadout_1", name = "Loadout 1" };
			}
			def.cardIds = new List<string>(deck.DeckCardKeys);
			SaveCache.SaveLoadout(def);
			Console.WriteLine("[DeckV2] Deck auto-saved.");
		}

		private static bool IsWithinNameCopyLimit(CustomizationV2DeckState deck)
		{
			if (deck?.DeckCardKeys == null) return true;

			var nameCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			foreach (var key in deck.DeckCardKeys)
			{
				string baseId = key.Split('|')[0].ToLowerInvariant();
				var card = CardFactory.Create(baseId);
				string name = (card?.Name ?? baseId).ToLowerInvariant();
				int count = (nameCounts.TryGetValue(name, out var c) ? c : 0) + 1;
				nameCounts[name] = count;
				if (count > 2) return false;
			}
			return true;
		}
	}
}
