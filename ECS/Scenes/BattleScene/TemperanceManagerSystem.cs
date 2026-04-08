using System;
using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Services;
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
            EventManager.Subscribe<SetTemperanceEvent>(OnSetTemperanceEvent);
            Console.WriteLine("[TemperanceManagerSystem] Subscribed to CardMoved, ModifyTemperanceEvent");
        }

        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return Array.Empty<Entity>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

        private void OnCardMoved(CardMoved evt)
        {
            Console.WriteLine($"[TemperanceManagerSystem] OnCardMoved from={evt.From} to={evt.To}");
            LoggingService.Append("TemperanceManagerSystem.OnCardMoved", new System.Text.Json.Nodes.JsonObject
            {
                ["from"] = evt.From.ToString(),
                ["to"] = evt.To.ToString(),
                ["cardId"] = evt.Card?.Id ?? -1
            });
            // When assigned blocks land in discard, grant Temperance for white cards
            if (evt.To == CardZoneType.DiscardPile && evt.From == CardZoneType.AssignedBlock) {
              var data = evt.Card.GetComponent<CardData>();
              if (data == null || data.Color != CardData.CardColor.White) return;
              var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
              if (player == null) return;
              var t = player.GetComponent<Temperance>();
              if (t == null) { t = new Temperance(); EntityManager.AddComponent(player, t); }
              EventManager.Publish(new ModifyTemperanceEvent { Delta = 1 });
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
            Console.WriteLine($"[TemperanceManagerSystem] Temperance changed {before} -> {t.Amount}");
            LoggingService.Append("TemperanceManagerSystem.OnModifyTemperance", new System.Text.Json.Nodes.JsonObject
            {
                ["delta"] = evt.Delta,
                ["before"] = before,
                ["after"] = t.Amount
            });
            TryTriggerTemperanceAbility(player, t);
        }
        private void OnSetTemperanceEvent(SetTemperanceEvent evt)
        {
            var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
            if (player == null) return;
            var t = player.GetComponent<Temperance>();
            LoggingService.Append("TemperanceManagerSystem.OnSetTemperanceEvent", new System.Text.Json.Nodes.JsonObject
            {
                ["amount"] = evt.Amount
            });
            t.Amount = evt.Amount;
        }

        private void TryTriggerTemperanceAbility(Entity player, Temperance t)
        {
            var equipped = player.GetComponent<EquippedTemperanceAbility>();
            if (equipped == null || string.IsNullOrEmpty(equipped.AbilityId)) return;

            if (!Data.Temperance.TemperanceAbilityDefinitionCache.TryGet(equipped.AbilityId, out var def)) return;
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

