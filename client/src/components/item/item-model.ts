export interface ItemStat {
  name: string;
  value: string;
}

export interface ItemModifier extends ItemStat {
  details: {
    definitionId: string;
    role: "Prefix" | "Suffix" | "Signature";
    bonus: { type: string; amount: number } | null;
    effect: {
      id: string;
      trigger: "OnHit" | "OnBeingHit";
      chancePercent: number;
      durationSeconds: number | null;
      count: number | null;
    } | null;
  } | null;
}

export interface Item {
  id: string;
  itemBase: string;
  name: string;
  stats: ItemStat[];
  modifiers: ItemModifier[];
  rarity: string;
  imageKey: string;
  dateGotten: string;
  value: number;
}

export interface LootSource {
  id: string;
  name: string;
  description: string;
}
