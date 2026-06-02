# HPN — Backbone Requirements ("Notice")

> **Status:** v1 complete — backbone for MVP implementation. Open items tracked in §16.
> **Internal codename:** HPN (human-perception-network) · **Public product name:** Notice
> **Audience:** implementation agents (Claude Code) and human reviewers.

---

## Locked Decisions (approved)

These are settled. The rationale for each is expanded in §4.

| Area | Decision |
|---|---|
| API runtime | **.NET 10 Web API** (fixed, non-negotiable) |
| Architecture | Modular monolith, single deployable |
| Database | PostgreSQL + EF Core 10 across the board (no Dapper in v1; reads use no-tracking + DTO projections) |
| Hosting | Cloud-neutral / self-host — Docker Compose on a single VPS for MVP; path to k8s |
| Object storage | S3-compatible — Cloudflare R2 (prod), MinIO (local dev) |
| Email | Transactional provider behind a swappable interface (Resend or SES) |
| Auth | Email **magic link** (MVP) |
| Frontend | Pure SPA — Vite + React + TypeScript, installable PWA (not Next.js) |
| Background jobs | **None in v1** — all work is synchronous (incl. fingerprint aggregation). The only real async case, NSFW photo scanning, is deferred to phase 2 |
| Location | User-declared country + optional coarse, rounded geopoint; no precise coordinates stored |
| Gender / audience | Small gender set (woman / man / non-binary / self-describe) + separate "audience preferences" (powers women-for-women mode) |
| Caching | Redis for feed candidates + rate limiting (introduced when justified, not day one) |
| Realtime | Polling/refresh in MVP; no SignalR yet |
| Social Fingerprint gate | Show after ≥ 20 received appreciations |
| Appreciation categories | Fixed curated set, config-seeded (not user-configurable in MVP) |
| Verification | Optional, not required at MVP |
| AI-photo detection | Lightweight: report + trust-score + manual review; no automated model gating |
| Naming | .NET root namespace `Hpn.*` (TBD vs `HumanPerceptionNetwork.*`) |

---

## 1. Overview & Naming

HPN is an **appreciation-first social platform**: people interact with each other's photo profiles only through positive, categorized appreciation — there is no dislike, no public comments, no follower system, no direct messaging, and no traditional matching in the MVP. To keep browsing, a user must appreciate a specific quality about the current profile. Over time each user accrues a **Social Fingerprint** (how others perceive them) and an **Appreciation Style** (what they tend to notice in others). The product's premise is that *appreciation reveals as much about the observer as the observed* — so it is explicitly **not a dating app** and avoids comparison, scores, and rankings.

This document is the technical backbone the implementation agents build against: it fixes architecture, stack, module boundaries, the data model, the API and frontend shape, and the build order. Per-milestone detail is produced as each milestone is implemented (§15).

**Naming:** the codebase, repository, namespaces, schemas, and infrastructure use **HPN** (from *human-perception-network*; .NET root namespace `Hpn.*`). Everything users see — the client app, copy, branding — uses the public product name **Notice**. The two never mix: no "HPN" in user-facing UI, no "Notice" in code identifiers.

The single fixed technical constraint is that the API is a **.NET 10 Web API**. Every other choice in this document was made on current best-practice grounds and is recorded with its rationale.

## 2. Product Principles & Non-Goals

These aren't aspirational copy — they are **implementation constraints**. Several map to concrete enforcement points elsewhere in this doc, and agents should treat violating them as a bug.

**Principles**
1. **Appreciation-gated, positive-only.** The only interaction is choosing a positive appreciation; advancing the feed requires it. There is no dislike, skip, or negative action anywhere in the system. (§9.4, §6.4)
2. **No comparison, no ranking.** No public like/follower counts, no attractiveness scores, no "your value is…". Fingerprint and received views use perception phrasing ("People often perceive…"). (§9.4)
3. **Emotional safety before exposure.** Onboarding makes *who can see you* clear before the first photo upload; privacy/audience controls are first-class, not buried. (§9.3, §7.3)
4. **Data minimization.** Collect only what participation needs. No race, body type, height, income, or detailed physical traits; age is de-emphasized in MVP. (§7, §10.5)
5. **Trust is the product.** Pause, delete, export, block, hide-by-geography, and report all work and are easy to reach. Moderation resists weaponization (trust score, not raw report counts). (§10.3, §10.5)
6. **Probabilistic, not authoritative.** The Social Fingerprint is interpretive perception data, gated behind a minimum sample (≥20), never presented as objective truth. (§6.6, §7.7)
7. **Self-host friendly, cost-conscious, modern.** Cloud-neutral, few dependencies, no premature infrastructure.

**Non-goals (MVP)** — direct messaging, comments, followers, native apps, public leaderboards, payments, advanced compatibility/recommendation engines, and complex AI-photo detection are all out of scope (some are phase-2 candidates, §16.3). The platform must also avoid *feeling* preachy, therapeutic, fake-positive, competitive, or dating-focused — a design constraint on copy and UX as much as on code.

## 3. Architecture

### 3.1 Style

HPN is a **modular monolith**: one deployable ASP.NET Core process containing several modules. One PostgreSQL database, but **one schema per module** (`identity`, `profile`, `appreciation`, …) so ownership is structural rather than a convention people forget. The boundary is enforced asymmetrically — strict on writes, pragmatic on reads (this is plain CQRS):

- **Writes are strictly isolated.** A command only ever mutates its own module's tables and enforces that module's invariants. No cross-module writes, ever. Cross-module reads needed *during* a write go through another module's **public contract** (interfaces + DTOs in `Hpn.<Module>.Contracts`), never by touching its tables.
- **Reads may join across schemas** — but only through explicitly owned **read models** (dedicated query types or Postgres views) that are the sanctioned place for cross-schema `SELECT`. The feed is one such read model: a single efficient query, not a fan-out of contract calls. Cross-schema joins are *possible but visible*, so they show up in review instead of accreting silently.
- **Cross-module side effects** are **domain events handled synchronously in-process** within the same request/transaction in v1. There is no background worker. The work these events trigger (counter updates, fingerprint recompute, appreciation-style projection) is cheap enough to run inline. If any handler later proves too slow for the request path, that specific event becomes the trigger to introduce a queue/worker — but v1 does not have one.

ORM is **EF Core 10 across the board** for v1 — including read models, using `AsNoTracking` + projections straight to DTOs. No Dapper. If a specific read ever becomes a measured bottleneck we have escape hatches (Postgres views, raw SQL through EF, or the feed prefetch-cache in §10) — but v1 does not pre-pay for them.

