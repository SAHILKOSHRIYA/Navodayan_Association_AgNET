# NAU — Phase 1: Product Discovery
### Navodaya Alumni Fundraising & Community Management Platform — Version 1 (MVP)

**Document status:** Draft for approval · **Date:** 2026-07-18
**Pilot institution:** Jawahar Navodaya Vidyalaya, Raipur (Batch 27R) — architected for multi-JNV from day one
**Next phase (blocked on approval):** Phase 2 — System Design

---

## 1. Vision & Product Goal

**Vision.** Become the official digital alumni ecosystem for Jawahar Navodaya Vidyalaya —
connecting alumni, students, teachers, and administration — starting with one school and scaling to
a nationwide Navodaya Alumni Network.

**Version 1 goal (single, sharp focus).** *Digitize the JNV alumni association by delivering a
verified alumni directory and a transparent fundraising platform, wrapped in a public landing
website and an admin control panel — production-ready enough for a real association to adopt.*

Everything in V1 exists to serve that one goal. Anything that doesn't is deferred to V2.

---

## 2. Product Scope

### 2.1 In scope (V1)
1. **Public landing website** — About, mission, committee, campaigns, events, success stories, gallery, contact, register/login.
2. **Authentication & onboarding** — register, login, email verification, forgot/reset password, JWT + refresh, RBAC.
3. **Alumni profiles** — personal / professional / academic info, social links, privacy settings, profile-completion indicator.
4. **Alumni verification** — submit → admin review → approve/reject → verified badge, with history.
5. **Alumni directory** — search & filter (batch, name, company, designation, industry, skills, city, country), profile cards, pagination, sorting.
6. **Fundraising** — campaigns with goal/progress, donations, donor wall, anonymous option, campaign updates.
7. **Payments** — Razorpay (UPI/cards), webhook verification, receipts, transaction history, refund-ready architecture.
8. **Events (basic)** — create, RSVP, participant list, event gallery, upcoming/past.
9. **Announcements** — admin-published, categorized (general, academic, events, fundraising, achievements).
10. **Admin dashboard** — KPI cards + charts (alumni, verifications, campaigns, funds, events, donations).
11. **User management** — search, assign roles, suspend, delete, audit history.
12. **Reports** — export donation/campaign/user/verification reports (CSV/Excel/PDF).

### 2.2 Explicitly OUT of scope (deferred to V2+)
Mentorship · Job/internship portal · Volunteer management · Mobile app · AI assistant/chatbot ·
Recommendation engine · WhatsApp automation · Certificates · Advanced analytics · Live chat ·
Multi-school onboarding UI *(the data model supports multi-school, but V1 ships single-school)* ·
International payments · Tax (80G) receipts.

> **Guardrail:** any request outside §2.1 during V1 goes to a V2 backlog, not into V1.

---

## 3. User Personas

