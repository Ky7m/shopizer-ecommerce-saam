---
name: saam-test-suite-template
description: "Mandatory xUnit + .NET Aspire integration-test template — one ComprehensiveTests class per microservice that verifies every business rule against a real Aspire host."
copyright: "Copyright 2024-2026 SoftServe Inc. All rights reserved."
authors: "Max Kozinenko, Roman Kalita (SoftServe)"
---

# SAAM: Comprehensive Test Suite Template (MANDATORY)

## Purpose

Every microservice produced by SAAM MUST have exactly one xUnit integration-test class,
`sourcecode/Shopizer.IntegrationTests/<Service>ComprehensiveTests.cs`, that validates ALL of the
service's business rules against the service **running inside a real .NET Aspire host** (real
PostgreSQL, real RabbitMQ). This class is the acceptance gate.

This document is the authority for the **catalogue of mandatory test-case classes** (what MUST be
covered) and how each case is expressed as an xUnit `[Fact]`. It is NOT the authority for the test
class *shape*. For the canonical class skeleton, naming, fixture usage, `Payloads` convention, BR
annotation contract, and the list of anti-patterns, this file **links to** and does not restate:

> **`.github/skills/saam-dotnet-reference-implementation/SKILL.md`** — the single authority for the
> .NET service and integration-test standard. Read its **Part 2 (Integration Test Standard)** and
> **Part 3 (Anti-Patterns)** before generating any test class.

The gold-standard reference to model everything on:

| Artifact | Path |
|---|---|
| Reference test class | `sourcecode/Shopizer.IntegrationTests/CustomerIdentityComprehensiveTests.cs` |
| Shared fixture | `sourcecode/Shopizer.IntegrationTests/AspireHostFixture.cs` |
| Reference service | `sourcecode/Shopizer.CustomerIdentity/` |

**READ the real reference file before writing anything.** The idioms below are extracted from it;
the file itself is the pattern to reproduce.

## Runtime Prerequisite (MANDATORY)

These tests boot a real `DistributedApplication` via `AspireHostFixture` and require a working
**container runtime with PostgreSQL and RabbitMQ**. There are no mocks for the service under test or
its datastore.

> A **skipped, non-compiled, or non-executed suite is a FAILED gate — never a pass.** Phase 5 must
> assert the tests actually ran (non-zero executed count, zero skips) before a service is accepted.

## Output Location (MANDATORY — the old rule is INVERTED)

One file per service, in the shared integration-test project:

```
sourcecode/
└── Shopizer.IntegrationTests/
    ├── AspireHostFixture.cs                      # shared host + tokens + clients
    ├── CustomerIdentityComprehensiveTests.cs     # reference (MS-01)
    └── <Service>ComprehensiveTests.cs            # one per service
```

- The former rule "**NEVER save test suites to `sourcecode/`**" is now **INVERTED**. Integration
  tests live *inside* `sourcecode/Shopizer.IntegrationTests/` — that is their required home.
- The legacy artifact `validation/<service>/comprehensive-test-suite.sh` (standalone bash/curl/jq) is
  **DEPRECATED**. Existing files are retained for historical reference only. **No new bash suites are
  generated, and they are not a gate.** See `saam-dotnet-reference-implementation/SKILL.md` §2.1 for
  the rationale.
- Still forbidden: placing tests under `spec/microservices/<service>/`. Specs drive code; tests
  verify it. That security boundary is unchanged.

## Run Command

```bash
dotnet test sourcecode/Shopizer.IntegrationTests --filter "FullyQualifiedName~<Service>ComprehensiveTests"
```

Run a single BR's tests via the trait filter: `--filter "BR=BR-CUS-019"`. The full project runs with
`dotnet test sourcecode/Shopizer.IntegrationTests`.

## API Contract as Naming Authority (MANDATORY)