This keeps coupling low enough that a hot module could later be extracted into its own service without a rewrite (swap in-process contract calls for HTTP/queue behind the same interface), while avoiding the operational cost of microservices at MVP.

### 3.2 Module map

```
                         ┌─────────────────────────────────────────┐
        PWA (Notice)  ──▶ │            Hpn.Api (host)               │
        HTTPS / JSON      │  minimal-API endpoint groups + auth +   │
                          │  rate limiting + problem-details errors │
                          └───────────────────┬─────────────────────┘
                                              │ in-process calls (contracts)
   ┌──────────┬──────────┬──────────┬─────────┴───┬──────────┬───────────┬──────────┐
   ▼          ▼          ▼          ▼             ▼          ▼           ▼          ▼
Identity   Profile     Photo    Appreciation    Feed     Fingerprint  Moderation  Admin
 (auth)   (profiles,  (uploads, (events,       (discovery (perception  (reports,  (review
          visibility) thumbs,   gating,        eligibility analytics + trust score, tools)
                      scan hook) style metrics) + filters) style)      ban logic)
   │          │          │          │             │          │           │          │
   └──────────┴──────────┴────┬─────┴─────────────┴──────────┴───────────┴──────────┘
                              ▼
                  PostgreSQL  ·  Object storage (R2/MinIO)  ·  Redis (later)
```

Modules (detailed responsibilities in §6):

- **Identity** — accounts, magic-link auth, sessions/tokens, account deletion, anti-abuse signals.
- **Profile** — profile CRUD, display name, interests, gender/audience prefs, visibility settings, profile status lifecycle.
- **Photo** — upload, object-storage persistence, thumbnailing, type/size validation, metadata stripping, NSFW-scan hook.
- **Appreciation** — appreciation events, duplicate/abuse prevention, gating enforcement, received counts, appreciation-style metrics.
- **Feed / Discovery** — selects next eligible profiles honouring privacy, blocks, gender/audience, country/distance, recency.
- **Social Fingerprint** — aggregates received appreciation into perception summaries + trend; enforces the ≥20 threshold.
- **Moderation** — reports intake, trust scoring, review queue, automatic temporary restrictions, ban logic.
- **Admin** — internal dashboards over the above (review, appeals, stats).

Appreciation-style metrics live inside the **Appreciation** module (they derive from the same event stream), not a separate module — they're a read projection, not a new domain.

### 3.3 Communication rules

- **Synchronous, in-process:** module-to-module reads via injected contract interfaces (e.g. Feed asks Profile "is this profile active and visible to user X?").
- **In-process events (synchronous):** a module raises a domain event (e.g. `AppreciationCreated`); subscribers handle it inline within the same request/transaction. v1 has no background worker — fingerprint recompute, counter updates, and the appreciation-style projection all run synchronously. They're cheap, and keeping them in-transaction means no eventual-consistency gaps to reason about.
- **No shared mutable state** beyond the database and cache. No module reaches into another's `DbContext`.

### 3.4 Request flow

Standard write (submit an appreciation, which also gates browsing):

```
PWA ──POST /appreciations──▶ Api endpoint
   ▶ auth middleware (validate session token)
   ▶ rate limiter (per-user submit budget)
   ▶ Appreciation.SubmitAppreciation(command)  [single transaction]
        ├─ validate: sender ≠ receiver, receiver visible, not duplicate-abuse
        ├─ persist AppreciationEvent (Postgres)
        ├─ raise AppreciationCreated  ──▶ handled inline, same transaction:
        │        · increment receiver's appreciation counters
        │        · recompute receiver Social Fingerprint (if ≥20 threshold met)
        │        · update sender's appreciation-style projection
        └─ commit → return 201 + "next profile unlocked"
   ◀ PWA fetches GET /feed/next
```

Standard read (feed) stays synchronous and is the latency-sensitive path. In v1 it's a plain EF read model (no-tracking, projected). When it eventually matters, the intended optimization is a **prefetch-cache**: the API hands the client a batch of eligible profiles and refills the batch in the background while the user browses the current one — so `/feed/next` is usually a cache hit. This is deliberately deferred (§10/§16); v1 ships the simple query.

### 3.5 Deployment topology (MVP)

Single VPS, Docker Compose, these services:

- `api` — the single .NET 10 host (endpoints + all processing in-process; no separate worker in v1).
- `postgres` — primary datastore.
- `minio` — S3-compatible storage locally; R2 in prod (same `IObjectStore` abstraction, config-swapped).
- `redis` — added when caching/rate-limiting demand it, not day one.
- reverse proxy (Caddy/Traefik) terminating TLS and serving the built PWA static assets.

The PWA is built to static files and served by the proxy/CDN; it is **not** a Node server (see §9).

### 3.6 Why modular monolith (and the exit ramp)

Microservices would add network hops, distributed transactions, and ops overhead the MVP can't justify. A modular monolith gives one codebase, one deploy, in-process calls, and trivial local dev — while the contract + event boundaries mean a hot module (likely Feed or Photo) can be peeled off into its own service later by swapping in-process calls for HTTP/queue calls behind the same contract. We design for that seam now; we do not pay for it yet.

## 4. Tech Stack & Rationale

### 4.1 Selection principles

1. **.NET 10, modern idioms** — minimal APIs, source generators over runtime reflection, nullable reference types on, `record` DTOs.
2. **Best tool, licensing is not a constraint** — we use industry-standard libraries on technical merit. Some popular .NET libraries now have commercial tiers, but their free thresholds (typically multi-million-dollar revenue) sit far beyond this project's horizon, so licensing does not drive selection.
3. **Fewer dependencies** — prefer the framework and source generators to third-party runtime libraries. Each dependency is justified below; if the framework does it adequately, we don't add a library.
4. **No premature infrastructure** — Redis, background workers, and NSFW scanning are all deferred until a real need appears (§10, §16).

### 4.2 Backend stack

