import { inject } from '@angular/core';
import {
  CanActivateFn,
  Router,
  ActivatedRouteSnapshot,
  RouterStateSnapshot,
} from '@angular/router';
import { UserService } from '../services/user.service';

/**
 * Route guard to ensure user has entered their name before accessing protected routes.
 * Redirects to /name route with returnUrl query param if name is not set.
 */
export const userNameGuard: CanActivateFn = (
  route: ActivatedRouteSnapshot,
  state: RouterStateSnapshot,
) => {
  const userService = inject(UserService);
  const router = inject(Router);

  if (userService.hasName()) {
    return true;
  }

  // Redirect to name entry with return URL
  return router.createUrlTree(['/name'], {
    queryParams: { returnUrl: state.url },
  });
};
