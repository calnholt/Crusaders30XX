# Display snapshot commands

Canonical reference for headless display verification. The game renders a fixture for two frames, writes a PNG under `debug/snapshots/`, prints the full path to the console, and exits.

**Do not document snapshot commands elsewhere** — link to this file instead. When adding a new fixture, register it in `DisplaySnapshotRegistry` and add a section below.

## Prerequisites

```bash
dotnet build
```

All commands use `dotnet run --` so arguments are passed to the game, not MSBuild.

## General form

```bash
dotnet run -- snapshot <fixture-id> [fixture-args...]
```

Add `--verify` to compare against the approved image, or `--accept` to
explicitly replace it. These flags are mutually exclusive and are not passed
to the fixture.

| Part | Description |
|------|-------------|
| `snapshot` | Required first argument (replaces the removed `card-debug` command) |
| `<fixture-id>` | Registered fixture name (see table below) |
| `[fixture-args...]` | Fixture-specific flags and positional args |

### Output

- **Directory:** `debug/snapshots/<fixture-id>/` (relative to repo root; parent `debug/` is gitignored)
- **File:** `<slug>.png` — slug is fixture-defined (see each fixture)
- **Console:** `[DisplaySnapshot] Saved: <absolute-path>`
- **Exit code:** `0` on success; `1` on unknown fixture, invalid args, or invalid card/reward id (fail fast)

### Behavior (all fixtures)

- Virtual resolution 1920×1080; no profiler, tooltips, debug menu, or cursor in the PNG
- `CardDisplayToggle.UseV2` is set where needed for card rendering
- Does not publish `LoadSceneEvent` — uses `SceneId.Snapshot` only
- Optional launch flag `no-shaders` (e.g. `dotnet run -- snapshot card no-shaders`) disables GPU screen effects; PNGs are not comparable to full-effect baselines

---

## Fixtures

| Fixture id | Display system | Purpose |
|------------|----------------|---------|
| `card` | Card display (V2) | Three color variants of one card on a green background |
| `brittle-card` | Brittle card shader | One brittle card on a patterned backdrop for shader debugging |
| `quest-reward-modal` | Quest reward modal | Quest complete overlay (gold and/or card reward) |
| `waystation` | WayStation run setup | Run setup scene with default Sword/Easy selections |
| `player-hud` | Production player HUD systems | Player HUD geometry and state variants |

---

## `card`

Renders **White**, **Red**, and **Black** copies of the same card side by side (same layout as the former `card-debug` flow).

### Commands

```bash
# Random non-weapon card (card id omitted)
dotnet run -- snapshot card

# Specific card by definition id
dotnet run -- snapshot card strike
dotnet run -- snapshot card fireball
```

### Output file

`debug/snapshots/card/<cardId>.png`

Example: `debug/snapshots/card/strike.png`

### Errors

- If `<cardId>` is provided but unknown: exit `1`, no PNG

---

## `brittle-card`

Renders one White card with the `Brittle` component attached on a high-contrast patterned backdrop.

### Commands

```bash
# Default card
dotnet run -- snapshot brittle-card

# Specific card by definition id
dotnet run -- snapshot brittle-card strike
dotnet run -- snapshot brittle-card fireball

# Disable shaders to compare against the normal card render
dotnet run -- snapshot brittle-card strike no-shaders
```

### Output file

`debug/snapshots/brittle-card/<cardId>.png`

Example: `debug/snapshots/brittle-card/strike.png`

### Errors

- If `<cardId>` is provided but unknown: exit `1`, no PNG

---

## `quest-reward-modal`

Renders `RewardModalDisplaySystem` with optional gold and card reward columns.

### Commands

```bash
# Default: gold 500 + card strike|white (full two-column layout)
dotnet run -- snapshot quest-reward-modal

# Gold only (narrow modal)
dotnet run -- snapshot quest-reward-modal --gold 1200

# Card reward only (must include color)
dotnet run -- snapshot quest-reward-modal --card 'strike|white'

# Full layout with explicit values
dotnet run -- snapshot quest-reward-modal --gold 500 --card 'strike|white'
dotnet run -- snapshot quest-reward-modal --gold 250 --card 'fireball|red'
```

