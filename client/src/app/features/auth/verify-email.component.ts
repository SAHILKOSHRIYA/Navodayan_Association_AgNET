import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-verify-email',
  imports: [RouterLink],
  template: `
    <div class="card p-8 text-center">
      @switch (state()) {
        @case ('verifying') {
          <p class="text-ink-600">Verifying your email…</p>
        }
        @case ('ok') {
          <div class="mx-auto mb-3 grid h-12 w-12 place-items-center rounded-full bg-success/10 text-2xl">✅</div>
          <h1 class="font-heading text-xl font-bold">Email verified</h1>
          <p class="mt-2 text-sm text-ink-600">Your account is active. You can now sign in.</p>
          <a routerLink="/auth/login" class="btn-primary mt-5">Sign in</a>
        }
        @case ('error') {
          <div class="mx-auto mb-3 grid h-12 w-12 place-items-center rounded-full bg-danger/10 text-2xl">⚠️</div>
          <h1 class="font-heading text-xl font-bold">Link invalid or expired</h1>
          <p class="mt-2 text-sm text-ink-600">Please sign in and request a new verification email.</p>
          <a routerLink="/auth/login" class="btn-primary mt-5">Go to sign in</a>
        }
      }
    </div>
  `,
})
export class VerifyEmailComponent {
  private route = inject(ActivatedRoute);
  private auth = inject(AuthService);
  state = signal<'verifying' | 'ok' | 'error'>('verifying');

  constructor() {
    const q = this.route.snapshot.queryParamMap;
    const email = q.get('email');
    const token = q.get('token');
    if (!email || !token) { this.state.set('error'); return; }
    this.auth.verifyEmail(email, token).subscribe({
      next: () => this.state.set('ok'),
      error: () => this.state.set('error'),
    });
  }
}