Before writing any assertion, the agent MUST read
`spec/microservices/<service>/04-api-contract.yaml` (OpenAPI 3.1) and the copied DTOs in
`spec/microservices/ms-NN/08-dtos/`. The contract is the SINGLE SOURCE OF TRUTH for:

- **Field names** — use exactly as defined in contract schemas (e.g. `serviceLevelTarget`, not
  `service_level_target`). These become the string keys passed to `AssertResponseAsync(..., "field")`
  and the JSON keys inside `Payloads`.
- **Endpoint paths** — use exactly as defined (e.g. `/api/v1/customers/me`, not `/api/v1/customer`).
  These become the `path` argument to `SendAsync`.
- **HTTP status codes** — assert the EXACT code the contract specifies (e.g. `201` for create, `204`
  for a state change with no body), not just "2xx".
- **Response shapes** — assert the required field via `AssertResponseAsync(response, status, "field")`;
  for list responses assert the contract's wrapper (`items`, `pagination`, …).
- **Query parameter names** — use exactly as defined (`pageSize`, not `page_size`).
- **Error response shape** — assert against the standard `ErrorResponse` schema from the contract.

**Protocol:**
1. Read `04-api-contract.yaml` for the service.
2. For each endpoint, extract: path, method, request schema, response schema, status codes.
3. Build `Payloads` `const string` literals and `AssertResponseAsync` field names using EXACT contract
   naming.
4. Never invent, guess, or infer field names — if it is not in the contract, the contract needs
   updating first.
5. **Never copy field names from `01-business-rules.md` Concrete Examples** — those examples may use
   inconsistent naming written before the contract was finalized. The contract is authoritative.

**If the contract doesn't exist:** STOP. It must be generated in Phase 4 before test generation. Tell
the user: "API contract (04-api-contract.yaml) not found for <service>. It must be generated before
test suites."

**If a BR-ID example uses different field names than the contract:** the CONTRACT wins. If BR-ID says
`"service_level_target": 0.95` but the contract schema has `serviceLevelTarget: number`, the test
asserts `"serviceLevelTarget"`.

## The Fact Shape (MANDATORY)

Every test is a single async `[Fact]` carrying its BR traceability. The four-part shape is
non-negotiable and MUST be reproduced exactly:

```csharp
// @BR-ID: BR-CUS-019
[Fact]
[Trait("BR", "BR-CUS-019")]
public async Task Test042_PostCustomerAuthLogin_WithWrongPassword_Returns401()
{
    using var response = await SendAsync(HttpMethod.Post, "/api/v1/customer-auth/login", Payloads.WrongPassword);
    await AssertResponseAsync(response, 401);
}
```

Rules (full contract in `saam-dotnet-reference-implementation/SKILL.md` §1.11 / §2.4):

- `// @BR-ID: <id>` comment immediately above the fact, and a `[Trait("BR", "<id>")]` with the
  **identical** value. A mismatch is a false coverage claim.
- Method name: `Test{NNN}_{HttpVerb}{ResourcePath}_{Condition}_{Expectation}` — `NNN` zero-padded and
  stable across the file (e.g. `Test001_PostAdminAuthLogin_Returns200WithSubjectId`).
- Every BR-ID assigned to the service MUST have **≥1 fact whose assertion could only pass if that
  specific rule were implemented.** Duplicate placeholder facts that all issue the same call under
  different traits inflate coverage and are an anti-pattern (see the end of this doc).
- Both flat (`BR-CUS-019`) and grouped (`BR-CUS-NN-013`) BR-ID forms are valid; `NN` is a real literal
  segment in this catalog — do not "correct" it.

## Case-Class Applicability Rule (MANDATORY)

Below are the mandatory test-case classes. **Generate every case class whose corresponding spec
section exists. Omit a case class ONLY when its spec section is absent.** Happy-path facts alone do
NOT catch implementation-vs-contract drift or skeleton stubs — a service can pass every happy-path
fact and still violate its own contract or do nothing at all.

