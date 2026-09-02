# Layer C Placement Review

**Status:** Complete — no placement candidates were flagged by the available Phase 1/Phase 4
evidence.

The Phase 3 convergence record states that calculation, provider calls, persistence joins, and
inventory concurrency remain application-tier defaults pending evidence. Phase 4b found no
candidate with sufficient volume, set-based execution evidence, batch frequency, or app-tier
performance risk to justify a DB-tier object.

| ID | Service | Target | Signal(s) | Legacy tier | Evidence | Recommended | Decision |
|---|---|---|---|---|---|---|---|
| — | — | None | No qualifying candidate | — | No reliable volume/set-vs-row evidence was supplied | app-tier | app-tier |

All business logic remains in the application tier. PostgreSQL constraints and indexes enforce
data integrity; no view, function, procedure, or trigger is introduced by Phase 4b.

No `assessment/placement-decision-register.md` is required because there are zero placement
candidates and therefore no individual tier decisions to record.
