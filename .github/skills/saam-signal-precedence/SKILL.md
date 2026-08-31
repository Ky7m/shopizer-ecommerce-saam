---
name: saam-signal-precedence
description: "Deterministic decision hierarchy and conflict resolution rules for modernization signals and quality gates."
copyright: "Copyright 2024-2026 SoftServe Inc. All rights reserved."
authors: "Max Kozinenko, Roman Kalita (SoftServe)"
---

# SAAM Signal Precedence Model

## Purpose

A business rule in SAAM carries multiple independent signals. When these signals disagree, the agent needs a deterministic resolution — not a judgment call that varies between sessions.

This document defines:
1. What each signal means
2. Which signals are **gates** (block progress) vs **flags** (inform, don't block)
3. The precedence hierarchy for conflict resolution
4. Resolution rules for common conflict patterns

## Signal Inventory

A fully-tracked BR-ID can carry all of these simultaneously:

| Signal | Source | Type | Meaning |
|--------|--------|------|---------|
| `lifecycleState` | Graph (computed) | State | Current progression: Extracted → Assigned → Declared → Tested → Passing |
| `provenanceConfidence` | Phase 1/4 extraction | Score (0-1) | How sure we are the rule was extracted correctly |
| `implementationConfidence` | Phase 5 execution | Score (0-1) | How sure we are the code is correct |
| `testQualityConfidence` | Phase 4c/5 validation | Score (0-1) | How sure we are the test validates the right thing |
| `effectiveConfidence` | Graph (min of dimensions) | Score (0-1) | Weakest-link overall confidence |
| `classification` | Phase 4a BA review | Category | Core / Active / Simplify / Obsolete / Deferred |
| `weight` | Phase 4a BA review | Priority | Critical / High / Medium / Low |
| `preservationFlag` | Phase 4 complexity check | Status | ok / flagged / unresolved |
| `specHash` alignment | Spec drift detection | Binary | Match (aligned) / mismatch (drifted) |
| CLAIMS_IMPLEMENTATION | detect_br_ids.py | Edge exists/absent | Code claims to implement this rule |
| VALIDATED_BY | reconcile_validation.py | Edge exists/absent | Tests pass for this rule |
| RECONCILED_WITH | CAST reconciliation | Edge exists/absent | CAST structural evidence confirms coverage |
| Cross-model verification | Optional Phase 4 | Agree / disagree | Independent extraction agrees or disagrees |
| Mutation test result | Phase 5 | Pass / fail | Tests have real verification power |
| Deviation status | Phase 5/6 | OPEN / RESOLVED | Known deviation exists against this rule |
| CAST path coverage | Graph validation | Covered / unaccounted | CAST shows legacy path with/without BR-ID |

## Signal Categories

### Gates (block progress until resolved)

These signals STOP the rule from being considered "done." If any gate is not satisfied, the rule cannot pass the exit gate for its service.

| Gate | Blocks When | Resolution Required |
|------|-------------|---------------------|
| Test execution | Tests fail for this BR-ID | Fix code to pass tests |
| Mutation test (Critical BRs only) | Mutation survives (test is weak) | Strengthen test assertion |
| CAST unaccounted path | CAST shows a path with no BR-ID coverage | Investigate: add BR-ID, mark as dead code, or document exclusion |
| Spec drift (Critical BRs) | Spec hash mismatch on Critical rule | Reconcile spec and implementation |
| Open SPEC_DRIFT deviation | Deviation.type = SPEC_DRIFT, status = OPEN | Human must decide which is correct (spec or code) |
| State machine not closed (Layer A, service) | An owned entity's state machine has a dangling transition or dead-end state | Fix the Entity State Model and re-verify closure |
| Mandatory-DB object missing (Layer A/C, service) | An integrity invariant (tier db/both) has no enforcing DbObject | Add the DB object + DDL |

### Flags (inform but don't block)

These signals are recorded, surfaced to the agent, and may affect confidence scores — but they don't prevent progress. They inform the human at the exit gate.

| Flag | What It Means | Agent Action |
|------|---------------|-------------|
| Cross-model disagreement | Independent extraction produced different interpretation | Note in exit gate report. Human decides. Does NOT block. |
| Preservation flag = unresolved | Complexity check found gaps that weren't resolved | Already went to BA review. If BA approved → flag is informational. |
| Low provenance confidence (< 0.7) | Extraction may be incorrect | Surface in "attention needed" list. Recommend human review. |
| Spec drift (non-Critical BRs) | Spec hash mismatch on standard rule | Surface in reconciliation report. Recommend update. Don't block. |
| Mutation test (non-Critical BRs) | Mutation survived for non-Critical rule | Record in telemetry. Recommend strengthening. Don't block exit gate. |

## Precedence Hierarchy

When multiple signals exist for the same rule and they suggest different actions, resolve using this hierarchy (highest precedence first):

```
LEVEL 1 — Objective structural evidence (independent of LLM interpretation)
    [1] CAST reconciliation (unaccounted paths)
    [2] Test execution results (pass/fail — binary, unambiguous)
    [3] Mutation test results (test strength verified or not)

LEVEL 2 — Deterministic computed state (hash-based, no interpretation)
    [4] Spec drift status (hash match/mismatch)
    [5] Lifecycle state (derived from edges that exist)

LEVEL 3 — Human authority (decisions with rationale)
    [6] BA classification and weight (Phase 4a decisions)
    [7] Human approval at exit gates

LEVEL 4 — LLM-derived assessment (valuable but potentially correlated)
    [8] Cross-model verification result
    [9] Provenance confidence (extraction self-assessment)
    [10] Preservation vector status
    [11] Automatibility score contribution
```

**Resolution principle:** Higher-level evidence overrides lower-level evidence. If Level 1 and Level 4 disagree, Level 1 wins.

## Conflict Resolution Rules

### Pattern 1: CAST says unaccounted, everything else says fine

```
Situation:
  - Tests pass ✓
  - Mutation passes ✓
  - Spec hash aligned ✓
  - BA approved ✓
  - BUT: CAST reconciliation shows a source path with no BR-ID

Resolution:
  CAST wins (Level 1 > all others).
  
  The rule is NOT done. Investigate the unaccounted path:
  a) Is it dead code? → Mark in CAST, document exclusion, proceed.
  b) Is it covered by a DIFFERENT BR-ID? → Add RECONCILED_WITH edge, proceed.
  c) Is it genuinely missing? → Extract new BR-ID, add to spec, add test, implement.

  Action: Block exit gate until human confirms one of (a), (b), or (c).
```

### Pattern 2: Cross-model disagrees, but tests pass and BA approved

```
Situation:
  - Cross-model verification says different interpretation
  - Tests pass ✓
  - BA validated ✓
  - Mutation passes ✓

Resolution:
  Cross-model disagreement is a FLAG, not a GATE.
  
  BA (Level 6) already validated this rule. Tests (Level 2) pass.
  Cross-model (Level 8) is lower precedence than both.
  
  Action: Record disagreement in exit gate report. Note: "Cross-model
  extracted [X], primary extracted [Y]. BA confirmed primary interpretation.
  Tests validate primary interpretation." No block.
```

### Pattern 3: Tests pass but mutation test fails (test is weak)

```
Situation:
  - Comprehensive test suite passes ✓
  - But mutation testing shows the test doesn't actually verify the logic
  - Implementation might be wrong — we just can't tell

Resolution (Critical BR-IDs):
  Mutation test (Level 3) is a GATE for Critical rules.
  Block exit gate. Strengthen the test first.
  
Resolution (non-Critical BR-IDs):
  Mutation test is a FLAG for non-Critical rules.
  Record in telemetry. Recommend strengthening. Don't block.
```

### Pattern 4: Spec drift detected but tests still pass

```
Situation:
  - Spec hash has changed (someone updated the spec)
  - But the old implementation still passes all tests
  - Tests were written from the OLD spec

Resolution:
  Spec drift (Level 4) detected. This means tests may be stale too.
  
  For Critical BRs: GATE. Must re-validate: update tests from new spec,
  re-run, confirm code still matches new spec. If code doesn't match new
  spec, fix code.
  
  For non-Critical BRs: FLAG. Surface in report. Recommend re-validation.
  Don't block (tests are passing against current code — functionality works,
  just may not match updated spec intent).
```

### Pattern 5: Low provenance confidence but tests pass and mutation passes

```
Situation:
  - Extraction confidence was 0.55 (agent wasn't sure about interpretation)
  - But implementation passes all tests
  - Mutation testing confirms tests are strong

Resolution:
  Test evidence (Level 2) + mutation evidence (Level 3) together override
  provenance concern (Level 9).
  
  If tests are strong AND passing → the implementation is behaviorally
  correct regardless of extraction confidence. The provenance concern
  was about extraction correctness, but test evidence proves the
  implemented behavior works.
  
  Action: Raise implementationConfidence to 0.9 (test-proven).
  Provenance stays at 0.55 (still not sure the SPEC is right, but the
  CODE works). Note: this means effectiveConfidence = min(0.55, 0.9, 0.9) = 0.55.
  
  The rule still shows up in "attention needed" (effective < 0.7) —
  which is correct. Someone should verify the spec matches business intent,
  even though the code works.
```

### Pattern 6: BA says Critical, but automatibility says fully automatable

```
Situation:
  - BA classified rule as Critical (highest governance)
  - Automatibility analysis says 95% (straightforward to implement)

Resolution:
  These are not contradictory — they measure different things.
  
  - Classification (Critical) = business IMPORTANCE
  - Automatibility = implementation DIFFICULTY
  
  A rule can be critically important to the business AND trivially easy
  to implement (e.g., "reject orders over $1M" — critical rule, simple code).
  
  Action: Implement with full automation confidence. But apply Critical
  governance (mutation testing mandatory, spec drift blocks, human exit gate).
  Difficulty doesn't reduce governance — importance drives governance.
```

### Pattern 7: Preservation vector flagged, BA dismissed the flag

```
Situation:
  - Phase 4 complexity check flagged 3 dimensions
  - Phase 4a BA reviewed and said "the flag is noise — extraction is correct"
  - BA dismissed the flag (false_flags_dismissed in telemetry)

Resolution:
  BA decision (Level 6) overrides preservation vector (Level 10).
  
  The BA explicitly reviewed the source and confirmed the extraction
  is faithful. The preservation flag was a false positive.
  
  Action: Rule proceeds normally. Flag dismissed. Record in telemetry
  (feeds false-positive rate calibration for complexity thresholds).
```

## Edge Case: No Human Available

If a conflict requires human resolution but no human is available:

1. Record the conflict in the deviation log (type = SPEC_DRIFT or DEV_TEST)
2. Do NOT auto-resolve
3. Continue with other work (other services, other rules)
4. Surface all unresolved conflicts at the next exit gate prompt

**Never auto-resolve conflicts that require human judgment.** The graph will show the rule with a pending deviation, reducing effective confidence, which is the correct representation of the state.

## Summary Decision Table

| Signal State | Critical BR | Non-Critical BR |
|-------------|-------------|-----------------|
| CAST unaccounted | GATE — block | GATE — block |
| Tests failing | GATE — block | GATE — block |
| Mutation survived | GATE — block | FLAG — report |
| Spec drift | GATE — block | FLAG — report |
| Cross-model disagrees | FLAG — report | FLAG — report |
| Low provenance | FLAG — surface in attention list | FLAG — surface |
| Preservation unresolved | Already sent to BA | Already sent to BA |
| Open deviation | GATE — resolve before deploy | GATE — resolve before deploy |

## How Agents Use This Document

When the agent encounters contradictory signals:

1. Identify which signals are in conflict
2. Look up each signal's level in the precedence hierarchy
3. Higher level wins
4. Check if the winning signal is a GATE or FLAG for this BR's classification
5. If GATE → block and report to human
6. If FLAG → record, continue, surface at exit gate

**This model is referenced by:** `.github/skills/saam-governance/SKILL.md`, `.github/skills/saam-graph-context/SKILL.md`, `.github/skills/saam-phase5-ai-dlc-implementation/SKILL.md`, and `reconcile_validation.py`.


---

## Graph-Computed signalStatus (Implementation)

The precedence rules defined above are NOT evaluated by the agent at runtime. They are computed by the graph inference engine and stored as a property on each BusinessRule node.

### Properties

| Property | Node | Meaning |
|----------|------|---------|
| `signalStatus` | BusinessRule | `CLEAR` / `BLOCKED` / `FLAGGED` — single answer to "can this proceed?" |
| `signalBlockers` | BusinessRule | Array of gate codes that are unsatisfied (e.g., `["TEST_FAILING", "SPEC_DRIFT_CRITICAL"]`) |
| `signalFlags` | BusinessRule | Array of flag codes that are active (e.g., `["LOW_CONFIDENCE"]`) |
| `_signalUpdatedAt` | BusinessRule | When signal status was last computed |
| `signalStatus` | Service | Rollup: BLOCKED if any rule BLOCKED, FLAGGED if any FLAGGED, else CLEAR |
| `signalBlockedCount` | Service | Number of BLOCKED rules in this service (+ implicit-layer structural blockers) |
| `implicitBlockers` | Service | Layer A/C structural gate codes (`STATE_MACHINE_NOT_CLOSED`, `MANDATORY_DB_OBJECT_MISSING`) — service-level, not per-BR |
| `signalFlaggedCount` | Service | Number of FLAGGED rules in this service |

### When It's Computed

Signal status is recomputed:
- Every time `graph_run_inferences()` is called (includes `signal_status` in default rules)
- By `reconcile_validation.py` after updating the graph (runs targeted signal evaluation for the affected service)
- On demand: `graph_run_inferences(rules=["signal_status"])` for a quick signal refresh

### How Agents Consume It

**Before doing work:**
```
graph_implementation_context(serviceId="MS-03")
→ Response includes signalStatus per rule:
  BLOCKED rules listed first with blockers
  FLAGGED rules listed with flags
  CLEAR rules summarized
```

**Before presenting exit gate:**
```
graph_query_nodes(Service, {serviceId: "MS-03"})
→ Check s.signalStatus
  If BLOCKED → do not present exit gate, work on blockers
  If FLAGGED → present gate with flags listed for human decision
  If CLEAR → present gate normally
```

**After fixing a blocker:**
```
./validation/run-and-reconcile.sh <service>
→ Updates graph → recomputes signal status → regenerates tasks.md
→ If all blockers resolved → signalStatus moves to CLEAR or FLAGGED
```

### Gate Codes Reference

| Code | Gate/Flag | Trigger | Resolution |
|------|-----------|---------|------------|
| `TEST_FAILING` | Gate (all) | Any TESTED_BY assertion has status=FAIL | Fix code to pass test |
| `MUTATION_SURVIVED` | Gate (Critical only) | mutationKillRate < 1.0 | Strengthen test assertion |
| `SPEC_DRIFT_CRITICAL` | Gate (Critical only) | specHash mismatch on CLAIMS_IMPLEMENTATION edge | Reconcile spec and code |
| `OPEN_DEVIATION` | Gate (all) | Deviation node with status=OPEN linked to this BR | Resolve the deviation |
| `STATE_MACHINE_NOT_CLOSED` | Gate (service, Layer A) | An owned entity has a transition to an undeclared state, or a non-terminal state with no exit | Fix the Entity State Model (add transition/state, mark terminal) and re-verify closure |
| `MANDATORY_DB_OBJECT_MISSING` | Gate (service, Layer A/C) | An integrity invariant (tier db/both) has no DbObject enforcing it | Add the DB object (trigger/CHECK) to `### Database Logic Objects` + backing DDL |
| `SPEC_DRIFT` | Flag (non-Critical) | specHash mismatch | Recommend re-validation |
| `WEAK_TEST` | Flag (non-Critical) | mutationKillRate < 1.0 | Recommend strengthening |
| `LOW_CONFIDENCE` | Flag (all) | effectiveConfidence < 0.7 | Recommend human review |

### Why This Matters

Without signalStatus, the agent must:
1. Read this steering document
2. Query 5+ graph properties per rule
3. Evaluate the precedence hierarchy
4. Decide whether to block or flag
5. Hope it does this consistently across sessions

With signalStatus, the agent:
1. Reads ONE property
2. Acts accordingly (BLOCKED = work on it, FLAGGED = report it, CLEAR = skip)

The complexity is in the inference engine, not in the agent's prompt adherence. This is deterministic, testable, and consistent.
