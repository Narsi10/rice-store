import { Injectable, computed, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, map, of, tap } from 'rxjs';
import { AuthUser } from './models';
import { runtimeConfig } from './app-config';

export interface AuthResult {
  ok: boolean;
  offline: boolean;
  error?: string;
}

/**
 * Handles registration/login against the Identity service (via the gateway).
 * Persists the signed-in user to localStorage so a refresh keeps you logged in.
 * If the backend isn't reachable it falls back to a local "demo" session so
 * the rest of the UI stays usable.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private get gateway() { return runtimeConfig.gatewayUrl; }
  private readonly key = 'rice-store-user';

  /** Emails treated as admins in offline mode (mirrors the backend seeder). */
  static readonly ADMIN_EMAILS = [
    'yaldandanarsireddy@gmail.com',
  ];

  readonly user = signal<AuthUser | null>(this.loadFromStorage());
  readonly isLoggedIn = computed(() => this.user() !== null);

  register(email: string, password: string, fullName: string): Observable<AuthResult> {
    return this.http
      .post<AuthUser>(`${this.gateway}/api/auth/register`, { email, password, fullName })
      .pipe(
        tap((u) => this.setUser(u)),
        map(() => ({ ok: true, offline: false }) as AuthResult),
        catchError((err) => this.handleFallback(err, email, fullName)),
      );
  }

  login(email: string, password: string): Observable<AuthResult> {
    return this.http
      .post<AuthUser>(`${this.gateway}/api/auth/login`, { email, password })
      .pipe(
        tap((u) => this.setUser(u)),
        map(() => ({ ok: true, offline: false }) as AuthResult),
        catchError((err) => this.handleFallback(err, email, email.split('@')[0])),
      );
  }

  logout(): void {
    this.user.set(null);
    try { localStorage.removeItem(this.key); } catch { /* ignore */ }
  }

  private handleFallback(err: unknown, email: string, fullName: string): Observable<AuthResult> {
    // A real HTTP error (e.g. 401/409) from a running backend: surface it.
    const status = (err as { status?: number })?.status;
    if (status && status !== 0) {
      const message =
        status === 401 ? 'Invalid email or password.' :
        status === 409 ? 'That email is already registered.' :
        'Something went wrong. Please try again.';
      return of({ ok: false, offline: false, error: message });
    }

    // status 0 => network/CORS: backend not running. Use a local demo session.
    // Emails in the admin list get the Admin role so the admin workflow is
    // usable offline too.
    const normalized = email.trim().toLowerCase();
    const isAdmin = AuthService.ADMIN_EMAILS.includes(normalized);
    this.setUser({
      userId: isAdmin ? 'admin-' + btoa(normalized).slice(0, 8) : 'demo-' + btoa(normalized).slice(0, 8),
      email,
      fullName: isAdmin && !fullName ? 'Administrator' : fullName,
      role: isAdmin ? 'Admin' : 'Customer',
      token: 'offline-demo-token',
    });
    return of({ ok: true, offline: true });
  }

  private setUser(u: AuthUser): void {
    this.user.set(u);
    try { localStorage.setItem(this.key, JSON.stringify(u)); } catch { /* ignore */ }
  }

  private loadFromStorage(): AuthUser | null {
    try {
      const raw = localStorage.getItem(this.key);
      return raw ? (JSON.parse(raw) as AuthUser) : null;
    } catch {
      return null;
    }
  }
}
