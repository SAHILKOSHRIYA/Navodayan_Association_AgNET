# 📋 Owner's Action Guide — Everything YOU Need To Do

This is your single checklist for taking the Navodaya Alumni platform from "built" to "live and
fully working." It's written in plain language — no coding needed. Do the sections in order; you can
stop after Section 3 and already have a usable pilot.

**What's already done for you:** the entire website and server are built, tested, packaged, and
uploaded to GitHub. You do **not** need to write or run any code. Your job is to create a few free
accounts and paste some settings.

Legend: ⏱️ = rough time · 💰 = cost · 🔑 = a secret you'll create/keep safe

---

## Section 0 — Accounts you'll need (all free to start)

| Account | What it's for | Cost |
|---|---|---|
| **GitHub** (you already have it) | Stores the code; the deploy reads from here | Free |
| **Render.com** | Runs the website + database online | Free tier to start |
| **Razorpay** | Accepts real donations (needed only when you want live money) | Free; charges a % per donation |
| **Email provider** (Brevo / Gmail / etc.) | Sends verification & receipt emails | Free tier is enough |
| **Domain registrar** (optional) | A custom web address like `nau27r.org` | ~₹800–1,200/year |

You can launch a pilot with just **GitHub + Render**. The rest can be added later.

---

## Section 1 — Put the website online (the pilot) ⏱️ 15 min · 💰 free

1. Go to **render.com** and **sign up with your GitHub account** (one click).
2. Click **New +** (top right) → **Blueprint**.
3. Choose the repository **`Navodayan_Association_AgNET`**. Render will read the deploy recipe
   (`render.yaml`) and automatically propose **one web service + one database**. Don't change these.
