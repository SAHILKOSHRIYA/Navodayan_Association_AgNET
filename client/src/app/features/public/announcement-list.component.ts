import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { AnnouncementService } from '../../core/services';
import { Announcement } from '../../core/models';

@Component({
  selector: 'app-announcement-list',
  imports: [DatePipe],
  template: `
    <div class="mx-auto max-w-3xl px-4 py-10">
      <h1 class="font-heading text-3xl font-bold">News & announcements</h1>
      @if (items(); as list) {
        @if (list.length) {
          <div class="mt-6 space-y-3">
            @for (a of list; track a.id) {
              <div class="card p-5">
                <span class="chip bg-primary-800/10 text-primary-800">{{ categories[a.category] }}</span>
                <h3 class="mt-2 font-heading text-lg font-semibold">{{ a.title }}</h3>
                <p class="mt-1 whitespace-pre-line text-sm text-ink-600">{{ a.body }}</p>
                <div class="mt-2 text-xs text-ink-400">{{ a.publishedAt | date: 'd MMM y' }}</div>
              </div>
            }
          </div>
        } @else { <div class="card mt-6 p-10 text-center text-ink-600">No announcements yet.</div> }
      } @else { <div class="py-16 text-center text-ink-600">Loading…</div> }
    </div>
  `,
})
export class AnnouncementListComponent {
  private service = inject(AnnouncementService);
  items = signal<Announcement[] | null>(null);
  categories = ['General', 'Academic', 'Events', 'Fundraising', 'Achievements'];

  constructor() { this.service.list().subscribe((r) => this.items.set(r.items)); }
}
