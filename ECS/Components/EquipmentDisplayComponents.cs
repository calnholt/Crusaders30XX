using Crusaders30XX.ECS.Core;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Components
{
	public class EquipmentDisplayRoot : IComponent
	{
		public Entity Owner { get; set; }
	}

	public class EquipmentTooltipState : IComponent
	{
		public Entity Owner { get; set; }
		public Entity EquipmentEntity { get; set; }
		public float Alpha01 { get; set; }
		public bool TargetVisible { get; set; }
		public Rectangle Bounds { get; set; }
	}
}
