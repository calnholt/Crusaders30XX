# Guard Mechanic Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a Guard mechanic where enemies gain value-based shields that absorb player damage and convert to aggression at turn start, prototyped on a new Sentinel enemy.

**Architecture:** New `GuardQueue` component holds an ordered list of guard values. `GuardManagementSystem` handles the full lifecycle (gain via Sentinel passive at PreBlock, convert to aggression at EnemyStart, cleanup). Guard consumption on player attacks is inline in `HpManagementSystem` via a `TryConsumeGuard` method. `GuardQueueDisplaySystem` renders guard pips above intent pips with break/gain animations.

**Tech Stack:** C# / .NET 8.0 / MonoGame DesktopGL / Custom ECS

---

## File Map

| File | Action | Responsibility |
|------|--------|----------------|
| `ECS/Components/GuardComponents.cs` | Create | `GuardQueue` component |
| `ECS/Events/GuardEvents.cs` | Create | `AddGuardEvent`, `GuardConsumedEvent`, `GuardGainedEvent` |
| `ECS/Scenes/BattleScene/GuardManagementSystem.cs` | Create | Guard lifecycle: gain at PreBlock, aggression at EnemyStart, cleanup |
| `ECS/Scenes/BattleScene/GuardQueueDisplaySystem.cs` | Create | Render guard pips + break/gain animations |
| `ECS/Objects/Enemies/Sentinel.cs` | Create | Enemy definition + 3 attack classes |
| `ECS/Components/CardComponents.cs` | Modify | Add `Sentinel` to `AppliedPassiveType` enum |
| `ECS/Scenes/BattleScene/HpManagementSystem.cs` | Modify | Add `TryConsumeGuard` before `GetPassiveDelta` |
| `ECS/Factories/EnemyFactory.cs` | Modify | Register Sentinel |
| `ECS/Factories/EnemyAttackFactory.cs` | Modify | Register 3 Sentinel attacks |
| `ECS/Scenes/BattleScene/BattleSceneSystem.cs` | Modify | Instantiate + register + draw guard systems |

---

### Task 1: Data Layer — GuardQueue Component and Events

**Files:**
- Create: `ECS/Components/GuardComponents.cs`
- Create: `ECS/Events/GuardEvents.cs`
- Modify: `ECS/Components/CardComponents.cs:1107` (add to `AppliedPassiveType` enum)

- [ ] **Step 1: Create GuardQueue component**

Create `ECS/Components/GuardComponents.cs`:

```csharp
using System.Collections.Generic;
using Crusaders30XX.ECS.Core;

namespace Crusaders30XX.ECS.Components
{
    public class GuardQueue : IComponent
    {
        public Entity Owner { get; set; }
        public List<int> Queue { get; set; } = new();
    }
}
```

- [ ] **Step 2: Create guard events**

Create `ECS/Events/GuardEvents.cs`:

```csharp
using Crusaders30XX.ECS.Core;

namespace Crusaders30XX.ECS.Events
{
    public class AddGuardEvent
    {
        public Entity Enemy { get; set; }
        public int Value { get; set; }
    }

    public class GuardConsumedEvent
    {
        public Entity Enemy { get; set; }
        public int GuardValue { get; set; }
        public int RemainingCount { get; set; }
    }

    public class GuardGainedEvent
    {
        public Entity Enemy { get; set; }
        public int GuardValue { get; set; }
    }
}
```

- [ ] **Step 3: Add Sentinel to AppliedPassiveType enum**

In `ECS/Components/CardComponents.cs`, add `Sentinel` after `Marksman` (line 1107):

```csharp
        Marksman,
        Sentinel,
```

- [ ] **Step 4: Build to verify**

Run: `dotnet build`
Expected: Build succeeds with no errors.

- [ ] **Step 5: Commit**

```bash
git add ECS/Components/GuardComponents.cs ECS/Events/GuardEvents.cs ECS/Components/CardComponents.cs
git commit -m "add GuardQueue component, guard events, and Sentinel passive type"
```

