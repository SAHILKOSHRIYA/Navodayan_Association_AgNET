# NAU-27R — Version 1 Plan
### Alumni Fundraising & Community Management Platform

**Client:** Navodaya's Uplift Association, 27th Batch (JNV Raipur)
**Batch:** 27R (2012–2019) · ~80+ alumni members
**Currency:** INR (₹)
**Date:** 2026-07-18

---

## 1. Purpose of Version 1

Version 1 is a **full re-architecture** of the existing prototype into an enterprise-grade,
role-based full-stack application. The current live site (React + Firebase + Node/Express +
Razorpay on GitHub Pages) proved the concept — donations, member gating, transaction lists.
V1 rebuilds it on a scalable, maintainable stack and expands it into a complete community
management platform.

### Migration note
The v0 data must be carried over:
- **Members** currently live in Firebase → migrate to the `Members` table in SQL Server.
- **Transactions** currently pulled from Firebase/Razorpay → import historical records into the
  `Donations` / `Transactions` tables so history and the ₹1,00,000+ raised total is preserved.

---

## 2. Target Tech Stack

| Layer            | Technology                                                        |
|------------------|-------------------------------------------------------------------|
| Frontend         | **Angular 17** (standalone components, lazy loading), Bootstrap 5 |
| Backend          | **ASP.NET Core Web API (.NET 8)**, C#                             |
| ORM / Data       | **Entity Framework Core** (Code-First + Migrations)               |
| Database         | **SQL Server**                                                    |
| Auth             | **JWT** (access + refresh tokens), role-based authorization       |
| Payments         | **Razorpay** (one-time Orders + Subscriptions for recurring)      |
| Charts/Reporting | ng2-charts / Chart.js                                             |
| Hosting          | **IIS** (Windows) — API + Angular static build                    |
| Source control   | Git / GitHub, Agile workflow                                      |

---

## 3. High-Level Architecture

```
┌────────────────────────────┐        HTTPS/JSON        ┌──────────────────────────────┐
│   Angular 17 SPA           │  ───────────────────────▶ │  ASP.NET Core Web API (.NET 8)│
│  - Member portal           │   JWT in Authorization    │  - Controllers                │
│  - Admin portal            │        header             │  - Application/Service layer  │
│  - Auth/route guards       │ ◀─────────────────────── │  - EF Core (repositories)     │
│  - HTTP interceptors       │                           │  - JWT + role authorization   │
└────────────────────────────┘                           └───────────────┬──────────────┘
                                                                          │
                                              ┌───────────────────────────┼───────────────┐
                                              │                           │               │
                                        ┌─────▼─────┐              ┌───────▼──────┐  ┌─────▼──────┐
                                        │ SQL Server│              │  Razorpay    │  │ Email (SMTP│
                                        │  (EF Core)│              │  API/Webhooks│  │  /SendGrid)│
                                        └───────────┘              └──────────────┘  └────────────┘
```

**Backend layering (Clean-ish / N-tier):**
- `NAU27R.Api` — controllers, middleware, DI, Program.cs, Swagger
- `NAU27R.Application` — services, DTOs, validation, business logic
- `NAU27R.Domain` — entities, enums, domain rules
- `NAU27R.Infrastructure` — EF Core `DbContext`, migrations, repositories, external integrations (Razorpay, email)

---

## 4. Data Model (initial SQL Server schema)

| Entity                  | Key fields                                                                                          |
|-------------------------|-----------------------------------------------------------------------------------------------------|
| **Member**              | Id, FullName, Email(unique), PhoneNumber, PasswordHash, Role, IsEmailVerified, IsApproved, JnvLocation, Batch, ProfileImageUrl, CreatedAt |
| **RefreshToken**        | Id, MemberId, Token, ExpiresAt, IsRevoked, CreatedAt                                                 |
| **Campaign**            | Id, Title, Description, TargetAmount, RaisedAmount, StartDate, EndDate, Status(Active/Closed), CreatedByMemberId |
| **Donation**            | Id, MemberId, CampaignId(nullable), Amount, Currency, PaymentStatus, RazorpayOrderId, RazorpayPaymentId, IsRecurring, CreatedAt |
| **RecurringSubscription** | Id, MemberId, Amount, Frequency, Status, RazorpaySubscriptionId, NextChargeDate, CreatedAt         |
| **Transaction/Ledger**  | Id, DonationId, Type(Credit/Refund), Amount, Notes, CreatedAt (source of truth for reporting)        |
| **Event**               | Id, Title, Description, EventDate, Location, CreatedByMemberId, CreatedAt                             |
| **EventRsvp**           | Id, EventId, MemberId, Status(Going/Maybe/No)                                                        |
| **AuditLog**            | Id, MemberId, Action, Entity, Details, CreatedAt                                                     |

Enums: `Role { Admin, Member }`, `PaymentStatus { Pending, Success, Failed, Refunded }`,
`Frequency { Monthly, Quarterly, Yearly }`.

---

## 5. API Surface (v1)

