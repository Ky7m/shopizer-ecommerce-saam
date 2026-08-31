---
name: saam-test-suite-template
description: "Mandatory standalone bash test suite template for end-to-end microservice verification."
copyright: "Copyright 2024-2026 SoftServe Inc. All rights reserved."
authors: "Max Kozinenko, Roman Kalita (SoftServe)"
---

# SAAM: Comprehensive Test Suite Template (MANDATORY)

## Purpose
Every microservice produced by SAAM MUST have a `comprehensive-test-suite.sh` that validates ALL business rules against the running service. This is the acceptance gate.

## Output Location (MANDATORY)

Test suites MUST be saved to `validation/<service-name>/comprehensive-test-suite.sh`:

```
validation/
├── <service-name>/
│   └── comprehensive-test-suite.sh
└── <service-name-2>/
    └── comprehensive-test-suite.sh
```

**NEVER place test suites in:**
- `spec/microservices/<service>/` — specs are the source of truth for code generation; test suites in this directory would be read by the implementation agent
- `sourcecode/<service-name>/` — source code directory is for implementation artifacts only

This separation is a security boundary that enforces "specs drive code, tests verify it."

## API Contract as Naming Authority (MANDATORY)

When generating a test suite, the agent MUST read `spec/microservices/<service>/04-api-contract.yaml` (OpenAPI 3.1) BEFORE writing any test assertions. The contract is the SINGLE SOURCE OF TRUTH for:

- **Field names** — use exactly as defined in contract schemas (e.g., `serviceLevelTarget` not `service_level_target`)
- **Endpoint paths** — use exactly as defined in contract paths (e.g., `/api/v1/sourcing/suppliers` not `/api/v1/sourcing/supplier`)
- **HTTP status codes** — assert exactly the codes defined in the contract for each operation (e.g., 201 for POST success, not 200)
- **Response shapes** — structure assertions per the contract's schema (e.g., `{ "items": [...], "pagination": {...} }` for list responses)
- **Query parameter names** — use exactly as defined (e.g., `pageSize` not `page_size` or `limit`)
- **Error response shape** — assert against the standard `ErrorResponse` schema from the contract

**Protocol:**
1. Read `04-api-contract.yaml` for the service
2. For each endpoint being tested, extract: path, method, request schema, response schema, status codes
3. Write test assertions using the EXACT field names from the contract schemas
4. Never invent, guess, or infer field names — if it's not in the contract, the contract needs updating first
5. **Never copy field names from `01-business-rules.md` Concrete Examples** — those examples may use inconsistent naming (written before the contract was finalized). The contract is authoritative for ALL naming.

**If the contract doesn't exist:** STOP. The contract must be generated during Phase 4 before test suite generation can proceed. Inform the user: "API contract (04-api-contract.yaml) not found for <service>. It must be generated before test suites."

**If a BR-ID example uses different field names than the contract:** The CONTRACT wins. Example: if BR-ID says `"service_level_target": 0.95` but the contract schema has `serviceLevelTarget: number`, the test asserts `"serviceLevelTarget"` — not `"service_level_target"`.

## Contract-Conformance Test Cases (MANDATORY — beyond happy path)

Happy-path tests (all params supplied, valid body) do NOT catch implementation-vs-contract drift.
A service can pass every happy-path test and still violate its own contract. The test suite MUST
include these conformance cases for EACH endpoint:

### 1. Optional-parameter omission
For every parameter the contract marks OPTIONAL (or with a default), include a test that OMITS it
and asserts success (not 400/500). Common defect: the implementation makes an optional parameter
required, so omitting it returns an error the contract says should not happen.

```bash
# Contract says activeOnly is optional (default true) → omitting it MUST succeed
assert_status "GET /api/v1/<domain>/vendors" 200   # no activeOnly param
```

### 2. Required-parameter omission (negative case)
For every REQUIRED parameter, include a test that omits it and asserts the contract's error code
(400 or 422 — whatever the contract specifies), NOT 500. A 500 means the missing input reached
business logic instead of being rejected at the boundary.

### 3. Status-code fidelity
Assert the EXACT status code the contract specifies for each outcome — not just "2xx". Common
defect: contract says 201 for create, implementation returns 200; or contract says 422 for
validation failure, implementation returns 400 or 500.

