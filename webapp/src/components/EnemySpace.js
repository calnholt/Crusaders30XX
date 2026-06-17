import {
  subscribe,
  interactEnemy,
  isSlotExpired,
  getWouldVanishSlotIds,
} from '../store/gameStore.js';
import { subscribeTimePreview, getTimePreview, clearTimePreview } from '../utils/timePreviewStore.js';
import { spaceSlotMarkup } from '../utils/spaceCardMarkup.js';
import { bindSpaceSlotInteractions } from '../utils/spaceCardInteractions.js';
import { getLeavesTiming } from '../utils/slotLeaveLabel.js';
import { applyLeaveLabel } from '../utils/spaceLeaveLabel.js';
import { spaceTimeBlockMarkup } from '../utils/spaceTimeBlock.js';

export class EnemySpace extends HTMLElement {
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
    const slot = this._state.enemySlots.find((s) => s.id === this.slotId);
    if (!slot) {
      return;
    }
    const vanishIds = getWouldVanishSlotIds(this._state, preview);
    slotEl.classList.toggle('space-slot--would-vanish', vanishIds.has(this.slotId));
    slotEl.classList.toggle('space-slot--preview-source', preview?.slotId === this.slotId);
    applyLeaveLabel(slotEl, slot, this._state);
  }

  render(state) {
    this._state = state;
    const slot = state.enemySlots.find((s) => s.id === this.slotId);
    if (!slot) {
      this.innerHTML = '';
      return;
    }

    const expired = isSlotExpired(slot);
    const leavesTiming = getLeavesTiming(slot, state);

    const compactHtml = `
      <enemy-portrait
        class="space-card-compact__portrait"
        enemy-id="${slot.enemy.id}"
        enemy-name="${slot.enemy.name}"
      ></enemy-portrait>
      <div class="space-card-compact__details">
        <span class="space-card-compact__meta">
          <span class="space-card-compact__meta-primary">
            <resource-reward class="space-card-compact__rewards" label="GAIN"></resource-reward>
          </span>
          ${spaceTimeBlockMarkup(slot.clickCost, leavesTiming)}
        </span>
      </div>
    `;

    this.innerHTML = spaceSlotMarkup({
      slotId: slot.id,
      kind: 'enemy',
      expired,
      compactHtml,
    });

    const slotEl = this.querySelector('.space-slot');
    bindSpaceSlotInteractions(slotEl, {
      slotId: slot.id,
      clickCost: slot.clickCost,
      advancesTime: !expired,
      disabled: expired,
    });

    slotEl.querySelector('.space-card-compact__rewards').rewards = slot.rewards;

    const compact = slotEl.querySelector('.space-card-compact');
    compact.addEventListener('click', () => {
      if (expired) {
        return;
      }
      clearTimePreview();
      interactEnemy(this.slotId);
    });

    this.applyPreview();
  }
}

customElements.define('enemy-space', EnemySpace);
