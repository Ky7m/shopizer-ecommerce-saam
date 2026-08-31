# SAAM Operator Guide

**Audience:** the architect running a legacy-modernization engagement end to end with SAAM + Kiro.
**What this is:** your journey — what you do, what you decide, what to open and eyeball, and what to run
to catch the framework lying to itself. It is NOT a phase summary (the `saam-phase*.md` steering files
are the authoritative per-phase mechanics). This guide is the human's map across them.

**The one mental model to keep:** SAAM is graph-driven. The Neo4j knowledge graph is the control plane —
it tracks every rule/service/contract/test/implementation and computes a single `signalStatus`
(CLEAR / BLOCKED / FLAGGED) that answers "can this proceed?" Your job at each gate is to (1) make a
judgment the agent cannot, and (2) cross-check the agent's output against an INDEPENDENT signal before
you approve. Approval without a cross-check is a rubber stamp, and rubber stamps are where wrong-but-green
systems come from.

---

## 0. Before you start

You need: Kiro IDE, `gh` (authenticated), `python3`, `uv`, `podman`, `git`. Optionally CAST Imaging
credentials and Jira. The enablement skill (`saam-enablement`) bootstraps everything — steering files,
directory structure, the Neo4j graph container, hooks, and the validation pipeline script.

In a fresh Kiro workspace: **"Start a new SAAM project."** The skill checks dependencies, downloads
steering, stands up Neo4j, installs the graph hooks, and drops you into Phase 0.

**First decision you own — CAST or not.** If CAST Imaging is available for this system, use it (or
Hybrid). CAST is the ONLY fully LLM-independent guardrail — it structurally proves no business logic was
silently dropped. If you go Direct Source (no CAST), you are giving that up (see the No-CAST callout at
the end). For anything large (>150K LOC) or high-risk, push for CAST/Hybrid.

---

## The rhythm at every phase

Each phase follows the same shape, and so does your role in it:

1. **Agent works** (often via subagents) and produces artifacts on disk + graph nodes.
2. **Hard exit gate:** the agent must produce the phase's telemetry file and mandatory artifacts BEFORE
   it is even allowed to ask for your approval. If it asks and the artifacts aren't there, that's a bug.
3. **You inspect** the specific artifacts (this guide tells you which and what to look for).
4. **You cross-check** against an independent signal (a command or graph query).
5. **You decide** at the 🔴 prompt and approve / send back / approve-with-notes.

Prompt colors you'll see: 🔴 BLOCKING (stop, answer), 🟡 INFORMATIONAL (agent proceeds with a stated
assumption — correct it if wrong), 🟢 NOTIFICATION (FYI).

---

## Phase 0 — Onboarding

**You decide:**
- **Analysis mode** (Direct / CAST / Hybrid). The real call: is the codebase small enough for direct
  source, and are you willing to lose the CAST safety net if you skip it? The agent now discloses that
  trade-off at the prompt — take it seriously.
- **Target stack (preliminary)** — non-binding; 4b confirms with evidence. "TBD" is a fine answer.
- **Segmentation** — does the agent's mechanical split match how your team thinks about the system? This
  is where your domain knowledge corrects the machine.

**Open and eyeball:** `inventory/INDEX.md` — is the analysis mode right, are component counts sane? The
generated `README.md` — is it about YOUR project, not SAAM boilerplate?

**Cross-check:** none yet (baseline). The thing to get right is mode selection — a wrong "Direct" on a
huge system is only caught later by a soft warning.

---

## Phase 1 — Bottom-Up (Source Architect) — runs parallel with Phase 2

**You decide:** `AMBIGUOUS_LOGIC` prompts — when the agent finds odd legacy behavior at low confidence,
is it a real rule or a bug/workaround? Only you (or an SME) know.

**Open and eyeball:** `assessment/<domain>-extraction-summary.md`:
- Every BR-ID has an EXACT `file:function:lines` source reference. "In the order module" is not
  acceptable — you must be able to open the file and find the code.
- The Layer A/B/C flag tables (entity lifecycles, extensibility signals, placement candidates) are
  populated or explicitly "none."

