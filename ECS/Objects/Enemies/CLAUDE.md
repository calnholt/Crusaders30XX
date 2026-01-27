# Enemy Design Guidelines

## Damage and Effect Relationships

When designing enemy attacks, always consider how the base damage interacts with any special effects:

- **On Hit effects** require enough base damage that the player faces a meaningful blocking decision. If the damage is too low, the attack will always be fully blocked and the effect will never trigger.

- **Conditional effects based on blocking behavior** (e.g., "if blocked by 2+ cards") must make sense given the base damage. Ask: would a player ever realistically meet this condition? A 1-damage attack will never be blocked by multiple cards.

- **Think through the player's decision tree** - what choices does the attack actually present? Effects that the player can trivially avoid aren't interesting.

## Favor Simplicity

When conveying an attack concept, prefer simpler mechanics:

- `OnAttackReveal` for guaranteed effects (e.g., deal damage when the attack is revealed) is often cleaner than complex damage modification systems
- Avoid mechanics that bypass core systems (like block) entirely - they remove player agency without adding interesting decisions
