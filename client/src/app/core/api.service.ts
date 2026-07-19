import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiResponse } from './models';

/**
 * Thin wrapper over HttpClient that unwraps the standard { success, data, ... } envelope,
 * so components work with the payload directly. Errors surface via the HTTP error channel
 * (handled by the error interceptor).
 */
@Injectable({ providedIn: 'root' })
export class ApiService {
  private http = inject(HttpClient);
  readonly base = '/api/v1';

  get<T>(url: string, params?: Record<string, string | number | boolean | undefined>): Observable<T> {
    return this.http
      .get<ApiResponse<T>>(`${this.base}${url}`, { params: toParams(params) })
      .pipe(map(unwrap));
  }

  post<T>(url: string, body?: unknown): Observable<T> {
    return this.http.post<ApiResponse<T>>(`${this.base}${url}`, body ?? {}).pipe(map(unwrap));
  }

  put<T>(url: string, body?: unknown): Observable<T> {
    return this.http.put<ApiResponse<T>>(`${this.base}${url}`, body ?? {}).pipe(map(unwrap));
  }

  patch<T>(url: string, body?: unknown): Observable<T> {
    return this.http.patch<ApiResponse<T>>(`${this.base}${url}`, body ?? {}).pipe(map(unwrap));
  }

  delete<T>(url: string): Observable<T> {
    return this.http.delete<ApiResponse<T>>(`${this.base}${url}`).pipe(map(unwrap));
  }

  upload<T>(url: string, form: FormData): Observable<T> {
    return this.http.post<ApiResponse<T>>(`${this.base}${url}`, form).pipe(map(unwrap));
  }

  /** Absolute URL for a stored file (profile photo, campaign cover, …). */
  fileUrl(key?: string | null): string | null {
    return key ? `${this.base}/files/${key}` : null;
  }
}

function unwrap<T>(res: ApiResponse<T>): T {
  return res.data as T;
}

function toParams(params?: Record<string, string | number | boolean | undefined>): HttpParams {
  let p = new HttpParams();
  if (params) {
    for (const [k, v] of Object.entries(params)) {
      if (v !== undefined && v !== null && v !== '') p = p.set(k, String(v));
    }
  }
  return p;
}
