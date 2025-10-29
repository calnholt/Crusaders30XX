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
		private readonly Dictionary<(int w, int h, bool right, int border), Texture2D> _triangleCache = new();
		private Texture2D _pixel;

		[DebugEditable(DisplayName = "Padding", Step = 1, Min = 0, Max = 40)]
		public int Padding { get; set; } = 10;

		[DebugEditable(DisplayName = "Corner Radius", Step = 1, Min = 0, Max = 64)]
		public int CornerRadius { get; set; } = 8;

		[DebugEditable(DisplayName = "Fade Seconds", Step = 0.05f, Min = 0.05f, Max = 1.5f)]
		public float FadeSeconds { get; set; } = 0.12f;

		[DebugEditable(DisplayName = "Max Alpha", Step = 5, Min = 0, Max = 255)]
		public int MaxAlpha { get; set; } = 140;

		[DebugEditable(DisplayName = "Text Scale", Step = 0.05f, Min = 0.5f, Max = 2.0f)]
		public float TextScale { get; set; } = 0.2f;

		[DebugEditable(DisplayName = "Header Height", Step = 2, Min = 12, Max = 200)]
		public int HeaderHeight { get; set; } = 64;

		[DebugEditable(DisplayName = "Header Left R", Step = 1, Min = 0, Max = 255)]
		public int HeaderLeftR { get; set; } = 20;

		[DebugEditable(DisplayName = "Header Left G", Step = 1, Min = 0, Max = 255)]
		public int HeaderLeftG { get; set; } = 0;

		[DebugEditable(DisplayName = "Header Left B", Step = 1, Min = 0, Max = 255)]
		public int HeaderLeftB { get; set; } = 0;

		[DebugEditable(DisplayName = "Header Right R", Step = 1, Min = 0, Max = 255)]
		public int HeaderRightR { get; set; } = 60;

		[DebugEditable(DisplayName = "Header Right G", Step = 1, Min = 0, Max = 255)]
		public int HeaderRightG { get; set; } = 0;

		[DebugEditable(DisplayName = "Header Right B", Step = 1, Min = 0, Max = 255)]
		public int HeaderRightB { get; set; } = 0;

		[DebugEditable(DisplayName = "Header Stripe Height", Step = 1, Min = 0, Max = 16)]
		public int HeaderStripeHeight { get; set; } = 3;

		[DebugEditable(DisplayName = "Box Width", Step = 10, Min = 100, Max = 1920)]
		public int BoxWidth { get; set; } = 520;

		[DebugEditable(DisplayName = "Box Height", Step = 10, Min = 100, Max = 1080)]
		public int BoxHeight { get; set; } = 260;

		[DebugEditable(DisplayName = "Gap", Step = 1, Min = 0, Max = 120)]
		public int Gap { get; set; } = 30;

		[DebugEditable(DisplayName = "Enemy Scale", Step = 0.05f, Min = 0.1f, Max = 3f)]
		public float EnemyScale { get; set; } = 0.8f;

		[DebugEditable(DisplayName = "Enemy Spacing", Step = 2, Min = 0, Max = 200)]
		public int EnemySpacing { get; set; } = 12;

		[DebugEditable(DisplayName = "Quest Title Scale", Step = 0.05f, Min = 0.1f, Max = 2f)]
		public float QuestTitleScale { get; set; } = 0.22f;

		[DebugEditable(DisplayName = "Bottom Bar Height", Step = 2, Min = 16, Max = 200)]
		public int BottomBarHeight { get; set; } = 50;

		// Bottom bar button (LB/RB) controls
		[DebugEditable(DisplayName = "Pill Side Padding", Step = 1, Min = 0, Max = 120)]
		public int PillSidePadding { get; set; } = 5;

		[DebugEditable(DisplayName = "Pill Min Height", Step = 1, Min = 12, Max = 200)]
		public int PillMinHeight { get; set; } = 27;

		[DebugEditable(DisplayName = "Pill InnerPad Min", Step = 1, Min = 0, Max = 40)]
		public int PillInnerPadMin { get; set; } = 5;

		[DebugEditable(DisplayName = "Pill InnerPad Factor (of height)", Step = 0.01f, Min = 0f, Max = 1f)]
		public float PillInnerPadFactor { get; set; } = 0.307f; // ~ 1/6 of height

		[DebugEditable(DisplayName = "Pill Corner Radius Max", Step = 1, Min = 0, Max = 64)]
		public int PillCornerRadiusMax { get; set; } = 0;

		[DebugEditable(DisplayName = "Bottom Label Scale", Step = 0.05f, Min = 0.1f, Max = 2.0f)]
		public float BottomLabelScale { get; set; } = 0.2f;

		[DebugEditable(DisplayName = "Bottom Right Text Margin", Step = 1, Min = 0, Max = 120)]
		public int BottomRightMargin { get; set; } = 10;

		[DebugEditable(DisplayName = "Bottom Right Text Scale", Step = 0.05f, Min = 0.1f, Max = 2.0f)]
		public float BottomRightTextScale { get; set; } = 0.2f;

		[DebugEditable(DisplayName = "Header Image Padding", Step = 1, Min = 0, Max = 40)]
		public int HeaderImagePadding { get; set; } = 4;

		private class FadeState { public float Alpha01; public bool TargetVisible; public Rectangle Rect; public string LocationId; public string Title; public List<LocationEventDefinition> Events; }

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
			// Only on WorldMap or Location scenes
			var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
			if (scene == null || (scene.Current != SceneId.WorldMap && scene.Current != SceneId.Location)) return;
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

			if (hovered != null)
			{
				var id = hovered.E.Id;
				var rect = ComputeTooltipRect(hovered.UI.Bounds, hovered.T);

				string locationIdTop = null;
				string title = null;
				List<LocationEventDefinition> events = null;

				// Case 1: WorldMap location tile
				locationIdTop = ExtractLocationId(hovered.E?.Name);
				if (!string.IsNullOrEmpty(locationIdTop) && !locationIdTop.StartsWith("locked_"))
				{
					var all = LocationDefinitionCache.GetAll();
					if (all.TryGetValue(locationIdTop, out var loc) && loc?.pointsOfInterest != null && loc.pointsOfInterest.Count > 0)
					{
						int completed = SaveCache.GetValueOrDefault(locationIdTop, 0);
						int idx = System.Math.Max(0, System.Math.Min(completed, loc.pointsOfInterest.Count - 1));
						events = loc.pointsOfInterest[idx].events;
						title = "Quest " + (idx + 1);
					}
				}
				else
				{
					// Case 2: Location scene POI entity (show only if revealed/completed or revealed by proximity)
					var poi = hovered.E.GetComponent<PointOfInterest>();
					if (poi != null && IsPoiVisible(poi) && TryFindLocationByPoiId(poi.Id, out var locId, out var questIdx))
					{
						locationIdTop = locId;
						var all = LocationDefinitionCache.GetAll();
						if (all.TryGetValue(locId, out var loc) && questIdx >= 0 && questIdx < (loc.pointsOfInterest?.Count ?? 0))
						{
							events = loc.pointsOfInterest[questIdx].events;
							title = string.IsNullOrWhiteSpace(loc.pointsOfInterest[questIdx].name) ? ("Quest " + (questIdx + 1)) : loc.pointsOfInterest[questIdx].name;
						}
					}
				}

				if (!string.IsNullOrEmpty(locationIdTop) && events != null && events.Count > 0)
				{
					if (!_fadeByEntityId.TryGetValue(id, out var fs))
					{
						fs = new FadeState { Alpha01 = 0f, TargetVisible = true, Rect = rect, LocationId = locationIdTop, Title = title, Events = events };
						_fadeByEntityId[id] = fs;
					}
					fs.TargetVisible = true;
					fs.Rect = rect;
					fs.LocationId = locationIdTop;
					fs.Title = title;
					fs.Events = events;
					_fadeByEntityId[id] = fs;
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
				DrawHeader(fs.LocationId, fs.Rect, fs.Alpha01);
				DrawQuestContent(fs.Rect, fs.Alpha01, fs.Title, fs.Events);
			}
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

		private void DrawHeader(string locationId, Rectangle rect, float alpha01)
		{
			int hh = System.Math.Max(12, HeaderHeight);
			int stripe = System.Math.Max(0, System.Math.Min(HeaderStripeHeight, hh));
			var headerRect = new Rectangle(rect.X, rect.Y, rect.Width, System.Math.Min(rect.Height, hh));
			int a = (int)System.Math.Round(System.Math.Max(0, System.Math.Min(255, MaxAlpha)) * alpha01);
			// Darken the left background color a bit more for contrast
			var leftColor = new Color(System.Math.Max(0, HeaderLeftR - 10), System.Math.Max(0, HeaderLeftG), System.Math.Max(0, HeaderLeftB), System.Math.Clamp(a, 0, 255));
			var rightColor = new Color(HeaderRightR, HeaderRightG, HeaderRightB, System.Math.Clamp(a, 0, 255));

			// Top white stripe
			if (stripe > 0)
			{
				var stripeRect = new Rectangle(headerRect.X, headerRect.Y, headerRect.Width, stripe);
				_spriteBatch.Draw(_pixel, stripeRect, Color.White);
			}

			// Split header: left square (image), right area (location name)
			int pad = System.Math.Max(0, Padding);
			int leftBoxSize = headerRect.Height - stripe; // square inside header below stripe
			var leftRect = new Rectangle(headerRect.X, headerRect.Y + stripe, System.Math.Min(leftBoxSize, headerRect.Width / 2), leftBoxSize);
			var rightRect = new Rectangle(leftRect.Right, headerRect.Y + stripe, System.Math.Max(0, headerRect.Width - leftRect.Width), leftBoxSize);
			_spriteBatch.Draw(_pixel, leftRect, leftColor);
			_spriteBatch.Draw(_pixel, rightRect, rightColor);

			// Draw location image centered in left box
			var loc = GetLocationDefinition(locationId);
			if (loc != null)
			{
				var tex = TryLoadEnemyTexture(loc.id); // reuse loader; location textures share Content id
				if (tex != null && leftRect.Width > 0 && leftRect.Height > 0)
				{
					int imgPad = System.Math.Max(0, HeaderImagePadding);
					var imgRect = new Rectangle(leftRect.X + imgPad, leftRect.Y + imgPad, System.Math.Max(1, leftRect.Width - 2 * imgPad), System.Math.Max(1, leftRect.Height - 2 * imgPad));
					float scale = System.Math.Min(imgRect.Width / (float)tex.Width, imgRect.Height / (float)tex.Height);
					int drawW = System.Math.Max(1, (int)System.Math.Round(tex.Width * scale));
					int drawH = System.Math.Max(1, (int)System.Math.Round(tex.Height * scale));
					var dst = new Rectangle(imgRect.X + (imgRect.Width - drawW) / 2, imgRect.Y + (imgRect.Height - drawH) / 2, drawW, drawH);
					_spriteBatch.Draw(tex, dst, Color.White * alpha01);
				}
			}

			// Draw location name in right area
			if (loc != null)
			{
				string name = loc.name ?? loc.id ?? "";
				var size = _font.MeasureString(name) * TextScale;
				var pos = new Vector2(rightRect.X + pad, rightRect.Y + System.Math.Max(0, (rightRect.Height - (int)System.Math.Ceiling(size.Y)) / 2));
				_spriteBatch.DrawString(_font, name, pos, Color.White * alpha01, 0f, Vector2.Zero, TextScale, SpriteEffects.None, 0f);
			}
		}

		private void DrawQuestContent(Rectangle rect, float alpha01, string title, List<LocationEventDefinition> questDefs)
		{
			if (questDefs == null || questDefs.Count == 0) return;

			// inner area below header
			int pad = System.Math.Max(0, Padding);
			int topY = rect.Y + System.Math.Min(rect.Height, System.Math.Max(12, HeaderHeight)) + pad;
			int innerH = System.Math.Max(1, rect.Bottom - topY - pad);
			var inner = new Rectangle(rect.X + pad, topY, System.Math.Max(1, rect.Width - 2 * pad), innerH);

			// Title
			string questTitle = title ?? "Quest";
			var qSize = _font.MeasureString(questTitle) * QuestTitleScale;
			var qPos = new Vector2(inner.X + (inner.Width - qSize.X) / 2f, inner.Y);
			_spriteBatch.DrawString(_font, questTitle, qPos, Color.White * alpha01, 0f, Vector2.Zero, QuestTitleScale, SpriteEffects.None, 0f);
			int enemiesTop = (int)System.Math.Round(qPos.Y + qSize.Y + pad);
			int enemiesHeight = System.Math.Max(1, inner.Bottom - enemiesTop);
			var enemiesRect = new Rectangle(inner.X, enemiesTop, inner.Width, enemiesHeight);

			// load enemy textures
			var textures = new List<Texture2D>();
			foreach (var q in questDefs)
			{
				var tex = TryLoadEnemyTexture(q.id);
				if (tex != null) textures.Add(tex);
			}
			if (textures.Count == 0) return;

			int maxH = enemiesRect.Height;
			int targetH = System.Math.Max(1, (int)System.Math.Round(maxH * MathHelper.Clamp(EnemyScale, 0.05f, 1f)));
			var sizes = textures.Select(t => new Point(
				(int)System.Math.Round(t.Width * (targetH / (float)System.Math.Max(1, t.Height))),
				targetH
			)).ToList();
			int totalW = sizes.Sum(s => s.X) + (textures.Count - 1) * System.Math.Max(0, EnemySpacing);
			int startX = enemiesRect.X + (enemiesRect.Width - totalW) / 2;
			for (int i = 0; i < textures.Count; i++)
			{
				int drawX = startX;
				for (int j = 0; j < i; j++) drawX += sizes[j].X + System.Math.Max(0, EnemySpacing);
				int drawY = enemiesRect.Y + (enemiesRect.Height - sizes[i].Y) / 2;
				_spriteBatch.Draw(textures[i], new Rectangle(drawX, drawY, sizes[i].X, sizes[i].Y), Color.White * alpha01);
			}

			// Bottom-right hint: "A - Select"
			string leftText = "A";
			string rightText = " - Select";
			float scale = BottomRightTextScale;
			var leftSize = _font.MeasureString(leftText) * scale;
			var rightSize = _font.MeasureString(rightText) * scale;
			int textPad = System.Math.Max(0, BottomRightMargin);
			var rightEndX = inner.Right - textPad;
			var rightPos = new Vector2(rightEndX - rightSize.X, inner.Bottom - rightSize.Y - textPad);
			var leftPos = new Vector2((int)System.Math.Round(rightPos.X - leftSize.X), inner.Bottom - leftSize.Y - textPad);
			var green = new Color(0, 200, 0) * alpha01;
			_spriteBatch.DrawString(_font, leftText, leftPos, green, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
			_spriteBatch.DrawString(_font, rightText, rightPos, Color.White * alpha01, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
		}

		private Texture2D TryLoadEnemyTexture(string id)
		{
			if (string.IsNullOrEmpty(id)) return null;
			string title = char.ToUpper(id[0]) + (id.Length > 1 ? id.Substring(1) : string.Empty);
			try { return _content.Load<Texture2D>(title); } catch { }
			try { return _content.Load<Texture2D>(id); } catch { }
			return null;
		}

		private void DrawPill(Rectangle rect, Color color, int radius)
		{
			int r = System.Math.Max(2, System.Math.Min(radius, System.Math.Min(rect.Width, rect.Height) / 2));
			if (!_roundedCache.TryGetValue((rect.Width, rect.Height, r), out var tex) || tex == null)
			{
				tex = RoundedRectTextureFactory.CreateRoundedRect(_graphicsDevice, rect.Width, rect.Height, r);
				_roundedCache[(rect.Width, rect.Height, r)] = tex;
			}
			_spriteBatch.Draw(tex, rect, color);
		}

		private LocationDefinition GetLocationDefinition(string id)
		{
			if (string.IsNullOrEmpty(id)) return null;
			var all = LocationDefinitionCache.GetAll();
			all.TryGetValue(id, out var loc);
			return loc;
		}

		private static string ExtractLocationId(string entityName)
		{
			if (string.IsNullOrEmpty(entityName)) return null;
			const string prefix = "Location_";
			if (!entityName.StartsWith(prefix)) return null;
			return entityName.Substring(prefix.Length);
		}

		private bool IsPoiVisible(PointOfInterest poi)
		{
			if (poi == null) return false;
			// Visible if self revealed or completed
			if (poi.IsRevealed || poi.IsCompleted) return true;
			// Or within reveal/unrevealed radius of any unlocker (revealed or completed)
			var allPoi = EntityManager.GetEntitiesWithComponent<PointOfInterest>()
				.Select(e => e.GetComponent<PointOfInterest>())
				.Where(p => p != null && (p.IsRevealed || p.IsCompleted))
				.ToList();
			foreach (var u in allPoi)
			{
				float dx = poi.WorldPosition.X - u.WorldPosition.X;
				float dy = poi.WorldPosition.Y - u.WorldPosition.Y;
				int r = u.IsCompleted ? u.RevealRadius : u.UnrevealedRadius;
				if ((dx * dx) + (dy * dy) <= (r * r)) return true;
			}
			return false;
		}

		private bool TryFindLocationByPoiId(string poiId, out string locationId, out int questIndex)
		{
			locationId = null;
			questIndex = -1;
			if (string.IsNullOrEmpty(poiId)) return false;
			var all = LocationDefinitionCache.GetAll();
			foreach (var kv in all)
			{
				var loc = kv.Value;
				if (loc?.pointsOfInterest == null) continue;
				for (int i = 0; i < loc.pointsOfInterest.Count; i++)
				{
					if (string.Equals(loc.pointsOfInterest[i].id, poiId, System.StringComparison.OrdinalIgnoreCase))
					{
						locationId = kv.Key;
						questIndex = i;
						return true;
					}
				}
			}
			return false;
		}
	}
}


