---
name: saam-phase4a-business-rule-validation
description: "Business rule classification, complexity weighting, optimization, and obsolescence screening."
copyright: "Copyright 2024-2026 SoftServe Inc. All rights reserved."
authors: "Max Kozinenko, Roman Kalita (SoftServe)"
---

# SAAM Phase 4a: Business Rule Validation (BA Review)

## Objective

Classify, weight, and validate extracted business rules BEFORE implementation investment (test suites, automatibility scoring, code generation). This phase produces an amended spec set with explicit decisions on what to keep, simplify, defer, or drop — and assigns business impact weights that drive Phase 4b priority.

**This phase is MANDATORY.** It always runs after Phase 4 spec generation. The human chooses between two completion modes: (A) approve agent-recommended defaults with minimal effort, or (B) provide actual BA/domain expert review with full workshop outputs.

## Required Steering Files (Read Before Proceeding)

The agent MUST read the following steering files before executing Phase 4a:

1. **`.github/skills/saam-ba-review-template/SKILL.md`** — The document template the agent generates for the BA (defines sections, format, parse-back instructions)
2. **`.github/skills/saam-human-guidance-protocol/SKILL.md`** — Prompt categories, decision register format, agent rules
3. **`.github/skills/saam-task-tracking/SKILL.md`** — Tracking file format and Jira dual-write protocol

The BA review template is CRITICAL — it defines the exact document structure the BA will work through. Do NOT generate BA review documents without first reading the template.

## Task Tracking Activation

**PRECONDITION: The agent MUST NOT begin BA review work until `tracking/phase4a-ba-review.md` exists.** If it doesn't exist, create it NOW with tasks per service (review doc generated, review completed, decisions parsed back, decision register updated) all listed as PENDING.

**PhaseEvent (telemetry timestamp):** Immediately after creating the tracking file, write: `graph_add_node(nodeType="PhaseEvent", id="P4A-started", properties={phase: "P4A", event: "started", timestamp: <current ISO timestamp>})`.

After each service's BA review is generated (or defaults approved), the agent MUST update the tracking file immediately (mark tasks DONE) BEFORE moving to the next service. If Jira is configured, create an Epic with Tasks. See `.github/skills/saam-task-tracking/SKILL.md` for format.

## Subagent Delegation (BA Review Document Generation)

When delegating BA review document generation to a subagent:

**contextFiles to include:**
- `.github/skills/saam-phase4a-business-rule-validation/SKILL.md`
- `.github/skills/saam-ba-review-template/SKILL.md`

**Delegation prompt template:**
```
Generate the BA review document for service <service-name>.

READ THESE FILES FIRST (included in your context):
- .github/skills/saam-phase4a-business-rule-validation/SKILL.md (review protocol)
- .github/skills/saam-ba-review-template/SKILL.md (EXACT document format)

INPUT: spec/microservices/<service>/01-business-rules.md

PRODUCE: assessment/ba-review-<service>.md

The document MUST follow the EXACT structure from .github/skills/saam-ba-review-template/SKILL.md:
- Section 1: Rule inventory table (all BR-IDs with pre-classification)
- Section 2: Per-rule review cards (Statement, Agent Classification, Agent Rationale, BA fields to fill)
- Section 3: Scope reduction summary
- Section 4: Parse-back instructions for the BA

NEVER invent your own document format. Follow the template exactly.
```

**Parent verification after subagent returns:**
- [ ] `assessment/ba-review-<service>.md` exists
- [ ] Document has all 4 sections from the template
- [ ] Every BR-ID from 01-business-rules.md appears in the review
- [ ] Pre-classifications are assigned (Core/Active/Simplify/Obsolete candidates)

## Completion Modes

Phase 4a supports two completion modes. The agent MUST present both options to the human at the start of Phase 4a:

**🔴 PROMPT HUMAN:**

"Phase 4a (Business Rule Validation) is ready. I've pre-classified and weighted all [N] extracted rules based on heuristics. You have two options:

