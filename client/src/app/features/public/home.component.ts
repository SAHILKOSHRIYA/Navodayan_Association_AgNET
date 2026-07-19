import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { ContentService } from '../../core/services';
import { HomeContent } from '../../core/models';
import { InrPipe } from '../../shared/inr.pipe';

@Component({
  selector: 'app-home',
  imports: [RouterLink, DatePipe, InrPipe],
  template: `
    <!-- Hero -->
    <section class="bg-primary-800 text-white">
      <div class="mx-auto max-w-6xl px-4 py-20 text-center">
        <h1 class="font-heading text-4xl font-bold leading-tight sm:text-5xl">Once a Navodayan,<br />always a Navodayan.</h1>
        <p class="mx-auto mt-4 max-w-2xl text-white/80">
          The official digital home for Jawahar Navodaya Vidyalaya alumni — reconnect with your batch,
          support fundraising campaigns, and grow the community.
        </p>
        <div class="mt-8 flex justify-center gap-3">
          <a routerLink="/auth/register" class="btn-accent">Join as alumni</a>
          <a routerLink="/campaigns" class="btn-ghost bg-white/10 text-white hover:bg-white/20">See campaigns</a>
        </div>
      </div>
    </section>

    @if (content(); as c) {
      <!-- Stats -->
      <section class="mx-auto -mt-10 max-w-5xl px-4">
        <div class="grid grid-cols-2 gap-3 sm:grid-cols-4">
          <div class="card p-4 text-center">
            <div class="font-heading text-2xl font-bold text-primary-800">{{ c.stats.verifiedAlumni }}</div>
            <div class="text-xs text-ink-600">Verified alumni</div>
          </div>
          <div class="card p-4 text-center">
            <div class="font-heading text-2xl font-bold text-primary-800">{{ c.stats.totalRaised | inr }}</div>
            <div class="text-xs text-ink-600">Raised</div>
          </div>
          <div class="card p-4 text-center">
            <div class="font-heading text-2xl font-bold text-primary-800">{{ c.stats.activeCampaigns }}</div>
            <div class="text-xs text-ink-600">Active campaigns</div>
          </div>
          <div class="card p-4 text-center">
            <div class="font-heading text-2xl font-bold text-primary-800">{{ c.stats.upcomingEvents }}</div>
            <div class="text-xs text-ink-600">Upcoming events</div>
          </div>
        </div>
      </section>

      <!-- Campaigns -->
      <section class="mx-auto max-w-6xl px-4 py-12">
        <div class="mb-4 flex items-center justify-between">
          <h2 class="font-heading text-2xl font-bold">Active campaigns</h2>
          <a routerLink="/campaigns" class="text-sm font-medium text-primary-800">View all →</a>
        </div>
        @if (c.latestCampaigns.length) {
          <div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            @for (cam of c.latestCampaigns; track cam.id) {
              <a [routerLink]="['/campaigns', cam.slug]" class="card overflow-hidden transition hover:shadow-md">
                <div class="flex h-32 items-center justify-center bg-primary-800/5 text-4xl">
                  @if (api.fileUrl(cam.coverImageKey); as url) { <img [src]="url" class="h-32 w-full object-cover" alt="" /> } @else { 🎯 }
                </div>
                <div class="p-4">
                  <h3 class="font-heading font-semibold text-ink-900">{{ cam.title }}</h3>
                  <div class="mt-3 h-2 overflow-hidden rounded-full bg-slate-100">
                    <div class="h-full rounded-full bg-accent-500" [style.width.%]="cam.progressPct"></div>
                  </div>
                  <div class="mt-2 flex justify-between text-xs text-ink-600">
                    <span>{{ cam.raisedAmount | inr }} raised</span>
                    <span>{{ cam.progressPct }}% of {{ cam.goalAmount | inr }}</span>
                  </div>
                </div>
              </a>
            }
          </div>
        } @else {
          <div class="card p-8 text-center text-ink-600">No active campaigns right now — check back soon.</div>
        }
      </section>

      <!-- Events + News -->
      <section class="mx-auto grid max-w-6xl gap-8 px-4 pb-16 lg:grid-cols-2">
        <div>
          <h2 class="mb-4 font-heading text-2xl font-bold">Upcoming events</h2>
          @if (c.upcomingEvents.length) {
            @for (e of c.upcomingEvents; track e.id) {
              <div class="card mb-3 flex items-center gap-4 p-4">
                <div class="grid h-12 w-12 place-items-center rounded-lg bg-primary-800/5 text-xl">📅</div>
                <div class="flex-1">
                  <div class="font-medium text-ink-900">{{ e.title }}</div>
                  <div class="text-xs text-ink-600">{{ e.eventDate | date: 'd MMM y, h:mm a' }} · {{ e.location }}</div>
                </div>
              </div>
            }
          } @else { <div class="card p-6 text-center text-sm text-ink-600">No upcoming events.</div> }
        </div>
        <div>
          <h2 class="mb-4 font-heading text-2xl font-bold">Latest news</h2>
          @if (c.recentAnnouncements.length) {
            @for (a of c.recentAnnouncements; track a.id) {
              <div class="card mb-3 p-4">
                <div class="font-medium text-ink-900">{{ a.title }}</div>
                <p class="mt-1 line-clamp-2 text-sm text-ink-600">{{ a.body }}</p>
                <div class="mt-1 text-xs text-ink-400">{{ a.publishedAt | date: 'd MMM y' }}</div>
              </div>
            }
          } @else { <div class="card p-6 text-center text-sm text-ink-600">No announcements yet.</div> }
        </div>
      </section>
    } @else {
      <div class="py-20 text-center text-ink-600">Loading…</div>
    }
  `,
})
export class HomeComponent {
  private contentService = inject(ContentService);
  api = inject(ApiService);
  content = signal<HomeContent | null>(null);

  constructor() {
    this.contentService.home().subscribe((c) => this.content.set(c));
  }
}
