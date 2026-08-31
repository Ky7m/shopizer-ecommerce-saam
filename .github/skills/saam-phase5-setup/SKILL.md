---
name: saam-phase5-setup
description: "Setup wizard for Phase 5 implementation, runtime parameter collection, and environment configuration."
copyright: "Copyright 2024-2026 SoftServe Inc. All rights reserved."
authors: "Max Kozinenko, Roman Kalita (SoftServe)"
---

# SAAM Phase 5: Setup & Model Selection

## Purpose

This steering file guides the agent through the Phase 5 setup process. When the user asks to start Phase 5, the agent MUST follow this workflow BEFORE writing any code. The agent selects an execution model, gathers required parameters, and creates all configuration artifacts needed to begin implementation.

## Required Steering Files (Read Before Proceeding)

The agent MUST read the following steering files before executing Phase 5 Setup:

1. **`saam-human-guidance-protocol.md`** — Prompt categories, decision register format, agent rules
2. **`saam-task-tracking.md`** — Tracking file format and Jira dual-write protocol
3. **`saam-api-contract.md`** — API contract protocol (needed for TD materials that reference the contract as naming authority)
4. **`saam-jira-integration.md`** — (Only if Jira is configured) Jira ticket structure and ATX skill configuration

## Task Tracking Activation