**Mode A: Approve Agent Defaults (5 min)**
- I've pre-classified all rules (Core/Active/Obsolete candidates) and assigned default weights
- You review the summary, approve the defaults, and we move on
- Best when: no BA available, time-constrained, or rules are straightforward

**Mode B: Full BA Workshop (1-5 days)**
- I generate detailed review documents for a Business Analyst
- The BA works through each rule: validates, reclassifies, assigns weights, flags obsolete logic
- You return the completed documents and I parse the decisions back into specs
- Best when: BA is available, system has known technical debt, or scope reduction is a goal

Which mode?"

### Mode A: Approve Agent Defaults

The agent applies its pre-classification heuristics (same heuristics documented in Step 1 below) and presents a summary:

1. **Generate classifications** using the pre-classification heuristics (Section "Pre-Classification Heuristics")
2. **Assign default weights:**
   - Rules flagged as Core → weight: CRITICAL or HIGH
   - Rules with regulatory/compliance references → weight: CRITICAL
   - Standard business logic → weight: MEDIUM
   - Simple validations and format checks → weight: LOW
3. **Present summary to human:**

```
Agent-Recommended Defaults:
- Core (Critical/High weight): N rules
- Active (Medium weight): N rules
- Active (Low weight): N rules
- Obsolete candidates: N rules (flagged, NOT dropped — need your approval)
- Optimization candidates: N rules

Obsolete candidates (require your explicit approval to drop):
  - BR-XX-001: <reason — references retired system X>
  - BR-XX-014: <reason — date-bounded logic expired 2019>
  ...
```

4. **🔴 PROMPT HUMAN**: "Here are the agent-recommended defaults. Options:
   - (a) **Approve all** — classifications and weights applied as shown, obsolete candidates dropped
   - (b) **Approve with exceptions** — tell me which specific rules to reclassify or keep
   - (c) **Switch to Mode B** — generate full BA review documents instead"

5. **After human response — Reconcile to Specs (MANDATORY):**
   - **(a) Approve all:** Apply all classifications and weights to `01-business-rules.md` per service. Move obsolete candidates to `07-obsolete-rules-appendix.md`. Generate `assessment/ba-decision-register.md`. Produce scope reduction report.
   - **(b) Approve with exceptions:** Apply defaults, then override the specific rules the human listed. Same reconciliation as (a) but with human overrides incorporated.
   - **(c) Switch to Mode B:** Discard defaults, proceed to Mode B workflow below.
   
   **Reconciliation actions (same as Step 3.1 for Mode B):**
   - Add `**Weight:** Critical|High|Medium|Low` to every BR-ID in `01-business-rules.md`
   - Move dropped rules to `spec/microservices/<service>/07-obsolete-rules-appendix.md`
   - Move deferred rules to `spec/microservices/<service>/06-deferred-rules.md`
   - Update `06-completion-summary.md` with new rule counts (post-removal)
   - Generate `assessment/ba-decision-register.md` with all decisions
   - Report scope reduction: original count → final count → % reduction
   - **Update graph (MANDATORY):**
     - For each dropped/obsolete rule: `graph_update_node(nodeType="BusinessRule", id=<brId>, properties={lifecycleState: "Obsolete", classification: "Obsolete", droppedReason: "<reason>", droppedAt: <timestamp>})`
     - For each active rule: `graph_update_node(nodeType="BusinessRule", id=<brId>, properties={classification: "<Core|Active>", weight: "<Critical|High|Medium|Low>"})`
     - For each deferred rule: `graph_update_node(nodeType="BusinessRule", id=<brId>, properties={lifecycleState: "Deferred", classification: "Deferred", deferredReason: "<reason>"})`
     - **For EVERY rule (create Decision node + edge):**
       → `graph_add_node(nodeType="Decision", id=<brId>, properties={classification: "<Core|Active|Obsolete|Deferred>", weight: "<Critical|High|Medium|Low>", rationale: "<reason>", decidedBy: "<BA name or 'agent-defaults'>", mode: "<ModeA|ModeB>"})`
       → `graph_add_edge(edgeType="DECIDED_AS", sourceId=<brId>, sourceType="Decision", targetId=<brId>, targetType="BusinessRule")`
     - Run `graph_run_inferences(rules=["lifecycle_states", "effective_confidence"])` after all updates
   - This ensures the graph reflects the BA decisions — downstream phases (P4b automatibility, P5 implementation) query the graph for active rules only. Without this update, obsolete rules appear in implementation scope.

