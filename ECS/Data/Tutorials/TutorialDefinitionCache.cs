using System.Collections.Generic;
using System.Linq;

namespace Crusaders30XX.ECS.Data.Tutorials
{
    public static class TutorialDefinitionCache
    {
        private static readonly Dictionary<string, TutorialDefinition> Definitions =
            BuildDefinitions();

        public static Dictionary<string, TutorialDefinition> GetAll()
        {
            return Definitions;
        }

        public static bool TryGet(string key, out TutorialDefinition def)
        {
            return Definitions.TryGetValue(key, out def);
        }

        public static void Reload() { }

        private static Dictionary<string, TutorialDefinition> BuildDefinitions()
        {
            var definitions = GuidedTutorialDefinitions.GuidedMessages
                .ToDictionary(def => def.key, def => def);

            void Add(string key, string text, string targetType, string targetId, string orientation, string condition = null)
            {
                definitions[key] = new TutorialDefinition
                {
                    key = key,
                    text = text,
                    targetType = targetType,
                    targetId = targetId,
                    bubbleOrientation = orientation,
                    condition = condition,
                };
            }

            Add("equipment", "Equipment can block or activate for an effect. Most equipment has limited uses.", "equipment", "Equipment", "right", "has_equipment");
            Add("medal", "Medals provide passive benefits. You can equip up to three.", "medal", "Medal", "bottom", "has_medal");
            Add("tribulation", "Tribulations change a quest. Hover over the chalice to review the current effects.", "entity_name", "TribulationChalice", "right", "has_tribulation");
            Add("threat", "Threat increases enemy aggression at the start of its turn.", "entity_name", "UI_ThreatTooltip", "right", "threat_enabled");
            return definitions;
        }
    }
}