All responses use a consistent envelope: `{ success, data, message, errors }`.
All endpoints (except auth + public policy) require a valid JWT; admin routes require `Admin` role.

**Auth**
- `POST /api/auth/register` — register, send verification email
- `GET  /api/auth/verify-email` — confirm email
- `POST /api/auth/login` — returns access + refresh token
- `POST /api/auth/refresh` — rotate tokens
- `POST /api/auth/logout` — revoke refresh token

**Members**
- `GET  /api/members/me` — own profile
- `PUT  /api/members/me` — update profile
- `GET  /api/members` *(Admin)* — list all
- `PUT  /api/members/{id}/approve` *(Admin)* — approve membership
- `PUT  /api/members/{id}/role` *(Admin)* — change role

**Campaigns**
- `GET  /api/campaigns` · `GET /api/campaigns/{id}`
- `POST /api/campaigns` *(Admin)* · `PUT /api/campaigns/{id}` *(Admin)* · `PUT /api/campaigns/{id}/close` *(Admin)*

**Donations**
- `POST /api/donations/order` — create Razorpay order
- `POST /api/donations/verify` — verify signature, record donation
- `GET  /api/donations/me` — own donation history
- `GET  /api/donations` *(Admin)* — all donations, filters

**Recurring**
- `POST /api/subscriptions` — create recurring donation
- `POST /api/subscriptions/{id}/cancel`
- `POST /api/webhooks/razorpay` — subscription/payment webhooks

**Events**
- `GET /api/events` · `POST /api/events` *(Admin)* · `POST /api/events/{id}/rsvp`

**Reporting** *(Admin)*
- `GET /api/reports/summary` — totals, by-campaign, by-month
- `GET /api/reports/export` — CSV/Excel

---

## 6. Frontend Structure (Angular 17)

```
src/app/
├── core/            (guards, interceptors, auth service, models, http services)
├── shared/          (reusable components, pipes, directives, layout)
├── features/
│   ├── auth/        (login, register, verify)
│   ├── member/      (dashboard, profile, donate, my-donations, events)
│   └── admin/       (dashboard, members, campaigns, events, reports)
└── app.routes.ts    (lazy-loaded feature routes)
```

- **Guards:** `authGuard` (logged in + verified + approved), `roleGuard` (Admin).
- **Interceptors:** attach JWT, refresh-on-401, global error/toast handling.
- **Reactive forms** with validation across all inputs.
- **Dashboards:** campaign progress bars, donation charts, financial summary (ng2-charts).
- **Responsive** via Bootstrap 5.

---

## 7. Delivery Phases (Version 1)

> Each phase is independently testable and ends in a deployable increment.

**Phase 0 — Foundations**
- Solution + project scaffolding (API layers, Angular app), Git repo, `.gitignore`, README.
- SQL Server + EF Core `DbContext`, initial migration, connection strings.
- Swagger, global exception middleware, CORS, response envelope, logging.

**Phase 1 — Auth & Membership**
- Member entity, JWT issuance + refresh, password hashing.
- Register → email verification → **admin approval gate** (mirrors current member-gating).
- Role-based authorization; Angular login/register/verify + auth guard + interceptor.

**Phase 2 — Donations (core value)**
- Razorpay order create + signature verify + record donation.
- Member donate page + own donation history.
- Migrate historical transactions from v0.

**Phase 3 — Campaigns**
- Admin CRUD for campaigns; public list + progress dashboards; link donations to campaigns.

**Phase 4 — Recurring Donations**
- Razorpay Subscriptions integration + webhooks; member manage/cancel.

**Phase 5 — Events**
- Admin create events; member list + RSVP.

**Phase 6 — Admin Dashboard & Reporting**
- Member management (approve/roles), financial summary, charts, CSV/Excel export, audit log.

**Phase 7 — Hardening & Deployment**
- Security review (JWT expiry, HTTPS, input validation, rate limiting, secrets in config/env).
- Seed admin account, performance/query tuning, IIS deployment (API + Angular build), smoke tests.

---

## 8. Open Decisions (need your input before/while building)

1. **Email provider** for verification/notifications — SMTP (Gmail) vs SendGrid vs other?
2. **Recurring donations** — confirm Razorpay Subscriptions is available on the account (needs KYC/plan).
3. **Hosting reality** — dedicated Windows/IIS server or VPS? (Affects deployment + HTTPS/domain setup.)
4. **Approval flow** — auto-approve verified emails, or manual admin approval like today?
5. **Legal disclaimer** — carry over the "not a registered association" refund/dispute disclaimer into V1 T&C.
6. **Data migration** — do we have export access to the current Firebase members + Razorpay transactions?

---

## 9. Suggested First Step

Scaffold **Phase 0** as a monorepo:

```
/Navodayan-Uplift-Association
├── /server   → .NET 8 solution (NAU27R.Api / .Application / .Domain / .Infrastructure)
├── /client   → Angular 17 workspace
└── /docs      → this plan, schema, API docs
```

Once you approve the plan (and answer the Section 8 decisions), I can generate the Phase 0
scaffolding and the initial EF Core schema + migration.
