---
name: saam-phase3-convergence
description: "Reconciliation and convergence protocols matching bottom-up extracted rules with top-down domain designs."
copyright: "Copyright 2024-2026 SoftServe Inc. All rights reserved."
authors: "Max Kozinenko, Roman Kalita (SoftServe)"
---

# SAAM Phase 3: Convergence & Feature Validation

## Objective
Map every source feature to a target service. Identify gaps. Validate boundaries. Ensure zero business logic loss.

## Required Steering Files (Read Before Proceeding)

The agent MUST read the following steering files before executing Phase 3:

1. **`.github/skills/saam-human-guidance-protocol/SKILL.md`** — Prompt categories, decision register format, agent rules
2. **`.github/skills/saam-task-tracking/SKILL.md`** — Tracking file format and Jira dual-write protocol

Phase 3 operates on the outputs of Phase 1 (extraction summaries) and Phase 2 (architecture artifacts). No additional templates are needed — convergence produces its own deliverables.

## Task Tracking Activation

**PRECONDITION: The agent MUST NOT begin convergence work until `tracking/phase3-convergence.md` exists.** If it doesn't exist, create it NOW with all deliverables listed as PENDING.

**PhaseEvent (telemetry timestamp):** Immediately after creating the tracking file, write: `graph_add_node(nodeType="PhaseEvent", id="P3-started", properties={phase: "P3", event: "started", timestamp: <current ISO timestamp>})`.

After each convergence task completes (mapping done, validation passed, gaps resolved), update the tracking file immediately. If Jira is configured, create an Epic with Tasks. See `.github/skills/saam-task-tracking/SKILL.md` for format.

## Entry Precondition: Verify Phase 2 Artifacts

Before beginning Phase 3, the agent MUST verify that Phase 2 produced its mandatory `modernization/` artifacts:
- [ ] `modernization/modernized-architecture.md` — exists and non-empty
- [ ] `modernization/services-composition.md` — exists and non-empty

**If missing:** Inform the human and offer to generate them before proceeding. Do NOT start convergence without a defined target architecture and service catalog.

## 3.1 Feature Matrix

Map EVERY extracted feature/rule to exactly one target service:

| Feature ID | Source Location | Description | Target Service | Status |
|-----------|----------------|-------------|----------------|--------|
| F-XX-001 | program:func | What it does | Service Name | ✅ Mapped / ❌ Gap |

**Acceptance**: Zero unmapped critical features.

## 3.1a Top-Down Flow Coverage (extraction-gap net — MANDATORY)

The feature matrix above maps what bottom-up EXTRACTED to services. It cannot, by construction, surface
a flow that top-down KNOWS exists but bottom-up never extracted — such a flow simply has no BR to place
in a row, so it is silently absent from the matrix rather than flagged as a gap.

This check closes that hole. For every operation/endpoint named by the TOP-DOWN track — sequence
diagrams (`modernization/*-sequence-diagrams.md`), cross-service workflows, user journeys — assert that
at least ONE bottom-up BR-ID implements it.

- A top-down flow with **≥1 backing BR** → covered (it will appear in the matrix).
- A top-down flow with **ZERO backing BRs** → **extraction gap**, NOT merely an unmapped feature. The
  analyst knew the flow existed (they drew it), but the extractor never read the logic behind it. This
  is the class where a sequence diagram shows e.g. an "enroll" / "create" / "initialize" step but no
  rule implements it — the backend generator then has nothing to build and produces only the half that
  WAS extracted.

**Resolution:** each zero-backing flow routes BACK to Phase 1 for targeted re-extraction of the source
behind that flow (not a design invention — read the legacy code the sequence diagram implies). Record it
in the gap analysis as an EXTRACTION GAP (distinct from a Source→Target assignment gap). Any unresolved
extraction gap on a critical flow blocks the convergence exit gate.

**🔴 PROMPT HUMAN** (if any top-down flow has zero backing BRs): "These flows are named in the design but
have no extracted business logic behind them: [list]. Each is an extraction gap — the source was never
read. Re-extract the logic for each (targeted Phase 1 pass), or confirm the flow is not real?"

This is the mode-independent twin of the CAST Table Write-Coverage Reconciliation (Phase 1 exit): CAST
catches missing writers from the data side; this catches missing flows from the design side. On a
Hybrid/CAST engagement both run; on Direct Source this is the primary net.

## 3.2 Gap Analysis

Three categories:

### Source → Target Gaps (Loss Risk)
Features in legacy with no target home. Each rated:
- 🔴 Critical: Must be in target
- 🟡 Important: Should be in target
- ⚪ Low: Can be deferred or intentionally dropped

**🔴 PROMPT HUMAN**: "These X features have no target service: [list]. For each: (a) assign to existing service, (b) create new service, (c) intentionally drop?"

