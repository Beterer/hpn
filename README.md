# Notice

**Notice** is an appreciation-first social platform. People browse one profile at a time and move forward only by choosing a genuine, specific appreciation — there is no like, skip, or dislike anywhere. Over time each member builds a **social fingerprint**: how others tend to perceive them, shown in interpretive phrasing ("People often perceive…"), never as scores, counts, or leaderboards.

> `HPN` (from *human-perception-network*) is the internal codename used throughout the code, namespaces, and database; **Notice** is the user-facing product name.

The MVP is complete: identity & magic-link auth, profiles, photos, the eligibility/ranking feed, the appreciation loop, received appreciation, social fingerprint & appreciation style, privacy & settings, reporting & trust-weighted moderation, and a minimal admin console.

## Tech stack

- **Backend** — .NET 10, minimal APIs + endpoint groups, EF Core 10, PostgreSQL, FluentValidation, RFC 9457 problem details. A **modular monolith**: one deployable host (`src/Hpn.Api`) composed of independent modules, each owning its own database schema.
- **Frontend** — `web/notice`: Vite + React + TypeScript PWA, React Router, TanStack Query, Tailwind. The API client is generated from the backend's OpenAPI document — DTO types are never hand-written.
- **Auth** — passwordless email magic link → an opaque, revocable, server-side session over an httpOnly cookie. The SPA never handles tokens.
- **Storage** — uploaded images go through an EXIF-stripping, re-encoding pipeline and are stored via an `IObjectStore` (MinIO in dev, Cloudflare R2 in prod).
- **Local infra** — Docker Compose: Postgres, MinIO, Mailpit (captures dev emails), and a Caddy reverse proxy.

## Repository layout

```
src/
  Hpn.Api/                 the only host — wires modules + shared middleware
  Hpn.SharedKernel/        cross-cutting contracts (events, auth, rate limits, …)
  Hpn.Modules.<Name>/      one project per module; everything internal except Contracts
web/notice/                the Notice PWA
tests/                     unit, architecture-boundary, and Testcontainers integration tests
seed/images/               sample photos used by the development data seeder
CLAUDE.md                  architecture & conventions guide (read this before contributing)
```

Modules: `Identity`, `Profile`, `Photo`, `Appreciation`, `Feed`, `SocialFingerprint`, `Moderation`, `Admin`.

## Getting started

**Prerequisites:** .NET 10 SDK, Node 22, and Docker.

```bash
make dev
```

That brings up the whole stack: Docker infra, the API, the generated client, and the PWA. Then:

- **App** — http://localhost:5173
- **API** — http://localhost:5080
- **Mailpit** (dev inbox for magic-link emails) — http://localhost:18025

To sign in, enter any email in the app, then open Mailpit and click the magic link.

In Development the app **seeds realistic data** on startup (configurable in `appsettings.Development.json` under `DevelopmentSeed`): a ready-to-use account **`test@notice.local`** with photos, an active feed of candidate profiles, and enough received appreciation to unlock the social fingerprint. Sign in as that address (via Mailpit) to explore a populated app immediately. Seeding is Development-only and idempotent.

### Running the pieces by hand

```bash
make up                              # start Docker infra only
dotnet run --project src/Hpn.Api     # API (auto-applies migrations in Development)
cd web/notice && npm run dev         # PWA
```

## Common tasks

```bash
dotnet build HPN.sln -c Release      # build (warnings are errors)
dotnet test HPN.sln                  # unit + architecture + integration tests (needs Docker)
make gen-api                         # regenerate the typed frontend API client from OpenAPI
make down                            # stop Docker infra
```

Integration tests run against a **real Postgres** spun up per test class via Testcontainers (so Docker must be running); they need no manual database setup.

See **[CLAUDE.md](CLAUDE.md)** for the architecture rules, module boundaries, the migration workflow, conventions, and the product principles that constrain the code.

## Configuration & secrets

Configuration is environment-variable / `appsettings` driven (12-factor). No secrets live in the repo — use `dotnet user-secrets` for local dev and env injection elsewhere. Object-store, email-provider, and database credentials are all externally provided.

## License

No open-source license has been declared for this repository; all rights are reserved by the authors.
