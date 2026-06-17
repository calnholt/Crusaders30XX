function hourglassIconMarkup(variant, { faded = false } = {}) {
  return `<time-icon variant="${variant}" icon-only${faded ? ' faded' : ''}></time-icon>`;
}

export function hourglassIconsMarkup(variant, count, activeCount = null) {
  const total = Math.max(0, Number(count) || 0);
  if (total === 0) {
    return '';
  }

  const active = activeCount == null ? total : Math.max(0, Math.min(Number(activeCount) || 0, total));

  return Array.from({ length: total }, (_, index) =>
    hourglassIconMarkup(variant, { faded: index >= active }),
  ).join('');
}

export function hourglassCycleMarkup(
  variant,
  interval,
  elapsed,
  remaining,
  { previewElapsed = elapsed, previewRemaining = remaining, fading = false } = {},
) {
  const total = Math.max(0, Number(interval) || 0);
  if (total === 0) {
    return '';
  }

  const depleted = fading ? previewElapsed : elapsed;
  const activeRemaining = fading ? previewRemaining : remaining;

  return Array.from({ length: total }, (_, index) => {
    const isDepleted = index < depleted;
    const isActive = index >= depleted && index < depleted + activeRemaining;
    return hourglassIconMarkup(variant, { faded: isDepleted || !isActive });
  }).join('');
}

export function timeCostMarkup(amount) {
  return `
    <span class="time-display time-display--cost">
      <span class="time-display__sign">+</span>
      ${hourglassIconsMarkup('white', amount)}
    </span>
  `;
}

export function timeLeavesDisplayMarkup(timing) {
  if (timing.remaining === 0) {
    return `<span class="time-display time-display--leaves time-display--gone">GONE</span>`;
  }

  const active = timing.fading ? timing.previewRemaining : timing.remaining;

  return `
    <span class="time-display time-display--leaves${timing.gone ? ' time-display--leaves-expiring' : ''}">
      ${hourglassIconsMarkup('red', timing.remaining, active)}
    </span>
  `;
}

export function timeRefreshDisplayMarkup(timing) {
  if (timing.remaining === 0) {
    return `<span class="time-display time-display--refresh time-display--imminent">REFRESHING</span>`;
  }

  return `
    <span class="time-display time-display--refresh${timing.refreshing ? ' time-display--refresh-expiring' : ''}">
      ${hourglassCycleMarkup('red', timing.interval, timing.elapsed, timing.remaining, {
        previewElapsed: timing.previewElapsed,
        previewRemaining: timing.previewRemaining,
        fading: timing.fading,
      })}
    </span>
  `;
}
