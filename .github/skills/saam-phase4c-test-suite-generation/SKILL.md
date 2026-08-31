---
name: saam-phase4c-test-suite-generation
description: "Test-first executable test suite generation and business rule validation coverage."
copyright: "Copyright 2024-2026 SoftServe Inc. All rights reserved."
authors: "Max Kozinenko, Roman Kalita (SoftServe)"
---

# SAAM Phase 4c: Test Suite Generation

## Objective

Generate a `comprehensive-test-suite.sh` for every in-scope service. This test suite is the ACCEPTANCE GATE for Phase 5 implementation — no service can be considered complete until the suite passes at 100%.

## Required Steering Files (Read Before Proceeding)

The agent MUST read the following steering files before executing Phase 4c:

1. **`.github/skills/saam-test-suite-template/SKILL.md`** — Template structure, helper functions, rules for test creation, common pitfalls
2. **`.github/skills/saam-api-contract/SKILL.md`** — Protocol for how the API contract drives naming in test assertions
3. **`.github/skills/saam-human-guidance-protocol/SKILL.md`** — Prompt categories and decision logging
4. **`.github/skills/saam-task-tracking/SKILL.md`** — Tracking file format (for `tracking/phase4c-test-suites.md`)
5. **`.github/skills/saam-frontend-spec-template/SKILL.md`** — (Only if frontend spec exists) Section `07-frontend-test-plan.md` defines frontend test structure

## Task Tracking Activation

**PRECONDITION: The agent MUST NOT begin test suite generation until `tracking/phase4c-test-suites.md` exists.** If it doesn't exist, create it NOW with all services listed as PENDING.

**PhaseEvent (telemetry timestamp):** Immediately after creating the tracking file, write: `graph_add_node(nodeType="PhaseEvent", id="P4C-started", properties={phase: "P4C", event: "started", timestamp: <current ISO timestamp>})`.

After each service's test suite is complete, the agent MUST update the tracking file immediately (mark service DONE) BEFORE starting the next service. If Jira is configured, create an Epic with Tasks per service. See `.github/skills/saam-task-tracking/SKILL.md` for format.

## Subagent Delegation (Per-Service Test Suite Generation)

When delegating test suite generation to a subagent for context optimization:

**contextFiles to include:**
- `.github/skills/saam-phase4c-test-suite-generation/SKILL.md`
- `.github/skills/saam-test-suite-template/SKILL.md`
- `.github/skills/saam-api-contract/SKILL.md`

**Delegation prompt template:**
```
Generate the comprehensive test suite for service <service-name>.

READ THESE FILES FIRST (included in your context):
- .github/skills/saam-phase4c-test-suite-generation/SKILL.md (orchestration protocol)
- .github/skills/saam-test-suite-template/SKILL.md (EXACT test format — extract_field function, curl patterns, BR-ID comments)
- .github/skills/saam-api-contract/SKILL.md (contract is naming authority)

INPUT:
- spec/microservices/<service>/04-api-contract.yaml (field names, paths, status codes)
- spec/microservices/<service>/08-dtos/ (EXACT request/response shapes — payload field names from here)
- spec/microservices/<service>/01-business-rules.md (BR-IDs to test)

PRODUCE EXACTLY: validation/<service>/comprehensive-test-suite.sh

Requirements:
- Every Active/Core BR-ID must have at least one test assertion
- Use extract_field() function for JSON parsing (defined in template)
- Use global HEADERS variable for auth/tenant headers
- Every test has: test number, BR-ID reference, curl command, assertion
- Tests build on each other (create → read → update → delete chains)
- Exit code 0 = all pass, 1 = failures
- Field names come ONLY from 04-api-contract.yaml — NEVER invent names
- NEVER skip tests or use placeholder assertions

NEVER create a test file that doesn't follow the template format.
```

**Parent verification after subagent returns:**
- [ ] `validation/<service>/comprehensive-test-suite.sh` exists and is executable
- [ ] File starts with the standard header (PASSED/FAILED/TOTAL counters)
- [ ] Contains `extract_field()` function definition
- [ ] Every Active/Core BR-ID has at least one test (grep for BR-ID pattern)
- [ ] Field names match `04-api-contract.yaml` (spot-check 3-5 fields)
- [ ] No placeholder assertions (grep for "TODO", "SKIP", "placeholder")

## Graph Population (Incremental — During Phase 4c)

The agent MUST update the knowledge graph after generating each service's test suite — NOT wait until the exit gate.

