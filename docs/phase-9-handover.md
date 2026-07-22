# NAU — Phase 9: Handover & Maintainer's Guide

A map of the project for anyone who picks it up later. Pair this with the [README](../README.md)
(how to run) and the [deployment runbook](phase-8-deployment.md) (how to ship).

---

## 1. The 30-second mental model

- **One backend** (ASP.NET Core, `server/`) exposes a REST API and, in production, also serves the
  **one frontend** (Angular, `client/`) as static files. A single Docker image contains both.
- The backend follows **Clean Architecture** — four projects with a strict dependency direction:
  `Api → Application → Domain`, and `Infrastructure → Application/Domain`. Domain depends on nothing.
- Requests flow: **Controller** (thin) → **MediatR command/query** (business logic in `Application`)
  → **`IAppDbContext` / services** (implemented in `Infrastructure`) → **PostgreSQL**.

## 2. Where things live

| You want to change… | Look in |
|---|---|
| An API endpoint's shape/route | `server/src/NAU.Api/Controllers/*` |
| Business rules / a feature's logic | `server/src/NAU.Application/Features/<Feature>/*` |
| Validation rules | the `*Validators.cs` next to each feature |
| Database tables / columns | `server/src/NAU.Domain/Entities/*` + mapping in `Infrastructure/Persistence/AppDbContext.cs`, then add a migration |
| Auth / JWT / password rules | `Infrastructure/Auth/*` and `Infrastructure/DependencyInjection.cs` |
| Payment behaviour | `Infrastructure/Payments/*` (+ `Application/Features/Donations/*`) |
| Email behaviour | `Infrastructure/Email/*` |
| A web page / screen | `client/src/app/features/<area>/*.component.ts` |
| Shared frontend plumbing (API calls, auth, guards) | `client/src/app/core/*` |
| Colours / design tokens | `client/tailwind.config.js` and `client/src/styles.scss` |

## 3. How to add a feature (the repeating pattern)

1. **Domain:** add/adjust an entity in `NAU.Domain`.
2. **Application:** add a `record` command/query + a MediatR handler in `Features/<Feature>/`, plus a
   FluentValidation validator. Handlers use `IAppDbContext`.
3. **Infrastructure:** map any new entity in `AppDbContext.OnModelCreating`; expose it on
   `IAppDbContext`. Run `dotnet ef migrations add <Name> -p src/NAU.Infrastructure -s src/NAU.Api`.
4. **Api:** add a thin controller action that sends the MediatR message and wraps the result in
   `ApiResponse<T>`.
5. **Client:** add a method to the relevant service in `core/services.ts`, then a component.
6. **Test:** a unit test for the handler/validator; extend the integration tests for a security-
   sensitive path.

## 4. Conventions that matter

- **Every API response** is the envelope `{ success, data, message, errors }`; lists are `PagedResult<T>`.
- **Errors** are thrown as typed exceptions (`NotFoundException`, `ConflictException`,
  `DomainRuleException`, `ForbiddenException`) and mapped to HTTP codes centrally — don't return raw
  status codes from handlers.
- **Money** (`raised_amount`, totals) is always **derived from captured donations**, never stored or
  hand-edited.
- **Privacy** for profiles goes through the single `ProfilePrivacyFilter` — never re-implement it.
- **Secrets** come only from environment variables (`Section__Key`). Never commit real values.
- **Migrations** are additive and run automatically on boot when `Database__MigrateOnStartup=true`.

## 5. Running, testing, shipping

```bash
# Run locally (see README for details)
docker compose -f deploy/docker-compose.dev.yml up -d      # database
cd server && dotnet run --project src/NAU.Api               # API + Swagger at :5080/swagger
cd client && npm start                                      # website at :4200

# Tests
cd server && dotnet test                                    # 42 unit + 7 integration (needs Docker)

# One production container
docker build -t nau-app . && docker run -p 8080:8080 nau-app  # (with DB + secrets env vars)
```

Deploy = push to GitHub → Render reads `render.yaml`. CI (`.github/workflows/ci.yml`) builds and
tests everything on every push.

## 6. Known local-environment gotcha

On the original dev machine, `npm` is broken by a bad PATH entry
(`C:\Program Files\nodejs\node_modules\npm\bin` doubles npm's module path). Workaround: remove that
entry from PATH for the shell session before running npm/ng. This does **not** affect CI or Render
(clean Linux environments).

## 7. What's intentionally deferred (V2 backlog)

Mentorship, job/internship portal, volunteer management, mobile app, AI features, multi-school
onboarding UI, tax (80G) receipts, and a full frontend E2E (Playwright) suite. The data model already
carries `school_id` everywhere, so multi-school is a UI/onboarding effort, not a schema rewrite.

## 8. Document index

- [phase-1-product-discovery.md](phase-1-product-discovery.md) — what & why, scope, personas, risks
- [phase-2-system-design.md](phase-2-system-design.md) — architecture, database schema, API contract, decisions
- [phase-3-uiux-design.md](phase-3-uiux-design.md) — flows, wireframes, design system
- [phase-7-security-review.md](phase-7-security-review.md) — security posture & checklist
- [phase-8-deployment.md](phase-8-deployment.md) — deployment runbook
- [OWNER-ACTION-GUIDE.md](OWNER-ACTION-GUIDE.md) — the non-technical owner's to-do list
