# Improvements backlog

A running, prioritized list of concrete improvements found by auditing the codebase
(`main`, 2026-06-05). Each item says **what / why / where / effort**. Priorities:

- **P0** — product-principle violation or broken/incomplete core flow. Fix first.
- **P1** — real feature gap or UX problem users will hit.
- **P2** — tech debt / refactor / cleanup that pays for itself.
- **P3** — nice-to-have, speculative, or polish.

Effort is a rough t-shirt size (S < ½ day, M ~1–2 days, L > 2 days).

> Not in scope here: the deliberate phase-2 deferrals already recorded in
> `docs/decisions.md` (Redis, workers, realtime, monetization, automated NSFW/selfie
> verification). Those have answers; this list is about gaps and debt, not deferrals.

---

## P0 — Product-principle violations

### 1. The feed "Report" button is a no-op — ✅ FIXED (2026-06-05)
**Resolution:** The report button now opens a tray (same visual language as the appreciation
picker) with three curated reasons — *Inappropriate photo* (`inappropriate_content`),
*Looks AI-generated* (`ai_generated`), *Seems underage* (`underage`) — and submits via
`useSubmitReport` to `POST /reports`. On success the button flips to its confirmed state and
the feed hint acknowledges it; reporting deliberately does **not** advance the card. The
moderation pipeline now receives real input. (`FeedScreen.tsx`, `notice.css`.)

**Original finding —** **What:** On a feed card the report button only flipped local `reported`
state — it never called the API. The whole report path was built and unused.
**Why it matters:** CLAUDE.md §2 / backbone product principles: *"Pause / delete / export /
block / report must always work and be easy to reach."* Right now reporting is reachable but
does nothing — arguably worse than missing, because it tells the user "Reported — thank you"
while sending nothing. This is also a trust-and-safety hole: the entire moderation pipeline
(report pressure → auto temp-restriction → review queue) is starved of its only real input.
**Where:**
- Dead UI: `web/notice/src/features/notice/FeedScreen.tsx:262` (comment literally says
  *"visual only for now; not wired to the API"*).
- Already-built but **unused** plumbing: `web/notice/src/lib/api/reports.ts`
  (`submitReport`, `REPORT_TYPES`) and `web/notice/src/lib/query/reports.ts`
  (`useSubmitReport`). Backend `POST /reports` is complete and tested.
