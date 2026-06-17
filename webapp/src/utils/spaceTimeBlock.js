export function spaceTimeBlockMarkup(clickCost, leaveLabel) {
  return `
    <div class="space-card-compact__time-block">
      <span class="space-card-compact__time-block__cost">+${clickCost} time</span>
      <span class="space-card-compact__time-block__leaves">${leaveLabel}</span>
    </div>
  `;
}
