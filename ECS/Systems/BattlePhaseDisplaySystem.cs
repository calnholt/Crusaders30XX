using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Draws the current battle phase in the top-right corner and animates a
	/// large phase transition banner when the phase changes.
	/// </summary>
	[DebugTab("Battle Phase Display")]
	public class BattlePhaseDisplaySystem : Core.System
	{
		private readonly GraphicsDevice _graphicsDevice;
		private readonly SpriteBatch _spriteBatch;
		private readonly SpriteFont _font;
		private Texture2D _pixel;

		// Small corner label
		[DebugEditable(DisplayName = "Label Offset X", Step = 2, Min = -2000, Max = 2000)]
		public int LabelOffsetX { get; set; } = -16;
		[DebugEditable(DisplayName = "Label Offset Y", Step = 2, Min = -2000, Max = 2000)]
		public int LabelOffsetY { get; set; } = 14;
		[DebugEditable(DisplayName = "Label Scale", Step = 0.05f, Min = 0.2f, Max = 3f)]
		public float LabelScale { get; set; } = 0.6f;

		// Transition banner
		[DebugEditable(DisplayName = "Trans In (s)", Step = 0.05f, Min = 0.05f, Max = 5f)]
		public float TransitionInSeconds { get; set; } = 0.5f;
		[DebugEditable(DisplayName = "Trans Hold (s)", Step = 0.05f, Min = 0.05f, Max = 5f)]
		public float TransitionHoldSeconds { get; set; } = 0.9f;
		[DebugEditable(DisplayName = "Trans Out (s)", Step = 0.05f, Min = 0.05f, Max = 5f)]
		public float TransitionOutSeconds { get; set; } = 0.5f;
		[DebugEditable(DisplayName = "Trans Y Offset", Step = 2, Min = -2000, Max = 2000)]
		public int TransitionOffsetY { get; set; } = 140;
		[DebugEditable(DisplayName = "Trans Scale", Step = 0.05f, Min = 0.2f, Max = 4f)]
		public float TransitionScale { get; set; } = 1.25f;
		[DebugEditable(DisplayName = "Shadow Offset", Step = 1, Min = 0, Max = 20)]
		public int ShadowOffset { get; set; } = 2;

		private BattlePhase _lastPhase = BattlePhase.StartOfBattle;
		private bool _transitionActive;
		private float _transitionT; // seconds in current transition
		private string _transitionText = string.Empty;
		private bool _playedInitial;

		public BattlePhaseDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, SpriteFont font)
			: base(entityManager)
		{
			_graphicsDevice = graphicsDevice;
			_spriteBatch = spriteBatch;
			_font = font;
			_pixel = new Texture2D(graphicsDevice, 1, 1);
			_pixel.SetData(new[] { Color.White });
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<BattlePhaseState>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			var state = entity.GetComponent<BattlePhaseState>();
			if (state == null || state.Phase == BattlePhase.ProcessEnemyAttack) return;
			if (!_playedInitial)
			{
				_playedInitial = true;
				_lastPhase = state.Phase;
				_transitionActive = true;
				_transitionT = 0f;
				_transitionText = PhaseToString(_lastPhase);
			}
			else if (state.Phase != _lastPhase)
			{
				_lastPhase = state.Phase;
				_transitionActive = true;
				_transitionT = 0f;
				_transitionText = PhaseToString(_lastPhase);
			}
			if (_transitionActive)
			{
				_transitionT += (float)gameTime.ElapsedGameTime.TotalSeconds;
				float total = TransitionInSeconds + TransitionHoldSeconds + TransitionOutSeconds;
				if (_transitionT >= total) _transitionActive = false;
			}
		}

		public void Draw()
		{
			if (_font == null) return;
			var state = EntityManager.GetEntitiesWithComponent<BattlePhaseState>().FirstOrDefault()?.GetComponent<BattlePhaseState>();
			if (state == null) return;

			int vw = _graphicsDevice.Viewport.Width;
			int xRight = vw + LabelOffsetX;
			string label = PhaseToString(state.Phase);
			var size = _font.MeasureString(label) * LabelScale;
			var pos = new Vector2(xRight - size.X, LabelOffsetY);
			_spriteBatch.DrawString(_font, label, pos + new Vector2(ShadowOffset, ShadowOffset), Color.Black * 0.6f, 0f, Vector2.Zero, LabelScale, SpriteEffects.None, 0f);
			_spriteBatch.DrawString(_font, label, pos, Color.White, 0f, Vector2.Zero, LabelScale, SpriteEffects.None, 0f);

			if (_transitionActive)
			{
				float total = Math.Max(0.01f, TransitionInSeconds + TransitionHoldSeconds + TransitionOutSeconds);
				float t = _transitionT;
				float inEnd = TransitionInSeconds;
				float holdEnd = TransitionInSeconds + TransitionHoldSeconds;

				float centerX = vw * 0.5f;
				var tSize = _font.MeasureString(_transitionText) * TransitionScale;
				float targetX = centerX - tSize.X / 2f;
				float y = TransitionOffsetY;
				float x;
				if (t <= inEnd)
				{
					float p = MathHelper.Clamp(t / Math.Max(0.001f, TransitionInSeconds), 0f, 1f);
					p = EaseOutCubic(p);
					x = MathHelper.Lerp(-tSize.X - 80f, targetX, p);
				}
				else if (t <= holdEnd)
				{
					x = targetX;
				}
				else
				{
					float u = MathHelper.Clamp((t - holdEnd) / Math.Max(0.001f, TransitionOutSeconds), 0f, 1f);
					u = EaseInCubic(u);
					x = MathHelper.Lerp(targetX, vw + 80f, u);
				}

				// simple shadow then text
				var pText = new Vector2(x, y);
				_spriteBatch.DrawString(_font, _transitionText, pText + new Vector2(ShadowOffset, ShadowOffset), Color.Black * 0.6f, 0f, Vector2.Zero, TransitionScale, SpriteEffects.None, 0f);
				_spriteBatch.DrawString(_font, _transitionText, pText, Color.White, 0f, Vector2.Zero, TransitionScale, SpriteEffects.None, 0f);
			}
		}

		private static string PhaseToString(BattlePhase p)
		{
			return p switch
			{
				BattlePhase.StartOfBattle => "Start of Battle",
				BattlePhase.Block => "Block Phase",
				BattlePhase.Action => "Action Phase",
				BattlePhase.ProcessEnemyAttack => "Processing Enemy Attack",
				_ => p.ToString()
			};
		}

		private static float EaseOutCubic(float t)
		{
			t = MathHelper.Clamp(t, 0f, 1f);
			float u = 1f - t;
			return 1f - u * u * u;
		}

		private static float EaseInCubic(float t)
		{
			t = MathHelper.Clamp(t, 0f, 1f);
			return t * t * t;
		}
	}
}


