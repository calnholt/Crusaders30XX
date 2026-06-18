import './ResourceBar.css';
import { subscribe } from '../store/gameStore.js';
import { RESOURCE_TYPES } from '../utils/resources.js';

export class ResourceIcon extends HTMLElement {
  static get observedAttributes() {
    return ['type', 'amount'];
  }

  connectedCallback() {
    this.render();
  }

  attributeChangedCallback() {
    this.render();
  }

  render() {
    const type = this.getAttribute('type') ?? 'red';
    const amount = this.getAttribute('amount') ?? '1';
    this.innerHTML = `
      <span class="resource-icon resource-icon--${type}" aria-label="${type} ${amount}">
        <span class="resource-icon__shape"></span>
        <span class="resource-icon__amount">${amount}</span>
      </span>
    `;
  }
}

customElements.define('resource-icon', ResourceIcon);

export class ResourceBar extends HTMLElement {
  connectedCallback() {
    this.unsub = subscribe((state) => this.render(state));
  }

  disconnectedCallback() {
    this.unsub?.();
  }

  render(state) {
    const icons = RESOURCE_TYPES.map(
      (type) => `<resource-icon type="${type}" amount="${state.resources[type]}"></resource-icon>`,
    ).join('');

    this.innerHTML = `
      <div class="resource-bar">
        <span class="resource-bar__label">Resources</span>
        <div class="resource-bar__icons">${icons}</div>
      </div>
    `;
  }
}

customElements.define('resource-bar', ResourceBar);
