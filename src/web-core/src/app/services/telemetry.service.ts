import { Injectable, NgZone } from '@angular/core';
import { HubConnectionBuilder, HubConnection } from '@microsoft/signalr';
import { Observable, Subject } from 'rxjs';

@Injectable()
export class TelemetryService {
  private readonly hub: HubConnection;
  private readonly subject = new Subject<any>();

  constructor(zone: NgZone) {
    this.hub = new HubConnectionBuilder()
      .withUrl('/notifications')
      .withAutomaticReconnect()
      .build();

    this.hub.on('telemetryNotification', payload => {
      zone.run(() => this.subject.next(payload));
    });

    this.hub.start().catch(err => console.error('SignalR hub start failed', err));
  }

  get notifications$(): Observable<any> {
    return this.subject.asObservable();
  }
}
