import { Component, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../core/auth/auth.service';

@Component({
  selector: 'app-public-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <header class="sticky top-0 z-40 border-b border-slate-200 bg-white/95 backdrop-blur">
      <nav class="mx-auto flex max-w-6xl items-center justify-between gap-4 px-4 py-3">
        <a routerLink="/" class="flex items-center gap-2 font-heading text-lg font-bold text-primary-800">
          <span class="grid h-8 w-8 place-items-center rounded-lg bg-primary-800 text-sm text-white">NAU</span>
          <span class="hidden sm:inline">Navodaya Alumni</span>
        </a>

        <div class="hidden items-center gap-1 md:flex">
          @for (l of links; track l.path) {
            <a [routerLink]="l.path" routerLinkActive="text-primary-800"
               [routerLinkActiveOptions]="{ exact: l.path === '/' }"
               class="rounded-lg px-3 py-2 text-sm font-medium text-ink-600 hover:text-primary-800">{{ l.label }}</a>
          }
        </div>

        <div class="flex items-center gap-2">
          @if (auth.isAuthenticated()) {
            <a routerLink="/app" class="btn-ghost">My portal</a>
          } @else {
            <a routerLink="/auth/login" class="btn-ghost hidden sm:inline-flex">Login</a>
          }
          <a routerLink="/campaigns" class="btn-accent">Donate</a>
          <button class="btn-ghost md:hidden" (click)="open.set(!open())">☰</button>
        </div>
      </nav>

      @if (open()) {
        <div class="border-t border-slate-200 bg-white px-4 py-2 md:hidden">
          @for (l of links; track l.path) {
            <a [routerLink]="l.path" (click)="open.set(false)"
               class="block rounded-lg px-3 py-2 text-sm font-medium text-ink-600 hover:bg-slate-50">{{ l.label }}</a>
          }
        </div>
      }
    </header>

    <main class="min-h-[70vh]"><router-outlet /></main>

    <footer class="mt-16 border-t border-slate-200 bg-white">
      <div class="mx-auto grid max-w-6xl gap-8 px-4 py-10 sm:grid-cols-3">
        <div>
          <div class="font-heading text-lg font-bold text-primary-800">Navodaya Alumni Association</div>
          <p class="mt-2 text-sm text-ink-600">The digital home for JNV alumni — connect, give back, and grow the community.</p>
        </div>
        <div>
          <div class="mb-2 text-sm font-semibold text-ink-900">Explore</div>
          @for (l of links; track l.path) {
            <a [routerLink]="l.path" class="block py-1 text-sm text-ink-600 hover:text-primary-800">{{ l.label }}</a>
          }
        </div>
        <div>
          <div class="mb-2 text-sm font-semibold text-ink-900">Legal</div>
          <p class="text-xs leading-relaxed text-ink-400">
            The Navodayans Uplift Association is not a registered body. Matters of payments, funds and disputes are
            resolved by mutual discussion among members. Refunds are issued only for mistaken payments.
          </p>
        </div>
      </div>
      <div class="border-t border-slate-100 py-4 text-center text-xs text-ink-400">
        © {{ year }} Navodaya Alumni Association · Once a Navodayan, always a Navodayan.
      </div>
    </footer>
  `,
})
export class PublicShellComponent {
  auth = inject(AuthService);
  open = signal(false);
  year = new Date().getFullYear();
  links = [
    { path: '/', label: 'Home' },
    { path: '/campaigns', label: 'Campaigns' },
    { path: '/events', label: 'Events' },
    { path: '/announcements', label: 'News' },
    { path: '/about', label: 'About' },
  ];
}
