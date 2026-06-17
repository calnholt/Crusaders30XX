export function spaceSlotMarkup({ slotId, kind, expired, active, unavailable, compactHtml }) {
  const classes = [
    'space-slot',
    `space-slot--${kind}`,
    expired ? 'space-slot--expired' : '',
    unavailable ? 'space-slot--unavailable' : '',
    active ? 'space-slot--active' : '',
  ]
    .filter(Boolean)
    .join(' ');

  return `
    <article class="${classes}" data-slot-id="${slotId}">
      <div class="space-slot__ring">
        <div class="space-card-compact space-card-compact--${kind}">
          ${compactHtml}
        </div>
      </div>
    </article>
  `;
}
