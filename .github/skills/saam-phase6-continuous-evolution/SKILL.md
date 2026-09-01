---
name: saam-phase6-continuous-evolution
description: "Post-modernization feedback loop for continuous evolution, bug resolution, and specification updates."
copyright: "Copyright 2024-2026 SoftServe Inc. All rights reserved."
authors: "Max Kozinenko, Roman Kalita (SoftServe)"
---

# SAAM Phase 6: Continuous Evolution

## Objective

Maintain the modernized system through a spec-driven feedback loop. Deviations from Phase 5 validation, bug reports, feature requests, and BA-resolved ambiguities all flow through the same pipeline: update spec → update tests → implement → validate. SAAM becomes an ongoing development methodology, not a one-shot modernization.

## Required Steering Files (Read Before Proceeding)

The agent MUST read the following steering files before executing Phase 6 work:

1. **`.github/skills/saam-human-guidance-protocol/SKILL.md`** — Prompt categories, decision register format
2. **`.github/skills/saam-task-tracking/SKILL.md`** — Tracking file format and Jira dual-write protocol
3. **`.github/skills/saam-api-contract/SKILL.md`** — API contract protocol (any spec change may require contract update)
4. **`.github/skills/saam-test-suite-template/SKILL.md`** — Test assertion format (for test updates)
5. **`.github/skills/saam-phase5-ai-dlc-implementation/SKILL.md`** — Implementation protocol (for code changes)
6. **`.github/skills/saam-governance/SKILL.md`** — Invisible governance model (drift detection drives proportional controls)

## When Phase 6 Activates

Phase 6 begins automatically after Phase 5 completes (all services passing validation). It remains active for the lifetime of the system. Any of these inputs triggers a Phase 6 iteration:

| Input Type | Source | Example |
|------------|--------|---------|
| **DEV-TEST** (spec deviation) | `validation/spec-deviation-log.md` | "DELETE returns 200, contract says 204" |
| **Bug report** | Human / production monitoring | "Payment calculation rounds incorrectly for JPY" |
| **Feature request** | Human / product backlog | "Add bulk order import via CSV" |
| **SPEC-DRIFT resolution** | BA / human decision | "BA confirmed: old rate table is correct, implementation was wrong" |
| **Dependency update** | Upstream service change | "Auth service changed token format" |
| **Spec drift** | `spec_drift.py` detection | "BR-PA-CAL-007 spec changed since implementation — code may be stale" |
| **Orphaned capability** | Phase 5 fidelity report / QC / reachability audit | "The service method that adds a line to a batch is fully implemented but no endpoint reaches it" |

### Gap classification: absent vs skeleton vs orphaned (size the work correctly)

When QC or a fidelity report reports "this feature doesn't work," the cost to fix depends entirely
on which of three states the underlying capability is in. Classify before estimating — conflating
them is how work gets mis-sized:

| Gap class | What is true | Cost | Fix path |
|-----------|--------------|------|----------|
| **Absent** | No implementing method exists at all | High | Spec-first: define/confirm the workflow recipe, add BR-IDs, generate tests, implement |
| **Skeleton** | Method exists and is reachable, but performs no real effect (returns a shaped placeholder) | Medium | Implement the effect the recipe names; assert the behavioral round-trip |
| **Orphaned capability** | Method exists and performs the real effect, but no route reaches it | Low | Wire an endpoint to it; assert the behavioral round-trip. Do NOT re-implement — the logic is done |
| **False flag** | Method is actually reachable, via a surface the reachability audit doesn't model (queue consumer, scheduled/batch job, or a language outside the scan set) | None | Not a gap. Confirm the entry surface; extend the audit's route tokens for that stack |

**Orchestrator proposes, operator confirms.** The orchestrator classifies each reported gap (from the
graph's `reachable` flag + inspecting the method against the spec's named effect) and proposes absent /
skeleton / orphaned / false-flag with evidence. The human confirms — the orphaned-vs-absent and
false-flag calls in particular are the judgments that go wrong on a thin or not-yet-current graph, so
this is a touchpoint, not an auto-decision.

**Preconditions (two):**
1. Classification reads `reachable` from the graph — it assumes the graph is a current from-disk
   projection (see the reconcile discipline in `.github/skills/saam-phase5-ai-dlc-implementation/SKILL.md` → Knowledge Graph
   Population). If in doubt the graph is current, reconcile first; a stale graph invents orphans and
   hides real ones.
