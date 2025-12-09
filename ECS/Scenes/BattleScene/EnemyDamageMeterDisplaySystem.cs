using System;
using System.Linq;
using System.Collections.Generic;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Rendering;
using Crusaders30XX.ECS.Singletons;
using Crusaders30XX.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Displays enemy attack damage breakdown as separate trapezoid segments inside the top of the
	/// enemy attack banner. Segments show: damage (red), aegis (white), block (black), condition (green).
	/// Ordered left-to-right: damage, aegis, block, condition.
	/// </summary>
	[DebugTab("Enemy Damage Meter")]
	public class EnemyDamageMeterDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly SpriteFont _font = FontSingleton.ContentFont;
		private readonly Texture2D _pixel;

		// Tooltip UIElement entities per segment type
		private readonly Dictionary<string, Entity> _segmentUiEntities = new();

		// Animation state: track previous values and elapsed time for pop animations
		private struct AnimState
		{
			public int PrevValue;
			public float PopElapsed;
			public bool IsPopping;
		}
		private AnimState _damageAnim;
		private AnimState _aegisAnim;
		private AnimState _blockAnim;
		private AnimState _conditionAnim;

		// Segment types for iteration
		private enum SegmentType { Damage, Aegis, Block, Condition }

		#region Debug-Editable Fields

		[DebugEditable(DisplayName = "Total Meter Width", Step = 5, Min = 50, Max = 400)]
		public int TotalMeterWidth { get; set; } = 160;

		[DebugEditable(DisplayName = "Min Segment Width", Step = 2, Min = 10, Max = 100)]
		public int MinSegmentWidth { get; set; } = 24;

		[DebugEditable(DisplayName = "Segment Height", Step = 2, Min = 10, Max = 100)]
		public int SegmentHeight { get; set; } = 24;

		[DebugEditable(DisplayName = "Segment Gap", Step = 1, Min = 0, Max = 20)]
		public int SegmentGap { get; set; } = 4;

		[DebugEditable(DisplayName = "Top Edge Angle (deg)", Step = 1f, Min = -45f, Max = 45f)]
		public float TopEdgeAngle { get; set; } = 0f;

		[DebugEditable(DisplayName = "Right Edge Angle (deg)", Step = 1f, Min = -45f, Max = 45f)]
		public float RightEdgeAngle { get; set; } = 0f;

		[DebugEditable(DisplayName = "Bottom Edge Angle (deg)", Step = 1f, Min = -45f, Max = 45f)]
		public float BottomEdgeAngle { get; set; } = 0f;

		[DebugEditable(DisplayName = "Left Edge Angle (deg)", Step = 1f, Min = -45f, Max = 45f)]
		public float LeftEdgeAngle { get; set; } = 0f;

		[DebugEditable(DisplayName = "Outer Slant Only", Step = 1, Min = 0, Max = 1)]
		public int OuterSlantOnly { get; set; } = 1;

		[DebugEditable(DisplayName = "Outer Slant Angle (deg)", Step = 1f, Min = 0f, Max = 45f)]
		public float OuterSlantAngle { get; set; } = 8f;

		[DebugEditable(DisplayName = "Offset Y from Banner Top", Step = 2, Min = -100, Max = 200)]
		public int OffsetYFromBannerTop { get; set; } = 10;

		[DebugEditable(DisplayName = "Font Scale", Step = 0.02f, Min = 0.05f, Max = 1f)]
		public float FontScale { get; set; } = 0.14f;

		[DebugEditable(DisplayName = "Pop Duration (s)", Step = 0.02f, Min = 0.05f, Max = 1f)]
		public float PopDurationSeconds { get; set; } = 0.2f;

		[DebugEditable(DisplayName = "Pop Scale Max", Step = 0.05f, Min = 1f, Max = 2f)]
		public float PopScaleMax { get; set; } = 1.25f;

		[DebugEditable(DisplayName = "Outline Thickness", Step = 1, Min = 0, Max = 6)]
		public int OutlineThickness { get; set; } = 2;

		[DebugEditable(DisplayName = "Damage Color R", Step = 5, Min = 0, Max = 255)]
		public int DamageColorR { get; set; } = 180;

		[DebugEditable(DisplayName = "Damage Color G", Step = 5, Min = 0, Max = 255)]
		public int DamageColorG { get; set; } = 40;

		[DebugEditable(DisplayName = "Damage Color B", Step = 5, Min = 0, Max = 255)]
		public int DamageColorB { get; set; } = 40;

		[DebugEditable(DisplayName = "Aegis Color R", Step = 5, Min = 0, Max = 255)]
		public int AegisColorR { get; set; } = 255;

		[DebugEditable(DisplayName = "Aegis Color G", Step = 5, Min = 0, Max = 255)]
		public int AegisColorG { get; set; } = 255;

		[DebugEditable(DisplayName = "Aegis Color B", Step = 5, Min = 0, Max = 255)]
		public int AegisColorB { get; set; } = 255;

		[DebugEditable(DisplayName = "Block Color R", Step = 5, Min = 0, Max = 255)]
		public int BlockColorR { get; set; } = 40;

		[DebugEditable(DisplayName = "Block Color G", Step = 5, Min = 0, Max = 255)]
		public int BlockColorG { get; set; } = 40;

		[DebugEditable(DisplayName = "Block Color B", Step = 5, Min = 0, Max = 255)]
		public int BlockColorB { get; set; } = 40;

		[DebugEditable(DisplayName = "Condition Color R", Step = 5, Min = 0, Max = 255)]
		public int ConditionColorR { get; set; } = 50;

		[DebugEditable(DisplayName = "Condition Color G", Step = 5, Min = 0, Max = 255)]
		public int ConditionColorG { get; set; } = 180;

		[DebugEditable(DisplayName = "Condition Color B", Step = 5, Min = 0, Max = 255)]
		public int ConditionColorB { get; set; } = 50;

		#endregion

		public EnemyDamageMeterDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
			: base(entityManager)
		{
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
			_pixel = new Texture2D(graphicsDevice, 1, 1);
			_pixel.SetData(new[] { Color.White });
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<EnemyAttackProgress>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);
			if (!IsActive) return;

			float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

			// Get current values from progress
			var progress = GetCurrentProgress();
			if (progress == null)
			{
				// Reset anim states when no progress
				_damageAnim = default;
				_aegisAnim = default;
				_blockAnim = default;
				_conditionAnim = default;
				return;
			}

			// Check for value changes and trigger pop animations
			UpdateAnimState(ref _damageAnim, progress.ActualDamage, dt);
			UpdateAnimState(ref _aegisAnim, progress.AegisTotal, dt);
			UpdateAnimState(ref _blockAnim, progress.AssignedBlockTotal, dt);
			UpdateAnimState(ref _conditionAnim, progress.PreventedDamageFromBlockCondition, dt);
		}

		private void UpdateAnimState(ref AnimState state, int currentValue, float dt)
		{
			if (currentValue != state.PrevValue)
			{
				// Value changed - start pop animation
				state.PrevValue = currentValue;
				state.PopElapsed = 0f;
				state.IsPopping = true;
			}
			else if (state.IsPopping)
			{
				// Continue pop animation
				state.PopElapsed += dt;
				if (state.PopElapsed >= PopDurationSeconds)
				{
					state.IsPopping = false;
					state.PopElapsed = PopDurationSeconds;
				}
			}
		}

		private float GetPopScale(AnimState state)
		{
			if (!state.IsPopping) return 1f;
			float t = MathHelper.Clamp(state.PopElapsed / Math.Max(0.001f, PopDurationSeconds), 0f, 1f);
			// Ease out elastic-like: pop up then settle back to 1
			float easeOut = 1f - (float)Math.Pow(1f - t, 3);
			float scale = MathHelper.Lerp(PopScaleMax, 1f, easeOut);
			return scale;
		}

		public void Draw()
		{
			// Only render during Block / EnemyAttack phases
			var phaseEntity = EntityManager.GetEntitiesWithComponent<PhaseState>().FirstOrDefault();
			if (phaseEntity == null) return;
			var phase = phaseEntity.GetComponent<PhaseState>();
			if (phase == null || (phase.Sub != SubPhase.Block && phase.Sub != SubPhase.EnemyAttack)) return;

			// Get banner anchor bounds
			var anchorEntity = EntityManager.GetEntitiesWithComponent<EnemyAttackBannerAnchor>().FirstOrDefault();
			if (anchorEntity == null) return;
			var anchorUi = anchorEntity.GetComponent<UIElement>();
			if (anchorUi == null || anchorUi.Bounds.Width < 1 || anchorUi.Bounds.Height < 1) return;

			var bannerBounds = anchorUi.Bounds;

			// Get current progress
			var progress = GetCurrentProgress();
			if (progress == null)
			{
				CleanupTooltips(new HashSet<string>());
				return;
			}

			// Build segment data: (type, value, color, tooltipLabel)
			var segments = new List<(SegmentType type, int value, Color color, string label, AnimState anim)>();
			
			int damageVal = Math.Max(0, progress.ActualDamage);
			int aegisVal = Math.Max(0, progress.AegisTotal);
			int blockVal = Math.Max(0, progress.AssignedBlockTotal);
			int conditionVal = Math.Max(0, progress.PreventedDamageFromBlockCondition);

			// Only include segments with non-zero values
			if (damageVal > 0)
				segments.Add((SegmentType.Damage, damageVal, new Color(DamageColorR, DamageColorG, DamageColorB), "Damage", _damageAnim));
			if (aegisVal > 0)
				segments.Add((SegmentType.Aegis, aegisVal, new Color(AegisColorR, AegisColorG, AegisColorB), "Aegis", _aegisAnim));
			if (blockVal > 0)
				segments.Add((SegmentType.Block, blockVal, new Color(BlockColorR, BlockColorG, BlockColorB), "Block", _blockAnim));
			if (conditionVal > 0)
				segments.Add((SegmentType.Condition, conditionVal, new Color(ConditionColorR, ConditionColorG, ConditionColorB), "Condition", _conditionAnim));

			if (segments.Count == 0)
			{
				CleanupTooltips(new HashSet<string>());
				return;
			}

			// Calculate proportional widths based on values
			int totalValue = 0;
			foreach (var seg in segments)
				totalValue += seg.value;

			// Calculate each segment's proportional width
			var segmentWidths = new List<int>();
			int availableWidth = TotalMeterWidth - (segments.Count - 1) * SegmentGap;
			int usedWidth = 0;
			for (int i = 0; i < segments.Count; i++)
			{
				float proportion = (float)segments[i].value / Math.Max(1, totalValue);
				int segW = Math.Max(MinSegmentWidth, (int)Math.Round(availableWidth * proportion));
				// Last segment gets remaining width to avoid rounding errors
				if (i == segments.Count - 1)
					segW = availableWidth - usedWidth;
				segmentWidths.Add(segW);
				usedWidth += segW;
			}

			// Calculate total width and center position
			int totalWidth = usedWidth + (segments.Count - 1) * SegmentGap;
			int startX = bannerBounds.Center.X - totalWidth / 2;
			int startY = bannerBounds.Top + OffsetYFromBannerTop;

			var presentKeys = new HashSet<string>();
			int currentX = startX;

			// Draw each segment as simple rectangles
			for (int i = 0; i < segments.Count; i++)
			{
				var (type, value, color, label, anim) = segments[i];
				int baseWidth = segmentWidths[i];
				
				float popScale = GetPopScale(anim);
				
				// Calculate scaled dimensions centered on the segment slot
				int scaledW = (int)Math.Round(baseWidth * popScale);
				int scaledH = (int)Math.Round(SegmentHeight * popScale);
				int drawX = currentX + (baseWidth - scaledW) / 2;
				int drawY = startY + (SegmentHeight - scaledH) / 2;

				// Draw outline (black border)
				if (OutlineThickness > 0)
				{
					var outlineRect = new Rectangle(drawX - OutlineThickness, drawY - OutlineThickness, 
						scaledW + OutlineThickness * 2, scaledH + OutlineThickness * 2);
					_spriteBatch.Draw(_pixel, outlineRect, Color.Black);
				}

				// Draw filled rectangle with segment color
				var fillRect = new Rectangle(drawX, drawY, scaledW, scaledH);
				_spriteBatch.Draw(_pixel, fillRect, color);

				// Draw number centered
				if (_font != null && value > 0)
				{
					string numText = value.ToString();
					var textSize = _font.MeasureString(numText) * FontScale;
					// Determine text color: use contrasting color based on segment
					Color textColor = (type == SegmentType.Block) ? Color.White : Color.Black;
					if (type == SegmentType.Damage) textColor = Color.White;
					
					var textPos = new Vector2(
						drawX + scaledW / 2f - textSize.X / 2f,
						drawY + scaledH / 2f - textSize.Y / 2f
					);
					_spriteBatch.DrawString(_font, numText, textPos, textColor, 0f, Vector2.Zero, FontScale, SpriteEffects.None, 0f);
				}

				// Update or create tooltip UIElement
				string key = $"DamageMeter_{type}";
				presentKeys.Add(key);
				var segmentRect = new Rectangle(currentX, startY, baseWidth, SegmentHeight);
				UpdateSegmentTooltipUi(key, segmentRect, $"{label}: {value}");

				// Advance X position for next segment
				currentX += baseWidth + SegmentGap;
			}

			// Cleanup any tooltips for segments no longer present
			CleanupTooltips(presentKeys);
		}

		private EnemyAttackProgress GetCurrentProgress()
		{
			// Get the first planned attack's context from the enemy
			var enemy = EntityManager.GetEntitiesWithComponent<AttackIntent>().FirstOrDefault();
			if (enemy == null) return null;
			var intent = enemy.GetComponent<AttackIntent>();
			if (intent == null || intent.Planned == null || intent.Planned.Count == 0) return null;
			
			var contextId = intent.Planned[0].ContextId;
			if (string.IsNullOrEmpty(contextId)) return null;

			foreach (var e in EntityManager.GetEntitiesWithComponent<EnemyAttackProgress>())
			{
				var p = e.GetComponent<EnemyAttackProgress>();
				if (p != null && p.ContextId == contextId)
					return p;
			}
			return null;
		}

		private void UpdateSegmentTooltipUi(string key, Rectangle rect, string tooltipText)
		{
			if (!_segmentUiEntities.TryGetValue(key, out var uiEntity) || uiEntity == null)
			{
				uiEntity = EntityManager.CreateEntity($"UI_DamageMeter_{key}");
				EntityManager.AddComponent(uiEntity, new Transform
				{
					BasePosition = new Vector2(rect.X, rect.Y),
					Position = new Vector2(rect.X, rect.Y),
					ZOrder = 10000
				});
				EntityManager.AddComponent(uiEntity, new UIElement
				{
					Bounds = rect,
					IsInteractable = true,
					Tooltip = tooltipText,
					TooltipPosition = TooltipPosition.Below,
					TooltipOffsetPx = 8
				});
				EntityManager.AddComponent(uiEntity, ParallaxLayer.GetUIParallaxLayer());
				_segmentUiEntities[key] = uiEntity;
			}
			else
			{
				var tr = uiEntity.GetComponent<Transform>();
				if (tr != null)
				{
					tr.BasePosition = new Vector2(rect.X, rect.Y);
					tr.Position = new Vector2(rect.X, rect.Y);
					tr.ZOrder = 10000;
				}
				var ui = uiEntity.GetComponent<UIElement>();
				if (ui != null)
				{
					ui.Bounds = rect;
					ui.Tooltip = tooltipText;
					ui.TooltipPosition = TooltipPosition.Below;
					ui.TooltipOffsetPx = 8;
					ui.IsInteractable = true;
				}
			}
		}

		private void CleanupTooltips(HashSet<string> presentKeys)
		{
			var toRemove = new List<string>();
			foreach (var kvp in _segmentUiEntities)
			{
				if (!presentKeys.Contains(kvp.Key))
				{
					if (kvp.Value != null)
					{
						EntityManager.DestroyEntity(kvp.Value.Id);
					}
					toRemove.Add(kvp.Key);
				}
			}
			foreach (var k in toRemove)
			{
				_segmentUiEntities.Remove(k);
			}
		}
	}
}

