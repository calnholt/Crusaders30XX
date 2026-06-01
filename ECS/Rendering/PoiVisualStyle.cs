using Crusaders30XX.ECS.Components;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Rendering
{
	public static class PoiVisualStyle
	{
		public static readonly Color OpenedMapIconTint = new Color(120, 120, 120);

		public static bool UsesOpenedLookWhenComplete(PointOfInterestType type) =>
			type is PointOfInterestType.Treasure or PointOfInterestType.Quest or PointOfInterestType.Hellrift;

		public static bool IsCombatPoiType(PointOfInterestType type) =>
			type is PointOfInterestType.Quest or PointOfInterestType.Hellrift;

		public static Color GetMapIconTint(PointOfInterest poi)
		{
			if (poi == null) return Color.White;
			return poi.IsCompleted && UsesOpenedLookWhenComplete(poi.Type)
				? OpenedMapIconTint
				: Color.White;
		}
	}
}
