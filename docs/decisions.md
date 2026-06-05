# Decision log

Why the code looks the way it does. Each entry: the **context**, the **decision**, the
**rationale**, the **consequences**, and the **alternatives** weighed. Append-only —
supersede rather than rewrite.

Status legend: **Accepted** (in force) · **Superseded by ADR-NN** · **Deferred** (chosen
not to do yet).

Most numeric constants below are **launch defaults to tune with real data**, not
invariants; each is named and centralized in code.

---

## Foundations

### ADR-001 — Modular monolith with compiler-enforced boundaries
**Status:** Accepted.
**Context:** A greenfield social app with several clear bounded contexts (identity,
profile, photos, appreciation, feed, moderation…). Microservices would give isolation but
impose network calls, distributed transactions, and ops overhead on a one-person/MVP team.
A plain layered monolith would give simplicity but no real boundaries — everything ends up
reaching into everything.
**Decision:** One deployable host (`Hpn.Api`) composed of modules, where each module is a
separate project, everything is `internal` **except** a `Contracts` namespace, and modules
may depend only on other modules' `Contracts` + `Hpn.SharedKernel`. A NetArchTest suite
fails CI if any module references another's `.Internal` namespace.
**Rationale:** The compiler does most of the enforcement for free (cross-project internals
are invisible), so the boundary is real, not a guideline. We get service-like isolation
(own schema, own internals, contract-only coupling) with monolith simplicity (one process,
one transaction scope, in-process calls, one deploy). If a module ever needs to become a
service, the contract is already the seam — an exit ramp without an upfront tax.
**Consequences:** Cross-module calls go through `I<Module>Api`. Some duplication (DTOs at
the boundary) is accepted as the price of isolation. The architecture test is load-bearing
— if it fails, fix the design, don't weaken the test.
**Alternatives:** Microservices (rejected: premature, ops cost); single project with
folders (rejected: no enforceable boundary).

### ADR-002 — Schema-per-module, no cross-schema foreign keys
**Status:** Accepted.
**Context:** Modules share one Postgres instance but must stay decoupled enough to reason
about (and extract) independently.
**Decision:** Each module owns one Postgres schema and its own EF `DbContext` and
migrations. A module's tables never have a foreign key into another module's schema;
other modules' rows are referenced by `uuid` and referential integrity is enforced in
application logic.
**Rationale:** Cross-schema FKs would re-couple the modules at the database level (shared
migration ordering, cascade behaviour, lock contention) and block ever splitting a module
out. Referencing by id keeps each schema independently migratable and the ownership clear.
**Consequences:** No DB-level cascade across modules — e.g. deleting an account fans out
through `IAccountDataContributor` (`ADR-009`) instead of `ON DELETE CASCADE`. Integrity
bugs are possible if app logic is sloppy; tests cover the important paths.
**Alternatives:** Single shared schema (rejected: no ownership); cross-schema FKs
(rejected: re-couples, blocks extraction).

### ADR-003 — Writes isolated; cross-schema reads only in a sanctioned read model
**Status:** Accepted.
**Context:** Plain CQRS-lite. Commands and queries have different coupling needs: a command
should touch one module; a query sometimes needs data from several.
**Decision:** A command mutates **only its own module's tables**. Reads may join across
schemas, but **only inside a sanctioned read model** (a keyless EF query type or a Postgres
view / `FromSqlInterpolated` projection), never ad hoc in a handler.
**Rationale:** Isolated writes keep the "who can change this row" answer to exactly one
module, which is what makes the boundary trustworthy. Confining cross-schema reads to named
read models keeps those reads visible and reviewable instead of scattered joins.
**Consequences:** Feed eligibility and the Admin queue/stats are read models, not contract
calls (`ADR-011`). Counters (received/given appreciation stats) are projections maintained
by event handlers, not computed on every read.
**Alternatives:** Full event-sourcing/separate read store (rejected: overkill for MVP);
ad-hoc cross-schema joins (rejected: erodes the boundary invisibly).

### ADR-004 — EF Core everywhere; no Dapper
**Status:** Accepted.
**Decision:** One data-access stack — EF Core 10 — for both writes and reads, with
hand-written `Select` projections (and `FromSqlInterpolated` for the few set-based read
models) where performance matters.
**Rationale:** A second data stack (Dapper) buys marginal read performance at the cost of
two mental models, two migration stories, and two sets of mapping bugs. EF's raw-SQL
escape hatch covers the cases where the LINQ provider isn't enough.
**Consequences:** Read models use explicit projections to stay cheap; we accept EF's
translation quirks (and test against real Postgres, `ADR-020`, to catch them).
**Alternatives:** Dapper for reads (rejected: dual stack); micro-ORM throughout (rejected:
loses migrations + change tracking for writes).

