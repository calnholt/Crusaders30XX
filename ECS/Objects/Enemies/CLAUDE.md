# Enemy Design Guidelines

For detailed design philosophy and rationale, see `DESIGN_PHILOSOPHY.md`.

## Core Principles

- **Restrict, don't remove** - Afflicted cards should still do something
- **Cure through normal play** - No dedicated "undo" actions required
- **The afflicted participates in rescue** - Frozen card blocking clears freeze
- **Layer, don't override** - New mechanics interact with existing ones
- **Decisions emerge from state** - No explicit "Choose A or B" prompts
- **Passives must change what the player DOES** - Not just damage numbers

## Quick Reference

### Health Ranges

| Tier | HP | Examples |
|------|----|----------|
| Fragile | 40-60 | Gleeber (50), SkeletalArcher (48) |
| Standard | 70-90 | Spider (80), Ninja (80), Demon (90) |
| Tough | 95-120 | Sorcerer (120), GlacialGuardian (110) |
| Boss | 120+ | Shadow (150) |

### Damage Ranges

| Tier | Damage | Role |
|------|--------|------|
| Chip | 1-4 | Setup, condition delivery |
| Standard | 5-7 | Core pressure |
| Heavy | 8-11 | Enders, punishes |
| Devastating | 12+ | Boss moves, needs counterplay |

### Burn Guidelines

- Player has 20 HP, burn deals X damage/turn
- 2-3 burn per attack is meaningful; 4+ needs clear counterplay
- 5 burn = 4 turn death clock

### Pattern Examples

| Pattern | Use When | Examples |
|---------|----------|----------|
| Single attack | Identity from WHAT the attack does | `Demon.cs`, `Sorcerer.cs`, `Berserker.cs` |
| Linker + Ender | Multiple threats to manage | `Spider.cs`, `Ogre.cs`, `Succubus.cs` |
| Multi-jab | Accumulated pressure | `Ninja.cs`, `Skeleton.cs` |
| Alternating | Predictable planning | `SandGolem.cs`, `Shadow.cs` |

---

## Creating a New Enemy

1. Create `ECS/Objects/Enemies/YourEnemy.cs`
2. Inherit from `EnemyBase`, define attacks inheriting from `EnemyAttackBase`
3. Register enemy in `EnemyFactory.cs`
4. Register attacks in `EnemyAttackFactory.cs`

---

## Design Checklist

1. **What's the identity?** (one sentence)
2. **What decision does the player make?** (beyond "block the big attack")
3. **Attack pattern?** (single/linker+ender/multi-jab/alternating)
4. **Health tier?** (fragile/standard/tough)
5. **Scaling?** (does the enemy get worse if fight drags?)
6. **Conditions?** (1-2 max, reinforce identity)
7. **Counterplay?** (smart play should mitigate)

---

## Common Conditions

### Applied to Player

| Condition | Frequency | Role |
|-----------|-----------|------|
| Burn | Very common | Sustained damage |
| Penance | Common | Punishes behaviors |
| Intimidate | Common | Restricts plays |
| Frozen | Moderate | Disables cards |
| Corrode | Rare | Degrades blockers |

### Applied to Enemy (Self-Buffs)

| Condition | Role |
|-----------|------|
| Armor | Damage reduction |
| Channel | Attack scaling |
| Power | Damage scaling |
| Thorns | Reflection damage |
