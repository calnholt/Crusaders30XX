function slotEnd(slot) {
  return slot.spawnTime + slot.duration;
}

function shadeIndex(index, total) {
  if (total <= 1) {
    return 0.5;
  }
  return index / (total - 1);
}

function colorForSlot(kind, index, total) {
  const t = shadeIndex(index, total);

  switch (kind) {
    case 'shop': {
      const lightness = 52 + t * 38;
      return {
        lifetime: `hsl(0 0% ${lightness}%)`,
        window: `hsl(0 0% ${Math.min(lightness + 10, 96)}%)`,
      };
    }
    case 'mystery': {
      const lightness = 14 + t * 28;
      return {
        lifetime: `hsl(0 0% ${lightness}%)`,
        window: `hsl(0 0% ${lightness + 14}%)`,
      };
    }
    case 'enemy':
    default: {
      const hue = 350 + t * 16;
      const saturation = 62 + t * 12;
      const lightness = 38 + t * 14;
      return {
        lifetime: `hsl(${hue} ${saturation}% ${lightness}%)`,
        window: `hsl(${hue} ${saturation + 6}% ${lightness + 12}%)`,
      };
    }
  }
}

function buildSlotEvent(slot, kind, index, total, toPercent, vanishIds, previewSourceId) {
  const colors = colorForSlot(kind, index, total);

  const segments =
    slot.kind === 'mystery'
      ? [
          {
            type: 'window',
            start: slot.windowStart,
            end: slot.windowEnd,
            left: toPercent(slot.windowStart),
            width: toPercent(slot.windowEnd) - toPercent(slot.windowStart),
            color: colors.window,
          },
        ]
      : [
          {
            type: 'lifetime',
            start: slot.spawnTime,
            end: slotEnd(slot),
            left: toPercent(slot.spawnTime),
            width: toPercent(slotEnd(slot)) - toPercent(slot.spawnTime),
            color: colors.lifetime,
          },
        ];

  const wouldVanish = vanishIds?.has(slot.id) ?? false;
  const isPreviewSource = previewSourceId === slot.id;

  return {
    slotId: slot.id,
    kind,
    segments,
    wouldVanish,
    isPreviewSource,
  };
}

export function buildTimeline(state, preview = null) {
  const shopSlots = state.shopSlots;
  const enemySlots = state.enemySlots;
  const mysterySlots = state.mysterySlots;
  const allSlots = [...shopSlots, ...enemySlots, ...mysterySlots];
  const currentTime = state.currentTime;
  const previewTime = preview ? currentTime + preview.amount : null;

  const vanishIds = preview
    ? new Set(
        allSlots
          .filter((slot) => {
            if (wouldSlotExpireAtForTimeline(slot, previewTime)) {
              return true;
            }
            return false;
          })
          .map((slot) => slot.id),
      )
    : new Set();

  if (preview?.slotId) {
    vanishIds.add(preview.slotId);
  }

  const starts = allSlots.map((slot) => {
    if (slot.kind === 'mystery') {
      return slot.windowStart;
    }
    return slot.spawnTime;
  });

  const ends = allSlots.map((slot) => {
    if (slot.kind === 'mystery') {
      return slot.windowEnd;
    }
    return slotEnd(slot);
  });

  const rangeAnchor = previewTime ?? currentTime;
  const rangeStart = Math.max(0, Math.min(currentTime, ...starts) - 1);
  const rangeEnd = Math.max(rangeAnchor + 6, ...ends, currentTime + 1, previewTime ?? 0);
  const span = Math.max(1, rangeEnd - rangeStart);

  const toPercent = (time) => ((time - rangeStart) / span) * 100;
  const nowPercent = toPercent(currentTime);
  const previewPercent = previewTime != null ? toPercent(previewTime) : null;

  const events = [
    ...shopSlots.map((slot, index) =>
      buildSlotEvent(
        slot,
        'shop',
        index,
        shopSlots.length,
        toPercent,
        vanishIds,
        preview?.slotId,
      ),
    ),
    ...enemySlots.map((slot, index) =>
      buildSlotEvent(
        slot,
        'enemy',
        index,
        enemySlots.length,
        toPercent,
        vanishIds,
        preview?.slotId,
      ),
    ),
    ...mysterySlots.map((slot, index) =>
      buildSlotEvent(
        slot,
        'mystery',
        index,
        mysterySlots.length,
        toPercent,
        vanishIds,
        preview?.slotId,
      ),
    ),
  ];

  const ticks = [];
  const tickStep = span <= 12 ? 2 : span <= 24 ? 4 : 5;
  const firstTick = Math.ceil(rangeStart / tickStep) * tickStep;
  for (let t = firstTick; t <= rangeEnd; t += tickStep) {
    ticks.push({ time: t, left: toPercent(t) });
  }

  return {
    currentTime,
    previewTime,
    rangeStart,
    rangeEnd,
    nowPercent,
    previewPercent,
    ticks,
    events,
    vanishIds,
  };
}

function wouldSlotExpireAtForTimeline(slot, time) {
  if (slot.kind === 'mystery') {
    if (time > slot.windowEnd) {
      return true;
    }
    return time >= slot.spawnTime + slot.duration;
  }
  return time >= slot.spawnTime + slot.duration;
}
