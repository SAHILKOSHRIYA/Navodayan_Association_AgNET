# NAU — Phase 2: System Design
### Navodaya Alumni Fundraising & Community Management Platform — Version 1

**Document status:** Draft for approval · **Date:** 2026-07-18
**Depends on:** [phase-1-product-discovery.md](phase-1-product-discovery.md) (approved)
**Next phase (blocked on approval):** Phase 3 — UI/UX Design

> **Living document.** Nothing here is cast in stone. Every consequential choice is in the
> Decision Log (§12) with its reasoning, so future scope changes are cheap and traceable.

---

## 1. Architecture Overview

Monorepo, two deployable applications plus infrastructure, all Docker-first and host-agnostic.

```
                        ┌──────────────────────── Internet ────────────────────────┐
                        │                                                          │
                 ┌──────▼──────┐                                          ┌────────▼────────┐
                 │   Browser   │                                          │ Razorpay servers│
                 └──────┬──────┘                                          └────────┬────────┘
                        │ HTTPS                                                    │ webhooks
        ┌───────────────▼───────────────────────────────────────────────────────── ▼──────────┐
        │                          NGINX (reverse proxy, TLS, gzip)                           │
        │        /  → Angular static build          /api → ASP.NET Core API                   │
        └───────────────┬───────────────────────────────────┬─────────────────────────────────┘
                        │                                   │
             ┌──────────▼──────────┐             ┌──────────▼───────────────────────┐
             │  Angular 20 SPA     │             │  NAU.Api (.NET 9, Kestrel)       │
             │  - public site      │  REST/JSON  │  Clean Architecture:             │
             │  - alumni portal    │────────────▶│   Api → Application (CQRS/       │
             │  - admin portal     │   JWT       │   MediatR) → Domain              │
             └─────────────────────┘             │   Infrastructure (EF Core, S3,   │
                                                 │   email, Razorpay, Identity)     │
                                                 └───┬──────────┬─────────┬─────────┘
                                                     │          │         │
                                           ┌─────────▼──┐  ┌────▼────┐  ┌─▼──────────────┐
                                           │ PostgreSQL │  │  Redis  │  │ S3-compatible  │
                                           │  (EF Core  │  │ (cache, │  │ object storage │
                                           │ migrations)│  │ optional│  │ (images, docs) │
                                           └────────────┘  │  at V1) │  └────────────────┘
                                                           └─────────┘
                              Email (SMTP/transactional provider)  ·  Serilog → files/console
```

**Key properties**
- **One API serves all three frontends** (public, alumni, admin) — RBAC separates capability, not separate backends. Microservices are *not* justified at this scale; the Clean Architecture module boundaries give us the seams to split later if a national rollout demands it.
- **Multi-school ready:** every domain table carries `school_id`; V1 seeds exactly one school (JNV Raipur).
- **Domain-agnostic:** all URLs, origins, keys come from environment config. Launch on a free HTTPS subdomain; buying a domain later = DNS + one env var.

---

## 2. Technology Decisions & Justification

| Area | Choice | Why |
|---|---|---|
| Frontend | Angular 20, standalone components, signals | Latest LTS line; enterprise structure; team skill target |
| UI | Angular Material + Tailwind | Material for a11y-complete components; Tailwind for layout/branding speed |
| Backend | ASP.NET Core (.NET 9) | LTS-adjacent, high performance, first-class DI/auth/health checks |
| Architecture | Clean Architecture + CQRS via MediatR | Testability, no logic in controllers, seams for future module extraction |
| Validation | FluentValidation (pipeline behavior) | Declarative, testable, runs before handlers |
| Mapping | AutoMapper (thin; manual mapping where clearer) | Reduce boilerplate without hiding logic |
| ORM | EF Core + Npgsql, code-first migrations | Versioned schema evolution; LINQ productivity |
| Database | PostgreSQL 16 | Free, enterprise-grade, `citext`, JSONB for privacy settings, full-text search for directory |
| Cache | Redis — **architected, optional at launch** | V1 traffic doesn't need it; interface (`ICacheService`) in place, defaults to in-memory, flips via config |
| Identity/Auth | ASP.NET Identity + JWT access (15 min) + rotating refresh tokens (30 d) | Battle-tested hashing/lockout/2FA-ready; stateless API auth |
| Storage | S3-compatible via `IFileStorage` | Works with AWS S3, Cloudflare R2, or self-hosted MinIO — host-agnostic |
| Payments | Razorpay Orders + server signature verify + webhooks as source of truth | Never trust the client callback; reconcile against provider |
| Logging | Serilog (console + rolling file), request logging, correlation IDs | Structured, greppable, OpenTelemetry-ready |
| CI/CD | GitHub Actions → build/test → Docker images → deploy over SSH (or PaaS hook) | Free for public/small repos; reproducible |
| Deploy | Docker Compose behind Nginx on Ubuntu | Smallest enterprise-sane footprint; portable to any host |

