import { TestBed } from '@angular/core/testing';
import { DashboardComponent } from './dashboard.component';
import { TelemetryService, TelemetryPayload } from '../../services/telemetry.service';
import { of } from 'rxjs';
import { TranslateLoader, TranslateModule } from '@ngx-translate/core';

class TranslateLoaderMock implements TranslateLoader {
  getTranslation(_: string) {
    return of({}); // nessuna traduzione, evita HttpClient
  }
}

class TelemetryServiceMock {
  notifications$ = of<TelemetryPayload>({ type: 'telemetry', message: 'demo' });
}

describe('DashboardComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        DashboardComponent,
        TranslateModule.forRoot({
          loader: { provide: TranslateLoader, useClass: TranslateLoaderMock },
        }),
      ],
    })
      // Se il componente dichiara `providers: [TelemetryService]`:
      .overrideComponent(DashboardComponent, {
        set: { providers: [{ provide: TelemetryService, useClass: TelemetryServiceMock }] },
      })
      .compileComponents();
  });

  it('should create and hydrate notifications', () => {
    const fixture = TestBed.createComponent(DashboardComponent);
    fixture.detectChanges();

    const comp = fixture.componentInstance;
    expect(comp).toBeTruthy();
    expect(comp.notifications().length).toBeGreaterThan(0);
    expect(comp.notifications()[0]).toEqual({ type: 'telemetry', message: 'demo' });
  });
});
