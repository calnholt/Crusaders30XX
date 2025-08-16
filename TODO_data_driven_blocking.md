### Data-driven blocking system — TODO checklist

- [ ] Owner: assign a primary owner to drive this checklist
- [ ] Status legend: [ ] todo, [x] done, [~] in progress

## Phase 1 — Data model and loader
- [x] Add models for JSON attacks (`ECS/Data/Attacks/AttackDefinition.cs`)
  - [ ] `AttackDefinition`
  - [ ] `ConditionNode`
  - [ ] `EffectDefinition`
- [x] Add repository/loader (`ECS/Data/Attacks/AttackRepository.cs`)
  - [x] Load all `*.json` under `ECS/Data/Enemies/` into `Dictionary<string, AttackDefinition>`
  - [x] Basic error handling + console logs on load
- [x] Add sample attack JSON (`ECS/Data/Enemies/demon_bite.json`)
  - [ ] Block if played ≥1 Red card; else deal 14 damage; if blocked apply Weak(1)
- [x] Test: temporary debug action logs number of loaded attacks and prints `demon_bite`

## Phase 2 — ECS components
- [x] Add combat components (`ECS/Components/CombatComponents.cs`)
  - [x] `EnemyArsenal { List<string> AttackIds }`
  - [x] `AttackIntent { List<PlannedAttack> Planned }` + `PlannedAttack { AttackId, ResolveStep, ContextId, WasBlocked }`
  - [x] `BlockProgress { Dictionary<string /*contextId*/, Dictionary<string,int> /*counters*/> }`
- [x] Initialize usage
  - [x] Ensure Player has `BlockProgress`
  - [x] Ensure an Enemy has `EnemyArsenal` with `["demon_bite"]`
- [ ] Test: debug print confirms components exist at runtime
  - [x] Use Debug Menu → Combat Debug → "Phase 2 Test: Print Combat Components"; verify Player has BlockProgress, Enemy has EnemyArsenal (includes demon_bite) and AttackIntent list (may be empty initially)

## Phase 3 — Events
- [x] Add combat events (`ECS/Events/CombatEvents.cs`)
  - [x] `StartEnemyTurn`, `EndEnemyTurn`
  - [x] `IntentPlanned { AttackId, ContextId, Step, TelegraphText }`
  - [x] `BlockCardPlayed { Entity Card, string Color }`
  - [x] `ResolveAttack { string ContextId }`
  - [x] `ApplyEffect { EffectType, Amount, Status, Stacks, Source, Target }`
  - [x] `AttackResolved { ContextId, WasBlocked }`
- [x] Test: temporary subscribers log when each event fires
  - [x] Use Debug Menu → Combat Events (Debug) → publish sample events and confirm console logs appear

## Phase 4 — Intent planning
- [x] `EnemyIntentPlanningSystem`
  - [x] On `StartEnemyTurn`, pick 1 attack from `EnemyArsenal` (start with first id)
  - [x] Create `PlannedAttack` with `ContextId = Guid`, `ResolveStep = 1`
  - [x] Store in enemy `AttackIntent`, publish `IntentPlanned`
- [x] Battle phase integration: when `BattlePhaseSystem` enters Block → publish `StartEnemyTurn`
- [x] Test: using debug “To Block Phase” → see `IntentPlanned` log and enemy `AttackIntent` populated

## Phase 5 — Tracking block progress
- [x] `BlockConditionTrackingSystem`
  - [x] Subscribe to `BlockCardPlayed`
  - [x] For each active `PlannedAttack`, increment counters in player `BlockProgress` under that `ContextId`
  - [x] Counter keys for initial leaves: `played_Red`, `played_White`, `played_Black`
- [x] Test: debug actions “Simulate BlockCardPlayed (Red/White/Black)” increment counters; print snapshot
  - [x] Use Debug Menu → Combat Debug → “Phase 5 Test: Simulate BlockCardPlayed Red and Print Counters” after a `StartEnemyTurn` has planned intents

## Phase 6 — Condition evaluation service
- [x] `ConditionService`
  - [x] Evaluate `ConditionNode` with context (attacker, target, `BlockProgress` by `ContextId`)
  - [x] Composites: `All`, `Any`, `Not`
  - [x] Leaf: `PlayColorAtLeastN` (params: `color`, `n`)
- [x] Tests
  - [x] Simple unit-like checks on nodes with fake counters
  - [x] Debug button prints evaluation for the current planned attack

## Phase 7 — Attack resolution
- [x] `AttackResolutionSystem`
  - [x] On `ResolveAttack(ContextId)`, find `PlannedAttack`
  - [x] Evaluate `conditionsBlocked` via `ConditionService`
  - [x] If blocked → enqueue `ApplyEffect` for each `effectsOnBlocked`
  - [x] Else → enqueue `ApplyEffect` for each `effectsOnHit`
  - [x] Publish `AttackResolved`
- [x] Debug helpers
  - [x] “Resolve Next Intent” button publishes `ResolveAttack` for the first pending intent
  - [ ] Optional: auto-transition to Action to trigger resolution
