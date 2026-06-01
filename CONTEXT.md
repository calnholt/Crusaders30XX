# Crusaders30XX — domain glossary

Terms only. No implementation details.

## Run

A single playthrough from a fresh (or reset) save through the procedurally generated quest map. One save file holds exactly one run in v1.

## Run-long applied passive

A debuff or status on the player that lasts for the whole **run**: it survives leaving battle, visiting the location hub, shopping, and starting other **quest nodes**, until **run failure**, **quest abandon**, or **new run start**. Frostbite is the first run-long applied passive.

_Avoid_: Quest passive (when meaning run-long; use **run-long applied passive**)

## Quest-scoped applied passive

A debuff or status on the player tied to the current **quest node** attempt: it persists across **queued encounters** within that node, but is removed when the player leaves the battle scene to the location hub, shop, or other non-battle scene. It is not written to the save file.

Examples: Penance, Shackled, Bleed (see implementation list `GetQuestPassives`).

_Avoid_: Quest passive (when meaning run-long; use **run-long applied passive**)

## Queued encounter

One enemy fight in the sequential battle queue for a **quest node** (`QueuedEvents` advances one step per **queued encounter**). A quest node may have several queued encounters before the player returns to the location hub.

**Temperance** built during one queued encounter carries to the next encounter in the same quest node. **Temperance** resets to zero when the player leaves the battle scene (e.g. return to the location hub). Courage, HP, and action points reset at the start of each queued encounter.

