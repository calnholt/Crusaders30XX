using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Rendering;
using Crusaders30XX.ECS.Data.Equipment;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Singletons;
using Crusaders30XX.ECS.Services;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Displays equipped equipment icons (head/chest/arms/legs) on the left side of the screen,
	/// grouped by type in vertical order: Head, Chest, Arms, Legs. Multiple items of the same
	/// type are drawn in a row for that type.
	/// </summary>
	[DebugTab("Equipment Display")] 
	public class EquipmentDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly ContentManager _content;
		private SpriteFont _font = FontSingleton.ContentFont;
		private readonly Dictionary<string, Texture2D> _iconCache = new();
		private readonly Dictionary<(int w, int h, int r), Texture2D> _roundedRectCache = new();
		private readonly Dictionary<int, Entity> _tooltipByEquipEntityId = new();

		private double _pulseTimeRemaining = 0.0;
		private const double PulseDurationSeconds = 0.18; // similar to draw pile
		private const float PulseAmplitude = 0.12f; // 12% size bump
		private double _lastDt = 0.0;
		// Root anchor removed; each equipment item will own its own transform base

		// Layout constants (pixels)
		[DebugEditable(DisplayName = "Left Margin", Step = 2, Min = 0, Max = 2000)]
		public int LeftMargin { get; set; } = 8;
		[DebugEditable(DisplayName = "Top Margin", Step = 2, Min = 0, Max = 2000)]
		public int TopMargin { get; set; } = 120;
		[DebugEditable(DisplayName = "Icon Size", Step = 1, Min = 8, Max = 512)]
		public int IconSize { get; set; } = 60;
		[DebugEditable(DisplayName = "Column Gap", Step = 1, Min = 0, Max = 128)]
		public int ColGap { get; set; } = 8;
		[DebugEditable(DisplayName = "Row Gap", Step = 1, Min = 0, Max = 128)]
		public int RowGap { get; set; } = 12;
		[DebugEditable(DisplayName = "Background Corner Radius", Step = 1, Min = 0, Max = 64)]
		public int BgCornerRadius { get; set; } = 23;
		[DebugEditable(DisplayName = "Background Border Thickness", Step = 1, Min = 0, Max = 16)]
		public int BgBorderThickness { get; set; } = 0;
		[DebugEditable(DisplayName = "Background Padding", Step = 1, Min = 0, Max = 64)]
		public int BgPadding { get; set; } = 28;
		[DebugEditable(DisplayName = "Icon Padding X", Step = 1, Min = 0, Max = 256)]
		public int IconPaddingX { get; set; } = 8;
		[DebugEditable(DisplayName = "Icon Padding Y", Step = 1, Min = 0, Max = 256)]
		public int IconPaddingY { get; set; } = 8;
		[DebugEditable(DisplayName = "Block Text Scale", Step = 0.05f, Min = 0.2f, Max = 3f)]
		public float BlockTextScale { get; set; } = 0.1875f;
		[DebugEditable(DisplayName = "Shield Icon Height", Step = 1, Min = 8, Max = 128)]
		public int ShieldIconHeight { get; set; } = 36;
		[DebugEditable(DisplayName = "Shield Gap", Step = 1, Min = 0, Max = 64)]
		public int ShieldGap { get; set; } = 2;
		[DebugEditable(DisplayName = "Background Opacity", Step = 0.05f, Min = 0f, Max = 1f)]
		public float BgOpacity { get; set; } = 0.75f;
		[DebugEditable(DisplayName = "Shield Offset X", Step = 1, Min = -200, Max = 200)]
		public int ShieldOffsetX { get; set; } = 0;
		[DebugEditable(DisplayName = "Shield Offset Y", Step = 1, Min = -200, Max = 200)]
		public int ShieldOffsetY { get; set; } = -2;
		[DebugEditable(DisplayName = "Checkmark Size", Step = 1, Min = 6, Max = 256)]
		public int CheckmarkSize { get; set; } = 35;
		[DebugEditable(DisplayName = "Checkmark Offset X", Step = 1, Min = -200, Max = 200)]
		public int CheckmarkOffsetX { get; set; } = 9;
		[DebugEditable(DisplayName = "Checkmark Offset Y", Step = 1, Min = -200, Max = 200)]
		public int CheckmarkOffsetY { get; set; } = -9;
		[DebugEditable(DisplayName = "Usage Text Scale", Step = 0.05f, Min = 0.3f, Max = 3f)]
		public float UsageTextScale { get; set; } = 0.1375f;
		[DebugEditable(DisplayName = "Usage Offset X", Step = 1, Min = -200, Max = 200)]
		public int UsageOffsetX { get; set; } = -6;
		[DebugEditable(DisplayName = "Usage Offset Y", Step = 1, Min = -200, Max = 200)]
		public int UsageOffsetY { get; set; } = -6;

		public EquipmentDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, ContentManager content)
			: base(entityManager)
		{
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
			_content = content;
			EventManager.Subscribe<EquipmentAbilityTriggered>(OnEquipmentAbilityTriggered);
			EventManager.Subscribe<DeleteCachesEvent>(OnDeleteCachesEvent);
		}

		private void OnEquipmentAbilityTriggered(EquipmentAbilityTriggered evt)
		{
			// Trigger a brief pulse when any equipment ability triggers
			_pulseTimeRemaining = PulseDurationSeconds;
		}

		private void OnDeleteCachesEvent(DeleteCachesEvent evt)
		{
			_tooltipByEquipEntityId.Clear();
			_iconCache.Clear();
			_roundedRectCache.Clear();
		}

		private float GetCurrentPulseScale()
		{
			if (_pulseTimeRemaining <= 0.0) return 1f;
			float t = (float)(1.0 - (_pulseTimeRemaining / PulseDurationSeconds));
			float wave = (float)Math.Sin(t * Math.PI);
			return 1f + PulseAmplitude * wave;
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<Player>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			// No-op: layout will be applied during Draw by setting BasePosition per item
		}

		public override void Update(GameTime gameTime)
		{
			_lastDt = gameTime.ElapsedGameTime.TotalSeconds;
			base.Update(gameTime);
		}

		public void Draw()
		{
			// Update pulse timer
			if (_pulseTimeRemaining > 0.0)
			{
				_pulseTimeRemaining = Math.Max(0.0, _lastDt > 0 ? _pulseTimeRemaining - _lastDt : _pulseTimeRemaining);
			}
			var player = GetRelevantEntities().FirstOrDefault();
			if (player == null) return;

			// Gather equipment for this player
			var allEquipmentForPlayer = EntityManager.GetEntitiesWithComponent<EquippedEquipment>()
				.Where(e => e.GetComponent<EquippedEquipment>().EquippedOwner == player)
				.Select(e => e.GetComponent<EquippedEquipment>())
				.ToList();
			var equipment = allEquipmentForPlayer
				.Where(eq => (eq.Owner.GetComponent<EquipmentZone>()?.Zone ?? EquipmentZoneType.Default) == EquipmentZoneType.Default)
				.ToList();

			if (equipment.Count == 0) return;

			// Group and order types
			string[] order = new[] { "Head", "Chest", "Arms", "Legs" };
			int baseX = LeftMargin;
			int y = TopMargin;
			int rowHeight = (IconSize + BgPadding * 2) + RowGap;
			foreach (var type in order)
			{
				bool hasAnyOfType = allEquipmentForPlayer.Any(eq => string.Equals(eq.EquipmentType, type, StringComparison.OrdinalIgnoreCase));
				if (!hasAnyOfType) { continue; }
				var items = equipment.Where(eq => string.Equals(eq.EquipmentType, type, StringComparison.OrdinalIgnoreCase)).ToList();
				// Draw items in a row if any are visible
				if (items.Count > 0)
				{
					int x = baseX;
					foreach (var item in items)
					{
					// Resolve equipment definition for visuals and tooltip
					int bgW = IconSize + BgPadding * 2;
					int bgH = IconSize + BgPadding * 2;
					// Write layout to BasePosition; ParallaxLayerSystem will produce Position
					var tEquip = item.Owner.GetComponent<Transform>();
					if (tEquip != null)
					{
						tEquip.BasePosition = new Vector2(x, y);
						tEquip.ZOrder = 10001;
					}
					// Build bg rect from current Position (after parallax)
					var curPos = tEquip != null ? tEquip.Position : new Vector2(x, y);
					var bgRect = new Rectangle((int)System.Math.Round(curPos.X), (int)System.Math.Round(curPos.Y), bgW, bgH);
					// Persist panel center for return animations
					var zoneState = item.Owner.GetComponent<EquipmentZone>();
					if (zoneState == null)
					{
						zoneState = new EquipmentZone();
						EntityManager.AddComponent(item.Owner, zoneState);
					}
					zoneState.LastPanelCenter = new Vector2(bgRect.X + bgRect.Width * 0.5f, bgRect.Y + bgRect.Height * 0.5f);
					var fillColor = ResolveFillColor(item);
					bool disabledNow = IsDisabledForBlock(item);
					bool destroyedNow = IsDestroyed(item);
					// Update tooltip and hover state before publishing highlight so hover is accurate
					UpdateTooltip(item, bgRect);
					UpdateClickable(item, bgRect);
					// Publish highlight event on hover unless disabled; during Player Action phase only if item has Activate ability
					var uiEquip = item.Owner.GetComponent<UIElement>();
					var phaseNow = EntityManager.GetEntitiesWithComponent<PhaseState>().FirstOrDefault()?.GetComponent<PhaseState>();
					bool isPlayerAction = phaseNow != null && phaseNow.Main == MainPhase.PlayerTurn && phaseNow.Sub == SubPhase.Action;
					// Now draw background and contents, with optional pulse
					float pulseScale = GetCurrentPulseScale();
					if (pulseScale != 1f)
					{
						int scaledW = (int)Math.Round(bgRect.Width * pulseScale);
						int scaledH = (int)Math.Round(bgRect.Height * pulseScale);
						var center = new Vector2(bgRect.X + bgRect.Width / 2f, bgRect.Y + bgRect.Height / 2f);
						var scaled = new Rectangle((int)(center.X - scaledW / 2f), (int)(center.Y - scaledH / 2f), scaledW, scaledH);
						DrawRoundedBackground(scaled, disabledNow ? DisabledFill(fillColor) : fillColor);
						bgRect = scaled;
					}
					else
					{
						DrawRoundedBackground(bgRect, disabledNow ? DisabledFill(fillColor) : fillColor);
					}

					// Draw icon within padding, preserving aspect ratio
					var tex = GetOrLoadIcon(type);
					if (tex != null)
					{
						int targetH = IconSize;
						int targetW = IconSize;
						if (tex.Width > 0 && tex.Height > 0)
						{
							float aspect = tex.Width / (float)tex.Height;
							if (aspect >= 1f)
							{
								targetW = IconSize;
								targetH = (int)Math.Round(IconSize / aspect);
							}
							else
							{
								targetH = IconSize;
								targetW = (int)Math.Round(IconSize * aspect);
							}
						}
						int innerX = bgRect.X + IconPaddingX;
						int innerY = bgRect.Y + IconPaddingY;
						// Center the icon within the padded area if one dimension is smaller due to aspect fit
						int padBoxW = IconSize;
						int padBoxH = IconSize;
						int drawX = innerX + (padBoxW - targetW) / 2;
						int drawY = innerY + (padBoxH - targetH) / 2;
						var iconRect = new Rectangle(drawX, drawY, targetW, targetH);
						_spriteBatch.Draw(tex, iconRect, disabledNow ? Color.Gray : Color.White);
					}

					// Draw block value and shield icon at bottom-left
					DrawBlockAndShield(item, bgRect, fillColor);
					// Draw once-per-battle checkmark if any ability has oncePerBattle=true
					DrawOncePerBattleCheck(item, bgRect);
					// Draw usage {remaining}/{total}
					DrawUsageCounter(item, bgRect, fillColor);
					// Overlay yellow X if destroyed
					if (destroyedNow)
					{
						DrawDestroyedX(bgRect);
					}
						x += bgW + ColGap;
					}
				}
				y += rowHeight; // reserve row space for this equipment type to prevent vertical shifting
			}
		}

		private bool IsDisabledForBlock(EquippedEquipment item)
		{
			// Disabled if out of uses during enemy turn
			var phase = EntityManager.GetEntitiesWithComponent<PhaseState>().FirstOrDefault()?.GetComponent<PhaseState>();
			bool isEnemyTurnBlock = phase != null && phase.Main == MainPhase.EnemyTurn && phase.Sub == SubPhase.Block;
			int total = 0; int used = 0;
			try
			{
				if (EquipmentDefinitionCache.TryGet(item.EquipmentId, out var def) && def != null) total = Math.Max(0, def.blockUses);
				var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
				var state = player?.GetComponent<EquipmentUsedState>();
				if (state != null && !string.IsNullOrEmpty(item.EquipmentId) && state.UsesByEquipmentId.TryGetValue(item.EquipmentId, out var v)) used = v;
			}
			catch { }
			return isEnemyTurnBlock && total > 0 && used >= total;
		}

		private Color DisabledFill(Color baseFill)
		{
			// Dim the fill color to indicate disabled
			return new Color((byte)(baseFill.R * 0.4f), (byte)(baseFill.G * 0.4f), (byte)(baseFill.B * 0.4f), (byte)(MathHelper.Clamp(BgOpacity * 255f, 0f, 255f)));
		}

		private void UpdateClickable(EquippedEquipment item, Rectangle rect)
		{
			var ui = item.Owner.GetComponent<UIElement>();
			ui.Bounds = rect;
			// Disable interaction when out of uses during enemy turn
			if (IsDisabledForBlock(item))
			{
				ui.IsInteractable = false;
				ui.IsHovered = false;
			}
			ui.Tooltip = BuildTooltipText(item);
			ui.TooltipPosition = TooltipPosition.Right;
			// Panel (default zone) items should use direct click handling, not delegate routing
			ui.EventType = UIElementEventType.None;
					// Transform.Position already reflects parallax; ZOrder handled above
		}

		private void DrawOncePerBattleCheck(EquippedEquipment item, Rectangle bgRect)
		{
			try
			{
				if (!EquipmentDefinitionCache.TryGet(item.EquipmentId, out var def) || def == null || def.abilities == null)
				{
					return;
				}
				// Only show if a once-per-battle ability for THIS equipment has already triggered this battle
				var owner = item.EquippedOwner ?? EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
				var st = owner?.GetComponent<BattleStateInfo>();
				bool shouldShow = false;
				if (st != null && st.EquipmentTriggeredThisBattle != null)
				{
					foreach (var a in def.abilities)
					{
						if (a.oncePerBattle && !string.IsNullOrWhiteSpace(a.id) && st.EquipmentTriggeredThisBattle.Contains(a.id))
						{
							shouldShow = true;
							break;
						}
					}
				}
				if (!shouldShow) return;
				int size = Math.Max(6, CheckmarkSize);
				float boxX = bgRect.Right - size + CheckmarkOffsetX;
				float boxY = bgRect.Top + CheckmarkOffsetY;
				var p1 = new Vector2(boxX + size * 0.15f, boxY + size * 0.55f);
				var p2 = new Vector2(boxX + size * 0.40f, boxY + size * 0.82f);
				var p3 = new Vector2(boxX + size * 0.90f, boxY + size * 0.20f);
				float thickness = Math.Max(2f, size * 0.14f);
				DrawCheckLine(p1, p2, thickness, Color.Black);
				DrawCheckLine(p2, p3, thickness, 	Color.Black);
			}
			catch { }
		}

		private void DrawUsageCounter(EquippedEquipment item, Rectangle bgRect, Color fillColor)
		{
			if (_font == null) return;
			try
			{
				int total = 0;
				if (EquipmentDefinitionCache.TryGet(item.EquipmentId, out var def) && def != null)
				{
					total = Math.Max(0, def.blockUses);
				}
				var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
				int used = 0;
				if (player != null)
				{
					var state = player.GetComponent<EquipmentUsedState>();
					if (state != null && !string.IsNullOrEmpty(item.EquipmentId) && state.UsesByEquipmentId.TryGetValue(item.EquipmentId, out var val)) used = val;
				}
				int remaining = Math.Max(0, total - used);
				string text = $"{remaining}/{total}";
				float scale = UsageTextScale;
				var size = _font.MeasureString(text) * scale;
				int x = bgRect.Right - (int)Math.Round(size.X) + UsageOffsetX;
				int y = bgRect.Bottom - (int)Math.Round(size.Y) + UsageOffsetY;
				// Match block text visibility: black on white fill, white otherwise
				var textColor = (fillColor == Color.White) ? Color.Black : Color.White;
				_spriteBatch.DrawString(_font, text, new Vector2(x, y), textColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
			}
			catch { }
		}

		private void DrawCheckLine(Microsoft.Xna.Framework.Vector2 start, Microsoft.Xna.Framework.Vector2 end, float thickness, Microsoft.Xna.Framework.Color color)
		{
			if (!_iconCache.TryGetValue("_px1", out var px) || px == null)
			{
				px = new Texture2D(_graphicsDevice, 1, 1);
				px.SetData(new[] { Color.White });
				_iconCache["_px1"] = px;
			}
			Vector2 edge = end - start;
			float len = edge.Length();
			if (len <= 0.0001f) return;
			float ang = (float)Math.Atan2(edge.Y, edge.X);
			_spriteBatch.Draw(px, position: start, sourceRectangle: null, color: color, rotation: ang, origin: new Microsoft.Xna.Framework.Vector2(0f, 0.5f), scale: new Microsoft.Xna.Framework.Vector2(len, thickness), effects: SpriteEffects.None, layerDepth: 0f);
		}

		private bool IsDestroyed(EquippedEquipment item)
		{
			try
			{
				var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
				var state = player?.GetComponent<EquipmentUsedState>();
				if (state == null || string.IsNullOrEmpty(item.EquipmentId)) return false;
				return state.DestroyedEquipmentIds.Contains(item.EquipmentId);
			}
			catch { return false; }
		}

		private void DrawDestroyedX(Rectangle rect)
		{
			// Reuse 1x1 pixel from icon cache
			if (!_iconCache.TryGetValue("_px1", out var px) || px == null)
			{
				px = new Texture2D(_graphicsDevice, 1, 1);
				px.SetData(new[] { Color.White });
				_iconCache["_px1"] = px;
			}
			var start1 = new Vector2(rect.Left + 6, rect.Top + 6);
			var end1 = new Vector2(rect.Right - 6, rect.Bottom - 6);
			var start2 = new Vector2(rect.Right - 6, rect.Top + 6);
			var end2 = new Vector2(rect.Left + 6, rect.Bottom - 6);
			var color = new Color(255, 215, 0); // golden yellow
			DrawCheckLine(start1, end1, 6f, color);
			DrawCheckLine(start2, end2, 6f, color);
		}

		private void DrawRoundedBackground(Rectangle rect, Color fill)
		{
			int w = rect.Width;
			int h = rect.Height;
			int rOuter = Math.Max(0, BgCornerRadius);
			int rInner = Math.Max(0, BgCornerRadius - BgBorderThickness);
			var outer = GetRoundedRectTexture(w, h, rOuter);
			var inner = GetRoundedRectTexture(Math.Max(1, w - BgBorderThickness * 2), Math.Max(1, h - BgBorderThickness * 2), rInner);
			var center = new Vector2(rect.X + w / 2f, rect.Y + h / 2f);
			if (BgBorderThickness > 0)
			{
				_spriteBatch.Draw(outer, center, null, Color.Black, 0f, new Vector2(outer.Width / 2f, outer.Height / 2f), 1f, SpriteEffects.None, 0f);
			}
			byte a = (byte)(MathHelper.Clamp(BgOpacity, 0f, 1f) * 255f);
			var fillWithAlpha = new Color(fill.R, fill.G, fill.B, a);
			_spriteBatch.Draw(inner, center, null, fillWithAlpha, 0f, new Vector2(inner.Width / 2f, inner.Height / 2f), 1f, SpriteEffects.None, 0f);
		}

		private Texture2D GetRoundedRectTexture(int width, int height, int radius)
		{
			var key = (width, height, radius);
			if (_roundedRectCache.TryGetValue(key, out var tex) && tex != null) return tex;
			var texture = RoundedRectTextureFactory.CreateRoundedRect(_graphicsDevice, width, height, radius);
			_roundedRectCache[key] = texture;
			return texture;
		}

		private void DrawBlockAndShield(EquippedEquipment item, Rectangle bgRect, Color fillColor)
		{
			if (_font == null) return;
			int block = 0;
			try
			{
				if (EquipmentDefinitionCache.TryGet(item.EquipmentId, out var def))
				{
					if (def != null)
					{
						block = Math.Max(0, def.block);
					}
				}
			}
			catch { }
			if (block <= 0) return;

			var textColor = GetTextColorForFill(fillColor);
			string blockText = block.ToString();
			var textSize = _font.MeasureString(blockText) * BlockTextScale;
			float marginX = 8f;
			float marginY = 8f;
			float baselineY = bgRect.Y + bgRect.Height - marginY;
			float numberX = bgRect.X + marginX;
			float numberY = baselineY - textSize.Y;
			_spriteBatch.DrawString(_font, blockText, new Vector2(numberX, numberY), textColor, 0f, Vector2.Zero, BlockTextScale, SpriteEffects.None, 0f);

			var shield = SafeLoadTexture("shield");
			if (shield != null)
			{
				float iconH = Math.Max(8, ShieldIconHeight);
				float iconW = shield.Height > 0 ? iconH * (shield.Width / (float)shield.Height) : iconH;
				float gap = Math.Max(0, ShieldGap);
				float iconX = numberX + textSize.X + gap + ShieldOffsetX;
				float iconY = baselineY - iconH + ShieldOffsetY;
				_spriteBatch.Draw(shield, new Rectangle((int)iconX, (int)iconY, (int)iconW, (int)iconH), Color.White);
			}
		}

		private void UpdateTooltip(EquippedEquipment item, Rectangle rect)
		{
			var uiEntity = item.Owner;
			var tr = uiEntity.GetComponent<Transform>();
			// Do not overwrite Transform.Position here; it's owned by Parallax/motion
			var ui = uiEntity.GetComponent<UIElement>();
			if (ui != null)
			{
				ui.Bounds = rect;
				ui.Tooltip = BuildTooltipText(item);
				ui.TooltipPosition = TooltipPosition.Right;
			}
		}

		private string BuildTooltipText(EquippedEquipment item)
		{
			return EquipmentService.GetTooltipText(item.EquipmentId, EquipmentTooltipType.Battle);
		}

		private Texture2D SafeLoadTexture(string asset)
		{
			try { return _content.Load<Texture2D>(asset); } catch { return null; }
		}

		private Color ResolveFillColor(EquippedEquipment item)
		{
			try
			{
				if (EquipmentDefinitionCache.TryGet(item.EquipmentId, out var def) && def != null)
				{
					string c = def.color?.Trim()?.ToLowerInvariant();
					switch (c)
					{
						case "red": return Color.DarkRed;
						case "white": return Color.White;
						case "black": return Color.Black;
						default: return Color.Gray;
					}
				}
			}
			catch { }
			return Color.Gray;
		}

		private Color GetTextColorForFill(Color fill)
		{
			// Match card logic: black text on white fill, white otherwise
			if (fill == Color.White) return Color.Black;
			return Color.White;
		}

		private Texture2D GetOrLoadIcon(string type)
		{
			string key = type.ToLowerInvariant();
			if (_iconCache.TryGetValue(key, out var t) && t != null) return t;
			string assetName = key; // assumes head.png, chest.png, arms.png, legs.png in Content root
			try
			{
				var tex = _content.Load<Texture2D>(assetName);
				_iconCache[key] = tex;
				return tex;
			}
			catch
			{
                Console.WriteLine($"[EquipmentDisplaySystem] Missing icon for type '{type}' (expected content asset '{assetName}')");
				_iconCache[key] = null;
				return null;
			}
		}


	}
}


