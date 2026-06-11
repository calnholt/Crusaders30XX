# Printed color is identity; combat qualification is contextual

## Decision

`CardData.CardColor` remains the immutable printed identity used by deck construction, rewards, card variants, and save keys.

Colorless is represented by a run-long card restriction component. Combat systems ask `CardColorQualificationService` whether a card currently qualifies as Red, White, or Black instead of extending `CardColor` with a fourth value.

Colorless cards have no qualified color. They remain eligible for `Any` costs because `Any` accepts a card regardless of its current specific-color qualification.

## Consequences

- Existing card keys and reward/deck data remain stable.
- Removing Colorless restores printed-color behavior without reconstructing the card.
- New combat color predicates must use `CardColorQualificationService`.
- Systems concerned only with identity, deck construction, rewards, or persistence continue reading `CardData.Color`.
