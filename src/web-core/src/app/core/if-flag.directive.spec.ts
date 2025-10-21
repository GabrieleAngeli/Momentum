import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FeatureFlagService } from './feature-flag.service';
import { IfFlagDirective } from './if-flag.directive';
import { By } from '@angular/platform-browser';
import { Subject } from 'rxjs';

class FeatureFlagServiceStub {
  private store: Record<string, boolean> = { 'demo.flag': true };
  readonly changes = new Subject<Record<string, any>>();

  getBoolean(key: string, defaultValue = false): boolean {
    return this.store[key] ?? defaultValue;
  }

  set(key: string, value: boolean) {
    this.store[key] = value;
    this.changes.next(this.store);
  }
}

@Component({
  standalone: true,
  imports: [IfFlagDirective],
  template: `<p *ifFlag="'demo.flag'">Visible</p>`
})
class HostComponent {}

describe('IfFlagDirective', () => {
  let fixture: ComponentFixture<HostComponent>;
  let flags: FeatureFlagServiceStub;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HostComponent],
      providers: [{ provide: FeatureFlagService, useClass: FeatureFlagServiceStub }]
    }).compileComponents();

    fixture = TestBed.createComponent(HostComponent);
    flags = TestBed.inject(FeatureFlagService) as any;
    fixture.detectChanges();
  });

  it('renders when flag enabled', () => {
    const paragraph = fixture.debugElement.query(By.css('p'));
    expect(paragraph).not.toBeNull();
  });

  it('hides when flag disabled', () => {
    flags.set('demo.flag', false);
    fixture.detectChanges();
    const paragraph = fixture.debugElement.query(By.css('p'));
    expect(paragraph).toBeNull();
  });
});
