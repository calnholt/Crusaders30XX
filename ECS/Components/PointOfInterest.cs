using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Components
{
	public class PointOfInterest : IComponent
	{
		public Entity Owner { get; set; }
		public string Id { get; set; } = string.Empty;
		public Vector2 WorldPosition { get; set; } = Vector2.Zero;
		public int Difficulty { get; set; } = 0;
		public int RevealRadius { get; set; } = 300;
		public bool IsCompleted { get; set; } = false;
		public bool IsRevealed { get; set; } = false;
		public int UnrevealedRadius { get; set; } = 50;
		public float DisplayRadius { get; set; } = 0f;
		public PointOfInterestType Type { get; set; } = PointOfInterestType.Quest;
		public bool IsRevealedByProximity { get; set; } = false;
		public MusicTrack MusicTrack { get; set; } = MusicTrack.None;
	}


	public enum PointOfInterestType
	{
		Quest,
		Shop,
		Hellrift,
		Dungeon
	}
}