---

### Task 2: Guard Consumption in HpManagementSystem

**Files:**
- Modify: `ECS/Scenes/BattleScene/HpManagementSystem.cs:33-43`

- [ ] **Step 1: Add using for GuardEvents**

At the top of `HpManagementSystem.cs`, verify `Crusaders30XX.ECS.Events` is already imported (it is at line 4). No change needed.

- [ ] **Step 2: Add TryConsumeGuard method**

Add this private method after `TryConsumeAegis` (after line 208):

```csharp
        private int TryConsumeGuard(Entity target, int rawDamage)
        {
            var gq = target.GetComponent<GuardQueue>();
            if (gq == null || gq.Queue.Count == 0) return 0;
            int remaining = rawDamage;
            int totalAbsorbed = 0;
            while (remaining > 0 && gq.Queue.Count > 0)
            {
                int guardValue = gq.Queue[0];
                gq.Queue.RemoveAt(0);
                int absorbed = System.Math.Min(guardValue, remaining);
                totalAbsorbed += absorbed;
                remaining -= guardValue; // guard is fully consumed; if guardValue > remaining, remaining goes negative (clamped below)
                EventManager.Publish(new GuardConsumedEvent { Enemy = target, GuardValue = guardValue, RemainingCount = gq.Queue.Count });
            }
            if (remaining < 0) remaining = 0;
            return totalAbsorbed;
        }
```

- [ ] **Step 3: Call TryConsumeGuard in OnModifyHpRequest**

In `OnModifyHpRequest`, after the null checks (line 39: `int before = hp.Current;`) and **before** `int passiveDelta = AppliedPassivesService.GetPassiveDelta(e);` (line 41), insert:

```csharp
            // Guard absorption: absorb raw incoming attack damage before passives
            if (e.Delta < 0 && e.DamageType == ModifyTypeEnum.Attack)
            {
                int guardAbsorbed = TryConsumeGuard(target, System.Math.Abs(e.Delta));
                if (guardAbsorbed > 0)
                {
                    e.Delta += guardAbsorbed;
                    if (e.Delta >= 0) return;
                }
            }
```

- [ ] **Step 4: Add GuardComponents using**

Add `using Crusaders30XX.ECS.Components;` — already imported (line 3). Verify `GuardQueue` and `GuardConsumedEvent` resolve. The events namespace `Crusaders30XX.ECS.Events` is also already imported (line 4). No change needed.

- [ ] **Step 5: Build to verify**

Run: `dotnet build`
Expected: Build succeeds.

- [ ] **Step 6: Commit**

```bash
git add ECS/Scenes/BattleScene/HpManagementSystem.cs
git commit -m "add guard consumption in HpManagementSystem before passive delta"
```

---

### Task 3: GuardManagementSystem

**Files:**
- Create: `ECS/Scenes/BattleScene/GuardManagementSystem.cs`

- [ ] **Step 1: Create GuardManagementSystem**

Create `ECS/Scenes/BattleScene/GuardManagementSystem.cs`:

