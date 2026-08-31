---
name: saam-phase4-spec-generation
description: "Microservice specification generation guidelines, business rule documentation, and interface definitions."
copyright: "Copyright 2024-2026 SoftServe Inc. All rights reserved."
authors: "Max Kozinenko, Roman Kalita (SoftServe)"
---

# SAAM Phase 4: Specification Generation

## Objective

Produce complete microservice specifications with all business rules, data models, APIs, and events — ready for AI-DLC implementation.

## Required Steering Files (Read Before Proceeding)

The agent MUST read the following steering files before executing Phase 4:

1. **`saam-human-guidance-protocol.md`** — Prompt categories, decision register format, agent rules
2. **`saam-task-tracking.md`** — Tracking file format and Jira dual-write protocol
3. **`saam-spec-template.md`** — Microservice specification structure (defines the output format for each service spec)
4. **`saam-api-contract.md`** — OpenAPI contract generation protocol (MUST be generated during Phase 4 per service)
5. **`saam-source-reading-<stack>.md`** — The source reading guide for the project's legacy stack (same one used in Phase 1). Phase 4 reads actual source code — the stack-specific guide provides extraction patterns for the technology.
6. **`saam-cast-imaging-integration.md`** — **(MANDATORY if CAST or Hybrid mode)** Full CAST-guided extraction workflow: service brief assembly, source file targeting, complexity classification, coverage validation. Defines the orchestrator's data-gathering protocol that REPLACES guesswork about which files to read. Skipping it = blind extraction on a large codebase.
7. **`saam-frontend-spec-template.md`** — (Only if the legacy system has a UI) Frontend specification template for generating frontend specs during Phase 4

## Task Tracking Activation

**PRECONDITION: The agent MUST NOT begin spec extraction for any service until `tracking/phase4-spec-generation.md` exists.** If it doesn't exist, create it NOW with all services listed as PENDING (from `modernization/services-composition.md`).

**PhaseEvent (telemetry timestamp):** Immediately after creating the tracking file, write: `graph_add_node(nodeType="PhaseEvent", id="P4-started", properties={phase: "P4", event: "started", timestamp: <current ISO timestamp>})`.

After each service's spec package is complete (all 7 files written), the agent MUST update the tracking file immediately (mark service DONE with timestamp) BEFORE starting the next service. If Jira is configured, create an Epic with Tasks per service. See `saam-task-tracking.md` for format.

**Verification:** The parent agent checks tracking file status as part of the per-service verification checklist. If tracking doesn't show the service as DONE, the service is not considered complete.

**CRITICAL**: Phase 4 is NOT a summary of Phase 1 findings. It is a DEEPER extraction pass that reads actual source code and produces exhaustive, implementation-ready specifications. The agent MUST read source files during this phase.

## Context Management (MANDATORY — Prevents Degraded Extraction)

**Phase 4 is context-intensive.** Each service requires reading multiple source files, holding business logic in context, counting vectors, and producing detailed specs. The orchestrator MUST NOT accumulate this context — it delegates ALL heavy work to specialized subagents.

**Root cause of extraction failures:** When the orchestrator reads source, holds CAST data, produces specs, validates output, updates graph, and tracks progress — all in one context — it degrades after 2-3 services. It takes shortcuts: 1-dimension vectors instead of 8, legacy names leaked into Statements, services marked DONE without all files. The fix: strict separation of concerns via specialized subagents.

### Rule 1: Orchestrator as Pure Sequencer

The orchestrator NEVER:
- Reads source files
- Holds CAST query results in its context
- Produces spec content
- Decides if quality is sufficient (that's the validator's job)
- Updates the graph directly (that's the tracker's job)

The orchestrator ONLY:
- Determines which service is next (from tracking file)
- Calls subagents in sequence (A → B → C → D)
- Passes FAIL results back to Subagent B for re-extraction
- Reports to the human at phase boundaries

### Rule 2: ONE Service at a Time, 4 Subagent Pipeline

```
For each service (in provider-first order from Phase 4 Execution Order):

  ┌─────────────────────────────────────────────────────────┐
  │ Step A: CAST SCOUT (CAST/Hybrid mode only)              │
  │   Queries CAST → produces assessment/<svc>-brief.md     │
  │   (Direct Source mode: skip this step)                  │
  │                                                         │
  │ Step B: EXTRACTOR                                       │
  │   Reads source + brief → produces 6-file spec package   │
  │                                                         │
  │ Step C: VALIDATOR                                       │
  │   Checks spec against ALL compliance gates              │
  │   If FAIL: orchestrator re-calls B with failure list    │
  │   If PASS: orchestrator calls D                         │
  │                                                         │
  │ Step D: TRACKER                                         │
  │   Updates graph + tracking + telemetry + git commit     │
  └─────────────────────────────────────────────────────────┘
  → Next service
```

### Rule 3: Maximum 2 Re-Extraction Cycles

If Subagent C (Validator) fails the output TWICE, the orchestrator:
1. Reports to human with the specific failures
2. Asks: "Fix manually, re-extract with different approach, or accept with known gaps?"
3. Does NOT loop indefinitely (infinite re-extraction wastes credits)

---

## Subagent A: CAST Scout (CAST/Hybrid Mode Only)

**Purpose:** Query CAST Imaging to identify which source files contain business logic for this service. Produces a service brief that tells the Extractor exactly what to read.

**When to skip:** If analysis mode is "Direct Source" (from `inventory/INDEX.md`), skip Step A entirely. The Extractor reads from Phase 1 extraction summaries and directory structure instead.

**contextFiles:**
```
- .github/skills/saam-cast-imaging-integration/SKILL.md
```

**Prompt template:**
```
You are the CAST Scout for Phase 4 extraction of <service-name> (<service-id>).

READ THIS FIRST (included in your context):
- saam-cast-imaging-integration.md — full CAST query workflow

YOUR JOB: Query CAST Imaging to build a service brief for the Extractor agent.
Follow the "Phase 4: CAST-Guided Extraction Workflow" protocol exactly.

STEPS:
1. Query transactions for domain prefix: <domain_prefix>
2. For critical transactions: get full call graph (nodes + links)
3. Get data access patterns for domain tables
4. Get complexity-sorted objects (business logic candidates)
5. Classify components: business logic (MUST read) vs infrastructure (skip)
6. Resolve CAST paths to local workspace paths (use mapping from inventory/INDEX.md)

PRODUCE: assessment/<service-id>-cast-brief.md

The brief MUST include:
- Entry points (transactions with size and complexity)
- Source files to read (business logic — local paths, complexity, reason for inclusion)
- Source files to skip (infrastructure — reason for exclusion)
- Tables owned by this service
- Cross-service dependencies from call graph
- Existing P1 rules that need P4 upgrade (from assessment/*-extraction-summary.md)
- Dead code excluded from scope

NEVER produce spec files. NEVER read source code. Your ONLY job is querying CAST
and producing the brief file.
```

**Output:** `assessment/<service-id>-cast-brief.md` (file on disk)

**For Direct Source mode (no CAST):** The Extractor gets its file list from:
- Phase 1 extraction summaries (`assessment/*-extraction-summary.md`)
- Service composition (`modernization/services-composition.md`)
- Directory structure of `initial-source/`

---

## Subagent B: Extractor

**Purpose:** Read source files and produce the complete 6-file spec package for one service. This is the ONLY subagent that reads legacy source code.

**contextFiles:**
```
- .github/skills/saam-phase4-spec-generation/SKILL.md
- .github/skills/saam-spec-template/SKILL.md
- .github/skills/saam-source-reading-<stack>.md
- .github/skills/saam-api-contract/SKILL.md
- .github/skills/saam-cast-imaging-integration/SKILL.md  (CAST/Hybrid mode — for ad-hoc queries during extraction)
```

**Additional input files (read from disk, NOT in contextFiles):**
- `assessment/<service-id>-cast-brief.md` (from Scout — tells which files to read)
- OR `assessment/*-extraction-summary.md` (Direct Source mode — P1 findings)
- Source files listed in the brief: `initial-source/<paths>`

**CAST access (CAST/Hybrid mode):** The Extractor has access to CAST Imaging MCP tools for ad-hoc queries during extraction. The Scout's brief provides the PRIMARY file list, but when the Extractor encounters unexpected cross-references in source (e.g., a call to another domain's procedure), it can query CAST directly to determine: "Is this my responsibility or another service's? Who else calls this? What tables does it access?" This avoids the Extractor guessing about cross-service boundaries.

**Prompt template:**
```
You are the Extractor for Phase 4 of <service-name> (<service-id>).

READ THESE STEERING FILES FIRST (included in your context):
- saam-phase4-spec-generation.md (extraction protocol, quality rules, semantic elevation)
- saam-spec-template.md (exact BR-ID format with all required fields)
- saam-source-reading-<stack>.md (stack-specific extraction patterns)
- saam-api-contract.md (OpenAPI contract format)
- saam-cast-imaging-integration.md (CAST/Hybrid mode — use for ad-hoc queries)

SERVICE BRIEF: Read assessment/<service-id>-cast-brief.md for the file list.
(If no brief exists: read assessment/*-extraction-summary.md for P1 context)

READ ONLY the source files listed in "Source Files to Read" (business logic).
DO NOT read files marked as "infrastructure" or "skip".

CAST ACCESS (CAST/Hybrid mode): You have CAST Imaging MCP tools available.
The brief gives you the PRIMARY file list, but if you encounter cross-references
to other procedures/modules during source reading, query CAST to determine:
- Who calls this? (mcp_imaging_object_details focus:"inward")
- What does it access? (mcp_imaging_data_graphs)
- Is it my service's responsibility or another's? (check which service owns the table)
Use CAST sparingly — only for cross-reference questions the brief doesn't answer.

PRODUCE EXACTLY these 6 files at spec/microservices/<service-id>/:
- 01-business-rules.md
- 02-domain-model.md
- 03-api-design.md
- 04-api-contract.yaml
- 06-completion-summary.md
- extraction-evidence.md

MANDATORY QUALITY REQUIREMENTS:

1. STATEMENT COMPLIANCE (zero tolerance):
   - Statement = SEMANTIC business meaning (domain expert can understand without legacy DB)
   - NEVER put legacy table/column/variable names in Statement (those go in Logic ONLY)
   - Test: would this Statement make sense if the target system had completely different table names?

2. SEMANTIC PRESERVATION (8 dimensions per rule):
   - EVERY rule MUST have the Semantic Preservation table:
     | Dimension | Source | Spec | Status |
     | Control-flow | N | N | OK/GAP/CRITICAL |
     | Data-flow | N | N | ... |
     | Constants | N | N | ... |
     | State transitions | N | N | ... |
     | Outcomes | N | N | ... |
     | Data writes | N | N | ... |
     | Integrations | N | N | ... |
     | Error paths | N | N | ... |
   - Count HONESTLY from source (not rubber-stamp). Complex procs WILL have gaps — that's OK.
   - If GAP detected: re-read source for that dimension before finalizing.

3. CONCRETE EXAMPLES (every rule):
   - At least one success + one error scenario
   - Use DOMAIN fields (not legacy column names)
   - Real business values (not generic "test123")

4. COMPLETE SPEC PACKAGE:
   - DDL must be executable PostgreSQL (not pseudocode)
   - API contract must be valid OpenAPI 3.1
   - Endpoint count must match what the rules require
   - extraction-evidence.md lists every source file actually read

NEVER skip dimensions. NEVER rubber-stamp Source=Spec. NEVER leak legacy names into Statements.
```

**Re-extraction prompt (when Validator returns FAIL):**
```
Your previous extraction for <service-name> FAILED validation.

SPECIFIC FAILURES:
<paste Validator's failure list here>

Fix ONLY the listed issues. Do NOT rewrite rules that passed validation.
Read the specific source files needed to correct the gaps.
```

---

## Subagent C: Validator

**Purpose:** Check the Extractor's output against ALL compliance gates. This subagent has FRESH context — it has NOT read source code and has NO bias toward the output being "good enough."

**contextFiles:**
```
- .github/skills/saam-phase4-spec-generation/SKILL.md (quality gates + semantic elevation rules)
- .github/skills/saam-spec-template/SKILL.md (expected format)
```

**Input files (read from disk):**
```
- spec/microservices/<service-id>/01-business-rules.md
- spec/microservices/<service-id>/02-domain-model.md
- spec/microservices/<service-id>/03-api-design.md
- spec/microservices/<service-id>/04-api-contract.yaml
- spec/microservices/<service-id>/06-completion-summary.md
- spec/microservices/<service-id>/extraction-evidence.md
```

**Prompt template:**
```
You are the Validator for Phase 4 extraction of <service-name> (<service-id>).

YOUR JOB: Check the spec package for compliance. You have NOT seen the source code.
You are checking ONLY whether the output meets quality standards.

READ THESE STEERING FILES (included in your context):
- saam-phase4-spec-generation.md (quality gates, semantic elevation rules, statement rules)
- saam-spec-template.md (expected format for each field)

THEN READ the 6 spec files at spec/microservices/<service-id>/.

RUN THESE CHECKS (report PASS/FAIL for each):

1. FILE COMPLETENESS:
   □ All 6 files exist (01-business-rules, 02-domain-model, 03-api-design, 04-api-contract, 06-completion-summary, extraction-evidence)
   □ 06-completion-summary counts match actual content (rule count, table count, endpoint count)

2. STATEMENT COMPLIANCE (zero tolerance):
   □ Read EVERY Statement field in 01-business-rules.md
   □ Flag ANY Statement containing: @Variables, legacy table prefixes (b+UpperCase pattern),
     uppercase TABLE.Column references, SQL keywords (SELECT/UPDATE/INSERT/SET/RAISERROR),
     stored procedure names (vsp/bsp prefixes)
   □ PASS = zero violations. ANY violation = FAIL with list of offending BR-IDs.

3. SEMANTIC PRESERVATION TABLES (8 dimensions):
   □ Count preservation tables in 01-business-rules.md (grep "| Dimension | Source | Spec |")
   □ Count total BR-IDs (grep "^### BR-")
   □ Table count MUST equal BR-ID count (every rule has a table)
   □ Each table MUST have ALL 8 dimensions (not just Control-flow)
   □ Check for "suspicious perfection": if >90% of rules show Source=Spec across all 8 dims
     with zero gaps → FLAG as "suspiciously uniform — may be rubber-stamped"

4. CONCRETE EXAMPLES:
   □ Every BR-ID has a "Concrete Example" or "HTTP Example" section
   □ Examples use domain terms (not legacy column names)
   □ At least 1 success + 1 error case per rule

5. DDL QUALITY:
   □ 02-domain-model.md contains executable PostgreSQL DDL (CREATE TABLE statements)
   □ Every table has a primary key
   □ Column names are domain-appropriate (not legacy abbreviations)

6. API CONTRACT:
   □ 04-api-contract.yaml is valid OpenAPI 3.1 structure
   □ Every endpoint in 03-api-design.md has a corresponding path in the contract
   □ Response schemas reference named schemas in components/schemas

7. EXTRACTION EVIDENCE:
   □ extraction-evidence.md lists source files that were actually read
   □ File count is reasonable for the service's complexity

8. GRAPH POPULATION (post-Tracker check):
   □ Run: `uv run --directory graph-mcp python scripts/import_specs.py --service <service-id> --check`
   □ Exit 0 = PASS (graph has all rules from spec). Exit 2 = FAIL (Tracker didn't run or failed)
   □ If FAIL: the Tracker subagent must re-run the import script

9. IMPLICIT-SYSTEM LAYERS (only when the corresponding 02-domain-model.md section is present):
   These sections drive generation; a broken one passes silently today and breaks the build later.
   □ **Entity State Model (Layer A)** — if `### Entity State Model` exists, for EACH entity's machine:
     - Exactly one state marked `(initial)`; at least one terminal state (or an explicit note why not).
     - Every `To` state in the transitions table is a declared state (no transition to an unknown state).
     - Every non-terminal state has at least one outgoing transition (no dead-end mid-lifecycle state).
     - Every state is reachable from the initial state (walk the transitions).
     - Every transition names a Trigger BR-ID that exists in `01-business-rules.md` and a Guard.
     A machine that fails ANY of these = FAIL (non-closed state machine) with the entity + defect.
   □ **Data Invariants (Layer A)** — if `### Data Invariants` exists:
     - Every row has an `INV-` id and a `Tier` value (app|db|both).
     - Integrity invariants (balance/reconciliation/monotonic-status/referential) are `db` or `both`,
       NOT `app` — data integrity cannot depend on the app being the sole writer.
     - Every `computed`-kind invariant states its source expression.
   □ **Database Logic Objects (Layer C)** — if `### Database Logic Objects` exists:
     - Every row has a valid `Kind` (view|function|procedure|trigger) and a `Migration Order`.
     - Every row has backing executable DDL under `### Core Entities` (or a labelled migration block) —
       a row with no DDL is a spec gap (generation would have nothing to emit).
     - Every row names at least one of `Implements` (a real BR-ID) or `Enforces Invariant` (a real INV-id).
     - Non-trigger rows have a `Binding`; `Placement` is `P4b:PLACE-<id>` or `mandatory-db-integrity`.
   □ **Extension Points (Layer B)** — if any BR-ID has an `Extension Point:` annotation:
     - The referenced `EXT-` id is documented in `spec/shared/extensibility-model.md`.
     - The rule's Logic calls the resolver (does not hardcode the configurable value).

