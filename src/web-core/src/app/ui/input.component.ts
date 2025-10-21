import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-ui-input',
  standalone: true,
  imports: [FormsModule],
  template: `
    <label class="ui-field">
      <span *ngIf="label">{{ label }}</span>
      <input
        [type]="type"
        [placeholder]="placeholder"
        [ngModel]="value"
        (ngModelChange)="valueChange.emit($event)"
      />
    </label>
  `,
  styles: [
    `
      .ui-field { display: flex; flex-direction: column; gap: 0.25rem; }
      input {
        padding: 0.5rem 0.75rem;
        border-radius: 0.6rem;
        border: 1px solid rgba(15,23,42,0.2);
        background: var(--surface-strong, #fff);
      }
    `
  ]
})
export class UiInputComponent {
  @Input() label?: string;
  @Input() placeholder?: string;
  @Input() type: string = 'text';
  @Input() value = '';
  @Output() readonly valueChange = new EventEmitter<string>();
}
