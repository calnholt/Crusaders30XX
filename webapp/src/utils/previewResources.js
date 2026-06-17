import { addRewards } from './resources.js';
import { getAllSlots, isSlotExpired } from '../store/gameStore.js';

export function getPreviewSourceSlot(state, preview) {
  if (!preview) {
    return null;
  }
  return getAllSlots(state).find((slot) => slot.id === preview.slotId) ?? null;
}

export function getProjectedResources(state, preview) {
  const source = getPreviewSourceSlot(state, preview);
  if (source?.kind === 'enemy' && !isSlotExpired(source)) {
    return addRewards(state.resources, source.rewards);
  }
  return state.resources;
}
