import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { ProfileService, VerificationService } from '../../core/services';
import { Profile, VerificationRequest } from '../../core/models';
import { ToastService } from '../../core/toast.service';

@Component({
  selector: 'app-member-dashboard',
  imports: [RouterLink],
  template: `
    <h1 class="font-heading text-2xl font-bold">Welcome, {{ auth.user()?.fullName }}</h1>

    <!-- Verification status banner -->
    @if (profile(); as p) {
      <div class="card mt-6 p-5">
        <div class="flex flex-wrap items-center justify-between gap-4">
          <div>
            <div class="text-sm font-semibold text-ink-900">Verification status</div>
            @switch (statusView()) {
              @case ('verified') { <span class="chip mt-1 bg-success/10 text-success">✓ Verified alumnus</span> }
              @case ('pending')  { <span class="chip mt-1 bg-warning/10 text-warning">⏳ Under review</span> }
              @case ('rejected') {
                <span class="chip mt-1 bg-danger/10 text-danger">Rejected</span>
                <p class="mt-1 text-sm text-ink-600">{{ request()?.rejectionReason }}</p>
              }
              @default { <p class="mt-1 text-sm text-ink-600">Complete your profile and submit for verification.</p> }
            }
          </div>
          @if (statusView() === 'none' || statusView() === 'rejected') {
            <button class="btn-primary" [disabled]="submitting() || p.completionPct < 60" (click)="submit()">
              Submit for verification
            </button>
          }
        </div>
        @if (p.completionPct < 60 && statusView() !== 'verified') {
          <p class="mt-3 text-xs text-ink-400">Your profile is {{ p.completionPct }}% complete — reach 60% to submit.</p>
        }
      </div>

      <!-- Profile completion -->
      <div class="mt-4 grid gap-4 sm:grid-cols-3">
        <div class="card p-5">
          <div class="text-sm text-ink-600">Profile completion</div>
          <div class="mt-2 flex items-center gap-3">
            <div class="h-2 flex-1 overflow-hidden rounded-full bg-slate-100">
              <div class="h-full rounded-full bg-primary-800" [style.width.%]="p.completionPct"></div>
            </div>
            <span class="font-heading font-bold text-primary-800">{{ p.completionPct }}%</span>
          </div>
          <a routerLink="/app/profile" class="mt-3 inline-block text-sm font-medium text-primary-800">Edit profile →</a>
        </div>
        <a routerLink="/app/directory" class="card p-5 transition hover:shadow-md">
          <div class="text-2xl">🔎</div><div class="mt-2 font-medium">Alumni directory</div>
          <div class="text-sm text-ink-600">Find batchmates & peers</div>
        </a>
        <a routerLink="/app/donations" class="card p-5 transition hover:shadow-md">
          <div class="text-2xl">💳</div><div class="mt-2 font-medium">My donations</div>
          <div class="text-sm text-ink-600">History & receipts</div>
        </a>
      </div>
    } @else if (loaded()) {
      <div class="card mt-6 p-8 text-center">
        <p class="text-ink-600">You haven't created your profile yet.</p>
        <a routerLink="/app/profile" class="btn-primary mt-4">Create my profile</a>
      </div>
    } @else {
      <div class="py-16 text-center text-ink-600">Loading…</div>
    }
  `,
})
export class MemberDashboardComponent {
  auth = inject(AuthService);
  private profileService = inject(ProfileService);
  private verification = inject(VerificationService);
  private toast = inject(ToastService);

  profile = signal<Profile | null>(null);
  request = signal<VerificationRequest | null>(null);
  loaded = signal(false);
  submitting = signal(false);

  constructor() {
    this.profileService.mine().subscribe({
      next: (p) => { this.profile.set(p); this.loaded.set(true); },
      error: () => this.loaded.set(true),
    });
    this.verification.mine().subscribe((r) => this.request.set(r));
  }

  statusView(): 'verified' | 'pending' | 'rejected' | 'none' {
    if (this.profile()?.isVerified) return 'verified';
    const r = this.request();
    if (!r) return 'none';
    return r.status === 0 ? 'pending' : r.status === 2 ? 'rejected' : 'none';
  }

  submit() {
    this.submitting.set(true);
    this.verification.submit().subscribe({
      next: (r) => { this.request.set(r); this.submitting.set(false); this.toast.success('Submitted for review.'); },
      error: () => this.submitting.set(false),
    });
  }
}