| Concern | Choice | License | Why |
|---|---|---|---|
| Runtime | .NET 10 | — | Fixed requirement. Current LTS; latest C#, minimal-API and source-gen improvements. |
| API style | Minimal APIs + endpoint groups | — | Less ceremony than controllers; pairs with the no-mediator handler approach; typed results. |
| Errors | RFC 9457 `ProblemDetails` | — | Standard machine-readable error shape across every endpoint. |
| ORM | EF Core 10 | MIT | Migrations are the deciding factor — one versioned schema story across all module schemas. No-tracking + projections for reads. |
| Database | PostgreSQL 16+ | OSI | Mature, free, strong JSON + indexing; fits self-host. |
| Dispatch | **None** (DI handlers + endpoint filters) | — | Vertical-slice handlers injected via DI, with cross-cutting logic in endpoint filters/middleware — fewer moving parts than a mediator. Swap in MediatR later if pipeline-behavior ergonomics are wanted. |
| Validation | FluentValidation | Apache-2.0 | Still free and maintained; run via an endpoint filter ahead of handlers. |
| Mapping | Mapperly (`Riok.Mapperly`) | Apache-2.0 | Source-generated, compile-time, no runtime reflection, debuggable output. Read models use hand-written `Select` projections so EF translates to SQL. |
| Auth | ASP.NET Core auth + custom magic-link issuer | — | See §10 for the magic-link flow and token model. |
| Object storage | `IObjectStore` over AWS SDK S3 client | Apache-2.0 | One abstraction, config-swapped between MinIO (dev) and Cloudflare R2 (prod). |
| Image processing | ImageSharp | — | Thumbnailing, compression, EXIF stripping. Industry standard for managed image processing on .NET. Config in §10. |
| Testing | xUnit v3 · Testcontainers · NSubstitute · FluentAssertions | — | Real-Postgres integration tests via Testcontainers (no SQLite substitution); standard unit + assertion tooling. |

### 4.3 Frontend stack

| Concern | Choice | Why |
|---|---|---|
| Framework | React + TypeScript | Mature, matches the PRD's intent; large hiring/agent familiarity. |
| Build/dev | Vite | Fast dev server + build to static assets; no Node server to host (unlike Next.js). |
| PWA | Vite PWA plugin (Workbox) | Installable, offline shell, service worker — meets the "installable to home screen" requirement. |
| Routing | React Router | Standard SPA routing. |
| Server state | TanStack Query | Caching, retries, background refetch — also underpins the feed prefetch pattern. |
| Forms + validation | React Hook Form + Zod | Zod schemas validate client-side and can be shared as the contract shape. |
| Styling | Tailwind CSS | Matches PRD; fast iteration. |
| Components | shadcn/ui (Radix) | Copy-in components, no heavy dependency, accessible primitives — fits the calm, custom aesthetic. |

### 4.4 Deliberate non-choices

- **Dapper** — not in v1 (see §3.1); EF Core everywhere.
- **Background job framework (Hangfire/Wolverine)** — no async worker in v1; all processing is synchronous. Revisited only for phase-2 NSFW scanning.
- **Next.js** — would require hosting a Node server beside the .NET API; a Vite SPA is simpler to self-host.
- **Microservices** — modular monolith instead (§3.6).
- **Redis** — deferred until caching/rate-limiting genuinely needs it.

## 5. Repository & Solution Layout + Naming Conventions

### 5.1 Repository root

Single repo, `HPN`. Backend solution and frontend app live side by side:

```
HPN/
├─ HPN.sln
├─ Directory.Build.props            # shared: <Nullable>enable</Nullable>, LangVersion latest, TreatWarningsAsErrors
├─ Directory.Packages.props         # central package version management (CPM)
├─ docker-compose.yml               # api · postgres · minio · (redis later) · proxy
├─ .editorconfig                    # formatting + analyzer rules
├─ src/
│  ├─ Hpn.Api/                      # the ONLY host/executable
│  ├─ Hpn.SharedKernel/             # cross-cutting primitives (no business logic)
│  ├─ Hpn.Modules.Identity/
│  ├─ Hpn.Modules.Profile/
│  ├─ Hpn.Modules.Photo/
│  ├─ Hpn.Modules.Appreciation/
│  ├─ Hpn.Modules.Feed/
│  ├─ Hpn.Modules.SocialFingerprint/
│  ├─ Hpn.Modules.Moderation/
│  └─ Hpn.Modules.Admin/
├─ tests/
│  ├─ Hpn.Modules.<X>.Tests/        # per-module unit tests
│  ├─ Hpn.IntegrationTests/         # WebApplicationFactory + Testcontainers (real Postgres)
│  └─ Hpn.ArchitectureTests/        # enforces module-boundary rules in CI
└─ web/
   └─ notice/                       # Vite + React + TS PWA (the "Notice" client)
```

### 5.2 The boundary is enforced by the compiler

Each module is **one project**. The boundary rule from §3.1 is enforced structurally, not by discipline:

- Everything inside a module is `internal` **except** its `Contracts` namespace, which is `public`.
- Because other modules reference the module *assembly*, `internal` types are invisible to them — the compiler physically prevents reaching into another module's domain, handlers, or `DbContext`.
- A module therefore depends only on `Hpn.Modules.<Other>.Contracts` + `Hpn.SharedKernel`. Never on another module's internals (there's nothing public to reference).
- `Hpn.ArchitectureTests` adds a CI backstop (e.g. NetArchTest) asserting no module references another module's non-contract namespace, and no two modules share a DbContext.

### 5.3 Inside a module (vertical slices)

```
Hpn.Modules.Appreciation/
├─ Contracts/                       # PUBLIC surface only
│  ├─ IAppreciationApi.cs           # what other modules may call
│  ├─ Dtos/                         # public DTOs
│  └─ Events/AppreciationCreated.cs # public domain events
├─ Internal/
│  ├─ Domain/                       # entities, value objects, invariants (internal)
│  ├─ Features/                     # one folder per use case (vertical slice)
│  │  ├─ SubmitAppreciation/        #   Endpoint + Handler + Validator + Request/Response
│  │  └─ GetReceived/
│  ├─ Persistence/
│  │  ├─ AppreciationDbContext.cs   # owns schema "appreciation"
│  │  ├─ Configurations/            # IEntityTypeConfiguration<T>
│  │  └─ Migrations/                # this module's migrations only
│  └─ AppreciationEventHandlers.cs  # in-process handlers for events it subscribes to
└─ AppreciationModule.cs            # AddAppreciationModule() + MapAppreciationEndpoints()
```

`AppreciationModule.cs` exposes two extension methods the host calls; the module wires its own DI and maps its own endpoint group. The host never reaches inside.

### 5.4 The host (`Hpn.Api`)

Thin. `Program.cs` only: builds the app, calls each module's `Add<Module>Module()` and `Map<Module>Endpoints()`, registers shared middleware (auth, problem-details, rate limiting, the in-process event dispatcher), and runs migrations on startup in dev. No business logic lives here.

### 5.5 Database: schema-per-module

- One physical Postgres database; **each module's `DbContext` sets `HasDefaultSchema("<module>")`** so its tables are namespaced (`appreciation.appreciation_events`, `profile.profiles`, …).
- **Migrations are per-DbContext**, generated and applied independently — a module's schema evolves without touching others'.
- Cross-schema reads happen only inside the sanctioned read models (§3.1); a read model is allowed a dedicated read-only context or raw `FromSql` against a Postgres view.

