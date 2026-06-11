# Crusaders30XX — domain glossary

Terms only. No implementation details.

## Run

A single playthrough from WayStation Depart through the procedurally generated combat map until victory, failure, or abandon. A save may hold one active run or no active run.

## Active run

A run whose map, deck, gold, inventory, and run-scoped state exist in the save. WayStation Depart creates and activates a run before applying the selected setup.

## Inactive run

The persisted state after run victory, run failure, or run abandon. Meta remains, but map data, gold, loadout, inventory, and other run-scoped data are cleared. Loading an inactive save does not generate a replacement run; only WayStation Depart creates the next run.

## Run-long applied passive

A debuff or status on the player that lasts for the whole **run**: it survives leaving battle, visiting the location hub, shopping, and starting other **quest nodes**, until **run failure**, **quest abandon**, or **new run start**. Examples: Frostbite, Scar, Bleed, Shackled (see implementation list `GetRunLongPassives`). Scar stacks persist across quest nodes; when the player leaves battle after completing a quest node, one scar stack is removed and max HP is restored by that amount.

_Avoid_: Quest passive (when meaning run-long; use **run-long applied passive**)

## Quest-scoped applied passive

A debuff or status on the player tied to the current **quest node** attempt: it persists across **queued encounters** within that node, but is removed when the player leaves the battle scene to the location hub, shop, or other non-battle scene. It is not written to the save file.

Examples: Penance, Webbing, Fear, Enflamed (see implementation list `GetQuestPassives`).

_Avoid_: Quest passive (when meaning run-long; use **run-long applied passive**)

## Queued encounter

One enemy fight in the sequential battle queue for a **quest node** (`QueuedEvents` advances one step per **queued encounter**). A quest node may have several queued encounters before the player returns to the location hub.

**Temperance** built during one queued encounter carries to the next encounter in the same quest node. **Temperance** resets to zero when the player leaves the battle scene (e.g. return to the location hub). Courage, HP, and action points reset at the start of each queued encounter.

