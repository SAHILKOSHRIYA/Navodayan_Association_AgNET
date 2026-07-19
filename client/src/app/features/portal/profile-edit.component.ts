import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiService } from '../../core/api.service';
import { ProfileService } from '../../core/services';
import { Profile } from '../../core/models';
import { ToastService } from '../../core/toast.service';

@Component({
  selector: 'app-profile-edit',
  imports: [ReactiveFormsModule],
  template: `
    <div class="flex items-center justify-between">
      <h1 class="font-heading text-2xl font-bold">My profile</h1>
      @if (profile(); as p) { <span class="text-sm text-ink-600">{{ p.completionPct }}% complete</span> }
    </div>

    <form class="mt-6 space-y-6" [formGroup]="form" (ngSubmit)="save()">
      <!-- Photo -->
      <div class="card flex items-center gap-4 p-5">
        <div class="grid h-16 w-16 place-items-center overflow-hidden rounded-full bg-primary-800/5 text-2xl">
          @if (photoUrl(); as url) { <img [src]="url" class="h-16 w-16 object-cover" alt="" /> } @else { 👤 }
        </div>
        <div>
          <label class="btn-ghost cursor-pointer">
            Upload photo
            <input type="file" class="hidden" accept="image/*" (change)="onPhoto($event)" />
          </label>
          <p class="mt-1 text-xs text-ink-400">JPEG/PNG/WebP, up to 5 MB.</p>
        </div>
      </div>

      <div class="card p-5">
        <h2 class="mb-3 font-heading font-semibold">Academic</h2>
        <div class="grid gap-4 sm:grid-cols-3">
          <div><label class="label">Batch (passing year)*</label><input class="input" type="number" formControlName="batch" /></div>
          <div><label class="label">House</label><input class="input" formControlName="house" /></div>
          <div><label class="label">Roll number</label><input class="input" formControlName="rollNumber" /></div>
        </div>
      </div>

      <div class="card p-5">
        <h2 class="mb-3 font-heading font-semibold">Professional</h2>
        <div class="grid gap-4 sm:grid-cols-2">
          <div><label class="label">Company</label><input class="input" formControlName="company" /></div>
          <div><label class="label">Designation</label><input class="input" formControlName="designation" /></div>
          <div><label class="label">Industry</label><input class="input" formControlName="industry" /></div>
          <div><label class="label">Education</label><input class="input" formControlName="education" /></div>
          <div><label class="label">Current city</label><input class="input" formControlName="currentCity" /></div>
          <div><label class="label">Current country</label><input class="input" formControlName="currentCountry" /></div>
          <div><label class="label">LinkedIn URL</label><input class="input" formControlName="linkedInUrl" /></div>
          <div><label class="label">GitHub URL</label><input class="input" formControlName="gitHubUrl" /></div>
        </div>
        <div class="mt-4"><label class="label">Skills (comma separated)</label><input class="input" formControlName="skillsText" placeholder="C#, Angular, Product" /></div>
        <div class="mt-4"><label class="label">Bio</label><textarea class="input" rows="3" formControlName="bio"></textarea></div>
      </div>

      <div class="card p-5">
        <h2 class="mb-3 font-heading font-semibold">Contact & privacy</h2>
        <div class="grid gap-4 sm:grid-cols-2">
          <div><label class="label">Mobile</label><input class="input" formControlName="mobile" /></div>
          <div><label class="label">Address</label><input class="input" formControlName="address" /></div>
        </div>
        <div class="mt-4 grid gap-4 sm:grid-cols-3">
          <div>
            <label class="label">Contact visibility</label>
            <select class="input" formControlName="privContact">
              <option [value]="0">Public</option><option [value]="1">Members</option><option [value]="2">Private</option>
            </select>
          </div>
          <div>
            <label class="label">Professional visibility</label>
            <select class="input" formControlName="privProfessional">
              <option [value]="0">Public</option><option [value]="1">Members</option><option [value]="2">Private</option>
            </select>
          </div>
          <div>
            <label class="label">Academic visibility</label>
            <select class="input" formControlName="privAcademic">
              <option [value]="0">Public</option><option [value]="1">Members</option><option [value]="2">Private</option>
            </select>
          </div>
        </div>
        <label class="mt-4 flex items-center gap-2 text-sm text-ink-600">
          <input type="checkbox" formControlName="directoryVisible" /> Show me in the alumni directory
        </label>
      </div>

      <button class="btn-primary" [disabled]="form.invalid || saving()">{{ saving() ? 'Saving…' : 'Save profile' }}</button>
    </form>
  `,
})
export class ProfileEditComponent {
  private fb = inject(FormBuilder);
  private service = inject(ProfileService);
  private toast = inject(ToastService);
  api = inject(ApiService);

  profile = signal<Profile | null>(null);
  saving = signal(false);
  photoUrl = signal<string | null>(null);

  form = this.fb.nonNullable.group({
    batch: [new Date().getFullYear() - 7, [Validators.required, Validators.min(1990)]],
    house: [''], rollNumber: [''],
    company: [''], designation: [''], industry: [''], education: [''],
    currentCity: [''], currentCountry: [''], linkedInUrl: [''], gitHubUrl: [''],
    skillsText: [''], bio: [''], mobile: [''], address: [''],
    privContact: [1], privProfessional: [1], privAcademic: [0],
    directoryVisible: [true],
  });

  constructor() {
    this.service.mine().subscribe((p) => {
      if (!p) return;
      this.profile.set(p);
      this.photoUrl.set(this.api.fileUrl(p.photoKey));
      this.form.patchValue({
        batch: p.batch, house: p.house ?? '', rollNumber: p.rollNumber ?? '',
        company: p.company ?? '', designation: p.designation ?? '', industry: p.industry ?? '',
        education: p.education ?? '', currentCity: p.currentCity ?? '', currentCountry: p.currentCountry ?? '',
        linkedInUrl: p.linkedInUrl ?? '', gitHubUrl: p.gitHubUrl ?? '', skillsText: p.skills.join(', '),
        bio: p.bio ?? '', mobile: p.mobile ?? '', address: p.address ?? '',
        privContact: p.privacy.contact, privProfessional: p.privacy.professional, privAcademic: p.privacy.academic,
        directoryVisible: p.directoryVisible,
      });
    });
  }

  save() {
    if (this.form.invalid) return;
    this.saving.set(true);
    const v = this.form.getRawValue();
    this.service.upsert({
      batch: Number(v.batch), house: v.house, rollNumber: v.rollNumber,
      company: v.company, designation: v.designation, industry: v.industry, education: v.education,
      currentCity: v.currentCity, currentCountry: v.currentCountry, linkedInUrl: v.linkedInUrl, gitHubUrl: v.gitHubUrl,
      bio: v.bio, mobile: v.mobile, address: v.address,
      skills: v.skillsText.split(',').map((s) => s.trim()).filter(Boolean),
      privacy: { contact: +v.privContact as any, professional: +v.privProfessional as any, academic: +v.privAcademic as any },
      directoryVisible: v.directoryVisible,
    }).subscribe({
      next: (p) => { this.profile.set(p); this.saving.set(false); this.toast.success('Profile saved.'); },
      error: () => this.saving.set(false),
    });
  }

  onPhoto(ev: Event) {
    const file = (ev.target as HTMLInputElement).files?.[0];
    if (!file) return;
    this.service.uploadPhoto(file).subscribe((r) => {
      this.photoUrl.set(this.api.fileUrl(r.photoKey));
      this.toast.success('Photo updated.');
    });
  }
}
