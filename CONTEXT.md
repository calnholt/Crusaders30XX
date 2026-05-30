# Crusaders30XX — domain glossary

Terms only. No implementation details.

## Run

A single playthrough from a fresh (or reset) save through the procedurally generated quest map. One save file holds exactly one run in v1.

## Save

Persistent player state for the current run: map topology, node progress, gold, collection, loadouts, and related meta. Starting a "new run" (future) will replace or reset this state; until then, map generation happens once per save.

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

On first completion of a quest node: flat gold (10) and one random card added to collection/loadout. No rewards on repeat attempts (replays disabled).
