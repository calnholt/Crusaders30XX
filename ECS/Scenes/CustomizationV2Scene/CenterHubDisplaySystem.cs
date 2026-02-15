using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Rendering;
using Crusaders30XX.ECS.Singletons;
using Crusaders30XX.ECS.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("CV2 Hub")]
	public class CenterHubDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly SpriteFont _headingFont = FontSingleton.TitleFont;
		private readonly SpriteFont _contentFont = FontSingleton.ContentFont;
		private Texture2D _pixel;
		private Entity _hubEntity;
		private float _idleRotation1;
		private float _idleRotation2;
		private float _idlePulse;

		[DebugEditable(DisplayName = "Hub Radius", Step = 4, Min = 60, Max = 600)]
		public int HubRadius { get; set; } = 230;

		[DebugEditable(DisplayName = "Hub BG R", Step = 1, Min = 0, Max = 255)]
		public int HubBgR { get; set; } = 26;

		[DebugEditable(DisplayName = "Hub BG G", Step = 1, Min = 0, Max = 255)]
		public int HubBgG { get; set; } = 26;

		[DebugEditable(DisplayName = "Hub BG B", Step = 1, Min = 0, Max = 255)]
		public int HubBgB { get; set; } = 26;

		[DebugEditable(DisplayName = "Hub Border Width", Step = 1, Min = 1, Max = 6)]
		public int HubBorderWidth { get; set; } = 3;

		[DebugEditable(DisplayName = "Hub Border R", Step = 1, Min = 0, Max = 255)]
		public int HubBorderR { get; set; } = 160;

		[DebugEditable(DisplayName = "Hub Border G", Step = 1, Min = 0, Max = 255)]
		public int HubBorderG { get; set; } = 0;

		[DebugEditable(DisplayName = "Hub Border B", Step = 1, Min = 0, Max = 255)]
		public int HubBorderB { get; set; } = 0;

		[DebugEditable(DisplayName = "Idle Ring1 Speed", Step = 0.1f, Min = 0.1f, Max = 2.0f)]
		public float IdleRing1Speed { get; set; } = 0.5236f;

		[DebugEditable(DisplayName = "Idle Ring2 Speed", Step = 0.1f, Min = -2.0f, Max = 0)]
		public float IdleRing2Speed { get; set; } = -0.7854f;

		[DebugEditable(DisplayName = "Idle Ring1 Radius", Step = 4, Min = 40, Max = 160)]
		public int IdleRing1Radius { get; set; } = 100;

		[DebugEditable(DisplayName = "Idle Ring2 Radius", Step = 4, Min = 30, Max = 120)]
		public int IdleRing2Radius { get; set; } = 80;

		[DebugEditable(DisplayName = "Idle Dot Radius", Step = 1, Min = 2, Max = 16)]
		public int IdleDotRadius { get; set; } = 6;

		[DebugEditable(DisplayName = "Tooltip Label Scale", Step = 0.01f, Min = 0.03f, Max = 0.2f)]
		public float TooltipLabelScale { get; set; } = 0.10f;

		[DebugEditable(DisplayName = "Tooltip Name Scale", Step = 0.01f, Min = 0.05f, Max = 0.3f)]
		public float TooltipNameScale { get; set; } = 0.18f;

		[DebugEditable(DisplayName = "Tooltip Desc Scale", Step = 0.01f, Min = 0.03f, Max = 0.2f)]
		public float TooltipDescScale { get; set; } = 0.10f;

		[DebugEditable(DisplayName = "Stat Line Scale", Step = 0.01f, Min = 0.03f, Max = 0.2f)]
		public float StatLineScale { get; set; } = 0.12f;

		[DebugEditable(DisplayName = "Equipped Badge Scale", Step = 0.01f, Min = 0.03f, Max = 0.15f)]
		public float EquippedBadgeScale { get; set; } = 0.06f;

		[DebugEditable(DisplayName = "Content Top Offset", Step = 4, Min = 20, Max = 120)]
		public int ContentTopOffset { get; set; } = 50;

		[DebugEditable(DisplayName = "Section Gap", Step = 1, Min = 2, Max = 20)]
		public int SectionGap { get; set; } = 8;

		[DebugEditable(DisplayName = "Desc Max Width Pct", Step = 0.05f, Min = 0.5f, Max = 1.0f)]
		public float DescMaxWidthPct { get; set; } = 0.85f;

		[DebugEditable(DisplayName = "Stat Pip Radius", Step = 1, Min = 2, Max = 12)]
		public int StatPipRadius { get; set; } = 5;

		[DebugEditable(DisplayName = "Stat Pip Gap", Step = 1, Min = 1, Max = 10)]
		public int StatPipGap { get; set; } = 4;

		public WheelLayoutSystem LayoutSystem { get; set; }
		public BrowseStateSystem BrowseSystem { get; set; }

		public CenterHubDisplaySystem(EntityManager em, GraphicsDevice gd, SpriteBatch sb) : base(em)
		{
			_graphicsDevice = gd;
			_spriteBatch = sb;
			EventManager.Subscribe<ShowTransition>(_ => CleanupHub());
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<SceneState>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			var scene = entity.GetComponent<SceneState>();
			if (scene == null || scene.Current != SceneId.CustomizationV2) return;

			var nav = EntityManager.GetEntitiesWithComponent<CustomizationV2NavigationState>().FirstOrDefault()?.GetComponent<CustomizationV2NavigationState>();
			if (nav == null || nav.ActiveTab != CustomizationV2TabType.Loadout) return;

			float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
			_idleRotation1 += IdleRing1Speed * dt;
			_idleRotation2 += IdleRing2Speed * dt;
			_idlePulse += dt * 2f;

			EnsureHubEntity();
		}

		private void EnsureHubEntity()
		{
			if (_hubEntity != null && _hubEntity.IsActive) return;
			_hubEntity = EntityManager.CreateEntity("CV2_CenterHub");
			EntityManager.AddComponent(_hubEntity, new CenterHub());
			var center = LayoutSystem?.GetWheelCenter() ?? new Vector2(480, 540);
			EntityManager.AddComponent(_hubEntity, new Transform { Position = center, ZOrder = 55000 });
			EntityManager.AddComponent(_hubEntity, new UIElement
			{
				Bounds = new Rectangle((int)(center.X - HubRadius), (int)(center.Y - HubRadius), HubRadius * 2, HubRadius * 2),
				IsInteractable = true,
				IsPreventDefaultClick = true
			});
		}

		public void Draw()
		{
			var scene = EntityManager.GetEntitiesWithComponent<SceneState>().FirstOrDefault()?.GetComponent<SceneState>();
			if (scene == null || scene.Current != SceneId.CustomizationV2) return;

			var nav = EntityManager.GetEntitiesWithComponent<CustomizationV2NavigationState>().FirstOrDefault()?.GetComponent<CustomizationV2NavigationState>();
			if (nav == null || nav.ActiveTab != CustomizationV2TabType.Loadout) return;

			if (_pixel == null)
			{
				_pixel = new Texture2D(_graphicsDevice, 1, 1);
				_pixel.SetData(new[] { Color.White });
			}

			var center = LayoutSystem?.GetWheelCenter() ?? new Vector2(480, 540);
			var loadout = EntityManager.GetEntitiesWithComponent<CustomizationV2LoadoutState>().FirstOrDefault()?.GetComponent<CustomizationV2LoadoutState>();
			bool hasSelection = loadout != null && loadout.HoveredSegmentIndex >= 0;

			// Hub circle background
			var hubBg = PrimitiveTextureFactory.GetAntiAliasedCircle(_graphicsDevice, HubRadius);
			int d = HubRadius * 2;
			_spriteBatch.Draw(hubBg, new Rectangle((int)(center.X - HubRadius), (int)(center.Y - HubRadius), d, d), new Color(HubBgR, HubBgG, HubBgB));

			// Hub border
			var borderCircle = PrimitiveTextureFactory.GetAntiAliasedCircle(_graphicsDevice, HubRadius + HubBorderWidth);
			int bd = (HubRadius + HubBorderWidth) * 2;
			var borderColor = new Color(HubBorderR, HubBorderG, HubBorderB);
			_spriteBatch.Draw(borderCircle, new Rectangle((int)(center.X - HubRadius - HubBorderWidth), (int)(center.Y - HubRadius - HubBorderWidth), bd, bd), borderColor);
			_spriteBatch.Draw(hubBg, new Rectangle((int)(center.X - HubRadius), (int)(center.Y - HubRadius), d, d), new Color(HubBgR, HubBgG, HubBgB));

			if (hasSelection)
			{
				DrawTooltipState(center, loadout);
			}
			else
			{
				DrawIdleState(center);
			}
		}

		private void DrawIdleState(Vector2 center)
		{
			// Rotating ring 1 (outer)
			DrawRotatingRing(center, IdleRing1Radius, _idleRotation1, new Color(160, 0, 0, 50));

			// Rotating ring 2 (inner, reverse)
			DrawRotatingRing(center, IdleRing2Radius, _idleRotation2, new Color(196, 30, 58, 40));

			// Pulsing center dot
			float pulseAlpha = 0.4f + 0.4f * MathF.Sin(_idlePulse);
			var dotCircle = PrimitiveTextureFactory.GetAntiAliasedCircle(_graphicsDevice, IdleDotRadius);
			int dotD = IdleDotRadius * 2;
			_spriteBatch.Draw(dotCircle, new Rectangle((int)(center.X - IdleDotRadius), (int)(center.Y - IdleDotRadius), dotD, dotD), new Color(160, 0, 0) * pulseAlpha);
		}

		private void DrawRotatingRing(Vector2 center, int radius, float angle, Color color)
		{
			var circle = PrimitiveTextureFactory.GetAntiAliasedCircle(_graphicsDevice, radius + 1);
			var innerCircle = PrimitiveTextureFactory.GetAntiAliasedCircle(_graphicsDevice, radius - 1);
			int od = (radius + 1) * 2;
			int id = (radius - 1) * 2;
			_spriteBatch.Draw(circle, new Rectangle((int)(center.X - radius - 1), (int)(center.Y - radius - 1), od, od), color);
			_spriteBatch.Draw(innerCircle, new Rectangle((int)(center.X - radius + 1), (int)(center.Y - radius + 1), id, id), new Color(HubBgR, HubBgG, HubBgB));
		}

		private void DrawTooltipState(Vector2 center, CustomizationV2LoadoutState loadout)
		{
			var slot = WheelLayoutSystem.GetSlotType(loadout.HoveredSegmentIndex);
			string slotLabel = WheelLayoutSystem.GetSlotLabel(slot);

			// Get browsed item info
			string itemId = BrowseSystem?.GetBrowsedItemId() ?? "";
			string itemName = GetItemName(itemId);
			string itemDesc = GetItemDescription(itemId);
			bool isEquipped = IsCurrentlyEquipped(slot, itemId, loadout);
			var stats = GetItemStats(itemId);

			float y = center.Y - HubRadius + ContentTopOffset;

			// Slot label (red, uppercase)
			y = DrawCenteredText(_headingFont, slotLabel, center.X, y, TooltipLabelScale, new Color(196, 30, 58));
			y += SectionGap;

			// Item name (white, large)
			if (!string.IsNullOrEmpty(itemName))
			{
				y = DrawCenteredText(_headingFont, itemName, center.X, y, TooltipNameScale, Color.White);
				y += SectionGap / 2;
			}

			// Stat line (red, type-specific)
			if (stats.HasStats)
			{
				y = DrawStatLine(center.X, y, stats);
				y += SectionGap;
			}

			// Description (gray, word-wrapped within hub circle)
			if (!string.IsNullOrEmpty(itemDesc))
			{
				int maxWidth = (int)(HubRadius * 2 * DescMaxWidthPct);
				var wrappedLines = TextUtils.WrapText(_contentFont, itemDesc, TooltipDescScale, maxWidth);
				var descColor = new Color(136, 136, 136);
				foreach (var line in wrappedLines)
				{
					if (string.IsNullOrEmpty(line)) { y += SectionGap; continue; }
					y = DrawCenteredText(_contentFont, line, center.X, y, TooltipDescScale, descColor);
				}
				y += SectionGap;
			}

			// Equipped badge (red)
			if (isEquipped)
			{
				string badge = "EQUIPPED";
				y = DrawCenteredText(_headingFont, badge, center.X, y, EquippedBadgeScale, new Color(196, 30, 58));
			}
		}

		private float DrawCenteredText(SpriteFont font, string text, float centerX, float y, float scale, Color color)
		{
			var size = font.MeasureString(text) * scale;
			float x = centerX - size.X / 2f;
			_spriteBatch.DrawString(font, text, new Vector2(x, y), color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
			return y + size.Y;
		}

		private struct ItemStats
		{
			public bool HasStats;
			public string Label;
			public int Value;
			public int SecondaryValue;
			public bool HasSecondary;
			public List<string> CostPips;
			public WheelSlotType SlotType;
		}

		private ItemStats GetItemStats(string id)
		{
			if (string.IsNullOrEmpty(id)) return default;

			// Weapon: show DMG + cost pips
			var card = Factories.CardFactory.Create(id);
			if (card != null && card.IsWeapon)
			{
				return new ItemStats
				{
					HasStats = true,
					Label = "DMG",
					Value = card.Damage,
					CostPips = card.Cost,
					SlotType = WheelSlotType.Weapon
				};
			}

			// Equipment: show BLK + uses
			var eq = Factories.EquipmentFactory.Create(id);
			if (eq != null)
			{
				return new ItemStats
				{
					HasStats = true,
					Label = "BLK",
					Value = eq.Block,
					SecondaryValue = eq.Uses,
					HasSecondary = true,
					SlotType = WheelSlotType.Head
				};
			}

			// Temperance: show threshold
			if (Data.Temperance.TemperanceAbilityDefinitionCache.TryGet(id, out var temp) && temp != null)
			{
				return new ItemStats
				{
					HasStats = true,
					Label = "Threshold",
					Value = temp.threshold,
					SlotType = WheelSlotType.Temperance
				};
			}

			return default;
		}

		private float DrawStatLine(float centerX, float y, ItemStats stats)
		{
			var statColor = new Color(196, 30, 58);
			string statText = $"{stats.Label}: {stats.Value}";

			// For equipment, append uses
			if (stats.HasSecondary && stats.SecondaryValue > 0)
			{
				statText += $"  x{stats.SecondaryValue}";
			}

			var textSize = _headingFont.MeasureString(statText) * StatLineScale;

			// Calculate total width including cost pips for weapons
			float totalWidth = textSize.X;
			if (stats.CostPips != null && stats.CostPips.Count > 0)
			{
				float pipWidth = stats.CostPips.Count * (StatPipRadius * 2 + StatPipGap) - StatPipGap;
				totalWidth += SectionGap + pipWidth;
			}

			float startX = centerX - totalWidth / 2f;

			// Draw stat text
			_spriteBatch.DrawString(_headingFont, statText, new Vector2(startX, y), statColor, 0f, Vector2.Zero, StatLineScale, SpriteEffects.None, 0f);

			// Draw cost pips for weapons
			if (stats.CostPips != null && stats.CostPips.Count > 0)
			{
				float pipX = startX + textSize.X + SectionGap;
				float pipCenterY = y + textSize.Y / 2f;

				foreach (var cost in stats.CostPips)
				{
					bool isAny = string.Equals(cost?.Trim(), "any", StringComparison.OrdinalIgnoreCase);
					if (isAny)
					{
						var anyTex = PrimitiveTextureFactory.GetAnyCostPipTexture(_graphicsDevice, StatPipRadius);
						_spriteBatch.Draw(anyTex, new Rectangle((int)pipX, (int)(pipCenterY - StatPipRadius), StatPipRadius * 2, StatPipRadius * 2), Color.White);
					}
					else
					{
						var pipColor = GetCostPipColor(cost);
						var circle = PrimitiveTextureFactory.GetAntiAliasedCircle(_graphicsDevice, StatPipRadius);
						_spriteBatch.Draw(circle, new Rectangle((int)pipX, (int)(pipCenterY - StatPipRadius), StatPipRadius * 2, StatPipRadius * 2), pipColor);
					}
					pipX += StatPipRadius * 2 + StatPipGap;
				}
			}

			return y + textSize.Y;
		}

		private static Color GetCostPipColor(string cost) => (cost?.ToLowerInvariant()) switch
		{
			"white" => Color.White,
			"red" => Color.DarkRed,
			"black" => Color.Black,
			_ => new Color(224, 224, 224)
		};

		private string GetItemName(string id)
		{
			if (string.IsNullOrEmpty(id)) return "";
			var eq = Factories.EquipmentFactory.Create(id);
			if (eq != null) return eq.Name ?? id;
			var medal = Factories.MedalFactory.Create(id);
			if (medal != null) return medal.Name ?? id;
			if (Data.Temperance.TemperanceAbilityDefinitionCache.TryGet(id, out var temp) && temp != null)
				return temp.name ?? id;
			var card = Factories.CardFactory.Create(id);
			if (card != null) return card.Name ?? id;
			return id;
		}

		private string GetItemDescription(string id)
		{
			if (string.IsNullOrEmpty(id)) return "";
			var eq = Factories.EquipmentFactory.Create(id);
			if (eq != null) return eq.Text ?? "";
			var medal = Factories.MedalFactory.Create(id);
			if (medal != null) return medal.Text ?? "";
			if (Data.Temperance.TemperanceAbilityDefinitionCache.TryGet(id, out var temp) && temp != null)
				return temp.text ?? "";
			var card = Factories.CardFactory.Create(id);
			if (card != null) return card.Text ?? "";
			return "";
		}

		private bool IsCurrentlyEquipped(WheelSlotType slot, string itemId, CustomizationV2LoadoutState st)
		{
			if (st == null || string.IsNullOrEmpty(itemId)) return false;
			string equippedId = slot switch
			{
				WheelSlotType.Weapon => st.WorkingWeaponId,
				WheelSlotType.Head => st.WorkingHeadId,
				WheelSlotType.Chest => st.WorkingChestId,
				WheelSlotType.Arms => st.WorkingArmsId,
				WheelSlotType.Legs => st.WorkingLegsId,
				WheelSlotType.Temperance => st.WorkingTemperanceId,
				WheelSlotType.Medal1 => st.WorkingMedalIds?.Count > 0 ? st.WorkingMedalIds[0] : "",
				WheelSlotType.Medal2 => st.WorkingMedalIds?.Count > 1 ? st.WorkingMedalIds[1] : "",
				WheelSlotType.Medal3 => st.WorkingMedalIds?.Count > 2 ? st.WorkingMedalIds[2] : "",
				_ => ""
			};
			return string.Equals(equippedId, itemId, StringComparison.OrdinalIgnoreCase);
		}

		private void CleanupHub()
		{
			if (_hubEntity != null)
			{
				EntityManager.DestroyEntity(_hubEntity.Id);
				_hubEntity = null;
			}
		}
	}
}
