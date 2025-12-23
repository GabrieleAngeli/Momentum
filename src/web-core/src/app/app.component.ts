import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { AuthService } from './core/auth.service';
import { MenuService } from './core/menu.service';
import { UiButtonComponent } from './ui/button.component';
import { AsyncPipe } from '@angular/common';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    TranslateModule,
    UiButtonComponent,
    AsyncPipe
  ],
  template: `
    <div class="shell">
      <header class="shell__header">
        <div class="shell__brand">Momentum Shell</div>
        <nav class="shell__nav">
          <a *ngFor="let item of menu$ | async" [routerLink]="item.route" routerLinkActive="active">
            {{ item.label | translate }}
          </a>
        </nav>
        <div class="shell__actions">
          <select class="shell__locale" #locale (change)="changeLocale(locale.value)">
            <option value="en">English</option>
            <option value="it">Italiano</option>
          </select>
          <ng-container *ngIf="auth$ | async as auth">
            <span *ngIf="auth?.isAuthenticated; else login">
              {{ auth.user.displayName }}
              <app-ui-button size="sm" variant="ghost" (clicked)="logout()">Logout</app-ui-button>
            </span>
            <ng-template #login>
              <app-ui-button size="sm" (clicked)="loginPrompt()">Login</app-ui-button>
            </ng-template>
          </ng-container>
        </div>
      </header>
      <main class="shell__main">
        <router-outlet></router-outlet>
      </main>
    </div>
  `,
  styles: [
    `
      :host { display: block; min-height: 100vh; background: var(--surface, #f7f9fc); color: var(--on-surface, #1f2933); }
      .shell { display: flex; flex-direction: column; min-height: 100vh; }
      .shell__header { display: flex; align-items: center; gap: 1rem; padding: 1rem 2rem; background: var(--surface-strong, #fff); box-shadow: 0 1px 0 rgba(15, 23, 42, 0.08); }
      .shell__brand { font-weight: 600; letter-spacing: 0.08em; text-transform: uppercase; }
      .shell__nav { display: flex; gap: 1rem; flex: 1; }
      .shell__nav a { text-decoration: none; color: inherit; font-weight: 500; opacity: 0.8; }
      .shell__nav a.active, .shell__nav a:hover { opacity: 1; }
      .shell__actions { display: flex; align-items: center; gap: 0.5rem; }
      .shell__locale { padding: 0.25rem 0.5rem; border-radius: 0.4rem; border: 1px solid rgba(15,23,42,0.1); background: transparent; }
      .shell__main { flex: 1; padding: 2rem; }
    `
  ]
})
export class AppComponent {
  private readonly translate = inject(TranslateService);
  private readonly authService = inject(AuthService);
  private readonly menuService = inject(MenuService);

  readonly auth$ = this.authService.auth$;
  readonly menu$ = this.menuService.items$;

  constructor() {
    this.translate.use('en');
  }

  changeLocale(locale: string) {
    this.translate.use(locale);
  }

  loginPrompt() {
    const username = prompt('username', 'user');
    const password = prompt('password', 'P@ssw0rd!');
    if (!username || !password) {
      return;
    }
    this.authService.login({ username, password }).subscribe();
  }

  logout() {
    this.authService.logout().subscribe();
  }
}
