---
name: saam-backend-fidelity
description: "Guidelines for cross-service wiring, event emission, tenant propagation, and round-trip backend fidelity verification."
copyright: "Copyright 2024-2026 SoftServe Inc. All rights reserved."
authors: "Max Kozinenko, Roman Kalita (SoftServe)"
---

# SAAM: Backend Fidelity — Cross-Service Wiring & Round-Trip

## Purpose

Phase 5 already forbids skeletons at the *service* level: no stubs, no algorithm
simplification, computed fields must be computed (`.github/skills/saam-phase5-ai-dlc-implementation/SKILL.md`,
Anti-Skeleton rules SAAM-01 / SAAM-08 / SAAM-09). Those rules govern a single method in
isolation.

This guide governs the layer where services meet each other and the database — event
emission, cross-service calls, and schema persistence. It is where a service that passes
its own unit tests and returns a clean `200` is still functionally broken: the event never
left the process, the tenant context never reached the callee, or the new column was never
written. Structural gates (build passes, shape tests pass, container is 1/1 Ready) do not
catch this class. Only a round-trip assertion against real state does.

**Read this when implementing the "Events" and "Integration Wiring" layers** (Model A Step 3
event/integration layers; Model B Task 3–4; Model C Units 1–3). It is the procedure those
layer names imply but do not spell out.

**Stack.** The principle in each checkpoint is language-agnostic — the same defect appears in
any async-messaging, multi-service, database-backed system. The examples below use this
engagement's actual target stack (ASP.NET Core 10 / .NET Aspire / Npgsql / RabbitMQ) as
established by `.github/skills/saam-dotnet-reference-implementation/SKILL.md` and demonstrated
in `sourcecode/Shopizer.CustomerIdentity/`. Read that skill before the wiring layers — it
defines the concrete `EventPublisher`, `RequestContext`/`HttpIdentity`, `TokenMiddleware`, and
`SchemaInitializer` patterns these checkpoints assume.

---

## The 8 Fidelity Checkpoints

Each checkpoint is stated as **symptom → why it passes every structural gate → what to
verify**. Work through them for any service that publishes events, calls another service, or
persists a schema that changed after first generation.

### 1. Event emission actually fires

**Symptom:** a publisher/producer is dependency-injected and configured, but no code path
calls it. The wiring exists; the invocation does not.

**Why it slips through:** the bean is present, the app starts, health is green. Nothing
proves the publish is ever reached.

**Verify:**
- Grep the call sites of the injected publisher. Injection with zero call sites is the
  signature.
- Every business rule whose spec Side-Effect names "Publishes: `<event>`" MUST have a
  reachable call to the publisher on the path that implements that rule.
- Per the reference pattern, the outbox row is written **inside** the mutation, and
  `EventPublisher` then attempts delivery and marks the row published. A publish failure is
  logged, not thrown — but an *absent* publish call is the defect.

```csharp
// SYMPTOM — injected, never called
public sealed class OrderService(OrderRepository repository, EventPublisher events)
{
    public async Task<Order> PlaceAsync(PlaceOrderRequestDto request, RequestContext context, CancellationToken ct)
    {
        var order = await repository.AddOrderAsync(new Order(request), context, ct);
        return order;                            // events was injected... and never used
    }
}

// CORRECT — the side effect the spec names is performed, after durable persistence
// @BR-ORD-012: Placing an order emits the approved OrderPlaced domain event.
public async Task<Order> PlaceAsync(PlaceOrderRequestDto request, RequestContext context, CancellationToken ct)
{
    var order = await repository.AddOrderAsync(new Order(request), context, ct); // writes event_outbox in the same transaction
    await events.PublishOrderPlacedAsync(order, context, ct);
    return order;
}
```

This is the cross-service face of SAAM-01: a named side effect that is not performed is a
skeleton, even when the surrounding method is otherwise real.

### 2. Stubbed endpoints implemented for real

