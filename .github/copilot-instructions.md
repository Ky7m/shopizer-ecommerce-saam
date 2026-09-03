# SAAM — SoftServe Agentic Application Modernization for GitHub Copilot

This repository uses **SAAM (SoftServe Agentic Application Modernization)**, a graph-driven, spec-first modernization framework based on the AWS AI-DLC v2 ("One Core, Many Harnesses") architecture.

---

## 1. Operating Rules & Core Principles

1. **Dual-Track Analysis**:
   - Modernization operates along two converging tracks:
     - **Bottom-Up** (Source Architect): Analyzes legacy codebases and extracts dependencies, data structures, and transactions.
     - **Top-Down** (Domain Architect): Designs bounded contexts, domain models, and microservice boundaries.
   - Both tracks converge in Phase 3 (Convergence Coordinator) with zero orphan legacy features allowed.

2. **Specification Authority**:
   - Every microservice must have a formal specification and OpenAPI contract in `spec/microservices/<service>/`.
   - The OpenAPI contract (`04-api-contract.yaml`) is the absolute naming authority for endpoints, DTOs, entity names, and error structures.
   - Every business rule must have a unique identifier matching `br_id_pattern.regex` in `.github/saam-calibration.yaml`. Both the flat form (`BR-CUS-001`) and the grouped form (`BR-{DOM}-{GRP}-{NN}`, e.g. `BR-CUS-NN-005`) are valid.

3. **Test-First Implementation (Phase 4c)**:
   - Before writing any implementation code in Phase 5, a comprehensive xUnit integration test class must be generated at `sourcecode/Shopizer.IntegrationTests/<Service>ComprehensiveTests.cs`, running against a live .NET Aspire host. Standalone bash suites (`validation/<service>/comprehensive-test-suite.sh`) are DEPRECATED.
   - Every `@BR-ID` in the specification must be verified by at least one explicit `[Fact]` whose assertion could only pass if that specific rule were implemented.

4. **Traceability & Code Annotations**:
   - All generated source code in `sourcecode/` must annotate implemented business rules with an intent sentence immediately above the implementing method, one line per rule:
     ```csharp
     // @BR-CUS-001: Login and email uniqueness are checked inside the tenant/store boundary.
     // @BR-CUS-005: Passwords are encoded before persistence.
     ```
   - Integration tests use the paired form — the comment and the trait must be identical:
     ```csharp
     // @BR-ID: BR-CUS-019
     [Fact]
     [Trait("BR", "BR-CUS-019")]
     ```
   - Annotate only reachable code. A BR-ID on a method no endpoint reaches is a false coverage claim.
   - Lifecycle hook adapter (`.github/hooks/saam-copilot-adapter.ts`) verifies annotations on file save.

5. **Implementation Standard (AUTHORITATIVE)**:
   - `.github/skills/saam-dotnet-reference-implementation/SKILL.md` is the single authority for *how* a service and its test suite are built — project layout, `Program.cs` composition, persistence, error model, auth/tenancy, events, code style, and anti-patterns.
   - It is derived from the MS-01 reference: `sourcecode/Shopizer.CustomerIdentity/` and `sourcecode/Shopizer.IntegrationTests/CustomerIdentityComprehensiveTests.cs`. Read both before implementing any service.
   - Other steering files link to it; they must never restate it.

6. **Knowledge Graph Context & Calibration**:
   - The modernization lifecycle is backed by a Neo4j knowledge graph tracking confidence dimensions (provenance, implementation, test quality).
   - MCP scripts in `graph-mcp/scripts/` provide dynamic graph context during session execution.
   - Tunable confidence weights, automatibility thresholds, and complexity ratios are defined in `.github/saam-calibration.yaml`.

---

## 2. Agent Roster & Phase Mapping

When working on specific modernization phases, reference the corresponding custom agent or steering skills:

- **Phase 0 (Onboarding & Intake)**: `.github/skills/saam-phase0-onboarding/SKILL.md`
- **Phase 1 (Bottom-Up Analysis)**: `@source-architect` (`.github/skills/saam-phase1-bottom-up/SKILL.md`)
- **Phase 2 (Top-Down Domain Design)**: `@domain-architect` (`.github/skills/saam-phase2-top-down/SKILL.md`)
- **Phase 3 (Convergence & Reconciliation)**: `@convergence-coordinator` (`.github/skills/saam-phase3-convergence/SKILL.md`)
- **Phase 4 (Spec Generation & BA Review)**: `@spec-architect` (`.github/skills/saam-phase4-spec-generation/SKILL.md`)
- **Phase 4c (Test Suite Generation)**: `@test-engineer` (`.github/skills/saam-phase4c-test-suite-generation/SKILL.md`)
- **Phase 5 (AI-DLC Implementation)**: `@implementation-engineer` (`.github/skills/saam-phase5-ai-dlc-implementation/SKILL.md`)
- **Phase 6 (Continuous Evolution)**: `@implementation-engineer` (`.github/skills/saam-phase6-continuous-evolution/SKILL.md`)
- **Implementation & Test Standard (Phases 4c–6)**: `.github/skills/saam-dotnet-reference-implementation/SKILL.md`
- **Calibration Parameters**: `.github/saam-calibration.yaml`