| Case class | Mandatory when… |
|---|---|
| Contract-Conformance | Always (every endpoint) |
| Behavioral Assertion | A BR-ID has `Intent: State Transition` or non-empty `Side Effects` |
| State Machine & Invariant | `02-domain-model.md` has `### Entity State Model` and/or `### Data Invariants` |
| Extension Point | A BR-ID has an `Extension Point:` annotation (Layer B) |
| DB-Tier Object | `02-domain-model.md` has a `### Database Logic Objects` table |

---

## Contract-Conformance Test Cases (MANDATORY — beyond happy path)

A service can pass every happy-path test and still violate its own contract. Include these conformance
facts for EACH endpoint.

### 1. Optional-parameter omission
For every parameter the contract marks OPTIONAL (or with a default), add a fact that OMITS it and
asserts success. Common defect: the implementation makes an optional parameter required, so omitting
it returns an error the contract says should not happen.

```csharp
// @BR-ID: BR-CUS-030
[Fact]
[Trait("BR", "BR-CUS-030")]
public async Task Test070_GetCustomers_WithoutActiveOnly_Returns200()
{
    // Contract marks activeOnly optional (default true) -> omitting it MUST succeed
    using var response = await SendAsync(HttpMethod.Get, "/api/v1/customers");
    await AssertResponseAsync(response, 200);
}
```

### 2. Required-parameter omission (negative case)
For every REQUIRED parameter/body, add a fact that omits it (`Payloads.Empty`) and asserts the
contract's error code (`400`/`422`), NOT `500`. A `500` means the missing input reached business logic
instead of being rejected at the boundary.

```csharp
// @BR-ID: BR-CUS-NN-010
[Fact]
[Trait("BR", "BR-CUS-NN-010")]
public async Task Test002_PostAdminAuthLogin_WithEmptyPayload_Returns400()
{
    using var response = await SendAsync(HttpMethod.Post, "/api/v1/admin-auth/login", Payloads.Empty);
    await AssertResponseAsync(response, 400);
}
```

### 3. Status-code fidelity
Assert the EXACT status code the contract specifies for each outcome — pass it as the second argument
to `AssertResponseAsync`. Common defect: contract says `201` for create, implementation returns `200`;
or contract says `422`, implementation returns `400`/`500`.

```csharp
await AssertResponseAsync(response, 201, "subjectId");   // create -> exactly 201
await AssertResponseAsync(response, 204);                // state change, no body -> exactly 204
```

### 4. Response-shape fidelity
Assert the response contains EXACTLY the field(s) the contract schema defines by passing
`requiredField` to `AssertResponseAsync` (it checks presence AND non-emptiness via
`HasNonEmptyJsonField`). Common defect: a flat object where the contract defines a wrapper, or
different field casing.

```csharp
await AssertResponseAsync(response, 200, "subjectId");   // field must exist and be non-empty
```

### 5. Schema-vs-entity drift (data layer)
Add at least one fact per entity that reads a persisted record back and asserts every
contract-defined field is populated. Common defect: the DB column name diverged from the contract
(e.g. contract `acctType` but column created `accountType`), causing `500`s on read. Re-reading is the
only way the suite surfaces stale-schema drift.

```csharp
// @BR-ID: BR-CUS-013
[Fact]
[Trait("BR", "BR-CUS-013")]
public async Task Test090_GetUsersById_ReturnsPersistedFields()
{
    var userId = await ArrangeUserIdAsync();
    using var response = await SendAsync(HttpMethod.Get, $"/api/v1/users/{userId}");
    await AssertResponseAsync(response, 200, "emailAddress");   // read-back proves the column exists
}
```

**These cases are not optional.** A service that passes happy-path but fails conformance cases is NOT
accepted.

---

## Behavioral Assertion Cases (MANDATORY — beyond shape, assert EFFECT)

