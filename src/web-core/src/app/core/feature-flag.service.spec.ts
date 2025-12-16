import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { FeatureFlagService } from './feature-flag.service';
import { RealtimeService } from './realtime.service';
import type { FlagsDelta } from '@core/types';
import { Subject } from 'rxjs';

class RealtimeServiceStub {
  readonly flags$ = new Subject<FlagsDelta>();
  readonly i18n$ = new Subject<any>();
  readonly menu$ = new Subject<any>();
  readonly manifest$ = new Subject<any>();
}

describe('FeatureFlagService', () => {
  let service: FeatureFlagService;
  let http: HttpTestingController;
  let realtime: RealtimeServiceStub;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: RealtimeService, useClass: RealtimeServiceStub },
      ],
    });

    service = TestBed.inject(FeatureFlagService);
    http = TestBed.inject(HttpTestingController);
    realtime = TestBed.inject(RealtimeService) as any;
  });

  afterEach(() => http.verify());

  it('applies realtime deltas', async () => {
    const loadPromise = service.load();
    http.expectOne('/api/flags').flush({
      feature: { key: 'feature', value: true, type: 'boolean', scope: 'Global' },
    });
    await loadPromise;

    realtime.flags$.next({
      updated: { feature: { key: 'feature', value: false, type: 'boolean', scope: 'Global' } },
      removed: [],
    });

    expect(service.getBoolean('feature', true)).toBe(false);
  });
});
