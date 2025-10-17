import { bootstrapApplication } from '@angular/platform-browser';
import { provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { AppComponent } from './app/app.component';
import { APP_ROUTES } from './app/app.routes';
import { provideAnimations } from '@angular/platform-browser/animations';
import { importProvidersFrom } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { provideTranslateHttpLoader } from '@ngx-translate/http-loader';

bootstrapApplication(AppComponent, {
  providers: [
    provideRouter(APP_ROUTES),
    provideHttpClient(withInterceptorsFromDi()),
    provideAnimations(),
    importProvidersFrom(
      TranslateModule.forRoot({
        lang: 'en',
        fallbackLang: 'en',
        loader: provideTranslateHttpLoader({
          prefix: '/assets/i18n/',
          suffix: '.json'
        })
      })
    )
  ]
}).catch(err => console.error(err));
