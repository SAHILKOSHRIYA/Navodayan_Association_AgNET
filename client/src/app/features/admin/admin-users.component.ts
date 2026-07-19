import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../core/services';
import { AuthService } from '../../core/auth/auth.service';
import { UserAdmin } from '../../core/models';
import { ToastService } from '../../core/toast.service';

@Component({
  selector: 'app-admin-users',
  imports: [FormsModule],
  template: `
    <h1 class="font-heading text-2xl font-bold">Users</h1>

    <div class="mt-4 flex gap-2">
      <input class="input max-w-xs" placeholder="Search name or email" [(ngModel)]="query" (keyup.enter)="load()" />
      <button class="btn-primary" (click)="load()">Search</button>
    </div>

    @if (items(); as list) {
      <div class="card mt-6 overflow-x-auto">
        <table class="w-full text-sm">
          <thead class="border-b border-slate-100 text-left text-ink-400">
            <tr><th class="p-3">Name</th><th class="p-3">Email</th><th class="p-3">Roles</th><th class="p-3">Status</th><th class="p-3">Actions</th></tr>
          </thead>
          <tbody>
            @for (u of list; track u.id) {
              <tr class="border-b border-slate-50">
                <td class="p-3 font-medium">{{ u.fullName }}</td>
                <td class="p-3 text-ink-600">{{ u.email }}</td>
                <td class="p-3">
                  @for (r of u.roles; track r) { <span class="chip mr-1 bg-primary-800/10 text-primary-800">{{ r }}</span> }
                </td>
                <td class="p-3"><span class="chip" [class.bg-success]="u.status===0" [class.text-white]="u.status===0" [class.bg-danger]="u.status===1" [class.text-white]="u.status===1">{{ statuses[u.status] }}</span></td>
                <td class="p-3">
                  @if (auth.hasRole('SuperAdmin')) {
                    @if (u.roles.includes('Teacher')) {
                      <button class="text-sm text-ink-600" (click)="setRoles(u, ['Alumni'])">Make alumni</button>
                    } @else {
                      <button class="text-sm text-primary-800" (click)="setRoles(u, ['Alumni','Teacher'])">Make teacher</button>
                    }
                  }
                  @if (u.status === 0) { <button class="ml-3 text-sm text-danger" (click)="setStatus(u, 1)">Suspend</button> }
                  @else if (u.status === 1) { <button class="ml-3 text-sm text-success" (click)="setStatus(u, 0)">Reactivate</button> }
                </td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    } @else { <div class="py-16 text-center text-ink-600">Loading…</div> }
  `,
})
export class AdminUsersComponent {
  private service = inject(AdminService);
  private toast = inject(ToastService);
  auth = inject(AuthService);
  items = signal<UserAdmin[] | null>(null);
  query = '';
  statuses = ['Active', 'Suspended', 'Deleted'];

  constructor() { this.load(); }
  load() { this.items.set(null); this.service.users(this.query || undefined, undefined, 1, 50).subscribe((r) => this.items.set(r.items)); }

  setRoles(u: UserAdmin, roles: string[]) {
    this.service.setRoles(u.id, roles).subscribe(() => { this.toast.success('Roles updated.'); this.load(); });
  }
  setStatus(u: UserAdmin, status: number) {
    this.service.setStatus(u.id, status).subscribe(() => { this.toast.success('Status updated.'); this.load(); });
  }
}
