# Dependency Versions — .NET 10

**Status:** BLOCKED — human approval required  
**Target:** C# / ASP.NET Core on `net10.0`, PostgreSQL, RabbitMQ, Redis, OpenTelemetry  
**Intended machine artifact:** `Directory.Packages.props`

The repository confirms the target stack in `modernization/modernized-architecture.md` and
`.saam/telemetry/phase2-top-down.yaml`, but contains no approved package-version source,
SDK manifest, central package-management file, or Phase 4b version decision. Consequently this
artifact intentionally contains no guessed package pins. Adding plausible-looking versions would
violate the provider-first and human-approval requirements and could select incompatible GA
packages.

The eventual `Directory.Packages.props` must pin, at minimum, the exact approved GA versions for:

| Package family | Required scope | Approved version |
|---|---|---|
| .NET runtime/SDK | `net10.0` build toolchain | **UNCONFIRMED** |
| ASP.NET Core / Microsoft.Extensions | web host, configuration, health checks | **UNCONFIRMED** |
| EF Core and Npgsql | ORM and PostgreSQL provider | **UNCONFIRMED** |
| RabbitMQ client | event transport | **UNCONFIRMED** |
| Redis client | cache/coordination | **UNCONFIRMED** |
| OpenTelemetry | traces, metrics, exporters | **UNCONFIRMED** |
| Validation library | request/domain validation | **UNCONFIRMED** |
| Test framework and assertions | service/contract tests | **UNCONFIRMED** |

**Human approval required:** supply an approved version source or approve a generated
`Directory.Packages.props` with exact GA pins. Until then, transformations must not choose
versions ad hoc and the Phase 4 exit gate remains blocked for this deliverable.

