import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-about',
  imports: [RouterLink],
  template: `
    <div class="mx-auto max-w-3xl px-4 py-12">
      <h1 class="font-heading text-3xl font-bold">About the Association</h1>
      <p class="mt-4 text-ink-600">
        We are proud alumni of Jawahar Navodaya Vidyalaya, Raipur. This community exists to keep Navodayans
        connected — through a searchable alumni directory, transparent fundraising for members and the school,
        events, and shared opportunities.
      </p>

      <h2 class="mt-8 font-heading text-xl font-bold">Our mission</h2>
      <ul class="mt-3 list-inside list-disc space-y-1 text-ink-600">
        <li>Stay connected across batches, cities and professions</li>
        <li>Donate transparently to causes that support our community</li>
        <li>Organize meets, webinars and reunions</li>
        <li>Grow the JNV network for the next generation</li>
      </ul>

      <div class="card mt-8 p-5 text-sm text-ink-600">
        <strong class="text-ink-900">Disclaimer.</strong> The Navodayans Uplift Association is not a registered
        association, group or committee. Matters relating to payments, funds, transactions, transparency and
        disputes are resolved through mutual discussion among members. Refunds are issued only in cases of
        mistaken payments, and only to members.
      </div>

      <div class="mt-8 flex gap-3">
        <a routerLink="/auth/register" class="btn-primary">Join as alumni</a>
        <a routerLink="/campaigns" class="btn-ghost">See campaigns</a>
      </div>
    </div>
  `,
})
export class AboutComponent {}
