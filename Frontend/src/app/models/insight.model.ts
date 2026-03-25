import type { BrandName } from './product.model';

export interface Insight {
  brand: number;
  brandName?: BrandName;
  model: string;
  category: string;
  storageCapacity: string | null;
  averagePrice: number;
  lowestPrice: number;
  suggestedResalePrice: number;
  profitMargin: number;
  listingCount: number;
  marketReferencePrice: number | null;
  lowestPriceSupplierName: string | null;
  color: string | null;
  conditionName: string;
  originFlag: string | null;
  isAnomaly: boolean;
}
