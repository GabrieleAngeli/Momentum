import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, catchError, lastValueFrom, map, of, switchMap, tap } from 'rxjs';
import type { AuthMeResponse, LoginRequest, LoginResponse } from '@core/types';
import { RealtimeService } from './realtime.service';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly realtime = inject(RealtimeService);
  private readonly me$ = new BehaviorSubject<AuthMeResponse | null>(null);
  private jwtToken: string | null = null;
  private csrfLoaded = false;

  get auth$(): Observable<AuthMeResponse | null> {
    return this.me$.asObservable();
  }

  get token(): string | null {
    return this.jwtToken;
  }

  async load(): Promise<void> {
    await lastValueFrom(this.http.get<AuthMeResponse>('/api/auth/me').pipe(
      tap(me => this.me$.next(me)),
      catchError(() => {
        this.me$.next(null);
        return of(null);
      }),
      map(() => void 0)
    ));
  }

  login(payload: LoginRequest): Observable<LoginResponse> {
    return this.ensureCsrf().pipe(
      switchMap(() => this.http.post<LoginResponse>('/api/auth/login', payload)),
      tap(response => {
        this.jwtToken = response.jwtToken ?? null;
        this.me$.next(response.me);
        this.realtime.connect(this.jwtToken);
      })
    );
  }

  logout(): Observable<void> {
    return this.ensureCsrf().pipe(
      switchMap(() => this.http.post<void>('/api/auth/logout', {})),
      tap(() => {
        this.jwtToken = null;
        this.me$.next(null);
        this.realtime.connect(null);
      })
    );
  }

  updateToken(token: string | null): void {
    this.jwtToken = token;
  }

  hasPermission(permission: string): boolean {
    const auth = this.me$.value;
    if (!auth?.isAuthenticated) {
      return false;
    }
    return auth.user.permissions.includes(permission);
  }

  private ensureCsrf(): Observable<void> {
    if (this.csrfLoaded) {
      return of(void 0);
    }

    return this.http.get('/api/csrf', { responseType: 'text' }).pipe(
      map(() => void 0),
      tap(() => {
        this.csrfLoaded = true;
      })
    );
  }
}
