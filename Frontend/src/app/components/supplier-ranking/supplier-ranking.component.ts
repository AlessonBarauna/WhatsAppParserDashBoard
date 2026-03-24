import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService, Supplier } from '../../services/api.service';

@Component({
  selector: 'app-supplier-ranking',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './supplier-ranking.component.html'
})
export class SupplierRankingComponent implements OnInit {
  suppliers: Supplier[] = [];
  loading = true;

  constructor(private apiService: ApiService) {}

  ngOnInit() {
    this.apiService.getSuppliers().subscribe({
      next: (data: Supplier[]) => {
        this.suppliers = data;
        this.loading = false;
      },
      error: (err: any) => {
        console.error('Failed to load suppliers', err);
        this.loading = false;
      }
    });
  }
}