Conformance cases check the response SHAPE. They do NOT check that the operation actually DID anything.
A stub that returns `{ "posted": true, "linesPosted": 0 }` with `200` passes every shape check while
doing nothing. Behavioral facts close that hole — they assert the EFFECT a stub cannot fake.

For every BR-ID with `Intent: State Transition` or non-empty `Side Effects`, assert the effect, not
just the HTTP response.

### 1. State transition actually happened
After an operation that changes state, READ the entity back in the same fact and assert the new state.

```csharp
// @BR-ID: BR-ORD-014
[Fact]
[Trait("BR", "BR-ORD-014")]
public async Task Test120_PostBatchesPost_TransitionsToPosted()
{
    var batchId = await ArrangeBatchIdAsync();

    using var post = await SendAsync(HttpMethod.Post, $"/api/v1/batches/{batchId}/post");
    await AssertResponseAsync(post, 200);

    using var read = await SendAsync(HttpMethod.Get, $"/api/v1/batches/{batchId}");
    var body = await read.Content.ReadAsStringAsync();
    Assert.Equal("Posted", JsonNode.Parse(body)?["status"]?.ToString());   // NOT still "Open"
}
```

### 2. Computed values are real (non-zero / correct)
For any field the spec marks computed, assert it is NOT a placeholder. A hardcoded `0` is the classic
skeleton tell. Where the exact value is known from setup, assert it; otherwise assert the invariant
(non-zero, debits==credits, count matches input).

```csharp
using var read = await SendAsync(HttpMethod.Get, $"/api/v1/batches/{batchId}");
var total = (decimal?)JsonNode.Parse(await read.Content.ReadAsStringAsync())?["totalAmount"];
Assert.NotNull(total);
Assert.True(total > 0m, "totalAmount must be computed, not a placeholder 0.");
```

### 3. Side effects occurred (events / cross-service writes)
For every BR-ID whose Side Effects publish an event or call another service, assert the effect is
observable — an outbox row, a downstream read that proves it landed, or a consumed event. If the
environment cannot observe the real broker, assert the outbox record the operation writes before
publishing — never skip the side-effect assertion.

```csharp
// After the operation, the downstream read (or outbox) proves the event landed.
using var downstream = await SendAsync(HttpMethod.Get, $"/api/v1/payments/{paymentId}");
await AssertResponseAsync(downstream, 200, "emittedEventId");
```

### 4. Reachability implied
If a BR-ID is annotated in code but NO endpoint reaches its logic, no behavioral fact can exercise it —
the behavioral suite will show it uncovered. That uncovered behavioral case IS the dead-code signal
(see the Implementation Fidelity audit in Phase 5).

**Behavioral assertions are the primary anti-skeleton control:** they are the forcing function a stub
cannot satisfy — a skeleton returns the right shape but fails "state changed / amount non-zero / event
emitted."

---

## State Machine & Invariant Cases (MANDATORY when the domain model defines them)

Applies when `02-domain-model.md` has an `### Entity State Model` and/or `### Data Invariants` section
(Layer A). Shape/behavioral facts prove an operation returns and does something; these prove the system
REFUSES what the legacy would refuse — illegal transitions and invariant violations that "green CRUD"
happily allows.

### 1. Illegal transition is rejected
Pick a transition NOT in the model (or whose guard fails); assert REJECTION (`409`/`422`, NOT `500`)
AND that the entity's state is UNCHANGED.

```csharp
// @BR-ID: BR-ORD-021   (ledger_batch model has NO Posted -> Draft transition)
[Fact]
[Trait("BR", "BR-ORD-021")]
public async Task Test150_PostBatchesReopen_OnPosted_IsRejected()
{
    var batchId = await ArrangePostedBatchIdAsync();

    using var reopen = await SendAsync(HttpMethod.Post, $"/api/v1/batches/{batchId}/reopen");
    await AssertResponseAsync(reopen, 409);                 // illegal transition

    using var read = await SendAsync(HttpMethod.Get, $"/api/v1/batches/{batchId}");
    var body = await read.Content.ReadAsStringAsync();
    Assert.Equal("Posted", JsonNode.Parse(body)?["status"]?.ToString());   // state unchanged
}
```

