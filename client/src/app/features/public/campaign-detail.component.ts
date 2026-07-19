import { Component, inject, input, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth/auth.service';
import { CampaignService, DonationService } from '../../core/services';
import { CampaignDetail } from '../../core/models';
import { ToastService } from '../../core/toast.service';
import { InrPipe } from '../../shared/inr.pipe';

declare const Razorpay: any;

@Component({
  selector: 'app-campaign-detail',
  imports: [RouterLink, DatePipe, ReactiveFormsModule, InrPipe],
  template: `
    @if (campaign(); as c) {
      <div class="mx-auto max-w-4xl px-4 py-8">
        <a routerLink="/campaigns" class="text-sm text-primary-800">← All campaigns</a>

        <div class="mt-4 flex h-56 items-center justify-center overflow-hidden rounded-hero bg-primary-800/5 text-6xl">
          @if (api.fileUrl(c.coverImageKey); as url) { <img [src]="url" class="h-56 w-full object-cover" alt="" /> } @else { 🎯 }
        </div>

        <div class="mt-6 grid gap-6 md:grid-cols-3">
          <div class="md:col-span-2">
            <h1 class="font-heading text-3xl font-bold">{{ c.title }}</h1>
            @if (c.organizerName) { <p class="mt-1 text-sm text-ink-600">by {{ c.organizerName }}</p> }
            <p class="mt-4 whitespace-pre-line text-ink-600">{{ c.description }}</p>

            @if (c.updates.length) {
              <h2 class="mt-8 font-heading text-xl font-bold">Updates</h2>
              @for (u of c.updates; track u.id) {
                <div class="card mt-3 p-4">
                  <div class="font-medium">{{ u.title }}</div>
                  <div class="text-xs text-ink-400">{{ u.createdAt | date: 'd MMM y' }}</div>
                  <p class="mt-1 text-sm text-ink-600">{{ u.body }}</p>
                </div>
              }
            }
          </div>

          <div>
            <div class="card sticky top-20 p-5">
              <div class="h-2 overflow-hidden rounded-full bg-slate-100">
                <div class="h-full rounded-full bg-accent-500" [style.width.%]="c.progressPct"></div>
              </div>
              <div class="mt-3 font-heading text-2xl font-bold text-primary-800">{{ c.raisedAmount | inr }}</div>
              <div class="text-sm text-ink-600">raised of {{ c.goalAmount | inr }} · {{ c.progressPct }}%</div>
              <div class="mt-1 text-sm text-ink-600">{{ c.donorCount }} donor(s)</div>

              @if (c.status === 1) {
                <button class="btn-accent mt-4 w-full" (click)="showDonate.set(true)">Donate now</button>
              } @else {
                <div class="mt-4 rounded-lg bg-slate-100 p-3 text-center text-sm text-ink-600">Not accepting donations.</div>
              }

              @if (c.topDonors.length) {
                <div class="mt-5 text-sm font-semibold">Top donors</div>
                @for (d of c.topDonors; track d.name) {
                  <div class="mt-1 flex justify-between text-sm text-ink-600">
                    <span>{{ d.name }}</span><span>{{ d.amount | inr }}</span>
                  </div>
                }
              }
            </div>
          </div>
        </div>
      </div>

      <!-- Donate sheet -->
      @if (showDonate()) {
        <div class="fixed inset-0 z-50 flex items-end justify-center bg-black/40 sm:items-center" (click)="showDonate.set(false)">
          <div class="w-full max-w-md rounded-t-2xl bg-white p-6 sm:rounded-2xl" (click)="$event.stopPropagation()">
            <h3 class="font-heading text-lg font-bold">Donate to {{ c.title }}</h3>
            <form class="mt-4 space-y-3" [formGroup]="form" (ngSubmit)="donate(c)">
              <div class="flex flex-wrap gap-2">
                @for (a of presets; track a) {
                  <button type="button" class="chip border border-slate-300 px-3 py-1.5"
                          [class.bg-primary-800]="form.value.amount === a" [class.text-white]="form.value.amount === a"
                          (click)="form.patchValue({ amount: a })">{{ a | inr }}</button>
                }
              </div>
              <div>
                <label class="label">Amount (₹)</label>
                <input class="input" type="number" formControlName="amount" min="1" />
              </div>
              <div>
                <label class="label">Name</label>
                <input class="input" formControlName="donorName" />
              </div>
              <div>
                <label class="label">Email</label>
                <input class="input" type="email" formControlName="donorEmail" />
              </div>
              <label class="flex items-center gap-2 text-sm text-ink-600">
                <input type="checkbox" formControlName="isAnonymous" /> Donate anonymously
              </label>
              <button class="btn-accent w-full" [disabled]="form.invalid || processing()">
                {{ processing() ? 'Processing…' : 'Proceed to pay' }}
              </button>
              <p class="text-center text-xs text-ink-400">
                By donating you agree to the association's terms. Refunds only for mistaken payments.
              </p>
            </form>
          </div>
        </div>
      }
    } @else {
      <div class="py-20 text-center text-ink-600">Loading…</div>
    }
  `,
})
export class CampaignDetailComponent {
  slug = input.required<string>();
  private service = inject(CampaignService);
  private donations = inject(DonationService);
  private auth = inject(AuthService);
  private toast = inject(ToastService);
  private fb = inject(FormBuilder);
  api = inject(ApiService);

  campaign = signal<CampaignDetail | null>(null);
  showDonate = signal(false);
  processing = signal(false);
  presets = [500, 1000, 2500, 5000];

  form = this.fb.nonNullable.group({
    amount: [1000, [Validators.required, Validators.min(1)]],
    donorName: ['', Validators.required],
    donorEmail: ['', [Validators.required, Validators.email]],
    isAnonymous: [false],
  });

  constructor() {
    const user = this.auth.user();
    if (user) this.form.patchValue({ donorName: user.fullName, donorEmail: user.email });
    // input() is available after construction; load in an effect-free microtask.
    queueMicrotask(() => this.service.bySlug(this.slug()).subscribe((c) => this.campaign.set(c)));
  }

  donate(c: CampaignDetail) {
    if (this.form.invalid) return;
    this.processing.set(true);
    const { amount, donorName, donorEmail, isAnonymous } = this.form.getRawValue();

    this.donations.order(c.id, amount, donorName, donorEmail, isAnonymous).subscribe({
      next: (order) => this.openCheckout(order, c),
      error: () => this.processing.set(false),
    });
  }

  private openCheckout(order: { orderId: string; keyId: string; amountMinor: number; currency: string }, c: CampaignDetail) {
    if (typeof Razorpay === 'undefined') {
      this.processing.set(false);
      this.toast.info('Payment gateway not loaded yet. (Live Razorpay keys are configured at deployment.)');
      return;
    }
    const rzp = new Razorpay({
      key: order.keyId,
      order_id: order.orderId,
      amount: order.amountMinor,
      currency: order.currency,
      name: 'Navodaya Alumni Association',
      description: c.title,
      prefill: { name: this.form.value.donorName, email: this.form.value.donorEmail },
      theme: { color: '#122B54' },
      handler: (resp: any) => {
        this.donations.verify(resp.razorpay_order_id, resp.razorpay_payment_id, resp.razorpay_signature).subscribe({
          next: (receipt) => {
            this.processing.set(false);
            this.showDonate.set(false);
            this.toast.success(`Thank you! Receipt ${receipt.receiptNumber}.`);
            this.service.bySlug(this.slug()).subscribe((cc) => this.campaign.set(cc));
          },
          error: () => this.processing.set(false),
        });
      },
      modal: { ondismiss: () => this.processing.set(false) },
    });
    rzp.open();
  }
}
