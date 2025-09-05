using System;
using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// Manages the player's Temperance resource by listening to events and applying changes.
    /// </summary>
    public class TemperanceManagerSystem : Core.System
    {
        public TemperanceManagerSystem(EntityManager entityManager) : base(entityManager)
        {
            EventManager.Subscribe<CardMoved>(OnCardMoved);
            EventManager.Subscribe<ModifyTemperanceEvent>(OnModifyTemperance);
            System.Console.WriteLine("[TemperanceManagerSystem] Subscribed to CardMoved, ModifyTemperanceEvent");
        }

        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return Array.Empty<Entity>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

        private void OnCardMoved(CardMoved evt)
        {
            System.Console.WriteLine($"[TemperanceManagerSystem] OnCardMoved from={evt.From} to={evt.To}");
            // When assigned blocks land in discard, grant Temperance for white cards
            if (evt.To == CardZoneType.DiscardPile && evt.From == CardZoneType.AssignedBlock) {
              var data = evt.Card.GetComponent<CardData>();
              if (data == null || data.Color != CardData.CardColor.White) return;
              var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
              if (player == null) return;
              var t = player.GetComponent<Temperance>();
              if (t == null) { t = new Temperance(); EntityManager.AddComponent(player, t); }
              t.Amount = Math.Max(0, t.Amount + 1);
              TryTriggerTemperanceAbility(player, t);
            }
        }

        private void OnModifyTemperance(ModifyTemperanceEvent evt)
        {
            var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
            if (player == null) return;
            var t = player.GetComponent<Temperance>();
            if (t == null) { t = new Temperance(); EntityManager.AddComponent(player, t); }
            int before = t.Amount;
            t.Amount = Math.Max(0, t.Amount + evt.Delta);
            System.Console.WriteLine($"[TemperanceManagerSystem] Temperance changed {before} -> {t.Amount}");
            TryTriggerTemperanceAbility(player, t);
        }

        private void TryTriggerTemperanceAbility(Entity player, Temperance t)
        {
            var equipped = player.GetComponent<EquippedTemperanceAbility>();
            if (equipped == null || string.IsNullOrEmpty(equipped.AbilityId)) return;

            if (!Crusaders30XX.ECS.Data.Temperance.TemperanceAbilityDefinitionCache.TryGet(equipped.AbilityId, out var def)) return;
            if (def.threshold <= 0) return;
            while (t.Amount >= def.threshold)
            {
                // Spend threshold and activate
                t.Amount -= def.threshold;
                TemperanceAbilityService.Activate(EntityManager, equipped.AbilityId);
            }
        }
    }
}

