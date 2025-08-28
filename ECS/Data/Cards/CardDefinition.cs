using System.Text.Json.Serialization;

namespace Crusaders30XX.ECS.Data.Cards
{
    public class CardDefinition
    {
        public string id { get; set; }
        public string name { get; set; }
        public string target { get; set; } = "Enemy"; // "Enemy" | "Player"
        public string color { get; set; } = "Red";     // Red | White | Black
        public string rarity { get; set; } = "Common"; // Common | Uncommon | Rare | Legendary
        public string text { get; set; }
    }
}


