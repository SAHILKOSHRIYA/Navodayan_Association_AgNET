import { Injectable, signal } from '@angular/core';

export interface Toast { id: number; text: string; kind: 'success' | 'error' | 'info'; }

/** Minimal transient-notification store rendered by the ToastHost component. */
@Injectable({ providedIn: 'root' })
export class ToastService {
  private _toasts = signal<Toast[]>([]);
  readonly toasts = this._toasts.asReadonly();
  private seq = 0;

  success(text: string) { this.push(text, 'success'); }
  error(text: string) { this.push(text, 'error'); }
  info(text: string) { this.push(text, 'info'); }

  private push(text: string, kind: Toast['kind']) {
    const id = ++this.seq;
    this._toasts.update((t) => [...t, { id, text, kind }]);
    setTimeout(() => this._toasts.update((t) => t.filter((x) => x.id !== id)), 4500);
  }

  dismiss(id: number) { this._toasts.update((t) => t.filter((x) => x.id !== id)); }
}
