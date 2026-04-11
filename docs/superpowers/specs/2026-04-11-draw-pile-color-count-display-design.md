# DrawPileColorCountDisplaySystem — Design Spec

**Date:** 2026-04-11

## Overview

A new BattleScene display system that shows the count of Red, White, and Black cards currently in the player's draw pile. Three vertically-stacked rounded-rect pills are rendered to the left of the DrawPileDisplaySystem panel, parallaxing with the UI layer.

## Data Source

- Reads `Deck.DrawPile` (a `List<Entity>`) each frame.
- For each entity in `DrawPile`, reads its `CardInstance.Color` (`CardColor` enum: `White`, `Red`, `Black`, `Yellow`).
- Counts Red, White, and Black separately. Yellow is excluded and not displayed.

## Entities

Three named root entities, one per color:

| Entity Name            | Pill Color       | Text Color   |
|------------------------|------------------|--------------|
| `UI_ColorCount_Red`    | `Color.Red`      | `Color.White` |
| `UI_ColorCount_White`  | `Color.White`    | `Color.Black` |
| `UI_ColorCount_Black`  | `new Color(20, 20, 20)` | `Color.White` |

Each entity gets:
- `Transform` with `ZOrder = 10000`
- `ParallaxLayer.GetUIParallaxLayer()`
- `UIElement` with `IsInteractable = false`, no `Tooltip`, bounds updated each frame

## Rendering

Each pill is drawn using `RoundedRectTextureFactory.CreateRoundedRect`. Textures are cached per pill and regenerated only when `PillWidth`, `PillHeight`, or `CornerRadius` changes (dirty flag pattern).

The pill texture is drawn as a white mask, tinted to the pill's background color via `SpriteBatch.Draw` color parameter. Text is drawn centered within the pill.

## Positioning

Pills are anchored to the left of the DrawPileDisplaySystem panel:

- **X:** `VirtualWidth - DrawPileRefWidth - DrawPileRefMargin - PillGap - PillWidth / 2`
- **Y (center of stack):** `VirtualHeight - DrawPileRefHeight / 2 - DrawPileRefMargin`
- Pills are stacked vertically with `PillSpacing` between centers, ordered top-to-bottom: Red, White, Black.

`DrawPileRefWidth`, `DrawPileRefHeight`, and `DrawPileRefMargin` are `DebugEditable` properties that mirror `DrawPileDisplaySystem`'s panel dimensions — no cross-system coupling, just duplicated reference values.

Positions are written to each entity's `Transform.Position` each frame in `UpdateEntity`, following the same pattern as `DrawPileDisplaySystem` and `DiscardPileDisplaySystem`.

## DebugEditables

| Property | Description | Default |
|---|---|---|
| `PillWidth` | Width of each pill | 40 |
| `PillHeight` | Height of each pill | 22 |
| `PillSpacing` | Vertical distance between pill centers | 26 |
| `PillGap` | Horizontal gap between pills and draw pile panel | 8 |
| `CornerRadius` | Rounded corner radius | 6 |
| `TextScale` | Font scale for count text | 0.08f |
| `DrawPileRefWidth` | Reference copy of DrawPileDisplaySystem.PanelWidth | 60 |
| `DrawPileRefHeight` | Reference copy of DrawPileDisplaySystem.PanelHeight | 80 |
| `DrawPileRefMargin` | Reference copy of DrawPileDisplaySystem.PanelMargin | 30 |

## Registration

Register in `Game1.cs` with `world.AddSystem()` alongside other BattleScene display systems. Wire `Draw()` call in the BattleScene draw loop, same as `DrawPileDisplaySystem`.

## Constraints

- No interactivity — `UIElement.IsInteractable = false`, no tooltip.
- No pulse animation — pure static display.
- `Draw()` is strictly rendering-only; no state mutation.
- Follows parallax contract: only write `Transform.Position`, never read or write `ParallaxLayer` internals.
