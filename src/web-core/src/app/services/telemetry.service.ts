import { inject, Injectable, NgZone, OnDestroy } from '@angular/core';
import { HubConnectionBuilder, HubConnection } from '@microsoft/signalr';
import { Observable, Subject } from 'rxjs';

export interface TelemetryPayload {
  type?: string;
  message?: string;
  // eventuali altre proprietà:
  [key: string]: unknown;
}

@Injectable({ providedIn: 'root' })
export class TelemetryService implements OnDestroy {
  private readonly subject = new Subject<TelemetryPayload>();
  private readonly zone = inject(NgZone);
   private readonly hub: HubConnection = new HubConnectionBuilder()
    .withUrl('/notifications')
    .withAutomaticReconnect()
    .build();

  constructor() {

    this.hub.on('telemetryNotification', (payload: TelemetryPayload) => {
      this.zone.run(() => this.subject.next(payload));
    });

    this.hub.start().catch(err => console.error('SignalR hub start failed', err));
  }

  get notifications$(): Observable<TelemetryPayload> {
    return this.subject.asObservable();
  }

  ngOnDestroy(): void {
    void this.hub.stop();
  }

}
