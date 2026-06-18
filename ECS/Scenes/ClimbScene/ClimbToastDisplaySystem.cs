using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("Climb Toast")]
	public class ClimbToastDisplaySystem : Core.System
	{
		private readonly SpriteBatch _spriteBatch;
		private readonly Texture2D _pixel;

		[DebugEditable(DisplayName = "Toast Font Scale", Step = 0.01f, Min = 0.03f, Max = 0.3f)]
		public float ToastFontScale { get; set; } = 0.09f;

		public ClimbToastDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
			: base(entityManager)
		{
			_spriteBatch = spriteBatch;
			_pixel = new Texture2D(graphicsDevice, 1, 1);
			_pixel.SetData(new[] { Color.White });
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			yield break;
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
		}

		public void Draw()
		{
			if (!IsClimbScene()) return;
			var hoveredUnaffordable = EntityManager.GetEntitiesWithComponent<ClimbSlotPresentation>()
				.Select(e => new { Slot = e.GetComponent<ClimbSlotPresentation>(), UI = e.GetComponent<UIElement>() })
				.FirstOrDefault(x => x.Slot?.Kind == ClimbSlotKind.Shop && x.UI?.IsHovered == true && x.UI.IsInteractable == false);
			if (hoveredUnaffordable == null) return;

			var rect = new Rectangle(Game1.VirtualWidth / 2 - 150, 92, 300, 34);
			_spriteBatch.Draw(_pixel, rect, Color.Black * 0.72f);
			ClimbSceneDrawHelpers.DrawBorder(_spriteBatch, _pixel, rect, ClimbSceneDrawHelpers.Red3, 1);
			ClimbSceneDrawHelpers.DrawText(_spriteBatch, "Unavailable", new Vector2(rect.X + 102, rect.Y + 9), ToastFontScale, ClimbSceneDrawHelpers.White2);
		}

		private bool IsClimbScene()
		{
			return EntityManager.GetEntitiesWithComponent<SceneState>()
				.FirstOrDefault()
				?.GetComponent<SceneState>()
				?.Current == SceneId.Climb;
		}
	}
}
