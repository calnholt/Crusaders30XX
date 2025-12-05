using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Data.Locations;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Crusaders30XX.Diagnostics;
using System;
using System.Collections.Generic;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Renders the shop background image with cover scaling, anchored to bottom, centered horizontally.
	/// </summary>
	[DebugTab("Shop Background")]
	public class ShopBackgroundDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly ContentManager _content;
		private Texture2D _background;

		private string _currentShopTitle = "Shop";

		[DebugEditable(DisplayName = "Offset Y", Step = 2, Min = -2000, Max = 2000)]
		public int OffsetY { get; set; } = 0;

		public ShopBackgroundDisplaySystem(EntityManager entityManager, GraphicsDevice gd, SpriteBatch sb, ContentManager content)
			: base(entityManager)
		{
			_graphicsDevice = gd;
			_spriteBatch = sb;
			_content = content;

			EventManager.Subscribe<SetShopTitle>(OnSetShopTitle);
			EventManager.Subscribe<LoadSceneEvent>(OnLoadScene);

			TryLoad();
		}

		private void OnSetShopTitle(SetShopTitle evt)
		{
			_currentShopTitle = string.IsNullOrWhiteSpace(evt?.Title) ? "Shop" : evt.Title;
			TryLoad();
		}

		private void OnLoadScene(LoadSceneEvent evt)
		{
			if (evt == null || evt.Scene != SceneId.Shop) return;
			TryLoad();
		}

		private void TryLoad()
		{
			try
			{
				// Resolve background from location POI definitions, falling back to generic shop background.
				string path = ResolveBackgroundPath();

				// Try POI-specific background first
				_background = SafeLoadTexture(path);

				// Fallback to generic shop background if needed
				if (_background == null)
				{
					_background = SafeLoadTexture("shop_background");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[ShopBackgroundDisplaySystem] Failed to load background: {ex.Message}");
				_background = null;
			}
		}

		private Texture2D SafeLoadTexture(string assetName)
		{
			if (string.IsNullOrWhiteSpace(assetName)) return null;
			try
			{
				return _content.Load<Texture2D>(assetName);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[ShopBackgroundDisplaySystem] Failed to load texture '{assetName}': {ex.Message}");
				return null;
			}
		}

		private static string RemoveExtension(string p)
		{
			if (string.IsNullOrWhiteSpace(p)) return p;
			try { return System.IO.Path.GetFileNameWithoutExtension(p); }
			catch { return p; }
		}

		private string ResolveBackgroundPath()
		{
			PointOfInterestDefinition chosen = null;
			PointOfInterestDefinition fallback = null;

			try
			{
				Dictionary<string, LocationDefinition> all = LocationDefinitionCache.GetAll();
				if (all == null) return null;

				foreach (var kv in all)
				{
					var def = kv.Value;
					if (def?.pointsOfInterest == null) continue;

					foreach (var poi in def.pointsOfInterest)
					{
						if (poi == null) continue;
						if (poi.type != PointOfInterestType.Shop) continue;
						if (string.IsNullOrWhiteSpace(poi.background)) continue;

						if (fallback == null)
						{
							fallback = poi;
						}

						if (!string.IsNullOrWhiteSpace(_currentShopTitle) &&
							string.Equals(poi.name ?? string.Empty, _currentShopTitle, StringComparison.OrdinalIgnoreCase))
						{
							chosen = poi;
							break;
						}
					}

					if (chosen != null) break;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[ShopBackgroundDisplaySystem] Failed to resolve background path: {ex.Message}");
				return null;
			}

			var target = chosen ?? fallback;
			if (target == null || string.IsNullOrWhiteSpace(target.background)) return null;

			return RemoveExtension(target.background);
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			yield break;
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		public void Draw()
		{
			if (_background == null) return;

			int viewportW = Game1.VirtualWidth;
			int viewportH = Game1.VirtualHeight;

			int texW = _background.Width;
			int texH = _background.Height;

			float scaleX = viewportW / (float)texW;
			float scaleY = viewportH / (float)texH;
			float scale = Math.Max(scaleX, scaleY);

			int drawW = (int)Math.Round(texW * scale);
			int drawH = (int)Math.Round(texH * scale);

			int x = (viewportW - drawW) / 2;
			int y = viewportH - drawH + OffsetY;

			var dest = new Rectangle(x, y, drawW, drawH);
			_spriteBatch.Draw(_background, dest, Color.White);
		}
	}
}


	