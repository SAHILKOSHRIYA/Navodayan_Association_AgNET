import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { ApiService } from '../../core/api.service';
import { DirectoryService } from '../../core/services';
import { DirectoryCard } from '../../core/models';

@Component({
  selector: 'app-directory',
  imports: [ReactiveFormsModule],
  template: `
    <h1 class="font-heading text-2xl font-bold">Alumni directory</h1>

    <form class="card mt-4 grid gap-3 p-4 sm:grid-cols-5" [formGroup]="filters" (ngSubmit)="search()">
      <input class="input sm:col-span-2" placeholder="Name" formControlName="name" />
      <input class="input" placeholder="Batch" type="number" formControlName="batch" />
      <input class="input" placeholder="Company" formControlName="company" />
      <input class="input" placeholder="City" formControlName="city" />
      <input class="input" placeholder="Skill" formControlName="skill" />
      <select class="input" formControlName="sort">
        <option value="name">Sort: Name</option><option value="batch">Sort: Batch</option><option value="city">Sort: City</option>
      </select>
      <button class="btn-primary sm:col-span-2">Search</button>
    </form>

    @if (results(); as list) {
      @if (list.length) {
        <div class="mt-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          @for (a of list; track a.profileId) {
            <div class="card p-5">
              <div class="flex items-center gap-3">
                <div class="grid h-12 w-12 place-items-center overflow-hidden rounded-full bg-primary-800/5 text-lg">
                  @if (api.fileUrl(a.photoKey); as url) { <img [src]="url" class="h-12 w-12 object-cover" alt="" /> } @else { 👤 }
                </div>
                <div>
                  <div class="font-heading font-semibold text-ink-900">{{ a.fullName }}</div>
                  @if (a.batch) { <div class="text-xs text-ink-400">Batch {{ a.batch }}</div> }
                </div>
              </div>
              @if (a.designation || a.company) {
                <div class="mt-3 text-sm text-ink-600">{{ a.designation }}{{ a.designation && a.company ? ' @ ' : '' }}{{ a.company }}</div>
              }
              @if (a.currentCity) { <div class="text-sm text-ink-400">📍 {{ a.currentCity }}</div> }
              @if (a.skills.length) {
                <div class="mt-3 flex flex-wrap gap-1">
                  @for (s of a.skills.slice(0, 4); track s) { <span class="chip bg-slate-100 text-ink-600">{{ s }}</span> }
                </div>
              }
            </div>
          }
        </div>
      } @else { <div class="card mt-6 p-10 text-center text-ink-600">No alumni match your search.</div> }
    } @else { <div class="py-16 text-center text-ink-600">Loading…</div> }
  `,
})
export class DirectoryComponent {
  private fb = inject(FormBuilder);
  private service = inject(DirectoryService);
  api = inject(ApiService);
  results = signal<DirectoryCard[] | null>(null);

  filters = this.fb.nonNullable.group({
    name: [''], batch: [''], company: [''], city: [''], skill: [''], sort: ['name'],
  });

  constructor() { this.search(); }

  search() {
    this.results.set(null);
    const v = this.filters.getRawValue();
    this.service.search({
      name: v.name || undefined, batch: v.batch || undefined, company: v.company || undefined,
      city: v.city || undefined, skill: v.skill || undefined, sort: v.sort, pageSize: 30,
    }).subscribe((r) => this.results.set(r.items));
  }
}
