import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from './auth.service';
import { catchError, switchMap, throwError } from 'rxjs';

export const jwtInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const token = auth.token();

  if (token) {
    req = req.clone({
      setHeaders: { Authorization: `Bearer ${token}` },
    });
  }

  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      if (err.status !== 401 || !token || req.url.includes('/auth/renew')) {
        if (err.status === 401) auth.logout();
        return throwError(() => err);
      }

      return auth.renewToken().pipe(
        switchMap(res => {
          const retriedRequest = req.clone({
            setHeaders: { Authorization: `Bearer ${res.token}` },
          });
          return next(retriedRequest);
        }),
        catchError(() => {
          auth.logout();
          return throwError(() => err);
        })
      );
    })
  );
};