### 2. Guard is enforced on a legal transition
For a legal transition whose guard is unmet, assert rejection; then satisfy the guard and assert
success.

```csharp
using var unmet = await SendAsync(HttpMethod.Post, $"/api/v1/batches/{unbalancedId}/post");
await AssertResponseAsync(unmet, 422);                      // guard failed: not balanced
// ...balance the batch...
using var ok = await SendAsync(HttpMethod.Post, $"/api/v1/batches/{balancedId}/post");
await AssertResponseAsync(ok, 200);
```

### 3. Terminal state accepts no further transitions

```csharp
using var response = await SendAsync(HttpMethod.Post, $"/api/v1/batches/{voidedId}/post");
await AssertResponseAsync(response, 409);                   // Voided is terminal
```

### 4. Data invariant holds after operations (app / both tier)
For each `app`/`both`-tier invariant, attempt a violating operation and assert rejection; for
`computed` invariants, assert the value equals its source expression (not a placeholder). (`db`/`both`
tier invariants are covered by the DB-Tier Object cases below.)

```csharp
// INV-GL-002: line amount == qty * unitPrice (computed)
using var _ = await SendAsync(HttpMethod.Post, $"/api/v1/batches/{batchId}/lines", Payloads.Line);
using var read = await SendAsync(HttpMethod.Get, $"/api/v1/batches/{batchId}/lines");
var amount = (decimal?)JsonNode.Parse(await read.Content.ReadAsStringAsync())?[0]?["amount"];
Assert.Equal(30.00m, amount);                              // qty 3 * unitPrice 10.00, not 0
```

---

## Extension Point Cases (MANDATORY when the service has configurable rules)

Applies when the service has BR-IDs annotated `Extension Point:` (Layer B — behavior is configurable
per instance via the extensibility engine, per `spec/shared/extensibility-model.md`). A generated
service can pass happy-path with a hardcoded value and silently freeze one instance's behavior into the
common code — the exact Layer B failure. These facts prove behavior VARIES with configuration and has a
sane default when unconfigured.

### 1. Extension point resolves (behavior varies with config)
Set a config/metadata value, exercise the rule, assert the behavior reflects it; change the value,
assert the behavior changes.

```csharp
// @BR-ID: BR-INV-005   (EXT-AP-001: approval threshold is configurable)
[Fact]
[Trait("BR", "BR-INV-005")]
public async Task Test200_ApprovalThreshold_VariesWithConfiguration()
{
    await ConfigureThresholdAsync(1000);
    using var requiresApproval = await SendAsync(HttpMethod.Post, "/api/v1/invoices", Payloads.Invoice1500);
    await AssertResponseAsync(requiresApproval, 202, "approvalRequired");

    await ConfigureThresholdAsync(2000);
    using var autoApproved = await SendAsync(HttpMethod.Post, "/api/v1/invoices", Payloads.Invoice1500);
    await AssertResponseAsync(autoApproved, 201, "approvedAt");
}
```

### 2. Default when unconfigured
With NO instance configuration for the point, assert the rule uses the documented default behavior
(not a crash, not a hardcoded surprise).

```csharp
// no threshold configured -> engine applies the documented default (all invoices require approval)
using var response = await SendAsync(HttpMethod.Post, "/api/v1/invoices", Payloads.Invoice1500);
await AssertResponseAsync(response, 202, "approvalRequired");
```

### 3. User-defined field round-trips (udf / metadata mechanism)
If the point is a UD field / metadata mechanism, define an instance field, write a value through the
API, read it back, and assert it persisted — proving the mechanism is real, not stubbed.