**After generating `comprehensive-test-suite.sh` for each service:**
1. For each test assertion: `graph_add_node(nodeType="TestAssertion", id=<testNum>, properties={testName, brId, endpoint, method, expectedStatus, service, assertionType, status: "NOT_RUN"})`
2. Link to BR-ID: `graph_add_edge(edgeType="TESTED_BY", sourceId=<brId>, sourceType="BusinessRule", targetId=<testNum>, targetType="TestAssertion")`
3. Run lifecycle advancement: `graph_run_inferences(rules=["lifecycle_states"])` — this advances BR-IDs from Declared to Tested (test now exists)

**Why incremental:** When Phase 5 starts implementing the first service, the graph already has TestAssertion nodes for that service — enabling `graph_implementation_context` to show which rules have tests and which don't.

## Entry Precondition

Before starting Phase 4c, verify:

- [ ] Phase 4 specifications exist for all in-scope services (`spec/microservices/<service>/01-business-rules.md`)
- [ ] API contracts exist for all services (`spec/microservices/<service>/04-api-contract.yaml`)
- [ ] Phase 4b is complete (automatibility scores calculated, roadmap finalized, **tech stack confirmed**) — OR human explicitly chose to skip 4b
- [ ] `modernization/services-composition.md` exists with service catalog
- [ ] `modernization/tech-stack-recommendation.md` exists (needed for Stage 0 DTO generation)

**If API contracts are missing:** STOP. The contract MUST exist before test suites can be generated — it is the naming authority. Inform the user and offer to generate contracts per `.github/skills/saam-api-contract/SKILL.md`.

---

## Stage 0: DTO Generation (MANDATORY — Before Test Suites)

### Purpose

Generate target-language DTO files for every in-scope service. These DTOs become the **concrete binding artifact** that eliminates naming drift between test suites (Phase 4c) and implementation (Phase 5). Both consume the same DTOs — zero interpretation room.

**Why here (Phase 4c) and not Phase 4:** The target technology stack is not confirmed until Phase 4b. Phase 4c is the FIRST point where the stack is known AND spec extraction is complete. DTOs require language-specific constructs (decorators, types, validation annotations) that depend on the target stack.

### Authority Chain

```
Business Rules + Domain Model → API Contract (stack-agnostic shapes)
                                      ↓
                              DTOs (target-language, with validations)
                                      ↓
                    ┌─────────────────┴─────────────────┐
                    ↓                                   ↓
          Test Scripts (payloads from DTOs)    Phase 5 Implementation (copies DTOs verbatim)
```

**The DTO is the naming authority for code. The contract remains the naming authority for API interface shapes. DTOs are the mechanical bridge between the two.**

### Generation Inputs (Per Service)

1. `spec/microservices/<service>/04-api-contract.yaml` — schema field names, types, required/optional, enums
2. `spec/microservices/<service>/02-domain-model.md` — validation constraints, defaults, relationships, business logic context
3. `modernization/tech-stack-recommendation.md` — target framework (determines decorators, casing, idioms)

### Generation Rules

1. **One DTO file per operation that has a request body OR a distinct response shape.** Common patterns:
   - `create-<resource>.dto.ts` — POST request body
   - `update-<resource>.dto.ts` — PUT/PATCH request body
   - `<resource>-response.dto.ts` — response shape (if different from entity)
   - `<resource>-query.dto.ts` — query parameters for list/filter endpoints
   - `index.ts` — barrel export of all DTOs

2. **Field names MUST match `04-api-contract.yaml` `components/schemas` property names exactly.** No renaming, no casing transformation. If the contract says `serviceLevelTarget`, the DTO field is `serviceLevelTarget`.

3. **Validation decorators derived from contract + domain model:**
   - `required` in contract schema → `@IsNotEmpty()` / `@IsString()` etc.
   - `format: email` → `@IsEmail()`
   - `minLength` / `maxLength` → `@MinLength()` / `@MaxLength()`
   - `pattern` → `@Matches(/<regex>/)`
   - `enum` → `@IsEnum(<EnumName>)`
   - Domain model constraints (e.g., "must be positive integer") → `@IsPositive()` / `@Min(1)`

4. **Default values from domain model.** If `02-domain-model.md` specifies a default (e.g., `status DEFAULT 'pending'`), encode it in the DTO: `@IsOptional() status?: string = 'pending'`

5. **Nested objects:** If a contract schema references another schema (`$ref`), create a separate DTO for it and use class composition.

6. **Response DTOs:** Define the response shape the service will return. Phase 5 controllers MUST return instances matching these shapes.

### Output Location

