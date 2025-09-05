using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Crusaders30XX.ECS.Data.Temperance
{
    public static class TemperanceAbilityRepository
    {
        public static Dictionary<string, TemperanceAbilityDefinition> LoadFromFolder(string folderAbsPath)
        {
            var map = new Dictionary<string, TemperanceAbilityDefinition>();
            if (!Directory.Exists(folderAbsPath)) return map;
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            foreach (var file in Directory.GetFiles(folderAbsPath, "*.json"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var def = JsonSerializer.Deserialize<TemperanceAbilityDefinition>(json, opts);
                    if (def?.id != null) map[def.id] = def;
                }
                catch (System.Exception ex)
                {
                    System.Console.WriteLine($"[TemperanceAbilityRepository] Failed to parse {file}: {ex.Message}");
                }
            }
            return map;
        }
    }
}


