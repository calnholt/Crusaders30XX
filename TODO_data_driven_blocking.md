### Data-driven blocking system — TODO checklist

- [ ] Owner: assign a primary owner to drive this checklist
- [ ] Status legend: [ ] todo, [x] done, [~] in progress

## Phase 1 — Data model and loader
- [ ] Add models for JSON attacks (`ECS/Data/Attacks/AttackDefinition.cs`)
  - [ ] `AttackDefinition`
  - [ ] `ConditionNode`
  - [ ] `EffectDefinition`
- [ ] Add repository/loader (`ECS/Data/Attacks/AttackRepository.cs`)
  - [ ] Load all `*.json` under `ECS/Data/Enemies/` into `Dictionary<string, AttackDefinition>`
  - [ ] Basic error handling + console logs on load
- [ ] Add sample attack JSON (`ECS/Data/Enemies/demon_bite.json`)
  - [ ] Block if played ≥1 Red card; else deal 14 damage; if blocked apply Weak(1)
- [ ] Test: temporary debug action logs number of loaded attacks and prints `demon_bite`

## Phase 2 — ECS components
- [ ] Add combat components (`ECS/Components/CombatComponents.cs`)
  - [ ] `EnemyArsenal { List<string> AttackIds }`
  - [ ] `AttackIntent { List<PlannedAttack> Planned }` + `PlannedAttack { AttackId, ResolveStep, ContextId, WasBlocked }`
  - [ ] `BlockProgress { Dictionary<string /*contextId*/, Dictionary<string,int> /*counters*/> }`
- [ ] Initialize usage
  - [ ] Ensure Player has `BlockProgress`
  - [ ] Ensure an Enemy has `EnemyArsenal` with `["demon_bite"]`
- [ ] Test: debug print confirms components exist at runtime

## Phase 3 — Events
- [ ] Add combat events (`ECS/Events/CombatEvents.cs`)
  - [ ] `StartEnemyTurn`, `EndEnemyTurn`
  - [ ] `IntentPlanned { AttackId, ContextId, Step, TelegraphText }`
  - [ ] `CardPlayed { Entity Card, string Color }`
  - [ ] `ResolveAttack { string ContextId }`
  - [ ] `ApplyEffect { EffectType, Amount, Status, Stacks, Source, Target }`
  - [ ] `AttackResolved { ContextId, WasBlocked }`
- [ ] Test: temporary subscribers log when each event fires

## Phase 4 — Intent planning
- [ ] `EnemyIntentPlanningSystem`
  - [ ] On `StartEnemyTurn`, pick 1 attack from `EnemyArsenal` (start with first id)
  - [ ] Create `PlannedAttack` with `ContextId = Guid`, `ResolveStep = 1`
  - [ ] Store in enemy `AttackIntent`, publish `IntentPlanned`
- [ ] Battle phase integration: when `BattlePhaseSystem` enters Block → publish `StartEnemyTurn`
- [ ] Test: using debug “To Block Phase” → see `IntentPlanned` log and enemy `AttackIntent` populated

## Phase 5 — Tracking block progress
- [ ] `BlockConditionTrackingSystem`
  - [ ] Subscribe to `CardPlayed`
  - [ ] For each active `PlannedAttack`, increment counters in player `BlockProgress` under that `ContextId`
  - [ ] Counter keys for initial leaves: `played_Red`, `played_White`, `played_Black`
- [ ] Test: debug actions “Simulate CardPlayed(Red/White/Black)” increment counters; print snapshot

## Phase 6 — Condition evaluation service
- [ ] `ConditionService`
  - [ ] Evaluate `ConditionNode` with context (attacker, target, `BlockProgress` by `ContextId`)
  - [ ] Composites: `All`, `Any`, `Not`
  - [ ] Leaf: `PlayColorAtLeastN` (params: `color`, `n`)
- [ ] Tests
  - [ ] Simple unit-like checks on nodes with fake counters
  - [ ] Debug button prints evaluation for the current planned attack

## Phase 7 — Attack resolution
- [ ] `AttackResolutionSystem`
  - [ ] On `ResolveAttack(ContextId)`, find `PlannedAttack`
  - [ ] Evaluate `conditionsBlocked` via `ConditionService`
  - [ ] If blocked → enqueue `ApplyEffect` for each `effectsOnBlocked`
  - [ ] Else → enqueue `ApplyEffect` for each `effectsOnHit`
  - [ ] Publish `AttackResolved`
- [ ] Debug helpers
  - [ ] “Resolve Next Intent” button publishes `ResolveAttack` for the first pending intent
  - [ ] Optional: auto-transition to Action to trigger resolution
- [ ] Tests: without card played → on-hit damage; after `CardPlayed(Red)` → on-blocked effects

## Phase 8 — Effect application
- [ ] `EffectApplicationSystem`
  - [ ] `Damage` → publish `ModifyHpEvent { Delta = -amount }`
  - [ ] `ApplyStatus` → log-only for now (wire status later)
- [ ] Test: HP changes on on-hit; logs on on-blocked

## Phase 9 — Intent display (optional initial UI)
- [ ] `IntentDisplaySystem`
  - [ ] Draw attack name and “Blocked?” state using live evaluation
- [ ] Test: telegraph visible; “Blocked?” flips after `CardPlayed(Red)`

## Phase 10 — Content and repository wiring
- [ ] Load repository at startup
  - [ ] Load path: `ECS/Data/Enemies/`
  - [ ] Provide repository to `EnemyIntentPlanningSystem`
- [ ] Ensure `demon_bite.json` matches the design doc
- [ ] Test: entering Block plans demon bite and can be resolved

## Phase 11 — Debug UX
- [ ] New debug tab: “Enemy Combat (JSON)”
  - [ ] Buttons: “Load Attacks”, “Plan Next Turn”, “Resolve Next Intent”
  - [ ] Buttons: “Simulate CardPlayed(Red/White/Black)”
  - [ ] Button: “Print BlockProgress snapshot”
- [ ] Test: each command works without crashes

## Phase 12 — Extensions (post-vertical slice)
- [ ] More leaves: `MaintainBlockGE`, `EnergySpentGE`, `HandContainsColor`, `HasStatus`
- [ ] More effects: `GainBlock`, `Draw`, `Heal`, `PublishEvent`
- [ ] Enemy cooldowns/weights on `EnemyArsenal`
- [ ] Multi-intent per turn with `resolveStep` ordering
- [ ] Persist and display resolved parameters in telegraph text (e.g., chosen random color)
- [ ] Status system: `ApplyStatus` real implementation + expiry

## Milestone acceptance criteria
- [ ] M1 (Vertical slice): From clean boot, entering Block plans `demon_bite`; resolving without Red deals 14 damage; after simulating `CardPlayed(Red)`, resolution applies `Weak(1)` and no damage.
- [ ] M2: UI shows the intent and live “Blocked?” state.
- [ ] M3: Editing the JSON file changes behavior without code changes.
