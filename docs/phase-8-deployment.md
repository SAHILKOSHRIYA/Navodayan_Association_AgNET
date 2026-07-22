# NAU — Phase 8: Deployment Runbook

**Status:** ready to deploy · **Target (approved):** Render free tier now; single small VPS later.
**Architecture:** one Docker image serves the **API + Angular SPA on a single origin** (Kestrel serves
`wwwroot`, `/api/*` is the API, everything else falls back to `index.html`). No CORS or separate
frontend host to manage.

---

## 1. What you need before going live

| # | Item | Why | Status |
|---|------|-----|--------|
| 1 | GitHub repo (done) | Source + CI + Render blueprint | ✅ pushed |
| 2 | Render.com account (free) | Hosts the app + Postgres | ⛳ you create |
| 3 | Super-admin email + password | First login to the admin portal | ⛳ you choose |
| 4 | Razorpay account + keys | Real donations (KeyId, KeySecret, Webhook secret) | ⛳ when KYC done |
| 5 | Email provider (optional for pilot) | Real verification emails (SMTP/SES/Brevo) | ⛳ later |
| 6 | Domain (optional) | Custom URL — a DNS change, no code change | ⛳ later |

The pilot works **without #4–#6**: payments run in `test` mode and verification emails are logged
to the server console (so an admin can still verify people manually).

---

## 2. Deploy to Render (free tier) — step by step

1. Push to GitHub (already done): `git push origin main`.
2. In Render → **New + → Blueprint** → connect this repository. Render reads [`render.yaml`](../render.yaml)
   and proposes one web service (`nau-app`) + one Postgres (`nau-postgres`).
3. Render will prompt for the `sync: false` secrets — set:
   - `Seed__SuperAdmin__Email`, `Seed__SuperAdmin__Password`
   - `Client__BaseUrl` → leave blank for now; after the first deploy, set it to the service URL
     (e.g. `https://nau-app.onrender.com`) so email links point to the right place, then redeploy.
   - Payment keys can stay blank while `Payments__Provider=test`.
4. Click **Apply**. Render builds the Docker image, runs migrations on boot (`Database__MigrateOnStartup=true`),
   seeds the school + roles + super-admin, and serves the app.
5. Open the service URL → the landing page loads. Sign in at `/auth/login` with the super-admin
   credentials from step 3.

> Free-tier notes: the web service **sleeps when idle** (first request after a nap is slow) and the
> free Postgres **expires after ~90 days**. Both are fine for a pilot; upgrade when you launch for real.

---

## 3. Going fully live (payments + email + domain)

- **Razorpay:** finish KYC, then in Render set `Payments__Provider=razorpay` and the three
  `Payments__*` secrets. In the Razorpay dashboard add a webhook → `https://<your-url>/api/v1/webhooks/razorpay`
  using the same webhook secret.
- **Email:** implement an SMTP `IEmailSender` (the interface is ready; only `ConsoleEmailSender` exists
  today) and add the provider credentials as env vars. Until then, verification links appear in the logs.
- **Domain:** add it in Render, update DNS (CNAME), set `Client__BaseUrl` to the new URL, redeploy.

---

## 4. Later: single VPS (the paid option)

When traffic or the 90-day Postgres limit makes the free tier tight, move to a small VPS
(~₹400–800/mo):

```bash
cp deploy/.env.example deploy/.env      # fill in real values
docker compose -f deploy/docker-compose.yml --env-file deploy/.env up -d --build
```

Put Caddy/Nginx in front for HTTPS (Let's Encrypt). Same image, same env vars — nothing else changes.

---

## 5. Operations

- **Migrations** run automatically on boot; they are additive, so a rollback = redeploy the previous image.
- **Backups (VPS):** nightly `pg_dump` to object storage; test a restore before launch.
  On Render, enable managed backups (paid) or run a scheduled `pg_dump` job.
- **Health:** `GET /health` (liveness) and `GET /health/ready` (checks Postgres) — used by Render's health check.
- **Logs:** structured (Serilog) to stdout — visible in the Render dashboard.
- **Rollback:** Render keeps previous deploys — one click to redeploy an older one.

---

## 6. First-launch checklist

- [ ] Blueprint applied; service is green on `/health`.
- [ ] Signed in as super-admin; **changed the seeded password**.
- [ ] `Client__BaseUrl` set to the real URL and redeployed.
- [ ] Created one real campaign and activated it.
- [ ] (If live) Razorpay keys set, webhook registered, a ₹1 test donation captured end-to-end.
- [ ] Invited a few batchmates to register → verified them from the admin queue.
