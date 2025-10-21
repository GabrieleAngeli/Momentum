import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-ui-date-picker',
  standalone: true,
  imports: [FormsModule],
  template: `
    <label class="ui-date">
      <span *ngIf="label">{{ label }}</span>
      <input type="date" [ngModel]="value" (ngModelChange)="valueChange.emit($event)" />
    </label>
  `,
  styles: [
    `
      .ui-date { display: flex; flex-direction: column; gap: 0.25rem; font-size: 0.9rem; }
      input { padding: 0.5rem 0.75rem; border-radius: 0.6rem; border: 1px solid rgba(15,23,42,0.2); }
    `
  ]
})
export class UiDatePickerComponent {
  @Input() label?: string;
  @Input() value: string | null = null;
  @Output() readonly valueChange = new EventEmitter<string | null>();
}
