import { wouldSlotExpireAt } from '../store/gameStore.js';

export function getSlotExpiresAt(slot) {
  const lifetimeEnd = slot.spawnTime + slot.duration;
  if (slot.kind === 'mystery') {
    return Math.min(slot.windowEnd + 1, lifetimeEnd);
  }
  return lifetimeEnd;
}

export function getRemainingTimeAt(slot, atTime) {
  return Math.max(0, getSlotExpiresAt(slot) - atTime);
}

export function formatLeavesLabelAt(slot, atTime) {
  const expiresAt = getSlotExpiresAt(slot);
  const remaining = Math.max(0, expiresAt - atTime);
  return `leaves in ${remaining} time (T${expiresAt})`;
}

export function formatLeavesLabel(slot, state, preview = null) {
  if (preview && preview.slotId !== slot.id) {
    const previewTime = state.currentTime + preview.amount;
    if (wouldSlotExpireAt(slot, previewTime)) {
      return 'GONE';
    }
    return formatLeavesLabelAt(slot, previewTime);
  }

  return formatLeavesLabelAt(slot, state.currentTime);
}

export function isGoneLabel(label) {
  return label === 'GONE';
}
