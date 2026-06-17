const listeners = new Set();
let hoveredSlotId = null;

function notify() {
  for (const listener of listeners) {
    listener(hoveredSlotId);
  }
}

export function setHoveredSlot(slotId) {
  if (hoveredSlotId === slotId) {
    return;
  }
  hoveredSlotId = slotId;
  notify();
}

export function clearHoveredSlot() {
  setHoveredSlot(null);
}

export function subscribeHover(listener) {
  listeners.add(listener);
  listener(hoveredSlotId);
  return () => listeners.delete(listener);
}

export function getHoveredSlot() {
  return hoveredSlotId;
}

export function bindSlotHover(element, slotId) {
  element.dataset.slotId = slotId;
  element.onmouseenter = () => setHoveredSlot(slotId);
  element.onmouseleave = () => clearHoveredSlot();
}