**Symptom:** an endpoint returns a well-shaped, plausible response without performing the
operation behind it (see SAAM-01 in the Phase 5 guide).

**Verify:** for each endpoint the spec marks as an operation (not a pure read), confirm the
write / computation / side effect the workflow recipe names is actually performed — not a
constructed literal response. Checkpoint 8 is the ultimate test of this.

### 3. Tenant / context propagation on outbound calls

**Symptom:** a service authenticates the inbound request and scopes its own data correctly,
then calls a second service with an HTTP client that carries none of that context. The callee
receives an unscoped or unauthenticated request.

**Why it slips through:** both services pass their own suites in isolation. The gap only
exists on the wire between them.

**Verify:**
- Every outbound client to another in-system service attaches the tenant/context propagation
  (a `DelegatingHandler` registered on the typed client, not per-call-site copy-paste).
- The context forwarded is the inbound `RequestContext` (`x-tenant-id`, `x-store-id`,
  `x-correlation-id`) resolved by `HttpIdentity.Context(HttpContext)` — not a value
  reconstructed from the path or a default.
- The integration smoke gate (Phase 5 Stage 5) asserts the callee returns correctly scoped
  data for a real token.

```csharp
// CORRECT — a single handler forwards context on every outbound call
public sealed class ContextPropagationHandler(IHttpContextAccessor accessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var context = RequestContext.From(accessor.HttpContext!);
        request.Headers.TryAddWithoutValidation("x-tenant-id", context.TenantId);
        request.Headers.TryAddWithoutValidation("x-store-id", context.StoreId);
        request.Headers.TryAddWithoutValidation("x-correlation-id", context.CorrelationId);
        return base.SendAsync(request, ct);
    }
}

// Program.cs
builder.Services.AddTransient<ContextPropagationHandler>();
builder.Services.AddHttpClient<CatalogClient>().AddHttpMessageHandler<ContextPropagationHandler>();
```

### 4. Cross-service client DTO matches the callee's ACTUAL shape

**Symptom:** the consumer defines a request/response DTO from its own assumption of the
provider's contract. The provider's real request type differs (a renamed field, an
optional-made-required, a different envelope). The call compiles and deserializes partially,
then fails or silently drops data at runtime.

**Why it slips through:** the consumer's unit tests use the consumer's own DTO — they never
see the provider's real shape.

**Verify:**
- The consumer's client DTO is derived from the provider's published contract
  (`04-api-contract.yaml` for the provider), not hand-written from the consumer's expectation.
- Field names, required/optional, and the response envelope match the provider exactly.
- This is the runtime form of the Stage 1.5 cross-service contract reconciliation — if the
  reconciliation was done, the shapes already agree; this checkpoint confirms the code honors
  it.

### 5. Schema evolution needs an explicit migration

**Symptom:** an entity gains a field after the schema was first created. `SchemaInitializer`'s
`CREATE TABLE IF NOT EXISTS` only creates tables that do not yet exist — it does NOT alter an
existing table to add the new column. The repository reads or writes a column that is not
there → failure at runtime (often a `500` on a core read).

**Why it slips through:** a freshly-provisioned dev database is always created from the latest
`SchemaSql`, so the column is always present locally. The defect only appears where the schema
persists across deployments — including the Aspire Postgres resource, which is declared
`.WithLifetime(ContainerLifetime.Persistent)` in `Shopizer.AppHost/AppHost.cs`.

**Verify:**
- Any column added after initial generation has a corresponding **additive, forward-only**
  statement in `SchemaInitializer.MigrationSql` (`ALTER TABLE ... ADD COLUMN IF NOT EXISTS`),
  not just a new property on the domain type and a new `SchemaSql` column.
- `MigrationSql` runs after `SchemaSql` on every startup and must be idempotent.
- Assert a round-trip read of the new column (checkpoint 8), not just that the app starts.

