import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { TranslateLoader } from '@ngx-translate/core';
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

  getTranslation(lang: string, namespace = 'common'): Observable<Record<string, unknown>> {
    const key = this.key(lang, namespace);
    if (this.cache.has(key)) {
      return of(this.cache.get(key)!);
    }

    return this.http
      .get<I18nResourceDto>(`/api/i18n?lang=${encodeURIComponent(lang)}&ns=${encodeURIComponent(namespace)}`)
      .pipe(map(res => {
        this.cache.set(key, res.resources);
        return res.resources;
      }));
  }

  private key(lang: string, namespace: string): string {
    return `${lang}:${namespace}`;
  }
}
