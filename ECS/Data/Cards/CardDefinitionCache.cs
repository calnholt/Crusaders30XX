using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Crusaders30XX.ECS.Systems;

namespace Crusaders30XX.ECS.Data.Cards
{
    public static class CardDefinitionCache
    {
        private static Dictionary<string, CardDefinition> _cache;
        private static string _folderPath;
        private static readonly object _lock = new object();

        public static bool TryGet(string id, out CardDefinition def)
        {
            EnsureLoaded();
            return _cache.TryGetValue(id, out def);
        }

        public static Dictionary<string, CardDefinition> GetAll()
        {
            EnsureLoaded();
            return _cache;
        }

        public static void Reload()
        {
            lock (_lock)
            {
                var folder = ResolveFolderPath();
                _cache = CardRepository.LoadFromFolder(folder);
                PostProcessDefinitions(_cache);
            }
        }

        private static void EnsureLoaded()
        {
            if (_cache != null) return;
            lock (_lock)
            {
                if (_cache == null)
                {
                    var folder = ResolveFolderPath();
                    _cache = CardRepository.LoadFromFolder(folder);
                    PostProcessDefinitions(_cache);
                }
            }
        }

        // Extract values from brackets in text and populate valuesParse, then replace brackets with values.
        private static void PostProcessDefinitions(Dictionary<string, CardDefinition> cache)
        {
            if (cache == null) return;
            foreach (var kv in cache)
            {
                var def = kv.Value;
                if (def == null) continue;
                if (!string.IsNullOrEmpty(def?.text))
                {
                    // Extract numeric values from {n} patterns in text
                    var valuesList = new List<int>();
                    var pattern = @"\{(\d+)\}";
                    var matches = Regex.Matches(def.text, pattern);
                    
                    foreach (Match match in matches)
                    {
                        if (int.TryParse(match.Groups[1].Value, out int value))
                        {
                            valuesList.Add(value);
                        }
                    }
                    
                    // Populate valuesParse with extracted values
                    def.valuesParse = valuesList.ToArray();
                    
                    // Replace brackets with numeric values in text (e.g., {4} -> 4)
                    string resolved = def.text;
                    foreach (Match match in matches)
                    {
                        resolved = resolved.Replace(match.Value, match.Groups[1].Value);
                    }
                    def.text = resolved;
                }
                // Compute keyword tooltip once per definition
                try
                {
                    def.tooltip = KeywordTooltipTextService.GetTooltip(def.text);
                }
                catch { }
            }
        }

        private static string ResolveFolderPath()
        {
            if (!string.IsNullOrEmpty(_folderPath) && Directory.Exists(_folderPath)) return _folderPath;
            _folderPath = Path.Combine(System.AppContext.BaseDirectory, "Content", "Data", "Cards");
            return _folderPath;
        }
    }
}