### 4. Response-shape fidelity
Assert the response contains EXACTLY the fields the contract schema defines (names, nesting,
list-vs-single). Common defect: implementation returns a flat object where the contract defines
`{ items: [...], total: N }`, or uses different field casing.

### 5. Schema-vs-entity drift (data layer)
Include at least one test per entity that reads a persisted record back and asserts every
contract-defined field is populated. Common defect: the DB column name diverged from the
ORM entity/contract (e.g., contract `acctType` but column created as `accountType`), causing
500s on read. This is the ONLY way the test suite surfaces stale-schema drift.

**These cases are not optional.** A service that passes happy-path but fails conformance cases
is NOT accepted. Record conformance-case pass/fail separately from happy-path in TEST_RESULTS.

## Behavioral Assertion Cases (MANDATORY — beyond shape, assert EFFECT)

Contract-conformance cases check that a response has the right SHAPE. They do NOT check that the
operation actually DID anything. A stub that returns `{ posted: true, linesPosted: 0 }` with a 200
passes every shape check while doing nothing. Behavioral assertions close that hole: they assert the
EFFECT of an operation, which a stub cannot fake.

For every BR-ID with `Intent: State Transition` or non-empty `Side Effects`, the suite MUST assert
the effect — not just the HTTP response:

### 1. State transition actually happened
After an operation that changes an entity's state, READ the entity back and assert the new state.
```bash
# After POST .../batches/{id}/post — the batch/transactions MUST actually be posted
POST .../batches/${id}/post            # asserts 200
GET  .../batches/${id}                 # assert status == "Posted" (NOT still "Open")
GET  .../transactions?batchId=${id}    # assert every line status == "Posted"
```

### 2. Computed values are real (non-zero / correct)
For any field the spec marks computed, assert it is NOT a placeholder. A hardcoded 0 is the classic
skeleton tell.
```bash
# After posting a batch with known line amounts, the total MUST be computed
GET .../batches/${id}    # assert totalAmount == <expected sum>, and totalAmount > 0
```
Where the exact value is known from the test setup, assert the exact value; otherwise assert the
invariant (non-zero, balanced debits==credits, count matches input).

### 3. Side effects occurred (events / cross-service writes)
For every BR-ID whose Side Effects publish an event or call another service, assert the effect is
observable — a consumed event, a row written in the target entity, or a recorded outbound call.
```bash
# WF publishes PaymentIssuedEvent — assert it was emitted
# (via a test consumer, an outbox row, or the downstream read that proves it landed)
GET .../payments/${id}    # assert an emitted-event / outbox record exists for this operation
```
If the environment cannot observe the real broker, assert the outbox/record the operation writes
before publishing — never skip the side-effect assertion.

### 4. Reachability implied
If a BR-ID is annotated in code but NO endpoint reaches its logic, no behavioral test can exercise
it — the behavioral suite will show it uncovered. That uncovered behavioral case IS the dead-code
signal (see the Implementation Fidelity audit in Phase 5).

**Why behavioral assertions are the primary anti-skeleton control:** they are the forcing function
that a stub cannot satisfy. A skeleton returns the right shape but fails "state changed / amount
non-zero / event emitted." Record behavioral-case results separately in TEST_RESULTS so the fidelity
audit can distinguish shape-pass from effect-pass.

## State Machine & Invariant Cases (MANDATORY when the service has an Entity State Model / Data Invariants)

Applies when `02-domain-model.md` has an `### Entity State Model` and/or `### Data Invariants` section
(Layer A). Shape/behavioral cases prove an operation returns the right thing and does something; these
prove the system REFUSES what the legacy would refuse — the illegal transitions and invariant violations
that "green CRUD" happily allows. A generated service can pass every happy-path test and still let an
entity reach a state the legacy never permitted.

### 1. Illegal transition is rejected
For each entity state machine, pick a transition NOT in the model (or one whose guard fails) and assert
the operation is REJECTED (validation error — 409/422, NOT 500) AND the entity's state is UNCHANGED.
```bash
# ledger_batch model has NO Posted -> Draft transition
POST .../batches/${postedId}/reopen     # assert 409/422 (illegal transition)
GET  .../batches/${postedId}            # assert status still "Posted"
```

### 2. Guard is enforced on a legal transition
For a legal transition whose guard is unmet, assert rejection; then satisfy the guard and assert success.
```bash
# Validated -> Posted requires debits == credits
POST .../batches/${unbalancedId}/post   # assert 422 (guard failed: not balanced)
# ...balance the batch...
POST .../batches/${balancedId}/post     # assert 200, status becomes "Posted"
```

