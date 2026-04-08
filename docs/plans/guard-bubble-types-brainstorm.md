# Guard Bubble Types Brainstorm

## Context

The guard system is currently gated behind the Sentinel passive — only the Sentinel enemy uses guards. The goal is to make the guard queue a **universal slot** any enemy can have, with guards added through various sources (passives, abilities, other enemies, etc.). Individual pips in the queue each have a **type** that determines their break condition and behavior.

## Core Mechanic

- Attacks flow through the guard queue automatically — damage hits the first pip
- If the pip's **break condition** is met, it breaks (absorbing damage equal to its value)
- If the break condition is **not met**, the attack is deflected (damage wasted)
- At EnemyStart, remaining guard pips convert to **Aggression** (unless the type specifies otherwise)

## Guard Bubble Types

### Standard (Blue)
- Breaks with any attack
- Absorbs damage equal to its value
- Converts to 1 Aggression at EnemyStart
- The baseline — no puzzle, just a speed bump

### Chromatic (Red / White / Black)
- Only breaks when hit with a **matching color** attack card
- Wrong color deflects
- Converts to 1 Aggression at EnemyStart
- **Tests:** Hand composition — "I need my Red cards for blocking, but that Red guard is sitting there"
- Three visual variants (one per card color), same mechanic

### Hardened (Gray)
- Only breaks when hit with an attack dealing **>= its pip value** in damage
- Weak attacks bounce off
- Converts to 1 Aggression at EnemyStart
- **Tests:** Attack strength — forces committing a strong attack to break through

### Thorned (Green)
- Breaks with any attack
- When broken, applies a **debuff** to the player (Wound, Burn, etc. — can vary per enemy)
- Converts to 1 Aggression at EnemyStart
- **Tests:** Risk tolerance — the classic leave-or-break dilemma

### Volatile (Orange)
- Value grows by **+1 each turn** it sits in the queue
- Breaks with any attack
- Converts to Aggression equal to its (growing) value at EnemyStart
- **Tests:** Timing — rewards breaking early, punishes ignoring

### Phantom (Purple)
- **Cannot be broken by attacks**
- Disappears when the paired enemy attack is **fully blocked**
- Converts to 1 Aggression at EnemyStart
- **Tests:** Blocking skill — ties guard removal to blocking performance

### Reflective (Silver)
- When broken, deals **damage equal to the pip's value** back to the player
- Converts to **nothing** at EnemyStart (just disappears)
- **Tests:** Cost/benefit — it's *safe* to leave and *dangerous* to break

### Brittle (Teal)
- Breaks with any attack
- Excess damage **bleeds through to the enemy's HP** instead of hitting the next pip
- Converts to 1 Aggression at EnemyStart
- **Tests:** Attack selection — rewards overkilling this pip with a big attack to sneak HP damage through the guard queue

### Absorbing (Dark Blue)
- Breaks with any attack
- Gains **+1 value** each time the player plays a Prayer card while this pip is in the queue
- Converts to 1 Aggression at EnemyStart
- **Tests:** Action economy — punishes buffing while this pip is up, pressures the player to break it before setting up

## Decision Matrix

| Type | Tests... | Break? | Leave? |
|------|----------|--------|--------|
| Standard | Nothing — baseline | Safe | Aggression |
| Chromatic | Hand composition | Need right color | Aggression |
| Hardened | Attack strength | Need big hit | Aggression |
| Thorned | Risk tolerance | Take debuff | Aggression |
| Volatile | Timing | Easy now, hard later | Growing Aggression |
| Phantom | Blocking skill | Can't — must block attack | Aggression |
| Reflective | Cost/benefit | Take damage | Safe (disappears) |
| Brittle | Attack selection | Overkill bleeds to HP | Aggression |
| Absorbing | Action economy | Safe, but break before buffing | Growing Aggression |

## How Guards Change Attack Card Design

### The Core Shift

Damage is no longer guaranteed to reach HP. Attack card value becomes **contextual** — dependent on board state, not just stats. This creates:
- **Situational value** — cards that are great in some fights and mediocre in others
- **Deck diversity pressure** — you can't just stack the highest-damage cards
- **Resource allocation tension** — attacks are finite per turn, split between clearing guards (prevention) and hitting HP (progress)

### New Attack Keywords

- **Pierce** — bypasses the guard queue entirely, hits HP directly. Should be rare/expensive to avoid undermining the guard system.
- **Shatter** — breaks any guard regardless of break condition. Utility, not power.
- **Overflow** — excess damage after breaking a guard continues to the next target (next pip or HP).
- **Multi-hit** ("Deal X damage N times") — each hit resolves separately against the queue. Efficient at breaking multiple small guards, terrible against Hardened.