```csharp
using System;
using System.Linq;
using System.Collections.Generic;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Systems
{
    public class GuardManagementSystem : Core.System
    {
        public static readonly float Duration = 0.5f;

        public GuardManagementSystem(EntityManager entityManager) : base(entityManager)
        {
            EventManager.Subscribe<AddGuardEvent>(OnAddGuard);
            EventManager.Subscribe<ChangeBattlePhaseEvent>(OnChangeBattlePhase);
            EventManager.Subscribe<LoadSceneEvent>(OnLoadScene);
        }

        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<GuardQueue>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

        private void OnAddGuard(AddGuardEvent e)
        {
            if (e.Enemy == null || e.Value <= 0) return;
            var gq = e.Enemy.GetComponent<GuardQueue>();
            if (gq == null)
            {
                gq = new GuardQueue();
                EntityManager.AddComponent(e.Enemy, gq);
            }
            gq.Queue.Add(e.Value);
            EventManager.Publish(new GuardGainedEvent { Enemy = e.Enemy, GuardValue = e.Value });
        }

        private void OnChangeBattlePhase(ChangeBattlePhaseEvent evt)
        {
            var enemy = EntityManager.GetEntity("Enemy");
            if (enemy == null) return;

            if (evt.Current == SubPhase.EnemyStart)
            {
                ConvertGuardsToAggression(enemy);
            }
            else if (evt.Current == SubPhase.PreBlock)
            {
                TryGuardConversion(enemy);
            }
        }

        private void ConvertGuardsToAggression(Entity enemy)
        {
            var gq = enemy.GetComponent<GuardQueue>();
            if (gq == null || gq.Queue.Count == 0) return;

            int count = gq.Queue.Count;
            gq.Queue.Clear();

            EventQueueBridge.EnqueueTriggerAction("GuardManagementSystem.ConvertGuardsToAggression", () =>
            {
                EventManager.Publish(new ApplyPassiveEvent
                {
                    Target = enemy,
                    Type = AppliedPassiveType.Aggression,
                    Delta = count
                });
            }, Duration);
        }

        private void TryGuardConversion(Entity enemy)
        {
            var ap = enemy.GetComponent<AppliedPassives>();
            if (ap == null || !ap.Passives.ContainsKey(AppliedPassiveType.Sentinel)) return;

            var intent = enemy.GetComponent<AttackIntent>();
            if (intent == null || intent.Planned == null || intent.Planned.Count == 0) return;

            // Process the first planned attack (the one being revealed at PreBlock)
            var planned = intent.Planned[0];
            var attackDef = planned.AttackDefinition;
            if (attackDef == null || attackDef.Damage <= 0) return;

            int damage = attackDef.Damage;
            int conversionAmount = RollGuardConversion(damage);
            if (conversionAmount <= 0) return;

            if (conversionAmount >= damage)
            {
                // Full conversion: skip this attack entirely
                intent.Planned.RemoveAt(0);

                EventQueueBridge.EnqueueTriggerAction("GuardManagementSystem.FullConversion", () =>
                {
                    EventManager.Publish(new AddGuardEvent { Enemy = enemy, Value = damage });

                    EventQueue.Clear();
                    if (intent.Planned.Count == 0)
                    {
                        // No attacks remain — short-circuit to EnemyEnd -> PlayerStart -> Action
                        EventQueue.EnqueueRule(new EventQueueBridge.QueuedPublish<ChangeBattlePhaseEvent>(
                            "Rule.ChangePhase.EnemyEnd",
                            new ChangeBattlePhaseEvent { Current = SubPhase.EnemyEnd }
                        ));
                        EventQueue.EnqueueRule(new EventQueueBridge.QueuedPublish<ChangeBattlePhaseEvent>(
                            "Rule.ChangePhase.PlayerStart",
                            new ChangeBattlePhaseEvent { Current = SubPhase.PlayerStart }
                        ));
                        EventQueue.EnqueueRule(new EventQueueBridge.QueuedPublish<ChangeBattlePhaseEvent>(
                            "Rule.ChangePhase.Action",
                            new ChangeBattlePhaseEvent { Current = SubPhase.Action }
                        ));
                    }
                    else
                    {
                        // Attacks remain — re-trigger PreBlock for the next attack
                        EventQueue.EnqueueRule(new EventQueueBridge.QueuedPublish<ChangeBattlePhaseEvent>(
                            "Rule.ChangePhase.PreBlock.GuardSkip",
                            new ChangeBattlePhaseEvent { Current = SubPhase.PreBlock }
                        ));
                    }
                }, Duration);
            }
            else
            {
                // Partial conversion: reduce attack damage, gain guard
                attackDef.Damage -= conversionAmount;

                EventQueueBridge.EnqueueTriggerAction("GuardManagementSystem.PartialConversion", () =>
                {
                    EventManager.Publish(new AddGuardEvent { Enemy = enemy, Value = conversionAmount });
                }, Duration);
            }
        }

        private int RollGuardConversion(int damage)
        {
            // 50% chance of 0; remaining 50% uniform across 1..damage
            int roll = Random.Shared.Next(0, damage * 2);
            if (roll < damage)
            {
                return 0; // 50% chance: no conversion
            }
            return roll - damage + 1; // maps [damage..damage*2-1] to [1..damage]
        }

        private void OnLoadScene(LoadSceneEvent e)
        {
            // Cleanup guard queues on scene transitions
            foreach (var entity in GetRelevantEntities().ToList())
            {
                var gq = entity.GetComponent<GuardQueue>();
                if (gq != null) gq.Queue.Clear();
            }
        }
    }
}
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build`
Expected: Build succeeds (system is not yet registered, but compiles).