```csharp
// SYMPTOM — new field on an existing entity, only added to SchemaSql
public sealed class CustomerAccount
{
    public Guid Id { get; set; }
    public string LoginName { get; set; } = null!;
    public DateTimeOffset? LastPasswordResetAt { get; set; }  // added later
}

private const string SchemaSql = """
    CREATE TABLE IF NOT EXISTS customer_identity.customer_accounts (
      ..., last_password_reset_at timestamptz);   -- IF NOT EXISTS → existing table untouched
    """;

// CORRECT — an additive migration reconciles the existing table
private const string MigrationSql = """
    ALTER TABLE customer_identity.customer_accounts
      ADD COLUMN IF NOT EXISTS last_password_reset_at timestamptz;
    """;
```

The create-if-missing-only behavior is not stack-specific — the same trap exists in any ORM
or DDL initializer that creates missing tables but does not reconcile columns on existing ones.

### 6. Build constraints under the batch/container runtime

**Symptom:** a build step that works on a developer laptop fails or behaves differently inside
the sealed batch/container build (network-restricted, no local caches, different working
directory).

**Why it slips through:** the local build has state the sealed build does not — a populated
dependency cache, ambient credentials, a warm working tree.

**Verify:**
- The build command used in generation matches the one the container/batch runtime will use.
- Do not assume flags that depend on pre-populated local state behave the same in a clean,
  network-restricted build context. Prefer the restore/fetch the sealed build performs itself.

This matters most for Model C (headless batch generation) where the sealed build IS the build.

### 7. Seed real values and reconcile context before asserting

**Symptom:** a round-trip test writes and reads back, but the seed data is a placeholder
(zeroes, empty context) so the assertion passes vacuously — it confirms the plumbing, not the
behavior.

**Verify:**
- Seed representative, non-trivial values (real amounts, a real tenant/context) before the
  round-trip assertion.
- Confirm the context used to write is the context used to read — a mismatch here masks the
  tenant-propagation defect from checkpoint 3.

### 8. Verify the round-trip against the DATABASE, not the 200

**This is the capstone.** A `200`/`201` response proves the request was accepted and shaped
correctly. It does NOT prove the effect happened, went to the right place, or persisted.

**Why it matters:** an API-level behavioral test reads back through the *same* code path that
wrote. If that path writes to the wrong tenant, drops a column, or no-ops the effect, the
read-back can still return a plausible value and the test passes. The write and the read share
the same blind spot.

**Verify:**
- For each operation, after the API call succeeds, read the resulting state directly from the
  database (or an independent path) and assert the specific values the workflow recipe named.
- Assert on the *effect*: the row exists, the computed value is non-zero and correct, the
  linkage/foreign key resolves, the column added in checkpoint 5 holds the written value.
- In integration tests this means going around the service. `AspireHostFixture` already uses
  `_application.GetConnectionStringAsync("<db>")` + `new NpgsqlConnection(...)` for seeding and
  cleanup; expose a public accessor following that same pattern and assert through it.
- This is the assertion class that catches checkpoints 2, 3, 5, and 7 that an API-only test
  cannot.

```csharp
// AspireHostFixture — expose the connection the seed/cleanup helpers already use
public async Task<NpgsqlConnection> OpenDatabaseAsync(string resourceName)
{
    var connectionString = await _application!.GetConnectionStringAsync(resourceName)
        ?? throw new InvalidOperationException($"No connection string for {resourceName}.");
    var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    return connection;
}
```

```csharp
// API says created — that is necessary, not sufficient
using var response = await SendAsync(HttpMethod.Post, "/api/v1/orders", Payloads.Order);
await AssertResponseAsync(response, 201, "id");

// Round-trip: assert the EFFECT in the database, not the response
await using var connection = await fixture.OpenDatabaseAsync("ordermanagementdb");
await using var command = new NpgsqlCommand(
    "select tenant_id, total_amount from order_management.orders where id = @id", connection);
command.Parameters.AddWithValue("id", Guid.Parse(orderId));
await using var reader = await command.ExecuteReaderAsync();
Assert.True(await reader.ReadAsync(), "Order row was not persisted.");
Assert.Equal(ExpectedTenant, reader.GetString(0));      // right tenant...
Assert.Equal(ExpectedAmount, reader.GetDecimal(1));     // ...AND a real computed amount
```

