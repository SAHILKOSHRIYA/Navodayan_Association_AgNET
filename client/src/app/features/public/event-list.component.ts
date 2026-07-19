import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { EventService } from '../../core/services';
import { EventCard } from '../../core/models';

@Component({
  selector: 'app-event-list',
  imports: [DatePipe],
  template: `
    <div class="mx-auto max-w-5xl px-4 py-10">
      <h1 class="font-heading text-3xl font-bold">Events</h1>
      <div class="mt-4 inline-flex rounded-lg border border-slate-300 p-1">
        <button class="rounded-md px-4 py-1.5 text-sm font-medium" [class.bg-primary-800]="scope() === 'upcoming'"
                [class.text-white]="scope() === 'upcoming'" (click)="load('upcoming')">Upcoming</button>
        <button class="rounded-md px-4 py-1.5 text-sm font-medium" [class.bg-primary-800]="scope() === 'past'"
                [class.text-white]="scope() === 'past'" (click)="load('past')">Past</button>
      </div>

      @if (events(); as list) {
        @if (list.length) {
          <div class="mt-6 space-y-3">
            @for (e of list; track e.id) {
              <div class="card flex items-center gap-4 p-4">
                <div class="grid h-14 w-14 place-items-center rounded-lg bg-primary-800/5 text-2xl">📅</div>
                <div class="flex-1">
                  <div class="font-heading font-semibold text-ink-900">{{ e.title }}</div>
                  <div class="text-sm text-ink-600">{{ e.eventDate | date: 'EEE, d MMM y · h:mm a' }}</div>
                  @if (e.location) { <div class="text-sm text-ink-400">{{ e.location }}</div> }
                </div>
                <div class="text-right text-sm text-ink-600">{{ e.goingCount }} going</div>
              </div>
            }
          </div>
        } @else { <div class="card mt-6 p-10 text-center text-ink-600">No {{ scope() }} events.</div> }
      } @else { <div class="py-16 text-center text-ink-600">Loading…</div> }
    </div>
  `,
})
export class EventListComponent {
  private service = inject(EventService);
  events = signal<EventCard[] | null>(null);
  scope = signal<'upcoming' | 'past'>('upcoming');

  constructor() { this.load('upcoming'); }

  load(scope: 'upcoming' | 'past') {
    this.scope.set(scope);
    this.events.set(null);
    this.service.list(scope).subscribe((r) => this.events.set(r.items));
  }
}
