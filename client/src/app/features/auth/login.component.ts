import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { ToastService } from '../../core/toast.service';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <h1 class="font-heading text-2xl font-bold text-ink-900">Welcome back</h1>
    <p class="mt-1 text-sm text-ink-600">Sign in to your alumni account.</p>

    <form class="mt-6 space-y-4" [formGroup]="form" (ngSubmit)="submit()">
      <div>
        <label class="label">Email</label>
        <input class="input" type="email" formControlName="email" autocomplete="email" />
      </div>
      <div>
        <div class="flex items-center justify-between">
          <label class="label">Password</label>
        </div>
        <input class="input" type="password" formControlName="password" autocomplete="current-password" />
      </div>
      <button class="btn-primary w-full" [disabled]="form.invalid || loading()">
        {{ loading() ? 'Signing in…' : 'Sign in' }}
      </button>
    </form>

    <p class="mt-6 text-center text-sm text-ink-600">
      New here? <a routerLink="/auth/register" class="font-medium text-primary-800">Create an account</a>
    </p>
  `,
})
export class LoginComponent {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private router = inject(Router);
  private toast = inject(ToastService);
  loading = signal(false);

  form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });

  submit() {
    if (this.form.invalid) return;
    this.loading.set(true);
    const { email, password } = this.form.getRawValue();
    this.auth.login(email, password).subscribe({
      next: () => {
        this.toast.success('Signed in.');
        this.router.navigate([this.auth.isAdmin() ? '/admin' : '/app']);
      },
      error: (err) => {
        this.loading.set(false);
        this.toast.error(err?.error?.message ?? 'Invalid email or password.');
      },
    });
  }
}
