---
name: saam-dotnet-reference-implementation
description: "Canonical ASP.NET Core 10 / .NET Aspire implementation and xUnit integration-test standard, derived from the MS-01 Customer and Identity reference service."
copyright: "Copyright 2024-2026 SoftServe Inc. All rights reserved."
authors: "Max Kozinenko, Roman Kalita (SoftServe)"
---

# SAAM: .NET Reference Implementation Standard (AUTHORITATIVE)

## Purpose

This skill is the **single authority** for *how* a Shopizer microservice and its integration test
suite are built. Other steering files describe *what* to build (specs, contracts, business rules) and
*when* (phase sequencing). They MUST link here rather than restate these conventions — three
divergent restatements of the BR-ID annotation format is precisely the failure this document exists
to prevent.

**Scope:** the backend target stack for this engagement is ASP.NET Core / .NET 10 with .NET Aspire
orchestration, PostgreSQL, and RabbitMQ, as fixed by `spec/shared/infrastructure-patterns.md`.

---

## The Reference (READ THESE FILES FIRST)

Before implementing any service or test suite, read the reference in full. It is not illustrative —
it is the pattern to be reproduced.

| Artifact | Path | What it establishes |
|---|---|---|
| Reference service | `sourcecode/Shopizer.CustomerIdentity/` | Project layout, composition, persistence, error model, auth, events, annotation style |
| Reference test suite | `sourcecode/Shopizer.IntegrationTests/CustomerIdentityComprehensiveTests.cs` | Integration test class shape, naming, fixture usage, arrange helpers, BR traceability |
| Shared fixture | `sourcecode/Shopizer.IntegrationTests/AspireHostFixture.cs` | Host bootstrap, per-service `HttpClient`, seeded identities and tokens, cleanup |
| Aspire host | `sourcecode/Shopizer.AppHost/AppHost.cs` | Resource graph, database naming, port allocation, health checks |
| Service defaults | `sourcecode/Shopizer.ServiceDefaults/` | Health endpoints, telemetry, resilience — consumed, never reimplemented |
| Cross-cutting contract | `spec/shared/infrastructure-patterns.md` | Normative HTTP/tenancy/messaging behavior the reference implements |

**MS-01 is the only fully implemented service.** The remaining services are `Program.cs`-only
scaffolds. Every service implemented from here on MUST match the reference's structure so the
codebase converges rather than fragments.

---

## Part 1 — Service Implementation Standard

### 1.1 Solution and project layout

```
sourcecode/
├── Shopizer.slnx                         # solution
├── Shopizer.AppHost/                     # Aspire orchestration (resource graph)
├── Shopizer.ServiceDefaults/             # shared health/telemetry/resilience
├── Shopizer.IntegrationTests/            # ALL integration tests (one class per service)
└── Shopizer.<Service>/                   # one project per microservice
    ├── Shopizer.<Service>.csproj
    ├── Program.cs
    ├── Dockerfile
    ├── README.md
    ├── implementation-audit.md
    ├── appsettings.json
    ├── appsettings.Development.json
    ├── Properties/launchSettings.json
    ├── Controllers/<Aggregate>Controller.cs
    ├── DTOs/                             # copied VERBATIM from spec/microservices/ms-NN/08-dtos/
    ├── Models/Domain.cs
    ├── Services/<Area>Services.cs
    ├── Data/<Area>Repository.cs
    ├── Data/SchemaInitializer.cs
    └── Middleware/{ErrorMiddleware,TokenMiddleware,HttpIdentity}.cs
```

Project name is `Shopizer.<PascalCaseServiceName>`, matching the domain name in
`spec/microservices/ms-NN/` (e.g. MS-01 "Customer and Identity" → `Shopizer.CustomerIdentity`).

### 1.2 File consolidation (DELIBERATE — do not split)

The reference groups related types into a small number of files. Reproduce this. One-class-per-file
is **not** the convention here.

| File | Contains |
|---|---|
| `Models/Domain.cs` | `RequestContext` record, `DomainException`, every persistence-facing entity, `AuthenticatedIdentity`, `PrincipalExtensions`, `DtoMapper` |
| `Services/<Area>Services.cs` | Multiple cooperating `sealed` services (e.g. `PasswordService`, `TokenData`, `TokenService`, `IdentityService`) |
| `Data/<Area>Repository.cs` | The single repository for the service's aggregates |
| `Data/SchemaInitializer.cs` | All DDL as `const string` raw string literals |

