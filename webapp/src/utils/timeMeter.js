import { getShopRefreshMarkerTimes } from '../store/gameStore.js';
import { shopIconMarkup } from '../components/ShopIcon.js';

export const TIME_METER_CAPACITY = 40;

export function getTimeMeterState(currentTime, preview = null) {
  const used = Math.min(TIME_METER_CAPACITY, Math.max(0, currentTime));
  let previewEnd = used;

  if (preview) {
    previewEnd = Math.min(
      TIME_METER_CAPACITY,
      Math.max(0, currentTime + preview.amount),
    );
  }

  const remaining = Math.max(0, TIME_METER_CAPACITY - used);
  const remainingAfterPreview = Math.max(0, TIME_METER_CAPACITY - previewEnd);

  return {
    capacity: TIME_METER_CAPACITY,
    used,
    remaining,
    previewEnd,
    remainingAfterPreview,
    previewing: preview != null && previewEnd > used,
  };
}

function meterHourglassMarkup(index, meter) {
  let fillLevel = 'empty';
  if (index < meter.used) {
    fillLevel = 'used';
  } else if (index < meter.previewEnd) {
    fillLevel = 'preview';
  }

  return `<time-icon variant="white" icon-only meter fill-level="${fillLevel}"></time-icon>`;
}

function meterShopMarkerMarkup(refreshAt, meter) {
  const passed = refreshAt <= meter.used;
  const expiring = !passed && meter.previewEnd >= refreshAt;
  const classes = [
    'time-meter__slot',
    'time-meter__slot--shop',
    passed ? 'time-meter__slot--shop-passed' : '',
    expiring ? 'time-meter__slot--shop-expiring' : '',
  ]
    .filter(Boolean)
    .join(' ');

  return `
    <span class="${classes}" title="Shop refresh T${refreshAt}">
      ${shopIconMarkup('meter')}
    </span>
  `;
}

function buildMeterTrack(meter) {
  const shopMarkerTimes = new Set(getShopRefreshMarkerTimes(meter.capacity));

  return Array.from({ length: meter.capacity }, (_, index) => {
    if (shopMarkerTimes.has(index)) {
      return meterShopMarkerMarkup(index, meter);
    }

    return `
      <span class="time-meter__slot">
        ${meterHourglassMarkup(index, meter)}
      </span>
    `;
  }).join('');
}

export function buildTimeMeterMarkup(state, preview = null) {
  const meter = getTimeMeterState(state.currentTime, preview);
  const track = buildMeterTrack(meter);

  const remainingValue = meter.previewing ? meter.remainingAfterPreview : meter.remaining;
  const previewDelta = meter.previewing ? meter.previewEnd - meter.used : 0;

  const previewNote =
    previewDelta > 0
      ? `<span class="time-meter__preview-delta">+${previewDelta}</span>`
      : '';

  return `
    <div class="time-meter${meter.previewing ? ' time-meter--previewing' : ''}">
      <div class="time-meter__labels">
        <span class="time-meter__stat time-meter__stat--used">
          <span class="time-meter__stat-value">${meter.used}</span>
          <span class="time-meter__stat-label">used</span>
        </span>
        ${previewNote}
        <span class="time-meter__stat time-meter__stat--remaining">
          <span class="time-meter__stat-value">${remainingValue}</span>
          <span class="time-meter__stat-label">remaining</span>
        </span>
      </div>
      <div class="time-meter__track" aria-label="Time meter ${meter.used} of ${meter.capacity} used">
        ${track}
      </div>
    </div>
  `;
}
