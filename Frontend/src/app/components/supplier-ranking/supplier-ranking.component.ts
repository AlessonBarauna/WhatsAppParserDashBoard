import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { CurrencyPipe, TitleCasePipe } from '@angular/common';
import { rxResource } from '@angular/core/rxjs-interop';
import { ApiService } from '../../services/api.service';
import type { Supplier } from '../../models/supplier.model';

@Component({
  selector: 'app-supplier-ranking',
  standalone: true,
  imports: [CurrencyPipe, TitleCasePipe],
  templateUrl: './supplier-ranking.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SupplierRankingComponent {
  private readonly apiService = inject(ApiService);

  protected readonly suppliersResource = rxResource({
    stream: () => this.apiService.getSuppliers(),
  });

  protected readonly expandedId = signal<string | null>(null);

  protected toggleExpand(id: string): void {
    this.expandedId.update(cur => cur === id ? null : id);
  }

  protected conditionClass(conditionName: string): string {
    switch (conditionName) {
      case 'Novo': return 'badge-green';
      case 'Seminovo': return 'badge-orange';
      case 'CPO': return 'badge-cyan';
      case 'Vitrine': return 'badge-purple';
      case 'Bat. 100%': return 'badge-green';
      default: return 'badge-dim';
    }
  }

  protected scoreColor(score: number): string {
    if (score >= 80) return 'var(--accent-green)';
    if (score >= 50) return 'var(--accent-orange)';
    return 'var(--accent-red)';
  }
}