### 3. Terminal state accepts no further transitions
```bash
POST .../batches/${voidedId}/post       # assert 409/422 (Voided is terminal)
```

### 4. Data invariant holds after operations (app / both tier)
For each `app`/`both`-tier invariant, attempt an operation that would violate it and assert rejection;
for `computed` invariants, assert the value equals its source expression (not a placeholder).
```bash
# INV-GL-002: line amount == qty * unitPrice (computed)
POST .../batches/${id}/lines {"qty": 3, "unitPrice": 10.00}
GET  .../batches/${id}/lines            # assert the line's amount == 30.00 (computed, not 0)
```
(For `db`/`both`-tier invariants, the DB-Tier Object Cases below assert the DB backstop.)

Record state-machine and invariant case results separately in TEST_RESULTS so the fidelity audit can
see lifecycle/integrity coverage distinct from happy-path.

## Extension Point Cases (MANDATORY when the service has configurable rules)

Applies when the service has BR-IDs annotated `Extension Point:` (Layer B — the behavior is configurable
per instance via the extensibility engine). A generated service can pass happy-path with a hardcoded value
and silently freeze one instance's behavior into the common code — the exact Layer B failure. These cases
prove the behavior actually VARIES with configuration and has a sane default when unconfigured.

For each extension point (from `spec/shared/extensibility-model.md`):

### 1. Extension point resolves (behavior varies with config)
Set a configuration/metadata value, exercise the rule, and assert the behavior reflects that value; then
change the value and assert the behavior changes accordingly.
```bash
# EXT-AP-001: approval threshold is a configurable parameter
# configure threshold = 1000, submit a 1500 invoice -> requires approval
# reconfigure threshold = 2000, submit the same 1500 invoice -> auto-approved
```

### 2. Default when unconfigured
With NO instance configuration for the point, assert the rule uses the documented default behavior (not a
crash, not a hardcoded surprise).
```bash
# no threshold configured -> engine applies the documented default (e.g., all invoices require approval)
```

### 3. User-defined field round-trips (udf/metadata mechanism)
If the point is a UD field / metadata mechanism, define an instance field, write a value through the API,
read it back, and assert it persisted and is returned — proving the mechanism is real, not stubbed.
```bash
# define UD field "costCenter" for an order line; create a line with it; GET the line -> costCenter present
```

Record extension-point case results separately in TEST_RESULTS.

## DB-Tier Object Cases (MANDATORY when the service has db-tier placed logic)

Applies ONLY when `02-domain-model.md` has a `### Database Logic Objects` table (most services
don't — app-tier is the default). For those that do, the placement decision (or a mandatory-DB
integrity invariant) says specific logic MUST run in the database. A test that only exercises the
HTTP surface can pass while the DB object is missing, stubbed in the app, or silently bypassed —
which would rebuild the app-tier bottleneck the placement decision rejected. These cases assert the
EFFECT is produced BY the DB object, exercised through the real DB sidecar the validate harness
already runs.

For each row in the Database Logic Objects table, add the matching case(s):

### 1. Function / procedure computes the real value (through its binding)
Drive the endpoint whose `Binding` maps to the function/proc, then assert the computed value is
correct AND non-placeholder — the same anti-skeleton bar as behavioral case 2, but it also proves
the DB object exists (a missing function would error, not return 0).
```bash
# BR mapped to db-function compute_batch_total via GlRepository.computeTotal
POST .../batches/${id}/post            # asserts 200
GET  .../batches/${id}                 # assert totalAmount == <expected sum>, and totalAmount > 0
```

### 2. View-backed read model returns the expected set
For a `view` bound as a read model, assert the read endpoint returns rows matching the view's
definition (seed known rows, assert only the qualifying ones appear).
```bash
GET .../orders?status=open             # assert only open orders returned, count matches seeded open rows
```

### 3. Trigger enforces its invariant (integrity holds even on a direct/edge write)
For a `trigger` enforcing a mandatory-DB invariant, attempt the operation that would violate the
invariant and assert it is REJECTED by the database (not merely by app validation). Where the suite
can only go through the API, drive the path that reaches the DML the trigger guards.
```bash
# trg_enforce_balanced enforces INV-GL-001 (posted batch must balance)
POST .../batches/${id}/post   with unbalanced lines   # assert rejected (422/409), batch stays Open
```

