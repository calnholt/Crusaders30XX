using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Save;
using Crusaders30XX.ECS.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("Climb Header")]
	public class ClimbHeaderDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly ContentManager _content;
		private readonly Texture2D _pixel;
		private Texture2D _weaponArt;
		private string _weaponArtKey = string.Empty;
		private float _resourcePreviewAlpha;
		private ClimbResourceSave _lastPreviewResources = new ClimbResourceSave();

		[DebugEditable(DisplayName = "Header Height", Step = 1, Min = 40, Max = 180)]
		public int HeaderHeight { get; set; } = 90;
		[DebugEditable(DisplayName = "Header Padding X", Step = 1, Min = 0, Max = 120)]
		public int HeaderPaddingX { get; set; } = 32;
		[DebugEditable(DisplayName = "Header Padding Top", Step = 1, Min = 0, Max = 80)]
		public int HeaderPaddingTop { get; set; } = 10;
		[DebugEditable(DisplayName = "Header Padding Bottom", Step = 1, Min = 0, Max = 80)]
		public int HeaderPaddingBottom { get; set; } = 12;
		[DebugEditable(DisplayName = "Header Gap", Step = 1, Min = 0, Max = 80)]
		public int HeaderGap { get; set; } = 16;
		[DebugEditable(DisplayName = "Weapon Button Size", Step = 1, Min = 32, Max = 160)]
		public int WeaponButtonSize { get; set; } = 67;
		[DebugEditable(DisplayName = "Timeline Padding X", Step = 1, Min = 0, Max = 40)]
		public int TimelinePaddingX { get; set; } = 10;
		[DebugEditable(DisplayName = "Timeline Padding Y", Step = 1, Min = 0, Max = 40)]
		public int TimelinePaddingY { get; set; } = 8;
		[DebugEditable(DisplayName = "Timeline Slot Gap", Step = 1, Min = 0, Max = 12)]
		public int TimelineSlotGap { get; set; } = 3;
		[DebugEditable(DisplayName = "Timeline Label Font Scale", Step = 0.01f, Min = 0.03f, Max = 0.3f)]
		public float TimelineLabelFontScale { get; set; } = 0.07f;
		[DebugEditable(DisplayName = "Timeline Value Font Scale", Step = 0.01f, Min = 0.03f, Max = 0.4f)]
		public float TimelineValueFontScale { get; set; } = 0.10f;
		[DebugEditable(DisplayName = "Resource Label Font Scale", Step = 0.01f, Min = 0.03f, Max = 0.3f)]
		public float ResourceLabelFontScale { get; set; } = 0.09f;
		[DebugEditable(DisplayName = "Resource Amount Font Scale", Step = 0.01f, Min = 0.03f, Max = 0.4f)]
		public float ResourceAmountFontScale { get; set; } = 0.14f;
		[DebugEditable(DisplayName = "Resource Icon Size", Step = 1, Min = 6, Max = 48)]
		public int ResourceIconSize { get; set; } = 18;
		[DebugEditable(DisplayName = "Resource Fade Seconds", Step = 0.01f, Min = 0.01f, Max = 1f)]
		public float ResourceFadeSeconds { get; set; } = 0.12f;
		[DebugEditable(DisplayName = "Timeline Hourglass Width", Step = 1, Min = 3, Max = 24)]
		public int TimelineHourglassWidth { get; set; } = 8;
		[DebugEditable(DisplayName = "Timeline Hourglass Height", Step = 1, Min = 4, Max = 32)]
		public int TimelineHourglassHeight { get; set; } = 11;

		internal static int HeaderHeightValue { get; private set; } = 90;
		internal static int HeaderPaddingXValue { get; private set; } = 32;
		internal static int HeaderPaddingTopValue { get; private set; } = 10;
		internal static int HeaderGapValue { get; private set; } = 16;
		internal static int WeaponButtonSizeValue { get; private set; } = 67;

		public ClimbHeaderDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, ContentManager content)
			: base(entityManager)
		{
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
			_content = content;
			_pixel = new Texture2D(graphicsDevice, 1, 1);
			_pixel.SetData(new[] { Color.White });
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			yield break;
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			HeaderHeightValue = HeaderHeight;
			HeaderPaddingXValue = HeaderPaddingX;
			HeaderPaddingTopValue = HeaderPaddingTop;
			HeaderGapValue = HeaderGap;
			WeaponButtonSizeValue = WeaponButtonSize;
		}

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);
			HeaderHeightValue = HeaderHeight;
			HeaderPaddingXValue = HeaderPaddingX;
			HeaderPaddingTopValue = HeaderPaddingTop;
			HeaderGapValue = HeaderGap;
			WeaponButtonSizeValue = WeaponButtonSize;
			if (IsClimbScene())
			{
				SyncWeaponArt();
				UpdateResourcePreviewFade(gameTime);
			}
		}

		public void Draw()
		{
			if (!IsClimbScene()) return;

			var climb = SaveCache.GetClimbState();
			var preview = GetPreview();
			var header = GetBounds(ClimbHeaderLayoutSystem.HeaderName);
			if (header.Width <= 0) header = new Rectangle(0, 0, Game1.VirtualWidth, HeaderHeight);

			_spriteBatch.Draw(_pixel, new Rectangle(header.X, header.Y + 4, header.Width, header.Height), Color.Black * 0.45f);
			_spriteBatch.Draw(_pixel, header, ClimbSceneDrawHelpers.Black1 * 0.82f);
			ClimbSceneDrawHelpers.DrawBorder(_spriteBatch, _pixel, new Rectangle(0, header.Bottom - 1, Game1.VirtualWidth, 1), Color.White * 0.85f, 1);

			DrawTimeline(GetBounds(ClimbHeaderLayoutSystem.TimelineName), climb, preview);
			DrawResources(GetBounds(ClimbHeaderLayoutSystem.ResourceBarName), climb, preview);
			DrawWeaponButton(GetBounds(ClimbHeaderLayoutSystem.LoadoutButtonName));
		}

		private void DrawTimeline(Rectangle rect, ClimbSaveState climb, ClimbPreviewState preview)
		{
			if (rect.Width <= 0 || rect.Height <= 0) return;

			int used = ClimbRuleService.ClampTime(climb?.time ?? 0);
			int projected = preview?.IsActive == true ? preview.ProjectedUsedTime : used;
			int remaining = ClimbRuleService.MaxTime - projected;
			int delta = Math.Max(0, projected - used);

			_spriteBatch.Draw(_pixel, rect, ClimbSceneDrawHelpers.Black0 * 0.66f);
			ClimbSceneDrawHelpers.DrawBorder(_spriteBatch, _pixel, rect, Color.White * 0.22f, 1);

			var labelY = rect.Y + TimelinePaddingY;
			ClimbSceneDrawHelpers.DrawText(_spriteBatch, used.ToString(), new Vector2(rect.X + TimelinePaddingX, labelY), TimelineValueFontScale, ClimbSceneDrawHelpers.White1);
			ClimbSceneDrawHelpers.DrawText(_spriteBatch, "used", new Vector2(rect.X + TimelinePaddingX + 28, labelY + 5), TimelineLabelFontScale, ClimbSceneDrawHelpers.White3);
			ClimbSceneDrawHelpers.DrawText(_spriteBatch, remaining.ToString(), new Vector2(rect.Right - TimelinePaddingX - 60, labelY), TimelineValueFontScale, ClimbSceneDrawHelpers.White1);
			ClimbSceneDrawHelpers.DrawText(_spriteBatch, "remaining", new Vector2(rect.Right - TimelinePaddingX - 34, labelY + 5), TimelineLabelFontScale, ClimbSceneDrawHelpers.White3);

			int trackX = rect.X + TimelinePaddingX;
			int trackY = rect.Y + rect.Height - TimelinePaddingY - 16;
			int trackW = rect.Width - TimelinePaddingX * 2;
			int gapTotal = TimelineSlotGap * (ClimbRuleService.MaxTime - 1);
			int slotW = Math.Max(4, (trackW - gapTotal) / ClimbRuleService.MaxTime);
			for (int i = 0; i < ClimbRuleService.MaxTime; i++)
			{
				int x = trackX + i * (slotW + TimelineSlotGap);
				var slotRect = new Rectangle(x, trackY, slotW, 16);
				bool shopMarker = i > 0 && i % ClimbRuleService.ShopRefreshInterval == 0;
				if (shopMarker)
				{
					bool expiring = delta > 0 && projected >= i && used < i;
					ClimbSceneDrawHelpers.DrawShopMarkerIcon(_spriteBatch, _pixel, slotRect, expiring ? ClimbSceneDrawHelpers.Red2 : ClimbSceneDrawHelpers.White3);
					continue;
				}

				bool isUsed = i < used;
				bool isPreview = delta > 0 && i >= used && i < projected;
				var iconRect = new Rectangle(slotRect.X + (slotRect.Width - TimelineHourglassWidth) / 2, slotRect.Y + 2, TimelineHourglassWidth, TimelineHourglassHeight);
				ClimbSceneDrawHelpers.DrawHourglassIcon(
					_spriteBatch,
					_pixel,
					iconRect,
					isPreview ? ClimbSceneDrawHelpers.Red2 : ClimbSceneDrawHelpers.White3,
					isPreview ? ClimbSceneDrawHelpers.Red2 : ClimbSceneDrawHelpers.White2,
					isUsed || isPreview);
			}
		}

		private void DrawResources(Rectangle rect, ClimbSaveState climb, ClimbPreviewState preview)
		{
			if (rect.Width <= 0 || rect.Height <= 0) return;
			var baseResources = climb?.resources ?? new ClimbResourceSave();
			var previewResources = _lastPreviewResources ?? baseResources;

			_spriteBatch.Draw(_pixel, rect, ClimbSceneDrawHelpers.Black0 * 0.62f);
			ClimbSceneDrawHelpers.DrawBorder(_spriteBatch, _pixel, rect, Color.White * 0.28f, 1);
			ClimbSceneDrawHelpers.DrawText(_spriteBatch, "Resources", new Vector2(rect.X + 10, rect.Y + 7), ResourceLabelFontScale, ClimbSceneDrawHelpers.White3);

			int y = rect.Y + 34;
			DrawResourceAmount(new Vector2(rect.X + 10, y), ClimbResourceType.Red, baseResources.red, previewResources.red, ClimbSceneDrawHelpers.Red2);
			DrawResourceAmount(new Vector2(rect.X + 56, y), ClimbResourceType.White, baseResources.white, previewResources.white, ClimbSceneDrawHelpers.White1);
			DrawResourceAmount(new Vector2(rect.X + 102, y), ClimbResourceType.Black, baseResources.black, previewResources.black, ClimbSceneDrawHelpers.White3);
		}

		private void DrawResourceAmount(Vector2 pos, ClimbResourceType type, int amount, int previewAmount, Color color)
		{
			ClimbSceneDrawHelpers.DrawResourceIcon(_spriteBatch, _graphicsDevice, _pixel, pos, type, ResourceIconSize, color);
			var textPos = new Vector2(pos.X + ResourceIconSize + 4, pos.Y - 1);
			float previewAlpha = MathHelper.Clamp(_resourcePreviewAlpha, 0f, 1f);
			if (previewAlpha <= 0.001f || amount == previewAmount)
			{
				ClimbSceneDrawHelpers.DrawText(_spriteBatch, amount.ToString(), textPos, ResourceAmountFontScale, ClimbSceneDrawHelpers.White1);
				return;
			}

			ClimbSceneDrawHelpers.DrawText(_spriteBatch, amount.ToString(), textPos, ResourceAmountFontScale, ClimbSceneDrawHelpers.White1 * (1f - previewAlpha));
			ClimbSceneDrawHelpers.DrawText(_spriteBatch, previewAmount.ToString(), textPos, ResourceAmountFontScale, ClimbSceneDrawHelpers.White1 * previewAlpha);
		}

		private void UpdateResourcePreviewFade(GameTime gameTime)
		{
			var climb = SaveCache.GetClimbState();
			var preview = GetPreview();
			var current = climb?.resources ?? new ClimbResourceSave();
			bool previewingResources = preview?.IsActive == true && !ResourcesEqual(current, preview.ProjectedResources);
			if (previewingResources)
			{
				_lastPreviewResources = CloneResources(preview.ProjectedResources);
			}

			float target = previewingResources ? 1f : 0f;
			float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
			float delta = ResourceFadeSeconds <= 0f ? 1f : elapsed / ResourceFadeSeconds;
			_resourcePreviewAlpha = MathHelper.Clamp(
				_resourcePreviewAlpha + (target > _resourcePreviewAlpha ? delta : -delta),
				0f,
				1f);
		}

		private static bool ResourcesEqual(ClimbResourceSave a, ClimbResourceSave b)
		{
			return (a?.red ?? 0) == (b?.red ?? 0)
				&& (a?.white ?? 0) == (b?.white ?? 0)
				&& (a?.black ?? 0) == (b?.black ?? 0);
		}

		private static ClimbResourceSave CloneResources(ClimbResourceSave resources)
		{
			return new ClimbResourceSave
			{
				red = resources?.red ?? 0,
				white = resources?.white ?? 0,
				black = resources?.black ?? 0,
			};
		}

		private void DrawWeaponButton(Rectangle rect)
		{
			if (rect.Width <= 0 || rect.Height <= 0) return;
			var ui = EntityManager.GetEntity(ClimbHeaderLayoutSystem.LoadoutButtonName)?.GetComponent<UIElement>();
			_spriteBatch.Draw(_pixel, rect, ClimbSceneDrawHelpers.Black0 * 0.80f);
			if (_weaponArt != null)
			{
				_spriteBatch.Draw(_weaponArt, rect, Color.White);
			}
			ClimbSceneDrawHelpers.DrawBorder(_spriteBatch, _pixel, rect, ui?.IsHovered == true ? ClimbSceneDrawHelpers.Red2 : Color.White * 0.8f, 2);
		}

		private Rectangle GetBounds(string name)
		{
			return EntityManager.GetEntity(name)?.GetComponent<UIElement>()?.Bounds ?? Rectangle.Empty;
		}

		private ClimbPreviewState GetPreview()
		{
			return EntityManager.GetEntity(ClimbHeaderLayoutSystem.RootName)?.GetComponent<ClimbPreviewState>();
		}

		private void SyncWeaponArt()
		{
			string weaponId = SaveCache.GetLoadout(RunDeckService.PrimaryLoadoutId)?.weaponId ?? "sword";
			string asset = CrusaderPortraitAssets.ResolveWeaponCardArtAsset(weaponId);
			if (_weaponArt != null && _weaponArtKey == asset) return;
			_weaponArtKey = asset;
			try { _weaponArt = _content.Load<Texture2D>(asset); }
			catch { _weaponArt = null; }
		}

		private bool IsClimbScene()
		{
			return EntityManager.GetEntitiesWithComponent<SceneState>()
				.FirstOrDefault()
				?.GetComponent<SceneState>()
				?.Current == SceneId.Climb;
		}
	}
}