### `--card` format

`cardId|color` — same as production `RewardCardKey`:

| Color | Token |
|-------|--------|
| White | `white` |
| Red | `red` |
| Black | `black` |

Example: `'strike|white'`, `'fireball|red'` (quote in shell so `|` is not piped)

### Output files

| Run | Example path |
|-----|----------------|
| Defaults | `debug/snapshots/quest-reward-modal/gold-500-card-strike-white.png` |
| `--gold 1200` | `debug/snapshots/quest-reward-modal/gold-1200.png` |
| `--card strike\|white` only | `debug/snapshots/quest-reward-modal/card-strike-white.png` |
| Both explicit | `debug/snapshots/quest-reward-modal/gold-500-card-strike-white.png` |

(Slugs are defined by `QuestRewardSnapshotVariant` at implementation time; adjust this table if slugs change.)

### Errors

- Invalid or unknown `cardId` in `--card`: exit `1`, no PNG
- Malformed `--card` (missing `|` or color): exit `1`, no PNG
- Invalid `--gold` (non-integer): exit `1`, no PNG

---

## `narrative-event-modal`

Renders `NarrativeEventModalDisplaySystem` for a narrative event type and optional visible option count.

### Commands

```bash
# Default: icebound_tithe, 3 options
dotnet run -- snapshot narrative-event-modal

dotnet run -- snapshot narrative-event-modal --event pruned_vocation

dotnet run -- snapshot narrative-event-modal --event icebound_tithe --options 1
dotnet run -- snapshot narrative-event-modal --event icebound_tithe --options 2
```

### Output files

| Run | Example path |
|-----|----------------|
| Defaults | `debug/snapshots/narrative-event-modal/icebound-tithe-options-3.png` |
| `--event pruned_vocation` | `debug/snapshots/narrative-event-modal/pruned-vocation-options-3.png` |
| `--event icebound_tithe --options 1` | `debug/snapshots/narrative-event-modal/icebound-tithe-options-1.png` |
| `--event icebound_tithe --options 2` | `debug/snapshots/narrative-event-modal/icebound-tithe-options-2.png` |

### Errors

- Unknown `--event` id: exit `1`, no PNG
- Invalid `--options` (not 1, 2, or 3): exit `1`, no PNG
- Malformed / unknown CLI token: exit `1`, no PNG

---

## `waystation`

Renders the WayStation run setup scene with default Sword/Easy selections.

### Commands

```bash
dotnet run -- snapshot waystation
```

### Output file

`debug/snapshots/waystation/default.png`

---

## `player-hud`

Renders the production player HUD systems against a fixed portrait and solid
backdrop. Approved images are stored under
`tests/VisualBaselines/player-hud/`.

```bash
dotnet run -- snapshot player-hud default
dotnet run -- snapshot player-hud unavailable
dotnet run -- snapshot player-hud incoming-damage
dotnet run -- snapshot player-hud low-health
dotnet run -- snapshot player-hud expanded

./scripts/verify-player-hud-snapshots.sh
./scripts/verify-player-hud-snapshots.sh --accept
```

The verification script is read-only by default. `--accept` explicitly
replaces all five approved baselines.

---

## Removed commands

| Old command | Replacement |
|-------------|-------------|
| `dotnet run -- card-debug` | `dotnet run -- snapshot card` |
| `dotnet run -- card-debug strike` | `dotnet run -- snapshot card strike` |

---

## Adding a new fixture

1. Implement `IDisplaySnapshotFixture` under `ECS/Diagnostics/Snapshots/Fixtures/`
2. Register in `DisplaySnapshotRegistry`
3. **Add a section to this document** with id, commands, output paths, and errors
4. Add a verification line to the implementation plan / PR test notes
