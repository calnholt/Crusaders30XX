import enemies from '../data/enemies.json';

export function enemyIdToAssetName(enemyId) {
  if (!enemyId) return '';
  return enemyId
    .split('_')
    .map((part) => (part ? part.charAt(0).toUpperCase() + part.slice(1) : part))
    .join('_');
}

export function getEnemyPortraitUrl(enemyId) {
  if (!enemyId) return null;
  return `/game-content/${enemyIdToAssetName(enemyId)}.png`;
}

export function getEnemyInitials(name) {
  return (name ?? '')
    .split(' ')
    .map((word) => word[0])
    .join('')
    .slice(0, 2)
    .toUpperCase();
}

export const ENEMY_PORTRAIT_FILES = enemies.map((enemy) => ({
  id: enemy.id,
  fileName: `${enemyIdToAssetName(enemy.id)}.png`,
}));
