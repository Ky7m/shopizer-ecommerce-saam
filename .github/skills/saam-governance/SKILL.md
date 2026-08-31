---
name: saam-governance
description: "Risk-adaptive governance framework, automated enforcement levels, and specification drift detection."
copyright: "Copyright 2024-2026 SoftServe Inc. All rights reserved."
authors: "Max Kozinenko, Roman Kalita (SoftServe)"
---

# SAAM Governance: Invisible Risk-Adaptive Controls

## Principle

Governance in SAAM is **invisible and automatic**. Developers never choose a governance level. The system detects what changed, determines the risk, and applies proportional controls.

- No friction for changes that don't touch business logic
- Minimal friction (automated check) for standard business changes
- Human gate only when critical business behavior is at risk

**The developer experiences proportional friction, not bureaucratic ceremony.**

## How It Works

Every code change passes through the same pipeline. The pipeline determines governance response based on three signals:

1. **Does the change touch BR-ID-annotated code?** (detect_br_ids.py)
2. **Does the spec hash still match?** (spec_drift.py)
3. **Is the affected BR-ID classified as Critical?** (graph query)

```
Code change committed/saved
    ↓
detect_br_ids.py scans changed files
    ↓
BR-IDs found? ──NO──→ Tests pass → Done (no governance friction)
    │
    YES
    ↓
spec_drift.py checks hash freshness
    ↓
Hash matches? ──YES──→ Tests pass → Done (governance satisfied automatically)
    │
    NO (drift detected)
    ↓
Classification query: is this BR-ID Critical?
    ↓
    ├── NOT Critical → Flag drift, require spec reconciliation before merge
    └── Critical → Flag drift + REQUIRE human review before merge
```

## Governance Levels (Not Tiers — Developers Never See These)

Internally the system operates at three levels, but they're never exposed as a choice:

| Level | Trigger | What Happens | Developer Experience |
|-------|---------|-------------|---------------------|
| **Passthrough** | No BR-IDs in changed files | Tests run. If pass → done. | Zero friction — indistinguishable from normal development |
| **Automated Check** | BR-IDs touched, spec hash matches | Tests run + graph updated. If pass → done. | Zero additional friction — reconciliation happens in background |
| **Drift Response** | BR-IDs touched, spec hash DOESN'T match | Tests run + drift flagged. Must reconcile spec or revert. | Gets a message: "Spec for BR-XX-YYY-NNN has changed since implementation — update code or spec" |
| **Critical Gate** | Critical BR-ID + drift detected | Everything above + human approval required | Gets a message: "Critical rule affected — human review required" |

## Spec Drift Detection

### What Drift Means

Drift occurs when the spec and implementation were written at different times and may no longer agree:

- **Spec changed after implementation:** Someone updated the business rule spec but didn't update the code. The implementation is stale.
- **Code changed without spec update:** Someone modified BR-ID-annotated code but the spec still describes the old behavior. The spec is stale.

Both are problems. Both get flagged.

### How Hashes Work

Every BR-ID section in `01-business-rules.md` has a content hash (SHA256, first 16 chars). When code is first implemented (CLAIMS_IMPLEMENTATION edge created), the current spec hash is stamped on the edge. When the reconciliation pipeline runs, it compares the stamped hash against the current spec content.

If they differ → drift.

### Running Drift Detection

```bash
# Check a specific service
python3 graph-mcp/scripts/spec_drift.py --service order-service

# Check all services
python3 graph-mcp/scripts/spec_drift.py --all

# After intentional spec edits, update hashes (marks current spec as the new baseline)
python3 graph-mcp/scripts/spec_drift.py --service order-service --update
```

### When Drift Detection Runs Automatically

| Event | What Triggers It | Script |
|-------|-----------------|--------|
| Validation run | `run-and-reconcile.sh` calls `reconcile_validation.py` which checks hashes | Automatic during Phase 5 + Phase 6 |
| File save (sourcecode/) | PostFileSave hook runs `detect_br_ids.py` which stamps new hash | Automatic via Kiro hook |
| CI/CD pipeline | Can include `spec_drift.py --all` as a merge gate | Manual setup per project |

## Governance Responses

### Passthrough (No BR-IDs Affected)

