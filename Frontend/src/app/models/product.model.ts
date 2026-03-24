export type BrandName = 'Unknown' | 'Apple' | 'Xiaomi';
export type ConditionName = 'Unknown' | 'New' | 'Used' | 'Refurbished' | 'Battery100';

export interface Product {
  id: string;
  brand: number;
  brandName: BrandName;
  model: string;
  storageCapacity: string | null;
  color: string | null;
  condition: number;
  conditionName: ConditionName;
  normalizedName: string;
  latestPrice: number | null;
}