### 4. Placement honored (no app-tier reimplementation)
This is a review assertion, not an HTTP call: confirm the `Implements` BR-ID's app method is the
binding (a call to the DB object), not a reimplementation of the logic in application code. The
fidelity audit + `_graph-context.md` `Implementation.tier` are the machine signal; the test suite
records the expectation so a regression (someone moves the logic back into app code) is visible.

Record DB-tier case results separately in TEST_RESULTS (alongside behavioral cases) so the fidelity
audit can tell "effect produced by the DB object" from "effect faked in app code."

## Template

```bash
#!/bin/bash

# Comprehensive Test Suite for <Service Name> v2.0
# Tests ALL <N> Business Rules (BR-<DOM>-001 to BR-<DOM>-NNN)
# ALL tests execute against the running service - ZERO SKIPS
# Generated by SAAM Framework

BASE_URL="http://localhost:<PORT>"
# NOTE: If spec/test-config.yaml exists, PORT and AUTH values should come from there.
# The test-config.yaml is generated during Phase 5 setup and provides:
#   - JWT test token (pre-signed, matches service JWT secret)
#   - Service ports (per-service, matching compose.yaml)
#   - Test account IDs (consistent UUIDs for reproducible tests)
# When generating tests in Phase 4c, use contract's x-global-headers testValues.
# When RUNNING tests in Phase 6, values from test-config.yaml override if present.
PASSED=0
FAILED=0
TOTAL=0

GREEN='\033[0;32m'
RED='\033[0;31m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
YELLOW='\033[1;33m'
NC='\033[0m'

declare -a FAILED_TESTS

# ============================================
# Global Headers (from API contract — applies to ALL requests)
# ============================================
# Extract required headers from 04-api-contract.yaml security/parameters sections.
# These are included in EVERY curl call automatically by the helper functions.
# Examples: tenant isolation, auth tokens, store scoping, correlation IDs.
#
# MANDATORY: If the API contract defines ANY of these, they MUST appear here:
#   - x-tenant-id / x-store-id (multi-tenancy/store isolation)
#   - Authorization (JWT/Bearer token)
#   - x-correlation-id (tracing)
#
# The test suite WILL FAIL without these if the service enforces them.

TENANT_HEADER="x-tenant-id: test-tenant-001"  # From contract security — adjust per service
# AUTH_HEADER="Authorization: Bearer <test-token>"  # Uncomment if auth is required
# STORE_HEADER="x-store-id: test-store-001"  # Uncomment if store isolation is required

# Build the global headers string used by all helper functions
GLOBAL_HEADERS="-H '$TENANT_HEADER' -H 'Content-Type: application/json'"
# Append additional headers as needed:
# GLOBAL_HEADERS="$GLOBAL_HEADERS -H '$AUTH_HEADER'"
# GLOBAL_HEADERS="$GLOBAL_HEADERS -H '$STORE_HEADER'"

# ============================================
# Helper Functions
# ============================================

# Extract a top-level field from JSON response (handles nested objects, UUIDs, string IDs)
# Usage: extract_field "$LAST_BODY" "id"
# NEVER use grep for ID extraction — nested objects may serialize before the root ID
extract_field() {
    local json=$1
    local field=$2
    if command -v jq &>/dev/null; then
        echo "$json" | jq -r ".$field"
    else
        echo "$json" | python3 -c "import sys,json; print(json.load(sys.stdin)['$field'])"
    fi
}

assert_status() {
    local test_num=$1
    local test_name=$2
    local rule_id=$3
    local http_method=$4
    local url=$5
    local payload=$6
    local expected_status=$7

    TOTAL=$((TOTAL + 1))
    echo -e "\n${BLUE}Test $test_num: $test_name${NC}"
    echo -e "  Rule: $rule_id | Expected Status: $expected_status"

    if [ -n "$payload" ] && [ "$payload" != "null" ]; then
        actual_status=$(curl -s -o /dev/null -w '%{http_code}' -X "$http_method" "$url" \
            -H 'Content-Type: application/json' -H "$TENANT_HEADER" -d "$payload")
    else
        actual_status=$(curl -s -o /dev/null -w '%{http_code}' -X "$http_method" "$url" \
            -H "$TENANT_HEADER")
    fi

    if [ "$actual_status" = "$expected_status" ]; then
        echo -e "  ${GREEN}✅ PASSED${NC} (HTTP $actual_status)"
        PASSED=$((PASSED + 1))
        return 0
    else
        echo -e "  ${RED}❌ FAILED${NC} (Expected: $expected_status, Got: $actual_status)"
        FAILED=$((FAILED + 1))
        FAILED_TESTS+=("Test $test_num: $test_name [$rule_id] - Expected $expected_status, Got $actual_status")
        return 1
    fi
}

assert_json_field() {
    local test_num=$1
    local test_name=$2
    local rule_id=$3
    local http_method=$4
    local url=$5
    local payload=$6
    local json_pattern=$7

    TOTAL=$((TOTAL + 1))
    echo -e "\n${BLUE}Test $test_num: $test_name${NC}"
    echo -e "  Rule: $rule_id | Expected pattern: $json_pattern"

    if [ -n "$payload" ] && [ "$payload" != "null" ]; then
        result=$(curl -s -X "$http_method" "$url" \
            -H 'Content-Type: application/json' -H "$TENANT_HEADER" -d "$payload")
    else
        result=$(curl -s -X "$http_method" "$url" -H "$TENANT_HEADER")
    fi

    if echo "$result" | grep -q "$json_pattern"; then
        echo -e "  ${GREEN}✅ PASSED${NC}"
        PASSED=$((PASSED + 1))
        return 0
    else
        echo -e "  ${RED}❌ FAILED${NC}"
        echo "  Expected pattern: $json_pattern"
        echo "  Got: $(echo "$result" | head -c 300)"
        FAILED=$((FAILED + 1))
        FAILED_TESTS+=("Test $test_num: $test_name [$rule_id] - Pattern '$json_pattern' not found")
        return 1
    fi
}

assert_json_regex() {
    local test_num=$1
    local test_name=$2
    local rule_id=$3
    local http_method=$4
    local url=$5
    local payload=$6
    local regex=$7

    TOTAL=$((TOTAL + 1))
    echo -e "\n${BLUE}Test $test_num: $test_name${NC}"
    echo -e "  Rule: $rule_id | Regex: $regex"

    if [ -n "$payload" ] && [ "$payload" != "null" ]; then
        result=$(curl -s -X "$http_method" "$url" \
            -H 'Content-Type: application/json' -H "$TENANT_HEADER" -d "$payload")
    else
        result=$(curl -s -X "$http_method" "$url" -H "$TENANT_HEADER")
    fi

    if echo "$result" | grep -qE "$regex"; then
        echo -e "  ${GREEN}✅ PASSED${NC}"
        PASSED=$((PASSED + 1))
        return 0
    else
        echo -e "  ${RED}❌ FAILED${NC}"
        echo "  Regex not matched: $regex"
        echo "  Got: $(echo "$result" | head -c 300)"
        FAILED=$((FAILED + 1))
        FAILED_TESTS+=("Test $test_num: $test_name [$rule_id] - Regex '$regex' not matched")
        return 1
    fi
}

# Capture response body for extracting IDs
capture_response() {
    local http_method=$1
    local url=$2
    local payload=$3

    if [ -n "$payload" ] && [ "$payload" != "null" ]; then
        curl -s -X "$http_method" "$url" \
            -H 'Content-Type: application/json' -H "$TENANT_HEADER" -d "$payload"
    else
        curl -s -X "$http_method" "$url" -H "$TENANT_HEADER"
    fi
}

# Capture BOTH status code AND response body in a single request
# Usage: capture_with_status "POST" "$URL" "$payload"
# Sets global variables: LAST_STATUS and LAST_BODY
capture_with_status() {
    local http_method=$1
    local url=$2
    local payload=$3
    local response

    if [ -n "$payload" ] && [ "$payload" != "null" ]; then
        response=$(curl -s -w "\n%{http_code}" -X "$http_method" "$url" \
            -H 'Content-Type: application/json' -H "$TENANT_HEADER" -d "$payload")
    else
        response=$(curl -s -w "\n%{http_code}" -X "$http_method" "$url" -H "$TENANT_HEADER")
    fi

    LAST_STATUS=$(echo "$response" | tail -1)
    LAST_BODY=$(echo "$response" | sed '$d')
}

# Assert status + capture body in one call (prevents double-POST problem)
# Use this when you need BOTH the status check AND the response body (e.g., to extract an ID)
assert_status_and_capture() {
    local test_num=$1
    local test_name=$2
    local rule_id=$3
    local http_method=$4
    local url=$5
    local payload=$6
    local expected_status=$7

    TOTAL=$((TOTAL + 1))
    echo -e "\n${BLUE}Test $test_num: $test_name${NC}"
    echo -e "  Rule: $rule_id | Expected Status: $expected_status"

    capture_with_status "$http_method" "$url" "$payload"

    if [ "$LAST_STATUS" = "$expected_status" ]; then
        echo -e "  ${GREEN}✅ PASSED${NC} (HTTP $LAST_STATUS)"
        PASSED=$((PASSED + 1))
        return 0
    else
        echo -e "  ${RED}❌ FAILED${NC} (Expected: $expected_status, Got: $LAST_STATUS)"
        FAILED=$((FAILED + 1))
        FAILED_TESTS+=("Test $test_num: $test_name [$rule_id] - Expected $expected_status, Got $LAST_STATUS")
        return 1
    fi
}

echo "=============================================="
echo "<Service Name> - Comprehensive Test Suite v2.0"
echo "ALL <N> Business Rules - ZERO SKIPS"
echo "=============================================="

# Pre-flight check
echo -e "${YELLOW}Pre-flight: Checking service availability...${NC}"
if ! curl -s -f "$BASE_URL/health" > /dev/null 2>&1; then
    if ! curl -s -o /dev/null -w '%{http_code}' "$BASE_URL/<main-endpoint>" | grep -q "200\|401\|403"; then
        echo -e "${RED}ERROR: Service not running at $BASE_URL${NC}"
        exit 1
    fi
fi
echo -e "${GREEN}✓ Service is running at $BASE_URL${NC}"

# Create temp directory for payloads
TMPDIR=$(mktemp -d)
trap "rm -rf $TMPDIR" EXIT

# ============================================
# SECTION 1: <Rule Group Name> (BR-<DOM>-001, N rules)
# ============================================
echo ""
echo -e "${CYAN}══════════════════════════════════════════════════${NC}"
echo -e "${CYAN}SECTION 1: <Group> (N rules)${NC}"
echo -e "${CYAN}══════════════════════════════════════════════════${NC}"

# IMPORTANT: Capture IDs from creation responses, not from subsequent GETs
# Use assert_status_and_capture when you need BOTH status verification AND the body
assert_status_and_capture 1 "Create entity" "BR-<DOM>-001-1" \
    "POST" "$BASE_URL/entities" \
    '{"name":"Test","type":"Client","createdBy":1}' \
    "201"
ENTITY_ID=$(extract_field "$LAST_BODY" "id")

# For subsequent tests that don't need the body, use assert_status (simpler)
assert_json_field 2 "<Test Description>" "BR-<DOM>-001-2" \
    "GET" "$BASE_URL/entities/$ENTITY_ID" \
    "null" \
    '"name":"Test"'

# ... more tests ...

# ============================================
# SUMMARY
# ============================================
echo ""
echo "================================================================"
echo "TEST SUMMARY"
echo "================================================================"
echo ""
echo -e "${CYAN}Total Tests:  $TOTAL${NC}"
echo -e "${GREEN}Passed:       $PASSED${NC}"
echo -e "${RED}Failed:       $FAILED${NC}"
echo ""

if [ $FAILED -gt 0 ]; then
    echo -e "${RED}Failed Tests:${NC}"
    for test in "${FAILED_TESTS[@]}"; do
        echo "  ✗ $test"
    done
    echo ""
fi

if [ $TOTAL -gt 0 ]; then
    echo "Success Rate: $((PASSED * 100 / TOTAL))%"
fi

echo ""
if [ $FAILED -eq 0 ]; then
    echo -e "${GREEN}✅ ALL $TOTAL TESTS PASSED — 100% SUCCESS${NC}"
    exit 0
else
    echo -e "${RED}❌ $FAILED OF $TOTAL TESTS FAILED${NC}"
    exit 1
fi
```