PRODUCE a validation report:
```markdown
# Validation Report: <service-id>

## Overall: PASS / FAIL

## Gate Results:
| Gate | Status | Details |
|------|--------|---------|
| File completeness | PASS/FAIL | ... |
| Statement compliance | PASS/FAIL | N violations (BR-IDs: ...) |
| Preservation tables | PASS/FAIL | N/M present, dimensions: ... |
| Concrete examples | PASS/FAIL | N missing |
| DDL quality | PASS/FAIL | ... |
| API contract | PASS/FAIL | ... |
| Extraction evidence | PASS/FAIL | ... |
| Implicit-system layers | PASS/FAIL/N/A | state-machine closure, invariant tiers, db-object DDL, extension points |

## Specific Failures (for re-extraction):
- BR-GL-PST-001: Statement contains "bGLCO.InterfaceLevel" — needs semantic rewrite
- BR-GL-PST-005: Only 1 dimension in preservation table — needs 8
- ENTITY ledger_batch: state "Voided" unreachable (no transition targets it) — non-closed machine
- INV-GL-001: integrity invariant marked tier=app — must be db/both
- DB-OBJECT compute_batch_total: row present but no CREATE FUNCTION DDL in Core Entities
- ...
```

Be STRICT. Your job is to catch problems, not approve output.
Do NOT give benefit of the doubt. If something looks suspicious, FLAG it.
```

---

## Subagent D: Tracker

**Purpose:** Handle all bookkeeping: graph population, tracking file update, telemetry, git commit. This subagent runs ONLY after the Validator says PASS.

**contextFiles:**
```
- .github/skills/saam-task-tracking/SKILL.md
- .github/skills/saam-telemetry/SKILL.md
- .github/skills/saam-graph-context/SKILL.md
```

**Input files (read from disk):**
```
- spec/microservices/<service-id>/01-business-rules.md (for BR-IDs + vectors + Extension Point annotations)
- spec/microservices/<service-id>/02-domain-model.md (for Table nodes AND — if present — Entity State
  Model → EntityState + HAS_STATE + TRANSITIONS_TO, Data Invariants → Invariant + CONSTRAINS,
  Database Logic Objects → DbObject + IMPLEMENTS_IN_DB)
- spec/microservices/<service-id>/04-api-contract.yaml (for Endpoint nodes)
- spec/microservices/<service-id>/extraction-evidence.md (for SourceComponent nodes)
- tracking/phase4-spec-generation.md (to update)
```

**Note:** the deterministic `import_specs.py` (Step 1 below) creates ALL of these nodes/edges — the
implicit-system nodes (EntityState, Invariant, DbObject, ExtensionPoint) as well as BR/Table/Endpoint.
The Tracker does NOT make individual MCP calls for them; the script is the single import path.

**Prompt template:**
```
You are the Tracker for Phase 4 extraction. The Validator has confirmed <service-name>
(<service-id>) PASSES all quality gates. Your job: update all tracking systems.

READ THESE STEERING FILES (in your context):
- saam-task-tracking.md (tracking file format + Phase Transition Protocol)
- saam-telemetry.md (timing + PhaseEvent requirements)
- saam-graph-context.md (graph node/edge types)

EXECUTE THESE STEPS IN ORDER:

1. GRAPH POPULATION (via deterministic import script — NOT individual MCP calls):
   ```bash
   uv run --directory graph-mcp python scripts/import_specs.py --service <service-id>
   ```
   This script parses the spec markdown files and bulk-imports to Neo4j:
   - BusinessRule nodes from 01-business-rules.md (with vectors if preservation tables exist)
   - ASSIGNED_TO edges (BR → Service)
   - Table nodes from 02-domain-model.md + OWNS edges
   - Endpoint nodes from 04-api-contract.yaml + EXPOSES edges
   
   **Why a script (not MCP calls):** Agents skip MCP calls under context pressure.
   The script is deterministic — it runs once, imports everything, can't be partially skipped.
   If it fails, it fails loudly (exit code 1).

   **Verify after script completes:**
   ```bash
   uv run --directory graph-mcp python scripts/import_specs.py --service <service-id> --check
   ```
   Exit 0 = graph matches spec. Exit 2 = mismatch (re-run import).

2. RUN INFERENCES:
   → graph_run_inferences(rules=["lifecycle_states", "effective_confidence"])

3. UPDATE TRACKING FILE:
   - Mark service DONE in tracking/phase4-spec-generation.md with ISO timestamp
   - Update Summary metrics (completed count, last updated)

4. GIT COMMIT:
   - git add spec/microservices/<service-id>/ tracking/phase4-spec-generation.md
   - git commit -m "feat(phase4): <service-name> spec complete — N rules, M tables, K endpoints"
   - Verify commit: git log --oneline -1 (confirm it shows the service commit)

5. REPORT:
   Print summary: "Tracker complete: <service-id> — N BR nodes, M Table nodes, K Endpoint nodes,
   J EXTRACTED_FROM edges. Committed as <hash>."
```

---

## Orchestrator Flow (Minimal Context)

The orchestrator's ENTIRE per-service logic is:

```python
for service in services_in_order:
    # Step A: CAST Scout (if CAST/Hybrid mode)
    if analysis_mode in ["CAST", "Hybrid"]:
        invoke_sub_agent("CAST Scout", prompt=scout_prompt(service))
        # Verify brief file exists on disk
        assert file_exists(f"assessment/{service}-cast-brief.md")
    
    # Step B: Extractor
    invoke_sub_agent("Extractor", prompt=extractor_prompt(service))
    # Don't validate here — that's C's job
    
    # Step C: Validator
    for attempt in range(1, 3):  # max 2 attempts
        report = invoke_sub_agent("Validator", prompt=validator_prompt(service))
        if "PASS" in report:
            break
        elif attempt == 1:
            # Re-extract with failure list
            invoke_sub_agent("Extractor", prompt=re_extract_prompt(service, failures=report))
        else:
            # 2 failures — ask human
            prompt_human(f"Service {service} failed validation twice. Failures: {report}")
            break
    
    # Step D: Tracker (only on PASS)
    if "PASS" in report:
        invoke_sub_agent("Tracker", prompt=tracker_prompt(service))
    
    # Orchestrator: one-line status update
    print(f"✓ {service} complete. Moving to next.")
```

**Why this works:** The orchestrator holds ~500 tokens per service cycle (the prompt templates + service name). It NEVER accumulates source code, CAST data, or spec content in its context. Even after 18 services, its context utilization stays below 10%.

### Rule 4: Source Vector Hard Gate

**The agent MUST NOT delegate the next service until steps a-f are verified.** These steps are the parent's responsibility — not the subagent's. The subagent produces files; the parent handles tracking, graph, and commit.

**COMMIT IS NOT OPTIONAL.** It is the durability guarantee for the extraction work AND the timing data source for telemetry. Without per-service commits, telemetry cannot compute per-service duration and context loss means re-extraction.
```

**Step-level timing (P4 is a step-instrumented phase — see `saam-telemetry.md`):** emit a StepEvent
(`PhaseEvent` with `step: "<service>-extraction"`) at the START and END of each service's deep extraction —
these bound the per-service active-work interval (more reliable than deriving it from commit cadence). Also
stamp `p4Intent` per component read (step 6a). If an unprompted human redirect changes the plan mid-phase,
stamp the resulting deviation event's `origin: "unsolicited-intervention"` + summary + led_to (the
intervention is a property of the deviation, not a separate log — see `saam-telemetry.md`).

**Why subagent delegation:** The subagent gets its own context window. It can read multiple source files without consuming the parent's context. The parent stays lean — it orchestrates and verifies, never loads source code itself.

**Why sequential (not parallel):** Deep extraction may discover NEW rules not in Phase 3 assignments. Sequential ensures earlier services' discoveries are visible to later services, avoiding duplicates or conflicts in shared source files.

**Fallback (no subagent available):** If running without subagent support, the parent agent processes files directly using Rule 2 (file-by-file). Monitor context usage — if approaching limits mid-service, commit progress and inform human.

### Rule 2: File-by-File Extraction with Immediate Write

Do NOT hold multiple large source files in context simultaneously. For each source file:
1. Read the file
2. Extract rules from it (Purpose-First method)
3. Write extracted rules to the spec file immediately
4. Count source vectors for that file's components
5. Update the extraction tracker
6. Release the file from working memory before reading the next

### Rule 3: Extraction Evidence Tracker (Per-Service)

Before beginning extraction for a service, create `spec/microservices/<service>/extraction-evidence.md`:

```markdown
# <service> — Extraction Evidence

## Source Files Processed

| # | File | Lines | Sections Read | Rules Extracted | Vectors Counted |
|---|------|-------|---------------|-----------------|-----------------|
| 1 | <path> | <N> | <one-line summaries> | <count> | ✅ / ❌ |
| 2 | <path> | <N> | <summaries> | <count> | ✅ / ❌ |
| ... | | | | | |

## Extraction Status
- Files total: <N>
- Files processed: <M>
- Rules extracted: <total>
- Source vectors complete: <yes/no>

## Session Log
| Session | Files Processed | Rules Added | Notes |
|---------|-----------------|-------------|-------|
| 1 | files 1-5 | 23 rules | — |
| 2 | files 6-10 | 18 rules | Resumed after context limit |
```

Update this file after each source file is processed. This serves as:
- **Proof of work** (the agent actually read the files)
- **Resumption mechanism** (next session knows which files are pending)
- **Progress tracking** (how far along is this service)

### Rule 4: Source Vector Hard Gate

**The agent MUST NOT present the Phase 4 exit gate if ANY service has source components with null/zero vectors.** The preservation validation requires vectors. If vectors are missing, deep extraction didn't happen.

Check: for each service, verify that all SourceComponent nodes assigned to it have non-null `srcControlFlow` (at minimum). If CAST mode was used and only srcControlFlow exists, that's acceptable (other dimensions populated during this phase). If Direct Source mode and vectors are null → the agent skipped the counting step.

### Source File Resolution (NEVER Declare Missing Without Searching)

When a business rule, call graph, or reference mentions a source component that isn't found at the expected path, the agent MUST escalate its search before declaring "not found":

1. **Fuzzy file search** — search for the core concept name as a filename fragment (e.g., if looking for "SyncBackgroundCheck", search for `*background_check*`)
2. **Grep for the class/module/function name** — the logic may live inside a differently-named file (a concern, helper, job, or inline class)
3. **Only then** → record as "NOT FOUND" in extraction-evidence.md, noting what searches were performed

NEVER assume a file doesn't exist based on a single path attempt. Legacy systems have inconsistent naming (pluralization, suffixes, nesting, framework conventions).

In extraction-evidence.md, "Not Found" entries MUST include:
- What was searched for (concept/class name)
- What search methods were used (file search pattern, grep pattern)
- Whether it might exist under a different name (suspected alternatives)

### What "Shortcutting" Looks Like (NEVER Do This)

❌ Reading Phase 1 `assessment/*.md` and repackaging the BR-IDs found there into Phase 4 templates
❌ Writing BR-ID specs without reading the actual source file referenced in the BR-ID
❌ Producing a `01-business-rules.md` with no new rules beyond what Phase 1 extracted
❌ Skipping the source vector count because "it was done in Phase 1" (Phase 4 may discover new components)
❌ Reporting completion without an `extraction-evidence.md` showing files read

**If you find yourself writing spec content without having called `read_file` on a source code file in THIS session → you are shortcutting. STOP and read the source.**

**TEST SUITE DEFERRAL**: Phase 4 produces specifications only. Test suite generation (`comprehensive-test-suite.sh`) is a SEPARATE step (Phase 4c). Do NOT create test files during Phase 4.

## Graph Population (Incremental — During Phase 4)

The agent MUST update the knowledge graph incrementally as each service's specification completes — NOT wait until the exit gate.

**After completing each service's spec package (01 through 04):**
1. For each table in `02-domain-model.md`: `graph_add_node(nodeType="Table", id=<tableName>, properties={service, columns, primaryKey})`
2. For each endpoint in `04-api-contract.yaml`: `graph_add_node(nodeType="Endpoint", id=<path>, properties={method, service, successStatus})`
3. For each schema field in `04-api-contract.yaml`: `graph_add_node(nodeType="Field", id=<fieldName>, properties={type, schema, endpoint})`
4. Service owns tables: `graph_add_edge(edgeType="OWNS", sourceId=<serviceId>, sourceType="Service", targetId=<tableName>, targetType="Table")`
5. Service exposes endpoints: `graph_add_edge(edgeType="EXPOSES", sourceId=<serviceId>, sourceType="Service", targetId=<path>, targetType="Endpoint")`
6. Column-to-field mapping: `graph_add_edge(edgeType="MAPS_TO", sourceId=<tableName>, sourceType="Table", targetId=<fieldName>, targetType="Field", properties={columnName})`
7. Any new BR-IDs extracted during deep extraction: `graph_add_node(nodeType="BusinessRule", ...)` + `graph_add_edge(edgeType="EXTRACTED_FROM", ...)`

**Why incremental:** Phase 4 may span multiple sessions across many services. If the graph is only updated at the exit gate, context construction tools (`graph_implementation_context`) won't have data for already-completed services.

## Entry Precondition: Verify Prior Phase Artifacts

Before beginning Phase 4 work, the agent MUST verify that prior phases produced their mandatory artifacts. Check for these files:

**From Phase 2 (`modernization/` folder):**
- [ ] `modernization/modernized-architecture.md` — exists and non-empty
- [ ] `modernization/services-composition.md` — exists and non-empty
- [ ] `modernization/*-modernization-roadmap.md` — exists and non-empty
- [ ] `modernization/*-risk-analysis.md` — exists and non-empty
- [ ] `modernization/*-entity-relationship-diagram.md` — exists and non-empty
- [ ] `modernization/*-sequence-diagrams.md` — exists and non-empty

**From Phase 3 (`assessment/` folder):**
- [ ] `assessment/microservice-gap-analysis.md` — exists and non-empty

**If ANY of these are missing:** The agent MUST NOT proceed with Phase 4. Instead:
1. Inform the human which artifacts are missing
2. Offer to generate the missing artifacts now (using Phase 2/3 outputs already gathered)
3. Only proceed to Phase 4 extraction once all prerequisite artifacts exist

**CAST Mode Check (MANDATORY):**
If `inventory/INDEX.md` declares analysis mode as CAST or Hybrid:
- Verify CAST Imaging MCP server is configured and responsive (test query: `mcp_imaging_applications()`)
- Confirm `saam-cast-imaging-integration.md` is loaded (this is a Required Steering File — item 6 above)
- If CAST MCP is unreachable: STOP. Do not proceed with direct-source-only extraction on a >150K LOC codebase. Inform the human.
- The Phase 4 extraction workflow MUST follow the CAST-guided protocol (service brief assembly, component classification, targeted file lists). Falling back to "read everything" is NOT acceptable for large codebases.

This prevents the "empty modernization folder" problem where Phase 4 completes but supporting artifacts were never written.

## Specification Structure (Mandatory)

Every spec MUST contain these sections:
1. Service Overview (ID, port, schema, priority)
2. Purpose (one paragraph)
3. Business Context (legacy programs replaced)
4. Data Model (complete DDL with indexes)
5. Business Rules (numbered BR-IDs with logic)
6. API Endpoints (method, path, description)
7. Events Published (event, trigger, consumers)
8. Events Consumed (event, source, action)
9. Dependencies (upstream, downstream, external)
10. Non-Functional Requirements (SLA targets)
11. Automation Assessment (before/after percentages)

## Deep Extraction Protocol (MANDATORY)

Phase 4 requires reading actual source code for every service. This is the core work of specification generation — not summarization.

### Default Mode: Hybrid (CAST + Direct Source)

Unless the project is explicitly configured for a different mode:
1. Use **CAST Imaging MCP** to identify which components contain business logic (transaction paths, complexity hotspots, call graphs)
2. Use **Direct Source Read** to extract the actual business rules from those components

If CAST is NOT available for the project, use Direct Source Read exclusively.

### Extraction Loop (per sub-domain/service)

For each target microservice, execute this loop:

```
1. IDENTIFY targets:
   - If CAST available: query CAST for components with highest complexity in this domain
   - If Direct only: use Phase 1 inventory to identify files by LOC (largest first)

2. For each source file/module/program with LOC > 100:
   a. READ the ENTIRE source unit as a whole — do NOT process line-by-line
   b. SUMMARIZE its PURPOSE in one sentence (what business operation does this accomplish?)
   c. IDENTIFY KEY DECISION POINTS — what determines success vs. failure? What branches exist?
   d. EXTRACT rules ONLY from decision points — each rule must be a business constraint, calculation formula, or state transition
   e. MERGE related conditionals — sequential parameter checks are ONE rule, not N rules
   f. For EACH identified rule, write a BR-ID entry with semantic Statement + implementation Logic

3. Cross-reference callers:
   - If CAST available: query CAST for "who calls this component"
   - If Direct only: search the codebase for references to this file/function

4. Update domain model (02-domain-model.md):
   - DDL MUST use REAL column/table names from the source system
   - Every table referenced in extracted rules must appear in the model

5. Update API design (03-api-design.md):
   - Every posting/processing operation discovered must have a corresponding endpoint
   - CRUD operations map to standard REST; business operations get custom endpoints

6. Update completion summary with ACCURATE counts

7. Attach placement evidence (Layer C — only for PLACEMENT_REVIEW candidates flagged in P1):
   - For each candidate, gather the evidence P4b needs to decide tier (see subsection below)
   - Do NOT decide tier here — P4 collects evidence, P4b decides
```

### Placement Candidate Evidence (Layer C — deepen P1 flags for the P4b decision)

P1 flagged which units are placement candidates (set-based / high-volume / high-frequency /
was-db-proc / report-aggregation / batch-sweep). During deep extraction, for EACH flagged candidate,
attach the concrete evidence the architect needs at P4b — turning a flag into a decidable question.

For each candidate, record (in the service's `06-completion-summary.md` under a "Placement Candidates"
heading, or inline on the BR-ID as a `Placement Candidate:` note):

- **Legacy tier** — where it ran in the source (app code / stored proc / function / trigger / view).
- **Data-volume signal** — approximate row counts / table sizes it touches (from source, DDL, or CAST
  data-access metrics). "Operates over all open lines in a period" is a signal; a number is better.
- **Set-vs-row** — does it operate set-based (one statement over many rows) or row-at-a-time (a loop)?
  Set-based logic reimplemented as an app loop is the classic N-round-trip cliff.
