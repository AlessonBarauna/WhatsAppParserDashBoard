import { ChangeDetectionStrategy, Component, inject } from '@angular/core';

import { rxResource } from '@angular/core/rxjs-interop';
import { ApiService } from '../../services/api.service';

@Component({
  selector: 'app-supplier-ranking',
  standalone: true,
  imports: [],
  templateUrl: './supplier-ranking.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SupplierRankingComponent {
  private readonly apiService = inject(ApiService);

  protected readonly suppliersResource = rxResource({
    stream: () => this.apiService.getSuppliers(),
  });
}
