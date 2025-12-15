import { Directive, DestroyRef, Input, TemplateRef, ViewContainerRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AuthService } from './auth.service';

@Directive({
  selector: '[appIfPermission]',
  standalone: true
})
export class IfPermissionDirective {
  private readonly view = inject(ViewContainerRef);
  private readonly template = inject(TemplateRef<unknown>);
  private readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);
  private permission = '';
  private hasView = false;

  constructor() {
    this.auth.auth$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.update());
  }

  @Input('appIfPermission') set required(permission: string) {
    this.permission = permission;
    this.update();
  }

  private update(): void {
    const allowed = this.auth.hasPermission(this.permission);
    if (allowed && !this.hasView) {
      this.view.createEmbeddedView(this.template);
      this.hasView = true;
    } else if (!allowed && this.hasView) {
      this.view.clear();
      this.hasView = false;
    }
  }
}
