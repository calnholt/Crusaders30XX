using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Services;

namespace Crusaders30XX.ECS.Objects.Events
{
	public class PrunedVocation : EventBase
	{
		public PrunedVocation()
		{
			Id = "pruned_vocation";
			Title = "The Pruned Vocation";
			EventText = "A stern pilgrim offers to carve weakness from your deck by taking cards from your first vows.";
		}

		public override string Option1Text => "Shed one card from your starting vows";
		public override string Option2Text => "Relinquish two cards from your starting vows";
		public override string Option3Text => "Abandon three cards from your starting vows";

		public override void OnOption1(EntityManager entityManager) => Resolve(entityManager, 1);
		public override void OnOption2(EntityManager entityManager) => Resolve(entityManager, 2);
		public override void OnOption3(EntityManager entityManager) => Resolve(entityManager, 3);

		private void Resolve(EntityManager entityManager, int amount)
		{
			RunDeckService.EnsureRunDeck(entityManager);
			EventManager.Publish(new RemoveRandomCardEvent { Amount = amount });
		}
	}
}
