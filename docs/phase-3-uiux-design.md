# NAU — Phase 3: UI/UX Design
### Navodaya Alumni Fundraising & Community Management Platform — Version 1

**Document status:** Draft for approval · **Date:** 2026-07-18
**Depends on:** Phase 1 (approved) · Phase 2 (approved; hosting = Render free tier now, VPS later)
**Next phase (blocked on approval):** Phase 4 — Backend Development

> Living document — wireframes are contracts of *content and hierarchy*, not pixel law.
> Visual polish iterates during Phase 5 without re-approval as long as structure holds.

---

## 1. Design Principles

1. **Trust first.** People are being asked for money and personal data. Every screen signals legitimacy: real numbers, named committee, transparent campaign math, receipts.
2. **Mobile-first.** Most alumni will arrive from WhatsApp links on phones. Every layout is designed at 360 px first, enhanced upward.
3. **Low-friction giving.** Landing → donate in ≤ 3 taps. Guests can donate without an account (D7).
4. **Admin without a developer.** Every operational task (verify, campaign, event, announcement, export) is self-service with obvious affordances.
5. **Accessible.** Material components, WCAG AA contrast, full keyboard paths, alt text, visible focus, form errors announced.

---

## 2. Design System

### 2.1 Color palette (Tailwind tokens + Material theme)

| Token | Value | Use |
|---|---|---|
| `primary-900…600` | Deep navy `#122B54 → #1E4A8F` | Header, primary buttons, links — institutional JNV blue |
| `accent-500` | Saffron `#F59E0B` | CTAs ("Donate"), progress bars, highlights |
| `accent-600` | `#D97706` | CTA hover |
| `success` | `#15803D` | Verified badge, captured payments |
| `warning` | `#B45309` | Pending states |
| `danger` | `#B91C1C` | Rejections, destructive actions |
| `surface` | `#FFFFFF` / `#F8FAFC` | Cards / page background |
| `ink-900/600/400` | `#0F172A / #475569 / #94A3B8` | Text hierarchy |

Semantic-only usage (no raw hex in components). Dark mode: **out of V1** (tokens make it a later theme swap — logged D11).

### 2.2 Typography
- **Headings:** *Poppins* (600/700) — friendly-institutional.
- **Body/UI:** *Inter* (400/500) — high legibility at small sizes.
- Scale (rem): `h1 2.25 · h2 1.75 · h3 1.375 · body 1 · small .875 · caption .75`; line-height 1.5 body / 1.2 headings. Self-hosted fonts (no CDN dependency).

### 2.3 Spacing, radius, elevation
4 px base grid; spacing steps 4/8/12/16/24/32/48/64. Radius: 8 px inputs/buttons, 12 px cards, 16 px hero blocks. Elevation: flat + 1 subtle card shadow + 1 dialog shadow — no shadow zoo.

### 2.4 Breakpoints (Tailwind defaults)
`<640` phone (1-col) · `≥640 sm` (2-col cards) · `≥768 md` (tablet, side-nav appears in portals) · `≥1024 lg` (3-col grids, admin tables full) · `≥1280 xl` (max content width 1200 px, centered).

### 2.5 Voice & content rules
Plain, warm, bilingual-friendly English (Hindi content can live in body text). Numbers always formatted Indian-style (₹1,00,000). Dates: `12 Aug 2026`. Buttons are verbs: *Donate now*, *Submit for verification*, *Approve*.

---

## 3. Information Architecture & Navigation Map

```
PUBLIC (no login)                      ALUMNI PORTAL (login+verified*)        ADMIN PORTAL (admin roles)
─────────────────                     ──────────────────────────────         ─────────────────────────
/                Home                 /app             Dashboard             /admin            Dashboard
/about           About+Mission        /app/profile     My profile (edit)     /admin/verifications  Queue
/committee       Executive committee  /app/directory   Alumni directory      /admin/campaigns  CRUD+updates
/campaigns       Campaign list        /app/campaigns   Campaigns (member     /admin/donations  All donations
/campaigns/:slug Campaign detail                        view + donate)       /admin/events     CRUD+RSVPs+gallery
/events          Events (upcoming/    /app/events      Events + my RSVPs     /admin/announcements CRUD
                 past)                /app/donations   My donations+receipts /admin/users      Users & roles
/stories         Success stories      /app/announcements Announcements       /admin/content    Stories/gallery/
/gallery         Gallery                                                                        committee
/contact         Contact              * unverified users see /app limited    /admin/reports    Exports
/auth/login ·    /auth/register ·       to profile + verification status     /admin/audit      Audit (SuperAdmin)
/auth/verify ·   /auth/forgot
```

**Shells:**
- **Public shell:** top navbar (logo · About · Campaigns · Events · Stories · Contact · **Login** · **Donate** accent button) → hamburger sheet on mobile; rich footer (contact, links, legal: T&C / privacy / refund incl. the association-status disclaimer).
- **Portal shell (alumni+admin):** left side-nav (md+) / bottom-sheet nav (mobile), top bar with school name, notifications placeholder, avatar menu (profile · logout). Admin nav section only renders for admin roles.

