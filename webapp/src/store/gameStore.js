import {
  rollShopSlot,
  rollEnemySlot,
  rollMysterySlot,
  rollInitialShopSlots,
  rollAllShopSlots,
  rollShopRefresh,
  SHOP_REFRESH_INTERVAL,
  getNextShopRefreshAt,
  getShopRefreshMarkerTimes,
  rollInitialEnemySlots,
  rollInitialMysterySlots,
  SHOP_SLOT_TYPES,
  getMysteryContentLabel,
  getMysteryContentText,
} from '../utils/rollSlots.js';
import {
  addRewards,
  canAfford,
  spendResources,
  emptyResources,
} from '../utils/resources.js';
import { getRemainingTimeAt } from '../utils/slotLeaveLabel.js';

const listeners = new Set();

let state = createInitialState();
let reveal = null;

function createInitialState() {
  return {
    currentTime: 0,
    resources: emptyResources(),
    shopRefresh: rollShopRefresh(),
    shopSlots: rollInitialShopSlots(),
    enemySlots: rollInitialEnemySlots(),
    mysterySlots: rollInitialMysterySlots(0),
    toast: null,
  };
}

function notify() {
  for (const listener of listeners) {
    listener(state, reveal);
  }
}

export function subscribe(listener) {
  listeners.add(listener);
  listener(state, reveal);
  return () => listeners.delete(listener);
}

export function getState() {
  return state;
}

export function getReveal() {
  return reveal;
}

export function clearReveal() {
  reveal = null;
  notify();
}

function setToast(message) {
  state = { ...state, toast: message };
  notify();
  setTimeout(() => {
    if (state.toast === message) {
      state = { ...state, toast: null };
      notify();
    }
  }, 2000);
}

export function advanceTime(amount) {
  state = { ...state, currentTime: state.currentTime + amount };
  processExpirations();
  notify();
}

function processExpirations() {
  let changed = false;
  let nextShop = state.shopSlots;
  let lastRefreshAt = state.shopRefresh.lastRefreshAt ?? 0;

  while (state.currentTime >= lastRefreshAt + SHOP_REFRESH_INTERVAL) {
    lastRefreshAt += SHOP_REFRESH_INTERVAL;
    changed = true;
    nextShop = rollAllShopSlots(lastRefreshAt);
  }

  let nextShopRefresh = state.shopRefresh;
  if (changed) {
    nextShopRefresh = {
      duration: SHOP_REFRESH_INTERVAL,
      lastRefreshAt,
    };
  }

  const nextEnemy = state.enemySlots.map((slot) => {
    if (isExpired(slot)) {
      changed = true;
      return rollEnemySlot(state.currentTime);
    }
    return slot;
  });

  const nextMystery = state.mysterySlots.map((slot) => {
    if (isMysteryExpired(slot)) {
      changed = true;
      return rollMysterySlot(state.currentTime);
    }
    return slot;
  });

  if (changed) {
    state = {
      ...state,
      shopRefresh: nextShopRefresh,
      shopSlots: nextShop,
      enemySlots: nextEnemy,
      mysterySlots: nextMystery,
    };
  }
}

export function getShopRefreshAtTime(state = getState()) {
  return getNextShopRefreshAt(state.currentTime);
}

export function wouldShopRefreshAt(time, state = getState()) {
  return time >= getNextShopRefreshAt(state.currentTime);
}

export { getShopRefreshMarkerTimes, getNextShopRefreshAt, SHOP_REFRESH_INTERVAL };

function isExpired(slot) {
  return state.currentTime >= slot.spawnTime + slot.duration;
}

function isMysteryExpired(slot) {
  if (state.currentTime > slot.windowEnd) {
    return true;
  }
  return isExpired(slot);
}

function isMysteryActive(slot) {
  return state.currentTime >= slot.windowStart && state.currentTime <= slot.windowEnd;
}

