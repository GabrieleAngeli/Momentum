import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { TranslateLoader, TranslationObject } from '@ngx-translate/core';
import { Observable, of } from 'rxjs';
import { map } from 'rxjs/operators';
import type { I18nResourceDto } from '@core/types';
import { RealtimeService } from './realtime.service';

@Injectable({ providedIn: 'root' })
export class RuntimeTranslateLoader implements TranslateLoader {
  private readonly http = inject(HttpClient);
  private readonly realtime = inject(RealtimeService);
  private cache = new Map<string, Record<string, unknown>>();

  constructor() {
    this.realtime.i18n$.subscribe(payload => {
      const key = this.key(payload.language, payload.namespace);
      this.cache.set(key, payload.resources);
    });
  }

  getTranslation(lang: string, namespace = 'common'): Observable<TranslationObject> {
    const key = this.key(lang, namespace);

    const cached = this.cache.get(key);
    if (cached) {
      return of(cached as TranslationObject);
    }

    return this.http
      .get<I18nResourceDto>(`/api/i18n?lang=${encodeURIComponent(lang)}&ns=${encodeURIComponent(namespace)}`)
      .pipe(
        map(res => {
          const resources = res.resources as unknown as TranslationObject;
          this.cache.set(key, resources);
          return resources;
        })
      );
  }

  private key(lang: string, namespace: string): string {
    return `${lang}:${namespace}`;
  }
}
