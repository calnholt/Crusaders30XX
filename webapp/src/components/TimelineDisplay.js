import './TimelineDisplay.css';
import { subscribe } from '../store/gameStore.js';
import { subscribeTimePreview, getTimePreview } from '../utils/timePreviewStore.js';
import { buildTimeMeterMarkup } from '../utils/timeMeter.js';

export class TimelineDisplay extends HTMLElement {
  connectedCallback() {
    this.unsubState = subscribe((state) => this.render(state));
    this.unsubPreview = subscribeTimePreview(() => {
      this.render(this._lastState);
    });
  }

  disconnectedCallback() {
    this.unsubState?.();
    this.unsubPreview?.();
  }

  render(state) {
    if (!state) {
      return;
    }

    this._lastState = state;
    const preview = getTimePreview();
    this.innerHTML = buildTimeMeterMarkup(state, preview);
  }
}

customElements.define('timeline-display', TimelineDisplay);
