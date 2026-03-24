import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import type { Product } from '../models/product.model';
import type { Supplier } from '../models/supplier.model';
import type { Insight } from '../models/insight.model';
import type { UploadResult } from '../models/upload-result.model';

// Re-exports so existing components can import models from this file
export type { Product } from '../models/product.model';
export type { Supplier } from '../models/supplier.model';
export type { Insight } from '../models/insight.model';
export type { UploadResult } from '../models/upload-result.model';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api`;

  getProducts(): Observable<Product[]> {
    return this.http.get<Product[]>(`${this.baseUrl}/products`);
  }

  getSuppliers(): Observable<Supplier[]> {
    return this.http.get<Supplier[]>(`${this.baseUrl}/suppliers`);
  }

  getInsights(): Observable<Insight[]> {
    return this.http.get<Insight[]>(`${this.baseUrl}/insights`);
  }

  uploadFile(file: File): Observable<UploadResult> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<UploadResult>(`${this.baseUrl}/messages/upload`, formData);
  }
}
