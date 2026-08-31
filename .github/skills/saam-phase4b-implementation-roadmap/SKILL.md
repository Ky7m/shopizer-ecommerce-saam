---
name: saam-phase4b-implementation-roadmap
description: "Modernization roadmap generation, automatibility scoring, and service implementation sequencing."
copyright: "Copyright 2024-2026 SoftServe Inc. All rights reserved."
authors: "Max Kozinenko, Roman Kalita (SoftServe)"
---

# SAAM Phase 4b: Implementation Roadmap & Automatibility

## Objective

Calculate automatibility scores for every service, produce an improvement plan to raise scores, iterate with the human until scores stabilize, then produce the final implementation roadmap.

Phase 4b is an ITERATIVE process — not a one-shot calculation. The architect drives improvement rounds until automatibility scores reach acceptable levels or the human accepts the current state.

## Required Steering Files (Read Before Proceeding)

The agent MUST read the following steering files before executing Phase 4b:

1. **`.github/skills/saam-human-guidance-protocol/SKILL.md`** — Prompt categories, decision register format, agent rules
2. **`.github/skills/saam-task-tracking/SKILL.md`** — Tracking file format and Jira dual-write protocol

No additional templates are needed — Phase 4b produces its own deliverables (scores, improvement plan, roadmap, team composition).

## Task Tracking Activation

**PRECONDITION: The agent MUST NOT begin automatibility scoring until `tracking/phase4b-automatibility.md` exists.** If it doesn't exist, create it NOW with tasks (initial scores calculated, improvement plan generated, iterations, roadmap finalized, team composition documented) all listed as PENDING.

**PhaseEvent (telemetry timestamp):** Immediately after creating the tracking file, write: `graph_add_node(nodeType="PhaseEvent", id="P4B-started", properties={phase: "P4B", event: "started", timestamp: <current ISO timestamp>})`.

After each major milestone (scores calculated, iteration complete, roadmap finalized), the agent MUST update the tracking file immediately. If Jira is configured, create an Epic with Tasks. See `.github/skills/saam-task-tracking/SKILL.md` for format.

## Entry Precondition

Before starting Phase 4b:
- [ ] Phase 4 specifications exist for all in-scope services (`spec/microservices/<service>/01-business-rules.md`)
- [ ] Phase 4 quality gates have passed (independent validation)
- [ ] Phase 4a has been completed — all rules have classifications and business impact weights (either agent defaults approved or full BA review completed)
- [ ] `modernization/services-composition.md` exists with service catalog

## Automatibility Score Calculation

### Scoring Dimensions

Each service receives a score (0-100%) across these dimensions. Weights are defined in `.github/saam-calibration.yaml` → `automatibility.weights` (defaults shown below):

| Dimension | Weight (calibrated) | What It Measures |
|-----------|--------|------------------|
| **Statement clarity** | 30% | Can each rule be implemented from its Statement alone? |
| **Algorithm completeness** | 25% | Are formulas, rates, and thresholds fully specified? |
| **Integration definition** | 15% | Are all external dependencies, APIs, and events defined with contracts? |
| **Data model readiness** | 15% | Is DDL complete with all columns, types, constraints, indexes? |
| **Edge case coverage** | 15% | Are error cases, boundary conditions, and rejection paths documented? |

**Note:** Read weights from `.github/saam-calibration.yaml` at execution time. The values above are fallback defaults if the calibration file is absent.

### Per-Dimension Scoring

**Statement clarity (0-100%):**
- 100%: Every BR-ID Statement is implementable without reading legacy source
- 80%: 1-2 rules need minor clarification
- 60%: 3-5 rules are ambiguous or reference undocumented behavior
- 40%: Many rules read as code transcriptions, not business statements
- 20%: Majority of rules cannot be implemented from Statement alone

**Algorithm completeness (0-100%):**
- 100%: All calculations have explicit formulas, all rates/brackets are documented, all rounding rules specified
- 80%: Minor gaps (e.g., one rounding mode unspecified)
- 60%: Several calculations missing parameters or thresholds
- 40%: Formulas are described qualitatively ("calculated based on...") without specifics
- 20%: Critical calculations are undefined or reference external sources without content

**Integration definition (0-100%):**
- 100%: Every external call has request/response schema, every event has payload schema
- 80%: Most integrations defined, 1-2 missing details
- 60%: Integration points identified but contracts incomplete
- 40%: "Calls external system" without specifying what/how
- 20%: Critical dependencies undocumented

**Data model readiness (0-100%):**
- 100%: DDL is executable, all columns typed, constraints defined, indexes planned
- 80%: DDL complete but missing 1-2 indexes or constraints
- 60%: Tables defined but some columns lack types or constraints
- 40%: Entity names identified but DDL not written
- 20%: Data model is conceptual only

