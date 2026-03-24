import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <nav class="navbar navbar-expand-lg navbar-dark bg-dark mb-4">
      <div class="container">
        <a class="navbar-brand fw-bold" href="#">WhatsApp Parser SaaS</a>
        <button class="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-toggle="target">
          <span class="navbar-toggler-icon"></span>
        </button>
        <div class="collapse navbar-collapse">
          <ul class="navbar-nav me-auto">
            <li class="nav-item">
              <a class="nav-link" routerLink="/dashboard" routerLinkActive="active">Insights</a>
            </li>
            <li class="nav-item">
              <a class="nav-link" routerLink="/products" routerLinkActive="active">Products</a>
            </li>
            <li class="nav-item">
              <a class="nav-link" routerLink="/suppliers" routerLinkActive="active">Suppliers</a>
            </li>
            <li class="nav-item">
              <a class="nav-link" routerLink="/upload" routerLinkActive="active">Upload</a>
            </li>
          </ul>
        </div>
      </div>
    </nav>
    <div class="container">
      <router-outlet></router-outlet>
    </div>
  `
})
export class LayoutComponent {}
