using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Objects.Cards
{
    public class DeusVult : CardBase
    {
        public DeusVult()
        {
            Name = "Deus Vult";
            CardId = "deus_vult";
            Text = "You can't play this if you have not used your weapon this turn. Gain {1} courage. This gains +X damage, where X is {2} times your courage";
            Animation = "Attack";
            Damage = 3;
            Block = 2;
            IsFreeAction = true;

            OnPlay = (entityManager, card) =>
            {
                var player = entityManager.GetEntity("Player");
                var enemy = entityManager.GetEntity("Enemy");
                EventManager.Publish(new ModifyCourageRequestEvent { Delta = ValuesParse[0], Type = ModifyCourageType.Gain });
                EventManager.Publish(new ModifyHpRequestEvent { 
                    Source = player, 
                    Target = enemy, 
                    Delta = -GetDerivedDamage(entityManager, card), 
                    DamageType = ModifyTypeEnum.Attack 
                });
            };

            GetConditionalDamage = (entityManager, card) =>
            {
                var player = entityManager.GetEntity("Player");
                var courage = player.GetComponent<Courage>().Amount;
                return courage * ValuesParse[1];
            };

            CanPlay = (entityManager, card) =>
            {
                var player = entityManager.GetEntity("Player");
                var weaponId = player.GetComponent<EquippedWeapon>().WeaponId;
                var battleState = entityManager.GetEntitiesWithComponent<BattleStateInfo>().FirstOrDefault().GetComponent<BattleStateInfo>();
                if (battleState == null) return false;
                battleState.PhaseTracking.TryGetValue(weaponId, out var weaponAttacked);
                var weaponAttackCount = weaponAttacked > 0;
                if (!weaponAttackCount)
                {
                    EventManager.Publish(new CantPlayCardMessage { Message = "You must attack with your weapon this turn!" });
                    return false;
                }
                return true;
            };
        }
    }
}