**Edge case coverage (0-100%):**
- 100%: Every rule has error examples, boundary conditions documented, rejection paths clear
- 80%: Most edge cases covered, 1-2 gaps
- 60%: Happy path clear but several error scenarios undefined
- 40%: Only happy path documented for most rules
- 20%: No error handling specified

### Composite Score Calculation

Each dimension is scored 0-100%. The composite is a weighted average using weights from `.github/saam-calibration.yaml` → `automatibility.weights` (result is also 0-100%):

```
Automatibility Score (%) = (Statement% × clarity_weight) + (Algorithm% × algorithm_weight) + (Integration% × integration_weight) + (DataModel% × data_model_weight) + (EdgeCases% × edge_case_weight)
```

Example (using default weights): Statement=80%, Algorithm=60%, Integration=70%, DataModel=90%, EdgeCases=50%
→ (80×0.30) + (60×0.25) + (70×0.15) + (90×0.15) + (50×0.15) = 24 + 15 + 10.5 + 13.5 + 7.5 = **70.5%**

### Score Interpretation

| Score Range | Classification | Implication |
|-------------|---------------|-------------|
| 90-100% | Fully automatable | Agent can implement with minimal human oversight |
| 75-89% | Highly automatable | Agent implements, human reviews critical sections |
| 60-74% | Moderately automatable | Agent implements with human guidance at decision points |
| 40-59% | Partially automatable | Significant human involvement required |
| < 40% | Manual implementation | Spec quality insufficient for agentic development |

### Minimum Threshold

Thresholds from `.github/saam-calibration.yaml` → `automatibility.thresholds` (defaults shown):

- **Type A (full auto):** ≥ `type_a_minimum` (default: 85%)
- **Type B (assisted):** ≥ `type_b_minimum` (default: 70%)
- **Type C (manual):** below `type_b_minimum`
- **Minimum for implementation:** ≥ `minimum_for_implementation` (default: 75%) — target before starting Phase 5
- **Mandatory improvement:** < `mandatory_improvement_below` (default: 60%) — MUST go through improvement iterations

**Focused Algorithm Extraction Pass (expert heuristic — see calibration `focused_extraction_pass`):**

If a service scores `algorithm_completeness < 75%`, consider a **focused re-extraction pass** before Phase 5 generation:

1. Identify BR-IDs with thin Logic sections (< 5 lines or contains "see source" references)
2. Re-read the source procedure(s) for each (from Source Reference field in 01-business-rules.md)
3. Expand Logic to step-by-step pseudocode: all branches, loop structures, calculation formulas, external lookups
4. Re-score algorithm_completeness after expansion
5. If still < 75% after 2 passes → note as a documented risk

This is agent work (not human co-development) — the same extraction agent, targeted at depth rather than breadth.

**IMPORTANT — empirical status:** This is an expert heuristic, NOT empirically validated. Real test data from ENG-003 (21 services against a real PostgreSQL sidecar) showed automatibility correlates only weakly with actual test pass rate (Pearson r = 0.22). The dominant driver of test failures was **Test-mode dependency isolation** (unmocked messaging, external services, reverse proxies) — not spec depth. Services with 89-91% automatibility (notification-service, platform-gateway) had the LOWEST test pass rates (12-30%) because their Test harness couldn't exercise SNS/Bedrock/YARP dependencies. The bigger lever is a complete `infrastructure-patterns.md` (Test-mode mocking, connection wiring, graceful empty-DB responses), not deeper algorithm extraction.

## Automatibility Score Improvement Plan

After initial scoring, the agent produces an improvement plan. This plan identifies SPECIFIC gaps and the ACTIONS needed to close them.

### Plan Structure

The improvement plan is saved to `modernization/automatibility-improvement-plan.md` and contains two sections:

#### Section 1: Working Sessions Required

Items that need interactive discussion with domain experts, architects, or stakeholders. These are NOT "confirmation workshops" — they are working sessions where the team PRODUCES missing information:

| Item | Service(s) | Gap | Session Goal | Expected Output |
|------|-----------|-----|--------------|-----------------|
| WS-01 | Payment Service | Algorithm: fee calculation formula undocumented | Walk through 3-4 real fee scenarios to derive formula | Complete formula with all variables, rates, brackets |
| WS-02 | Order Service | Edge cases: what happens when partial shipment + partial refund overlap | Architect traces through legacy behavior with SME | 2-3 new BR-IDs covering overlap scenarios |
| WS-03 | Inventory Service | Integration: ERP sync contract undefined | Define payload schema with ERP team | OpenAPI snippet or event schema |

**Rules for working sessions:**
- Each session has ONE clear goal and ONE expected output
- Sessions produce ARTIFACTS (formulas, schemas, rule definitions) — not just "alignment"
- Maximum 60 minutes per session
- Architect prepares specific questions BEFORE the session (listed in the plan)
- After the session, architect updates specs immediately