**Applies to:** Config changes, CI scripts, documentation, CSS, infra-as-code, test utilities, dependency updates.

**Response:** None. Tests pass → merged. The governance system is invisible.

### Automated Check (BR-IDs Touched, No Drift)

**Applies to:** Code changes in BR-ID-annotated methods where the spec hasn't changed.

**Response:**
1. `detect_br_ids.py` updates CLAIMS_IMPLEMENTATION edges (graph tracks new implementation)
2. `reconcile_validation.py` runs after tests → updates lifecycle states
3. If tests pass → done
4. If tests fail → treated as implementation bug (agent/developer fixes code to match spec)

**Developer experience:** Same as normal development. Graph updates happen in background.

### Drift Response (Spec Hash Mismatch)

**Applies to:** Code changes where the spec has been updated since the implementation was last validated, OR spec changes where the implementation hasn't been re-validated.

**Response:**
1. SPEC_DRIFT deviation created in graph
2. Service flagged with specific BR-IDs affected
3. Resolution required: either update the code to match the new spec, or run `spec_drift.py --update` to accept current implementation as the new baseline

**Developer experience:** Gets a message identifying exactly which BR-IDs are stale and what changed.

**Resolution paths:**
- **Code is correct, spec was wrong:** Update spec → run `spec_drift.py --update` → hashes align
- **Spec is correct, code is stale:** Fix code → tests pass → `reconcile_validation.py` stamps new hash
- **Both need updating:** Update spec → fix code → validate → update hashes

### Critical Gate (Critical BR-ID + Drift)

**Applies to:** Changes affecting BR-IDs classified as `Critical` in Phase 4a that also have spec drift.

**Response:**
1. Everything in Drift Response above
2. Additionally: merge/deployment BLOCKED until a human approves
3. The graph records who approved and when (Decision node linked to the BR-ID)

**Developer experience:** Gets a message: "Critical business rule BR-XX-YYY-NNN is affected and spec has drifted. Requires human approval."

## Integration with Existing SAAM Mechanisms

### Phase 5 (Implementation)

During Phase 5, drift detection runs as part of the reconciliation pipeline:
- After every validation run (`run-and-reconcile.sh`), spec hashes are checked
- If the agent modifies a spec during implementation (to fix an error), it must run `spec_drift.py --update` to re-baseline
- Critical BR-ID changes require the human exit gate (already enforced by Phase 5's exit gate protocol)

### Phase 6 (Continuous Evolution)

During Phase 6, drift detection is the PRIMARY governance mechanism:
- Every change flows through the reconciliation pipeline
- Drift is the signal that triggers proportional governance
- Without drift → change flows through with minimal friction
- With drift → change requires reconciliation before acceptance

### Telemetry

Drift events feed telemetry:
- Count of drift detections per service per phase
- Resolution method (code fix vs spec update vs human approval)
- Time from drift detection to resolution
- Correlation: do services with frequent drift have more deviations?

## Anti-Gaming Properties

This governance model is resistant to gaming because:

1. **Classification is automatic** — derived from code content (BR-ID annotations), not developer claims
2. **Hashes are computed from file content** — can't be faked without changing the spec itself
3. **Critical classification comes from Phase 4a** — set by BA/human, stored in graph, not changeable by developer
4. **Detection runs on every validation** — can't skip it without skipping tests (which blocks merge anyway)
5. **Removing BR-ID annotations to avoid governance** is itself detectable — CLAIMS_IMPLEMENTATION edge disappears from graph, service completeness drops, which triggers a different alert

The only way to "game" this system is to not use BR-ID annotations at all — which means the code isn't traceable and can't pass the Phase 5 exit gate.

## Scripts Reference

| Script | Purpose | When It Runs |
|--------|---------|-------------|
| `graph-mcp/scripts/detect_br_ids.py` | Detects BR-IDs in code, stamps spec hash on edges | PostFileSave hook + batch mode |
| `graph-mcp/scripts/spec_drift.py` | Compares current spec hashes against graph, reports drift | Manual + CI integration |
| `graph-mcp/scripts/reconcile_validation.py` | Runs full reconciliation including drift check | After every test suite run |
| `validation/run-and-reconcile.sh` | Orchestrates: run tests → produce artifact → reconcile | Phase 5 Step 4 + Phase 6 |