### ADR-005 — DI handlers + endpoint filters; no mediator library
**Status:** Accepted.
**Decision:** Each use case is a plain handler class resolved from DI and invoked directly
by a minimal-API endpoint. Validation is a FluentValidation **endpoint filter**
(`.WithValidation<T>()`); errors are RFC 9457 `application/problem+json`. A use case is a
**vertical slice** folder: Endpoint + Handler + Validator + Request/Response.
**Rationale:** A mediator library (MediatR-style) adds indirection, reflection, and a
pipeline abstraction we don't need when the host already gives us DI and endpoint filters.
Direct handler calls are trivially navigable ("go to definition" lands on the code that
runs). Vertical slices keep a change in one folder instead of smeared across layers.
**Consequences:** No `IPipelineBehavior` cross-cutting hook — cross-cutting concerns are
endpoint filters or middleware instead.
**Alternatives:** MediatR (rejected: indirection for no gain at this size).

### ADR-006 — In-process synchronous domain events
**Status:** Accepted.
**Context:** Modules need to react to each other's writes (counters, feed exclusion,
deletion fan-out) without direct calls everywhere.
**Decision:** `IDomainEventDispatcher` resolves and invokes every
`IDomainEventHandler<T>` for an event's runtime type **synchronously, in the raising
request's DI scope**, so handlers share the same DbContext/transaction.
**Rationale:** Synchronous in-transaction handlers give us atomicity (the counter upsert
commits with the appreciation row) and simple reasoning (no eventual-consistency window
inside a request) without any messaging infrastructure. Handlers must be fast; a slow
handler is a signal to revisit async (`ADR-007`), not to make the dispatcher async.
**Consequences:** Cross-**module** event handlers use their *own* DbContext (separate
transaction), so a workflow spanning modules (e.g. moderation → profile status) is
eventually consistent across schemas, which is acceptable given no cross-schema FKs.
**Alternatives:** An outbox + message bus (deferred: real infra for a future scale need).

### ADR-007 — No background workers in v1; two-phase work is a gated maintenance step
**Status:** Accepted.
**Context:** Some work is naturally deferred: the hard delete after a grace window, and the
release of a temporary restriction after 48h.
**Decision:** v1 has **no background worker / scheduler**. All request processing is
synchronous. Deferred work is exposed as an **explicit, gated maintenance method** —
`AccountPurgeService.PurgeDueAsync(now)` and `RestrictionExpiryService.ReleaseExpiredAsync(now)`
— invoked deliberately (and driven directly in tests with the clock advanced), the same
posture as gated production migrations.
**Rationale:** A worker is real operational surface (hosting, scheduling, monitoring,
failure modes) we don't need to prove the product. Modeling deferred work as an idempotent,
time-parameterized method keeps it testable and lets a scheduler be added later by simply
calling it — without redesigning the work itself.
**Consequences:** Until a scheduler exists, purge/expiry must be triggered (cron job
hitting an admin action, a manual step, etc.). The services are idempotent and isolate
per-item failures so a bad row can't stall the batch.
**Alternatives:** Hangfire/Quartz/hosted service (deferred: infra before need).

### ADR-008 — Cross-module workflow events live in SharedKernel, not a module's Contracts
**Status:** Accepted.
**Context:** Account deletion and moderation decisions must make *other* modules react.
Putting `AccountDeletionRequested` in Identity's Contracts and `UserRestricted` in
Moderation's Contracts would force the reacting module (Profile) to reference Identity and
Moderation — and since Moderation already references Profile, that's a **project cycle**.
**Decision:** Shared cross-module lifecycle events live in `Hpn.SharedKernel`
(`Accounts.AccountDeletionRequested`, `Moderation.UserRestricted/UserBanned/UserCleared`).
The reacting module subscribes via SharedKernel and reflects the effect **into its own
schema** (Profile flips `profile.status`; the feed honours it for free because eligibility
already filters `status = 'active'`).
**Rationale:** SharedKernel is the one place every module may depend on, so events there
create no new edges and no cycles. "Reflect into your own schema" keeps write isolation
(`ADR-003`) intact — Moderation never writes profile rows; it raises an event and Profile
writes its own. It also means Feed needs zero knowledge of Moderation.
**Consequences:** A small set of genuinely cross-cutting events lives outside any module.
The rule: an event is a module's own Contract if only that module cares; it goes in
SharedKernel once another module must react and a direct dependency would cycle.
**Alternatives:** Event in the raising module's Contracts (rejected: cycles); direct
contract call from Moderation into a Profile *write* API (rejected: breaks write isolation).

### ADR-009 — Account export & erasure via `IAccountDataContributor`
**Status:** Accepted.
**Context:** GDPR export and the hard delete must cover every module's data, but no module
may write another's tables (`ADR-003`) and there are no cross-schema cascades (`ADR-002`).
**Decision:** Each module that stores personal data registers **one**
`IAccountDataContributor` (SharedKernel) over its own schema, exposing `Section`,
`ExportAsync`, `EraseAsync`, and an `IsAccountRoot` flag. An orchestrator in Identity
resolves the `AccountScope` (user id + profile id) once and fans out to all contributors,
erasing the account-root (Identity) last. Deletion is two-phase: a soft-delete
(`AccountDeletionRequested`, `ADR-008`) makes the account inert immediately; the hard purge
(`ADR-007`) runs after the grace window. The captured profile id is stored on the user row
so a retried purge still erases profile-keyed data after the profile row is gone.
**Rationale:** Keeps the cross-cutting concern (delete *everything*) compatible with the
isolation invariant (each module deletes *its own* everything). Adding a module is a
one-interface change, and forgetting it is the obvious bug.
**Consequences:** **Any new module storing personal data must register a contributor**, or
its rows survive deletion and miss export. Photo's contributor also deletes object-store
blobs, not just rows.
**Alternatives:** A central erase service with cross-schema SQL (rejected: breaks
isolation, knows every schema); DB cascades (rejected: `ADR-002`).

