using System;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Data.Attacks;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("Attack Data (JSON)")]
	public class AttackDataDebugSystem : Core.System
	{
		private System.Collections.Generic.Dictionary<string, AttackDefinition> _cache;

		public AttackDataDebugSystem(EntityManager em) : base(em) { }

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return Array.Empty<Entity>();
		}

		protected override void UpdateEntity(Entity entity, Microsoft.Xna.Framework.GameTime gameTime) { }

		[DebugAction("Load Attacks from /ECS/Data/Enemies")]
		public void LoadAttacks()
		{
			// Use absolute to the workspace root
			string root = AppContext.BaseDirectory;
			// Walk up to project root: BaseDirectory points to bin/Debug/...; so traverse up until we find Crusaders30XX.csproj sibling
			string folder = FindProjectRootContaining("Crusaders30XX.csproj");
			if (string.IsNullOrEmpty(folder))
			{
				Console.WriteLine("[AttackData] Could not locate project root");
				return;
			}
			string attacksDir = System.IO.Path.Combine(folder, "ECS", "Data", "Enemies");
			_cache = AttackRepository.LoadFromFolder(attacksDir);
			Console.WriteLine($"[AttackData] Loaded {_cache.Count} attacks from {attacksDir}");
			if (_cache.TryGetValue("demon_bite", out var demo))
			{
				Console.WriteLine($"[AttackData] demon_bite: name={demo.name}, target={demo.target}, step={demo.resolveStep}");
			}
		}

		private static string FindProjectRootContaining(string filename)
		{
			try
			{
				var dir = new System.IO.DirectoryInfo(AppContext.BaseDirectory);
				for (int i = 0; i < 6 && dir != null; i++)
				{
					var candidate = System.IO.Path.Combine(dir.FullName, filename);
					if (System.IO.File.Exists(candidate)) return dir.FullName;
					dir = dir.Parent;
				}
			}
			catch { }
			return null;
		}
	}
}