### 2.1 Hosting recommendation (no domain yet — your call to approve)

Build is identical either way (Docker Compose). Recommended path:

1. **Now (pilot/staging), ₹0:** **Render.com free tier** — deploy API container + Postgres + Angular static site; free `*.onrender.com` HTTPS subdomain; zero server admin. *Limitation:* free instances sleep when idle — fine for a pilot.
2. **Production launch (~₹400–800/mo):** one small VPS (Hetzner CX22 / DigitalOcean / AWS Lightsail), Ubuntu 24.04 + Docker Compose + Nginx + Let's Encrypt, S3/R2 for files, nightly Postgres dumps to storage. Point the purchased domain here when ready.

This gives a free start today and a clean, boring, cheap production story later — with no code changes between them.

---

## 3. Monorepo Structure

```
Navodayan-Uplift-Association/
├── docs/                         # phase docs, ADRs, runbook, API docs
│   └── adr/                      # Architecture Decision Records (one file per decision)
├── server/
│   ├── NAU.sln
│   ├── src/
│   │   ├── NAU.Domain/           # entities, enums, domain events, invariants — zero dependencies
│   │   ├── NAU.Application/      # CQRS handlers, DTOs, validators, interfaces (IFileStorage, IEmailSender, IPaymentGateway, ICacheService)
│   │   ├── NAU.Infrastructure/   # EF Core DbContext + migrations, Identity, Razorpay, S3, SMTP, Redis
│   │   └── NAU.Api/              # controllers (thin), auth setup, middleware, Swagger, Program.cs
│   └── tests/
│       ├── NAU.UnitTests/        # domain + application handler tests
│       └── NAU.IntegrationTests/ # API tests against Testcontainers PostgreSQL
├── client/
│   └── (Angular 20 workspace — see §4)
├── deploy/
│   ├── docker-compose.yml        # prod: nginx, api, client build, postgres, (redis)
│   ├── docker-compose.dev.yml    # local dev overrides
│   ├── nginx/nginx.conf
│   └── .env.example              # every env var documented, never committed with values
├── .github/workflows/            # ci.yml (build+test on PR), deploy.yml (on main)
└── README.md
```

**Dependency rule (enforced):** `Api → Application → Domain`; `Infrastructure → Application + Domain`. Domain references nothing. Controllers contain no business logic — they translate HTTP ⇄ MediatR.

---

## 4. Frontend Structure (Angular 20)

```
client/src/app/
├── core/
│   ├── auth/            # auth.service, token storage, authGuard, roleGuard, jwt.interceptor
│   ├── api/             # typed API clients per module (generated-friendly)
│   ├── models/          # shared TypeScript interfaces (mirror API DTOs)
│   └── interceptors/    # error/toast, loading, retry
├── shared/              # reusable UI: cards, tables, dialogs, form controls, pipes, layout shells
├── features/            # ALL lazy-loaded
│   ├── public/          # landing: home, about, committee, campaigns, events, stories, gallery, contact
│   ├── auth/            # login, register, verify-email, forgot/reset password
│   ├── alumni/          # dashboard, profile editor, directory, campaign detail + donate, events, announcements
│   └── admin/           # dashboard, verification queue, campaigns CRUD, donations, events, announcements, users, reports
├── app.routes.ts        # lazy routes + guards
└── app.config.ts        # providers, interceptors, environment wiring
```

