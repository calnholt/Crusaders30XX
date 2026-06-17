import { subscribeTimePreview, setTimePreview, clearTimePreview, isPreviewBlocked, isPreviewSource } from '../utils/timePreviewStore.js';

export class TimeActionButton extends HTMLElement {
  static get observedAttributes() {
    return ['cost', 'label', 'disabled', 'slot-id', 'advances-time'];
  }

  connectedCallback() {
    this.render();
    this.addEventListener('click', this.handleClick);
    this.unsubPreview = subscribeTimePreview(() => this.updatePreviewState());
    this.addEventListener('pointerenter', this.handlePointerEnter);
    this.addEventListener('pointerleave', this.handlePointerLeave);
  }

  disconnectedCallback() {
    this.removeEventListener('click', this.handleClick);
    this.unsubPreview?.();
    this.removeEventListener('pointerenter', this.handlePointerEnter);
    this.removeEventListener('pointerleave', this.handlePointerLeave);
  }

  attributeChangedCallback(name, oldValue, newValue) {
    if (oldValue === newValue) {
      return;
    }

    if (name === 'disabled' || name === 'advances-time') {
      this.updatePreviewState();
      return;
    }

    this.render();
    this.updatePreviewState();
  }

  get slotId() {
    return this.getAttribute('slot-id');
  }

  canAdvanceTime() {
    return (
      this.getAttribute('advances-time') === 'true'
      && this.getAttribute('disabled') !== 'true'
      && !isPreviewBlocked(this.slotId)
    );
  }

  handlePointerEnter = () => {
    if (!this.canAdvanceTime()) {
      return;
    }
    const amount = Number.parseInt(this.getAttribute('cost') ?? '1', 10);
    setTimePreview(this.slotId, amount);
  };

  handlePointerLeave = () => {
    if (isPreviewSource(this.slotId)) {
      clearTimePreview();
    }
  };

  handleClick = () => {
    const button = this.querySelector('button');
    if (button?.disabled) {
      return;
    }
    clearTimePreview();
    this.dispatchEvent(new CustomEvent('time-action', { bubbles: true }));
  };

  updatePreviewState() {
    const button = this.querySelector('button');
    if (!button) {
      return;
    }

    const baseDisabled = this.getAttribute('disabled') === 'true';
    const blocked = isPreviewBlocked(this.slotId);
    const previewing = isPreviewSource(this.slotId);

    button.disabled = baseDisabled || blocked;
    this.classList.toggle('time-action-button-host--previewing', previewing);
    this.classList.toggle('time-action-button-host--preview-blocked', blocked);
  }

  render() {
    const cost = this.getAttribute('cost') ?? '1';
    const label = this.getAttribute('label') ?? 'Act';
    const baseDisabled = this.getAttribute('disabled') === 'true';

    this.innerHTML = `
      <button class="time-action-button" type="button" ${baseDisabled ? 'disabled' : ''}>
        <span class="time-action-button__label">${label}</span>
        <span class="time-action-button__cost">+${cost} time</span>
      </button>
    `;
  }
}

customElements.define('time-action-button', TimeActionButton);

export class DurationIndicator extends HTMLElement {
  set leaveLabel(value) {
    this._leaveLabel = value;
    this.render();
  }

  /** @deprecated use leaveLabel */
  set remaining(value) {
    this._leaveLabel = `leaves in ${value} time`;
    this.render();
  }

  connectedCallback() {
    this.render();
  }

  render() {
    const label = this._leaveLabel ?? 'leaves in 0 time (T0)';
    const gone = label === 'GONE';
    this.innerHTML = `
      <div class="duration-indicator ${gone ? 'duration-indicator--gone' : ''}">
        <span class="duration-indicator__label">Duration</span>
        <span class="duration-indicator__value">${label}</span>
      </div>
    `;
  }
}

customElements.define('duration-indicator', DurationIndicator);