```
spec/microservices/<service>/08-dtos/
├── create-<resource>.dto.ts
├── update-<resource>.dto.ts
├── <resource>-response.dto.ts
├── <resource>-query.dto.ts
├── ... (one per distinct request/response shape)
└── index.ts
```

**File extension matches target stack:** `.ts` for TypeScript/NestJS, `.java` for Spring, `.py` for FastAPI, `.cs` for .NET.

### Target Stack Patterns

**TypeScript/NestJS:**
```typescript
import { IsNotEmpty, IsString, IsEmail, IsOptional, MinLength, IsEnum } from 'class-validator';
import { ApiProperty, ApiPropertyOptional } from '@nestjs/swagger';

export class CreateAccountDto {
  @ApiProperty({ description: 'User email address' })
  @IsNotEmpty()
  @IsEmail()
  email: string;

  @ApiProperty({ description: 'Display name', minLength: 2, maxLength: 100 })
  @IsNotEmpty()
  @IsString()
  @MinLength(2)
  displayName: string;

  @ApiPropertyOptional({ description: 'Profile photo URL' })
  @IsOptional()
  @IsString()
  avatarUrl?: string;
}
```

**Java/Spring:**
```java
public record CreateAccountRequest(
    @NotBlank @Email String email,
    @NotBlank @Size(min = 2, max = 100) String displayName,
    @Nullable String avatarUrl
) {}
```

**Python/FastAPI:**
```python
class CreateAccountRequest(BaseModel):
    email: EmailStr
    display_name: str = Field(..., min_length=2, max_length=100)
    avatar_url: str | None = None
```

### Execution Protocol

For each service in the service catalog:

1. **Read** `04-api-contract.yaml` — extract all schemas under `components/schemas`
2. **Read** `02-domain-model.md` — extract validation rules, defaults, constraints
3. **Identify** which schemas are request bodies (referenced by `requestBody`) and which are responses
4. **Generate** one DTO file per distinct schema, applying target-stack decorators
5. **Generate** `index.ts` (barrel export)
6. **Verify** every field name in the DTO matches the corresponding contract schema property name exactly (case-sensitive)
7. **Save** to `spec/microservices/<service>/08-dtos/`

### Subagent Delegation (DTO Generation)

When delegating to a subagent:

**Delegation prompt template:**
```
Generate DTOs for service <service-name>.

TARGET STACK: <TypeScript/NestJS | Java/Spring | Python/FastAPI>

INPUT:
- spec/microservices/<service>/04-api-contract.yaml (schema field names — NAMING AUTHORITY)
- spec/microservices/<service>/02-domain-model.md (validation constraints, defaults)

OUTPUT: spec/microservices/<service>/08-dtos/ (one file per request/response schema + index barrel)

RULES:
- Field names MUST match 04-api-contract.yaml property names EXACTLY (case-sensitive)
- Validation decorators from domain model constraints
- Default values from domain model DDL defaults
- One DTO per distinct request body or response shape
- Include barrel export (index.ts / index.java / __init__.py)
```

### Verification Gate (Before Proceeding to Test Suite Generation)

After generating DTOs for ALL services, verify:

- [ ] Every service has `spec/microservices/<service>/08-dtos/` directory
- [ ] Every request body schema in the contract has a corresponding DTO
- [ ] Field names in DTOs match contract schemas exactly (spot-check 3 services)
- [ ] Validation decorators reflect domain model constraints
- [ ] Barrel export exists

**ONLY after Stage 0 is complete for ALL services does Stage 1 (test suite generation) begin.**

### Relationship to Test Scripts (Stage 1)

Stage 1 (test suite generation) uses DTOs as the payload reference:
- Test payloads are constructed using the EXACT field names from DTOs
- Required fields (those with `@IsNotEmpty()` / `@NotBlank`) MUST be present in test payloads
- Optional fields are tested both with and without values
- Enum fields use values defined in the DTO's enum type

This ensures test payloads are mechanically consistent with what the implementation will accept (since Phase 5 copies these same DTOs).

### Relationship to Phase 5 (Implementation)

Phase 5 copies `spec/<service>/08-dtos/` into `sourcecode/<service>/src/dto/` **verbatim**. The implementation MUST NOT regenerate or rename DTO fields. See `.github/skills/saam-phase5-ai-dlc-implementation/SKILL.md` for the hard rule.

### Frontend API Client Generation (Stage 0b — If Frontend Spec Exists)

After backend DTOs are generated for ALL services, and IF `spec/frontend/<app-name>/` exists, generate a typed API client that the frontend implementation MUST use. This prevents the frontend from inventing API paths.

