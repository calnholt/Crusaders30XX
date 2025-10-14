using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("Parallax Layer System")]
	public class ParallaxLayerSystem : Core.System
	{
		private readonly GraphicsDevice _graphics;
		private Vector2 _cursorPos;

		public ParallaxLayerSystem(EntityManager em, GraphicsDevice graphics)
			: base(em)
		{
			_graphics = graphics;
			EventManager.Subscribe<CursorStateEvent>(OnCursor);
		}

		private void OnCursor(CursorStateEvent evt)
		{
			_cursorPos = evt.Position;
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<ParallaxLayer>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			var layer = entity.GetComponent<ParallaxLayer>();
			var t = entity.GetComponent<Transform>();
			if (layer == null || t == null) return;

			int w = _graphics.Viewport.Width;
			int h = _graphics.Viewport.Height;
			var center = new Vector2(w / 2f, h / 2f);

			// Allow cooperating with external layouts: reconstruct base from current pos each frame
			if (layer.UpdateBaseFromCurrentEachFrame)
			{
				t.BasePosition = t.Position - layer.LastAppliedOffset;
			}
			else if (layer.CaptureBaseOnFirstUpdate)
			{
				// Capture current position as the stable base
				t.BasePosition = t.Position;
				layer.CaptureBaseOnFirstUpdate = false;
			}

			Vector2 delta = center - _cursorPos; // invert so UI moves opposite cursor
			Vector2 raw = new Vector2(delta.X * layer.MultiplierX, delta.Y * layer.MultiplierY);
			float max = System.Math.Max(0f, layer.MaxOffset);
			Vector2 offset = ClampMagnitude(raw, max);

			Vector2 target = t.BasePosition + offset;
			float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
			float smooth = layer.SmoothTime;
			float a = (smooth <= 0f) ? 1f : (1f - (float)System.Math.Exp(-dt / smooth));

			// If an external system moved us (layout), snap to target this frame to avoid easing back
			bool externallyMoved = (t.Position != layer.LastAppliedPosition) && (layer.LastAppliedPosition != Vector2.Zero);
			var newPos = externallyMoved ? target : Vector2.Lerp(t.Position, target, MathHelper.Clamp(a, 0f, 1f));
			var offsetDelta = newPos - t.Position;
			t.Position = newPos;
			layer.LastAppliedOffset = newPos - t.BasePosition;
			layer.LastAppliedPosition = newPos;

			// Keep UI bounds aligned with the parallax-adjusted transform
			var ui = entity.GetComponent<UIElement>();
			if (ui != null && (offsetDelta.X != 0 || offsetDelta.Y != 0))
			{
				ui.Bounds = new Microsoft.Xna.Framework.Rectangle(
					ui.Bounds.X + (int)System.Math.Round(offsetDelta.X),
					ui.Bounds.Y + (int)System.Math.Round(offsetDelta.Y),
					ui.Bounds.Width,
					ui.Bounds.Height
				);
			}
		}

		private static Vector2 ClampMagnitude(Vector2 v, float maxLen)
		{
			float len = v.Length();
			if (len <= maxLen || len == 0f) return v;
			return v * (maxLen / len);
		}
	}
}


