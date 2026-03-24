import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SupplierRanking } from './supplier-ranking';

describe('SupplierRanking', () => {
  let component: SupplierRanking;
  let fixture: ComponentFixture<SupplierRanking>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SupplierRanking],
    }).compileComponents();

    fixture = TestBed.createComponent(SupplierRanking);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
