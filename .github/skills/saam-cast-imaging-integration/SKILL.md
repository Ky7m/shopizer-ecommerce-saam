---
name: saam-cast-imaging-integration
description: "Procedures and MCP tool integration for extracting application architecture and dependencies using CAST Imaging."
copyright: "Copyright 2024-2026 SoftServe Inc. All rights reserved."
authors: "Max Kozinenko, Roman Kalita (SoftServe)"
---

# SAAM: CAST Imaging MCP Integration

## Activation Rule (MANDATORY)

**If the engagement's analysis mode is CAST or Hybrid (recorded in `inventory/INDEX.md`), this file MUST be read and followed for ALL extraction phases (P1, P3, P4).** It is not optional guidance — it defines the orchestrator's data gathering workflow that replaces or supplements direct source reading.

The agent MUST NOT skip CAST queries when CAST is configured. Direct source reading without first querying CAST wastes context on files that may be dead code, infrastructure, or low-complexity utilities. CAST tells you WHERE the business logic lives — then you read ONLY those files.

---

## When to Use CAST Imaging

| Criterion | Use CAST | Use Direct Source |
|-----------|----------|-------------------|
| LOC > 150K | Required | Not feasible |
| LOC 50K-150K | Preferred | Acceptable (with chunking) |
| LOC < 50K | Optional | Preferred |
| Complex dependencies (50+ modules) | Required | Risky (miss relationships) |
| Multi-language system | Required | Difficult to navigate |
| Unknown application structure | Required | Where do you start? |
| Need dead code exclusion | Required | Can't detect without execution analysis |

---

## CAST Imaging MCP Capabilities (Full Catalog)

| Capability | MCP Tool | Use In Phase | What It Returns |
|-----------|----------|--------------|-----------------|
| Application inventory | `mcp_imaging_applications` | P0 | All analyzed applications, LOC, technology breakdown |
| Component catalog | `mcp_imaging_objects` | P1, P4 | Modules, programs, classes filtered by type/name/complexity |
| Transaction paths | `mcp_imaging_transactions` | P1, P4 | Entry-to-data execution paths (full business flows) |
| Transaction detail | `mcp_imaging_transaction_details` | P4 | Call graph nodes + links for a specific transaction |
| Call graphs | `mcp_imaging_object_details` (focus: outward/inward) | P1, P3, P4 | Who calls whom, callers/callees per object |
| Data access | `mcp_imaging_data_graphs` | P1, P3, P4 | Which components access which tables (CRUD matrix) |
| Source files | `mcp_imaging_source_files` | P4 | File paths matching a pattern (locate source) |
| Code snippets | `mcp_imaging_object_details` (focus: code) | P4 | Source code of specific methods/procedures |
| Complexity metrics | `mcp_imaging_objects` (sorted by complexity) | P1, P4 | Cyclomatic complexity, LOC, fan-in/fan-out |
| Dead code | `mcp_imaging_objects` (filter: unreachable) | P1, P3 | Components with zero callers (candidates for exclusion) |

---

## Phase-Specific CAST Usage

### Phase 0: Discovery

```
1. mcp_imaging_applications() → list all analyzed apps
2. For target app: get technology breakdown, LOC, module count
3. Record in inventory/INDEX.md: total LOC, language mix, module structure
4. Use module boundaries to propose segmentation strategy
```

### Phase 1: Bottom-Up (Structure + Surface Rules)

CAST provides the STRUCTURE. Direct source provides the SEMANTICS.

```
For each segment:
  1. Query transactions in this domain → identifies entry points
  2. Query complexity-sorted objects → identifies WHERE business logic concentrates
  3. Query data access for domain tables → identifies WHAT data each component touches
  4. For high-complexity objects (cyclomatic > 15): read source directly for BR extraction
  5. For low-complexity objects: record existence but don't deep-read (likely CRUD/infrastructure)
  6. Record source vectors from CAST complexity metric (srcControlFlow = cyclomatic complexity)
  7. Document segment findings → move to next
```

