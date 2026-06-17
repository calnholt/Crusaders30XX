import { formatCost } from '../utils/resources.js';

export class ResourceCost extends HTMLElement {
  set cost(value) {
    this._cost = value;
    this.render();
  }

  connectedCallback() {
    this.render();
  }

  render() {
    const label = this.getAttribute('label');
    const entries = formatCost(this._cost ?? {});
    if (entries.length === 0) {
      if (label) {
        this.innerHTML = `
          <div class="resource-cost">
            <span class="resource-cost__label">${label}</span>
            <span class="resource-cost__icons resource-cost__icons--empty">Free</span>
          </div>
        `;
        return;
      }
      this.innerHTML = '<span class="resource-cost resource-cost--empty">Free</span>';
      return;
    }

    const icons = entries
      .map(({ type, amount }) => `<resource-icon type="${type}" amount="${amount}"></resource-icon>`)
      .join('');

    if (label) {
      this.innerHTML = `
        <div class="resource-cost">
          <span class="resource-cost__label">${label}</span>
          <div class="resource-cost__icons">${icons}</div>
        </div>
      `;
      return;
    }

    this.innerHTML = `<div class="resource-cost">${icons}</div>`;
  }
}

customElements.define('resource-cost', ResourceCost);

export class ResourceReward extends HTMLElement {
  set rewards(value) {
    this._rewards = value ?? [];
    this.render();
  }

  connectedCallback() {
    this.render();
  }

  render() {
    const label = this.getAttribute('label') ?? 'Reward';
    const icons = (this._rewards ?? [])
      .map(({ type, amount }) => `<resource-icon type="${type}" amount="${amount}"></resource-icon>`)
      .join('');

    this.innerHTML = `
      <div class="resource-reward">
        <span class="resource-reward__label">${label}</span>
        <div class="resource-reward__icons">${icons || '<span class="muted">None</span>'}</div>
      </div>
    `;
  }
}

customElements.define('resource-reward', ResourceReward);
