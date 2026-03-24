import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { CurrencyPipe, NgClass } from '@angular/common';
import { rxResource } from '@angular/core/rxjs-interop';
import { ApiService } from '../../services/api.service';

@Component({
  selector: 'app-insights-dashboard',
  standalone: true,
  imports: [CurrencyPipe, NgClass],
  templateUrl: './insights-dashboard.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InsightsDashboardComponent {
  private readonly apiService = inject(ApiService);

  protected readonly insightsResource = rxResource({
    stream: () => this.apiService.getInsights(),
  });
}
