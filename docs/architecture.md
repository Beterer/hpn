# Architecture

How Notice is built, as of the completed MVP (milestones M0–M10). This describes the
system as it actually exists in `main`; the *why* behind each choice is in
[decisions.md](decisions.md), cross-referenced as `ADR-NN`.

## 1. The shape of the system

Notice is a **modular monolith**: a single deployable .NET 10 Web API plus a React PWA.
The backend is split into modules that are isolated like services (own schema, own
internals, talk only through contracts) but deploy and run as one process and share one
database. See `ADR-001` (modular monolith), `ADR-002` (schema-per-module).

```
                          ┌─────────────────────────────┐
   Browser (PWA) ───────▶ │  Hpn.Api  (the only host)   │
   web/notice             │  wires modules + middleware │
                          └──────────────┬──────────────┘
                                         │  Add<Module>Module() / Map<Module>Endpoints()
        ┌───────────┬───────────┬────────┴───────┬────────────┬───────────┬───────────┐
        ▼           ▼           ▼                ▼            ▼           ▼           ▼
    Identity    Profile      Photo         Appreciation    Feed   SocialFingerprint  Moderation  Admin
        │           │           │                │            │            │           │         │
        └───────────┴───────────┴── one Postgres, one schema per module ──┴───────────┴─────────┘
```

The host (`src/Hpn.Api`) contains **no business logic**. It registers shared
cross-cutting middleware (problem details, rate limiter, auth, OpenTelemetry, the domain
event dispatcher) and calls each module's two public methods:

- `Add<Module>Module(IServiceCollection, IConfiguration)` — registers the module's
  services, DbContext, handlers, validators, and event handlers.
- `Map<Module>Endpoints(IEndpointRouteBuilder)` — maps the module's HTTP endpoints under
  `/api/v1`.

Everything else in a module is `internal`. The **only** types a module exposes are in its
`Contracts` namespace. The compiler enforces this (internals aren't visible across
projects), and `tests/Hpn.ArchitectureTests` fails CI if any module references another
module's `.Internal` namespace (`ADR-001`).

## 2. Modules and their dependencies

Each module owns the Postgres schema of the same name. Cross-module **project references
are Contracts-only** — a module's `.csproj` may reference another module's project, but it
may touch only that module's `Contracts` types (`I<Module>Api`, DTOs, events).

| Module | Owns schema | References (Contracts only) | Responsibility |
|---|---|---|---|
| `Identity` | `identity` | `Profile` | Accounts, magic-link auth, sessions, account deletion orchestration |
| `Profile` | `profile` | — | Profiles, interests, visibility preferences, blocks, coarse location |
| `Photo` | `photo` | `Profile` | Validated/processed photos, object-store blobs |
| `Appreciation` | `appreciation` | `Photo`, `Profile` | The 12 appreciation categories, the appreciation event + counters |
| `Feed` | `feed` | — (reads via its own read model) | Eligibility + ranking |
| `SocialFingerprint` | `social_fingerprint` | `Appreciation`, `Profile` | Perception summary + appreciation style |
| `Moderation` | `moderation` | `Appreciation`, `Identity`, `Photo`, `Profile` | Reports, trust score, restrictions, ban/clear |
| `Admin` | `admin` | `Identity`, `Moderation`, `Profile` | Internal review console + audit log |

`Hpn.SharedKernel` holds the glue every module may depend on: the domain-event
abstractions, `ICurrentUser`, `IModuleInitializer`, the account export/erasure contract
(`IAccountDataContributor`), the shared cross-module events (account + moderation
lifecycle), `RateLimitPolicies`, `ApiRoutes`, and the FluentValidation endpoint filter.