### Mode B: Full BA Workshop

Follow the full workflow documented below (Steps 1-3): generate BA review documents, BA completes them, agent parses decisions back.

## Workflow Overview (Mode B: Full BA Workshop)

```
Phase 4 Complete (specs exist)
        │
        ▼
┌─────────────────────────────────────────┐
│  AGENT: Generate BA Review Document     │
│  (one document per service or domain)   │
└─────────────────────────────────────────┘
        │
        ▼
┌─────────────────────────────────────────┐
│  BA: Works through the review document  │
│  - Validates accuracy                   │
│  - Classifies each rule                 │
│  - Marks obsolete candidates            │
│  - Proposes simplifications             │
│  - Weights by business impact           │
└─────────────────────────────────────────┘
        │
        ▼
┌─────────────────────────────────────────┐
│  AGENT: Parses completed review doc     │
│  - Updates service specs                │
│  - Moves dropped rules to appendix      │
│  - Applies simplifications              │
│  - Records decision register            │
│  - Reports scope reduction metrics      │
└─────────────────────────────────────────┘
        │
        ▼
    Phase 4b (Automatibility Scoring)
```

## Step 1: Generate BA Review Document (Mode B)

The agent produces ONE review document per service (or per domain if services are small). The document uses the template from `.github/skills/saam-ba-review-template/SKILL.md`.

### Generation Protocol

For each service spec:

1. Read `01-business-rules.md` — all BR-IDs
2. **Pre-classify** each rule using heuristics (the BA can override any of these):
   - Rules referencing retired systems/interfaces → flag as **Obsolete Candidate**
   - Rules with date-bounded logic (specific years, expired deadlines) → flag as **Obsolete Candidate**
   - Rules implementing workarounds for limitations that won't exist in the target → flag as **Obsolete Candidate**
   - Rules with cyclomatic complexity > 10 (multiple nested branches) → flag as **Optimization Candidate**
   - Rules that duplicate logic already present in another rule → flag as **Merge Candidate**
   - Rules that are clearly core (payment processing, authorization, data integrity) → pre-classify as **Core**
   - Everything else → pre-classify as **Active**
3. **Group rules** by business function (not by source file) for BA readability
4. **Highlight focus areas** — sections where BA input is most needed (marked with attention indicators)
5. **Calculate baseline metrics** — total rules, estimated implementation effort at current scope

### Pre-Classification Heuristics

The agent flags rules for BA attention based on these patterns:

| Pattern | Flag | Rationale |
|---------|------|-----------|
| References a system/interface listed as "decommissioned" or "retired" | Obsolete Candidate | Integration target no longer exists |
| Contains hardcoded dates in the past (e.g., "effective before 2018") | Obsolete Candidate | Time-bounded logic may have expired |
| Implements workaround for a constraint that won't exist in target (e.g., file size limits, batch windows) | Obsolete Candidate | Technical debt from legacy limitations |
| Duplicates logic in another BR-ID with minor variation | Merge Candidate | Can be consolidated |
| Has > 5 conditional branches for what appears to be one business decision | Optimization Candidate | Likely accumulated complexity over time |
| References regulatory/compliance requirement | Attention Needed | BA must confirm still applicable |
| Has Confidence: Low from Phase 1 | Attention Needed | Extraction may be inaccurate |
| Implements calculation with magic numbers (unexplained constants) | Attention Needed | BA should confirm current values |
| **complexityFlag = 'unresolved'** (from Phase 4 Complexity Validation Loop) | **Complexity Review** | Source algorithm complexity exceeds spec complexity after 2-3 extraction passes. Agent could not fully decompose — BA should verify if the condensed spec captures the full business intent or if decision paths were lost. |

### What the Agent Does NOT Do