- [ ] **Step 3: Commit**

```bash
git add ECS/Scenes/BattleScene/GuardManagementSystem.cs
git commit -m "add GuardManagementSystem with guard lifecycle and Sentinel conversion"
```

---

### Task 4: Sentinel Enemy Definition

**Files:**
- Create: `ECS/Objects/Enemies/Sentinel.cs`
- Modify: `ECS/Factories/EnemyFactory.cs:45-46`
- Modify: `ECS/Factories/EnemyAttackFactory.cs:120-121`

- [ ] **Step 1: Create Sentinel enemy and attacks**

Create `ECS/Objects/Enemies/Sentinel.cs`:

```csharp
using System;
using System.Collections.Generic;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.ECS.Objects.Enemies;
using Crusaders30XX.ECS.Systems;

namespace Crusaders30XX.ECS.Objects.EnemyAttacks;

public class Sentinel : EnemyBase
{
    public Sentinel(EnemyDifficulty difficulty = EnemyDifficulty.Easy) : base(difficulty)
    {
        Id = "sentinel";
        Name = "Sentinel";
        MaxHealth = 16 + (int)difficulty * 2;

        OnStartOfBattle = (entityManager) =>
        {
            var enemy = entityManager.GetEntity("Enemy");
            entityManager.AddComponent(enemy, new GuardQueue());

            EventQueueBridge.EnqueueTriggerAction("Sentinel.OnStartOfBattle", () =>
            {
                EventManager.Publish(new ApplyPassiveEvent
                {
                    Target = enemy,
                    Type = AppliedPassiveType.Sentinel,
                    Delta = 1
                });
            }, AppliedPassivesManagementSystem.Duration);
        };
    }

    public override IEnumerable<string> GetAttackIds(EntityManager entityManager, int turnNumber)
    {
        int roll = Random.Shared.Next(0, 3);
        return roll switch
        {
            0 => ["sentinel_slam"],
            1 => ["twin_strike", "twin_strike"],
            _ => ["rapid_jab", "rapid_jab", "rapid_jab"],
        };
    }
}

public class SentinelSlam : EnemyAttackBase
{
    public SentinelSlam()
    {
        Id = "sentinel_slam";
        Name = "Sentinel Slam";
        Damage = 9;
    }
}

public class TwinStrike : EnemyAttackBase
{
    public TwinStrike()
    {
        Id = "twin_strike";
        Name = "Twin Strike";
        Damage = 5;
    }
}

public class RapidJab : EnemyAttackBase
{
    public RapidJab()
    {
        Id = "rapid_jab";
        Name = "Rapid Jab";
        Damage = 3;
    }
}
```

- [ ] **Step 2: Register Sentinel in EnemyFactory**

In `ECS/Factories/EnemyFactory.cs`, add to the `Create` switch (after `"sniper"` line 45):

```csharp
                "sentinel" => new Sentinel(difficulty),
```

And add to `GetAllEnemies` dictionary (after `"sniper"` line 82):

```csharp
                { "sentinel", new Sentinel(difficulty) },
```

Also add the using at top if not already there:

```csharp
using Crusaders30XX.ECS.Objects.EnemyAttacks;
```

(Already imported at line 3.)

- [ ] **Step 3: Register attacks in EnemyAttackFactory**

