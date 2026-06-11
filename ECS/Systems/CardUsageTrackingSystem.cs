using System;
using System.Collections.Generic;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Telemetry;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Systems
{
	public sealed class CardUsageTrackingSystem : Core.System
	{
		private readonly CardUsageTelemetryStore _store;

		public CardUsageTrackingSystem(
			EntityManager entityManager,
			CardUsageTelemetryStore store)
			: base(entityManager)
		{
			_store = store ?? throw new ArgumentNullException(nameof(store));
			EventManager.Subscribe<CardPlayedEvent>(evt => Record(evt?.Card, CardUsageKind.Played));
			EventManager.Subscribe<CardBlockedEvent>(evt => Record(evt?.Card, CardUsageKind.Blocked));
			EventManager.Subscribe<CardDiscardedForCostEvent>(
				evt => Record(evt?.Card, CardUsageKind.DiscardedForCost));
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			return Array.Empty<Entity>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
		}

		private void Record(Entity card, CardUsageKind kind)
		{
			var definition = card?.GetComponent<CardData>()?.Card;
			if (definition == null || string.IsNullOrWhiteSpace(definition.CardId)) return;

			_store.Record(
				definition.CardId,
				definition.Name,
				definition.Type.ToString(),
				kind);
		}
	}
}
