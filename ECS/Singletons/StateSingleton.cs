using System;

namespace Crusaders30XX.ECS.Systems
{
	public static class StateSingleton
	{
		public static bool IsActive { get; set; } = false;
		public static bool HasPendingLocationPoiReveal { get; set; } = false;
		public static string PendingPoiId { get; set; } = null;
		public static bool PreventClicking { get; set; } = false;
	}
}