---

## Grep-able Self-Audit (Recurring Wiring Defects)

Run this table as a mechanical self-check during the wiring/integration layers, before the
service is containerized. Each row is a *pattern*; the .NET form is one example of a
language-agnostic defect. These make the abstract Anti-Skeleton rules executable — they turn
"don't leave a side effect unwired" into a grep.

| # | Defect (pattern) | Detection | Fix |
|---|------------------|-----------|-----|
| W1 | Publisher injected, never called | `EventPublisher` in a primary constructor with zero call sites on the rule's path | Invoke the publish where the spec Side-Effect names it (checkpoint 1) |
| W2 | Monotonic ID via read-max-then-insert | Read of `MAX(seq)` inside a loop / batch insert → collisions on multi-row writes | Use a DB sequence / `gen_random_uuid()`, or compute the range once outside the loop |
| W3 | Phantom join on a read path | A repository read selects a column or joins a table that `SchemaSql` never created → error on a core read | Add the column/table to `SchemaSql` + `MigrationSql`, or drop it from the read |
| W4 | Async publish inherits the request's cancellation | `context.RequestAborted` / the action's `CancellationToken ct` passed into a fire-and-forget publish → the publish is cancelled when the request completes | Use a non-request-scoped token for the in-flight publish |
| W5 | Publish-only bus, no receive side declared | An exchange declared for publishing only never materializes its consumer topology | Declare the topology (e.g. a no-op consumer binding) so the send side is materialized |
| W6 | Wrong in-cluster address | Outbound call to a hardcoded `localhost:81NN` instead of the Aspire service reference | Resolve the callee through its Aspire resource name / typed `HttpClient`, not the ingress port |
| W7 | Consumer DTO drifts from provider | Consumer's client DTO differs from the provider's actual request/response shape | Align the consumer DTO to the provider's published contract (checkpoint 4) |
| W8 | Added column, no migration | Domain property with no corresponding column on an existing table — present in `SchemaSql` but absent from `MigrationSql` | Add an `ALTER TABLE ... ADD COLUMN IF NOT EXISTS` change; assert the round-trip (checkpoints 5, 8) |
| W9 | Tenant filter omitted on a query | A repository method taking `RequestContext` that never uses `TenantId`/`StoreId` in its `WHERE` clause | Scope every tenant-owned query; reject cross-tenant access rather than returning an empty success |

**Scope note:** W2/W4/W5 describe patterns common in async-messaging and database-backed stacks
generally. The runtime specifics (which token, which broker, which sequence primitive) vary by
stack — the detection heuristic and the fix intent do not.

---

## Relationship to existing Phase 5 controls

This guide does not replace any existing gate — it makes them land earlier and more concretely.

| Existing control | What this guide adds |
|------------------|----------------------|
| Anti-Skeleton rules (SAAM-01/08/09) | Named symptoms + greps for the cross-service/persistence forms of "unwired side effect" |
| `.github/skills/saam-dotnet-reference-implementation/SKILL.md` | The concrete stack patterns (`EventPublisher`/outbox, `HttpIdentity.Context`, `TokenMiddleware`, `SchemaInitializer`) these checkpoints verify are actually wired |
| Generation-time self-audit (Steps 1–4) | The W1–W9 table as the checklist that self-audit runs |
| Integration Runtime Smoke Gate (Stage 5) | Checkpoint 8's DB round-trip as an explicit assertion; W3/W6/W8 caught before deploy instead of only at the gate |
| Stage 1.5 cross-service contract reconciliation | Checkpoint 4 as the code-level confirmation the reconciled shapes are honored |

The smoke gate remains the backstop. The point of this guide is that most of these defects
should be caught at generation/wiring time — before the service is ever built into an image.
