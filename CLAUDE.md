# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

HPN (internal codename, *human-perception-network*) is an **appreciation-first social platform**; the user-facing product is **Notice**. Backend is a **.NET 10** modular-monolith Web API; frontend is a Vite + React PWA in `web/notice`.

**`docs/architecture.md` and `docs/decisions.md` (the ADR log) are the source of truth** for architecture and product decisions. When this file and those docs disagree, the docs win. If you change a decision, append/supersede an ADR in `docs/decisions.md` **in the same change** (append-only — supersede rather than rewrite). The MVP is fully implemented and merged to `main`; the numeric tuning constants called out in the ADRs (each named and centralized in code) are the only values expected to change with real data — don't invent *new* product decisions silently, flag them.

## Commands

Backend (run from repo root):
- Build (warnings are errors): `dotnet build HPN.sln -c Release`. The build is memory-heavy; if it gets OOM-killed, add `-maxcpucount:1`.
- All tests: `dotnet test HPN.sln` — **integration tests need Docker** (Testcontainers starts a real Postgres on a random port).
- One project: `dotnet test tests/Hpn.Modules.Moderation.Tests/Hpn.Modules.Moderation.Tests.csproj`
- One test / class / namespace: `dotnet test tests/Hpn.IntegrationTests/Hpn.IntegrationTests.csproj --filter "FullyQualifiedName~AdminFlowTests.Admin_can_review_queue"`
- Run the API: `dotnet run --project src/Hpn.Api` (auto-applies migrations in Development only).

Frontend (`cd web/notice`):
- `npm run dev` · `npm run build` (`tsc -b && vite build`) · `npm run lint` (eslint) · `npm run typecheck`.
- **Don't hand-write DTO types.** The typed client is generated from OpenAPI into `src/lib/api/generated/schema.ts` (gitignored). Regenerate after any API change: `make gen-api` (builds `Hpn.Api` → emits `src/Hpn.Api/openapi/Hpn.Api.json` → `openapi-typescript`). openapi-typescript emits int32 as `null | number | string` — coerce with `Number(...)`.

Local infra & dev:
- `make up` / `make down` — docker compose: Postgres `55432`, MinIO `19000`/`19001`, Mailpit UI `18025` (SMTP `11025`, catches magic-link emails), Caddy proxy `18080`. `make dev` brings up infra + API + web.

Migrations (one DbContext per module, schema-per-module):
- `dotnet ef migrations add <Name> --project src/Hpn.Modules.<Module> --startup-project src/Hpn.Modules.<Module> --output-dir Internal/Persistence/Migrations`
- **Always pass `--output-dir Internal/Persistence/Migrations`** — this repo keeps migrations (and the model snapshot) there, not in EF's default `Migrations/` folder. Scaffolding doesn't need a live DB. Prod migrations are a gated deploy step — never auto-migrate prod on boot.

## Architecture invariants (violating these is a bug)

- **Modular monolith, one deployable.** `src/Hpn.Api` is the only host; it wires modules and shared middleware and contains **no business logic**. Each module is one project (`src/Hpn.Modules.<Name>`); everything is `internal` **except** its `Contracts` namespace.
- **Never reach into another module's internals.** Depend only on `Hpn.Modules.<Other>.Contracts` + `Hpn.SharedKernel`. The `Hpn.ArchitectureTests` project (NetArchTest) fails CI if any module references another's `.Internal` namespace. If it fails, fix the design — don't weaken the test.
- **Schema-per-module.** A module owns its `snake_case` Postgres schema and migrations. **No cross-schema foreign keys** — reference other modules' rows by `uuid`, enforce integrity in app logic.
- **Writes are strictly isolated**: a command mutates only its own module's tables. **Cross-schema reads are allowed only inside a sanctioned read model** (a keyless query type via `FromSqlInterpolated`, or a Postgres view) — never ad hoc.
- **No Dapper, no mediator library, no background worker in v1.** EF Core everywhere; DI handlers + endpoint filters; all processing synchronous in-request. Wanting any of these is an ADR conversation (`docs/decisions.md`), not a quiet addition.

## Modules & how they talk

Modules (each owns the schema of the same name): `Identity`, `Profile`, `Photo`, `Appreciation`, `Feed`, `SocialFingerprint`, `Moderation`, `Admin`, `Notification`. `Hpn.SharedKernel` holds the cross-cutting glue (events, `ICurrentUser`, `IAccountDataContributor`, shared moderation/account events, `RateLimitPolicies`, `ApiRoutes`, the validation endpoint filter). The host calls each module's `Add<Module>Module()` + `Map<Module>Endpoints()`.

Cross-module collaboration uses four patterns — recognise them before adding a fifth:

