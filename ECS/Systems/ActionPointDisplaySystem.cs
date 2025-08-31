using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Crusaders30XX.ECS.Rendering;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Draws the player's Action Points as red pips on a rounded black background below the player portrait.
	/// </summary>
	public class ActionPointDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;

		// Cached textures keyed by (w,h,r)
		private Texture2D _pipTexture;
		private readonly System.Collections.Generic.Dictionary<(int w,int h,int r), Texture2D> _bgCache = new();

		// Layout settings
		public int PipDiameter { get; set; } = 18;
		public int PipSpacing { get; set; } = 6;
		public int PaddingX { get; set; } = 12;
		public int PaddingY { get; set; } = 8;
		public int CornerRadius { get; set; } = 10;
		public int AnchorOffsetY { get; set; } = 290;

		public ActionPointDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
			: base(entityManager)
		{
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<Player>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		public void Draw()
		{
			// Only render during Action phase
			var phase = EntityManager.GetEntitiesWithComponent<BattlePhaseState>().FirstOrDefault()?.GetComponent<BattlePhaseState>()?.Phase ?? BattlePhase.StartOfBattle;
			if (phase != BattlePhase.Action) return;
			var player = GetRelevantEntities().FirstOrDefault();
			if (player == null) return;
			var t = player.GetComponent<Transform>();
			var info = player.GetComponent<PortraitInfo>();
			var ap = player.GetComponent<ActionPoints>();
			if (t == null || info == null || ap == null) return;

			int count = System.Math.Max(0, ap.Current);
			if (count <= 0) return;

			int d = System.Math.Max(4, PipDiameter);
			int spacing = System.Math.Max(0, PipSpacing);
			int padX = System.Math.Max(0, PaddingX);
			int padY = System.Math.Max(0, PaddingY);
			int radius = System.Math.Max(0, CornerRadius);

			int innerWidth = count * d + (count - 1) * spacing;
			int innerHeight = d;
			int bgW = innerWidth + padX * 2;
			int bgH = innerHeight + padY * 2;

			// Center below portrait anchor
			var center = new Vector2(t.Position.X, t.Position.Y + AnchorOffsetY);
			var topLeft = new Vector2(center.X - bgW / 2f, center.Y - bgH / 2f);

			// Background
			var bg = GetOrCreateBackground(bgW, bgH, radius);
			_spriteBatch.Draw(bg, position: topLeft, color: Color.Black);

			// Pips
			var pip = GetOrCreatePipTexture(d);
			float startX = topLeft.X + padX + d / 2f;
			float y = topLeft.Y + padY + d / 2f;
			for (int i = 0; i < count; i++)
			{
				var pos = new Vector2(startX + i * (d + spacing), y);
				_spriteBatch.Draw(pip, position: pos, sourceRectangle: null, color: Color.Red, rotation: 0f, origin: new Vector2(d / 2f, d / 2f), scale: Vector2.One, effects: SpriteEffects.None, layerDepth: 0f);
			}
		}

		private Texture2D GetOrCreateBackground(int w, int h, int r)
		{
			var key = (w, h, r);
			if (_bgCache.TryGetValue(key, out var tex) && tex != null) return tex;
			tex = RoundedRectTextureFactory.CreateRoundedRect(_graphicsDevice, w, h, r);
			_bgCache[key] = tex;
			return tex;
		}

		private Texture2D GetOrCreatePipTexture(int diameter)
		{
			if (_pipTexture != null && _pipTexture.Width == diameter && _pipTexture.Height == diameter) return _pipTexture;
			_pipTexture?.Dispose();
			_pipTexture = CreateFilledCircleTexture(_graphicsDevice, diameter);
			return _pipTexture;
		}

		private static Texture2D CreateFilledCircleTexture(GraphicsDevice device, int diameter)
		{
			int r = System.Math.Max(1, diameter / 2);
			int w = r * 2;
			int h = r * 2;
			var tex = new Texture2D(device, w, h);
			var data = new Color[w * h];
			int r2 = r * r;
			for (int y = 0; y < h; y++)
			{
				int dy = y - r;
				for (int x = 0; x < w; x++)
				{
					int dx = x - r;
					data[y * w + x] = (dx * dx + dy * dy) <= r2 ? Color.White : Color.Transparent;
				}
			}
			tex.SetData(data);
			return tex;
		}
	}
}


