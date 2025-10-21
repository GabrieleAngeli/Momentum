import { Injectable } from '@angular/core';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { Subject } from 'rxjs';
import type { FlagsDelta, I18nResourceDto, MenuEntryDto, UiManifestDto } from '@core/types';

@Injectable({ providedIn: 'root' })
export class RealtimeService {
  private connection: HubConnection | null = null;
  private lastToken: string | null | undefined;
  private readonly flagsSubject = new Subject<FlagsDelta>();
  private readonly i18nSubject = new Subject<I18nResourceDto>();
  private readonly menuSubject = new Subject<MenuEntryDto[]>();
  private readonly manifestSubject = new Subject<UiManifestDto>();

  readonly flags$ = this.flagsSubject.asObservable();
  readonly i18n$ = this.i18nSubject.asObservable();
  readonly menu$ = this.menuSubject.asObservable();
  readonly manifest$ = this.manifestSubject.asObservable();

  connect(token?: string | null): void {
    if (this.connection) {
      if (token === this.lastToken) {
        return;
      }
      void this.connection.stop();
      this.connection = null;
    }

    const builder = new HubConnectionBuilder()
      .withUrl('/hubs/ui', {
        accessTokenFactory: token ? () => token : undefined,
        withCredentials: true
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Information);

    this.connection = builder.build();
    this.lastToken = token;
    this.connection.on('FlagsUpdated', delta => this.flagsSubject.next(delta));
    this.connection.on('I18nUpdated', payload => this.i18nSubject.next(payload));
    this.connection.on('MenuUpdated', payload => this.menuSubject.next(payload));
    this.connection.on('ManifestUpdated', payload => this.manifestSubject.next(payload));
    this.connection.start().catch(err => console.warn('signalr connection failed', err));
  }
}
