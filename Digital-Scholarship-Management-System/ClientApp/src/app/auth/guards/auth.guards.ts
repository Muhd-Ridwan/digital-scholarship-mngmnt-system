import { inject } from '@angular/core';
import { CanActivateFn, Router, ActivatedRouteSnapshot } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { ROLE_ROUTES } from '../role-routes';

export const authGuard: CanActivateFn = async (route: ActivatedRouteSnapshot) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  const profile = await auth.getProfile();
  if (!profile) {
    return router.parseUrl('/login');
  }

  const requiredRole = route.data['role'] as string | undefined;
  if (requiredRole && profile.role !== requiredRole) {
    return router.parseUrl(ROLE_ROUTES[profile.role] ?? '/login');
  }

  return true;
};
