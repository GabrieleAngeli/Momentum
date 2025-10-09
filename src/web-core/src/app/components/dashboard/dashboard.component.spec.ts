import { TestBed } from '@angular/core/testing';
import { DashboardComponent } from './dashboard.component';
import { TelemetryService } from '../../services/telemetry.service';
import { of } from 'rxjs';

class TelemetryServiceMock {
  notifications$ = of({ type: 'telemetry', message: 'demo' });
}

describe('DashboardComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DashboardComponent],
      providers: [{ provide: TelemetryService, useClass: TelemetryServiceMock }]
    }).compileComponents();
  });

  it('should create and hydrate notifications', () => {
    const fixture = TestBed.createComponent(DashboardComponent);
    fixture.detectChanges();
    expect(fixture.componentInstance.notifications().length).toBeGreaterThan(0);
  });
});
