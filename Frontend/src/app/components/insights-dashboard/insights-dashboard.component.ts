import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { rxResource } from '@angular/core/rxjs-interop';
import { ApiService } from '../../services/api.service';

@Component({
  selector: 'app-insights-dashboard',
  standalone: true,
  imports: [CurrencyPipe],
  templateUrl: './insights-dashboard.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InsightsDashboardComponent {
  private readonly apiService = inject(ApiService);

  protected readonly insightsResource = rxResource({
    stream: () => this.apiService.getInsights(),
  });

  protected readonly activeTab = signal<string>('Todos');

  protected readonly tabs = computed(() => {
    const items = this.insightsResource.value() ?? [];
    const cats = [...new Set(items.map(i => i.category))].sort();
    return ['Todos', ...cats];
  });

  protected readonly filtered = computed(() => {
    const tab = this.activeTab();
    const items = this.insightsResource.value() ?? [];
    return tab === 'Todos' ? items : items.filter(i => i.category === tab);
  });

  protected setTab(tab: string): void {
    this.activeTab.set(tab);
  }
}
