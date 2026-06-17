import { bindSlotHover } from './highlightStore.js';
import {
  setTimePreview,
  clearTimePreview,
  isPreviewSource,
  isPreviewBlocked,
} from './timePreviewStore.js';

export function canAdvanceTime({ advancesTime, disabled, slotId }) {
  return advancesTime && !disabled && !isPreviewBlocked(slotId);
}

export function bindSpaceSlotInteractions(slotEl, { slotId, clickCost, advancesTime, disabled }) {
  const ring = slotEl.querySelector('.space-slot__ring');
  const compact = slotEl.querySelector('.space-card-compact');

  if (!ring || !compact) {
    return;
  }

  bindSlotHover(ring, slotId);

  const previewOptions = { slotId, clickCost, advancesTime, disabled };

  if (canAdvanceTime(previewOptions)) {
    compact.classList.add('space-card-compact--previewable');
  }

  compact.addEventListener('pointerenter', () => {
    if (!canAdvanceTime(previewOptions)) {
      return;
    }
    setTimePreview(slotId, clickCost);
  });

  compact.addEventListener('pointerleave', () => {
    if (isPreviewSource(slotId)) {
      clearTimePreview();
    }
  });
}
