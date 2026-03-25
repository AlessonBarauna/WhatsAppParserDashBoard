import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { CurrencyPipe, TitleCasePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { rxResource } from '@angular/core/rxjs-interop';
import { ApiService } from '../../services/api.service';

@Component({
  selector: 'app-insights-dashboard',
  standalone: true,
  imports: [CurrencyPipe, TitleCasePipe, FormsModule],
  templateUrl: './insights-dashboard.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InsightsDashboardComponent {
  private readonly apiService = inject(ApiService);

  protected readonly insightsResource = rxResource({
    stream: () => this.apiService.getInsights(),
  });

  // ── Tab (category) ────────────────────────────────────────────
  protected readonly activeTab = signal<string>('Todos');

  protected readonly tabs = computed(() => {
    const items = this.insightsResource.value() ?? [];
    const cats = [...new Set(items.map(i => i.category))].sort();
    return ['Todos', ...cats];
  });

  // ── Filters ───────────────────────────────────────────────────
  protected readonly conditionFilter = signal<string>('Todos');
  protected readonly storageFilter   = signal<string>('Todos');
  protected readonly minPrice        = signal<number | null>(null);
  protected readonly maxPrice        = signal<number | null>(null);
  protected readonly showFilters     = signal(false);

  protected readonly availableConditions = computed(() => {
    const items = this.insightsResource.value() ?? [];
    const conds = [...new Set(
      items.filter(i => !i.isAnomaly).map(i => i.conditionName).filter(c => c && c !== '—')
    )].sort();
    return ['Todos', ...conds];
  });

  protected readonly availableStorages = computed(() => {
    const items = this.insightsResource.value() ?? [];
    const storages = [...new Set(
      items.filter(i => !i.isAnomaly && i.storageCapacity).map(i => i.storageCapacity!)
    )];
    storages.sort((a, b) => this.parseStorage(a) - this.parseStorage(b));
    return ['Todos', ...storages];
  });

  private parseStorage(s: string): number {
    const n = parseInt(s, 10);
    return s.toUpperCase().includes('TB') ? n * 1000 : n;
  }

  protected readonly activeFilterCount = computed(() => {
    let n = 0;
    if (this.conditionFilter() !== 'Todos') n++;
    if (this.storageFilter() !== 'Todos') n++;
    if (this.minPrice() != null) n++;
    if (this.maxPrice() != null) n++;
    return n;
  });

  protected clearFilters(): void {
    this.conditionFilter.set('Todos');
    this.storageFilter.set('Todos');
    this.minPrice.set(null);
    this.maxPrice.set(null);
  }

  // ── Anomalies ─────────────────────────────────────────────────
  protected readonly showAnomalies = signal(false);

  protected readonly anomalyCount = computed(() =>
    (this.insightsResource.value() ?? []).filter(i => i.isAnomaly).length
  );

  // ── Filtered list ─────────────────────────────────────────────
  protected readonly filtered = computed(() => {
    const tab       = this.activeTab();
    const show      = this.showAnomalies();
    const condition = this.conditionFilter();
    const storage   = this.storageFilter();
    const min       = this.minPrice();
    const max       = this.maxPrice();

    let items = (this.insightsResource.value() ?? []).filter(i => show || !i.isAnomaly);
    if (tab !== 'Todos')       items = items.filter(i => i.category === tab);
    if (condition !== 'Todos') items = items.filter(i => i.conditionName === condition);
    if (storage !== 'Todos')   items = items.filter(i => i.storageCapacity === storage);
    if (min != null)           items = items.filter(i => i.lowestPrice >= min);
    if (max != null)           items = items.filter(i => i.lowestPrice <= max);
    return items;
  });

  protected setTab(tab: string): void {
    this.activeTab.set(tab);
  }

  protected toggleAnomalies(): void {
    this.showAnomalies.update(v => !v);
  }

  protected conditionClass(conditionName: string): string {
    switch (conditionName) {
      case 'Novo':      return 'badge-green';
      case 'Seminovo':  return 'badge-orange';
      case 'CPO':       return 'badge-cyan';
      case 'Vitrine':   return 'badge-purple';
      case 'Bat. 100%': return 'badge-green';
      default:          return 'badge-dim';
    }
  }
}