Split only when a file materially exceeds the reference's scale (`IdentityRepository.cs` is ~464
lines, `IdentityServices.cs` ~446, `Domain.cs` ~196). Splitting earlier fragments the service.

### 1.3 `csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <ItemGroup>
    <PackageReference Include="Aspire.RabbitMQ.Client" Version="13.5.3" />
    <ProjectReference Include="..\Shopizer.ServiceDefaults\Shopizer.ServiceDefaults.csproj" />
    <PackageReference Include="Aspire.Npgsql" Version="13.5.3" />
  </ItemGroup>

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

</Project>
```

Rules:
- `net10.0`, `Nullable` and `ImplicitUsings` enabled — non-negotiable.
- Package versions come from `spec/shared/09-dependency-versions.md`. Do not float versions.
- Omit `Aspire.RabbitMQ.Client` only if the service publishes and consumes no events (see
  `Shopizer.Tax` in `AppHost.cs`, which takes no RabbitMQ reference).
- **No ORM package.** See §1.5.

### 1.4 `Program.cs` — composition order is normative

```csharp
using Shopizer.<Service>.Data;
using Shopizer.<Service>.Middleware;
using Shopizer.<Service>.Services;
using Shopizer.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDataSource("<service>db");
builder.AddRabbitMQClient("rabbitmq");
builder.Services.AddSingleton<<Area>Repository>();
builder.Services.AddSingleton<EventPublisher>();
builder.Services.AddScoped<<Area>Service>();
builder.Services.AddSingleton<SchemaInitializer>();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