- Does NOT drop any rules on its own — only flags candidates
- Does NOT simplify rules without BA approval
- Does NOT make business decisions — only presents evidence
- Does NOT assume a rule is obsolete because it looks old

## Invariant & Lifecycle Validation (Layer A — parallel review track)

Alongside business-rule classification, the BA validates the entity state models and data invariants
extracted in Phase 4 (from `02-domain-model.md` `### Entity State Model` + `### Data Invariants`).
These are business-intent questions — exactly the SME/BA territory 4a already owns.

**Load-bearing invariant classification** (per invariant in the review doc):

| Classification | Meaning | Action |
|----------------|---------|--------|
| **Load-bearing** | Essential business truth that must always hold | Keep; exhaustive test coverage; sets `Invariant.isLoadBearing = true` |
| **Legacy artifact** | An artifact of a legacy limitation, not real business truth | Drop or relax (BA confirms — dropping data-integrity is a serious call) |
| **Needs correction** | Correct concept, wrong threshold/expression | BA supplies the correct statement/expression |

**State machine closure review** (per entity lifecycle): the agent presents each entity's state machine
and FLAGS any closure violation (unreachable state, non-terminal state with no exit, an operation that
targets a state not in the model). A non-closed machine is a spec gap — the BA either confirms a missing
transition (agent adds it) or confirms a state is dead (agent removes it). This mirrors how unresolved
preservation flags are resolved at 4a.

**Mandatory-DB is NOT a BA choice.** Whether an invariant is enforced in the DB is decided by its
NATURE (data integrity → db/both), not by the BA and not by 4b placement. The BA decides whether the
invariant is load-bearing at all; the tier follows from that. This keeps integrity invariants from being
accidentally demoted to app-only during a business review.

**Reconciliation (Layer A):**
- Set `Invariant.isLoadBearing` and any corrected `statement`/`tier` on the `Invariant` node.
- Apply state-machine corrections to `02-domain-model.md` `### Entity State Model` (add/remove
  transitions or states) and re-verify closure.
- Record load-bearing/legacy-artifact/correction decisions in `assessment/ba-decision-register.md`
  (a dedicated "Invariant & Lifecycle Decisions" subsection).
- Load-bearing invariants get exhaustive test coverage (the invariant-holds and illegal-transition
  test cases in `.github/skills/saam-test-suite-template/SKILL.md`).

## Extension Point Validation (Layer B — parallel review track)

Alongside rule classification and invariant validation, the BA decides the fate of each extension point
extracted in Phase 4 (from `spec/shared/extensibility-model.md` + `Extension Point:` annotations). This
is a business-intent question — does this per-instance variation serve a real need, or is it legacy cruft
we can collapse? — which is exactly the SME/BA judgment 4a owns.

**Reproduce-vs-unify-vs-drop** (per extension point in the review doc):

| Decision | Meaning | Action |
|----------|---------|--------|
| **Reproduce** | The variation serves a real business need across instances | Keep the point; the engine resolves it per instance; sets `ExtensionPoint.decision = Reproduce` |
| **Unify** | The variation is accidental divergence; collapse to ONE behavior | Remove the point; pick the canonical behavior; instances must converge (a migration/change-management note); sets `decision = Unify` |
| **Drop** | The customization is obsolete | Remove the point and its behavior; sets `decision = Drop` |

**What we do NOT do:** we never preserve different-code-per-instance. If an instance's behavior can't be
expressed as engine configuration over the common code, that's flagged as a processual problem for the
legacy operator — the choices are Unify (converge instances) or Reproduce (via the engine), not fork the
code base. The BA confirms which.

**Reconciliation (Layer B):**
- Set `ExtensionPoint.decision` (+ `decidedBy`) on the node.
- **Reproduce:** keep the `Extension Point:` annotations on the affected BR-IDs; the engine is generated.
- **Unify:** remove the extension point from `spec/shared/extensibility-model.md`; rewrite the affected
  BR-IDs to the canonical behavior (drop their `Extension Point:` annotation); note the convergence
  requirement in the decision register.