2. The reachability audit (`fidelity_audit.py`) is an HTTP-shaped heuristic — it does not recognize
   non-HTTP entry surfaces (message-queue consumers, scheduled/batch jobs) and skips languages outside
   its scan set. For such services, false-flag results are expected until the audit's route tokens are
   extended for that stack. This is a second reason the classification is operator-confirmed.

## Continuous Loop

```mermaid
flowchart TD
    subgraph Inputs["Inputs"]
        DEV[DEV-TEST items]
        BUG[Bug reports]
        FEAT[Feature requests]
        DRIFT[SPEC-DRIFT resolutions]
        DEP[Dependency updates]
    end

    subgraph Classify["Classify and Route"]
        DEV --> ROUTE{Needs spec change?}
        BUG --> ROUTE
        FEAT --> ROUTE
        DRIFT --> ROUTE
        DEP --> ROUTE
    end

    ROUTE -->|No| FIXONLY[Fix code only]
    ROUTE -->|Yes| SPECUPDATE[Update spec and contract]

    SPECUPDATE --> TESTUPDATE[Update test suite]
    TESTUPDATE --> IMPLEMENT[Implement change]
    FIXONLY --> IMPLEMENT

    IMPLEMENT --> VALIDATE[Run validation]
    VALIDATE -->|Pass| DONE[Done - update tracking]
    VALIDATE -->|Fail| IMPLEMENT
    DONE --> WAIT[Wait for next input]
    WAIT --> Inputs
```

## Routing Rules (Which Path Through the Loop)

