import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet,TranslateModule],
  template: `
    <div class="shell">
      <header>
        <h1>{{ 'app.title' | translate }}</h1>
        <select #sel  (change)="changeLocale(sel.value)">
          <option value="en">English</option>
          <option value="it">Italiano</option>
        </select>
      </header>
      <main>
        <router-outlet></router-outlet>
      </main>
    </div>
  `,
  styles: [`
    .shell { font-family: Arial, sans-serif; margin: 0 auto; max-width: 960px; padding: 1rem; }
    header { display: flex; justify-content: space-between; align-items: center; }
  `]
})
export class AppComponent {
  private readonly translate = inject(TranslateService);

  constructor() {
    this.translate.use('en');
  }

  changeLocale(locale: string) {
    this.translate.use(locale);
  }
}
