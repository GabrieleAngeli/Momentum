import { DestroyRef, Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, catchError, lastValueFrom, of } from 'rxjs';
import type { RemoteModuleDescriptor, UiManifestDto } from '@core/types';
import { Router } from '@angular/router';
import { RemoteLoaderService } from './remote-loader.service';
import { RealtimeService } from './realtime.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Injectable({ providedIn: 'root' })
export class ManifestService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly remoteLoader = inject(RemoteLoaderService);
  private readonly realtime = inject(RealtimeService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly manifest$ = new BehaviorSubject<UiManifestDto | null>(null);

  constructor() {
    this.realtime.manifest$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(manifest => {
      this.manifest$.next(manifest);
      this.registerRemotes(manifest.remotes);
    });
  }

  get manifest(): Observable<UiManifestDto | null> {
    return this.manifest$.asObservable();
  }

  get manifestSnapshot(): UiManifestDto | null {
    return this.manifest$.value;
  }

  async load(): Promise<void> {
    const manifest = await lastValueFrom(
      this.http.get<UiManifestDto>('/api/ui/manifest').pipe(
        catchError(() => of({ remotes: [], shared: {} } as UiManifestDto))
      )
    );
    this.manifest$.next(manifest);
    this.registerRemotes(manifest.remotes);
  }

  registerRemotes(remotes: RemoteModuleDescriptor[]): void {
    const routes = this.router.config.filter(route => route.path !== '**');
    for (const remote of remotes) {
      if (!routes.some(r => r.path === remote.id)) {
        routes.push({
          path: remote.id,
          loadChildren: () => this.remoteLoader.loadRemote(remote)
        });
      }
    }
    this.router.resetConfig([...routes, { path: '**', redirectTo: '' }]);
  }
}
