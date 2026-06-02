using System.Collections.Generic;
using Crusaders30XX.ECS.Objects.Events;

namespace Crusaders30XX.ECS.Factories
{
	public static class EventFactory
	{
		public static EventBase Create(string eventId)
		{
			return eventId switch
			{
				"icebound_tithe" => new IceboundTithe(),
				_ => null
			};
		}

		public static Dictionary<string, EventBase> GetAllEvents()
		{
			return new Dictionary<string, EventBase>
			{
				{ "icebound_tithe", new IceboundTithe() },
			};
		}
	}
}
