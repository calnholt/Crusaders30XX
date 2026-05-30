using System.Collections.Generic;

namespace Crusaders30XX.ECS.Data.Locations
{
	public class RunMapNode
	{
		public string id { get; set; } = string.Empty;
		public float worldX { get; set; }
		public float worldY { get; set; }
		public string enemyId { get; set; } = string.Empty;
		public int parentIndex { get; set; } = -1;
		public List<int> childIndices { get; set; } = new List<int>();
		public bool isRevealed { get; set; }
		public bool isCompleted { get; set; }
	}
}
