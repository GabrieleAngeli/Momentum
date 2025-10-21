import { Directive, DestroyRef, Input, TemplateRef, ViewContainerRef, inject } from '@angular/core';
import { FeatureFlagService } from './feature-flag.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Directive({
  selector: '[ifFlag]',
  standalone: true
})
export class IfFlagDirective {
  private readonly view = inject(ViewContainerRef);
  private readonly template = inject(TemplateRef<any>);
  private readonly flags = inject(FeatureFlagService);
  private readonly destroyRef = inject(DestroyRef);
  private hasView = false;
  private currentKey = '';

  constructor() {
    this.flags.changes.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.update());
  }

  @Input('ifFlag') set flagKey(key: string) {
    this.currentKey = key;
    this.update();
  }

  private update(): void {
    const enabled = this.flags.getBoolean(this.currentKey);
    if (enabled && !this.hasView) {
      this.view.createEmbeddedView(this.template);
      this.hasView = true;
    } else if (!enabled && this.hasView) {
      this.view.clear();
      this.hasView = false;
    }
  }
}
