using Xunit;

// These suites each boot a WebApplicationFactory<Program> over their own
// Testcontainers Postgres. Building two hosts for the same entry point in
// parallel races on the shared host-factory listener, so run integration test
// classes serially — the confidence layer values determinism over speed (§14).
[assembly: CollectionBehavior(DisableTestParallelization = true)]
