# MILESTONES.md — HPN ("Notice") build playbook

Companion to `HPN-BACKBONE.md` (source of truth) and `CLAUDE.md` (standing rules). One milestone at a time, in order. A milestone is **done** only when every box is checked, tests pass, and CI is green.

## How an agent runs a milestone
1. Read `CLAUDE.md`, then the backbone sections listed under the milestone.
2. Implement only this milestone's scope. Don't pull future work forward.
3. Build backend + matching frontend together; write tests as you go (not after).
4. Stop at the "Done when" checklist and report status box-by-box. If something can't be met, surface it — don't work around an architecture rule.

## Rules that apply to every milestone (don't re-litigate)
- Respect module boundaries (`internal` everything except `Contracts`; no cross-schema FKs; reads-join-only-in-read-models).
- New use case = a vertical slice under `Internal/Features/` (Endpoint + Handler + Validator + Request/Response).
- Every endpoint: authenticated (except auth/landing), validated, ownership-checked, RFC 9457 errors.
- Every milestone adds: unit tests for new logic + at least one integration test (real Postgres via Testcontainers) for the happy path + key failure; architecture tests stay green.
- No new infrastructure or libraries beyond what the backbone names, without flagging first.

---

## M0 — Scaffolding
**Goal:** empty but fully wired skeleton everything else slots into.
**Backbone:** §3, §4, §5, §10.7–10.9, §13, §14.
**Done when:**
- [ ] Solution + all module projects exist per §5.1; namespaces `Hpn.*`; `Hpn.Api` is the only host.
- [ ] `Directory.Build.props` (nullable on, warnings-as-errors) + `Directory.Packages.props` (CPM) in place.
- [ ] Each module exposes `Add<Module>Module()` + `Map<Module>Endpoints()`; host wires them and shared middleware only.
- [ ] EF naming conventions (snake_case) + per-module `DbContext` with `HasDefaultSchema` configured (schemas may be empty).
- [ ] Problem-details, rate limiter, and the synchronous in-process event dispatcher registered.
- [ ] `docker-compose.yml`: postgres + minio (+ bucket init) + mailpit + proxy. `web/notice` Vite app boots.
- [ ] OpenAPI document served; frontend client generation script runs.
- [ ] CI runs build + unit + **architecture tests** + Testcontainers integration harness; all green.
- [ ] `dotnet run` starts the API; `npm run dev` starts the SPA; one script brings up everything.

## M1 — Identity / Auth
**Goal:** a user can sign in via magic link and hold a session.
**Depends on:** M0. **Backbone:** §6.1, §7.2, §8 (Auth), §10.1, §11.
**Done when:**
- [ ] `POST /auth/magic-link` issues a single-use, hashed, ~15-min token; emails via `IEmailSender` (Resend impl; Mailpit in dev); always 202 (no enumeration); throttled per email + IP.
- [ ] `POST /auth/verify` consumes token, creates a server-side session, sets httpOnly/Secure/SameSite cookie.
- [ ] `GET /me` returns user + onboarding state; `POST /auth/logout` revokes session.
- [ ] Sessions are sliding-expiry and revocable; expired/consumed tokens rejected.
- [ ] Frontend: landing page + magic-link verify route + authenticated shell.
- [ ] Integration tests: full request→verify→/me→logout; expired/reused token paths.

## M2 — Profile creation
**Goal:** an authenticated user can create and edit a profile and activate it.
**Depends on:** M1. **Backbone:** §6.2, §7.3, §8 (Profile), §2 (data minimization).
**Done when:**
- [ ] `PUT /profile` upserts display name, gender (+ self-describe), country, bio; `GET /profile/me` + `GET /profiles/{id}` (visibility-checked public projection).
- [ ] Interests reference list seeded; `PUT /profile/interests`, `GET /interests`.
- [ ] `visibility_preferences` row created with safe defaults; status lifecycle (`draft→active→paused`) via `PUT /profile/status`.
- [ ] No prohibited fields collected (race/body/height/income); age de-emphasized.
- [ ] Frontend: onboarding steps (account→gender→…) and profile edit; copy reflects "not a dating app".
- [ ] Integration tests: create/edit/activate; visibility projection hides fields from non-permitted viewers.

## M3 — Photo upload
**Goal:** a profile can have validated, processed photos and reach min-1-to-activate.
**Depends on:** M2. **Backbone:** §6.3, §7.4, §8 (Photos), §10.2, §11.
**Done when:**
- [ ] `POST /profile/photos` (multipart): MIME+magic-byte+size validation → ImageSharp decode → **EXIF/metadata stripped** → display+thumb variants → WebP re-encode → `content_hash` → object storage → `photos` row `status=ready`.
- [ ] `DELETE /profile/photos/{id}`, `PUT /profile/photos/order`; primary = position 0.
- [ ] Profile cannot activate with 0 ready photos; `verified` flag exists (admin-set only).
- [ ] NSFW hook present as a no-op pass-through (phase-2 seam).
- [ ] Frontend: upload + crop/preview + reorder.
- [ ] Integration tests (MinIO container): upload→metadata gone→variants exist→stored; rejects bad type/oversize.