**Key Phase 1 CAST outputs:**
- SourceComponent nodes with `castId`, `complexity`, `srcControlFlow`
- Call graph edges (SOURCE_CALLS)
- Data access patterns (SOURCE_ACCESSES)
- Dead code identification (exclude from scope)

### Phase 3: Convergence (Dependency Validation)

```
1. Query cross-module dependencies → validates service boundaries
2. Query shared table access → identifies data ownership conflicts
3. Query dead code → exclude from convergence (not mapped to any service)
4. For boundary violations: query full call path between conflicting components
```

### Phase 4: Deep Extraction (THE CRITICAL USAGE)

This is where CAST delivers the most value. The orchestrator uses CAST to build a **complete service brief** before delegating extraction to a subagent. The subagent receives pre-filtered, targeted context — not the entire codebase.

---

## Phase 4: CAST-Guided Extraction Workflow (MANDATORY for CAST/Hybrid Mode)

### Orchestrator Role (Main Agent)

The orchestrator does NOT read source files directly. It:
1. Queries CAST to understand the service's component landscape
2. Identifies which source files contain business logic (vs infrastructure)
3. Assembles a "service brief" with file paths and context
4. Delegates to a subagent with the brief + steering files
5. Validates the subagent's output against quality gates

### Step 1: Query CAST for Service Components

For each service (in provider-first order):

```
# 1a. Get all transactions (entry points) for this domain
mcp_imaging_transactions(application="<app>", filters="name:contains:<domain_prefix>")

# 1b. For critical transactions, get full call graph
mcp_imaging_transaction_details(application="<app>", transaction_id=<id>, focus="nodes", full_call_graph=True)
mcp_imaging_transaction_details(application="<app>", transaction_id=<id>, focus="links", full_call_graph=True)

# 1c. Get data access patterns for domain tables
mcp_imaging_data_graphs(application="<app>", filters="name:contains:<table_prefix>")

# 1d. Get complexity-sorted objects (highest first — most likely to contain business rules)
mcp_imaging_objects(application="<app>", filters="name:startswith:<prefix>,type:contains:Procedure")

# 1e. Get callers/callees for the most complex objects (identify shared logic)
mcp_imaging_object_details(application="<app>", filters="id:eq:<id>", focus="outward")
```

### Step 2: Resolve Source File Paths

CAST returns paths with a root prefix specific to the CAST analysis environment. Translate to local workspace:

```
CAST path format:  §{ROOT_IDENTIFIER}§/path/to/file.ext
Local path format: initial-source/path/to/file.ext

Translation: Replace the §{...}§ prefix with 'initial-source/'
```

**Project-specific root mapping:** During Phase 0, document the CAST root prefix in `inventory/INDEX.md`:
```markdown
## CAST Path Mapping
| CAST Root | Local Root |
|-----------|-----------|
| §{APP_ROOT}§/ | initial-source/ |
```

The orchestrator uses this mapping every time it translates CAST paths to local file paths for the subagent.

### Step 3: Classify Components (Business Logic vs Infrastructure)

From the CAST data, classify each component:

| Indicator | Likely Business Logic | Likely Infrastructure |
|-----------|----------------------|----------------------|
| Cyclomatic complexity > 15 | YES — deep-read required | |
| Contains IF/CASE with domain conditions | YES | |
| Writes to domain tables (not audit/log) | YES | |
| Called by 1-2 entry points | YES (specific business flow) | |
| Cyclomatic complexity < 5 | | YES — skip or skim |
| Called by > 10 components | | YES — utility/shared library |
| Only reads/writes audit tables | | YES — logging infrastructure |
| Name contains "Log", "Audit", "Util", "Helper" | | YES |
| Fan-in > 10 | | YES — framework/utility |

**This classification determines which files the subagent reads:**
- Business logic files → included in subagent's source file list (MUST read)
- Infrastructure files → excluded (or listed as "context only — don't extract BRs")

### Step 4: Assemble Service Brief

Produce a structured brief for the subagent:

