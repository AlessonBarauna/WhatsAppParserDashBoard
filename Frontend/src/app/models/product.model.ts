export type BrandName = 'Unknown' | 'Apple' | 'Xiaomi' | 'Samsung' | 'Motorola' | 'Meta' | 'Accessory';
export type ConditionName = 'Unknown' | 'New' | 'Used' | 'Refurbished' | 'Battery100' | 'CPO';

export interface Product {
  id: string;
  brand: number;
  brandName: BrandName;
  model: string;
  category: string;
  storageCapacity: string | null;
  color: string | null;
  condition: number;
  conditionName: ConditionName;
  normalizedName: string;
  latestPrice: number | null;
  originFlag: string | null;
}
