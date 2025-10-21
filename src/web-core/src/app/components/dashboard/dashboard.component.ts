import { CommonModule } from '@angular/common';
import { Component, computed, inject } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { ManifestService } from '../../core/manifest.service';
import { FeatureFlagService } from '../../core/feature-flag.service';
import { UiTableComponent } from '../../ui/table.component';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, TranslateModule, UiTableComponent],
  template: `
    <section class="dashboard">
      <header>
        <h2>{{ 'dashboard.title' | translate }}</h2>
        <p>{{ 'dashboard.description' | translate }}</p>
      </header>
      <app-ui-table [columns]="['id','url','flags']" [data]="manifestRows()"></app-ui-table>
    </section>
  `
})
export class DashboardComponent {
  private readonly manifest = inject(ManifestService);
  private readonly flags = inject(FeatureFlagService);

  readonly manifestRows = computed(() => {
    const manifest = this.manifest.manifestSnapshot;
    if (!manifest) {
      return [] as Array<Record<string, unknown>>;
    }
    return manifest.remotes.map(remote => ({
      id: remote.id,
      url: remote.url,
      flags: remote.flags.map(flag => `${flag}:${this.flags.getBoolean(flag)}`).join(', ')
    }));
  });
}