In `ECS/Factories/EnemyAttackFactory.cs`, add to `Create` switch (after `"sniper_shot"` line 120):

```csharp
                // Sentinel attacks
                "sentinel_slam" => new SentinelSlam(),
                "twin_strike" => new TwinStrike(),
                "rapid_jab" => new RapidJab(),
```

And add to `GetAllAttacks` dictionary (after `"sniper_shot"` line 232):

```csharp
                // Sentinel attacks
                { "sentinel_slam", new SentinelSlam() },
                { "twin_strike", new TwinStrike() },
                { "rapid_jab", new RapidJab() },
```

- [ ] **Step 4: Build to verify**

Run: `dotnet build`
Expected: Build succeeds.

- [ ] **Step 5: Commit**

```bash
git add ECS/Objects/Enemies/Sentinel.cs ECS/Factories/EnemyFactory.cs ECS/Factories/EnemyAttackFactory.cs
git commit -m "add Sentinel enemy with three attack patterns"
```

---

### Task 5: GuardQueueDisplaySystem

**Files:**
- Create: `ECS/Scenes/BattleScene/GuardQueueDisplaySystem.cs`

- [ ] **Step 1: Create GuardQueueDisplaySystem**

Create `ECS/Scenes/BattleScene/GuardQueueDisplaySystem.cs`:

