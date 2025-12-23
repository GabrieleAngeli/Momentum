import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { RouterTestingModule } from '@angular/router/testing';
import { of } from 'rxjs';
import { AppComponent } from './app.component';
import { AuthService } from './core/auth.service';
import { MenuService } from './core/menu.service';
import type { AuthMeResponse, MenuEntryDto } from '@core/types';
import { TranslateLoader, TranslateModule } from '@ngx-translate/core';
import { FeatureFlagService } from './core/feature-flag.service';

class FakeTranslateLoader implements TranslateLoader {
  getTranslation() {
    return of({
      'featureA.title': 'Feature A workspace',
    });
  }
}

class FeatureFlagServiceStub {
  load() { return Promise.resolve(); }
  getBoolean(_key: string, fallback = false) { return fallback; }
  // se nel remote usi anche altri metodi, aggiungili qui (getString/getNumber/hasFlag ecc.)
}

class AuthServiceStub {
  readonly auth$ = of<AuthMeResponse>({
    isAuthenticated: true,
    requiresMfa: false,
    user: {
      id: '1',
      email: 'user@example.com',
      displayName: 'Demo User',
      tenantId: 'tenant-default',
      roles: ['admin'],
      permissions: ['feature-a:view'],
      claims: {}
    }
  });

  login() { return of(); }
  logout() { return of(); }
}

class MenuServiceStub {
  readonly items$ = of<MenuEntryDto[]>([
    { id: 'feature-a', label: 'featureA.title', route: '/feature-a', requiredFlags: [], requiredPermissions: [], children: [] }
  ]);

  load() { return Promise.resolve(); }
}

@Component({
  standalone: true,
  template: `<p>Home</p>`
})
class HomeComponent {}

describe('Shell smoke scenario', () => {
  it('navigates to feature-a remote', async () => {
    await TestBed.configureTestingModule({
      imports: [
        RouterTestingModule.withRoutes([
          { path: '', component: HomeComponent },
          { path: 'feature-a', loadChildren: () => import('./remotes/feature-a/feature-a.module').then(m => m.FeatureARemoteModule) }
        ]),
        TranslateModule.forRoot({ loader: { provide: TranslateLoader, useClass: FakeTranslateLoader } }),
        AppComponent
      ],
      providers: [
        { provide: AuthService, useClass: AuthServiceStub },
        { provide: MenuService, useClass: MenuServiceStub },
        { provide: FeatureFlagService, useClass: FeatureFlagServiceStub },
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(AppComponent);
    const router = TestBed.inject(Router);
    router.initialNavigation();
    fixture.detectChanges();

    await router.navigateByUrl('feature-a');
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Feature A workspace');
  });
});
