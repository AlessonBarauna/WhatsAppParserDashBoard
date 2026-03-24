import { Routes } from '@angular/router';
import { LayoutComponent } from './components/layout/layout.component';

export const routes: Routes = [
  {
    path: '',
    component: LayoutComponent,
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      {
        path: 'dashboard',
        loadComponent: () => import('./components/insights-dashboard/insights-dashboard.component').then(m => m.InsightsDashboardComponent),
      },
      {
        path: 'products',
        loadComponent: () => import('./components/product-list/product-list.component').then(m => m.ProductListComponent),
      },
      {
        path: 'suppliers',
        loadComponent: () => import('./components/supplier-ranking/supplier-ranking.component').then(m => m.SupplierRankingComponent),
      },
      {
        path: 'upload',
        loadComponent: () => import('./components/file-upload/file-upload.component').then(m => m.FileUploadComponent),
      },
    ]
  }
];
