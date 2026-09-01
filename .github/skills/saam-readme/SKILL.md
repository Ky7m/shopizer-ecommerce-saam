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
6. **AI-DLC Output**: Specifications enable agentic code generation via GitHub Copilot specs
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
- `.github/skills/saam-framework/SKILL.md` — Master framework, phases, architecture
- `.github/skills/saam-human-guidance-protocol/SKILL.md` — When/how to prompt humans, decision register

### Phase Guides
- `.github/skills/saam-phase0-onboarding/SKILL.md` — System intake, inventory, analysis mode selection
- `.github/skills/saam-phase1-bottom-up/SKILL.md` — Source Architect: code analysis or CAST data extraction
- `.github/skills/saam-phase2-top-down/SKILL.md` — Domain Architect: boundaries, target architecture
- `.github/skills/saam-phase3-convergence/SKILL.md` — Feature validation, gap analysis, boundary checks
- `.github/skills/saam-phase4-spec-generation/SKILL.md` — Microservice specs with business rules
- `.github/skills/saam-api-contract/SKILL.md` — OpenAPI contract generation (naming authority for tests + code)
- `.github/skills/saam-phase4a-business-rule-validation/SKILL.md` — BA review: classify, weight, optimize, drop obsolete rules (mandatory — agent defaults or full workshop)
- `.github/skills/saam-ba-review-template/SKILL.md` — Template for the BA-oriented review document
- `.github/skills/saam-phase4b-implementation-roadmap/SKILL.md` — Automatibility scores, improvement plan, roadmap iteration
- `.github/skills/saam-phase4c-test-suite-generation/SKILL.md` — Phase 4c orchestration: test suite generation per service
- `.github/skills/saam-task-tracking/SKILL.md` — File-based task tracking per phase + Jira dual-write protocol
- `.github/skills/saam-phase5-setup/SKILL.md` — Phase 5 setup wizard: model selection, parameter gathering, artifact creation
- `.github/skills/saam-phase5-ai-dlc-implementation/SKILL.md` — AI-DLC code generation, test suites, CI/CD
- `.github/skills/saam-backend-fidelity/SKILL.md` — Cross-service wiring & round-trip fidelity: 8 checkpoints (event emission, tenant propagation, callee-DTO alignment, schema migration, DB round-trip) + grep-able wiring-defect self-audit. Read before the Events / Integration Wiring layers in Phase 5.
- `.github/skills/saam-phase6-continuous-evolution/SKILL.md` — Continuous loop: deviations, bugs, features, SPEC-DRIFT feed back through spec → test → implement → validate

### Source Reading Guides (activate based on legacy stack)
- `.github/skills/saam-source-reading-ibm-rpg/SKILL.md` — IBM i: RPG IV, CL, DDS, Data Queues
- `.github/skills/saam-source-reading-cobol/SKILL.md` — COBOL, JCL, CICS, VSAM (create dynamically if missing — see Phase 1 Protocol)
- `.github/skills/saam-source-reading-java-legacy/SKILL.md` — Java EE, EJB, Struts (create dynamically if missing — see Phase 1 Protocol)
- `.github/skills/saam-source-reading-dotnet/SKILL.md` — .NET Framework: WCF, WinForms, ASP.NET WebForms/MVC

### Templates
- `.github/skills/saam-spec-template/SKILL.md` — Microservice specification template (backend)
- `.github/skills/saam-frontend-spec-template/SKILL.md` — Frontend application specification template
- `.github/skills/saam-test-suite-template/SKILL.md` — Comprehensive test suite template (MANDATORY)

### Integration & Knowledge Graph
- `.github/skills/saam-graph-context/SKILL.md` — Knowledge Graph agent usage: lifecycle states, confidence model, context construction (always active)
- `.github/skills/saam-graph-validation/SKILL.md` — CAST Validation Layer: reconciliation queries comparing against legacy CAST graph (requires CAST)
- `.github/skills/saam-cast-imaging-integration/SKILL.md` — CAST Imaging MCP usage guide
- `.github/skills/saam-jira-integration/SKILL.md` — Optional Jira integration for task tracking

### Telemetry & Calibration
- `.github/skills/saam-telemetry/SKILL.md` — Telemetry collection protocol: per-phase YAML schemas, export/import rules, timing inference
- `.github/saam-calibration.yaml` — Single source of tunable parameters: confidence weights, automatibility thresholds, complexity ratios, planning estimates (v1: expert heuristics)

### Governance & Verification
- `.github/skills/saam-governance/SKILL.md` — Invisible risk-adaptive governance: spec drift detection, automatic enforcement levels
- `.github/skills/saam-cross-model-verification/SKILL.md` — Optional Phase 4 quality gate: independent extraction verification to detect correlated errors
- `.github/skills/saam-signal-precedence/SKILL.md` — Formal decision model: resolves contradictory signals deterministically (gates vs flags, precedence hierarchy)
- `.github/skills/saam-ci-governance/SKILL.md` — CI/CD integration guide: GitHub Actions reference + GitLab/Azure/Bitbucket adaptations

## How to Use

1. New engagement: activate `.github/skills/saam-framework/SKILL.md`
2. Phase 0: activate `.github/skills/saam-phase0-onboarding/SKILL.md` (selects analysis mode)
3. Bottom-Up: activate `.github/skills/saam-phase1-bottom-up/SKILL.md` + appropriate source reading guide
4. Top-Down: activate `.github/skills/saam-phase2-top-down/SKILL.md`
5. Convergence: activate `.github/skills/saam-phase3-convergence/SKILL.md`
6. Specs: activate `.github/skills/saam-phase4-spec-generation/SKILL.md` + `.github/skills/saam-spec-template/SKILL.md`
7. BA Review: activate `.github/skills/saam-phase4a-business-rule-validation/SKILL.md` + `.github/skills/saam-ba-review-template/SKILL.md`
8. Roadmap: activate `.github/skills/saam-phase4b-implementation-roadmap/SKILL.md`
9. Test Suites: activate `.github/skills/saam-phase4c-test-suite-generation/SKILL.md` + `.github/skills/saam-test-suite-template/SKILL.md`
10. Implementation: activate `.github/skills/saam-phase5-setup/SKILL.md` then `.github/skills/saam-phase5-ai-dlc-implementation/SKILL.md` (+ `.github/skills/saam-backend-fidelity/SKILL.md` before the wiring layers)
11. Continuous Evolution: activate `.github/skills/saam-phase6-continuous-evolution/SKILL.md` (ongoing after Phase 5)

Always keep `.github/skills/saam-human-guidance-protocol/SKILL.md` active during analysis work.

## Authors

- **Max Kozinenko** — SoftServe
- **Roman Kalita** — SoftServe
