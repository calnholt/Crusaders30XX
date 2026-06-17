import { subscribe } from '../store/gameStore.js';
import { subscribeHover, setHoveredSlot, clearHoveredSlot, getHoveredSlot } from '../utils/highlightStore.js';
import { subscribeTimePreview, getTimePreview } from '../utils/timePreviewStore.js';
import { buildTimeline } from '../utils/timeline.js';

function kindClass(kind) {
  return `timeline-lane--${kind}`;
}

export class TimelineDisplay extends HTMLElement {
  connectedCallback() {
    this.unsubState = subscribe((state) => this.render(state));
    this.unsubHover = subscribeHover(() => this.applyHighlight(getHoveredSlot()));
    this.unsubPreview = subscribeTimePreview(() => {
      this.render(this._lastState);
    });
  }

  disconnectedCallback() {
    this.unsubState?.();
    this.unsubHover?.();
    this.unsubPreview?.();
  }

  applyHighlight(slotId) {
    if (getTimePreview()) {
      return;
    }

    this.querySelectorAll('.timeline-lane').forEach((lane) => {
      lane.classList.toggle('timeline-lane--highlighted', lane.dataset.slotId === slotId);
    });

    document.querySelectorAll('.space-slot[data-slot-id]').forEach((slot) => {
      slot.classList.toggle('space-slot--highlighted', slot.dataset.slotId === slotId);
    });
  }

  render(state) {
    if (!state) {
      return;
    }

    this._lastState = state;
    const preview = getTimePreview();
    const timeline = buildTimeline(state, preview);
    const activeHover = preview ? null : getHoveredSlot();

    const tickMarkup = timeline.ticks
      .map(
        (tick) =>
          `<span class="timeline__tick" style="left: ${tick.left}%">T${tick.time}</span>`,
      )
      .join('');

    const laneMarkup = timeline.events
      .map((event) => {
        const laneClasses = [
          'timeline-lane',
          kindClass(event.kind),
          event.wouldVanish ? 'timeline-lane--would-vanish' : '',
          event.isPreviewSource ? 'timeline-lane--preview-source' : '',
        ]
          .filter(Boolean)
          .join(' ');

        const segments = event.segments
          .map((segment) => {
            const rangeLabel = `T${segment.start} - T${segment.end}`;
            return `
              <span
                class="timeline-lane__segment timeline-lane__segment--${segment.type}"
                style="left: ${segment.left}%; width: ${Math.max(segment.width, 0.5)}%; --segment-color: ${segment.color};"
                title="${rangeLabel}"
              ></span>
            `;
          })
          .join('');

        return `
          <div class="${laneClasses}" data-slot-id="${event.slotId}">
            <div class="timeline-lane__track">${segments}</div>
          </div>
        `;
      })
      .join('');

    const previewMarker = timeline.previewPercent != null
      ? `<span class="timeline__preview-marker" style="left: ${timeline.previewPercent}%"></span>`
      : '';

    this.innerHTML = `
      <div class="timeline ${preview ? 'timeline--previewing' : ''}">
        <div class="timeline__chart">
          <div class="timeline__axis">
            ${tickMarkup}
            <span class="timeline__now-marker" style="left: ${timeline.nowPercent}%"></span>
            ${previewMarker}
          </div>
          <div class="timeline__lanes">${laneMarkup}</div>
        </div>
      </div>
    `;

    if (!preview) {
      this.querySelectorAll('.timeline-lane').forEach((lane) => {
        lane.addEventListener('mouseenter', () => setHoveredSlot(lane.dataset.slotId));
        lane.addEventListener('mouseleave', () => clearHoveredSlot());
      });

      if (activeHover) {
        this.applyHighlight(activeHover);
      }
    }
  }
}

customElements.define('timeline-display', TimelineDisplay);
