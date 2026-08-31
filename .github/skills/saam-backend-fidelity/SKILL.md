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

**Stack-neutral.** Examples use the framework's default stack (Java 17 / Spring Boot / JPA)
for consistency. The *principle* in each checkpoint is language-agnostic — the same defect
appears in any async-messaging, multi-service, ORM-backed system. Where a runtime detail
matters, it is named as an example, not a requirement.

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

```java
// SYMPTOM — injected, never called
@Service
public class OrderService {
    private final DomainEventPublisher publisher; // injected...
    public Order place(PlaceOrderCommand cmd) {
        Order order = repository.save(new Order(cmd));
        return order;                              // ...never published
    }
}

// CORRECT — the side effect the spec names is performed
public Order place(PlaceOrderCommand cmd) {
    Order order = repository.save(new Order(cmd));
    publisher.publish(new OrderPlacedEvent(order.getId(), order.getTenantId()));
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
  (an interceptor/filter on the client, not per-call-site copy-paste).
- The integration smoke gate (Phase 5 Stage 5) asserts the callee returns correctly scoped
  data for a real token.

```java
// CORRECT — a single interceptor forwards context on every outbound call
@Bean
public RestClient partnerClient(TenantContext ctx) {
    return RestClient.builder()
        .requestInterceptor((request, body, execution) -> {
            request.getHeaders().add("X-Tenant-Id", ctx.currentTenantId());
            return execution.execute(request, body);
        })
        .build();
}
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

**Symptom:** an entity gains a field after the schema was first created. The ORM's
create-if-missing behavior only creates tables that do not yet exist — it does NOT alter an
existing table to add the new column. The entity maps to a column that is not there → read or
write fails at runtime (often a `500` on a core read).

**Why it slips through:** local/dev profiles frequently drop-and-recreate the schema
(`create-drop`), so the column is always present in dev. The defect only appears where the
schema persists across deployments (`validate` / production).

**Verify:**
- Any field added to an entity after initial generation has a corresponding migration
  (Flyway/Liquibase change set), not just an entity annotation.
- `ddl-auto: validate` (the production default) will fail fast on the mismatch — treat that
  failure as the signal, not noise.
- Assert a round-trip read of the new column (checkpoint 8), not just that the app starts.

```java
// SYMPTOM — new field on an existing entity, no migration
@Entity
public class Account {
    @Id private Long id;
    private String name;
    private String region;   // added later — table never altered → 500 on read under validate
}
```

The create-if-missing-only behavior is not Java-specific — the same trap exists in any ORM
whose auto-DDL creates missing tables but does not reconcile columns on existing ones.

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
- This is the assertion class that catches checkpoints 2, 3, 5, and 7 that an API-only test
  cannot.

```bash
# API says created — that is necessary, not sufficient
curl -s -X POST "$BASE_URL/api/v1/orders" -d '{...}' -o /tmp/resp.json
test "$(jq -r '.status' /tmp/resp.json)" = "PLACED"

# Round-trip: assert the EFFECT in the database, not the response
ROW=$(psql "$DATABASE_URL" -tAc \
  "select tenant_id, total_amount from orders where id = '$ORDER_ID'")
test "$ROW" = "$EXPECTED_TENANT|$EXPECTED_AMOUNT"   # right tenant AND real amount persisted
```

---

## Grep-able Self-Audit (Recurring Wiring Defects)

Run this table as a mechanical self-check during the wiring/integration layers, before the
service is containerized. Each row is a *pattern*; the Java form is one example of a
language-agnostic defect. These make the abstract Anti-Skeleton rules executable — they turn
"don't leave a side effect unwired" into a grep.

| # | Defect (pattern) | Detection | Fix |
|---|------------------|-----------|-----|
| W1 | Publisher injected, never called | Injected publisher/producer with zero call sites on the rule's path | Invoke the publish where the spec Side-Effect names it (checkpoint 1) |
| W2 | Monotonic ID via read-max-then-insert | Read of `MAX(seq)` inside a loop / batch insert → collisions on multi-row writes | Use a DB sequence / identity, or compute the range once outside the loop |
| W3 | ORM shadow relationship on a read path | An un-ignored/unmapped collection navigation triggers a phantom join/FK → error on a core read | Map the relationship explicitly or exclude it from the read model |
| W4 | Async publish inherits the request's cancellation | Request-scoped cancellation token passed into a fire-and-forget publish → the publish is cancelled when the request completes | Use a non-request-scoped token for the in-flight publish |
| W5 | Publish-only bus, no receive side declared | A broker configured for publishing only never deploys its send topology | Declare the topology (e.g. a no-op consumer) so the send side is materialized |
| W6 | Wrong in-cluster port | Outbound call to an external-facing port for an in-cluster service | Call the in-cluster service port, not the ingress/external one |
| W7 | Consumer DTO drifts from provider | Consumer's client DTO differs from the provider's actual request/response shape | Align the consumer DTO to the provider's published contract (checkpoint 4) |
| W8 | Added column, no migration | Entity field with no corresponding column on an existing table → runtime failure under `validate` | Add a migration change set; assert the round-trip (checkpoints 5, 8) |

**Scope note:** W2/W4/W5 describe patterns common in async-messaging and ORM stacks generally.
The runtime specifics (which token, which broker, which sequence primitive) vary by stack — the
detection heuristic and the fix intent do not.

---

## Relationship to existing Phase 5 controls

This guide does not replace any existing gate — it makes them land earlier and more concretely.

| Existing control | What this guide adds |
|------------------|----------------------|
| Anti-Skeleton rules (SAAM-01/08/09) | Named symptoms + greps for the cross-service/persistence forms of "unwired side effect" |
| Generation-time self-audit (Steps 1–4) | The W1–W8 table as the checklist that self-audit runs |
| Integration Runtime Smoke Gate (Stage 5) | Checkpoint 8's DB round-trip as an explicit assertion; W3/W6/W8 caught before deploy instead of only at the gate |
| Stage 1.5 cross-service contract reconciliation | Checkpoint 4 as the code-level confirmation the reconciled shapes are honored |

The smoke gate remains the backstop. The point of this guide is that most of these defects
should be caught at generation/wiring time — before the service is ever built into an image.
