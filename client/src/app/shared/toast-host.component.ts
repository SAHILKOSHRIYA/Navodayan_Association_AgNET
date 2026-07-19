import { Component, inject } from '@angular/core';
import { ToastService } from '../core/toast.service';

@Component({
  selector: 'app-toast-host',
  template: `
    <div class="fixed bottom-4 right-4 z-50 flex flex-col gap-2">
      @for (t of toast.toasts(); track t.id) {
        <div
          class="card flex items-start gap-3 px-4 py-3 text-sm shadow-lg"
          [class.border-l-4]="true"
          [style.border-left-color]="color(t.kind)"
          role="status">
          <span class="flex-1">{{ t.text }}</span>
          <button class="text-ink-400 hover:text-ink-900" (click)="toast.dismiss(t.id)">✕</button>
        </div>
      }
    </div>
  `,
})
export class ToastHostComponent {
  toast = inject(ToastService);
  color(kind: string) {
    return kind === 'success' ? '#15803D' : kind === 'error' ? '#B91C1C' : '#1E4A8F';
  }
}
