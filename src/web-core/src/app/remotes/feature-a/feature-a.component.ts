import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { UiButtonComponent } from '../../ui/button.component';
import { UiInputComponent } from '../../ui/input.component';
import { UiTableComponent } from '../../ui/table.component';
import { FeatureFlagService } from '../../core/feature-flag.service';

@Component({
  standalone: true,
  selector: 'app-feature-a',
  imports: [CommonModule, TranslateModule, UiButtonComponent, UiInputComponent, UiTableComponent],
  template: `
    <section class="feature-a">
      <header>
        <h2>{{ 'featureA.title' | translate }}</h2>
        <p>{{ 'featureA.description' | translate }}</p>
      </header>
      <div class="feature-a__form">
        <app-ui-input label="{{ 'featureA.form.name' | translate }}" [value]="name()"(valueChange)="name.set($event)"></app-ui-input>
        <app-ui-button (clicked)="addRecord()">{{ 'featureA.actions.add' | translate }}</app-ui-button>
      </div>
      <app-ui-table [columns]="['name', 'created']" [data]="rows()"></app-ui-table>
      <p class="feature-a__flag" *ngIf="!flagEnabled">{{ 'featureA.flagDisabled' | translate }}</p>
    </section>
  `,
  styles: [
    `
      .feature-a { display: flex; flex-direction: column; gap: 1.5rem; }
      header { display: flex; flex-direction: column; gap: 0.5rem; }
      .feature-a__form { display: flex; gap: 1rem; align-items: flex-end; }
      .feature-a__flag { color: #b91c1c; font-size: 0.9rem; }
    `
  ]
})
export class FeatureARemoteComponent {
  private readonly flags = inject(FeatureFlagService);
  readonly name = signal('');
  readonly rows = signal<Record<string, unknown>[]>([]);

  get flagEnabled(): boolean {
    return this.flags.getBoolean('featureA.enabled', true);
  }

  addRecord(): void {
    if (!this.flagEnabled) {
      return;
    }
    const currentName = this.name().trim();
    if (!currentName) {
      return;
    }
    this.rows.update(list => [{ name: currentName, created: new Date().toLocaleString() }, ...list]);
    this.name.set('');
  }
}