---

## Feed, moderation, location

### ADR-010 — Feed = stable eligibility + swappable `IFeedRankingStrategy`
**Status:** Accepted.
**Context:** Ranking is the part of a discovery feed that changes constantly (boosts,
fairness, freshness, experiments); eligibility (who may be shown at all) rarely changes.
**Decision:** Split them. Hard filters live in one eligibility query; ordering/selection is
entirely behind `IFeedRankingStrategy`. v1 is `RandomWithinEligibleStrategy`. New ranking
behaviour is a **new strategy class only** — never an edit to the eligibility query, the
`IFeedApi` contract, or callers.
**Rationale:** Isolating the volatile half behind an interface means the risky, frequent
changes can't accidentally break the stable safety filters (blocks, already-appreciated,
audience rules). New signals flow through the strategy's inputs.
**Consequences:** Recency is handled without an impressions table — already-appreciated
excluded via `appreciation_events`, plus a client-supplied session "seen" list for soft
dedupe. A future strategy needing the whole eligible set with custom signals widens the
strategy's inputs, not the eligibility query.
**Alternatives:** One big ranked query (rejected: couples safety filters to ranking
churn); an impressions table (deferred: not needed for session dedupe).

### ADR-011 — Feed and Admin read across schemas via their own read models
**Status:** Accepted.
**Context:** Feed eligibility joins profiles, photos, blocks, appreciation events, and
visibility prefs; the Admin queue/stats aggregate reports, profiles, trust, and actions.
Doing these as per-row `I<Module>Api` calls would be N+1 and hopeless to rank/aggregate.
**Decision:** Feed and Admin define their **own** keyless read entities mapped onto the
other schemas' tables (Feed) or query them via `FromSqlInterpolated` (Admin), as sanctioned
read models (`ADR-003`). They still go through Moderation/Profile/etc. **Contracts** for
any *write* or decision.
**Rationale:** These are set-based reads where the read model *is* the right abstraction.
This is the explicit, bounded exception to "depend only on Contracts" — taken for read
performance, never for writes.
**Consequences:** Feed/Admin know other schemas' column shapes for read; that coupling is
the deliberate cost. A schema change in a read source can break a read model — covered by
integration tests.
**Alternatives:** Contract calls per row (rejected: N+1); materialized cross-module views
owned centrally (deferred).

