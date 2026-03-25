import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <nav class="app-nav">
      <a class="nav-brand" routerLink="/dashboard">
        <span class="brand-dot"></span>
        WP<span class="text-neon">Parser</span>
      </a>
      <ul class="nav-links">
        <li><a routerLink="/dashboard" routerLinkActive="active">Insights</a></li>
        <li><a routerLink="/products"  routerLinkActive="active">Produtos</a></li>
        <li><a routerLink="/suppliers" routerLinkActive="active">Fornecedores</a></li>
        <li><a routerLink="/upload"    routerLinkActive="active">Upload</a></li>
        <li><a routerLink="/chat"      routerLinkActive="active">Assistente IA</a></li>
      </ul>
      <div class="nav-status">
        <span class="status-dot"></span>LIVE
      </div>
    </nav>
    <div class="page-wrapper">
      <router-outlet />
    </div>
  `
})
export class LayoutComponent {}
