import { inject, Injectable, NgZone, OnDestroy } from '@angular/core';
import { HubConnectionBuilder, HubConnection } from '@microsoft/signalr';
import { Observable, Subject } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class TelemetryService implements OnDestroy {
  private readonly subject = new Subject<unknown>();
  private readonly zone = inject(NgZone);
   private readonly hub: HubConnection = new HubConnectionBuilder()
    .withUrl('/notifications')
    .withAutomaticReconnect()
    .build();

  constructor() {

    this.hub.on('telemetryNotification', (payload: unknown) => {
      this.zone.run(() => this.subject.next(payload));
    });

    this.hub.start().catch(err => console.error('SignalR hub start failed', err));
  }

  get notifications$(): Observable<unknown> {
    return this.subject.asObservable();
  }

  ngOnDestroy(): void {
    void this.hub.stop();
  }

}
