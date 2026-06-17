import { formatLeavesLabel, isGoneLabel } from './slotLeaveLabel.js';
import { getTimePreview } from './timePreviewStore.js';

export function applyLeaveLabel(slotEl, slot, state) {
  const preview = getTimePreview();
  const label = formatLeavesLabel(slot, state, preview);

  const leavesEl = slotEl.querySelector('.space-card-compact__time-block__leaves');
  if (leavesEl) {
    leavesEl.textContent = label;
    leavesEl.classList.toggle('space-card-compact__time-block__leaves--gone', isGoneLabel(label));
  }
}
