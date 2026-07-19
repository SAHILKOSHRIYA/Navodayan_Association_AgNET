import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CampaignService } from '../../core/services';
import { CampaignCard } from '../../core/models';
import { ToastService } from '../../core/toast.service';
import { InrPipe } from '../../shared/inr.pipe';

@Component({
  selector: 'app-admin-campaigns',
  imports: [ReactiveFormsModule, InrPipe],
  template: `
    <div class="flex items-center justify-between">
      <h1 class="font-heading text-2xl font-bold">Campaigns</h1>
      <button class="btn-primary" (click)="showForm.set(!showForm())">{{ showForm() ? 'Close' : '+ New campaign' }}</button>
    </div>

    @if (showForm()) {
      <form class="card mt-4 grid gap-4 p-5 sm:grid-cols-2" [formGroup]="form" (ngSubmit)="create()">
        <div class="sm:col-span-2"><label class="label">Title*</label><input class="input" formControlName="title" /></div>
        <div class="sm:col-span-2"><label class="label">Description</label><textarea class="input" rows="3" formControlName="description"></textarea></div>
        <div><label class="label">Goal amount (₹)*</label><input class="input" type="number" formControlName="goalAmount" /></div>
        <div><label class="label">Organizer</label><input class="input" formControlName="organizerName" /></div>
        <div><label class="label">Start date*</label><input class="input" type="date" formControlName="startDate" /></div>
        <div><label class="label">End date</label><input class="input" type="date" formControlName="endDate" /></div>
        <div class="sm:col-span-2"><button class="btn-primary" [disabled]="form.invalid || busy()">Create as draft</button></div>
      </form>
    }

    @if (items(); as list) {
      <div class="card mt-6 overflow-x-auto">
        <table class="w-full text-sm">
          <thead class="border-b border-slate-100 text-left text-ink-400">
            <tr><th class="p-3">Campaign</th><th class="p-3">Raised / Goal</th><th class="p-3">Status</th><th class="p-3">Action</th></tr>
          </thead>
          <tbody>
            @for (c of list; track c.id) {
              <tr class="border-b border-slate-50">
                <td class="p-3 font-medium">{{ c.title }}</td>
                <td class="p-3 text-ink-600">{{ c.raisedAmount | inr }} / {{ c.goalAmount | inr }} ({{ c.progressPct }}%)</td>
                <td class="p-3"><span class="chip bg-slate-100">{{ statuses[c.status] }}</span></td>
                <td class="p-3">
                  @if (c.status === 0) { <button class="text-sm font-medium text-primary-800" (click)="activate(c)">Activate</button> }
                  @else if (c.status === 1) { <button class="text-sm font-medium text-ink-600" (click)="complete(c)">Mark complete</button> }
                </td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    } @else { <div class="py-16 text-center text-ink-600">Loading…</div> }
  `,
})
export class AdminCampaignsComponent {
  private fb = inject(FormBuilder);
  private service = inject(CampaignService);
  private toast = inject(ToastService);
  items = signal<CampaignCard[] | null>(null);
  showForm = signal(false);
  busy = signal(false);
  statuses = ['Draft', 'Active', 'Paused', 'Completed', 'Closed'];

  form = this.fb.nonNullable.group({
    title: ['', Validators.required],
    description: [''],
    goalAmount: [100000, [Validators.required, Validators.min(1)]],
    organizerName: [''],
    startDate: [new Date().toISOString().slice(0, 10), Validators.required],
    endDate: [''],
  });

  constructor() { this.load(); }
  load() { this.service.list(1, 50).subscribe((r) => this.items.set(r.items)); }

  create() {
    if (this.form.invalid) return;
    this.busy.set(true);
    const v = this.form.getRawValue();
    this.service.create({
      title: v.title, description: v.description, goalAmount: Number(v.goalAmount),
      startDate: v.startDate, endDate: v.endDate || undefined, organizerName: v.organizerName,
    }).subscribe({
      next: () => { this.toast.success('Campaign created.'); this.showForm.set(false); this.form.reset(); this.busy.set(false); this.load(); },
      error: () => this.busy.set(false),
    });
  }

  activate(c: CampaignCard) { this.service.setStatus(c.id, 1).subscribe(() => { this.toast.success('Activated.'); this.load(); }); }
  complete(c: CampaignCard) { this.service.setStatus(c.id, 3).subscribe(() => { this.toast.success('Completed.'); this.load(); }); }
}
