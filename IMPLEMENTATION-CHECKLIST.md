# Implementation checklist (10/10 quality)

Quality bar: [ARCHITECT-RATINGS-CSHARP-IMPLEMENTATIONS.adoc](../../docs/ARCHITECT-RATINGS-CSHARP-IMPLEMENTATIONS.adoc). Align with **robotico-results-csharp** and **robotico-outbox-inmemory-csharp**.

- [ ] Implement `MongoDbOutbox` : `IOutbox` with a dedicated collection; use `IClientSessionHandle` so EnqueueAsync (insert) and CommitAsync (commit) share the same session with domain writes.
- [ ] XML docs, guards, tests: enqueue/commit, null, commit failure, order preserved.

Reference: `Robotico.Outbox.InMemory.InMemoryOutbox` in robotico-outbox-inmemory-csharp.
