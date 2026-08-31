---
name: saam-graph-context
description: "Guidelines for Neo4j knowledge graph integration, lifecycle state management, and agent context construction."
copyright: "Copyright 2024-2026 SoftServe Inc. All rights reserved."
authors: "Max Kozinenko, Roman Kalita (SoftServe)"
---

# SAAM Knowledge Graph: Agent Context Construction

## Purpose

The SAAM Knowledge Graph is a core subsystem that provides lifecycle tracking, multi-dimensional confidence, context construction, and impact analysis for ALL SAAM projects. It is always active — set up during project bootstrapping (Step 2b) regardless of whether CAST Imaging is configured.

This steering file defines WHEN and HOW to use graph-based context vs. file-based context, the lifecycle state model, and the confidence-driven prioritization system.

## Authority Model (Graph vs. Files)

The graph is NOT a copy of files. It stores what files cannot: relationships, lifecycle states, decisions, and computed confidence. Each entity type has one authoritative source:

| Entity | Authority (source of truth) | Graph Role |
|--------|---------------------------|------------|
| **Business rule text** | Spec file (`01-business-rules.md`) | Index + lifecycle state + relationships + confidence |
| **API contract** | OpenAPI file (`04-api-contract.yaml`) | Validation reference — does NOT duplicate schema content |
| **Human decision** | Graph (primary) | Only place decisions with rationale, authority, and timestamp live |
| **Service ownership** | Graph (primary) | Relationships (ASSIGNED_TO, OWNS, EXPOSES) not expressible in flat files |
| **Implementation** | Source code (files) | Graph tracks location + BR mapping via CLAIMS_IMPLEMENTATION edges |
| **Test result** | CI/test execution | Graph stores outcome + timestamp via TestAssertion.status |
| **Confidence** | Graph (computed) | Derived from evidence edges — not stored anywhere else |
| **Deviation** | Graph (primary) | Tracks delta between intent and reality with full context |

### Implications

- **If graph and spec file disagree on rule text:** The spec file wins. Regenerate the graph node.
- **If graph and code disagree on implementation existence:** The code wins. Re-run `detect_br_ids.py --all` (reconcile), then `fidelity_audit.py --all` (reachability/dead-code).
- **If graph says "no decision exists" but the spec shows a classification:** The graph is missing data — import it.
- **Decisions, deviations, and confidence have NO file representation.** They exist only in the graph. If the graph is lost, decisions must be re-entered.

### Why Both (The Architectural Answer)

Files are authoritative for **content** (what a rule says, what an API looks like, what code does).

