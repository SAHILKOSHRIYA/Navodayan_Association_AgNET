import { Component } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-auth-shell',
  imports: [RouterOutlet, RouterLink],
  template: `
    <div class="grid min-h-screen lg:grid-cols-2">
      <div class="hidden flex-col justify-between bg-primary-800 p-12 text-white lg:flex">
        <a routerLink="/" class="font-heading text-xl font-bold">Navodaya Alumni</a>
        <div>
          <h1 class="font-heading text-4xl font-bold leading-tight">Once a Navodayan,<br />always a Navodayan.</h1>
          <p class="mt-4 max-w-md text-white/80">
            Reconnect with your batch, mentor students, and support causes that matter — all in one place.
          </p>
        </div>
        <p class="text-sm text-white/60">JNV Raipur · Alumni Association</p>
      </div>
      <div class="flex items-center justify-center p-6">
        <div class="w-full max-w-md">
          <router-outlet />
        </div>
      </div>
    </div>
  `,
})
export class AuthShellComponent {}
