using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
    /// <summary>
    /// Animates the plunder "snatch" effect when an enemy steals a card from the player's deck.
    /// Multi-phase animation: Lift -> Pause -> Arc -> Settle -> Complete
    /// </summary>
    [DebugTab("Plunder Snatch Display")]
    public class PlunderSnatchDisplaySystem : Core.System
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;

        private class SnatchAnim
        {
            public Entity Card;
            public PlunderSnatchPhase Phase;
            public Vector2 OriginPos;
            public Vector2 LiftedPos;
            public Vector2 TargetPos;
            public float PhaseElapsed;
            public float CurrentScale;
            public float CurrentRotation;
            public int DamageThreshold;
            public readonly List<TrailNode> Trail = new List<TrailNode>();
        }

        private struct TrailNode
        {
            public Vector2 Pos;
            public float Age;
        }

        private readonly List<SnatchAnim> _anims = new List<SnatchAnim>();

        #region Phase Durations
        [DebugEditable(DisplayName = "Lift Duration (s)", Step = 0.01f, Min = 0.01f, Max = 0.5f)]
        public float LiftDuration { get; set; } = 0.12f;

        [DebugEditable(DisplayName = "Pause Duration (s)", Step = 0.01f, Min = 0.01f, Max = 0.5f)]
        public float PauseDuration { get; set; } = 0.15f;

        [DebugEditable(DisplayName = "Arc Duration (s)", Step = 0.01f, Min = 0.1f, Max = 1.0f)]
        public float ArcDuration { get; set; } = 0.35f;

        [DebugEditable(DisplayName = "Settle Duration (s)", Step = 0.01f, Min = 0.01f, Max = 0.3f)]
        public float SettleDuration { get; set; } = 0.1f;
        #endregion

        #region Animation Parameters
        [DebugEditable(DisplayName = "Lift Height (px)", Step = 5, Min = 0, Max = 100)]
        public int LiftHeightPx { get; set; } = 20;

        [DebugEditable(DisplayName = "Lift Scale", Step = 0.02f, Min = 1.0f, Max = 1.5f)]
        public float LiftScale { get; set; } = 1.15f;

        [DebugEditable(DisplayName = "Arc Height (px)", Step = 10, Min = 50, Max = 400)]
        public int ArcHeightPx { get; set; } = 180;

        [DebugEditable(DisplayName = "End Scale", Step = 0.02f, Min = 0.3f, Max = 1.0f)]
        public float EndScale { get; set; } = 0.55f;

        [DebugEditable(DisplayName = "Ease Power", Step = 0.1f, Min = 1.0f, Max = 5.0f)]
        public float EasePower { get; set; } = 2.5f;

        [DebugEditable(DisplayName = "Max Rotation (deg)", Step = 1, Min = 0, Max = 90)]
        public int MaxRotationDeg { get; set; } = 25;

        [DebugEditable(DisplayName = "Settle Bounce Scale", Step = 0.01f, Min = 0.5f, Max = 0.8f)]
        public float SettleBounceScale { get; set; } = 0.58f;
        #endregion

        #region Trail Settings
        [DebugEditable(DisplayName = "Trail Lifetime (s)", Step = 0.01f, Min = 0.05f, Max = 1.0f)]
        public float TrailLifetime { get; set; } = 0.35f;

        [DebugEditable(DisplayName = "Trail Core Alpha", Step = 0.01f, Min = 0f, Max = 1f)]
        public float TrailCoreAlpha { get; set; } = 0.50f;

        [DebugEditable(DisplayName = "Trail Glow Alpha", Step = 0.01f, Min = 0f, Max = 1f)]
        public float TrailGlowAlpha { get; set; } = 0.20f;

        [DebugEditable(DisplayName = "Trail Core Scale", Step = 0.01f, Min = 0.05f, Max = 2.0f)]
        public float TrailCoreScale { get; set; } = 0.30f;

        [DebugEditable(DisplayName = "Trail Glow Scale", Step = 0.01f, Min = 0.1f, Max = 3.0f)]
        public float TrailGlowScale { get; set; } = 0.9f;

        [DebugEditable(DisplayName = "Trail Radius (px)", Step = 1, Min = 2, Max = 128)]
        public int TrailRadiusPx { get; set; } = 32;

        [DebugEditable(DisplayName = "Trail Color R", Step = 5, Min = 0, Max = 255)]
        public int TrailR { get; set; } = 160;

        [DebugEditable(DisplayName = "Trail Color G", Step = 5, Min = 0, Max = 255)]
        public int TrailG { get; set; } = 50;

        [DebugEditable(DisplayName = "Trail Color B", Step = 5, Min = 0, Max = 255)]
        public int TrailB { get; set; } = 180;
        #endregion

        public PlunderSnatchDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
            : base(entityManager)
        {
            _graphicsDevice = graphicsDevice;
            _spriteBatch = spriteBatch;
            EventManager.Subscribe<PlunderSnatchAnimationRequested>(OnAnimRequested);
            EventManager.Subscribe<DeleteCachesEvent>(_ => _anims.Clear());
        }

        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<SceneState>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (dt <= 0f) return;

            for (int i = _anims.Count - 1; i >= 0; i--)
            {
                var a = _anims[i];
                a.PhaseElapsed += dt;

                // Age trail nodes and cull old ones
                for (int t = a.Trail.Count - 1; t >= 0; t--)
                {
                    var node = a.Trail[t];
                    node.Age += dt;
                    if (node.Age > TrailLifetime) { a.Trail.RemoveAt(t); }
                    else { a.Trail[t] = node; }
                }

                UpdatePhase(a);

                if (a.Phase == PlunderSnatchPhase.Complete)
                {
                    // Animation done - publish completion event
                    EventManager.Publish(new PlunderSnatchAnimationCompleted
                    {
                        Card = a.Card,
                        DamageThreshold = a.DamageThreshold
                    });
                    _anims.RemoveAt(i);
                }
            }
        }

        private void UpdatePhase(SnatchAnim a)
        {
            switch (a.Phase)
            {
                case PlunderSnatchPhase.Lift:
                    UpdateLift(a);
                    break;
                case PlunderSnatchPhase.Pause:
                    UpdatePause(a);
                    break;
                case PlunderSnatchPhase.Arc:
                    UpdateArc(a);
                    break;
                case PlunderSnatchPhase.Settle:
                    UpdateSettle(a);
                    break;
            }
        }

        private void UpdateLift(SnatchAnim a)
        {
            float t = MathHelper.Clamp(a.PhaseElapsed / Math.Max(0.001f, LiftDuration), 0f, 1f);
            float eased = EaseOut(t);

            // Position: rise upward
            a.LiftedPos = new Vector2(a.OriginPos.X, a.OriginPos.Y - LiftHeightPx);
            var currentPos = Vector2.Lerp(a.OriginPos, a.LiftedPos, eased);

            // Scale: grow slightly
            a.CurrentScale = MathHelper.Lerp(1.0f, LiftScale, eased);

            // Update component for rendering
            UpdateFlightComponent(a, currentPos);

            if (a.PhaseElapsed >= LiftDuration)
            {
                a.Phase = PlunderSnatchPhase.Pause;
                a.PhaseElapsed = 0f;
            }
        }

        private void UpdatePause(SnatchAnim a)
        {
            // Hold position and scale at lifted state
            UpdateFlightComponent(a, a.LiftedPos);

            if (a.PhaseElapsed >= PauseDuration)
            {
                a.Phase = PlunderSnatchPhase.Arc;
                a.PhaseElapsed = 0f;
            }
        }

        private void UpdateArc(SnatchAnim a)
        {
            float t = MathHelper.Clamp(a.PhaseElapsed / Math.Max(0.001f, ArcDuration), 0f, 1f);
            float eased = EaseIn(t, EasePower);

            // Position: arc from lifted to target
            var pos = GetArcPosition(a.LiftedPos, a.TargetPos, ArcHeightPx, eased);

            // Scale: shrink from lift scale to end scale
            a.CurrentScale = MathHelper.Lerp(LiftScale, EndScale, eased);

            // Rotation: sin curve that peaks mid-flight
            float rotationRad = (float)Math.Sin(Math.PI * t) * MathHelper.ToRadians(MaxRotationDeg);
            a.CurrentRotation = rotationRad;

            // Add trail node during arc phase
            a.Trail.Add(new TrailNode { Pos = pos, Age = 0f });

            UpdateFlightComponent(a, pos);

            if (a.PhaseElapsed >= ArcDuration)
            {
                a.Phase = PlunderSnatchPhase.Settle;
                a.PhaseElapsed = 0f;
            }
        }

        private void UpdateSettle(SnatchAnim a)
        {
            float t = MathHelper.Clamp(a.PhaseElapsed / Math.Max(0.001f, SettleDuration), 0f, 1f);
            float eased = EaseOut(t);

            // Subtle bounce: scale slightly larger then back to end scale
            a.CurrentScale = MathHelper.Lerp(SettleBounceScale, EndScale, eased);

            // Rotation: ease back to 0
            a.CurrentRotation = MathHelper.Lerp(a.CurrentRotation, 0f, eased);

            UpdateFlightComponent(a, a.TargetPos);

            if (a.PhaseElapsed >= SettleDuration)
            {
                a.Phase = PlunderSnatchPhase.Complete;
            }
        }

        private void UpdateFlightComponent(SnatchAnim a, Vector2 pos)
        {
            var flight = a.Card.GetComponent<PlunderSnatchFlight>();
            if (flight != null)
            {
                flight.CurrentPos = pos;
                flight.CurrentScale = a.CurrentScale;
                flight.CurrentRotation = a.CurrentRotation;
                flight.Phase = a.Phase;
            }
        }

        public void Draw()
        {
            if (_anims.Count == 0) return;

            // Draw trail first (behind card)
            DrawTrails();

            // Draw cards
            for (int i = 0; i < _anims.Count; i++)
            {
                var a = _anims[i];
                var flight = a.Card.GetComponent<PlunderSnatchFlight>();
                if (flight == null) continue;

                // Use CardRenderScaledRotatedEvent to render the card
                var t = a.Card.GetComponent<Transform>();
                if (t != null)
                {
                    t.Rotation = flight.CurrentRotation;
                }

                EventManager.Publish(new CardRenderScaledRotatedEvent
                {
                    Card = a.Card,
                    Position = flight.CurrentPos,
                    Scale = flight.CurrentScale
                });
            }
        }

        private void DrawTrails()
        {
            var circle = PrimitiveTextureFactory.GetAntiAliasedCircle(_graphicsDevice, Math.Max(1, TrailRadiusPx));
            var origin = new Vector2(circle.Width / 2f, circle.Height / 2f);
            var coreColor = new Color(ClampByte(TrailR), ClampByte(TrailG), ClampByte(TrailB));

            for (int i = 0; i < _anims.Count; i++)
            {
                var a = _anims[i];
                for (int j = 0; j < a.Trail.Count; j++)
                {
                    var node = a.Trail[j];
                    float w = 1f - MathHelper.Clamp(node.Age / Math.Max(0.001f, TrailLifetime), 0f, 1f);

                    // Outer glow
                    _spriteBatch.Draw(
                        circle,
                        node.Pos,
                        null,
                        coreColor * (TrailGlowAlpha * w),
                        0f,
                        origin,
                        TrailGlowScale * MathHelper.Lerp(0.6f, 1f, w),
                        SpriteEffects.None,
                        0f
                    );

                    // Inner core
                    _spriteBatch.Draw(
                        circle,
                        node.Pos,
                        null,
                        coreColor * (TrailCoreAlpha * w),
                        0f,
                        origin,
                        TrailCoreScale * MathHelper.Lerp(0.5f, 1f, w),
                        SpriteEffects.None,
                        0f
                    );
                }
            }
        }

        private void OnAnimRequested(PlunderSnatchAnimationRequested evt)
        {
            if (evt == null || evt.Card == null) return;

            var anim = new SnatchAnim
            {
                Card = evt.Card,
                Phase = PlunderSnatchPhase.Lift,
                OriginPos = evt.StartPos,
                LiftedPos = new Vector2(evt.StartPos.X, evt.StartPos.Y - LiftHeightPx),
                TargetPos = evt.TargetPos,
                PhaseElapsed = 0f,
                CurrentScale = 1f,
                CurrentRotation = 0f,
                DamageThreshold = evt.DamageThreshold
            };

            // Add flight component to card for tracking
            if (!evt.Card.HasComponent<PlunderSnatchFlight>())
            {
                EntityManager.AddComponent(evt.Card, new PlunderSnatchFlight
                {
                    Owner = evt.Card,
                    Phase = PlunderSnatchPhase.Lift,
                    OriginPos = evt.StartPos,
                    LiftedPos = anim.LiftedPos,
                    TargetPos = evt.TargetPos,
                    CurrentPos = evt.StartPos,
                    PhaseElapsed = 0f,
                    CurrentScale = 1f,
                    CurrentRotation = 0f
                });
            }

            _anims.Add(anim);
        }

        private static float EaseOut(float t)
        {
            t = MathHelper.Clamp(t, 0f, 1f);
            return 1f - (float)Math.Pow(1f - t, 2);
        }

        private static float EaseIn(float t, float pow)
        {
            t = MathHelper.Clamp(t, 0f, 1f);
            return (float)Math.Pow(t, pow);
        }

        private static Vector2 GetArcPosition(Vector2 a, Vector2 b, float arcHeight, float t)
        {
            var p = Vector2.Lerp(a, b, t);
            var ab = b - a;
            if (ab.LengthSquared() < 0.000001f) return p;
            var n = new Vector2(-ab.Y, ab.X);
            n.Normalize();
            // Ensure arc generally goes upward on screen (negative Y)
            if (n.Y > 0f) n = -n;
            float wave = (float)Math.Sin(Math.PI * t);
            return p + n * arcHeight * wave;
        }

        private static byte ClampByte(int v)
        {
            if (v < 0) return 0;
            if (v > 255) return 255;
            return (byte)v;
        }

        public bool IsAnimating => _anims.Count > 0;
    }
}