var app = builder.Build();
await app.Services.GetRequiredService<SchemaInitializer>().InitializeAsync(CancellationToken.None);
app.UseMiddleware<ErrorMiddleware>();
app.UseMiddleware<TokenMiddleware>();
app.MapControllers();
app.MapDefaultEndpoints();
app.Run();
```

Invariants:
- `AddServiceDefaults()` first; `MapDefaultEndpoints()` last. Health endpoints come from
  ServiceDefaults — **never hand-roll `/health`, `/health/alive`, `/health/ready`.**
- The Aspire connection name is `<lowercaseservicename>db` and MUST match the database added in
  `Shopizer.AppHost/AppHost.cs` (`postgres.AddDatabase("customeridentitydb")`).
- Schema initialization runs **before** the middleware pipeline is exercised.
- `ErrorMiddleware` is registered **before** `TokenMiddleware`, so token-validation faults surface as
  contract-shaped errors.
- camelCase + `WhenWritingNull` JSON options are mandatory — the API contract and DTOs assume them.

### 1.5 Persistence — Npgsql ADO.NET, no ORM

- PostgreSQL is the primary store, obtained via `builder.AddNpgsqlDataSource("<service>db")`.
- Use `NpgsqlDataSource` / `NpgsqlCommand` directly. **Do not add EF Core, Dapper, or any ORM.** The
  reference records this as a deliberate decision: it keeps every table and constraint visible and
  prevents accidental cross-service database access.
- All DDL lives in `Data/SchemaInitializer.cs` as `const string` raw string literals, executed at
  startup:
  - `CREATE SCHEMA IF NOT EXISTS <service_schema>;`
  - Enum types created idempotently via `DO $$ BEGIN CREATE TYPE ... EXCEPTION WHEN duplicate_object THEN NULL; END $$;`
  - `CREATE TABLE IF NOT EXISTS` with explicit `CHECK` and `UNIQUE` constraints from
    `02-domain-model.md`
  - A separate `MigrationSql` const for **additive, forward-only** changes applied after `SchemaSql`
- Every tenant-owned row carries `tenant_id` and (where store-scoped) `store_id`, plus
  `created_at`, `updated_at`, `correlation_id` audit columns.
- Uniqueness is scoped to the tenant/store boundary
  (`UNIQUE (tenant_id, store_id, login_name)`), never global.
- **Never query another service's schema.** Cross-service data is obtained over its API or via events.

### 1.6 Error model

```csharp
public sealed class DomainException(string code, string message, int statusCode) : Exception(message)
```

- Business and validation failures throw `DomainException` with a stable SCREAMING_SNAKE code, a
  human-readable message, and the contract's status code (e.g.
  `throw new DomainException("CUSTOMER_IDENTITY_CONFLICT", "Login identifier is already registered for this store", 409);`).
- `ErrorMiddleware` is the sole translator to HTTP. It maps:
  - `DomainException` → its own `StatusCode` + code
  - `FormatException` → `400 INVALID_REQUEST` (malformed route identifiers)
  - anything else → `500 INTERNAL_ERROR`, logged with the exception
- The response body is always `ErrorResponseDto` (`error`, `message`, `statusCode`, `timestamp`,
  `correlationId`) from the copied DTOs. Never leak stack traces or framework HTML error pages.
- Status codes come from `04-api-contract.yaml`, not from developer judgement.

### 1.7 Authentication, authorization, tenancy

- Required request headers: `x-tenant-id`, `x-store-id`, `x-correlation-id`, materialized by
  `RequestContext.From(HttpContext)`.
- `TokenMiddleware` is **non-rejecting**: it parses a `Bearer` token when present, builds a
  `ClaimsPrincipal` (`sub`, `kind`, `tenantId`, `storeId`, role claims), and swallows
  `DomainException`. Authorization is enforced at the action, not the middleware.
- Actions enforce access via `HttpIdentity.RequireSubject(HttpContext, kind, params roles)`, which
  throws `401 UNAUTHORIZED` on a missing/mismatched principal kind and `403 FORBIDDEN` on a role
  miss, returning the subject `Guid`.
- Tokens are HS512 JWTs carrying `sub`, `name`, `aud`, `kind`, `tenantId`, `storeId`, `iat`, `exp`,
  `roles`. The signing secret comes from configuration
  (`"<Service>": { "JwtSecret": ... }`) and MUST throw outside Development when unset.
- Cross-tenant access is **rejected**, never silently returned as an empty success.

### 1.8 Events

- Inject the Aspire `IConnection` and publish through a dedicated `Services/EventPublisher.cs`.
- **Outbox first, publish second.** The domain mutation writes an `event_outbox` row in the same
  transaction; `EventPublisher` then attempts RabbitMQ delivery and marks the row published on
  success. A publish failure is logged, not thrown — the outbox row is the durability guarantee.
- Topic exchange `domain-events`, durable, routing key = event type, `Persistent = true`.
- Payload envelope matches `spec/shared/event-schemas/`: `eventId`, `eventType`, `eventVersion`,
  `occurredAt`, `tenantId`, `storeId`, `correlationId`, then the typed payload.
- Injecting a publisher and never calling it is a skeleton implementation (Phase 5 rule SAAM-01).

### 1.9 DTOs

- Copy `spec/microservices/ms-NN/08-dtos/*.cs` **verbatim** into `sourcecode/Shopizer.<Service>/DTOs/`
  as the FIRST implementation step, before any controller, service, or repository code.
- Do not rename, restructure, merge, or "improve" them. If a DTO looks wrong, check
  `04-api-contract.yaml`; if the contract agrees with the DTO, the DTO is correct.
- Do not create additional request/response DTOs that duplicate a shape already in `08-dtos/`.
  Internal-only types that never cross the API boundary belong in `Models/Domain.cs`.

### 1.10 Code style

- **Primary-constructor dependency injection** on every service, repository, controller, and
  middleware: `public sealed class IdentityService(IdentityRepository repository, ...)`.
- All concrete types are `sealed`. Value shapes are `sealed record`.
- Controllers are thin: `[ApiController]`, `[Route("api/v1")]`, expression-bodied actions that
  resolve identity, delegate to the service, and return the contract shape. Business logic lives in
  `Services/`.
- Async everywhere, with `CancellationToken ct` threaded to the data layer.
- Comment only what needs clarification. The BR annotations (§1.11) carry the business narrative.

### 1.11 BR-ID annotation contract (MANDATORY)

Two canonical forms. Both are in use in the reference; both are required.

**Source code** — an `// @<BR-ID>: <intent sentence>` line immediately above the implementing
method. Multiple rules stack, one per line:

```csharp
// @BR-CUS-001: Login and email uniqueness are checked inside the tenant/store boundary.
// @BR-CUS-002: Self-service loginName is always derived from emailAddress.
// @BR-CUS-005: Passwords are encoded before persistence.
public async Task<(CustomerAccount Customer, AuthenticationResponseDto Token)> RegisterAsync(...)
```

**Integration tests** — an `// @BR-ID: <BR-ID>` comment plus a matching `[Trait]`:

```csharp
// @BR-ID: BR-CUS-019
[Fact]
[Trait("BR", "BR-CUS-019")]
public async Task Test042_PostCustomerAuthLogin_WithWrongPassword_Returns401()
```

Rules:
- The intent sentence states the *effect*, not the mechanics. It is the human-readable proof the rule
  was understood, not restated.
- Annotate **only reachable** methods — a BR-ID on code no endpoint reaches is a false claim
  (Phase 5 rule SAAM-08).
- The `// @BR-ID:` comment and the `[Trait("BR", …)]` value MUST be identical.
- Both flat (`BR-CUS-001`) and grouped (`BR-CUS-NN-005`) forms are valid and both match
  `br_id_pattern.regex` in `.github/saam-calibration.yaml`. `NN` is a real literal group segment in
  this engagement's catalog — do not "correct" it without operator confirmation.
- `graph-mcp/scripts/detect_br_ids.py` projects these annotations into `CLAIMS_IMPLEMENTATION`
  edges. An unannotated rule is an untracked rule.

### 1.12 Per-service deliverables

Every implemented service ships all of these:

1. **`README.md`** — run command, connection/config expectations, required headers, contract base
   path, Docker build note, pointer to `spec/microservices/ms-NN/04-api-contract.yaml`.
2. **`implementation-audit.md`** — three tables: *Decisions made during implementation* (date,
   decision, rationale), *Specification boundaries requiring review* (area, boundary, resolution),
   and a *Validation record*.
3. **`Dockerfile`** — multi-stage, `mcr.microsoft.com/dotnet/sdk:10.0` → `aspnet:10.0`, restore
   before source copy, `EXPOSE 8080`, `ASPNETCORE_URLS=http://+:8080`, `/p:UseAppHost=false`.
   **Build context is `sourcecode/`**, so the `ServiceDefaults` project reference resolves.
4. **AppHost registration** in `Shopizer.AppHost/AppHost.cs`: `AddProject<Projects.Shopizer_<Service>>`
   with its database reference, `rabbitmq` reference where applicable,
   `.WithExternalHttpEndpoints()`, its allocated `WithHttpEndpoint(port: 81NN, name: "http")`, and
   `.WithHttpHealthCheck("/health")`.
5. **Integration test class** per Part 2.

---

## Part 2 — Integration Test Standard

### 2.1 What replaces the bash suites

**xUnit + .NET Aspire integration tests are the sole mandatory quality gate.** The former standalone
bash suites (`validation/<service>/comprehensive-test-suite.sh`) are **DEPRECATED**. Existing files
are retained for historical reference; no new ones are generated, and they are not a gate.

Rationale and its cost: bash+curl was the stack-agnostic gate. Dropping it is acceptable here because
this engagement has a single .NET target stack, and the Aspire host gives real PostgreSQL and
RabbitMQ rather than mocked dependencies. A future multi-stack engagement must revisit this.

**Runtime prerequisite:** these tests boot a real `DistributedApplication` and require a working
container runtime with PostgreSQL and RabbitMQ. A **skipped or non-executed suite is a FAILED gate,
never a pass.** Phase 5 must assert that tests actually ran.

### 2.2 Location and class shape

One file per service: `sourcecode/Shopizer.IntegrationTests/<Service>ComprehensiveTests.cs`.

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

    private static class Payloads { /* ... */ }

    #region <BR Group>
    // tests
    #endregion

    // private helpers last: SendAsync, SelectToken, AssertResponseAsync,
    // Arrange*IdAsync, HasNonEmptyJsonField, TryExtractResourceId
}
```

- `sealed`, primary constructor taking `AspireHostFixture`.
- **Self-contained. Do NOT inherit `ComprehensiveTestBase`.** That base class is legacy (see §2.8).
- Helpers are `private` members of the class, so each service's suite can encode its own auth,
  seeding, and uniquification rules without coupling to other services.

### 2.3 `Payloads`

A `private static class Payloads` of `const string` raw string literals — one per distinct request
shape, named for the operation (`Registration`, `Login`, `ResetPassword`, `CustomerUpdate`, …), plus
`Empty = "{}"` for required-field negative cases.

Field names and casing come from `04-api-contract.yaml` and the DTOs in `08-dtos/` — **never** from
`01-business-rules.md` examples.

### 2.4 Test naming and organization

```
Test{NNN}_{HttpVerb}{ResourcePath}_{Condition}_{Expectation}
```

Examples: `Test001_PostAdminAuthLogin_Returns200WithSubjectId`,
`Test042_PostCustomerAuthLogin_WithWrongPassword_Returns401`.

- `NNN` is a zero-padded sequence number, stable across the file.
- Group tests with `#region` blocks matching the BR groups in `01-business-rules.md`
  (`#region Authentication`, `#region Password Reset`, …).