- **Call frequency** — per-transaction / interactive / scheduled batch / on-demand report.
- **App-tier risk** — one sentence: what specifically goes wrong if this is naively reimplemented as
  app code (latency cliff, DB round-trip storm, memory blow-up loading a large set into the app).
- **Default** — app-tier (always the starting position; the candidate exists because app-tier is *risky*,
  not because DB is preferred).

This evidence flows to P4b, which surfaces each candidate as a placement question, decides the tier,
and reconciles the decision back into `02-domain-model.md` (`### Database Logic Objects`) + the graph
(`PlacementDecision` node + `PLACED_AS` edge). If P4b decides `db-*`, the DB object is generated as an
ordered migration (see the backend-service transformation); if `app-tier` or `app-with-strategy`, no DB
object is created and the strategy (batch/stream/read-model) is annotated on the rule.

### Purpose-First Extraction Method (MANDATORY)

The agent MUST NOT mechanically slice source code into line segments and wrap each in a template. Instead, follow this cognitive process for each source unit:

#### Step A: Understand the Whole

Read the entire source unit. Then answer:
- What business operation does this accomplish? (one sentence)
- What are the inputs and outputs?
- What is the happy path (success case)?
- What makes it fail (rejection cases)?

#### Step B: Identify Decision Points

A decision point is a place where the code chooses between outcomes based on business conditions. Look for:
- Branching that determines whether a transaction succeeds or is rejected
- Calculations that produce a business-meaningful result (rate, amount, allocation)
- State transitions that advance a business workflow (draft → approved → posted)
- Authorization checks that gate access based on role or relationship

Do NOT treat these as decision points:
- Null checks on required parameters (infrastructure concern, not business rule)
- Record-exists validations (referential integrity, handle with FK constraints)
- Logging, audit trail inserts, or error formatting
- Loop mechanics (cursor declarations, counter increments)

#### Step C: Write Rules from Decision Points Only

Each decision point becomes ONE rule (or occasionally 2-3 if it contains distinct business logic). The rule must pass this test:

> "Could a developer who has never seen this legacy code implement the correct behavior from this Statement alone (given the data model)?"

If the answer is no, the Statement needs more business context.

#### Step D: Merge Related Logic

Multiple sequential checks that serve the same purpose become ONE rule:

❌ **BAD — mechanical slicing (5 separate rules):**
- Rule 1: "Amount must not be null"
- Rule 2: "Amount must be greater than zero"
- Rule 3: "Amount must not exceed credit limit"
- Rule 4: "Currency must be valid"
- Rule 5: "Account must be active"

✅ **GOOD — merged into business constraint (1 rule):**
- Rule: "A payment can only be processed if: the amount is positive, does not exceed the customer's credit limit, uses a valid currency, and targets an active account"

### What IS a Business Rule (extract these)

| Category | Example | Why it's a rule |
|----------|---------|----------------|
| Calculation formula | "Overtime pay = base rate × 1.5 for hours exceeding 40/week" | Defines a business-specific computation |
| Business validation | "An order cannot be shipped if any line item is backordered" | Business constraint that gates a workflow |
| State transition | "Invoice status moves from Draft to Posted only after approval and GL distribution" | Defines the business lifecycle |
| Authorization | "Only managers can approve purchase orders above $10,000" | Business access control |
| Rate/bracket logic | "Tax is calculated at 10% for income 0-50K, 20% for 50K-100K, 30% above 100K" | Business-defined tiers |
| Allocation rule | "Overhead costs are distributed across projects proportional to direct labor hours" | Business-defined distribution |
| Deadline/trigger | "Finance charges are assessed on balances unpaid after 30 days" | Time-based business policy |

### What is NOT a Business Rule (do NOT extract these)

| Pattern | Why it's NOT a rule | Correct treatment |
|---------|--------------------|--------------------|
| "Record must exist before processing" | Referential integrity — FK constraint | Note in data model, not BR-ID |
| "Parameter must not be null" | Input validation boilerplate | Part of API request schema |
| "Insert audit log entry" | Cross-cutting concern | Note as side effect of a real rule |
| "Format error message" | Presentation logic | Ignore |
| "Open cursor, fetch next, close cursor" | Data access mechanics | Ignore |
| "Begin transaction / commit / rollback" | Infrastructure concern | Ignore |
| "Set default value if not provided" | Configuration, not business decision | Note in API defaults |
| "The process requires the referenced record to exist" | Vague restatement of FK check | NOT a rule — ignore or merge into actual business rule |

### Quality Over Quantity

**10 well-written semantic rules that an AI agent can implement > 80 mechanical code slices that no one can act on.**

The expected rule yield table provides targets, but those targets assume REAL business rules. If a 2000-LOC module truly contains only 15 distinct business decisions (the rest being data access mechanics, formatting, and infrastructure), then 15 rules is the correct count. Do NOT inflate by splitting or extracting non-rules.

### CAST-Specific Extraction Steps

When CAST Imaging is available:

1. **Query transaction paths** for the domain — these reveal end-to-end business flows
2. **Query complexity metrics** — components with cyclomatic complexity > 20 contain the most rules
3. **Query data access patterns** — which tables this domain reads/writes (drives the domain model)
4. **For each high-complexity component**: fall back to Direct Source Read for the actual logic extraction
5. **Record CAST Reference** in every BR-ID: which CAST query/transaction path identified this component

CAST tells you WHERE to look. Direct Source Read tells you WHAT the rule actually does.

## Multi-Pass Read Protocol (MANDATORY)

Agents MUST follow this protocol when reading source files:

### File Size Tiers

| File LOC | Read Protocol | Minimum Passes |
|----------|---------------|----------------|
| <= 500 | Single full read | 1 |
| 501-1000 | Two-pass read (1-500, 501-end) | 2 |
| 1001-2000 | Two-pass read (1-1000, 1001-end) | 2 |
| 2001-5000 | Multi-pass (sections of 1000 lines) | 3-5 |
| 5000+ | Multi-pass (sections of 1000 lines) | 5+ |

### Proof of Full Read

For every source file reviewed, the agent MUST report:
- Total lines in file
- Number of sections read
- One-line summary of each section's content (e.g., "lines 1-1000: parameter declarations and cursor setup; lines 1001-2000: PO validation and three-way match logic; lines 2001-3000: GL distribution routing")

### Anti-Pattern: Premature CRUD Classification

A source file with LOC >= 200 CANNOT be classified as "CRUD-only" or "no business rules" unless:
1. The agent has read ALL sections of the file
2. The agent provides a 2-3 sentence evidence statement explaining why (e.g., "This file is entirely a column-by-column INSERT from staging to production with no conditionals, calculations, or branching")
3. A one-line dismissal like "setup or CRUD flow in the sampled window" is NEVER acceptable

### Expected Yield by File Size

| File LOC | Expected Rules (Real Business Logic) |
|----------|--------------------------------------|
| < 100 | 0 (likely CRUD) |
| 100-300 | 1-4 |
| 300-500 | 3-8 |
| 500-1000 | 5-15 |
| 1000-2000 | 10-25 |
| 2000-5000 | 20-40 |
| 5000+ | 30-60 |

If a file with 3000+ LOC yields fewer than 15 rules, the agent MUST re-read it — sections were likely skipped.

## Expected Rule Yield

Use this heuristic as a GUIDE, not a hard target. These assume real business rules, not mechanical slices:

| Source File LOC | Expected Rules | Rationale |
|----------------|---------------|-----------|
| < 50 | 0 (CRUD only) | Simple INSERT/UPDATE, no conditionals |
| 50–100 | 0–2 | Minimal logic, possibly one validation |
| 100–300 | 2–5 | Several decision points |
| 300–500 | 5–12 | Moderate business logic |
| 500–1000 | 10–20 | Significant business logic |
| 1000–2000 | 15–35 | Complex business operations |
| 2000+ | 25–50 | Major business process (e.g., payroll processing, billing engine) |

**IMPORTANT**: These targets assume REAL business rules. If a 2000-LOC module is mostly data access boilerplate with only 15 genuine business decisions, then 15 is the correct count. Do NOT inflate to hit a number.

**Red flag**: If you have 80 rules and most read like "record must exist" or "field must not be null" — you've extracted infrastructure, not business logic. Rewrite.

**Red flag**: If you hit the target count but >50% of rules use shared Statement patterns (same structure, different entity name), the count is inflated. Reduce to only unique business constraints and accept a lower count. **30 implementable rules > 100 template rules.**

### Minimum Service Targets (quality-adjusted)

| Service Complexity | Min Rules | Min Endpoints |
|-------------------|-----------|---------------|
| Supporting (priority 3) | 15–30 | 10–20 |
| Business (priority 2) | 40–70 | 20–40 |
| Core (priority 1) | 60–100+ | 30–50+ |

## Business Rule Numbering

```
BR-<DOMAIN>-<GROUP>-<NUMBER>

Domain codes (extend as needed):
  AC = Account/Financial    OR = Order Management
  IT = Inventory/Item       WH = Warehouse
  CU = Customer            CR = Credit Risk
  PA = Payment             WF = Workflow
  NT = Notification        RP = Reporting
  AU = Audit               SC = Security
  CF = Configuration       IN = Integration
  DD = Direct Delivery     DM = Document Management
  SH = Shipping            PR = Procurement
```

## Rule Quality Standard

Each rule MUST have ALL of these fields:

```markdown
### BR-<DOM>-<GRP>-<NNN>: <Descriptive Name>

**Source Reference:** `<exact file path>` : lines XX-YY
**Cross-Reference:** `<caller file path>` : `<Method/Function>()` : lines XX-YY (if found)
**Discovery Method:** Direct Source Read | CAST Imaging (Hybrid)
**CAST Reference:** <CAST object ID / transaction path / query> (if applicable, omit if Direct only)

**Statement:** <Semantic business statement — what the business rule MEANS, not what the code does>
**Intent:** <Why this rule exists: Validation | Calculation | Authorization | State Transition | Routing | Compliance>

**Logic:**
```pseudocode
<Implementation-level pseudocode extracted from the source>
<Uses real variable/column/table names from the code>
<This is EVIDENCE — the Statement above is what drives implementation>
```

**Data Dependencies:**
- Reads: <table.column, table.column, ...>
- Writes: <table.column, table.column, ...>

**Side Effects:**
- Publishes: <event or message>
- Calls: <other programs/modules/functions>

**Concrete Example (MANDATORY):**
- API Input: `<HTTP method> <path> <JSON body with real domain field values>`
- Success Output: `<HTTP status> <JSON response body with actual fields>`
- Error Input: `<JSON body that violates this rule>`
- Error Output: `<HTTP status> <JSON error body with specific message>`
```

## Semantic Elevation (CRITICAL FOR AUTOMATIBILITY)

The #1 factor that determines whether AI-DLC can implement a rule autonomously is **Statement clarity**. A semantic business statement enables implementation; a code transcription does not.

### The Two Layers

Every BR-ID has two distinct layers:

| Layer | Purpose | Audience | Example |
|-------|---------|----------|---------|
| **Statement** (semantic) | What the business rule MEANS | Domain expert, AI-DLC agent | "Overtime pay is 1.5× base rate for hourly and salaried employees" |
| **Logic** (mechanical) | How the legacy code implements it | Human validator, traceability | `IF @OTHours > 0 AND @EmpType IN (1,2) THEN @OTRate = @BaseRate * 1.5` |

The **Statement** drives implementation. The **Logic** provides traceability to the source system.

### Before/After Examples

❌ **BAD — mechanical dump (unimplementable by AI-DLC):**
```
Statement: IF @OTHours > 0 AND @EmpType IN (1,2) THEN SET @OTRate = @BaseRate * 1.5
```

✅ **GOOD — semantic business statement (implementable by AI-DLC):**
```
Statement: Overtime pay is calculated at 1.5× the employee's base hourly rate, applicable only to Hourly (type 1) and Salaried (type 2) employees
Intent: Calculation
Logic: OT_Pay = Base_Rate × 1.5 × OT_Hours WHERE Employee_Type IN (Hourly, Salaried)
```

❌ **BAD:**
```
Statement: IF LEN(@InvoiceNum) = 0 RAISERROR('Invoice number required', 16, 1)
```

✅ **GOOD:**
```
Statement: Every invoice must have a non-empty invoice number before it can be posted
Intent: Validation
Logic: REJECT posting if InvoiceNumber is null or empty string → raise error "Invoice number required"
```

❌ **BAD:**
```
Statement: UPDATE bPRTH SET Status = 4 WHERE Status = 3 AND BatchId = @BatchId
```

✅ **GOOD:**
```
Statement: When a payroll batch is finalized, all timecard entries in that batch transition from "Approved" (3) to "Posted" (4) status
Intent: State Transition
Logic: UPDATE TimeCardHeader SET Status = Posted WHERE Status = Approved AND Batch = current_batch
```

### Semantic Statement Rules

1. **No code variables** — use business terms (`employee type` not `@EmpType`, `base rate` not `@BaseRate`)
2. **No SQL/programming syntax** — use natural language (`must have a non-empty invoice number` not `LEN(@X) > 0`)
3. **No legacy table/column names** — the Statement MUST NOT reference source system table names, column names, or stored procedure names. These belong ONLY in the Logic section. If the target architecture doesn't have that table, the Statement can't reference it.
4. **Declare WHAT, not HOW** — the statement says what must be true, the logic shows how it's currently achieved
5. **Domain expert test** — could a business analyst understand this statement without seeing the code? If not, rewrite.
6. **Implementation-ready** — could a developer implement this from the statement alone (with the data model)? If not, add context.
7. **Architecture-agnostic** — the Statement must remain valid even if the target system uses different table names, different column names, or a completely different data model. It describes the BUSINESS constraint, not the data structure.

### Statement Compliance Gate (Parent Verification — MANDATORY)

After the subagent produces `01-business-rules.md`, the orchestrator MUST check for legacy leakage in Statements:

**Automated check (grep-based):**
```bash
# Extract Statement lines and check for legacy patterns
grep "Statement:" 01-business-rules.md | grep -ciE \
  '@[A-Z][a-z]|b[A-Z]{2,4}\.|[A-Z]{2,4}\.[A-Z]|dbo\.|vsp[A-Z]|SET @|SELECT.*FROM|UPDATE.*SET|INSERT INTO|RAISERROR|IF.*@|LEN\(@|ISNULL\(' \
  > /dev/null && echo "FAIL: Legacy implementation details in Statements"
```