### Conditional Effect Patterns

- **"If this hits HP..."** — powerful effect gated behind guard penetration
- **"If this breaks a guard..."** — rewards targeting guards, turns clearing into card advantage
- **"If deflected..."** — consolation prize, card isn't wasted when guards block it
- **"If this deals X+ damage to HP..."** — threshold gate, needs to get through guards AND have enough leftover

## Attack Card Ideas

### Guard Breakers

**Rapid Strikes** — Cost: [Red] | Damage: 1 x3 | Block: 2
"Deal 1 damage 3 times." Each hit resolves separately against the queue. Efficient at chewing through small pips, useless against Hardened.

**Sunder** — Cost: [Black, Any] | Damage: 3 | Block: 0 | Weapon
"Shatter. Deal 3 damage." Breaks any guard regardless of break condition. Universal answer — but it's a weapon, so one per turn.

**Prismatic Strike** — Cost: [Any] | Damage: 2 | Block: 1
"This attack counts as all colors. Deal 2 damage." Solves Chromatic guards cheaply, but low damage means it bounces off Hardened.

### HP Seekers

**Phantom Blade** — Cost: [Red, Black] | Damage: 4 | Block: 0 | Weapon
"Pierce. Deal 4 damage." Bypasses the guard queue entirely. Expensive cost, can't block with it. High payoff when guards are stacked, dead weight when they're not.

**Executioner's Strike** — Cost: [Black, Any] | Damage: 5 | Block: 0 | Weapon
"Deal 5 damage. If this hits HP, apply 2 Wound." The Wound is the real prize, but only fires if guards are already cleared. Rewards setup turns.

### Adaptive

**Calculated Strike** — Cost: [Any] | Damage: 3 | Block: 2
"Deal 3 damage. If this breaks a guard, draw 1. If this hits HP, gain 1 Courage." Always gives you something. Never amazing, never wasted.

**Reckless Swing** — Cost: [Red] | Damage: 4 | Block: 1
"Overflow. Deal 4 damage." Excess damage after breaking a guard continues to the next target. A 4-damage hit on a 1-value Standard guard pushes 3 damage forward.

### Guard Exploiters

**Feedback** — Cost: [Any, Any] | Damage: conditional | Block: 2
"Deal 1 damage per guard pip in the queue." Turns heavy guard setups into a liability. A 5-pip queue means 5 damage to the first pip.

**Reversal** — Cost: [Any] | Damage: 0 | Block: 1
"Remove the first guard pip. Deal its value as damage to the enemy." High-value pips become weapons. A Volatile guard that's been growing for 3 turns is a huge hit.

**Dismantle** — Cost: [Red, Any] | Damage: 2 | Block: 1
"Deal 2 damage. If this breaks a guard, apply Burn equal to the guard's value." Converts guard investment into DoT. The bigger the pip, the more burn.

### Conditional / Threshold

**Heavy Blow** — Cost: [Black, Any, Any] | Damage: 6 | Block: 0 | Weapon
"Deal 6 damage. If this deals 4+ damage to HP, apply Stun." Needs to both get through guards AND have enough left over. Against a 2-value guard, only 4 reaches HP — just barely triggers.

**Probing Strike** — Cost: free | Damage: 2 | Block: 1 | Free Action
"Deal 2 damage. If deflected, gain 1 Power." Safe to throw at a Chromatic guard you can't match — you get Power either way.

**Shieldbreaker's Fury** — Cost: [Red] | Damage: 1 + conditional | Block: 0
"Deal 1 damage. +2 for each guard broken this turn." Play this LAST after clearing guards. A guard-clearing turn of 3 pips makes this a 7-damage finisher.

### Weird / Creative

**Fracture** — Cost: [Black] | Damage: 3 | Block: 2
"Deal 3 damage. If this breaks a guard, split the next pip into two pips at half value." Turns a Hardened 6 into two 3s you can actually break. Softens the queue for follow-up attacks.

**Absorb Strike** — Cost: [White, Any] | Damage: 2 | Block: 2
"Deal 2 damage. If this breaks a guard, gain Aegis equal to the guard's value." Stealing the enemy's defense. Breaking a 4-value guard gives you 4 Aegis.

**Detonate** — Cost: [Red, Red] | Damage: 0 | Block: 0
"Destroy all Volatile guards. Deal their total value as damage to the enemy." The anti-Volatile bomb. Let them grow, then pop them all at once.
