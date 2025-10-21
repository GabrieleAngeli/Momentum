import { Component, Input } from '@angular/core';
import { NgFor } from '@angular/common';

export interface UiToastMessage {
  id: string;
  text: string;
  tone?: 'info' | 'success' | 'error' | 'warning';
}

@Component({
  selector: 'app-ui-toast',
  standalone: true,
  imports: [NgFor],
  template: `
    <aside class="ui-toast-container">
      <article *ngFor="let toast of messages" class="ui-toast" [attr.data-tone]="toast.tone ?? 'info'">
        {{ toast.text }}
      </article>
    </aside>
  `,
  styles: [
    `
      .ui-toast-container {
        position: fixed;
        bottom: 1.5rem;
        right: 1.5rem;
        display: flex;
        flex-direction: column;
        gap: 0.75rem;
        z-index: 2000;
      }
      .ui-toast {
        padding: 0.75rem 1rem;
        border-radius: 0.75rem;
        background: rgba(15,23,42,0.9);
        color: #fff;
        box-shadow: 0 12px 24px rgba(15,23,42,0.25);
      }
      .ui-toast[data-tone='success'] { background: #0f766e; }
      .ui-toast[data-tone='error'] { background: #b91c1c; }
      .ui-toast[data-tone='warning'] { background: #f97316; }
    `
  ]
})
export class UiToastComponent {
  @Input() messages: UiToastMessage[] = [];
}
