# NAU — Navodaya Alumni Fundraising & Community Management Platform

The official digital home for Jawahar Navodaya Vidyalaya alumni — a **verified alumni directory** and
a **transparent fundraising platform**, with events, announcements, and a full admin back-office.
Piloting with JNV Raipur; architected to scale to a nationwide Navodaya alumni network.

Built as a production-grade product: Clean Architecture, tests, CI, migrations, audit logging, and a
one-command Docker deploy.

---

## What it does

- **Public site** — landing page with live stats, campaigns, events, news, and a donate flow.
- **Alumni** — register → verify email → build a profile (with per-section privacy) → get verified by an
  admin → appear in a searchable directory → donate and download receipts.
- **Admins** — dashboard (KPIs + charts), verification queue, campaign management, donation records +
  CSV export, and user management — no developer needed for day-to-day operations.

## Tech stack

| Layer | Technology |
|-------|------------|
| Frontend | **Angular 20** (standalone components, signals, lazy routes) + Tailwind CSS |
| Backend | **ASP.NET Core (.NET 9)** — Clean Architecture, CQRS via MediatR, FluentValidation |
| Data | **PostgreSQL** + EF Core (code-first migrations) |
| Auth | ASP.NET Identity + **JWT** with rotating refresh tokens, role-based access |
| Payments | **Razorpay** (behind a swappable gateway; a test gateway ships for local/dev) |
| Ops | Docker · GitHub Actions CI · Serilog · health checks |

## Repository layout

| Path | Contents |
|------|----------|
| `docs/` | Phase documents (discovery, system design, UI/UX, **deployment runbook**) + decision logs |
| `server/` | .NET 9 solution — `NAU.Domain` / `NAU.Application` / `NAU.Infrastructure` / `NAU.Api` + tests |
| `client/` | Angular 20 workspace (public site, alumni portal, admin portal) |
| `deploy/` | Docker Compose (dev + prod) and the environment template |
| `Dockerfile` | Single image: builds the SPA, builds the API, serves both on one origin |
| `render.yaml` | One-click Render deployment blueprint |

## Run it locally

```bash
# 1. Start PostgreSQL
docker compose -f deploy/docker-compose.dev.yml up -d

# 2. API  →  http://localhost:5080  (Swagger at /swagger)
cd server && dotnet run --project src/NAU.Api

# 3. Client  →  http://localhost:4200  (proxies /api to the API)
cd client && npm install && npm start

# Tests
cd server && dotnet test
```

Seeded super-admin (dev): `admin@nau.local` / `Admin@12345`.

## Run the whole thing in one container

```bash
docker build -t nau-app .
# then run it with a Postgres connection string and a JWT secret (see deploy/.env.example)
```

## Deploy

See **[docs/phase-8-deployment.md](docs/phase-8-deployment.md)** — a step-by-step runbook for the
free-tier Render deployment (and the VPS option for later).

## Documentation

Start with [docs/phase-1-product-discovery.md](docs/phase-1-product-discovery.md), then
[docs/phase-2-system-design.md](docs/phase-2-system-design.md) (architecture, schema, API contract)
and [docs/phase-3-uiux-design.md](docs/phase-3-uiux-design.md).