**Route guards** (from Phase 2 §4): `authGuard` → `/auth/login`; `verifiedGuard` → "verification pending" screen; `roleGuard` → 403 page.

---

## 4. Key User Journeys

**J1 Register → Verified (the trust pipeline)**
Landing → *Join as alumni* → register form → "check your email" screen → verify link → guided profile wizard (3 steps: personal → academic (batch/house) → professional) → *Submit for verification* → status screen (Pending, with what-happens-next) → email on approval → badge + full portal unlocked.
*States designed:* unverified-email, profile-incomplete, pending, rejected (reason + resubmit).

**J2 Donate (guest, ≤3 taps)**
Campaign card → campaign detail → *Donate now* → amount sheet (preset chips ₹500/₹1,000/₹2,500/₹5,000 + custom; name+email; anonymous checkbox; T&C link) → Razorpay checkout → success screen (amount, receipt download, share button) / failure screen (retry, nothing charged).

**J3 Admin verifies an alumnus**
Admin dashboard "Pending verifications: N" card → queue → row expands to full submitted profile → *Approve* (confirm) or *Reject* (reason required) → queue advances; action audit-logged.

**J4 Admin runs a campaign**
Campaigns → *New campaign* → form (title, rich description, cover upload, goal, dates) → save Draft → preview → *Activate* → posts progress updates over time → *Complete/Close* → export donation report.

---

## 5. Wireframes

### 5.1 Public — Home (mobile-first)

```
┌────────────────────────────┐   ≥1024px: hero becomes 2-col (text|photo),
│ ☰  NAU-JNV        [Donate] │   stats 4-across, campaigns 3-across.
├────────────────────────────┤
│  HERO photo (school)       │
│  "Once a Navodayan,        │
│   always a Navodayan."     │
│  [Join as alumni] [Donate] │
├────────────────────────────┤
│  ₹6.2L raised · 312 alumni │  ← live stats strip (API /content/home)
│  9 campaigns · 14 events   │
├────────────────────────────┤
│  ACTIVE CAMPAIGNS          │
│  ┌──────────────────────┐  │
│  │ [cover]              │  │
│  │ Library Renovation   │  │
│  │ ███████░░░  62%      │  │
│  │ ₹6.2L of ₹10L        │  │
│  │ [Donate]  [Details]  │  │
│  └──────────────────────┘  │
│  (…more cards)  [View all] │
├────────────────────────────┤
│  UPCOMING EVENTS (cards)   │
│  SUCCESS STORIES (carousel)│
│  GALLERY (2-col grid)      │
│  TESTIMONIALS              │
├────────────────────────────┤
│  FOOTER: about · contact · │
│  legal/disclaimer · social │
└────────────────────────────┘
```

### 5.2 Public — Campaign detail + donate sheet

```
┌────────────────────────────┐    Donate bottom-sheet (mobile) / dialog (desktop):
│ ← Campaigns                │    ┌──────────────────────────┐
│ [cover image]              │    │ Donate to Library Renov. │
│ Library Renovation         │    │ (₹500)(₹1,000)(₹2,500)   │
│ by NAU Association         │    │ (₹5,000)(Custom ____)    │
│ ███████░░░ 62%             │    │ Name  [____________]     │
│ ₹6,20,000 raised of ₹10L   │    │ Email [____________]     │
│ 128 donors · ends 30 Sep   │    │ [ ] Donate anonymously   │
│ [ Donate now ]             │    │ ☑ I agree to T&C         │
│ ── About ─ Updates ─ Donors│    │ [ Proceed to pay ]       │
│ (tabbed content)           │    └──────────────────────────┘
│ Donors: Top · Recent lists │    Logged-in: name/email prefilled.
│ (anonymous → "Well-wisher")│
└────────────────────────────┘
```

### 5.3 Auth — Register / profile wizard

```
Register:  full name · email · password (strength meter) · school (fixed V1) ·
           [Create account] · "Already registered? Login"
Wizard (after email verify):  Step chips  ①Personal ②Academic ③Professional
           batch (dropdown 1993–2026) · house · roll no(opt) · city/country ·
           company · designation · industry · skills (chip input) · bio ·
           photo upload (crop 1:1) · LinkedIn/GitHub ·
           per-section privacy: (Public ▾ / Members ▾ / Private ▾)
           Completion ring: "Profile 78% complete"
           [Save draft] [Submit for verification]
```

### 5.4 Alumni — Directory