- **Guards:** `authGuard` (valid session), `verifiedGuard` (verified alumni areas), `roleGuard(['AssociationAdmin','SuperAdmin'])` (admin).
- **State:** signals + services; NgRx deferred until complexity demands it (logged as ADR).
- **SEO:** public pages get proper titles/meta; prerendering (Angular SSR/prerender) for the landing site is a Phase 5 option, logged in the decision log.

---

## 5. Database Design (PostgreSQL)

Conventions: `snake_case`, UUID PKs (`gen_random_uuid()`), `created_at/updated_at timestamptz` everywhere, soft-delete (`deleted_at`) only where history matters (users, campaigns, donations never hard-deleted). Money as `numeric(12,2)` + `currency char(3)` (INR).

### 5.1 Entity-relationship summary

```
schools 1─* alumni_profiles *─1 users(Identity) 1─* refresh_tokens
users   1─* verification_requests *─1 users(reviewer)
schools 1─* campaigns 1─* campaign_updates
campaigns 1─* donations *─1 users(nullable, for guest donors)
donations 1─* payment_events(webhook log)
schools 1─* events 1─* event_rsvps *─1 users
events  1─* event_gallery_images
schools 1─* announcements
users   1─* audit_logs
alumni_profiles *─* skills (via alumni_skills)
```

### 5.2 Tables

**`schools`** — id, name, code (e.g. `JNV-RAIPUR`, unique), district, state, is_active. *(V1 seeds one row.)*

**Identity tables** — standard ASP.NET Identity (`users`, `roles`, `user_roles`, …) mapped to snake_case. `users` extended with: `full_name`, `school_id FK`, `status` (`Active|Suspended|Deleted`), `email_verified_at`. Roles seeded: `SuperAdmin`, `AssociationAdmin`, `Teacher`, `Alumni`, `Student`.

**`refresh_tokens`** — id, user_id FK, token_hash (never plaintext), expires_at, created_at, revoked_at, replaced_by_token_hash, created_ip. *(Rotation: each refresh issues a new token and revokes the old; reuse of a revoked token revokes the whole family.)*

**`alumni_profiles`** — id, user_id FK unique, school_id FK, batch (int, e.g. 27), house, roll_number?, dob?, mobile?, address?, current_city, current_country, company, designation, industry, education, bio, linkedin_url, github_url, photo_key (S3 key), privacy jsonb (per-group visibility: `{"contact":"private","professional":"members","academic":"public"}`), completion_pct (computed on save), is_verified bool, directory_visible bool.
*Indexes:* (school_id, batch), (current_city), (company), GIN full-text on (name ∥ company ∥ designation ∥ city) for directory search.

**`skills`** — id, name citext unique. **`alumni_skills`** — profile_id + skill_id (PK pair).

**`verification_requests`** — id, user_id FK, status (`Pending|Approved|Rejected`), submitted_at, reviewed_by FK?, reviewed_at?, rejection_reason?, admin_notes?. *History preserved: new request per submission, never overwritten.*

**`campaigns`** — id, school_id FK, title, slug unique, description (rich text), cover_image_key, goal_amount, start_date, end_date?, status (`Draft|Active|Paused|Completed|Closed`), organizer_name, created_by FK, deleted_at?.
`raised_amount` is **not stored** — it's `SUM(donations.amount) WHERE status='Captured'`, exposed via a view `campaign_totals` (cacheable). No hand-edited money.

**`campaign_updates`** — id, campaign_id FK, title, body, created_by, created_at.
**`campaign_documents`** — id, campaign_id FK, file_key, file_name, content_type, size_bytes.

**`donations`** — id, campaign_id FK, user_id FK? (null for guest), donor_name, donor_email, is_anonymous bool, amount, currency, status (`Created|Captured|Failed|Refunded`), razorpay_order_id unique, razorpay_payment_id?, razorpay_signature?, receipt_number? (sequential per school-year, e.g. `NAU27R/2026-27/000123`, assigned only on capture), failure_reason?, captured_at?, created_at.
*Indexes:* (campaign_id, status), (user_id), (razorpay_order_id).

**`payment_events`** — id, provider (`razorpay`), event_type, razorpay_event_id unique (idempotency), payload jsonb, donation_id FK?, processed bool, error?, received_at. *Every webhook stored before processing — replayable audit trail.*

