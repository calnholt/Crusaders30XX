using System;
using Crusaders30XX.ECS.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Events
{
	public class BeginDefeatPresentationEvent
	{
		public Entity Enemy;
		public bool IsPreview;
	}

	public class PixelBurstAnimationRequested
	{
		public Texture2D Texture;
		public Vector2 Center;
		public Vector2 DrawTopLeft;
		public Vector2 DrawScale = Vector2.One;
		public int SourceEntityId;
		public Guid BurstId;
		public bool IsPreview;
	}

	public class PixelBurstAnimationCompleted
	{
		public Guid BurstId;
		public int SourceEntityId;
		public bool IsPreview;
	}
}