```csharp
using System;
using System.Linq;
using System.Collections.Generic;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Crusaders30XX.ECS.Systems
{
    [DebugTab("Guard Queue Display")]
    public class GuardQueueDisplaySystem : Core.System
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;
        private Texture2D _pixel;
        private SpriteFont _font;
        private readonly Dictionary<(int radius, int thickness), Texture2D> _ringCache = new();

        // Animation state
        private readonly Dictionary<int, BreakAnim> _breakAnims = new(); // keyed by animation id
        private readonly List<GainAnim> _gainAnims = new();
        private int _nextAnimId = 0;

        [DebugEditable(DisplayName = "Offset Y", Step = 2, Min = -400, Max = 400)]
        public int OffsetY { get; set; } = -260;

        [DebugEditable(DisplayName = "Pip Radius", Step = 1, Min = 4, Max = 32)]
        public int PipRadius { get; set; } = 12;

        [DebugEditable(DisplayName = "Pip Gap", Step = 1, Min = 0, Max = 32)]
        public int PipGap { get; set; } = 8;

        [DebugEditable(DisplayName = "Pip Thickness", Step = 1, Min = 1, Max = 8)]
        public int PipThickness { get; set; } = 3;

        [DebugEditable(DisplayName = "Font Scale", Step = 0.05f, Min = 0.1f, Max = 2f)]
        public float FontScale { get; set; } = 0.5f;

        [DebugEditable(DisplayName = "Break Duration", Step = 0.05f, Min = 0.1f, Max = 1f)]
        public float BreakDuration { get; set; } = 0.3f;

        [DebugEditable(DisplayName = "Break Max Scale", Step = 0.1f, Min = 1f, Max = 3f)]
        public float BreakMaxScale { get; set; } = 1.5f;

        [DebugEditable(DisplayName = "Gain Duration", Step = 0.05f, Min = 0.1f, Max = 1f)]
        public float GainDuration { get; set; } = 0.25f;

        [DebugEditable(DisplayName = "Pip Color R", Step = 5, Min = 0, Max = 255)]
        public int PipColorR { get; set; } = 100;

        [DebugEditable(DisplayName = "Pip Color G", Step = 5, Min = 0, Max = 255)]
        public int PipColorG { get; set; } = 180;

        [DebugEditable(DisplayName = "Pip Color B", Step = 5, Min = 0, Max = 255)]
        public int PipColorB { get; set; } = 255;

        public GuardQueueDisplaySystem(EntityManager entityManager, GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
            : base(entityManager)
        {
            _graphicsDevice = graphicsDevice;
            _spriteBatch = spriteBatch;
            _pixel = new Texture2D(graphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });
            EventManager.Subscribe<GuardConsumedEvent>(OnGuardConsumed);
            EventManager.Subscribe<GuardGainedEvent>(OnGuardGained);
            EventManager.Subscribe<DeleteCachesEvent>(OnDeleteCaches);
            EventManager.Subscribe<LoadSceneEvent>(OnLoadScene);
        }

        protected override IEnumerable<Entity> GetRelevantEntities()
        {
            return EntityManager.GetEntitiesWithComponent<GuardQueue>();
        }

        protected override void UpdateEntity(Entity entity, GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Update break animations
            var completedBreaks = new List<int>();
            foreach (var kv in _breakAnims)
            {
                kv.Value.Elapsed += dt;
                if (kv.Value.Elapsed >= BreakDuration)
                    completedBreaks.Add(kv.Key);
            }
            foreach (var id in completedBreaks)
                _breakAnims.Remove(id);

            // Update gain animations
            for (int i = _gainAnims.Count - 1; i >= 0; i--)
            {
                _gainAnims[i].Elapsed += dt;
                if (_gainAnims[i].Elapsed >= GainDuration)
                    _gainAnims.RemoveAt(i);
            }
        }

        public void Draw()
        {
            if (_font == null)
            {
                _font = FontSingleton.DefaultFont;
                if (_font == null) return;
            }

            var enemy = EntityManager.GetEntity("Enemy");
            if (enemy == null) return;
            var gq = enemy.GetComponent<GuardQueue>();
            if (gq == null || gq.Queue.Count == 0 && _breakAnims.Count == 0) return;

            var t = enemy.GetComponent<Transform>();
            if (t == null) return;

            var pipColor = new Color(PipColorR, PipColorG, PipColorB);
            int count = gq.Queue.Count;
            int diameter = PipRadius * 2;
            int totalWidth = count * diameter + Math.Max(0, count - 1) * PipGap;
            float centerX = t.Position.X;
            float centerY = t.Position.Y + OffsetY;
            int startX = (int)Math.Round(centerX - totalWidth / 2f);

            for (int i = 0; i < count; i++)
            {
                int value = gq.Queue[i];
                int x = startX + i * (diameter + PipGap) + PipRadius;
                int y = (int)centerY;

                // Check for gain animation (scale from 0 to 1)
                float scale = 1f;
                float alpha = 1f;
                var gainAnim = _gainAnims.FirstOrDefault(g => g.QueueIndex == i && !g.Used);
                if (gainAnim != null)
                {
                    float progress = Math.Clamp(gainAnim.Elapsed / GainDuration, 0f, 1f);
                    // Overshoot: scale goes to 1.15 then settles to 1.0
                    scale = progress < 0.6f
                        ? MathHelper.Lerp(0f, 1.15f, progress / 0.6f)
                        : MathHelper.Lerp(1.15f, 1f, (progress - 0.6f) / 0.4f);
                    gainAnim.Used = true;
                }

                DrawGuardPip(new Vector2(x, y), PipRadius, pipColor, scale, alpha, value);
            }

            // Draw break animations (fading out pips)
            foreach (var kv in _breakAnims)
            {
                var anim = kv.Value;
                float progress = Math.Clamp(anim.Elapsed / BreakDuration, 0f, 1f);
                float breakScale = MathHelper.Lerp(1f, BreakMaxScale, progress);
                float breakAlpha = MathHelper.Lerp(1f, 0f, progress);
                DrawGuardPip(anim.Position, PipRadius, pipColor, breakScale, breakAlpha, anim.Value);
            }
        }

        private void DrawGuardPip(Vector2 center, int radius, Color color, float scale, float alpha, int value)
        {
            var drawColor = color * alpha;
            int scaledRadius = Math.Max(1, (int)Math.Round(radius * scale));

            // Draw ring
            var tex = GetRingTexture(scaledRadius, PipThickness);
            _spriteBatch.Draw(tex, center, sourceRectangle: null, color: drawColor,
                rotation: 0f, origin: new Vector2(scaledRadius, scaledRadius),
                scale: 1f, effects: SpriteEffects.None, layerDepth: 0f);

            // Draw value text centered
            string text = value.ToString();
            var textSize = _font.MeasureString(text) * FontScale;
            var textPos = new Vector2(
                center.X - textSize.X / 2f,
                center.Y - textSize.Y / 2f
            );
            _spriteBatch.DrawString(_font, text, textPos, drawColor * alpha,
                0f, Vector2.Zero, FontScale * scale, SpriteEffects.None, 0f);
        }

        private void OnGuardConsumed(GuardConsumedEvent e)
        {
            // Compute where the consumed guard was (first pip position)
            var enemy = EntityManager.GetEntity("Enemy");
            if (enemy == null) return;
            var t = enemy.GetComponent<Transform>();
            if (t == null) return;

            // The consumed guard was at the front, so compute its last known position
            // Since the guard was just removed, we estimate from the current queue + 1
            int totalCount = e.RemainingCount + 1;
            int diameter = PipRadius * 2;
            int totalWidth = totalCount * diameter + Math.Max(0, totalCount - 1) * PipGap;
            float centerX = t.Position.X;
            float centerY = t.Position.Y + OffsetY;
            int startX = (int)Math.Round(centerX - totalWidth / 2f);
            int x = startX + PipRadius; // first pip
            int y = (int)centerY;

            _breakAnims[_nextAnimId++] = new BreakAnim
            {
                Position = new Vector2(x, y),
                Value = e.GuardValue,
                Elapsed = 0f
            };
        }

        private void OnGuardGained(GuardGainedEvent e)
        {
            var enemy = EntityManager.GetEntity("Enemy");
            if (enemy == null) return;
            var gq = enemy.GetComponent<GuardQueue>();
            if (gq == null) return;

            // The new guard is at the end of the queue
            _gainAnims.Add(new GainAnim
            {
                QueueIndex = gq.Queue.Count - 1,
                Elapsed = 0f,
                Used = false
            });
        }

        private void OnDeleteCaches(DeleteCachesEvent evt)
        {
            foreach (var kv in _ringCache)
            {
                try { kv.Value?.Dispose(); } catch { }
            }
            _ringCache.Clear();
        }

        private void OnLoadScene(LoadSceneEvent e)
        {
            _breakAnims.Clear();
            _gainAnims.Clear();
        }

        private Texture2D GetRingTexture(int radius, int thickness)
        {
            if (radius < 1) radius = 1;
            if (thickness < 1) thickness = 1;
            var key = (radius, thickness);
            if (_ringCache.TryGetValue(key, out var existing) && existing != null && !existing.IsDisposed) return existing;

            int d = radius * 2;
            var tex = new Texture2D(_graphicsDevice, d, d);
            var data = new Color[d * d];

            float outerRadius = radius - 0.5f;
            float innerRadius = Math.Max(0f, radius - thickness) + 0.5f;
            float smooth = 1.0f;

            for (int y = 0; y < d; y++)
            {
                float dy = y - radius + 0.5f;
                for (int x = 0; x < d; x++)
                {
                    float dx = x - radius + 0.5f;
                    float dist = MathF.Sqrt(dx * dx + dy * dy);

                    float outerAlpha;
                    if (dist <= outerRadius - smooth) outerAlpha = 1f;
                    else if (dist >= outerRadius + smooth) outerAlpha = 0f;
                    else outerAlpha = 0.5f + 0.5f * (outerRadius - dist) / smooth;

                    float innerAlpha;
                    if (dist <= innerRadius - smooth) innerAlpha = 1f;
                    else if (dist >= innerRadius + smooth) innerAlpha = 0f;
                    else innerAlpha = 0.5f + 0.5f * (innerRadius - dist) / smooth;

                    float ringAlpha = outerAlpha - innerAlpha;
                    if (ringAlpha < 0f) ringAlpha = 0f;
                    if (ringAlpha > 1f) ringAlpha = 1f;

                    byte A = (byte)MathHelper.Clamp((int)Math.Round(ringAlpha * 255f), 0, 255);
                    data[y * d + x] = Color.FromNonPremultiplied(255, 255, 255, A);
                }
            }

            tex.SetData(data);
            _ringCache[key] = tex;
            return tex;
        }

        private class BreakAnim
        {
            public Vector2 Position;
            public int Value;
            public float Elapsed;
        }

        private class GainAnim
        {
            public int QueueIndex;
            public float Elapsed;
            public bool Used;
        }
    }
}
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build`
Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add ECS/Scenes/BattleScene/GuardQueueDisplaySystem.cs
git commit -m "add GuardQueueDisplaySystem with break and gain animations"
```

---

### Task 6: Register Systems in BattleSceneSystem

**Files:**
- Modify: `ECS/Scenes/BattleScene/BattleSceneSystem.cs`

Three changes needed: (1) field declarations, (2) instantiation, (3) AddSystem calls, (4) Draw call.

- [ ] **Step 1: Add field declarations**

After the `_recoilDisplaySystem` field declaration (around line 112), add:

```csharp
		private GuardManagementSystem _guardManagementSystem;
		private GuardQueueDisplaySystem _guardQueueDisplaySystem;
