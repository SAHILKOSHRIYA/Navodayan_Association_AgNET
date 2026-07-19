import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { VerificationService } from '../../core/services';
import { VerificationQueueItem } from '../../core/models';
import { ToastService } from '../../core/toast.service';

@Component({
  selector: 'app-verification-queue',
  imports: [DatePipe],
  template: `
    <h1 class="font-heading text-2xl font-bold">Verification queue</h1>

    @if (items(); as list) {
      @if (list.length) {
        <div class="mt-6 space-y-3">
          @for (v of list; track v.requestId) {
            <div class="card p-5">
              <div class="flex flex-wrap items-start justify-between gap-4">
                <div>
                  <div class="font-heading font-semibold text-ink-900">{{ v.fullName }}</div>
                  <div class="text-sm text-ink-600">{{ v.email }} · Batch {{ v.batch }}</div>
                  <div class="mt-1 text-sm text-ink-600">
                    {{ v.designation }}{{ v.designation && v.company ? ' @ ' : '' }}{{ v.company }}
                    @if (v.currentCity) { · {{ v.currentCity }} }
                  </div>
                  <div class="mt-1 text-xs text-ink-400">Profile {{ v.completionPct }}% · submitted {{ v.submittedAt | date: 'd MMM y' }}</div>
                </div>
                <div class="flex gap-2">
                  <button class="btn-primary" [disabled]="busy()" (click)="approve(v)">Approve</button>
                  <button class="btn-ghost text-danger" [disabled]="busy()" (click)="reject(v)">Reject</button>
                </div>
              </div>
            </div>
          }
        </div>
      } @else { <div class="card mt-6 p-10 text-center text-ink-600">No pending verifications. 🎉</div> }
    } @else { <div class="py-16 text-center text-ink-600">Loading…</div> }
  `,
})
export class VerificationQueueComponent {
  private service = inject(VerificationService);
  private toast = inject(ToastService);
  items = signal<VerificationQueueItem[] | null>(null);
  busy = signal(false);

  constructor() { this.load(); }
  load() { this.service.queue(1, 50).subscribe((r) => this.items.set(r.items)); }

  approve(v: VerificationQueueItem) {
    this.busy.set(true);
    this.service.approve(v.requestId).subscribe({
      next: () => { this.toast.success(`${v.fullName} verified.`); this.remove(v.requestId); this.busy.set(false); },
      error: () => this.busy.set(false),
    });
  }

  reject(v: VerificationQueueItem) {
    const reason = prompt(`Reject ${v.fullName}? Enter a reason:`);
    if (!reason) return;
    this.busy.set(true);
    this.service.reject(v.requestId, reason).subscribe({
      next: () => { this.toast.info('Request rejected.'); this.remove(v.requestId); this.busy.set(false); },
      error: () => this.busy.set(false),
    });
  }

  private remove(id: string) { this.items.update((l) => (l ?? []).filter((x) => x.requestId !== id)); }
}