## M4 — Feed eligibility + ranking seam
**Goal:** `GET /feed/next` returns eligible profiles, with ranking behind a swappable strategy.
**Depends on:** M3. **Backbone:** §6.5 (read this carefully), §8 (Feed), §3.1 (read models).
**Done when:**
- [ ] **Eligibility query** (read model) excludes: self, non-active, blocked (both directions), audience/gender mismatch, country/distance-filtered, already-appreciated; respects `women_for_women` and `verified_only`.
- [ ] **`IFeedRankingStrategy`** defined; v1 `RandomWithinEligibleStrategy` registered in DI; `GetNext` = eligibility → strategy.select → batch.
- [ ] Swapping the strategy requires touching **only** a strategy class — verified by a test that registers a fake strategy and asserts ordering changes without touching eligibility or the contract.
- [ ] Recency: already-appreciated excluded via `appreciation_events`; session-level dedupe of recent shows (no `feed_impressions` table).
- [ ] Frontend: feed card shell consuming a prefetch batch.
- [ ] Integration tests: each eligibility rule; batch shape; strategy-swap test.

## M5 — Appreciation submission (the core loop)
**Goal:** appreciating unlocks the next profile; counters + style update in one transaction.
**Depends on:** M4. **Backbone:** §6.4, §7.5, §8 (Appreciation), §3.4.
**Done when:**
- [ ] 12 categories seeded; `GET /appreciation-categories`.
- [ ] `POST /appreciations` (accepts `Idempotency-Key`): validates sender≠receiver, receiver visible, not duplicate `(sender,receiver,category)`; persists event; **in the same transaction** increments `received_appreciation_stats` and `given_appreciation_stats` and raises `AppreciationCreated`.
- [ ] Browsing is gated: the feed advances only after a successful appreciation; no skip/dislike anywhere.
- [ ] Frontend: appreciation chooser; on submit, prefetch queue serves next card instantly.
- [ ] Integration tests: submit→counters updated→next unlocked; duplicate rejected; idempotency replays safely.

## M6 — Received appreciation
**Goal:** users see appreciation received, in perception phrasing.
**Depends on:** M5. **Backbone:** §6.4, §8 (`/appreciations/received`), §9.4, §2.
**Done when:**
- [ ] `GET /appreciations/received` returns summarized counts + phrasing ("People often describe…"), optional individual events.
- [ ] No raw popularity dashboard, no scores/rankings; copy verified against §2/§9.4.
- [ ] Frontend: received view.
- [ ] Tests: aggregation correctness; phrasing not numeric-ranking.

## M7 — Social Fingerprint + Appreciation Style
**Goal:** perception summary (gated) + what-you-notice insights.
**Depends on:** M6. **Backbone:** §6.6, §7.7, §8 (`/fingerprint`, `/appreciation-style`), §10 phrasing.
**Done when:**
- [ ] `GET /fingerprint/me`: below 20 received → `{status:"insufficient_data", needed}`; at/above → live distribution + top traits computed from stats; opportunistic weekly snapshot written for trend (no job).
- [ ] `GET /appreciation-style/me`: user's category mix vs platform average, phrased as insight.
- [ ] Presented as interpretive ("People often perceive…"), never objective/score.
- [ ] Frontend: fingerprint + style dashboards (radar/distribution per PRD).
- [ ] Tests: threshold gate; distribution math; snapshot written once per period.

## M8 — Privacy & settings
**Goal:** every trust control works and is easy to reach.
**Depends on:** M2+. **Backbone:** §7.3, §8 (Settings), §10.4, §10.5, §11.
**Done when:**
- [ ] `PUT /settings/visibility`: all toggles (outside-country, hide-from-country, min-distance, women-for-women, verified-only, pause) — and they actually affect the feed.
- [ ] Block from feed/report contexts; blocks honoured both directions.
- [ ] `POST /settings/account/delete` (two-phase soft→hard purge incl. object storage); `GET /settings/account/export` (GDPR).
- [ ] Coarse geopoint captured with explicit consent; distance shown in buckets only.
- [ ] Frontend: settings screens.
- [ ] Tests: each toggle changes eligibility; deletion purges; export completeness.

## M9 — Reporting & moderation
**Goal:** reports intake, trust-weighted auto-restriction, review queue, ban logic.
**Depends on:** M8. **Backbone:** §6.7, §7.8, §8 (Reports), §10.3.
**Done when:**
- [ ] `POST /reports` (rate-limited, dedup per reporter/target/type).
- [ ] `account_trust` computed per §10.3 formula on relevant events.
- [ ] Weighted report-pressure auto-applies a **48h temp restriction + review queue entry** at the §10.3 threshold; **never auto-bans**; restricted users excluded from feed.
- [ ] Ban/clear only via `moderation_actions`.
- [ ] Tests: trust math; threshold triggers restriction not ban; restriction expiry; low-trust reporter has little effect.

## M10 — Admin tools
**Goal:** minimal internal review surface.
**Depends on:** M9. **Backbone:** §6.8, §7.9, §8 (Admin), §11 (audit).
**Done when:**
- [ ] Admin-role gate on all `/admin`; `GET /admin/queue`, `/admin/reports`, `/admin/stats`; `POST /admin/profiles/{id}/action`, `/admin/appeals/{id}/resolve`.
- [ ] Every admin action writes `admin_audit_log`.
- [ ] Minimal internal UI (separate from the Notice PWA shell).
- [ ] Tests: authz (non-admin blocked); actions recorded + auditable.

---

## Phase 2 (after MVP, not now)
NSFW scanning + async worker · automated selfie verification · Redis (caching + multi-node rate limiting) · opt-in leaderboards · perception-based compatibility · richer AI-profile detection · Capacitor wrapper · monetization. Each is a §16.3 deferral — pick up only when MVP is validated.

## Tuning backlog (set defaults now, adjust with real data)
Trust-score weights · auto-restriction threshold constants · distance buckets · appreciation-category list refinements · fingerprint snapshot adequacy. None block building.
