import { AsyncPipe, CommonModule } from '@angular/common';
import { Component, effect, inject, signal } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { TelemetryService } from '../../services/telemetry.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, AsyncPipe, TranslateModule],
  providers: [TelemetryService],
  template: `
    <section>
      <h2>{{ 'dashboard.title' | translate }}</h2>
      <ul>
        <li *ngFor="let notification of notifications()">
          <strong>{{ notification.type }}</strong> → {{ notification.message }}
        </li>
      </ul>
    </section>
  `
})
export class DashboardComponent {
  private readonly telemetry = inject(TelemetryService);
  readonly notifications = signal<{ type: string; message: string }[]>([]);

  constructor() {
    effect(() => {
      this.telemetry.notifications$.subscribe(payload => {
        this.notifications.update(current => [{
          type: payload.type ?? 'telemetry',
          message: payload.message ?? JSON.stringify(payload)
        }, ...current].slice(0, 10));
      });
    });
  }
}
