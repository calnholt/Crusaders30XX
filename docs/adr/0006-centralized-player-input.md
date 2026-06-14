# Centralize player input behind immutable frames

All player hardware input is sampled once per game update by `MonoGamePlayerInputAdapter`. No gameplay, UI, diagnostic, or display system may reference MonoGame keyboard, mouse, or gamepad types directly. The adapter converts hardware state into an immutable `PlayerInputFrame` containing virtual cursor coordinates, axes, active-device metadata, button levels, and press and release edges.

ECS updates run in explicit `Input`, `Interaction`, `Gameplay`, and `Presentation` phases, followed by the existing late-update pass. `PlayerInputSystem` runs in `Input`, resolves the rendered cursor position and cursor target, stores the frame on `PlayerInputState`, and publishes `PlayerInputEvent`, `PlayerCommandEvent`, and the cursor presentation event. `UIInteractionSystem` runs in `Interaction` and exclusively owns UI hover, click reset, eligibility gates, and `UIElementEventType` dispatch.

Cursor and command routing use `InputContext` roots and `InputContextMember` controls. The highest-priority active modal context wins. Diagnostic contexts override cursor routing only while the rendered cursor intersects one of their entity-backed regions, so open diagnostics do not disable the rest of the screen. Hotkeys use the command context and own both immediate and held activation; the progress-ring system only renders hold state.

The OS cursor is always hidden. Mouse movement updates the rendered virtual cursor, and gamepad movement updates the same cursor state. Only the target resolved for that rendered cursor can receive pointer interaction.

Display methods do not poll hardware. New controls with behavior or bounds must be entities with `Transform`, `UIElement`, and context membership where applicable. An architecture test rejects MonoGame input references outside the hardware adapter.