### 5.6 Naming conventions

| Thing | Convention | Example |
|---|---|---|
| .NET namespace root | `Hpn.*` | `Hpn.Modules.Profile.Contracts` |
| Projects | `Hpn.` prefix | `Hpn.Modules.Feed` |
| DB schema | lowercase module name | `appreciation` |
| Tables / columns | `snake_case` via `EFCore.NamingConventions` | `appreciation_events`, `created_at` |
| Public DTOs | `record`, suffix `Dto` / `Request` / `Response` | `ReceivedAppreciationDto` |
| Domain events | past-tense, in `Contracts/Events` | `AppreciationCreated` |
| Endpoints | REST groups per module (§8) | `/appreciations`, `/feed/next` |
| Frontend dir | product name, lowercase | `web/notice` |

> Public product name **Notice** appears in user-facing copy and the `web/notice` client. Internal code, repo, namespaces, and infra all use **HPN**.

## 6. Domain Modules

Each module lists its responsibilities, the **public contract** other modules may call, and the **entities it owns** (which become its schema in §7). Anything not in the contract is `internal`.

### 6.1 Identity
- **Responsibilities:** account creation, magic-link issuance/verification, session lifecycle, account deletion, role (member/admin), basic anti-abuse signals on auth.
- **Contract:** `IIdentityApi` — `GetUser(userId)`, `UserExists(userId)`, `IsAdmin(userId)`; events `UserRegistered`, `UserDeleted`.
- **Owns:** `users`, `magic_link_tokens`, `sessions`, `account_deletion_requests`.

### 6.2 Profile
- **Responsibilities:** profile CRUD, display name, gender + self-describe, interests, country, visibility/audience preferences, blocks, profile-status lifecycle (`draft → active → paused → under_review → banned → deleted`).
- **Contract:** `IProfileApi` — `GetPublicProfile(profileId, viewerId)`, `IsVisibleTo(profileId, viewerId)`, `GetStatus(profileId)`, `GetBlockedBy(viewerId)`; events `ProfileActivated`, `ProfilePaused`, `ProfileBlocked`.
- **Owns:** `profiles`, `interests`, `profile_interests`, `visibility_preferences`, `user_blocks`.

### 6.3 Photo
- **Responsibilities:** upload intake, validation (type/size), EXIF/metadata stripping, compression, thumbnail generation, object-storage persistence, photo ordering, photo status. Holds the **NSFW-scan hook** (a no-op pass-through in v1).
- **Contract:** `IPhotoApi` — `GetProfilePhotos(profileId)`, `GetPhoto(photoId)`, `GetPrimaryPhoto(profileId)`; events `PhotoUploaded`, `PhotoRemoved`.
- **Owns:** `photos` (metadata + object keys; binaries live in object storage, never the DB).

### 6.4 Appreciation
- **Responsibilities:** record appreciation events, enforce appreciation-gated browsing, prevent duplicate/abusive appreciation, maintain received-appreciation counters, maintain **appreciation-style** projections (what the sender tends to notice). Owns the seeded appreciation category set.
- **Contract:** `IAppreciationApi` — `HasAppreciated(senderId, profileId)`, `GetReceivedSummary(profileId)`, `GetAppreciationStyle(userId)`, `GetCategories()`; events `AppreciationCreated`.
- **Owns:** `appreciation_categories`, `appreciation_events`, `received_appreciation_stats`, `given_appreciation_stats`.

### 6.5 Feed / Discovery
- **Responsibilities:** select the next eligible profile(s) for a viewer honouring status, blocks, gender/audience preferences, country/distance rules, and recency (not recently shown/appreciated). Serves the appreciation-gated browse loop.
- **Contract:** `IFeedApi` — `GetNext(viewerId, batchSize)`; no events.
- **Owns:** no tables in v1 — recency suppression derives from `appreciation_events` + session-level dedupe (§7.6). Reads are a cross-schema read model over Profile/Photo/Appreciation/Blocks (sanctioned per §3.1).

> **Built to change.** The feed algorithm is expected to evolve substantially (e.g. prioritizing/boosting certain profiles), so the module separates two concerns from day one:
> - **Eligibility** — the hard filters that decide whether a profile *may* be shown (status, blocks, audience/gender, country/distance, recency). Stable; rarely changes.
> - **Ranking/Selection** — how eligible candidates are *ordered/chosen*. Volatile. This lives behind an `IFeedRankingStrategy` interface. v1 ships `RandomWithinEligibleStrategy`; future strategies (priority boosts, freshness weighting, fairness balancing, perception-based ordering) are new implementations registered in DI — **no change to eligibility, the contract, or callers**.
>
> `GetNext` = `eligibility query → candidate set → IFeedRankingStrategy.Select(candidates, viewerContext) → batch`. Any signals a future strategy needs (e.g. a profile `boost` weight, recency decay, or experiment bucket) are passed via the strategy's inputs, keeping the swap surface to a single class. The strategy is also the natural seam for A/B testing feed variants later.

### 6.6 Social Fingerprint
- **Responsibilities:** aggregate a profile's received appreciation into a perception summary (category percentages, top traits), compute trend over time, enforce the **≥20 received-appreciations** gate before exposing results. Computed synchronously on read; snapshots persisted opportunistically for trend.
- **Contract:** `ISocialFingerprintApi` — `GetFingerprint(profileId)` (returns "insufficient data" below threshold).
- **Owns:** `social_fingerprint_snapshots`.

### 6.7 Moderation
- **Responsibilities:** report intake, account **trust score**, review queue, automatic *temporary* restrictions (never automatic permanent bans), admin decisions, ban logic. Combines report count **and** trust score plus review — never report-count alone.
- **Contract:** `IModerationApi` — `GetTrustScore(userId)`, `IsRestricted(userId)`, `SubmitReport(...)`; events `UserRestricted`, `UserBanned`, `UserCleared`.
- **Owns:** `reports`, `moderation_actions`, `account_trust`.

### 6.8 Admin
- **Responsibilities:** internal dashboard surface over Moderation/Profile/Photo — review queue, suspicious/banned profiles, photo review, appeals, moderation stats. Admin-only authorization. Very basic in MVP.
- **Contract:** none consumed by other modules (top-level only).
- **Owns:** `admin_audit_log`.

## 7. Data Model

One PostgreSQL database, one schema per module (§5.5). `snake_case` names. All tables carry `id uuid` PKs (v7 UUIDs for time-ordering), `created_at`/`updated_at timestamptz`. Cross-schema FKs are avoided — references across modules are by `uuid` value, integrity enforced in application logic, not DB constraints (keeps modules independently migratable).

