import { TestBed } from '@angular/core/testing';
import { DashboardComponent } from './dashboard.component';
import { TranslateLoader, TranslateModule } from '@ngx-translate/core';
import { of } from 'rxjs';
import type { UiManifestDto } from '@core/types';
import { ManifestService } from '../../core/manifest.service';
import { FeatureFlagService } from '../../core/feature-flag.service';

class FakeTranslateLoader implements TranslateLoader {
  getTranslation() {
    return of({
      'featureA.title': 'Feature A workspace',
    });
  }
}

class ManifestServiceStub {
  manifestSnapshot: UiManifestDto | null = {
    remotes: [
      {
        id: 'feature-a',
        url: 'https://cdn.example/feature-a/remoteEntry.js',
        flags: ['featureA.enabled'],
        permissions: ['feature-a:view'],
        semver: '^19.0.0'
      }
    ],
    shared: { angular: '19.x' }
  };
}

class FeatureFlagServiceStub {
  getBoolean(key: string) {
    return key === 'featureA.enabled';
  }
}

describe('DashboardComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        DashboardComponent,
        TranslateModule.forRoot({
          loader: { provide: TranslateLoader, useClass: FakeTranslateLoader }
        })
      ],
      providers: [
        { provide: ManifestService, useClass: ManifestServiceStub },
        { provide: FeatureFlagService, useClass: FeatureFlagServiceStub }
      ]
    }).compileComponents();
  });

  it('renders manifest table rows', () => {
    const fixture = TestBed.createComponent(DashboardComponent);
    fixture.detectChanges();

    const comp = fixture.componentInstance;
    const rows = comp.manifestRows();
    expect(rows.length).toBe(1);
    expect(rows[0]).toEqual({
      id: 'feature-a',
      url: 'https://cdn.example/feature-a/remoteEntry.js',
      flags: 'featureA.enabled:true'
    });
  });
});
