import { DestroyRef, Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, catchError, lastValueFrom, of } from 'rxjs';
import type { MenuEntryDto } from '@core/types';
import { FeatureFlagService } from './feature-flag.service';
import { AuthService } from './auth.service';
import { RealtimeService } from './realtime.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Injectable({ providedIn: 'root' })
export class MenuService {
  private readonly http = inject(HttpClient);
  private readonly flags = inject(FeatureFlagService);
  private readonly auth = inject(AuthService);
  private readonly realtime = inject(RealtimeService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly menu$ = new BehaviorSubject<MenuEntryDto[]>([]);

  get items$(): Observable<MenuEntryDto[]> {
    return this.menu$.asObservable();
  }

  async load(): Promise<void> {
    const menu = await lastValueFrom(
      this.http.get<MenuEntryDto[]>('/api/ui/menu').pipe(
        catchError(() => of([]))
      )
    );
    this.menu$.next(this.filter(menu));
    this.realtime.menu$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(items => this.menu$.next(this.filter(items)));
  }

  private filter(items: MenuEntryDto[]): MenuEntryDto[] {
    return items
      .map(item => ({
        ...item,
        requiredFlags: item.requiredFlags ?? [],
        requiredPermissions: item.requiredPermissions ?? [],
        children: this.filter(item.children ?? [])
      }))
      .filter(item => {
        const flagsOk = item.requiredFlags.every(flag => this.flags.getBoolean(flag));
        const permsOk = item.requiredPermissions.every(perm => this.auth.hasPermission(perm));
        return flagsOk && permsOk;
      });
  }
}
