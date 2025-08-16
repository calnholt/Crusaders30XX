using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Data.Attacks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Crusaders30XX.Diagnostics;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Skeleton banner that displays current attack name, base damage (sum of on-hit Damage effects),
	/// and a simple list of leaf blocking conditions. Shown when there is a current planned attack.
	/// </summary>
	[DebugTab("Enemy Attack Display")]
	public class EnemyAttackDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly SpriteFont _font;
		private readonly Texture2D _pixel;

		[DebugEditable(DisplayName = "Center Offset X", Step = 2, Min = -1000, Max = 1000)]
		public int OffsetX { get; set; } = 0;

		[DebugEditable(DisplayName = "Center Offset Y", Step = 2, Min = -400, Max = 400)]
		public int OffsetY { get; set; } = -192;

		[DebugEditable(DisplayName = "Panel Padding", Step = 1, Min = 4, Max = 40)]
		public int PanelPadding { get; set; } = 20;

		[DebugEditable(DisplayName = "Border Thickness", Step = 1, Min = 1, Max = 8)]
		public int BorderThickness { get; set; } = 2;

		[DebugEditable(DisplayName = "Background Alpha", Step = 5, Min = 0, Max = 255)]
		public int BackgroundAlpha { get; set; } = 200;

		[DebugEditable(DisplayName = "Title Scale", Step = 0.05f, Min = 0.3f, Max = 2.5f)]
		public float TitleScale { get; set; } = 1f;

		[DebugEditable(DisplayName = "Text Scale", Step = 0.05f, Min = 0.3f, Max = 2.5f)]
		public float TextScale { get; set; } = 0.55f;

		[DebugEditable(DisplayName = "Line Spacing Extra", Step = 1, Min = 0, Max = 20)]
		public int LineSpacingExtra { get; set; } = 8;

		public EnemyAttackDisplaySystem(EntityManager em, GraphicsDevice gd, SpriteBatch sb, SpriteFont font) : base(em)
		{
			_graphicsDevice = gd;
			_spriteBatch = sb;
			_font = font;
			_pixel = new Texture2D(gd, 1, 1);
			_pixel.SetData(new[] { Color.White });
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<AttackIntent>();
		}

		protected override void UpdateEntity(Entity entity, Microsoft.Xna.Framework.GameTime gameTime) { }

		public void Draw()
		{
			var enemy = GetRelevantEntities().FirstOrDefault();
			var intent = enemy?.GetComponent<AttackIntent>();
			if (intent == null || intent.Planned.Count == 0 || _font == null) return;

			var pa = intent.Planned[0];
			var def = LoadAttackDefinition(pa.AttackId);
			if (def == null) return;

			int baseDamage = (def.effectsOnHit ?? System.Array.Empty<EffectDefinition>())
				.Where(e => e.type == "Damage")
				.Sum(e => e.amount);

			// Compose lines: Name, Damage, Leaf conditions
			var lines = new System.Collections.Generic.List<(string text, float scale, Color color)>();
			lines.Add((def.name, TitleScale, Color.White));
			lines.Add(($"Damage: {baseDamage}", TextScale, Color.White));
			AppendLeafConditions(def.conditionsBlocked, lines);

			// Measure and draw a simple panel in the center
			int pad = System.Math.Max(0, PanelPadding);
			float maxW = 0f;
			float totalH = 0f;
			foreach (var (text, scale, _) in lines)
			{
				var sz = _font.MeasureString(text);
				maxW = System.Math.Max(maxW, sz.X * scale);
				totalH += sz.Y * scale + LineSpacingExtra;
			}
			int w = (int)System.Math.Ceiling(maxW) + pad * 2;
			int h = (int)System.Math.Ceiling(totalH) + pad * 2;
			int vx = _graphicsDevice.Viewport.Width;
			int vy = _graphicsDevice.Viewport.Height;
			var rect = new Rectangle((vx - w) / 2 + OffsetX, (vy - h) / 2 + OffsetY, w, h);
			_spriteBatch.Draw(_pixel, rect, new Color(20, 20, 20, System.Math.Clamp(BackgroundAlpha, 0, 255)));
			DrawRect(rect, Color.White, System.Math.Max(1, BorderThickness));

			float y = rect.Y + pad;
			foreach (var (text, scale, color) in lines)
			{
				_spriteBatch.DrawString(_font, text, new Vector2(rect.X + pad, y), color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
				var sz = _font.MeasureString(text);
				y += sz.Y * scale + LineSpacingExtra;
			}
		}

		private AttackDefinition LoadAttackDefinition(string id)
		{
			var root = FindProjectRootContaining("Crusaders30XX.csproj");
			if (string.IsNullOrEmpty(root)) return null;
			var dir = System.IO.Path.Combine(root, "ECS", "Data", "Enemies");
			var defs = AttackRepository.LoadFromFolder(dir);
			defs.TryGetValue(id, out var def);
			return def;
		}

		private void AppendLeafConditions(ConditionNode node, System.Collections.Generic.List<(string text, float scale, Color color)> lines)
		{
			if (node == null) return;
			if (node.kind == "Leaf")
			{
				if (!string.IsNullOrEmpty(node.leafType))
				{
					if (node.leafType == "PlayColorAtLeastN")
					{
						var color = node.@params != null && node.@params.TryGetValue("color", out var c) ? c : "?";
						var n = node.@params != null && node.@params.TryGetValue("n", out var nStr) ? nStr : "?";
						lines.Add(($"Condition: Play {n} {color}", TextScale, Color.LightGray));
					}
					else
					{
						lines.Add(($"Condition: {node.leafType}", TextScale, Color.LightGray));
					}
				}
				return;
			}
			if (node.children != null)
			{
				foreach (var c in node.children)
				{
					AppendLeafConditions(c, lines);
				}
			}
		}

		private void DrawRect(Rectangle rect, Color color, int thickness)
		{
			_spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
			_spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
			_spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
			_spriteBatch.Draw(_pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
		}

		private static string FindProjectRootContaining(string filename)
		{
			try
			{
				var dir = new System.IO.DirectoryInfo(System.AppContext.BaseDirectory);
				for (int i = 0; i < 6 && dir != null; i++)
				{
					var candidate = System.IO.Path.Combine(dir.FullName, filename);
					if (System.IO.File.Exists(candidate)) return dir.FullName;
					dir = dir.Parent;
				}
			}
			catch { }
			return null;
		}
	}
}


