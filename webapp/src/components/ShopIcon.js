const SHOP_ICON_SVG = `
  <svg class="shop-icon__shape" viewBox="0 0 16 16" aria-hidden="true">
    <path d="M1 5.25h14v2.25H1V5.25Z" fill="currentColor"/>
    <path d="M1 5.25h2.33v2.25H1V5.25Zm2.33 0h2.34v2.25H3.33V5.25Zm2.34 0h2.33v2.25H5.67V5.25Zm2.33 0h2.34v2.25H8V5.25Zm2.34 0h2.33v2.25h-2.33V5.25Zm2.33 0H15v2.25h-2.33V5.25Z" fill="currentColor" opacity="0.35"/>
    <path d="M2.25 7.5h2.25v7H2.25V7.5Zm9.25 0H13.75v7H11.5V7.5Z" fill="currentColor"/>
    <path d="M5.25 9.25h5.5v5.25h-5.5V9.25Z" fill="none" stroke="currentColor" stroke-width="1.25" stroke-linejoin="round"/>
    <path d="M5.25 11.25h5.5" fill="none" stroke="currentColor" stroke-width="1.1" stroke-linecap="round"/>
  </svg>
`;

export class ShopIcon extends HTMLElement {
  static get observedAttributes() {
    return ['size'];
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
    const size = this.getAttribute('size') ?? 'default';
    this.className = `shop-icon shop-icon--${size}`;
    this.innerHTML = SHOP_ICON_SVG;
  }
}

customElements.define('shop-icon', ShopIcon);

export function shopIconMarkup(size = 'default') {
  return `<shop-icon size="${size}"></shop-icon>`;
}
