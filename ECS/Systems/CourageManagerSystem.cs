using System;
using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Manages the player's Courage resource by listening to events and applying changes.
	/// </summary>
	public class CourageManagerSystem : Core.System
	{
		public CourageManagerSystem(EntityManager entityManager) : base(entityManager)
		{
			EventManager.Subscribe<ModifyCourageEvent>(OnModifyCourage);
			EventManager.Subscribe<CardMoved>(OnCardMoved);
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			// Not entity-driven; listens to events
			return Array.Empty<Entity>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		private void OnModifyCourage(ModifyCourageEvent evt)
		{
			var player = EntityManager.GetEntitiesWithComponent<Player>()
				.FirstOrDefault(e => e.HasComponent<Courage>());
			if (player == null) return;
			var courage = player.GetComponent<Courage>();
			if (courage == null) return;
			int old = courage.Amount;
			courage.Amount = Math.Max(0, old + evt.Delta);
		}

		private void OnCardMoved(CardMoved evt)
		{
			// When assigned blocks land in discard, grant Courage for red cards
			if (evt.To == CardZoneType.DiscardPile && evt.From == CardZoneType.AssignedBlock) {
				var data = evt.Card.GetComponent<CardData>();
				if (data == null) return;
				if (data.Color != CardData.CardColor.Red) return;
				var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
				if (player == null) return;
				var c = player.GetComponent<Courage>();
				if (c == null) { c = new Courage(); EntityManager.AddComponent(player, c); }
				c.Amount = Math.Max(0, c.Amount + 1);
			}
		}
	}
}



