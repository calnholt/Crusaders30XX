import { getLeavesTiming } from './slotLeaveLabel.js';
import { timeLeavesDisplayMarkup } from './timeDisplay.js';
import { getTimePreview } from './timePreviewStore.js';

export function applyLeaveLabel(slotEl, slot, state) {
  const preview = getTimePreview();
  const timing = getLeavesTiming(slot, state, preview);

  const leavesEl = slotEl.querySelector('.space-card-compact__time-block__leaves');
  if (leavesEl) {
    leavesEl.innerHTML = timeLeavesDisplayMarkup(timing);
    leavesEl.classList.toggle(
      'space-card-compact__time-block__leaves--gone',
      timing.remaining === 0,
    );
  }
}
