import { randInt } from './random.js';

export const RESOURCE_TYPES = ['red', 'white', 'black'];

export function emptyResources() {
  return { red: 0, white: 0, black: 0 };
}

export function randomCost() {
  const cost = emptyResources();
  const types = pickNTypes(randInt(1, 3));
  for (const type of types) {
    cost[type] = randInt(1, 3);
  }
  return cost;
}

export function randomRewards(countMin = 2, countMax = 3) {
  const count = Math.min(randInt(countMin, countMax), RESOURCE_TYPES.length);
  const types = pickNTypes(count);
  return types.map((type) => ({ type, amount: randInt(1, 3) }));
}

export function canAfford(resources, cost) {
  return RESOURCE_TYPES.every((type) => resources[type] >= (cost[type] ?? 0));
}

export function spendResources(resources, cost) {
  const next = { ...resources };
  for (const type of RESOURCE_TYPES) {
    next[type] -= cost[type] ?? 0;
  }
  return next;
}

export function addRewards(resources, rewards) {
  const next = { ...resources };
  for (const reward of rewards) {
    next[reward.type] += reward.amount;
  }
  return next;
}

export function pickNTypes(n) {
  const copy = [...RESOURCE_TYPES];
  const result = [];
  for (let i = 0; i < n; i++) {
    const idx = randInt(0, copy.length - 1);
    result.push(copy.splice(idx, 1)[0]);
  }
  return result;
}

export function formatCost(cost) {
  return RESOURCE_TYPES.filter((type) => (cost[type] ?? 0) > 0).map((type) => ({
    type,
    amount: cost[type],
  }));
}
