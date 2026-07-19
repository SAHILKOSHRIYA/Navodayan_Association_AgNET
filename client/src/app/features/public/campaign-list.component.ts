import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { CampaignService } from '../../core/services';
import { CampaignCard } from '../../core/models';
import { InrPipe } from '../../shared/inr.pipe';

@Component({
  selector: 'app-campaign-list',
  imports: [RouterLink, InrPipe],
  template: `
    <div class="mx-auto max-w-6xl px-4 py-10">
      <h1 class="font-heading text-3xl font-bold">Campaigns</h1>
      <p class="mt-1 text-ink-600">Support the causes our community cares about.</p>

      @if (campaigns(); as list) {
        @if (list.length) {
          <div class="mt-8 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            @for (c of list; track c.id) {
              <a [routerLink]="['/campaigns', c.slug]" class="card overflow-hidden transition hover:shadow-md">
                <div class="flex h-40 items-center justify-center bg-primary-800/5 text-5xl">
                  @if (api.fileUrl(c.coverImageKey); as url) { <img [src]="url" class="h-40 w-full object-cover" alt="" /> } @else { 🎯 }
                </div>
                <div class="p-4">
                  <h3 class="font-heading font-semibold text-ink-900">{{ c.title }}</h3>
                  <div class="mt-3 h-2 overflow-hidden rounded-full bg-slate-100">
                    <div class="h-full rounded-full bg-accent-500" [style.width.%]="c.progressPct"></div>
                  </div>
                  <div class="mt-2 flex justify-between text-xs text-ink-600">
                    <span>{{ c.raisedAmount | inr }} raised</span>
                    <span>{{ c.progressPct }}% of {{ c.goalAmount | inr }}</span>
                  </div>
                </div>
              </a>
            }
          </div>
        } @else {
          <div class="card mt-8 p-10 text-center text-ink-600">No campaigns yet.</div>
        }
      } @else {
        <div class="py-16 text-center text-ink-600">Loading…</div>
      }
    </div>
  `,
})
export class CampaignListComponent {
  private service = inject(CampaignService);
  api = inject(ApiService);
  campaigns = signal<CampaignCard[] | null>(null);

  constructor() {
    this.service.list(1, 30).subscribe((r) => this.campaigns.set(r.items));
  }
}
