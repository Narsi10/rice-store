import { HttpInterceptorFn } from '@angular/common/http';

/**
 * Attaches the signed-in user's JWT as a Bearer token on API calls so the
 * backend can authorize admin-only endpoints. Reads the token from the same
 * localStorage key the AuthService uses.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  try {
    const raw = localStorage.getItem('rice-store-user');
    if (raw) {
      const user = JSON.parse(raw) as { token?: string };
      if (user.token && user.token !== 'offline-demo-token') {
        req = req.clone({
          setHeaders: { Authorization: `Bearer ${user.token}` },
        });
      }
    }
  } catch {
    /* no token — send request as-is */
  }
  return next(req);
};
