using System.Text.Json.Serialization;

namespace Crusaders30XX.ECS.Data.Cards
{
    public class CardDefinition
    {
        public string id { get; set; }
        public string name { get; set; }
        public string target { get; set; } = "Enemy"; // "Enemy" | "Player"
        public string rarity { get; set; } = "Common"; // Common | Uncommon | Rare | Legendary
        public string text { get; set; }
        public string animation { get; set; }
        public bool isFreeAction { get; set; }
        public string[] cost { get; set; } = [];
        public bool isWeapon { get; set; } = false;
        public int block { get; set; } = 0;
    }
}