### Target → Source Gaps (New Capability)
Modern features not in legacy (event notifications, APIs, dashboards). Document as value-add.

### Boundary Violations
Logic that legacy implements in one place but target splits across services:
- Define the API contract at the split point
- Ensure transactional integrity
- No business logic lost in the split

## 3.3 Domain Boundary Validation

For each service boundary verify:
1. **Cohesion**: All assigned features logically belong together
2. **Coupling**: Cross-service calls minimized
3. **Data Ownership**: Service owns all data it needs
4. **Transaction Scope**: Operations complete within one service

**🔴 PROMPT HUMAN**: "Services [A] and [B] share tables [X, Y] in source. Options: (a) Service A owns, B calls API (b) duplicate with sync (c) merge services. Which?"

## 3.4 Business Rule Final Assignment

Every BR-ID maps to exactly one service. Medium/Low confidence rules need human review.

**🔴 PROMPT HUMAN**: "These rules have unclear ownership: [list]. Please confirm target service for each."

## 3.5 Comprehensive Test Suite Feasibility Check

For each service, verify:
- [ ] Service has REST API endpoints defined
- [ ] Business rules are testable via API (input → expected output)
- [ ] State transitions are observable via GET endpoints
- [ ] Error cases produce specific HTTP status codes

If a rule cannot be tested via the API, it must be tested in the unit test suite.

## 3.6 Implicit-Layer Ownership (resolve shared/cross-cutting ambiguity)

Most implicit-system items (Layer A entity state models, single-entity invariants; Layer C db-objects;
Layer B extension points) belong to whichever service owns the entity/table they concern — so their
ownership follows the table assignment from 3.1/3.3 and is stamped implicitly at Phase 4 spec-authoring.
But two cases are genuinely cross-cutting and MUST be assigned deliberately HERE, where boundaries are
decided — otherwise Phase 4 stamps an arbitrary owner:

- **Cross-entity invariants (Layer A, `kind = cross-entity`)** — an invariant where one entity's state
  gates another's (e.g., an order cannot ship while any line is on hold). If the entities live in
  DIFFERENT services, decide: which service enforces it, and how (owning-service enforces on its write
  path + the other service exposes the needed state; or an event-driven check). Record the owner.
- **The shared extensibility engine (Layer B)** — `spec/shared/extensibility-model.md` is ONE common-code
  mechanism used by many services. Decide its home: a shared library/capability consumed by all, or a
  dedicated configuration service. This is an architecture call to make now, not per-service later.

**🔴 PROMPT HUMAN** (only if cross-entity invariants span services OR an extensibility engine exists):
"Cross-cutting ownership: [cross-entity invariant INV-... spans services A/B] and [the extensibility
engine]. Who owns/enforces each? Options per item presented." If neither exists, this is a no-op.

## Deliverables
- [ ] Complete feature matrix (source → target, 100% mapped)
- [ ] Top-down flow coverage checked: every design-named flow has ≥1 backing BR, or is logged as an extraction gap + re-extracted
- [ ] Gap analysis with severity and resolution
- [ ] Boundary validation report
- [ ] Every BR-ID assigned to one service
- [ ] Cross-entity invariants + extensibility engine ownership assigned (or explicitly "none cross-cutting")
- [ ] Test suite feasibility confirmed per service
- [ ] `assessment/microservice-gap-analysis.md`
- [ ] `validation/comprehensive-validation-summary.md`

## Exit Gate
**🔴 PROMPT HUMAN**: "Convergence complete. Zero critical gaps. All rules assigned. Approve to begin specification generation?"

**PRECONDITION before presenting exit gate:**
- `tracking/phase3-convergence.md` fully updated (all tasks DONE)
- Graph updated: `graph_bulk_import` executed with ASSIGNED_TO edges (BR-IDs → Services). Run `graph_run_inferences(rules=["lifecycle_states", "effective_confidence"])`.
- If CAST configured: Run graph validation Query 2 (Assignment Coverage) — report any BR-IDs without service assignment.
- **PhaseEvent (completed):** Write: `graph_add_node(nodeType="PhaseEvent", id="P3-completed", properties={phase: "P3", event: "completed", timestamp: <current ISO timestamp>})`
- Produce `.saam/telemetry/phase3-convergence.yaml`

**Next steps after human approval:**
- Activate `.github/skills/saam-phase4-spec-generation/SKILL.md` for microservice specification generation
- Activate `.github/skills/saam-spec-template/SKILL.md` for the specification structure reference
- Activate `.github/skills/saam-api-contract/SKILL.md` for the API contract generation protocol
- Update the root `README.md` — add Phase 3 completion summary: feature matrix status, gaps resolved, services with assigned rules, test feasibility confirmed
