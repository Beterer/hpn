# Notice / HPN — documentation

Reference docs for the codebase. Start here.

- **[architecture.md](architecture.md)** — how the system fits together: the modular monolith, schema-per-module, the cross-module collaboration patterns, the feed, moderation/trust, auth, and the data model. Read this to understand *what* exists and *how* the pieces connect.
- **[decisions.md](decisions.md)** — the decision log (ADR-style). Every significant architectural and product decision, *why* it was made, what was traded away, and what would change it. Read this to understand *why* the code looks the way it does before you change it.

For day-to-day commands (build, test, migrations, running locally) see [`../CLAUDE.md`](../CLAUDE.md) and [`../README.md`](../README.md).

## Conventions in these docs

- **HPN** is the internal codename (code, namespaces `Hpn.*`, DB schemas, infra). **Notice** is the user-facing product name.
- Decisions are numbered `ADR-NN` and are append-only — supersede rather than rewrite, so the history of reasoning survives.
- Numeric constants quoted here (trust weights, thresholds, windows) are **launch defaults meant to be tuned with real data**, not load-bearing invariants. Each is named and centralized in code; this doc records the starting value and the intent.