```markdown
## Service Brief: <service_name> (<service_id>)

### Entry Points (from CAST transactions)
| Transaction | Size (objects) | End Points | Complexity |
|-------------|---------------|------------|------------|
| <name> | <N> | <table writes> | <sum of cyclomatic> |

### Source Files to Read (business logic — MUST extract BRs from these)
| File | Local Path | Complexity | Reason for Inclusion |
|------|-----------|------------|---------------------|
| <proc_name> | initial-source/<path> | <N> | Highest complexity in domain |
| <class_name> | initial-source/<path> | <N> | Writes to <domain_table> |

### Source Files — Context Only (infrastructure — DON'T extract BRs)
| File | Local Path | Reason for Exclusion |
|------|-----------|---------------------|
| <util_name> | initial-source/<path> | Fan-in > 10, utility |
| <log_name> | initial-source/<path> | Audit logging only |

### Tables Owned by This Service
| Table | Columns | CAST ID |
|-------|---------|---------|

### Cross-Service Dependencies (from CAST call graph)
| Calls TO | Via Component | Reason |
|----------|--------------|--------|
| <other_service> | <shared_proc> | Reads <table> owned by <other> |

### Existing P1 Rules (to re-extract at P4 depth)
| BR-ID | Original Source | Needs Upgrade |
|-------|----------------|---------------|
| <br_id> | <file:proc:lines> | YES — missing examples, vectors |

### Dead Code (excluded from scope)
| Component | Reason | CAST ID |
|-----------|--------|---------|
| <name> | Zero callers, unreachable | <id> |
```

### Step 5: Delegate to Subagent

```python
invoke_sub_agent(
    name="general-task-execution",
    contextFiles=[
        {path: ".github/skills/saam-phase4-spec-generation/SKILL.md"},
        {path: ".github/skills/saam-spec-template/SKILL.md"},
        {path: ".github/skills/saam-source-reading-<stack>/SKILL.md"},
        {path: ".github/skills/saam-api-contract/SKILL.md"},
    ],
    prompt="""
    Extract business rules for <service_name> (<service_id>).

    ## Service Brief (from CAST analysis)
    <paste assembled service brief here>

    ## Instructions
    1. Read ONLY the files listed in "Source Files to Read" (business logic)
    2. DO NOT read files in "Context Only" section unless a business-logic file references them
    3. Re-extract ALL "Existing P1 Rules" at full P4 depth
    4. For each BR-ID: produce Statement, Logic, Examples, Vectors, Preservation table
    5. Every rule MUST have the Semantic Preservation table (Source + Spec columns)
    6. PRODUCE EXACTLY: 01-business-rules.md, 02-domain-model.md, 03-api-design.md,
       04-api-contract.yaml, 06-completion-summary.md, extraction-evidence.md
    
    QUALITY REQUIREMENTS (from .github/skills/saam-phase4-spec-generation/SKILL.md):
    - Every rule has Semantic Statement (business meaning, not just pseudocode)
    - Every rule has Concrete Examples (success + error, real domain fields)
    - Every rule has Semantic Preservation table (8 dimensions, Source + Spec columns)
    - Every rule has Source Reference (file:procedure:lines)
    """
)
```

### Step 6: Validate Output

After subagent returns, the orchestrator validates:

```
□ All 6 files exist with correct names
□ BR-ID count matches expectation (from CAST complexity → expected yield)
□ EVERY rule has Semantic Preservation table (grep count matches BR count)
□ EVERY rule has Concrete Examples section
□ EVERY rule has a Source Reference pointing to a file in the service brief
□ extraction-evidence.md lists files from the service brief (confirms subagent read them)
□ No rules extracted from "Context Only" files (subagent shouldn't have gone there)
```

**If validation fails:** Re-delegate with specific instructions ("Rules 5, 12, 18 are missing examples — read <file> lines <N-M> and add examples").

### Step 7: Graph Update + Commit

```
□ Create/update BR nodes in graph (with vectors from preservation tables)
□ Create EXTRACTED_FROM edges (BR → SourceComponent via CAST ID)
□ Create ASSIGNED_TO edges (BR → Service)
□ Update tracking file
□ Git commit
□ Move to next service
```

---

## CAST-Specific Advantages Over Direct Source

