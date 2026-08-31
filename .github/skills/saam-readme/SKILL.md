---
name: saam-readme
description: "Index and navigation guide for all SAAM framework steering documents, phase guides, and architecture specifications."
copyright: "Copyright 2024-2026 SoftServe Inc. All rights reserved."
authors: "Max Kozinenko, Roman Kalita (SoftServe)"
---

# SAAM — SoftServe Agentic Application Modernization

## What is SAAM?

SAAM is a graph-driven modernization platform that transforms legacy applications into verified microservices through source-grounded specification, multi-dimensional confidence tracking, and execution-engine-agnostic code generation. It runs two parallel architect tracks (Bottom-Up source analysis + Top-Down domain design) that converge into validated specifications, governed by a Neo4j knowledge graph that tracks lifecycle states and drives agent context.

## Core Principles

1. **Dual-Track Analysis**: Two architects work in parallel — one reads code, one designs domains
2. **Human-in-the-Loop**: Mandatory checkpoints where human guidance is required
3. **Technology Agnostic**: Works with any legacy stack (RPG, COBOL, Java, .NET, PL/SQL, etc.)
4. **Knowledge Graph**: Neo4j-backed lifecycle tracking, multi-dimensional confidence, and context construction — always active for all projects
5. **CAST Imaging Integration**: Optional — use CAST data for structural analysis + graph validation layer
6. **AI-DLC Output**: Specifications enable agentic code generation via Kiro specs
7. **Mandatory Validation**: Every microservice MUST have a bash-scripted comprehensive test suite
8. **Continuous Evolution**: Phase 6 feedback loop — deviations, bugs, and features flow through the same spec-driven pipeline
9. **No Budget Calculations**: SAAM produces timelines and team composition only — no financial projections
10. **Common to Shared, Specific to Service**: For any cross-service concern (API conventions, events, auth, infra patterns, entity lifecycle, dependency versions), the common form is defined ONCE in `spec/shared/` and referenced by every service; only genuine module-specific variation lives in the service spec. New cross-cutting concerns follow the same sweep → promote → reference → gate discipline (Phase 4 Stage 1.5 shared-convention reconciliation)
11. **Reverse-Direction Completeness**: Coverage checks must also run BACKWARD — from what the legacy WRITES/DOES to what was extracted — not only forward from what was extracted. A never-extracted producer (e.g. an upstream `Init`/insert proc feeding a prominent downstream batch) is invisible to any forward check because it has no node to start from. Phase 1 exit runs table write-coverage (CAST: every written business table has an extracted writer); Phase 3 runs top-down flow coverage (every design-named flow has a backing BR). Both are human-confirmed extraction-gap registers
10. **Implicit-System Layers**: The database is a first-class citizen, not dumb storage. Three layers extracted before generation prevent "green but not working": (A) entity state models + data invariants (integrity enforced, integrity invariants mandatory-DB), (B) the extensibility engine (one common code base configured per instance — data powers, code defines), (C) tier placement (app-first by default; DB placement is an advised, evidence-based exception decided at 4b — never a blind preservation of legacy bottlenecks)

## Analysis Modes

| Mode | When to Use | Context Load |
|------|-------------|-------------|
| **Direct Source** | Small codebases (<50K LOC), source available in workspace | High |
| **CAST Imaging** | Large codebases, complex dependencies, >50K LOC | Low (MCP-driven) |
| **Hybrid** | CAST for structure, direct source for business logic extraction | Medium |

## Document Index

### Framework
- `saam-framework.md` — Master framework, phases, architecture
- `saam-human-guidance-protocol.md` — When/how to prompt humans, decision register

