import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { DonationService } from '../../core/services';
import { DonationListItem } from '../../core/models';
import { InrPipe } from '../../shared/inr.pipe';

@Component({
  selector: 'app-my-donations',
  imports: [DatePipe, InrPipe],
  template: `
    <h1 class="font-heading text-2xl font-bold">My donations</h1>
    @if (items(); as list) {
      @if (list.length) {
        <div class="card mt-6 overflow-x-auto">
          <table class="w-full text-sm">
            <thead class="border-b border-slate-100 text-left text-ink-400">
              <tr><th class="p-3">Date</th><th class="p-3">Campaign</th><th class="p-3">Amount</th><th class="p-3">Status</th><th class="p-3">Receipt</th></tr>
            </thead>
            <tbody>
              @for (d of list; track d.id) {
                <tr class="border-b border-slate-50">
                  <td class="p-3 text-ink-600">{{ d.createdAt | date: 'd MMM y' }}</td>
                  <td class="p-3 font-medium">{{ d.campaignTitle }}</td>
                  <td class="p-3">{{ d.amount | inr }}</td>
                  <td class="p-3">
                    <span class="chip" [class.bg-success]="d.status === 1" [class.text-white]="d.status === 1"
                          [class.bg-slate-100]="d.status !== 1">{{ statuses[d.status] }}</span>
                  </td>
                  <td class="p-3 text-ink-400">{{ d.receiptNumber || '—' }}</td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      } @else { <div class="card mt-6 p-10 text-center text-ink-600">You haven't made any donations yet.</div> }
    } @else { <div class="py-16 text-center text-ink-600">Loading…</div> }
  `,
})
export class MyDonationsComponent {
  private service = inject(DonationService);
  items = signal<DonationListItem[] | null>(null);
  statuses = ['Created', 'Captured', 'Failed', 'Refunded'];

  constructor() { this.service.mine().subscribe((r) => this.items.set(r.items)); }
}
