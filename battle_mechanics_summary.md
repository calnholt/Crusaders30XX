# Crusaders 30XX – Battle Mechanics Summary

## 🔷 Core Battle Loop Mechanics

### 📌 General Structure
- Single-player deckbuilding combat game.
- Player equips a 15-card deck, a weapon, and armor (head, chest, arms, legs).
- Battles are turn-based and typically last ~5 turns.

### 📋 Turn Order
1. **Enemy Turn**
   - Enemy reveals a pattern of 1–3 attack pips per turn.
   - The player can see the current attack pip sequence, and the enemy's next attack pip sequence.
   - Each pip is color-coded: red gradient for strength (darker = stronger); blue for utility
   - Player sees pip order and colors.
   - Each pip has damage, on-damage effects, and block pip conditions (e.g., "block with 2 blue cards to prevent all damage").

2. **Block Phase**
   - Player assigns cards or armor to block enemy attacks.
   - Red and White cards have block value of 3; Blue cards have block value of 6.
   - Enemy attacks will have preventable conditions if cards are used to block.
      - EX: block with any color to prevent receiving 2 burn; block with 2 blue cards to prevent all damage; etc
   - Armor can block once per combat.
   - **Resource Generation**: When blocking with cards, gain resources based on card color:
     - Red cards → Courage
     - Black cards → No resource (higher block value instead)
     - White cards → Temperance
   - **Temperance Mechanic**: When you reach 3 Temperance points, immediately stun the enemy and reset Temperance to 0.
   - **Timing**: Resources gained from blocking are available immediately and can affect the same combat.
   - Unblocked damage reduces HP; some pip effects trigger only on partial or no block.

3. **Player Turn**
   - Player can play 1 action card unless it has "Go Again".
   - **Card Costs**: Cards require discarding other cards by color to play (e.g., Cost: Red means discard any red card).
   - **Multiple Costs**: Cards can require multiple discards (e.g., Cost: Red, Blue).
   - **Go Again**: Allows playing another action on the same turn. No limit to chaining Go Again effects.
   - Actions often scale with thresholds like Courage or Power.
   - **Weapon Attack**: Provides a default attack once per turn unless reset by cards like Sharpen Blade.
   - **Weapon Timing**: If weapon has Go Again, can be used multiple times per turn when reset.

4. **End of Turn**
   - Player draws up to 4 cards.
   - Block resets unless stated otherwise.
   - Some buffs/debuffs persist into the next turn.

## 🟥 Cards

### Card Properties
- `name: string`
- `color: 'Red' | 'White' | 'Black'` (determines blocking resource generation and block value)
- `cost: CardColor[]` (what you discard to play, can differ from card color)
- `blockValue: number` (3 for Red/White cards, 6 for Black cards)
- `isGoAgain: boolean`
- `play(gameState: GameState): void`
- `etc...`

### Card Functionality
- **Playing Cards**: Requires discarding other cards matching the cost colors.
- **Card Color vs Cost**: A card's color (for blocking resources) can differ from its cost (what you discard to play it).
- **Block Values**: Red and White cards block for 3, Black cards block for 6.
- Threshold effects activate at Courage levels.
- Some grant healing, bonus damage, stuns, marks, Temperance, etc.

## 🛡️ Blocking System

- Executed during the enemy turn using cards and armor.
- **Automatic Resource Generation** when blocking with cards:
  - Red cards → Courage
  - Black cards → No resource (higher block value: 6)
  - White cards → Temperance
- **Temperance Mechanic**: Accumulate 3 Temperance points to immediately stun the enemy.
- Some cards have `onBlock()` side effects.
- **Armor Blocking**: Can be used during Block Phase OR activated for effects during Player Turn.

### Block Evaluation
- Each enemy pip checks for:
  - Block pip condition met → status avoided.
  - Check for "on X damage" triggers.
  - Remaining damage → applied to player HP.

## 💥 Resources

| Resource | Source | Use | Notes |
|----------|--------|-----|-------|
| **Courage** | Red cards (blocking/effects), weapon attacks, passives | Unlock card bonus effects | Generally not spent, just checked for thresholds. Some cards explicitly consume Courage. |
| **Temperance** | White cards (blocking/effects), marked kills, passives | Accumulate 3 to stun enemy | Resets to 0 when 3 points are reached and enemy is stunned. Resets at end of battle. |

## ⚔️ Weapon System

- One default attack per turn based on equipped weapon.
- **Reset Mechanics**: Cards like Sharpen Blade can remove "Once per turn" restriction.
- Can be enhanced by cards and passives.

## 🛡️ Armor

- Slots: head, chest, arms, legs.
- **Usage**: Block during enemy turn OR activate for effects during player turn.
- **Costs**: Some armor effects require discarding cards (e.g., Arms: Cost Blue).
- **Destruction**: When armor says "destroy this", it's gone for the rest of the run.
- Provides blocking and additional passive/triggered benefits.

## Beyond Combat

This game plays like a roguelike / RPG adventure hybrid. You select different areas where you perform "runs". A run can be around 8 combat encounters along with some non-combat encounters. Different areas have different monsters, items to unlock, stories, characters, etc. The meta progression of the game is unlocking new armor, weapons, cards, passives that can be equipped before a run. Damage does not persist between battles.