export interface Supplier {
  id: string;
  name: string;
  phoneNumber: string | null;
  reliabilityScore: number;
  totalMessages: number;
  totalPricesLogged: number;
}
