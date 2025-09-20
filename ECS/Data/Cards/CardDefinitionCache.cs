using System.Collections.Generic;
using System.IO;

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

        // Replace placeholders in text using valuesParse so all systems see resolved text.
        private static void PostProcessDefinitions(Dictionary<string, CardDefinition> cache)
        {
            if (cache == null) return;
            foreach (var kv in cache)
            {
                var def = kv.Value;
                if (def == null) continue;
                if (!string.IsNullOrEmpty(def?.text) && def.valuesParse != null && def.valuesParse.Length > 0)
                {
                    string resolved = def.text;
                    for (int i = 0; i < def.valuesParse.Length; i++)
                    {
                        // Replace occurrences like {1}, {2}, ... with corresponding values
                        resolved = resolved.Replace($"{{{i + 1}}}", def.valuesParse[i].ToString());
                    }
                    def.text = resolved;
                }
            }
        }

        private static string ResolveFolderPath()
        {
            if (!string.IsNullOrEmpty(_folderPath) && Directory.Exists(_folderPath)) return _folderPath;
            string root = FindProjectRootContaining("Crusaders30XX.csproj");
            _folderPath = string.IsNullOrEmpty(root) ? string.Empty : Path.Combine(root, "ECS", "Data", "Cards");
            return _folderPath;
        }

        private static string FindProjectRootContaining(string filename)
        {
            try
            {
                var dir = new DirectoryInfo(System.AppContext.BaseDirectory);
                for (int i = 0; i < 6 && dir != null; i++)
                {
                    var candidate = Path.Combine(dir.FullName, filename);
                    if (File.Exists(candidate)) return dir.FullName;
                    dir = dir.Parent;
                }
            }
            catch { }
            return null;
        }
    }
}