When card or medal text says **win a battle**, that means winning a **queued encounter** (the enemy's **defeat presentation** has finished and the battle advances), not completing the whole **quest node**. Reaching 0 HP starts **defeat presentation**; the encounter is not won until that presentation ends.

## Defeat presentation

The on-screen sequence when a combatant is reduced to 0 HP before the battle advances. For enemies in v1, this is the pixel-burst animation: the enemy **portrait** is hidden and replaced by the burst; other enemy UI (HP bar, intents, passives) may remain visible until the presentation ends. When it completes, the game runs the normal post-kill flow (achievements, next queued encounter, or quest completion). During defeat presentation the player cannot take battle actions (cards, end turn, equipment). Player defeat presentation is separate (game-over overlay).

_Avoid_: Death animation (ambiguous with attack animations or game-over)

_Avoid_: Battle node (ambiguous with **quest node** on the map)

## Run failure

Losing a battle (v1: only in battle). The current run ends only after the game-over sequence finishes, when run state on disk is replaced with a new run and the player returns to the title screen. Until then, the failed run's progress still exists in the save file. Meta earned during that fight is kept.

## Quest abandon

Voluntarily ending the current run from the in-battle quit overlay (prompt: "Abandon run?"). Uses the same game-over sequence as run failure, then replaces run state on disk and returns to the title screen.

## Meta

Progress that survives run failure and new-run creation: achievements, card mastery, and seen tutorials.

## Save

Persistent file for the active run plus meta. Run state includes map topology, node progress, gold, loadouts, inventory, **run-long applied passive** stacks on the player, and **run-long card restriction** markers per deck card entry. Starting a new run replaces run state only; meta is kept.

The save file has a `version` field. If it does not match the game's current save version, the entire file is replaced with a new default save. There is no migration between versions. Omitting `version` or using an old version number clears all progress (map, gold, deck, mastery, achievements, tutorials).

## Loadout

The player's configured battle kit for the run: deck card list, weapon, temperance ability, equipment slots, and medals. Shop card purchases append a card entry to the deck list immediately. Quest rewards do the same. There is no separate owned-cards list.

## Deck

The player's deck for the current run: the ordered set of card entries (card plus color) on the loadout, plus how those cards are distributed in battle (draw pile, hand, discard). A new run begins with a 20-card starting deck; the deck may grow beyond 20 during the run. There is no maximum deck size.

## Exhaust

Removing a card from the deck for the rest of the run. Exhausted cards do not return in later battles. Exhaustion removes that card entry from the save loadout and destroys the run deck entity.

## Inter-battle deck reset

At the start of each battle, all surviving cards (draw pile, hand, and discard) combine into one draw pile, which is then shuffled. Exhausted cards are not included.

## Run-long card restriction

A combat effect attached to a specific deck card entity (e.g. frozen, shackled, sealed) that lasts for the whole **run** until **run failure**, **quest abandon**, or **new run start**. Restrictions survive leaving battle and hub visits; they are cleared when the run ends, not at each queued encounter.

## Quest-scoped card modification

A permanent change to a deck card's combat numbers (e.g. +1 damage from Carve, +1 block from Bulwark) for the current **quest node** attempt. It persists across **queued encounters** in that node and is removed on **node completion** when the player earns **quest reward**. It does not carry to other quest nodes or the hub.

_Avoid_: Rest of the quest (ambiguous with whole **run**; prefer **quest node** or **quest-scoped card modification**)

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

A single battle POI on the run map. Completing it marks the node completed and may reveal other quest nodes within map fog range. On the location hub, quest node and POI refer to the same map marker in v1.

## Root quest node

The first quest node in a run. It is the player's initial battle and the anchor for procedural placement. Its position is near the desert map center with random offset so it is not always at the exact geometric center.

## Run map tree

Internal parent/child links used only when generating the 20 quest positions. Not shown on the desert map in v1 and does not gate which quests can be revealed or fought.

## Run map coverage

How widely quest nodes are distributed across the playable desert. Good coverage means the player must pan and zoom to see the whole run; nodes should not sit in one tight cluster. Uneven blobs are acceptable; symmetry across quadrants is not required. A new run is not created until the generator produces a layout that meets minimum spread standards and fog reveal can reach every quest from the root (in-progress saves from older generators are not repaired).

## Node completion

Whether the player has won the battle at that quest node. Stored only on the run map in save v1; not a separate global quest-id list. Completed nodes remain visible on the map but cannot be started again in v1.

## Node reveal

A node becomes fightable when it is **revealed**. The root quest node is revealed at run start. When a quest is **completed**, up to three other quest nodes within **map fog range** of that completed node can become revealed as its fog circle expands (closest first; see **Reveal cutscene**). Revealed quests are visible and can be started; they do **not** clear map fog. Only **completed** quests clear fog. Merely revealing a quest does not reveal further quests until that quest is completed. The player may tackle any revealed, incomplete node; branches are not mutually exclusive.

## Map fog range

The distance from a **completed** quest node's world position used for revealing other quests and for the maximum radius of fog that node clears. Measured center-to-center; icon size and fog feather are visual only. Uses `DefaultRevealRadius` (~1000 world units on the desert map). Shop enterability uses the same range but only from **completed** quests whose fog actually covers the shop.

## Map fog

The desert overlay that hides unexplored areas. Only **completed** quest nodes clear map fog (a circular cleared area centered on the node). **Revealed** but incomplete quests do not clear fog; they only become visible and fightable when reached. `DefaultUnrevealedRadius` is icon-scale (~204px) and is the starting size for the expanding fog circle in the **Reveal cutscene** on the node just completed.

## Reveal cutscene

After completing a quest, returning to the location hub focuses the completed marker, locks player controls, and animates **that completed node's** fog circle from icon size to full **map fog range**. Other hidden quest nodes become **revealed** when the expanding edge reaches them (visible and fightable, no fog clear at their position).

Shop markers are an exception: their map icon and minimap dot are visible from the start of the run. Shops do not clear map fog. Unrevealed shops (not yet enterable) do not show a tooltip or enter prompt on hover.

Treasure Chest markers are also visible from run start and do not clear map fog. A chest becomes enterable only after the player has won battles deeper into the desert (cleared fog from a completed battle far enough along the run). Until then, no tooltip or hold-to-open prompt. When enterable, the tooltip reads **Open Treasure**.

## Shop (run map)

A card vendor on the desert run map. Three shops exist per run. Shops are **not** quest nodes: they are not part of the run map tree and do not replace any of the 20 combat nodes.

After the 20-node battle map is generated, each shop is placed at a world position within completed-quest fog range of at least one battle node (so completing that battle can unlock the shop). Each shop has three fixed listings (identity, color, price) generated at run creation and stored in save.

## Shop reveal (enterable)

A shop becomes enterable when its world position lies inside cleared map fog from at least one **completed** quest (same center and **map fog range** as that quest's fog circle). A nearby **revealed** quest does not unlock shops. The shop icon can be visible before enterable; until cleared fog reaches it, no tooltip or hold-to-enter.

The same card identity may appear in more than one shop in a run; within one shop the three listings use distinct identities and distinct colors.

_Avoid_: Shop node (when meaning the map marker; prefer **shop** vs **quest node**)

## Treasure Chest (run map)

A one-time loot marker on the desert run map. Two exist per run. Treasure Chests are **not** quest nodes or shops: they are not part of the run map tree.

After the battle map and shops are generated, each chest is placed so that entering it requires having cleared fog from battles deeper into the desert. Opening a chest (hold-to-open on the hub) grants a random amount of gold (10-30 per chest, rolled at run creation) and one medal the player does not already own (excluding medals still for sale in run-map shops). The medal is added to the loadout and equipped immediately. A claimed chest stays on the map as a dimmed icon and cannot be opened again.

_Avoid_: Treasure POI, treasure node (prefer **Treasure Chest** vs **quest node** or **shop**)

## Quest reward

On first completion of a quest node: flat gold (30; 75 for a dual-battle quest node) and one random card added to the loadout deck. No rewards on repeat attempts (replays disabled). The reward modal shows only this quest reward gold; other gold grants (e.g. from medals) may apply at the same moment without changing the modal amount.

## Might (applied passive)

A turn-long buff on the player: each of your attacks this turn deals extra damage equal to your Might stacks. Might does not carry to the next turn. Might stacks with **Power** on the same attack.

## Sharpen (applied passive)

A turn-long buff on the player: your next **weapon** attack this turn deals extra damage equal to your Sharpen stacks, then Sharpen is removed. Non-weapon attacks do not consume Sharpen. Sharpen stacks with **Aggression** on the same weapon hit; both are consumed on that hit. Multiple Sharpen grants in the same turn add their stack counts together.

## Bottom of deck

The end of the draw pile list (drawn last). The top of the draw pile is the card drawn next (`RemoveAt(0)` in battle).

## Medal bonus gold

Gold granted by a medal on **node completion**, separate from **quest reward** gold shown in the completion modal. The player's total gold increases; the modal still displays the quest reward amount only.

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