Two modules — **`Feed`** and **`Admin`** — reference no peer module yet read across
schemas. They do it through **sanctioned read models** (keyless EF entities mapped onto
other modules' tables / `FromSqlInterpolated` queries), never by calling contracts in a
loop. This is the deliberate exception to "depend only on Contracts," taken because feed
eligibility and the admin queue/stats are set-based reads that would be N+1 disasters as
per-row contract calls (`ADR-011`).

## 3. How modules collaborate

There are exactly four collaboration patterns. Recognise them before inventing a fifth.

### 3.1 Read through a contract
Each module exposes a **read-only** `I<Module>Api` (`IProfileApi`, `IAppreciationApi`,
`IPhotoApi`, `IIdentityApi`, `IModerationApi`, `IFeedApi`). Writes never appear on a
contract — they stay inside the owning module's vertical slices. So
`Appreciation` asks `IProfileApi.IsVisibleToAsync(...)` and `IPhotoApi.GetPhotoAsync(...)`
to validate a submission, but only the Profile/Photo modules write their own tables
(`ADR-003`). Moderation is the one contract that also exposes a *write-ish* method
(`ApplyAdminProfileActionAsync`) so Admin can drive a human decision without reaching
inside — but the actual writes still happen inside Moderation's own services (`ADR-017`).

### 3.2 In-process synchronous domain events
`IDomainEventDispatcher` dispatches an `IDomainEvent` to all `IDomainEventHandler<T>`
registered for its runtime type, **synchronously, in the raising request's DI scope** — so
a handler shares the same DbContext/transaction as the code that raised the event. There
is no queue, no async path, no worker (`ADR-006`, `ADR-007`). Example: submitting an
appreciation raises `AppreciationCreated` inside the transaction, and the counter
projection handler upserts the received/given stats atomically with the event row.

### 3.3 Shared events for cross-module workflows (in SharedKernel, not Contracts)
When module A's action must make module B react, and B already (or might) react too, the
event lives in **`Hpn.SharedKernel`**, not in A's `Contracts`. This avoids project
**cycles** and lets B subscribe without depending on A at all (`ADR-008`). The receiving
module reflects the effect **into its own schema**:

- `AccountDeletionRequested` (raised by Identity) → Profile marks the profile `deleted`,
  so it leaves the feed immediately, long before the hard purge.
- `UserRestricted` / `UserBanned` / `UserCleared` (raised by Moderation) → Profile flips
  `profile.status` to `under_review` / `banned` / back to `active`. The Feed eligibility
  query already filters `status = 'active'`, so **moderation reflects into the feed with
  zero coupling between Feed and Moderation** — Feed has never heard of Moderation.

This is why `Profile` references no peer module yet participates in deletion and
moderation workflows: it only depends on `SharedKernel` events.

### 3.4 Account export & erasure: `IAccountDataContributor`
GDPR export and the hard delete are inherently cross-cutting, but writes must stay
isolated. Each module registers **one** `IAccountDataContributor` over its own schema
(`Section`, `ExportAsync`, `EraseAsync`, an `IsAccountRoot` flag). An orchestrator in
Identity resolves the `AccountScope` (user id + profile id) **once** and fans out to every
contributor; Identity (the "account root") is erased last (`ADR-009`). **Any new module
that stores personal data must register a contributor** — otherwise its rows silently
survive a delete and leak into nothing on export.

## 4. A request, end to end

The standard request flow (`ADR-005`):

1. **Endpoint** (minimal API in `Internal/Features/<UseCase>/`) — authenticated by the
   session cookie, validated by the FluentValidation endpoint filter
   (`.WithValidation<T>()`), returns RFC 9457 `application/problem+json` on failure.
2. **Handler** — the use case. Reads/writes its own DbContext, calls other modules'
   `I<Module>Api` for cross-module reads, raises domain events inside its transaction.
3. **Domain** — entities with behaviour (`UserProfile.Activate`, `ModerationAction.Ban`,
   …); persistence is EF Core with hand-written `Select` projections for read models and
   Mapperly where a straight map is enough.

A **vertical slice** is the unit of change: one folder containing Endpoint + Handler +
Validator + Request/Response. New use case → new slice; don't spread it across layers.

## 5. The feed (built to change)

The feed is deliberately split so the part that changes often is isolated from the part
that rarely does (`ADR-010`):

```
eligibility query  ──▶  candidate set  ──▶  IFeedRankingStrategy.Select  ──▶  batch
   (stable)                                      (volatile)
```

- **Eligibility** is the stable half: the hard filters that decide whether a profile *may*
  be shown — active status, not self, not blocked (either direction), not already
  appreciated, has a ready photo, audience rules (women-for-women, verified-only),
  country/`min_distance` rules. These live in one query in the Feed module's read model.
- **Ranking** is the volatile half, fully behind `IFeedRankingStrategy`. v1 is
  `RandomWithinEligibleStrategy` (sample within eligible). New behaviour — boosts,
  fairness, freshness, A/B — is a **new strategy class only**. You never edit the
  eligibility query, the `IFeedApi` contract, or callers to change ranking; new signals
  are passed through the strategy's inputs.

Recency without an impressions table: already-appreciated profiles are excluded via the
`appreciation_events` anti-join, and the client passes a session-level "seen" list for
soft dedupe (`ADR-010`).

Distance is computed in-query as an **equirectangular approximation** over the
0.1°-rounded points (not great-circle `acos`, which produces `NaN` on identical points),
and the card carries only a coarse **bucket** (`nearby` / `under_50km` / `50_200km` /
`200km_plus` / `different_country`) — never an exact distance (`ADR-013`).

## 6. Moderation & trust

Reporting and moderation combine **report volume and trust and human review** — never
report count alone, and never an automatic ban (`ADR-016`).

- **Trust score** (`account_trust`, `[0,1]`) is computed on demand from cross-module
  signals — account age (Identity), a ready primary photo (Photo), verified flag
  (Profile), genuine engagement (Appreciation), and upheld actions (Moderation's own) —
  and cached via an `INSERT … ON CONFLICT` upsert. Launch formula: base `0.4`, `+0.2`
  ramped over the first 14 days, `+0.1` ready photo, `+0.2` verified, `+0.1` if ≥10 given
  **and** ≥10 received, `−0.25` per upheld action, clamped. It's recomputed at the
  moderation-relevant moments (a report on/by the user, an action against them), which
  keeps domain-event handlers fast.
- **Auto temp-restriction** uses *weighted report pressure*, not a raw count. Over a 7-day
  window, `pressure = Σ trust` across **distinct** reporters; trigger when
  `pressure ≥ 1.5 + 2.0 · targetTrust` **and** there are ≥3 distinct reporters → a **48h**
  `temp_restrict` + a review-queue entry. A trusted reporter counts for more; a low-trust
  one barely moves it.
- A temp-restriction is **temporary**. There's no worker, so release is a **gated
  maintenance step** (`RestrictionExpiryService.ReleaseExpiredAsync`) that records a
  `clear` action and raises `UserCleared` (`ADR-007`).
- **Bans and clears are written only through `moderation_actions`**, always by an
  admin/system decision, and reflected into the feed via the shared lifecycle events
  (§3.3). Admin appeal resolution reuses the same `clear` path: an *upheld* appeal lifts
  the restriction; a *dismissed* one only audits.

## 7. Auth & sessions

Passwordless, token-free-on-the-client (`ADR-012`):

1. `POST /auth/magic-link` issues a single-use, hashed token (~15 min TTL), emailed via
   `IEmailSender` (Resend in prod, captured by Mailpit in dev). Always `202` — no account
   enumeration. Throttled per email + IP.
2. `POST /auth/verify` consumes the token and creates an **opaque, server-side session**
   (a hashed token row), set as an httpOnly + Secure + SameSite cookie. **The SPA never
   sees or stores a token.**
3. Sessions are **sliding-expiry (30 days) and revocable**; logout and account deletion
   revoke them immediately. Admin endpoints are gated by an endpoint filter that checks
   `IIdentityApi.IsAdminAsync`.

## 8. Data model notes

- **Schema per module**, `snake_case` via `EFCore.NamingConventions`. **No cross-schema
  foreign keys** — other modules' rows are referenced by `uuid` and integrity is enforced
  in app logic (`ADR-002`).
- **Enums are stored as `snake_case` strings**, not Postgres enum types, via a `*Format`
  helper (`ToStorageValue`/`Parse`) + EF `HasConversion`. The wire/DB value is the stable
  contract; the C# name is free to refactor (`ADR-015`).
- **Writes are isolated; cross-schema reads happen only inside a sanctioned read model** —
  a keyless query type or a `FromSqlInterpolated` projection — never ad hoc (`ADR-003`,
  `ADR-011`).
- IDs are UUIDv7 (`Guid.CreateVersion7()`) so they're time-ordered for index locality;
  the dev seeder uses deterministic hash-based GUIDs so re-seeding is idempotent.
- Cursor pagination everywhere (never offset); `Idempotency-Key` honoured on
  `POST /appreciations`.

## 9. Storage & images

Binaries never live in a module's database. Photos go through an ImageSharp pipeline —
MIME + magic-byte + size validation, decode, **EXIF/metadata strip**, display + thumb
variants, WebP re-encode, content hash — and are stored behind `IObjectStore` (MinIO in
dev, Cloudflare R2 in prod, swapped by config). An NSFW hook exists as a no-op
pass-through seam for phase 2 (`ADR-019`).

## 10. Testing

The confidence layers (`ADR-020`):

- **Integration tests over real Postgres via Testcontainers** are the primary layer — each
  test class starts its own `PostgreSqlContainer` + `WebApplicationFactory<Program>`. No
  SQLite substitution; the SQL, migrations, and EF translation are exercised for real.
  Integration runs are serialized.
- **Unit tests** per module for pure logic (the trust formula, distribution math, domain
  invariants).
- **Architecture tests** guard the module boundary in CI.

## 11. Local development & seeding

`make dev` brings up Docker infra (Postgres `55432`, MinIO `19000/19001`, Mailpit `18025`,
Caddy `18080`) + the API + the PWA. Migrations auto-apply in Development; in production
they're a gated deploy step (never auto-migrate prod on boot).

In Development the app **seeds a realistic dataset on startup** via phased
`IDevelopmentDataSeeder`s (`ADR-021`) — a ready `test@notice.local` account, ~30 candidate
profiles with real processed photos, an active feed, and enough received appreciation to
unlock the social fingerprint. Seeding is Development-only, gated by a config flag,
idempotent, runs each seeder against only its own schema (mirroring the
`IAccountDataContributor` fan-out), and pushes images through the **real** photo pipeline.
