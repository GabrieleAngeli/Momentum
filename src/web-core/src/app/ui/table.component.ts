import { Component, Input } from '@angular/core';
import { NgFor, NgIf } from '@angular/common';

@Component({
  selector: 'app-ui-table',
  standalone: true,
  imports: [NgFor, NgIf],
  template: `
    <div class="ui-table">
      <table>
        <thead *ngIf="columns?.length">
          <tr>
            <th *ngFor="let column of columns">{{ column }}</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let row of data">
            <td *ngFor="let column of columns">{{ row[column] }}</td>
          </tr>
        </tbody>
      </table>
      <div class="ui-table__empty" *ngIf="!data?.length">{{ emptyState }}</div>
    </div>
  `,
  styles: [
    `
      .ui-table { border: 1px solid rgba(15,23,42,0.08); border-radius: 1rem; overflow: hidden; background: #fff; }
      table { width: 100%; border-collapse: collapse; }
      th, td { padding: 0.75rem 1rem; text-align: left; border-bottom: 1px solid rgba(15,23,42,0.06); }
      thead { background: rgba(15,23,42,0.04); font-size: 0.85rem; text-transform: uppercase; letter-spacing: 0.05em; }
      .ui-table__empty { padding: 1rem; text-align: center; color: rgba(15,23,42,0.5); }
    `
  ]
})
export class UiTableComponent {
  @Input() columns: string[] = [];
  @Input() data: Array<Record<string, unknown>> = [];
  @Input() emptyState = 'Nothing to display';
}
