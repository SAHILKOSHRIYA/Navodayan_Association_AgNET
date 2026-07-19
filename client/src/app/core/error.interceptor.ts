import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { ApiResponse } from './models';
import { ToastService } from './toast.service';

/** Surfaces API error messages as toasts, using the standard envelope when present. */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const toast = inject(ToastService);

  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      // 401s on auth calls are handled by the flow itself (bad credentials shown inline).
      const silent = err.status === 401 && req.url.includes('/api/v1/auth/');
      if (!silent) toast.error(messageFor(err));
      return throwError(() => err);
    }),
  );
};

function messageFor(err: HttpErrorResponse): string {
  const body = err.error as ApiResponse<unknown> | undefined;
  if (body?.message) return body.message;
  if (body?.errors?.length) return body.errors[0].message;
  if (err.status === 0) return 'Cannot reach the server. Please check your connection.';
  if (err.status === 403) return 'You do not have permission to do that.';
  return 'Something went wrong. Please try again.';
}
