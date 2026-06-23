import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isLoggedIn()) return true;

  return router.createUrlTree(['/login']);
};

export const loginGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.isLoggedIn()) return true;

  if (auth.userLevel() === 'Child') {
    const userId = auth.getUserIdFromToken();
    if (userId) return router.createUrlTree(['/children', userId]);
  }

  return router.createUrlTree(['/children']);
};

export const parentGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isLoggedIn() && auth.userLevel() === 'Parent') return true;

  return router.createUrlTree(['/login']);
};
