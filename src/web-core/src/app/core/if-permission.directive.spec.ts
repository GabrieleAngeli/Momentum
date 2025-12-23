import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AuthService } from './auth.service';
import { IfPermissionDirective } from './if-permission.directive';
import { By } from '@angular/platform-browser';
import { BehaviorSubject } from 'rxjs';
import type { AuthMeResponse } from '@core/types';

class AuthServiceStub {
  private subject = new BehaviorSubject<AuthMeResponse | null>({
    isAuthenticated: true,
    requiresMfa: false,
    user: {
      id: '1',
      email: 'user@example.com',
      displayName: 'User',
      tenantId: 'tenant',
      roles: ['user'],
      permissions: ['feature-a:view'],
      claims: {}
    }
  });

  readonly auth$ = this.subject.asObservable();

  hasPermission(permission: string): boolean {
    return this.subject.value?.user.permissions.includes(permission) ?? false;
  }

  setPermissions(permissions: string[]) {
    const current = this.subject.value!;
    this.subject.next({ ...current, user: { ...current.user, permissions } });
  }
}

@Component({
  standalone: true,
  imports: [IfPermissionDirective],
  template: `<span *appIfPermission="'feature-a:view'">Allowed</span>`
})
class HostComponent {}

describe('IfPermissionDirective', () => {
  let fixture: ComponentFixture<HostComponent>;
  let auth: AuthServiceStub;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HostComponent],
      providers: [{ provide: AuthService, useClass: AuthServiceStub }]
    }).compileComponents();

    fixture = TestBed.createComponent(HostComponent);
    auth = TestBed.inject(AuthService) as any;
    fixture.detectChanges();
  });

  it('renders when permission present', async () => {
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.debugElement.query(By.css('span'))).not.toBeNull();
  });

  it('hides when permission missing', async () => {
    auth.setPermissions([]);
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.debugElement.query(By.css('span'))).toBeNull();
  });
});
