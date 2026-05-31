using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Locations;
using Crusaders30XX.ECS.Data.Save;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Services;
using Crusaders30XX.ECS.Rendering;
using Crusaders30XX.ECS.Singletons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("Location POI Display")]
	public class PointOfInterestDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly Texture2D _pixel;
		private readonly Texture2D _questIconTexture;
		private readonly Texture2D _hellriftIconTexture;
		private readonly Texture2D _shopIconTexture;
		private readonly Texture2D _treasureIconTexture;
		private readonly Texture2D _skullTexture;
		private bool _spawned;
		private readonly System.Collections.Generic.List<Entity> _pois = new System.Collections.Generic.List<Entity>();
		private readonly System.Collections.Generic.Dictionary<int, Vector2> _worldByEntityId = new System.Collections.Generic.Dictionary<int, Vector2>();
		private readonly System.Collections.Generic.Dictionary<int, float> _hoverScales = new System.Collections.Generic.Dictionary<int, float>();

		[DebugEditable(DisplayName = "Icon Size", Step = 1f, Min = 10f, Max = 200f)]
		public float IconSize { get; set; } = 140f;

		[DebugEditable(DisplayName = "Circle Size", Step = 1f, Min = 4f, Max = 32f)]
		public float CircleSize { get; set; } = 16f;

		[DebugEditable(DisplayName = "Circle Offset X", Step = 0.05f, Min = -2f, Max = 2f)]
		public float CircleOffsetX { get; set; } = 1f;

		[DebugEditable(DisplayName = "Circle Offset Y", Step = 0.05f, Min = -2f, Max = 2f)]
		public float CircleOffsetY { get; set; } = -1f;

		[DebugEditable(DisplayName = "Hover Scale", Step = 0.05f, Min = 1f, Max = 2f)]
		public float HoverScale { get; set; } = 1.1f;

		[DebugEditable(DisplayName = "Animation Speed", Step = 1f, Min = 1f, Max = 30f)]
		public float AnimationSpeed { get; set; } = 20f;

		[DebugEditable(DisplayName = "Skull Size", Step = 1f, Min = 4f, Max = 128f)]
		public float SkullSize { get; set; } = 39f;

		[DebugEditable(DisplayName = "Skull Gap", Step = 1f, Min = 0f, Max = 64f)]
		public float SkullGap { get; set; } = 11f;

		// Vertical offset for skull row relative to the bottom of the POI icon (in icon heights)
		[DebugEditable(DisplayName = "Skull Offset Y", Step = 0.05f, Min = -2f, Max = 2f)]
		public float SkullOffsetY { get; set; } = 0.1f;

		public PointOfInterestDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, ContentManager content)
			: base(entityManager)
		{
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
			_pixel = new Texture2D(graphicsDevice, 1, 1);
			_pixel.SetData(new[] { Color.White });
			try
			{
				_questIconTexture = content.Load<Texture2D>("Quest_poi");
			}
			catch
			{
				_questIconTexture = null;
			}
			try
			{
				_hellriftIconTexture = content.Load<Texture2D>("Hellrift_poi");
			}
			catch
			{
				_hellriftIconTexture = null;
			}
			try
			{
				_shopIconTexture = content.Load<Texture2D>("Shop_poi");
			}
			catch
			{
				_shopIconTexture = null;
			}
			try
			{
				_treasureIconTexture = content.Load<Texture2D>("treasure_chest");
			}
			catch
			{
				_treasureIconTexture = null;
			}
			try
			{
				_skullTexture = content.Load<Texture2D>("skull");
			}
			catch
			{
				_skullTexture = null;
			}
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<SceneState>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			var scene = entity.GetComponent<SceneState>();
			if (scene == null || scene.Current != SceneId.Location) return;

			if (!_spawned)
			{
				SpawnPois();
				_spawned = true;
			}

			// Provide base positions (screen-space) for parallax to offset from
			var cam = EntityManager.GetEntity("LocationCamera")?.GetComponent<LocationCameraState>();
			if (cam == null) return;
			var origin = cam.Origin;
			float mapScale = cam.MapScale;
			float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
			var runNodes = SaveCache.GetRunMapNodes();
			foreach (var e in _pois)
			{
				var t = e.GetComponent<Transform>();
				if (t == null) continue;
				if (!_worldByEntityId.TryGetValue(e.Id, out var world)) continue;
				var poiComp = e.GetComponent<PointOfInterest>();
				if (poiComp != null && poiComp.Type == PointOfInterestType.Shop && !string.IsNullOrEmpty(poiComp.ShopId))
				{
					if (SaveCache.TryGetRunShop(poiComp.ShopId, out var shop, out _))
					{
						var uiShop = e.GetComponent<UIElement>();
						if (uiShop != null)
						{
							uiShop.IsInteractable = RunMapShopService.IsEnterable(shop, runNodes);
						}
					}
				}
				if (poiComp != null && poiComp.Type == PointOfInterestType.Treasure && !string.IsNullOrEmpty(poiComp.TreasureId))
				{
					if (SaveCache.TryGetRunTreasure(poiComp.TreasureId, out var treasure, out _))
					{
						poiComp.IsCompleted = treasure.isClaimed;
						var uiTreasure = e.GetComponent<UIElement>();
						if (uiTreasure != null)
						{
							uiTreasure.IsInteractable = RunMapTreasureService.IsEnterable(treasure, runNodes);
						}
					}
				}
				// Scale world position by map scale to match scaled world space
				var scaledWorld = world * mapScale;
				var screenPos = scaledWorld - origin;
				t.Position = screenPos;

				// Update hover scale animation
				var ui = e.GetComponent<UIElement>();
				if (ui != null)
				{
					float targetScale = ui.IsHovered ? HoverScale : 1f;
					if (!_hoverScales.TryGetValue(e.Id, out float currentScale))
					{
						currentScale = 1f;
						_hoverScales[e.Id] = currentScale;
					}
					float lerpSpeed = AnimationSpeed * dt;
					currentScale = MathHelper.Lerp(currentScale, targetScale, MathHelper.Clamp(lerpSpeed, 0f, 1f));
					_hoverScales[e.Id] = currentScale;
					
					// Update UI bounds size based on zoom and hover scale
					var poi = e.GetComponent<PointOfInterest>();
					if (poi != null)
					{
						Texture2D iconTexture = GetIconTexture(poi.Type);
						
						// Calculate bounds size scaled by map zoom and hover scale
						float boundsWidth = IconSize * mapScale * currentScale;
						float boundsHeight = boundsWidth;
						if (iconTexture != null && iconTexture.Width > 0 && iconTexture.Height > 0)
						{
							float aspectRatio = iconTexture.Height / (float)iconTexture.Width;
							boundsHeight = boundsWidth * aspectRatio;
						}
						
						// Update bounds size and center around transform position
						// (ParallaxLayerSystem may update position, but this ensures alignment)
						ui.Bounds = new Rectangle(
							(int)System.Math.Round(t.Position.X - boundsWidth / 2f),
							(int)System.Math.Round(t.Position.Y - boundsHeight / 2f),
							(int)System.Math.Round(boundsWidth),
							(int)System.Math.Round(boundsHeight));
					}
				}

			}
		}

		private void SpawnPois()
		{
			int i = 0;
			var runNodes = SaveCache.GetRunMapNodes();
			for (int nodeIndex = 0; nodeIndex < runNodes.Count; nodeIndex++)
			{
				var node = runNodes[nodeIndex];
				if (node == null) continue;
				var worldPos = new Vector2(node.worldX, node.worldY);
				var e = EntityManager.CreateEntity($"POI_{i++}");
				_worldByEntityId[e.Id] = worldPos;
				_pois.Add(e);
				EntityManager.AddComponent(e, new Transform { Position = worldPos, ZOrder = 10 });

				int boundsWidth = (int)IconSize;
				int boundsHeight = (int)IconSize;
				if (_questIconTexture != null && _questIconTexture.Width > 0 && _questIconTexture.Height > 0)
				{
					float aspectRatio = _questIconTexture.Height / (float)_questIconTexture.Width;
					boundsHeight = (int)(IconSize * aspectRatio);
				}

				bool canFight = node.isRevealed && !node.isCompleted;
				EntityManager.AddComponent(e, new UIElement
				{
					Bounds = new Rectangle(0, 0, boundsWidth, boundsHeight),
					IsInteractable = canFight,
					TooltipType = TooltipType.Quests,
					EventType = UIElementEventType.QuestSelect,
					IsPreventDefaultClick = true,
				});
				EntityManager.AddComponent(e, ParallaxLayer.GetLocationParallaxLayer());

				var childPoiIds = new List<string>();
				if (node.childIndices != null)
				{
					foreach (int childIndex in node.childIndices)
					{
						if (childIndex >= 0 && childIndex < runNodes.Count && runNodes[childIndex] != null)
						{
							childPoiIds.Add(runNodes[childIndex].id);
						}
					}
				}

				var poi = new PointOfInterest
				{
					Id = node.id,
					WorldPosition = worldPos,
					RevealRadius = LocationMapConstants.DefaultRevealRadius,
					UnrevealedRadius = LocationMapConstants.DefaultUnrevealedRadius,
					IsRevealed = node.isRevealed,
					IsCompleted = node.isCompleted,
					Type = PointOfInterestType.Quest,
					RunMapIndex = nodeIndex,
					ChildPoiIds = childPoiIds,
				};
				poi.DisplayRadius = poi.IsCompleted ? poi.RevealRadius : 0f;
				EntityManager.AddComponent(e, poi);
			}

			SpawnShopPois(ref i, runNodes);
			SpawnTreasurePois(ref i, runNodes);
		}

		private void SpawnShopPois(ref int entityIndex, IReadOnlyList<RunMapNode> runNodes)
		{
			foreach (var shop in SaveCache.GetRunMapShops())
			{
				if (shop == null || string.IsNullOrEmpty(shop.id)) continue;

				var worldPos = new Vector2(shop.worldX, shop.worldY);
				var e = EntityManager.CreateEntity($"POI_Shop_{entityIndex++}");
				_worldByEntityId[e.Id] = worldPos;
				_pois.Add(e);
				EntityManager.AddComponent(e, new Transform { Position = worldPos, ZOrder = 10 });

				int boundsWidth = (int)IconSize;
				int boundsHeight = (int)IconSize;
				if (_shopIconTexture != null && _shopIconTexture.Width > 0 && _shopIconTexture.Height > 0)
				{
					float aspectRatio = _shopIconTexture.Height / (float)_shopIconTexture.Width;
					boundsHeight = (int)(IconSize * aspectRatio);
				}

				bool canEnter = RunMapShopService.IsEnterable(shop, runNodes);
				EntityManager.AddComponent(e, new UIElement
				{
					Bounds = new Rectangle(0, 0, boundsWidth, boundsHeight),
					IsInteractable = canEnter,
					TooltipType = TooltipType.None,
					EventType = UIElementEventType.None,
					IsPreventDefaultClick = true,
				});
				EntityManager.AddComponent(e, ParallaxLayer.GetLocationParallaxLayer());
				EntityManager.AddComponent(e, new PointOfInterest
				{
					Id = shop.id,
					ShopId = shop.id,
					WorldPosition = worldPos,
					Type = PointOfInterestType.Shop,
					IsMapVisibleFromStart = true,
					RunMapIndex = -1,
					DisplayRadius = 0f,
				});
			}
		}

		private void SpawnTreasurePois(ref int entityIndex, IReadOnlyList<RunMapNode> runNodes)
		{
			foreach (var treasure in SaveCache.GetRunMapTreasures())
			{
				if (treasure == null || string.IsNullOrEmpty(treasure.id)) continue;

				var worldPos = new Vector2(treasure.worldX, treasure.worldY);
				var e = EntityManager.CreateEntity($"POI_Treasure_{entityIndex++}");
				_worldByEntityId[e.Id] = worldPos;
				_pois.Add(e);
				EntityManager.AddComponent(e, new Transform { Position = worldPos, ZOrder = 10 });

				int boundsWidth = (int)IconSize;
				int boundsHeight = (int)IconSize;
				if (_treasureIconTexture != null && _treasureIconTexture.Width > 0 && _treasureIconTexture.Height > 0)
				{
					float aspectRatio = _treasureIconTexture.Height / (float)_treasureIconTexture.Width;
					boundsHeight = (int)(IconSize * aspectRatio);
				}

				bool canEnter = RunMapTreasureService.IsEnterable(treasure, runNodes);
				EntityManager.AddComponent(e, new UIElement
				{
					Bounds = new Rectangle(0, 0, boundsWidth, boundsHeight),
					IsInteractable = canEnter,
					TooltipType = TooltipType.None,
					EventType = UIElementEventType.None,
					IsPreventDefaultClick = true,
				});
				EntityManager.AddComponent(e, ParallaxLayer.GetLocationParallaxLayer());
				EntityManager.AddComponent(e, new PointOfInterest
				{
					Id = treasure.id,
					TreasureId = treasure.id,
					WorldPosition = worldPos,
					Type = PointOfInterestType.Treasure,
					IsMapVisibleFromStart = true,
					RunMapIndex = -1,
					DisplayRadius = 0f,
					IsCompleted = treasure.isClaimed,
				});
			}
		}

		public void DrawLandmarksOverFog()
		{
			DrawPois(PointOfInterestType.Shop, includeAlwaysVisibleLandmarks: true);
			DrawPois(PointOfInterestType.Treasure, includeAlwaysVisibleLandmarks: true);
		}

		public void DrawShopsOverFog()
		{
			DrawLandmarksOverFog();
		}

		public void DrawQuestPoisOverFog()
		{
			DrawPois(PointOfInterestType.Quest, includeAlwaysVisibleLandmarks: false);
		}

		private Texture2D GetIconTexture(PointOfInterestType poiType)
		{
			return poiType switch
			{
				PointOfInterestType.Hellrift when _hellriftIconTexture != null => _hellriftIconTexture,
				PointOfInterestType.Shop when _shopIconTexture != null => _shopIconTexture,
				PointOfInterestType.Treasure when _treasureIconTexture != null => _treasureIconTexture,
				_ => _questIconTexture,
			};
		}

		private void DrawPois(PointOfInterestType? filterType, bool includeAlwaysVisibleLandmarks)
		{
			var cam = EntityManager.GetEntity("LocationCamera")?.GetComponent<LocationCameraState>();
			if (cam == null) return;
			int w = cam.ViewportW;
			int h = cam.ViewportH;

			var list = _pois
				.Select(e => new { E = e, P = e.GetComponent<PointOfInterest>(), T = e.GetComponent<Transform>(), UI = e.GetComponent<UIElement>() })
				.Where(x => x.P != null && x.T != null && x.UI != null)
				.Where(x => filterType == null || x.P.Type == filterType)
				.ToList();
			float mapScale = cam.MapScale;

			foreach (var x in list)
			{
				bool isVisible = includeAlwaysVisibleLandmarks && x.P.IsMapVisibleFromStart;
				if (!isVisible && x.P.Type == PointOfInterestType.Quest)
				{
					isVisible = x.P.IsRevealed || x.P.IsCompleted;
				}

				if (x.UI != null)
				{
					x.UI.IsHidden = !isVisible;
					if (x.P.Type == PointOfInterestType.Shop || x.P.Type == PointOfInterestType.Treasure)
					{
						// Interactability updated each frame in UpdateEntity
					}
					else
					{
						x.UI.IsInteractable = isVisible && x.P.IsRevealed && !x.P.IsCompleted;
					}
				}

				if (!isVisible) continue;
				
				// Get current hover scale
				float scale = _hoverScales.TryGetValue(x.E.Id, out float s) ? s : 1f;
				
				Texture2D iconTexture = GetIconTexture(x.P.Type);
				bool isClaimedTreasure = x.P.Type == PointOfInterestType.Treasure && x.P.IsCompleted;
				Color iconTint = isClaimedTreasure ? new Color(120, 120, 120) : Color.White;
				
				// Calculate icon dimensions preserving aspect ratio, scaled by map zoom
				float iconWidth = IconSize * mapScale * scale;
				float iconHeight = iconWidth;
				if (iconTexture != null && iconTexture.Width > 0 && iconTexture.Height > 0)
				{
					float aspectRatio = iconTexture.Height / (float)iconTexture.Width;
					iconHeight = iconWidth * aspectRatio;
				}
				
				// Calculate icon bounds
				float halfWidth = iconWidth / 2f;
				float halfHeight = iconHeight / 2f;
				var iconPos = new Vector2(x.T.Position.X, x.T.Position.Y);
				var iconRect = new Rectangle((int)System.Math.Round(iconPos.X - halfWidth), (int)System.Math.Round(iconPos.Y - halfHeight), (int)System.Math.Round(iconWidth), (int)System.Math.Round(iconHeight));
				
				// Skip if off-screen
				if (iconRect.Right < 0 || iconRect.Bottom < 0 || iconRect.Left > w || iconRect.Top > h) continue;
				
				// Draw icon texture
				if (iconTexture != null)
				{
					_spriteBatch.Draw(iconTexture, iconRect, iconTint);
				}
				else
				{
					// Fallback to pixel if texture failed to load
					_spriteBatch.Draw(_pixel, iconRect, x.P.IsCompleted ? Color.Green : Color.Red);
				}
				
				// Draw red circle for incomplete quests (not for Hellrift POIs)
				if (!x.P.IsCompleted && x.P.Type == PointOfInterestType.Quest)
				{
					DrawCircle(iconPos, halfWidth, halfHeight, CircleSize * mapScale * scale);
				}

				// Draw difficulty skulls below the icon for any POI with Difficulty > 0
				if (_skullTexture != null && x.P.Difficulty > 0)
				{
					int difficulty = System.Math.Max(0, x.P.Difficulty);
					// Clamp to a reasonable max to avoid extreme layouts
					difficulty = System.Math.Min(difficulty, 10);

					// Size and gap scaled by zoom and hover
					float skullWidth = SkullSize * mapScale * scale;
					float skullHeight = skullWidth;
					if (_skullTexture.Width > 0 && _skullTexture.Height > 0)
					{
						float skullAspect = _skullTexture.Height / (float)_skullTexture.Width;
						skullHeight = skullWidth * skullAspect;
					}

					float skullGap = SkullGap * mapScale * scale;
					float totalWidth = difficulty * skullWidth + (difficulty - 1) * skullGap;

					// Row is centered horizontally on the icon and positioned below it
					float startX = iconPos.X - totalWidth / 2f;
					float offsetY = SkullOffsetY * iconHeight * mapScale * scale;
					float rowY = iconRect.Bottom + offsetY;

					for (int i = 0; i < difficulty; i++)
					{
						float xPos = startX + i * (skullWidth + skullGap);
						var skullRect = new Rectangle(
							(int)System.Math.Round(xPos),
							(int)System.Math.Round(rowY),
							(int)System.Math.Round(skullWidth),
							(int)System.Math.Round(skullHeight));

						_spriteBatch.Draw(_skullTexture, skullRect, Color.White);
					}
				}
			}
		}

		private void DrawCircle(Vector2 iconCenter, float iconHalfWidth, float iconHalfHeight, float circleSize)
		{
			// Position circle relative to icon center
			float offsetX = iconHalfWidth * CircleOffsetX;
			float offsetY = iconHalfHeight * CircleOffsetY;
			Vector2 circlePos = iconCenter + new Vector2(offsetX, offsetY);
			
			// Get anti-aliased circle texture
			int radius = (int)System.Math.Round(circleSize / 2f);
			if (radius < 1) radius = 1;
			var circleTexture = PrimitiveTextureFactory.GetAntiAliasedCircle(_graphicsDevice, radius);
			
			// Draw circle centered at position
			Vector2 origin = new Vector2(radius, radius);
			_spriteBatch.Draw(circleTexture, circlePos, null, Color.Red, 0f, origin, 1f, SpriteEffects.None, 0f);
		}
	}
}


