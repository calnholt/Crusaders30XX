# Enemy Brainstorm

## Fire Theme

### Overheat
"If you blocked with 3+ cards last turn, this attack deals +X damage."

Creates pressure to "run cool" - don't over-commit blocks. Changes how many cards you play defensively.

### Ignition
"On reveal: Your burn triggers again immediately."

Creates urgency to heal *before* this attack appears, not after. Changes turn sequencing and when you prioritize healing.

### Flashpoint
"Deals damage equal to cards in your hand."

Creates dump-your-hand pressure. Conflicts with saving cards for later turns.

### Slow Burn
"If you didn't deal damage last turn, +X damage/effect."

Punishes pure defense turns. Forces aggression even when you want to turtle.

### Thermal Momentum
"If you fully blocked this enemy last turn, +X damage/effect."

Punishes playing too safe. Eventually you MUST let some damage through.

### Scorched
"Red and white cards block for half value but gain double courage/temperance."

Trade-off, not just a penalty. You might want to block with those cards for the passive gain even though they're worse blockers. Changes card evaluation.

### Fury (tracking mechanic)
Enemy gains Fury for each card the player plays. Effect TBD.

Creates tension around playing lots of cards - big combo turns have a cost.

### Burn-to-Power (kernel)
You gain power (or some benefit) equal to burn damage you take each turn.

Burn becomes fuel. Changes whether you want to heal or ride the burn for value.

### Backdraft
"If you have X+ cards in hand, Y."

Threshold-based hand size punishment. Simpler than scaling - creates a clear "dump below X" pressure point. Player knows exactly when they're safe vs in danger.

### Doused
"Deals X damage, -1 for each burn stack on you."

Burn becomes armor against this attack. Creates tension: do you heal the burn (and take more from this), or ride the burn for protection? Inverts the usual "burn is bad" assumption.

### Spreading Flames
"This attack gains +1 damage for each card that blocks it."

Punishes over-blocking. Throwing multiple small cards at it makes it worse. Forces commitment to a single big blocker or accepting damage.

### Smolder
"At the start of action phase, gain 1 Smolder. At 3 Smolder: take 1 damage, reset to 0."

Inevitable ticking clock that doesn't interact with blocking at all. Slow, steady pressure - every 3 turns you take 1 unavoidable damage. Creates background urgency to end the fight.

---

## Darkness Theme

### Hesitation
"Your first attack each turn deals half damage."

Blunts your opening strike. Changes attack sequencing - do you throw away a weak attack first to "clear" the debuff, or accept reduced damage on your big hit? Makes attack order matter.

### Nightmare (Card)
"2 Block. Exhaust on play or block. If [hyper-conditional], gain [strong effect]."

Deck pollution that isn't pure dead weight. It's a bad card - 2 block is weak, and it clogs your draws. But it exhausts when used, so it cycles out. The hyper-conditional upside creates a mini-game: do you hold it hoping to hit the condition, or just dump it for 2 block and move on?

### Leech
"Heals equal to damage dealt."

Simple sustain threat. Chip damage now extends the fight - every point you let through undoes your progress. Full blocks matter more. Changes the "let 2 through" calculus.

---

## Seal (Medusa Theme)

### Core Mechanic
**Sealed** cards:
- Cannot be played
- Cannot be pledged
- CAN block
- Persists across battles until freed

### Crack System
Sealed cards have 0-3 cracks. **Cracks only accumulate while the card is in hand:**
- +1 crack when this card is used to block
- +1 crack for each card you play (applies to ALL sealed cards in hand)
- At 3 cracks: remove Sealed, card is freed

### Key Interactions
| Interaction | Ruling |
|-------------|--------|
| Multiple sealed cards in hand | All gain +1 crack per card played |
| Freed mid-block | Completes block normally, goes to discard freed |
| Pledged cards | Cannot become sealed (immune) |
| Stacking (Frozen, Shackled) | Can stack - card can be Sealed AND Frozen |
| Forced exhaust/discard | Can target sealed cards; stays sealed in exhaust pile |
| In draw/discard pile | Stays sealed, but no crack accumulation |

### Why It's Interesting
- **Not fully dead**: Can still block, which chips away at the curse
- **Activity = freedom**: Aggressive play breaks stone faster; turtling stays cursed
- **Draw matters**: Must actually draw the card to make progress
- **Quest persistence**: Medusa leaves her mark - you walk away cursed
- **Discard tension**: Discarding sealed card pauses progress
- **Exhaust = disaster**: Sealed card in exhaust pile is nearly unfreeable