**`events`** — id, school_id FK, title, description, event_date, end_date?, location, cover_image_key?, status (`Draft|Published|Completed|Cancelled`), created_by.
**`event_rsvps`** — id, event_id FK, user_id FK, status (`Going|Maybe|NotGoing`), unique(event_id, user_id).
**`event_gallery_images`** — id, event_id FK, file_key, caption?, uploaded_by.

**`announcements`** — id, school_id FK, title, body, category (`General|Academic|Events|Fundraising|Achievements`), audience (`Public|Members|Students`), published_at?, created_by.

**`audit_logs`** — id, actor_id FK?, action (e.g. `user.role_changed`, `verification.approved`, `campaign.created`), entity_type, entity_id, details jsonb (before/after), ip, created_at. *Append-only; written via MediatR pipeline behavior for all commands.*

**`success_stories`**, **`gallery_images`**, **`committee_members`** — small content tables backing the landing site (title/body/image_key/order/is_published), admin-managed.

### 5.3 Migration & seed plan
1. `InitialCreate` migration = full schema above.
2. Seed: JNV Raipur school row, 5 roles, one SuperAdmin (credentials via env vars on first boot).
3. **v0 migration script** (Phase 6): import Firebase members → `users` + unverified `alumni_profiles`; import Razorpay history → `donations` (status `Captured`, legacy flag in details) so ₹-raised history survives.

---

## 6. API Specification

**Base:** `/api/v1` · **Auth:** `Authorization: Bearer <jwt>` · **Envelope (all responses):**

```json
{ "success": true,  "data": { }, "message": null, "errors": null }
{ "success": false, "data": null, "message": "Validation failed",
  "errors": [ { "field": "email", "code": "EMAIL_TAKEN", "message": "…" } ] }
```

Paged lists: `data: { items: [], page, pageSize, totalCount }`. Errors use proper HTTP codes (400 validation, 401 unauthenticated, 403 forbidden, 404, 409 conflict, 422 domain rule, 429 rate-limited, 500). Full request/response schemas land in Swagger during Phase 4; this table is the contract.

### Auth — `/auth`
| Method | Route | Access | Purpose |
|---|---|---|---|
| POST | `/register` | Public | Create account, send verification email |
| POST | `/verify-email` | Public | Confirm email (token) |
| POST | `/resend-verification` | Public (rate-limited) | Resend token |
| POST | `/login` | Public | Issue access + refresh tokens |
| POST | `/refresh` | Public (refresh token) | Rotate tokens |
| POST | `/logout` | Auth | Revoke refresh token family |
| POST | `/forgot-password` · `/reset-password` | Public | Reset flow |
| GET  | `/me` | Auth | Current user + roles + verification state |

### Profiles — `/profiles`
| POST/PUT | `/me` | Alumni | Create/update own profile (drives completion %) |
| POST | `/me/photo` | Alumni | Upload photo (validated → S3) |
| GET | `/me` | Alumni | Own profile |
| GET | `/{id}` | Verified members | View profile (privacy-filtered) |

### Verification — `/verification`
| POST | `/requests` | Alumni | Submit verification request |
| GET | `/requests/me` | Alumni | Own request history/status |
| GET | `/requests?status=Pending` | Admin | Review queue (paged) |
| POST | `/requests/{id}/approve` · `/reject` | Admin | Decide (reject requires reason) |

### Directory — `/directory`
| GET | `/search?batch=&name=&company=&city=&country=&industry=&skill=&page=&sort=` | Verified members | Filtered, paged, privacy-aware search |

### Campaigns — `/campaigns`
| GET | `/` , `/{slug}` | Public | List/detail + totals + recent donors + updates |
| POST / PUT / PATCH status | `/`, `/{id}`, `/{id}/status` | Admin | CRUD + lifecycle |
| POST | `/{id}/updates` · `/{id}/documents` · `/{id}/cover` | Admin | Content management |

