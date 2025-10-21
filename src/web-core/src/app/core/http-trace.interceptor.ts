import { Injectable } from '@angular/core';
import { HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable()
export class TraceInterceptor implements HttpInterceptor {
  intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    const traceId = crypto.randomUUID();
    const request = req.clone({
      setHeaders: {
        'X-Trace-Id': traceId
      }
    });
    return next.handle(request);
  }
}
