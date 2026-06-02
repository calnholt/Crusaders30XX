using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Services;

namespace Crusaders30XX.ECS.Objects.Events
{
	public class IceboundTithe : EventBase
	{
		public IceboundTithe()
		{
			Id = "icebound_tithe";
			Title = "The Icebound Tithe";
			EventText = "A frost-bound reliquary hums at the roadside. It offers coin if you surrender cards to the cold.";
		}

		public override string Option1Text => "Offer one card to the frost";
		public override string Option2Text => "Surrender two cards to the cold";
		public override string Option3Text => "Yield three cards to the rime";

		public override void OnOption1(EntityManager entityManager) => Resolve(entityManager, 1, 10);
		public override void OnOption2(EntityManager entityManager) => Resolve(entityManager, 2, 30);
		public override void OnOption3(EntityManager entityManager) => Resolve(entityManager, 3, 50);

		private void Resolve(EntityManager entityManager, int freezeCount, int goldAmount)
		{
			RunDeckService.EnsureRunDeck(entityManager);
			EventManager.Publish(new FreezeCardsEvent { Amount = freezeCount, Type = FreezeType.Deck });
			EventManager.Publish(new ModifyGoldRequestEvent { Delta = goldAmount, Reason = Id });
		}
	}
}
