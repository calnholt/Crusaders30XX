using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Rendering;
using Crusaders30XX.ECS.Singletons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("CV2 Browse Arrows")]
	public class BrowseArrowDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly SpriteFont _headingFont = FontSingleton.TitleFont;
		private InputMethod _inputSource;

		[DebugEditable(DisplayName = "Button Radius", Step = 1, Min = 8, Max = 30)]
		public int ButtonRadius { get; set; } = 13;

		[DebugEditable(DisplayName = "Button Border Width", Step = 1, Min = 1, Max = 4)]
		public int ButtonBorderWidth { get; set; } = 1;

		[DebugEditable(DisplayName = "Button BG R", Step = 1, Min = 0, Max = 255)]
		public int ButtonBgR { get; set; } = 10;

		[DebugEditable(DisplayName = "Button BG G", Step = 1, Min = 0, Max = 255)]
		public int ButtonBgG { get; set; } = 10;

		[DebugEditable(DisplayName = "Button BG B", Step = 1, Min = 0, Max = 255)]
		public int ButtonBgB { get; set; } = 10;

		[DebugEditable(DisplayName = "Button Border R", Step = 1, Min = 0, Max = 255)]
		public int ButtonBorderR { get; set; } = 51;

		[DebugEditable(DisplayName = "Button Border G", Step = 1, Min = 0, Max = 255)]
		public int ButtonBorderG { get; set; } = 51;

		[DebugEditable(DisplayName = "Button Border B", Step = 1, Min = 0, Max = 255)]
		public int ButtonBorderB { get; set; } = 51;

		[DebugEditable(DisplayName = "Arrow Text Scale", Step = 0.01f, Min = 0.03f, Max = 0.2f)]
		public float ArrowTextScale { get; set; } = 0.10f;

		[DebugEditable(DisplayName = "Counter Scale", Step = 0.01f, Min = 0.03f, Max = 0.15f)]
		public float CounterScale { get; set; } = 0.09f;

		[DebugEditable(DisplayName = "Arrow Offset X", Step = 4, Min = 40, Max = 200)]
		public int ArrowOffsetX { get; set; } = 120;

		[DebugEditable(DisplayName = "Arrow Offset Y", Step = 4, Min = -60, Max = 60)]
		public int ArrowOffsetY { get; set; } = 40;

		[DebugEditable(DisplayName = "Key Hint Scale", Step = 0.01f, Min = 0.03f, Max = 0.15f)]
		public float KeyHintScale { get; set; } = 0.07f;

		[DebugEditable(DisplayName = "Key Hint Offset Y", Step = 2, Min = 0, Max = 40)]
		public int KeyHintOffsetY { get; set; } = 24;

		public WheelLayoutSystem LayoutSystem { get; set; }
		public BrowseStateSystem BrowseSystem { get; set; }

		public BrowseArrowDisplaySystem(EntityManager em, GraphicsDevice gd, SpriteBatch sb) : base(em)
		{
			_graphicsDevice = gd;
			_spriteBatch = sb;
			EventManager.Subscribe<CursorStateEvent>(e => _inputSource = e.Source);
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<SceneState>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		public void Draw()
		{
			var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
			if (scene == null || scene.Current != SceneId.CustomizationV2) return;

			var nav = EntityManager.GetEntitiesWithComponent<CustomizationV2NavigationState>().FirstOrDefault()?.GetComponent<CustomizationV2NavigationState>();
			if (nav == null || nav.ActiveTab != CustomizationV2TabType.Loadout) return;

			var loadout = EntityManager.GetEntitiesWithComponent<CustomizationV2LoadoutState>().FirstOrDefault()?.GetComponent<CustomizationV2LoadoutState>();
			if (loadout == null || loadout.HoveredSegmentIndex < 0) return;

			if (LayoutSystem == null || BrowseSystem == null) return;
			if (BrowseSystem.GetBrowseCount() <= 1) return;

			var center = LayoutSystem.GetWheelCenter();
			float btnY = center.Y + ArrowOffsetY;

			// Left arrow button
			DrawCircleButton(center.X - ArrowOffsetX, btnY, "<", Color.White);

			// Right arrow button
			DrawCircleButton(center.X + ArrowOffsetX, btnY, ">", Color.White);

			// Browse counter between arrows
			int browseIdx = BrowseSystem.GetBrowseIndex();
			int browseCount = BrowseSystem.GetBrowseCount();
			string counter = $"{browseIdx + 1}/{browseCount}";
			var counterSize = _headingFont.MeasureString(counter) * CounterScale;
			float cx = center.X - counterSize.X / 2f;
			float cy = btnY - counterSize.Y / 2f;
			_spriteBatch.DrawString(_headingFont, counter, new Vector2(cx, cy), new Color(102, 102, 102), 0f, Vector2.Zero, CounterScale, SpriteEffects.None, 0f);

			// Key hints
			bool gamepad = _inputSource == InputMethod.Gamepad;
			DrawKeyHint(center.X - ArrowOffsetX, btnY + ButtonRadius + KeyHintOffsetY, gamepad ? "D<" : "A");
			DrawKeyHint(center.X + ArrowOffsetX, btnY + ButtonRadius + KeyHintOffsetY, gamepad ? "D>" : "D");
		}

		private void DrawCircleButton(float x, float y, string symbol, Color textColor)
		{
			var borderColor = new Color(ButtonBorderR, ButtonBorderG, ButtonBorderB);
			var bgColor = new Color(ButtonBgR, ButtonBgG, ButtonBgB);

			// Outer circle (border)
			int outerRadius = ButtonRadius + ButtonBorderWidth;
			var outerCircle = PrimitiveTextureFactory.GetAntiAliasedCircle(_graphicsDevice, outerRadius);
			int od = outerRadius * 2;
			_spriteBatch.Draw(outerCircle, new Rectangle((int)(x - outerRadius), (int)(y - outerRadius), od, od), borderColor);

			// Inner circle (background)
			var innerCircle = PrimitiveTextureFactory.GetAntiAliasedCircle(_graphicsDevice, ButtonRadius);
			int id = ButtonRadius * 2;
			_spriteBatch.Draw(innerCircle, new Rectangle((int)(x - ButtonRadius), (int)(y - ButtonRadius), id, id), bgColor);

			// Arrow symbol centered inside
			var symbolSize = _headingFont.MeasureString(symbol) * ArrowTextScale;
			float sx = x - symbolSize.X / 2f;
			float sy = y - symbolSize.Y / 2f;
			_spriteBatch.DrawString(_headingFont, symbol, new Vector2(sx, sy), textColor, 0f, Vector2.Zero, ArrowTextScale, SpriteEffects.None, 0f);
		}

		private void DrawKeyHint(float x, float y, string key)
		{
			var size = _headingFont.MeasureString(key) * KeyHintScale;
			float kx = x - size.X / 2f;
			_spriteBatch.DrawString(_headingFont, key, new Vector2(kx, y), new Color(102, 102, 102), 0f, Vector2.Zero, KeyHintScale, SpriteEffects.None, 0f);
		}
	}
}
