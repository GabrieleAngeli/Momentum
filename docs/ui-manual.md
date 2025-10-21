# UI Kit Manual

The UI kit provides a minimal, neutral set of standalone components to compose pages across the shell and remote modules.

## Button

```
<app-ui-button variant="outline" size="sm" (clicked)="save()">Save</app-ui-button>
```

- `variant`: `primary` | `outline` | `ghost`
- `size`: `sm` | `md` | `lg`
- Emits `clicked` event on interaction.

## Dialog

```
<app-ui-dialog [open]="show" title="Confirm" (close)="show = false">
  <p>Proceed with the action?</p>
  <div dialog-actions>
    <app-ui-button variant="ghost" (clicked)="show = false">Cancel</app-ui-button>
    <app-ui-button (clicked)="confirm()">Confirm</app-ui-button>
  </div>
</app-ui-dialog>
```

## Date Picker

```
<app-ui-date-picker label="Due date" [(value)]="model.dueDate"></app-ui-date-picker>
```

## Form Inputs

```
<app-ui-input label="Title" [(value)]="model.title"></app-ui-input>
<app-ui-textarea label="Notes" rows="4" [(value)]="model.notes"></app-ui-textarea>
<app-ui-select label="Assignee" [options]="users" [(value)]="model.assignee"></app-ui-select>
```

`users` is an array of `{ value: string; label: string }`.

## Table

```
<app-ui-table [columns]="['name','status']" [data]="rows"></app-ui-table>
```

`rows` is an array of objects where each property matches a column key.

## Toasts

```
<app-ui-toast [messages]="toasts"></app-ui-toast>
```

`toasts` is an array of `{ id: string; text: string; tone?: 'info'|'success'|'error'|'warning' }`.

## Example usage in Shell

```
<app-ui-button (clicked)="loginPrompt()">Login</app-ui-button>
<app-ui-table [columns]="['route','flag']" [data]="manifestRows"></app-ui-table>
```

## Example usage in Feature A remote

```
<section>
  <app-ui-input label="{{ 'featureA.form.name' | translate }}" [(value)]="name()"></app-ui-input>
  <app-ui-button (clicked)="addRecord()">{{ 'featureA.actions.add' | translate }}</app-ui-button>
  <app-ui-table [columns]="['name','created']" [data]="rows()"></app-ui-table>
</section>
```