When card or medal text says **win a battle**, that means winning a **queued encounter** (the enemy's **defeat presentation** has finished and the battle advances), not completing the whole **quest node**. Reaching 0 HP starts **defeat presentation**; the encounter is not won until that presentation ends.

## Action Point

A battle resource spent to play cards during the **Action phase**. Playing a card that is not a **Free Action** costs one Action Point. The player begins each turn with one Action Point, and card effects may grant more.

## Free Action

A card play or equipment ability that can be activated during the **Action phase** without spending an Action Point. Equipment marked as a Free Action still consumes any equipment uses or other resources listed by its ability.

## Equipment use

A quest-node-scoped charge shared by an equipment item's block and activation behavior. Blocking with equipment or activating its ability consumes the listed uses. Uses do not reset between **queued encounters** in the same **quest node**. All equipped items replenish to their total uses when the **quest reward** overlay opens after node completion.

## Pledge available

The player may pledge during the **Action phase** when pledging is enabled, they have not pledged during that Action phase, no card is already pledged, and their hand contains an eligible card. Sealed cards, weapons, block cards, relics, tokens, and cards already pledged are not eligible.

## Defeat presentation

The on-screen sequence when a combatant is reduced to 0 HP before the battle advances. For enemies in v1, this is the pixel-burst animation: the enemy **portrait** is hidden and replaced by the burst; other enemy UI (HP bar, intents, passives) may remain visible until the presentation ends. When it completes, the game runs the normal post-kill flow (achievements, next queued encounter, or quest completion). During defeat presentation the player cannot take battle actions (cards, end turn, equipment). Player defeat presentation is separate (game-over overlay).

_Avoid_: Death animation (ambiguous with attack animations or game-over)

_Avoid_: Battle node (ambiguous with **quest node** on the map)

## Run failure

Losing a battle. The current run ends after the game-over sequence finishes: run entities and run-scoped save data are cleared, an inactive run is persisted, and the player returns to WayStation. Meta earned during that fight is kept.

## Quest abandon

Voluntarily ending the current run from the in-battle quit overlay (prompt: "Abandon run?"). Uses the same game-over sequence as run failure, persists an inactive run, and returns to WayStation.

## Meta

Progress that survives run failure and new-run creation: achievements, card mastery, and seen tutorials.

## Save

Persistent file for run lifecycle state plus meta. An active run includes map topology, node progress, gold, loadouts, inventory, **run-long applied passive** stacks on the player, and **run-long card restriction** markers per deck card entry. An inactive run contains none of that run-scoped data. Starting a new run replaces run state only; meta is kept.

The save file has a `version` field. If it does not match the game's current save version, the entire file is replaced with a new default save. There is no migration between versions. Omitting `version` or using an old version number clears all progress (map, gold, deck, mastery, achievements, tutorials).

## Loadout

The player's configured battle kit for the run: deck card list, weapon, temperance ability, equipment slots, and medals. Shop card purchases append a card entry to the deck list immediately. Quest rewards do the same. There is no separate owned-cards list. Equipment is one piece per slot (head, chest, arms, legs). Gaining new equipment in a slot replaces whatever was in that slot; the replaced piece cannot be re-equipped later in the run.

## Deck

The player's deck for the current run: the ordered set of card entries (card plus color) on the loadout, plus how those cards are distributed in battle (draw pile, hand, discard). A new run begins with a 20-card starting deck; the deck may grow beyond 20 during the run. There is no maximum deck size.

## Printed color

The immutable Red, White, or Black identity stored on a card entry. Printed color determines deck construction, rewards, save keys, and the card's normal combat color. Effects may change whether the card qualifies as that color in combat without changing its printed color.

## Colorless

A **run-long card restriction** that makes a card qualify as no color during combat while preserving its **printed color**. A Colorless card can pay an `Any` color cost but not a Red, White, or Black cost. It does not count for color predicates, color resources, or the printed-Black block bonus.

## Exhaust

Removing a card from the deck for the rest of the run. Exhausted cards do not return in later battles. Exhaustion removes that card entry from the save loadout and destroys the run deck entity.

## Inter-battle deck reset

At the start of each battle, all surviving cards (draw pile, hand, and discard) combine into one draw pile, which is then shuffled. Exhausted cards are not included.

## Run-long card restriction

A combat effect attached to a specific deck card entity (e.g. Frozen, Sealed, Brittle, or Colorless) that lasts for the whole **run** until **run failure**, **quest abandon**, or **new run start**. Restrictions survive leaving battle and hub visits; they are cleared when the run ends, not at each queued encounter.

The run-long **Shackled** player passive is not a card restriction. Battle-local **Shackle** markers link cards so they are assigned and unassigned as blockers together; those markers are not persisted.

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

WayStation Depart after first launch or after an inactive run. Depart creates and activates the run, applies the selected weapon and difficulty setup, then starts the root combat node.

## Combat node

One battle location in the 20-node run map. A combat node is either a **quest node** or a **Hellrift**.

## Quest node

An ordinary combat node on the run map. Each run has 19 quest nodes. Completing one marks it completed, grants a quest reward, and may reveal other combat nodes within map fog range.

## Hellrift

The non-quest combat-node type. Each run has exactly one Hellrift, named **The Gate**, placed on a deepest leaf with at least six quest ancestors. It contains only the Fallen Shepherd encounter and grants no quest reward.

The Gate follows normal spatial node reveal. Before reveal it has no map icon, minimap marker, tooltip, or off-screen indicator. Once revealed it behaves like an ordinary fightable combat marker.

## Root quest node

The first quest node in a run. It is the player's initial battle and the anchor for procedural placement. Its position is near the desert map center with random offset so it is not always at the exact geometric center.

## Run map tree

Internal parent/child links used only when generating the 20 combat-node positions. Not shown on the desert map and does not gate which combat nodes can be revealed or fought.

## Run map coverage

How widely combat nodes are distributed across the playable desert. Good coverage means the player must pan and zoom to see the whole run; nodes should not sit in one tight cluster. Uneven blobs are acceptable; symmetry across quadrants is not required. A new run is not created until the generator produces a layout that meets minimum spread standards and fog reveal can reach every combat node from the root.

## Node completion

Whether the player has won the battle at that quest node. Stored only on the run map in save v1; not a separate global quest-id list. Completed nodes remain visible on the map but cannot be started again in v1.

## Node reveal

A combat node becomes fightable when it is **revealed**. The root quest node is revealed at run start. When a quest is **completed**, up to three other combat nodes within **map fog range** of that completed node can become revealed as its fog circle expands (closest first; see **Reveal cutscene**). Revealed combat nodes are visible and can be started; they do **not** clear map fog. Only **completed** quest nodes clear fog. Merely revealing a node does not reveal further nodes until a quest node is completed. The player may tackle any revealed, incomplete combat node; branches are not mutually exclusive.

## Boss phase

One segment of a multi-phase enemy battle. `EnemyBase.Phases` is the total phase count and `CurrentPhase` is the active phase. Lethal damage before the final phase ends the current phase without publishing enemy-kill, battle-win, node-completion, or reward signals.

## Phase reset

The transition between boss phases after checkpoint dialogue. It advances `CurrentPhase`, rebuilds the enemy arsenal, fully heals player and enemy, merges and shuffles hand/draw/discard while leaving exhausted cards removed, clears pledges and transient card/attack interaction state, and removes only turn-duration passives from player and enemy.

Courage, Temperance, Guard, battle/quest/run passives, card restrictions, card modifications, and cumulative turn number persist. A phase reset does not publish StartBattle, rerun start-of-battle abilities, or show the Start of Battle banner. It enters the next EnemyStart flow directly.

## Map fog range

The distance from a **completed** quest node's world position used for revealing other quests and for the maximum radius of fog that node clears. Measured center-to-center; icon size and fog feather are visual only. Uses `DefaultRevealRadius` (~1000 world units on the desert map). Shop enterability uses the same range but only from **completed** quests whose fog actually covers the shop.

## Map fog

The desert overlay that hides unexplored areas. Only **completed** quest nodes clear map fog (a circular cleared area centered on the node). **Revealed** but incomplete quests do not clear fog; they only become visible and fightable when reached. `DefaultUnrevealedRadius` is icon-scale (~204px) and is the starting size for the expanding fog circle in the **Reveal cutscene** on the node just completed.

## Reveal cutscene

After completing a quest, returning to the location hub focuses the completed marker, locks player controls, and animates **that completed node's** fog circle from icon size to full **map fog range**. Other hidden quest nodes become **revealed** when the expanding edge reaches them (visible and fightable, no fog clear at their position).

Shop markers are an exception: their map icon and minimap dot are visible from the start of the run. Shops do not clear map fog. Unrevealed shops (not yet enterable) do not show a tooltip or enter prompt on hover.

Treasure Chest markers are also visible from run start and do not clear map fog. A chest becomes enterable only after the player has won battles deeper into the desert (cleared fog from a completed battle far enough along the run). Until then, no tooltip or hold-to-open prompt. When enterable, the tooltip reads **Open Treasure**.

Map event markers are also visible from run start and do not clear map fog. A Map event becomes enterable when cleared map fog from a **completed** quest covers it (same rule as shops). Until then, no tooltip or hold prompt. When enterable, the tooltip reads **Event**.

## Shop (run map)

A vendor on the desert run map. Three shops exist per run. Shops are **not** combat nodes: they are not part of the run map tree and do not replace any of the 20 combat nodes.

After the 20-node battle map is generated, each shop is placed at a world position within completed-quest fog range of at least one battle node (so completing that battle can unlock the shop). Each shop has three fixed listings generated at run creation and stored in save. Listings are usually cards; one shop per run also lists a medal and one non-medal shop lists equipment.

## Shop reveal (enterable)

A shop becomes enterable when its world position lies inside cleared map fog from at least one **completed** quest (same center and **map fog range** as that quest's fog circle). A nearby **revealed** quest does not unlock shops. The shop icon can be visible before enterable; until cleared fog reaches it, no tooltip or hold-to-enter.

The same card identity may appear in more than one shop in a run; within one shop the three listings use distinct identities and distinct colors.

_Avoid_: Shop node (when meaning the map marker; prefer **shop** vs **quest node**)

## Treasure Chest (run map)

A one-time loot marker on the desert run map. Three exist per run. Treasure Chests are **not** quest nodes or shops: they are not part of the run map tree.

After the battle map and shops are generated, each chest is placed so that entering it requires having cleared fog from battles deeper into the desert. Opening a chest (hold-to-open on the hub) grants a random amount of gold (10-30 per chest, rolled at run creation). Two chests per run also grant one medal the player does not already own (excluding medals still for sale in run-map shops); the medal is added to the loadout and equipped immediately. One chest per run (chosen at random) grants equipment instead of a medal; that equipment is a different slot type than the equipment listed in run-map shops and is equipped immediately. A claimed chest stays on the map as a dimmed icon and cannot be opened again.

_Avoid_: Treasure POI, treasure node (prefer **Treasure Chest** vs **quest node** or **shop**)

## Map event (run map)

A choice landmark on the desert run map. Two exist per run. Map events are **not** quest nodes, shops, or Treasure Chests: they are not part of the run map tree.

After the battle map, shops, and chests are generated, each Map event is placed using the same scatter rules as shops. At run creation each marker rolls a distinct **narrative event** type (stored as `eventTypeId`) from the authored pool. The hold on the hub opens the **narrative event** choice UI. The Map event is marked completed only after the player selects an option and that choice is resolved.

## Map event reveal (enterable)

A Map event becomes enterable when its world position lies inside cleared map fog from at least one **completed** quest (same center and **map fog range** as that quest's fog circle). A nearby **revealed** quest does not unlock Map events. The red question-mark icon can be visible before enterable; until cleared fog reaches it, no tooltip or hold prompt.

_Avoid_: Event node (when meaning the map marker; prefer **Map event** vs **quest node**)

## Narrative event

Authored hub content with a title, body text, and one to three player choices (`EventBase`). Examples include Icebound Tithe and Pruned Vocation. A Map event's `eventTypeId` selects which narrative event runs when the player enters that marker on the hub.

_Avoid_: Using "event" alone when you mean the map marker or the choice content; use **Map event** or **Narrative event**

## Quest reward

On first completion of a quest node: flat gold (30; 75 for a dual-battle quest node) and one random card added to the loadout deck. The Gate is not a quest node and grants no quest reward. No rewards on repeat attempts (replays disabled). The reward modal shows only this quest reward gold; other gold grants (e.g. from medals) may apply at the same moment without changing the modal amount.

## Run victory

Defeating the Fallen Shepherd's final phase. Victory dialogue finishes first, then the normal enemy defeat presentation and enemy-kill signal run. No node completion or reward is granted. The run is ended, an inactive run is persisted, and the game transitions to WayStation.

## Might (applied passive)

A turn-long buff on the player: each of your attacks this turn deals extra damage equal to your Might stacks. Might does not carry to the next turn. Might stacks with **Power** on the same attack.

## Aggression (applied passive)

A turn-long buff on the player: your next **non-weapon** attack this turn deals extra damage equal to your Aggression stacks, then Aggression is removed. Weapon attacks do not consume Aggression. Multiple Aggression grants in the same turn add their stack counts together.

## Sharpen (applied passive)

A turn-long buff on the player: your next **weapon** attack this turn deals extra damage equal to your Sharpen stacks, then Sharpen is removed. Non-weapon attacks do not consume Sharpen. Multiple Sharpen grants in the same turn add their stack counts together.

Aggression and Sharpen are complementary (non-weapon vs weapon) and do not apply to the same attack.

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