- [ ] Tests: without BlockCardPlayed → on-hit damage; after `BlockCardPlayed(Red)` → on-blocked effects

## Phase 8 — Effect application
- [x] `EffectApplicationSystem`
  - [x] `Damage` → publish `ModifyHpEvent { Delta = -amount }`
  - [x] `ApplyStatus` → log-only for now (wire status later)
- [ ] Test: HP changes on on-hit; logs on on-blocked
  - [ ] Use Debug Menu → Combat Debug → “Phase 8 Test: Resolve (no block) then Resolve (blocked)” and observe effects/logs

## Phase 9 — UI: Enemy intents and attack resolution display (incremental)

### Phase 9A — Above-enemy intent pips (current turn)
- [x] `EnemyIntentPipsSystem` (UI)
  - [x] Render small circles above the enemy equal to count of `AttackIntent.Planned` for THIS turn
  - [x] Highlight the next-to-resolve pip (lowest `ResolveStep`)
- [ ] Tests
  - [ ] Plan intents (To Block Phase) → correct number of pips shown
  - [ ] Replan (StartEnemyTurn again) → pip count updates

### Phase 9B — Above-enemy intent pips (next turn preview)
- [ ] Extend data model or add `NextTurnAttackIntent` (simple stub list for now)
- [ ] Extend `EnemyIntentPipsSystem` to render a secondary row (smaller/faded) for NEXT turn planned count
- [ ] Tests
  - [ ] Manually seed next-turn intents via a debug action → second row appears with correct count

### Phase 9C — Attack resolution banner (skeleton)
- [ ] `EnemyAttackDisplaySystem` (center banner)
  - [ ] Display attack name and base damage (sum of on-hit “Damage” entries) for the CURRENT resolving context
  - [ ] Display list of blocking conditions (raw from `conditionsBlocked` tree; leaf-only listing for now)
- [ ] Tests
  - [ ] On resolve start (debug trigger), banner appears with name + base damage + condition list

### Phase 9D — Live condition status updates
- [ ] Bind condition evaluation to banner (green/red per leaf) using `ConditionService`
- [ ] Update when `BlockCardPlayed` events arrive (via existing tracker)
- [ ] Tests
  - [ ] Simulate `BlockCardPlayed Red` → corresponding leaf turns satisfied on banner

### Phase 9E — Damage prediction (full vs actual) with StoredBlock
- [ ] `DamagePredictionService`
  - [ ] Compute FullDamage: sum of on-hit Damage for current `AttackDefinition`
  - [ ] Track assigned block for the attack context (new counters in `BlockProgress`, e.g., `assignedBlockTotal`)
  - [ ] Compute ActualDamage = max(0, FullDamage - (StoredBlock.Amount + assignedBlockTotal))
- [ ] Extend banner to show “Full” vs “After Blocks” values
- [ ] Tests
  - [ ] With StoredBlock only → verify reduction shown
  - [ ] After assigning a blocking card (see Phase 10) → actual damage updates

### Phase 9F — Banner updates during resolution
- [ ] When an effect applies (e.g., Damage or ApplyStatus), ensure banner stays in sync until `AttackResolved`
- [ ] Tests
  - [ ] Resolve unblocked → banner shows full damage, then clears on `AttackResolved`
  - [ ] Resolve blocked → banner shows reduced damage and condition met, then clears

## Phase 10 — Using cards from hand as blocking (interaction)
- [ ] Events
  - [ ] `BlockAssignmentChanged { contextId, cardId, deltaBlock, color }`
- [ ] Systems
  - [ ] `HandBlockInteractionSystem`: allow selecting a hand card to assign/unassign to the CURRENT attack context during Block phase
  - [ ] Update `BlockProgress.Counters[contextId]["assignedBlockTotal"] += deltaBlock`
  - [ ] Publish `BlockCardPlayed` (for condition leaves) when appropriate
- [ ] UI
  - [ ] Simple affordance: click a hand card while in Block phase toggles assignment to current context
  - [ ] Visual feedback on assigned cards (outline/flag)
- [ ] Tests
  - [ ] Assign/unassign a card → counters change; banner’s actual damage updates
  - [ ] Condition leaves react to `BlockCardPlayed` emitted by assignment

## Phase 11 — Content and repository wiring
- [ ] Load repository at startup
  - [ ] Load path: `ECS/Data/Enemies/`
  - [ ] Provide repository to `EnemyIntentPlanningSystem`
- [ ] Ensure `demon_bite.json` matches the design doc
- [ ] Test: entering Block plans demon bite and can be resolved

## Phase 12 — Debug UX
- [ ] New debug tab: “Enemy Combat (JSON)”
  - [ ] Buttons: “Load Attacks”, “Plan Next Turn”, “Resolve Next Intent”
  - [ ] Buttons: “Simulate CardPlayed(Red/White/Black)”
  - [ ] Button: “Print BlockProgress snapshot”
- [ ] Test: each command works without crashes

## Phase 13 — Extensions (post-vertical slice)
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
