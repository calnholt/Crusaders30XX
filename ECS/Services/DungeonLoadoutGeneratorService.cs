using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Data.Loadouts;
using Crusaders30XX.ECS.Factories;
using Crusaders30XX.ECS.Objects.Cards;
using Crusaders30XX.ECS.Data.Temperance;

namespace Crusaders30XX.ECS.Services
{
    /// <summary>
    /// Service for generating randomized loadouts for dungeon quests.
    /// </summary>
    public static class DungeonLoadoutGeneratorService
    {
        public static LoadoutDefinition GenerateRandomLoadout()
        {
            var allCards = CardFactory.GetAllCards();
            var random = Random.Shared;

            // 1. Generate Deck (30 cards, exactly 10 of each color, no duplicate ID|Color pairs, ID usage <= 2)
            var finalDeck = new List<string>();
            var cardIdUsage = new Dictionary<string, int>();
            int redLeft = 10, whiteLeft = 10, blackLeft = 10;
            int targetRelicCount = random.Next(3); // 0, 1, or 2
            int currentRelics = 0;

            // Prepare all possible unique (ID, Color) pairs
            var allPairs = new List<(string Id, string Color, bool IsRelic)>();
            foreach (var card in allCards.Values.Where(c => c.CanAddToLoadout && !c.IsToken && !c.IsWeapon))
            {
                bool isRelic = card.Type == CardType.Relic;
                allPairs.Add((card.CardId, "Red", isRelic));
                allPairs.Add((card.CardId, "White", isRelic));
                allPairs.Add((card.CardId, "Black", isRelic));
            }

            // Shuffle the pool
            allPairs = allPairs.OrderBy(_ => random.Next()).ToList();

            // Greedy selection with constraints
            foreach (var pair in allPairs)
            {
                if (finalDeck.Count >= 30) break;

                // Check Relic limit
                if (pair.IsRelic && currentRelics >= targetRelicCount) continue;

                // Check ID usage limit (max 2 per deck)
                cardIdUsage.TryGetValue(pair.Id, out int usage);
                if (usage >= 2) continue;

                // Check Color quota
                if (pair.Color == "Red" && redLeft <= 0) continue;
                if (pair.Color == "White" && whiteLeft <= 0) continue;
                if (pair.Color == "Black" && blackLeft <= 0) continue;

                // All constraints passed
                finalDeck.Add($"{pair.Id}|{pair.Color}");
                cardIdUsage[pair.Id] = usage + 1;
                
                if (pair.IsRelic) currentRelics++;
                if (pair.Color == "Red") redLeft--;
                else if (pair.Color == "White") whiteLeft--;
                else if (pair.Color == "Black") blackLeft--;
            }

            // Shuffle final deck for distribution
            finalDeck = finalDeck.OrderBy(_ => random.Next()).ToList();

            // 2. Weapon
            var weaponPool = allCards.Values
                .Where(c => c.IsWeapon && c.CanAddToLoadout)
                .Select(c => c.CardId)
                .ToList();
            string weaponId = weaponPool.Count > 0 ? weaponPool[random.Next(weaponPool.Count)] : "sword";

            // 3. Equipment (Head, Chest, Arms)
            var allEquip = EquipmentFactory.GetAllEquipment();
            
            var headPool = allEquip.Values.Where(e => e.Slot == Components.EquipmentSlot.Head).Select(e => e.Id).ToList();
            var chestPool = allEquip.Values.Where(e => e.Slot == Components.EquipmentSlot.Chest).Select(e => e.Id).ToList();
            var armsPool = allEquip.Values.Where(e => e.Slot == Components.EquipmentSlot.Arms).Select(e => e.Id).ToList();

            string headId = headPool.Count > 0 ? headPool[random.Next(headPool.Count)] : "";
            string chestId = chestPool.Count > 0 ? chestPool[random.Next(chestPool.Count)] : "";
            string armsId = armsPool.Count > 0 ? armsPool[random.Next(armsPool.Count)] : "";

            // 4. Medals (All 3)
            var allMedalIds = MedalFactory.GetAllMedals().Keys.ToList();
            var medalIds = allMedalIds.OrderBy(_ => random.Next()).Take(3).ToList();

            // 5. Temperance Ability
            var temperancePool = TemperanceAbilityDefinitionCache.GetAll().Keys.ToList();
            string temperanceId = temperancePool.Count > 0 ? temperancePool[random.Next(temperancePool.Count)] : "";

            return new LoadoutDefinition
            {
                id = "dungeon_loadout_" + Guid.NewGuid().ToString().Substring(0, 8),
                name = "Dungeon Loadout",
                cardIds = finalDeck,
                weaponId = weaponId,
                headId = headId,
                chestId = chestId,
                armsId = armsId,
                legsId = "", // Skip legs as none defined
                medalIds = medalIds,
                temperanceId = temperanceId
            };
        }
    }
}