- **Drop:** move the affected behavior to the obsolete appendix (like a dropped rule).
- Record each decision in `assessment/ba-decision-register.md` (an "Extension Point Decisions" subsection).

## Step 2: BA Works Through the Document

The BA receives the review document and works through it section by section. For each rule (or group of related rules), the BA provides:

### Classification (REQUIRED for flagged rules)

| Classification | Meaning | Action |
|---------------|---------|--------|
| **Core** | Essential to application purpose, business-critical | Carry forward, highest test priority |
| **Active** | Currently needed, standard business logic | Carry forward as-is |
| **Simplify** | Correct intent but overly complex, can be streamlined | Agent rewrites per BA guidance |
| **Obsolete** | No longer serves a business need | Move to appendix, do not implement |
| **Deferred** | Valid but not needed for initial release | Move to backlog, implement in later phase |
| **Merge** | Combine with another rule (BA specifies which) | Agent merges and renumbers |

### Business Impact Weight (REQUIRED for Core and Active rules)

| Weight | Meaning | Test Implication |
|--------|---------|------------------|
| **Critical** | Failure causes financial loss, compliance violation, or data corruption | Must have exhaustive test coverage |
| **High** | Failure causes significant operational disruption | Must have thorough test coverage |
| **Medium** | Failure causes inconvenience but has workarounds | Standard test coverage |
| **Low** | Failure has minimal business impact | Basic test coverage sufficient |

### BA Notes (OPTIONAL)

Free-text field for:
- Corrections to the extracted logic ("rate is actually 2.5%, not 2%")
- Simplification guidance ("these 3 rules can be one: just check if amount > credit limit")
- Context the extraction missed ("this only applies to government clients")
- Confirmation ("correct as extracted")

## Step 3: Agent Parses Completed Review Document

After the BA completes the review document, the agent:

### 3.1 Update Service Specs

- **Core/Active rules**: Add `weight: Critical|High|Medium|Low` tag to each BR-ID
- **Simplify rules**: Rewrite the Statement and Logic per BA guidance, mark as `[Simplified per BA review]`
- **Merge rules**: Combine BR-IDs, update numbering, preserve original IDs in cross-reference
- **Deferred rules**: Move to a `06-deferred-rules.md` file in the service spec directory
- **Obsolete rules**: Move to a `07-obsolete-rules-appendix.md` with drop rationale

### 3.2 Generate Decision Register

Create `assessment/ba-decision-register.md`:

```markdown
# BA Decision Register

## Review Metadata
- Reviewer: <BA name>
- Date: <review date>
- Services reviewed: <list>

## Decisions Summary
| Decision Type | Count | Impact |
|---------------|-------|--------|
| Confirmed as-is | X | — |
| Simplified | X | -Y implementation days |
| Merged | X | -Y BR-IDs (reduced count) |
| Deferred | X | -Y implementation days (moved to backlog) |
| Dropped (obsolete) | X | -Y implementation days |

## Detailed Decisions

### Dropped Rules (Obsolete)
| BR-ID | Rule Name | Drop Rationale | Approved By |
|-------|-----------|----------------|-------------|

### Simplified Rules
| BR-ID | Original | Simplified To | Rationale |
|-------|----------|---------------|-----------|

### Deferred Rules
| BR-ID | Rule Name | Defer Reason | Target Phase |
|-------|-----------|--------------|--------------|

### Merged Rules
| Original BR-IDs | Merged Into | Rationale |
|-----------------|-------------|-----------|
```

### 3.3 Report Scope Reduction

After processing all BA decisions:

```
Scope Reduction Report:
- Original rule count: X
- After BA review: Y (Z% reduction)
- Rules dropped (obsolete): N
- Rules deferred (backlog): N
- Rules merged: N pairs → N single rules
- Rules simplified: N (reduced implementation complexity)
- Estimated effort savings: ~N implementation days
```

### 3.4 Validate No Critical Rules Were Dropped

Safety check: if a rule classified as "Core" by the agent's pre-classification was marked "Obsolete" by the BA, flag it:

> "BR-XX-YYY was pre-classified as Core (payment validation) but marked Obsolete. Please confirm this is intentional — dropping this rule means [specific consequence]. Is this correct?"

