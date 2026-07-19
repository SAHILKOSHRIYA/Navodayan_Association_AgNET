import { HttpErrorResponse, HttpHandlerFn, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, catchError, filter, switchMap, take, throwError } from 'rxjs';
import { ApiResponse, AuthResult } from '../models';
import { AuthService } from './auth.service';

// Shared refresh state so concurrent 401s trigger only one refresh call.
let refreshing = false;
const refreshed$ = new BehaviorSubject<string | null>(null);

/** Attaches the access token and transparently refreshes it once on a 401. */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  const isAuthCall = req.url.includes('/api/v1/auth/');
  const authReq = auth.accessToken && !isAuthCall ? withToken(req, auth.accessToken) : req;

  return next(authReq).pipe(
    catchError((err: HttpErrorResponse) => {
      if (err.status !== 401 || isAuthCall || !auth.refreshToken) return throwError(() => err);
      return handle401(req, next, auth, router);
    }),
  );
};

function handle401(req: HttpRequest<unknown>, next: HttpHandlerFn, auth: AuthService, router: Router): Observable<any> {
  if (refreshing) {
    // Wait for the in-flight refresh, then retry with the new token.
    return refreshed$.pipe(
      filter((t): t is string => t !== null),
      take(1),
      switchMap((token) => next(withToken(req, token))),
    );
  }

  refreshing = true;
  refreshed$.next(null);

  return refreshTokens(auth).pipe(
    switchMap((result) => {
      auth.setSession(result);
      refreshing = false;
      refreshed$.next(result.accessToken);
      return next(withToken(req, result.accessToken));
    }),
    catchError((err) => {
      refreshing = false;
      auth.clearSession();
      router.navigate(['/auth/login']);
      return throwError(() => err);
    }),
  );
}

function refreshTokens(auth: AuthService): Observable<AuthResult> {
  // Direct fetch via HttpClient would recurse through this interceptor; use a plain call.
  return new Observable<AuthResult>((sub) => {
    fetch('/api/v1/auth/refresh', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ refreshToken: auth.refreshToken }),
    })
      .then(async (r) => {
        const body = (await r.json()) as ApiResponse<AuthResult>;
        if (!r.ok || !body.success || !body.data) throw new Error('refresh failed');
        sub.next(body.data);
        sub.complete();
      })
      .catch((e) => sub.error(e));
  });
}

function withToken(req: HttpRequest<unknown>, token: string): HttpRequest<unknown> {
  return req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
}