#### Section 2: Information Requests

Items that can be resolved asynchronously via email, Slack, or documentation lookup. No meeting required — someone provides the answer and the architect updates the spec:

| Item | Service(s) | Gap | Question | Who Can Answer |
|------|-----------|-----|----------|----------------|
| IR-01 | Billing Service | Algorithm: late fee percentage by client tier | "What are the exact late fee rates per tier? (e.g., Gold=1.5%, Silver=2%, Bronze=3%)" | Finance team / config file |
| IR-02 | Auth Service | Integration: SSO provider SAML metadata URL | "What is the IdP metadata endpoint for production SSO?" | DevOps / Identity team |
| IR-03 | Reporting Service | Data model: which fields populate the monthly summary report | "Provide a sample report output with field labels" | Business analyst |

**Rules for information requests:**
- Each request has ONE specific question (not "tell me everything about X")
- Expected response format is stated (number, URL, schema, sample data)
- "Who can answer" is identified so the architect knows who to ask

### Plan Generation Protocol

The agent generates the improvement plan by:

1. For each service, identify dimensions scoring < 80%
2. For each low-scoring dimension, identify the SPECIFIC rules or areas causing the gap
3. For each gap, determine: is this a "working session" (needs discussion/exploration) or "information request" (someone has the answer)?
4. Group items by service and type
5. Prioritize: highest-impact items first (items that would raise score by 10+ points)

### Anti-Patterns in Improvement Plans

❌ "Conduct confirmation workshop to validate approach" — this is a time-wasting rubber stamp
❌ "Review architecture decisions with stakeholders" — too vague, no specific output
❌ "Align on naming conventions" — trivial, handle asynchronously
❌ "Discuss integration patterns" — discuss WHAT specifically? Name the exact contract needed

✅ "Walk through 3 real invoice-dispute scenarios to define resolution rules (expected output: 2-3 new BR-IDs)"
✅ "Derive overtime calculation formula from payroll examples (expected output: formula with all variables)"
✅ "Provide the exact tax bracket percentages for 2024 (expected output: table of brackets and rates)"

## Iteration Workflow

Phase 4b is iterative. Each iteration follows this cycle:

