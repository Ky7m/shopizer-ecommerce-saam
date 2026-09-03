---
name: saam-spec-template
description: "Standard microservice specification template with business rule definitions, data models, and API endpoints."
copyright: "Copyright 2024-2026 SoftServe Inc. All rights reserved."
authors: "Max Kozinenko, Roman Kalita (SoftServe)"
---

# SAAM Microservice Specification Template

Copy this structure for each new service specification.

---

```markdown
# <Service Name> Specification

**Version**: 1.0
**Date**: <Date>
**Status**: 🔴 Not Started | 🟡 In Progress | 🟢 100% COMPLETE
**Service ID**: MS-<NN>

## Service Overview
| Attribute | Value |
|-----------|-------|
| Service Name | |
| Service ID | MS-<NN> |
| Port | 80XX |
| Database Schema | `<name>_schema` |
| Automation Potential | X% |
| Priority | 1/2/3 |
| Implementation Phase | Months X-Y |
| Effort | X weeks |

## Purpose
<One paragraph>

## Business Context
### Legacy Components Replaced
| Component | Size | Stack | Function |
|-----------|------|-------|----------|

## Data Model
### Core Entities
<Complete DDL with CREATE TABLE, indexes, constraints>

### DDL Quality Rules
- Every column must have ONE of:
  - (a) A legacy source column reference: "Maps to <legacy_table>.<column>"
  - (b) A BR-ID justification: "Required by BR-XX-YYY-NNN for <purpose>"
  - (c) A standard infrastructure annotation: "Audit/multi-tenancy standard"
- Columns that appear identically across >5 tables with no domain justification are template artifacts — REMOVE THEM
- Standard infrastructure columns (tenant_id, created_at, updated_at, created_by, correlation_id) are exempt from this check
- Do NOT add `amount_total` to identity tables, `currency_code` to session tables, or `quantity` to role tables — every column must make domain sense

### Entity State Model (Layer A — for every entity that has a lifecycle/status)

Omit for entities with no lifecycle (pure reference/lookup data). For every entity whose rows move
through states (a `status`/`state` column, or an implicit lifecycle in the legacy), model the closed
state machine. "Green CRUD" that can drive an entity into a state the legacy would never allow is a
primary cause of "builds but doesn't behave like the legacy" — this section prevents it.

```markdown
#### <Entity> lifecycle
- **States:** Draft (initial), Validated, Posted (terminal), Voided (terminal)
- **Transitions:**
  | From | To | Trigger (BR-ID) | Guard (precondition) |
  |------|----|-----------------|----------------------|
  | Draft | Validated | BR-GL-VAL-001 | all lines have an account and amount |
  | Validated | Posted | BR-GL-PST-003 | debits equal credits |
  | Draft | Voided | BR-GL-VOID-001 | user has void permission |
  | Validated | Voided | BR-GL-VOID-001 | not yet posted |
```

**Closed-machine rules (verified at 4a):**
- Every state is reachable from the initial state.
- Every non-terminal state has at least one outgoing transition.
- Terminal states have NO outgoing transitions and are explicitly marked.
- No operation may move an entity to a state not in this model. The generator enforces this — an
  endpoint that sets an arbitrary status is a spec violation.
- Every transition names the BR-ID that drives it (Intent: State Transition) and its guard.

### Data Invariants (Layer A — constraints that must ALWAYS hold)

Constraints that must hold regardless of which code path runs. Each is tagged with an enforcement tier.

```markdown
| Invariant ID | Statement (domain terms) | Entity | Kind | Tier |
|--------------|--------------------------|--------|------|------|
| INV-GL-001 | A posted batch's debits must equal its credits | ledger_batch | cross-field | db |
| INV-GL-002 | A line's amount equals quantity times unit price | ledger_line | computed | both |
| INV-OR-001 | An order's status transitions are monotonic (no Posted -> Draft) | order | constraint | both |
```

**Tier rules:**
- **Integrity invariants are mandatory-DB** (`db` or `both`) — data integrity cannot depend on the app
  being the sole writer. This is decided by the invariant's NATURE, not by a Phase 4b placement decision.
  A `db`/`both` integrity invariant becomes a DB object (usually a trigger or CHECK constraint) in the
  `### Database Logic Objects` table with `Placement = mandatory-db-integrity`.
- Non-integrity business invariants MAY be `app` (enforced in the domain layer only).
- **Computed invariants** (`computed` kind) reuse the computed-field-provenance convention: state the
  source expression (e.g., `amount = quantity * unit_price`) so a hardcoded value is a visible violation.
