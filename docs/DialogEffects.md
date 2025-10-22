# Dialog Effects Reference

Inline, nestable tags to control per-character text effects and reveal speed in dialog.

## Syntax
- Tag form: `[name key=value key2=value2]text[/name]`
- Nesting: tags can contain other tags; effects combine
- Escaping: use `[[` to render `[` and `]]` to render `]`

## Timing
- Typewriter is per-character. Each glyph uses a speed multiplier from surrounding `[speed]`, `[fast]`, `[slow]` tags.
- Reveal cost per glyph: `1 / (CharsPerSecond * speedFactor)` seconds.
- Some effects use reveal-relative time (e.g., explode pop) so each glyph animates as it appears.

## Effects

- jitter: random micro jitter
  - params: `amp` (px), `freq` (hz)
  - example: `[jitter amp=2 freq=12]shaky[/jitter]`

- shake: horizontal shake
  - params: `amp` (px), `freq` (hz)
  - example: `[shake amp=4]No![/shake]`

- big: scale up
  - params: `scale` (factor)
  - example: `[big scale=1.6]Big word[/big]`

- small: scale down
  - params: `scale` (factor)
  - example: `[small scale=0.75]tiny[/small]`

- nod: vertical nodding
  - params: `amp` (px), `freq` (hz)

- ripple: vertical wave along the text
  - params: `amp` (px), `wavelength` (px), `speed` (hz)

- explode: pop on reveal
  - params: `duration` (sec), `strength` (scale)
  - example: `boom [explode duration=0.25 strength=1.4]![/explode]`

- bloom: soft glow under text
  - params: `radius` (px), `intensity` (0..1), `passes` (int)
  - note: implemented as multiple blurred under-draws; tune in Debug panel

- speed: change typewriter speed
  - params: `factor` (multiplier)
  - example: `[speed factor=0.5]sloooow...[/speed] then [fast]FAST![/fast]`
  - aliases: `[fast]` and `[slow]` use debug tunables

## Combining effects
- Position offsets (jitter, shake, nod, ripple) add
- Scales (big, small, explode) multiply
- Colors/alpha multiply (currently default white; future tags can tint)

## Debug settings (Dialog Overlay)
- Enable Effects
- Jitter Amp/Freq, Shake Amp/Freq, Nod Amp/Freq
- Ripple Amp/Wavelength/Speed
- Big Scale, Small Scale
- Pop Duration, Pop Scale
- Bloom Radius/Intensity/Passes
- Fast Speed x, Slow Speed x

## Examples
- Dramatic reveal:
  `[slow][small]I...[/small][/slow] [speed factor=1.5][big][explode]WON'T![/explode][/big][/speed]`

- Wavy spooky text:
  `[ripple amp=3 wavelength=36 speed=1.5]h̷e̷l̷l̷o̷[/ripple]`

- Excited stutter:
  `[jitter amp=2]W-wait![/jitter]`

## Notes
- Unknown or malformed tags render as plain text.
- Effects are only active in dialog; other UI text is unaffected.



