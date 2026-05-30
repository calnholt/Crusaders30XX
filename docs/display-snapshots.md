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
| `quest-reward-modal` | Quest reward modal | Quest complete overlay (gold and/or card reward) |

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

## `quest-reward-modal`

Renders `QuestRewardModalDisplaySystem` with optional gold and card reward columns.

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