| Scenario | Without CAST | With CAST |
|----------|-------------|-----------|
| "Which files should I read for billing rules?" | Guess from directory structure, read many wrong files | Query transactions + data access → exact file list |
| "Is this utility or business logic?" | Read it and decide (burns context) | Check fan-in/complexity → classify before reading |
| "Did I miss any business rules?" | Compare to P1 count (weak signal) | Compare reviewed components vs total CAST components (quantitative) |
| "Is this code dead?" | Can't know without runtime data | CAST flags zero-caller components |
| "What calls this shared procedure?" | Read every file looking for calls | Query CAST callers in 1 second |
| "What tables does this service own?" | Grep source for table names | Query CAST data access graph (complete CRUD matrix) |

---

## Extraction Coverage Validation (Post-P4, CAST-Specific)

After ALL services are extracted, validate that extraction covered the business-logic codebase:

```
1. Query CAST: total components with cyclomatic complexity > 15 in the application
2. Compare against: sum of all components listed in extraction-evidence.md across all services
3. Coverage = reviewed / total

If coverage < 80%: investigate which high-complexity components were NOT assigned to any service.
These may be:
- Dead code (CAST flags as unreachable — acceptable to skip)
- Shared libraries (infrastructure — acceptable to skip)
- Cross-cutting concerns assigned to no specific service (NEED to be assigned)
- Missed domains (a service was missed in Phase 2 architecture)
```

This is the QUANTITATIVE validation that CAST enables — "we reviewed 847 out of 923 business-logic components (92% coverage)." Without CAST, you can only validate against P1 rule counts (qualitative).

---

## Context Pressure Management (CAST-Specific)

CAST's primary value for context management:

1. **Pre-filtering:** Only read files that CAST identifies as business-relevant (skip 60-80% of the codebase)
2. **Complexity-sorted reading:** Read the highest-complexity files first (most rules per context-token spent)
3. **Targeted deep-reads:** When subagent needs more context on a reference, query CAST for the specific call chain instead of reading the entire dependency tree
4. **Dead code exclusion:** Don't waste context reading code that isn't reachable in production
5. **Batch by transaction:** Each transaction is a complete business flow — extract all rules in that flow together (coherent context, not fragmented)

**Rule of thumb for subagent delegation:**
- A service with < 30 CAST-identified business-logic files → Mode 1 (full service in one shot)
- A service with 30-60 business-logic files → Mode 2 (per-layer or per-transaction batch)
- A service with > 60 business-logic files → Mode 3 (per-transaction batched, max 15-20 files per delegation)

---

## Recording CAST References (Traceability)

Every BR-ID extracted with CAST assistance MUST include:

```markdown
**Discovery Method:** Hybrid (CAST transaction path + Direct Source Read)
**CAST Reference:** Transaction: <name> | Object: <object_name> | CAST ID: <id> | Complexity: <N>
```

This enables:
- Traceability from BR → CAST artifact → source file → production code
- Validation that high-complexity objects produced proportional rule counts
- Future reconciliation against CAST updates (if the application is re-analyzed)

---

## MCP Tool Reference (Quick Lookup)

| Task | Tool | Filters |
|------|------|---------|
| List apps | `mcp_imaging_applications` | — |
| Find procedures by name | `mcp_imaging_objects` | `name:startswith:<prefix>,type:contains:Procedure` |
| Find tables by name | `mcp_imaging_objects` | `name:startswith:<prefix>,type:contains:Table` |
| Get transactions | `mcp_imaging_transactions` | `name:contains:<domain>` |
| Transaction call graph (nodes) | `mcp_imaging_transaction_details` | `focus: "nodes", full_call_graph: True` |
| Transaction call graph (links) | `mcp_imaging_transaction_details` | `focus: "links", full_call_graph: True` |
| Object callers/callees | `mcp_imaging_object_details` | `focus: "outward"` or `focus: "inward"` |
| Object source code | `mcp_imaging_object_details` | `focus: "code"` |
| Data access patterns | `mcp_imaging_data_graphs` | `name:contains:<table_name>` |
| Find source files | `mcp_imaging_source_files` | `file_path: "<partial_path>"` |
| Sort by complexity | `mcp_imaging_objects` | `sort: "cyclomaticComplexity", order: "desc"` |
