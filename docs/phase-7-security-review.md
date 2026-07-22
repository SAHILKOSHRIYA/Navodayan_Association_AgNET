# NAU — Phase 7: Security Review & Checklist

**Date:** 2026-07 · **Scope:** the V1 backend + frontend as built.
This documents the security posture (what's in place and why), the review findings and fixes, and the
residual items to address as the platform grows. Written to double as a handover reference.

---

## 1. Summary

The platform was built security-first (Phase 2 §7). This review re-checked each control against the
OWASP Top 10 and fixed the issues found. **No high-severity issues remain open.** Two hardening fixes
were applied during this review (§3).

---

## 2. Controls in place

| Area | Control | Where |
|---|---|---|
| **Authentication** | ASP.NET Identity (PBKDF2 password hashing), account lockout after 5 failed logins | `AddIdentityCore` |
| | JWT access tokens (15 min) + **rotating** refresh tokens, hashed at rest; reuse of a revoked token **revokes the whole family** | `AuthService`, `JwtTokenService` |
| | Email verification required before login; password reset invalidates all sessions | `AuthService` |
| | Account-enumeration protection (forgot-password / resend never reveal if an account exists) | `AuthService` |
| **Authorization** | Role-based, deny-by-default per endpoint (`[Authorize]` / explicit `[AllowAnonymous]`); admin routes behind an `Admin` policy; role changes & deletes restricted to SuperAdmin | all controllers |
| | Resource-level checks (e.g. a donation receipt is readable only by its owner or an admin) | `GetDonationReceiptHandler` |
| | Data privacy: a single enforcement path filters profile fields by per-section visibility | `ProfilePrivacyFilter` |
| **Payments** | Constant-time HMAC signature verification (checkout + webhook); webhook is the source of truth, stored before processing and **idempotent** by event id; amount validated server-side, never trusted from the client | `RazorpaySignature`, `HandleWebhook`, `VerifyDonation` |
| | Money integrity: campaign totals are **derived** from captured donations, never hand-editable | `CampaignTotals` |
| **Input** | FluentValidation on every command/query; EF Core parameterises all SQL (no string concatenation) | `ValidationBehavior`, handlers |
| **Uploads** | Content-type allow-list + size cap; **stored extension derived from the validated content type** (not the client filename); path-traversal guard; `nosniff` on file responses | `LocalFileStorage`, `FilesController` |
| **Transport/headers** | `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy` on every response; HTTPS terminated by the host (Render/Nginx) | security-headers middleware |
| **Rate limiting** | Tight per-IP limit on auth + donation-order endpoints (10/min), general ceiling (200/min) | `AddRateLimiter` |
| **Secrets** | All secrets from environment variables; none committed; `.gitignore` blocks `.env`; `.env.example` documents keys | config |
| **Audit** | Append-only audit log on every admin mutation (role change, suspend, delete) | `UserAdminService` |
| **Errors** | Global handler returns a safe message + trace id; internals/stack traces never leaked | `ExceptionHandlingMiddleware` |

---

## 3. Findings fixed in this review

| # | Severity | Finding | Fix |
|---|---|---|---|
| S1 | **High** | Uploaded files were stored using the extension from the client-supplied filename, while only the (spoofable) content-type header was validated. A crafted `evil.html` with an `image/png` header could be stored as `.html` and later served inline → **stored XSS**. | Storage now derives the extension from the validated content type (`image/jpeg→.jpg` etc.); unknown types fall back to a sanitised short extension or `.bin`. Added `X-Content-Type-Options: nosniff` on file responses. |
| S2 | Medium | No baseline security headers on responses. | Added `nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy` middleware for all responses. |

---

## 4. Residual items (recommended, not blocking the pilot)

| Item | Priority | Notes |
|---|---|---|
| **Content Security Policy (CSP)** | Medium | Add a CSP header. Needs an allowance for the Razorpay checkout script/frames (`checkout.razorpay.com`). Best added at go-live once the final origin is known. |
| **Image re-encoding / magic-byte sniffing** | Medium | S1 closes the practical risk; for defence-in-depth, verify uploads by magic bytes and/or re-encode images server-side. |
| **HSTS** | Low | Enable `Strict-Transport-Security` once on the final HTTPS domain (Render already serves HTTPS). |
| **Refresh-token cookie option** | Low | Tokens are stored in `localStorage` (simple, XSS-dependent). A httpOnly-cookie refresh flow is a stronger future option. |
| **Automated dependency scanning** | Low | Add GitHub Dependabot / `dotnet list package --vulnerable` to CI. |
| **Penetration test** | Low | A third-party pass before a large public launch. |

---

## 5. Test coverage note

42 unit tests cover the security-relevant logic (validation rules, the privacy-filter matrix, receipt
numbering, token behaviours). The critical flows were also verified end-to-end against a real database
during development (auth incl. token-reuse defence, payment signature + webhook idempotency,
cross-member privacy, RBAC denials). A future integration/E2E suite (Playwright + API tests against a
throwaway Postgres) is the recommended next step and is scaffolded in `server/tests/NAU.IntegrationTests`.