- Order regions so arrange-dependencies come first.

### 2.5 Request helper

A private `SendAsync(HttpMethod method, string path, string? payload = null, ...)` that:

1. Seeds any precondition the path needs (e.g. `await fixture.EnsureTestResetTokenAsync(...)`).
2. **Uniquifies collision-prone payload fields** — emails, usernames, external provider IDs — with
   `Guid.NewGuid():N`, so re-runs and parallel facts do not trip uniqueness constraints.
3. Rewrites embedded resource IDs to match the path (e.g. `customerId` in a review payload).
4. Builds the `HttpRequestMessage` with `application/json` content.
5. Attaches the right bearer token via a private `SelectToken(path, method)` that maps route prefixes
   to `fixture.CustomerAccessToken` / `fixture.AdminAccessToken`, or an explicitly passed token.
6. Sends through `fixture.<Service>Client`.

### 2.6 Assertions

`AssertResponseAsync(response, expectedStatus, requiredField = null)` asserts the exact status code
and, when given, that the named JSON field is present and non-empty (`HasNonEmptyJsonField` via
`JsonNode`).

Status-code-only assertions are acceptable **only** for negative cases and `204` responses. Every
state-changing operation MUST additionally assert its effect (§2.7).

### 2.7 Mandatory case classes

Beyond the happy path, generate every case class whose corresponding spec section exists. These are
specified in `.github/skills/saam-test-suite-template/SKILL.md`; the short form:

