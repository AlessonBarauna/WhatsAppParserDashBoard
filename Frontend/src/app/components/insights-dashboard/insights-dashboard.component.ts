import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService, Insight } from '../../services/api.service';

@Component({
  selector: 'app-insights-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './insights-dashboard.component.html'
})
export class InsightsDashboardComponent implements OnInit {
  insights: Insight[] = [];
  loading = true;

  constructor(private apiService: ApiService) {}

  ngOnInit() {
    this.apiService.getInsights().subscribe({
      next: (data: Insight[]) => {
        this.insights = data;
        this.loading = false;
      },
      error: (err: any) => {
        console.error('Failed to load insights', err);
        this.loading = false;
      }
    });
  }
}