## Integration with Phase 4b

Phase 4b (Automatibility Scoring) uses the BA-reviewed specs:
- Only **Core + Active** rules count toward automatibility score
- **Deferred** rules don't affect the initial implementation timeline
- **Obsolete** rules are excluded entirely
- **Business impact weight** influences the Phase 4b improvement plan priority (Critical gaps get addressed first)

## Deliverables

- [ ] BA Review Document per service/domain (generated by agent, completed by BA)
- [ ] Updated service specs with classifications and weights
- [ ] `assessment/ba-decision-register.md` — all decisions with rationale
- [ ] `spec/microservices/<service>/06-deferred-rules.md` (if any deferred)
- [ ] `spec/microservices/<service>/07-obsolete-rules-appendix.md` (if any dropped)
- [ ] Scope reduction report

## Exit Gate

**For Mode A (Agent Defaults Approved):**

**PhaseEvent (completed):** Write: `graph_add_node(nodeType="PhaseEvent", id="P4A-completed", properties={phase: "P4A", event: "completed", timestamp: <current ISO timestamp>})`.

**🔴 PROMPT HUMAN**: "Phase 4a complete (agent defaults approved). Classifications and weights applied to [N] rules across [M] services. [X] obsolete rules dropped, [Y] rules flagged as Critical priority. Ready for Phase 4b (Automatibility Scoring). Proceed?"

**For Mode B (Full BA Workshop):**

**PhaseEvent (completed):** Same as above — write P4A-completed PhaseEvent after BA confirms decisions are parsed.

**🔴 PROMPT HUMAN**: "BA review complete for [N] services. Scope reduction: [X]% ([Y] rules dropped, [Z] deferred, [W] simplified). Updated specs ready for Phase 4b (Automatibility Scoring). Proceed?"

**Next steps after human approval (both modes):**
- Activate `.github/skills/saam-phase4b-implementation-roadmap/SKILL.md` for automatibility scoring and roadmap generation
- Update the root `README.md` — add Phase 4a completion summary: mode used (A or B), scope reduction %, rules dropped/deferred/simplified, weight distribution
- **Graph update (always):** Use `graph_bulk_import` to add Decision nodes + `decidedAs` edges. Run `graph_run_inferences(rules=["lifecycle_states", "effective_confidence"])`. This records the authorized delta (Obsolete + Deferred counts) for reconciliation.

## Telemetry Production (MANDATORY)

**PRECONDITION: The agent MUST produce `.saam/telemetry/phase4a-ba-review.yaml` BEFORE presenting the exit gate prompt.** If the file does not exist after this step, the agent must create it now.

After the exit gate is approved, the agent MUST produce `.saam/telemetry/phase4a-ba-review.yaml`.

**Data to compute:**

1. **Timing** — infer from task tracker (`tracking/phase4a-ba-review.md`): first task `in_progress` → last task `completed`
2. **Review metrics** — from the decision register and updated specs:
   - Total BRs reviewed, approved unchanged, modified, dropped (obsolete), added (new), reclassified, deferred
   - Critical BR count (post-review)
   - Average review time per BR (duration_hours / total_br_reviewed × 60 = minutes)
   - Disputes requiring escalation
3. **Retroactive complexity corrections** — from BA decisions on unresolved preservation flags:
   - `false_flags_dismissed`: Phase 4 preservation flags the BA said were noise
   - `true_gaps_confirmed`: Phase 4 preservation flags the BA confirmed as real gaps
   - `new_rules_added_from_flags`: new BRs created because of unresolved preservation flags
4. **Mode** — `full_workshop` or `agent_defaults`

**Schema:** See `.github/skills/saam-telemetry/SKILL.md` → `phase4a-ba-review.yaml` for the full YAML structure.

**Retroactive update to Phase 4 telemetry:** If Phase 4 telemetry exists and has `true_positives_pending: true`, update the `dimension_flags.{dim}.true_positive_count` values based on BA review outcomes (gaps confirmed = true positives, flags dismissed = false positives).