### Donations & Payments — `/donations`, `/webhooks`
| POST | `/donations/order` | Public/Auth | Validate → create Razorpay order (guest: name+email required) |
| POST | `/donations/verify` | Public | Verify checkout signature server-side → mark Captured → receipt |
| POST | `/webhooks/razorpay` | Razorpay (signature header) | Source of truth; idempotent by event id |
| GET | `/donations/me` | Auth | Own history + receipt links |
| GET | `/donations/{id}/receipt` | Owner/Admin | PDF receipt |
| GET | `/donations?campaign=&status=&from=&to=` | Admin | All donations, filters, paging |

### Events — `/events`
| GET | `/` (`?scope=upcoming|past`), `/{id}` | Public | Listing/detail |
| POST/PUT/PATCH | admin routes | Admin | CRUD + status |
| POST | `/{id}/rsvp` | Auth | RSVP (idempotent upsert) |
| GET | `/{id}/participants` | Admin | Participant list |
| POST | `/{id}/gallery` | Admin | Upload images |

### Announcements — `/announcements`
| GET | `/?category=&audience=` | Public/Auth (audience-filtered) | List/detail |
| POST/PUT/DELETE | admin routes | Admin | Manage |

### Admin — `/admin`
| GET | `/dashboard` | Admin | KPI cards + chart datasets (monthly donations, registration trend, campaign performance, verification status) |
| GET | `/users?query=&role=&status=` | Admin | Search users |
| PATCH | `/users/{id}/roles` · `/status` | Admin (role changes: SuperAdmin) | Assign roles, suspend/restore |
| DELETE | `/users/{id}` | SuperAdmin | Soft delete |
| GET | `/reports/{donations|campaigns|users|verifications}?format=csv|xlsx|pdf&from=&to=` | Admin | Exports |
| GET | `/audit-logs?actor=&entity=&from=&to=` | SuperAdmin | Audit trail |

### Content (landing) — `/content`
| GET | `/home` | Public | Aggregated: stats, latest campaigns, events, stories, gallery |
| CRUD | `/stories`, `/gallery`, `/committee` | Admin | Landing content |

### Ops
| GET | `/health` (liveness) · `/health/ready` (DB/storage checks) | Public/internal | Monitoring |

---

## 7. Security Architecture

- **AuthN:** ASP.NET Identity (PBKDF2 hashing, lockout after repeated failures). JWT access tokens 15 min, HS256 with ≥256-bit secret from env (RS256 if we ever need multi-service). Rotating refresh tokens, hashed at rest, family-revocation on reuse (§5.2).
- **AuthZ:** policy-based RBAC (`RequireRole`, resource checks in handlers — e.g. only owner or admin reads a receipt). Deny-by-default: `[Authorize]` globally, `[AllowAnonymous]` explicit.
- **Input:** FluentValidation on every command/query; EF Core parameterization (no raw SQL without parameters); rich text sanitized server-side (allow-list) before storage.
- **Payments:** order amounts validated server-side against request; signature verification with constant-time compare; webhook signature required; idempotency on `razorpay_event_id`; amounts never taken from the client at capture time.
- **Uploads:** allow-list content types (jpeg/png/webp, pdf for documents), size caps, content-sniffed not extension-trusted, stored under random keys, served via presigned URLs or proxied — never executable paths.
- **Transport/headers:** HTTPS-only (HSTS), CSP, X-Content-Type-Options, X-Frame-Options DENY, Referrer-Policy via Nginx; CORS locked to configured origins.
- **Rate limiting:** .NET rate limiter — tight on `/auth/*` and `/donations/order`; general per-IP ceiling.
- **Secrets:** environment variables / Docker secrets only; `.env.example` documents keys; repo history kept clean.
- **Audit:** append-only `audit_logs` via MediatR behavior for all state-changing commands; admin actions always attributable.
- **Privacy:** per-field-group visibility enforced in query handlers (single `ProfilePrivacyFilter` used by directory + profile detail — one code path, no leaks).

---

## 8. Caching, Logging, Health

- **`ICacheService`** abstraction: in-memory implementation at launch; Redis implementation behind a config flag. Cached (short TTL): campaign totals, landing `/content/home` aggregate, dashboard datasets. All writes invalidate by key prefix.
- **Serilog:** JSON console (container-friendly) + rolling file; request logging with correlation ID middleware; log levels via env.
- **Health checks:** `/health` (self), `/health/ready` (Postgres, storage reachability) — wired into Docker/monitoring.

