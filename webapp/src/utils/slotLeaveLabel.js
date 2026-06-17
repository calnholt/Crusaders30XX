import {
  wouldSlotExpireAt,
  getShopRefreshAtTime,
  getNextShopRefreshAt,
  SHOP_REFRESH_INTERVAL,
} from '../store/gameStore.js';

export function getSlotExpiresAt(slot, state = null) {
  if (slot.kind === 'shop') {
    return getShopRefreshAtTime(state);
  }
  const lifetimeEnd = slot.spawnTime + slot.duration;
  if (slot.kind === 'mystery') {
    return Math.min(slot.windowEnd + 1, lifetimeEnd);
  }
  return lifetimeEnd;
}

export function getRemainingTimeAt(slot, atTime, state = null) {
  return Math.max(0, getSlotExpiresAt(slot, state) - atTime);
}

export function getLeavesTiming(slot, state, preview = null) {
  const expiresAt = getSlotExpiresAt(slot, state);
  const remaining = Math.max(0, expiresAt - state.currentTime);
  let previewRemaining = remaining;
  let gone = false;
  let fading = false;

  if (preview && preview.slotId !== slot.id) {
    const atTime = state.currentTime + preview.amount;
    if (wouldSlotExpireAt(slot, atTime, state)) {
      gone = true;
      previewRemaining = 0;
    } else {
      previewRemaining = Math.max(0, expiresAt - atTime);
    }
    fading = previewRemaining < remaining;
  }

  return { remaining, previewRemaining, expiresAt, gone, fading };
}

export function getRefreshTiming(state, preview = null) {
  const refreshAt = getShopRefreshAtTime(state);
  const remaining = Math.max(0, refreshAt - state.currentTime);
  const elapsed = SHOP_REFRESH_INTERVAL - remaining;
  let previewRemaining = remaining;
  let previewElapsed = elapsed;
  let refreshing = false;
  let fading = false;

  if (preview) {
    const atTime = state.currentTime + preview.amount;
    const previewRefreshAt = getNextShopRefreshAt(atTime);
    previewRemaining = Math.max(0, previewRefreshAt - atTime);
    previewElapsed = SHOP_REFRESH_INTERVAL - previewRemaining;
    refreshing = previewRemaining === 0;
    fading = previewRemaining !== remaining || previewElapsed !== elapsed;
  }

  return {
    interval: SHOP_REFRESH_INTERVAL,
    remaining,
    elapsed,
    previewRemaining,
    previewElapsed,
    refreshAt,
    refreshing,
    fading,
  };
}

export function formatLeavesLabelAt(slot, atTime, state = null) {
  const expiresAt = getSlotExpiresAt(slot, state);
  const remaining = Math.max(0, expiresAt - atTime);
  return `leaves in ${remaining} time (T${expiresAt})`;
}

export function formatShopRefreshLabel(state, preview = null) {
  const timing = getRefreshTiming(state, preview);
  if (timing.refreshing) {
    return 'REFRESHING';
  }
  return `refreshes in ${timing.remaining} time (T${timing.refreshAt})`;
}

export function isRefreshingLabel(label) {
  return label === 'REFRESHING';
}

export function formatLeavesLabel(slot, state, preview = null) {
  const timing = getLeavesTiming(slot, state, preview);
  if (timing.gone) {
    return 'GONE';
  }
  return formatLeavesLabelAt(slot, preview ? state.currentTime + preview.amount : state.currentTime, state);
}

export function isGoneLabel(label) {
  return label === 'GONE';
}
