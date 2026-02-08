using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("CV2 Connector")]
	public class ConnectorChevronDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private float _fadeAlpha;
		private int _lastHoveredIndex = -1;

		[DebugEditable(DisplayName = "Chevron Width", Step = 2, Min = 10, Max = 60)]
		public float ChevronWidth { get; set; } = 30f;

		[DebugEditable(DisplayName = "Chevron Height", Step = 2, Min = 10, Max = 40)]
		public float ChevronHeight { get; set; } = 20f;

		[DebugEditable(DisplayName = "Chevron Thickness", Step = 1, Min = 1, Max = 10)]
		public float ChevronThickness { get; set; } = 4f;

		[DebugEditable(DisplayName = "Chevron Count", Step = 1, Min = 1, Max = 5)]
		public int ChevronCount { get; set; } = 3;

		[DebugEditable(DisplayName = "Chevron Gap", Step = 1, Min = 0, Max = 10)]
		public float ChevronGap { get; set; } = 4f;

		[DebugEditable(DisplayName = "Chevron R", Step = 1, Min = 0, Max = 255)]
		public int ChevronR { get; set; } = 196;

		[DebugEditable(DisplayName = "Chevron G", Step = 1, Min = 0, Max = 255)]
		public int ChevronG { get; set; } = 30;

		[DebugEditable(DisplayName = "Chevron B", Step = 1, Min = 0, Max = 255)]
		public int ChevronB { get; set; } = 58;

		[DebugEditable(DisplayName = "Chevron Max Alpha", Step = 5, Min = 0, Max = 255)]
		public int ChevronMaxAlpha { get; set; } = 140;

		[DebugEditable(DisplayName = "Fade Duration", Step = 0.05f, Min = 0.05f, Max = 1.0f)]
		public float FadeDuration { get; set; } = 0.25f;

		public WheelLayoutSystem LayoutSystem { get; set; }

		public ConnectorChevronDisplaySystem(EntityManager em, GraphicsDevice gd, SpriteBatch sb) : base(em)
		{
			_graphicsDevice = gd;
			_spriteBatch = sb;
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<SceneState>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			var scene = entity.GetComponent<SceneState>();
			if (scene == null || scene.Current != SceneId.CustomizationV2) return;

			var nav = EntityManager.GetEntitiesWithComponent<CustomizationV2NavigationState>().FirstOrDefault()?.GetComponent<CustomizationV2NavigationState>();
			if (nav == null || nav.ActiveTab != CustomizationV2TabType.Loadout) return;

			var loadout = EntityManager.GetEntitiesWithComponent<CustomizationV2LoadoutState>().FirstOrDefault()?.GetComponent<CustomizationV2LoadoutState>();
			int hovered = loadout?.HoveredSegmentIndex ?? -1;

			float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

			if (hovered != _lastHoveredIndex)
			{
				_lastHoveredIndex = hovered;
			}

			// Fade in/out
			if (hovered >= 0)
			{
				_fadeAlpha = Math.Min(1f, _fadeAlpha + dt / FadeDuration);
			}
			else
			{
				_fadeAlpha = Math.Max(0f, _fadeAlpha - dt / FadeDuration);
			}
		}

		public void Draw()
		{
			if (_fadeAlpha <= 0.01f) return;

			var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
			if (scene == null || scene.Current != SceneId.CustomizationV2) return;

			var nav = EntityManager.GetEntitiesWithComponent<CustomizationV2NavigationState>().FirstOrDefault()?.GetComponent<CustomizationV2NavigationState>();
			if (nav == null || nav.ActiveTab != CustomizationV2TabType.Loadout) return;

			if (LayoutSystem == null) return;

			int hovered = _lastHoveredIndex;
			if (hovered < 0) return;

			var center = LayoutSystem.GetWheelCenter();
			var segLayout = LayoutSystem.ComputeSegmentLayout(hovered);
			var segPos = segLayout.position;

			// Direction from segment toward center
			var dir = center - segPos;
			if (dir.LengthSquared() < 1f) return;
			dir.Normalize();

			// Position chevron midway between segment and center
			float midDist = Vector2.Distance(segPos, center) * 0.5f;
			var chevronPos = segPos + dir * midDist;

			// Rotation: chevrons point toward center
			float rotation = MathF.Atan2(dir.Y, dir.X) + MathHelper.PiOver2;

			var chevronTex = PrimitiveTextureFactory.GetAntialiasedChevronMask(
				_graphicsDevice, ChevronWidth, ChevronHeight, ChevronThickness, ChevronCount, ChevronGap);

			var origin = new Vector2(chevronTex.Width / 2f, chevronTex.Height / 2f);
			var color = new Color(ChevronR, ChevronG, ChevronB) * (_fadeAlpha * ChevronMaxAlpha / 255f);

			_spriteBatch.Draw(chevronTex, chevronPos, null, color, rotation, origin, 1f, SpriteEffects.None, 0f);
		}
	}
}