### 7.1 Enums (Postgres enum types, per owning schema)

- `profile.gender`: `woman · man · non_binary · self_describe`
- `profile.profile_status`: `draft · active · paused · under_review · banned · deleted`
- `photo.photo_status`: `processing · ready · rejected`
- `moderation.report_type`: `ai_generated · fake_profile · inappropriate_content · stolen_photos · spam · nsfw · harassment · underage`
- `moderation.report_status`: `open · reviewing · actioned · dismissed`
- `moderation.action_type`: `warn · temp_restrict · ban · clear`

### 7.2 `identity` schema

- **users** — `id`, `email citext unique`, `role` (`member|admin`, default member), `status` (`active|deactivated`), `created_at`, `last_login_at`.
- **magic_link_tokens** — `id`, `user_id`, `token_hash` (store hash, never raw), `expires_at` (≈15 min), `consumed_at nullable`, `requested_ip`. Single-use.
- **sessions** — `id`, `user_id`, `token_hash`, `expires_at` (sliding), `created_at`, `user_agent`, `ip`, `revoked_at nullable`. Opaque server-side session (see §10.1).
- **account_deletion_requests** — `id`, `user_id`, `requested_at`, `purge_after` (grace window), `state` (`pending|purged`).

### 7.3 `profile` schema

- **profiles** — `id`, `user_id` (1:1 with users), `display_name`, `gender`, `self_describe_text nullable`, `country_code nullable` (ISO-3166-1 alpha-2), `geopoint nullable` (coarse, rounded — see §10.4), `bio nullable`, `status profile_status`, `created_at`, `updated_at`.
- **interests** — `id`, `slug unique`, `label`. Seeded reference list.
- **profile_interests** — `profile_id`, `interest_id` (composite PK).
- **visibility_preferences** — `profile_id` (PK), `show_only_outside_country bool`, `hide_from_country bool`, `min_distance_km int nullable`, `women_for_women bool`, `verified_only bool`, `paused bool`. One row per profile.
- **user_blocks** — `blocker_user_id`, `blocked_user_id`, `created_at` (composite PK). Feed + visibility honour both directions.

### 7.4 `photo` schema

- **photos** — `id`, `profile_id`, `position smallint` (ordering, 0 = primary), `status photo_status`, `original_key`, `display_key`, `thumb_key` (object-store keys), `width`, `height`, `content_hash` (dedupe / stolen-photo signal), `scan_result nullable` (phase-2 NSFW), `created_at`. 1–~5 per profile; min 1 to activate.

### 7.5 `appreciation` schema

- **appreciation_categories** — `id`, `slug unique`, `label`, `sort_order`, `active bool`. **Seeded fixed set (v1, tunable):** `warm_smile` "Warm smile", `authentic` "Authentic", `stylish` "Stylish", `calming_energy` "Calming energy", `confident` "Confident", `expressive` "Expressive", `fun_energy` "Fun energy", `elegant` "Elegant", `trustworthy` "Trustworthy", `creative` "Creative", `kind` "Kind", `intelligent` "Intelligent-looking". Curated to stay multidimensional rather than appearance-only.
- **appreciation_events** — `id`, `sender_user_id`, `receiver_profile_id`, `category_id`, `photo_id nullable`, `created_at`. **Unique** `(sender_user_id, receiver_profile_id, category_id)` to prevent duplicate spam; index on `(receiver_profile_id)` and `(sender_user_id, created_at)`.
- **received_appreciation_stats** — `receiver_profile_id`, `category_id`, `count int`, `last_at` (composite PK). Incremented in the same transaction as the event (§3.4).
- **given_appreciation_stats** — `sender_user_id`, `category_id`, `count int` (composite PK). Powers appreciation-style insights vs. platform averages.

### 7.6 `feed` schema

- *(no table in v1)* — recency suppression is derived from `appreciation_events` (exclude already-appreciated profiles) plus **session-level dedupe** of recently-shown profiles. A persistent `feed_impressions` table is intentionally **not** built for v1; it's reintroduced only if cross-session repeat-suppression becomes a real problem.

### 7.7 `social_fingerprint` schema

- **social_fingerprint_snapshots** — `id`, `profile_id`, `period` (`weekly|monthly`), `period_start date`, `sample_size int`, `distribution jsonb` (category→percentage), `top_traits jsonb`, `created_at`. Unique `(profile_id, period, period_start)`. Written opportunistically on read when a new period rolls over; current fingerprint is computed live from `received_appreciation_stats`.

### 7.8 `moderation` schema

- **reports** — `id`, `reporter_user_id`, `target_profile_id`, `type report_type`, `note nullable`, `status report_status`, `created_at`, `resolved_at nullable`. Index on `(target_profile_id, status)`. Rate-limited per reporter to prevent abuse.
- **moderation_actions** — `id`, `target_user_id`, `action action_type`, `reason`, `actor` (`system|admin_user_id`), `expires_at nullable` (for temp restrictions), `created_at`.
- **account_trust** — `user_id` (PK), `score numeric` (0–1), `signals jsonb`, `updated_at`. Inputs: account age, photo verification, prior reports/actions, appreciation activity. Used with report count to decide auto temp-restriction (§10.3).

### 7.9 `admin` schema

- **admin_audit_log** — `id`, `admin_user_id`, `action`, `target_ref`, `metadata jsonb`, `created_at`. Every admin decision is auditable.

## 8. API Surface

### 8.1 Conventions
- Base path `/api/v1`. Version in the path; additive changes don't bump, breaking changes do.
- JSON only. `record` request/response DTOs. Field naming `camelCase` over the wire.
- Errors use **RFC 9457 `application/problem+json`** with a stable `type` URI, `title`, `status`, `detail`, and a `errors` map for validation failures.
- Auth via httpOnly session cookie (§10.1). Every endpoint except auth + landing is authenticated.
- Mutations that should be safe to retry (notably `POST /appreciations`) accept an `Idempotency-Key` header.
- List endpoints are cursor-paginated (`?cursor=&limit=`), never offset, to stay stable under inserts.
- Authorization: ownership checks on every resource; admin endpoints require the `admin` role.

### 8.2 Endpoint groups

**Auth** (`/auth`)
- `POST /auth/magic-link` — `{ email }` → 202; issues + emails a single-use link (always 202, never reveals account existence).
- `POST /auth/verify` — `{ token }` → sets session cookie, returns the user.
- `POST /auth/logout` — revokes the current session.
- `GET /me` — current user + onboarding state.

**Profile** (`/profile`)
- `PUT /profile` — upsert display name, gender/self-describe, country, bio.
- `GET /profile/me` · `GET /profiles/{id}` (public projection, visibility-checked).
- `PUT /profile/interests` · `GET /interests` (reference list).
- `PUT /profile/status` — activate / pause.