The graph is authoritative for **relationships and state** (what lifecycle stage a rule is in, what decision was made about it, whether it's been validated, what depends on what, and how confident we are).

This separation eliminates synchronization risk: the graph never duplicates content that belongs in files. It tracks the *metadata about those files* that enables cross-cutting queries, confidence computation, and automated context construction.

## Core Graph vs. CAST Validation Layer

| Layer | Always Active? | What It Provides |
|-------|---------------|-----------------|
| **Core Graph** | YES — all projects | Lifecycle tracking, confidence dimensions, context construction, impact analysis, inference, implementation detection |
| **CAST Validation Layer** | Only if CAST configured | Extraction coverage, unaccounted loss detection, call pattern preservation (compares against CAST structural data) |

**Core tools (always available):** `graph_add_node`, `graph_add_edge`, `graph_update_node`, `graph_bulk_import`, `graph_query_nodes`, `graph_traverse`, `graph_impact_analysis`, `graph_cypher`, `graph_run_inferences`, `graph_propagate_confidence`, `graph_detect_unused_tables`, `graph_implementation_context`, `graph_fix_context`, `graph_phase_status`

**CAST-only tools (require CAST MCP + SourceComponent data):** `graph_extraction_coverage`, `graph_assignment_coverage`, `graph_implementation_coverage`, `graph_unaccounted_loss`, `graph_call_pattern_preservation`, `graph_reconciliation_report`

## Activation Condition

The Core Graph activates when:
- The `saam-graph` MCP server is configured and responding
- Neo4j contains data (graph has been populated during prior phases)

**This is always true for projects bootstrapped with the SAAM enablement skill** (Step 2b sets up Neo4j and the MCP server unconditionally).

**If Neo4j is temporarily unavailable** (container stopped): The SessionStart hook auto-starts it. If that also fails, scripts exit 1 silently and the agent proceeds with file-based context.

## Automatic Context Injection (Hooks)

The SAAM Knowledge Graph hooks are always active (installed during bootstrapping):

### Hook 1: SessionStart — Engagement Status

**File:** `.github/hooks/graph-session-context.json`
**Script:** `graph-mcp/scripts/session_context.py`
**Trigger:** Every new session

On session start, the agent automatically receives:
- Engagement overview (component count, rule count, service count, test count)
- Per-service implementation status (completeness %, test coverage %, confidence)
- Pending work (services with unimplemented rules)
- Open deviations (top 5, showing what needs attention)
- Reminder to use graph tools for context

**What this replaces:** The agent no longer needs to read tracking files, spec summaries, or run `graph_phase_status` manually to know where things stand. It starts every session informed.

**Fallback:** If Neo4j is unavailable (container down), the script exits 1 silently and no context is injected — the agent proceeds normally with file-based context.

### Hook 2: PreToolUse — Service Context on File Writes

**File:** `.github/hooks/graph-file-context.json`
**Script:** `graph-mcp/scripts/file_context.py`
**Trigger:** Before `fs_write`, `str_replace`, or `fs_append` on files matching `sourcecode/`

When the agent is about to write to a file in `sourcecode/<service>/`, it automatically receives:
- The service's API endpoints (paths, methods, expected status codes)
- Contract field names (from the graph's Field nodes)
- Pending rules for that service (BR-IDs not yet implemented)
- Open deviations for that service (known issues to be aware of)
- Reminder that field names MUST come from `04-api-contract.yaml`

**What this replaces:** The agent can't write code with wrong field names because the correct names are IN context before the write happens. It can't forget pending rules because they're surfaced automatically.

**Scope:** Only activates for `sourcecode/` file paths. Writing to `spec/`, `validation/`, or other directories does not trigger this hook.

**Fallback:** If the file path doesn't match `sourcecode/<service>/`, or Neo4j is unavailable, the script exits 1 and the write proceeds normally without graph context.

## Context Automation Levels

The SAAM graph provides context at four levels of automation:

| Level | Mechanism | What It Does | Status |
|-------|-----------|-------------|--------|
| **L1: Agent-initiated** | `graph_implementation_context`, `graph_fix_context` tools | Agent explicitly queries graph before starting work | Implemented |
| **L2: Session-start injection** | SessionStart hook → `session_context.py` | Agent receives engagement status automatically on every session | Implemented |
| **L3: File-write injection** | PreToolUse hook → `file_context.py` | Agent receives service endpoints/fields/rules when writing to sourcecode/ | Implemented |
| **L4: Implementation tracking** | Orchestrator reconcile → `detect_br_ids.py --all` | Projects BR-ID annotations from the source tree into CLAIMS_IMPLEMENTATION edges. Run by the Kiro orchestrator at code-landing checkpoints (after pull, P5 exit, SessionStart) | Implemented |

**L1** is always available (agent calls tools when it thinks to).
**L2+L3** are automatic — context arrives without the agent asking.
**L4** closes the loop: the orchestrator reconciles the graph against the source tree, keeping completeness scores true.

### L4 Details: BR-ID Annotation Convention + Orchestrator-Only Population

SAAM Rule SAAM-07 requires every method implementing a business rule to have a BR-ID reference in a comment:

```java
// BR-OR-VAL-001: Order total cannot exceed credit limit
public void validateOrderTotal(Order order) { ... }
```

**Graph population is ORCHESTRATOR-ONLY and mode-independent.** The knowledge graph is a projection
of the source tree, maintained by ONE actor (the Kiro orchestrator) via ONE idempotent operation:
`detect_br_ids.py --all`. There is NO per-file-save hook — it fragmented per execution mode and
silently missed bulk-landed code (ATX batch, git pull, fix loops never trigger PostFileSave).

**Why orchestrator-only:** generation/test/validate/fix agents — especially Model C (ATX batch on
Fargate) — run sandboxed with NO Neo4j access. Only the orchestrator has both the local Neo4j
connection and visibility of landed code. Sandboxed agents' only graph responsibility is to LEAVE
BR-ID annotations in the code; the orchestrator HARVESTS them after pulling. Graph population is
NEVER delegated to a sandboxed agent.

`detect_br_ids.py --all` (idempotent, MERGE):
1. Scans `sourcecode/**` for `BR-[A-Z]{2}-[A-Z]{2,4}-\d{2,3}` patterns
2. Creates/updates `Implementation` nodes
3. Creates `CLAIMS_IMPLEMENTATION` edges (claim only — not yet validated)
4. Recalculates service `implementationCompleteness`

**Reconcile checkpoints (all orchestrator actions with Neo4j access):**
- After ANY operation that lands code into `sourcecode/` (git pull, branch merge, ATX retrieval, fix-loop pull)
- Phase 5 exit gate (before the Implementation Fidelity report reads the graph)
- SessionStart (the reconcile hook corrects any drift since last session)

Idempotent → safe to run after every pull; a missed reconcile self-heals at the next checkpoint.

**Edge progression:**
- `CLAIMS_IMPLEMENTATION` — BR-ID annotation found in code (created by detect_br_ids.py). This is a *claim*, not proof.
- `TESTED_BY` — a test assertion exists targeting this BR (created in Phase 4c)
- `VALIDATED_BY` — test passes for this rule (created/updated when TestAssertion.status = PASS). This is the *evidence* edge.
- `RECONCILED_WITH` — CAST structural evidence confirms path coverage (optional, CAST mode only)

**Invocation (from workspace root — NOT `--directory`, which changes CWD away from the tree):**
```bash
uv run --project graph-mcp python graph-mcp/scripts/detect_br_ids.py --all
```

### Actionable State: What the Graph Uniquely Holds (Egress Foundation)

The graph's value is NOT re-storing what spec files or TEST_RESULTS.json already contain. Its value
is the state that no single artifact can express — the **actionable delta** that changes an agent's
next decision. Before the graph is used to inform generation/fix agents (the egress path), it must
hold these four, each set by an orchestrator-run script:

| Actionable state | Property | Set by | Why spec/TEST_RESULTS can't hold it | Fix action it drives |
|------------------|----------|--------|-------------------------------------|----------------------|
| **Reachability** | `Implementation.reachable`, `BusinessRule.deadCode` | `fidelity_audit.py` | An unreachable annotated method looks like an untested endpoint in TEST_RESULTS — no hint it's dead code | "Wire it to a route" (not "re-implement") |
| **Behavioral status** | `BusinessRule.behavioralStatus` (unimplemented\|stub\|partial\|real) | `reconcile_validation.py` (from behavioral assertions) | A stub returns the right shape with 200 — shape-level tests pass; the effect is absent | "Implement the effect per the workflow recipe" (not "it's missing") |
| **Deviation history** | `Deviation.occurrences`, `regressedCount`, `attemptLog` | `reconcile_validation.py` | TEST_RESULTS.json is stateless — it knows only THIS run | "You tried X twice and it regressed — root cause is elsewhere; stop retrying" |
| **Cross-service shapes** | `CALLS.requestShape/responseShape/verified` | `import_specs.py` (from cross-service-contracts.md) | Each service's spec only describes ITS own contract, not the reconciled pair | "Call provider endpoint X with exactly this shape" |

**The test for storing anything in the graph:** *would it change the agent's next action?* If not,
don't store it and don't export it — raw statements, full rule logic, and test output text already
live in the spec files and TEST_RESULTS.json; duplicating them into the graph (or an export) just
spends context budget without changing a decision.

**Why this matters for egress (the graph→file→agent path, orchestrator-mediated):** the whole point
of feeding graph data to a sandboxed generation/fix agent is that it's ACTIONABLE — it tells the
agent to do something different than it would from the spec + test output alone. The four states
above are exactly that. Reachability + behavioral status are the memory of the fidelity audit across
passes; deviation history is the loop-stop memory; cross-service shapes are the Class-A knowledge.
Together they turn "blind retry against test output" into "targeted fix against known state."

### Egress: Graph → File → Sandboxed Agent (the closed loop)

Ingest (`detect_br_ids --all`, `fidelity_audit --all`, `reconcile_validation`) keeps the graph TRUE.
Egress (`graph_context_export`) makes it USEFUL — it projects the actionable slice into a committed
per-service file the sandboxed agents can read.

```
Kiro orchestrator (has Neo4j)
  reconcile IN:  tree/results → graph   (detect_br_ids, fidelity_audit, reconcile_validation)
  export  OUT:   graph → sourcecode/<service>/_graph-context.md   (graph_context_export)
  commit + push  (git is the ONLY channel into a sandboxed container)
        ▼
Sandboxed gen/fix agent (ATX Fargate / fix container — NO Neo4j)
  clones branch → reads _graph-context.md via its TD reference list → targeted fix
```

**Rules:**
- **Orchestrator-only.** Only Kiro runs export (it needs Neo4j). Agents never query the graph.
- **Committed, not gitignored.** The file must be on the branch or the container never sees it.
- **Regenerated every dispatch.** Never hand-edited; the orchestrator overwrites before push.
- **Actionable-only content.** Dead code, stubs, deviation history, cross-service shapes, priority —
  nothing already in the spec or TEST_RESULTS.json.
- **Not a naming authority.** `04-api-contract.yaml` + `08-dtos/` remain authoritative; the export
  says what to FIX and what NOT to retry.
- **Order matters.** Export runs AFTER reconcile so the file reflects the freshest truth, including
  what the previous pass did.
- **Mode split.** File path for Model B/C (sandboxed). Model A (Kiro inline, has Neo4j) uses the MCP
  tools live (`graph_implementation_context`, `graph_fix_context`) — no file needed.

**Primitive (from workspace root):**
```bash
uv run --project graph-mcp python graph-mcp/scripts/graph_context_export.py --all
```
See `.github/skills/saam-phase5-ai-dlc-implementation/SKILL.md` → "Reconcile-In / Export-Out Dispatch Protocol" for the
full pre-dispatch sequence.

## BR-ID Lifecycle State Model

Every business rule in the graph has a `lifecycleState` that progresses through a defined sequence. This is NOT a binary "implemented or not" — it captures the full verification journey:

```
Extracted → Assigned → Declared → Tested → Passing
                                              ↑
                                    (future: → Verified)
```

| State | Meaning | Evidence That Advances It | Confidence Impact |
|-------|---------|--------------------------|-------------------|
| **Extracted** | Rule exists in spec with source reference | Phase 1/4 extraction | provenanceConfidence set from extraction confidence |
| **Assigned** | Rule belongs to a specific target service | Phase 3 ASSIGNED_TO edge | No confidence change |
| **Declared** | Code claims to implement it (BR-ID annotation found) | detect_br_ids.py finds pattern → CLAIMS_IMPLEMENTATION edge | implementationConfidence = 0.5 (claim only) |
| **Tested** | A test assertion exists targeting this rule | TESTED_BY edge created in Phase 4c | testQualityConfidence = 0.6 |
| **Passing** | ALL test assertions for this rule pass | TestAssertion.status = PASS → VALIDATED_BY edge created | implementationConfidence = 0.9, testQualityConfidence = 0.9 |

**Future state (documented, not active):**

| State | Meaning | When It Will Be Added |
|-------|---------|----------------------|
| **Verified** | Production telemetry confirms the rule fires correctly | When runtime monitoring integration is built (structured logging, feature flags, observability metrics). NOT included in effectiveConfidence until then — including it would make all rules 0.0 during development. |

### Multi-Dimensional Confidence (Weakest-Link Model)

Each rule carries THREE active confidence dimensions. The `effectiveConfidence` is the MINIMUM of all active dimensions — the weakest link determines the rule's overall trust level.

| Dimension | What It Measures | Score Drivers |
|-----------|-----------------|---------------|
| **provenanceConfidence** | Was the rule extracted correctly? | Extraction confidence from Phase 1 (0.5-0.9), BA validation → 1.0, Phase 4b Mode A inference → capped at 0.6 |
| **implementationConfidence** | Is the code correct? | Declared=0.5, Tested=0.5 (code exists, test exists), Passing=0.9 (test proves it works) |
| **testQualityConfidence** | Does the test validate the right behavior? | Contract-aligned=0.9, DEV-TEST deviation (test adapted)=0.6, no test=not set |

```
effectiveConfidence = min(provenanceConfidence, implementationConfidence, testQualityConfidence)
```

Only dimensions that ARE SET are included in the min(). If a rule only has provenanceConfidence (just extracted, not yet implemented), effectiveConfidence = provenanceConfidence.

### How Agents Use Dimensional Confidence

When `graph_implementation_context` is called, rules are **sorted by effectiveConfidence (lowest first)** and split into two groups:

**ATTENTION NEEDED (effectiveConfidence < 0.7):**
- Shows the weakest dimension and its score
- Tells the agent exactly what to fix:
  - weakest = `provenance` → rule may be incorrectly extracted, ask human/BA to validate
  - weakest = `implementation` → code doesn't exist or test isn't passing, implement/fix it
  - weakest = `testQuality` → test was adapted (DEV-TEST), fix the service to match spec

**Passing (effectiveConfidence >= 0.7):**
- Rules the agent can trust — implementation exists and tests pass (or are close)
- These don't need immediate attention

### Example Context Output

```
BUSINESS RULES (42 Active/Core, 45 total):
----------------------------------------

  *** ATTENTION NEEDED (5 rules with confidence < 0.7): ***

  BR-OR-VAL-003 [Declared] confidence=0.50 (weakest: implementation=0.50)
    [Core/Critical]
    Statement: Order total cannot exceed customer credit limit
    → Agent action: implement this rule and make its test pass

  BR-PA-CAL-007 [Tested] confidence=0.55 (weakest: provenance=0.55)
    [Active/High]
    Statement: Late fee calculated at base rate × tier multiplier
    → Agent action: this was agent-inferred (Phase 4b Mode A) — verify with human before implementing

  BR-OR-INT-002 [Passing] confidence=0.60 (weakest: testQuality=0.60)
    [Core/High]
    Statement: Payment confirmation triggers inventory reservation
    → Agent action: test was adapted (DEV-TEST deviation) — fix service to match spec

  --- Rules at confidence >= 0.7 (37): ---

  BR-OR-VAL-001 [Passing] confidence=0.90
    [Core/Critical]
    Statement: Every order must have at least one line item
```

### State Regression

Lifecycle states can regress in specific situations:
- **Passing → Tested**: if a previously passing test starts failing (code was broken by a later change)
- **testQualityConfidence drops to 0.6**: if a DEV-TEST deviation is logged against a rule (test was adapted)

The inference engine (`graph_run_inferences`) handles both advancement and regression automatically.

## When to Use Graph Context vs. File Reading

| Task | Graph Context (preferred) | File Reading (fallback) |
|------|--------------------------|------------------------|
| Implement a service | `graph_implementation_context(serviceId)` | Read 01-business-rules.md + 02-domain-model.md + 03-api-design.md + 04-api-contract.yaml |
| Fix a deviation | `graph_fix_context(deviationId)` | Read deviation log + find BR-ID + read spec + find test |
| Fix a bug (by BR-ID) | `graph_fix_context(brId)` | Read service spec + find implementation + find tests |
| Check phase progress | `graph_phase_status(phase)` | Read tracking files |
| Impact of changing a rule | `graph_impact_analysis(nodeType, nodeId)` | Grep across spec, test, and code files |
| Find all rules for a service | `graph_query_nodes(BusinessRule, {service: X})` | Read 01-business-rules.md |
| Find unimplemented rules | `graph_implementation_coverage(service)` | Cross-reference tracking file with spec |
| Validate modernization fidelity | `graph_reconciliation_report(phase)` | Manual CAST queries + spec review |
| Get service dependencies | `graph_traverse(Service, id, outgoing, [CALLS])` | Read architecture docs |
| Find what a change breaks | `graph_impact_analysis(Endpoint, path)` | Grep for path across test files |

**Rule:** If the graph has the data, use the graph. It's faster, more complete, and pre-resolves relationships. Only read files when:
1. The graph hasn't been populated yet (early in Phase 0-1 before bulk import)
2. You need the FULL text of a rule's Logic/pseudocode section (graph stores statements but may not store full logic blocks)
3. You need to modify a file (graph is read-only context — mutations go to files, then graph is updated)

## Context Construction Patterns

### Pattern 1: Before Implementing a Service

```
STEP 1: Get structured context from graph
  → graph_implementation_context(serviceId="MS-01")
  Returns: BR-IDs + statements, tables + columns, endpoints + status codes,
           dependencies, open deviations, confidence score

STEP 2: Read the API contract file (for exact field names — graph may not have all schema details)
  → Read spec/microservices/<service>/04-api-contract.yaml

STEP 3: Begin implementation using graph context as the primary reference
  The graph output replaces reading 01-business-rules.md, 02-domain-model.md, and 03-api-design.md
```

### Pattern 2: Before Fixing a Deviation

```
STEP 1: Get fix context from graph
  → graph_fix_context(deviationId="CAT-04")
  Returns: what spec says, what service does, fix recommendation,
           affected endpoint, related tests, implementation method

STEP 2: Apply the fix to the code (using spec/contract as truth)

STEP 3: Update the graph
  → graph_update_node(Deviation, "CAT-04", {status: "RESOLVED", resolvedIn: "phase-6-iter-1"})
```

### Pattern 3: At a Phase Gate (Validation)

```
STEP 1: Run reconciliation
  → graph_reconciliation_report(phase="phase-5")
  Returns: extraction coverage, assignment coverage, implementation coverage,
           unaccounted loss count, call pattern preservation

STEP 2: Run inferences (update derived data)
  → graph_run_inferences()
  → graph_propagate_confidence()

STEP 3: Report to human with metrics from graph
```

### Pattern 4: Impact Analysis Before a Change

```
STEP 1: Before modifying a business rule, check what it affects
  → graph_impact_analysis(nodeType="BusinessRule", nodeId="BR-OR-005")
  Returns: which service, which implementation method, which test assertions,
           which deviations reference it

STEP 2: Make the change to the spec file

STEP 3: Update all affected downstream (tests, implementation)

STEP 4: Update graph to reflect changes
  → graph_update_node(BusinessRule, "BR-OR-005", {statement: "...", _lastUpdated: "..."})
```

### Pattern 5: Phase 6 Continuous Evolution

```
STEP 1: Check open deviations for a service
  → graph_fix_context(service="MS-01")
  Returns: all open DEV-TEST and SPEC-DRIFT items with context

STEP 2: Process each deviation using its graph context
  For each: the graph tells you exactly what's wrong and what to do

STEP 3: After fixing, update graph and re-run confidence
  → graph_update_node(Deviation, id, {status: "RESOLVED"})
  → graph_propagate_confidence(service="MS-01")
```

## Graph Population Protocol

The graph must be populated as phases execute. Each phase adds specific node/edge types:

| Phase | What to Add | Tool |
|-------|------------|------|
| Phase 1 | SourceComponent nodes (from CAST), BusinessRule nodes, EXTRACTED_FROM edges | `graph_bulk_import(phase="phase-1")` |
| Phase 2 | Service nodes | `graph_bulk_import(phase="phase-2")` |
| Phase 3 | ASSIGNED_TO edges (BR → Service) | `graph_bulk_import(phase="phase-3")` |
| Phase 4 | Table, Endpoint, Field nodes; OWNS, EXPOSES, MAPS_TO edges | `graph_bulk_import(phase="phase-4")` |
| Phase 4a | Decision nodes, DECIDED_AS edges | `graph_bulk_import(phase="phase-4a")` |
| Phase 4c | TestAssertion nodes, TESTED_BY edges | `graph_bulk_import(phase="phase-4c")` |
| Phase 5 | Implementation nodes, CLAIMS_IMPLEMENTATION edges | Orchestrator reconcile: `detect_br_ids.py --all` (idempotent, at code-landing checkpoints) |
| Phase 5 | Deviation nodes, DEVIATES_FROM edges; VALIDATED_BY edges (on test pass) | `graph_add_node` / `graph_add_edge` (orchestrator, at exit gate) |
| Phase 6 | Deviation status updates, new BR-IDs (features), resolved items | `graph_update_node` / `graph_add_node` |

**When to use `graph_bulk_import` vs individual tools:**
- End of a phase (batch of new nodes/edges): `graph_bulk_import`
- During implementation (one BR-ID at a time): `graph_add_node` + `graph_add_edge`
- Updating existing data: `graph_update_node`

## Confidence-Driven Decision Making

The graph tracks confidence scores at every level. Use them to prioritize work:

```
graph_phase_status(phase="all")
→ Shows per-service confidence scores

Low confidence (< 0.7) means:
  - Rules were agent-inferred (Phase 4b Mode A) — not SME-validated
  - Tests are failing — implementation doesn't match spec
  - Open deviations exist — service has known spec compliance gaps

High confidence (> 0.9) means:
  - All rules are test-verified (tests pass)
  - No open deviations
  - Rules came from direct source extraction with high confidence
```

**Use confidence to decide what needs human attention:**
- Services at < 0.7 → flag for human review before deployment
- Rules with `extractionRisk: High` → flag for SME validation
- Tables marked `candidateForRemoval` → confirm with architect before dropping DDL

## Integration with Standard SAAM Workflow

The graph subsystem augments the file-based workflow per the Authority Model (see top of this document):

1. **Files are authoritative for content.** Spec files, API contracts, and test suites are what gets committed to git and what drives code generation.
2. **Graph is authoritative for relationships, state, decisions, and confidence.** These have no natural file representation.
3. **Updates flow: files → graph (for content); graph is primary (for decisions/deviations).** When a spec file changes, the graph must be updated. Decisions and deviations are written directly to graph.
4. **If graph and file disagree on content:** The file wins. Regenerate the graph node from the file.

## Fallback Protocol

If the graph server is unavailable (Neo4j down, MCP connection failed):

1. Log: "SAAM Graph unavailable — falling back to file-based context"
2. Read spec files directly (standard Phase 5 Step 1 protocol)
3. Skip reconciliation queries (can be run later when graph is back)
4. Skip confidence propagation (recalculate when graph returns)
5. Continue all other work normally — the graph is not a blocker

## Quick Reference: Most Useful Tools Per Situation

| Situation | Tool to Call | What You Get |
|-----------|-------------|--------------|
| "I'm starting to implement MS-03" | `graph_implementation_context(serviceId="MS-03")` | All rules, tables, endpoints, deps, deviations |
| "Test #14 is failing on CAT-04 deviation" | `graph_fix_context(deviationId="CAT-04")` | Exact deviation details + what to fix + where |
| "What's the overall project status?" | `graph_phase_status(phase="all")` | Component/rule/service/test counts + confidence |
| "If I change this endpoint, what breaks?" | `graph_impact_analysis(nodeType="Endpoint", nodeId="/api/v1/orders")` | Affected rules, tests, deviations |
| "Which rules haven't been implemented yet?" | `graph_implementation_coverage(service="MS-03")` | Grouped: no impl / impl but no test / test failing |
| "Is our modernization complete?" | `graph_unaccounted_loss()` | Zero = done; N = gaps to investigate |
| "Show me high-risk extractions" | `graph_query_nodes(nodeType="BusinessRule", filters={extractionRisk: "High"})` | Rules needing SME validation |
| "What depends on OrderService?" | `graph_traverse(startNodeType="Service", startNodeId="MS-02", direction="incoming", edgeTypes=["CALLS"])` | All callers |

## Orchestrator-Run Scripts (not MCP tools — run from workspace root, need local Neo4j)

These are the reconcile-in / export-out primitives. They are NOT MCP tools (they run as scripts because
sandboxed generation/fix agents have no Neo4j); the orchestrator runs them around every Phase 5/6
dispatch. See the full dispatch protocol in `.github/skills/saam-phase5-ai-dlc-implementation/SKILL.md`.

| Script | Purpose | When |
|--------|---------|------|
| `detect_br_ids.py --all` | Harvest BR-ID annotations from code → CLAIMS_IMPLEMENTATION edges | after any code lands |
| `fidelity_audit.py --all` | Reachability → `Implementation.reachable` / `BusinessRule.deadCode` (db-tier exempt) | after code + before fidelity report |
| `reconcile_validation.py <artifact>` | Test results → deviations, behavioralStatus, promote/regress lifecycle | after a validation run |
| `spec_drift.py --service X` / `--update` | Detect (or re-baseline) spec-hash drift, incl. the `02` state/invariant/db-object sections | after spec edits |
| `graph_context_export.py --all` | EGRESS: graph actionable state → committed `sourcecode/<svc>/_graph-context.md` for sandboxed agents | before dispatching a gen/fix job |
| `import_specs.py --service X` / `--all` | Deterministic spec → graph population (BR/Table/Endpoint + EntityState/Invariant/DbObject/ExtensionPoint) | Phase 4 Tracker + `--check` in Validator |
