# Narrative Event Modal UI Plan

Implementation reference for `NarrativeEventModalDisplaySystem`. See attached grill decisions in git history.

**Mockup:** `mockups/quest-event-modal-v1.html`  
**Snapshot id:** `narrative-event-modal`  
**Virtual resolution:** 1920x1080

## Layout (fixed 520x920 modal)

- Body padding: 40/40/28; stack gap 20
- Footer: padding 20; option height 64; gap 20
- Footer height: `40 + visibleCount * 64 + max(0, visibleCount - 1) * 20`
- Fonts: Title 0.281, body 0.172, options 0.133 (native 128)

## Draw order

Dim, shadow, modal+footer shell, title, red rule, wrapped body, option buttons.

## Events

- Open: `ShowNarrativeEventOverlay`
- Close teardown: `NarrativeEventOverlayClosedEvent`
