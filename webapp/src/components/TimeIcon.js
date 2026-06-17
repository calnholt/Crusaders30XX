function hourglassSvgMarkup(fillLevel) {
  const sandOpacity =
    fillLevel === 'used' ? '1' : fillLevel === 'preview' ? '0.85' : '0';

  return `
    <svg class="time-icon__shape" viewBox="0 0 12 16" aria-hidden="true">
      <path
        class="time-icon__sand"
        d="M5.15 8.85 L6.85 8.85 L9.55 13.85 L2.45 13.85 Z"
        fill="currentColor"
        opacity="${sandOpacity}"
      ></path>
      <path
        class="time-icon__frame"
        d="M2 1.35 H10 L6.75 8 L10 14.65 H2 L5.25 8 Z"
        fill="none"
        stroke="currentColor"
        stroke-width="1.45"
        stroke-linejoin="round"
        stroke-linecap="round"
      ></path>
    </svg>
  `;
}

export class TimeIcon extends HTMLElement {
  static get observedAttributes() {
    return ['variant', 'amount', 'icon-only', 'faded', 'fill-level', 'meter'];
  }

  connectedCallback() {
    this.render();
  }

  attributeChangedCallback(name, oldValue, newValue) {
    if (oldValue !== newValue) {
      this.render();
    }
  }

  render() {
    const variant = this.getAttribute('variant') ?? 'white';
    const iconOnly = this.hasAttribute('icon-only');
    const faded = this.hasAttribute('faded');
    const isMeter = this.hasAttribute('meter');
    const fillLevel = this.getAttribute('fill-level') ?? (faded ? 'empty' : 'used');
    const amount = this.getAttribute('amount') ?? '1';
    const ariaLabel = iconOnly ? 'hourglass' : `${amount} time`;
    const amountMarkup = iconOnly ? '' : `<span class="time-icon__amount">${amount}</span>`;
    const classes = [
      'time-icon',
      `time-icon--${variant}`,
      iconOnly ? 'time-icon--solo' : '',
      faded ? 'time-icon--faded' : '',
      isMeter ? 'time-icon--meter' : '',
      `time-icon--fill-${fillLevel}`,
    ]
      .filter(Boolean)
      .join(' ');

    this.innerHTML = `
      <span class="${classes}" aria-label="${ariaLabel}">
        ${hourglassSvgMarkup(fillLevel)}
        ${amountMarkup}
      </span>
    `;
  }
}

customElements.define('time-icon', TimeIcon);
