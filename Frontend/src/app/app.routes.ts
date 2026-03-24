import { Routes } from '@angular/router';
import { ProductListComponent } from './components/product-list/product-list.component';
import { InsightsDashboardComponent } from './components/insights-dashboard/insights-dashboard.component';
import { SupplierRankingComponent } from './components/supplier-ranking/supplier-ranking.component';
import { LayoutComponent } from './components/layout/layout.component';

export const routes: Routes = [
  {
    path: '',
    component: LayoutComponent,
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', component: InsightsDashboardComponent },
      { path: 'products', component: ProductListComponent },
      { path: 'suppliers', component: SupplierRankingComponent }
    ]
  }
];