**Purpose:** The API client is the mechanical binding between frontend pages and backend services — same role as DTOs for backend code. Frontend pages NEVER construct URLs themselves; they call methods on the API client.

**Generation inputs:**
1. ALL backend `spec/microservices/*/04-api-contract.yaml` files (exact paths, methods, request/response schemas)
2. `spec/frontend/<app>/01-api-contract.md` (Gateway Routing Table — maps frontend-facing paths to backend service paths)
3. Target frontend stack (from Phase 4b tech stack recommendation)

**Output location:**
```
spec/frontend/<app-name>/09-api-client/
├── index.ts                    # barrel export
├── client.ts                   # base HTTP client (fetch/axios wrapper)
├── types.ts                    # shared request/response types (from backend DTOs)
├── identity.api.ts             # identity-service endpoints
├── team.api.ts                 # team-service endpoints
├── submission.api.ts           # submission-service endpoints
├── ... (one file per backend service)
└── README.md                   # usage instructions for frontend devs
```

**Generation rules:**

**Source restriction (CRITICAL — prevents invented paths):**
- The api-client MUST be derived EXCLUSIVELY from backend `04-api-contract.yaml` files and the Gateway Routing Table
- The api-client MUST NOT be derived from screen specs (`02-screen-inventory.md`), user flows (`03-user-flows.md`), or assumptions about "what a page probably needs"
- If a screen spec references data that no backend contract exposes → that's a SPEC GAP (flag it), not a reason to invent an endpoint in the api-client
- Every function in the api-client MUST trace back to a specific `path` + `method` in a backend `04-api-contract.yaml`. No function exists without a contract backing it.

1. **One API module file per backend service.** Each exports typed functions matching the service's endpoints.

2. **Function signatures derived from `04-api-contract.yaml`:**
   ```typescript
   // From team-service 04-api-contract.yaml:
   //   GET /api/v1/teams?memberId={id}&page={n}&pageSize={n}
   //   POST /api/v1/teams  (body: CreateTeamRequest)
   
   export async function listTeams(params: { memberId: string; page?: number; pageSize?: number }): Promise<TeamListResponse> { ... }
   export async function createTeam(body: CreateTeamDto): Promise<Team> { ... }
   ```

3. **Paths come from Gateway Routing Table** (frontend-facing URLs, not backend internal paths):
   - If gateway routing: functions use the frontend path (e.g., `/api/teams`) which the gateway routes to the backend
   - If direct mode: functions use the backend path with the service port from `test-config.yaml`

4. **Request/response types imported from backend DTOs** (or re-exported from `types.ts`). Field names MUST match backend `08-dtos/` exactly.

5. **Error handling standardized:** Every function wraps the HTTP call with consistent error handling (throws typed errors matching backend `ErrorResponse` schema).

6. **Auth header injection:** The base `client.ts` automatically attaches the JWT token from the auth store — individual API functions don't handle auth.

7. **Workflow-level methods (from 07-workflows.md):** For multi-step operations where the backend orchestrates a sequence, expose a WORKFLOW method that maps to the single trigger endpoint — NOT multiple individual calls that the page has to chain:
   ```typescript
   // GOOD: one method = one backend workflow (backend orchestrates internally)
   export async function postInvoice(id: string): Promise<PostInvoiceResult> { ... }
   // The backend handles: validate → GL post → status update → event publish
   
   // BAD: page orchestrates the sequence (fragile, duplicates backend logic)
   await validateInvoice(id);
   await postGlDistributions(id);
   await updateInvoiceStatus(id, 'posted');
   ```
   For workflows where the FRONTEND must call multiple endpoints in sequence (rare — only when the backend requires separate API calls for each step, e.g., wizard flows), document the sequence in the function's JSDoc referencing the workflow ID.

**Subagent delegation prompt (api-client generation):**
```
Generate the frontend API client for <app-name>.

INPUT:
- ALL spec/microservices/*/04-api-contract.yaml (backend endpoint definitions)
- ALL spec/microservices/*/07-workflows.md (backend operation sequences — for workflow-level methods)
- spec/07-cross-service-workflows.md (cross-service choreographies)
- spec/frontend/<app>/01-api-contract.md (Gateway Routing Table)
- spec/microservices/*/08-dtos/ (request/response types — reuse these)
- Target frontend stack: <React/Next.js/Vue/Angular>

OUTPUT: spec/frontend/<app>/09-api-client/ (typed API client module)

RULES:
- One file per backend service (identity.api.ts, team.api.ts, etc.)
- Function names: list<Resource>, create<Resource>, get<Resource>, update<Resource>, delete<Resource>
- Paths MUST come from Gateway Routing Table or backend 04-api-contract.yaml — NEVER invented
- Types MUST match backend 08-dtos/ field names exactly
- Base client handles auth token injection + error parsing
```

