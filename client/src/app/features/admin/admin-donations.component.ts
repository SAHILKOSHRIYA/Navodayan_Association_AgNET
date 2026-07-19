import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { AdminService, DonationService } from '../../core/services';
import { DonationListItem } from '../../core/models';
import { InrPipe } from '../../shared/inr.pipe';

@Component({
  selector: 'app-admin-donations',
  imports: [DatePipe, InrPipe],
  template: `
    <div class="flex items-center justify-between">
      <h1 class="font-heading text-2xl font-bold">Donations</h1>
      <a class="btn-ghost" [href]="reportUrl" target="_blank" rel="noopener">⬇ Export CSV</a>
    </div>

    @if (items(); as list) {
      <div class="card mt-6 overflow-x-auto">
        <table class="w-full text-sm">
          <thead class="border-b border-slate-100 text-left text-ink-400">
            <tr><th class="p-3">Date</th><th class="p-3">Campaign</th><th class="p-3">Donor</th><th class="p-3">Amount</th><th class="p-3">Status</th><th class="p-3">Receipt</th></tr>
          </thead>
          <tbody>
            @for (d of list; track d.id) {
              <tr class="border-b border-slate-50">
                <td class="p-3 text-ink-600">{{ (d.capturedAt || d.createdAt) | date: 'd MMM y' }}</td>
                <td class="p-3">{{ d.campaignTitle }}</td>
                <td class="p-3">{{ d.isAnonymous ? 'Anonymous' : d.donorName }}</td>
                <td class="p-3 font-medium">{{ d.amount | inr }}</td>
                <td class="p-3"><span class="chip" [class.bg-success]="d.status===1" [class.text-white]="d.status===1" [class.bg-slate-100]="d.status!==1">{{ statuses[d.status] }}</span></td>
                <td class="p-3 text-ink-400">{{ d.receiptNumber || '—' }}</td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    } @else { <div class="py-16 text-center text-ink-600">Loading…</div> }
  `,
})
export class AdminDonationsComponent {
  private service = inject(DonationService);
  private admin = inject(AdminService);
  items = signal<DonationListItem[] | null>(null);
  statuses = ['Created', 'Captured', 'Failed', 'Refunded'];
  reportUrl = this.admin.reportUrl();

  constructor() { this.service.all(undefined, undefined, 1, 100).subscribe((r) => this.items.set(r.items)); }
}
