import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AdminService } from '../../core/services';
import { Dashboard } from '../../core/models';
import { InrPipe } from '../../shared/inr.pipe';

@Component({
  selector: 'app-admin-dashboard',
  imports: [RouterLink, InrPipe],
  template: `
    <h1 class="font-heading text-2xl font-bold">Admin dashboard</h1>

    @if (data(); as d) {
      <div class="mt-6 grid grid-cols-2 gap-3 lg:grid-cols-4">
        <div class="card p-4"><div class="text-xs text-ink-600">Registered alumni</div><div class="font-heading text-2xl font-bold text-primary-800">{{ d.cards.registeredAlumni }}</div></div>
        <div class="card p-4"><div class="text-xs text-ink-600">Verified</div><div class="font-heading text-2xl font-bold text-success">{{ d.cards.verifiedAlumni }}</div></div>
        <a routerLink="/admin/verifications" class="card p-4 transition hover:shadow-md"><div class="text-xs text-ink-600">Pending verifications</div><div class="font-heading text-2xl font-bold text-warning">{{ d.cards.pendingVerifications }}</div></a>
        <div class="card p-4"><div class="text-xs text-ink-600">Funds raised</div><div class="font-heading text-2xl font-bold text-primary-800">{{ d.cards.fundsRaised | inr }}</div></div>
        <div class="card p-4"><div class="text-xs text-ink-600">Active campaigns</div><div class="font-heading text-2xl font-bold">{{ d.cards.activeCampaigns }}</div></div>
        <div class="card p-4"><div class="text-xs text-ink-600">Upcoming events</div><div class="font-heading text-2xl font-bold">{{ d.cards.upcomingEvents }}</div></div>
        <div class="card p-4"><div class="text-xs text-ink-600">Total donations</div><div class="font-heading text-2xl font-bold">{{ d.cards.totalDonations }}</div></div>
      </div>

      <div class="mt-6 grid gap-4 lg:grid-cols-2">
        <div class="card p-5">
          <div class="mb-4 text-sm font-semibold">Monthly donations</div>
          <div class="flex h-40 items-end gap-2">
            @for (p of d.monthlyDonations; track p.label) {
              <div class="flex flex-1 flex-col items-center gap-1">
                <div class="w-full rounded-t bg-accent-500" [style.height.%]="barHeight(p.value, maxDonation())"></div>
                <span class="text-[10px] text-ink-400">{{ p.label }}</span>
              </div>
            }
          </div>
        </div>
        <div class="card p-5">
          <div class="mb-4 text-sm font-semibold">New registrations</div>
          <div class="flex h-40 items-end gap-2">
            @for (p of d.registrationTrend; track p.label) {
              <div class="flex flex-1 flex-col items-center gap-1">
                <div class="w-full rounded-t bg-primary-800" [style.height.%]="barHeight(p.value, maxReg())"></div>
                <span class="text-[10px] text-ink-400">{{ p.label }}</span>
              </div>
            }
          </div>
        </div>
      </div>

      <div class="card mt-4 p-5">
        <div class="mb-3 text-sm font-semibold">Verification breakdown</div>
        <div class="flex gap-4 text-sm">
          <span class="chip bg-warning/10 text-warning">Pending {{ d.verification.pending }}</span>
          <span class="chip bg-success/10 text-success">Approved {{ d.verification.approved }}</span>
          <span class="chip bg-danger/10 text-danger">Rejected {{ d.verification.rejected }}</span>
        </div>
      </div>
    } @else { <div class="py-16 text-center text-ink-600">Loading…</div> }
  `,
})
export class AdminDashboardComponent {
  private service = inject(AdminService);
  data = signal<Dashboard | null>(null);
  maxDonation = computed(() => Math.max(1, ...(this.data()?.monthlyDonations.map((p) => p.value) ?? [1])));
  maxReg = computed(() => Math.max(1, ...(this.data()?.registrationTrend.map((p) => p.value) ?? [1])));

  constructor() { this.service.dashboard().subscribe((d) => this.data.set(d)); }
  barHeight(v: number, max: number) { return Math.max(3, (v / max) * 100); }
}