| Persona | Who they are | Primary jobs-to-be-done | V1 access |
|---|---|---|---|
| **Guest** | Unregistered visitor | Learn about the association, view public campaigns/announcements, register/donate | Public site, register, login, public campaign view |
| **Alumni** | Verified ex-student (e.g. JNV Raipur '19) | Maintain profile, find batchmates, donate, RSVP events | Full member portal (privacy-controlled) |
| **Student** | Current JNV student | Read announcements, RSVP public events | Read-only + public events |
| **Teacher** | Current/former faculty | View announcements/events, manage assigned content | Read + limited assigned content |
| **Association Admin** | Association office-bearer (e.g. Sanjay Nishad) | Verify alumni, run campaigns, manage donations/events/announcements, view reports | Admin portal (scoped to their school) |
| **Super Admin** | Platform owner/maintainer | Full control, manage admins, platform config, multi-school seed | Everything |

**Role hierarchy:** Super Admin → School/Association Admin → Teacher → Alumni → Student → Guest.

---

## 4. Functional Requirements (by module)

> IDs are stable references for Phase 2 API design and the backlog.

**FR-1 Landing website** — public, responsive, SEO-friendly pages listed in §2.1(1); homepage shows hero, mission, live statistics (alumni count, funds raised), latest campaigns, recent events, testimonials, gallery, footer with contact.

**FR-2 Authentication** — email+password registration; email verification required before profile submission; login issues short-lived JWT access token + long-lived refresh token; refresh rotation + revoke on logout; forgot/reset password via emailed token; RBAC enforced server-side on every protected endpoint; architecture leaves room for Google OAuth (not enabled in V1 unless trivial).

**FR-3 Alumni profile** — create/edit profile with fields: name, batch, house, school, roll no (optional), email, mobile, DOB, address, current city/country, company, designation, industry, education, skills[], bio, LinkedIn, GitHub, profile picture (uploaded to object storage); privacy toggles per field group (contact/professional); profile-completion percentage.

**FR-4 Verification** — alumnus submits verification request; admin sees a queue; admin approves (assigns verified badge) or rejects (with reason); every decision recorded with actor + timestamp (verification history); only verified alumni appear in the public directory and can donate as named donors.

**FR-5 Directory** — search across batch/name/company/designation/industry/skills/city/country; results as profile cards; pagination + sorting; profile detail respects privacy settings; only verified alumni are listed.

**FR-6 Fundraising** — admin creates campaign (title, description, cover image, goal amount, start/end date, status, organizer, supporting documents); public campaign page shows progress bar, raised/remaining, recent donations, top donors, campaign updates; `RaisedAmount` derived from successful donations, never edited by hand.

**FR-7 Payments** — Razorpay order creation → checkout → **server-side signature verification** → donation recorded on verified success only; Razorpay webhook as the source of truth for final status; receipt (PDF) generated per successful donation; full transaction log; refund-capable data model (refund UI deferred).

**FR-8 Events** — admin creates event (title, description, date, location, cover); alumni/students RSVP; participant list; event gallery; upcoming vs past split.

**FR-9 Announcements** — admin creates categorized announcements; visible on landing site and in portals per audience.

**FR-10 Admin dashboard** — cards: registered alumni, verified alumni, pending verifications, active campaigns, funds raised, upcoming events, recent donations; charts: monthly donations, registration trend, campaign performance, verification status.

**FR-11 User management** — admin can search users, assign/change roles, suspend, delete; all actions audit-logged.

**FR-12 Reports** — export donation/campaign/user/verification reports as CSV/Excel/PDF with date-range filters.

---

## 5. Key User Stories & Acceptance Criteria

> Representative stories per epic (full backlog produced in Phase 2). Format: *As a … I want … so that …* + **AC**.

**US-AUTH-1** — *As a guest, I want to register with email/password and verify my email so that I can access member features.*
**AC:** duplicate email rejected; password meets policy (min length + complexity); a verification email is sent; unverified users cannot submit a profile or donate as a named donor; token expiry handled with a resend option.

**US-PROFILE-1** — *As an alumnus, I want to complete my profile and control field privacy so that I share only what I choose.*
**AC:** required fields validated; image upload validated (type/size); completion % updates live; private fields are hidden from other users and the public directory.

**US-VERIFY-1** — *As an association admin, I want to review and approve/reject verification requests so that only genuine alumni are listed.*
**AC:** queue shows pending requests with submitted details; approve sets verified badge + directory visibility; reject requires a reason; decision + actor + timestamp stored and viewable.

**US-DIR-1** — *As a verified alumnus, I want to search the directory by batch/company/city so that I can reconnect with peers.*
**AC:** filters combine (AND); results paginated & sortable; only verified & directory-visible profiles returned; privacy settings enforced on detail view.

**US-FUND-1** — *As a guest or alumnus, I want to donate to a campaign and get a receipt so that I can contribute transparently.*
**AC:** donation only recorded after server-side Razorpay signature verification; campaign progress updates from successful donations only; anonymous option hides donor identity on the donor wall but retains it in admin records; PDF receipt generated and downloadable.

**US-ADMIN-1** — *As an association admin, I want a dashboard of key metrics so that I can run the association without a developer.*
**AC:** cards and charts reflect live data; funds-raised matches the sum of successful donations; pending-verification count links to the queue.

---

## 6. Non-Functional Requirements

- **Scale:** correct and performant for **1,000+ alumni** and concurrent campaign traffic; paginate all lists; index directory/search fields; cache hot reads (Redis, architected in V1).
- **Security:** RBAC on every protected route; hashed passwords (ASP.NET Identity); JWT + refresh rotation; HTTPS-only; rate limiting; input validation (FluentValidation); XSS/SQLi/CSRF mitigations; secure headers; file-upload validation; audit logging; secrets via environment variables only — **never hard-coded**.
- **Reliability/Ops:** structured logging (Serilog); health-check endpoints; graceful error handling with a consistent API error envelope; DB migrations versioned; backup + rollback plan (Phase 8).
- **Performance/UX:** responsive (mobile-first); accessible (WCAG-minded); optimized/resized images; fast first load for the landing site (SEO-friendly).
- **Maintainability:** Clean Architecture, SOLID, DRY/KISS; no business logic in controllers; documented APIs (Swagger).

---

## 7. Success Metrics (Year 1 targets)

| Metric | Target |
|---|---|
| Alumni registered | 1,000+ |
| Verified alumni | 90%+ of registered |
| Active monthly users | 500+ |
| Funds raised | ₹10–25 lakh |
| Events organized | 20+ |
| Directory searches / MAU | healthy engagement (baseline set post-launch) |

**V1 "done" criteria:** the association can, in production — register & verify alumni · maintain a
searchable directory · create & manage campaigns · accept secure online donations · publish
news/events · operate day-to-day from the admin dashboard.

---

## 8. Risk Analysis

| # | Risk | Impact | Likelihood | Mitigation |
|---|---|---|---|---|
| R1 | **Cold-start / low adoption** — a directory is only valuable when populated | High | Med | Seed with existing WhatsApp/Excel data; batch-captain outreach; make register→verify frictionless; migrate v0 members |
| R2 | **Legal/financial** — association is *not a registered body* (per v0 disclaimer) | High | Med | Carry disclaimer into T&C; Razorpay account under a clear owner; transparent ledger; keep refund/dispute process documented; defer 80G/tax receipts |
| R3 | **Payment integrity** — spoofed/failed donations | High | Low | Server-side signature + webhook as source of truth; never trust client callback; reconcile against Razorpay |
| R4 | **Data privacy** — alumni PII exposure | High | Med | Per-field privacy controls; verified-only directory; encryption in transit; least-privilege RBAC; audit logs |
| R5 | **Scope creep** — the V2 vision is large and tempting | High | High | Strict §2.2 guardrail; V2 backlog; phase-gated approvals |
| R6 | **Single maintainer / bus factor** | Med | High | Clean Architecture + docs + CI/CD so others can contribute; runbook in Phase 9 |
| R7 | **Hosting cost/ops** (AWS EC2, S3, Postgres) | Med | Med | Start with a single small EC2 + Docker Compose + managed/self-hosted Postgres; Redis optional at launch; monitor cost |
| R8 | **Razorpay KYC/approval delays** | Med | Med | Start KYC early; use test mode for development; gate go-live on account activation |

---

## 9. Assumptions

1. **V1 pilots with one school** (JNV Raipur) while the schema is multi-school ready; multi-school onboarding UI is V2.
2. **Razorpay** is the sole gateway for V1 (INR, UPI/cards); a Razorpay account (KYC) will be available before go-live.
3. **Email delivery** via an SMTP/transactional provider will be available (choice pending — see §10).
4. **Deployment** targets a single Ubuntu server (AWS EC2 or equivalent VPS) via Docker Compose for V1; Kubernetes/multi-node is out of scope.
5. **Object storage** is S3-compatible (AWS S3 or a MinIO-compatible bucket) for profile images, campaign covers, galleries.
6. **The maintainer has** (or will create) accounts for: GitHub, Razorpay, a cloud host, a domain, and an email provider.
7. **Existing v0 data** (Firebase members, Razorpay transaction history) can be exported for migration.

---

## 10. Decisions I Need From You (to finalize discovery)

These don't block me from starting Phase 2 design, but I'll need answers before implementation:

1. **Pilot scope confirm** — V1 ships **JNV Raipur only** with multi-school-ready schema? (recommended: yes)
2. **Domain name** — do you have/plan a domain (e.g. `nau27r.org` / `jnvalumni.in`)? Needed for HTTPS + emails.
3. **Hosting** — AWS (EC2 + S3 + RDS-Postgres) vs a cheaper VPS (e.g. Hetzner/DigitalOcean + self-hosted Postgres/MinIO)? Affects cost & DevOps setup.
4. **Email provider** — transactional email choice (e.g. Brevo/SendGrid/Amazon SES/Gmail SMTP)?
5. **Member approval model** — keep **manual admin verification** (recommended for trust) as the directory gate?
6. **Google Sign-In** — include in V1 if low-effort, or defer to V2?
7. **Data migration** — can you export the current Firebase members + Razorpay transactions?
8. **Razorpay account** — is KYC done / in progress, and who is the legal account holder?

---

## 11. Approval Gate

Per the master prompt's phased process, **implementation does not begin here.** On your approval of
this Product Discovery document (and answers to §10 where known), I will produce **Phase 2 — System
Design**: architecture diagrams, PostgreSQL ER model & schema, full REST API specification, Clean
Architecture folder structure, security architecture, and deployment architecture — then pause again
for approval before any code is written.