```
┌──────────────────────────────────────────────┐
│ Search [ name/company/city…      ] [Filters] │  Filters drawer: batch ▾ ·
├──────────────────────────────────────────────┤  city ▾ · country ▾ ·
│ ┌────────┐ ┌────────┐ ┌────────┐             │  industry ▾ · skills chips
│ │ ◉ photo│ │        │ │        │             │  Sort: name · batch · city
│ │ A. Verma│ │ …     │ │ …      │             │
│ │ ✓ B-27 │ │        │ │        │             │  Card → profile detail
│ │ SDE @ X│ │        │ │        │             │  (privacy-filtered fields;
│ │ Raipur │ │        │ │        │             │   hidden = simply absent)
│ └────────┘ └────────┘ └────────┘             │
│              ‹ 1 2 3 … ›                     │
└──────────────────────────────────────────────┘
```

### 5.5 Admin — Dashboard

```
┌ Side-nav ┬───────────────────────────────────────────┐
│ Dashboard│  ┌─────────┐┌─────────┐┌─────────┐┌──────┐│
│ Verifs(7)│  │ Alumni  ││Verified ││ Pending ││Funds ││  ← KPI cards; Pending
│ Campaigns│  │  312    ││  268    ││   7 →   ││₹6.2L ││    links to queue
│ Donations│  └─────────┘└─────────┘└─────────┘└──────┘│
│ Events   │  [Monthly donations bar chart          ]  │
│ Announce │  [Registration trend line] [Campaign    ] │
│ Users    │                            [performance ] │
│ Content  │  Recent donations (table, last 10)        │
│ Reports  │  Upcoming events (list)                   │
└──────────┴───────────────────────────────────────────┘
```

### 5.6 Admin — Verification queue

```
Queue (Pending 7 | Approved | Rejected)         Row expand →
┌───────────────────────────────────────┐   full submitted profile
│ ◉ Rahul S. · B-29 · submitted 2d ago  │   side-by-side with any
│   [Review ▾]                          │   previous rejection notes.
│     └ profile detail…                 │   [Approve ✓]  [Reject ✗]
│       [Approve] [Reject(reason req.)] │   Reject → reason dialog
└───────────────────────────────────────┘   (required, sent to user)
```

### 5.7 Remaining screens (structure specified, same patterns)
- **Events (public/portal):** card list w/ upcoming|past toggle → detail (date, venue, map link, gallery) → RSVP segmented control (Going/Maybe/No). Admin: CRUD form + participants table + gallery upload.
- **My donations:** table (date, campaign, amount, status chip, receipt ⬇).
- **Announcements:** category-chip filtered feed; admin: editor w/ category+audience+publish toggle.
- **Admin users:** search + table (name, role chips, status, joined) → role dialog, suspend confirm; SuperAdmin-only delete.
- **Admin reports:** report type ▾ + date range + format (CSV/XLSX/PDF) → [Generate & download].
- **System screens:** 403, 404, empty states (every list has one, with next-action CTA), skeleton loaders, global toast pattern, "verification pending" hold screen.

---

## 6. Component Inventory (shared/)

**Layout:** `PublicShell`, `PortalShell`, `SideNav`, `TopBar`, `PageHeader`, `Footer`
**Display:** `StatCard`, `CampaignCard` (+`ProgressBar`), `EventCard`, `ProfileCard`, `VerifiedBadge`, `StatusChip`, `EmptyState`, `SkeletonCard/Table`, `AvatarPhoto`, `SectionHeading`, `TestimonialCarousel`, `GalleryGrid`
**Forms:** `FormField` wrapper (label+error+hint), `AmountChips`, `ChipInput` (skills), `ImageUploader` (crop+progress), `RichTextEditor` (sanitized), `PrivacySelect`, `PasswordMeter`, `DateRangePicker`, `ConfirmDialog`, `ReasonDialog`
**Data:** `DataTable` (sort/paginate/export hooks), `FilterDrawer`, `Paginator`, `ChartCard` (bar/line/donut via ng2-charts)
**Feedback:** `ToastService`, `LoadingButton`, `InlineAlert`

Each ships with: typed inputs/outputs, a11y notes, and usage in at least one feature before generalizing (rule: extract on 2nd use, not 1st).

---

## 7. Decision Log (Phase 3)

| # | Decision | Why | Revisit |
|---|---|---|---|
| D11 | No dark mode in V1; tokens keep it cheap later | Surface area control | Post-launch |
| D12 | Poppins + Inter, self-hosted | Institutional-friendly, no CDN/PII leak | Branding pass |
| D13 | Donate = bottom-sheet/dialog, not separate page | ≤3-tap giving (principle 3) | Conversion data |
| D14 | Profile privacy = per-section (not per-field) | Simpler mental model + simpler enforcement | User feedback |
| D15 | Batch dropdown 1993–2026 (JNV Raipur founding→now) | Data hygiene vs free text | Multi-school V2 |
| D16 | Empty/skeleton/error states are first-class deliverables | Perceived quality = trust | — |

---

## 8. Approval Gate

With your approval, **Phase 4 — Backend Development** begins: scaffolding the .NET 9 solution
(M0 skeleton → M1 auth) exactly per Phase 2 §10, with migrations, tests, and Swagger from the first
commit. I'll build module-by-module and report progress at each module boundary rather than asking
for approval on every file.