```
┌─────────────────────────────────────────────────────────────┐
│  ITERATION N                                                 │
│                                                              │
│  1. Execute improvement items (sessions + info requests)     │
│  2. Update service specs with new information                │
│  3. Recalculate automatibility scores                        │
│  4. If all services ≥ 75% → finalize roadmap                │
│  5. If gaps remain → generate next iteration plan            │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

### Iteration Protocol (Step by Step)

**Before Iteration 1:**
1. Agent calculates initial automatibility scores for ALL services
2. Agent generates the improvement plan (`modernization/automatibility-improvement-plan.md`)
3. For each gap in the plan, the agent also produces an **agent recommendation** — what the agent WOULD add to the spec if it had to resolve this gap autonomously (e.g., reasonable default formula, assumed integration contract, inferred edge cases)
4. Agent presents scores + plan + recommendations to human:

**🔴 PROMPT HUMAN**: "Automatibility scores calculated. [N] services scored, average [X]%. [Y] gaps identified requiring resolution.

**Mode A: Apply Agent Recommendations (~10 min)**
- I've produced recommended resolutions for each gap (default formulas, assumed contracts, inferred edge cases)
- You review the summary, approve the recommendations (or adjust specific ones), and I reconcile them into the specs
- Best when: no SMEs available, time-constrained, or gaps are minor (scores already > 60%)

**Mode B: Real Workshops + Information Requests (days/weeks)**
- The improvement plan has [X] working sessions and [Y] information requests
- You conduct the sessions, gather the answers, and provide them to me
- I reconcile the new information into the specs and recalculate scores
- Best when: critical gaps exist (scores < 60%), SMEs are available, or accuracy is paramount

Which mode?"

### Mode A: Apply Agent Recommendations

The agent resolves each gap using its best judgment:

1. **For algorithm gaps:** Agent proposes a reasonable formula/logic based on what's available in the source and spec context. Marks each recommendation as `[Agent-inferred — not validated by SME]`.
2. **For integration gaps:** Agent defines a plausible contract structure based on common patterns and available context.
3. **For edge case gaps:** Agent infers likely error scenarios from the happy-path logic.
4. **For data model gaps:** Agent adds missing columns/constraints based on rules that reference them.

After human approves (or adjusts):
1. Agent updates ALL affected service specs (`01-business-rules.md`, `02-domain-model.md`, `03-api-design.md`, `04-api-contract.yaml`)
2. Agent recalculates automatibility scores
3. Agent regenerates the roadmap with updated timelines
4. Agent marks inferred items in the specs so Phase 5 agents know which logic was NOT validated by a domain expert

**Spec reconciliation for Mode A:**
- Every spec change carries an annotation: `[Inferred in Phase 4b — Mode A]`
- These annotations alert Phase 5 to flag potential issues during implementation
- If implementation fails on an inferred rule, the fix is to go back and get the real answer (escalate to Mode B for that specific item)

### Mode B: Real Workshops + Information Requests

**During Each Iteration:**
1. Human provides answers to information requests and/or completes working sessions
2. Agent updates the relevant service specs (`spec/microservices/<service>/01-business-rules.md`, `02-domain-model.md`, `03-api-design.md`, `04-api-contract.yaml`)
3. Agent recalculates automatibility scores for affected services
4. Agent updates the improvement plan (remove resolved items, add new items if discovered)
5. Agent recalculates implementation timeline (lower scores = longer timeline)

**After Each Iteration (Mode B):**
- Agent reports: "Iteration N complete. Scores changed: [Service A: 62% → 78%, Service B: 55% → 68%]. Remaining items: X sessions, Y requests."
- If all services ≥ 75%: proceed to roadmap finalization
- If gaps remain but human wants to stop iterating: document remaining gaps as risks in the roadmap

**Iteration Limit (Mode B):**
- Maximum 5 iterations. If scores haven't stabilized by iteration 5, finalize with current state and document remaining gaps.
- "Stabilized" = no score changed by more than 3 points in the last iteration
- Human can switch to Mode A at any point for remaining unresolved gaps: "Apply agent recommendations for the rest"

### Score Recalculation After Spec Updates (Both Modes)

After updating specs with new information (whether from agent recommendations or real workshop outputs):
1. Re-evaluate ONLY the dimensions that were affected by the update
2. Document what changed: "Algorithm completeness: 55% → 82% (fee formula now fully specified)"
3. Update the composite score
4. Update `modernization/automatibility-scores.md`
5. Regenerate `04-api-contract.yaml` if the domain model or API design changed

## Placement Review (Layer C — decide tier for flagged candidates)

Runs alongside the architecture decisions in this phase (same human-in-the-loop shape as the
improvement plan and tech-stack recommendation). P1 flagged which units are placement candidates and
P4 attached performance evidence. Here the architect DECIDES the tier for each candidate. This is an
**architecture/performance** decision — distinct from the Phase 4a business-intent decisions.

### The principle (do not rebuild the legacy's bottlenecks)

The default tier for ALL logic is the application. DB placement is a deliberate, advised EXCEPTION —
never a preservation default. The legacy often put logic in a giant stored proc not because it was the
right design but because the era/tooling made it easy — and that proc is now the thing that pins the
DB server during a periodic sweep and sits idle the rest of the time. Blindly preserving the legacy
tier just rebuilds that bottleneck in a new language. But app-first must not be BLIND: a set-based,
high-volume operation naively reimplemented as an app-tier row-by-row loop is its own performance
cliff. So the system SURFACES the candidates and the architect decides — with evidence.

### Surface candidates (the placement table)

Produce `modernization/placement-review.md` — one row per PLACEMENT_REVIEW candidate (from P1 flags +
P4 evidence). Empty is a valid, common outcome (all logic stays app-tier):

```markdown
| ID | Service | Target (BR-ID / unit) | Signal(s) | Legacy Tier | Evidence (volume / set-vs-row / frequency / app-tier risk) | Recommended | Decision |
|----|---------|----------------------|-----------|-------------|-----------------------------------------------------------|-------------|----------|
| PLACE-001 | ledger | BR-GL-PST-003 (period rollup) | set-based, report-aggregation | db-proc | ~2M lines/period; set-based; monthly; app loop = 2M round-trips | db-function | <architect fills> |
| PLACE-002 | orders | BR-OR-LST-004 (open-orders list) | high-frequency | app | small result; per-request | app-tier | <architect fills> |
```

Decision values (the architect picks one per row):
- `app-tier` — keep in app (default). Use when the app can do it without a performance cliff.
- `app-with-strategy` — keep in app but with an explicit approach to avoid the cliff: batching,
  streaming/cursor, or a materialized read model. Record the strategy.
- `db-view` / `db-function` / `db-proc` / `db-trigger` — move to the DB tier. Use when set-based /
  high-volume work is genuinely cheaper in-DB and the app-tier risk is real.

### Two modes (mirrors the improvement plan)

- **Mode A (agent recommendation):** the agent fills the `Recommended` column with its best call and a
  one-line rationale per candidate; the human approves or adjusts the batch. Best when candidates are
  few / low-risk or no architect is available. Recommendations are annotated
  `[Agent-recommended placement — not validated by architect]`.
- **Mode B (real workshop):** the architect works the table, deciding each row with the evidence,
  possibly pulling in DBA/performance input. Best when candidates are high-impact.

**🔴 PROMPT HUMAN (PLACEMENT_REVIEW):** "[N] placement candidates flagged (logic whose tier is in
question). Default is app-tier; these are the units where app-tier is risky (set-based / high-volume /
was-a-DB-proc / batch sweeps). **Mode A:** I recommend a tier per candidate, you approve/adjust.
**Mode B:** you decide each with the evidence. Which mode?" (If zero candidates: NOTIFICATION only —
"No placement candidates; all logic stays app-tier.")

### Reconcile decisions back to specs + graph (MANDATORY)

For each decided candidate:
- **If `db-*`:** add a row to the service's `02-domain-model.md` `### Database Logic Objects` table
  (Name / Kind / Implements / Enforces Invariant / Migration Order / Binding / Placement=`P4b:PLACE-<id>`)
  AND ensure the object's executable DDL exists under `### Core Entities`. Annotate the implementing
  BR-ID with `[Placement: db-<kind> — PLACE-<id>]`.
