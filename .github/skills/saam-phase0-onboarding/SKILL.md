---
name: saam-phase0-onboarding
description: "Initial system intake, codebase inventory, environment validation, and analysis mode selection."
copyright: "Copyright 2024-2026 SoftServe Inc. All rights reserved."
authors: "Max Kozinenko, Roman Kalita (SoftServe)"
---

# SAAM Phase 0: System Onboarding

## Objective
Intake the legacy system, build inventory, and select the analysis mode (Direct Source, CAST Imaging, or Hybrid).

## Required Steering Files (Read Before Proceeding)

The agent MUST read the following steering files before executing Phase 0:

1. **`.github/skills/saam-human-guidance-protocol/SKILL.md`** — Prompt categories, decision register format, agent rules
2. **`.github/skills/saam-task-tracking/SKILL.md`** — Tracking file format and Jira dual-write protocol

These files provide the interaction patterns and tracking mechanics needed throughout this phase.

## Task Tracking Activation

**PRECONDITION: The agent MUST NOT begin any onboarding steps until `tracking/phase0-onboarding.md` exists.** If it doesn't exist, create it NOW with all deliverables listed as PENDING.

**PhaseEvent (telemetry timestamp):** Immediately after creating the tracking file, write a PhaseEvent node to the graph: `graph_add_node(nodeType="PhaseEvent", id="P0-started", properties={phase: "P0", event: "started", timestamp: <current ISO timestamp>})`. This provides a machine-recorded start time for telemetry.

After each step completes (system profile documented, analysis mode selected, etc.), update the tracking file immediately. If Jira is configured, create an Epic with Tasks. See `.github/skills/saam-task-tracking/SKILL.md` for format.

## Agent Interaction Style (MANDATORY)

Phase 0 is a WIZARD — one question at a time, conversational. The agent MUST:
- Ask ONE question, wait for the answer, then ask the next
- Never dump a numbered list of questions for the user to answer all at once
- Use the user's previous answer to inform the next question (adaptive flow)
- Acknowledge each answer before moving on ("Got it — .NET Framework with SQL Server.")
- Skip questions the agent can infer from context (workspace name, file structure, prior answers)

The agent MUST NOT:
- Present all Step 0.1 questions as a batch
- Use bullet-point "menus" with options to select from
- Ask about things already evident from the workspace (e.g., if source files are already present)

## Step 0.0: Point the operator at the guide (first time only)

Before the first question, if this looks like the human's first Phase 0 on this engagement (no
`tracking/phase0-onboarding.md` progress yet), mention the operator guide once — the human is about to
start making decisions and may never have opened the framework repo:

> "Before we dive in: `OPERATOR-GUIDE.md` (project root) is your end-to-end map — what you decide at each
> phase, what to inspect, and the cross-checks that catch a 'green but wrong' result. Phases 4a and 4b in
> particular are yours to drive (they define the DESIRED state, not just a copy of the legacy). Skim it
> whenever you like — I'll keep going. Ready for a few quick questions?"

If `OPERATOR-GUIDE.md` isn't in the project root (older bootstrap), point to `docs/OPERATOR-GUIDE.md` in
the framework repo instead. Do NOT block on this — it's a one-line pointer, then proceed to 0.1.

## Step 0.1: System Identification (Wizard Flow)

Ask these questions ONE AT A TIME in conversational order. Skip any that can be inferred from workspace context (folder names, existing files, prior conversation):

1. "What system are we modernizing?" (application name)
2. "What's the business domain?" (ERP, billing, CRM, etc.) — skip if obvious from app name
3. "What's the tech stack?" (RPG, COBOL, .NET, Java, etc.) — skip if source files are already visible
4. "Roughly how large is the codebase?" (LOC, module count, table count) — accept rough estimates
5. "Is CAST Imaging available for this system?" — skip if already answered during enablement

After gathering enough context, the agent summarizes what it knows and proposes the analysis mode. Do NOT wait for all 5 answers if some are already clear.

## Step 0.2: Analysis Mode Selection

Based on system size and CAST availability:

