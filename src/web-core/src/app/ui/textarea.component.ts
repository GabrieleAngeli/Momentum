import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-ui-textarea',
  standalone: true,
  imports: [FormsModule, CommonModule],
  template: `
    <label class="ui-field">
      <span *ngIf="label">{{ label }}</span>
      <textarea
        [rows]="rows"
        [placeholder]="placeholder"
        [ngModel]="value"
        (ngModelChange)="valueChange.emit($event)"
      ></textarea>
    </label>
  `,
  styles: [
    `
      textarea {
        padding: 0.75rem;
        border-radius: 0.75rem;
        border: 1px solid rgba(15,23,42,0.2);
        resize: vertical;
        min-height: 5rem;
      }
    `
  ]
})
export class UiTextareaComponent {
  @Input() label?: string;
  @Input() placeholder?: string;
  @Input() rows = 3;
  @Input() value = '';
  @Output() readonly valueChange = new EventEmitter<string>();
}
