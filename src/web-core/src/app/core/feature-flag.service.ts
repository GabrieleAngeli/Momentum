import { DestroyRef, Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, catchError, lastValueFrom, of } from 'rxjs';
import type { FlagValue, FlagsDelta } from '@core/types';
import { RealtimeService } from './realtime.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Injectable({ providedIn: 'root' })
export class FeatureFlagService {
  private readonly http = inject(HttpClient);
  private readonly realtime = inject(RealtimeService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly flags$ = new BehaviorSubject<Record<string, FlagValue>>({});

  get changes(): Observable<Record<string, FlagValue>> {
    return this.flags$.asObservable();
  }

  async load(): Promise<void> {
    const flags = await lastValueFrom(
      this.http.get<Record<string, FlagValue>>('/api/flags').pipe(
        catchError(() => of({}))
      )
    );
    this.flags$.next(flags);
    this.realtime.flags$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(delta => this.applyDelta(delta));
  }

  getBoolean(key: string, defaultValue = false): boolean {
    const flag = this.flags$.value[key];
    if (!flag) {
      return defaultValue;
    }
    if (typeof flag.value === 'boolean') {
      return flag.value;
    }
    if (typeof flag.value === 'string') {
      return flag.value.toLowerCase() === 'true';
    }
    return defaultValue;
  }

  private applyDelta(delta: FlagsDelta): void {
    const current = { ...this.flags$.value };
    for (const key of delta.removed) {
      delete current[key];
    }
    for (const [key, value] of Object.entries(delta.updated)) {
      current[key] = value;
    }
    this.flags$.next(current);
  }
}
