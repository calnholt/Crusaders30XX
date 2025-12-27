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
			EventManager.Subscribe<ModifyCourageRequestEvent>(OnModifyCourage);
			EventManager.Subscribe<SetCourageEvent>(OnSetCourageEvent);
			EventManager.Subscribe<ApplyEffect>(OnApplyEffect);
			EventManager.Subscribe<CardMoved>(OnCardMoved);
            Console.WriteLine("[CourageManagerSystem] Subscribed to ModifyCourageEvent, CardMoved");
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			// Not entity-driven; listens to events
			return Array.Empty<Entity>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		private void OnModifyCourage(ModifyCourageRequestEvent evt)
		{
            Console.WriteLine($"[CourageManagerSystem] OnModifyCourage delta={evt.Delta}");
			var player = EntityManager.GetEntitiesWithComponent<Player>()
				.FirstOrDefault(e => e.HasComponent<Courage>());
			if (player == null) return;
			var courage = player.GetComponent<Courage>();
			if (courage == null) return;
			int old = courage.Amount;
			int amount = Math.Max(0, old + evt.Delta);
			int newAmount = amount;
			courage.Amount = newAmount;
			EventManager.Publish(new ModifyCourageEvent { Delta = newAmount - old, Reason = evt.Reason });
		}

		private void OnSetCourageEvent(SetCourageEvent evt)
		{
            Console.WriteLine($"[CourageManagerSystem] OnSetCourageEvent amount={evt.Amount}");
			var player = EntityManager.GetEntitiesWithComponent<Player>()
				.FirstOrDefault(e => e.HasComponent<Courage>());
			if (player == null) return;
			var courage = player.GetComponent<Courage>();
			if (courage == null) return;
			courage.Amount = evt.Amount;
		}

		private void OnApplyEffect(ApplyEffect evt)
		{
			// Translate data-driven effects to resource changes
			string type = evt.EffectType ?? string.Empty;
			if (type != "LoseCourage") return;
			// Only apply if the target is the player
			if (evt.Target == null || !evt.Target.HasComponent<Player>()) return;
			int amt = Math.Max(0, evt.Amount);
			if (amt <= 0) amt = 1; // default to 1 if unspecified or invalid
			EventManager.Publish(new ModifyCourageRequestEvent { Delta = -amt });
		}

		private void OnCardMoved(CardMoved evt)
		{
            Console.WriteLine($"[CourageManagerSystem] OnCardMoved from={evt.From} to={evt.To}");
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