**Verification gate:**
- [ ] Every backend service has a corresponding `.api.ts` file
- [ ] Every endpoint in each backend contract has a corresponding function
- [ ] All paths match the Gateway Routing Table (or direct backend paths if no gateway)
- [ ] Types reference backend DTO field names (spot-check 5 functions)

**Relationship to Phase 5 frontend implementation:**

Phase 5 copies `spec/frontend/<app>/09-api-client/` into `sourcecode/<app>/src/api/` **verbatim**. Frontend pages MUST import from this api-client — they MUST NOT construct URLs, use fetch/axios directly for backend calls, or invent path strings. See `.github/skills/saam-phase5-ai-dlc-implementation/SKILL.md` for the enforcement rule.

---

## Generation Protocol (Per Service)

For each service in the service catalog, execute this loop:

### Step 1: Read Inputs (MANDATORY — in this order)

1. Read `spec/microservices/<service>/04-api-contract.yaml` — this is the NAMING AUTHORITY for all field names, paths, status codes, and response shapes in the test suite. **Also extract `x-global-headers`** — these are required on EVERY request and must populate the test suite's `GLOBAL_HEADERS` / `TENANT_HEADER` variables.
2. Read `spec/microservices/<service>/08-dtos/` — the DTOs define the EXACT request payload shapes (field names, required/optional, types). **Test payloads MUST use the same field names as the DTOs.** Since DTOs were generated from the contract in Stage 0, they are mechanically consistent.
3. Read `spec/microservices/<service>/01-business-rules.md` — every BR-ID must have at least one test assertion
4. Read `spec/microservices/<service>/03-api-design.md` — endpoint details, error responses, operation descriptions
5. Read `spec/microservices/<service>/02-domain-model.md` — entity relationships (for test data setup and
   ordering) AND the implicit-system sections that drive extra case-classes: `### Entity State Model` +
   `### Data Invariants` (Layer A), `### Database Logic Objects` (Layer C). Also check `01-business-rules.md`
   for `Extension Point:` annotations (Layer B). These determine which mandatory case-classes apply (Step 3.9).

**The agent MUST NOT write any test assertions until all five inputs have been read for the current service.**

**Global headers extraction (from Step 1):** After reading `04-api-contract.yaml`, identify ALL entries in the `x-global-headers` extension (or per-operation required header parameters). These MUST appear in the generated test suite's header configuration section. Common patterns:
- `x-tenant-id` → set `TENANT_HEADER="x-tenant-id: <testValue>"`
- `x-store-id` → set `STORE_HEADER="x-store-id: <testValue>"`
- `Authorization` → set `AUTH_HEADER="Authorization: Bearer <testValue>"`

If the contract defines required headers but the agent generates a test suite WITHOUT them, EVERY mutating test (POST/PUT/DELETE) will fail because the service scopes queries by those headers.

### Step 2: Plan Test Coverage

Before writing the script, produce a coverage plan:

| BR-ID | Test Type | Endpoint | Happy Path? | Error Case? | Notes |
|-------|-----------|----------|-------------|-------------|-------|
| BR-XX-001 | Status + JSON field | POST /resource | Yes | Yes (missing required field) | |
| BR-XX-002 | JSON regex | GET /resource/:id | Yes | Yes (not found → 404) | |
| ... | | | | | |

**Rules:**
- Every BR-ID in `01-business-rules.md` MUST appear at least once
- Every endpoint in the API contract MUST be tested (at minimum: success case + one error case)
- State transitions must be verified (create → read → update → verify state → delete)
- Error cases test specific HTTP status codes from the contract

### Step 3: Generate Test Suite

Generate the test script following the structure from `.github/skills/saam-test-suite-template/SKILL.md`:

1. Use the template's helper functions (`assert_status`, `assert_json_field`, `assert_json_regex`)
2. Organize tests into sections by BR-ID group (matching the business rules document structure)
3. **For EVERY test assertion, look up the field name in `04-api-contract.yaml` — NOT from `01-business-rules.md` Concrete Examples.** BR-ID examples may use inconsistent naming. The contract is authoritative.
4. Use EXACT endpoint paths from the contract (including base path prefix like `/api/v1/<service>`)
5. Assert EXACT status codes defined in the contract for each operation (201 for creation, 200 for retrieval, 422 for validation error — check each one)
6. **Use `extract_field "$LAST_BODY" "fieldName"` for all ID/value extraction from responses** — NEVER use `grep -o '"id":[0-9]*'` which breaks on nested objects serialized before root fields
7. Capture IDs from POST responses (never from subsequent GET lists)
8. Build tests in dependency order (create entities before testing operations on them)
9. **Generate ALL applicable mandatory case-classes from `.github/skills/saam-test-suite-template/SKILL.md`** — not just happy
   path. In addition to Contract-Conformance and Behavioral Assertion cases (always mandatory), generate
   the implicit-system case-classes WHEN the service's spec has the corresponding section:
   - **State Machine & Invariant Cases** (Layer A) — if `02-domain-model.md` has `### Entity State Model`
     and/or `### Data Invariants`: illegal-transition-rejected, guard-enforced, terminal-accepts-none,
     invariant-holds (app/both tier), computed-value-non-placeholder.
   - **Extension Point Cases** (Layer B) — if any BR-ID has an `Extension Point:` annotation: behavior
     varies with config, default-when-unconfigured, UD-field round-trip.
   - **DB-Tier Object Cases** (Layer C) — if `02-domain-model.md` has `### Database Logic Objects`:
     function/proc computes real value, view read-model returns the set, trigger enforces invariant on a
     direct write, placement-honored (no app-tier reimplementation).
   Record each case-class's results separately in TEST_RESULTS so the fidelity audit can distinguish
   shape-pass from lifecycle/integrity/config/db-tier coverage. Omit a case-class ONLY when its spec
   section is absent (most services will have some but not all).

**CRITICAL — Field Name Resolution Order:**
```
When writing a test assertion that references a response field:
  1. FIRST: look up the field in 04-api-contract.yaml schemas → use that exact name
  2. NEVER: copy field names from 01-business-rules.md Concrete Examples
  3. NEVER: invent field names based on domain model column names
  4. IF the contract and the BR-ID example disagree: the CONTRACT wins
```

**Why BR-ID examples can't be trusted for naming:** Business rules are extracted during Phase 4 BEFORE the API contract is finalized. The examples in rules may use snake_case, abbreviations, or inconsistent naming. The contract (`04-api-contract.yaml`) is generated AFTER the domain model and applies a consistent naming convention (camelCase for JSON, kebab-case for paths). Always use the contract.

### Step 4: Save Test Suite

Save to: `validation/<service-name>/comprehensive-test-suite.sh`

```bash
chmod +x validation/<service-name>/comprehensive-test-suite.sh
```

**NEVER save to `spec/` or `sourcecode/` directories.** The `validation/` directory is the sole home for test suites.

### Step 5: Validate Assertions Against Contract and Spec

After generating, cross-check EVERY assertion against the API contract:

**Contract compliance (MUST pass — no exceptions):**
- [ ] Every endpoint path in the test matches a path in `04-api-contract.yaml` exactly (including base path prefix)
- [ ] Every field name asserted in test output matches the corresponding schema field in the contract exactly (case-sensitive)
- [ ] Every expected status code matches the contract's response definition for that operation
- [ ] List response assertions use the contract's envelope structure (e.g., `"items"` not `"data"` or `"results"`)
- [ ] Error assertions use the contract's `ErrorResponse` schema field names
- [ ] Query parameter names match the contract (e.g., `pageSize` not `limit` or `page_size`)