**Fix:** Replace the placeholder with a small report sheet — list `REPORT_TYPES`, optional
note, call `useSubmitReport`. On success show the confirmation. Consider auto-advancing the
card after a report (a reported profile probably shouldn't be appreciated).
**Effort:** S–M.

### 2. There is no way to block someone from the feed — ✅ FIXED (2026-06-05)
**Resolution:** The card's report tray now also holds a decisive **"Block this person"** action
(below the report reasons, set off by a divider). It calls `useBlockProfile` and, on success,
advances the card; the queue's seen-set + the both-directions eligibility filter keep the
profile gone. It's reversible from You → Blocked, so there's no confirm step. Report + Block now
live together in one card menu, as recommended. (`FeedScreen.tsx`, `settings.ts` query,
`notice.css`.)

**Original finding —** **What:** `blockProfile` / `useBlockProfile` existed and were wired in the
API/query layer, but **no component called them.** The only block-related UI is *Unblock* inside You → Blocked
people. A user can manage a blocklist they have no way to add to.
**Why it matters:** Same principle as #1 — *block must always work and be easy to reach.*
Today a user who wants to never see someone again literally cannot, except by reporting
(which also does nothing, per #1). The feed's eligibility query already honours blocks in
both directions (`GetFeedNextHandler.cs:276`); only the entry point is missing.
**Where:** unused `useBlockProfile` in `web/notice/src/lib/query/settings.ts:54`;
natural home is the same per-card overflow menu as #1 (`FeedScreen.tsx`).
**Fix:** Add a "Block" action to the card (ideally a small "⋯" menu holding Report + Block
together). On block, invalidate the feed queue and advance.
**Effort:** S.

> #1 and #2 are the headline findings: two of the five "must always be easy to reach" safety
> actions are reachable in name only. Strongly recommend a single card overflow menu that
> houses **Report** and **Block** and ships them together.

---

## P1 — Feature gaps & UX

### 3. A card can become un-advanceable (stuck feed) — ✅ FIXED (2026-06-05)
**Resolution:** `submitAppreciation` now throws a typed `ApiError` carrying the HTTP status and
the RFC 9457 problem slug. `FeedScreen.pick`'s `onError` skips the card (`onAdvance`) on a
*permanent* rejection (`profile-unavailable`, `duplicate-appreciation`, `self-appreciation`)
instead of trapping the user; transient/validation errors still keep the card and show a
retryable message. Report + Block (#1/#2) provide deliberate exits alongside this.

**Original finding —** **What:** Advancing the feed *requires* a successful appreciation (by design — no skip). But
if `POST /appreciations` fails non-transiently — receiver became not-visible (they paused /
blocked you between fetch and tap), or the category was already used on a retry — the card
shows an error and stays. Picking another trait can keep failing. There is no escape.
**Why it matters:** The positive-only "only way forward is to appreciate" rule assumes the
appreciation can always succeed. Once it can't, the user is wedged on one card with no skip,
no block (#2), and a no-op report (#1).
**Where:** `FeedScreen.tsx` `pick()` `onError` (line ~164) just sets `phase:'idle'` + error.
**Fix:** On a *permanent* failure (`receiverNotVisible`, `409` profile-gone), quietly drop the
card and advance instead of trapping the user. Keep the retry behaviour for transient/network
errors. This dovetails with adding Block/Report as alternative exits.
**Effort:** S.

### 4. Country is unvalidated free text — ✅ RESOLVED: internal + IP-derived (2026-06-05)
**Resolution:** Final shape (**ADR-028**, superseding ADR-027): the onboarding country *input* is
gone; country is **estimated server-side** via `IClientCountryResolver` — edge header
(`CF-IPCountry`) first, then an **offline GeoIP database** (`MaxMind.Db` over a DB-IP Lite `.mmdb`,
fetched with `make geoip`, gitignored) looked up on the client IP — and stored on
`profiles.country_code` **internal-only**: never on the feed card, public profile, or any response,
only read by the feed's same-country eligibility rule and included in the user's own data export. The "hide me from people in my own country"
(`hide_from_country`) privacy toggle is back on onboarding + You. Distance stays geopoint-only.
(Bio was removed in the same change — see #7.) Backend + frontend build clean; unit/arch +
integration (incl. a raw-SQL-seeded same-country test) green.

**Original finding —** **What:** Onboarding country is a 2-char free-text input (`"RO"`), uppercased and truncated
client-side, with no list or validation. Distance/country feed rules key off `country_code`,
so a typo silently mis-buckets a user.
**Where:** `web/notice/src/features/notice/OnboardingFlow.tsx:181-184`.
**Fix:** Replace with an ISO-3166 country picker (searchable select). Validate server-side too.
**Effort:** S–M (need a country list; it's static data).

### 5. No photo reordering / "make primary" in onboarding — ✅ FIXED (2026-06-05)
**Resolution:** Each occupied photo slot now shows a tag — a coral "★ Primary" badge on the
lead photo, and a "Make primary" pill on the others that calls the existing
`useUpdatePhotoOrder` (PUT /profile/photos/order), moving that photo to position 0 and keeping
the rest in order. Full drag-to-reorder was deferred as lower-value; "make primary" is the move
that actually matters for the feed. (`OnboardingFlow.tsx`, `notice.css`.)

**Original finding —** **What:** Photos are shown in fixed positional slots `photoList[0..2]`; the first is the feed
primary. There's no way to reorder or choose which photo leads. The backend already supports a
two-phase position shuffle (see M2/M3 notes), so this is a UI-only gap.
**Where:** `OnboardingFlow.tsx:140-174`.
**Fix:** Add drag-to-reorder or a "make primary" affordance; call the existing reorder endpoint.
**Effort:** M.

### 6. Appeals require hand-typed IDs; no pending-appeals list
**What:** The admin Appeal resolver makes the admin paste an *Appeal id* and *Target profile
id* by hand. Nothing in the dashboard lists open appeals, so an admin can't discover an appeal
without an out-of-band channel.
**Where:** `web/notice/src/features/admin/AdminDashboard.tsx:320` (`AppealResolver`).
**Fix:** Add a "Pending appeals" list (needs an admin list endpoint if one doesn't exist) and
let the admin act on a row, pre-filling the IDs.
**Effort:** M (likely needs a small backend read endpoint).

### 7. Reconcile the founder's feed-direction notes (mostly built — confirm & trim) — ✅ RESOLVED (2026-06-05)
**Resolution:** Confirmed realized. The anon nudge, magic-link signup=signin, minimal
name/gender/interests card, and full-bleed photo + corner-FAB tray are all built. The feed-DTO
trim decision is now made: `countryCode` and `bio` were both **removed** from the card DTO (and
`bio` dropped product-wide, column and all — ADR-028); `verified` / `distanceBucket` are **kept
deliberately** — cheap signals reserved for a near-term verified badge and distance display, passed
through the strategy seam rather than re-plumbed later. CLAUDE.md's stale `HPN-BACKBONE.md` / `§16`
references were also fixed (folds in #13). Rename of `docs/Design` still tracked under #12.

**Original finding —** **What:** `docs/Design` is a founder product note (not clutter). Auditing against the code,
most of it is **already done**, which is worth recording so it isn't re-litigated:
- *Pulsing nudge for anon users* → **done**: `Chrome.tsx` `AppHeader` shows the "Be noticed
  back" nudge with a halo for `anon`, the gear for members.
- *Signup doubles as sign-in* → **inherently done**: it's a magic link, so one form is both.
  (Optional: copy that says "Sign in or create your profile" to make it explicit.)
- *Feed shows only name / gender / interests, no country/distance/bio* → **done** in
  `FeedScreen`. But the feed DTO still carries `bio`, `countryCode`, `verified`, and a distance
  bucket the UI never renders. Decide: trim `FeedProfileDto` to what's shown (less data over
  the wire, tighter privacy surface), or keep them for a future detail view and note why.
- *Minimal/full-screen photo, react options from a corner icon* → **done**: the `+` FAB opens
  the trait tray over a full-bleed card.
**Where:** `docs/Design`; `FeedScreen.tsx`; `FeedProfileDto`.
**Fix:** Mark the note as realized; the only live decision is whether to trim the feed DTO.
Rename the file (see #12).
**Effort:** S.

---

## P2 — Tech debt, refactors, cleanups

### 8. Feed handler duplicates ~100 lines between member and guest paths
**What:** `GetFeedNextHandler.HandleAsync` and `HandleForGuestAsync` are nearly identical:
same candidate sampling, same big profile→`FeedProfileDto` projection (photos + interests
subqueries), same "restore strategy order" dictionary dance. Only the viewer context and
distance args differ.
**Why it matters:** Any change to the card shape (e.g. trimming the DTO per #7, adding a field)
must be made twice and can drift. This is the most duplicated logic in the backend.
**Where:** `src/Hpn.Modules.Feed/Internal/Features/GetNext/GetFeedNextHandler.cs:101-150` vs
`193-239`.
**Fix:** Extract a private `Task<IReadOnlyList<FeedProfileDto>> ProjectAndOrderAsync(
IReadOnlyList<Guid> selectedIds, double? viewerLat, double? viewerLng, string? viewerCountry,
CancellationToken)` and call it from both. Both `HandleAsync`/`HandleForGuestAsync` collapse to
viewer setup → eligibility → sample → strategy → `ProjectAndOrderAsync`.
**Effort:** S. Behaviour-preserving; covered by `FeedFlowTests`.

### 9. `Toggle` component is copy-pasted
**What:** The exact same `Toggle` function (incl. markup and `aria-pressed`) is defined in both
`YouScreen.tsx:201` and `OnboardingFlow.tsx:255`.
**Fix:** Move it to `web/notice/src/features/notice/ui.tsx` and import in both.
**Effort:** S.

### 10. CI doesn't run the frontend linter
**What:** `.github/workflows/ci.yml` runs `npm run build` (which does `tsc -b`, so types are
checked) but never `npm run lint`. ESLint config and `react-hooks` rules exist but aren't
enforced — lint can rot.
**Where:** `.github/workflows/ci.yml` frontend job.
**Fix:** Add a `npm run lint` step after install.
**Effort:** S.

### 11. Zero frontend tests
**What:** No vitest / testing-library; the only confidence is `tsc` + manual. The feed has real
stateful logic worth covering: the reward state machine (`beatDone`/`saved` gating in
`FeedScreen.pick`) and the queue dedupe/refill (`useFeedQueue` `seen` set, exhaustion).
**Fix:** Add vitest + a couple of unit tests for `useFeedQueue` (dedupe, refill threshold,
exhaustion) and the reward gating. Wire into CI.
**Effort:** M.

### 12. Repo-root clutter and an extensionless doc
**What:** Untracked in the repo root: `Notice.zip` (a 56 KB design-handoff archive) and a
`Notice/design_handoff_notice` directory. And `docs/Design` is a real founder note with no
extension (easy to mistake for a stray file or a directory).
**Fix:** `.gitignore` or relocate the handoff archive/dir under `docs/` (or drop it if it's a
one-off export). Rename `docs/Design` → `docs/design-notes.md` and link it from `docs/README.md`.
Also reconcile the `backlog.md` move (root → `docs/backlog.md`) that's still uncommitted in
`git status`.
**Effort:** S.

### 13. CLAUDE.md points at docs that don't exist — ✅ FIXED (2026-06-05)
**Resolution:** CLAUDE.md now points at `docs/architecture.md` + `docs/decisions.md` (the ADR
log) as the source of truth, and the `§16` / `§2` / `§16.2` references were replaced with ADR
pointers (e.g. ADR-014 for product principles, the "Deferred" section for infra). No `§`
references remain in CLAUDE.md.

**Original finding —** **What:** CLAUDE.md repeatedly cites `HPN-BACKBONE.md`, its `§16 decisions log`, and
`MILESTONES.md` as the source of truth, but the real docs are `docs/architecture.md` and
`docs/decisions.md` (ADRs). New contributors (and agents) are sent to missing files.
**Fix:** Update CLAUDE.md's pointers to the actual `docs/` files; replace `§N` backbone
references with the corresponding ADR numbers where practical.
**Effort:** S. (Consider running the `claude-md-improver` skill.)

---

## P3 — Polish & smaller wins

### 14. Shared date-formatting helpers
`relativeTime` (`Panels.tsx:11`) and `formatDate` (`AdminDashboard.tsx:417`) are bespoke per
file. Minor: a tiny `lib/format/date.ts` would centralize them. Effort S.

### 15. PWA manifest has no icons
`vite.config.ts` `VitePWA` manifest ships `icons: []`. An installed PWA with no icon looks
broken on a home screen. Add at least a 192/512 maskable icon. Effort S.

### 16. Feed reward gating could be a small reducer
The `beatDone`/`saved` closure flags + `flyIfReady` in `FeedScreen.pick` work but are easy to
get subtly wrong on edit. A 4-state `useReducer` (`idle→reacting→flying→done`) would make the
"whichever lands last" intent explicit. Low value; only if the file is being touched anyway.
Effort S.

### 17. No index on `profile.status` — the feed's foundational filter
**What:** Every feed fetch starts with `WHERE status = 'active'` (`GetFeedNextHandler.cs:263`),
but `profiles.status` has no index (`ProfileConfiguration.cs` only indexes `Id`/`UserId`). As
the table accumulates draft/paused/under_review/banned/deleted rows, the eligibility scan reads
them all and filters in memory.
**Why it matters:** At MVP scale it's invisible; it degrades linearly with non-active rows. A
partial index keeps the hot path proportional to *active* profiles, which is what the feed
actually browses. The other feed sub-filters are already index-backed (`visibility_preferences`
PK = `ProfileId`; `user_blocks` PK covers both block directions; appreciation dedupe uses the
`(SenderUserId, ReceiverProfileId, CategoryId)` unique index).
**Fix:** Add a partial index, e.g. `CREATE INDEX ix_profiles_active ON profile.profiles (id)
WHERE status = 'active';` (via an EF `HasIndex(...).HasFilter("status = 'active'")` migration).
**Effort:** S. Verify with `EXPLAIN` on a seeded DB before/after.

### 18. `ORDER BY random()` over the whole eligible set won't scale (forward-looking)
**What:** The candidate sample is `eligible.OrderBy(random()).Take(200)`. Postgres must sort
the *entire* eligible set on a random key to take the top 200, every fetch.
**Why it matters:** Fine now; at large eligible counts this becomes the feed's dominant cost.
The handler's own comment already anticipates a future strategy widening this input, so this is
a known seam, not a surprise.
**Fix (when needed):** `TABLESAMPLE SYSTEM`, a random-threshold (`WHERE random() < k` then cap),
or a precomputed shuffled cursor. Keep it behind the eligibility/strategy seam so callers don't
change. Defer until load testing says so.
**Effort:** M, later.

### 19. Tabs aren't real routes — URL never updates, back/forward dead
**What:** `react-router-dom` is a dependency and `app/router.tsx` registers `/received`,
`/me/fingerprint`, `/me/style`, `/you`, `/settings`, `/profile` (all rendering `<Home/>` so a
deep-link/refresh doesn't 404). But the actual tab is **local `useState`** in `NoticeApp`;
`initialTab()` reads the path once on mount and `setTab` never updates the URL. Consequences:
- Navigating feed → you leaves the URL at `/`; a refresh then lands back on the feed.
- Browser back/forward doesn't move between tabs.
- `/me/style` is registered but `initialTab()` doesn't map it, so it silently falls to the feed.
**Where:** `web/notice/src/features/notice/NoticeApp.tsx:34-86`; `web/notice/src/app/router.tsx`.
**Fix:** Either drive the tabs through the router (real routes + `<NavLink>`), or at minimum
`history.replaceState` the path on tab change so refresh/share/back behave. Drop or wire the
unused `/me/style` route. Effort: M (router) / S (replaceState stopgap).

### 20. The feed read path is the only hot endpoint with no rate limit
**What:** Every write path opts into a limiter (`appreciation`, `reports`, `uploads`,
`magic-link`, `guest-start`), but `GET /feed/next` has only `RequireAuthorization(GuestOrMember)`
— no `RequireRateLimiting`. It's also the most expensive query (eligibility scan + `ORDER BY
random()` + per-card photo/interest subqueries). A guest can mint a session (20/15min) and then
pull the feed in a tight loop unbounded.
**Why it matters:** It's the cheapest way to load the DB. The limiter table already exists;
this is a one-line opt-in plus a policy.
**Where:** `src/Hpn.Modules.Feed/Internal/Features/GetNext/GetFeedNextEndpoint.cs:26`.
**Fix:** Add a `RateLimitPolicies.Feed` partitioned per actor (member id / guest id, IP fallback)
— e.g. a generous fixed window — and `.RequireRateLimiting(...)` on the endpoint.
**Effort:** S.

### 21. No i18n — all copy is hardcoded English (P3, forward-looking)
**What:** Every user-facing string is inline English across `FeedScreen`, `Panels`, `YouScreen`,
`OnboardingFlow`, etc. The product is explicitly global (country codes, coarse location,
"people abroad"), and the owner is not a native English speaker.
**Why it matters:** Retrofitting i18n later means touching every component. Even just routing
copy through a tiny `t()` indirection now keeps strings extractable.
**Fix:** Decide if/when localization matters. If yes-eventually, introduce a minimal message
catalog seam early so new strings land there. Don't pull in a heavy i18n framework speculatively.
**Effort:** L (and deferrable) — flag, don't build yet.

### 22. Confirm OpenTelemetry/Serilog have a prod sink story
`Program.cs` only adds the OTLP exporter when `OTEL_EXPORTER_OTLP_ENDPOINT` is set, and Serilog
writes to console only. Fine for now, but verify the deploy sets the endpoint and that console
logs are shipped, so prod isn't silently unobservable. Effort S (config/ops, not code).