| Input Type | Spec Update? | Contract Update? | Test Update? | Implementation |
|------------|-------------|-----------------|--------------|----------------|
| DEV-TEST (code doesn't match spec) | No | No | No | Fix code to match existing spec/contract |
| Bug (spec was correct, code is wrong) | No | No | Maybe (add regression test) | Fix code |
| Bug (spec was wrong) | Yes (fix BR-ID) | Maybe | Yes (fix assertion) | Fix code |
| Feature request (new capability) | Yes (new BR-IDs) | Yes (new endpoints/fields) | Yes (new assertions) | New code |
| SPEC-DRIFT (BA chose spec is correct) | No | No | No | Fix code to match spec |
| SPEC-DRIFT (BA chose implementation is correct) | Yes (update spec) | Maybe | Yes (update assertions) | No code change |
| Dependency update | Maybe | Maybe | Maybe | Update integration code |
| Spec drift (auto-detected) | No (spec already changed) | No | No | Update code to match new spec, then re-validate |
| State machine change (Layer A: new/removed state or transition) | Yes (02 Entity State Model) | Maybe | Yes (illegal-transition + guard tests) | Update transition enforcement; re-verify closure |
| Invariant change (Layer A: new/changed constraint or tier) | Yes (02 Data Invariants) | No | Yes (invariant-holds test) | Enforce per tier; if db/both, regenerate the DB object |
| Placement / DB-object change (Layer C) | Yes (02 Database Logic Objects) | Maybe | Yes (DB-tier object tests) | Regenerate the ordered migration; rebind the app caller |
| Extension-point change (Layer B: reproduce/unify/drop) | Yes (extensibility-model + BR annotation) | Maybe | Yes (extension-point tests) | Wire/unwire the resolver; never hardcode a configurable value |
| Orphaned capability (real method, no route — operator-confirmed) | No (logic already matches spec) | Maybe (add the endpoint to the contract) | Yes (behavioral round-trip for the newly-reachable path) | Wire a route to the existing method; do NOT re-implement the logic |

## Iteration Protocol

### Step 1: Receive and Classify Input

When new work arrives (from deviation log, human, or monitoring):

1. Identify the input type (DEV-TEST / Bug / Feature / SPEC-DRIFT / Dependency / Orphaned capability). For a reported "feature doesn't work," first run the absent / skeleton / orphaned / false-flag classification above (orchestrator proposes, operator confirms) — the class determines the path and the cost.
2. Identify affected service(s)
3. Determine the path through the loop (using Routing Rules table above)
4. Create tracking entry in `tracking/phase6-evolution.md`
5. If Jira configured: create ticket

### Step 2: Update Spec (if needed)

For inputs requiring spec changes:

1. Read the relevant service spec (`spec/microservices/<service>/01-business-rules.md`)
2. Add new BR-IDs (feature request) or update existing ones (bug fix, SPEC-DRIFT)
3. Update `02-domain-model.md` if data model changes — INCLUDING the implicit-system sections:
   - **Entity State Model (Layer A):** if a state or transition changes, edit the states/transitions
     table and **re-verify closure** (every state reachable, non-terminal states have an exit, no
     transition targets an undeclared state — the same check Phase 4 Validator #9 and the
     `STATE_MACHINE_NOT_CLOSED` gate enforce). A non-closed machine blocks the service.
   - **Data Invariants (Layer A):** if an invariant or its tier changes, update the row. Integrity
     invariants stay `db`/`both`. If the tier is `db`/`both`, ensure a `DbObject` enforces it (else the
     `MANDATORY_DB_OBJECT_MISSING` gate blocks) — regenerate the DB object as an ordered migration.
   - **Database Logic Objects (Layer C):** if a db-object or placement changes, update the row + its
     backing DDL and regenerate the ordered migration; rebind the app caller. If a placement flips
     app↔db, run it through the Phase 4b Placement Review lens (evidence + decision) before changing tier.
   - **Extension points (Layer B):** a reproduce/unify/drop change updates `spec/shared/extensibility-model.md`
     and the affected BR-IDs' `Extension Point:` annotations; the rule must call the resolver, not hardcode.
4. Update `03-api-design.md` if endpoint changes
5. **Update `04-api-contract.yaml`** — this is the naming authority; any field/path/status change MUST be reflected here
6. Update `06-completion-summary.md` with new counts
7. **Re-baseline spec hashes:** After any spec edit, run `python3 graph-mcp/scripts/spec_drift.py --service <service> --update` to stamp new hashes. This re-baselines BOTH the BR-ID hashes AND the implicit-layer hashes (state machines, invariants, db-objects), preventing false drift detections on the next validation run.

**Feature requests get full BR-ID treatment:**
- Numbered BR-ID with semantic statement
- Intent classification
- Logic section
- Concrete examples (input/output)
- Data dependencies

**Bug fixes get minimal spec update:**
- Correct the affected BR-ID's Statement or Logic
- Mark as `[Corrected in Phase 6 — <date>]`

### Step 3: Update Tests (if needed)

For inputs requiring test changes:

1. Read the current `validation/<service>/comprehensive-test-suite.sh`
2. Read `04-api-contract.yaml` for field names (ALWAYS — even for small changes)
3. Add new assertions (feature) or fix existing ones (spec correction)
4. Follow all rules from `.github/skills/saam-test-suite-template/SKILL.md` (extract_field, global headers, etc.)
5. Verify the test suite still runs (syntax check)

**For new features:** Add test section with header comment referencing the new BR-IDs.
**For bug fixes:** Add a regression test assertion that specifically covers the bug scenario.
**For DEV-TEST items:** No test changes — the test is already correct; the code needs to change.

### Step 4: Implement Change

Route to AI-DLC (GitHub Copilot) for implementation:

**For DEV-TEST / Bug fixes (code-only):**
1. Read the relevant BR-ID + API contract
2. Fix the specific code issue
3. Run only the affected test assertions to verify
4. Log in deviation log as resolved (DEV-CODE)

**For feature requests (new code):**
1. Read the new BR-IDs from updated spec
2. Read the updated API contract
3. Implement following the same protocol as Phase 5 Step 3 (per-layer, spec-driven, no stubs)
4. Run the full test suite for the affected service

**For SPEC-DRIFT (BA chose spec is correct):**
1. This is equivalent to DEV-TEST — fix code to match spec
2. Remove the SPEC-DRIFT entry from deviation log, add as DEV-CODE (resolved)

**For SPEC-DRIFT (BA chose implementation is correct):**
1. Spec was already updated in Step 2
2. Test was already updated in Step 3
3. No code change needed — just verify tests pass with current code

### Step 5: Validate

Run the comprehensive test suite for the affected service using the reconciliation pipeline:

```bash
./validation/run-and-reconcile.sh <service-name> phase6_evolution
```

This automatically:
- Runs the comprehensive test suite
- Produces a validation artifact in `.saam/reconciliation/<service>/`
- Updates the graph (lifecycle states, VALIDATED_BY edges, confidence)
- Checks for spec drift (flags SPEC_DRIFT deviations if spec changed since implementation)
- Generates/updates `.github/specs/<service>/tasks.md` if failures remain

**Governance enforcement (automatic — see `.github/skills/saam-governance/SKILL.md`):**
- If the change touched BR-ID code and spec hashes match → validated automatically
- If spec drift is detected → SPEC_DRIFT deviation created, must be resolved
- If Critical BR-ID is affected + drift detected → human approval required before marking done

- If 100% pass AND no drift → mark iteration as DONE
- If failures remain → return to Step 4 (fix implementation)
- Log any new deviations discovered during this validation in the deviation log

### Step 6: Update Tracking

After successful validation:
1. Update `tracking/phase6-evolution.md` — mark item DONE
2. Update `validation/spec-deviation-log.md` — move resolved items to "Resolved" section
3. Update root `README.md` — increment evolution cycle count
4. If Jira configured: transition ticket to Done (or In Review for human PR review)
5. **Graph update (always):** Use `graph_update_node` to update resolved deviations, add new BR-IDs (features), update lifecycle states. Run `graph_run_inferences(rules=["lifecycle_states", "effective_confidence"])`.
6. **If CAST is configured (additional):** Run Query 4 (Unaccounted Loss) periodically (every 5 iterations or after major feature additions) to verify no drift from legacy behavior.

## Batch Processing

Multiple items can be processed in one iteration if they affect the same service:

1. Collect all pending items for a service
2. Apply ALL spec updates at once
3. Apply ALL test updates at once
4. Implement ALL changes at once
5. Validate once (full suite)

This is more efficient than processing items one at a time.

## Systemic-First Remediation (MANDATORY — Initial P6 Activation)

When Phase 6 first activates after Phase 5 completion, there is typically a batch of DEV-TEST items (integration test failures from the initial validation sweep). The agent MUST NOT fix these per-service. Instead:

**Protocol (systemic patterns FIRST, per-service SECOND):**

1. **Run ALL test suites across ALL services** (or read existing results if just captured):
   - Capture: service name, test number, failure reason, expected vs actual

2. **Catalog ALL failures into patterns** — group by root cause, not by service:
   ```
   Pattern: "ValidationPipe returns 400, contract expects 422"
   Affected: 12/12 services
   Fix: Add ValidationPipe with {errorHttpStatusCode: 422} to all services

   Pattern: "Auth guard rejects test JWT token"
   Affected: 8/12 services
   Fix: Add test-mode bypass using spec/test-config.yaml JWT secret

   Pattern: "camelCase field names expected, snake_case returned"
   Affected: 3/12 services
   Fix: Add ClassSerializerInterceptor with camelCase transform
   ```

3. **Fix patterns in descending frequency** — the pattern affecting the most services first:
   - Apply fix to ALL affected services at once (not one at a time)
   - Re-run the affected test assertions across all services
   - Confirm pattern is resolved before moving to next pattern

4. **After ALL systemic patterns are resolved** — proceed to per-service fixes:
   - Now remaining failures are service-specific logic issues
   - Process per-service using normal Phase 6 iteration protocol (Steps 1-6 above)
   - Process in priority order (highest integration %, most business-critical first)

**Why systemic-first:** In the first pilot engagement, 60%+ of integration failures were caused by 4 systemic issues (ValidationPipe, auth tokens, URL prefixes, field casing). Fixing these 4 patterns resolved ~400 tests across 12 services in ~30 min. Fixing per-service would have meant discovering and fixing the same pattern 12 separate times.

**Threshold for "systemic":** A failure pattern is systemic if it appears in ≥3 services. Below that, it's per-service.

**When this section does NOT apply:** If Phase 6 activates for feature requests or bugs (not initial validation), skip directly to the normal iteration protocol.

## Priority Order

When multiple items are pending, process in this order:

1. **SPEC-DRIFT** (BA decisions) — unblock ambiguities first
2. **DEV-TEST** (deviations) — align code with spec (these are known issues, easy wins)
3. **Bugs** — production issues
4. **Dependency updates** — upstream changes that may break integration
5. **Feature requests** — new capabilities (lowest priority, highest effort)

## Remediation Stop Conditions (Prevents Unbounded Loops)

Phase 6 can theoretically loop forever (fix → test → fail → fix again). These stop conditions prevent unbounded remediation:

**Per-BR-ID:**
- **Max 3 remediation attempts** per BR-ID. If a specific rule fails validation 3 times with different fixes attempted, STOP and escalate to human.
- After 3 attempts, mark the BR-ID as `remediation_stalled` in tracking and present to human: "BR-XX-YYY-NNN has failed 3 fix attempts. Options: (a) accept deviation as permanent, (b) reclassify the rule, (c) rewrite spec and re-extract, (d) human fixes manually."

**Per-service:**
- **Max 5 remediation cycles** per service per session. If a service goes through validate→fix 5 times without reaching 100%, STOP and report progress + remaining failures to human.
- This prevents the "fix one thing, break another" oscillation.

**Per-session:**
- **Integration pass rate must INCREASE** between cycles. If a cycle produces zero improvement (same or worse pass rate), STOP immediately — the approach is wrong.
- Switch to: re-read the spec for the failing rules, understand WHY the fix isn't working, try a fundamentally different approach OR escalate.

**Structural (implicit-layer) failures:**
- A `STATE_MACHINE_NOT_CLOSED` or `MANDATORY_DB_OBJECT_MISSING` gate is a SERVICE-level blocker, not a
  per-BR one — it does not clear by fixing individual rules. Treat it like a per-service remediation
  cycle: fix the state model / add the DB object, re-run inferences (`graph_run_inferences`), confirm the
  gate clears. If it survives 3 structural fix attempts, escalate to human (the model itself is wrong).

**Permanent deviation path:**
- A human can mark any deviation as `ACCEPTED` (won't be fixed). This is legitimate for: rules that are intentionally simplified in the target, features deferred to a later phase, edge cases accepted as out-of-scope.
- Accepted deviations are logged in the graph with `status: ACCEPTED` and a rationale. They don't count toward pass rate.

## Relationship to Phase 5

Phase 6 uses the SAME implementation protocols as Phase 5:
- Same spec-driven approach (read spec, implement, validate)
- Same API contract enforcement (contract is naming authority)
- Same test modification policy (deviation log for any test changes)
- Same tracking model (append-only log for implementation work)

The difference: Phase 5 builds from scratch. Phase 6 modifies existing code. All Phase 6 work is brownfield.

## System Integration Validation (MANDATORY — After Per-Service Tests Pass)

Per-service comprehensive tests validate **vertical correctness** (each service's logic works). But they DON'T validate that the services work together as a system. This stage bridges that gap.

**When to run:** After ALL backend services reach 95%+ confidence (per-service tests passing). Before declaring the system ready for UAT or production.

### What Per-Service Tests DON'T Cover

| Gap | Example | Why Tests Miss It |
|-----|---------|-------------------|
| Inter-service auth | Gateway validates JWT issued by identity-service | Tests use pre-generated tokens, never test the issuance→validation chain |
| Event consumption | team.created → notification-service sends welcome email | Tests mock events; never verify real RabbitMQ routing |
| Cross-service data | Judging reads teams from team-service API | Tests seed their own data; never test real inter-service HTTP calls |
| Compose networking | Services reference each other by container hostname | Tests run against localhost; compose uses Docker DNS |
| Bootstrap state | App needs admin account + season config to be usable | Tests create their own state; real app starts empty |
| Gateway routing | Frontend → gateway → correct backend service | Tests hit backends directly; gateway routing never exercised |

### System Integration Protocol

**Step 1: Bootstrap Data Script**

Generate `sourcecode/scripts/bootstrap.sh` — seeds minimum viable data for the system to be usable:

```bash
#!/bin/bash
# Bootstrap: create minimum data for the system to function
# Run ONCE after compose up, before first use

BASE_URL="${GATEWAY_URL:-http://localhost:3000}"

# 1. Create admin account (identity-service)
curl -s -X POST "$BASE_URL/api/v1/identity/internal/bootstrap" \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@system.local","password":"<from-test-config>","role":"admin"}'

# 2. Create current season (admin-service)
curl -s -X POST "$BASE_URL/api/v1/admin/seasons" \
  -H "Authorization: Bearer <admin-token>" \
  -H "Content-Type: application/json" \
  -d '{"name":"2026","status":"active","registrationOpen":true}'

# 3. Seed any required config (toggles, rubric, divisions)
# ... (derived from spec business rules that define "system requires X to exist")
```

**The bootstrap script is generated from:**
- Business rules that reference "default" or "required" state (e.g., "system must have an active season")
- Admin-service toggle defaults
- Any config that integration tests seed manually but the real app needs permanently

**Step 2: Compose Health Verification**

After `podman compose up`, verify ALL services are healthy and can reach each other:

```bash
#!/bin/bash
# sourcecode/scripts/verify-system.sh

SERVICES=(gateway identity-service team-service submission-service ...)

echo "=== Waiting for all services ==="
for svc in "${SERVICES[@]}"; do
  PORT=$(grep "${svc}:" compose.yaml -A5 | grep "ports:" -A1 | grep -oE '[0-9]+:' | head -1 | tr -d ':')
  for i in $(seq 1 30); do
    curl -sf "http://localhost:${PORT}/health" >/dev/null && break
    sleep 1
  done
  echo "  $svc: $(curl -sf http://localhost:${PORT}/health | head -c 50)"
done

echo "=== Inter-service connectivity ==="
# Gateway can reach identity-service (JWT key fetch)
curl -sf "http://localhost:3000/health/dependencies" || echo "WARN: gateway deps check failed"
```

**Step 3: User Journey Tests (Multi-Service)**

Generate `validation/system/user-journey-tests.sh` — tests that exercise the FULL request chain across multiple services.

**Derivation rule:** User journey tests MUST be derived from `spec/07-cross-service-workflows.md`. Each cross-service workflow with a user trigger = one journey test. Do NOT invent test scenarios — use the authoritative workflow sequences.

```bash
#!/bin/bash
# System-level user journey tests
# These test HORIZONTAL correctness: services working together

GATEWAY="http://localhost:3000"
PASSED=0; FAILED=0; TOTAL=0

# Journey 1: Registration → Login → Get Profile
echo "=== Journey 1: Auth Flow ==="
# Register via gateway → identity-service
REG_RESULT=$(curl -s -X POST "$GATEWAY/api/v1/identity/auth/register" \
  -H "Content-Type: application/json" \
  -d '{"email":"test-journey@test.local","password":"Test1234!","displayName":"Journey Test"}')
# Login → get token
TOKEN=$(curl -s -X POST "$GATEWAY/api/v1/identity/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"test-journey@test.local","password":"Test1234!"}' | python3 -c "import sys,json; print(json.load(sys.stdin)['accessToken'])")
# Use token on another service
PROFILE=$(curl -s "$GATEWAY/api/v1/identity/account/profile" \
  -H "Authorization: Bearer $TOKEN")
# Verify: token issued by identity-service works on gateway-protected routes

# Journey 2: Create Team → Invite → Accept (cross-service state)
echo "=== Journey 2: Team Creation Flow ==="
# ... tests team-service creates team, notification-service sends invite email

# Journey 3: Submit → Judge → Score (event-driven chain)
echo "=== Journey 3: Submission Flow ==="
# ... tests submission-service → event → judging-service picks up

# Journey 4: Admin → Toggle Season → Verify cascading effect
echo "=== Journey 4: Admin Operations ==="
# ... tests admin toggles affect team-service + submission-service behavior
```

**Journey test design principles:**
- Each journey crosses ≥2 services
- Tests use the GATEWAY (not direct service ports) — exercises routing
- Auth tokens are obtained via login (not pre-generated) — exercises the full auth chain
- Events are verified by polling the consumer (not just publishing) — exercises async flow
- Tests run IN ORDER (journey 1 creates data that journey 2 uses) — exercises data persistence

**Step 4: Event Flow Verification**

For each event in `spec/microservices/*/05-dependencies.md`:
- Publish the event from the producer service
- Wait (poll) for the consumer to process it
- Verify the consumer's state changed

```bash
# Example: team.created → notification-service sends welcome
# 1. Create a team (produces team.created event)
curl -s -X POST "$GATEWAY/api/v1/teams" -H "Authorization: Bearer $TOKEN" ...
# 2. Poll notification-service for the welcome notification (consumer processed event)
for i in $(seq 1 10); do
  NOTIF=$(curl -s "$GATEWAY/api/v1/notifications?type=team_welcome" -H "Authorization: Bearer $TOKEN")
  echo "$NOTIF" | grep -q "team_welcome" && break
  sleep 2
done
# 3. Assert: notification exists (event was consumed)
```

**Step 5: Compose Networking Configuration**

During Phase 5 setup, `sourcecode/compose.yaml` must include inter-service environment variables so services can find each other:

```yaml
services:
  identity-service:
    environment:
      DATABASE_URL: "postgresql://saam:saam_local@postgres:5432/saam_dev"
      REDIS_URL: "redis://redis:6379"
      # No inter-service deps for identity
      
  gateway:
    environment:
      IDENTITY_SERVICE_URL: "http://identity-service:3001"
      # Gateway needs to reach identity for JWT key
      
  team-service:
    environment:
      DATABASE_URL: "postgresql://saam:saam_local@postgres:5432/saam_dev"
      RABBITMQ_URL: "amqp://saam:saam_local@rabbitmq:5672"
      IDENTITY_SERVICE_URL: "http://identity-service:3001"
      # Team-service calls identity for user lookup
      
  notification-service:
    environment:
      RABBITMQ_URL: "amqp://saam:saam_local@rabbitmq:5672"
      # Notification consumes events from RabbitMQ
```

**This is generated from `05-dependencies.md`** — each service's dependencies tell us what environment variables it needs to reach other services.

### When System Integration Fails

Failures at this stage are typically:
- **Compose networking** — service can't reach another (fix: env var with container hostname)
- **Event routing** — producer publishes to wrong exchange/queue (fix: RabbitMQ config alignment)
- **Auth chain** — token format mismatch between issuer and validator (fix: shared JWT secret in compose env)
- **Missing bootstrap** — service needs seed data that doesn't exist (fix: add to bootstrap script)
- **Schema mismatch** — consumer expects different event payload than producer sends (fix: align with `05-dependencies.md`)

These are NOT per-service bugs (those are caught by comprehensive tests). They're WIRING issues — the plumbing between services.

### Deliverables

- [ ] `sourcecode/scripts/bootstrap.sh` — seed minimum viable data
- [ ] `sourcecode/scripts/verify-system.sh` — health + connectivity check
- [ ] `validation/system/user-journey-tests.sh` — multi-service journey tests
- [ ] `sourcecode/compose.yaml` updated with inter-service env vars (from `05-dependencies.md`)
- [ ] Event flow verification (poll-based, not mock-based)

### Relationship to CI/CD

In CI, the system integration test runs AFTER per-service tests pass:

```
per-service tests (parallel) → all pass → compose up → bootstrap → system integration → all pass → deploy
```

## Deviation Log Lifecycle

The spec deviation log has a full lifecycle across Phases 5 and 6:

```
Phase 5 (initial implementation):
  DEV-TEST items created → code adapted to pass tests
  
Phase 6 (continuous evolution):
  DEV-TEST items received as inputs → code fixed to match spec
  Items resolved → moved to "Resolved" section with date
  New items may be discovered → added to log
  
Steady state:
  Zero DEV-TEST items = full spec compliance
  SPEC-DRIFT items = zero (all BA decisions made)
  Only inputs are bugs + features (normal development)
```

## Task Tracking

Phase 6 uses a single tracking file: `tracking/phase6-evolution.md`

```markdown
# Phase 6: Continuous Evolution — Task Tracker

## Status: ACTIVE (ongoing)

## Summary
| Metric | Value |
|--------|-------|
| Total iterations | <N> |
| Items resolved | <N> |
| Items pending | <N> |
| Last iteration | <date> |

## Pending Items

| # | Type | Service | Description | Source | Priority | Status |
|---|------|---------|-------------|--------|----------|--------|
| 1 | DEV-TEST | catalog-service | DELETE returns 200, should be 204 | deviation-log#CAT-04 | High | PENDING |
| 2 | FEATURE | order-service | Add bulk CSV import | Product backlog | Medium | PENDING |
| 3 | BUG | payment-service | JPY rounding error | Production alert | Critical | IN_PROGRESS |

## Completed Items

| # | Type | Service | Description | Resolved | Iteration |
|---|------|---------|-------------|----------|-----------|
| ... | | | | | |
```

## Exit Condition

Phase 6 does not have a traditional exit gate — it's a continuous loop. However, the engagement may formally end when:

- All DEV-TEST items are resolved (full spec compliance)
- All SPEC-DRIFT items have BA decisions applied
- The system is handed off to a maintenance team
- The client decides to pause evolution

At that point, produce a final status report:

**🟢 NOTIFICATION**: "Phase 6 status: [N] iterations completed. [X] deviations resolved. [Y] features added. [Z] bugs fixed. System is spec-compliant. No pending items."