### ADR-012 — Magic-link auth with opaque server-side sessions (no token in the SPA)
**Status:** Accepted.
**Decision:** Email magic link → single-use hashed token (~15 min) → an opaque,
server-side **session** (hashed token row) delivered as an httpOnly + Secure + SameSite
cookie. Sessions are sliding-expiry (30 days) and revocable. The SPA never handles a token.
**Rationale:** No passwords to store/leak; no JWT in browser storage to be stolen by XSS;
revocation is a DB update (a stateless JWT can't be revoked before expiry). httpOnly
cookies keep the credential out of JS entirely. Always-`202` on magic-link request avoids
account enumeration.
**Consequences:** Session lookups hit the DB (fine at this scale; cache later if needed).
CSRF is mitigated by SameSite + the API shape.
**Alternatives:** JWT access/refresh tokens (rejected: revocation + XSS exposure);
passwords (rejected: storage/breach liability, worse UX).

### ADR-013 — Coarse location only (0.1°), distance shown in buckets
**Status:** Accepted.
**Context:** "Minimum distance from me" is a useful filter; exact location is a privacy
liability the product principles forbid storing.
**Decision:** Capture a geopoint **rounded to 0.1° (~11 km)** at the point of consent, never
finer. `min_distance_km` filtering computes distance between rounded points; the UI shows
distance only as coarse **buckets** (`nearby` / `under_50km` / `50_200km` / `200km_plus` /
`different_country`), never a number. Distance is an **equirectangular approximation** in
SQL, not great-circle `acos` (which returns `NaN` for identical points and doesn't
translate cleanly).
**Rationale:** Satisfies the distance feature without ever holding a precise position, so a
breach can't reveal where someone lives. Buckets avoid triangulation from exact numbers.
**Consequences:** Distance is approximate by design; "exactly N km" is not a product
capability. Consent is recorded; withdrawing it clears the stored point.
**Alternatives:** Precise coordinates + radius (rejected: privacy); PostGIS (deferred:
unnecessary for coarse buckets).

### ADR-016 — Moderation: trust + report-volume + review; never an automatic ban
**Status:** Accepted.
**Context:** Pure report-count moderation is trivially abused (brigading); pure automation
bans innocent people.
**Decision:** Combine a per-account **trust score** with **weighted report pressure** and
**human review**. Trust `[0,1]` from account age / ready photo / verified / genuine
engagement, minus upheld actions, computed on demand and cached. Over a 7-day window,
`pressure = Σ trust` across **distinct** reporters; at `pressure ≥ 1.5 + 2.0 · targetTrust`
**and** ≥3 distinct reporters, apply a **48h temporary restriction + review-queue entry**.
**Bans/clears are admin/system decisions only, written through `moderation_actions`.**
**Rationale:** Weighting by reporter trust makes a trusted reporter matter and a serial
false-reporter nearly irrelevant, so brigading with throwaway accounts doesn't work.
Scaling the threshold by *target* trust protects established accounts. Auto action is
*always* temporary + reviewed, so automation can inconvenience but never permanently
punish. Computing trust on demand (not via an event from every module) keeps event
handlers fast (`ADR-006`).
**Consequences:** Restriction reflects into the feed via `UserRestricted` (`ADR-008`) and
auto-expires via a gated step (`ADR-007`). Constants are launch defaults to tune.
**Alternatives:** Report-count thresholds (rejected: brigadable); fully automated bans
(rejected: punishes the innocent, violates the product's tone).

### ADR-017 — `IModerationApi` stays read-oriented; reports filed via HTTP
**Status:** Accepted.
**Context:** Admin must apply moderation decisions to a profile owned by Moderation;
members file reports.
**Decision:** Member reports are filed via the HTTP `POST /reports` endpoint (not a
contract method). The Admin module applies decisions through a single explicit contract
method, `IModerationApi.ApplyAdminProfileActionAsync`, which internally routes to
Moderation's own write services and raises the lifecycle events. The contract otherwise
exposes only reads (`GetTrustScore`, `IsRestricted`).
**Rationale:** Keeps writes inside the owning module (`ADR-003`) while giving Admin one
sanctioned, auditable entry point instead of reaching into internals. Reports are a
member-facing HTTP concern, not a cross-module call, so they don't belong on the contract.
**Consequences:** Admin actions are audited in Admin's schema; the moderation write itself
is recorded in `moderation_actions`. The two are separate writes across schemas; the admin
handler **audits before acting** so an applied action is never unaudited.
**Alternatives:** A general write API on the contract (rejected: leaks writes); Admin
reaching into Moderation internals (rejected: boundary violation).

---

## Conventions & tooling

### ADR-014 — Product principles are code constraints, not polish
**Status:** Accepted.
**Decision:** Enforce in code and UX: **positive-only** (no dislike/skip/negative action
anywhere; advancing the feed requires choosing an appreciation); **no comparison
surfaces** (no public counts, scores, or leaderboards — fingerprint/received use perception
phrasing, gated behind **≥20** received appreciations); **data minimization** (no
race/body/height/income; age de-emphasized; coarse location only); pause/delete/export/
block/report always reachable.
**Rationale:** These are the product's identity, not features — if they're "polish" they
erode under deadline pressure. Encoding them as hard rules (no negative-action endpoint
exists; counts aren't exposed; the fingerprint endpoint returns `insufficient_data` below
the threshold) makes the principle the path of least resistance.
**Consequences:** Some conventionally easy features are deliberately impossible (a "skip"
button, a popularity number). The ≥20 gate is centralized (`FingerprintDistribution.MinimumSampleSize`).
**Alternatives:** Treat as UX guidelines (rejected: they'd drift).

### ADR-015 — Enums stored as snake_case strings, not Postgres enum types
**Status:** Accepted.
**Decision:** Domain enums (gender, profile status, report type/status, action type, …)
are persisted as `snake_case` strings via a `*Format` helper (`ToStorageValue`/`Parse`) and
EF `HasConversion`, not as Postgres `enum` types.
**Rationale:** Postgres enum types are awkward to evolve (adding/reordering values is a
migration dance) and couple the DB to the code's enum. A string column with an app-side
mapping makes the stored value the stable contract while the C# name stays free to
refactor, and adding a value is a code change, not a DDL migration.
**Consequences:** No DB-level enum constraint; validity is enforced in app + validators.
Read models compare against the string literal (e.g. `status = 'active'`).
**Alternatives:** Native PG enums (rejected: rigid evolution); int codes (rejected:
unreadable in the DB).

### ADR-018 — Frontend API client generated from OpenAPI
**Status:** Accepted.
**Decision:** The TypeScript client/DTO types are generated from the backend's OpenAPI
document (`openapi-typescript` → `web/notice/src/lib/api/generated/schema.ts`, gitignored).
DTO types are never hand-written.
**Rationale:** A single source of truth (the API) means the frontend can't drift from the
contract — a breaking change surfaces as a TypeScript error, not a runtime 400. Regenerate
after any API change (`make gen-api`).
**Consequences:** The generated file is build output (gitignored). `openapi-typescript`
emits int32 as `null | number | string`, so numeric fields are coerced with `Number(...)`.
**Alternatives:** Hand-written types (rejected: drift); a typed client SDK by hand
(rejected: maintenance).

### ADR-019 — Object storage behind `IObjectStore`; full image pipeline on every upload
**Status:** Accepted.
**Decision:** Binaries live in object storage behind `IObjectStore` (MinIO dev / Cloudflare
R2 prod, swapped by config), never in a module DB. Every uploaded image is validated
(MIME + magic byte + size), decoded, **EXIF/metadata-stripped**, re-encoded to WebP in
display + thumb variants, hashed, and stored. An NSFW check is a no-op pass-through seam.
**Rationale:** The store abstraction keeps us cloud-neutral and lets tests substitute a
recording fake. Stripping metadata is a privacy hard-requirement (EXIF GPS), so it's in the
one pipeline every image must pass through — including the dev seeder (`ADR-021`), which
uses the real pipeline rather than inserting fake rows.
**Consequences:** Seeded and uploaded photos are real WebP blobs with stripped metadata.
The NSFW seam is ready for a phase-2 scanner without restructuring.
**Alternatives:** Blobs in Postgres (rejected: bloat, backup cost); cloud SDK directly
(rejected: vendor lock, untestable).

### ADR-020 — Real Postgres via Testcontainers as the primary test layer
**Status:** Accepted.
**Decision:** The main confidence layer is integration tests over a **real Postgres**
spun up per test class with Testcontainers + `WebApplicationFactory<Program>`. No SQLite
substitution. Unit tests cover pure logic; architecture tests guard the boundary.
**Rationale:** The risky parts are SQL, EF translation, migrations, and cross-schema read
models — exactly what an in-memory/SQLite provider gets wrong. Testing against the real
engine catches translation NaNs, `ON CONFLICT` behaviour, jsonb, `DISTINCT ON`, etc. The
gate is "critical flows have integration coverage and the architecture tests pass."
**Consequences:** Tests need Docker and are slower (serialized, container per class). Worth
it — they caught real translation and concurrency issues during the build.
**Alternatives:** In-memory/SQLite (rejected: doesn't exercise the real SQL); mocking the
DB (rejected: tests the mock, not the system).

### ADR-021 — Development data seeding via phased `IDevelopmentDataSeeder`
**Status:** Accepted.
**Context:** Exploring the app (and demoing it) needs realistic data — a populated feed,
appreciation history, an unlocked fingerprint — not an empty shell.
**Decision:** A `IDevelopmentDataSeeder` per module (SharedKernel interface), each seeding
**only its own schema**, ordered by an explicit `Phase`, run once at startup by a host
orchestrator. **Development-only**, gated by `DevelopmentSeed:Enabled`, idempotent
(existence-checked / deterministic ids / `ON CONFLICT`), and it pushes images through the
**real** photo pipeline (`ADR-019`). Cross-phase ids pass through a `DevelopmentSeedContext`
— the same fan-out shape as `IAccountDataContributor` (`ADR-009`).
**Rationale:** Reuses the established isolation pattern instead of one cross-schema seed
script, so seeding can't violate the boundary and a new module adds its own seeder. Using
the real pipeline means seeded photos behave exactly like uploaded ones. Idempotency makes
restart-and-reseed safe; every integration test disables it so throwaway DBs stay clean and
assertions stay deterministic.
**Consequences:** Seed fixtures (sample images) live under `seed/images/` (backend-owned,
not in the frontend tree). A seed failure **fails hard** (aborts Development startup) — a
deliberate fail-fast so a broken seed is noticed immediately. Seeder classes are registered
in all environments but only execute in Development.
**Alternatives:** A SQL seed script (rejected: cross-schema, bypasses the pipeline and
domain); EF `HasData` (rejected: migration-bound, can't run the image pipeline or object
store).

### ADR-022 — Migrations kept under `Internal/Persistence/Migrations`; gated in prod
**Status:** Accepted.
**Decision:** Each module's EF migrations (and model snapshot) live in
`Internal/Persistence/Migrations` — generated with
`--output-dir Internal/Persistence/Migrations`, not EF's default `Migrations/` folder.
Migrations auto-apply in Development on boot; in production they are an explicit, gated
deploy step — **never auto-migrate prod on boot**.
**Rationale:** Keeping migrations beside the rest of the module's persistence keeps the
module self-contained. Auto-applying in dev is convenient; auto-applying in prod is
dangerous (an unexpected migration on a scaled-out boot, or on rollback) — schema change is
a deliberate, reviewed action.
**Consequences:** Adding a migration always needs the `--output-dir` flag (easy to forget;
the default folder + namespace must be cleaned up if used by mistake). Prod deploys run
migrations as a separate gated step.
**Alternatives:** Default `Migrations/` location (rejected: inconsistent, splits
persistence); auto-migrate prod (rejected: unsafe).

### ADR-023 — Repo hygiene: CPM, warnings-as-errors, record DTOs, cursor pagination
**Status:** Accepted.
**Decision:** Central Package Management (`Directory.Packages.props`) for all versions;
nullable reference types on and **warnings treated as errors**; `record` request/response
DTOs; cursor pagination everywhere (never offset); `Idempotency-Key` honoured on
`POST /appreciations`; base path `/api/v1` from a single `ApiRoutes.Prefix`.
**Rationale:** CPM stops version drift across many projects. Warnings-as-errors keeps the
nullable contract honest and the build clean. Records give value-equality DTOs for free.
Cursor pagination stays correct under concurrent inserts (offset pages skip/duplicate).
Idempotency keys make the core write (appreciate) safe to retry.
**Consequences:** A new warning fails the build (intended). Every list endpoint is
cursor-based.
**Alternatives:** Per-project versions (rejected: drift); offset pagination (rejected:
unstable under inserts).

### ADR-024 — Guest actors can browse and appreciate before signup
**Status:** Accepted.
**Context:** The appreciation-gated feed has no skip/dislike action; advancing requires a
positive appreciation. Requiring email and profile setup before anyone can even browse
adds friction and hides the product loop from curious visitors.
**Decision:** Support a true guest actor: `POST /guest/start` mints an opaque,
server-side, revocable `hpn_guest` session. A guest principal carries only
`hpn:actor_kind=guest` and `hpn:actor_id=<guest id>`; it deliberately has no
`ClaimTypes.NameIdentifier`, so member-only endpoints stay member-only through the
default authorization policy. Feed and Appreciation opt in to `guest-or-member`.
Guest appreciations count fully and immediately in `appreciation_events`,
`received_appreciation_stats`, and `given_appreciation_stats`, using the guest id as the
sender id. Profile adds `hidden_from_guests` as a default-false visibility opt-out,
honoured only by the guest feed path. When a guest verifies a magic link while holding the
guest cookie, Identity raises `GuestConverted`; Appreciation re-keys guest sender rows to
the member id, resolves unique collisions by keeping existing member events, and merges
given counters. Guest "not interested" remains client-side via the existing `seen` feed
parameter; Feed still owns no tables.
**Rationale:** Guests get the real product loop without turning into anonymous fake users.
The separate actor model keeps the safety boundary obvious: `UserId` means member, while
`ActorId` means member-or-guest only in endpoints that explicitly accept it. Counting
guest appreciations preserves recipient value and avoids a shadow/non-counting experience.
Server-side sessions give revocation, sliding expiry, and per-guest rate limiting without
introducing Redis or a new token model. Keeping hides in the client-side `seen` list avoids
creating a Feed-owned table for ephemeral guest preference.
**Consequences:** Default authorization is intentionally member-only; any endpoint that
should accept guests must say so with `guest-or-member`. Abuse controls for guests are
session revocation, `/guest/start` per-IP rate limiting, and per-actor appreciation rate
limiting; guest reporting and durable cross-device guest hides are deferred. Conversion
has a collision edge when an existing member also reacted as a guest, covered by the
Appreciation re-key handler and integration tests.
**Alternatives:** Anonymous stateless tokens (rejected: not revocable/rate-limitable
enough for reactions that count); require signup before reacting (rejected: defeats the
adoption goal and feed mechanic); a Feed-owned guest hides table (rejected: durable hides
are not needed for v1 and would violate Feed's table-less posture).

### ADR-025 — Two-level appreciation taxonomy: 6 categories × specific traits
**Status:** Accepted. Supersedes the flat 12-category taxonomy from `ADR-021`-era seeding.
**Context:** The original taxonomy was a flat list of 12 categories (`warm_smile`,
`calming_energy`, …) with no grouping. The Notice redesign (`docs/Design`) specifies a
**flattened trait picker**: the appreciator sees ~20 specific traits at once as a single
colour-coded cloud and reacts in one tap, where colour conveys a category. That needs two
levels — a small set of categories carrying a colour, and the named traits under them —
which the flat model could not express.
**Decision:** Replace the flat list with **6 categories** (`physical`, `energy`, `style`,
`humor`, `mind`, `authentic`), each carrying a `hue` (the OKLCH hue used for its accent),
and **20 traits** seeded under them (e.g. `physical` → Warm smile, Kind eyes, Great hair,
Natural glow). `appreciation_events` gains a required `trait_id` (FK to the new
`appreciation_traits` table) alongside the existing `category_id`, which stays denormalized
from the trait so the projections, the `(sender, receiver, category)` duplicate guard, and
the `GuestConverted` re-key SQL keep working unchanged. `SubmitAppreciation` takes a
`traitId` and derives the category. `GET /appreciation-categories` returns each category
with its `hue` and nested traits (feeds the picker). Received gains trait-level "ways people
describe you" cards (label + category hue + count + phrasing), computed on demand from
events rather than via a new projection table.
**Rationale:** Keeping `category_id` denormalized on the event preserves every existing
category-level read (fingerprint distribution, appreciation style, given/received counters)
and the guest-conversion machinery, so the change is additive at the storage layer. Trait
counts for Received are read on demand because that surface is cold and a second
projection + a second `GuestConverted` compensation path was not worth the complexity.
**Consequences:** This is a breaking taxonomy change with no data migration path — the
migration truncates `appreciation_events` and the stat tables and reseeds, acceptable
pre-launch. The Fingerprint radar/distribution stays **category-level** (correctly 6 axes),
while its **"recurring traits" list is trait-level**: `IAppreciationApi.GetReceivedTraitSummaryAsync`
exposes per-trait received counts (read from events, carrying the category hue), so the
second level of the taxonomy surfaces there. Appreciation Style stays category-level. The
duplicate guard remains per-category, which is sufficient because the feed is
appreciate-once-then-advance and never re-shows a profile.
**Alternatives:** Keep the flat 12 categories and render them as the cloud (rejected by
product: loses the named-trait warmth the redesign is built around); a separate
`received_trait_stats` projection (rejected: extra write path + guest-conversion
compensation for a cold read).

---

### ADR-026 — One country-visibility control: inbound `hide_from_country` only
**Status:** Superseded by `ADR-027` (country removed entirely). Narrows the country rules from `ADR`-era M8 settings.
**Context:** Settings exposed two country toggles that read as duplicates to users but did
opposite things: `hide_from_country` (**inbound** — remove me from the feed of viewers in my
own country) and `show_only_outside_country` (**outbound** — hide same-country candidates from
*my* feed). Having both on the same screen was confusing, and product judged the outbound
"only show me people abroad" filter to be the weaker, less-requested of the two.
**Decision:** Drop `show_only_outside_country` entirely — the column (migration
`RemoveShowOnlyOutsideCountry`), the domain field, the settings request/response DTOs, the
account-export payload, the feed eligibility filter, and both UI toggles. Keep the **inbound
privacy** control `hide_from_country`: a candidate who enables it is removed from the feed of
any viewer whose `country_code` matches theirs (`GetFeedNextHandler` country rule).
**Rationale:** The retained control is the privacy-protective one (it governs *who can see
you*), consistent with §2 data-minimization and the "block/hide must always be easy" posture.
The outbound filter was a convenience knob on the viewer's own feed, recoverable later as a
feed-ranking signal if asked for, without a stored preference.
**Consequences:** Existing rows lose the column on migrate (pre-launch, no data concern). The
feed no longer offers a viewer-side "people abroad only" filter. `BuildEligibilityQuery` loses
its `viewerWantsOutsideCountry` parameter; the inbound `hide_from_country` rule is unchanged.
**Alternatives:** Keep both and reword the labels (rejected by product as redundant surface);
keep only the outbound filter (rejected — it removes the inbound privacy capability).

### ADR-027 — Remove country from the product entirely
**Status:** Superseded by `ADR-028` (country reinstated as an internal, IP-derived field). Supersedes `ADR-026`.
**Context:** After `ADR-026` only the inbound `hide_from_country` control remained. In practice
country still cost a surface and a data point everywhere: a free-text 2-letter onboarding field
(unvalidated — no picker, easy to typo), a `country_code` column on `profiles`, a
`hide_from_country` flag on `visibility_preferences`, a feed eligibility rule, a
`different_country` distance fallback, and the field on the feed/public DTOs. The card UI never
shows country, distance is already handled by the coarse geopoint buckets (`ADR`-era §10.4), and
the lone remaining toggle governed a niche same-country privacy case that the broader pause /
block / women-for-women / verified-only controls already cover.
**Decision:** Remove country end-to-end. Migration `RemoveCountry` drops both columns; the
domain (`UserProfile.CountryCode`, `VisibilityPreferences.HideFromCountry`), the upsert +
visibility request/response DTOs and validators, the public/feed contract DTOs, the feed
eligibility "country rule" and `FeedViewerContext.CountryCode`, the `DistanceBuckets`
`different_country` fallback, the account-export payload, the dev seeder, and both onboarding
and You-screen toggles all go with it. Distance bands now derive solely from the coarse
geopoint (null when there is no point to measure).
**Rationale:** Data minimization (§2) — stop collecting a field the product doesn't use. It also
resolves the unvalidated-input bug without building an ISO-3166 country picker. Less surface,
tighter privacy, one fewer thing on the wire and in the schema.
**Consequences:** Pre-launch, dropping the columns is a clean one-way migration (no data
concern; `Down` re-adds them empty). No country-based feed shaping remains; if a "people abroad"
or same-country-privacy need ever returns it comes back as a feed-ranking signal or a fresh
preference, not a resurrected column. The feed `DistanceBucket` set shrinks to
`nearby/under_50km/50_200km/200km_plus`.
**Alternatives:** Build a proper country picker + server validation and keep the inbound toggle
(rejected — keeps an unused-by-the-card data point and a niche control for marginal value);
keep the column but stop surfacing it (rejected — dead weight in schema and DTOs).

### ADR-028 — Country is internal + IP-derived; bio removed
**Status:** Accepted. Supersedes `ADR-027`.
**Context:** `ADR-027` removed country because the only thing wrong with it was the *unvalidated
onboarding input* and its presence on user-facing surfaces. But the same-country privacy control
("don't show me to people in my own country") is genuinely wanted — what wasn't wanted was asking
the user to type a country, or showing country anywhere. Separately, the free-text **bio** ("a
line about you") is never rendered on the card or anywhere in the product.
**Decision:** Reinstate `country_code` (`profiles`) and `hide_from_country`
(`visibility_preferences`), but make country **internal and derived, never entered or shown**:
- **Derivation:** `IClientCountryResolver` (SharedKernel), implemented in the host as
  `RequestClientCountryResolver`, resolves country via a fallback chain: (1) the edge geo header
  (Cloudflare `CF-IPCountry`), then (2) an **offline GeoIP database** lookup on the client IP
  (`GeoIpCountryDatabase` over a memory-resident `.mmdb`, read with the `MaxMind.Db` library — no
  external call). The upsert handler calls `UserProfile.SetCountry` on create/edit; a null signal
  (loopback IP in dev, missing header, `XX`/`T1`, or no DB present) leaves the stored value
  untouched. The DB is the free **DB-IP Lite Country** file (CC BY 4.0) fetched by `make geoip`
  into a gitignored `App_Data/` and pointed at by `GeoIp:DatabasePath`; the reader also accepts a
  MaxMind GeoLite2-Country file (it handles both the flat `country_code` and nested
  `country.iso_code` schemas). If the file is absent, IP estimation is simply disabled — no failure.
- **Internal-only:** country is **absent** from `FeedProfileDto`, `PublicProfileDto`,
  `ProfileResponse`, and the public projection. It is read only by the feed eligibility
  same-country rule (`FeedViewerContext.CountryCode` + `FeedProfileRow.CountryCode`) and included
  in the account **export** (the subject's own data, right of access).
- **Toggle stays user-facing:** `hide_from_country` is back on the visibility request/response and
  both the onboarding privacy step and the You screen — it's a preference, not the country value.
- **Distance** remains geopoint-only (no `different_country` bucket) — country is never used for display.
- **Bio removed entirely** (migration `DropBio`): the onboarding input, the `bio` column, the
  upsert request/validator, every response/contract DTO, the feed card DTO + projection, the
  export payload, and the seeder.
**Rationale:** Keeps the wanted privacy capability while honouring data-minimization and "never
show country": the user neither enters nor sees it. Deriving from the edge header is dependency-free
and degrades gracefully. Dropping bio removes a collected-but-unshown field.
**Consequences:** Country is estimated in any environment that has the `.mmdb` (run `make geoip`),
not only behind Cloudflare; without both the header and the DB it stays null and the filter is
inert — acceptable. Adds one dependency (`MaxMind.Db`, permissive licence) and a ~8 MB data file
that is **not committed** (gitignored, refreshed via `make geoip`). Migrations net out to: country
columns retained, `bio` dropped (the interim `RemoveCountry` migration was removed before commit,
so history has no remove-then-readd churn). Integration tests cover both the header→storage path
(via `CF-IPCountry`) and the same-country filter (seeding `country_code` by SQL).
**Alternatives:** External IP-geolocation API at signup (rejected — an in-request external call +
rate limits, against the "no external calls in v1" posture); edge header only (rejected — inert
outside Cloudflare, which prompted this revision); keep bio for a future detail view (rejected —
speculative, never shown); keep entering country in onboarding (rejected by "never ask/never show").

---

## Deferred (chosen not to build yet)

These are explicit phase-2+ deferrals — recorded so "why isn't there an X?" has an answer.

- **Redis** — caching + multi-node rate limiting. In-memory is fine for single-node MVP.
- **Background worker / scheduler** — see `ADR-007`; deferred work is gated maintenance.
- **Async/outbox messaging** — domain events are synchronous in-process (`ADR-006`).
- **NSFW scanning + automated selfie verification** — seams exist (`ADR-019`); verification
  is a manual admin flag for now.
- **Realtime (SignalR), opt-in leaderboards, perception-based compatibility, richer
  AI-profile detection, a native wrapper (Capacitor), monetization** — product phase 2+.

Each becomes a new ADR (superseding the relevant deferral) when picked up.

### ADR-029 — Notification module: appreciation notifications via client polling
**Status:** Accepted.
**Context:** The design handoff includes a one-time "Someone just noticed your ..." toast and
we want a quiet "something new" signal on the Received tab. v1 has no notification subsystem,
real-time transport, or background worker.
**Decision:**
- Introduce a `Notification` module (schema `notification`) that subscribes to
  `AppreciationCreated` and persists one receiver notification per appreciation, idempotent on
  `(user_id, type, source_id)`.
- Delivery is client polling of `GET /notifications/summary` about every 20 seconds. No
  SignalR/WebSocket/SSE, Redis, or worker is added.
- Notification creation funnels through one internal `NotificationWriter` so a future
  server-push transport can be added behind its own ADR.
- The Received badge is a dot, not a count, honoring `ADR-014`. The payload never stores or
  reveals the sender, only what was appreciated.
**Consequences:** The event handler writes through the Notification module's own DbContext and
transaction, separate from appreciation submission. Dispatch occurs just before the submit
commit, so a rolled-back appreciation could in principle leave an orphan notification; this
small window is accepted in v1, and an outbox would require a separate ADR. The unique source
index prevents duplicates. `AppreciationCreated` gained `TraitLabel` and `CategorySlug` so the
notification needs no read-back into Appreciation. Because the module stores personal data, it
registers an `IAccountDataContributor`.
