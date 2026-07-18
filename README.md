# NAU — Navodaya Alumni Fundraising & Community Management Platform

Digital ecosystem for Jawahar Navodaya Vidyalaya alumni — verified alumni directory,
transparent fundraising, events, and announcements. Piloting with JNV Raipur,
architected to scale to a nationwide Navodaya Alumni Network.

## Repository layout

| Path | Contents |
|---|---|
| `docs/` | Phase documents: product discovery, system design, UI/UX, ADRs |
| `server/` | ASP.NET Core (.NET 9) Web API — Clean Architecture (Domain / Application / Infrastructure / Api) |
| `client/` | Angular 20 workspace (public site, alumni portal, admin portal) |
| `deploy/` | Docker Compose, Nginx config, environment templates |

## Quick start (development)

```bash
# 1. Infrastructure (PostgreSQL 16)
docker compose -f deploy/docker-compose.dev.yml up -d

# 2. API  → http://localhost:5080/swagger
cd server
dotnet run --project src/NAU.Api

# 3. Tests
dotnet test
```

## Documentation

Start with [docs/phase-1-product-discovery.md](docs/phase-1-product-discovery.md),
then [docs/phase-2-system-design.md](docs/phase-2-system-design.md) (architecture, schema,
API contract) and [docs/phase-3-uiux-design.md](docs/phase-3-uiux-design.md).

## Tech stack

Angular 20 · Angular Material + Tailwind · ASP.NET Core (.NET 9) · Clean Architecture ·
MediatR (CQRS) · FluentValidation · EF Core + PostgreSQL · ASP.NET Identity + JWT ·
Razorpay · Serilog · Docker · GitHub Actions
