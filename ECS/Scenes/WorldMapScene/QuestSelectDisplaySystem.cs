using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Locations;
using Crusaders30XX.ECS.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("Quest Select")]
	public class QuestSelectDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphics;
		private readonly SpriteBatch _sb;
		private readonly ContentManager _content;
		private readonly SpriteFont _font;
		private Texture2D _pixel;
		private readonly Dictionary<(bool right, int size), Texture2D> _arrowCache = new();
		private Texture2D _roundedCache;

		[DebugEditable(DisplayName = "Back Btn Width", Step = 10, Min = 60, Max = 600)]
		public int BackButtonWidth { get; set; } = 180;

		[DebugEditable(DisplayName = "Back Btn Height", Step = 5, Min = 24, Max = 200)]
		public int BackButtonHeight { get; set; } = 56;

		[DebugEditable(DisplayName = "Back Btn Corner", Step = 1, Min = 0, Max = 60)]
		public int BackButtonCornerRadius { get; set; } = 12;

		[DebugEditable(DisplayName = "Back Btn Offset X", Step = 2, Min = -200, Max = 400)]
		public int BackButtonOffsetX { get; set; } = 16;

		[DebugEditable(DisplayName = "Back Btn Offset Bottom", Step = 2, Min = 0, Max = 400)]
		public int BackButtonOffsetBottom { get; set; } = 16;

		[DebugEditable(DisplayName = "Back Btn Label Scale", Step = 0.05f, Min = 0.2f, Max = 2f)]
		public float BackButtonLabelScale { get; set; } = 0.2f;

		[DebugEditable(DisplayName = "Panel Width", Step = 16, Min = 100, Max = 1920)]
		public int PanelWidth { get; set; } = 900;

		[DebugEditable(DisplayName = "Panel Height", Step = 16, Min = 100, Max = 1080)]
		public int PanelHeight { get; set; } = 420;

		[DebugEditable(DisplayName = "Corner Radius", Step = 2, Min = 0, Max = 300)]
		public int CornerRadius { get; set; } = 24;

		[DebugEditable(DisplayName = "Panel Y Offset", Step = 4, Min = -2000, Max = 2000)]
		public int PanelYOffset { get; set; } = 0;

		[DebugEditable(DisplayName = "Panel Padding", Step = 2, Min = 0, Max = 200)]
		public int PanelPadding { get; set; } = 16;

		[DebugEditable(DisplayName = "Title Scale", Step = 0.05f, Min = 0.2f, Max = 2f)]
		public float TitleScale { get; set; } = 0.5f;

		[DebugEditable(DisplayName = "Enemy Scale", Step = 0.05f, Min = 0.1f, Max = 3f)]
		public float EnemyScale { get; set; } = 0.15f;

		[DebugEditable(DisplayName = "Enemy Spacing", Step = 4, Min = 0, Max = 400)]
		public int EnemySpacing { get; set; } = 16;

		[DebugEditable(DisplayName = "Arrow Size", Step = 4, Min = 10, Max = 400)]
		public int ArrowSize { get; set; } = 80;

		[DebugEditable(DisplayName = "Arrow Offset X", Step = 2, Min = -400, Max = 400)]
		public int ArrowOffsetX { get; set; } = 40;

		[DebugEditable(DisplayName = "Arrow Offset Y", Step = 2, Min = -400, Max = 400)]
		public int ArrowOffsetY { get; set; } = 0;

		public QuestSelectDisplaySystem(EntityManager em, GraphicsDevice graphics, SpriteBatch sb, ContentManager content, SpriteFont font) : base(em)
		{
			_graphics = graphics;
			_sb = sb;
			_content = content;
			_font = font;
			_pixel = new Texture2D(graphics, 1, 1);
			_pixel.SetData(new[] { Color.White });
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<QuestSelectState>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			var qs = entity.GetComponent<QuestSelectState>();
			if (qs == null || !qs.IsOpen) return;

			// Position arrow UI elements outside the panel and handle clicks
			var panel = GetPanelRect();
			var leftRect = new Rectangle(panel.Left - ArrowSize - ArrowOffsetX, panel.Top + panel.Height / 2 - ArrowSize / 2 + ArrowOffsetY, ArrowSize, ArrowSize);
			var rightRect = new Rectangle(panel.Right + ArrowOffsetX, panel.Top + panel.Height / 2 - ArrowSize / 2 + ArrowOffsetY, ArrowSize, ArrowSize);

			EnsureArrowEntity<QuestArrowLeft>("QuestArrowLeft", leftRect, qs.SelectedQuestIndex > 0);
			EnsureArrowEntity<QuestArrowRight>("QuestArrowRight", rightRect, HasMoreRight(qs));

			HandleArrowClicks(qs);

			// Back button anchored bottom-left of screen
			int w = _graphics.Viewport.Width;
			int h = _graphics.Viewport.Height;
			int btnW = BackButtonWidth;
			int btnH = BackButtonHeight;
			var backRect = new Rectangle(BackButtonOffsetX, h - btnH - BackButtonOffsetBottom, btnW, btnH);
			EnsureBackButton(backRect);
			HandleBackButtonClick(qs);
		}

		public void Draw()
		{
			var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
			if (scene == null || scene.Current != SceneId.WorldMap) return;
			var qs = EntityManager.GetEntitiesWithComponent<QuestSelectState>().FirstOrDefault()?.GetComponent<QuestSelectState>();
			if (qs == null || !qs.IsOpen || string.IsNullOrEmpty(qs.LocationId)) return;

			var panel = GetPanelRect();
			DrawPanel(panel);

			// Title
			string title = "Quest " + (qs.SelectedQuestIndex + 1).ToString();
			var titleSize = _font.MeasureString(title) * TitleScale;
			var titlePos = new Vector2(panel.X + panel.Width / 2f - titleSize.X / 2f, panel.Y + PanelPadding);
			_sb.DrawString(_font, title, titlePos, Color.White, 0f, Vector2.Zero, TitleScale, SpriteEffects.None, 0f);

			// Enemy icons row
			var all = LocationDefinitionCache.GetAll();
			if (!all.TryGetValue(qs.LocationId, out var loc) || loc?.quests == null || loc.quests.Count == 0) return;
			int unlockedMax = System.Math.Max(0, EntityManager == null ? 0 : Crusaders30XX.ECS.Data.Save.SaveCache.GetValueOrDefault(qs.LocationId, 0));
			int clampedIndex = System.Math.Max(0, System.Math.Min(qs.SelectedQuestIndex, System.Math.Min(unlockedMax, loc.quests.Count - 1)));
			var quests = loc.quests[clampedIndex];
			var textures = new List<(Texture2D tex, string id)>();
			foreach (var quest in quests)
			{
				Texture2D t = TryLoadEnemyTexture(quest.id);
				if (t != null) textures.Add((t, quest.id));
			}
			int count = textures.Count;
			if (count == 0) return;
			int maxH = System.Math.Max(1, panel.Height - (int)(titleSize.Y + PanelPadding * 3));
			int y = (int)(titlePos.Y + titleSize.Y + PanelPadding);
			// Compute scaled widths and total width including spacing
			var scaledSizes = textures.Select(t => new Point((int)System.Math.Round(t.tex.Width * EnemyScale), (int)System.Math.Round(t.tex.Height * EnemyScale))).ToList();
			int totalW = scaledSizes.Sum(s => s.X) + (count - 1) * EnemySpacing;
			int startX = panel.X + panel.Width / 2 - totalW / 2;
			for (int i = 0; i < count; i++)
			{
				var t = textures[i].tex;
				var size = scaledSizes[i];
				int drawX = startX;
				for (int j = 0; j < i; j++) drawX += scaledSizes[j].X + EnemySpacing;
				int drawY = y + (maxH - size.Y) / 2;
				_sb.Draw(t, new Rectangle(drawX, drawY, size.X, size.Y), Color.White);
			}

			// Arrows
			DrawArrowGlyphs(panel, qs);

			// Back button draw
			DrawBackButton();
		}

		private Rectangle GetPanelRect()
		{
			int w = _graphics.Viewport.Width;
			int h = _graphics.Viewport.Height;
			int x = w / 2 - PanelWidth / 2;
			int y = h / 2 - PanelHeight / 2 + PanelYOffset;
			return new Rectangle(x, y, PanelWidth, PanelHeight);
		}

		private void DrawPanel(Rectangle rect)
		{
			var tex = RoundedRectTextureFactory.CreateRoundedRect(_graphics, rect.Width, rect.Height, CornerRadius);
			_sb.Draw(tex, rect, new Color(90, 0, 0));
		}

		private void EnsureArrowEntity<TMarker>(string name, Rectangle bounds, bool visible) where TMarker : class, IComponent, new()
		{
			var ent = EntityManager.GetEntitiesWithComponent<TMarker>().FirstOrDefault();
			if (ent == null)
			{
				ent = EntityManager.CreateEntity(name);
				EntityManager.AddComponent(ent, new TMarker());
				EntityManager.AddComponent(ent, new UIElement { Bounds = bounds, IsInteractable = visible });
			}
			else
			{
				var ui = ent.GetComponent<UIElement>();
				if (ui == null) EntityManager.AddComponent(ent, new UIElement { Bounds = bounds, IsInteractable = visible });
				else { ui.Bounds = bounds; ui.IsInteractable = visible; }
			}
		}

		private void HandleArrowClicks(QuestSelectState qs)
		{
			var left = EntityManager.GetEntitiesWithComponent<QuestArrowLeft>().FirstOrDefault();
			var right = EntityManager.GetEntitiesWithComponent<QuestArrowRight>().FirstOrDefault();
			var leftUI = left?.GetComponent<UIElement>();
			var rightUI = right?.GetComponent<UIElement>();
			var all = LocationDefinitionCache.GetAll();
			if (!all.TryGetValue(qs.LocationId, out var loc) || loc?.quests == null || loc.quests.Count == 0) return;
			int maxIdx = loc.quests.Count - 1;
			if (leftUI != null && leftUI.IsClicked && qs.SelectedQuestIndex > 0) qs.SelectedQuestIndex--;
			if (rightUI != null && rightUI.IsClicked && qs.SelectedQuestIndex < maxIdx) qs.SelectedQuestIndex++;
		}

		private bool HasMoreRight(QuestSelectState qs)
		{
			var all = LocationDefinitionCache.GetAll();
			if (!all.TryGetValue(qs.LocationId, out var loc) || loc?.quests == null) return false;
			int unlockedMax = System.Math.Max(0, Crusaders30XX.ECS.Data.Save.SaveCache.GetValueOrDefault(qs.LocationId, 0));
			int maxIndex = System.Math.Min(unlockedMax, loc.quests.Count - 1);
			return qs.SelectedQuestIndex < maxIndex;
		}

		private Texture2D TryLoadEnemyTexture(string id)
		{
			if (string.IsNullOrEmpty(id)) return null;
			// Try TitleCase asset name then raw id
			string title = char.ToUpper(id[0]) + (id.Length > 1 ? id.Substring(1) : string.Empty);
			try { return _content.Load<Texture2D>(title); } catch { }
			try { return _content.Load<Texture2D>(id); } catch { }
			return null;
		}

		private void DrawArrowGlyphs(Rectangle panel, QuestSelectState qs)
		{
			var left = EntityManager.GetEntitiesWithComponent<QuestArrowLeft>().FirstOrDefault()?.GetComponent<UIElement>();
			var right = EntityManager.GetEntitiesWithComponent<QuestArrowRight>().FirstOrDefault()?.GetComponent<UIElement>();
			if (left?.IsInteractable == true)
			{
				var r = left.Bounds;
				var tex = GetArrowTexture(false, System.Math.Min(r.Width, r.Height));
				_sb.Draw(tex, new Rectangle(r.X, r.Y, r.Width, r.Height), Color.White);
			}
			if (right?.IsInteractable == true)
			{
				var r = right.Bounds;
				var tex = GetArrowTexture(true, System.Math.Min(r.Width, r.Height));
				_sb.Draw(tex, new Rectangle(r.X, r.Y, r.Width, r.Height), Color.White);
			}
		}

		private void EnsureBackButton(Rectangle rect)
		{
			var ent = EntityManager.GetEntitiesWithComponent<QuestBackButton>().FirstOrDefault();
			if (ent == null)
			{
				ent = EntityManager.CreateEntity("QuestBackButton");
				EntityManager.AddComponent(ent, new QuestBackButton());
				EntityManager.AddComponent(ent, new UIElement { Bounds = rect, IsInteractable = true });
			}
			else
			{
				var ui = ent.GetComponent<UIElement>();
				if (ui == null) EntityManager.AddComponent(ent, new UIElement { Bounds = rect, IsInteractable = true });
				else { ui.Bounds = rect; ui.IsInteractable = true; }
			}
		}

		private void HandleBackButtonClick(QuestSelectState qs)
		{
			var ent = EntityManager.GetEntitiesWithComponent<QuestBackButton>().FirstOrDefault();
			var ui = ent?.GetComponent<UIElement>();
			if (ui != null && ui.IsClicked)
			{
				qs.IsOpen = false;
			}
		}

		private void DrawBackButton()
		{
			var ent = EntityManager.GetEntitiesWithComponent<QuestBackButton>().FirstOrDefault();
			var ui = ent?.GetComponent<UIElement>();
			if (ui == null) return;
			var r = ui.Bounds;
			var tex = _roundedCache;
			if (tex == null || tex.Width != r.Width || tex.Height != r.Height)
			{
				tex = RoundedRectTextureFactory.CreateRoundedRect(_graphics, r.Width, r.Height, BackButtonCornerRadius);
				_roundedCache = tex;
			}
			_sb.Draw(tex, r, new Color(0, 0, 0));
			string text = "Back";
			var size = _font.MeasureString(text);
			float scale = BackButtonLabelScale;
			var pos = new Vector2(r.X + r.Width / 2f - (size.X * scale) / 2f, r.Y + r.Height / 2f - (size.Y * scale) / 2f);
			_sb.DrawString(_font, text, pos, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
		}

		private Texture2D GetArrowTexture(bool right, int size)
		{
			if (size < 4) size = 4;
			var key = (right, size);
			if (_arrowCache.TryGetValue(key, out var cached) && cached != null) return cached;
			var tex = new Texture2D(_graphics, size, size);
			var data = new Color[size * size];
			// Define triangle vertices
			Point A, B, C;
			if (right)
			{
				A = new Point(0, 0);
				B = new Point(0, size - 1);
				C = new Point(size - 1, size / 2);
			}
			else
			{
				A = new Point(size - 1, 0);
				B = new Point(size - 1, size - 1);
				C = new Point(0, size / 2);
			}
			// Barycentric fill
			Vector2 v0 = new Vector2(B.X - A.X, B.Y - A.Y);
			Vector2 v1 = new Vector2(C.X - A.X, C.Y - A.Y);
			float d00 = Vector2.Dot(v0, v0);
			float d01 = Vector2.Dot(v0, v1);
			float d11 = Vector2.Dot(v1, v1);
			float denom = d00 * d11 - d01 * d01;
			if (denom == 0f) denom = 1f;
			for (int y = 0; y < size; y++)
			{
				for (int x = 0; x < size; x++)
				{
					Vector2 v2 = new Vector2(x - A.X, y - A.Y);
					float d20 = Vector2.Dot(v2, v0);
					float d21 = Vector2.Dot(v2, v1);
					float v = (d11 * d20 - d01 * d21) / denom;
					float w = (d00 * d21 - d01 * d20) / denom;
					float u = 1.0f - v - w;
					bool inside = (u >= 0f && v >= 0f && w >= 0f);
					data[y * size + x] = inside ? Color.White : Color.Transparent;
				}
			}
			tex.SetData(data);
			_arrowCache[key] = tex;
			return tex;
		}
	}
}


