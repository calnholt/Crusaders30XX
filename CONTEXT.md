# Crusaders30XX — domain glossary

Terms only. No implementation details.

## Run

A single playthrough from a fresh (or reset) save through the procedurally generated quest map. One save file holds exactly one run in v1.

## Save

Persistent player state for the current run: map topology, node progress, gold, loadouts, and related meta. Starting a new run replaces or resets this state.

The save file has a `version` field. If it does not match the game's current save version, the entire file is replaced with a new default save. There is no migration between versions. Omitting `version` or using an old version number clears all progress (map, gold, deck, mastery, achievements, tutorials).

## Loadout

The player's configured battle kit for the run: deck card list, weapon, temperance ability, equipment slots, and medals. Shop card purchases append a card entry to the deck list immediately. Quest rewards do the same. There is no separate owned-cards list.

## Deck

The ordered set of card entries (card plus color) on the loadout used in battle. A new run begins with a 20-card starting deck; the deck may grow beyond 20 during the run. There is no maximum deck size.

## Starter pool

The fixed set of card identities used only when rolling a new run's starting deck. Cards earned or bought during a run do not add to the starter pool for future runs.

## Starting deck

The 20-card deck assigned at new-run creation by random selection from the starter pool, subject to color balance and copy limits. Persisted on the loadout until the player edits it.

## Copy limit

A deck may include at most one copy of a given card identity and color pairing, and at most two copies of the same card identity across all colors.

## Location (desert)

The desert is a presentation wrapper for the run map (background, title). It is not a separately authored level graph in v1. The world map scene is bypassed in v1; the run map (location scene) is the hub after the title menu.

## Quest node

A single battle POI on the run map. Completing it marks the node completed and may reveal child nodes.

## Node completion

Whether the player has won the battle at that quest node. Stored only on the run map in save v1; not a separate global quest-id list. Completed nodes remain visible on the map but cannot be started again in v1.

## Node reveal

A node becomes fightable when `isRevealed` is true. Reveal happens when its parent is completed (all children revealed at once). The player may tackle any revealed, incomplete node; branches are not mutually exclusive.

## Map fog

Unrevealed nodes are hidden and cannot be interacted with. Fog radius around revealed/completed nodes is visual only and must not expose unrevealed nodes early.

## Quest reward

On first completion of a quest node: flat gold (10) and one random card added to the loadout deck. No rewards on repeat attempts (replays disabled).
