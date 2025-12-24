using System.Collections.Generic;
using System.IO;

namespace Crusaders30XX.ECS.Data.Tutorials
{
    public static class TutorialDefinitionCache
    {
        private static Dictionary<string, TutorialDefinition> _cache;
        private static string _folderPath;
        private static readonly object _lock = new object();

        public static Dictionary<string, TutorialDefinition> GetAll()
        {
            EnsureLoaded();
            return _cache;
        }

        public static bool TryGet(string key, out TutorialDefinition def)
        {
            EnsureLoaded();
            return _cache.TryGetValue(key, out def);
        }

        public static void Reload()
        {
            lock (_lock)
            {
                var folder = ResolveFolderPath();
                _cache = TutorialRepository.LoadFromFolder(folder);
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
                    _cache = TutorialRepository.LoadFromFolder(folder);
                }
            }
        }

        private static string ResolveFolderPath()
        {
            if (!string.IsNullOrEmpty(_folderPath) && Directory.Exists(_folderPath)) return _folderPath;
            _folderPath = Path.Combine(System.AppContext.BaseDirectory, "Content", "Data", "Tutorials");
            return _folderPath;
        }
    }
}