**Photos** (`/profile/photos`)
- `POST /profile/photos` — multipart upload → validated, stripped, compressed, stored; returns photo metadata.
- `DELETE /profile/photos/{id}` · `PUT /profile/photos/order` — reorder.

**Feed** (`/feed`)
- `GET /feed/next?limit=` — next eligible profile(s); supports the client prefetch batch.

**Appreciation** (`/appreciations`)
- `POST /appreciations` — `{ receiverProfileId, categoryId, photoId? }` (idempotent) → records appreciation, unlocks next profile.
- `GET /appreciations/received` — summarized received appreciation (counts + phrasing, see §10).
- `GET /appreciation-style/me` — what you tend to notice vs. platform average.
- `GET /appreciation-categories` — the seeded set.

**Social Fingerprint** (`/fingerprint`)
- `GET /fingerprint/me` — perception summary + trend, or `{ status: "insufficient_data", needed: N }` below threshold.

**Settings** (`/settings`)
- `PUT /settings/visibility` — all privacy/audience toggles.
- `POST /settings/account/delete` — start deletion (grace window). `GET /settings/account/export` — GDPR export.

**Reports** (`/reports`)
- `POST /reports` — `{ targetProfileId, type, note? }` (rate-limited).

**Admin** (`/admin`, admin role)
- `GET /admin/queue` · `POST /admin/profiles/{id}/action` (`warn|temp_restrict|ban|clear`) · `GET /admin/reports` · `GET /admin/stats` · `POST /admin/appeals/{id}/resolve`.

## 9. Frontend Architecture

The "Notice" client — `web/notice`, a Vite + React + TypeScript installable PWA. Built to static assets, served by the reverse proxy; it talks to `/api/v1` only.

### 9.1 Structure
```
web/notice/
├─ src/
│  ├─ app/                 # router, providers (QueryClient, auth), layout shells
│  ├─ features/            # mirrors backend modules: onboarding, profile, feed,
│  │                       #   appreciation, fingerprint, settings, report
│  │   └─ <feature>/       #   components + hooks + api calls colocated
│  ├─ lib/
│  │   ├─ api/             # typed client generated from the API's OpenAPI doc
│  │   └─ query/           # TanStack Query keys + hooks
│  ├─ components/ui/       # shadcn/ui primitives
│  └─ styles/              # Tailwind config + tokens
├─ public/                 # manifest.webmanifest, icons
└─ vite.config.ts          # Vite PWA plugin (Workbox)
```

### 9.2 Key decisions
- **Typed API client generated from OpenAPI** so frontend types track the backend; no hand-maintained DTOs. Zod schemas validate form input client-side; server remains source of truth.
- **TanStack Query** owns all server state (caching, retries, background refetch). It also implements the **feed prefetch**: `GET /feed/next?limit=N` fills a local queue; the next card is served from cache while the queue refills in the background, so the gated browse loop feels instant without server-side work.
- **React Router** for the screen map; **React Hook Form + Zod** for onboarding/profile forms.
- **Auth:** session cookie is httpOnly, so the SPA never handles tokens. A `GET /me` on load establishes session + onboarding state; the magic-link landing route posts the token to `/auth/verify`.

### 9.3 Screen map (PRD → routes)
Landing (`/`, unauth, explains the concept + "not a dating app") · Onboarding (`/onboarding`: account → gender → photos → visibility) · Feed (`/`, the primary appreciation-gated screen) · Received (`/received`) · Fingerprint (`/me/fingerprint`) · Appreciation Style (`/me/style`) · Settings/Privacy (`/settings`) · Report (modal). Admin is a separate minimal internal view, not part of the PWA shell.

### 9.4 UX guardrails (enforce the product philosophy in the UI)
- The feed cannot advance without choosing a positive appreciation — no skip/dislike affordance exists.
- Fingerprint and received views use perception phrasing ("People often perceive…"), never scores or rankings (§ vision doc). No public counts, no leaderboards.
- Onboarding states clearly *who can see you* before the first photo upload, to reduce vulnerability.

## 10. Cross-Cutting Concerns

### 10.1 Magic-link auth flow
1. `POST /auth/magic-link {email}` → find-or-create user, generate a high-entropy token, store **only its hash** with a ~15-min expiry, email the link. Always return 202 (no account enumeration). Throttled per email + per IP.
2. User clicks link → SPA route posts the token to `POST /auth/verify` → token looked up by hash, checked unexpired + unconsumed, marked consumed, a **server-side session** row is created and delivered as an **httpOnly, Secure, SameSite=Lax cookie** holding an opaque session id.
3. Sessions are sliding-expiry and revocable (logout, deletion, ban all revoke). Opaque server sessions chosen over JWTs for instant revocation and zero token-handling in the client.

### 10.2 Image pipeline (Photo module)
On upload: validate MIME + magic bytes + size cap → decode with **ImageSharp** → **strip all EXIF/metadata** (privacy: removes GPS) → generate `display` (bounded long edge) + `thumb` variants → re-encode (WebP) → compute `content_hash` → upload original/display/thumb to object storage → persist `photos` row `status=ready`. The **NSFW-scan hook** is a pass-through interface in v1; phase 2 implements it behind the same seam (§12).