## Rules for Test Suite Creation

1. **ZERO skip parameter** — the helper functions have no skip option
2. **Every BR-ID tested** — at minimum one test per business rule
3. **Real HTTP calls** — curl against running service, no mocks
4. **Ordered execution** — create before read before update before delete
5. **Temp files for complex payloads** — avoids shell escaping issues
6. **Error cases included** — test 4xx responses for invalid input
7. **State verification** — GET after POST/PUT to confirm state change
8. **Exit code matters** — CI/CD uses exit code to gate deployment
9. **Capture IDs using `extract_field`** — always use `extract_field "$LAST_BODY" "id"` to extract entity IDs from responses. NEVER use `grep -o '"id":[0-9]*' | head -1` — nested objects (descriptions, relationships, etc.) may serialize before the root-level ID due to ORM serialization order, causing grep to capture the WRONG ID.
10. **Verify actual field names** — run a manual curl before writing assertions to confirm response shape matches expectations
11. **Test multiple assertion types** — use `assert_status` for status codes, `assert_json_field` for field presence, `assert_json_regex` for pattern matching
12. **Fresh database per run** — always reset the database before running the full suite to avoid state pollution from previous runs
13. **Use assert_json_field (not assert_json_regex) for numeric values** — grep with `-q` (fixed string matching) is sufficient and avoids regex escaping issues with dots in decimals. Only use `assert_json_regex` when alternation (`|`) or character classes are genuinely needed (e.g., status matching `"status":"(Running|Complete)"`)
14. **ALL field names, paths, and status codes MUST come from `04-api-contract.yaml`** — never invent names. Read the OpenAPI contract before writing any assertion. If a field name in the contract differs from what a manual curl returns, the code has a bug (not the contract). The contract is authoritative.
15. **NEVER call the same endpoint twice to get status + body** — use `assert_status_and_capture` when you need both. Calling `capture_response` then `assert_status` with the same POST payload creates a duplicate record. The combined helper does one request and gives you both `$LAST_STATUS` and `$LAST_BODY`.

