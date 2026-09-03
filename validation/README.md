# `validation/`

## What lives here

| Path | Status | Purpose |
|---|---|---|
| `run-and-reconcile.sh` | **ACTIVE** | Builds the solution, runs a service's xUnit integration suite via `dotnet test`, writes the reconciliation artifact, and calls `graph-mcp/scripts/reconcile_validation.py` |
| `.saam/reconciliation/<service>/` (outside this tree) | ACTIVE | Run artifacts consumed by the graph |
| `ms-NN/comprehensive-test-suite.sh` | **DEPRECATED** | Legacy standalone bash suites |
| `atx-check.sh.template` | Historical | ATX pipeline helper |
| `comprehensive-validation-summary.md` | Historical | Phase 3 validation record |

## The bash suites are deprecated

`validation/ms-NN/comprehensive-test-suite.sh` are **no longer generated and are not a quality
gate.** They are retained for historical reference only. `run-and-reconcile.sh` does not execute
them.

The mandatory quality gate is now the xUnit + .NET Aspire integration suite:

```
sourcecode/Shopizer.IntegrationTests/<Service>ComprehensiveTests.cs
```

See `.github/skills/saam-dotnet-reference-implementation/SKILL.md` (Part 2) for the standard, and
`.github/skills/saam-test-suite-template/SKILL.md` for the template.

**Why the change:** the bash suites drove a live service over HTTP with `curl`/`jq` against mocked
or manually-provisioned dependencies. The Aspire host provisions real PostgreSQL and RabbitMQ,
so the suite can assert database round-trips and event side effects — the defect class an
API-shape-only test cannot reach. The cost is that the gate is no longer stack-agnostic; that is
acceptable for this single-target-stack engagement and must be revisited for a multi-stack one.

## Usage

```bash
# service may be ms-NN, the PascalCase service name, or the Aspire resource name
./validation/run-and-reconcile.sh ms-01 stage4_final
```

Equivalent direct invocation, without artifact production or graph reconciliation:

```bash
dotnet build sourcecode/Shopizer.slnx
dotnet test sourcecode/Shopizer.IntegrationTests \
  --filter "FullyQualifiedName~CustomerIdentityComprehensiveTests"
```

## Prerequisites

- .NET SDK 10 on `PATH`
- A running container runtime — `AspireHostFixture` provisions PostgreSQL and RabbitMQ
- `python3` with `pyyaml` (for `reconcile_validation.py`)

**A skipped or non-executed suite is a FAILED gate, never a pass.** `run-and-reconcile.sh` exits
non-zero when zero tests execute or any test is skipped, precisely so a missing container runtime
cannot be mistaken for a green build.
