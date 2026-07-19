import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

/** Requires a signed-in user. */
export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return auth.isAuthenticated() ? true : router.createUrlTree(['/auth/login']);
};

/** Requires a verified email (used for full member areas like the directory). */
export const verifiedGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (!auth.isAuthenticated()) return router.createUrlTree(['/auth/login']);
  return auth.isVerified() ? true : router.createUrlTree(['/app']);
};

/** Requires an admin role. */
export const adminGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (!auth.isAuthenticated()) return router.createUrlTree(['/auth/login']);
  return auth.isAdmin() ? true : router.createUrlTree(['/app']);
};
