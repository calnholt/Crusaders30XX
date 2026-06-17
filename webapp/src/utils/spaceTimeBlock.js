import { timeCostMarkup, timeLeavesDisplayMarkup } from './timeDisplay.js';

export function spaceTimeBlockMarkup(clickCost, leavesTiming = null) {
  const leavesLine =
    leavesTiming != null
      ? `<span class="space-card-compact__time-block__leaves">${timeLeavesDisplayMarkup(leavesTiming)}</span>`
      : '';

  return `
    <div class="space-card-compact__time-block">
      <span class="space-card-compact__time-block__cost">${timeCostMarkup(clickCost)}</span>
      ${leavesLine}
    </div>
  `;
}
