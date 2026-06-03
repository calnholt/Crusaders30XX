namespace Crusaders30XX.ECS.Data.Locations
{
	public class RunMapEvent
	{
		public string id { get; set; } = string.Empty;
		public float worldX { get; set; }
		public float worldY { get; set; }
		public string eventTypeId { get; set; } = string.Empty;
		public bool isCompleted { get; set; }
	}
}