- **If `app-tier`:** no DB object. Optionally annotate the BR-ID `[Placement: app-tier — PLACE-<id>]`.
- **If `app-with-strategy`:** annotate the BR-ID `[Placement: app-with-strategy (<batch|stream|read-model>) — PLACE-<id>]`
  so the generator applies the strategy instead of a naive loop.
- **Decision register:** record each in `assessment/placement-decision-register.md` (columns: ID,
  Service, Target, Signal, Legacy Tier, Decision, Rationale, DecidedBy, Mode).
- **Graph (MANDATORY):** for each candidate:
  - `graph_add_node(nodeType="PlacementDecision", id="PLACE-<id>", properties={targetKind:"BusinessRule", targetId:"<brId>", service:"<svc>", candidateReason:"<signal>", decision:"<tier>", appStrategy:"<if any>", evidence:"<summary>", rationale:"<why>", decidedBy:"<architect|agent-defaults>", mode:"<ModeA|ModeB>"})`
  - `graph_add_edge(edgeType="PLACED_AS", sourceId="PLACE-<id>", sourceType="PlacementDecision", targetId="<brId>", targetType="BusinessRule")`
  - If `db-*`, the `DbObject` node + `IMPLEMENTS_IN_DB` edge are created when `import_specs.py` re-parses
    the updated `02-domain-model.md`. Set `Implementation.tier` at generation time.
  - Run `graph_run_inferences(rules=["lifecycle_states", "effective_confidence"])`.

Without this reconciliation the placement decision is unenforceable — generation would default everything
to app-tier and rebuild the bottleneck the architect just rejected.

## Tech Stack Recommendation (After Iterations Complete)

Once automatibility scores stabilize and the improvement plan is resolved, the agent produces a tech stack recommendation. This is the point where enough evidence exists (rule complexity, integration patterns, data models, automatibility profiles) to make an informed stack decision.

### Why Now (Not Phase 0)

Phase 0 collects a "preliminary" tech stack preference from the human. But that decision is made with zero evidence about what each service actually needs. After Phase 4b, we know:
- Per-service complexity profile (BR count, algorithm density)
- Integration patterns (sync REST, async events, batch)
- Data model shape (relational, key-value, document)
- Automatibility score (what execution model fits)
- Team constraints (from Phase 0)

This is the RIGHT information to make a stack decision.

### Protocol

**Step 1: Generate tech-stack-recommendation.md**

After final scores are calculated, produce `modernization/tech-stack-recommendation.md`:

```markdown
# Tech Stack Recommendation

## Engagement Context
- Preliminary stack (from Phase 2): <what was chosen early>
- Services in scope: <N>
- Average automatibility: <X%>
- Team expertise: <from Phase 0 profile>

## Per-Service Recommendations

### <service-name>
| Aspect | Preliminary | Recommended | Rationale |
|--------|-------------|-------------|-----------|
| Language | <from Phase 2> | <recommendation> | <why — based on complexity, patterns, team> |
| Framework | <from Phase 2> | <recommendation> | <why> |
| Database | <from Phase 2> | <recommendation> | <why — based on data access patterns> |
| Events | <from Phase 2> | <recommendation> | <why> |

**Decision:** [Accept / Override]
**If override, specify:** _______________
**If override, rationale:** _______________

### <next-service>
...

## Architecture Impact Assessment
<If recommendations differ from preliminary: what architecture docs need updating, team impact, timeline impact>

## Constraints Considered
- Team expertise (weighted 40%)
- Service complexity profile (weighted 30%)
- ATX/Transform compatibility (weighted 15%)
- Operational consistency (weighted 15%)

## Global Questions

**Q1: Polyglot tolerance?**
Are you willing to operate multiple tech stacks, or prefer uniformity?
[Answer]: _______________

**Q2: Serverless appetite?**
Would Lambda/serverless be acceptable for low-complexity services?
[Answer]: _______________

**Q3: Team growth plans?**
Will the team acquire new stack expertise, or must we constrain to current skills?
[Answer]: _______________
```

