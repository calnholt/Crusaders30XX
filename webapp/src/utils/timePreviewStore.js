const listeners = new Set();

/** @type {{ slotId: string, amount: number } | null} */
let preview = null;

function notify() {
  for (const listener of listeners) {
    listener(preview);
  }
}

export function setTimePreview(slotId, amount) {
  if (!slotId || amount <= 0) {
    return;
  }
  preview = { slotId, amount };
  document.body.classList.add('time-preview-active');
  notify();
}

export function clearTimePreview() {
  if (!preview) {
    return;
  }
  preview = null;
  document.body.classList.remove('time-preview-active');
  notify();
}

export function subscribeTimePreview(listener) {
  listeners.add(listener);
  listener(preview);
  return () => listeners.delete(listener);
}

export function getTimePreview() {
  return preview;
}

export function isPreviewSource(slotId) {
  return preview?.slotId === slotId;
}

export function isPreviewBlocked(slotId) {
  return preview != null && preview.slotId !== slotId;
}