## Common Pitfalls (Lessons Learned)

| Pitfall | Solution |
|---------|----------|
| Missing tenant/store/auth headers | Read `x-global-headers` from `04-api-contract.yaml`. Set `TENANT_HEADER` variable at the top. All helpers include it automatically. Without it, findById-with-tenant queries return 404. |
| grep captures wrong ID from nested objects | NEVER use `grep -o '"id":[0-9]*'` — nested objects (TypeORM eager relations, embedded entities) may serialize before the root ID. Use `extract_field "$LAST_BODY" "id"` which uses jq/python3 for correct top-level field access. |
| Capturing IDs from GET lists instead of POST | Always capture from the POST creation response, not from a subsequent GET list (list ordering is non-deterministic) |
| Double-POST when needing status + body | Use `assert_status_and_capture` — single request returns both `$LAST_STATUS` and `$LAST_BODY` |
| Response field name mismatch | Verify with manual curl; API may use `"inherited"` not `"brandingInherited"` |
| DTO type coercion with multipart forms | Use `@Type(() => Number)` on integer DTO fields when FileInterceptor is active |
| Soft-deleted entities still visible | Service should check `is_active` on read — GET for deleted entity should return 404 |
| Stale relationships in test order | Be aware that earlier tests may terminate relationships; use different entities or verify state |
| Shell variable empty in payload | Use `${VAR:-fallback}` pattern and log captured values during development |
| Floating-point regex matching | NEVER use `assert_json_regex` for decimal values. Use `assert_json_field` with a plain string pattern instead. Regex backslash escaping in bash variables is unreliable across shells. For `"serviceLevelTarget":0.975`, use `assert_json_field` with pattern `"serviceLevelTarget":0.97` (grep fixed-string match). |