4. Render will ask you to fill a few blanks. Enter:
   - 🔑 **`Seed__SuperAdmin__Email`** → the email you want to use as the master admin (e.g. your email).
   - 🔑 **`Seed__SuperAdmin__Password`** → a strong password (write it down safely).
   - **`Client__BaseUrl`** → leave **blank** for now (you'll set it in Section 2).
   - Leave the `Payments__*` blanks empty for now (payments stay in safe "test" mode).
5. Click **Apply**. Render now builds everything and starts it. First build takes ~5–10 minutes.
6. When it says **Live**, click the URL (looks like `https://nau-app.onrender.com`). You should see
   the alumni homepage. 🎉

> ⚠️ Free-tier quirks (totally fine for a pilot):
> - The site **"sleeps" after ~15 min of no visitors** — the first visit after that takes ~30–50 sec
>   to wake up, then it's fast.
> - The **free database is deleted after ~90 days**. Before then, either upgrade the database to a
>   paid plan (~₹600/mo) or export your data. (I can add automatic backups when you're ready.)

---

## Section 2 — Finish the basic setup ⏱️ 5 min

1. Copy your live URL from Section 1 (e.g. `https://nau-app.onrender.com`).
2. In Render → your **web service** → **Environment** tab → find **`Client__BaseUrl`** → paste that URL
   → **Save**. (This makes the links inside emails point to the right place.)
3. Render redeploys automatically. Once live, open the URL and go to **`/auth/login`**.
4. Sign in with the admin email + password from Section 1.
5. **Immediately change the password** from your profile for safety (or note that the seeded one is
   now your real one and keep it secret).

✅ **You now have a working pilot.** Admins can verify members, create campaigns, post events and
announcements. The only limits until later sections: donations are in "test" mode (no real money),
and verification emails appear in the server logs instead of arriving in inboxes.

---

## Section 3 — Turn on real emails ⏱️ 20 min · 💰 free tier

Right now the system is ready to send emails but needs an email account to send *through*. I've built
the email feature so it just needs your provider's details.

**Recommended: Brevo (formerly Sendinblue)** — free 300 emails/day, easy SMTP.

1. Sign up at **brevo.com** → go to **SMTP & API** → note your **SMTP server, port, login, and key**.
2. In Render → web service → **Environment** → add these (I'll confirm the exact key names in the
   `email-setup` note once you're ready):
   - `Email__Provider` = `smtp`
   - `Email__Host` = (from Brevo, e.g. `smtp-relay.brevo.com`)
   - `Email__Port` = `587`
   - 🔑 `Email__Username` = (your Brevo login)
   - 🔑 `Email__Password` = (your Brevo SMTP key)
   - `Email__FromEmail` = the "from" address (e.g. `noreply@yourdomain` or your Brevo-verified email)
   - `Email__FromName` = `Navodaya Alumni Association`
3. Save → Render redeploys. Now registration/verification/receipt emails actually arrive.

*(Gmail also works via an "App Password," but Brevo is more reliable for a real service.)*

---

## Section 4 — Accept real donations (Razorpay) ⏱️ 1–3 days (mostly KYC waiting)

1. Sign up at **razorpay.com**. Complete **KYC** (identity + bank details). This is the slow part —
   Razorpay reviews it; can take a day or two.
2. Decide **whose bank account and PAN** the association's money settles into. Because the association
   isn't a registered body, this will be an individual's account (as noted in your disclaimer). Agree
   this among the group first.
3. Once approved, in Razorpay get: 🔑 **Key ID**, 🔑 **Key Secret**.
4. In Razorpay → **Settings → Webhooks** → **Add webhook**:
   - URL: `https://<your-render-url>/api/v1/webhooks/razorpay`
   - 🔑 Set a **webhook secret** (make one up, strong).
   - Select the event **`payment.captured`**.
5. In Render → web service → **Environment**, set:
   - `Payments__Provider` = `razorpay`
   - 🔑 `Payments__KeyId` = your Key ID
   - 🔑 `Payments__KeySecret` = your Key Secret
   - 🔑 `Payments__WebhookSecret` = the webhook secret from step 4
6. Save → redeploy. Do **one real ₹1 donation** end-to-end to confirm money + receipt work, then
   you're live for fundraising.

---

## Section 5 — Custom domain (optional) ⏱️ 30 min + DNS wait · 💰 ~₹1,000/yr

1. Buy a domain (e.g. from GoDaddy, Namecheap, or Google Domains) — e.g. `nau27r.org`.
2. In Render → web service → **Settings → Custom Domains** → add your domain. Render shows a DNS record.
3. In your domain registrar, add that DNS record (a CNAME). Wait for it to take effect (minutes to a
   few hours). Render sets up HTTPS automatically.
4. Update **`Client__BaseUrl`** in Render to the new domain → redeploy.

---

## Section 6 — Bring over your old data (optional) ⏱️ depends

If you want the members and donation history from the **current site** (the React/Firebase one) carried
into the new platform:

1. Export the **member list** from Firebase (Firebase console → Firestore/Auth → export), and the
   **transaction history** from Razorpay (Razorpay dashboard → Transactions → export CSV).
2. Send me those files. I'll write a one-time import so existing members and the "₹ raised so far"
   history show up in the new system. (No action from you beyond providing the exports.)

---

## Section 7 — Running it day-to-day (for admins)

Once live, here's what admins do — all from the website, no tech needed:

- **Verify alumni:** Admin portal → *Verifications* → review each request → Approve / Reject.
- **Create a campaign:** Admin → *Campaigns* → New → fill goal + dates → **Activate** to make it public.
- **Post events & news:** create them; publish to show on the public site.
- **See the money:** Admin → *Donations* → view all, or **Export CSV** for records/accounting.
- **Manage people:** Admin → *Users* → change roles or suspend accounts.

---

## Quick reference — every secret you'll create

Keep these somewhere safe (a password manager). Never share or post them publicly.

| Secret | Where you set it | When |
|---|---|---|
| Super-admin email + password | Render (Section 1) | Now |
| Email username + password/key | Render (Section 3) | For real emails |
| Razorpay Key ID + Key Secret | Render (Section 4) | For live donations |
| Razorpay Webhook secret | Razorpay + Render (Section 4) | For live donations |

---

## What I'm doing on my side (no action needed from you)

While you handle the accounts above, I'm continuing with everything that doesn't need your logins:
building the real email-sending feature, adding automated tests, a security review, and (when you send
exports) the old-data import. I'll keep the code on GitHub up to date and note anything new here.

**Questions or stuck on a step?** Tell me which section and I'll walk you through it.