| Criterion | Direct Source | CAST Imaging | Hybrid |
|-----------|--------------|-------------|--------|
| LOC < 50K | ✅ Preferred | Optional | — |
| LOC 50K-500K | Possible | ✅ Preferred | ✅ Best |
| LOC > 500K | Not feasible | ✅ Required | ✅ Required |
| CAST available | Either | ✅ Use it | ✅ Use it |
| CAST not available | ✅ Only option | N/A | N/A |
| Deep rule extraction needed | ✅ Required for rules | Structure only | CAST for structure, source for rules |

**🔴 PROMPT HUMAN**: "Based on the system profile, I recommend [Mode]. Do you agree, or prefer a different approach?"

**Safety-net disclosure (MANDATORY when the human chooses Direct Source, i.e. no CAST):** the agent
MUST state plainly what guardrail is being given up, so the choice is informed — not silent:

> "Note: without CAST, you lose the **zero-unaccounted-loss** structural guarantee — the fully
> LLM-independent check (`graph_unaccounted_loss`, call-pattern/data-access preservation) that proves
> every legacy component with business logic is accounted for. In Direct Source mode we fall back to
> heuristic extraction coverage (expected yield per LOC) + comprehensive test suites + BA review. Those
> are real but weaker at catching silent omissions. If this system is large or high-risk, consider
> CAST/Hybrid. Proceed with Direct Source?"

This is the ONE point where the human can weigh the safety-net trade-off with full information. Do not
bury it — omission-detection independent of the LLM is the single strongest anti-correlated-error layer.

## Step 0.3: Target Stack Assumption (Early Signal)

After analysis mode is selected, ask about the target technology direction. This is NOT a binding decision — Phase 4b will confirm/override with evidence. But knowing the assumed target stack helps P1 produce relevant observations and P2 design with stack idioms in mind.

**🔴 PROMPT HUMAN** (conversational): "Do you have a target technology stack in mind for the modernized system? For example: TypeScript/NestJS, Java/Spring, Python/FastAPI, .NET Core? If not decided yet, I'll proceed stack-agnostic and we'll confirm during Phase 4b."

Record the answer in `inventory/INDEX.md` under System Profile:
```
| Target Stack (assumed) | <answer or "TBD — decided in Phase 4b"> |
```

Also record in `engagement.yaml` telemetry as `target_stack_assumed`.

**If the human provides a target stack:** P1 extraction can note relevant patterns ("this Rails concern maps well to a NestJS guard"), and P2 can reference framework idioms in architecture decisions.

**If "not decided yet":** Proceed stack-agnostic. All phases work without this — it's an optimization, not a requirement.

## Step 0.4: Source Loading (Direct/Hybrid Mode)

If Direct Source or Hybrid:
1. Place source under `initial-source/<system-name>/`
2. Preserve original directory structure
3. Source files are READ-ONLY reference

## Step 0.5: CAST Imaging Setup (CAST/Hybrid Mode)

If CAST Imaging available:
1. Confirm CAST Imaging MCP server is configured
2. Verify application is analyzed in CAST
3. Test connectivity: query application list
4. Document CAST application ID for this system

## Step 0.6: Build Inventory

Regardless of mode, produce and save to **exactly** `inventory/INDEX.md` (this filename is mandatory — NEVER use `system-inventory.md` or any other name):

```markdown
# <System> Source Inventory

## System Profile
| Attribute | Value |
|-----------|-------|
| Name | <name> |
| Stack | <technologies> |
| Target Stack (assumed) | <target or "TBD — decided in Phase 4b"> |
| LOC | <approximate> |
| Programs/Modules | <count> |
| Tables/Files | <count> |
| Analysis Mode | Direct / CAST / Hybrid |

## Component Breakdown
| Component Type | Count | Description |
|----------------|-------|-------------|
| Programs | X | Main executable units |
| Screens/Forms | X | UI components |
| Database Objects | X | Tables, views, indexes |
| Batch Jobs | X | Scheduled/triggered processes |
| APIs/Services | X | External interfaces |
| Reports | X | Output documents |

## Naming Conventions
| Pattern | Meaning | Example |
|---------|---------|---------|
| ... | ... | ... |
```

**🟡 PROMPT HUMAN**: "Can you provide any documentation on naming conventions used in this system?"

## Step 0.7: Identify Segmentation Strategy

For large systems, define how to segment the analysis. The agent proposes segmentation based on what it's learned so far:

