using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Crusaders30XX.ECS.Data.Dialog
{
    public static class DialogRepository
    {
        public static Dictionary<string, DialogDefinition> LoadFromFolder(string folderAbsPath)
        {
            var map = new Dictionary<string, DialogDefinition>();
            if (!Directory.Exists(folderAbsPath)) return map;
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            foreach (var file in Directory.GetFiles(folderAbsPath, "*.json"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var root = JsonNode.Parse(json);
                    if (root == null) continue;

                    // Accept either top-level array or object with lines property
                    List<DialogLine> lines = null;
                    if (root is JsonArray arr)
                    {
                        lines = arr.Deserialize<List<DialogLine>>(opts);
                    }
                    else if (root["lines"] is JsonArray linesArr)
                    {
                        lines = linesArr.Deserialize<List<DialogLine>>(opts);
                    }

                    if (lines == null) continue;
                    string id = Path.GetFileNameWithoutExtension(file);
                    map[id] = new DialogDefinition { id = id, lines = lines };
                }
                catch (System.Exception ex)
                {
                    System.Console.WriteLine($"[DialogRepository] Failed to parse {file}: {ex.Message}");
                }
            }
            return map;
        }
    }
}


