import { Injectable } from '@angular/core';
import { HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable()
export class CredentialsInterceptor implements HttpInterceptor {
  intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    const headers: Record<string, string> = {};
    if (!['GET', 'HEAD', 'OPTIONS', 'TRACE'].includes(req.method)) {
      const token = this.readCookie('XSRF-TOKEN');
      if (token) {
        headers['X-CSRF-TOKEN'] = token;
      }
    }

    const request = req.clone({ withCredentials: true, setHeaders: headers });
    return next.handle(request);
  }

  private readCookie(name: string): string | null {
    if (typeof document === 'undefined') {
      return null;
    }
    const match = document.cookie.match(new RegExp('(^| )' + name + '=([^;]+)'));
    return match ? decodeURIComponent(match[2]) : null;
  }
}
