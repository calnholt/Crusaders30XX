using System.Collections.Generic;
using System.Numerics;

namespace Crusaders30XX.ECS.Data.Locations
{
	public class LocationDefinition
	{
		public string id { get; set; }
		public string name { get; set; }
		public List<PointOfInterestDefinition> pointsOfInterest { get; set; } = new List<PointOfInterestDefinition>();
	}

	public class PointOfInterestDefinition
	{
		public string id { get; set; }
		public Vector2 worldPosition { get; set; } = new Vector2(0f, 0f);
		public int revealRadius { get; set; } = 300;
		public bool isRevealed { get; set; } = false;
		public int unrevealedRadius { get; set; } = 50;
		public string name { get; set; } = string.Empty;
		public string type { get; set; } = "Quest";
		public List<LocationEventDefinition> events { get; set; } = new List<LocationEventDefinition>();
	}

	public class LocationEventDefinition
	{
		public string id { get; set; }
		public string type { get; set; }
		public List<EnemyModification> modifications { get; set; } = new List<EnemyModification>();
	}

	public class EnemyModification
	{
		public string Type { get; set; }
		public int Delta { get; set; }
	}
}


