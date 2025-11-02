using System;
using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System.Collections.Generic;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Displays the chalice icon to the right of the CourageDisplay when tribulations are active.
	/// Shows tooltip with tribulation text on hover. Includes pulse animation similar to medals.
	/// </summary>
	[DebugTab("Tribulation Display")]
	public class QuestTribulationDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly ContentManager _content;
		private Texture2D _chaliceTexture;
		private Entity _chaliceEntity;
		private readonly Dictionary<int, float> _bounceByEntityId = new Dictionary<int, float>();
		private double _lastDt = 0.0;

		// Layout/debug controls
		[DebugEditable(DisplayName = "Chalice Spacing", Step = 2, Min = 0, Max = 200)]
		public int ChaliceSpacing { get; set; } = 50;

		[DebugEditable(DisplayName = "Chalice Scale", Step = 0.1f, Min = 0.1f, Max = 5f)]
		public float ChaliceScale { get; set; } = .18f;

		[DebugEditable(DisplayName = "Chalice Offset X", Step = 1, Min = -200, Max = 200)]
		public int ChaliceOffsetX { get; set; } = 0;

		[DebugEditable(DisplayName = "Chalice Offset Y", Step = 1, Min = -200, Max = 200)]
		public int ChaliceOffsetY { get; set; } = 0;

		// Pulse animation debug fields (matching MedalDisplaySystem)
		[DebugEditable(DisplayName = "Pulse Duration (s)", Step = 0.05f, Min = 0.1f, Max = 2f)]
		public float PulseDurationSeconds { get; set; } = 0.5f;

		[DebugEditable(DisplayName = "Pulse Scale Amp", Step = 0.01f, Min = 0f, Max = 0.6f)]
		public float PulseScaleAmplitude { get; set; } = 0.44f;

		[DebugEditable(DisplayName = "Jiggle Degrees", Step = 0.5f, Min = 0f, Max = 45f)]
		public float JiggleDegrees { get; set; } = 5f;

		[DebugEditable(DisplayName = "Pulse Frequency (Hz)", Step = 0.1f, Min = 0.5f, Max = 8f)]
		public float PulseFrequencyHz { get; set; } = 1.7f;

		public QuestTribulationDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, ContentManager content)
			: base(entityManager)
		{
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
			_content = content;
			TryLoadAssets();
			EventManager.Subscribe<TribulationTriggered>(OnTribulationTriggered);
		}

		private void OnTribulationTriggered(TribulationTriggered evt)
		{
			if (evt == null) return;
			// Start pulse animation for the chalice entity
			if (_chaliceEntity != null)
			{
				_bounceByEntityId[_chaliceEntity.Id] = 0f; // start bounce timer
			}
		}

		private void TryLoadAssets()
		{
			try { _chaliceTexture = _content.Load<Texture2D>("chalice"); } catch { _chaliceTexture = null; }
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<Player>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		public override void Update(GameTime gameTime)
		{
			_lastDt = gameTime.ElapsedGameTime.TotalSeconds;
			// Decay active bounces over time (same pattern as MedalDisplaySystem)
			if (_bounceByEntityId.Count > 0)
			{
				var keys = _bounceByEntityId.Keys.ToList();
				for (int i = 0; i < keys.Count; i++)
				{
					int id = keys[i];
					float t = _bounceByEntityId[id];
					t += (float)_lastDt;
					if (t >= PulseDurationSeconds) _bounceByEntityId.Remove(id); else _bounceByEntityId[id] = t;
				}
			}
			base.Update(gameTime);
		}

		public void Draw()
		{
			var playerEntity = GetRelevantEntities().FirstOrDefault();
			if (playerEntity == null) return;

			// Check if player has any tribulations
			var tribulations = EntityManager.GetEntitiesWithComponent<Tribulation>()
				.Where(e => e.GetComponent<Tribulation>()?.PlayerOwner == playerEntity)
				.Select(e => e.GetComponent<Tribulation>())
				.Where(t => t != null)
				.ToList();

			if (tribulations.Count == 0)
			{
				// Hide chalice if no tribulations
				if (_chaliceEntity != null)
				{
					var ui = _chaliceEntity.GetComponent<UIElement>();
					if (ui != null) ui.Bounds = Rectangle.Empty;
				}
				return;
			}

			// Get courage center position (duplicate logic from CourageDisplaySystem)
			var courage = playerEntity.GetComponent<Courage>();
			if (courage == null) return;

			var anchor = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
			if (anchor == null) return;
			var anchorTransform = anchor.GetComponent<Transform>();
			if (anchorTransform == null) return;

			Vector2 courageCenter;
			var hpAnchor = playerEntity.GetComponent<HPBarAnchor>();
			if (hpAnchor != null && hpAnchor.Rect.Width > 0 && hpAnchor.Rect.Height > 0)
			{
				// Use same logic as CourageDisplaySystem for consistency
				int xRight = hpAnchor.Rect.X + hpAnchor.Rect.Width;
				int yMid = hpAnchor.Rect.Y + hpAnchor.Rect.Height / 2;
				// Get courage radius from CourageDisplaySystem's default or calculate
				int courageRadius = 20; // Default CircleRadius from CourageDisplaySystem
				courageCenter = new Vector2(xRight + Math.Max(-128, 8) + courageRadius, yMid);
			}
			else
			{
				courageCenter = new Vector2(anchorTransform.Position.X, anchorTransform.Position.Y + 224);
			}

			// Position chalice to the right of courage display
			Vector2 chalicePosition = new Vector2(
				courageCenter.X + ChaliceSpacing + ChaliceOffsetX,
				courageCenter.Y + ChaliceOffsetY
			);

			// Ensure chalice entity exists and is valid
			const string ChaliceEntityName = "TribulationChalice";
			var existingChalice = EntityManager.GetEntity(ChaliceEntityName);
			if (existingChalice != null && existingChalice.IsActive)
			{
				_chaliceEntity = existingChalice;
			}
			else
			{
				_chaliceEntity = EntityManager.CreateEntity(ChaliceEntityName);
				EntityManager.AddComponent(_chaliceEntity, new Transform { Position = chalicePosition, ZOrder = 10000 });
				EntityManager.AddComponent(_chaliceEntity, ParallaxLayer.GetUIParallaxLayer());
			}

			// Calculate pulse animation (same formula as MedalDisplaySystem)
			float scale = 1f;
			float rotation = 0f;
			if (_bounceByEntityId.TryGetValue(_chaliceEntity.Id, out var tPulse))
			{
				float dur = Math.Max(0.1f, PulseDurationSeconds);
				float norm = MathHelper.Clamp(tPulse / dur, 0f, 1f);
				float env = (1f - norm);
				env *= env; // quadratic decay
				float phase = MathHelper.TwoPi * PulseFrequencyHz * tPulse;
				float s = (float)Math.Sin(phase);
				scale = 1f + PulseScaleAmplitude * env * s;
				float jiggleRad = MathHelper.ToRadians(JiggleDegrees);
				rotation = jiggleRad * env * (float)Math.Sin(phase * 1.2f);
			}

			// Draw chalice icon
			if (_chaliceTexture != null)
			{
				float finalScale = ChaliceScale * scale;
				var origin = new Vector2(_chaliceTexture.Width / 2f, _chaliceTexture.Height / 2f);
				_spriteBatch.Draw(
					_chaliceTexture,
					chalicePosition,
					null,
					Color.White,
					rotation,
					origin,
					finalScale,
					SpriteEffects.None,
					0f
				);

				// Update UI element bounds for tooltip
				int iconSize = (int)(Math.Max(_chaliceTexture.Width, _chaliceTexture.Height) * finalScale);
				var hitRect = new Rectangle(
					(int)(chalicePosition.X - iconSize / 2f),
					(int)(chalicePosition.Y - iconSize / 2f),
					iconSize,
					iconSize
				);

				var ui = _chaliceEntity.GetComponent<UIElement>();
				if (ui == null)
				{
					ui = new UIElement { IsInteractable = false };
					EntityManager.AddComponent(_chaliceEntity, ui);
				}
				// Always update tooltip and bounds to ensure they're current after tribulations are recreated
				ui.Bounds = hitRect;
				ui.TooltipType = TooltipType.Text;
				ui.Tooltip = BuildTribulationTooltip(tribulations);
				ui.TooltipPosition = TooltipPosition.Above;
				ui.IsInteractable = false;

				var transform = _chaliceEntity.GetComponent<Transform>();
				if (transform != null)
				{
					transform.Position = chalicePosition;
				}
			}
		}

		private string BuildTribulationTooltip(List<Tribulation> tribulations)
		{
			if (tribulations == null || tribulations.Count == 0) return string.Empty;
			return string.Join("\n\n", tribulations.Select(t => t.Text ?? string.Empty));
		}
	}
}