- **Contract conformance** — optional-parameter omission succeeds; required-parameter omission fails;
  exact status codes; exact response shape.
- **Behavioral effect** — the state transition actually happened (re-read it); computed values are
  real, not placeholder zeros; side effects landed (outbox row or downstream read).
- **State machine & invariants** — when `02-domain-model.md` has an Entity State Model or Data
  Invariants: illegal transition rejected, guard enforced, terminal state accepts nothing further,
  invariant holds after operations.
- **Extension points** — when a BR has an `Extension Point:` annotation: behavior varies with config,
  documented default when unconfigured, user-defined field round-trips.
- **DB-tier objects** — when `02-domain-model.md` has Database Logic Objects: function/procedure
  computes the real value through its binding, view returns the expected set, trigger enforces its
  invariant, placement honored.

### 2.8 Arrange helpers and fixture

- `private async Task<string> Arrange<Entity>IdAsync()` creates a prerequisite entity via the API
  (never via direct SQL), extracts its ID with `TryExtractResourceId`, and falls back to
  `SeedResourceId`. IDs come from `POST` responses — **never** from a subsequent `GET` list.
- `AspireHostFixture` owns the `DistributedApplication`, waits for each resource to be healthy
  (`ResourceNotifications.WaitForResourceHealthyAsync`), exposes one `HttpClient` per service, seeds
  baseline identities, exposes `CustomerAccessToken`/`AdminAccessToken`, and cleans up test data.
  Extend it when adding a service; do not spin up a second host.

### 2.9 Legacy `ComprehensiveTestBase` — migrate on touch

Eleven test classes still inherit `ComprehensiveTestBase` (`AssertShellAsync`) — a shape-only,
pre-Aspire-auth pattern that cannot express token selection, payload uniquification, or arrange
chains.

