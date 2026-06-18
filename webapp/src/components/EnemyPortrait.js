import './EnemyPortrait.css';
import { getEnemyPortraitUrl } from '../utils/enemyAssets.js';

export class EnemyPortrait extends HTMLElement {
  static get observedAttributes() {
    return ['enemy-id', 'enemy-name', 'variant'];
  }

  connectedCallback() {
    this.classList.add('enemy-portrait');
    this.render();
  }

  attributeChangedCallback(name, oldValue, newValue) {
    if (oldValue !== newValue) {
      this.render();
    }
  }

  render() {
    const enemyId = this.getAttribute('enemy-id');
    const enemyName = this.getAttribute('enemy-name') ?? enemyId ?? 'Enemy';
    const isHeadshot = this.getAttribute('variant') === 'headshot';
    const url = getEnemyPortraitUrl(enemyId);

    this.classList.toggle('enemy-portrait--headshot', isHeadshot);

    if (!url) {
      this.showPlaceholder(enemyId);
      return;
    }

    if (this._enemyId === enemyId && this._variant === this.getAttribute('variant') && this.querySelector('.enemy-portrait__img')) {
      this.classList.toggle('enemy-portrait--headshot', isHeadshot);
      return;
    }

    this._variant = this.getAttribute('variant');
    this._enemyId = enemyId;
    this.classList.remove('enemy-portrait--placeholder');
    this.innerHTML = `<img class="enemy-portrait__img" alt="${enemyName}" />`;

    const img = this.querySelector('.enemy-portrait__img');
    img.addEventListener('error', () => this.showPlaceholder(enemyId));
    img.src = url;
  }

  showPlaceholder(enemyId) {
    this._enemyId = enemyId;
    this.classList.add('enemy-portrait--placeholder');
    this.innerHTML = '';
  }
}

customElements.define('enemy-portrait', EnemyPortrait);