## Pre-Flight Health Check Rule

The pre-flight health check MUST use the service's actual health endpoint URL directly — NEVER construct it relative to BASE_URL.

**CORRECT pattern:**

```bash
BASE_URL="http://localhost:8001/api/v1/sourcing"

# Pre-flight: use absolute health URL (not relative to BASE_URL)
if ! curl -s -f "http://localhost:8001/health" > /dev/null 2>&1; then
    echo "ERROR: Service not running"
    exit 1
fi
```

**WRONG pattern (produces double-slash or incorrect path):**

```bash
# NEVER do this — "$BASE_URL/../health" produces mangled URLs
if ! curl -s -f "$BASE_URL/../health" > /dev/null 2>&1; then
```

**Why**: `$BASE_URL/../health` relies on curl/server path normalization which is inconsistent. If BASE_URL is `http://host:port/api/v1/service`, then `/../health` becomes `http://host:port/api/v1/health` — not `http://host:port/health`. Always define a separate `HEALTH_URL` variable or hardcode the absolute health endpoint.

**Recommended pattern for all test suites:**

```bash
BASE_URL="http://localhost:${PORT}/api/v1/${SERVICE_NAME}"
HEALTH_URL="http://localhost:${PORT}/health"

# Pre-flight check
if ! curl -s -f "$HEALTH_URL" > /dev/null 2>&1; then
    if ! curl -s -o /dev/null -w '%{http_code}' -H 'x-tenant-id: test-tenant' "$BASE_URL/${MAIN_ENDPOINT}" | grep -q "200\|401\|403"; then
        echo "ERROR: Service not running at $BASE_URL"
        exit 1
    fi
fi
```

