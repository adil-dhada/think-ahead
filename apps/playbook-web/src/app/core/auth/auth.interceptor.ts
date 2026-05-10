import { HttpInterceptorFn, HttpErrorResponse, HttpRequest, HttpHandlerFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { from, switchMap, catchError, throwError } from 'rxjs';
import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const authed = attachToken(req, auth.accessToken());
  return next(authed).pipe(
    catchError((err: HttpErrorResponse) => {
      if (err.status === 401 && !req.url.endsWith('/graphql') && !req.context.has(SKIP_REFRESH_CTX)) {
        return from(auth.init()).pipe(
          switchMap(() => {
            const token = auth.accessToken();
            return token ? next(attachToken(req, token)) : throwError(() => err);
          }),
          catchError(() => throwError(() => err))
        );
      }
      return throwError(() => err);
    })
  );
};

function attachToken(req: HttpRequest<unknown>, token: string | null): HttpRequest<unknown> {
  if (!token) return req;
  return req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
}

import { HttpContextToken } from '@angular/common/http';
export const SKIP_REFRESH_CTX = new HttpContextToken<boolean>(() => false);
