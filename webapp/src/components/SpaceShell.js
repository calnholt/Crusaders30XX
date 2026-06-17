export class SpaceShell extends HTMLElement {
  set data(value) {
    this._data = value;
    this.render();
  }

  connectedCallback() {
    this.render();
  }

  render() {
    const {
      title,
      subtitle,
      bodyHtml = '',
      actionLabel = 'Act',
      clickCost = 1,
      disabled = false,
      slotId = '',
      advancesTime = false,
    } = this._data ?? {};

    this.innerHTML = `
      <article class="space-shell ${disabled ? 'space-shell--disabled' : ''}">
        <header class="space-shell__header">
          <h3 class="space-shell__title">${title ?? ''}</h3>
          ${subtitle ? `<p class="space-shell__subtitle">${subtitle}</p>` : ''}
        </header>
        <div class="space-shell__body">${bodyHtml}</div>
        <footer class="space-shell__footer">
          <slot name="meta"></slot>
          <time-action-button
            slot-id="${slotId}"
            cost="${clickCost}"
            label="${actionLabel}"
            advances-time="${advancesTime ? 'true' : 'false'}"
            ${disabled ? 'disabled="true"' : ''}
          ></time-action-button>
        </footer>
      </article>
    `;
  }
}

customElements.define('space-shell', SpaceShell);
