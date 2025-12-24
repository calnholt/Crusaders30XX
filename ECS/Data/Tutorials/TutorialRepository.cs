using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Crusaders30XX.ECS.Data.Tutorials
{
    public static class TutorialRepository
    {
        public static Dictionary<string, TutorialDefinition> LoadFromFolder(string folderAbsPath)
        {
            var map = new Dictionary<string, TutorialDefinition>();
            if (!Directory.Exists(folderAbsPath)) return map;
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            foreach (var file in Directory.GetFiles(folderAbsPath, "*.json"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var root = JsonNode.Parse(json);
                    if (root == null) continue;

                    // Accept either top-level array or object with tutorials property
                    List<TutorialDefinition> tutorials = null;
                    if (root is JsonArray arr)
                    {
                        tutorials = arr.Deserialize<List<TutorialDefinition>>(opts);
                    }
                    else if (root["tutorials"] is JsonArray tutorialsArr)
                    {
                        tutorials = tutorialsArr.Deserialize<List<TutorialDefinition>>(opts);
                    }

                    if (tutorials == null) continue;
                    foreach (var tutorial in tutorials)
                    {
                        if (!string.IsNullOrEmpty(tutorial.key))
                        {
                            map[tutorial.key] = tutorial;
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    System.Console.WriteLine($"[TutorialRepository] Failed to parse {file}: {ex.Message}");
                }
            }
            return map;
        }
    }
}