- Cross-entity invariants (one entity's state constrains another) also appear in
  `spec/shared/entity-lifecycle.md` — list them here with `Kind = cross-entity` and cross-reference.

### Database Logic Objects (Layer C — only if any logic is placed in the DB tier)

**Default is app-tier.** Omit this section entirely if no logic for this service was placed in the
database tier. Populate it ONLY for:
- Business rules whose Phase 4b **placement decision** was `db-view | db-function | db-proc | db-trigger`
  (surfaced as a `PLACEMENT_REVIEW` candidate — set-based / high-volume / high-frequency / was-a-DB-proc /
  report-aggregation / batch-sweep — and confirmed db-tier by the architect with performance evidence), OR
- **Mandatory-DB integrity invariants** (Layer A) that must be enforced by the database regardless of the
  app-first default (data integrity cannot depend on the app being the sole writer).

Each object is generated as an **ordered migration** (see backend-service transformation). The table
column order is FIXED — the graph importer (`import_specs.py`) parses it positionally into `DbObject` nodes.

```markdown
| Name | Kind | Implements | Enforces Invariant | Migration Order | Binding | Placement |
|------|------|------------|--------------------|-----------------|---------|-----------|
| compute_batch_total | function | BR-GL-PST-003 |  | 10 | repository method GlRepository.computeTotal -> SELECT compute_batch_total(:batchId) | P4b:PLACE-004 |
| v_open_orders | view |  |  | 20 | read model backed by view (OrderReadRepository) | P4b:PLACE-007 |
| trg_enforce_balanced | trigger |  | INV-GL-001 | 30 | trigger — no app call (fires on INSERT/UPDATE of ledger_line) | mandatory-db-integrity |
```

Column meanings:
- **Name** — domain-appropriate DB object name (NOT a legacy proc name).
- **Kind** — `view | function | procedure | trigger`.
- **Implements** — the BR-ID whose logic this object carries (blank if it only enforces an invariant).
- **Enforces Invariant** — the invariantId (from Data Invariants) this object enforces (blank if it only
  implements a rule). At least one of Implements / Enforces Invariant MUST be present.
- **Migration Order** — deterministic apply order. Convention: functions=10, views=20, triggers=30,
  procedures=40 (add an intra-kind offset for dependency chains). Tables are always order 0 (base DDL).
- **Binding** — exactly how the application invokes it (repository method → call, read model → view, or
  "trigger — no app call"). The generator uses this verbatim; it must not guess.
- **Placement** — provenance: `P4b:PLACE-<id>` (a placement decision) or `mandatory-db-integrity`
  (a Layer A integrity invariant — not a 4b decision).

**Rule:** every object in this table MUST also appear as executable DDL under `### Core Entities` (or a
clearly labelled migration block), so `backend-service` can emit it. A row here with no DDL is a spec gap.

## Business Rules
### BR-<DOM>-<GRP>: <Name> (N rules)
1. Rule statement
   - Source Reference: <exact file path>:<function/method>:<line number(s)>
   - Discovery Method: Direct Source Read | CAST Imaging
   - CAST Reference: <CAST object ID / transaction path / query> (if applicable)
   - Semantic Preservation:
     | Dimension | Source | Spec | Status |
     |-----------|--------|------|--------|
     | Control-flow | <N> | <N> | OK / GAP |
     | Data-flow | <N> | <N> | OK / GAP |
     | Constants | <N> | <N> | OK / GAP |
     | State transitions | <N> | <N> | OK / GAP |
     | Outcomes | <N> | <N> | OK / GAP |
     | Data writes | <N> | <N> | OK / GAP |
     | Integrations | <N> | <N> | OK / GAP |
     | Error paths | <N> | <N> | OK / GAP |
   - Preservation: OK | FLAGGED (<dims with gaps>) | UNRESOLVED
   - Statement: <Semantic business statement — what the rule MEANS to the business>
   - Intent: Validation | Calculation | Authorization | State Transition | Routing | Compliance
   - Logic: <Implementation-level pseudocode from source — EVIDENCE, not the spec>
   - Data: <dependencies — real table.column names>
   - Side Effects: <what gets written/event published>
   - Extension Point: <EXT-XXX-NNN, if this rule's behavior is parameterized by the extensibility
     engine — see spec/shared/extensibility-model.md. Omit if the rule is not configurable. The rule's
     Logic must call the engine's resolver, NOT hardcode the customized value/behavior.>
   - Concrete Example:
     - Input: <HTTP method + path + JSON body with real domain fields>
     - Success: <HTTP status + response body>
     - Error Input: <JSON that violates this rule>
     - Error Output: <HTTP status + error message>

### Example — BAD (template, unimplementable):
```
### BR-AP-VAL-001: Invoice Header Validation
Statement: Invoice Header requires the mandatory identifiers and effective dates before processing can start.
Input: {"topic": "invoice-header", "requestedAction": "validation"}
Output: {"result": "validated", "outcome": "recorded"}
```
WHY BAD: Statement doesn't say WHAT identifiers. Example uses generic envelope fields. A developer cannot implement this.

### Example — GOOD (domain-specific, implementable):
```
### BR-AP-MATCH-001: Three-Way Match Validation
Statement: An AP invoice can only be approved for payment when the PO quantity, receipt quantity, and invoice quantity match within the configured tolerance percentage. Non-stock service lines are exempt from receipt matching.
Intent: Validation
Source Reference: <source-file> : lines 145-198
Semantic Preservation:
  | Dimension | Source | Spec | Status |
  |-----------|--------|------|--------|
  | Control-flow | 5 | 4 | OK |
  | Data-flow | 4 | 4 | OK |
  | Constants | 1 | 1 | OK (tolerance_percent) |
  | State transitions | 2 | 2 | OK (APPROVE/HOLD) |
  | Outcomes | 2 | 2 | OK |
  | Data writes | 1 | 1 | OK |
  | Integrations | 0 | 0 | OK |
  | Error paths | 1 | 1 | OK |
Preservation: OK
Logic:
  IF line_type = 'SERVICE' AND NOT requires_receipt THEN APPROVE (exempt)
  ELSE
    variance = ABS(receipt_qty - invoice_qty) / po_qty
    IF variance <= tolerance_percent THEN APPROVE
    ELSE HOLD with reason "Receipt variance exceeds tolerance"
Concrete Example:
  Input: POST /api/v1/ap/invoices/INV-001/validate {"poNumber": "PO-5001", "lines": [{"lineNo": 1, "qty": 100, "unitPrice": 25.00}]}
  Context: PO qty = 100, Receipt qty = 95, Tolerance = 5%
  Success: 200 {"status": "approved", "matchResult": "within_tolerance", "variance": "5%"}
  Error: 422 {"status": "held", "reason": "Receipt variance 12% exceeds tolerance 5%", "holdCode": "MATCH_FAILED"}
```
WHY GOOD: Statement declares the business constraint with enough detail to implement. Example uses real domain fields. A developer can write code and tests from this.

## API Endpoints
| Method | Endpoint | Description |

## Events Published
| Event | Trigger | Consumers |

## Events Consumed
| Event | Source | Action |

## Dependencies
- Upstream: <services>
- Downstream: <services>
- External: <databases, messaging, caches>

## Non-Functional Requirements
| Requirement | Target |

## Automation Assessment
| Component | Before | After | Delta |
```

---

## Acceptance Criteria for Spec Completeness

- [ ] All BR-IDs numbered uniquely
- [ ] Every rule has source reference
- [ ] DDL is executable SQL
- [ ] Entity State Model present for every entity with a lifecycle, and each machine is closed (Layer A)
- [ ] Data Invariants listed with enforcement tier; integrity invariants are db/both (Layer A)
- [ ] API endpoints cover all CRUD + business operations
- [ ] Events cross-referenced with consumer/producer specs
- [ ] Workflows documented in `07-workflows.md` (Phase 4 Stage 1.6 — every state-changing BR-ID in at least one workflow)
- [ ] Test assertions designed per rule for the xUnit + .NET Aspire integration suite (`sourcecode/Shopizer.IntegrationTests/<Service>ComprehensiveTests.cs`)
- [ ] DTOs generated in `08-dtos/` (Phase 4c Stage 0 — after tech stack confirmed)
  - `Requests.*` — all request DTOs for this service's endpoints
  - `Responses.*` — all response DTOs for this service's endpoints
  - `Enums.*` — shared enums, status codes, and constants referenced by request/response types
  - All types in `04-api-contract.yaml` schemas MUST have a corresponding DTO or Enum entry
  - **Computed-field provenance:** any response field whose value is CALCULATED (not stored
    directly) MUST be annotated with its source expression (e.g., `// computed: SUM(lines.amount)`).
    This makes a hardcoded `0`/placeholder a visible spec violation the generator must not ship.
- [ ] Human reviewed and signed off