```csharp
// define UD field "costCenter"; create a line with it; GET the line -> costCenter present
using var create = await SendAsync(HttpMethod.Post, "/api/v1/order-lines", Payloads.LineWithCostCenter);
var lineId = TryExtractResourceId(await create.Content.ReadAsStringAsync());
using var read = await SendAsync(HttpMethod.Get, $"/api/v1/order-lines/{lineId}");
await AssertResponseAsync(read, 200, "costCenter");
```

---

## DB-Tier Object Cases (MANDATORY when the service has db-tier placed logic)

Applies ONLY when `02-domain-model.md` has a `### Database Logic Objects` table (most services don't —
app-tier is the default). The placement decision says specific logic MUST run in the database. A test
that only exercises the HTTP surface can pass while the DB object is missing, stubbed in the app, or
bypassed — rebuilding the app-tier bottleneck the placement decision rejected. These facts assert the
EFFECT is produced BY the DB object, exercised through the real PostgreSQL resource the Aspire host runs.

### 1. Function / procedure computes the real value (through its binding)
Drive the endpoint whose `Binding` maps to the function/proc; assert the value is correct AND
non-placeholder. A missing function errors rather than returning `0`, so this also proves existence.

```csharp
// BR mapped to db-function compute_batch_total via BatchRepository.ComputeTotal
using var post = await SendAsync(HttpMethod.Post, $"/api/v1/batches/{batchId}/post");
await AssertResponseAsync(post, 200);
using var read = await SendAsync(HttpMethod.Get, $"/api/v1/batches/{batchId}");
var total = (decimal?)JsonNode.Parse(await read.Content.ReadAsStringAsync())?["totalAmount"];
Assert.True(total is > 0m, "totalAmount must be computed by the DB function.");
```

### 2. View-backed read model returns the expected set
For a `view` bound as a read model, seed known rows and assert the read endpoint returns only the
qualifying ones.

```csharp
using var read = await SendAsync(HttpMethod.Get, "/api/v1/orders?status=open");
var items = JsonNode.Parse(await read.Content.ReadAsStringAsync())?["items"]?.AsArray();
Assert.All(items!, node => Assert.Equal("open", node?["status"]?.ToString()));
```

### 3. Trigger enforces its invariant (integrity holds even on a direct write)
For a `trigger` enforcing a mandatory-DB invariant, attempt the violating operation and assert it is
REJECTED by the database (not merely app validation). Drive the path that reaches the DML the trigger
guards.

```csharp
// trg_enforce_balanced enforces INV-GL-001 (posted batch must balance)
using var response = await SendAsync(HttpMethod.Post, $"/api/v1/batches/{unbalancedId}/post");
await AssertResponseAsync(response, 422);                   // rejected; batch stays Open
```

### 4. Placement honored (no app-tier reimplementation)
This is a review assertion, not an HTTP call: confirm the `Implements` BR-ID's app method is the
binding (a call to the DB object), not a reimplementation of the logic in application code. The
fidelity audit + `_graph-context.md` `Implementation.tier` are the machine signal; the test class
records the expectation so a regression (logic moved back into app code) is visible.

---

## Template (xUnit — model on `CustomerIdentityComprehensiveTests.cs`)

A complete, compilable skeleton. Reproduce this structure; read the real reference file for the full
set of idioms before filling it in.

