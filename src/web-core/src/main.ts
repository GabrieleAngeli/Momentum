import { APP_INITIALIZER, importProvidersFrom } from '@angular/core';
import { bootstrapApplication } from '@angular/platform-browser';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideRouter } from '@angular/router';
import { HTTP_INTERCEPTORS, provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { AppComponent } from './app/app.component';
import { APP_ROUTES } from './app/app.routes';
import { TranslateLoader, TranslateModule } from '@ngx-translate/core';
import { RuntimeTranslateLoader } from './app/core/i18n-runtime.loader';
import { AuthService } from './app/core/auth.service';
import { FeatureFlagService } from './app/core/feature-flag.service';
import { ManifestService } from './app/core/manifest.service';
import { MenuService } from './app/core/menu.service';
import { RealtimeService } from './app/core/realtime.service';
import { TraceInterceptor } from './app/core/http-trace.interceptor';
import { AuthInterceptor } from './app/core/auth.interceptor';
import { CredentialsInterceptor } from './app/core/credentials.interceptor';

function bootstrapData(
  auth: AuthService,
  flags: FeatureFlagService,
  manifest: ManifestService,
  menu: MenuService,
  realtime: RealtimeService
) {
  return async () => {
    await auth.load();
    realtime.connect(auth.token);
    await Promise.all([flags.load(), manifest.load(), menu.load()]);
  };
}

bootstrapApplication(AppComponent, {
  providers: [
    provideRouter(APP_ROUTES),
    provideAnimations(),
    provideHttpClient(withInterceptorsFromDi()),
    { provide: HTTP_INTERCEPTORS, useClass: CredentialsInterceptor, multi: true },
    { provide: HTTP_INTERCEPTORS, useClass: TraceInterceptor, multi: true },
    { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true },
    importProvidersFrom(
      TranslateModule.forRoot({
        defaultLanguage: 'en',
        loader: { provide: TranslateLoader, useClass: RuntimeTranslateLoader }
      })
    ),
    {
      provide: APP_INITIALIZER,
      multi: true,
      useFactory: bootstrapData,
      deps: [AuthService, FeatureFlagService, ManifestService, MenuService, RealtimeService]
    }
  ]
}).catch(err => console.error(err));
