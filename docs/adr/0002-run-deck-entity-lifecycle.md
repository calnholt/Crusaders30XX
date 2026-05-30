# Run deck entities persist for the whole run

Deck cards are ECS entities created once per run from the save loadout (`loadout_1`), marked with `RunDeckCard` and `DontDestroyOnLoad`, and held on a persistent `Deck` entity. They survive Battle and Location scene transitions. `RunLifecycleService.EndCurrentRun` destroys all run deck entities before `SaveCache.StartNewRun`.

Exhaust removes the card key from the save loadout immediately and destroys the entity; exhausted cards do not return in later battles. At each battle start, `ResetDeckExcludingWeapon` merges draw pile, hand, and discard into one shuffled draw pile. Mid-run loadout changes publish `LoadoutCardAdded` / `LoadoutCardRemoved` so entities are created or destroyed immediately. Token and effect-spawned cards stay battle-scoped (no `RunDeckCard`).

Dungeon loadouts and POI type were removed from v1; the run deck always mirrors the save loadout.
