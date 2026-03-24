import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { rxResource } from '@angular/core/rxjs-interop';
import { ApiService } from '../../services/api.service';

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [CurrencyPipe],
  templateUrl: './product-list.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProductListComponent {
  private readonly apiService = inject(ApiService);

  protected readonly productsResource = rxResource({
    stream: () => this.apiService.getProducts(),
  });

  protected readonly activeTab = signal<string>('Todos');

  protected readonly tabs = computed(() => {
    const items = this.productsResource.value() ?? [];
    const cats = [...new Set(items.map(p => p.category))].sort();
    return ['Todos', ...cats];
  });

  protected readonly filtered = computed(() => {
    const tab = this.activeTab();
    const items = this.productsResource.value() ?? [];
    return tab === 'Todos' ? items : items.filter(p => p.category === tab);
  });

  protected setTab(tab: string): void {
    this.activeTab.set(tab);
  }
}