### Medusa Attack Concepts
- **Gaze** (4 dmg): On hit, seal 1 random card from hand
- **Stone Stare** (6 dmg): On reveal, seal top card of draw pile
- **Basilisk Glare** (3 dmg): On hit, shuffle a sealed card from hand into draw pile (delays progress)
- **Serpent Strike** (7 dmg): On hit, all sealed in your deck cards lose 1 crack (undoes progress)

---

## Wyvern Theme

### Plunder (Passive)
**Triggers**: Enemy preblock phase each turn
**Effect**: Wyvern grabs a random card from the player's deck. The plundered card is visible to the player.

- If the player deals X damage to the Wyvern this turn → plundered card goes to player's hand (bonus card!)
- If the player doesn't hit the threshold → plundered card is discarded, new card grabbed next turn

### Why It's Interesting
- **Positive-feeling mechanic**: It's a rescue mission, not a debuff
- **Turn-by-turn evaluation**: Each turn you decide if the current card is worth fighting for
- **Risk/reward scaling**: Good card grabbed = go aggressive, take hits. Bad card = play safe, let it go
- **Deck matters**: Since no reshuffle, every discarded card is a real loss
- **Damage threshold creates tension**: Must commit to offense over defense to rescue

### Key Decisions
| Grabbed Card | Player Tendency |
|--------------|-----------------|
| Key card | Overextend, take risks to rescue |
| Decent card | Weigh damage taken vs. card value |
| Bad/situational card | Play safe, let it go, hope for better next turn |

### Open Questions
- **Damage threshold**: TBD - needs to be achievable but require offensive commitment
- **Empty deck**: What happens when deck runs out? Passive stops? Grabs from discard?



---

## Blood Martyr Theme

### Core Fantasy
Masochistic cultist who weaponizes suffering. Hurts itself, manipulates its own defenses, and curses you for hitting too hard.

### Sanguine Curse (Passive)
**Triggers**: When you deal 6+ damage in a single attack
**Effect**: Gain 4 bleed

The threshold is affected by the enemy's own Wounded and Armor stacks:
- `actual damage = base damage + wounded - armor`
- Player must constantly recalculate whether their attacks are "safe"

### Why It's Interesting
- **Changes what you DO**: Instead of maximizing damage, you're evaluating "is this hit worth the curse?"
- **Dynamic puzzle**: The martyr's attacks shift its own Armor/Wounded, changing the math each turn
- **Self-harm theme**: Burns itself, creating urgency (it's slowly dying) while pressuring you
- **Multiple viable strategies**: Chip it down safely, burst and accept bleed, or time your big hits when Armor is high

### Attacks

| Attack | Damage | Self-Effect | Role |
|--------|--------|-------------|------|
| **Flagellation** | 3 | Gains 2 burn, 1 Wounded | Self-harm, becomes more vulnerable |
| **Blood Ward** | 3 | Gains 2 Armor | Protection, becomes tankier |
| **Blood Tithe** | 5 | None (+2 if you have bleed) | Punish attack |

### Attack Pattern
**Weighted random** - each attack is a fresh reveal, forcing reassessment:
- After Flagellation: Wounded up → your hits more likely to cross threshold
- After Blood Ward: Armor up → your hits safer
- Blood Tithe: Pure pressure, punishes existing bleed

### Player Decision Tree
| Situation | Consideration |
|-----------|---------------|
| High Wounded, low Armor | Chip attacks only, or accept the curse for burst |
| High Armor, low Wounded | Window for heavy attacks |
| Already bleeding | Blood Tithe hurts more - prioritize blocking or killing fast |
| Martyr burning low | Can wait it out if your HP is stable |

### Stats
- **Health**: 80 (Standard tier)
- **Attack pattern**: Random selection each turn

### Open Questions
- **Threshold tuning**: Is 6 the right number? Could be 5 (tighter) or 7 (looser)
- **Bleed amount**: 4 bleed = 4 damage over 4 turns. Too harsh? Too mild?
- **Armor/Wounded caps**: Should there be maximums to prevent runaway stacking?


I lsot enemy idea of marking random card in hand, jumps to different card when used to block. playing marked card has negative attrivutbe. each jump changes attribute. ending turn without playing a marked card is even worse penalty