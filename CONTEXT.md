# Crusaders30XX — domain glossary

Terms only. No implementation details.

## Run

A single playthrough from a fresh (or reset) save through the procedurally generated quest map. One save file holds exactly one run in v1.

## Run failure

Losing a battle (v1: only in battle). The current run ends only after the game-over sequence finishes, when run state on disk is replaced with a new run and the player returns to the title screen. Until then, the failed run's progress still exists in the save file. Meta earned during that fight is kept.

## Quest abandon

Voluntarily ending the current run from the in-battle quit overlay (prompt: "Abandon run?"). Uses the same game-over sequence as run failure, then replaces run state on disk and returns to the title screen.

## Meta

Progress that survives run failure and new-run creation: achievements, card mastery, and seen tutorials.

## Save

Persistent file for the active run plus meta. Run state includes map topology, node progress, gold, loadouts, and inventory. Starting a new run replaces run state only; meta is kept.

The save file has a `version` field. If it does not match the game's current save version, the entire file is replaced with a new default save. There is no migration between versions. Omitting `version` or using an old version number clears all progress (map, gold, deck, mastery, achievements, tutorials).

## Loadout

The player's configured battle kit for the run: deck card list, weapon, temperance ability, equipment slots, and medals. Shop card purchases append a card entry to the deck list immediately. Quest rewards do the same. There is no separate owned-cards list.

## Deck

The player's deck for the current run: the ordered set of card entries (card plus color) on the loadout, plus how those cards are distributed in battle (draw pile, hand, discard). A new run begins with a 20-card starting deck; the deck may grow beyond 20 during the run. There is no maximum deck size.

## Exhaust

Removing a card from the deck for the rest of the run. Exhausted cards do not return in later battles. Exhaustion removes that card entry from the save loadout and destroys the run deck entity.

## Inter-battle deck reset

At the start of each battle, all surviving cards (draw pile, hand, and discard) combine into one draw pile, which is then shuffled. Exhausted cards are not included.

## Starter pool

The fixed set of card identities used only when rolling a new run's starting deck. Cards earned or bought during a run do not add to the starter pool for future runs.

## Starting deck

The 20-card deck assigned at new-run creation by random selection from the starter pool, subject to color balance and copy limits. Persisted on the loadout until the player edits it.

## Copy limit

A deck may include at most one copy of a given card identity and color pairing, and at most two copies of the same card identity across all colors. Quest rewards and other non-shop sources follow this limit.

Shop purchases are an exception: buying from a run-map shop may add a card entry even when it would exceed the normal copy limit. Each shop listing can still only be bought once per run (`isPurchased` on that slot).

## Location (desert)

The desert is a presentation wrapper for the run map (background, title). It is not a separately authored level graph in v1. The world map scene is bypassed in v1. The location scene is the mid-run hub once at least one quest node is completed; a brand-new run starts from the title screen straight into the first quest battle.

## New run start

Entry from the title screen after run failure or on first launch. The player begins the first available quest battle without visiting the location hub until at least one node is completed.

## Quest node

A single battle POI on the run map. Completing it marks the node completed and may reveal child nodes. On the location hub, quest node and POI refer to the same map marker in v1.

## Root quest node

The first quest node in a run. It is the player's initial battle and the anchor of the procedural map tree. Its position is near the desert map center with random offset so it is not always at the exact geometric center.

## Run map coverage

How widely quest nodes are distributed across the playable desert. Good coverage means the player must pan and zoom to see the whole run; nodes should not sit in one tight cluster. Uneven blobs are acceptable; symmetry across quadrants is not required.

## Node completion

Whether the player has won the battle at that quest node. Stored only on the run map in save v1; not a separate global quest-id list. Completed nodes remain visible on the map but cannot be started again in v1.

## Node reveal

A node becomes fightable when `isRevealed` is true. Reveal happens when its parent is completed (all children revealed at once). The player may tackle any revealed, incomplete node; branches are not mutually exclusive.

## Map fog

Unrevealed quest nodes are hidden and cannot be interacted with. Fog radius around revealed/completed nodes is visual only and must not expose unrevealed quest nodes early.

Shop markers are an exception: their map icon and minimap dot are visible from the start of the run. A shop becomes enterable when the fog circle of any **revealed or completed** quest node covers the shop's world position (same radius rules as quest fog). Shops do not add their own fog-clear circles. Unrevealed shops (not yet inside any quest fog) do not show a tooltip or enter prompt on hover.

## Shop (run map)

A card vendor on the desert run map. Three shops exist per run. Shops are **not** quest nodes: they have no parent/child links in the battle tree and do not replace any of the 20 combat nodes.

After the 20-node battle map is generated, each shop is placed at a world position within completed-quest fog range of at least one battle node (so completing that battle can unlock the shop). Each shop has three fixed listings (identity, color, price) generated at run creation and stored in save.

## Shop reveal (enterable)

A shop becomes enterable when its world position lies inside the fog circle of at least one **completed** quest node (full reveal radius). Merely revealing a nearby quest without completing it is not enough.

The same card identity may appear in more than one shop in a run; within one shop the three listings use distinct identities and distinct colors.

_Avoid_: Shop node (when meaning the map marker; prefer **shop** vs **quest node**)

## Quest reward

On first completion of a quest node: flat gold (10) and one random card added to the loadout deck. No rewards on repeat attempts (replays disabled).

## Diagnostics

**Run map generator log**:
An append-only text log written in debug builds when a new run map is created. Each line summarizes spread for one generation (seed, coverage stats). The log is cleared when the map generator version integer is bumped in code.
_Avoid_: Spread log, map debug file

**Profiled game frame**:
One full game loop iteration: Update phase then Draw phase. Profiler stats for draw and update attach to the same frame id.
_Avoid_: Draw frame, profiler frame

**Performance report**:
A plain-text session summary written when the developer quits with Shift+Escape. Lists timing stats per instrumented scope plus frame-level totals.
_Avoid_: Profiler dump, perf log

**Unaccounted time**:
Wall-clock time in a profiled game frame not explained by leaf instrumented scopes (shader passes, present, uninstrumented code). Parent/inclusive scopes are listed separately and do not count toward this bucket.
_Avoid_: Mystery time, overhead
