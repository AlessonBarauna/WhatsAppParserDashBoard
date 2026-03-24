import { ComponentFixture, TestBed } from '@angular/core/testing';

import { InsightsDashboard } from './insights-dashboard';

describe('InsightsDashboard', () => {
  let component: InsightsDashboard;
  let fixture: ComponentFixture<InsightsDashboard>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [InsightsDashboard],
    }).compileComponents();

    fixture = TestBed.createComponent(InsightsDashboard);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
