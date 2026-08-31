---
name: saam-phase1-bottom-up
description: "Bottom-up source analysis procedures for extracting legacy data structures, transactions, and business rules."
copyright: "Copyright 2024-2026 SoftServe Inc. All rights reserved."
authors: "Max Kozinenko, Roman Kalita (SoftServe)"
---

# SAAM Phase 1: Bottom-Up Analysis (Source Architect)

## Role
The Source Architect builds understanding of what the system DOES by reading code or querying CAST data.

## Required Steering Files (Read Before Proceeding)

The agent MUST read the following steering files before executing Phase 1:

1. **`saam-human-guidance-protocol.md`** — Prompt categories, decision register format, agent rules
2. **`saam-task-tracking.md`** — Tracking file format and Jira dual-write protocol
3. **`saam-source-reading-<stack>.md`** — The source reading guide matching the project's legacy stack (determined in Phase 0). For example:
   - IBM i / RPG → `saam-source-reading-ibm-rpg.md`
   - .NET Framework → `saam-source-reading-dotnet.md`
   - COBOL → `saam-source-reading-cobol.md` (create if needed)
   - Java EE → `saam-source-reading-java-legacy.md` (create if needed)
4. **`saam-cast-imaging-integration.md`** — **(MANDATORY if CAST or Hybrid mode)** Full CAST-guided extraction workflow: service brief assembly, component classification, file targeting, coverage validation. This file defines HOW the orchestrator uses CAST to build subagent context. Skipping it in CAST mode = blind extraction.

The source reading guide provides stack-specific patterns for identifying business rules, call structures, data access, and integration points. Skipping it results in missed extraction patterns.

## Entry Precondition: Verify Phase 0 Outputs

Before beginning Phase 1, the agent MUST verify that Phase 0 produced its deliverables:

- [ ] `inventory/INDEX.md` exists — contains system profile, component breakdown, naming conventions, segmentation strategy (this is the Phase 0 system inventory — NOT the same as `tracking/phase5-implementation/INDEX.md` which is a Phase 5 artifact)
- [ ] Analysis mode is documented (Direct Source / CAST Imaging / Hybrid)
- [ ] Source is available: either `initial-source/<system>/` has files (Direct/Hybrid) or CAST MCP is verified (CAST/Hybrid)

**If `inventory/INDEX.md` does not exist:** The agent MUST NOT proceed with Phase 1. Inform the user: "Phase 0 inventory is missing. The system profile, component breakdown, and segmentation strategy must be documented before extraction can begin. Should I run Phase 0 first?"

## Output Location (IMPORTANT — do not confuse directories)

| Directory | Phase | What Goes There |
|-----------|-------|----------------|
| `inventory/` | Phase 0 | System profile, component counts, naming conventions (`INDEX.md`) |
| `assessment/` | Phase 1 and 3 | Extraction summaries, gap analysis, convergence reports |
| `modernization/` | Phase 2 | Architecture decisions, service catalog, roadmap |
| `spec/` | Phase 4 | Microservice specifications, API contracts |

