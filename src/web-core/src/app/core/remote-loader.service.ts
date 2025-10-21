import { Injectable } from '@angular/core';
import { loadRemoteModule } from '@angular-architects/module-federation';
import type { RemoteModuleDescriptor } from '@core/types';

@Injectable({ providedIn: 'root' })
export class RemoteLoaderService {
  loadRemote(remote: RemoteModuleDescriptor) {
    if (remote.url.startsWith('local:')) {
      const moduleName = remote.url.split(':')[1];
      switch (moduleName) {
        case 'feature-a':
          return import('../remotes/feature-a/feature-a.module').then(m => m.FeatureARemoteModule);
        default:
          throw new Error(`Unknown local remote: ${moduleName}`);
      }
    }

    return loadRemoteModule({
      type: 'module',
      remoteEntry: remote.url,
      exposedModule: './Module'
    }).then(m => m.RemoteEntryModule ?? m.default ?? m);
  }
}