```csharp
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Net.Http.Headers;

namespace Shopizer.IntegrationTests;

[Collection(ShopizerAspireCollection.Name)]
public sealed class <Service>ComprehensiveTests(AspireHostFixture fixture)
{
    private const string SeedResourceId = "00000000-0000-0000-0000-000000000001";
    private static readonly HttpMethod Patch = new("PATCH");

    private static class Payloads
    {
        public const string Empty = "{}";
        public const string Create = """{"name":"phase4c-test","code":"phase4c-test"}""";
        public const string Update = """{"name":"phase4c-test","isActive":true}""";
        // one const per distinct request shape; field names/casing from 04-api-contract.yaml + 08-dtos/
    }

    #region <BR Group — e.g. Catalog>

    // @BR-ID: BR-<DOM>-001
    [Fact]
    [Trait("BR", "BR-<DOM>-001")]
    public async Task Test001_PostEntities_Returns201WithId()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/entities", Payloads.Create);
        await AssertResponseAsync(response, 201, "id");
    }

    // @BR-ID: BR-<DOM>-001
    [Fact]
    [Trait("BR", "BR-<DOM>-001")]
    public async Task Test002_PostEntities_WithEmptyPayload_Returns400()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/entities", Payloads.Empty);
        await AssertResponseAsync(response, 400);
    }

    // @BR-ID: BR-<DOM>-002   (behavioral: read-back proves persistence)
    [Fact]
    [Trait("BR", "BR-<DOM>-002")]
    public async Task Test003_GetEntitiesById_ReturnsPersistedName()
    {
        var entityId = await ArrangeEntityIdAsync();
        using var response = await SendAsync(HttpMethod.Get, $"/api/v1/entities/{entityId}");
        await AssertResponseAsync(response, 200, "name");
    }

    #endregion

    // ---- private helpers (last) ----

    private Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, string? payload = null) =>
        SendCoreAsync(method, path, payload, fixture.AdminAccessToken, selectToken: true);

    private async Task<HttpResponseMessage> SendCoreAsync(
        HttpMethod method, string path, string? payload, string? explicitToken, bool selectToken)
    {
        // 1. Seed any precondition this path needs, e.g.:
        //    if (path.Contains("/password-resets/default/phase4c-token", StringComparison.Ordinal))
        //        await fixture.EnsureTestResetTokenAsync("phase4c-token", administrator: false);

        // 2. Uniquify collision-prone fields so re-runs / parallel facts don't trip unique constraints.
        if (payload == Payloads.Create)
        {
            payload = payload.Replace("phase4c-test", $"phase4c-{Guid.NewGuid():N}", StringComparison.Ordinal);
        }

        // 3. Rewrite embedded resource IDs to match the path (e.g. a customerId inside a nested payload).
        //    payload = payload?.Replace(SeedResourceId, idFromPath, StringComparison.Ordinal);

        using var request = new HttpRequestMessage(method, path);
        if (payload is not null)
        {
            request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
        }

        // 4. Attach the right bearer token.
        var token = selectToken ? SelectToken(path, method) : explicitToken;
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await fixture.<Service>Client.SendAsync(request);
    }

    private string? SelectToken(string path, HttpMethod method)
    {
        if (path.StartsWith("/api/v1/entities/me", StringComparison.Ordinal))
        {
            return fixture.CustomerAccessToken;
        }

        if (path.StartsWith("/api/v1/entities", StringComparison.Ordinal))
        {
            return fixture.AdminAccessToken;
        }

        return null;
    }

    private static async Task AssertResponseAsync(
        HttpResponseMessage response, int expectedStatus, string? requiredField = null)
    {
        Assert.Equal(expectedStatus, (int)response.StatusCode);
        if (requiredField is not null)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.True(
                HasNonEmptyJsonField(body, requiredField),
                $"Response is missing non-empty JSON field '{requiredField}'.");
        }
    }

    private async Task<string> ArrangeEntityIdAsync()
    {
        var payload = Payloads.Create.Replace("phase4c-test", $"phase4c-{Guid.NewGuid():N}", StringComparison.Ordinal);
        using var response = await SendAsync(HttpMethod.Post, "/api/v1/entities", payload);
        var body = await response.Content.ReadAsStringAsync();
        return TryExtractResourceId(body) ?? SeedResourceId;   // ID from POST, NEVER from a subsequent GET list
    }

    private static bool HasNonEmptyJsonField(string body, string field)
    {
        try
        {
            var value = JsonNode.Parse(body)?[field];
            return value is not null && value.ToJsonString() is not "null" and not "\"\"";
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string? TryExtractResourceId(string body)
    {
        try
        {
            var root = JsonNode.Parse(body);
            foreach (var field in new[] { "id", "subjectId", "customerId", "productId", "orderId", "storeId", "userId" })
            {
                var value = root?[field]?.ToString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }
        }
        catch (JsonException)
        {
            // 204 / empty bodies are valid and have no resource ID.
        }

        return null;
    }
}
```

