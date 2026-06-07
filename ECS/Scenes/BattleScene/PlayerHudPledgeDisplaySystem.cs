using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Rendering;
using Crusaders30XX.ECS.Services;
using Crusaders30XX.ECS.Singletons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
	internal readonly record struct PlayerHudPledgeRenderState(
		Rectangle Bounds,
		Rectangle IconBounds,
		Rectangle TextBounds,
		int Slant,
		Color BackgroundColor,
		Color TextColor,
		float FontScale,
		int LetterSpacing);

	[DebugTab("Player HUD Pledge")]
	public class PlayerHudPledgeDisplaySystem : Core.System
	{
		private const string Label = "PLEDGE AVAILABLE";
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly Texture2D _pledgeTexture;

		public PlayerHudPledgeDisplaySystem(
			EntityManager entityManager,
			GraphicsDevice graphicsDevice,
			SpriteBatch spriteBatch,
			ContentManager content) : base(entityManager)
		{
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
			if (content != null)
			{
				try { _pledgeTexture = content.Load<Texture2D>("pledge"); }
				catch { _pledgeTexture = null; }
			}
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<PlayerHudRegion>()
				.Where(entity => entity.GetComponent<PlayerHudRegion>()?.Type == PlayerHudRegionType.Pledge);
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			var region = entity.GetComponent<PlayerHudRegion>();
			var ui = entity.GetComponent<UIElement>();
			if (region == null || ui == null) return;

			region.IsVisible = PledgeAvailabilityService.IsAvailable(EntityManager);
			ui.IsInteractable = false;
			ui.IsHovered = false;
			ui.Tooltip = string.Empty;
			ui.TooltipType = TooltipType.None;
			ui.ShowHoverHighlight = false;
		}

		public void Draw()
		{
			var state = GetRenderState();
			var font = FontSingleton.ChakraPetchFont;
			if (state == null || font == null || _graphicsDevice == null || _spriteBatch == null) return;

			var drawState = state.Value;
			float skewPercent = drawState.Bounds.Width <= 0
				? 0f
				: drawState.Slant * 100f / drawState.Bounds.Width;
			var mask = PrimitiveTextureFactory.GetParallelogramMask(
				_graphicsDevice,
				drawState.Bounds.Width,
				drawState.Bounds.Height,
				skewPercent);
			_spriteBatch.Draw(mask, drawState.Bounds, drawState.BackgroundColor);

			DrawSpacedLabel(font, drawState);
			if (_pledgeTexture != null)
			{
				_spriteBatch.Draw(
					_pledgeTexture,
					AspectFit(_pledgeTexture.Bounds, drawState.IconBounds),
					Color.White);
			}
		}

		internal PlayerHudPledgeRenderState? GetRenderState()
		{
			var root = EntityManager.GetEntitiesWithComponent<PlayerHudAnchor>().FirstOrDefault();
			var anchor = root?.GetComponent<PlayerHudAnchor>();
			var region = EntityManager.GetEntitiesWithComponent<PlayerHudRegion>()
				.Select(entity => entity.GetComponent<PlayerHudRegion>())
				.FirstOrDefault(candidate => candidate?.Type == PlayerHudRegionType.Pledge);
			if (anchor == null
				|| region == null
				|| !region.IsVisible
				|| region.Bounds.Width <= 0
				|| region.Bounds.Height <= 0)
			{
				return null;
			}

			int iconSize = Math.Min(
				Math.Max(1, anchor.PledgeIconSize),
				Math.Max(1, region.Bounds.Height - anchor.PledgePaddingVertical * 2));
			int contentLeft = region.Bounds.X + anchor.Slant + anchor.PledgePaddingLeft;
			int contentRight = region.Bounds.Right - anchor.PledgePaddingRight;
			var iconBounds = new Rectangle(
				Math.Max(contentLeft, contentRight - iconSize),
				region.Bounds.Center.Y - iconSize / 2,
				iconSize,
				iconSize);
			var textBounds = new Rectangle(
				contentLeft,
				region.Bounds.Y,
				Math.Max(0, iconBounds.X - anchor.PledgeContentGap - contentLeft),
				region.Bounds.Height);

			return new PlayerHudPledgeRenderState(
				region.Bounds,
				iconBounds,
				textBounds,
				anchor.Slant,
				anchor.HudBlack,
				anchor.HudWhite,
				anchor.LabelFontScale,
				anchor.LabelLetterSpacing);
		}

		internal static Rectangle AspectFit(Rectangle source, Rectangle destination)
		{
			if (source.Width <= 0 || source.Height <= 0
				|| destination.Width <= 0 || destination.Height <= 0)
			{
				return Rectangle.Empty;
			}

			float scale = Math.Min(
				destination.Width / (float)source.Width,
				destination.Height / (float)source.Height);
			int width = Math.Max(1, (int)Math.Round(source.Width * scale));
			int height = Math.Max(1, (int)Math.Round(source.Height * scale));
			return new Rectangle(
				destination.Center.X - width / 2,
				destination.Center.Y - height / 2,
				width,
				height);
		}

		private void DrawSpacedLabel(SpriteFont font, PlayerHudPledgeRenderState state)
		{
			float scale = Math.Max(0.01f, state.FontScale);
			float spacing = Math.Max(0f, state.LetterSpacing);
			float width = 0f;
			float height = 0f;
			foreach (char character in Label)
			{
				Vector2 size = font.MeasureString(character.ToString()) * scale;
				width += size.X;
				height = Math.Max(height, size.Y);
			}
			width += Math.Max(0, Label.Length - 1) * spacing;

			float x = state.TextBounds.Center.X - width / 2f;
			float y = state.TextBounds.Center.Y - height / 2f;
			foreach (char character in Label)
			{
				string glyph = character.ToString();
				_spriteBatch.DrawString(
					font,
					glyph,
					new Vector2(x, y),
					state.TextColor,
					0f,
					Vector2.Zero,
					scale,
					SpriteEffects.None,
					0f);
				x += font.MeasureString(glyph).X * scale + spacing;
			}
		}
	}
}
