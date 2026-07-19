import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { ApiResponse, AuthResult, AuthUser } from '../models';

const ACCESS_KEY = 'nau_access';
const REFRESH_KEY = 'nau_refresh';
const USER_KEY = 'nau_user';

/** Central authentication state (signals) + auth API calls. */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private base = '/api/v1/auth';

  private _user = signal<AuthUser | null>(readJson(USER_KEY));
  readonly user = this._user.asReadonly();
  readonly isAuthenticated = computed(() => this._user() !== null);
  readonly isVerified = computed(() => this._user()?.emailVerified ?? false);
  readonly isAdmin = computed(() => this.hasAnyRole('SuperAdmin', 'AssociationAdmin'));

  get accessToken(): string | null { return localStorage.getItem(ACCESS_KEY); }
  get refreshToken(): string | null { return localStorage.getItem(REFRESH_KEY); }

  hasRole(role: string): boolean { return this._user()?.roles.includes(role) ?? false; }
  hasAnyRole(...roles: string[]): boolean { return roles.some((r) => this.hasRole(r)); }

  register(fullName: string, email: string, password: string): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(`${this.base}/register`, { fullName, email, password });
  }

  verifyEmail(email: string, token: string): Observable<ApiResponse<unknown>> {
    return this.http.post<ApiResponse<unknown>>(`${this.base}/verify-email`, { email, token });
  }

  resendVerification(email: string): Observable<ApiResponse<unknown>> {
    return this.http.post<ApiResponse<unknown>>(`${this.base}/resend-verification`, { email });
  }

  forgotPassword(email: string): Observable<ApiResponse<unknown>> {
    return this.http.post<ApiResponse<unknown>>(`${this.base}/forgot-password`, { email });
  }

  resetPassword(email: string, token: string, newPassword: string): Observable<ApiResponse<unknown>> {
    return this.http.post<ApiResponse<unknown>>(`${this.base}/reset-password`, { email, token, newPassword });
  }

  login(email: string, password: string): Observable<ApiResponse<AuthResult>> {
    return this.http.post<ApiResponse<AuthResult>>(`${this.base}/login`, { email, password }).pipe(
      tap((res) => res.data && this.setSession(res.data)),
    );
  }

  logout(): void {
    const refreshToken = this.refreshToken;
    if (refreshToken) this.http.post(`${this.base}/logout`, { refreshToken }).subscribe({ error: () => {} });
    this.clearSession();
  }

  setSession(result: AuthResult): void {
    localStorage.setItem(ACCESS_KEY, result.accessToken);
    localStorage.setItem(REFRESH_KEY, result.refreshToken);
    localStorage.setItem(USER_KEY, JSON.stringify(result.user));
    this._user.set(result.user);
  }

  clearSession(): void {
    localStorage.removeItem(ACCESS_KEY);
    localStorage.removeItem(REFRESH_KEY);
    localStorage.removeItem(USER_KEY);
    this._user.set(null);
  }
}

function readJson<T>(key: string): T | null {
  const raw = localStorage.getItem(key);
  try { return raw ? (JSON.parse(raw) as T) : null; } catch { return null; }
}