### What each helper does (idioms from the reference)

| Helper | Responsibility |
|---|---|
| `SendAsync` | Public entry; seeds preconditions, uniquifies collision-prone fields via `Guid.NewGuid():N`, rewrites embedded IDs, builds the request, attaches the token, sends through `fixture.<Service>Client`. |
| `SelectToken(path, method)` | Maps route prefixes to `fixture.CustomerAccessToken` / `fixture.AdminAccessToken`. |
| `AssertResponseAsync(response, status, field?)` | Asserts the exact status; when `field` given, asserts it is present and non-empty. |
| `Arrange<Entity>IdAsync()` | Creates a prerequisite entity via the API, extracts its ID with `TryExtractResourceId`, falls back to `SeedResourceId`. IDs come from `POST` responses — never a subsequent `GET` list. |
| `HasNonEmptyJsonField(body, field)` | `JsonNode`-based presence + non-emptiness check (rejects `null` / `""`). |
| `TryExtractResourceId(body)` | `JsonNode`-based extraction of the first known ID field; tolerant of empty `204` bodies. |

`AspireHostFixture` owns the `DistributedApplication`, waits for each resource to be healthy
(`ResourceNotifications.WaitForResourceHealthyAsync`), exposes one `HttpClient` per service and the
`CustomerAccessToken`/`AdminAccessToken`, seeds baseline identities, and cleans up test data. Extend it
when adding a service — never spin up a second host.

## Legacy `ComprehensiveTestBase` — migrate on touch

Several legacy test classes still inherit `ComprehensiveTestBase` (a shape-only, pre-Aspire-auth
`AssertShellAsync` pattern that cannot express token selection, payload uniquification, or arrange
chains).

- New suites MUST NOT inherit it — they are self-contained per the template above.
- **Migrate on touch:** when a service is implemented, its test class is rewritten to this standard in
  the same unit of work.
- `ComprehensiveTestBase.cs` is deleted only after the last class migrates.

## Anti-Patterns (see `saam-dotnet-reference-implementation/SKILL.md` Part 3)

Part 3 of the reference-implementation skill is the authoritative anti-pattern list. Three defects
that this test template most directly guards against:

1. **Duplicate placeholder tests.** In the reference file today, `Test001`–`Test011` are near-identical
   — all issuing `POST /api/v1/admin-auth/login` and asserting `200` + `subjectId` while carrying
   eleven *different* `[Trait("BR", …)]` values. This inflates BR coverage without verifying anything.
   **Every BR-ID MUST have ≥1 assertion that could only pass if that specific rule were implemented.**
   Do NOT reproduce this pattern in new suites.
2. **Trait/BR mismatch.** A `[Trait]` naming a rule the test body does not exercise. The trait is the
   graph's coverage claim; a false claim is worse than an absent one. The `// @BR-ID:` comment and the
   `[Trait]` value MUST be identical AND actually exercised.
3. **Status-code-only assertions on state-changing operations.** A `201`/`200` proves routing, not that
   the entity was persisted with the right values or that the transition happened. Re-read and assert
   the effect (Behavioral / State-Machine cases above).

## Validation Criteria

A service passes SAAM acceptance when its class runs under the Aspire host with **every applicable case
class present**, every BR-ID specifically asserted, and:

```
dotnet test sourcecode/Shopizer.IntegrationTests --filter "FullyQualifiedName~<Service>ComprehensiveTests"
Passed!  - Failed: 0, Skipped: 0, Passed: <N>
```

Any skipped, non-executed, or failing result = service is NOT ready for deployment. A suite that did
not run against a real host is a FAILED gate, never a pass.
