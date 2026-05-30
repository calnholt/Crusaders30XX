# Run-map shops are off-tree vendors unlocked by completed-quest fog

## Status

Accepted (2026-05-30)

## Context

The desert run map is a procedural tree of 20 quest (combat) nodes. We want three card shops per run: visible landmarks from the start, enterable only after the player has cleared nearby battles, selling fixed non-starter listings at 30 gold with no separate collection layer.

Quest nodes use graph reveal (`isRevealed` when a parent is completed) and map fog hides unrevealed quests. Shops need different rules: always visible, not in the battle tree, and unlock tied to **completed** quest fog rather than shop parent/child links.

Deck building normally enforces copy limits (one per identity+color, two per identity). Shop design allows stacking duplicates the player could not get from quest rewards.

## Decision

1. **Separate save list** `runMapShops` (ids `shop_0`..), each with `worldX`/`worldY` and three `RunMapShopItem` listings. The 20-node `runMapNodes` list is unchanged.

2. **Generation order**: build the battle tree as today, then scatter three shops. Placement rejects overlap with battle nodes (`MinNodeSpacing`); shops may be closer to each other. Each shop must lie within `DefaultRevealRadius` (derived from `MaxStep`, same constant as quest fog) of at least one battle node so some completion can unlock it.

3. **Enterable rule**: a shop is enterable when its position lies inside the fog circle of any **completed** quest node (full `RevealRadius`). Revealed-but-incomplete nearby quests do not unlock shops.

4. **Map UX**: shop icon and minimap dot always shown; no hover tooltip or hold-X until enterable; shops do not emit fog-clear circles.

5. **Purchases**: spend gold, append the exact `cardId|color` listing to `loadout_1`, mark the shop slot `isPurchased`. Shop purchases **ignore** deck copy limits; quest rewards still enforce them.

6. **Scope**: shops only on **new** run map generation. No migration of in-progress saves. Remove the legacy JSON `LocationDefinitionCache` shop inventory path.

## Consequences

- **Positive**: Keeps 20 combat encounters; shops do not steal nodes or alter tree reveal logic; inventory is deterministic from `runMapSeed`; glossary in `CONTEXT.md` stays the player-facing source of truth.

- **Negative**: Saves created before this feature have no shops until a new run. Enterability requires a runtime distance check against completed nodes (or cached refresh on quest complete). Allowing shop dupes can produce decks that break assumptions in UI that expect copy limits.

- **Follow-up**: Implement `RunMapShopService.IsEnterable`, spawn shop POIs alongside quest POIs, and wire hold-X entry to `SceneId.Shop` with save-backed `ForSaleDisplaySystem` inventory.
