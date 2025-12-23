using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Handles player portrait animations (e.g., attack lunge) and publishes impact signals.
	/// Keeps UI stable by writing only to PlayerAnimationState.DrawOffset.
	/// </summary>
	public class PlayerAnimationSystem : Core.System
	{
		private readonly Vector2 _attackOffset = new Vector2(80f, -20f);
		private readonly float _attackNudgePixels = 36f;

		public PlayerAnimationSystem(EntityManager entityManager) : base(entityManager)
		{
			EventManager.Subscribe<StartPlayerAttackAnimation>(_ =>
			{
				var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
				if (player == null) return;
				var anim = player.GetComponent<PlayerAnimationState>();
				if (anim == null)
				{
					anim = new PlayerAnimationState();
					EntityManager.AddComponent(player, anim);
				}
				anim.AttackAnimTimer = anim.AttackAnimDuration;
				var enemy = EntityManager.GetEntitiesWithComponent<Enemy>().FirstOrDefault();
				anim.AttackTargetPos = enemy?.GetComponent<Transform>()?.Position ?? Vector2.Zero;
				EventManager.Publish(new PlaySfxEvent { Track = SfxTrack.SwordAttack, Volume = 0.5f });
			});
			EventManager.Subscribe<StartBuffAnimation>(evt =>
			{
				// Apply squash-stretch to the requested target (player or enemy) over timed keyframes
				if (evt == null) return;
				if (evt.TargetIsPlayer)
				{
					var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
					if (player == null) return;
					EventManager.Publish(new PlaySfxEvent { Track = SfxTrack.Prayer, Volume = 0.5f });
					EnsureHasAnimState(player);
					EnsureScaleAnim(player, out var scaleAnim);
					EnqueueBuffKeyframes(scaleAnim);
				}
				else
				{
					var enemy = EntityManager.GetEntitiesWithComponent<Enemy>().FirstOrDefault();
					if (enemy == null) return;
					EnsureHasAnimState(enemy);
					EnsureScaleAnim(enemy, out var scaleAnim);
					EnqueueBuffKeyframes(scaleAnim);
				}
			});
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<PlayerAnimationState>();
		}

		protected override void UpdateEntity(Entity entity, Microsoft.Xna.Framework.GameTime gameTime)
		{
			var anim = entity.GetComponent<PlayerAnimationState>();
			var t = entity.GetComponent<Transform>();
			if (anim == null || t == null) return;
			float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
			anim.DrawOffset = Vector2.Zero;
			anim.ScaleMultiplier = new Vector2(1f, 1f);
			if (anim.AttackAnimTimer > 0f)
			{
				anim.AttackAnimTimer = System.Math.Max(0f, anim.AttackAnimTimer - dt);
				float ta = 1f - (anim.AttackAnimTimer / System.Math.Max(0.0001f, anim.AttackAnimDuration));
				float outPhase = System.Math.Min(0.5f, ta) * 2f;
				float backPhase = System.Math.Max(0f, ta - 0.5f) * 2f;
				var basePos = t.Position;
				Vector2 desired = anim.AttackTargetPos + _attackOffset;
				Vector2 dir = desired - basePos;
				Vector2 fallbackDir = Vector2.Normalize(_attackOffset);
				if (dir.LengthSquared() > 0.0001f)
				{
					dir = Vector2.Normalize(dir);
				}
				else
				{
					dir = fallbackDir;
				}
				Vector2 outPos = basePos + dir * _attackNudgePixels;
				Vector2 mid = Vector2.Lerp(basePos, outPos, 1f - (float)System.Math.Pow(1f - outPhase, 3));
				var animPos = Vector2.Lerp(mid, basePos, backPhase);
				anim.DrawOffset = animPos - basePos;
				if (anim.AttackAnimTimer == 0f)
				{
					EventManager.Publish(new PlayerAttackImpactNow());
				}
			}
			// Update scale keyframe animation if present
			if (_scaleAnims.TryGetValue(entity.Id, out var sa))
			{
				sa.Update(dt);
				if (sa.IsComplete)
				{
					_scaleAnims.Remove(entity.Id);
				}
				else
				{
					// Store scale multiplier on state so display can combine with base scale without stomping other effects
					anim.ScaleMultiplier = sa.CurrentScale;
				}
			}
		}
		private void EnsureHasAnimState(Entity e)
		{
			var st = e.GetComponent<PlayerAnimationState>();
			if (st == null)
			{
				st = new PlayerAnimationState();
				EntityManager.AddComponent(e, st);
			}
		}

		// Simple per-entity scale keyframe animation state
		private readonly System.Collections.Generic.Dictionary<int, ScaleAnim> _scaleAnims = new();

		private void EnsureScaleAnim(Entity e, out ScaleAnim sa)
		{
			if (!_scaleAnims.TryGetValue(e.Id, out sa))
			{
				sa = new ScaleAnim();
				_scaleAnims[e.Id] = sa;
			}
		}

		private void EnqueueBuffKeyframes(ScaleAnim sa)
		{
			// Reset and add the anime.js-inspired keyframes with durations (seconds)
			sa.Reset();
			sa.AddKeyframe(new Vector2(1.25f, 0.75f), 0.288f);
			sa.AddKeyframe(new Vector2(0.75f, 1.25f), 0.096f);
			sa.AddKeyframe(new Vector2(1.15f, 0.85f), 0.096f);
			sa.AddKeyframe(new Vector2(0.95f, 1.05f), 0.144f);
			sa.AddKeyframe(new Vector2(1.05f, 0.95f), 0.096f);
			sa.AddKeyframe(new Vector2(1f, 1f), 0.240f);
			sa.OnComplete = () => { EventManager.Publish(new BuffAnimationComplete { TargetIsPlayer = true }); };
		}

		private class ScaleAnim
		{
			private struct Kf { public Vector2 Scale; public float Duration; }
			private readonly System.Collections.Generic.List<Kf> _kfs = new();
			private int _index;
			private float _elapsed;
			public Vector2 CurrentScale = new Vector2(1f, 1f);
			public bool IsComplete => _index >= _kfs.Count;
			public System.Action OnComplete;

			public void Reset()
			{
				_kfs.Clear();
				_index = 0;
				_elapsed = 0f;
				CurrentScale = new Vector2(1f, 1f);
			}

			public void AddKeyframe(Vector2 scale, float durationSec)
			{
				_kfs.Add(new Kf { Scale = scale, Duration = System.Math.Max(0.0001f, durationSec) });
			}

			public void Update(float dt)
			{
				if (IsComplete) return;
				_elapsed += dt;
				float dur = _kfs[_index].Duration;
				float t = System.Math.Clamp(_elapsed / dur, 0f, 1f);
				// easeInOutQuad
				float eased = t < 0.5f ? 2f * t * t : 1f - System.MathF.Pow(-2f * t + 2f, 2f) / 2f;
				Vector2 from = (_index == 0) ? new Vector2(1f, 1f) : _kfs[_index - 1].Scale;
				Vector2 to = _kfs[_index].Scale;
				CurrentScale = Vector2.Lerp(from, to, eased);
				if (_elapsed >= dur)
				{
					_index++;
					_elapsed = 0f;
					if (IsComplete)
					{
						OnComplete?.Invoke();
					}
				}
			}
		}
	}
}