**Phase 1 outputs go to `assessment/` — NEVER to `inventory/`.** The `inventory/` directory is read-only after Phase 0 (it's the baseline reference for what the legacy system contains).

## Graph Population (Incremental — During Phase 1)

The agent MUST update the knowledge graph incrementally as each segment is analyzed — NOT wait until the exit gate to bulk import everything.

### STEP 0 (CAST / Hybrid mode — MANDATORY, runs BEFORE any extraction): Ingest the FULL CAST business inventory

**This is the load-bearing step for the zero-unaccounted-loss guarantee.** Before extracting a single
rule, create a `SourceComponent` node for EVERY business-layer component CAST reports — not only the
ones you will end up extracting a rule from. This makes the coverage/loss denominator the *full legacy
inventory*, so a component you never walk is still visible as `extracted=false` (a gap), instead of
being invisible because no node was ever created for it.

> **Why this exists:** the reconciliation queries (`graph_extraction_coverage`, `graph_unaccounted_loss`)
> count `SourceComponent` nodes. If nodes are created ONLY during extraction (one per component walked),
> the denominator is the ingested subset and coverage falsely reports ~100% while the majority of the
> legacy is silently missing. Full-inventory ingestion up front is what makes the guarantee measure
> against the legacy, not against itself. See `saam-graph-validation.md`.

Protocol:
1. Query CAST for the full business-layer component inventory (stored procedures, programs, modules,
   forms that carry business logic — with `castId`, `name`, `type`, `complexity`, `module`).
2. For EACH, create a node with `extracted=false` and a first-pass classification:
   ```
   graph_add_node(nodeType="SourceComponent", id=<castId>, properties={
     name, type, complexity, module,
     businessLayer: <true|false>,      # false = infra/utility/getter/report/framework (excluded from the denominator)
     intentCategory: <post|entry|validate|derive|distribute|util|report|unknown>,
     extracted: false
   })
   ```
   - **businessLayer classification (same absence-of-signal principle as intent):** exclude ONLY on
     POSITIVE evidence of non-business — pure getters, technical/framework utilities, reports, tax-table/
     lookup-variant families that carry no distinct business logic. **When unsure, mark
     `businessLayer: true`** — never exclude merely because you lack information. Over-inclusion is safe
     (it shows as a gap to confirm and stays in the denominator); silent exclusion on absence-of-evidence
     is the exact wrong-denominator failure this mechanism exists to prevent. Excluding (`businessLayer:
     false`) and the `util`/`report` intent categories are BOTH denominator-removing decisions — both
     require positive evidence, never "no data, so out."
   - **intentCategory** exposes the COVERAGE SHAPE later (posting captured vs entry/derive/distribute
     missed): `post`=batch post/commit; `entry`=create/insert/init that produces records;
     `validate`=validation; `derive`=calculation/defaulting (tax, rate, deduction, eligibility);
     `distribute`=cross-module fan-out (e.g. `*PostGL/JC/EM/IN`); `util`/`report`=non-business.
     Classify per **Step 0b** below — NOT from name alone.
   - **Stamp `castIntent` (snapshot 1) at ingestion:** record what CAST itself reports for the component,
     or `unknown` if CAST has no classification / no data-access accessors. Also set `intentCategory` to
     this initial value. This snapshot is the baseline — the count of `castIntent='unknown'` among
     businessLayer components is the empirical size of "CAST fails classification" (a P1 telemetry number).
3. Do this as a `graph_bulk_import` of SourceComponent nodes (one call, whole inventory) — it is cheap
   and idempotent (MERGE on castId).

**STEP 0b — Classify `intentCategory` from BEHAVIOR + NAME combined (MANDATORY when CAST data-access is available):**

`intentCategory` is the input to the coverage-SHAPE signal (the P1 exit gate's ability to tell an
entry/derive/distribute miss from a getter) — and that shape feeds P2 boundary decisions and P3 flow
coverage, not just P4. So it must be *real*, not a name guess. Classify from **two signals combined**:

- **Observed data-access (authoritative):** what tables each component INSERTs/UPDATEs/DELETEs/SELECTs
  (from CAST data-access). This is what the component actually DOES — it catches both the lying name (a
  `...Val`-named proc that INSERTs into three tables is `entry`/`distribute`) AND the object CAST never
  classified (which would otherwise default to `unknown`).
- **Naming patterns (disambiguating prior):** the legacy naming convention (`*Post`, `*Val`, `*Init`,
  `*Calc`, `*PostGL/JC/EM/IN`, ...) refines cases where the write-set alone is ambiguous (two procs both
  write the same table but one validates and one posts — the name separates them).

**Rule: behavior WINS on genuine conflict.** If the name says one thing and the data-access demonstrably
shows another, the data-access is the truth.

**Rule: ABSENCE of signal → `unknown`, NEVER a terminal/excluding category.** A component with no
data-access record (and no strongly-informative name) MUST stay `unknown` — it must NOT be defaulted to
`util` or `report`. This is the load-bearing guardrail. `util`/`report` are EXCLUDING categories
(they drop the component out of the businessLayer coverage denominator), so assigning one asserts "this is
confirmed non-business." A missing data-access record is NOT evidence of non-business — it usually means
CAST couldn't resolve the component's access (app-tier classes that reach data via ADO.NET not direct
table links; procs that write via triggers, dynamic SQL, or through views/functions rather than base
tables; procs that only CALL other procs). Defaulting those to `util` silently removes real posting/
distribution/entry engines from the denominator — the exact wrong-denominator failure this whole
mechanism exists to prevent, one level down.
- **`util`/`report` require POSITIVE evidence** — a known utility/format/helper name pattern, a confirmed
  getter, a reporting object. Never "no data, so util."
- **No positive evidence → `unknown`.** It stays in the businessLayer denominator, shows as an uncovered/
  uncertain component in the coverage shape (honest), and is RESOLVED later: P4 deep extraction reads the
  component's source and stamps `p4Intent` — a source read classifies the app-tier and trigger/dynamic-SQL
  procs more accurately than any CAST call-graph inference could. So the `unknown` residual is a known,
  quantified, in-denominator set that P4 progressively resolves — NOT a permanent hole, and NOT something
  to launder into `util` to make the shape look complete.
- **Do NOT spend a long per-component call-graph fetch just to classify the no-signal residual.** P4 reads
  that source anyway and supersedes any call-graph-inferred intent with a stronger source-read intent.
  Paying for a weaker signal that P4 will overwrite is effort in the wrong place.

**Stamp `firstPassIntent` (snapshot 2) as the result of this pass**, and update `intentCategory` to match.
Do NOT overwrite `castIntent` — leave snapshot 1 intact. The flip is then readable as the delta
`castIntent → firstPassIntent` (SAAM's behavior+name correcting CAST) without any mid-pass instrumentation
— it's just two stamped snapshots compared at telemetry time. Keep the two facts distinct in telemetry:
`castIntent='unknown'` (CAST was silent) is a DIFFERENT signal from `castIntent ≠ firstPassIntent` (CAST
had an opinion, SAAM overrode it) — the first implies a source-read gap, the second implies CAST's
convention is untrustworthy for this stack.

**The mechanism is engagement-specific — the framework prescribes the PRINCIPLE, not a script.** How you
fetch data-access efficiently depends on your CAST version, endpoint behavior (some serialize requests),
and system scale — choose an approach that fits (e.g. fetch accessors by-table and invert to per-component
sets if per-component calls are slow; checkpoint and resume for large surfaces). Do NOT hardcode one
project's method. On a large or poorly-classified CAST surface this sweep can be lengthy; it is a
one-time Step 0 cost per CAST snapshot and belongs HERE, before extraction — its output shapes P2/P3.

**Why this is at Step 0 and not P4:** intent classification is *logically* rule-level detail, but it is
*shape* information about the whole legacy surface — and shape is an INPUT to P2 boundaries (distribution
hubs define boundaries), P3 flow coverage, and the P1 zero-loss guarantee (unknown intent disables the
shape signal). Doing it late means designing on blind shape. Cost early beats rework later.

**🔴 PROMPT HUMAN (fallback — surface the consequence BEFORE skipping, at the point of decision):** If
behavioral classification is unavailable or the operator wants to skip it (name-only), state the
consequence explicitly and get an eyes-open decision:
> "Behavioral intent classification (Step 0b) is the recommended default. Skipping it (name-only)
> leaves every component CAST didn't classify — potentially a large fraction — at intentCategory=`unknown`.
> Consequence: the coverage-SHAPE completeness signal is effectively OFF (can't distinguish
> entry/derive/distribute misses from getters at the P1 exit gate), P2 service-boundary decisions are made
> without distribution/entry-hub visibility, and the zero-unaccounted-loss guarantee degrades to a headline
> count with no shape. Proceed name-only only if you accept these consequences. Recommended: run the
> behavioral sweep now."

Name-only is a permitted fallback — but never a silent one.

**Direct Source mode:** there is no CAST inventory to enumerate, so full-inventory ingestion is not
possible — the mode-independent nets (Phase 3 top-down flow coverage) carry the completeness burden
instead. Note `partial_inventory: true` in the exit telemetry.

**After completing each segment (extraction):**
1. For each SourceComponent you actually extract a rule from: it already exists as a node (from Step 0);
   flip its `extracted` flag — `graph_update_node(nodeType="SourceComponent", id=<castId>, properties={extracted: true})`. (In Direct Source mode where Step 0 didn't run, create the node here as before.)
2. For each BR-ID extracted: `graph_add_node(nodeType="BusinessRule", id=<brId>, properties={statement, intent, confidence, sourceRef, ...}, confidence=<extraction confidence>)`
3. For each extraction: `graph_add_edge(edgeType="EXTRACTED_FROM", sourceId=<brId>, sourceType="BusinessRule", targetId=<componentId>, targetType="SourceComponent")` (the reconciliation queries derive coverage from the presence of this edge, so the `extracted` flag is a convenience — the edge is authoritative)
4. If CAST provides call relationships: `graph_add_edge(edgeType="SOURCE_CALLS", ...)`

**Why incremental:** If context is compacted mid-phase or the session breaks, the graph retains what was already extracted. The agent can resume by querying the graph for what's already there.

**Step-level timing (P1 is a step-instrumented phase — see `saam-telemetry.md`):** emit a StepEvent
(`PhaseEvent` with a `step` label) at the START and END of each of these boundaries — they are checkpoints
you already hit, so this is timestamping, not new work: Step 0 inventory ingest, the Step 0b
reclassification sweep (bracket it — it is a known long cycle, so it counts as WORK not a mystery gap),
and each segment. If a plan-deviation is driven by an unprompted human redirect, stamp that event's
`origin: "unsolicited-intervention"` + `interventionSummary` + `interventionLedTo` (the intervention is a
property of the deviation event, not a separate log). Missed stamps surface later as unattributed
wall-clock gaps — never silent.

## Task Tracking Activation

**PRECONDITION: The agent MUST NOT begin any extraction work until `tracking/phase1-bottom-up.md` exists.** If it doesn't exist, create it NOW with all segments listed as PENDING.

**PhaseEvent (telemetry timestamp):** Immediately after creating the tracking file, write: `graph_add_node(nodeType="PhaseEvent", id="P1-started", properties={phase: "P1", event: "started", timestamp: <current ISO timestamp>})`.

After each segment is analyzed, the agent MUST update this tracking file immediately (mark segment DONE) BEFORE moving to the next segment. If Jira is configured, create an Epic with Tasks per segment. See `saam-task-tracking.md` for format.

**Verification:** If `tracking/phase1-bottom-up.md` does not show a segment as DONE, that segment's extraction is not considered complete — even if the assessment file exists.

## Subagent Delegation (Per-Segment Extraction)

When delegating segment extraction to a subagent for context optimization:

**contextFiles to include:**
- `.github/skills/saam-phase1-bottom-up/SKILL.md`
- `.github/skills/saam-source-reading-<stack>.md` (the project's legacy stack guide)

**Delegation prompt template:**
```
Extract business rules from segment <segment-name> of the legacy system.

READ THESE FILES FIRST (included in your context):
- saam-phase1-bottom-up.md (extraction protocol)
- saam-source-reading-<stack>.md (stack-specific patterns)

SOURCE FILES: initial-source/<system>/<segment-files>

PRODUCE:
- assessment/<segment>-extraction-summary.md (per the format in Phase 1 steering)
- Each BR-ID must have: Source Reference (file:function:lines), Discovery Method,
  Confidence level, Logic (pseudocode), Data dependencies, Side Effects
- Count 8-dimension source vector per component (srcControlFlow, srcDataFlow,
  srcConstants, srcStateTransitions, srcOutcomes, srcDataWrites, srcIntegrations, srcErrorPaths)
- Update graph with SourceComponent + BusinessRule nodes + EXTRACTED_FROM edges

NEVER produce generic summaries. ALWAYS include exact file:function:line references.
```

**Parent verification after subagent returns:**
- [ ] `assessment/<segment>-extraction-summary.md` exists
- [ ] Every BR-ID has a Source Reference with exact file:function:lines
- [ ] Source vectors are non-null for components in this segment
- [ ] Graph nodes created (SourceComponent + BusinessRule + EXTRACTED_FROM)

## Source File Resolution (All Modes)

When a call graph, dependency reference, or configuration file mentions a source component that isn't found at the expected path, the agent MUST escalate its search before declaring "not found":

1. **Fuzzy file search** — search for the core concept name as a filename fragment
2. **Grep for the class/module/function name** — the logic may live in a differently-named file
3. **Only then** → record as "NOT FOUND" with evidence of what searches were performed

NEVER assume a file doesn't exist based on a single path attempt. Legacy systems have inconsistent naming.

## Mode A: Direct Source Analysis

### 1A.1 Program Classification
Read each program/module and classify:
- **Type**: Interactive, Batch, Service/Library, Utility, Report
- **Complexity**: Simple (<200 LOC), Medium (200-1K), Complex (>1K)
- **Source Semantic Vector**: Count the 8-dimension source vector (see below). This is used for semantic preservation validation in Phase 4.
- **Domain**: Functional area (from naming conventions)
- **Dependencies**: Calls, called-by, files/tables used
- **Connector Proc Flag**: YES / NO (see detection heuristic below)

**Connector Proc Detection (MANDATORY — prevents workflow gap misses):**

Programs classified as Utility or Service/Library that appear to be "init/setup/scaffolding" MUST be checked against these connector signals before deprioritizing them for business rule extraction:

| Signal | Example | Why It Matters |
|--------|---------|----------------|
| Reads from entity A, writes to entity B (different domain tables) | `InitFromWorkFile` reads open invoices, creates payment batch records | This is a workflow bridge, not scaffolding |
| Called between two known state-transition procs | Proc sits between "post invoice" and "process payment" in the call graph | It's the user-journey connector |
| Name contains "Init" + domain entity (not just "Init" + technical resource) | `InitFromWorkFile`, `InsertExistingTrans`, `AddRemove` | Domain-entity init = business step |
| Takes user-selected IDs as input (array/list parameter) | Parameter is a list of transaction IDs or document IDs | User-driven selection step |

**Rule:** If ANY signal is YES → do NOT classify as scaffolding. Instead:
1. Extract as a BR-ID with `Intent: Workflow Connector`
2. Document the "from" and "to" workflow contexts it bridges
3. Flag for inclusion in Stage 1.6 Workflow Compilation

**CAST mode equivalent:** When reviewing CAST "Init/Setup" categorizations, apply the same 4 signals. CAST classifies by code complexity; we need to override based on workflow significance.

**Entity Lifecycle & Invariant Detection (Layer A — lightweight P1 sweep, deepened in P4):**

Two implicit-system signals to flag now so P4 can extract the full model later:

- **Entity lifecycle:** any entity/table with a `status`/`state`/`stage`-style column, or logic that
  reads/asserts a status before acting, HAS a lifecycle. Note it as a lifecycle candidate. (The
  `srcStateTransitions` dimension you already count is the raw signal — a component with
  `srcStateTransitions > 0` almost always touches an entity lifecycle.)
- **Data invariants:** any always-true constraint the source enforces regardless of path — a DB CHECK
  constraint, a balance/reconciliation assertion (sum A == sum B), a computed-column relationship
  (`x = y * z`), or a guard that appears in multiple procs for the same entity. Note it as an invariant
  candidate, and whether the legacy enforces it in the DB (constraint/trigger) or only in code.

P1 does NOT build the state machine or finalize invariants — it FLAGS which entities have a lifecycle
and which constraints look load-bearing. P4 extracts the full closed state machine + tiered invariants.
Record both in the extraction summary (see Output Per Segment).

**CAST / Hybrid mode (CAST LOCATES, source EXTRACTS):** CAST narrows *where to look*; it never replaces
the read for anything carrying business intent. Use CAST to LOCATE candidates — tables whose data-access
patterns show a status/state column read-then-written, components with high `srcStateTransitions`, and
existing DB CHECK constraints/triggers CAST reports (structural, cheap). But the actual model — the legal
states, the transition GUARDS, the invariant EXPRESSION — is business intent that CAST cannot give; it is
extracted from the source in P4 (Hybrid, the default when CAST is available, exactly because CAST-only is
insufficient for intent). P1 records the CAST-located candidates as flags; do NOT treat CAST structure as
the model.

**Extensibility Signal Detection (Layer B — lightweight P1 sweep, deepened in P4):**

Legacy products are usually configurable from ONE code base (many instances, different config/data, same
code). Capturing customizations as one-off rules — instead of the ENGINE that resolves them — freezes one
instance's behavior into the reimplementation. Flag a component as an **extensibility signal** if it:

| Signal | Example | Why flagged |
|--------|---------|-------------|
| Reads a metadata / user-defined-field / attribute table | Logic that loads a UD-field definition then reads its value | UD/metadata mechanism |
| Behavior gated on a configuration / parameter value | `IF config.mode = 'X' THEN ...`, threshold read from a settings table | Config/parameter surface |
| Dynamic column / attribute handling | Iterating a set of instance-defined columns/attributes | Metadata-driven behavior |

**Rule:** flagging is NOT the engine spec — P4 compiles the full engine (mechanism + config surface +
resolution logic) into `spec/shared/extensibility-model.md`. P1 only NOTES which components touch the
mechanism and roughly what varies. We NEVER model different-code-per-instance — only the common-code engine.

**CAST / Hybrid mode (CAST LOCATES, source EXTRACTS):** use CAST data-access patterns to LOCATE
components that read a metadata / user-defined-field / settings / configuration table (names are usually
recognizable, e.g. `*_udf`, `*_meta`, `*_config`, `*_settings`) — that's the structural flag. But the
resolution logic (HOW the metadata/config is consumed to produce instance-specific behavior — the engine
itself) is business intent CAST cannot express; it is extracted from source in P4 (Hybrid default). P1
records which components touch the mechanism; the mechanism is modeled from the source read.

**Placement Candidate Detection (Layer C — lightweight P1 sweep, deepened in P4):**

The default target tier for ALL logic is the application. But some logic, if blindly reimplemented as
app code, rebuilds the legacy's performance cliffs (the monster stored proc that pins the DB server
during a periodic sweep and sits idle otherwise). P1 does NOT decide tier — it only FLAGS the units
whose tier is genuinely in question, so P4b can put them on the architect's table with evidence.

Flag a component (and note which of its BR-IDs) as a **PLACEMENT_REVIEW candidate** if ANY signal holds:

| Signal | Example | Why flagged |
|--------|---------|-------------|
| Set-based operation over many rows in one unit | A single proc that updates every open line in a period | App-tier row-by-row reimplementation risks an N-round-trip cliff |
| High volume / high frequency | Runs over large tables, or on every transaction | Latency/throughput sensitive |
| Was a database stored procedure / function / trigger in the legacy | Logic already lived in the DB tier | Candidate to KEEP in DB — or deliberately move to app with a strategy |
| Report / aggregation over large data | Month/period rollups, dashboards | Set-based aggregation often cheaper in-DB |
| Batch sweep / scheduled job touching bulk data | Nightly close, mass status update | The classic "kills the DB on run, idle otherwise" bottleneck |

**Rule:** flagging is NOT a placement decision. The default remains app-tier. Record the flag + the
signal(s) + a one-line performance concern in the extraction summary (see below). P4 attaches evidence;
P4b decides. Do NOT preserve the legacy tier by reflex — that just rebuilds the bottleneck.

**CAST / Hybrid mode (CAST LOCATES, source EXTRACTS):** placement signals are the MOST CAST-derivable of
the three layers because they are largely structural — `SourceComponent.type = StoredProcedure` (or
Function/Trigger) is the "was-a-DB-proc" signal outright; high cyclomatic complexity + broad data-access
fan-out over large tables signals set-based/high-volume; scheduled/batch transaction entry points signal
batch-sweep. Flag these from CAST metadata. But confirming a candidate is genuinely set-based *business
logic* (not incidental bulk I/O) and attaching the volume/frequency/app-tier-risk evidence P4b needs
requires the source read in P4 (Hybrid default). CAST bounds the candidate set; source confirms and
quantifies it.

**Source Semantic Vector Counting (Direct Source Mode — MANDATORY):**

Since CAST is not available, the agent MUST count 8 dimensions while reading each source file. These produce the source vector stored on the `SourceComponent` graph node.

**WARNING:** Phase 4 has a hard gate (`Source Vector Hard Gate`) that BLOCKS the exit if any SourceComponent has null/zero vectors. If P1 skips vector counting, P4 will be unable to validate semantic preservation and the agent will be forced to re-read source files. Compute vectors now to avoid rework later.

| Dimension | What to Count | Examples |
|-----------|---------------|----------|
| **Control-flow** | ALL branching: IF/ELSE/CASE/SWITCH/LOOP/AND/OR/ternary. Include BOTH business and infrastructure branches. | `if (valid)`, `while (hasNext)`, `catch (ex)` |
| **Data-flow** | Distinct tables/columns READ or WRITTEN (count unique table.column references) | `SELECT orders.status`, `UPDATE accounts.balance` |
| **Constants** | Hardcoded rates, thresholds, magic numbers, named config values | `0.15`, `MAX_RETRY=3`, `GOLD_TIER_DISCOUNT` |
| **State transitions** | Status field assignments, workflow state changes | `status = 'APPROVED'`, `state = POSTED` |
| **Outcomes** | Distinct return values, response types, output paths | `return SUCCESS`, `throw InsufficientFunds`, `response 201` |
| **Data writes** | INSERT/UPDATE/DELETE operations (count distinct write targets) | `INSERT INTO audit_log`, `UPDATE order SET status` |
| **Integrations** | External HTTP calls, queue publishes, service invocations | `httpClient.post('/payments')`, `queue.send(event)` |
| **Error paths** | Distinct error/exception handling branches (business + infrastructure) | `catch SQLException`, `if (amount < 0) reject` |

**Important:** Count ALL occurrences including infrastructure (retries, reconnects, audit writes). Phase 4's semantic preservation check will distinguish business from infrastructure — Phase 1 just counts raw numbers.

Example: A function with 8 IF/ELSE blocks (3 business + 5 retries), reading 4 tables, 2 magic numbers, 1 status change, 3 return paths, 2 writes, 1 external call, 4 error handlers:
```
Source vector: [control=8, data=4, constants=2, states=1, outcomes=3, writes=2, integrations=1, errors=4]
```

Record on the graph node:
```
graph_add_node(nodeType="SourceComponent", ..., properties={
  srcControlFlow: 8, srcDataFlow: 4, srcConstants: 2, srcStateTransitions: 1,
  srcOutcomes: 3, srcDataWrites: 2, srcIntegrations: 1, srcErrorPaths: 4
})
```

### 1A.2 Call Graph Construction
For each entry point (transaction, job, API endpoint):
- Trace the full call chain
- Map file/table access (CRUD per table)
- Identify decision points (business rules)
- Note integration points (queues, external calls, APIs)

### 1A.3 Business Rule Extraction
For each rule found in source:
```
### BR-<DOMAIN>-<NNN>-<N>: <Short Name>
Source: <file>:<function/line>
Source Reference: <exact file path, function name, and line number(s) in the legacy codebase>
Discovery Method: Direct Source Read | CAST Imaging (transaction path / call graph / data access query)
CAST Reference: <if CAST was used: CAST object ID, transaction path name, or query that identified this rule>
Type: Validation | Calculation | State Transition | Authorization | Routing
Confidence: High | Medium | Low
Logic: <pseudocode or formula>
Data: <fields/tables read>
Side Effects: <what gets written/published>
```

**MANDATORY**: Every business rule MUST include:
1. **Source Reference** — exact file path + function/method + line number(s) in the legacy source so a human can locate and validate it
2. **Discovery Method** — how this rule was identified (direct source read or CAST query)
3. **CAST Reference** (if applicable) — the specific CAST Imaging query, transaction path, or object that led to discovering this rule

These references enable human validation of extracted business rules against the actual source system.

**🔴 PROMPT HUMAN** (when Confidence = Low): "In [location], logic does [X]. Is this intentional behavior or a workaround?"

## Mode B: CAST Imaging Analysis

### 1B.1 Application Structure Query
Using CAST Imaging MCP, retrieve:
- Application components and their types
- Layer architecture (UI, business, data)
- Technology distribution
- Module boundaries
- **Cyclomatic complexity per component** (provides `srcControlFlow` dimension)

**Source Semantic Vector (CAST Mode):**

CAST provides cyclomatic complexity as a pre-computed metric — this maps to the `srcControlFlow` dimension. For the remaining 7 dimensions, the agent counts them during Phase 4 deep extraction when the source files are actually read.

When querying CAST for components:
- Store `srcControlFlow` from CAST cyclomatic complexity immediately
- The other 7 dimensions (data-flow, constants, states, outcomes, writes, integrations, errors) are populated LATER during Phase 4 when the agent reads each source file

If CAST provides additional metrics (data access patterns, object counts), use them to pre-populate relevant dimensions where possible.

### 1B.2 Dependency Analysis
Query CAST for:
- Call graphs between components
- Data access patterns (which components access which tables)
- Transaction entry points
- Dead code / unreachable components
- **Implicit-system flags (Layers A/B/C)** — from the above CAST data, LOCATE the same P1 candidates the
  direct-source path flags (see the "CAST / Hybrid mode" notes under 1A.1): entity-lifecycle candidates
  (status-column read/write patterns), data-invariant candidates (DB CHECK/trigger metadata),
  extensibility signals (reads of metadata/UD/config tables), and placement candidates
  (`type=StoredProcedure/Function/Trigger`, high-complexity + large-table fan-out, batch entry points).
  These are STRUCTURAL flags — CAST locates them; the actual models (state guards, invariant expressions,
  resolution logic, set-based-vs-incidental) are business intent extracted from source in P4. In Hybrid
  (the default when CAST is available), CAST targets the read and source delivers the meaning — CAST-only
  is not sufficient for intent. Record the located candidates in the extraction summary's Layer A/B/C tables.

### 1B.3 Segment-by-Segment Extraction
Process ONE domain segment at a time to avoid context overload:
1. Query CAST for all components in segment X
2. Get call graph for segment X
3. Get data dependencies for segment X
4. If business rules need extraction → fall back to direct source for that segment
5. Document findings for segment X
6. Move to segment X+1

### 1B.4 Business Rule Extraction (Hybrid)
When CAST identifies complex business logic components:
- Pull specific source files into context (targeted, not full codebase)
- Extract rules only from those files
- Release context, move to next segment

## Mode C: Hybrid (Recommended for Large Systems)

1. Use CAST for: structure, dependencies, call graphs, dead code identification, **cyclomatic complexity (srcControlFlow)**
2. Use Direct Source for: business rule extraction from critical components
3. Segment approach: CAST identifies segments → direct source extracts rules per segment

**Source Semantic Vector (Hybrid Mode):**

- **srcControlFlow:** Use CAST's cyclomatic complexity metric (retrieved in step 1).
- **All 8 dimensions:** Count the full vector when reading source files during direct extraction (step 2). This supplements CAST's single metric with the remaining 7 dimensions.
- **If only CAST data available (no source read yet):** Only srcControlFlow is populated. The other 7 dimensions get populated during Phase 4 deep extraction.
- **Prefer agent-counted full vector** over CAST-only partial vector when source has been read.

## Output Per Segment

Create `assessment/<domain>-extraction-summary.md`:
```markdown
# <Domain> - Extraction Summary

## Segment Profile
- Programs analyzed: X
- Tables accessed: X
- Business rules extracted: X
- Confidence: High X% | Medium X% | Low X%

## Call Graph
<entry point → call chain diagram>

## Business Rules
### BR-<DOM>-001: <Group> (N rules)
1. <rule>
2. <rule>
...

## Data Access Patterns
| Table | Create | Read | Update | Delete | Programs |
|-------|--------|------|--------|--------|----------|

## Integration Points
| Type | Direction | Target | Protocol | Programs |
|------|-----------|--------|----------|----------|

## Entity Lifecycles & Invariants (Layer A — flags only, deepened in P4)
Flag entities with a lifecycle and constraints that look load-bearing. P4 builds the closed state
machine + tiered invariants. Empty only if the segment genuinely has no stateful entities.
| Entity | Has Lifecycle? | Observed States (rough) | Candidate Invariants (one line each) | Legacy enforces in |
|--------|----------------|-------------------------|--------------------------------------|--------------------|
| <entity/table> | yes/no | <e.g., Draft/Posted/Voided> | <e.g., posted batch must balance> | DB constraint/trigger / code only |

## Extensibility Signals (Layer B — flags only, deepened in P4)
Flag components that touch the configurability engine (UD/metadata, config/parameter-gated behavior).
P4 compiles the full engine into spec/shared/extensibility-model.md. Empty if the product is not configurable.
| Component | Mechanism (udf/metadata/config/parameter) | What Varies (one line) |
|-----------|-------------------------------------------|------------------------|
| <program/module> | <mechanism> | <what an instance can customize here> |

## Placement Candidates (Layer C — flags only, default app-tier)
Only list components/BR-IDs that tripped a Placement Candidate signal. Empty is normal and expected —
most logic stays app-tier. This is NOT a tier decision; P4b decides with evidence.
| BR-ID / Component | Signal(s) | Legacy Tier | Performance Concern (one line) |
|-------------------|-----------|-------------|--------------------------------|
| <BR-ID or program> | set-based / high-volume / high-frequency / was-db-proc / report-aggregation / batch-sweep | app / db-proc / db-function / db-trigger / db-view | <why app-tier reimplementation is risky> |

## Items Requiring Human Clarification
- [ ] <question about ambiguous logic>
- [ ] <question about undocumented behavior>
```

## Deliverables
- [ ] Every program/module classified
- [ ] Call graphs for all entry points
- [ ] Business rules extracted with BR-IDs and confidence levels
- [ ] Data access patterns per table
- [ ] Integration points cataloged
- [ ] Entity lifecycles & candidate invariants flagged (Layer A — or explicitly "none stateful")
- [ ] Extensibility signals flagged (Layer B — or explicitly "not configurable")
- [ ] Placement candidates flagged (Layer C — or explicitly "none")
- [ ] Frontend/UI inventory (if UI exists in the legacy system)
- [ ] Ambiguous items flagged for human review

### Graph Verification Gate (MANDATORY before proceeding to P2/P3)

After all segments are complete, verify that the graph contains ALL extracted rules:

1. Count BR-IDs in `assessment/*-extraction-summary.md` files (grep for `BR-` pattern)
2. Query graph: `MATCH (br:BusinessRule) RETURN count(br)`
3. **If markdown count > graph count:** Rules were extracted to markdown but NOT loaded to graph. The agent MUST batch-import the missing rules NOW before P1 is considered complete.

**Batch import protocol (if incremental loading was missed):**
```
For each assessment/<segment>-extraction-summary.md:
  Parse BR-IDs from the markdown
  For each BR-ID not already in graph:
    graph_add_node(nodeType="BusinessRule", id=<brId>, properties={...})
    graph_add_edge(edgeType="EXTRACTED_FROM", ...)
```

**P1 is NOT complete until:** `graph BR-ID count == markdown BR-ID count`. This prevents the downstream divergence where P3 assigns from graph (partial) while specs reference markdown (complete).

### Frontend/UI Discovery (MANDATORY if UI exists)

During Phase 1 analysis, the agent MUST identify and document any frontend/UI components in the legacy system. This information feeds Phase 4 Stage 2 (frontend spec generation).

Document in the extraction summary for the relevant segment:

```markdown
## Frontend/UI Components

### UI Technology
- Framework: <WebForms / WinForms / WPF / React / Angular / green-screen / etc.>
- Language: <C# code-behind / JavaScript / TypeScript / RPG DDS / etc.>
- Rendering: <server-side / client-side SPA / desktop / terminal>

### Screen Inventory
| Screen/Form | Purpose | Source File | Key Interactions |
|-------------|---------|-------------|-----------------|
| <name> | <what it does> | <file path> | <buttons, links, workflows> |

### Reusable Assets Discovered
| Asset Type | Location | Count | Notes |
|------------|----------|-------|-------|
| Icons/images | <path> | <N> | <format: PNG/SVG/etc.> |
| CSS/styles | <path> | <N> | <framework: Bootstrap/custom/etc.> |
| Form layouts | <path> | <N> | <reusable components/templates> |
| Fonts | <path> | <N> | <custom or standard> |

### Navigation Structure
- <How users move between screens — menus, breadcrumbs, tabs, etc.>
```

If the legacy system has NO UI (pure batch processing, API-only, library code), explicitly note: "No frontend/UI components found in this system."

## Telemetry Production (MANDATORY)

After the exit gate is approved, the agent MUST produce `.saam/telemetry/phase1-bottom-up.yaml` by querying the engagement graph.

**Data to compute from graph:**

1. **Timing** — infer from task tracker (`tracking/phase1-bottom-up.md`): first task `in_progress` timestamp → last task `completed` timestamp
2. **Source vectors** — query all `SourceComponent` nodes, aggregate their `srcControlFlow`, `srcDataFlow`, `srcConstants`, `srcStateTransitions`, `srcOutcomes`, `srcDataWrites`, `srcIntegrations`, `srcErrorPaths` properties
3. **Distribution stats** — compute min/max/median/p90/mean per dimension across all components
4. **Complexity distribution** — count components by total vector sum buckets (simple <10, medium 10-30, complex 30-60, very complex >60)

**Schema:** See `saam-telemetry.md` → `phase1-bottom-up.yaml` for the full YAML structure.

**Rules:**
- Aggregate and distribution stats only — never export individual component names or file paths
- If source vectors are incomplete (CAST mode — only srcControlFlow populated), record what's available and note `partial_vectors: true`
- If graph is unavailable, compute from `assessment/` extraction summaries as fallback

## Table Write-Coverage Reconciliation (CAST / Hybrid — MANDATORY at Phase 1 exit)

**The completeness principle: check in the REVERSE direction.** Every existing coverage check starts
from what was EXTRACTED (does this component/rule map to a service, is it tested). That direction is
structurally blind to a writer that was never extracted at all — there is no node to start from. The
classic miss: a prominent downstream consumer/validator/poster of a table gets extracted (it is on a
cross-service call path), while the quiet UPSTREAM producer that actually creates the rows — an
`Init*`/insert procedure — is never pulled into a BR. The batch that validates/posts the rows is
meaningless without the enrollment/init that creates them, yet only the batch gets a rule. No forward
check can see this, because the missing writer isn't in the graph.

This step asks the exhaustive reverse question: **for every business table that is WRITTEN in the
legacy, did we extract a BR for at least one of its writers?**

**Protocol (agent-run at Phase 1 exit; the reverse query is the same CAST/graph data already captured):**

1. **Enumerate writers per business table.** From CAST data-access (component → table, operation =
   INSERT/UPDATE/DELETE) — or, in the graph, the `srcDataWrites` already recorded per `SourceComponent`
   — list every business table that is written, and every component that writes it. (Exclude obvious
   infra/audit/log tables.)
2. **Check extraction coverage per table.** For each written business table, is there at least one
   writer component that maps to an extracted BR (`EXTRACTED_FROM`)? A table written ONLY by
   never-extracted components is an **extraction gap** — its write path has no rule.
3. **Producer/consumer pairing (the sharpened heuristic).** For every table that HAS an extracted
   consumer/validator/poster (a rule that reads/validates/posts it), confirm its PRODUCER/writer was
   also extracted. A validated-but-never-produced table is the exact signature of this miss.
4. **Write the human-confirmed GAP register** `assessment/write-coverage-reconciliation.md`: one row per
   written business table — writers found, whether any writer is extracted, and the agent's proposed
   classification (`EXTRACT` = real write path, re-extract its writer into a BR / `INFRA` = audit/log/
   technical, out of scope / `DEAD` = CAST-confirmed unreachable). The agent PROPOSES; the human confirms.
5. **WAIT for the human signal.** This is a human-confirmed register, not a hard auto-block — a CAST
   heuristic can false-positive on infra tables, so the architect confirms each `EXTRACT` before any
   re-extraction. Any table still classified `EXTRACT` and not yet re-extracted is an open GAP that
   blocks the Phase 1 exit gate.

**Why at Phase 1 exit (not after Phase 4):** the existing Query 1 (extraction coverage) runs after
Phase 4 and is non-blocking — by then specs are already generated on top of the hole. Catching the
missing writer HERE, before specs, means the gap is re-extracted into a BR before anything is built on
the assumption it doesn't exist.

**Direct Source mode:** the CAST write-enumeration isn't available, so this exact sweep can't run — the
Phase 3 convergence "top-down flow without a backing BR" check (see `saam-phase3-convergence.md`) is the
mode-independent net for the same class. In Hybrid/CAST, run BOTH: they catch it from two directions.

## Exit Gate

**PRECONDITION: The agent MUST produce `.saam/telemetry/phase1-bottom-up.yaml` BEFORE presenting the exit gate.** If the file does not exist after this step, the agent must create it now.

**PhaseEvent (completed):** Write: `graph_add_node(nodeType="PhaseEvent", id="P1-completed", properties={phase: "P1", event: "completed", timestamp: <current ISO timestamp>})`.

**MANDATORY — the agent MUST PRINT the Coverage Summary in the exit-gate message (CAST/Hybrid).**
Thinness is only visible if it is PUSHED, not pulled. The agent MUST include this block verbatim in the
exit-gate prompt to the human — never omit it, never summarize it as "coverage looks good":

```
=== PHASE 1 COVERAGE SUMMARY (read before approving) ===
Business components (full CAST inventory, businessLayer=true): <N>
Business rules extracted:                                      <M>
Denominator sanity: <N> should be >> <M>. If <N> ≈ <M>, Step 0 (full-inventory
  ingestion) did NOT run — the numbers below are measuring the extracted subset
  against itself and are INVALID. Fix before approving.
Accountability (extracted or explicitly excluded / total business):  <X>%
Coverage shape by intent (from graph_unaccounted_loss):
    post:       <c>/<t>  (<%>)   cx <%>
    entry:      <c>/<t>  (<%>)   cx <%>
    derive:     <c>/<t>  (<%>)   cx <%>
    distribute: <c>/<t>  (<%>)   cx <%>
    validate:   <c>/<t>  (<%>)   cx <%>
Read the SHAPE, not just the headline: high `post` with near-zero
  entry/derive/distribute = posting-bias miss (spine captured, body missed).
Un-extracted business components: <count>  (EXPECTED to be high at P1 — surface sweep, not deep
  extraction. Full extract-or-exclude guarantee is enforced after P4/P5, not here. What blocks P1:
  dishonest denominator, util-inflated shape, a systematically-skipped area, or a confirmed
  write-coverage gap — NOT a low coverage %.)
```

Populate every `<...>` from `graph_extraction_coverage()` + `graph_unaccounted_loss()` — do not
hand-wave the numbers. If the denominator sanity check fails (`N ≈ M`), STOP and re-run Step 0 before
presenting the gate.

All segments analyzed. Human reviews flagged items **and the Coverage Summary above**. Proceed to Phase 3 convergence.

**Next steps after human approval:**
- Activate `saam-phase3-convergence.md` (once Phase 2 is also complete)
- Phase 3 requires BOTH Phase 1 and Phase 2 outputs — do not start convergence until both tracks finish
- Update the root `README.md` — add Phase 1 completion summary: segments analyzed, total rules extracted, confidence distribution, integration points identified
- **Telemetry:** Produce `.saam/telemetry/phase1-bottom-up.yaml` per the Telemetry Production section above
- **Graph update (always):** Verify the graph has been populated incrementally during extraction. Run `graph_run_inferences(rules=["lifecycle_states", "effective_confidence"])` to calculate scores. If any segments were extracted without graph updates (e.g., due to Neo4j being temporarily down), run `graph_bulk_import` for the missing data now.
- **If CAST is configured (additional):**
  - **Verify the full inventory was ingested (Step 0):** the reconciliation denominator MUST be the full
    CAST business inventory, not the ingested subset. Sanity check: the count of `SourceComponent` nodes
    with `businessLayer=true` should be on the order of the CAST business-layer component count — NOT
    roughly equal to the number of extracted BR-IDs. If the two are close, Step 0 was skipped and every
    coverage number below is meaningless (the historical wrong-denominator bug). Ingest the full inventory
    before proceeding.
  - Run graph validation **Query 1 (Extraction Coverage)** and **Query 4 (Unaccounted Loss)** per
    `saam-graph-validation.md` — now measured against the FULL business inventory. Read the coverage
    SHAPE (per-intent breakdown): a healthy full run is NOT posting-heavy with entry/derive/distribute
    near-zero. A high `post` coverage with near-zero `entry`/`derive`/`distribute` is the classic
    posting-bias miss (posting spine captured, the body of entry/calculation/fan-out logic missed) — treat
    it as a coverage gap, not a pass.
  - Run the **Table Write-Coverage Reconciliation** (section above) — the reverse, exhaustive check that
    every written business table has an extracted writer. Complements the component-inventory check from
    the table side.
  - **What blocks P1 vs what doesn't (do NOT demand full extraction here — P1 is a surface sweep; P4 does
    the deep extraction that produces the majority of rules):**
    - **Blocks:** the denominator being dishonest (≈ BR count → Step 0 didn't run), a `util`-inflated
      shape (missing signal wrongly excluded — see Step 0b), a systematically-skipped area (zero
      components touched in a major domain), and confirmed-`EXTRACT` write-coverage gaps (a specific
      business table whose writer was never extracted — a targeted miss, not "extract everything").
    - **Does NOT block:** a low overall coverage % / large `unknown` residual. That is EXPECTED at P1 —
      the full unaccounted-loss guarantee (every business component extracted-or-excluded) is enforced
      after **P4/P5**, not here. P1 disposition of the surface sweep = confirm honesty + shape + resolve
      the targeted findings above; it is not a demand to extract the whole business layer now.
