import { subscribe, clearReveal } from '../store/gameStore.js';

export class RevealModal extends HTMLElement {
  connectedCallback() {
    this.unsub = subscribe((_state, reveal) => this.render(reveal));
    this.addEventListener('click', this.handleBackdropClick);
  }

  disconnectedCallback() {
    this.unsub?.();
    this.removeEventListener('click', this.handleBackdropClick);
  }

  handleBackdropClick = (event) => {
    if (event.target.classList.contains('reveal-modal__backdrop')) {
      clearReveal();
    }
  };

  render(reveal) {
    if (!reveal) {
      this.innerHTML = '';
      this.classList.remove('reveal-modal--open');
      return;
    }

    this.classList.add('reveal-modal--open');
    this.innerHTML = `
      <div class="reveal-modal__backdrop">
        <div class="reveal-modal__panel" role="dialog" aria-modal="true">
          <h2 class="reveal-modal__title">${reveal.title}</h2>
          <div class="reveal-modal__rule"></div>
          <p class="reveal-modal__text">${reveal.text}</p>
          <p class="reveal-modal__stub">Effect applied (mockup)</p>
          <button class="reveal-modal__close" type="button">Continue</button>
        </div>
      </div>
    `;

    this.querySelector('.reveal-modal__close').addEventListener('click', clearReveal, { once: true });
  }
}

customElements.define('reveal-modal', RevealModal);

export class ToastDisplay extends HTMLElement {
  connectedCallback() {
    this.unsub = subscribe((state) => this.render(state));
  }

  disconnectedCallback() {
    this.unsub?.();
  }

  render(state) {
    if (!state.toast) {
      this.innerHTML = '';
      return;
    }

    this.innerHTML = `<div class="toast">${state.toast}</div>`;
  }
}

customElements.define('toast-display', ToastDisplay);
