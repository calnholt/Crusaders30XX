using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Equipment;
using Crusaders30XX.ECS.Data.Locations;
using Crusaders30XX.ECS.Data.Medals;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Factories;
using Crusaders30XX.ECS.Rendering;
using Crusaders30XX.ECS.Singletons;
using Crusaders30XX.ECS.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("Shop For-Sale Grid")]
	public class ForSaleDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly ContentManager _content;
		private readonly SpriteFont _font = FontSingleton.ContentFont;

		private string _currentShopTitle = "Shop";
		private bool _needsRebuild = true;

		private Texture2D _goldIcon;
		private readonly Dictionary<string, Texture2D> _textureCache = new();
		private readonly Dictionary<string, Entity> _cardPreviewCache = new();

		// Layout
		[DebugEditable(DisplayName = "Max Columns", Step = 1, Min = 1, Max = 8)]
		public int MaxColumns { get; set; } = 4;
		[DebugEditable(DisplayName = "Tile Width", Step = 5, Min = 80, Max = 800)]
		public int TileWidth { get; set; } = 425;
		[DebugEditable(DisplayName = "Tile Height", Step = 5, Min = 80, Max = 800)]
		public int TileHeight { get; set; } = 275;
		[DebugEditable(DisplayName = "Horizontal Gap", Step = 2, Min = 0, Max = 200)]
		public int HorizontalGap { get; set; } = 32;
		[DebugEditable(DisplayName = "Vertical Gap", Step = 2, Min = 0, Max = 200)]
		public int VerticalGap { get; set; } = 24;
		[DebugEditable(DisplayName = "Panel Margin X", Step = 2, Min = 0, Max = 400)]
		public int PanelMarginX { get; set; } = 60;
		[DebugEditable(DisplayName = "Panel Margin Top", Step = 2, Min = 0, Max = 800)]
		public int PanelMarginTop { get; set; } = 160;
		[DebugEditable(DisplayName = "Padding X", Step = 1, Min = 0, Max = 100)]
		public int PaddingX { get; set; } = 12;
		[DebugEditable(DisplayName = "Padding Y", Step = 1, Min = 0, Max = 100)]
		public int PaddingY { get; set; } = 12;

		// Text and content
		[DebugEditable(DisplayName = "Name Text Scale", Step = 0.01f, Min = 0.05f, Max = 1.5f)]
		public float NameTextScale { get; set; } = 0.15f;
		[DebugEditable(DisplayName = "Price Text Scale", Step = 0.01f, Min = 0.05f, Max = 1.5f)]
		public float PriceTextScale { get; set; } = 0.3f;
		[DebugEditable(DisplayName = "Content Scale", Step = 0.01f, Min = 0.05f, Max = 2f)]
		public float ContentScale { get; set; } = 0.45f;
		[DebugEditable(DisplayName = "Icon Size", Step = 2, Min = 16, Max = 512)]
		public int IconSize { get; set; } = 148;
		[DebugEditable(DisplayName = "Price Icon Scale", Step = 0.05f, Min = 0.1f, Max = 3f)]
		public float PriceIconScale { get; set; } = 0.3f;
		[DebugEditable(DisplayName = "Name Offset Y", Step = 2, Min = -100, Max = 200)]
		public int NameOffsetY { get; set; } = 6;
		[DebugEditable(DisplayName = "Content Offset Y", Step = 2, Min = -200, Max = 300)]
		public int ContentOffsetY { get; set; } = 134;
		[DebugEditable(DisplayName = "Price Offset Y", Step = 2, Min = -200, Max = 300)]
		public int PriceOffsetY { get; set; } = 140;

		// Color fan for card previews
		[DebugEditable(DisplayName = "Color Fan Enabled", Step = 1)]
		public bool ColorFanEnabled { get; set; } = true;
		[DebugEditable(DisplayName = "Color Fan Angle (deg)", Step = 1f, Min = 0f, Max = 30f)]
		public float ColorFanAngleStepDeg { get; set; } = 8f;
		[DebugEditable(DisplayName = "Color Fan Spacing X", Step = 2, Min = 0, Max = 200)]
		public int ColorFanSpacingX { get; set; } = 28;
		[DebugEditable(DisplayName = "Color Fan Offset Y", Step = 2, Min = -100, Max = 100)]
		public int ColorFanSpacingY { get; set; } = 0;

		// Trapezoid background (single shape with per-edge A/B and mix)
		[DebugEditable(DisplayName = "Left Side Offset", Step = 1f, Min = 0f, Max = 100f)]
		public float LeftSideOffset { get; set; } = 14f;
		[DebugEditable(DisplayName = "Top Angle A", Step = 1f, Min = -45f, Max = 45f)]
		public float TopAngleA { get; set; } = -6f;
		[DebugEditable(DisplayName = "Top Angle B", Step = 1f, Min = -45f, Max = 45f)]
		public float TopAngleB { get; set; } = 36f;
		[DebugEditable(DisplayName = "Right Angle A", Step = 1f, Min = -45f, Max = 45f)]
		public float RightAngleA { get; set; } = -16f;
		[DebugEditable(DisplayName = "Right Angle B", Step = 1f, Min = -45f, Max = 45f)]
		public float RightAngleB { get; set; } = -16f;
		[DebugEditable(DisplayName = "Bottom Angle A", Step = 1f, Min = -45f, Max = 45f)]
		public float BottomAngleA { get; set; } = -8f;
		[DebugEditable(DisplayName = "Bottom Angle B", Step = 1f, Min = -45f, Max = 45f)]
		public float BottomAngleB { get; set; } = -2f;
		[DebugEditable(DisplayName = "Left Angle A", Step = 1f, Min = -45f, Max = 45f)]
		public float LeftAngleA { get; set; } = -45f;
		[DebugEditable(DisplayName = "Left Angle B", Step = 1f, Min = -45f, Max = 45f)]
		public float LeftAngleB { get; set; } = 9f;
		[DebugEditable(DisplayName = "Angle Mix (0..1)", Step = 0.05f, Min = 0f, Max = 1f)]
		public float AngleMix { get; set; } = 0f;
		[DebugEditable(DisplayName = "Alternate Angles A/B", Step = 1)]
		public bool AlternateAngles { get; set; } = false;

		public ForSaleDisplaySystem(EntityManager em, GraphicsDevice gd, SpriteBatch sb, ContentManager content)
			: base(em)
		{
			_graphicsDevice = gd;
			_spriteBatch = sb;
			_content = content;

			EventManager.Subscribe<LoadSceneEvent>(_ =>
			{
				if (_.Scene != SceneId.Shop) return;
				_needsRebuild = true;
			});
			EventManager.Subscribe<SetShopTitle>(_ =>
			{
				_currentShopTitle = string.IsNullOrWhiteSpace(_.Title) ? "Shop" : _.Title;
				_needsRebuild = true;
			});
			EventManager.Subscribe<HotKeyHoldCompletedEvent>(OnHotKeyHoldCompleted);
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<ForSaleItem>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			// draw-only system; we rebuild lazily in Draw
		}

		private void OnHotKeyHoldCompleted(HotKeyHoldCompletedEvent evt)
		{
			try
			{
				var e = evt?.Entity;
				if (e == null) return;
				var fs = e.GetComponent<ForSaleItem>();
				var ui = e.GetComponent<UIElement>();
				if (fs == null || ui == null) return;
				if (fs.IsPurchased) return;
				if (!ui.IsInteractable) return;
				if (!PurchaseItemService.CanAfford(fs.Price)) return;

				var result = PurchaseItemService.TryPurchase(EntityManager, e);
				if (result.Success)
				{
					EventManager.Publish(new PlaySfxEvent { Track = SfxTrack.CoinBag, Volume = 0.5f });
					fs.IsPurchased = true;
					ui.IsInteractable = false;
					// Remove hotkey if present
					var hk = e.GetComponent<HotKey>();
					if (hk != null) EntityManager.RemoveComponent<HotKey>(e);
					_needsRebuild = true;
				}
			}
			catch { }
		}

		private void EnsureInventoryEntities()
		{
			if (!_needsRebuild) return;
			_needsRebuild = false;

			// Clear previous
			var existing = EntityManager.GetEntitiesWithComponent<ForSaleItem>().ToList();
			foreach (var e in existing) EntityManager.DestroyEntity(e.Id);

			// Find matching shop by title; fallback to first shop with items
			var all = LocationDefinitionCache.GetAll();
			PointOfInterestDefinition chosen = null;
			PointOfInterestDefinition fallback = null;
			foreach (var kv in all)
			{
				var def = kv.Value;
				if (def?.pointsOfInterest == null) continue;
				foreach (var poi in def.pointsOfInterest)
				{
					if (poi?.type != PointOfInterestType.Shop) continue;
					if (poi?.forSale == null || poi.forSale.Count == 0) continue;
					if (fallback == null) fallback = poi;
					if (!string.IsNullOrWhiteSpace(_currentShopTitle) && string.Equals(poi.name ?? string.Empty, _currentShopTitle, StringComparison.OrdinalIgnoreCase))
					{
						chosen = poi;
						break;
					}
				}
				if (chosen != null) break;
			}
			var target = chosen ?? fallback;
			if (target == null || target.forSale == null || target.forSale.Count == 0) return;

			EntityFactory.CreateForSale(EntityManager, target.forSale, target.name ?? "Shop");

			// ensure gold icon
			_goldIcon ??= SafeLoadTexture("gold");
		}

		public void Draw()
		{
			var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
			if (scene == null || scene.Current != SceneId.Shop) return;

			EnsureInventoryEntities();

			var items = GetRelevantEntities()
				.Select(e => new { E = e, T = e.GetComponent<Transform>(), UI = e.GetComponent<UIElement>(), FS = e.GetComponent<ForSaleItem>() })
				.Where(x => x.FS != null)
				.ToList();

			if (items.Count == 0) return;

			int cols = Math.Max(1, MaxColumns);
			int w = Game1.VirtualWidth;

			for (int i = 0; i < items.Count; i++)
			{
				var x = items[i];
				int row = i / cols;
				int col = i % cols;
				int px = PanelMarginX + col * (TileWidth + HorizontalGap);
				int py = PanelMarginTop + row * (TileHeight + VerticalGap);
				var tileRect = new Rectangle(px, py, TileWidth, TileHeight);

				// Compute base center and apply parallax offset (from Transform.Position - BasePosition)
				var baseCenter = new Vector2(tileRect.X + tileRect.Width / 2f, tileRect.Y + tileRect.Height / 2f);
				Vector2 offset = Vector2.Zero;
				if (x.T != null)
				{
					// Initialize transform to its layout center so it doesn't slide in from (0,0)
					if (x.T.BasePosition == Vector2.Zero)
					{
						x.T.BasePosition = baseCenter;
						x.T.Position = baseCenter;
					}
					// Update BasePosition for ParallaxLayerSystem
					x.T.BasePosition = baseCenter;
					// Only apply offset when Transform.Position looks initialized (avoid first-frame jump)
					var pos = x.T.Position;
					if (pos != Vector2.Zero)
					{
						var cand = pos - x.T.BasePosition;
						// Guard against absurd offsets from uninitialized transforms
						if (System.Math.Abs(cand.X) <= 512 && System.Math.Abs(cand.Y) <= 512)
						{
							offset = cand;
						}
					}
					// Maintain draw ordering above background
					x.T.ZOrder = 10002;
				}

				// Apply parallax offset to the tile for visuals and UI bounds
				var drawRect = new Rectangle(
					(int)System.Math.Round(tileRect.X + offset.X),
					(int)System.Math.Round(tileRect.Y + offset.Y),
					tileRect.Width,
					tileRect.Height);

				// background trapezoid
				var innerRect = new Rectangle(
					drawRect.X + PaddingX,
					drawRect.Y + PaddingY,
					System.Math.Max(1, drawRect.Width - 2 * PaddingX),
					System.Math.Max(1, drawRect.Height - 2 * PaddingY));

				var angles = GetAnglesForIndex(i);
				var trap = PrimitiveTextureFactory.GetAntialiasedTrapezoid(
					_graphicsDevice,
					innerRect.Width,
					innerRect.Height,
					LeftSideOffset,
					angles.top,
					angles.right,
					angles.bottom,
					angles.left);
				_spriteBatch.Draw(trap, innerRect, Color.White);

				// update UI bounds and transform center
				if (x.UI != null)
				{
					// Bounds centered on the (possibly parallax-shifted) drawRect
					x.UI.Bounds = drawRect;
					x.UI.IsInteractable = !x.FS.IsPurchased;
				}

				// Name text (top-left inside padding)
				if (_font != null && !string.IsNullOrEmpty(x.FS.DisplayName))
				{
					Vector2 namePos = new Vector2(innerRect.X + PaddingX, innerRect.Y + NameOffsetY);
					_spriteBatch.DrawString(_font, x.FS.DisplayName, namePos, Color.White, 0f, Vector2.Zero, NameTextScale, SpriteEffects.None, 0f);
				}

				// Content
				Vector2 contentCenter = new Vector2(innerRect.X + innerRect.Width / 2f, innerRect.Y + ContentOffsetY);
				switch (x.FS.ItemType)
				{
					case ForSaleItemType.Card:
					{
						if (ColorFanEnabled)
						{
							float angle = MathHelper.ToRadians(ColorFanAngleStepDeg);
							var drawSpecs = new (CardData.CardColor color, int dx, float rot)[]
							{
								// Back to front: black, red, white (white on top)
								(CardData.CardColor.Red, -ColorFanSpacingX, -angle),
								(CardData.CardColor.Black, 0, 0f),
								(CardData.CardColor.White, ColorFanSpacingX, angle)
							};
							foreach (var spec in drawSpecs)
							{
								var card = EnsureCardPreview(x.FS.Id, spec.color);
								if (card == null) continue;
								var t = card.GetComponent<Transform>();
								if (t != null) t.Rotation = spec.rot;
								EventManager.Publish(new CardRenderScaledRotatedEvent
								{
									Card = card,
									Position = contentCenter + new Vector2(spec.dx, ColorFanSpacingY),
									Scale = ContentScale
								});
							}
						}
						else
						{
							var card = EnsureCardPreview(x.FS.Id, CardData.CardColor.White);
							if (card != null)
							{
								var t = card.GetComponent<Transform>();
								if (t != null) t.Rotation = 0f;
								EventManager.Publish(new CardRenderScaledRotatedEvent
								{
									Card = card,
									Position = contentCenter,
									Scale = ContentScale
								});
							}
						}
						break;
					}
					case ForSaleItemType.Medal:
					{
						DrawIconScaled(ResolveMedalTextureName(x.FS.Id), contentCenter, IconSize);
						break;
					}
					case ForSaleItemType.Equipment:
					{
						DrawIconScaled(ResolveEquipmentSlotIcon(x.FS.Id), contentCenter, IconSize);
						break;
					}
				}

				// Price line
				if (x.FS.IsPurchased)
				{
					int priceY = innerRect.Y + PriceOffsetY;
					int priceX = innerRect.X + PaddingX;
					_spriteBatch.DrawString(_font, "Sold!", new Vector2(priceX, priceY), Color.White, 0f, Vector2.Zero, PriceTextScale, SpriteEffects.None, 0f);
				}
				else
				{
					string priceText = $"{x.FS.Price}";
					Vector2 priceSize = _font?.MeasureString(priceText) * PriceTextScale ?? Vector2.Zero;
					int priceY = innerRect.Y + PriceOffsetY;
					int priceX = innerRect.X + PaddingX;
					_spriteBatch.DrawString(_font, priceText, new Vector2(priceX, priceY), Color.White, 0f, Vector2.Zero, PriceTextScale, SpriteEffects.None, 0f);
					if (_goldIcon != null)
					{
						int iconW = (int)Math.Round(_goldIcon.Width * PriceIconScale);
						int iconH = (int)Math.Round(_goldIcon.Height * PriceIconScale);
						var goldRect = new Rectangle((int)Math.Round(priceX + priceSize.X + 6), priceY, iconW, iconH);
						_spriteBatch.Draw(_goldIcon, goldRect, Color.White);
					}
				}

				// Hover HotKey (FaceButton.X) when affordable and not purchased
				if (x.UI != null && x.UI.IsHovered && !x.FS.IsPurchased && PurchaseItemService.CanAfford(x.FS.Price))
				{
					var hk = x.E.GetComponent<HotKey>();
					if (hk == null)
					{
						EntityManager.AddComponent(x.E, new HotKey { Button = FaceButton.X, RequiresHold = true, Position = HotKeyPosition.Below });
					}
					else
					{
						hk.Button = FaceButton.X;
						hk.RequiresHold = true;
						hk.Position = HotKeyPosition.Below;
					}
				}
				else
				{
					// Remove HotKey when not eligible (not hovered / not affordable / purchased)
					var hk = x.E.GetComponent<HotKey>();
					if (hk != null) EntityManager.RemoveComponent<HotKey>(x.E);
				}
			}
		}

		private (float top, float right, float bottom, float left) GetAnglesForIndex(int index)
		{
			if (AlternateAngles)
			{
				bool useB = (index % 2) == 1;
				return useB
					? (TopAngleB, RightAngleB, BottomAngleB, LeftAngleB)
					: (TopAngleA, RightAngleA, BottomAngleA, LeftAngleA);
			}
			float Lerp(float a, float b, float t) => a + (b - a) * MathHelper.Clamp(t, 0f, 1f);
			return (
				Lerp(TopAngleA, TopAngleB, AngleMix),
				Lerp(RightAngleA, RightAngleB, AngleMix),
				Lerp(BottomAngleA, BottomAngleB, AngleMix),
				Lerp(LeftAngleA, LeftAngleB, AngleMix)
			);
		}

		private Entity EnsureCardPreview(string cardId)
		{
			return EnsureCardPreview(cardId, CardData.CardColor.White);
		}

		private Entity EnsureCardPreview(string cardId, CardData.CardColor color)
		{
			if (string.IsNullOrWhiteSpace(cardId)) return null;
			string key = $"{cardId}|{color}";
			if (_cardPreviewCache.TryGetValue(key, out var existing) && existing != null)
			{
				// Ensure preview cards are not interactive in the shop grid
				var uiExisting = existing.GetComponent<UIElement>();
				if (uiExisting != null)
				{
					uiExisting.IsInteractable = false;
					uiExisting.TooltipType = TooltipType.None;
					uiExisting.Tooltip = string.Empty;
				}
				return existing;
			}
			var created = EntityFactory.CreateCardFromDefinition(EntityManager, cardId, color, allowWeapons: true);
			if (created != null)
			{
				_cardPreviewCache[key] = created;
				// Ensure preview cards are not interactive in the shop grid
				var ui = created.GetComponent<UIElement>();
				if (ui != null)
				{
					ui.IsInteractable = false;
					ui.TooltipType = TooltipType.None;
					ui.Tooltip = string.Empty;
				}
			}
			return created;
		}

		private void DrawIconScaled(string assetName, Vector2 center, int targetSize)
		{
			if (string.IsNullOrEmpty(assetName)) return;
			var tex = SafeLoadTexture(assetName);
			if (tex == null) return;
			float scale = Math.Min(targetSize / (float)tex.Width, targetSize / (float)tex.Height);
			int drawW = (int)Math.Round(tex.Width * scale);
			int drawH = (int)Math.Round(tex.Height * scale);
			var r = new Rectangle((int)Math.Round(center.X - drawW / 2f), (int)Math.Round(center.Y - drawH / 2f), drawW, drawH);
			_spriteBatch.Draw(tex, r, Color.White);
		}

		private string ResolveMedalTextureName(string medalId)
		{
			// Try id first, then fallback to generic "medal"
			if (MedalDefinitionCache.TryGet(medalId, out var _))
			{
				if (SafeLoadTexture(medalId) != null) return medalId;
			}
			return "medal";
		}

		private string ResolveEquipmentSlotIcon(string equipmentId)
		{
			if (!EquipmentDefinitionCache.TryGet(equipmentId, out var def) || def == null || string.IsNullOrWhiteSpace(def.slot))
			{
				return "arms";
			}
			string key = def.slot.Trim().ToLowerInvariant();
			switch (key)
			{
				case "head": return "head";
				case "chest": return "chest";
				case "arms": return "arms";
				case "legs": return "legs";
				default: return "arms";
			}
		}

		private Texture2D SafeLoadTexture(string assetName)
		{
			if (string.IsNullOrWhiteSpace(assetName)) return null;
			if (_textureCache.TryGetValue(assetName, out var tex) && tex != null) return tex;
			try
			{
				var loaded = _content.Load<Texture2D>(assetName);
				_textureCache[assetName] = loaded;
				return loaded;
			}
			catch { _textureCache[assetName] = null; return null; }
		}
	}
}




