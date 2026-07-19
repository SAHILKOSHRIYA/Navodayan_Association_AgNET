import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { ToastService } from '../../core/toast.service';

@Component({
  selector: 'app-register',
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    @if (done()) {
      <div class="card p-8 text-center">
        <div class="mx-auto mb-3 grid h-12 w-12 place-items-center rounded-full bg-success/10 text-2xl">✉️</div>
        <h1 class="font-heading text-xl font-bold">Check your email</h1>
        <p class="mt-2 text-sm text-ink-600">
          We've sent a verification link to <strong>{{ form.value.email }}</strong>.
          Click it to activate your account, then sign in.
        </p>
        <a routerLink="/auth/login" class="btn-primary mt-5">Go to sign in</a>
      </div>
    } @else {
      <h1 class="font-heading text-2xl font-bold text-ink-900">Join as alumni</h1>
      <p class="mt-1 text-sm text-ink-600">Create your account to get started.</p>

      <form class="mt-6 space-y-4" [formGroup]="form" (ngSubmit)="submit()">
        <div>
          <label class="label">Full name</label>
          <input class="input" formControlName="fullName" autocomplete="name" />
        </div>
        <div>
          <label class="label">Email</label>
          <input class="input" type="email" formControlName="email" autocomplete="email" />
        </div>
        <div>
          <label class="label">Password</label>
          <input class="input" type="password" formControlName="password" autocomplete="new-password" />
          <p class="mt-1 text-xs text-ink-400">Min 8 characters, with upper, lower and a digit.</p>
        </div>
        <button class="btn-primary w-full" [disabled]="form.invalid || loading()">
          {{ loading() ? 'Creating…' : 'Create account' }}
        </button>
      </form>

      <p class="mt-6 text-center text-sm text-ink-600">
        Already registered? <a routerLink="/auth/login" class="font-medium text-primary-800">Sign in</a>
      </p>
    }
  `,
})
export class RegisterComponent {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private toast = inject(ToastService);
  loading = signal(false);
  done = signal(false);

  form = this.fb.nonNullable.group({
    fullName: ['', [Validators.required, Validators.maxLength(120)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8),
      Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$/)]],
  });

  submit() {
    if (this.form.invalid) return;
    this.loading.set(true);
    const { fullName, email, password } = this.form.getRawValue();
    this.auth.register(fullName, email, password).subscribe({
      next: () => this.done.set(true),
      error: () => this.loading.set(false),
    });
  }
}
