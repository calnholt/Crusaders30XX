using System.Linq;
using System.Collections.Generic;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Locations;
using Crusaders30XX.ECS.Data.Save;
using Crusaders30XX.ECS.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("Quest Tooltip")]
	public class TooltipQuestDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly ContentManager _content;
		private readonly SpriteFont _font;
		private readonly Dictionary<int, FadeState> _fadeByEntityId = new();
		private readonly Dictionary<(int w, int h, int r), Texture2D> _roundedCache = new();
		private readonly Dictionary<string, int> _indexByLocationId = new();
		private GamePadState _prevGamePadState;
		private Texture2D _pixel;

		[DebugEditable(DisplayName = "Padding", Step = 1, Min = 0, Max = 40)]
		public int Padding { get; set; } = 10;

		[DebugEditable(DisplayName = "Corner Radius", Step = 1, Min = 0, Max = 64)]
		public int CornerRadius { get; set; } = 8;

		[DebugEditable(DisplayName = "Fade Seconds", Step = 0.05f, Min = 0.05f, Max = 1.5f)]
		public float FadeSeconds { get; set; } = 0.12f;

		[DebugEditable(DisplayName = "Max Alpha", Step = 5, Min = 0, Max = 255)]
		public int MaxAlpha { get; set; } = 230;

		[DebugEditable(DisplayName = "Text Scale", Step = 0.05f, Min = 0.5f, Max = 2.0f)]
		public float TextScale { get; set; } = 0.2f;

		[DebugEditable(DisplayName = "Header Height", Step = 2, Min = 12, Max = 200)]
		public int HeaderHeight { get; set; } = 36;

		[DebugEditable(DisplayName = "Header R", Step = 1, Min = 0, Max = 255)]
		public int HeaderR { get; set; } = 30;

		[DebugEditable(DisplayName = "Header G", Step = 1, Min = 0, Max = 255)]
		public int HeaderG { get; set; } = 0;

		[DebugEditable(DisplayName = "Header B", Step = 1, Min = 0, Max = 255)]
		public int HeaderB { get; set; } = 0;

		[DebugEditable(DisplayName = "Box Width", Step = 10, Min = 100, Max = 1920)]
		public int BoxWidth { get; set; } = 520;

		[DebugEditable(DisplayName = "Box Height", Step = 10, Min = 100, Max = 1080)]
		public int BoxHeight { get; set; } = 260;

		[DebugEditable(DisplayName = "Gap", Step = 1, Min = 0, Max = 120)]
		public int Gap { get; set; } = 10;

		[DebugEditable(DisplayName = "Enemy Scale", Step = 0.05f, Min = 0.1f, Max = 3f)]
		public float EnemyScale { get; set; } = 0.8f;

		[DebugEditable(DisplayName = "Enemy Spacing", Step = 2, Min = 0, Max = 200)]
		public int EnemySpacing { get; set; } = 12;

		private class FadeState { public float Alpha01; public bool TargetVisible; public Rectangle Rect; public string LocationId; }

		public TooltipQuestDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, ContentManager content, SpriteFont font)
			: base(entityManager)
		{
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
			_content = content;
			_font = font;
			_pixel = new Texture2D(graphicsDevice, 1, 1);
			_pixel.SetData(new[] { Color.White });
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<UIElement>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		public void Draw()
		{
			// Only on WorldMap scene
			var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
			if (scene == null || scene.Current != SceneId.WorldMap) return;
			if (_font == null) return;

			var hovered = GetRelevantEntities()
				.Select(e => new { E = e, UI = e.GetComponent<UIElement>(), T = e.GetComponent<Transform>() })
				.Where(x => x.UI != null && x.UI.TooltipType == TooltipType.Quests && x.UI.IsHovered)
				.OrderByDescending(x => x.T?.ZOrder ?? 0)
				.FirstOrDefault();

			// Default all fades to invisible
			foreach (var key in _fadeByEntityId.Keys.ToList())
			{
				_fadeByEntityId[key].TargetVisible = false;
			}

			string locationIdTop = null;
			Rectangle tooltipRect = Rectangle.Empty;
			if (hovered != null)
			{
				locationIdTop = ExtractLocationId(hovered.E?.Name);
				if (!string.IsNullOrEmpty(locationIdTop) && !locationIdTop.StartsWith("locked_"))
				{
					var rect = ComputeTooltipRect(hovered.UI.Bounds, hovered.T);
					tooltipRect = rect;
					var id = hovered.E.Id;
					if (!_fadeByEntityId.TryGetValue(id, out var fs))
					{
						fs = new FadeState { Alpha01 = 0f, TargetVisible = true, Rect = rect, LocationId = locationIdTop };
						_fadeByEntityId[id] = fs;
					}
					fs.TargetVisible = true;
					fs.Rect = rect;
					fs.LocationId = locationIdTop;
					_fadeByEntityId[id] = fs;

					// Initialize selection for this location if needed
					EnsureSelectionInitialized(locationIdTop);
					// Handle LB/RB edge to cycle
					HandleBumperCycle(locationIdTop);
					// Sync shared state (non-overlay usage)
					SyncQuestSelectState(locationIdTop);
				}
			}

			// Draw visible tooltips with fade
			foreach (var kv in _fadeByEntityId.ToList())
			{
				var id = kv.Key;
				var fs = kv.Value;
				float step = (FadeSeconds <= 0f) ? 1f : (1f / (FadeSeconds * 60f));
				fs.Alpha01 = MathHelper.Clamp(fs.Alpha01 + (fs.TargetVisible ? step : -step), 0f, 1f);
				_fadeByEntityId[id] = fs;
				if (fs.Alpha01 <= 0f && !fs.TargetVisible)
				{
					_fadeByEntityId.Remove(id);
					continue;
				}

				DrawTooltipBox(fs.Rect, fs.Alpha01);
				DrawHeader(fs.Rect, fs.Alpha01);
				DrawQuestContent(fs.LocationId, fs.Rect, fs.Alpha01);
			}
		}

		private void HandleBumperCycle(string locationId)
		{
			var gp = GamePad.GetState(PlayerIndex.One);
			bool left = gp.Buttons.LeftShoulder == ButtonState.Pressed;
			bool leftPrev = _prevGamePadState.Buttons.LeftShoulder == ButtonState.Pressed;
			bool right = gp.Buttons.RightShoulder == ButtonState.Pressed;
			bool rightPrev = _prevGamePadState.Buttons.RightShoulder == ButtonState.Pressed;
			bool leftEdge = left && !leftPrev;
			bool rightEdge = right && !rightPrev;
			if (!leftEdge && !rightEdge)
			{
				_prevGamePadState = gp;
				return;
			}

			var all = LocationDefinitionCache.GetAll();
			if (!all.TryGetValue(locationId, out var loc) || loc?.quests == null || loc.quests.Count == 0)
			{
				_prevGamePadState = gp;
				return;
			}
			int unlockedMax = System.Math.Max(0, SaveCache.GetValueOrDefault(locationId, 0));
			int maxIndex = System.Math.Min(unlockedMax, loc.quests.Count - 1);
			if (!_indexByLocationId.TryGetValue(locationId, out var idx)) idx = 0;
			if (leftEdge) idx = System.Math.Max(0, idx - 1);
			if (rightEdge) idx = System.Math.Min(maxIndex, idx + 1);
			_indexByLocationId[locationId] = idx;
			_prevGamePadState = gp;
		}

		private void EnsureSelectionInitialized(string locationId)
		{
			if (_indexByLocationId.ContainsKey(locationId)) return;
			var all = LocationDefinitionCache.GetAll();
			if (!all.TryGetValue(locationId, out var loc) || loc?.quests == null || loc.quests.Count == 0)
			{
				_indexByLocationId[locationId] = 0;
				return;
			}
			int completed = SaveCache.GetValueOrDefault(locationId, 0);
			int maxIndex = System.Math.Max(0, (loc.quests?.Count ?? 1) - 1);
			int startIndex = System.Math.Max(0, System.Math.Min(completed, maxIndex));
			_indexByLocationId[locationId] = startIndex;
		}

		private void SyncQuestSelectState(string locationId)
		{
			var qsEntity = EntityManager.GetEntitiesWithComponent<QuestSelectState>().FirstOrDefault();
			if (qsEntity == null)
			{
				qsEntity = EntityManager.CreateEntity("QuestSelectState");
				EntityManager.AddComponent(qsEntity, new QuestSelectState());
			}
			var qs = qsEntity.GetComponent<QuestSelectState>();
			qs.IsOpen = false;
			qs.LocationId = locationId;
			qs.SelectedQuestIndex = _indexByLocationId.GetValueOrDefault(locationId, 0);
		}

		private Rectangle ComputeTooltipRect(Rectangle anchor, Transform t)
		{
			int w = System.Math.Max(100, BoxWidth);
			int h = System.Math.Max(60, BoxHeight);
			int gap = System.Math.Max(0, Gap);
			int viewportW = _graphicsDevice.Viewport.Width;
			int viewportH = _graphicsDevice.Viewport.Height;

			int centerX = (int)System.Math.Round(t?.Position.X ?? (anchor.X + anchor.Width / 2f));
			bool preferRight = centerX < viewportW / 2;
			int rx = preferRight ? (anchor.Right + gap) : (anchor.Left - gap - w);
			int ry = anchor.Top + (anchor.Height - h) / 2;
			var rect = new Rectangle(rx, ry, w, h);
			// clamp to screen
			rect.X = System.Math.Max(0, System.Math.Min(rect.X, viewportW - rect.Width));
			rect.Y = System.Math.Max(0, System.Math.Min(rect.Y, viewportH - rect.Height));
			return rect;
		}

		private void DrawTooltipBox(Rectangle rect, float alpha01)
		{
			int r = System.Math.Max(0, System.Math.Min(CornerRadius, System.Math.Min(rect.Width, rect.Height) / 2));
			if (!_roundedCache.TryGetValue((rect.Width, rect.Height, r), out var tex) || tex == null)
			{
				tex = RoundedRectTextureFactory.CreateRoundedRect(_graphicsDevice, rect.Width, rect.Height, r);
				_roundedCache[(rect.Width, rect.Height, r)] = tex;
			}
			int a = (int)System.Math.Round(System.Math.Max(0, System.Math.Min(255, MaxAlpha)) * alpha01);
			var back = new Color(0, 0, 0, System.Math.Clamp(a, 0, 255));
			_spriteBatch.Draw(tex, rect, back);
		}

		private void DrawHeader(Rectangle rect, float alpha01)
		{
			int hh = System.Math.Max(12, HeaderHeight);
			var headerRect = new Rectangle(rect.X, rect.Y, rect.Width, System.Math.Min(rect.Height, hh));
			int a = (int)System.Math.Round(System.Math.Max(0, System.Math.Min(255, MaxAlpha)) * alpha01);
			var color = new Color(HeaderR, HeaderG, HeaderB, System.Math.Clamp(a, 0, 255));
			_spriteBatch.Draw(_pixel, headerRect, color);
			string title = "Quests";
			var size = _font.MeasureString(title) * TextScale;
			var pos = new Vector2(headerRect.X + Padding, headerRect.Y + System.Math.Max(0, (headerRect.Height - (int)System.Math.Ceiling(size.Y)) / 2));
			var textColor = Color.White * alpha01;
			_spriteBatch.DrawString(_font, title, pos, textColor, 0f, Vector2.Zero, TextScale, SpriteEffects.None, 0f);
		}

		private void DrawQuestContent(string locationId, Rectangle rect, float alpha01)
		{
			if (string.IsNullOrEmpty(locationId)) return;
			var all = LocationDefinitionCache.GetAll();
			if (!all.TryGetValue(locationId, out var loc) || loc?.quests == null || loc.quests.Count == 0) return;
			int completed = SaveCache.GetValueOrDefault(locationId, 0);
			int unlockedMax = System.Math.Max(0, System.Math.Min(completed, loc.quests.Count - 1));
			int idx = _indexByLocationId.GetValueOrDefault(locationId, unlockedMax);
			idx = System.Math.Max(0, System.Math.Min(idx, unlockedMax));
			var questDefs = loc.quests[idx];

			// inner area below header
			int pad = System.Math.Max(0, Padding);
			int topY = rect.Y + System.Math.Min(rect.Height, System.Math.Max(12, HeaderHeight)) + pad;
			int innerH = System.Math.Max(1, rect.Bottom - topY - pad);
			var inner = new Rectangle(rect.X + pad, topY, System.Math.Max(1, rect.Width - 2 * pad), innerH);

			// load textures
			var textures = new List<Texture2D>();
			foreach (var q in questDefs)
			{
				var tex = TryLoadEnemyTexture(q.id);
				if (tex != null) textures.Add(tex);
			}
			if (textures.Count == 0) return;

			int maxH = inner.Height;
			int targetH = System.Math.Max(1, (int)System.Math.Round(maxH * MathHelper.Clamp(EnemyScale, 0.05f, 1f)));
			var sizes = textures.Select(t => new Point(
				(int)System.Math.Round(t.Width * (targetH / (float)System.Math.Max(1, t.Height))),
				targetH
			)).ToList();
			int totalW = sizes.Sum(s => s.X) + (textures.Count - 1) * System.Math.Max(0, EnemySpacing);
			int startX = inner.X + (inner.Width - totalW) / 2;
			for (int i = 0; i < textures.Count; i++)
			{
				int drawX = startX;
				for (int j = 0; j < i; j++) drawX += sizes[j].X + System.Math.Max(0, EnemySpacing);
				int drawY = inner.Y + (inner.Height - sizes[i].Y) / 2;
				_spriteBatch.Draw(textures[i], new Rectangle(drawX, drawY, sizes[i].X, sizes[i].Y), Color.White * alpha01);
			}
		}

		private Texture2D TryLoadEnemyTexture(string id)
		{
			if (string.IsNullOrEmpty(id)) return null;
			string title = char.ToUpper(id[0]) + (id.Length > 1 ? id.Substring(1) : string.Empty);
			try { return _content.Load<Texture2D>(title); } catch { }
			try { return _content.Load<Texture2D>(id); } catch { }
			return null;
		}

		private static string ExtractLocationId(string entityName)
		{
			if (string.IsNullOrEmpty(entityName)) return null;
			const string prefix = "Location_";
			if (!entityName.StartsWith(prefix)) return null;
			return entityName.Substring(prefix.Length);
		}
	}
}