### Recommendation Factors

| Factor | Drives Toward | Evidence From |
|--------|--------------|---------------|
| Pure CRUD, few BRs, event-driven | Lightweight / serverless (Lambda, NestJS) | Phase 4 rule count + automatibility |
| Complex calculations, many constants | Strongly-typed (Java, C#) | Phase 4 complexity vectors |
| Heavy event processing | Stream framework (Kafka Streams, Akka) | Phase 4 integration patterns |
| High concurrency requirements | Go, reactive frameworks | Phase 1 volume analysis |
| Team expertise | Strong weight toward known stacks | Phase 0 team profile |
| ATX compatibility | Stacks ATX generates well (Java Spring, NestJS) | Engagement model choice |

### Step 2: Human Resolution

**🔴 PROMPT HUMAN**: "Tech stack recommendation ready for [N] services.

- [X] services: recommendation matches preliminary (no change needed)
- [Y] services: recommendation differs from preliminary (needs your decision)

**Mode A: Accept SAAM recommendations (~2 min)**
- I'll apply all recommendations and update architecture docs

**Mode B: Review and override**
- Review `modernization/tech-stack-recommendation.md`
- Fill in decisions for each service (Accept/Override)
- Answer the global questions
- Return the document, I'll reconcile

Which mode?"

### Step 3: Architecture Reconciliation

After human responds (Mode A acceptance or Mode B filled document):

1. Parse decisions from `tech-stack-recommendation.md`
2. For each service where stack changed from preliminary:
   - Update `modernization/architecture.md` — technology stack section
   - Update `modernization/services-composition.md` — per-service stack column
   - Flag any impacted architecture decisions (e.g., if switching to serverless, deployment model changes)
3. Recalculate implementation roadmap timelines (different stacks may have different ATX generation speed)
4. Update `modernization/implementation-team-composition.md` if new skills required
5. Record tech stack decisions in the decision register
6. **Generate `spec/shared/infrastructure-patterns.md`** (MANDATORY — see below)

**Architecture reconciliation only happens if recommendations DIFFER from preliminary.** If all services keep their Phase 2 stack, this step is a no-op — just confirm and move on.

### Infrastructure Patterns Document (MANDATORY after tech stack confirmation)

Once the target stack is confirmed, produce `spec/shared/infrastructure-patterns.md`. This is the **single source of truth** for cross-cutting HTTP/runtime conventions that ALL services share. Without it, each service independently invents plumbing — causing systemic test failures and convention drift.

**Required sections (adapt terminology to target stack):**

```markdown
# Infrastructure Patterns — <Target Stack>

## Health Endpoints
- Paths: /health (simple 200), /health/alive (liveness), /health/ready (readiness — checks DB + messaging)
- Response format: JSON { "status": "Healthy|Degraded|Unhealthy" }

## Error Handling Middleware
- Validation errors → HTTP 422, body: { "errors": [{ "field": "...", "message": "..." }] }
- Authentication failures → HTTP 401, body: { "error": "Unauthorized" }
- Authorization failures → HTTP 403, body: { "error": "Forbidden" }
- Not found → HTTP 404, body: { "error": "Resource not found" }
- Unhandled exceptions → HTTP 500, body: { "error": "Internal server error", "correlationId": "..." }
- NEVER return stack traces or framework-default HTML error pages

## Tenant Extraction
- Header name: x-tenant-id
- Format: UUID (validate on extraction, reject with 400 if malformed)
- Injection: scoped DI service (request-lifetime), available in all service/repository layers
- DB query filter: automatic (global query filter on all entities)

## Request/Response Conventions
- All responses: JSON, camelCase field names (matching 04-api-contract.yaml)
- Pagination: { "items": [...], "total": N, "page": N, "pageSize": N }
- Empty collections: return 200 with { "items": [], "total": 0 } (NOT 404)

## Startup / Initialization
- DB migration: run on startup (EnsureCreated or Migrate depending on env)
- Test mode: seed reference data from a deterministic seed script
- Messaging: connect after DB is ready; health/ready waits for messaging connection
- Graceful shutdown: drain in-flight requests, stop consumers, then exit

## Logging / Observability
- Structured logging (JSON in production, console in dev)
- Correlation ID: propagate from request header or generate if absent
- OpenTelemetry: traces for HTTP requests, DB queries, messaging operations
```

**Why this must happen in Phase 4b (not Phase 5):**
- Phase 4c (test suites) needs these conventions to write correct assertions
- Phase 5 (generation) needs these conventions so all services are consistent
- Without it: test suites assume one convention, generators assume another → systemic failures

**Empirical validation (ENG-003):** Missing infrastructure-patterns caused 70% of systemic test failures across 22 services. All services independently invented health endpoints, error responses, and tenant extraction differently. A single document would have prevented the entire class of failures.

### Anti-Pattern: Do NOT Skip This Step

Even if the human said "Java Spring for everything" in Phase 0, this step MUST still run. The recommendation document may confirm "Java Spring is correct for all services" — but it does so with EVIDENCE. Or it may surface that 2 services would genuinely benefit from a different choice.

The human can still override every recommendation. The point is not to force a stack — it's to ensure the decision is informed rather than assumed.

## Implementation Roadmap (Final Output)

Once scores stabilize (or human accepts current state), the agent produces the final implementation roadmap.

### Roadmap Deliverables

All saved under `modernization/`:

1. **`automatibility-scores.md`** — Final scores per service with dimension breakdown
2. **`automatibility-improvement-plan.md`** — Final state (resolved + remaining items)
3. **`implementation-roadmap.md`** — Timeline, phases, dependencies, critical path
4. **`implementation-team-composition.md`** — Roles and allocation per phase
5. **`placement-review.md`** — Layer C placement candidates + decisions (or "none")
6. **`placement-decision-register.md`** — Layer C tier decisions with rationale (if any candidates)

### Timeline Calculation

The roadmap MUST include timelines for ALL THREE Phase 5 execution models so the human can make an informed choice at Phase 5 start. The automatibility score drives duration differently for each model.

#### Per-Model Duration Tables

**Model A: Pure Kiro (Sequential, Interactive)**

| Score | Base Duration (per 20 rules) | Human Oversight Level |
|-------|-----------------------------|-----------------------|
| 90-100% | 1-2 days | Review only (async) |
| 75-89% | 2-4 days | Review critical sections |
| 60-74% | 4-7 days | Pair with human at decision points |
| 40-59% | 7-14 days | Heavy human involvement |
| < 40% | 14+ days | Mostly manual with AI assist |

**Model B: Transform + Kiro (Semi-Automated, per service)**

| Score | Base Duration (per service) | Notes |
|-------|----------------------------|-------|
| 90-100% | 1-2 days | ATX generates ~95%, minimal Kiro fixes |
| 75-89% | 2-4 days | ATX generates ~80%, Kiro fixes + wiring |
| 60-74% | 4-6 days | ATX generates ~60%, significant Kiro rework |
| 40-59% | 6-10 days | ATX struggles, heavy Kiro intervention |
| < 40% | Not recommended | Spec quality too low for Transform |

**Model C: ATX Batch + AI-DLC Pipeline (Maximum Velocity)**

| Score | Stage 1: ATX Batch (all services) | Stage 2: AI-DLC Wiring | Total |
|-------|-----------------------------------|------------------------|-------|
| 90-100% | 1-2 hours (parallel) | 2-3 days | 2-4 days total |
| 75-89% | 1-2 hours (parallel) | 3-5 days | 4-6 days total |
| 60-74% | 1-2 hours (parallel) | 5-10 days | 6-11 days total |
| 40-59% | Not recommended | — | — |
| < 40% | Not recommended | — | — |

#### Multipliers (apply to all models)

Adjust base duration for:
- Service size: rule count beyond 20 scales linearly (×1.5 for 40 rules, ×2 for 60 rules, etc.)
- Integration complexity: each external dependency adds 0.5-1 day
- Data migration needs: each complex migration adds 1-2 days
- Cross-service dependencies: sequential bottlenecks add serial time to parallel models

### Roadmap Structure

The implementation roadmap MUST present ALL THREE models side by side:

```markdown
# Implementation Roadmap

## Summary
- Total services: N
- Average automatibility: X%
- Service with lowest score: <name> at Y%

## Timeline Comparison by Execution Model

| Model | Total Duration | Parallelism | Best For |
|-------|---------------|-------------|----------|
| A: Pure Kiro | X weeks | Sequential (1 service at a time) | Complex services, learning phase |
| B: Transform + Kiro | Y weeks | 1-2 services parallel | Mid-scale, balanced control |
| C: ATX Batch + AI-DLC | Z days | All services parallel (Stage 1) | Maximum velocity, 5+ services |

## Per-Service Estimates (All Models)

| Service | Score | Rules | Model A | Model B | Model C (Stage 1+2) |
|---------|-------|-------|---------|---------|---------------------|
| <service-1> | X% | N | Y days | Z days | included in batch |
| <service-2> | X% | N | Y days | Z days | included in batch |
| ... | | | | | |
| **Total (sequential)** | | | **W weeks** | **X weeks** | **— (parallel)** |
| **Total (parallel)** | | | **N/A** | **Y weeks** | **Z days** |

## Recommended Model

Based on [N] services with average automatibility [X%]:
- **Recommended:** [Model X] because [rationale]
- **Alternative:** [Model Y] if [condition]

## Critical Path: [Service A → Service B → Service C]
(applies to all models — dependency order is the same regardless of execution engine)

## Phase Plan (using recommended model)
### Wave 1: Foundation Services (Week 1-N)
| Service | Score | Rules | Est. Duration | Dependencies |
|---------|-------|-------|---------------|--------------|

### Wave 2: Core Business Services (Week N-M)
...

### Wave 3: Supporting Services (Week M-P)
...

## Parallel Execution Plan
- Track 1: [services that can run in parallel]
- Track 2: [services that can run in parallel]
- Sequential bottleneck: [services that must be serial]

## Risk-Adjusted Schedule
| Risk | Impact on Timeline | Mitigation |
|------|-------------------|------------|

## Remaining Automatibility Gaps
| Service | Score | Gap Description | Impact if Unresolved |
```

### Roadmap Updates on Score Changes

Every time automatibility scores change (during iterations), the roadmap timeline MUST be recalculated:
- Higher scores → shorter timelines → earlier delivery dates
- Lower scores (rare, if new complexity discovered) → longer timelines → later delivery dates
- The roadmap is a LIVING DOCUMENT during Phase 4b iterations

## Deliverables

Phase 4b MUST produce these files (agent creates them, not deferred):

- [ ] `modernization/automatibility-scores.md` — Per-service scores with dimension breakdown
- [ ] `modernization/automatibility-improvement-plan.md` — Working sessions + information requests
- [ ] `modernization/tech-stack-recommendation.md` — Per-service stack recommendation with decisions
- [ ] `modernization/implementation-roadmap.md` — Timeline, phases, parallel tracks, critical path
- [ ] `modernization/implementation-team-composition.md` — Roles per phase
- [ ] `modernization/architecture.md` — UPDATED with confirmed tech stack (reconciled from preliminary)
- [ ] `modernization/placement-review.md` — Layer C placement candidates + decisions (or explicit "none")
- [ ] `assessment/placement-decision-register.md` — Layer C tier decisions with rationale (only if candidates existed)
- [ ] `spec/shared/infrastructure-patterns.md` — Cross-cutting HTTP/runtime conventions (health, errors, tenancy, startup)

### Artifact Existence Gate

Before presenting the exit gate, ALL four files must exist with substantive content. The agent MUST NOT present the exit gate with missing deliverables.

## Exit Gate

**PhaseEvent (completed):** Write: `graph_add_node(nodeType="PhaseEvent", id="P4B-completed", properties={phase: "P4B", event: "completed", timestamp: <current ISO timestamp>})`.

**🔴 PROMPT HUMAN**: "Phase 4b complete.

| Service | Score (before) | Score (after) | Mode | Gaps Remaining |
|---------|---------------|---------------|------|----------------|
| <service-1> | X% | Y% | A/B | N |
| ... | | | | |

Improvement plan: [X items resolved, Y remaining as documented risks]. Implementation timeline: [duration]. Ready for Phase 4c (Test Suite Generation)?"

The human may:
- Accept and proceed to Phase 4c
- Request another improvement iteration (Mode B) for specific gaps
- Switch unresolved Mode B items to Mode A (agent infers the rest)
- Adjust the roadmap manually (agent incorporates changes)

**Next steps after human approval:**
- Activate `.github/skills/saam-phase4c-test-suite-generation/SKILL.md` for comprehensive test suite generation per service
- The Phase 4c file will instruct reading `.github/skills/saam-test-suite-template/SKILL.md` and `.github/skills/saam-api-contract/SKILL.md`
- Update the root `README.md` — add Phase 4b completion summary: automatibility scores per service, implementation timeline, parallel execution plan, team composition

## Telemetry Production (MANDATORY)

**PRECONDITION: The agent MUST produce `.saam/telemetry/phase4b-roadmap.yaml` BEFORE presenting the exit gate prompt.** If the file does not exist after this step, the agent must create it now.

**Data to compute:**

1. **Timing** — infer from task tracker (`tracking/phase4b-automatibility.md`): first task `in_progress` → last task `completed`
2. **Automatibility metrics:**
   - Services scored, iterations needed, mode used
   - Score distribution (high >85%, medium 70-85%, low <70%)
   - Average, lowest, highest automatibility
   - Per-dimension averages (statement_clarity, algorithm_completeness, etc.)
   - Primary blockers (categorized reasons for low scores)
   - Implementation type distribution (Type A/B/C counts)
3. **Improvement effectiveness:**
   - Working sessions planned vs completed
   - Information requests planned vs completed
   - Average score improvement in points across iterations
4. **Planning data (from `.github/saam-calibration.yaml` values used):**
   - Record which calibration version was active during scoring

**Schema:** See `.github/skills/saam-telemetry/SKILL.md` → `phase4b-roadmap.yaml` for the full YAML structure.
