export interface SupplierProduct {
  model: string;
  category: string;
  storageCapacity: string | null;
  color: string | null;
  conditionName: string;
  lowestPrice: number;
}

export interface Supplier {
  id: string;
  name: string;
  phoneNumber: string | null;
  reliabilityScore: number;
  totalMessages: number;
  totalPricesLogged: number;
  todayProductCount: number;
  todayCategories: string[];
  todayProducts: SupplierProduct[];
}
