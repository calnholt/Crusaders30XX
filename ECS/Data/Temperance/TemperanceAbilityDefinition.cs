namespace Crusaders30XX.ECS.Data.Temperance
{
    public class TemperanceAbilityDefinition
    {
        public string id { get; set; }
        public string name { get; set; }
        public string target { get; set; } = "Player"; // Player | Enemy
        public string text { get; set; }
        public int threshold { get; set; } = 1;
    }
}


