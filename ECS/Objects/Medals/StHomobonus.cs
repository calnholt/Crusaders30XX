using System;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Save;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Medals
{
    public class StHomobonus : MedalBase
    {
        private const int BonusGold = 10;

        public StHomobonus()
        {
            Id = "st_homobonus";
            Name = "St. Homobonus";
            Text = "Whenever you finish a quest, gain 10 gold.";
        }

        public override void Initialize(EntityManager entityManager, Entity medalEntity)
        {
            EntityManager = entityManager;
            MedalEntity = medalEntity;
            EventManager.Subscribe<ShowQuestRewardOverlay>(OnQuestComplete);
        }

        private void OnQuestComplete(ShowQuestRewardOverlay evt)
        {
            EmitActivateEvent();
        }

        public override void Activate()
        {
            int oldGold = 0;
            try { oldGold = SaveCache.GetGold(); } catch { oldGold = 0; }
            SaveCache.AddGold(BonusGold);
            int newGold = oldGold + BonusGold;
            try { newGold = SaveCache.GetGold(); } catch { }
            EventManager.Publish(new GoldChanged
            {
                OldGold = oldGold,
                NewGold = newGold,
                Delta = newGold - oldGold,
                Reason = Id
            });
        }

        public override void Dispose()
        {
            EventManager.Unsubscribe<ShowQuestRewardOverlay>(OnQuestComplete);
        }
    }
}