### Phase Guides
- `saam-phase0-onboarding.md` — System intake, inventory, analysis mode selection
- `saam-phase1-bottom-up.md` — Source Architect: code analysis or CAST data extraction
- `saam-phase2-top-down.md` — Domain Architect: boundaries, target architecture
- `saam-phase3-convergence.md` — Feature validation, gap analysis, boundary checks
- `saam-phase4-spec-generation.md` — Microservice specs with business rules
- `saam-api-contract.md` — OpenAPI contract generation (naming authority for tests + code)
- `saam-phase4a-business-rule-validation.md` — BA review: classify, weight, optimize, drop obsolete rules (mandatory — agent defaults or full workshop)
- `saam-ba-review-template.md` — Template for the BA-oriented review document
- `saam-phase4b-implementation-roadmap.md` — Automatibility scores, improvement plan, roadmap iteration
- `saam-phase4c-test-suite-generation.md` — Phase 4c orchestration: test suite generation per service
- `saam-task-tracking.md` — File-based task tracking per phase + Jira dual-write protocol
- `saam-phase5-setup.md` — Phase 5 setup wizard: model selection, parameter gathering, artifact creation
- `saam-phase5-ai-dlc-implementation.md` — AI-DLC code generation, test suites, CI/CD
- `saam-backend-fidelity.md` — Cross-service wiring & round-trip fidelity: 8 checkpoints (event emission, tenant propagation, callee-DTO alignment, schema migration, DB round-trip) + grep-able wiring-defect self-audit. Read before the Events / Integration Wiring layers in Phase 5.
- `saam-phase6-continuous-evolution.md` — Continuous loop: deviations, bugs, features, SPEC-DRIFT feed back through spec → test → implement → validate

### Source Reading Guides (activate based on legacy stack)
- `saam-source-reading-ibm-rpg.md` — IBM i: RPG IV, CL, DDS, Data Queues
- `saam-source-reading-cobol.md` — COBOL, JCL, CICS, VSAM (create when needed)
- `saam-source-reading-java-legacy.md` — Java EE, EJB, Struts (create when needed)
- `saam-source-reading-dotnet.md` — .NET Framework: WCF, WinForms, ASP.NET WebForms/MVC

### Templates
- `saam-spec-template.md` — Microservice specification template (backend)
- `saam-frontend-spec-template.md` — Frontend application specification template
- `saam-test-suite-template.md` — Comprehensive test suite template (MANDATORY)

### Integration & Knowledge Graph
- `saam-graph-context.md` — Knowledge Graph agent usage: lifecycle states, confidence model, context construction (always active)
- `saam-graph-validation.md` — CAST Validation Layer: reconciliation queries comparing against legacy CAST graph (requires CAST)
- `saam-cast-imaging-integration.md` — CAST Imaging MCP usage guide
- `saam-jira-integration.md` — Optional Jira integration for task tracking

### Telemetry & Calibration
- `saam-telemetry.md` — Telemetry collection protocol: per-phase YAML schemas, export/import rules, timing inference
- `saam-calibration.yaml` — Single source of tunable parameters: confidence weights, automatibility thresholds, complexity ratios, planning estimates (v1: expert heuristics)

### Governance & Verification
- `saam-governance.md` — Invisible risk-adaptive governance: spec drift detection, automatic enforcement levels
- `saam-cross-model-verification.md` — Optional Phase 4 quality gate: independent extraction verification to detect correlated errors
- `saam-signal-precedence.md` — Formal decision model: resolves contradictory signals deterministically (gates vs flags, precedence hierarchy)
- `saam-ci-governance.md` — CI/CD integration guide: GitHub Actions reference + GitLab/Azure/Bitbucket adaptations

## How to Use

1. New engagement: activate `saam-framework.md`
2. Phase 0: activate `saam-phase0-onboarding.md` (selects analysis mode)
3. Bottom-Up: activate `saam-phase1-bottom-up.md` + appropriate source reading guide
4. Top-Down: activate `saam-phase2-top-down.md`
5. Convergence: activate `saam-phase3-convergence.md`
6. Specs: activate `saam-phase4-spec-generation.md` + `saam-spec-template.md`
7. BA Review: activate `saam-phase4a-business-rule-validation.md` + `saam-ba-review-template.md`
8. Roadmap: activate `saam-phase4b-implementation-roadmap.md`
9. Test Suites: activate `saam-phase4c-test-suite-generation.md` + `saam-test-suite-template.md`
10. Implementation: activate `saam-phase5-setup.md` then `saam-phase5-ai-dlc-implementation.md` (+ `saam-backend-fidelity.md` before the wiring layers)
11. Continuous Evolution: activate `saam-phase6-continuous-evolution.md` (ongoing after Phase 5)

Always keep `saam-human-guidance-protocol.md` active during analysis work.

## Authors

- **Max Kozinenko** — SoftServe
- **Roman Kalita** — SoftServe
