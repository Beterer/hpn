# CLAUDE.md — HPN ("Notice")

Standing instructions for any agent working in this repo. Read this first, every task.

## What this is
HPN (internal codename, from *human-perception-network*) is an **appreciation-first social platform**. Public product name shown to users: **Notice**. The single fixed technical constraint is a **.NET 10 Web API**; the rest of the architecture is specified in **`HPN-BACKBONE.md`** — that document is the source of truth. When this file and the backbone disagree, the backbone wins; if you change a decision, update the backbone's §16 decisions log in the same change.

## Before you build
1. Read the relevant section(s) of `HPN-BACKBONE.md` for the area you're touching.
2. Find the current milestone in §15 — build in that order; don't pull future-milestone work forward without reason.
3. The former open questions (categories, trust weights, distance, email provider, verification, feed_impressions) now have concrete defaults in the backbone — implement those. Only the **numeric tuning constants** are expected to change with real data; don't treat them as blockers, and don't invent *new* product decisions silently — flag those.

## Naming (do not mix)
- Code, repo, namespaces (`Hpn.*`), DB schemas, infra → **HPN**.
- User-facing app, copy, branding, the `web/notice` client → **Notice**.
- DB: `snake_case` tables/columns (via `EFCore.NamingConventions`), one schema per module, lowercase module name.

## Architecture rules (these are invariants — violating them is a bug)
- **Modular monolith.** One deployable. Each module is one project; everything `internal` **except** its `Contracts` namespace.
- **Never reach into another module's internals.** Depend only on `Hpn.Modules.<Other>.Contracts` + `Hpn.SharedKernel`. Cross-module reads go through contract interfaces.
- **Schema-per-module.** A module owns its schema and migrations. **No cross-schema foreign keys**; reference other modules' rows by `uuid`, enforce integrity in app logic.
- **Writes are strictly isolated** — a command mutates only its own module's tables. **Reads may join across schemas, but only inside a sanctioned read model** (query type or Postgres view), not ad hoc.
- **No Dapper, no mediator library, no background worker in v1.** EF Core everywhere; DI handlers + endpoint filters; all processing synchronous in-request. If you think you need any of these, that's a §16 conversation, not a quiet addition.
- The architecture test project enforces the boundary in CI. If it fails, fix the design, don't weaken the test.

## The feed is built to change — keep it that way
Feed = **eligibility (stable) → `IFeedRankingStrategy` (volatile)**. Hard filters live in the eligibility query; *how candidates are ordered/chosen* lives behind the strategy interface. v1 is `RandomWithinEligibleStrategy`. New behavior (priority/boosts, fairness, freshness, A/B) = a **new strategy implementation only** — never edit eligibility, the `IFeedApi` contract, or callers to change ranking. Pass new signals through the strategy's inputs.

## Product principles that constrain code (see backbone §2)
Enforce these in implementation and UX — they are not optional polish:
- **Positive-only, appreciation-gated.** No dislike/skip/negative action exists anywhere. Advancing the feed requires choosing an appreciation.
- **No comparison or ranking surfaces.** No public counts, no scores, no leaderboards. Fingerprint/received use perception phrasing ("People often perceive…"), gated behind ≥20 received appreciations.
- **Privacy & data minimization.** No race/body/height/income; age de-emphasized. Coarse location only — never store precise coordinates. Pause/delete/export/block/report must always work and be easy to reach.
- Not a dating app; must not *feel* preachy, therapeutic, fake-positive, or competitive.

## Stack quick reference (rationale in backbone §4)
- Backend: .NET 10, minimal APIs + endpoint groups, RFC 9457 `ProblemDetails`, EF Core 10, PostgreSQL, FluentValidation (endpoint filter), Mapperly (mapping; read models use hand-written `Select` projections), ImageSharp.
- Auth: email magic link → opaque, revocable **server-side session** over an httpOnly Secure SameSite cookie. The SPA never handles tokens.
- Frontend: `web/notice` — Vite + React + TypeScript PWA, React Router, TanStack Query (incl. feed prefetch), React Hook Form + Zod, Tailwind, shadcn/ui. **API client is generated from OpenAPI** — don't hand-write DTO types.
- Storage: `IObjectStore` over S3 client — MinIO (dev) / Cloudflare R2 (prod), config-swapped.
- Hosting: cloud-neutral, Docker Compose, single VPS for MVP.

## Conventions
- `record` DTOs; nullable reference types on; warnings as errors.
- Central package management (`Directory.Packages.props`) — add/upgrade versions there.
- Cursor pagination (never offset). `Idempotency-Key` on `POST /appreciations`.
- New use case = a vertical slice folder under the module's `Internal/Features/` (Endpoint + Handler + Validator + Request/Response).
- A module exposes `Add<Module>Module()` + `Map<Module>Endpoints()`; the host (`Hpn.Api`) only wires modules and shared middleware — no business logic in the host.

## Testing (backbone §14)
- Unit tests for domain/handlers; **integration tests over real Postgres via Testcontainers** are the primary confidence layer (no SQLite substitution); architecture tests guard the module boundary.
- Frontend: Vitest + React Testing Library; Playwright for the critical onboarding→upload→appreciate→fingerprint flow.
- The gate is "critical flows have integration coverage," and the architecture tests pass.

## Local dev
`docker compose up` brings up Postgres + MinIO + Mailpit (catches magic-link emails locally) + proxy. Run `dotnet run --project src/Hpn.Api` and `npm run dev` in `web/notice`. Migrations auto-apply in Development; in prod they're an explicit, gated deploy step — never auto-migrate prod on boot.

## Secrets & safety
No secrets in the repo. Env-injected everywhere; `dotnet user-secrets` for dev. HTTPS only, secure cookies, validate + re-encode + strip metadata on every uploaded image, rate-limit auth/appreciation/reports/uploads. Moderation uses trust score **plus** report volume — never auto-permanent-ban on report count alone.

## When in doubt
Prefer the simplest thing that respects the boundary rules and the product principles. Don't add infrastructure (Redis, workers, new libraries) speculatively — the backbone defers these deliberately. Surface the decision instead of guessing on anything in §16.2.