**Cross-check (do this — it's a real self-consistency test):**
```bash
# markdown BR count vs graph BR count — must match, or P1 is not complete
grep -rhoE 'BR-[A-Z]{2}-[A-Z]{2,4}-[0-9]{2,3}' assessment/*.md | sort -u | wc -l
```
Then in Kiro: `graph_query_nodes(BusinessRule)` count. **Bad result:** markdown count > graph count →
rules were extracted but not loaded; the agent must batch-import before P1 is "done."
**What P1 coverage means (read this first — it prevents a false alarm at the gate):** P1 is a SURFACE
sweep + full-inventory ingestion, NOT the deep extraction. The bulk of business logic is extracted in
**P4** (deep, per-service source reads — P4 typically produces 2-3x the P1 rule count). So **low P1
coverage against the full CAST inventory is EXPECTED and correct** — a P1 that ingests ~10k components and
extracts a few hundred BRs is normal, not a failure. The full-inventory denominator exists so coverage can
be **tracked as it rises across P1 → P4**, not so it's filled at P1. At the P1 gate you are checking that
the denominator is HONEST and the picture is truthful (nothing hidden), NOT that coverage is high. The
"unaccounted business logic" you resolve at the P1 gate is about *explicit decisions on the surface sweep*
(exclude as non-business / defer / flag for P4 deep read) — it is not a demand to extract everything now.
The zero-unaccounted-loss guarantee is enforced at its full strength after **P4/P5**, against the same
denominator; P1 just establishes that denominator and begins filling it.

- **CAST only — the Coverage Summary is PRINTED in the P1 exit-gate message; read it before approving.**
  The agent must surface a block showing: business-component count (full CAST inventory) vs BR count, a
  denominator sanity line, accountability %, and the per-intent coverage shape. This is pushed to you, not
  something you have to ask for. (Remember: at P1 the accountability % is expected to be LOW — you're
  reading it for HONESTY and SHAPE, not height.)
  - **Denominator sanity (make-or-break):** the business-component count must be MUCH larger than the BR
    count. If they're roughly equal, Step 0 (full-inventory ingestion) didn't run — the numbers are the
    extracted subset measured against itself, and any "100% / zero loss" is FALSE. The agent is told to
    STOP and re-run Step 0 in this case; if you see `N ≈ M` in the summary, don't approve.
  - **Read the SHAPE, not the headline:** high `post` coverage with near-zero `entry`/`derive`/`distribute`
    is the posting-bias miss — you have the posting spine, the body of entry/calculation/cross-module logic
    is missing. That's a gap even if the headline % looks moderate.
  - **Block vs pilot (note the P1 vs post-P4 distinction):** at the P1 gate, "unaccounted" is resolved by
    a DECISION on the surface sweep, not by extracting everything — each uncovered component is either
    marked non-business (positive evidence), deferred to P4 deep read, or flagged. The FULL
    zero-unaccounted-loss block (every business component extracted-or-explicitly-excluded) is enforced
    after **P4/P5**, against this same denominator — that is when a full (assurance) run must have no
    silent unaccounted business logic. On a PILOT you may accept a thin slice at either gate — but as an
    eyes-open recorded decision seeing the summary, never a silent pass. At P1 specifically, do NOT read a
    low coverage % as a blocker — it's expected; you're gating on honesty of the denominator and the
    shape, and on the explicit disposition of what the sweep surfaced.
  - **No-CAST:** this summary doesn't exist — you rely on the expected-yield heuristic + the Phase 3
    top-down flow-coverage net instead.
- **CAST only — Step 0b behavioral intent sweep (recommended default, you'll be asked):** the coverage
  SHAPE above is only meaningful if components have a real `intentCategory`. Step 0 classifies intent from
  BOTH observed data-access (what tables each component writes/reads — authoritative) AND naming patterns
  (a prior) — combined, because names lie and CAST may leave objects unclassified. On a large or
  poorly-classified CAST surface this sweep can be lengthy — that's expected work, not a hang. **If you're
  offered the name-only fallback:** skipping the behavioral sweep leaves the unclassified components at
  `unknown`, which turns the coverage-shape signal OFF, blinds P2 boundary decisions to distribution/entry
  hubs, and degrades the loss guarantee to a headline count. The agent will state this before skipping —
  decline the sweep only with eyes open.
- **CAST only — watch for a `util`-inflated shape (a real trap this run hit):** if the coverage shape
  shows a huge `util`/`report` count, be suspicious. Absence of a data-access signal must map to
  `unknown` (still counted, still visible as uncovered), NEVER to `util` (which drops the component OUT
  of the business denominator). A classifier that defaults no-signal components to `util` silently
  removes real posting/entry/distribution engines from coverage — the same wrong-denominator failure the
  full-inventory fix prevents, one level down. `util`/`report` and `businessLayer=false` are both
  denominator-removing and both require POSITIVE evidence of non-business. If you see the shape swing
  heavily to `util`, the classifier likely defaulted missing signal instead of leaving it `unknown` —
  reject it. The `unknown` residual is fine — P4 source reads resolve it.

**🔴 YOUR write-coverage register (CAST/Hybrid) — `assessment/write-coverage-reconciliation.md`:** the
strongest omission defense, and it caught a real class Query 1 missed. Query 1 is complexity-keyed — it
misses a quiet, low-complexity `Init`/insert procedure that writes a business table. Write-coverage asks
the REVERSE question: for every table the legacy WRITES, did we extract a rule for at least one writer?
The trap it catches: a prominent downstream batch/poster of a table gets extracted, but the upstream
producer that CREATES the rows never does — so the "post" half exists and the "create" half is a phantom
(the sequence diagram shows an enroll/create step, but no rule implements it). The agent proposes a
classification per written table (EXTRACT = real write path, re-extract it / INFRA = audit/log, skip /
DEAD = unreachable); **you confirm each.** Any table you mark EXTRACT and it's not yet re-extracted blocks
P1 exit. Do this before Phase 4 builds specs on the hole.

---

## Phase 2 — Top-Down (Domain Architect) — the heaviest human phase

**You decide:** business capabilities, **service boundaries** (`BOUNDARY_APPROVE` — the core DDD call),
and the **target stack** (`STACK_CONFIRM` — you own this; the agent must ASK, never infer it from the
legacy version).

**This gate has NO independent automated check** — unlike Phase 4, nothing proves the boundaries are
right except your eye. Do not rubber-stamp. Before approving, check:
- **Cohesion** — each service owns one coherent capability (not two unrelated domains, not one capability
  split three ways).
- **Transaction integrity** — operations that must be atomic stay inside ONE service. A business
  transaction split across services (needing a distributed transaction) is a red flag — raise it now.
- **Data ownership** — each table owned by exactly one service.
- **Coupling** — the CALLS graph is not a mesh. Open `modernization/*-sequence-diagrams.md`; chatty
  cross-service chains mean the boundaries are wrong.

**Open and eyeball:** `modernization/modernized-architecture.md`, `services-composition.md` (the service
catalog), the ERD and sequence diagrams. It's preliminary — 4b reconciles the stack with evidence — so
approve-with-notes if unsure rather than blocking.

---

## Phase 3 — Convergence

**You decide (all about explicit loss and ownership):**
- **Source→target gaps** — features with no target service: assign, create new, or *intentionally drop*.
  Dropping legacy behavior is a business decision you're signing off on.
- **`DATA_OWNERSHIP`** — shared tables: who owns, who calls the API.
- **Cross-cutting implicit-layer ownership** (only if it exists) — a cross-entity invariant that spans
  services, or the shared extensibility engine's home service.

**Open and eyeball:** `assessment/microservice-gap-analysis.md` — every source feature mapped, severity
rated. The acceptance bar is **zero unmapped critical features.**

**Cross-check:** feature matrix is 100% mapped. **CAST only:** Query 2 (Assignment Coverage) reports
orphaned BR-IDs (in graph, assigned to no service).

**🔴 Top-down flow coverage (all modes — the mode-independent omission net):** the feature matrix only
maps what was EXTRACTED, so a flow the design KNOWS about but nobody extracted is silently absent, not
flagged. The agent checks every operation named in the sequence diagrams / workflows for a backing BR;
a design-named flow with ZERO backing rules is an **extraction gap** (the analyst drew the flow, the
extractor never read the logic). You confirm each: re-extract the source behind it, or confirm the flow
isn't real. This is your primary omission net on No-CAST (where write-coverage isn't available); on
CAST/Hybrid it runs alongside write-coverage, catching the same class from the design side.

---

## Phase 4 — Specification Generation — the highest-risk phase

This is where correlated error and "category templating" live (the agent generating plausible-looking
rules that pass numeric gates but aren't implementable). The agent runs a 4-subagent pipeline
(Scout → Extractor → Validator → Tracker); a fresh-context Validator enforces hard internal gates.

**You decide:** `SPEC_REVIEW` per service (missing scenarios / wrong logic), and the frontend decisions
if there's a UI (asset reuse; gateway/BFF/direct API access).

**Open and eyeball** `spec/microservices/<svc>/`:
- `01-business-rules.md` — **Statements contain NO legacy table/column names** (those belong only in
  Logic). Would each Statement make sense if the target had totally different table names? Each rule has
  the 8-dimension Semantic Preservation table.
- `02-domain-model.md` — executable DDL; if there's an Entity State Model, the machine is closed
  (every state reachable, terminals marked, no transition to an undeclared state); invariants have a
  tier and integrity invariants are `db`/`both`; every Database Logic Objects row has backing DDL.
- `06-completion-summary.md` — counts match reality; look for FLAGGED / UNRESOLVED preservation items.
- `extraction-evidence.md` — proof the source files were actually read.
- `spec/shared/cross-service-contracts.md` — any `GAP` row blocks P4 exit.

**🔴 YOUR reconciliation signal — `assessment/shared-convention-reconciliation.md` (Stage 1.5):** the
agent sweeps every service contract and proposes ONE common form for the cross-service conventions
(company/tenant param name, pagination params, list envelope, error shape, casing, auth headers), listing
each service's current form and a per-divergence recommendation (normalize vs keep-as-service-specific).
**The agent WAITS here — it does not touch any spec until you signal.** This is a desired-state decision,
yours to make: open the file, for each divergence confirm "normalize to common" or "keep — this service
is legitimately different," and return it (or say "reconcile as proposed"). Getting this right is the
single biggest lever against frontend wiring pain — every divergence you leave unnormalized is variance
the frontend must absorb later. Any concern left at `GAP` (proposed, not yet reconciled) blocks P4 exit.
Do this BEFORE Phase 4c derives DTOs and tests, or the drift propagates into code.

**The cross-check that matters most — the independent 5-rule test (🔴 SPEC_VALIDATION):** pick 5 random
rules across services and, for each, ask *"could I write a unit test from this Statement + DDL alone,
without the legacy source?"* If **2 or more of 5 fail**, the specs FAIL — reject and re-extract. Also
check: same Statement structure across >3 rules = templating = FAIL; `amount_total` on an identity table
= DDL domain-fit failure. This is your primary defense against the LLM being internally-consistent-but-wrong.
- **CAST only:** rerun Extraction Coverage. **No-CAST:** the 5-rule test + BA review (Phase 4a) are your
  substitutes — lean on them harder.

---

## Phase 4a — BA Rule Validation (mandatory)

This is the human breaking the LLM's correlated-error chain — the highest-value human step in the flow.

**You decide (or the BA does):**
- **Mode A** (approve agent defaults, ~5 min) vs **Mode B** (full BA workshop, days). Mode A is fine for
  low-risk / no-SME-available; Mode B for critical systems.
- **Obsolete drops** — the agent only FLAGS candidates; it never drops a rule itself. You approve every
  drop (you're removing business logic).
- **Load-bearing invariants** — essential truth vs legacy artifact (parallel review track).
- **Extension points** — reproduce / unify / drop per point.
- Safety check: if a rule the agent pre-classified "Core" was marked "Obsolete," the agent will ask you
  to confirm with the consequence spelled out. Read that one carefully.

**Open and eyeball:** `assessment/ba-decision-register.md` (every decision + rationale + who), the
`07-obsolete-rules-appendix.md` per service, the scope-reduction numbers.

**Note:** "mandatory-DB" is NOT a BA choice — whether an integrity invariant is DB-enforced follows from
its nature, not the reviewer's preference.

---

## Phase 4b — Roadmap & Automatibility (iterative)

**You decide:**
- **Improvement mode** — Mode A (apply agent recommendations, ~10 min) vs Mode B (real workshops).
- **`PLACEMENT_REVIEW`** — for each flagged candidate, app-tier vs DB-tier. The principle: app-first by
  default, DB placement only where a set-based/high-volume operation would otherwise become an app-tier
  N-round-trip cliff — and never a blind rebuild of a legacy bottleneck. Skipped (notification only) if
  no candidates were flagged.
- **`STACK_CONFIRM` (the real one)** — this is where Phase 2's preliminary stack is confirmed or revised
  WITH evidence (rule complexity, integration patterns, data shapes, automatibility). Accept or override.

**Open and eyeball — and hold automatibility honestly:** the automatibility score's predictive
relationship to actual outcomes is NOT yet confirmed — treat it as a spec-readiness signal and an
execution-mode control, not a promise about test pass rate. Give at least equal weight to
**`spec/shared/infrastructure-patterns.md`**: an early multi-service engagement's systemic failures
traced to a missing structural layer (incomplete infrastructure conventions + the implicit-system layers
now added) — a blind spot the score wasn't measuring, not proof the score is weak. So scrutinize that
document (health endpoints, error middleware, tenant extraction, startup migration incl. ordered DB-object
migrations) rather than over-indexing on a small automatibility difference. Also open
`placement-review.md` + `placement-decision-register.md` and confirm every `db-*` decision reconciled
into `02-domain-model.md` `### Database Logic Objects` (else generation defaults to app-tier and rebuilds
the bottleneck).

**Watch for rubber-stamp risk in Mode A:** agent-inferred formulas/contracts are annotated
`[Agent-inferred — not validated by SME]`. If any inferred item is on a Critical rule, escalate that
item to Mode B — don't accept an inferred formula on money/compliance logic sight-unseen.

---

## Phase 4c — Test Suite Generation

**You decide:** `SPEC_REVIEW` of each test suite for completeness (mostly agent-autonomous).

**Open and eyeball:** `validation/<svc>/comprehensive-test-suite.sh`:
- Every Active/Core BR-ID has at least one assertion; **no `TODO`/`SKIP`/placeholder** assertions.
- Field names come from `04-api-contract.yaml`, NOT from BR-ID examples (the contract is the naming
  authority — examples may be inconsistent).
- The implicit-layer case-classes are present when their spec section exists: illegal-transition-rejected,
  guard-enforced, invariant-holds, computed-value-non-placeholder, DB-tier-object-behaves. These are what
  later let the fidelity audit tell a real implementation from a stub.
- `08-dtos/` field names match the contract exactly; frontend `09-api-client/` functions each trace to a
  real contract path.

---

## Phase 5 — Implementation

**You decide first — the execution model:**

| Model | When | Graph access |
|-------|------|--------------|
| **A — Pure Kiro** | 1–3 services, complex custom logic, learning the system | Live Neo4j (uses graph MCP tools directly) |
| **B — Transform + Kiro** | 3–10 services, clear boundaries; ATX generates ~80%, Kiro polishes | Sandboxed (no Neo4j) |
| **C — ATX Batch + AI-DLC** | 5+ services, max velocity; batch → deviation log → systemic fixes → gate | Sandboxed |

Decision criteria: number of services, boundary clarity, whether ATX infra is available, team size.

**How the graph feeds the agents (the reconcile-in / export-out protocol — worth knowing, it's easy to
miss):** the orchestrator keeps the graph true and useful around every dispatch:
```
RECONCILE IN:  git pull → detect_br_ids.py --all → fidelity_audit.py --all → reconcile_validation.py <artifact>
EXPORT OUT:    graph_context_export.py --all → commit+push sourcecode/*/_graph-context.md
DISPATCH:      submit the gen/fix job (the agent reads _graph-context.md)
```
Order is load-bearing (export after reconcile, so the agent sees the freshest truth — including what the
previous pass tried and what regressed). This turns the fix loop from blind-retry into targeted-fix.
Model A skips the file and uses the MCP tools live. See `saam-phase5-ai-dlc-implementation.md` for the
full protocol; the scripts are listed in `saam-graph-context.md` → "Orchestrator-Run Scripts."

**One-time (do this ONCE, right after the stack + architecture are settled, before you rely on the
fidelity audit):** calibrate the reachability audit to your project's entry surfaces. The framework
ships `fidelity_audit.py` with a stack-neutral, HTTP-shaped heuristic — it recognizes route
registration for ASP.NET / Spring / Flask / FastAPI / Django / Express / Nest. If your services are
reached any other way (message-queue consumers, scheduled/batch jobs, gRPC, GraphQL resolvers, an MCP
tool surface, or a target language outside the scan set), the audit will emit **false dead-code flags**
until it knows those surfaces. Calibrating it now means every later audit run is trustworthy — no
recurring false flags to hand-wave past at each exit gate.

Give the orchestrator this bounded prompt (it edits YOUR engagement's `graph-mcp` copy — the framework
stays stack-neutral):

> Extend `graph-mcp/scripts/fidelity_audit.py` to recognize this project's actual entry surfaces.
> Our stack: **<languages>**, framework(s): **<web/API framework>**, non-HTTP entry surfaces:
> **<queue consumers / scheduled jobs / gRPC / GraphQL / batch entry points / none>**.
> Rules:
> - Edit ONLY the `ROUTE_REGISTRATION_TOKENS` and `ROUTE_SURFACE_HINTS` constants (add the tokens/
>   filename hints for the surfaces above) and, if our language isn't already in `SOURCE_EXTENSIONS`,
>   add its extension. Do NOT change the reachability algorithm, the graph writes, or the DB-tier
>   exemption.
> - For each token/hint you add, add a one-line comment naming the framework/surface it covers.
> - Show me a diff and a one-paragraph explanation of what each addition recognizes. Do not run it
>   until I approve.

**You validate before it's live.** Read the diff: it should be additive constants only (new strings in
those two tuples + maybe one extension), nothing touching the audit logic. Sanity-check one known
example — a service you KNOW is reached via a queue/job — by asking the orchestrator to run
`fidelity_audit.py --service <that-service>` and confirming it now reports it reachable, not dead. Once
that's right, the audit is calibrated for the whole engagement and every exit-gate fidelity report is
trustworthy from here on. This is the "ready to rock" switch — do it once, up front.

**Per-service exit gate — the agent will NOT present it if `signalStatus = BLOCKED`.** Before you accept
a service, check its signal:
- In Kiro: `graph_implementation_context(serviceId)` — lists BLOCKED rules first with their blocker codes,
  then FLAGGED, then CLEAR.
- Blocker codes: `TEST_FAILING`, `MUTATION_SURVIVED` (Critical rule, weak test), `SPEC_DRIFT_CRITICAL`,
  `OPEN_DEVIATION`, and the structural gates `STATE_MACHINE_NOT_CLOSED`, `MANDATORY_DB_OBJECT_MISSING`.

**Open and eyeball:** `validation/spec-deviation-log.md` — the audit trail. Three types:
- `DEV-CODE` = a bug the agent caught and fixed (informational).
- `DEV-TEST` = the test was adapted because the service doesn't match spec → the service should be fixed;
  becomes a follow-up ticket.
- `SPEC-DRIFT` = spec and code disagree → **you (or the BA) decide which is correct.** Don't let these
  sit unresolved.
Also open `sourcecode/<svc>/implementation-audit.md` — spec ambiguities the agent resolved on its own;
anything flagged "needs human review" is yours.

**Cross-checks — these catch the "green skeleton" (tests pass, does nothing):**
- **Mutation testing** (mandatory for Critical BR-IDs): a surviving mutation means the test is weak — the
  implementation could be wrong and the test wouldn't notice. Blocks the exit gate.
- **Fidelity audit** (`fidelity_audit.py`): flags BR-IDs that are annotated but unreachable (no route
  reaches them) and stubs (reachable but behavioral assertions fail → "implement the effect"). Note:
  db-tier BRs are correctly exempt (a trigger has no app caller by design).
- **CAST only:** run the full reconciliation → `graph_unaccounted_loss()` must be 0.

**Open and eyeball:** `validation/<service>/fidelity-report.md` — the per-service summary the
orchestrator (Kiro) produces at each exit gate (spec promises vs. code reality). Note the actor: the
reachability audit and this report are **orchestrator-run, not the execution engine** — under Model C
the ATX Fargate containers can't produce it (no graph access); Kiro runs `fidelity_audit.py` after it
pulls the branches. If the report isn't there at the gate, that's a bug.

This is where you make the one judgment call the tooling cannot make reliably: **for each
annotated-but-unreachable BR-ID, is it dead code, an orphaned capability, or a false flag?**
- **Dead code** = unreachable AND does nothing real → remove it / downgrade the claim.
- **Orphaned capability** = unreachable BUT the method actually performs the effect the spec names →
  the logic is DONE, only the route is missing. **Wire an endpoint to it, don't reimplement.** (This
  is the exact "feature button does nothing" class QC catches.)
- **False flag** = actually reachable, just not via HTTP. The reachability check is an HTTP-shaped
  heuristic — it does NOT see message-queue consumers, scheduled/batch jobs, or languages outside its
  scan set. A batch/queue-driven service can be flagged "dead" while working fine. Confirm the real
  entry surface; it's not a gap.

The orchestrator proposes a classification with evidence; you confirm or override. Getting it wrong is
expensive every way — orphaned-called-dead is silent capability loss; dead-called-orphaned wires a
route to nothing; a false flag deleted as "dead code" destroys working batch logic. Two preconditions:
the graph must be reconciled from disk (a stale graph invents and hides orphans), and the audit must
have been calibrated to your entry surfaces (the one-time step above) — otherwise non-HTTP services
throw false flags at every gate. The orphaned items you confirm here flow into Phase 6 as the low-cost
"wire the route" fix, not a rebuild.

---

## The critical question: "tests pass" ≠ "it works"

A service can be 100% green on its own test suite and still be broken as a SYSTEM — because per-service
tests use pre-made tokens, mock events, and seed their own data. Two mandatory gates close that, and you
must not stop at "Phase 5 done" without them:

1. **Integration Runtime Smoke Gate (Phase 5, Stage 5):** a REAL token walks gateway → auth → backend
   against the deployed environment, and an unauthenticated request is asserted to be rejected. This is
   the ONLY control that catches auth-not-actually-enforced, tenant-propagation gaps, and cross-service
   schema drift. "Deployed and healthy" is not "correct."
2. **System Integration Validation (Phase 6):** `bootstrap.sh` seeds the minimum data so the empty app
   is usable; `verify-system.sh` checks inter-service connectivity; `user-journey-tests.sh` (derived from
   `spec/07-cross-service-workflows.md`) walks real end-to-end journeys; event flows are verified by
   polling. This is what proves the modernized system reproduces the legacy user journey — not just that
   each service passes in isolation.
3. **Frontend Render-and-Walk Gate (Phase 5, if there's a UI):** the frontend twin of gate 1 — with
   seeded data and a real token, every landing screen must RENDER ROWS (not blank rows — the empty-row
   defect from column/envelope mismatch), entitlement gating must actually gate (not fail-open and show
   every module), nav must land on each journey's ENTRY POINT (not an incidental read-only list), and
   each primary journey must complete end to end. A screen that returns 200 with blank rows, or a nav
   that shows modules the tenant isn't entitled to, is broken — and looks fine until you walk it. An
   honest empty-state on unseeded data is a PASS; blank rows on SEEDED data is a FAIL.

If you only run per-service tests, you have NOT validated the system. Run these.

**Upstream of all this:** most frontend wiring pain is prevented back in Phase 4 by the shared-convention
reconciliation (see the Phase 4 section — your reconciliation signal on
`assessment/shared-convention-reconciliation.md`). Normalizing the cross-service conventions into
`spec/shared/` there means the frontend consumes ONE convention instead of absorbing per-service variance.
If that step was skipped or rushed, expect the "every screen wired slightly differently" problem here.

---

## Phase 6 — Continuous Evolution

Auto-activates after Phase 5 and runs as a loop. Inputs (deviations, bugs, features, spec-drift
resolutions) are routed: code-only fix vs spec-change-needed.

**You decide:** SPEC-DRIFT resolutions (spec-correct vs code-correct), and permanent-deviation ACCEPTs
(legitimately out-of-scope items). Escalations reach you after 3 per-rule / 5 per-service fix attempts —
that's the signal the root cause is elsewhere, not a reason to keep retrying.

**First activation runs Systemic-First Remediation:** catalog ALL failures across ALL services by root
cause and fix patterns affecting ≥3 services first (a handful of systemic issues usually explain the
majority of integration failures). Don't let the agent fix 40 symptoms of one cause one at a time.

**Implicit-layer changes** (state machine, invariant, db-object, extension point) route through the same
loop with re-verification (re-close the machine, regenerate the migration, re-tier) — and remember to
re-baseline hashes with `spec_drift.py --service X --update` after any intentional spec edit, or false
drift fires on the next run.

---

## The graph is your dashboard

You rarely need to read tracking files — the graph answers status questions directly. Most useful:

| Question | Ask Kiro |
|----------|----------|
| Overall engagement status | `graph_phase_status(phase="all")` |
| Everything about one service (with signal status) | `graph_implementation_context(serviceId="MS-03")` |
| Detail + fix recommendation for a deviation | `graph_fix_context(deviationId="...")` |
| Is a service safe to sign off? | `graph_query_nodes(Service, {serviceId})` → check `signalStatus` + `implicitBlockers` |
| What breaks if I change this endpoint/rule? | `graph_impact_analysis(nodeType, nodeId)` |
| Rules that need SME validation | `graph_query_nodes(BusinessRule, {extractionRisk: "High"})` |
| Is modernization complete? (CAST only) | `graph_unaccounted_loss()` — 0 = done |

The SessionStart hook injects engagement status automatically at the top of every session, so you open
each session already knowing where things stand and what's blocked.

---

## No-CAST engagements: what safety net you gave up

If you chose Direct Source (no CAST), be conscious that these guardrails simply do not exist — they are
not "off," they are unavailable, and nothing warns you again after Phase 0:

- **Zero-unaccounted-loss guarantee** (`graph_unaccounted_loss`) — the fully LLM-independent proof that
  every legacy component with business logic is accounted for. This is the single strongest
  omission-detector; without CAST it is gone.
- **Call-pattern and data-access preservation** checks.
- **Extraction/assignment/implementation coverage** reconciliation against legacy structure.

Your substitutes are heuristic extraction coverage (expected yield per LOC), the mandatory comprehensive
test suites, the Phase 4 independent 5-rule validation, the **Phase 3 top-down flow-coverage check**
(every design-named flow has a backing BR — the mode-independent net for the missing-writer class that,
with CAST, write-coverage catches from the data side), and — most importantly — a *thorough* Phase 4a BA
review (not a rubber stamp). On a no-CAST engagement, the BA review + flow coverage ARE your omission
defense. Weight them accordingly.

---

## Telemetry (you don't need to open it)

Every phase writes a YAML file under `.saam/telemetry/`. These are anonymized aggregates for
cross-engagement calibration (they tune confidence weights, automatibility thresholds, complexity
ratios) — NOT operator dashboards. You will never need to open one to make a decision; your status
surface is the graph + the README + the deviation log. Producing them is a hard exit-gate precondition,
so if a phase completes without its telemetry file, that's a process bug, not something you act on.

---

## One-page checklist

- [ ] P0: mode chosen with eyes open (CAST loss disclosed if Direct); segmentation matches the business.
- [ ] P1: BR count in markdown == graph; every rule has an exact source reference.
- [ ] P2: boundaries checked for cohesion / transaction integrity / data ownership / coupling — not
      rubber-stamped; stack was a deliberate answer.
- [ ] P3: zero unmapped critical features; every intentional drop is a conscious sign-off.
- [ ] P4: ran the 5-rule independent test; Statements are legacy-name-free; no templating; cross-service
      contracts have no GAP.
- [ ] P4a: BA review was real (especially no-CAST); every obsolete drop approved.
- [ ] P4b: infrastructure-patterns.md scrutinized; placement decisions reconciled to specs; no inferred
      formula on a Critical rule left unvalidated.
- [ ] P4c: every Active/Core rule tested; no SKIP; field names from the contract; implicit-layer cases present.
- [ ] P5: model chosen; each service `signalStatus=CLEAR` before sign-off; mutation + fidelity clean;
      deviation log triaged.
- [ ] SYSTEM: ran the Integration Runtime Smoke Gate AND System Integration Validation — not just
      per-service tests.
- [ ] P6: systemic-first remediation done; SPEC-DRIFTs resolved; hashes re-baselined after spec edits.