## Dependency Mocking (Services with Cross-Service Calls)

Services that call OTHER services (as documented in `05-dependencies.md`) need their dependencies mocked during test suite execution. The comprehensive test suite tests the service IN ISOLATION — dependencies are simulated.

### When Mocking Is Needed

If `05-dependencies.md` lists any sync REST calls to other services, those endpoints must be mocked during testing. Without mocks, the test suite fails because the dependency isn't running.

### Mock Strategy

Start a lightweight mock server before the test suite runs. The mock returns predefined responses that match the provider service's `04-api-contract.yaml`.

```bash
# ============================================
# DEPENDENCY MOCKS (start before main service)
# ============================================

# Mock for payment-service (called by order-service)
# Responds to POST /api/v1/payments/charge with 201 + predefined body
MOCK_PAYMENT_PORT=9001

# Start mock using python3 (available on all systems)
python3 -c "
import http.server, json, sys

class MockHandler(http.server.BaseHTTPRequestHandler):
    def do_POST(self):
        if '/payments/charge' in self.path:
            self.send_response(201)
            self.send_header('Content-Type', 'application/json')
            self.end_headers()
            self.wfile.write(json.dumps({
                'paymentId': 'mock-pay-001',
                'status': 'confirmed',
                'chargedAt': '2026-01-01T00:00:00Z'
            }).encode())
        else:
            self.send_response(404)
            self.end_headers()
    def log_message(self, format, *args): pass

http.server.HTTPServer(('', $MOCK_PAYMENT_PORT), MockHandler).serve_forever()
" &
MOCK_PAYMENT_PID=$!

# Configure the service under test to point to the mock
export PAYMENT_SERVICE_URL="http://localhost:$MOCK_PAYMENT_PORT"
```

### Cleanup

```bash
# After tests complete, kill mock servers
kill $MOCK_PAYMENT_PID 2>/dev/null
```

### Rules for Dependency Mocking

1. **Mock only SYNC REST dependencies** — async events (Kafka/SQS) are tested separately in integration tests, not in the per-service comprehensive suite.
2. **Mock responses MUST match the provider's `04-api-contract.yaml`** — use exact field names, status codes, and response shapes from the contract.
3. **Include error mocks for resilience testing** — if `05-dependencies.md` defines retry/circuit-breaker behavior, add tests that make the mock return 503 and verify the service retries correctly.
4. **Service must be configurable** — dependency URLs must come from environment variables (e.g., `PAYMENT_SERVICE_URL`). The test suite sets these to point at mocks. This is already required by the database configuration rule (env-var driven).
5. **Document which dependencies are mocked** — add a comment block at the top of the test suite listing all mocked services and their ports.

### Template for Mock Section

```bash
# ============================================
# DEPENDENCY MOCKS
# ============================================
# This service depends on:
#   - payment-service (sync REST) → mocked at localhost:9001
#   - inventory-service (sync REST) → mocked at localhost:9002
# Async dependencies (events) are NOT mocked — tested in integration tests only.

# [mock server startup code here]

# Export URLs for the service under test
export PAYMENT_SERVICE_URL="http://localhost:9001"
export INVENTORY_SERVICE_URL="http://localhost:9002"

# ============================================
# START SERVICE UNDER TEST
# ============================================
# Service reads dependency URLs from environment
```

### Services with No Dependencies

If `05-dependencies.md` states "no external dependencies" — no mocks are needed. The test suite starts only the service itself.

## Validation Criteria

A service passes SAAM acceptance when:
```
Total Tests: <N>
Passed: <N>
Failed: 0
Success Rate: 100%
✅ ALL <N> TESTS PASSED - 100% SUCCESS
```

Any other result = service is NOT ready for deployment.