- New suites MUST NOT inherit it.
- **Migrate on touch:** when a service is implemented, its test class is rewritten to the §2.2
  standard in the same unit of work.
- `ComprehensiveTestBase.cs` is deleted only after the last class migrates.

---

## Part 3 — Anti-Patterns (DO NOT REPRODUCE)

The reference is authoritative for structure, not infallible in content. These are real defects
present in `CustomerIdentityComprehensiveTests.cs` today. Recognize them; do not copy them.

1. **Duplicate placeholder tests.** `Test001`–`Test011` are near-identical, all issuing
   `POST /api/v1/admin-auth/login` and asserting `200` + `subjectId`, while carrying eleven
   *different* `[Trait("BR", …)]` values (`BR-CUS-NN-003`, `-006`, `-007`, `-008`, `-009`, `-014`,
   `-020`, `BR-CUS-022`, `BR-CUS-027`, …). This inflates BR coverage metrics without verifying
   anything. **Every BR-ID MUST have at least one assertion that could only pass if that specific
   rule were implemented.**
2. **Trait/BR mismatch.** A `[Trait]` naming a rule the test body does not exercise. The trait is the
   graph's coverage claim; a false claim is worse than an absent one.
3. **Status-code-only assertions on state-changing operations.** `201` proves routing, not that the
   entity was persisted with the right values. Re-read and assert.
4. **Placeholder literals for computed fields.** Returning `total = 0` or `balanced = true` instead
   of computing them (Phase 5 rule SAAM-09).
5. **Annotating unreachable code.** A BR-ID on a method no route reaches (Phase 5 rule SAAM-08).
6. **Hand-rolled health endpoints** instead of `MapDefaultEndpoints()`.
7. **Adding an ORM** or reaching into another service's schema.
8. **Editing the test suite to make code pass.** Code conforms to tests and to the contract, never
   the reverse. A naming mismatch is resolved by reading `04-api-contract.yaml` and the copied DTOs.
9. **Restating this standard** in another steering file instead of linking to it.

---

## Part 4 — Per-Service Exit Checklist

A service is not complete until every item holds.

**Implementation**
- [ ] `DTOs/` copied verbatim from `spec/microservices/ms-NN/08-dtos/`, unmodified
- [ ] Project layout matches §1.1; files consolidated per §1.2
- [ ] `csproj` targets `net10.0` with `Nullable`/`ImplicitUsings`; versions from `09-dependency-versions.md`
- [ ] `Program.cs` composition order matches §1.4 exactly
- [ ] `SchemaInitializer` creates the full schema from `02-domain-model.md`, idempotently
- [ ] All errors flow through `DomainException` → `ErrorMiddleware` → `ErrorResponseDto`
- [ ] Every action enforces identity via `HttpIdentity.RequireSubject`; tenant/store scoping applied to every query
- [ ] Events written to the outbox in-transaction and published via `EventPublisher`
- [ ] Every BR-ID assigned to this service is annotated on a reachable method (§1.11)
- [ ] `README.md`, `implementation-audit.md`, and `Dockerfile` present
- [ ] Registered in `Shopizer.AppHost/AppHost.cs` with database, health check, and allocated port

**Tests**
- [ ] `sourcecode/Shopizer.IntegrationTests/<Service>ComprehensiveTests.cs` exists, self-contained, no `ComprehensiveTestBase`
- [ ] Every BR-ID has ≥1 test whose assertion is specific to that rule (no placeholder duplication)
- [ ] `// @BR-ID:` comment matches `[Trait("BR", …)]` on every fact
- [ ] All applicable mandatory case classes generated (§2.7)
- [ ] Field names and paths sourced from `04-api-contract.yaml`
- [ ] Suite **actually executed** against a live Aspire host — skips are failures

**Verification**
- [ ] `dotnet build sourcecode/Shopizer.slnx` succeeds
- [ ] `dotnet test sourcecode/Shopizer.IntegrationTests --filter "FullyQualifiedName~<Service>ComprehensiveTests"` passes
- [ ] `python3 graph-mcp/scripts/detect_br_ids.py` reports the expected annotation count
- [ ] `validation/run-and-reconcile.sh <service>` produces an artifact and reconciles into the graph
