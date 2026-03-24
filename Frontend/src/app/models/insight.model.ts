import type { BrandName } from './product.model';

export interface Insight {
  brand: number;
  brandName?: BrandName;
  model: string;
  storageCapacity: string | null;
  averagePrice: number;
  lowestPrice: number;
  suggestedResalePrice: number;
  profitMargin: number;
  listingCount: number;
}