At the START of Phase 5, the agent MUST create `tracking/phase5-setup.md` (if it doesn't exist) with setup tasks listed as PENDING. If Jira is configured, create an Epic with Tasks. See `saam-task-tracking.md` for format.

**PhaseEvent (telemetry timestamp):** Immediately after creating the tracking file, write: `graph_add_node(nodeType="PhaseEvent", id="P5-started", properties={phase: "P5", event: "started", timestamp: <current ISO timestamp>})`.

## Entry Precondition Checks

Before proceeding with setup, verify:

1. **Specs exist:** `spec/microservices/` contains at least one service directory with `01-business-rules.md`
2. **API contracts exist:** Each service has `04-api-contract.yaml` (if missing, this is a Phase 4 gap — offer to run Phase 4's API contract generation step now per `saam-api-contract.md`. The contract should NEVER have been deferred past Phase 4.)
3. **DTOs exist:** Each service has `spec/microservices/<service>/08-dtos/` with target-language DTO files (if missing, offer to run Phase 4c Stage 0 per `saam-phase4c-test-suite-generation.md`). **These DTOs are the concrete binding that prevents naming drift between tests and implementation.**
4. **Test suites exist:** `validation/<service>/comprehensive-test-suite.sh` exists per service (if missing, offer to run Phase 4c first per `saam-phase4c-test-suite-generation.md`)
5. **Modernization artifacts exist:** `modernization/implementation-roadmap.md` exists (if missing, warn that Phase 4b was skipped)

If any critical precondition fails, inform the user and offer to resolve before continuing.

## Step 1: Model Selection (Interactive)

Present the three models and ask the user to choose:

**🔴 PROMPT HUMAN:**

"Phase 5 is ready to begin. You have [N] services to implement. Choose an execution model:

**Model A: Pure Kiro (Interactive)**
- I implement each service task by task, you review PRs
- Best for: 1-3 services, complex custom logic, learning the codebase
- Timeline: ~1-2 weeks per service (sequential)
- Requirements: Just this workspace + Kiro
- Sub-options:
  - **A-spec:** Generate Kiro spec files (requirements.md/design.md/tasks.md) — visible task progress in UI
  - **A-direct:** Implement directly from SAAM specs via subagent delegation — faster, fewer tokens, same outcome

**Model B: Transform + Kiro (Semi-Automated)**
- AWS Transform generates ~80% of each service, then I fix/extend via Kiro tasks
- Best for: 3-10 services, well-defined specs with clear boundaries
- Timeline: ~3-5 days per service
- Requirements: ATX CLI installed, TD published

**Model C: ATX Batch + AI-DLC Pipeline (Maximum Velocity)**
- ATX generates ALL services in parallel (30-60 min total), then AI-DLC handles cross-cutting wiring
- Best for: 5+ services, maximum speed, team environment
- Timeline: ~2-5 days for ALL services combined
- Requirements: ATX scaled platform deployed, AI-DLC rules installed

Which model?"

Wait for the user's response. Then proceed to the corresponding setup section.

---

## Model A Setup: Pure Kiro

### Sub-Mode Selection

If human chose Model A, ask:

> "Model A has two sub-modes:
> - **A-spec:** I generate Kiro spec files (requirements.md, design.md, tasks.md) for each service. You see task progress in the Kiro UI. Good for learning the codebase or when you want visible step-by-step progress.
> - **A-direct:** I implement directly from SAAM specs (01-business-rules.md, 02-domain-model.md, 04-api-contract.yaml) via subagent delegation. No intermediate format. Faster, uses fewer tokens. Progress tracked via reconciliation pipeline + signalStatus.
>
> Which sub-mode?"

### Parameters Needed

None required beyond what's already in the workspace. The agent confirms:
- Target technology stack (from Phase 4b tech stack recommendation)
- Service implementation order (from `modernization/implementation-roadmap.md`)

### Model A-spec: Artifacts Created

The agent creates:

1. **`.kiro/specs/<first-service>/`** — Kiro spec (requirements.md, design.md, tasks.md) for the first service in dependency order
2. **`tracking/phase5-implementation/<first-service>.md`** — tracking file for the first service

**Confirmation:** "Model A-spec is set up. Starting with `<first-service>` (priority [N] in the roadmap). Kiro spec generated at `.kiro/specs/<service>/tasks.md` with [N] tasks. Ready to begin Task 1?"

### Model A-direct: Artifacts Created

The agent creates:

1. **`tracking/phase5-implementation/<first-service>.md`** — tracking file for the first service
2. No `.kiro/specs/` files generated — implementation is driven directly from SAAM specs

**Confirmation:** "Model A-direct is set up. Starting with `<first-service>` (priority [N] in the roadmap). Implementing directly from SAAM specs. Progress tracked via validation artifacts + signalStatus. Ready to begin?"

**A-direct implementation flow:**
```
For each service (sequential, one at a time):
  0. Context sufficiency check (BEFORE delegating):
     - Estimate input size: BR count × ~500 tokens + DDL (~2K) + contract (~3K) + DTOs (~1K)
     - If estimated input > 60K tokens → MUST use Mode 3 (batched), regardless of BR count
     - If estimated input > 40K tokens → MUST use Mode 2 (per-layer) minimum
     - If estimated input ≤ 40K tokens → Mode 1 is safe
     - NEVER start a delegation that will clearly exceed context capacity
     - If in doubt: split. An incomplete output from a context-exhausted subagent wastes more time than two smaller delegations.

  1. Determine delegation granularity based on BR count + context check:
     - ≤ 20 rules AND context check passes → ONE subagent (full service in one shot)
     - 21-40 rules OR context marginal → PER-LAYER delegation (4 subagents sequential)
     - > 40 rules OR context clearly insufficient → PER-LAYER + BR batching (each layer subagent handles 10-15 rules max)
  
  2. Delegate implementation (see delegation modes below)
  3. Parent verifies: code compiles, BR-ID annotations present, field names match contract
  4. Run validation: ./validation/run-and-reconcile.sh <service>
  5. If signalStatus = BLOCKED → subagent fixes from generated tasks.md
  6. Repeat 4-5 until signalStatus = CLEAR or FLAGGED
  7. Post-service checklist (tracking, graph, commit)
  8. Next service
```

**Delegation Mode 1: Full service (≤ 20 rules)**
```
Single subagent implements entire service:
  contextFiles: saam-phase5-ai-dlc-implementation.md, saam-api-contract.md
  Input: 01-business-rules.md, 02-domain-model.md, 03-api-design.md, 04-api-contract.yaml, 08-dtos/
  Output: sourcecode/<service>/ (complete project, DTOs copied from 08-dtos/ as first step)
```

**Delegation Mode 2: Per-layer (21-40 rules)**
```
4 subagents, sequential — each adds to what the previous produced:

  Subagent 1 — Scaffold + DTOs + Domain + Repository:
    Input: 02-domain-model.md, 04-api-contract.yaml, 08-dtos/
    Output: project scaffold, DTOs copied from spec VERBATIM into src/dto/, entities, Prisma/JPA schema, repository interfaces
    (Creates scaffold + copies DTOs + data layer — no business logic yet)

  Subagent 2 — Service Layer (business logic):
    Input: 01-business-rules.md, 04-api-contract.yaml + generated entities from step 1
    Output: service classes with BR-ID annotations, validation logic
    (Reads existing entities, implements all BR-IDs)

  Subagent 3 — Controllers:
    Input: 03-api-design.md, 04-api-contract.yaml + copied DTOs + generated services from step 2
    Output: controllers using the COPIED DTOs for request/response types
    (Wires API surface to service layer — uses pre-existing DTOs in src/dto/, NEVER regenerates them)

  Subagent 4 — Events + Integration + Unit Tests:
    Input: 05-dependencies.md, 04-api-contract.yaml + all generated code
    Output: event publishers/consumers, HTTP clients, unit tests
    (Cross-service wiring + basic test coverage)
```

**Delegation Mode 3: Per-layer + BR batching (> 40 rules)**
```
Same as Mode 2, but Subagent 2 (service layer) is split into batches:

  Subagent 2a — Service Layer (BR-IDs 001-015):
    Input: first 15 BR-IDs from 01-business-rules.md + entities
    Output: service methods for those rules

  Subagent 2b — Service Layer (BR-IDs 016-030):
    Input: next 15 BR-IDs + entities + code from 2a
    Output: additional service methods

  Subagent 2c — Service Layer (remaining BR-IDs):
    ...

  Each batch adds to the existing service code without rewriting previous work.
```

**Why per-layer works:** The API contract (`04-api-contract.yaml`) is the naming authority and DTOs (`08-dtos/`) are the concrete binding. Each layer can be implemented independently because:
- DTOs are copied first — controller layer has shapes already defined
- Repository layer derives from DDL (02-domain-model.md) — deterministic
- Service layer derives from BR-IDs (01-business-rules.md) — needs entities but not controllers
- Controller layer derives from contract (04-api-contract.yaml) — needs services but naming is predetermined
- Events derive from dependencies (05-dependencies.md) — independent of internal layers

**Parent responsibilities between layer subagents:**
- Verify previous layer compiled before delegating next
- Pass reference to generated code directory (subagent reads existing sourcecode/)
- Update `IMPLEMENTATION-STATE.md` after each layer completes
- Do NOT re-read entire generated code — just verify it exists

**Intra-Service State Store: `sourcecode/<service>/IMPLEMENTATION-STATE.md`**

For Delegation Modes 2 and 3, the parent MUST maintain an implementation state file inside the service directory. This enables resumption if context compacts mid-service.

**Created:** Before delegating the first layer subagent.
**Updated:** After each layer subagent returns.
**Deleted:** After the service passes validation (signalStatus = CLEAR/FLAGGED) — it's a build artifact, not a permanent file.

```markdown
# <service> — Implementation State

## Service Profile
- BR count: <N>
- Delegation mode: <2 (per-layer) | 3 (per-layer + batched)>
- Target stack: <from tech-stack-recommendation.md>

## Layer Progress
| Layer | Status | BR-IDs Covered | Key Outputs |
|-------|--------|----------------|-------------|
| 1. Domain + Repository | DONE | — | <N> entities, <N> repos |
| 2a. Service (BR-001–015) | DONE | BR-XX-001 through BR-XX-015 | ServiceA.ts, ServiceB.ts |
| 2b. Service (BR-016–030) | IN_PROGRESS | 8 of 15 done | ServiceC.ts (partial) |
| 3. Controllers + DTOs | PENDING | — | — |
| 4. Events + Integration | PENDING | — | — |

## Resumption Info
- Last completed layer: 2a
- Next action: Continue Layer 2b from BR-XX-024
- Compilation status: PASSES (as of Layer 2a)
- Files generated so far: <count>

## Subagent Delegation Log
| Layer | Delegated At | Returned At | Outcome |
|-------|-------------|-------------|---------|
| 1 | 2026-09-01T09:00 | 2026-09-01T09:15 | OK — 7 entities |
| 2a | 2026-09-01T09:16 | 2026-09-01T09:45 | OK — 15 BR-IDs |
| 2b | 2026-09-01T09:46 | — | IN PROGRESS |
```

**Rules for IMPLEMENTATION-STATE.md:**
- Parent creates it before first delegation (with all layers as PENDING)
- Parent updates it immediately after each subagent returns (mark DONE, log outputs)
- If context compacts: parent reads this file FIRST to know where to resume
- Subagents do NOT write to this file — only the parent orchestrator does
- The file is removed at the post-service checklist (after validation passes)

**Why A-direct is faster:** No token spend on reformatting SAAM specs into Kiro format. No redundant requirements.md that restates 01-business-rules.md. No design.md that restates 02-domain-model.md + 03-api-design.md. The agent goes straight from spec to code. Per-layer delegation prevents context pressure on large services.

---

## Model B Setup: Transform + Kiro

### Parameters Needed

Ask the user ONE AT A TIME:

1. "Is the ATX CLI installed? (Run `atx --version` to check)"
   - If no: "Install it first: https://docs.aws.amazon.com/transform/latest/userguide/custom-get-started.html"

2. "Do you have a Transformation Definition (TD) published for this target stack?"
   - If no: "Let's create one. I'll prepare the TD reference materials."
   - If yes: "What's the TD name? (Run `atx custom def list` to see available TDs)"

3. "Which service should we start with? (I recommend `<first-from-roadmap>` based on the implementation roadmap)"

### Artifacts Created

The agent creates:

1. **`transform/SKILL.md`** (if TD doesn't exist) — Transformation definition reference for ATX:
   ```markdown
   # SAAM to <Target Stack> Transformation

   ## Context
   Transform SAAM microservice specifications into <Target Stack> implementations.

   ## Input
   A directory containing:
   - 01-business-rules.md — Business rules with BR-IDs
   - 02-domain-model.md — DDL for database schema
   - 03-api-design.md — API endpoints and operations
   - 04-api-contract.yaml — OpenAPI 3.1 (NAMING AUTHORITY — use exact field names from here)

   ## Output
   A complete <Target Stack> project with:
   - Domain entities mapped from DDL (field names per 04-api-contract.yaml)
   - Repository layer with real database queries
   - Service layer implementing ALL business rules (no stubs)
   - REST controllers at exact paths from 04-api-contract.yaml
   - Unit tests (one per BR-ID minimum)
   - Containerfile + configuration
   - application.yml with env-var database config (H2 fallback for local only)

   ## Critical Rules
   - Field names MUST match 04-api-contract.yaml exactly
   - Every BR-ID must have a real implementation (no TODOs, no stubs)
   - Database is the PRIMARY persistence (not in-memory)
   - Never simplify algorithms — implement full complexity from spec
   ```

2. **`transform/references/`** (if TD doesn't exist) — coding pattern references for ATX:
   - `coding-patterns.md` — target stack conventions, project structure
   - Copy of `04-api-contract.yaml` for the first service

3. **`.kiro/specs/<service>/`** — Kiro spec with Transform-first tasks.md (Task 1: Run ATX, Task 2: Fix output, etc.)

4. **`tracking/phase5-implementation/<service>.md`** — tracking file

### TD Publication (if needed)

If the user doesn't have a TD, guide them:

"I've prepared the transformation definition materials in `transform/`. To publish the TD:

```bash
cd transform/
atx -t
# When prompted: describe the transformation and point to SKILL.md + references/
# ATX will publish the TD to your account
```

Let me know the TD name once published."

### Confirmation

"Model B (Transform + Kiro) is set up. TD: `<td-name>`. Starting with `<service>`. Run the transform:

```bash
atx custom def exec -n <td-name> -p spec/microservices/<service>/ -x -t
```

Once ATX completes, let me know and I'll assess the output and create fix tasks."

---

## Model C Setup: ATX Batch + AI-DLC Pipeline

### Parameters Needed

Ask the user ONE AT A TIME:

1. "Is the ATX scaled platform deployed? (API endpoint, S3 buckets, Batch environment)"
   - If no: "You'll need to deploy it first. See `reference/atx-platform-infra/` for the Terraform + GitHub Actions reference. Should I walk you through it?"
   - If yes: "What's the API endpoint? (Or set `ATX_API_ENDPOINT` env var)"

2. "Is the TD published for this target stack?"
   - Same flow as Model B above

3. "Where is the specs Git repository? (URL for ATX to clone from)"
   - Or: "Are specs in the current workspace? I'll use S3 upload as fallback."

4. "Where should generated code land? (Git repo URL for output, or S3)"

5. "Do you want AI-DLC rules installed now for the wiring phase? (Recommended — installs to `.github/skills/`)"
   - If yes: proceed with AI-DLC installation
   - If later: "OK, I'll remind you before Stage 2."

> **AI-DLC Version Note:** SAAM currently integrates with AI-DLC Workflows v1 (single `core-workflow.md` rule file). AI-DLC v2 (GA) introduces a 14-agent roster, 32-stage workflow, TypeScript engine with bun, and a fundamentally different installation model. v2 support is planned once validated against a real engagement. For now, use v1 — it works and is well-tested with the SAAM pipeline.

### Artifacts Created

The agent creates:

1. **`transform/SKILL.md`** + **`transform/references/`** (same as Model B, if TD doesn't exist)

2. **`transform/batch-jobs.csv`** — CSV file for batch submission with ALL services (git-based output):
   ```csv
   source_repo,source_path,source_branch,output_repo,output_branch,transform_name
   <repo>,spec/microservices/<service-1>,main,<repo>,atx/<service-1>,<td-name>
   <repo>,spec/microservices/<service-2>,main,<repo>,atx/<service-2>,<td-name>
   <repo>,spec/microservices/<service-3>,main,<repo>,atx/<service-3>,<td-name>
   ...
   ```

3. **`transform/submit-all.sh`** — Script to submit all services as batch (git output primary):
   ```bash
   #!/bin/bash
   set -euo pipefail

   # ATX Batch Submission — All SAAM Services (git-based output)
   # Generated by SAAM Phase 5 Setup

   API_ENDPOINT="${ATX_API_ENDPOINT:-<endpoint>}"
   SPEC_REPO="<repo-url>"
   CODE_REPO="${SPEC_REPO}"  # Same repo, different branches
   BRANCH="main"
   TD_NAME="<td-name>"

   SERVICES=(
     <service-1>
     <service-2>
     <service-3>
   )

   echo "Submitting ${#SERVICES[@]} services to ATX (output: git branches)..."

   for service in "${SERVICES[@]}"; do
     echo "  Submitting: $service → branch atx/$service"
     curl -s -X POST "${API_ENDPOINT}/jobs" \
       --aws-sigv4 "aws:amz:${AWS_REGION:-us-east-1}:execute-api" \
       --user "" \
       -H "Content-Type: application/json" \
       -d "{
         \"source_repo\": \"${SPEC_REPO}\",
         \"source_path\": \"spec/microservices/${service}\",
         \"source_branch\": \"${BRANCH}\",
         \"output_repo\": \"${CODE_REPO}\",
         \"output_path\": \"sourcecode/${service}\",
         \"output_branch\": \"atx/${service}\",
         \"transform_name\": \"${TD_NAME}\"
       }" | jq -r '.job_id // "FAILED"'
   done

   echo ""
   echo "All jobs submitted. Monitor at: ${API_ENDPOINT}/jobs"
   echo ""
   echo "After all jobs complete, checkout branches:"
   echo "  for service in ${SERVICES[*]}; do"
   echo "    git fetch origin atx/\$service"
   echo "    git checkout origin/atx/\$service -- sourcecode/\$service/"
   echo "  done"
   echo ""
   echo "Then run BR-ID detection:"
   echo "  for service in ${SERVICES[*]}; do"
   echo "    python3 graph-mcp/scripts/detect_br_ids.py --service \$service"
   echo "  done"
   ```

4. **AI-DLC rules** (if user approved installation):
   ```bash
   mkdir -p .github/skills/aws-aidlc-rules
   mkdir -p .kiro/aws-aidlc-rule-details
   # Copy core-workflow.md to .github/skills/aws-aidlc-rules/
   # Copy rule-details to .kiro/aws-aidlc-rule-details/
   ```

5. **`.kiro/aws-aidlc-rule-details/extensions/saam/saam-rules.md`** — SAAM extension for AI-DLC:
   ```markdown
   ## Rule SAAM-01: No Shell Implementations
   Every method that calls an external service MUST make real HTTP calls.
   Verification: grep for mock/stub patterns; ensure HttpClient/RestTemplate calls exist.

   ## Rule SAAM-02: No Algorithm Simplification
   Implement EXACTLY the complexity described in the spec.
   Verification: count conditionals in spec Logic vs code; counts must match.

   ## Rule SAAM-03: API Contract Naming Authority
   ALL field names, paths, status codes MUST come from 04-api-contract.yaml.
   Verification: compare DTO field names against contract schema.

   ## Rule SAAM-04: Database Configuration
   Production database via env vars is PRIMARY. In-memory fallback ONLY when env vars absent.
   Verification: check application.yml for DATABASE_URL env var reference.

   ## Rule SAAM-05: No Test-Driven Implementation
   Never read test suites to determine implementation logic.
   Verification: code generation plan must reference spec files, not validation/ files.

   ## Rule SAAM-06: Known Deviations (Stage 2 Output)
   Read .kiro/aws-aidlc-rule-details/extensions/saam/known-deviations.md BEFORE construction.
   Unit 0 fixes these systemic issues. Do NOT build integration on top of known-broken patterns.
   Verification: known-deviations.md exists and Unit 0 is the first construction unit in the plan.

   ## Rule SAAM-07: BR-ID Annotation (Traceability)
   Every method that implements a business rule MUST include a comment referencing the BR-ID.
   Format: `// BR-XX-YYY-NNN: <rule name>` above the method.
   Multiple BR-IDs in one method: list all on separate comment lines.
   Verification: grep for BR-ID pattern in generated code; count must match assigned rules per service.
   ```

6. **`transform/aidlc-wiring-units.md`** — AI-DLC construction units for Stage 3:
   ```markdown
   # AI-DLC Construction Units (Stage 3 — Deviation-Aware Wiring)

   ## Unit 0: Systemic Fixes (MANDATORY FIRST — from Stage 2 deviation log)
   - Scope: Fix ALL systemic deviations identified during Stage 2 smoke validation
   - Inputs: .kiro/aws-aidlc-rule-details/extensions/saam/known-deviations.md + all 04-api-contract.yaml files
   - Output: All services aligned with their API contracts (status codes, headers, field names, response shapes)
   - Priority: RUNS FIRST — before any other unit. Do NOT build integration on broken patterns.
   - Verification: Re-run affected test assertions after each pattern fix

   ## Unit 1: Cross-Service Integration
   - Scope: HTTP clients between services, service discovery
   - Inputs: All 04-api-contract.yaml files
   - Output: Shared HTTP client library or per-service Refit/RestTemplate configs

   ## Unit 2: Authentication & Multi-Tenancy
   - Scope: JWT validation, tenant isolation, RBAC
   - Inputs: Phase 2 architecture decisions
   - Output: Auth middleware, tenant context provider

   ## Unit 3: Event Wiring
   - Scope: Kafka/SQS producers and consumers
   - Inputs: Event contracts from specs
   - Output: Event publisher/consumer implementations

   ## Unit 4: Frontend Polish & Wiring
   - Scope: Fix ATX-generated frontend, wire to backend APIs, add auth/state management
   - Inputs: ATX-generated frontend + spec/frontend/<app>/ (if exists)
   - Output: Polished, fully wired frontend application

   ## Unit 5: Infrastructure
   - Scope: Podman Compose, K8s manifests, CI/CD
   - Inputs: All services
   - Output: Deployment configurations

   ## Unit 6: Integration Testing
   - Scope: Cross-service test scenarios
   - Inputs: API contracts + business flows
   - Output: Integration test suite
   ```

7. **`tracking/phase5-implementation/INDEX.md`** — overview of all services:
   ```markdown
   # Phase 5 Implementation — All Services

   | # | Service | Model | Stage | Status |
   |---|---------|-------|-------|--------|
   | 1 | <service-1> | C (ATX Batch) | Stage 1: ATX | PENDING |
   | 2 | <service-2> | C (ATX Batch) | Stage 1: ATX | PENDING |
   ...
   ```

### Confirmation

"Model C (ATX Batch + AI-DLC Pipeline) is set up:
- Batch submission script ready at `transform/submit-all.sh` ([N] services)
- AI-DLC rules installed with SAAM extensions (including SAAM-06: Known Deviations)
- Wiring units defined for Stage 3 (Unit 0: Systemic Fixes + Units 1-6)
- Tracking initialized for all services

**Pipeline stages:**
1. Stage 1: ATX Batch (submit all services in parallel)
2. Stage 2: Smoke Validation (run tests, catalog deviations, produce known-deviations.md)
3. Stage 3: AI-DLC Construction (fix systemic issues first, then wire cross-cutting concerns)
4. Stage 4: Final Validation Gate (100% test pass required)

**Next step — Stage 1: Submit all services to ATX:**
```bash
chmod +x transform/submit-all.sh
./transform/submit-all.sh
```

Monitor progress, then let me know when all jobs complete. I'll assess the output and we'll move to Stage 2 (AI-DLC wiring)."

---

## Post-Setup: Common Actions

Regardless of model chosen, after setup the agent:

1. Updates `tracking/phase5-setup.md` with setup tasks marked DONE
2. If Jira configured: creates Phase 5 Epic with appropriate tasks
3. Reminds the user of the SAAM guardrails that apply to all models:
   - Specs drive code, tests verify (never the reverse)
   - API contract is the naming authority
   - No stubs, no algorithm simplification, no shell implementations
   - Database must use production target (env-var driven)
   - Test suites live in `validation/` (separated from code)

4. **Scaffolds `sourcecode/compose.yaml` (shared infrastructure):**

   The compose file provides shared infrastructure (database, cache, message broker) that ALL services depend on. It is created ONCE during setup and updated after each service is implemented (adding the service container).

   **Project namespace:** The compose project name MUST be derived from the engagement/repo name — NEVER use generic names like "sourcecode" or "saam" which conflict when multiple projects run on the same machine.

   **Initial scaffold (generated during setup):**
   ```yaml
   # sourcecode/compose.yaml — Shared infrastructure for <engagement-name>
   # Generated by SAAM Phase 5 setup. Updated per-service after implementation.

   name: <engagement-name>  # e.g., "acme-erp", "benefits-platform" — NOT "sourcecode"

   services:
     postgres:
       image: postgres:16-alpine
       environment:
         POSTGRES_USER: saam
         POSTGRES_PASSWORD: saam_local
         POSTGRES_DB: saam_dev
       ports:
         - "5432:5432"
       volumes:
         - pgdata:/var/lib/postgresql/data
       healthcheck:
         test: ["CMD-SHELL", "pg_isready -U saam"]
         interval: 5s
         timeout: 3s
         retries: 5

     redis:
       image: redis:7-alpine
       ports:
         - "6379:6379"
       healthcheck:
         test: ["CMD", "redis-cli", "ping"]
         interval: 5s
         timeout: 3s
         retries: 5

     # Add message broker if services use async messaging:
     # rabbitmq:
     #   image: rabbitmq:3.13-management-alpine
     #   ports:
     #     - "5672:5672"
     #     - "15672:15672"
     #   environment:
     #     RABBITMQ_DEFAULT_USER: saam
     #     RABBITMQ_DEFAULT_PASS: saam_local

   volumes:
     pgdata:
   ```

   **Rules:**
   - Compose project `name` = engagement name (from `engagement.yaml` or repo directory name)
   - Ports are static by convention (5432, 6379, 5672). If conflicts detected at runtime, agent adjusts.
   - After each service implementation completes (post-service checklist), the agent adds the service to compose if it has a Dockerfile/Containerfile. Include inter-service environment variables derived from `spec/microservices/<service>/05-dependencies.md` (e.g., `IDENTITY_SERVICE_URL: "http://identity-service:3001"` for services that call identity-service).
   - Message broker (RabbitMQ/Kafka) is uncommented only if services use async events (check `05-dependencies.md` for event patterns)
   - This file lives at `sourcecode/` root — NEVER inside individual service directories
   - **After ALL services are implemented:** generate `sourcecode/scripts/bootstrap.sh` (seed minimum viable data) and `sourcecode/scripts/verify-system.sh` (health + connectivity check). See `saam-phase6-continuous-evolution.md` → "System Integration Validation" for the full protocol.

5. **Scaffolds `spec/test-config.yaml` (shared test configuration):**

   Generated once during setup. Consumed by comprehensive test suites and service startup.

   ```yaml
   # spec/test-config.yaml — Shared test environment configuration
   # Generated by SAAM Phase 5 setup. Consumed by test suites and service .env.test files.

   auth:
     jwt_secret: "saam-test-secret-do-not-use-in-production"
     jwt_issuer: "saam-test"
     jwt_expiry_seconds: 3600
     test_token: "<generated JWT signed with jwt_secret, sub=test-user-001>"

   test_accounts:
     admin:
       id: "00000000-0000-4000-a000-000000000001"
       email: "admin@test.local"
       role: "admin"
     user:
       id: "00000000-0000-4000-a000-000000000002"
       email: "user@test.local"
       role: "user"

   services:
     # Ports match compose.yaml static ports + service-specific ports from contracts
     gateway: { port: 3000, base_path: "/api/v1" }
     # identity-service: { port: 3001, base_path: "/api/v1/identity" }
     # team-service: { port: 3002, base_path: "/api/v1/teams" }
     # ... (populated as services are implemented)

   database:
     host: "localhost"
     port: 5432
     user: "saam"
     password: "saam_local"
     name: "saam_dev"

   redis:
     host: "localhost"
     port: 6379

   seed:
     script: "./scripts/seed-test-data.sh"
     reset_script: "./scripts/reset-test-data.sh"
   ```

   **Rules:**
   - JWT test token is pre-generated (signed with the secret) so test suites can include it directly
   - Service ports are added as each service is implemented
   - Test suites reference this file for BASE_URL, AUTH_HEADER, and test account IDs
   - Services reference this for `.env.test` generation (DB connection, JWT secret)

6. **Scaffolds `spec/shared/infrastructure-patterns.md` (code-level patterns):**

   Defines cross-cutting code patterns that ALL services MUST implement identically.
   Generated during Phase 5 setup based on the tech stack confirmed in Phase 4b.

   ```markdown
   # Infrastructure Patterns — <Target Stack>
   
   ## Auth Guard
   - JWT validation middleware (verify signature, check expiry, extract claims)
   - Tenant extraction from token or header
   - Role-based access control pattern (decorator/annotation approach)
   - Test bypass: @Public() decorator skips auth in test mode
   
   ## Error Handling
   - Global exception filter (catches all unhandled errors)
   - Standard error response shape (matches API contract ErrorResponse schema)
   - HTTP status code mapping (domain exceptions → HTTP codes)
   - Validation error format (field-level errors for 422 responses)
   
   ## Tenant Isolation
   - How tenant ID flows through the request lifecycle
   - Database query scoping (all queries filtered by tenant)
   - Multi-tenant middleware/interceptor pattern
   
   ## Logging
   - Structured logging format (JSON with correlation ID)
   - What to log at each level (error, warn, info, debug)
   - Correlation ID propagation across service calls
   
   ## Health Checks
   - Liveness probe (/health/live — is the process running?)
   - Readiness probe (/health/ready — can it serve traffic? checks DB, cache, etc.)
   - Dependency health (can it reach its dependencies?)
   
   ## Event Publishing
   - How to publish domain events (message broker client pattern)
   - Event envelope format (type, source, timestamp, correlationId, payload)
   - Idempotency (event deduplication key pattern)
   
   ## Common Decorators/Annotations
   - @Public() — skip auth
   - @Roles('admin', 'user') — role check
   - @TenantScoped() — auto-inject tenant filter
   - @Audit('action') — audit log the operation
   ```

   **Rules:**
   - Stack-specific: NestJS patterns differ from Spring Boot differs from FastAPI
   - Generated from the Phase 4b tech stack decision
   - Implementation subagent MUST follow these patterns (included in its context)
   - If a service deviates from these patterns, it's a compliance issue

7. **Scaffolds `spec/shared/event-schemas/` (async message contracts):**

   Defines the exact payload shape for every domain event published across the system.
   Derived from `05-dependencies.md` "Events Published" sections across all services.

   ```
   spec/shared/event-schemas/
   ├── index.md                    # Event catalog (all events, publishers, consumers)
   ├── invoice.posted.yaml         # { invoiceId, vendorId, amount, ... }
   ├── team.created.yaml           # { teamId, creatorId, name, ... }
   ├── payment.processed.yaml      # { paymentId, invoiceId, amount, ... }
   └── ...
   ```

   Each event schema:
   ```yaml
   event: invoice.posted
   version: 1
   publisher: ap-service
   consumers: [notification-service, reporting-service]
   payload:
     invoiceId: { type: string, format: uuid, required: true }
     vendorId: { type: string, format: uuid, required: true }
     amount: { type: number, required: true }
     currency: { type: string, default: "USD" }
     postedAt: { type: string, format: date-time, required: true }
     postedBy: { type: string, format: uuid, required: true }
   ```

   **Rules:**
   - ONE file per event type (not per publisher — the event is the contract)
   - Both publisher and consumer implementations reference this schema
   - If a consumer expects a field not in the schema → spec gap
   - Generated during Phase 5 setup from all 05-dependencies.md event declarations

8. **Scaffolds `spec/shared/common-schemas.yaml` (shared types):**

   Types used across ALL services — defined ONCE, referenced everywhere.
   Prevents each service from inventing its own PaginationMeta or ErrorResponse.

   ```yaml
   # spec/shared/common-schemas.yaml
   schemas:
     PaginationMeta:
       type: object
       properties:
         page: { type: integer }
         pageSize: { type: integer }
         totalItems: { type: integer }
         totalPages: { type: integer }

     ErrorResponse:
       type: object
       properties:
         error: { type: string }
         message: { type: string }
         statusCode: { type: integer }
         timestamp: { type: string, format: date-time }
         path: { type: string }

     ListResponse:
       type: object
       description: "Standard envelope for all list endpoints"
       properties:
         data: { type: array, items: {} }  # type varies per endpoint
         meta: { $ref: "#/schemas/PaginationMeta" }

     AuditFields:
       type: object
       properties:
         createdAt: { type: string, format: date-time }
         updatedAt: { type: string, format: date-time }
         createdBy: { type: string, format: uuid }
         updatedBy: { type: string, format: uuid }
   ```

   **Rules:**
   - Every service's 04-api-contract.yaml MUST reference these shared schemas (not redefine them)
   - The 08-dtos/ generation uses these for base types
   - Implementation copies `common-schemas.yaml` into a shared library/package

9. **Scaffolds `spec/shared/env-schema.md` (configuration contract):**

   Documents ALL environment variables across the system — what each service needs.

   ```markdown
   # Environment Variable Schema

   ## Global (all services)
   | Variable | Required | Default | Description |
   |----------|----------|---------|-------------|
   | DATABASE_URL | yes | — | PostgreSQL connection string |
   | REDIS_URL | yes | — | Redis connection string |
   | JWT_SECRET | yes | — | Shared JWT signing secret |
   | JWT_ISSUER | yes | "saam" | Token issuer claim |
   | LOG_LEVEL | no | "info" | Structured log level |
   | TENANT_HEADER | no | "x-tenant-id" | Header name for tenant isolation |
   | CORRELATION_HEADER | no | "x-correlation-id" | Header for distributed tracing |

   ## Per-Service
   | Service | Variable | Required | Description |
   |---------|----------|----------|-------------|
   | gateway | IDENTITY_SERVICE_URL | yes | URL to identity service for JWT key fetch |
   | ap-service | GL_SERVICE_URL | yes | URL to GL service for distribution posting |
   | ap-service | RABBITMQ_URL | yes | Message broker for event publishing |
   | ... | ... | ... | ... |
   ```

   **Rules:**
   - Derived from 05-dependencies.md (inter-service URLs) + infrastructure (DB, cache, broker)
   - compose.yaml uses these variable names (consistency between dev + production)
   - Each service's startup validates all REQUIRED vars are present (fail-fast)

10. **Scaffolds `spec/shared/migration-strategy.md` (database evolution):**

    Defines how database schema evolves across the system lifecycle.

    ```markdown
    # Migration Strategy

    ## Tool
    - <Prisma Migrate | Flyway | Alembic | EF Migrations> (from tech stack)

    ## Principles
    - Migrations are forward-only (no rollback in production)
    - Each service owns its schema — no cross-service table access
    - Migrations run automatically on service startup (dev mode)
    - Migrations run as a separate step in CI/CD (production mode)

    ## Multi-Tenant Handling
    - <schema-per-tenant | row-level-isolation | database-per-tenant>
    - Tenant provisioning: <how new tenants get their schema>
    - Migration applies to ALL tenant schemas simultaneously

    ## Seed Data
    - `scripts/seed-dev-data.sh` — development data (realistic but fake)
    - `scripts/seed-test-data.sh` — test data (minimal, deterministic UUIDs)
    - `scripts/bootstrap.sh` — production bootstrap (admin account, default config)

    ## Schema Naming
    - Tables: snake_case plural (e.g., journal_entries, invoice_lines)
    - Columns: snake_case (e.g., created_at, tenant_id)
    - Indexes: idx_<table>_<columns> (e.g., idx_invoices_vendor_id)
    - Foreign keys: fk_<table>_<referenced_table> (e.g., fk_invoice_lines_invoices)
    ```

    **Rules:**
    - Phase 5 implementation follows this strategy exactly
    - 02-domain-model.md DDL must conform to the naming conventions here
    - Multi-tenant strategy from Phase 0 onboarding decision
