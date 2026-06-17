import {
  subscribe,
  interactShop,
  getWouldVanishSlotIds,
} from '../store/gameStore.js';
import { canAfford } from '../utils/resources.js';
import { subscribeTimePreview, getTimePreview, clearTimePreview } from '../utils/timePreviewStore.js';
import { getProjectedResources } from '../utils/previewResources.js';
import { spaceSlotMarkup } from '../utils/spaceCardMarkup.js';
import { bindSpaceSlotInteractions } from '../utils/spaceCardInteractions.js';
import { spaceTimeBlockMarkup } from '../utils/spaceTimeBlock.js';

export class ShopSpace extends HTMLElement {
  static get observedAttributes() {
    return ['slot-id'];
  }

  connectedCallback() {
    this.unsub = subscribe((state) => this.render(state));
    this.unsubPreview = subscribeTimePreview(() => this.applyPreview());
  }

  disconnectedCallback() {
    this.unsub?.();
    this.unsubPreview?.();
  }

  get slotId() {
    return this.getAttribute('slot-id');
  }

  applyPreview() {
    const preview = getTimePreview();
    const slotEl = this.querySelector('.space-slot');
    if (!slotEl || !this._state) {
      return;
    }
    const slot = this._state.shopSlots.find((s) => s.id === this.slotId);
    if (!slot) {
      return;
    }
    const vanishIds = getWouldVanishSlotIds(this._state, preview);
    slotEl.classList.toggle('space-slot--would-vanish', vanishIds.has(this.slotId));
    slotEl.classList.toggle('space-slot--preview-source', preview?.slotId === this.slotId);
    this.updateAvailability(slotEl, slot, preview);
  }

  getAvailability(slot, preview = getTimePreview()) {
    const resources = getProjectedResources(this._state, preview);
    const affordable = canAfford(resources, slot.cost);
    return {
      affordable,
      unavailable: !affordable,
      disabled: !affordable,
    };
  }

  updateAvailability(slotEl, slot, preview = getTimePreview()) {
    const { unavailable } = this.getAvailability(slot, preview);
    slotEl.classList.toggle('space-slot--unavailable', unavailable);
  }

  render(state) {
    this._state = state;
    const slot = state.shopSlots.find((s) => s.id === this.slotId);
    if (!slot) {
      this.innerHTML = '';
      return;
    }

    const { unavailable, disabled } = this.getAvailability(slot);

    const compactHtml = `
      <span class="space-card-compact__title">${slot.item.name}</span>
      <span class="space-card-compact__badge">${slot.label}</span>
      <span class="space-card-compact__meta">
        <span class="space-card-compact__meta-primary">
          <resource-cost class="space-card-compact__cost" label="PRICE"></resource-cost>
        </span>
        ${spaceTimeBlockMarkup(slot.clickCost)}
      </span>
    `;

    this.innerHTML = spaceSlotMarkup({
      slotId: slot.id,
      kind: 'shop',
      expired: false,
      unavailable,
      compactHtml,
    });

    const slotEl = this.querySelector('.space-slot');
    bindSpaceSlotInteractions(slotEl, {
      slotId: slot.id,
      clickCost: slot.clickCost,
      advancesTime: !disabled,
      disabled,
    });
    this.querySelector('.space-card-compact__cost').cost = slot.cost;

    const compact = slotEl.querySelector('.space-card-compact');
    compact.addEventListener('click', () => {
      clearTimePreview();
      interactShop(this.slotId);
    });

    this.applyPreview();
  }
}

customElements.define('shop-space', ShopSpace);
