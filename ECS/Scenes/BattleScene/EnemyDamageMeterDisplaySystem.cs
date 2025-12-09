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
	/// Displays enemy attack damage breakdown as parallelogram segments.
	/// Order: Damage (red, elevated) | Block (black) | Aegis (white) | Condition (green).
	/// When block/aegis/condition decreases, a chunk animates toward damage and merges.
	/// </summary>
	[DebugTab("Enemy Damage Meter")]
	public class EnemyDamageMeterDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly SpriteFont _font = FontSingleton.ContentFont;
		private readonly Texture2D _pixel;
		private readonly BasicEffect _basicEffect;

		// Tooltip UIElement entities per segment type
		private readonly Dictionary<string, Entity> _segmentUiEntities = new();

		// Segment types for iteration - ordered: Damage, Block, Aegis, Condition
		private enum SegmentType { Damage, Block, Aegis, Condition }

		// Floating chunk for animation when values decrease
		private struct FloatingChunk
		{
			public float StartX, StartY;      // Source position
			public float TargetX, TargetY;    // Destination (alongside damage)
			public float Progress;            // 0 to 1
			public Color StartColor;          // Source segment color
			public int Value;                 // Chunk value (typically 1)
			public int Width;                 // Chunk width
			public int Height;                // Chunk height
		}
		private readonly List<FloatingChunk> _floatingChunks = new();

		// Track previous values to detect decreases
		private int _prevBlockVal;
		private int _prevAegisVal;
		private int _prevConditionVal;
		private bool _initialized;

		// Cached segment positions for spawning chunks
		private Rectangle _damageRect;
		private Rectangle _blockRect;
		private Rectangle _aegisRect;
		private Rectangle _conditionRect;

		#region Debug-Editable Fields

		[DebugEditable(DisplayName = "Total Meter Width", Step = 5, Min = 50, Max = 400)]
		public int TotalMeterWidth { get; set; } = 200;

		[DebugEditable(DisplayName = "Min Segment Width", Step = 2, Min = 10, Max = 100)]
		public int MinSegmentWidth { get; set; } = 40;

		[DebugEditable(DisplayName = "Segment Height", Step = 2, Min = 10, Max = 100)]
		public int SegmentHeight { get; set; } = 36;

		[DebugEditable(DisplayName = "Segment Gap", Step = 1, Min = -20, Max = 20)]
		public int SegmentGap { get; set; } = -8;

		[DebugEditable(DisplayName = "Parallelogram Slant", Step = 2, Min = 0, Max = 40)]
		public int ParallelogramSlant { get; set; } = 12;

		[DebugEditable(DisplayName = "Damage Y Offset", Step = 2, Min = -50, Max = 50)]
		public int DamageYOffset { get; set; } = -12;

		[DebugEditable(DisplayName = "Offset Y from Banner Top", Step = 2, Min = -100, Max = 200)]
		public int OffsetYFromBannerTop { get; set; } = 24;

		[DebugEditable(DisplayName = "Font Scale", Step = 0.02f, Min = 0.05f, Max = 1f)]
		public float FontScale { get; set; } = 0.18f;

		[DebugEditable(DisplayName = "Chunk Anim Duration (s)", Step = 0.05f, Min = 0.1f, Max = 2f)]
		public float ChunkAnimDuration { get; set; } = 0.4f;

		[DebugEditable(DisplayName = "Chunk Separation Offset", Step = 2, Min = 0, Max = 40)]
		public int ChunkSeparationOffset { get; set; } = 8;

		[DebugEditable(DisplayName = "Chunk Width", Step = 2, Min = 10, Max = 60)]
		public int ChunkWidth { get; set; } = 30;

		[DebugEditable(DisplayName = "Damage Color R", Step = 5, Min = 0, Max = 255)]
		public int DamageColorR { get; set; } = 200;

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
		public int BlockColorR { get; set; } = 30;

		[DebugEditable(DisplayName = "Block Color G", Step = 5, Min = 0, Max = 255)]
		public int BlockColorG { get; set; } = 30;

		[DebugEditable(DisplayName = "Block Color B", Step = 5, Min = 0, Max = 255)]
		public int BlockColorB { get; set; } = 30;

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

			// Setup BasicEffect for drawing parallelograms
			_basicEffect = new BasicEffect(graphicsDevice)
			{
				VertexColorEnabled = true,
				TextureEnabled = false
			};
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
				// Reset state when no progress
				_initialized = false;
				_prevBlockVal = 0;
				_prevAegisVal = 0;
				_prevConditionVal = 0;
				_floatingChunks.Clear();
				return;
			}

			int blockVal = Math.Max(0, progress.AssignedBlockTotal);
			int aegisVal = Math.Max(0, progress.AegisTotal);
			int conditionVal = Math.Max(0, progress.PreventedDamageFromBlockCondition);

			// Only spawn chunks after initialization (so we don't spawn on first frame)
			if (_initialized)
			{
				// Detect decreases and spawn chunks
				SpawnChunksForDecrease(SegmentType.Block, _prevBlockVal, blockVal, new Color(BlockColorR, BlockColorG, BlockColorB));
				SpawnChunksForDecrease(SegmentType.Aegis, _prevAegisVal, aegisVal, new Color(AegisColorR, AegisColorG, AegisColorB));
				SpawnChunksForDecrease(SegmentType.Condition, _prevConditionVal, conditionVal, new Color(ConditionColorR, ConditionColorG, ConditionColorB));
			}

			// Update previous values
			_prevBlockVal = blockVal;
			_prevAegisVal = aegisVal;
			_prevConditionVal = conditionVal;
			_initialized = true;

			// Update floating chunk animations
			UpdateChunks(dt);
		}

		private void SpawnChunksForDecrease(SegmentType sourceType, int prevVal, int currentVal, Color sourceColor)
		{
			int delta = prevVal - currentVal;
			if (delta <= 0) return;

			// Get source rectangle based on type
			Rectangle sourceRect = sourceType switch
			{
				SegmentType.Block => _blockRect,
				SegmentType.Aegis => _aegisRect,
				SegmentType.Condition => _conditionRect,
				_ => Rectangle.Empty
			};

			if (sourceRect.Width < 1) return;

			// Spawn chunks for the delta (one chunk per point of decrease, or combine into one)
			float startX = sourceRect.Right - ChunkWidth - ChunkSeparationOffset;
			float startY = sourceRect.Y + ChunkSeparationOffset;

			// Target is to the right of damage segment
			float targetX = _damageRect.Right + SegmentGap;
			float targetY = _damageRect.Y;

			_floatingChunks.Add(new FloatingChunk
			{
				StartX = startX,
				StartY = startY,
				TargetX = targetX,
				TargetY = targetY,
				Progress = 0f,
				StartColor = sourceColor,
				Value = delta,
				Width = ChunkWidth,
				Height = SegmentHeight
			});
		}

		private void UpdateChunks(float dt)
		{
			for (int i = _floatingChunks.Count - 1; i >= 0; i--)
			{
				var chunk = _floatingChunks[i];
				chunk.Progress += dt / Math.Max(0.01f, ChunkAnimDuration);

				if (chunk.Progress >= 1f)
				{
					_floatingChunks.RemoveAt(i);
				}
				else
				{
					_floatingChunks[i] = chunk;
				}
			}
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

			// Get segment values - Order: Damage, Block, Aegis, Condition
			int damageVal = Math.Max(0, progress.ActualDamage);
			int blockVal = Math.Max(0, progress.AssignedBlockTotal);
			int aegisVal = Math.Max(0, progress.AegisTotal);
			int conditionVal = Math.Max(0, progress.PreventedDamageFromBlockCondition);

			// Build segments list with non-zero values
			var segments = new List<(SegmentType type, int value, Color color, string label)>();

			if (damageVal > 0)
				segments.Add((SegmentType.Damage, damageVal, new Color(DamageColorR, DamageColorG, DamageColorB), "Damage"));
			if (blockVal > 0)
				segments.Add((SegmentType.Block, blockVal, new Color(BlockColorR, BlockColorG, BlockColorB), "Block"));
			if (aegisVal > 0)
				segments.Add((SegmentType.Aegis, aegisVal, new Color(AegisColorR, AegisColorG, AegisColorB), "Aegis"));
			if (conditionVal > 0)
				segments.Add((SegmentType.Condition, conditionVal, new Color(ConditionColorR, ConditionColorG, ConditionColorB), "Condition"));

			if (segments.Count == 0)
			{
				CleanupTooltips(new HashSet<string>());
				_damageRect = _blockRect = _aegisRect = _conditionRect = Rectangle.Empty;
				return;
			}

			// Calculate proportional widths
			int totalValue = 0;
			foreach (var seg in segments)
				totalValue += seg.value;

			var segmentWidths = new List<int>();
			int availableWidth = TotalMeterWidth - (segments.Count - 1) * Math.Max(0, SegmentGap);
			int usedWidth = 0;
			for (int i = 0; i < segments.Count; i++)
			{
				float proportion = (float)segments[i].value / Math.Max(1, totalValue);
				int segW = Math.Max(MinSegmentWidth, (int)Math.Round(availableWidth * proportion));
				if (i == segments.Count - 1)
					segW = Math.Max(MinSegmentWidth, availableWidth - usedWidth);
				segmentWidths.Add(segW);
				usedWidth += segW;
			}

			// Calculate total width and center position
			int totalWidth = usedWidth + (segments.Count - 1) * SegmentGap;
			int startX = bannerBounds.Center.X - totalWidth / 2;
			int baseY = bannerBounds.Top + OffsetYFromBannerTop;

			// End SpriteBatch to draw parallelograms with BasicEffect
			_spriteBatch.End();

			// Setup BasicEffect matrices
			_basicEffect.World = Matrix.Identity;
			_basicEffect.View = Matrix.Identity;
			_basicEffect.Projection = Matrix.CreateOrthographicOffCenter(
				0, _graphicsDevice.Viewport.Width,
				_graphicsDevice.Viewport.Height, 0,
				0, 1);

			var presentKeys = new HashSet<string>();
			int currentX = startX;

			// Reset cached rectangles
			_damageRect = _blockRect = _aegisRect = _conditionRect = Rectangle.Empty;

			// Draw each segment as parallelogram
			for (int i = 0; i < segments.Count; i++)
			{
				var (type, value, color, label) = segments[i];
				int segWidth = segmentWidths[i];

				// Damage segment is elevated
				int yOffset = (type == SegmentType.Damage) ? DamageYOffset : 0;
				int drawY = baseY + yOffset;

				// Store rectangle for this segment (for chunk spawning)
				var segRect = new Rectangle(currentX, drawY, segWidth, SegmentHeight);
				switch (type)
				{
					case SegmentType.Damage: _damageRect = segRect; break;
					case SegmentType.Block: _blockRect = segRect; break;
					case SegmentType.Aegis: _aegisRect = segRect; break;
					case SegmentType.Condition: _conditionRect = segRect; break;
				}

				// Draw the parallelogram
				DrawParallelogram(currentX, drawY, segWidth, SegmentHeight, ParallelogramSlant, color);

				currentX += segWidth + SegmentGap;
			}

			// Draw floating chunks
			Color damageColor = new Color(DamageColorR, DamageColorG, DamageColorB);
			foreach (var chunk in _floatingChunks)
			{
				float t = EaseOutCubic(chunk.Progress);

				float x = MathHelper.Lerp(chunk.StartX, chunk.TargetX, t);
				float y = MathHelper.Lerp(chunk.StartY, chunk.TargetY, t);

				// Lerp color from source to damage red
				Color chunkColor = Color.Lerp(chunk.StartColor, damageColor, t);

				DrawParallelogram((int)x, (int)y, chunk.Width, chunk.Height, ParallelogramSlant, chunkColor);
			}

			// Restart SpriteBatch for text rendering
			_spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

			// Draw text on segments
			currentX = startX;
			for (int i = 0; i < segments.Count; i++)
			{
				var (type, value, color, label) = segments[i];
				int segWidth = segmentWidths[i];

				int yOffset = (type == SegmentType.Damage) ? DamageYOffset : 0;
				int drawY = baseY + yOffset;

				// Draw number centered (accounting for parallelogram slant)
				if (_font != null && value > 0)
				{
					string numText = value.ToString();
					var textSize = _font.MeasureString(numText) * FontScale;

					// Text color: white for dark backgrounds, black for light
					Color textColor = (type == SegmentType.Aegis) ? Color.Black : Color.White;

					// Center text in parallelogram (shift right by half slant)
					var textPos = new Vector2(
						currentX + segWidth / 2f + ParallelogramSlant / 2f - textSize.X / 2f,
						drawY + SegmentHeight / 2f - textSize.Y / 2f
					);
					_spriteBatch.DrawString(_font, numText, textPos, textColor, 0f, Vector2.Zero, FontScale, SpriteEffects.None, 0f);
				}

				// Update tooltip
				string key = $"DamageMeter_{type}";
				presentKeys.Add(key);
				var segmentRect = new Rectangle(currentX, drawY, segWidth + ParallelogramSlant, SegmentHeight);
				UpdateSegmentTooltipUi(key, segmentRect, $"{label}: {value}");

				currentX += segWidth + SegmentGap;
			}

			// Cleanup tooltips for segments no longer present
			CleanupTooltips(presentKeys);
		}

		private void DrawParallelogram(int x, int y, int width, int height, int slant, Color color)
		{
			// Parallelogram vertices (slanted to the right):
			// Top-left is shifted right by 'slant'
			//
			//     TL------TR
			//    /        /
			//   BL------BR
			//
			// TL = (x + slant, y)
			// TR = (x + slant + width, y)
			// BR = (x + width, y + height)
			// BL = (x, y + height)

			var vertices = new VertexPositionColor[4];
			vertices[0] = new VertexPositionColor(new Vector3(x + slant, y, 0), color);           // TL
			vertices[1] = new VertexPositionColor(new Vector3(x + slant + width, y, 0), color);   // TR
			vertices[2] = new VertexPositionColor(new Vector3(x + width, y + height, 0), color);  // BR
			vertices[3] = new VertexPositionColor(new Vector3(x, y + height, 0), color);          // BL

			// Draw as triangle strip: TL, TR, BL, BR
			var indices = new short[] { 0, 1, 3, 1, 2, 3 };

			foreach (var pass in _basicEffect.CurrentTechnique.Passes)
			{
				pass.Apply();
				_graphicsDevice.DrawUserIndexedPrimitives(
					PrimitiveType.TriangleList,
					vertices, 0, 4,
					indices, 0, 2);
			}
		}

		private static float EaseOutCubic(float t)
		{
			return 1f - (float)Math.Pow(1f - t, 3);
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
