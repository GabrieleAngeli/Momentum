import { Component, EventEmitter, Input, Output } from '@angular/core';
import { UiButtonComponent } from './button.component';
import { NgIf } from '@angular/common';

@Component({
  selector: 'app-ui-dialog',
  standalone: true,
  imports: [NgIf, UiButtonComponent],
  template: `
    <div class="ui-dialog__backdrop" *ngIf="open" (click)="backdropClick.emit()"></div>
    <section class="ui-dialog" *ngIf="open" role="dialog" aria-modal="true">
      <header class="ui-dialog__header">
        <h2>{{ title }}</h2>
        <app-ui-button variant="ghost" size="sm" (clicked)="closed.emit()">×</app-ui-button>
      </header>
      <div class="ui-dialog__body">
        <ng-content></ng-content>
      </div>
      <footer class="ui-dialog__footer">
        <ng-content select="[dialog-actions]"></ng-content>
      </footer>
    </section>
  `,
  styles: [
    `
      :host { position: relative; }
      .ui-dialog__backdrop {
        position: fixed;
        inset: 0;
        background: rgba(15, 23, 42, 0.35);
        backdrop-filter: blur(4px);
      }
      .ui-dialog {
        position: fixed;
        top: 50%;
        left: 50%;
        transform: translate(-50%, -50%);
        min-width: 20rem;
        max-width: 32rem;
        background: var(--surface-strong, #fff);
        border-radius: 1rem;
        box-shadow: 0 24px 48px rgba(15, 23, 42, 0.15);
        display: flex;
        flex-direction: column;
        overflow: hidden;
      }
      .ui-dialog__header, .ui-dialog__footer {
        display: flex;
        align-items: center;
        justify-content: space-between;
        padding: 1rem;
        gap: 1rem;
      }
      .ui-dialog__body {
        padding: 1.5rem;
      }
    `
  ]
})
export class UiDialogComponent {
  @Input() open = false;
  @Input() title = '';
  @Output() readonly closed = new EventEmitter<void>();
  @Output() readonly backdropClick = new EventEmitter<void>();
  
}
