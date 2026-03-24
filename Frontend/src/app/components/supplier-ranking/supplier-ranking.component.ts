import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { NgClass } from '@angular/common';
import { rxResource } from '@angular/core/rxjs-interop';
import { ApiService } from '../../services/api.service';

@Component({
  selector: 'app-supplier-ranking',
  standalone: true,
  imports: [NgClass],
  templateUrl: './supplier-ranking.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SupplierRankingComponent {
  private readonly apiService = inject(ApiService);

  protected readonly suppliersResource = rxResource({
    stream: () => this.apiService.getSuppliers(),
  });
}
