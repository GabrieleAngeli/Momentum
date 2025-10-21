import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';

export interface UiSelectOption {
  value: string;
  label: string;
}

@Component({
  selector: 'app-ui-select',
  standalone: true,
  imports: [FormsModule],
  template: `
    <label class="ui-field">
      <span *ngIf="label">{{ label }}</span>
      <select [ngModel]="value" (ngModelChange)="valueChange.emit($event)">
        <option *ngFor="let option of options" [value]="option.value">{{ option.label }}</option>
      </select>
    </label>
  `,
  styles: [
    `
      select {
        padding: 0.5rem 0.75rem;
        border-radius: 0.6rem;
        border: 1px solid rgba(15,23,42,0.2);
        background: var(--surface-strong, #fff);
      }
    `
  ]
})
export class UiSelectComponent {
  @Input() label?: string;
  @Input() options: UiSelectOption[] = [];
  @Input() value: string | null = null;
  @Output() readonly valueChange = new EventEmitter<string>();
}
