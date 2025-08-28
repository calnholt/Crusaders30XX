using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Crusaders30XX.ECS.Data.Cards
{
    public static class CardRepository
    {
        public static Dictionary<string, CardDefinition> LoadFromFolder(string folderAbsPath)
        {
            var map = new Dictionary<string, CardDefinition>();
            if (!Directory.Exists(folderAbsPath)) return map;
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            foreach (var file in Directory.GetFiles(folderAbsPath, "*.json"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var def = JsonSerializer.Deserialize<CardDefinition>(json, opts);
                    if (def?.id != null) map[def.id] = def;
                }
                catch (System.Exception ex)
                {
                    System.Console.WriteLine($"[CardRepository] Failed to parse {file}: {ex.Message}");
                }
            }
            return map;
        }
    }
}