1. **Read through a contract.** Each module exposes a read-only `I<Module>Api` (e.g. `IProfileApi`, `IModerationApi`). Writes never appear on a contract; they stay in the module's vertical slices. A referencing module's `.csproj` lists the other module's project but touches **only** its `Contracts`.
2. **In-process domain events** (`IDomainEventDispatcher` / `IDomainEventHandler<T>`) dispatched **synchronously inside the request scope** (handlers share the request's DbContext/transaction). No async/queue path exists in v1.
3. **Shared events for cross-module workflows live in `Hpn.SharedKernel`, not a module's Contracts**, to avoid project cycles. Examples: `AccountDeletionRequested` (Identity → Profile, Photo, …) and `UserRestricted`/`UserBanned`/`UserCleared` (Moderation → Profile). The receiving module reflects the effect into **its own** schema — e.g. Profile flips `profile.status` to `under_review`/`banned`, so the Feed eligibility query (which already filters `status = 'active'`) honours moderation with zero knowledge of the Moderation module.
4. **Account export/erasure fans out via `IAccountDataContributor`** (SharedKernel): each module implements one contributor over its own schema; an orchestrator in Identity resolves the `AccountScope` once and invokes them all, preserving write isolation. **Any new module that stores personal data must register a contributor.**

### Things the system deliberately does *not* use a worker for
There is no background processing. Two-phase work is done as **explicit, gated maintenance steps** (same posture as gated prod migrations), driven directly in tests: `AccountPurgeService.PurgeDueAsync` (hard-delete after the deletion grace window) and `RestrictionExpiryService.ReleaseExpiredAsync` (release a 48h temp restriction). Fingerprint snapshots are written opportunistically on read.

### The feed is built to change — keep it that way
Feed = **eligibility (stable hard filters) → `IFeedRankingStrategy` (volatile ordering/selection)**. v1 is `RandomWithinEligibleStrategy`. New ranking behaviour (boosts, fairness, freshness, A/B) is a **new strategy implementation only** — never edit the eligibility query, the `IFeedApi` contract, or callers to change ranking; pass new signals through the strategy's inputs.

### Moderation & trust
Trust score is computed on demand from cross-module signals (account age, photo, verified, engagement, upheld actions) and cached in `account_trust` via an `INSERT … ON CONFLICT` upsert. Reports drive **weighted report pressure**; crossing the threshold auto-applies a **temporary** restriction + review-queue entry — **never an automatic ban**. Bans/clears are admin/system decisions written **only** through `moderation_actions`.

## Conventions

- **Vertical slice = a folder under `Internal/Features/<UseCase>/`** containing Endpoint + Handler + Validator + Request/Response. Endpoints: authenticated (except auth/landing), validated via `.WithValidation<T>()` (FluentValidation endpoint filter), ownership-checked, RFC 9457 `ProblemDetails` errors.
- `record` DTOs; nullable reference types on; warnings as errors. Central Package Management — add/upgrade versions in `Directory.Packages.props`.
- Base path `/api/v1` (`ApiRoutes.Prefix`); `camelCase` over the wire; **cursor pagination, never offset**; `Idempotency-Key` honoured on `POST /appreciations`.
- DB enums are stored as `snake_case` **strings** (a `*Format` helper with `ToStorageValue`/`Parse` + EF `HasConversion`), not Postgres enum types. `EFCore.NamingConventions` maps the rest.
- Auth: email magic link → opaque, revocable **server-side session** over an httpOnly Secure SameSite cookie. The SPA never handles tokens. Admin endpoints gate via an endpoint filter checking `IIdentityApi.IsAdminAsync`.
- Storage: binaries live behind `IObjectStore` (MinIO dev / R2 prod, config-swapped) — never in a module's DB. Validate + re-encode + strip EXIF on every uploaded image.

## Product principles that constrain code (see `docs/decisions.md` ADR-014) — enforce in code and UX

- **Positive-only, appreciation-gated.** No dislike/skip/negative action exists anywhere; advancing the feed requires choosing an appreciation.
- **No comparison or ranking surfaces.** No public counts, scores, or leaderboards. Fingerprint/received use perception phrasing ("People often perceive…"), gated behind **≥20** received appreciations.
- **Privacy & data minimization.** No race/body/height/income; age de-emphasized. **Coarse location only** (geopoint rounded to 0.1°), never precise coordinates; distance shown only in coarse buckets. Pause / delete / export / block / report must always work and be easy to reach.
- Not a dating app; must not *feel* preachy, therapeutic, fake-positive, or competitive.

## Testing

- **Integration tests over real Postgres via Testcontainers are the primary confidence layer** (no SQLite substitution) — each test class spins its own `PostgreSqlContainer` + `WebApplicationFactory<Program>`; integration runs are serialized (`CollectionBehavior(DisableTestParallelization=true)`). Drive gated services (purge, restriction expiry) directly with the clock advanced.
- Unit tests for domain/pure logic live in per-module `tests/Hpn.Modules.<Module>.Tests`. Architecture tests guard the boundary. The gate is "critical flows have integration coverage and the architecture tests pass."
- A module's `.csproj` adds `InternalsVisibleTo Hpn.IntegrationTests` (and its own `.Tests`) when tests need to drive internal services or substitute fakes (e.g. a recording `IObjectStore`).

## Naming (do not mix)
Code, repo, namespaces (`Hpn.*`), DB schemas, infra → **HPN**. User-facing app, copy, branding, the `web/notice` client → **Notice**.

## When in doubt
Prefer the simplest thing that respects the boundary rules and product principles. Don't add infrastructure (Redis, workers, new libraries) speculatively — the ADRs defer these deliberately (`docs/decisions.md`, "Deferred" section). Surface the decision instead of guessing on anything covered by an ADR. Commit/push only when asked; branch off `main` first.