---

## 9. Deployment Architecture & CI/CD

### Environments
| Env | Where | Data | Purpose |
|---|---|---|---|
| `local` | docker-compose.dev.yml | throwaway Postgres | development |
| `staging/pilot` | Render free tier (recommended) | small managed Postgres | pilot with real alumni |
| `production` | VPS + Docker Compose + Nginx + Let's Encrypt | backed-up Postgres | launch, after domain purchase |

### Pipelines (GitHub Actions)
- **`ci.yml`** (every PR/push): restore → build → unit tests → integration tests (Testcontainers Postgres) → Angular lint/test/build. Merges blocked on red.
- **`deploy.yml`** (push to `main`, after CI): build & push Docker images (GHCR) → deploy (Render deploy hook now; SSH `docker compose pull && up -d` on the VPS later) → run EF migrations (one-shot migrator container) → hit `/health/ready` → fail loudly if unhealthy.

### Operations
- **Backups:** nightly `pg_dump` to object storage, 30-day retention; restore procedure documented + tested in Phase 8.
- **Rollback:** images tagged by git SHA; rollback = redeploy previous tag (migrations are additive-first to keep old code compatible).
- **Monitoring (V1-appropriate):** UptimeRobot (free) on `/health/ready` + Serilog files; OpenTelemetry exporters are a config addition later.

---

## 10. What Phase 4/5 Will Build, In Order

Backend module order (each = migrations + handlers + validators + tests + Swagger):
**M0** skeleton/health/logging → **M1** Identity+JWT+refresh+RBAC → **M2** profiles+uploads → **M3** verification → **M4** directory → **M5** campaigns → **M6** donations+Razorpay+receipts → **M7** events → **M8** announcements+content → **M9** admin dashboard+users+reports+audit.

Frontend follows the same order (auth shell → profile → directory → campaigns/donate → events → announcements → admin), against the live API from M1 onward.

---

## 11. Testing Strategy (summary — full plan in Phase 7)

- **Unit:** domain rules + every command/query handler (happy + failure paths); validators.
- **Integration:** API-level against real Postgres (Testcontainers) — auth flows, RBAC denials, donation verify/webhook idempotency, privacy filtering.
- **E2E (Phase 7):** Playwright on the critical journeys: register→verify→profile→admin approve→directory; donate→receipt; admin campaign lifecycle.
- **Security checklist:** OWASP-top-10 pass before launch (Phase 7 gate).

---

## 12. Decision Log

| # | Decision | Why | Revisit when |
|---|---|---|---|
| D1 | Modular monolith, not microservices | 1 school, ≤ thousands of users; complexity unjustified | Multi-school national rollout |
| D2 | Redis optional at launch (in-memory behind `ICacheService`) | V1 load is small; keeps pilot free | Dashboard/directory latency degrades |
| D3 | `raised_amount` derived, never stored/edited | Financial integrity; single source of truth | Never (principle) |
| D4 | **APPROVED 2026-07-18:** deploy on Render free tier now; paid VPS kept as documented later option (post-domain) | ₹0 start, no code delta between hosts | Domain purchased / traffic grows |
| D5 | Signals + services; no NgRx yet | Simpler; NgRx adds ceremony without present need | Cross-feature state pain appears |
| D6 | Google Sign-In deferred (architecture OAuth-ready) | Email flow required anyway; cut V1 surface | V2, or trivial post-launch add |
| D7 | Guest donations allowed (name+email, no account) | Fundraising friction kills conversion | Association policy change |
| D8 | Receipt numbers sequential per school-year, on capture only | Audit-friendly, no gaps from failed payments | Registration/80G formalization |
| D9 | Soft delete for users/campaigns/donations | Long-term auditability (enterprise directive) | Never for financial records |
| D10 | UUID keys everywhere | Multi-school merges, no enumeration leaks | — |

---

## 13. Approval Gate

On your approval of this design (or with your amendments), next is **Phase 3 — UI/UX Design**:
user journeys, navigation map, wireframes for every screen (public / alumni / admin), the design
system (palette, typography, spacing, breakpoints), and the component inventory — then the final
pause before implementation begins in Phase 4.