**What triggers a FAIL:**
- `@VariableName` — SQL variable syntax
- `bTableName.Column` — legacy table.column reference
- `TABLENAME.ColumnName` — uppercase legacy reference
- `dbo.procedureName` — stored procedure reference
- SQL keywords in Statement (SELECT, UPDATE, INSERT, SET, RAISERROR, ISNULL)
- Programming constructs (IF @, LEN(@)

**What to do on FAIL:** Re-delegate to subagent with specific instructions: "These N rules have legacy table/column names in their Statement field. Rewrite each Statement as a business-domain sentence that would make sense to a domain expert who has never seen the legacy database. Keep legacy references ONLY in the Logic section."

**Threshold:** Zero tolerance. ANY Statement that references a legacy table/column/variable name fails. The Statement is the spec that drives implementation — if it references artifacts that don't exist in the target system, the implementation agent will be confused.

### Semantic Quality Gate

A rule FAILS the semantic quality check if:
- The Statement contains `@variables`, `IF/THEN`, `SET`, `UPDATE`, `SELECT`, or SQL syntax
- The Statement cannot be understood without reading the Logic section
- The Statement describes code mechanics rather than business intent
- A non-technical domain expert would not recognize the Statement as a real business rule

**MANDATORY**: Source references must point to the EXACT file and line range. Logic must be REAL pseudocode from the source, not generic descriptions. Data dependencies must use REAL table and column names from the source system. If you cannot read a file, state "FILE NOT ACCESSIBLE" — do NOT fabricate content.

## Anti-Condensation Rules (Extraction Behavior)

These rules govern HOW the agent writes business rules. They prevent condensation at write time — before the complexity validation loop even runs.

### Writing the Logic Section

1. **Every conditional branch in the source gets a line in the Logic.** If the source has `IF A THEN X, ELSE IF B THEN Y, ELSE Z` — the Logic MUST have three paths, not "validates input and routes accordingly."

2. **Every formula term stays explicit.** If the source calculates `result = (base * rate * (1 + adjustment)) / divisor` — the Logic MUST show all four terms. Do NOT write "calculates the adjusted amount."

3. **Every state in a state machine is named.** If the source has 5 states (Draft, Submitted, Approved, Rejected, Posted) with transitions between them — the Logic MUST list all 5 states and their transition conditions. Do NOT write "manages lifecycle transitions."

4. **Compound conditions stay compound.** If the source checks `IF (typeA OR typeB) AND amount > limit AND NOT exempted` — the Logic preserves all three clauses. Do NOT collapse to "validates eligibility."

5. **Rate tiers stay enumerated.** If the source has brackets (0-30 days: rate1, 31-60: rate2, 61-90: rate3, 90+: rate4) — the Logic lists ALL tiers. Do NOT write "applies tiered rate based on age."

### Writing the Statement Section

6. **The Statement is a business declaration, not a summary.** GOOD: "Overtime pay is 1.5x base rate for hours beyond 40/week, applicable to hourly and salaried employees." BAD: "Calculates overtime pay."

7. **If the rule has multiple conditions, the Statement names them.** GOOD: "An order can only ship when: all items are in stock, payment is confirmed, and the shipping address is validated." BAD: "Validates order readiness."

### Batching Over Condensing

8. **If the output would be too long for one response — write in multiple batches.** Use `fs_append` for rules 6-10 after writing rules 1-5. NEVER condense rules to fit a single response.

9. **Each rule is a SEPARATE entity.** If a source function contains 3 distinct business decisions — write 3 rules, not 1 rule that "handles the processing flow."

### What Condensation Looks Like (Examples to Avoid)

| Source Has | Condensed Version (BAD) | Faithful Version (GOOD) |
|-----------|------------------------|------------------------|
| 5 IF/ELSE branches | "Validates and routes based on type" | "IF type=A: action1, ELSE IF type=B: action2, ELSE IF type=C: action3, ELSE IF type=D: action4, ELSE: default action" |
| Formula with 4 terms | "Calculates the fee" | "fee = base_amount * rate_multiplier * (1 + surcharge_pct) - discount" |
| State machine with 5 states | "Manages document lifecycle" | "Draft→Submitted (on submit), Submitted→Approved (on manager approval), Submitted→Rejected (on rejection), Approved→Posted (on period close), Rejected→Draft (on revision)" |
| Compound AND/OR condition | "Checks eligibility" | "Eligible IF (employee_type IN [hourly, salaried]) AND (tenure_months >= 6) AND NOT (on_probation OR terminated)" |

## Incremental Execution Protocol

For large systems (100+ source files per service), work incrementally:

### Batch Processing
- Process 5–10 critical source files per session (highest LOC first)
- Commit after each batch (update completion summary with accurate counts)
- Focus on files with LOC > 200 first — they contain the most business logic

### Context Management
- Process ONE sub-domain at a time — complete it before moving to the next
- Release context between sub-domains to avoid overload
- For very large files (2000+ LOC): extract in sections (validation rules, then calculations, then state transitions)

### Progress Tracking
Update `06-completion-summary.md` after each batch:
```markdown
## Progress
| Source File | LOC | Rules Extracted | Status |
|-------------|-----|----------------|--------|
| <module-a> | 2465 | 32 | ✅ Complete |
| <module-b> | 1633 | 18 | ✅ Complete |
| <module-c> | 945 | 0 | ⏳ Pending |

## Complexity Preservation
| Source Component | Source Complexity | Rules | Total Spec Complexity | Ratio | Status |
|-----------------|------------------|-------|-----------------------|-------|--------|
| <module-a> | 30 | 5 | 24 | 1.25 | OK |
| <module-b> | 45 | 3 | 12 | 3.75 | FLAGGED — pass 2 needed |
| <module-c> | — | — | — | — | Pending |

## Complexity Summary
- Components analyzed: <N>
- All ratios OK (<=3.0): <N>
- Flagged (ratio >3.0): <N>
- Unresolved (after max passes): <N> → forwarded to Phase 4a
```

### CRUD-Only Notation
Simple CRUD files (INSERT/UPDATE with no conditionals) get noted in the inventory as "no rule — CRUD only" and do NOT receive BR-IDs. This prevents rule count inflation.

### Artifact Synchronization After Each Extraction Pass

After every extraction batch that adds 20+ rules, the agent MUST verify:

1. **Domain model coverage**: Every table referenced in newly extracted rules must appear in `02-domain-model.md`. If missing, add the DDL.
2. **API completeness**: Every posting/processing operation discovered must map to an endpoint in `03-api-design.md`. If a new operation was found (e.g., "unapproved invoice approval workflow"), add the endpoint.
3. **Event completeness**: If new rules reveal event-driven behavior (publishes event, consumes event), update `04-event-contracts.md`.
4. **Completion summary**: Update rule count, table count, endpoint count, event count.

### Artifact Lag Indicator

If `01-business-rules.md` has N rules but `02-domain-model.md` has fewer than N/10 tables, the domain model is likely stale. Same for API endpoints (expect 1 endpoint per 10-20 rules as a rough guide).

## Endpoint-to-Rule Coverage Validation (Per-Service — MANDATORY)

After extracting all rules for a service, the agent MUST verify that every endpoint in `03-api-design.md` has at least one business rule that drives it. Endpoints without driving rules indicate either a missing extraction or a pure CRUD endpoint.

### Protocol

```
For each endpoint in 03-api-design.md (or 04-api-contract.yaml):
  1. Search 01-business-rules.md for BR-IDs whose Concrete Examples reference this endpoint path
  2. Search BR-ID Statements for operations that map to this endpoint's purpose
  3. Classify the endpoint:

     (a) COVERED — at least one BR-ID drives this endpoint's behavior
     (b) CRUD-ONLY — endpoint performs simple Create/Read/Update/Delete with no business logic
         → Mark explicitly in 03-api-design.md: "CRUD — no BR-ID (standard data access)"
     (c) UNCOVERED — endpoint has a business purpose but NO BR-ID drives it
         → This is an EXTRACTION GAP — extract the missing rule NOW
```

### What "Drives" Means

An endpoint is "driven by" a BR-ID if:
- The BR-ID's Concrete Example uses that endpoint (Input field shows the path)
- The BR-ID's Statement describes the operation that endpoint performs
- The BR-ID's Side Effects produce the response that endpoint returns

### CRUD-Only Endpoints (Acceptable — Mark Explicitly)

These endpoints have NO business logic — they simply expose database CRUD:
- `GET /resources` — list with pagination (no filtering rules beyond standard params)
- `GET /resources/:id` — read by ID (no authorization rules beyond global auth)
- `DELETE /resources/:id` — soft/hard delete (no conditional logic)

For each CRUD-only endpoint, add to `03-api-design.md`:
```markdown
### GET /api/v1/products
- **Driven by:** CRUD — no BR-ID (standard list with pagination)
```

### Uncovered Endpoints (EXTRACTION GAP)

If an endpoint has a business purpose (validation, calculation, state transition, authorization beyond basic auth) but no BR-ID references it — the agent MUST:
1. Re-read the source code that implements this endpoint's logic
2. Extract the missing business rule(s)
3. Add BR-IDs to `01-business-rules.md`
4. Re-run the complexity validation loop for the new rules

### Coverage Report (in 06-completion-summary.md)

```markdown
## Endpoint Coverage
| Endpoint | Method | Status | Driving BR-IDs |
|----------|--------|--------|----------------|
| /api/v1/orders | POST | COVERED | BR-OR-VAL-001, BR-OR-VAL-002 |
| /api/v1/orders | GET | CRUD-ONLY | — |
| /api/v1/orders/:id/approve | PUT | COVERED | BR-OR-WF-005 |
| /api/v1/orders/:id | DELETE | CRUD-ONLY | — |
```

## Semantic Preservation Validation (Per-Service — MANDATORY)

After extracting all rules for a service, the agent MUST run a multi-dimensional semantic preservation check. This validates that business-relevant information from the source was faithfully captured in the spec — using 8 independent dimensions rather than a single complexity ratio.

### Why Multi-Dimensional (Not Just Cyclomatic Complexity)

A single control-flow ratio produces false positives and false negatives:
- **False positive:** Source has 25 IF/ELSE (mostly retries/reconnects), rule has 3 (the actual business logic). Ratio screams "condensed" but business logic IS fully captured.
- **False negative:** Source has `price = formula(inputs)` (complexity=1), but the formula encodes 15 business constants. Single ratio says "fine" but business IP is missing from spec.

The 8-dimension vector eliminates both by checking EACH semantic aspect independently.

### The 8 Dimensions

| # | Dimension | Source Count (from Phase 1) | Spec Count (from rule fields) | What a Gap Means |
|---|-----------|---------------------------|------------------------------|-----------------|
| 1 | **Control-flow** | IF/ELSE/CASE/LOOP/AND/OR (all, incl. infra) | Decision points in Logic section | If ONLY this has a gap → likely infrastructure noise (retries etc.) — acceptable |
| 2 | **Data-flow** | Tables/columns READ + WRITTEN | Data Dependencies section entries | Missing data access → rule may not read/write what source does |
| 3 | **Constants** | Rates, thresholds, magic numbers | Named values in Logic/Statement | Missing constants → formula terms or tier brackets were dropped |
| 4 | **State transitions** | Status field changes | States in Logic | Missing states → lifecycle steps were condensed |
| 5 | **Outcomes** | Distinct return/response paths | Concrete Example outputs | Missing outcomes → error/success cases dropped |
| 6 | **Data writes** | INSERT/UPDATE operations | Side Effects section | Missing writes → mutations not documented |
| 7 | **Integrations** | External calls, queue publishes | Rules referencing external services in Logic/Side Effects | Missing integrations → cross-service calls lost (CRITICAL) |
| 8 | **Error paths** | Business rejection reasons | Error cases in Examples | Missing error paths → rejection scenarios dropped |

### How It Works

```
For each service (after initial extraction is complete):

  PASS 1: Count and Compare (per component)
    For each source component that produced rules:
      - Get source vector from graph (srcControlFlow, srcDataFlow, srcConstants, etc.)
      - Sum spec vector across all rules from that component:
        specControlFlow = sum of decision points in all rules' Logic sections
        specDataFlow = sum of entries in all rules' Data Dependencies
        specConstants = sum of named constants across all rules
        specStateTransitions = sum of states in all rules' Logic
        specOutcomes = sum of distinct outputs in all rules' Concrete Examples
        specDataWrites = sum of entries in all rules' Side Effects
        specIntegrations = count of integration references in rules
        specErrorPaths = sum of error cases in all rules' Concrete Examples
      - Compare each dimension independently:

    Per-dimension assessment (thresholds from saam-calibration.yaml → complexity):
      - source[dim] > 0 AND spec[dim] == 0 → CRITICAL (entire dimension missing)
      - source[dim] / spec[dim] > complexity.ratio_threshold (default: 3.0) → FLAGGED
      - source[dim] / spec[dim] <= complexity.ratio_threshold → OK
      - source[dim] == 0 → N/A (dimension not present in source)

    Overall component assessment:
      - Any CRITICAL dimension → MUST investigate
      - complexity.multi_flag_threshold+ (default: 3) non-control-flow dimensions FLAGGED → likely condensed
      - 1 FLAGGED dimension AND it's control-flow → likely infrastructure noise (ACCEPTABLE)
      - All OK → extraction is faithful

  IF no flagged components → EXIT LOOP

  PASS 2: Targeted Re-Extraction (flagged dimensions only)
    For each flagged component:
      - Re-read ONLY the source sections relevant to the FLAGGED dimensions
      - If constants flagged: look for hardcoded values, rate tables, thresholds
      - If errorPaths flagged: look for business rejection reasons (not infra retries)
      - If stateTransitions flagged: look for status field assignments
      - If dataWrites flagged: look for INSERT/UPDATE with business meaning
      - Expand rules or create new rules to cover the missing dimensions
      - Recount spec vector

  STOP CONDITIONS:
    - All dimensions OK or only control-flow flagged alone → DONE
    - No improvement (>20%) between passes in flagged dimensions → STOP
    - Max 3 passes → STOP
    - Remaining gaps are ONLY in control-flow → STOP (infrastructure noise — acceptable)

  FOR REMAINING FLAGGED RULES (after max passes):
    - Set preservationFlag = 'unresolved'
    - Set flaggedDimensions = '<comma-separated list of still-flagged dims>'
    - Add note to rule: "Preservation: UNRESOLVED (dimensions: <list>). 
       Flagged for Phase 4a BA review."
    - Update graph
```

### Spec Vector Counting Protocol

After writing each rule, count its spec vector from the rule's own fields:

| Spec Dimension | Where to Count | What to Count |
|---------------|----------------|---------------|
| specControlFlow | Logic section | IF/WHEN/CASE/ELSE/AND/OR paths |
| specDataFlow | Data Dependencies field | Each table.column entry |
| specConstants | Logic + Statement | Named rates, thresholds, numbered values |
| specStateTransitions | Logic | Each state assignment or transition |
| specOutcomes | Concrete Examples | Distinct Success + Error outputs |
| specDataWrites | Side Effects field | Each write/publish entry |
| specIntegrations | Rules' Logic/Side Effects sections | Each external service call mentioned |
| specErrorPaths | Concrete Examples | Each distinct Error output |

### Source Vector: Where Numbers Come From

| Mode | When Fully Available | Partially Available |
|------|---------------------|---------------------|
| **Direct Source** | Phase 1 (agent counted all 8 while reading) | — |
| **CAST** | Phase 4 (agent counts full 8 during deep read; CAST gave control-flow in Phase 1) | Phase 1 (only srcControlFlow from CAST) |
| **Hybrid** | Phase 4 (full 8 from direct read for each component) | Phase 1 (srcControlFlow from CAST) |

**If source vector dimensions are missing** when the validation loop runs: The agent MUST count them from the source file NOW (during Phase 4 deep extraction), then store on the graph before comparing.

### Key Principle: Control-Flow Alone Is Not a Signal

If the ONLY flagged dimension is control-flow, and all other 7 dimensions are OK — this means the source has infrastructure branching (retries, reconnects, error formatting, transaction management) that was CORRECTLY excluded from the spec. This is NOT condensation. Do NOT re-extract.

Condensation is indicated by gaps in BUSINESS-RELEVANT dimensions: constants, state transitions, outcomes, data writes, integrations, or error paths. These represent actual business logic that was dropped.

### Preservation Report (in 06-completion-summary.md)

```markdown
## Semantic Preservation
| Source Component | Flagged Dimensions | Status | Notes |
|-----------------|-------------------|--------|-------|
| OrderProcessor.cs | none | OK | All 8 dimensions preserved |
| PaymentCalc.cs | constants, errorPaths | FLAGGED → resolved pass 2 | Added 3 rate constants, 2 rejection cases |
| SessionManager.cs | control-flow only | OK (infra noise) | 15 retries/reconnects correctly excluded |
| PricingEngine.cs | constants, outcomes | UNRESOLVED | → Phase 4a BA review |
```

## Per-Service Spec Directory Structure (ALL services)

Every service gets its own directory with the following mandatory files:

```
spec/microservices/<service-name>/
├── INDEX.md
├── 00-component-inventory.md
├── 01-business-rules.md
├── 02-domain-model.md
├── 03-api-design.md
├── 04-api-contract.yaml       (OpenAPI 3.1 — naming contract for tests + code gen)
├── 05-dependencies.md         (cross-service integration contracts — consumer perspective)
├── 06-completion-summary.md
└── FINAL-EXTRACTION-COMPLETE.md
```

**This structure applies to ALL services — not just large ones.** Services with >50 rules may additionally split `01-business-rules.md` into sub-files by domain group, but the directory and mandatory file set is always the same.

### 05-dependencies.md (Cross-Service Integration)

**Generated in Stage 1.5** (NOT during per-service extraction). This file requires all provider services to have their `04-api-contract.yaml` already written.

This file defines how the service CONSUMES other services — the exact calls it makes, headers it sends, responses it expects, and how it handles failures. It is the consumer-perspective view of integration contracts.

**Generated from:** The graph's `CALLS` edges (populated in Phase 2) + provider service `04-api-contract.yaml` files.

**Template:**

```markdown
# Dependencies: <Service Name>

## Services Consumed

### <Provider Service Name> (sync REST)

#### Call: <Operation Name>
- **Triggered by:** BR-<ID> (<rule that requires this call>)
- **Method:** POST
- **Path:** /api/v1/<provider-path>/<endpoint>
- **Headers:**
  - x-tenant-id: {propagated from request context}
  - x-correlation-id: {propagated from request context}
  - Authorization: Bearer {service-to-service token}
- **Request body:**
  ```json
  { "field1": "type", "field2": "type" }
  ```
- **Success response:** <status code>
  ```json
  { "responseField": "type" }
  ```
- **Error handling:**
  | Status | Meaning | Action |
  |--------|---------|--------|
  | 402 | Insufficient funds | Mark operation as failed, notify caller |
  | 503 | Provider unavailable | Retry (3x, exponential backoff 2s/4s/8s) |
  | 408 | Timeout | Same as 503 |
- **Resilience:**
  - Timeout: 10s
  - Retries: 3 (exponential backoff)
  - Circuit breaker: open after 5 failures, half-open after 30s
  - Fallback: <what to do if all retries fail>

### <Provider Service Name> (async Event)

#### Publishes: <EventName>
- **Triggered by:** BR-<ID>
- **Channel:** <topic/queue name>
- **Schema:**
  ```json
  { "field": "type" }
  ```
- **Guarantees:** at-least-once
- **Ordering:** by <partition key>

#### Consumes: <EventName> (from <publisher service>)
- **Channel:** <topic/queue name>
- **Action:** <what this service does when event arrives>
- **Idempotency:** <how duplicates are handled>

## No Dependencies

If the service has NO cross-service calls (fully self-contained), state explicitly:
"This service has no external dependencies. All operations complete within its own database."
```

**Rules for 05-dependencies.md:**
- Every `CALLS` edge in the graph where this service is the SOURCE must appear here
- Every event this service publishes or consumes must appear here
- Request/response shapes MUST match the provider's `04-api-contract.yaml` field names exactly
- Error handling and resilience parameters are ARCHITECTURAL decisions (from Phase 2), not deferred to implementation
- The `Triggered by` field links back to BR-IDs — connecting business rules to integration points

Note: Test suite files are NOT created during Phase 4. They belong to Phase 4c (Test Suite Generation) and live in `validation/`.

## Quality Gates (MANDATORY)

Do NOT claim Phase 4 completion unless ALL of the following are true:

- [ ] Every sub-domain meets the minimum rule count for its complexity tier
- [ ] Every rule has REAL pseudocode extracted from actual source (not generic placeholders)
- [ ] Every rule references REAL table/column names from the source system
- [ ] **Template detection**: No more than 3 rules in a service share the same Statement structure (entity name swap = template)
- [ ] **DDL domain fit**: No table has columns from an unrelated domain (e.g., `amount_total` on an identity table). Every column traces to a legacy column or BR-ID requirement.
- [ ] **Example concreteness**: Every Concrete Example uses domain-specific field names with realistic values (not generic envelopes)
- [ ] **Algorithm linkage**: Every state in a state machine and every formula in an algorithm spec is referenced by at least one BR-ID
- [ ] DDL in domain model uses REAL column names derived from source tables
- [ ] API endpoints cover every posting/processing operation discovered
- [ ] Events are cross-validated (producer spec matches consumer spec)
- [ ] Completion summary has ACCURATE counts that match actual content in the files
- [ ] **Independent validation**: 5-rule random sample must pass the "implement from Statement alone" test (≥4 of 5 must pass)
- [ ] Human sign-off obtained on each service spec

### Completion Summary Accuracy Gate

After every extraction batch:
1. Count ACTUAL rules in `01-business-rules.md` (grep for rule ID pattern)
2. Count ACTUAL tables in `02-domain-model.md`
3. Count ACTUAL endpoints in `03-api-design.md`
4. Update `06-completion-summary.md` with these REAL counts

The completion summary MUST match reality. If rule count in the summary differs from actual content by more than 5%, the extraction is NOT complete.

**Automated verification command (include in commit hook or final check):**
```bash
# Example for a Unix-like environment
ACTUAL=$(grep -c "^| BR-" 01-business-rules.md)
REPORTED=$(grep "Rule count" 06-completion-summary.md | grep -o '[0-9]*')
if [ "$ACTUAL" != "$REPORTED" ]; then
  echo "MISMATCH: actual=$ACTUAL reported=$REPORTED — update summary!"
fi
```

### Extraction Adequacy Thresholds

Every extraction steering file MUST include:

1. **Coverage requirement** — what percentage of source files with LOC >= N must be reviewed
2. **Minimum rule count** — based on total LOC and expected yield table
3. **Per-file minimums** — large files (2000+ LOC) must yield N+ rules or provide evidence
4. **Failure conditions** — explicit list of what makes a run "failed" requiring re-execution

Example failure conditions:
- Rule count below [threshold] after all batches complete
- Any file with LOC >= [N] dismissed as CRUD without full-read evidence
- Per-SP summaries missing for files >= [N] LOC
- Completion summary not updated to match actual rule count
- More than [X]% of rules fail the semantic quality check (code-as-statement)

### Coverage Heuristic

| Module Total LOC | Min SPs to Review (LOC >= 200) | Min Expected Rules |
|-----------------|-------------------------------|-------------------|
| < 5,000 | All | LOC / 50 |
| 5,000-20,000 | All >= 200 LOC | LOC / 60 |
| 20,000-50,000 | All >= 200 LOC | LOC / 70 |
| 50,000+ | All >= 300 LOC + sample of 100-300 | LOC / 80 |

These are MINIMUMS. If actual yield is significantly below these thresholds, the extraction is incomplete.

### Template Detection Heuristic

```
TEMPLATE DETECTED IF:
- Statement text matches pattern: "<Entity> [verb]s the [generic noun] [generic predicate]"
- Same Statement structure appears with only entity name changed across >3 rules
- Input Example contains "requestedAction" or "topic" fields (not real API fields)
- Output Example contains "outcome recorded" or "result: validated" (not real system responses)
- Rule categories repeat identically across entities (Validation/Decision/Lifecycle/Integration/...)

ACTION: Delete ALL detected template rules. Re-extract from source using Purpose-First method.
```

### Independent Validation Step (MANDATORY before claiming completion)

After spec generation, validate quality independently:
1. Select 5 rules at RANDOM from the generated specs
2. For each rule, attempt to determine: "Could I write a unit test from this Statement + DDL alone?"
3. If ≥2 of 5 rules CANNOT be implemented without reading the legacy source code → the spec FAILS
4. Failed specs must be reworked using the Purpose-First extraction method

Self-assessed scores (automatibility percentages, completion checkmarks) are PROVISIONAL until this validation passes.

**🔴 PROMPT HUMAN**: "Specification for [Service] complete with X rules across Y source files. Minimum target was Z. Please review for missing scenarios or incorrect logic."

### Source Reference Validation (MANDATORY — Per Service)

After extraction is complete for a service, validate that all source references actually point to real files with relevant logic. This catches hallucinated references before they compound in downstream phases.

**Protocol (quick pass — not a full re-read):**

For each service's `01-business-rules.md`:
1. Parse all `Source Reference` lines (extract file paths)
2. For each unique file path referenced:
   - **Verify file exists** (use fuzzy search if exact path fails)
   - **Verify file is non-empty** and has content at the referenced line range
   - **Quick plausibility check**: does the file contain keywords related to the rule? (function name, table name, or domain term from the rule's Statement)

3. **Report:**
   - Files found: N/N (100% = pass)
   - Files with plausible content: N/N
   - Files NOT found: list (requires re-extraction or path correction)

**Failure conditions:**
- Any referenced file genuinely not found (after fuzzy search) → the rule's source reference must be corrected or the rule flagged as "unverified provenance"
- >10% of references point to files that don't contain any related logic → extraction quality is suspect, re-read required

**Implementation:** This can be delegated to a lightweight subagent or run as a script that greps for file existence + keyword presence. It does NOT require reading the full source files — just existence + surface plausibility.

## PoC-Scoped Extraction Mode

### When to Use

- Client needs a working PoC before committing to full modernization
- A specific use case (e.g., AI assistant, dashboard, API) needs only a subset of the domain
- Time-boxed to 1-2 weeks instead of the full 4-8 week extraction cycle

### Principles

1. **Use-case-driven** — start from the user stories/queries the PoC must support, work backward to the source files needed
2. **Read-path focused** — most PoCs query existing data rather than processing transactions. Extract read logic and calculations, not write/posting mechanics
3. **Thin cross-domain** — pull from multiple domains but only the relevant slice (e.g., vendor risk from AP, not full AP invoice lifecycle)
4. **Reference existing specs** — if rules already exist from a prior full extraction, reference them rather than re-extracting
5. **Separate output directory** — PoC specs go in `spec/poc/<poc-name>/` not in the main service specs

### PoC Extraction Template

```
spec/poc/<poc-name>/
├── 00-poc-overview.md        — Vision, use cases, scope (IN/OUT)
├── 01-read-model.md          — Entity relationships for the query side
├── 02-<domain>-rules.md      — Rules per domain slice (one file per domain)
├── 03-api-design.md          — API surface the PoC exposes
└── 04-extraction-summary.md  — SPs reviewed, coverage, known gaps
```

### Scoping Technique

For each PoC use case:
1. Identify the USER QUESTION (e.g., "Which vendors are putting this job at risk?")
2. Identify the DATA needed to answer it (tables, calculations, joins)
3. Identify the SOURCE FILES that produce or query that data (SPs, views, reports)
4. Extract ONLY from those source files
5. Document the ENTITY RELATIONSHIPS that connect the data

### What to SKIP in PoC Mode

- Transaction posting mechanics (how data gets written)
- Batch processing and lifecycle management
- Regulatory reporting (1099, tax, compliance filings)
- Period close and reconciliation
- Error recovery and void/reversal flows
- Audit trail insertion
- Multi-currency conversion (unless the PoC specifically needs it)

### Expected Effort

| PoC Complexity | Source Files to Review | Rules Expected | Duration |
|---------------|----------------------|----------------|----------|
| Simple (single domain, 3-5 queries) | 10-20 SPs | 20-40 | 3-5 days |
| Medium (cross-domain, 8-12 queries) | 30-50 SPs | 40-80 | 1-2 weeks |
| Complex (multi-domain, 15+ queries) | 60-100 SPs | 80-150 | 2-3 weeks |

## Anti-Patterns (STRICTLY FORBIDDEN)

### Category Templating (CRITICAL)
❌ Do NOT generate rules by applying a fixed category template per entity. A pattern like "Validation/Decision/Lifecycle/Integration/Recovery/Calculation/Idempotency/Dependency/Compliance/Reconciliation" rotated across entities is NEVER acceptable. Each rule must describe a UNIQUE business constraint specific to that domain.

**Detection:** If more than 3 rules in a service share the same Statement structure with only the entity name swapped, the entire batch is templated and must be DELETED and re-extracted using Purpose-First method.

### Generic DDL Templates
❌ Do NOT generate DDL from a generic document template. Every column must trace to either (a) a legacy table column or (b) a BR-ID requirement. Columns that appear identically across >5 tables with no domain justification are template artifacts — REMOVE THEM.

### Message-Envelope Examples
❌ Do NOT use generic message-envelope examples. If Input contains fields like "requestedAction", "businessKey", or "topic" and Output contains "outcome recorded" or "result: validated" — it's a template, not a real example. Every Input must use real domain field names with realistic values. Every Output must show the actual API response or state change.

### General
❌ Do NOT mechanically slice source code into 3-4 line segments and wrap each in a rule template — this produces volume without value
❌ Do NOT write generic placeholder rules like "validates input" or "requires record to exist" — extract the ACTUAL business constraint
❌ Do NOT write Statements that read like code — "IF @X > 0 THEN SET @Y" is NOT a semantic statement
❌ Do NOT inflate rule count by splitting one validation into N null-checks — merge related checks into one rule
❌ Do NOT extract infrastructure concerns (FK checks, null guards, cursor mechanics, transaction management) as business rules
❌ Do NOT claim completion with rules that fail the implementability test ("could a developer implement from this Statement alone?")
❌ Do NOT skip reading the source — the inventory is done, Phase 4 READS THE CODE
❌ Do NOT write DDL without real column names from the source tables
❌ Do NOT fabricate source references — if you can't read a file, say "FILE NOT ACCESSIBLE"
❌ Do NOT inflate completion summary counts beyond what actually exists in the files
❌ Do NOT create test suite files (04-test-assertions.md, comprehensive-test-suite.sh) during Phase 4
❌ Do NOT summarize Phase 1 findings — Phase 4 goes DEEPER than Phase 1
❌ Do NOT confuse Logic with Statement — Statement is semantic/business, Logic is mechanical/code
❌ Do NOT self-assess at 100% — self-assessed scores are PROVISIONAL until independently validated

## AI-DLC Readiness Checklist

Before a spec is "ready for Phase 4b/4c" (NOT "complete" — human must still approve):
- [ ] All BR-IDs numbered and unique
- [ ] Every rule has a precise source reference with line numbers
- [ ] Data model is executable DDL with real column names
- [ ] API endpoints cover all operations (CRUD + business)
- [ ] Events match between producer/consumer specs
- [ ] Completion summary counts are accurate
- [ ] Quality gates above are all satisfied

## API Contract Generation (MANDATORY)

After `03-api-design.md` is complete for a service, the agent MUST generate `04-api-contract.yaml` (OpenAPI 3.1 specification). This contract locks field names, endpoint paths, status codes, and response shapes for both test suite generation and code generation.

**Generation sequence:**
1. Read `02-domain-model.md` — map DDL columns to schema fields using the target stack's naming convention
2. Read `03-api-design.md` — map endpoints to OpenAPI paths with methods, parameters, and status codes
3. Apply naming convention consistently (camelCase for JSON fields, kebab-case for paths — or per target stack)
4. Define standard response shapes (error responses, pagination)
5. Write `04-api-contract.yaml`

**The contract MUST be generated during Phase 4 — NEVER deferred to later phases.** Both Phase 4c (test suites) and Phase 5 (code generation) depend on it.

See `saam-api-contract.md` for the full generation protocol, schema mapping tables, and validation checklist.

## Deliverables (ALL MANDATORY — Phase 4 is NOT complete until every item exists)

Phase 4 produces a COMPLETE specification package per service. Business rule extraction alone is NOT enough. The following artifacts MUST ALL exist for EVERY service before Phase 4 can be declared complete:

### Per-Service Artifact Checklist

For each service, verify ALL files exist at `spec/microservices/<service-name>/`:

| # | Artifact | File | Content |
|---|----------|------|---------|
| 1 | Business Rules | `01-business-rules.md` | All BR-IDs with semantic statements, source references, logic, examples |
| 2 | Domain Model | `02-domain-model.md` | Complete executable DDL (CREATE TABLE with indexes, constraints, FKs) |
| 3 | API Design | `03-api-design.md` | All endpoints (method, path, description, request/response) |
| 4 | API Contract | `04-api-contract.yaml` | OpenAPI 3.1 spec — naming authority for tests + code. Generated from #2 and #3 |
| 5 | Dependencies | `05-dependencies.md` | Cross-service integration contracts (consumer perspective: calls, events, resilience) |
| 6 | Completion Summary | `06-completion-summary.md` | Accurate counts matching actual file content |

**The generation sequence is MANDATORY and ordered (per service):**
1. Extract business rules → `01-business-rules.md`
2. Build domain model from rules + source tables → `02-domain-model.md`
3. Design API endpoints from rules + domain model → `03-api-design.md`
4. Generate API contract from domain model + API design → `04-api-contract.yaml`
5. Write completion summary with verified counts → `06-completion-summary.md`

**After ALL services complete their per-service specs (01-04 + 06):**
6. Cross-service dependency compilation → `05-dependencies.md` for each service (see "Stage 1.5: Dependency Compilation" below)

**Phase 4 is NOT complete if any service is missing artifacts 1-5.** The agent MUST NOT present the exit gate until all artifacts exist for all services.

### Additional Deliverables

- [ ] **Frontend spec** (if legacy system has a UI) — generated at `spec/frontend/<app-name>/` using `saam-frontend-spec-template.md`. Uses brownfield mode if source UI exists. Generated AFTER all backend service specs are complete (because it references their `04-api-contract.yaml` files).

## Phase 4 Execution Order (MANDATORY)

Phase 4 has two sequential stages. The agent MUST complete Stage 1 for ALL services before starting Stage 2.

**Stage 1 Service Extraction Order (MANDATORY — Provider-First):**

Services MUST be extracted in dependency order: providers before consumers. This ensures that when a consumer's rules reference another service's API, the provider's contract already exists for accurate cross-referencing.

**Determining extraction order:**
1. Query graph for service dependency edges: `MATCH (a:Service)-[:CALLS]->(b:Service) RETURN a.name, b.name`
2. Produce a topological sort (providers first, consumers last)
3. If circular dependencies exist: extract both services but flag the circular dependency for Stage 1.5 resolution
4. Services with NO dependencies can be extracted in any order (parallelizable in theory, sequential in practice due to context constraints)

**Example:** If `team-service` CALLS `identity-service`, extract identity-service FIRST. Then when extracting team-service rules, the identity-service contract already exists for accurate inter-service reference.

**Why this matters:** Without provider-first ordering, the agent extracting a consumer service may invent provider endpoint paths/field names because the contract doesn't exist yet. Stage 1.5 then has to reconcile these inventions against the real contract — creating unnecessary rework.

```
STAGE 1: Backend Service Specifications (per service, sequential, PROVIDER-FIRST ORDER)
  For each service:
    1. Extract business rules → 01-business-rules.md
    2. Build domain model → 02-domain-model.md
    3. Design API endpoints → 03-api-design.md
    4. Generate API contract → 04-api-contract.yaml
    5. Write completion summary → 06-completion-summary.md
    6. Run quality gates

STAGE 1.5: Cross-Service Dependency Compilation (once, after ALL backend specs done)

  **Subagent delegation:** This step can be delegated to a subagent for context optimization.

  **contextFiles to include:**
  - `.github/skills/saam-phase4-spec-generation/SKILL.md`
  - `.github/skills/saam-api-contract/SKILL.md`
  - `.github/skills/saam-spec-template/SKILL.md`

  **Delegation prompt:**
  ```
  Generate 05-dependencies.md for all services in this engagement.

  READ THESE FILES FIRST (included in your context):
  - saam-phase4-spec-generation.md (Stage 1.5 protocol + 05-dependencies.md template)
  - saam-api-contract.md (contract naming authority)
  - saam-spec-template.md (spec format reference)

  INPUT:
  - All spec/microservices/<service>/04-api-contract.yaml files (provider contracts)
  - Graph CALLS edges (query: graph_traverse or graph_cypher for service dependencies)
  - Each service's 01-business-rules.md (for Triggered-by BR-ID references)

  FOR EACH SERVICE:
  1. Query graph for this service's outgoing CALLS edges
  2. For each dependency: read the provider's 04-api-contract.yaml
  3. Generate spec/microservices/<service>/05-dependencies.md with:
     - Exact endpoint paths from provider contract
     - Request/response shapes matching provider's schema field names
     - Error handling (status codes + actions)
     - Resilience config (timeout, retries, circuit breaker, fallback)
     - Triggered-by BR-ID linking back to business rules
  4. If service has NO outgoing CALLS: write "no external dependencies" explicitly
  5. Reconcile integration dimension:
     - Count specIntegrations from the generated 05-dependencies.md
     - Compare with preliminary count from rules' Logic/Side Effects
     - Flag mismatches (see Stage 1.5 protocol for gap handling)

  PRODUCE EXACTLY: spec/microservices/<service>/05-dependencies.md for EACH service

  NEVER invent endpoint paths or field names. ALL must come from the provider's
  04-api-contract.yaml. If a referenced provider contract doesn't exist, flag it.
  ```

  **Parent verification after subagent returns:**
  - [ ] `05-dependencies.md` exists for every service (including "no dependencies" for isolated services)
  - [ ] Endpoint paths in 05-dependencies match the provider's 04-api-contract.yaml exactly
  - [ ] Every dependency has a Triggered-by BR-ID
  - [ ] Integration dimension reconciliation completed (gaps flagged if found)

  **Protocol:**
  For each service that has CALLS edges in the graph:
    1. Read the service's graph CALLS edges (populated in Phase 2)
    2. For each dependency: read the PROVIDER service's 04-api-contract.yaml
    3. Generate 05-dependencies.md with exact endpoint paths, request/response shapes,
       error handling, and resilience parameters
    4. If a service has NO dependencies: write "no external dependencies" explicitly
    5. Reconcile integration dimension:
       - Recount specIntegrations from 05-dependencies.md (authoritative count)
       - Compare with preliminary count (from rules' Logic/Side Effects during Stage 1)
       - If 05-dependencies has MORE integrations → rules missed some external calls.
         Flag as potential preservation gap (integration not backed by a BR-ID).
       - If rules have MORE integrations → dependency contract missing.
         Add the missing call to 05-dependencies.md.
       - Update the graph: set specIntegrations to the reconciled count
       - If gaps were found: re-run preservation validation for affected services
  
  WHY NOW (not per-service): 05-dependencies.md references OTHER services' contracts.
  Those contracts only exist after Stage 1 completes for all services. Generating it
  earlier would produce incomplete or incorrect dependency specs.

  **Event Schema Compilation (part of Stage 1.5):**
  
  After ALL 05-dependencies.md files are generated, compile `spec/shared/event-schemas/`:
  - Scan all 05-dependencies.md files for "Events Published" sections
  - For each unique event: create a YAML schema file with the payload definition
  - Derive payload fields from the BR-ID that triggers the event (its Side Effects + Data)
  - Create `spec/shared/event-schemas/index.md` listing all events with publishers and consumers
  - VERIFY: every event published by one service has at least one consumer in another service's 05-dependencies
  - If an event has NO consumer → flag as potential dead event (may be intentional for future use)

  **Consumer-Provider Contract Reconciliation (part of Stage 1.5 — MANDATORY):**

  This is the SYNCHRONOUS twin of event-schema reconciliation. Event schemas verify async
  publisher/consumer agreement; this step verifies synchronous REST call agreement between services.

  After ALL `04-api-contract.yaml` and `05-dependencies.md` files exist, for each cross-service
  dependency edge (service A calls service B):

  1. **Endpoint existence:** The endpoint A calls MUST exist in B's `04-api-contract.yaml`
     (exact path + HTTP method). If it does not exist → CONTRACT GAP.
  2. **Response shape agreement:** The response shape A's client expects (the DTO/type it
     deserializes into) MUST match the response schema B's contract publishes for that endpoint.
     Compare field names, types, and cardinality (list vs single object).
  3. **Request shape agreement:** The request A sends MUST satisfy B's endpoint's required
     parameters and body schema.

  **Why this matters:** Each service's contract is internally valid, but two services can be
  generated against DIFFERENT mental models of the same concept. Example: a consumer expects
  a coarse `{ enabledModules: [...], tier, rateLimit }` object from an endpoint, while the
  provider only models per-item checks `{ entitled: bool, itemCode }` and never exposes the
  aggregate endpoint at all. Neither contract is "wrong" alone — the PAIR is inconsistent.
  Per-service conformance checking will NEVER catch this because each service passes its own suite.

  **Resolution when a divergence is found:**
  - If the provider is missing the endpoint → add it to the provider's `04-api-contract.yaml`
    (+ 03-api-design.md + a BR-ID if it computes something) and to the provider's DTOs
  - If the shapes disagree → reconcile to ONE shape. Prefer the shape that matches the domain
    model; update the consumer's client expectation OR the provider's response, whichever is wrong
  - Record the reconciliation as a GAPS entry (must be fixed before P4 exit)
  - Update `05-dependencies.md` on both sides to reference the reconciled contract

  **Output:** `spec/shared/cross-service-contracts.md` — a markdown table, one row per synchronous
  consumer→provider call. Column order is FIXED (the graph importer `import_specs.py` parses it to
  annotate CALLS edges with shapes):

  ```markdown
  | Consumer | Provider | Endpoint | Request Shape | Response Shape | Status |
  |----------|----------|----------|---------------|----------------|--------|
  | ap-service | gl-service | POST /distributions/post | {distributions: [...]} | {postedId} | OK |
  ```

  Status is one of `OK | RECONCILED | GAP`. Any GAP blocks the P4 exit gate. The orchestrator's
  `import_specs.py --all` reads this table and sets `requestShape`/`responseShape`/`verified` on the
  graph CALLS edges, so downstream generation gets the exact cross-service shape (Class-A knowledge).

  **Shared-Convention Reconciliation (part of Stage 1.5 — MANDATORY):**

  This is the THIRD sibling of the two reconciliations above, and it catches a class NEITHER of them
  can. Event-schema and consumer-provider reconciliation both check **pairwise** agreement (does THIS
  consumer agree with THAT provider). This step checks **system-wide** convention uniformity — a class
  of drift that is invisible pairwise because no two services directly disagree; they each just quietly
  picked a different local convention for the same cross-cutting concept.

  **The governing principle (state it, apply it to every cross-service concern):**
  > **Common to `spec/shared/`, module-specific to the service spec.** For ANY concern that appears in
  > more than one service, the common form is defined ONCE in a shared artifact and REFERENCED by every
  > service; only genuine module-specific variation lives in the service spec. This is a first-class
  > SAAM rule — it applies to API conventions, and to every other cross-service concern (events, auth,
  > infra patterns, entity lifecycle, dependency versions), and to any NEW cross-cutting concern that
  > arises. New concerns follow the same discipline: sweep → detect common form → promote to shared →
  > reference from the service → gate conformance.

  **The concrete failure this prevents:** each service is generated in isolation and each makes a
  locally-reasonable choice — the scoping param named `companyId` in some services and `company` in
  others; the list envelope `{items}` here, `{data}` or a nested `{data.data}` there; `page`/`pageSize`
  required by some services and defaulted by others. No two services disagree pairwise, so every existing
  gate passes. The frontend then has to absorb per-service variance instead of a normalized contract —
  the single largest source of frontend wiring pain. Without a step that sweeps the drafted contracts,
  detects the divergence, and normalizes it, this drift ships. This step is that sweep.

  **Protocol — sweep, then WAIT for a human reconciliation signal (never auto-normalize):**

  Renaming `company`→`companyId` across services, or collapsing three list envelopes into one, is a
  real contract change. Some divergences are legitimate (a service genuinely scoped differently). So the
  agent PROPOSES; the human confirms. This mirrors the BA-review model exactly.

  1. **Sweep** all drafted `04-api-contract.yaml` files (and the other shared concerns) and detect, per
     cross-service concern, the common form vs the divergences:
     - company/tenant scoping param name(s)
     - pagination param names (`page`/`pageSize` vs `limit`/`offset`) and required-vs-defaulted
     - list response envelope shape (`{items, pagination}` vs `{data}` vs nested)
     - error response shape, field-name casing, auth/tenant header set
  2. **Write the working file** `assessment/shared-convention-reconciliation.md` — for each concern: the
     proposed common form (majority/most-domain-aligned), every service's current form, and a
     per-divergence recommendation (`normalize to common` vs `keep — legitimately service-specific`)
     with rationale. This file goes in `assessment/` and is the analogue of `ba-review-<service>.md`.
  3. **WAIT for the human reconciliation signal.** The agent does NOT edit any spec until the human
     returns `assessment/shared-convention-reconciliation-completed.md` (or explicitly signals "reconcile
     as proposed"). No silent normalization — this is a desired-state decision, human-owned.
  4. **On the signal, reconcile the specs:** promote each confirmed-common form into
     `spec/shared/common-schemas.yaml` (+ `auth-config.md` for headers), and rewrite each service's
     `04-api-contract.yaml` to REFERENCE the shared form (not redefine it). Divergences the human marked
     "keep" stay in the service spec, annotated with the rationale so a later sweep does not re-flag them.

  **Output + status:** `spec/shared/common-schemas.yaml` becomes DERIVED from the reconciled reality
  (not a forward-declared scaffold), and `assessment/shared-convention-reconciliation.md` carries a
  per-concern status `OK | RECONCILED | GAP`. Any concern still `GAP` (proposed but not yet
  human-reconciled) blocks the P4 exit gate — same teeth as consumer-provider reconciliation.

  **Ordering (load-bearing):** this sweep runs BEFORE Phase 4c generates `08-dtos/` and test suites. The
  DTOs are copied VERBATIM into code (Phase 5) and the tests are generated from the contracts — so if
  the convention drift is not normalized HERE, it propagates into DTOs and tests and then into code,
  where it becomes the frontend-wiring pain. Normalize at the contract, before anything is derived from it.

  **Dependency Version Manifest (part of Stage 1.5):**

  After target stack is confirmed (from Phase 4b or Phase 0 assumption), produce `spec/shared/09-dependency-versions.*`:
  - Format is stack-specific: `.NET` → `Directory.Packages.props`, Java → `pom.xml` (BOM), Node → `package.json` (pinned versions)
  - Pin ALL shared packages to exact GA versions (ORM, messaging, validation, health checks, telemetry, testing)
  - The transformation definition MUST consume this file — generators do NOT choose versions ad-hoc
  - Update when target stack changes or when a package has a known CVE

  Purpose: eliminates version drift between services generated in parallel (batch mode) and prevents
  build failures from incompatible transitive dependencies.

STAGE 1.6: Workflow Compilation (per service + cross-service, after Stage 1.5 — dedicated Workflow subagent)

  **Purpose:** Produce `07-workflows.md` for each service AND `spec/07-cross-service-workflows.md`
  documenting ALL multi-step operations, state machine progressions, event-driven chains,
  and cross-service choreographies. This bridges the gap between individual BR-IDs and
  complete business operations.

  **Why now:** Workflows reference both service endpoints (from 04-api-contract.yaml) AND
  cross-service dependencies (from 05-dependencies.md). Both must exist before workflows
  can be compiled accurately.

  **Coverage rule:** Every BR-ID with Intent: State Transition or with Side Effects
  (publishes event, writes to another entity, triggers async process) MUST appear in at
  least one workflow. Orphaned state-changing rules = implementation gaps.

  **Two analysis modes — the Workflow subagent operates differently based on CAST availability:**

  ### Mode A: CAST-Guided Workflow Compilation (CAST/Hybrid mode)

  CAST provides transaction paths — which ARE essentially the legacy workflows traced
  from entry point to data. The Workflow subagent queries CAST for transactions assigned
  to this service and annotates them with BR-IDs and target endpoints.

  ```
  For each service:
    1. Query CAST transactions for this domain:
       mcp_imaging_transactions(application="<app>", filters="name:contains:<domain>")
    2. For each transaction: get the call graph (nodes + links)
       → This IS the legacy workflow (procedure call sequence)
    3. Map each call graph node to:
       - The BR-ID(s) extracted from that procedure
       - The target API endpoint that implements it
    4. Convert the CAST transaction into a Mermaid sequence diagram
    5. Identify which steps now live in OTHER services (cross-service boundaries)
    6. Document error paths from the legacy error handling patterns
  ```

  **Advantage:** CAST transactions are AUTHORITATIVE — they show the REAL execution
  sequence from the legacy system. No inference needed.

  ### Mode B: Inference-Based Workflow Compilation (Direct Source mode)

  Without CAST, the Workflow subagent derives workflows from the spec artifacts:

  ```
  For each service:
    1. Read 01-business-rules.md — identify all BR-IDs with:
       - Intent: State Transition
       - Non-empty Side Effects (events, writes to other entities)
       - Logic that references other BR-IDs or sequential steps
    2. Read 03-api-design.md — identify endpoint groups that form sequences
       (e.g., /batches/{id}/initialize, /batches/{id}/validate, /batches/{id}/post)
    3. Read 05-dependencies.md — identify cross-service calls
    4. Infer the workflow sequence from:
       - State machine progressions (BR-IDs that change the same entity's status)
       - Endpoint naming patterns (initialize → validate → post → close)
       - Side effects that trigger other BR-IDs
    5. Document error paths from BR-IDs with Intent: Validation (guard clauses)
  ```

  **Risk:** Inference may miss ordering that only the legacy source knows. Mitigated by:
  - Checking against P1 extraction summaries (which document call chains)
  - Flagging low-confidence sequences for human review

  ### Cross-Service Workflows (MANDATORY when workflows span boundaries)

  When a legacy operation becomes a multi-service choreography in the target architecture:

  1. **Each participating service's `07-workflows.md`** documents its PIECE:
     - What triggers it (API call from another service, event consumed)
     - What it does (BR-IDs executed)
     - What it calls next (outbound dependency)
     - What it returns/publishes (response, event)

  2. **`spec/07-cross-service-workflows.md`** (top-level) documents the FULL choreography:
     - Which services participate
     - The end-to-end sequence across all services
     - Who ORCHESTRATES (the service that initiates the chain)
     - Failure modes (what if a mid-chain service fails? rollback? compensation?)

  3. **Spec reconciliation (MANDATORY):** When the Workflow subagent identifies a cross-service
     call in a workflow, it MUST verify:
     - The calling service's `05-dependencies.md` includes this call
     - The provider service's `04-api-contract.yaml` exposes the endpoint
     - If EITHER is missing → **FLAG as a spec gap** and add to a "gaps to fix" list
     - After all workflows are compiled: the orchestrator fixes gaps by updating
       05-dependencies.md or 04-api-contract.yaml for affected services

  **Example of a cross-service workflow:**
  ```markdown
  ### XWF-001: Invoice Posting (AP → GL → Notification)

  **Orchestrator:** ap-service
  **Participants:** ap-service, gl-service, notification-service
  **Trigger:** POST /api/v1/ap/invoices/{id}/post

  ```mermaid
  sequenceDiagram
      participant User
      participant AP as AP Service
      participant GL as GL Service
      participant NOTIF as Notification Service
      participant DB as AP Database

      User->>AP: POST /invoices/{id}/post
      AP->>DB: Validate invoice (BR-AP-VAL-001, BR-AP-VAL-002)
      AP->>GL: POST /distributions/post (BR-AP-POST-003)
      GL-->>AP: {posted: true, journalId: "J-001"}
      AP->>DB: Update invoice status → Posted (BR-AP-POST-004)
      AP-->>User: 200 {status: "Posted", journalId: "J-001"}
      AP->>NOTIF: Event: invoice.posted {invoiceId, vendorId, amount}
      NOTIF->>NOTIF: Send vendor notification (BR-NT-INV-001)
  ```

  **Error paths:**
  - GL posting fails → AP rolls back, invoice stays Validated, returns 502
  - Notification fails → non-blocking (event queued for retry), invoice stays Posted
  ```

  **Subagent delegation:** Dedicated Workflow subagent per service.

  **contextFiles (CAST/Hybrid mode):**
  ```
  - .github/skills/saam-phase4-spec-generation/SKILL.md
  - .github/skills/saam-spec-template/SKILL.md
  - .github/skills/saam-api-contract/SKILL.md
  - .github/skills/saam-cast-imaging-integration/SKILL.md
  ```

  **contextFiles (Direct Source mode):**
  ```
  - .github/skills/saam-phase4-spec-generation/SKILL.md
  - .github/skills/saam-spec-template/SKILL.md
  - .github/skills/saam-api-contract/SKILL.md
  ```

  **Delegation prompt (per service):**
  ```
  Generate 07-workflows.md for service <service-name> (<service-id>).

  ANALYSIS MODE: <CAST/Hybrid | Direct Source>

  READ THESE FILES FIRST (included in your context):
  - saam-phase4-spec-generation.md (Stage 1.6 protocol + workflow format)
  - saam-spec-template.md (spec format reference)
  - saam-api-contract.md (contract naming authority)
  - saam-cast-imaging-integration.md (CAST mode only — transaction query patterns)

  INPUT (read from disk):
  - spec/microservices/<service>/01-business-rules.md (all BR-IDs with intents + side effects)
  - spec/microservices/<service>/03-api-design.md (endpoint definitions)
  - spec/microservices/<service>/04-api-contract.yaml (exact paths + methods)
  - spec/microservices/<service>/05-dependencies.md (cross-service calls)

  CAST ACCESS (CAST/Hybrid mode only):
  - Query transactions for this domain to discover legacy execution sequences
  - Each CAST transaction = one workflow to document
  - Map transaction call graph nodes → BR-IDs → target endpoints

  PRODUCE: spec/microservices/<service>/07-workflows.md

  DOCUMENT EVERY WORKFLOW this service handles. Include:
  - Multi-step business processes (create → validate → post → close)
  - State machine progressions (every entity with lifecycle states)
  - Event-driven chains (entity action → event published → downstream effect)
  - CRUD with side effects (any operation that triggers writes/events beyond the primary entity)
  - Cross-service choreography (any flow that calls another service via 05-dependencies.md)
  - Batch/scheduled operations (period-end, nightly processing, batch posting)
  - Error/rollback flows (what happens when a step fails mid-workflow)

  FORMAT (per workflow):
  ```markdown
  ### WF-<SERVICE>-<NNN>: <Workflow Name>

  **Trigger:** <What initiates this workflow — API call, event, schedule>
  **Entities:** <Which domain entities are affected>
  **BR-IDs involved:** BR-XX-YYY-001, BR-XX-YYY-002, BR-XX-YYY-003
  **State transitions:** <entity>: <from> → <to>
  **Cross-service calls:** <service> via <endpoint> (from 05-dependencies.md)
  **CAST Transaction:** <transaction name> (CAST mode only)

  ```mermaid
  sequenceDiagram
      ...
  ```

  **Executable Step Recipe (MANDATORY — this is what prevents skeleton/stub implementations):**

  A workflow that states only INTENT ("post the batch", "process the payment") lets a code
  generator satisfy it with a stub that returns 200 and does nothing. Each step MUST spell out
  the concrete effects so that a stub is no longer a valid reading of the spec:

  | Step | Reads (entities) | Writes (entity ← value/expression) | Computation | Side Effects |
  |------|------------------|------------------------------------|-------------|--------------|
  | 1 | <entities read> | <entity ← what, with the VALUE or FORMULA> | <the actual formula, not "calculate X"> | <event emitted / cross-service call / row created> |
  | 2 | ... | ... | ... | ... |

  Rules for the recipe:
  - **Writes name the value, not just the target.** "payment_batch.amount ← SUM(eligible invoice
    openAmount − discount)" — NOT "insert payment batch". A generator cannot leave a named-value
    write at 0 without visibly ignoring the spec.
  - **Computations give the formula**, not the intent. "discount = invoice.amount × terms.discountPct
    IF paidWithin(terms.discountDays)" — NOT "apply discount".
  - **Side effects are explicit and named.** "publish PaymentIssuedEvent{batchId, totalAmount, tenantId}"
    and "call gl-service POST /distributions/post with the batch distributions" — NOT "notify GL".
  - **Terminal side effects are mandatory to list.** Every workflow whose purpose is a state change
    MUST end with its terminal writes + events. A workflow that "posts" but lists no ledger write and
    no event is incomplete — fix the recipe before proceeding.

  **Error paths:**
  - If <step N fails>: <rollback? partial state? error response?>
  - If <dependency unavailable>: <circuit breaker? queue for retry?>
  ```

  CROSS-SERVICE VERIFICATION (MANDATORY):
  For every cross-service call in a workflow:
  - Verify the call exists in 05-dependencies.md for this service
  - Verify the provider's 04-api-contract.yaml exposes that endpoint
  - If EITHER is missing: add to GAPS section at end of file

  WORKFLOW COMPLIANCE (ZERO TOLERANCE — same rules as Statement compliance):
  Workflows describe the TARGET system operation, NOT the legacy execution.
  - NEVER reference legacy table names (bXXYY, bZZTT, bAABB, etc.) in workflow descriptions
  - NEVER reference legacy procedure names (vspXXX, bspXXX, exec ...) in sequence steps
  - NEVER reference legacy mechanisms (GLTrans sequence, InUseBatchId, etc.)
  - USE target domain model entity names (from 02-domain-model.md: journal_entries, distributions, etc.)
  - USE target API paths (from 04-api-contract.yaml: POST /api/v1/gl/distributions/post)
  - USE business-domain language in step descriptions ("create journal entry records" NOT "INSERT bXXYY entries")
  - The workflow shows WHAT HAPPENS in business terms with TARGET system artifacts
  - Legacy implementation details belong ONLY in BR-ID Logic sections (traceability)
  
  Test: could a developer who never saw the legacy database implement this workflow
  using only the target domain model and API contract? If not → rewrite the step.

  COVERAGE TABLE (at end of file):
  ```markdown
  ## BR-ID Workflow Coverage
  | BR-ID | Intent | Workflow(s) | Covered |
  |-------|--------|-------------|---------|
  | BR-XX-001 | StateTransition | WF-XX-001 | YES |
  | BR-XX-002 | Calculation (no state change) | — | N/A (simple) |
  | BR-XX-003 | StateTransition | — | GAP |
  ```
  Every StateTransition/SideEffect BR-ID showing "GAP" = spec deficiency.

  GAPS SECTION (if any cross-service verification failures):
  ```markdown
  ## Spec Gaps Found
  | Gap | Service | What's Missing | Fix Needed |
  |-----|---------|---------------|------------|
  | WF-AP-003 calls GL POST /distributions/post | gl-service | Endpoint not in 04-api-contract.yaml | Add endpoint to GL contract |
  | WF-AP-005 event invoice.posted consumed by notification | ap-service | Event not in 05-dependencies.md | Add event publish to AP dependencies |
  ```
  ```

  **After ALL services have workflows compiled — Cross-Service Index:**

  The orchestrator (or a final Workflow subagent pass) produces `spec/07-cross-service-workflows.md`:
  - Read ALL per-service `07-workflows.md` files
  - Identify workflows that span multiple services (those with cross-service calls or events)
  - Compile into end-to-end choreography diagrams showing the FULL chain
  - Document: orchestrator service, participants, failure modes, compensation patterns

  **Parent verification after Workflow subagent returns:**

  The orchestrator delegates to a **Workflow Validator subagent** (fresh context, same pattern
  as the P4 Validator for spec extraction). The Validator checks the workflow file independently.

  **Workflow Validator contextFiles:**
  ```
  - .github/skills/saam-phase4-spec-generation/SKILL.md (Stage 1.6 compliance rules)
  - .github/skills/saam-spec-template/SKILL.md
  ```

  **Workflow Validator input (read from disk):**
  ```
  - spec/microservices/<service>/07-workflows.md (the file to validate)
  - spec/microservices/<service>/01-business-rules.md (for BR-ID coverage check)
  - spec/microservices/<service>/02-domain-model.md (for entity name verification)
  - spec/microservices/<service>/04-api-contract.yaml (for path verification)
  - spec/microservices/<service>/05-dependencies.md (for cross-service verification)
  ```

  **Workflow Validator prompt:**
  ```
  You are the Workflow Validator for <service-name> (<service-id>).
  Check 07-workflows.md for compliance. You have NOT generated this file.

  RUN THESE CHECKS:

  1. LEGACY COMPLIANCE (zero tolerance):
     □ Scan ALL text in 07-workflows.md for legacy patterns:
       - Legacy table prefixes (b+UpperCase: bXXYY, bZZTT, bAABB, etc.)
       - Legacy procedure names (vsp*, bsp*, exec ...)
       - @Variables, SQL keywords (INSERT, UPDATE, SELECT in step descriptions)
       - Legacy mechanism names (check against 02-domain-model.md — if an entity
         name appears in workflows but NOT in the domain model, it's a legacy leak)
     □ PASS = zero violations. ANY violation = FAIL with specific lines.

  2. TARGET DOMAIN ALIGNMENT:
     □ Every entity mentioned in workflow steps exists in 02-domain-model.md
     □ Every API path in sequence diagrams exists in 04-api-contract.yaml
     □ Step descriptions use business-domain language (not code operations)

  3. BR-ID COVERAGE:
     □ Count BR-IDs with Intent=StateTransition in 01-business-rules.md
     □ Count BR-IDs with non-empty Side Effects in 01-business-rules.md
     □ Every such BR-ID appears in at least one workflow
     □ Coverage table at end of file has zero "GAP" entries for state-changing rules

  4. CROSS-SERVICE VERIFICATION:
     □ Every cross-service call in a workflow exists in 05-dependencies.md
     □ Provider endpoints referenced actually exist in their contracts

  5. ERROR PATHS:
     □ Every workflow has at least one error path documented
     □ Error paths specify what happens (rollback? partial? error response?)

  PRODUCE validation report: PASS/FAIL with specific violations list.
  Be STRICT. Do NOT give benefit of the doubt.
  ```

  **Stage 1.6 execution flow (per service):**
  ```
  1. Workflow subagent generates 07-workflows.md
  2. Workflow Validator subagent checks compliance
  3. If FAIL: re-delegate to Workflow subagent with failure list
  4. If PASS: orchestrator proceeds
  5. Max 2 attempts — after 2 failures, escalate to human
  ```

  **Combined parent verification checklist (after Validator PASS):**
  - [ ] `spec/microservices/<service>/07-workflows.md` exists
  - [ ] Workflow Validator returned PASS on all 5 checks
  - [ ] Zero legacy table/column/procedure names in workflow text
  - [ ] Every BR-ID with Intent=StateTransition appears in at least one workflow
  - [ ] Every BR-ID with non-empty Side Effects appears in at least one workflow
  - [ ] Mermaid sequence diagrams use paths from 04-api-contract.yaml (not invented)
  - [ ] Cross-service calls verified against 05-dependencies.md
  - [ ] GAPS section lists any spec inconsistencies found (must be fixed before P4 exit)
  - [ ] Sequential-pair coverage check PASSED (see below)

  **Sequential-Pair Coverage Check (MANDATORY — prevents workflow connector gaps):**

  For every pair of workflows within a service where WF-N's terminal state is the precondition for WF-M's entry:
  1. Identify all terminal states across all workflows (e.g., "invoice status = Open", "receipt status = Received")
  2. Identify all entry preconditions across all workflows (e.g., "eligible transactions exist in workfile", "payment batch created")
  3. For each terminal-state → entry-precondition pair: **document what happens between them**
  4. If the answer is "user-driven selection/preparation" (browse, filter, select, adjust, commit) — this is a **workflow connector gap**

  **Resolution for detected gaps:**
  - Option A: Create a new workflow (WF-XX-NNN) covering the user preparation steps
  - Option B: Add a "Precondition Steps" preamble to WF-M documenting the user actions required before entry
  - Option C: If the gap is trivial (single API call, no user decisions) — document it as a note in WF-M's preconditions

  **The check FAILS if:** Any terminal→entry pair has undocumented user-driven steps between them. The Workflow subagent must fix before proceeding.

  **If verification fails:** Re-delegate with specific gaps.

  **After ALL per-service workflows complete:**
  - Compile `spec/07-cross-service-workflows.md` (top-level index)
  - Fix all gaps found (update 05-dependencies.md and/or 04-api-contract.yaml)
  - Re-run Validator on affected services if contracts were updated
  - **Graph population (Tracker subagent):**
    - For each workflow in per-service 07-workflows.md files:
      → `graph_add_node(nodeType="Workflow", id=<workflowId>, properties={name, service, trigger, stateTransitions, isCrossService: false, errorHandling})`
      → `graph_add_edge(edgeType="ORCHESTRATES", source=<serviceId>, target=<workflowId>)`
      → For each BR-ID in the workflow: `graph_add_edge(edgeType="PARTICIPATES_IN", source=<brId>, target=<workflowId>, properties={stepOrder, role})`
    - For each CROSS-SERVICE workflow in spec/07-cross-service-workflows.md:
      → `graph_update_node(nodeType="Workflow", id=<workflowId>, properties={isCrossService: true, participants: [<serviceId1>, <serviceId2>, ...]})`
      → For each participating service (not the orchestrator): `graph_add_edge(edgeType="CALLS", source=<orchestratorServiceId>, target=<participantServiceId>, properties={workflow: <workflowId>, reason: "<what the call does>"})`
      (NOTE: some of these CALLS edges may already exist from Phase 2. Update properties if so.)
    - For each event in spec/shared/event-schemas/:
      → `graph_add_node(nodeType="Event", id=<eventName>, properties={version, publisher, consumers, payloadSchema, triggerBrId})`
      → `graph_add_edge(edgeType="PUBLISHES", source=<publisherServiceId>, target=<eventName>, properties={triggerBrId})`
      → For each consumer: `graph_add_edge(edgeType="CONSUMES", source=<consumerServiceId>, target=<eventName>)`
      → If workflow triggers this event: `graph_add_edge(edgeType="TRIGGERS_EVENT", source=<workflowId>, target=<eventName>)`

STAGE 1.7: Entity Lifecycle Compilation (Layer A — after Stage 1.6, once, cross-service)

  **Purpose:** Populate the graph from the per-service `### Entity State Model` and `### Data Invariants`
  sections (already written in each `02-domain-model.md` during deep extraction), and compile the
  cross-entity coherence file `spec/shared/entity-lifecycle.md`.

  **Why now:** State transitions are DRIVEN by workflows (Stage 1.6). Cross-entity constraints (one
  entity's state gates another's) only become visible once all per-service state models and workflows
  exist. This mirrors the Stage 1.5/1.6 "compile-shared-after-all-services" rhythm.

  **Steps:**
  1. For each service `02-domain-model.md`:
     - Parse `### Entity State Model` → states + transitions (with guard + trigger BR-ID).
     - Parse `### Data Invariants` → invariants with tier.
  2. **Verify each state machine is CLOSED** (the same checks 4a re-validates): every state reachable
     from initial; every non-terminal state has an outgoing transition; terminal states have none; no
     workflow (Stage 1.6) drives a transition to a state not in the model. Flag violations as spec gaps.
  3. **Compile `spec/shared/entity-lifecycle.md`** — the cross-entity coherence view:
     - List every entity's states + transitions (index).
     - Document CROSS-ENTITY constraints: where one entity's transition is guarded by another entity's
       state (e.g., an order cannot ship while any line is on hold). Each such constraint is also an
       `Invariant` with `Kind = cross-entity`.
  4. **Graph population (Tracker subagent):**
     - For each state: `graph_add_node(nodeType="EntityState", id="<svc>.<entity>.<state>", properties={entity, state, service, isInitial, isTerminal})`
     - Entity owns its states: `graph_add_edge(edgeType="HAS_STATE", sourceId=<tableName>, sourceType="Table", targetId="<svc>.<entity>.<state>", targetType="EntityState")`
     - For each transition: `graph_add_edge(edgeType="TRANSITIONS_TO", sourceId="<svc>.<entity>.<from>", sourceType="EntityState", targetId="<svc>.<entity>.<to>", targetType="EntityState", properties={guard, triggerBrId})`
     - For each invariant: `graph_add_node(nodeType="Invariant", id="<invariantId>", properties={statement, entity, service, kind, tier})` + `graph_add_edge(edgeType="CONSTRAINS", sourceId=<invariantId>, sourceType="Invariant", targetId=<tableName>, targetType="Table")`
     - (These are also written by `import_specs.py` when it re-parses the updated `02-domain-model.md` — the script is the deterministic path; MCP calls are the fallback.)
  5. **Mandatory-DB integrity invariants → DB objects:** for each invariant with `tier = db|both` that is
     an integrity constraint, ensure it appears in that service's `### Database Logic Objects` table
     (usually a trigger or CHECK) with `Placement = mandatory-db-integrity` and `Enforces Invariant = <invariantId>`.
     This is the Layer A ↔ Layer C bridge: integrity invariants are the one non-negotiable DB placement,
     and they flow through the same DB-object generation path as 4b-placed logic.

  **Coverage rule:** every entity with a `status`/`state` column MUST have an Entity State Model, and
  every state-changing BR-ID (Intent: State Transition) MUST name a transition in some entity's model.
  Orphaned state-changers = the same implementation gap that Stage 1.6 catches for workflows.

  **CAST/Hybrid mode (CAST bounds, source extracts):** CAST transaction paths BOUND the reachable states
  and reveal WHICH transitions exist (a transaction moving an entity between statuses is a transition), so
  closure verification is easier than pure inference. But the transition's GUARD (the precondition
  semantics) is business intent — read it from the source the transaction touches. CAST-reported DB CHECK
  constraints/triggers ARE existing DB-tier invariants (structural) — capture them as tier=db directly;
  their business meaning still comes from the source read. Hybrid is the default here precisely because
  CAST alone gives the shape of the machine, not the guards that make it correct.

STAGE 1.8: Extensibility Model Compilation (Layer B — after Stage 1.7, once, cross-service)

  **Purpose:** Compile `spec/shared/extensibility-model.md` — the SINGULAR common-code engine that makes
  the product configurable per instance — and annotate the per-service BR-IDs that are parameterized by it.

  **The principle (why singular):** A legacy product serves many instances from ONE code base via
  user-defined fields, metadata-driven behavior, and configuration/parameter surfaces. Data POWERS a
  customization (a specific instance's config); code DEFINES the mechanism (the engine). We do NOT support
  different-code-per-instance — that is a processual antipattern, not a case to preserve. For any observed
  instance-to-instance variation we either (i) REPRODUCE it via the engine, (ii) UNIFY behavior and require
  instances to converge, or (iii) DROP it as obsolete. That reproduce/unify/drop call is a 4a decision.

  **Why now:** extension points are referenced by BR-IDs across services, and the engine is shared. Like
  events/workflows/lifecycles, it can only be compiled coherently after all per-service extraction is done.

  **Steps:**
  1. Gather extensibility signals from P1 flags + deep extraction: reads of a metadata/UD/config table,
     behavior gated on a parameter value, dynamic column/attribute handling.
  2. **Compile `spec/shared/extensibility-model.md`** with three parts:
     - **User-defined-field / metadata mechanism** — how instances add fields/attributes without code
       change (the metadata tables, how they're resolved at read/write time).
     - **Configuration / parameter surface** — the parameters/toggles/thresholds that alter behavior and
       where they are read.
     - **Resolution logic** — the CODE path that consumes metadata/config to produce instance-specific
       behavior. This is the engine; it is generated ONCE as a shared capability (see backend-service).
     - Enumerate each **extension point** with an `EXT-<DOM>-<NNN>` id, its `mechanism`
       (udf|metadata|config|parameter), and the BR-IDs that use it.
  3. **Annotate per-service BR-IDs:** for each rule parameterized by the engine, add
     `Extension Point: EXT-<DOM>-<NNN>` to its entry in `01-business-rules.md`. The rule's Logic must call
     the resolver, not hardcode the customized value.
  4. **Graph population (Tracker subagent):**
     - For each extension point: `graph_add_node(nodeType="ExtensionPoint", id="EXT-<DOM>-<NNN>", properties={name, service, mechanism, resolution})`
     - For each BR-ID using it: `graph_add_edge(edgeType="EXTENDS_VIA", sourceId=<brId>, sourceType="BusinessRule", targetId="EXT-<DOM>-<NNN>", targetType="ExtensionPoint")`
     - (Also written by `import_specs.py` when it parses the `Extension Point:` annotations — the script is
       the deterministic path; MCP calls are the fallback.)

  **Coverage rule:** every BR-ID whose behavior varies by instance configuration MUST name an extension
  point, and every extension point MUST be documented in `spec/shared/extensibility-model.md`. A rule that
  hardcodes what is actually a configurable value is a spec gap (the reimplementation would freeze one
  instance's behavior into the common code).

  **CAST/Hybrid mode:** CAST data-access patterns pinpoint which components read the metadata/UD/config
  tables flagged in P1 — target those components for source reading to extract the resolution logic. The
  engine itself (the resolver) is usually a small set of high-fan-in components (many rules call it); CAST
  fan-in metrics help locate it.

STAGE 2: Frontend Specification (once, after ALL backend specs done)
  Step 2.1: Frontend Discovery (from Phase 1 analysis + source scan)
    - Does the legacy system have a UI? What kind? (WebForms, WinForms, SPA, green-screen, etc.)
    - What technology? (ASP.NET, React, Angular, WPF, terminal, etc.)
    - What reusable assets exist? (icons, images, CSS/styles, form layouts, component libraries)
    - What navigation structure exists? (menus, screen flows, page hierarchy)

  Step 2.2: Human Decision on Asset Reuse
    🔴 PROMPT HUMAN: "The legacy system has a [type] frontend built with [technology].
       I found these reusable assets:
       - [N] icons/images in [path]
       - [CSS/style files] in [path]
       - [form layouts / component templates] in [path]
       
       Options:
       (a) Reuse existing design assets (icons, images, styles) — modernize the tech but keep the visual identity
       (b) Fresh design — new visual identity, only preserve screen structure and workflows
       (c) No frontend needed — backend APIs only for this engagement
       
       Which approach?"

  Step 2.3: API Access Pattern Decision (MANDATORY)
    🔴 PROMPT HUMAN: "The modernized system has [N] backend services. How should the frontend reach them?
       
       (a) **API Gateway** — single URL, gateway routes by path to backend services (recommended for production)
       (b) **BFF (Backend-for-Frontend)** — dedicated aggregation layer that calls backends and shapes data for the UI
       (c) **Direct** — frontend calls each service directly on different ports (dev only, not production-ready)
       
       This decision determines how the frontend API contract is written — specifically whether URLs like
       '/api/v1/products' map directly to catalog-service:3001/api/v1/catalog/products or go through a router.
       
       Which pattern?"
    
    After response, build the Gateway Routing Table that maps frontend paths → backend service paths.
    This table is the BRIDGE between frontend `01-api-contract.md` and backend `04-api-contract.yaml` files.
    Without it, the frontend will be implemented calling URLs that don't exist.

  Step 2.4: Generate Frontend Spec (per human decisions)
    - Read ALL backend 04-api-contract.yaml files
    - Read ALL backend 07-workflows.md files + spec/07-cross-service-workflows.md
    - Read saam-frontend-spec-template.md
    - Build the Gateway Routing Table (from Step 2.3 decision + backend contract paths)
    - **DERIVE user flows from backend workflows:** Every backend workflow with a user trigger
      (API call initiated by a human action) maps to a frontend user flow. Do NOT invent
      user flows independently — derive them from the authoritative backend workflow sequences.
    - If (a) from Step 2.2: use brownfield mode — preserve screen inventory, navigation, terminology, and reference asset paths
    - If (b) from Step 2.2: use greenfield mode — preserve workflows and data bindings but design fresh UI
    - If (c) from Step 2.2: skip Stage 2 entirely
    - Generate spec/frontend/<app-name>/ with all section files
    - **VERIFY:** All endpoints in generated `01-api-contract.md` use FRONTEND-FACING paths (from routing table left column), NOT the backend service paths directly
    - **VERIFY:** The Gateway Routing Table is included in `01-api-contract.md` so Phase 5 knows how to configure the gateway/BFF
    - **VERIFY:** Every user flow in `03-user-flows.md` references a backend workflow ID (WF-XX-NNN) and follows the same operation sequence

  Step 2.5: Contract Compatibility Check (MANDATORY — catches misalignments before implementation)
    This is the frontend↔backend twin of the Stage 1.5 consumer-provider reconciliation.
    For EVERY endpoint in the frontend's `01-api-contract.md`:
    1. Resolve the frontend-facing path through the Gateway Routing Table to the backend path,
       then look up that endpoint in the provider's `04-api-contract.yaml`. If the resolved
       endpoint does not exist in any backend contract → CONTRACT GAP.
    2. Compare ALL of:
       - Identifiers (slug vs UUID?)
       - Query params (same names? required vs optional match? — a required backend param the
         frontend never sends is a GAP that will 400/500 at runtime, e.g. missing companyId/tenantId)
       - Request body shape (fields, types, required flags)
       - Response fields (all fields the frontend reads are present in the backend response schema)
       - Field casing (camelCase vs snake_case) and nesting (flat vs `{items, total}`)
       - Auth/tenant headers (the frontend api-client MUST send exactly the headers the backend
         requires — token, x-tenant-id, x-company-id — per spec/shared/auth-config.md)
    3. Document findings in the "Contract Binding" section of `01-api-contract.md`
    4. For each mismatch, choose a resolution:
       - (a) Change frontend to match backend (rename param in API client)
       - (b) Change backend spec (add endpoint/field/param — requires updating `04-api-contract.yaml` + backend test suite)
       - (c) Add to gateway/BFF layer (transformation happens at the routing layer)
    5. If resolution = (b): UPDATE the backend `04-api-contract.yaml` and `03-api-design.md` NOW, before Phase 4 exit gate
    6. Present gap summary to human for approval

    🔴 PROMPT HUMAN: "Contract compatibility check found [N] mismatches between frontend needs and backend contracts:
       - [X] resolved by frontend mapping (param rename, field alias)
       - [Y] require backend spec updates (new endpoints/fields added to contracts)
       - [Z] handled by gateway/BFF layer
       
       Backend spec updates have been applied to [services]. Approve and proceed?"

  IF no UI discovered in Phase 1: skip Stage 2 entirely (note in exit gate)

EXIT GATE: Only after BOTH stages are complete
```

**Why this order matters:** Frontend specs reference backend API contracts for exact field names, endpoint paths, and response shapes. If a frontend spec is generated before all backend contracts are stable, it will reference outdated or missing field names — causing implementation failures in Phase 5.

## Mandatory Completion Verification (Before Exit Gate)

The agent MUST run this verification for EVERY service before presenting the exit gate. Do NOT skip this step.

```
STAGE 1 VERIFICATION — For each service in the service catalog:
  ✓ spec/microservices/<service>/01-business-rules.md exists and has ≥ minimum rule count
  ✓ spec/microservices/<service>/02-domain-model.md exists and has executable DDL
  ✓ spec/microservices/<service>/03-api-design.md exists and has endpoint definitions
  ✓ spec/microservices/<service>/04-api-contract.yaml exists and is valid OpenAPI 3.1
  ✓ spec/microservices/<service>/05-dependencies.md exists (generated in Stage 1.5 — states dependencies or explicitly says "no dependencies")
  ✓ spec/microservices/<service>/06-completion-summary.md exists and counts match reality
  ✓ Quality gates passed (template detection, semantic quality, independent validation)
  ✓ Complexity validation loop passed (all ratios ≤3 or unresolved flagged for Phase 4a)

CROSS-REFERENCE CONSISTENCY CHECK — For each service:
  ✓ Every schema type in 04-api-contract.yaml has a corresponding entry in 08-dtos/ (Requests, Responses, or Enums)
  ✓ Every enum referenced by a DTO exists in 08-dtos/Enums.*
  ✓ Every entity referenced by a DTO exists in 02-domain-model.md DDL
  ✓ No type name appears in both domain entities and DTOs without a disambiguation rule
  ✓ spec/shared/09-dependency-versions.* exists and lists all shared framework packages with pinned versions

CROSS-SERVICE CONTRACT RECONCILIATION — Across all services:
  ✓ Every synchronous cross-service call in 05-dependencies.md targets an endpoint that EXISTS in the provider's 04-api-contract.yaml (exact path + method)
  ✓ The response shape each consumer expects matches the provider's published response schema (field names, types, list vs single)
  ✓ The request shape each consumer sends satisfies the provider's required parameters and body schema
  ✓ spec/shared/cross-service-contracts.md exists with reconciliation status for every sync call (no GAP entries remain)
  ✓ Shared-convention reconciliation complete: every service 04-api-contract.yaml CONFORMS to the shared
    conventions (company/tenant param name, pagination params, list envelope, error shape, auth headers) —
    references spec/shared/common-schemas.yaml, does not redefine. assessment/shared-convention-reconciliation.md
    has NO concern still at GAP (all OK or human-reconciled; "keep" divergences annotated with rationale)

STAGE 2 VERIFICATION — Frontend (if applicable):
  ✓ Was a UI discovered during Phase 1 source analysis?
  ✓ If YES: human was prompted about asset reuse approach (brownfield/greenfield/skip)
  ✓ If approach = (a) or (b): spec/frontend/<app-name>/ exists with all required section files
  ✓ If approach = (a) or (b): frontend spec references 04-api-contract.yaml from backend services
  ✓ If approach = (c) or no UI discovered: explicitly noted as "no frontend — skipped" with rationale

FRONTEND-BACKEND CONTRACT RECONCILIATION — If a frontend exists:
  ✓ Every frontend api-client endpoint resolves (via the Gateway Routing Table) to an endpoint that EXISTS in a backend 04-api-contract.yaml
  ✓ Every required backend query param / body field is supplied by the frontend api-client (no missing companyId/tenantId-class params)
  ✓ Response field names + casing + nesting the frontend reads match the backend response schema
  ✓ Auth/tenant headers the api-client sends match spec/shared/auth-config.md and backend expectations
  ✓ Contract Compatibility Check (Step 2.5) completed with zero unresolved GAPs (resolved via frontend mapping, backend spec update, or gateway/BFF layer)
```

**If ANY artifact is missing for ANY service:** Generate it NOW before proceeding. Phase 4 cannot be declared complete with partial specifications.

## Telemetry Production (MANDATORY)

After all services are specified and the semantic preservation validation has completed, the agent MUST produce `.saam/telemetry/phase4-specs.yaml` by querying the engagement graph and the preservation reports.

**Data to compute:**

1. **Timing** — infer from task tracker (`tracking/phase4-specs.md`): first task `in_progress` → last task `completed`
2. **Spec metrics** — total services, total BRs, avg BRs per service, contracts generated
3. **Complexity/preservation metrics** — from the semantic preservation validation results:
   - Components analyzed, flagged, critical, resolved, unresolved
   - Per-dimension flag counts (flagged, flagged_alone, resolved, true_positive)
   - Control-flow-alone correctness count
   - Threshold sensitivity: count how many components WOULD be flagged at ratio thresholds 2, 4, and 5 (in addition to the current threshold)
4. **Passes needed** — average re-extraction passes across all flagged components

**Schema:** See `saam-telemetry.md` → `phase4-specs.yaml` for the full YAML structure.

**Rules:**
- `true_positive_count` per dimension can only be determined after Phase 4a (BA review) or Phase 5 (implementation failures). Initially set to 0 and mark `true_positives_pending: true`. Phase 4a telemetry will retroactively update this file.
- Record `flags_at_threshold_2`, `flags_at_threshold_4`, `flags_at_threshold_5` to enable future threshold calibration without re-running analysis.

## Exit Gate

**PRECONDITION: The agent MUST produce `.saam/telemetry/phase4-specs.yaml` BEFORE presenting the exit gate.** If the file does not exist, create it now per the Telemetry Production section above.

**PhaseEvent (completed):** Write: `graph_add_node(nodeType="PhaseEvent", id="P4-completed", properties={phase: "P4", event: "completed", timestamp: <current ISO timestamp>})`.

**🔴 PROMPT HUMAN**: "Phase 4 specification generation complete for [N] services:

| Service | Rules | Tables | Endpoints | API Contract |
|---------|-------|--------|-----------|--------------|
| <service-1> | X | Y | Z | ✅ |
| <service-2> | X | Y | Z | ✅ |
| ... | | | | |

All [N] services have complete spec packages (business rules + domain model + API design + API contract + completion summary). Frontend spec: [✅ generated at spec/frontend/<app>/ | N/A — no UI]. Ready for Phase 4a."

**Next step (mandatory):** Proceed to Phase 4a (Business Rule Validation) — activate `saam-phase4a-business-rule-validation.md` and read `saam-ba-review-template.md`.

**BUT FIRST — Deep Convergence Re-Check (MANDATORY before P4a):**

Phase 4 extraction typically discovers 2-3x more rules than Phase 1 (new rules from deep extraction, greenfield opportunities, integration patterns). The Phase 3 convergence that ran after P1/P2 was based on an incomplete rule set. A re-check is MANDATORY now to catch:

- Rules assigned to wrong services during deep extraction (new context changed the picture)
- New services that should exist (patterns only visible after deep extraction)
- Boundary violations introduced by rule growth
- Cross-service dependencies not visible at P1 surface level
- Data ownership conflicts from expanded domain models

**Deep Convergence Protocol (runs in-session, no separate phase):**

1. **Rule-to-service assignment validation:**
   - Query graph: `MATCH (br:BusinessRule)-[:ASSIGNED_TO]->(s:Service) RETURN s.name, count(br) ORDER BY count(br) DESC`
   - Compare against `spec/microservices/*/01-business-rules.md` actual content
   - Flag any BR-ID in a spec file that isn't ASSIGNED_TO the corresponding service in the graph

2. **Boundary violation check:**
   - For each service pair: check if >3 rules reference each other's domain tables
   - If found: flag for human review ("Should these rules be reassigned?")

3. **Orphan rule detection + node creation (CRITICAL — this is where P4 rules enter the graph):**
   - Parse ALL BR-IDs from `spec/microservices/*/01-business-rules.md` files
   - Query graph: `MATCH (br:BusinessRule) RETURN br.brId`
   - For each BR-ID in specs that does NOT exist as a graph node:
     - **CREATE the BusinessRule node:** `graph_add_node(nodeType="BusinessRule", id=<brId>, properties={statement: <from spec>, intent: <from spec>, service: <service-name>, lifecycleState: "Assigned", source: "phase4_extraction"})`
     - **CREATE the ASSIGNED_TO edge:** `graph_add_edge(edgeType="ASSIGNED_TO", sourceId=<brId>, sourceType="BusinessRule", targetId=<serviceId>, targetType="Service")`
   - Any BR-ID in graph without a spec → flag as "Phase 1 rule not carried to Phase 4 (dropped or merged)"
   - **Expected outcome:** After this step, graph BR-ID count MUST equal spec BR-ID count. If not, the step failed — repeat.
   - **Why this is critical:** P4 subagents write rules to markdown but cannot call graph tools. The orchestrator MUST import them here. Without this, the graph stays at the P1 count while specs have 2-3x more rules (observed in both N=1 and N=2 engagements). All downstream phases (P4a weighting, P5 implementation tracking, P6 reconciliation) query the graph — if rules aren't there, they're invisible to governance.

4. **Service completeness:**
   - Verify every service in `modernization/services-composition.md` has a spec directory
   - If a service has 0 rules → flag for human review ("Is this service still needed?")

5. **Update graph (MANDATORY — not optional):**
   - Fix all assignment mismatches found in steps 1-4 (create missing ASSIGNED_TO edges, remove incorrect ones)
   - Ensure total BR-ID count in graph matches total across all spec files
   - Update service node properties: `ruleCount`, `tableCount`, `endpointCount` from specs
   - Run `graph_run_inferences(rules=["lifecycle_states", "effective_confidence"])`
   - Log summary: "Deep convergence: X mismatches fixed, Y orphans resolved, Z services validated"

6. **Graph enrichment — vectors + provenance (MANDATORY):**

   After step 5 ensures all BR nodes exist, enrich them with vectors and provenance edges. The data comes from two spec files per service:
   - `extraction-evidence.md` → source vectors per component
   - `01-business-rules.md` → per-BR Semantic Preservation tables + Source References

   **6a. Source vectors on SourceComponent nodes:**
   For each service's `extraction-evidence.md`:
   - Parse the file/component list with their counted vectors
   - For each component: `graph_update_node(nodeType="SourceComponent", id=<name>, properties={srcControlFlow: N, srcDataFlow: N, srcConstants: N, srcStateTransitions: N, srcOutcomes: N, srcDataWrites: N, srcIntegrations: N, srcErrorPaths: N})`
   - If SourceComponent node doesn't exist: create it first
   - **Stamp `p4Intent` (snapshot 3):** for each component the deep read actually TOUCHED, set its intent as confirmed/corrected by the source read — `graph_update_node(nodeType="SourceComponent", id=<name>, properties={p4Intent: <post|entry|validate|derive|distribute|util|report>})` and update `intentCategory` to match. Stamp it whether the read CONFIRMS the first pass (`p4Intent = firstPassIntent`) or CORRECTS it — a confirmation is a meaningful signal, not silence. Do NOT stamp `p4Intent` on components P4 never reads (the un-extracted majority) — they keep `firstPassIntent` as their last word. The delta `firstPassIntent → p4Intent` is the second-order fidelity signal (how often even behavior+name missed and a human source read corrected it) — a P4 telemetry number.

   **6b. Per-BR spec vectors (from Semantic Preservation tables):**
   For each BR-ID in `01-business-rules.md`:
   - Parse the `Semantic Preservation` table (if present):
     ```
     | Dimension | Source | Spec | Status |
     | Control-flow | 5 | 4 | OK |
     ```
   - Set on the BR node: `graph_update_node(nodeType="BusinessRule", id=<brId>, properties={srcControlFlow: <Source col>, srcDataFlow: ..., specControlFlow: <Spec col>, specDataFlow: ..., preservationStatus: "<OK|FLAGGED|CRITICAL>"})`
   - If table is missing (greenfield rule, or subagent skipped): set `vectorsComputed: false` on the BR node — this flags it for later attention

   **6c. EXTRACTED_FROM provenance edges:**
   For each BR-ID in `01-business-rules.md`:
   - Parse the `Source Reference: <file>:<function>:<lines>` field
   - Resolve `<file>` to a SourceComponent node (match by file path or name)
   - Create edge: `graph_add_edge(edgeType="EXTRACTED_FROM", sourceId=<brId>, sourceType="BusinessRule", targetId=<componentId>, targetType="SourceComponent")`
   - If source reference is missing or "N/A" (greenfield rule): skip edge, log it

   **Verification after step 6:**
   - Count BR nodes with non-null `specControlFlow`: should be ≥80% of total (some greenfield rules won't have vectors)
   - Count EXTRACTED_FROM edges: should match count of non-greenfield BR-IDs
   - If <50% of rules have vectors: the extraction subagents likely skipped the Semantic Preservation table — flag for human attention

7. **Update tracking file:**
   - Append to `tracking/phase4-spec-generation.md`: "Deep Convergence complete: [timestamp], [N] mismatches fixed, [M] services validated, [K] BR vectors imported, [J] provenance edges created"
   - If Jira configured: update Phase 4 Epic with convergence subtask (DONE)

8. **Commit checkpoint:**
   - `git add spec/ tracking/ && git commit -m "chore(phase4): deep convergence re-check — N mismatches fixed, K vectors imported"`
   - This creates a verifiable timestamp for telemetry (git commit time = convergence completion time)

**Duration:** 2-5 minutes (graph queries + spot checks). NOT a full re-extraction — just a validation pass.

**If issues found:** Present to human for resolution before P4a. P4a classifies and weights rules — if rules are in the wrong service, the weights will be applied incorrectly.

---

Phase 4a will classify, weight, and validate all extracted rules. The human chooses between approving agent-recommended defaults (fast path, ~5 min) or providing full BA workshop outputs (thorough path, 1-5 days).

**README update:** Update the root `README.md` — add Phase 4 completion summary: services specified, total rules per service, API contracts generated, frontend specs (if applicable).
