import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-ui-button',
  standalone: true,
  template: `
    <button
      type="button"
      class="ui-button"
      [attr.data-variant]="variant"
      [attr.data-size]="size"
      [disabled]="disabled"
      (click)="clicked.emit($event)"
    >
      <ng-content></ng-content>
    </button>
  `,
  styles: [
    `
      :host { display: inline-flex; }
      .ui-button {
        appearance: none;
        border: 1px solid transparent;
        border-radius: 0.75rem;
        font-weight: 500;
        cursor: pointer;
        transition: background-color 150ms ease, color 150ms ease, box-shadow 150ms ease;
        background-color: var(--btn-bg, #111827);
        color: var(--btn-fg, #ffffff);
        padding: 0.5rem 1rem;
      }
      .ui-button[data-variant='ghost'] {
        background: transparent;
        color: inherit;
        border-color: transparent;
      }
      .ui-button[data-variant='outline'] {
        background: transparent;
        border-color: rgba(15, 23, 42, 0.2);
        color: inherit;
      }
      .ui-button[data-size='sm'] {
        padding: 0.25rem 0.75rem;
        font-size: 0.875rem;
      }
      .ui-button[data-size='lg'] {
        padding: 0.75rem 1.5rem;
        font-size: 1rem;
      }
      .ui-button:disabled {
        opacity: 0.5;
        cursor: not-allowed;
      }
    `
  ]
})
export class UiButtonComponent {
  @Input() variant: 'primary' | 'outline' | 'ghost' = 'primary';
  @Input() size: 'sm' | 'md' | 'lg' = 'md';
  @Input() disabled = false;
  @Output() readonly clicked = new EventEmitter<Event>();
}