**Spec coverage (MUST pass):**
- [ ] Every BR-ID from `01-business-rules.md` has at least one test
- [ ] Test ordering respects entity dependencies (can't test "update order" before "create order")

**Technical correctness:**
- [ ] Pre-flight health check uses absolute URL (not relative to BASE_URL)
- [ ] No `assert_json_regex` used for decimal/numeric values (use `assert_json_field` instead)
- [ ] IDs captured from POST creation responses (not from GET list responses)

**If ANY contract mismatch is found:** Fix the test assertion to match the contract. The contract is authoritative — if a field is named `serviceLevelTarget` in the contract, the test MUST assert `"serviceLevelTarget"`, even if the BR-ID example says `"service_level_target"`.

### Step 6: Human Review

**🔴 PROMPT HUMAN**: "Test suite generated for [Service] with [N] tests covering [M] business rules. Key coverage: [summary of what's tested]. Please review `validation/<service>/comprehensive-test-suite.sh` for completeness."

## Quality Gates

A test suite is NOT ready for Phase 5 unless:

- [ ] Zero BR-IDs are untested
- [ ] All field names come from the API contract (no invented names)
- [ ] No "skip" mechanism exists in the helper functions
- [ ] Tests include both happy path AND error cases for every endpoint
- [ ] State is verified via GET after mutations (POST/PUT/DELETE)
- [ ] Test uses proper ID capture from creation responses
- [ ] Exit code is 0 on all pass, 1 on any failure
- [ ] Summary output format matches: "ALL N TESTS PASSED - 100% SUCCESS"

## Common Pitfalls (from lessons learned)

These are real failure modes encountered across engagements:

| Pitfall | Prevention |
|---------|-----------|
| Missing global headers (tenant/store/auth) | Extract `x-global-headers` from contract in Step 1. Set `TENANT_HEADER` / `STORE_HEADER` at top of test script. All helper functions include them automatically. |
| Field name mismatch between test and code | BOTH must use `04-api-contract.yaml` — read it FIRST |
| Guessing endpoint path format | Read contract paths exactly (`/api/v1/orders` not `/api/v1/order`) |
| Wrong status code assertions | Contract defines 201 for creation, 200 for retrieval — don't assume |
| Capturing IDs from GET lists | Always capture from POST creation response |
| Using regex for decimals | `assert_json_field` with plain string pattern — no regex escaping issues |
| Relative health check URL | Use absolute `http://localhost:PORT/health` — never `$BASE_URL/../health` |
| Testing against stale state | Reset database before running full suite (fresh state per run) |
| Floating point comparison | Use string contains for decimal values, not exact match |
| Double-POST for status + body | Use `assert_status_and_capture` — single request gives both |

## Multi-Service Execution

For engagements with multiple services:
- Generate test suites in the SAME ORDER as the service catalog (priority 1 first)
- Each service gets its own tracking entry in `tracking/phase4c-test-suites.md`
- Services can be processed independently (no cross-service dependencies in test suites)
- If a service's API contract references another service's endpoints (integration tests), note this but DON'T test cross-service calls — those belong in Phase 5 integration testing

## Relationship to Phase 5

Phase 4c test suites are generated BEFORE Phase 5 begins. They serve as the acceptance contract:

```
Phase 4c (this phase)          Phase 5 (implementation)
─────────────────────          ──────────────────────────
Generate test suites    ───►   Code must pass these tests
from specs + contract          without modifying the tests
```

**Rules:**
- Phase 5 agents MUST NOT modify test suites to make code pass
- If a test seems wrong during Phase 5, it's flagged for human review — not changed
- Test suites are the QUALITY GATE, not the IMPLEMENTATION GUIDE
- Code generation reads specs; code validation runs tests

## Deliverables

### Backend Test Suites
- [ ] `validation/<service-name>/comprehensive-test-suite.sh` per backend service (executable)
- [ ] Coverage plan documented (every BR-ID mapped to test assertions)
- [ ] All assertions validated against API contract
- [ ] Human review completed per service

### Frontend Test Plan (if frontend spec exists)
- [ ] `validation/<app-name>/frontend-e2e-tests.md` — E2E test plan derived from frontend spec
- [ ] `validation/<app-name>/frontend-test-suite.sh` — executable E2E test script (Playwright-based or curl-based depending on frontend type)
- [ ] Every user flow from `spec/frontend/<app>/03-user-flows.md` has at least one E2E test
- [ ] Every screen from `spec/frontend/<app>/02-screen-inventory.md` is visited in at least one test
- [ ] Error states, loading states, and empty states are tested

### Tracking
- [ ] `tracking/phase4c-test-suites.md` updated with completion status (backend + frontend)

## Frontend Test Generation (If Frontend Spec Exists)

After ALL backend test suites are generated, check: does `spec/frontend/<app-name>/` exist?

**If YES — generate frontend tests:**

1. Read `spec/frontend/<app>/07-frontend-test-plan.md` — this defines what needs testing
2. Read `spec/frontend/<app>/01-api-contract.md` — this defines the API calls the frontend makes
3. Read `spec/frontend/<app>/03-user-flows.md` — this defines the state machines to test
4. Read `spec/frontend/<app>/05-interaction-matrix.md` — this defines every interactive element

Generate `validation/<app-name>/frontend-test-suite.sh`:
- Tests user flows end-to-end (login → navigate → create → verify → logout)
- Validates that API calls happen in the expected sequence
- Checks that error states are handled (mock API failures → verify UI response)
- Tests navigation structure (every route from screen inventory is reachable)
- Tests form validation (submit invalid data → verify error messages)

**Frontend test format:**
- If the frontend is a SPA/web app: generate Playwright test scripts (`*.spec.ts`) in addition to the bash suite
- If the frontend is API-driven (no server-side rendering): the backend `comprehensive-test-suite.sh` already covers the API layer; generate only a lightweight navigation/integration check
- Always generate a `frontend-test-suite.sh` bash wrapper that runs whatever test framework was chosen

### Frontend Integration Test Orchestration

Unlike backend tests (which test one service in isolation), frontend E2E tests require ALL backend services + the frontend to be running simultaneously. The orchestration script handles this.

**Generate `validation/<app-name>/run-frontend-integration.sh`:**

```bash
#!/bin/bash
# Frontend Integration Test Orchestration
# Starts all backend services + frontend, runs Playwright/E2E tests, tears down.
# Uses sourcecode/compose.yaml for backend infrastructure.

set -euo pipefail

COMPOSE_FILE="../../sourcecode/compose.yaml"
FRONTEND_DIR="../../sourcecode/<app-name>"
RESULTS_DIR="./results"
mkdir -p "$RESULTS_DIR"

echo "=== Starting backend infrastructure ==="
(cd ../../sourcecode && podman compose up -d postgres redis)
sleep 5

echo "=== Starting backend services ==="
# Start each backend service (build + run in background)
for service_dir in ../../sourcecode/*/; do
  service=$(basename "$service_dir")
  if [ "$service" = "<app-name>" ] || [ "$service" = "compose.yaml" ]; then continue; fi
  if [ -f "$service_dir/package.json" ]; then
    echo "  Starting $service..."
    (cd "$service_dir" && npm run build && npm run start:test &) 2>/dev/null
  fi
done

echo "=== Waiting for services to be healthy ==="
# Wait for each service's health endpoint (from spec/test-config.yaml ports)
# Timeout: 30 seconds per service
sleep 15  # initial warm-up

echo "=== Starting frontend dev server ==="
(cd "$FRONTEND_DIR" && npm run dev &) 2>/dev/null
sleep 10

echo "=== Running frontend E2E tests ==="
# Run Playwright (or configured test runner)
if [ -f "$FRONTEND_DIR/playwright.config.ts" ]; then
  (cd "$FRONTEND_DIR" && npx playwright test --reporter=list) 2>&1 | tee "$RESULTS_DIR/playwright-output.txt"
  TEST_EXIT=$?
else
  # Fallback: run the bash frontend-test-suite.sh
  ./frontend-test-suite.sh 2>&1 | tee "$RESULTS_DIR/frontend-test-output.txt"
  TEST_EXIT=$?
fi

echo "=== Tearing down ==="
# Kill all background services
pkill -f "npm run start:test" 2>/dev/null || true
pkill -f "npm run dev" 2>/dev/null || true
(cd ../../sourcecode && podman compose down) 2>/dev/null

echo "=== Results ==="
if [ $TEST_EXIT -eq 0 ]; then
  echo "✅ Frontend integration tests PASSED"
else
  echo "❌ Frontend integration tests FAILED (exit code: $TEST_EXIT)"
  echo "See $RESULTS_DIR/ for details"
fi

exit $TEST_EXIT
```

**Key design decisions:**
- Uses `sourcecode/compose.yaml` for shared infrastructure (postgres, redis, rabbitmq)
- Starts each backend service in test mode (background processes)
- Frontend dev server runs separately (Next.js dev, Vite dev, etc.)
- Playwright runs against the dev server URL
- Full teardown after tests (no orphan processes)
- Results captured to `validation/<app-name>/results/` for analysis

**When this runs:**
- NOT during Phase 4c (tests are generated but not run — no services exist yet)
- During Phase 5 AFTER the frontend is implemented
- During Phase 6 after frontend changes
- In CI/CD as the frontend acceptance gate

**If NO (no frontend spec):** Skip frontend tests. Note in exit gate: "No frontend — backend tests only."

## Exit Gate

**PhaseEvent (completed):** Write: `graph_add_node(nodeType="PhaseEvent", id="P4C-completed", properties={phase: "P4C", event: "completed", timestamp: <current ISO timestamp>})`.

**🔴 PROMPT HUMAN**: "Phase 4c complete. Test suites generated:
- Backend: [N] services, [X] total business rules covered, [Y] test assertions
- Frontend: [✅ E2E test plan + test suite for <app-name> | N/A — no frontend]

All suites validated against API contracts. Ready for Phase 5 (Implementation)?"

**Next steps after human approval:**
- Activate `.github/skills/saam-phase5-setup/SKILL.md` to begin the Phase 5 setup wizard
- The setup wizard will verify test suites exist as a precondition
- Update the root `README.md` — add Phase 4c completion summary: test suites generated per service, total test assertions, frontend test coverage, coverage confirmation