```

- [ ] **Step 2: Add system instantiation**

After `_recoilDisplaySystem = new RecoilDisplaySystem(...)` (around line 540), add:

```csharp
			_guardManagementSystem = new GuardManagementSystem(_world.EntityManager);
			_guardQueueDisplaySystem = new GuardQueueDisplaySystem(_world.EntityManager, _graphicsDevice, _spriteBatch);
```

- [ ] **Step 3: Add AddSystem calls**

After `_world.AddSystem(_recoilDisplaySystem);` (around line 656), add:

```csharp
			_world.AddSystem(_guardManagementSystem);
			_world.AddSystem(_guardQueueDisplaySystem);
```

- [ ] **Step 4: Add Draw call**

After `FrameProfiler.Measure("EnemyIntentPipsSystem.Draw", _enemyIntentPipsSystem.Draw);` (line 294), add:

```csharp
			FrameProfiler.Measure("GuardQueueDisplaySystem.Draw", _guardQueueDisplaySystem.Draw);
```

- [ ] **Step 5: Build to verify**

Run: `dotnet build`
Expected: Build succeeds. All systems are now registered and wired.

- [ ] **Step 6: Commit**

```bash
git add ECS/Scenes/BattleScene/BattleSceneSystem.cs
git commit -m "register GuardManagementSystem and GuardQueueDisplaySystem in BattleSceneSystem"
```

---

### Task 7: Add Sentinel Passive to Display Categories

**Files:**
- Modify: `ECS/Scenes/BattleScene/AppliedPassivesManagementSystem.cs`

The `Sentinel` passive type needs to be categorized so it persists correctly (battle passive, not removed at end of turn).

- [ ] **Step 1: Find passive category methods**

In `AppliedPassivesManagementSystem.cs`, find `GetBattlePassives()` method. Add `AppliedPassiveType.Sentinel` to the returned HashSet.

- [ ] **Step 2: Build to verify**

Run: `dotnet build`
Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add ECS/Scenes/BattleScene/AppliedPassivesManagementSystem.cs
git commit -m "categorize Sentinel as battle passive in AppliedPassivesManagementSystem"
```

---

### Task 8: Verification

- [ ] **Step 1: Full build**

Run: `dotnet build`
Expected: Clean build, no warnings related to guard/sentinel.

- [ ] **Step 2: Run the game**

Run: `dotnet run`

To test, start a battle against the Sentinel enemy. Verify:
1. Guards appear above intent pips during PreBlock phase (some attacks get reduced, some fully converted)
2. Playing attack cards against the enemy breaks guards with animation
3. Guards remaining at enemy turn start convert to aggression (1 per guard)
4. When an attack is fully converted, it's skipped and the next attack proceeds
5. When all attacks are fully converted in a turn, the turn ends and transitions to player turn

- [ ] **Step 3: Commit any final fixes if needed**
