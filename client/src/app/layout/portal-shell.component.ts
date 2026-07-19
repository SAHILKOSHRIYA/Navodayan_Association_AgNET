import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../core/auth/auth.service';

interface NavItem { path: string; label: string; icon: string; }

@Component({
  selector: 'app-portal-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="flex min-h-screen bg-slate-50">
      <aside class="hidden w-60 flex-col border-r border-slate-200 bg-white md:flex">
        <a routerLink="/" class="flex items-center gap-2 border-b border-slate-100 px-5 py-4 font-heading font-bold text-primary-800">
          <span class="grid h-8 w-8 place-items-center rounded-lg bg-primary-800 text-sm text-white">NAU</span>
          Alumni Portal
        </a>
        <nav class="flex-1 space-y-1 p-3">
          @for (n of memberNav; track n.path) {
            <a [routerLink]="n.path" routerLinkActive="bg-primary-800 text-white" [routerLinkActiveOptions]="{ exact: true }"
               class="flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium text-ink-600 hover:bg-slate-100">
              <span>{{ n.icon }}</span>{{ n.label }}
            </a>
          }
          @if (auth.isAdmin()) {
            <div class="px-3 pb-1 pt-4 text-xs font-semibold uppercase tracking-wide text-ink-400">Admin</div>
            @for (n of adminNav; track n.path) {
              <a [routerLink]="n.path" routerLinkActive="bg-primary-800 text-white" [routerLinkActiveOptions]="{ exact: true }"
                 class="flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium text-ink-600 hover:bg-slate-100">
                <span>{{ n.icon }}</span>{{ n.label }}
              </a>
            }
          }
        </nav>
      </aside>

      <div class="flex min-w-0 flex-1 flex-col">
        <header class="flex items-center justify-between border-b border-slate-200 bg-white px-4 py-3">
          <div class="text-sm font-medium text-ink-600">JNV Raipur · Alumni Association</div>
          <div class="flex items-center gap-3">
            <span class="hidden text-sm text-ink-600 sm:inline">{{ auth.user()?.fullName }}</span>
            <div class="grid h-8 w-8 place-items-center rounded-full bg-primary-800 text-sm font-semibold text-white">
              {{ initials() }}
            </div>
            <button class="btn-ghost" (click)="logout()">Logout</button>
          </div>
        </header>
        <main class="mx-auto w-full max-w-6xl flex-1 p-4 sm:p-6"><router-outlet /></main>
      </div>
    </div>
  `,
})
export class PortalShellComponent {
  auth = inject(AuthService);
  private router = inject(Router);
  mobileOpen = signal(false);

  memberNav: NavItem[] = [
    { path: '/app', label: 'Dashboard', icon: '🏠' },
    { path: '/app/profile', label: 'My profile', icon: '👤' },
    { path: '/app/directory', label: 'Directory', icon: '🔎' },
    { path: '/app/donations', label: 'My donations', icon: '💳' },
  ];
  adminNav: NavItem[] = [
    { path: '/admin', label: 'Dashboard', icon: '📊' },
    { path: '/admin/verifications', label: 'Verifications', icon: '✅' },
    { path: '/admin/campaigns', label: 'Campaigns', icon: '🎯' },
    { path: '/admin/donations', label: 'Donations', icon: '🧾' },
    { path: '/admin/users', label: 'Users', icon: '👥' },
  ];

  initials() {
    const name = this.auth.user()?.fullName ?? '';
    return name.split(' ').map((p) => p[0]).slice(0, 2).join('').toUpperCase();
  }

  logout() {
    this.auth.logout();
    this.router.navigate(['/']);
  }
}
