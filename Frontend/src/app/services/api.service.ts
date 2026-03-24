import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Product {
  id: string;
  brand: number;
  brandName: string;
  model: string;
  storageCapacity: string;
  color: string;
  condition: number;
  conditionString: string;
  normalizedName: string;
  latestPrice: number;
}

export interface Insight {
  brand: number;
  model: string;
  storageCapacity: string;
  averagePrice: number;
  lowestPrice: number;
  suggestedResalePrice: number;
  profitMargin: number;
  listingCount: number;
}

export interface Supplier {
  id: string;
  name: string;
  phoneNumber: string;
  reliabilityScore: number;
  totalMessages: number;
  totalPricesLogged: number;
}

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private apiUrl = 'http://localhost:5031/api';

  constructor(private http: HttpClient) { }

  getProducts(): Observable<Product[]> {
    return this.http.get<Product[]>(`${this.apiUrl}/products`);
  }

  getInsights(): Observable<Insight[]> {
    return this.http.get<Insight[]>(`${this.apiUrl}/insights`);
  }

  getSuppliers(): Observable<Supplier[]> {
    return this.http.get<Supplier[]>(`${this.apiUrl}/suppliers`);
  }
}