> **Verification (v1):** the `verified` flag and the `verified_only` visibility toggle exist, but verification in v1 is **manual admin marking only** (so the toggle isn't dead and trusted accounts can be flagged). Automated selfie/liveness verification is deferred to phase 2; when added it sits behind a verification provider interface and contributes `+0.2` to trust score (§10.3).

### 10.3 Moderation & trust score
- Reports are low-friction but rate-limited per reporter; duplicate reports on the same target collapse.
- **Trust score** (`account_trust.score`, [0,1]) — starting formula, tunable: base `0.4`; `+0.2` for account age ramped linearly over 14 days; `+0.1` for a `ready` primary photo; `+0.2` if verified; `+0.1` for genuine engagement (≥10 appreciations given **and** ≥10 received); `−0.25` per upheld (`actioned`) moderation action; clamp to [0,1]. Recomputed when the relevant events fire.
- **Auto temp-restriction** uses *weighted report pressure*, never raw counts. Over a 7-day window: `pressure = Σ reporterTrust` across **distinct** reporters (so a trusted reporter counts more, a serial-dismissed reporter barely counts). Trigger when `pressure ≥ 1.5 + 2.0 · targetTrust` **and** `distinctReporters ≥ 3` → apply a 48h `temp_restrict` + enqueue for review. These constants are launch defaults to tune with real data.
- It only ever applies a *temporary* restriction + review. **Bans are always an admin/system decision** through `moderation_actions` — never automatic.

### 10.4 Location & distance
- Country is user-declared (ISO alpha-2), used for the country visibility rules.
- Distance uses a **coarse geopoint rounded to 0.1° (~11 km)** at capture, with consent — finer precision is never stored. `min_distance_km` filtering computes distance between these rounded points, and the UI surfaces distance only as **coarse buckets** (`nearby` / `<50 km` / `50–200 km` / `200+ km` / `different country`), never an exact number. This satisfies "minimum distance from me" without exposing real location.

### 10.5 Privacy / GDPR
Data minimization is a product principle (no race/body/height/income; age de-emphasized). Account deletion is a two-phase soft-delete → hard purge after a grace window (also purges object storage). `GET /settings/account/export` returns the user's data. Consent recorded for optional geopoint. Visibility controls (pause, hide-by-country, block) are first-class.

### 10.6 Rate limiting
Built-in ASP.NET Core rate limiter with per-policy limits: magic-link requests (tight, per email + IP), appreciation submits (per-user budget), reports (per reporter), uploads. Backed by in-memory for single-node v1; moves to Redis when multi-node (§ deferred).

### 10.7 Domain events
In-process dispatcher invoked within the handler's transaction (§3.3). Handlers are synchronous and must be fast; a slow handler is a signal to revisit async processing (§12), not to make the dispatcher async in v1.

### 10.8 Observability
**Serilog** structured JSON logging + **OpenTelemetry** traces/metrics (OTLP export, no vendor lock). `/health` + `/health/ready` endpoints. Request correlation ids. Minimal but present from day one.

### 10.9 Configuration & secrets
`appsettings.json` + environment variables (12-factor). Dev secrets via `dotnet user-secrets`; compose via a git-ignored `.env`. No secrets in the repo. Object-store, SMTP/email-provider, and DB creds are all env-injected.

## 11. Security & Privacy Baseline

Non-negotiable for v1:
- **HTTPS only**; HSTS at the proxy. Secure, httpOnly, SameSite cookies. No tokens in JS-reachable storage.
- **No passwords** in v1 (magic-link only) — removes a whole class of credential risk.
- **Authorization everywhere:** ownership checks on every resource mutation/read; admin role gate on `/admin`. Default-deny.
- **Input validation** on every endpoint via FluentValidation filters; reject unknown/oversized payloads.
- **Upload safety:** MIME + magic-byte + size validation, re-encode through ImageSharp (defuses malformed-image exploits), strip metadata, content-hash for stolen-photo/dedup signals.
- **Rate limiting & anti-abuse** on auth, appreciation, reports, uploads (§10.6); appreciation uniqueness constraint blocks spam.
- **PII minimization:** collect only what's needed; coarse location only; age de-emphasized; deletion + export supported (§10.5).
- **Secrets** out of the repo, env-injected; least-privilege object-store credentials scoped to the bucket.
- **Dependency hygiene:** CPM-pinned versions, automated vulnerability scanning in CI (`dotnet list package --vulnerable`, npm audit).
- **Abuse/trust model** (§10.3) so moderation can't be weaponized by mass-reporting.
- **Audit:** all admin actions logged (`admin_audit_log`).

Threats specifically tracked from the vision doc: AI-generated/fake profiles (trust score + reporting + optional verification), photo theft (content-hash + reporting), and cold-start trust (privacy controls front-and-center in onboarding).

## 12. Background / Asynchronous Processing

**v1 has none.** Everything — including received-appreciation counters, Social Fingerprint recompute, and appreciation-style projections — runs synchronously inside the request transaction (§3.3, §3.4). There is no worker process, no queue, no Hangfire. This is a deliberate simplification: the work is cheap, and staying in-transaction removes eventual-consistency edge cases.

**What would introduce async processing (phase 2):**
- **NSFW photo scanning** — the genuine async case. Calling an external/ML classifier on upload shouldn't block the user. When built, it slots behind the existing `INsfwScanner` hook in the Photo module (§10.2): upload returns `status=processing`, a worker scans and flips to `ready`/`rejected`, photos stay out of the feed until cleared.
- Any domain-event handler that becomes too slow for the request path (§10.7).
- Periodic Fingerprint snapshotting if opportunistic-on-read (§7.7) proves insufficient.

When that day comes, the recommended shape is a Postgres-backed queue + a dedicated `worker` container (the deployment topology in §3.5 already anticipates splitting it out). Not before.

## 13. Local Dev & Environments

### 13.1 docker-compose (local)
Services: `postgres` (16+), `minio` (+ a one-shot bucket-create init), `mailpit` (catches magic-link emails locally so no real provider is needed in dev), and the reverse `proxy`. The API and the Vite dev server run on the host for fast hot-reload during development; a `prod`-profile compose runs them as containers too.

### 13.2 Running it
- Backend: `dotnet run --project src/Hpn.Api` (applies migrations on startup in Development; serves `/api/v1`).
- Frontend: `npm run dev` in `web/notice` (Vite dev server, proxies `/api` to the backend).
- One `make dev` / script target brings up infra + both apps.

### 13.3 Migrations & seeding
- Per-module migrations (§5.5); a startup migrator applies all module contexts in dev. Prod applies migrations as an explicit deploy step, not silently on boot.
- **Seed data:** appreciation categories, interest reference list, and a couple of dev admin/test accounts — via an idempotent seeder run in Development.

### 13.4 Environments
| Env | Storage | Email | DB | Notes |
|---|---|---|---|---|
| Local | MinIO | Mailpit | compose Postgres | migrations auto-apply |
| Staging | R2 (separate bucket) | real provider (test mode) | managed/self-host Postgres | mirrors prod config |
| Prod | Cloudflare R2 | real provider | Postgres on VPS (backed up) | migrations gated in deploy; secrets from env |

All environment differences are configuration only — the same image runs everywhere. Daily Postgres backups + object-store lifecycle rules from launch.

## 14. Testing Strategy

Pragmatic pyramid — heavier on integration than is fashionable, because the risk here is in cross-module behavior and real SQL, not in isolated units.

- **Unit tests** (`Hpn.Modules.<X>.Tests`) — domain invariants and handler logic in isolation; NSubstitute for the few contract dependencies. Fast, no infrastructure.
- **Integration tests** (`Hpn.IntegrationTests`) — `WebApplicationFactory` + **Testcontainers** spinning up **real Postgres** (and MinIO where storage matters). Exercise endpoints end-to-end through real EF + migrations. This is the primary confidence layer; it catches the schema-per-module and read-model join behavior that mocks would hide. No SQLite substitution.
- **Architecture tests** (`Hpn.ArchitectureTests`) — assert the module boundary from §5.2 holds: no module references another's internals, no shared DbContext, contracts-only dependencies. Runs in CI so the boundary can't erode silently.
- **Frontend** — **Vitest** + React Testing Library for components/hooks; **Playwright** for a thin set of critical e2e flows (onboarding → upload → appreciate → see fingerprint). Mock Service Worker against the OpenAPI types.
- **Contract sanity** — because the API client is generated from OpenAPI, a CI step regenerates and diffs it to catch breaking API changes before the frontend does.

Coverage is a guide, not a gate; the gate is "critical flows have integration coverage." CI runs unit + architecture + integration (Testcontainers) on every PR.

## 15. Implementation Roadmap

Build order follows the PRD's priorities, sliced so each milestone is independently demonstrable. Backend and the matching frontend feature are built together per milestone.

**M0 — Scaffolding.** Solution + module skeletons (§5), `Directory.*.props`, CPM, compose (Postgres/MinIO/Mailpit/proxy), EF naming conventions, problem-details + rate-limiter + event-dispatcher in the host, OpenAPI + generated client, CI (build, unit, architecture, Testcontainers). Empty but wired.

**M1 — Identity / Auth.** Magic-link request + verify, sessions, `/me`, logout. Email via **Resend** behind `IEmailSender` (Mailpit in dev). Frontend: landing + magic-link verify route.

**M2 — Profile creation.** Profile CRUD, gender/self-describe, interests, country, status lifecycle. Frontend: onboarding steps + profile edit.

**M3 — Photo upload.** Upload → validate → strip metadata → compress → thumbnails → object storage; ordering; min-1-to-activate. NSFW hook as no-op. Frontend: upload + crop/preview.

**M4 — Feed eligibility.** The cross-schema read model honouring status, blocks, gender/audience, country/distance, recency, **split into a stable eligibility query + a pluggable `IFeedRankingStrategy`** (v1 = random within eligible). Frontend: feed card shell.

**M5 — Appreciation submission.** Gated browse loop, categories seeded, idempotent submit, uniqueness, counters + style projection in-transaction. Frontend: appreciation chooser + prefetch queue.

**M6 — Received appreciation.** Summaries with perception phrasing (no scores). Frontend: received view.

**M7 — Social Fingerprint.** Live compute from stats, ≥20 gate, opportunistic snapshots + trend. Frontend: fingerprint + appreciation-style dashboards.

**M8 — Privacy & settings.** All visibility/audience toggles, pause, block, account deletion + export. Frontend: settings.

**M9 — Reporting & moderation.** Report intake, trust score, auto temp-restriction + review queue, ban logic.

**M10 — Admin tools.** Minimal internal review/appeal/stats dashboard.

**Phase 2 (post-MVP):** NSFW scanning (async worker), verification hardening, Redis (caching + multi-node rate limiting), opt-in leaderboards, perception-based compatibility, richer AI-profile detection.

## 16. Decisions Log & Open Questions

### 16.1 Decisions made (this round)
- Modular monolith, **schema-per-module**, strict writes / joinable read models (plain CQRS). (§3.1)
- **EF Core 10 across the board** in v1; no Dapper. (§3.1)
- Cross-module boundary **compiler-enforced via `internal`**, single project per module, architecture test backstop. (§5.2)
- **No mediator library** — DI handlers + endpoint filters. (§4.2)
- Mapperly (mapping), FluentValidation (validation), minimal APIs + RFC 9457, xUnit/Testcontainers/NSubstitute/FluentAssertions (testing). (§4)
- **No background jobs in v1**; all processing synchronous, Fingerprint included. (§3.3, §12)
- Vite + React SPA PWA (not Next.js); generated OpenAPI client; TanStack Query feed prefetch. (§9)
- Magic-link auth with opaque, revocable **server-side sessions** over httpOnly cookies. (§10.1)
- Coarse, rounded geopoint for distance; never precise coordinates. (§10.4)
- **Feed split into stable eligibility + pluggable `IFeedRankingStrategy`** (v1 random) so the algorithm can change radically — priority/boosts, fairness, A/B — without touching eligibility or callers. (§6.5)
- Licensing is **not** a selection constraint — industry-standard libraries used on merit. (§4.1)
- MVP answers: Fingerprint gate **≥20**; categories **fixed, config-seeded**; verification **optional**; AI-photo detection **lightweight** (report + trust + review). (Locked Decisions)
- **Account export & erasure fan out through a shared `IAccountDataContributor`** (in `Hpn.SharedKernel.Accounts`): each module implements one contributor over *its own* schema; an orchestrator in Identity resolves the `AccountScope` once and invokes every contributor, so write isolation holds (no module touches another's tables). Any new module that stores personal data must register a contributor. Soft-delete is announced via the shared `AccountDeletionRequested` event (Profile hides the account from the feed at once). The **hard purge is a gated maintenance step** (`AccountPurgeService.PurgeDueAsync`), not a background worker — same posture as production migrations (§10.5, §12). (§M8)

### 16.2 Open questions — now resolved (defaults set; tune with data)
- **Email provider:** Resend for MVP, behind `IEmailSender` (SES later if cost/volume warrants). (§M1)
- **Trust-score formula + auto-restriction threshold:** concrete launch defaults defined in §10.3 — base 0.4 with weighted signals; weighted report-pressure trigger `pressure ≥ 1.5 + 2.0·targetTrust` and `≥3` distinct reporters → 48h restriction. Constants are meant to be tuned with real data.
- **Distance granularity:** geopoint rounded to 0.1° (~11 km); distance shown in coarse buckets, never exact. (§10.4)
- **Appreciation category set:** finalized 12-category seeded list with slugs in §7.5.
- **Fingerprint snapshot cadence:** opportunistic-on-read, no scheduled job in v1. (§7.7)
- **Verification:** manual admin-flag in v1; automated selfie/liveness deferred to phase 2; verified adds +0.2 trust. (§10.2)
- **`feed_impressions`:** not built in v1 — recency derived from `appreciation_events` + session dedupe. (§7.6)

The only items genuinely left to decide *with real usage* are the **numeric tuning constants** (trust weights, restriction threshold, distance buckets, category list refinements) — none block implementation; they have working defaults.

### 16.3 Explicitly deferred to phase 2+
NSFW scanning + async worker · Redis · SignalR/realtime · opt-in leaderboards · perception-based compatibility · advanced AI-profile detection · native app wrapper (Capacitor) · payments/monetization.

---

_End of backbone. This document defines structure and decisions; per-module detail (exact DTO fields, full migrations, endpoint contracts) is produced during each milestone in §15._