export function getAllSlots(state = getState()) {
  return [...state.shopSlots, ...state.enemySlots, ...state.mysterySlots];
}

export function wouldSlotExpireAt(slot, time, gameState = getState()) {
  if (slot.kind === 'shop') {
    return wouldShopRefreshAt(time, gameState);
  }
  if (slot.kind === 'mystery') {
    if (time > slot.windowEnd) {
      return true;
    }
    return time >= slot.spawnTime + slot.duration;
  }
  return time >= slot.spawnTime + slot.duration;
}

export function getWouldVanishSlotIds(state, preview) {
  if (!preview) {
    return new Set();
  }

  const previewTime = state.currentTime + preview.amount;
  const ids = new Set();

  for (const slot of getAllSlots(state)) {
    if (wouldSlotExpireAt(slot, previewTime)) {
      ids.add(slot.id);
    }
  }

  ids.add(preview.slotId);
  return ids;
}

export function interactShop(slotId) {
  const slot = state.shopSlots.find((s) => s.id === slotId);
  if (!slot) {
    return;
  }

  if (!canAfford(state.resources, slot.cost)) {
    setToast('Not enough resources');
    return;
  }

  const index = state.shopSlots.findIndex((s) => s.id === slotId);
  const purchasedName = slot.item.name;
  const newTime = state.currentTime + slot.clickCost;

  state = {
    ...state,
    currentTime: newTime,
    resources: spendResources(state.resources, slot.cost),
    shopSlots: state.shopSlots.map((s, i) =>
      i === index ? rollShopSlot(SHOP_SLOT_TYPES[index], newTime) : s,
    ),
  };

  processExpirations();
  notify();
  setToast(`Purchased ${purchasedName}`);
}

export function interactEnemy(slotId) {
  const slot = state.enemySlots.find((s) => s.id === slotId);
  if (!slot || isExpired(slot)) {
    return;
  }

  const index = state.enemySlots.findIndex((s) => s.id === slotId);
  const rewards = slot.rewards;
  const defeatedName = slot.enemy.name;
  const newTime = state.currentTime + slot.clickCost;

  state = {
    ...state,
    currentTime: newTime,
    resources: addRewards(state.resources, rewards),
    enemySlots: state.enemySlots.map((s, i) =>
      i === index ? rollEnemySlot(newTime) : s,
    ),
  };

  processExpirations();
  notify();
  setToast(`Defeated ${defeatedName}`);
}

export function interactMystery(slotId) {
  const slot = state.mysterySlots.find((s) => s.id === slotId);
  if (!slot || isMysteryExpired(slot)) {
    return;
  }

  if (!isMysteryActive(slot)) {
    setToast(`Opens T${slot.windowStart} - T${slot.windowEnd}`);
    return;
  }

  reveal = {
    title: getMysteryContentLabel(slot),
    text: getMysteryContentText(slot),
    contentType: slot.contentType,
  };

  const index = state.mysterySlots.findIndex((s) => s.id === slotId);
  const newTime = state.currentTime + slot.clickCost;

  state = {
    ...state,
    currentTime: newTime,
    mysterySlots: state.mysterySlots.map((s, i) =>
      i === index ? rollMysterySlot(newTime) : s,
    ),
  };

  processExpirations();
  notify();
}

export function getRemainingTime(slot) {
  return getRemainingTimeAt(slot, state.currentTime, state);
}

export function isSlotExpired(slot) {
  if (slot.kind === 'shop') {
    return false;
  }
  if (slot.kind === 'mystery') {
    return isMysteryExpired(slot);
  }
  return isExpired(slot);
}

export function isMysteryWindowActive(slot) {
  return isMysteryActive(slot);
}

export function getAccessibleMysterySlots(state = getState()) {
  return state.mysterySlots.filter(
    (slot) => isMysteryWindowActive(slot) && !isSlotExpired(slot),
  );
}

export function resetGame() {
  state = createInitialState();
  reveal = null;
  notify();
}