**🔴 PROMPT HUMAN** (conversational, one message): "Based on what I've seen, I'd suggest breaking this into [N] segments: [list]. Does that align with how your team thinks about the system, or would you group things differently?"

Wait for the user's input. Adjust segmentation based on their domain knowledge.

Segmentation criteria:
- Functional domain (orders, inventory, customers, etc.)
- Deployment unit (if already separated)
- Data ownership (tables that cluster together)
- CAST imaging modules (if CAST provides natural groupings)

## Step 0.8: Create Application Context Steering

After all previous steps are complete, use GitHub Copilot’s built-in steering generation feature to automatically generate project steering for the analyzed application.

The generated steering should establish core workspace context based on:
- source inventory
- detected technology stack
- project/module structure
- application purpose
- naming conventions
- integration points
- database/storage elements
- identified segmentation strategy

After generation, review the created steering files and update only clearly incorrect or missing facts.
Do not manually invent application context.
Mark uncertain information as assumptions or needs confirmation.

Create `.github/skills/saam-application-context/SKILL.md`:

```markdown
---
title: Application Context
inclusion: fileMatch
fileMatchPattern: 'initial-source/**'
authors: SAAM Phase 0 (auto-generated)
---

# Application Context

```

## Step 0.9: Generate Project README (MANDATORY)

After all previous steps are complete, the agent MUST generate or overwrite the root `README.md` with project-specific content. This README is the living document for the modernization engagement — it reflects what was learned and decided, NOT the SAAM framework itself.

The README is updated after every subsequent phase completion to reflect current project state.

Generate `README.md` at the workspace root using the template from `.github/skills/saam-framework/SKILL.md` (section "Project README Template"). Populate it with:

- System name and business domain (from Step 0.1)
- Technology stack and analysis mode (from Step 0.2)
- Codebase size and component counts (from Step 0.5)
- Segmentation strategy (from Step 0.6)
- Current phase status (Phase 0 complete, Phase 1/2 starting)
- Project directory structure (as it currently exists)
- Team members / stakeholders (if known)

The README should be written for someone joining the engagement mid-flight — they should understand what system is being modernized, what's been decided so far, and where to find artifacts.

## Deliverables
- [ ] System profile documented
- [ ] Analysis mode selected and confirmed
- [ ] Source loaded or CAST connection verified
- [ ] Inventory with component counts
- [ ] Naming conventions documented
- [ ] Segmentation strategy agreed with human
- [ ] `inventory/INDEX.md` created
- [ ] `.github/skills/saam-application-context/SKILL.md` created (auto-loading context)
- [ ] `README.md` generated with project-specific content (not SAAM framework boilerplate)

## Exit Gate

**PRECONDITION: The agent MUST produce `.saam/telemetry/engagement.yaml` and `.saam/telemetry/phase0-onboarding.yaml` BEFORE presenting the exit gate.** Create the `.saam/telemetry/` directory if it doesn't exist.

**PhaseEvent (completed):** Write: `graph_add_node(nodeType="PhaseEvent", id="P0-completed", properties={phase: "P0", event: "completed", timestamp: <current ISO timestamp>})`.

**Telemetry data to capture:**
- `engagement.yaml`: engagement_id, industry, legacy_stack, target_stack, total services in scope, analysis mode, start date, team size
- `phase0-onboarding.yaml`: timing (started_at, completed_at, duration_hours), actor, metrics (total LOC, segments identified, analysis mode, integrations found)

**Schema:** See `.github/skills/saam-telemetry/SKILL.md` for full YAML structures.

Human confirms: "Phase 0 complete, proceed with Phase 1 and Phase 2 in parallel."

**Next steps after human approval:**
- Activate `.github/skills/saam-phase1-bottom-up/SKILL.md` for the Source Architect track
- Activate `.github/skills/saam-phase2-top-down/SKILL.md` for the Domain Architect track
- Activate or dynamically generate the appropriate source reading guide for the project's legacy stack (e.g., `.github/skills/saam-source-reading-ibm-rpg/SKILL.md`, `.github/skills/saam-source-reading-dotnet/SKILL.md`, or dynamically create per `.github/skills/saam-phase1-bottom-up/SKILL.md` protocol)
- Keep `.github/skills/saam-human-guidance-protocol/SKILL.md` active throughout both tracks
