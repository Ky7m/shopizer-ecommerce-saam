---
name: saam-task-tracking
description: "Dual-write task tracking protocols and file-based state management across modernization phases."
copyright: "Copyright 2024-2026 SoftServe Inc. All rights reserved."
authors: "Max Kozinenko, Roman Kalita (SoftServe)"
---

# SAAM Task Tracking

## Overview

SAAM uses file-based task tracking as the primary mechanism for progress visibility across all phases. When Jira integration is configured, tracking is DUPLICATED to Jira — the file-based tracker remains the source of truth, and Jira mirrors it.

## Tracking Directory Structure

All tracking files live in a dedicated `tracking/` directory at the project root:

```
tracking/
├── phase0-onboarding.md
├── phase1-bottom-up.md
├── phase2-top-down.md
├── phase3-convergence.md
├── phase4-spec-generation.md
├── phase4a-ba-review.md
├── phase4b-automatibility.md
├── phase4c-test-suites.md
├── phase5-implementation/
│   ├── INDEX.md                   (overview of all services)
│   ├── <service-name>.md          (one file per service)
│   └── ...
├── phase6-evolution.md            (continuous loop — ongoing after Phase 5)
└── jira-mapping.md                (if Jira configured — maps file items to Jira IDs)
```

## Tracking Modes: Analysis (Phases 0-4) vs. Implementation (Phase 5)

SAAM uses TWO different tracking principles depending on the phase:

### Phases 0-4: Current-State Tracking

Analysis phases (0 through 4c) use **mutable current-state tracking**. The tracking file reflects the CURRENT state of the work — tasks can move forward and backward, statuses update in place.

**Characteristics:**
- File reflects "where are we NOW" — a snapshot of progress
- Tasks move: PENDING → IN_PROGRESS → DONE (or back to IN_PROGRESS if rework needed)
- Agent CAN mark tasks DONE when deliverables are produced (no PR review needed for analysis artifacts)
- If a task needs rework (e.g., human feedback requires re-extraction), status reverts to IN_PROGRESS
- Summary metrics update to reflect current reality
- This is a **status board**, not a log

**Why:** Analysis phases are iterative and agent-driven. The agent produces artifacts (specs, extractions, diagrams), the human reviews and gives feedback, the agent revises. There's no PR/merge cycle — artifacts are committed directly.

### Phase 5: Append-Only Actions Log

Implementation phase uses **append-only log tracking**. Tasks are never undone — new fix tasks are appended instead.

**Characteristics:**
- File reflects "what happened" — a chronological record of actions
- Tasks only move forward: PENDING → IN_PROGRESS → IN_REVIEW (agent's max)
- Agent CANNOT mark tasks DONE — only human action (PR merge) does that
- If a Jira ticket is reopened, a NEW fix task is appended (original stays IN_REVIEW)
- The file grows over time — this is intentional
- This is an **actions log**, not a status board

**Why:** Implementation produces code that goes through PR review. The agent's work and the human's review are distinct actions that must both be visible. Reopens create new work items, not reverts.

### Quick Reference

| Aspect | Phases 0-4 | Phase 5 |
|--------|-----------|---------|
| Tracking model | Mutable status board | Append-only log |
| Agent can mark DONE | Yes (artifact produced) | No (max: IN_REVIEW) |
| Rework handling | Revert status to IN_PROGRESS | Append new fix task |
| Status can go backward | Yes | Never |
| File represents | Current state | Full history |
| Jira DONE transition | Agent (when deliverable exists) | Human only (after PR merge) |

---

## Tracking File Format

### Phases 0-4: Current-State Format

```markdown
# Phase <N>: <Phase Name> — Task Tracker

## Jira Epic: <PROJ-123> — <Epic Name>
<!-- Jira line present ONLY if Jira integration is configured -->
<!-- If no Jira: omit this line entirely -->

## Status: NOT_STARTED | IN_PROGRESS | COMPLETE | BLOCKED

## Summary
| Metric | Value |
|--------|-------|
| Total tasks | <N> |
| Completed | <N> |
| In progress | <N> |
| Blocked | <N> |
| Started | <date> |
| Last updated | <date> |

## Tasks

### <Task Group / Category>

| # | Task | Status | Jira | Assignee | Notes |
|---|------|--------|------|----------|-------|
| 1 | <description> | DONE / IN_PROGRESS / PENDING / BLOCKED | <PROJ-456> | Agent / Human | |
| 2 | <description> | PENDING | — | | |

<!-- Jira column shows ticket ID if Jira configured, dash (—) if not -->
```

### Phase 5: Append-Only Log Format

```markdown
# Phase 5: Implementation — <Service Name>

## Jira Epic: <PROJ-XXX> — Implementation: <Service Name>
<!-- Present only if Jira configured -->

## Status: IN_PROGRESS | COMPLETE | BLOCKED

## Summary
| Metric | Value |
|--------|-------|
| Total tasks | <N> |
| In review | <N> |
| Done (human confirmed) | <N> |
| Pending | <N> |
| Started | <date> |
| Last updated | <date> |

## Tasks

| # | Task | Status | SDD Task | Jira | BR-IDs | Notes |
|---|------|--------|----------|------|--------|-------|
| 1 | <description> | DONE / IN_REVIEW / IN_PROGRESS / PENDING | tasks.md#task-1 | <PROJ-456> | BR-XX-001..005 | |
| 2 | <description> | PENDING | tasks.md#task-2 | — | | |

<!-- SDD Task column links to .kiro/specs/<service>/tasks.md task reference -->
<!-- Tasks are APPEND-ONLY — never delete, revert, or renumber -->
```

## Jira Ticket Structure (When Configured)

When Jira integration is active, the agent creates tickets with this hierarchy:

```
Epic: "SAAM Phase <N>: <Phase Name> — <System Name>"
├── Task: "<Major deliverable or milestone>"
│   ├── Sub-task: "<Specific action item>" (if needed)
│   └── Sub-task: "<Specific action item>"
├── Task: "<Major deliverable or milestone>"
└── Task: "<Major deliverable or milestone>"
```

### Ticket Type Mapping

| SAAM Concept | Jira Type | When to Create |
|-------------|-----------|----------------|
| Phase | Epic | One per phase |
| Major deliverable | Task (Story) | One per deliverable in the phase |
| Specific action item | Sub-task | Only when a task has 3+ distinct steps |
| Blocker/question for human | Task (Bug/Impediment) | When agent is blocked |

### Ticket Fields

Every Jira ticket created by SAAM MUST include:

| Field | Content |
|-------|---------|
| Summary | Concise task description (< 80 chars) |
| Description | Full details, acceptance criteria, references to spec files |
| Labels | `saam`, `phase-<N>`, `<service-name>` (if applicable) |
| Priority | Derived from BA weight (Critical/High/Medium/Low) or default Medium |

### Status Transitions

The agent transitions Jira tickets as work progresses:

| File Status | Jira Transition |
|-------------|-----------------|
| PENDING | To Do (default state) |
| IN_PROGRESS | In Progress |
| IN_REVIEW | In Review (agent submits PR, awaits human review) |
| DONE | Done — **ONLY after human PR merge + tests pass** |
| BLOCKED | Blocked (or flagged) |

**Phase-Specific DONE Semantics (Important — read before the rule below):**

| Phase | Who can mark DONE? | What "DONE" means |
|-------|-------------------|-------------------|
| **Phases 0-4c** (analysis) | **Agent** — when the deliverable artifact is produced | Artifact exists and meets the phase's quality gate |
| **Phase 5** (implementation) | **Human only** — after PR merge + CI passes | Code reviewed, merged, deployed |

This distinction exists because analysis phases produce artifacts (specs, diagrams, extractions) that are committed directly — no PR review cycle. Implementation produces code that goes through human code review.

**CRITICAL (Phase 5 Only): The agent MUST NEVER move a Phase 5 task to DONE.** The DONE transition happens ONLY after:
1. The agent submits a Pull Request for the implemented code
2. A human reviews and merges the PR
3. CI/CD tests pass on the merged code

The maximum status the agent can set autonomously during Phase 5 is **IN_REVIEW**. Transitioning to DONE requires human action (PR merge). This will be extended with agentic validation in a future version.

During Phases 0-4c, the agent CAN mark tasks DONE when deliverables are produced — because there is no PR review cycle for analysis artifacts.

### Two-Level Task Lifecycle (Kiro ↔ Jira)

Kiro's `tasks.md` and Jira operate at DIFFERENT lifecycle levels:

| System | Scope | "Done" means |
|--------|-------|--------------|
| **Kiro tasks.md** | Agent's implementation work | Code written, all deliverables [x], PR created |
| **Jira** | Full SDLC process | PR reviewed, merged, CI passes, deployed |

**A Kiro task can be complete while the Jira ticket is still open.** This is normal — the gap between "agent finished coding" and "human approved and merged" is the review cycle.

## Append-Only Actions Log — Phase 5 Only (NEVER Rewrite History)

The tracking file is an **append-only log** — it mirrors the Kiro task flow. Tasks are NEVER undone, reverted, or removed. If something needs fixing, a NEW task is created.

**Rules:**
- A task marked IN_REVIEW stays IN_REVIEW forever (it reflects that the agent DID complete its work at that point)
- If the PR is rejected / Jira ticket reopened → a NEW fix task is appended (the original is not modified)
- A task marked DONE stays DONE forever (human confirmed it)
- The file grows over time — this is intentional. It's a log of what happened, not a current-state snapshot.
- Tasks are NEVER deleted, renumbered, or have their status reverted

**Example progression:**

```markdown
| # | Task | Status | SDD Task | Jira | Notes |
|---|------|--------|----------|------|-------|
| 4 | Service Layer — Payment Validation | IN_REVIEW | tasks.md#task-4 | PROJ-145 | PR #12 submitted 2025-06-20 |
| ... | | | | | |
| 9 | Fix — Payment Validation (Reopened) | IN_REVIEW | tasks.md#task-9 | PROJ-145 | PR #18, fixes rounding issue from review |
```

Task 4 remains IN_REVIEW (it was completed by the agent at that time). Task 9 is the fix that addresses the reopen. Both are visible in the log. When PROJ-145 finally moves to Done in Jira, the tracking file gets a note on the LATEST task (Task 9) acknowledging the Jira closure.

**What the agent MUST NOT do:**
- Change Task 4's status back to IN_PROGRESS or PENDING after reopen
- Delete or hide Task 4
- Overwrite Task 4's notes with the reopen reason
- Renumber existing tasks

### Jira Status Flow

```
To Do → In Progress → In Review → Done
                          ↑          ↓ (if issues found)
                          └── Reopened ──→ In Progress (new fix task)
```

### Reopened Ticket Protocol

When the agent detects (on session start) that a Jira ticket linked to a completed Kiro task has been **Reopened**:

1. **Read the reopen reason** — query Jira for the latest comment/reason on the reopened ticket
2. **Create a new fix task** in `tasks.md`:
   ```markdown
   ## Task N+X: Fix — <Original Task Name> (Reopened) [PROJ-XXX]
   - **Status:** PENDING
   - **SAAM Spec:** <same as original task>
   - **BR-IDs:** <affected BR-IDs from original task>
   - **Tracking:** tracking/phase5-implementation/<service>.md#task-N+X
   - **Jira:** <same ticket ID — PROJ-XXX>
   - **Reopen Reason:** <from Jira comment>
   - **Deliverables:**
     - [ ] Investigate root cause per reopen reason
     - [ ] Fix implementation
     - [ ] Verify fix passes comprehensive test suite
     - [ ] Submit updated PR
   ```
3. **Update tracking file** — add the new fix task row
4. **Transition Jira ticket** back to In Progress
5. **Implement the fix** following the same spec-driven protocol (re-read BR-IDs, fix code, verify)

### Session Start: Jira Sync Check (MANDATORY BEFORE ANY WORK)

On every Phase 5 session start (when Jira is configured), the agent MUST sync BEFORE doing any implementation work. This is the FIRST action — before reading specs, before writing code, before anything else.

**Sync protocol:**

1. Read `tracking/phase5-implementation/<service-name>.md`
2. Identify which task the agent was last working on (first non-DONE, non-IN_REVIEW task, or the task marked IN_PROGRESS)
3. Query Jira for the current status of ALL tasks that are IN_REVIEW or IN_PROGRESS in the tracking file
4. Process each result:

| Tracking Status | Jira Status | Action |
|----------------|-------------|--------|
| IN_REVIEW | Done | Update tracking to DONE. Move to next pending task. |
| IN_REVIEW | Reopened | Create fix task (per reopen protocol). Work the fix. |
| IN_REVIEW | In Review | No action — still waiting for human. Skip to next pending task. |
| IN_PROGRESS | In Progress | No conflict — resume this task. |
| IN_PROGRESS | Reopened | Jira was moved externally. Read reopen reason, adjust work accordingly. |
| PENDING | To Do | No conflict — pick up this task next. |
| PENDING | In Progress | Someone started it externally. Update tracking to IN_PROGRESS, check if agent should take over or skip. |

5. After sync, state: "Jira sync complete. [N] tasks confirmed Done, [M] reopened (fix tasks created), [P] still in review. Resuming from Task #X."
6. Only THEN proceed with implementation work.

**If Jira is unreachable:** Log the failure in the session log, proceed with work based on tracking file state alone. Re-attempt sync at next session.

This keeps the tracking file truthful even when humans move tickets outside the agent's sessions.

## Per-Phase Tracking Details

### Phase 0: Onboarding

Tasks:
- System identification complete
- Analysis mode selected
- Source loaded / CAST verified
- Inventory built
- Naming conventions documented
- Segmentation agreed
- Application context steering created

### Phase 1: Bottom-Up

Tasks (per segment):
- Segment <X> programs classified
- Segment <X> call graphs built
- Segment <X> business rules extracted
- Segment <X> data access patterns documented
- Segment <X> integration points cataloged
- Human clarification items resolved

### Phase 2: Top-Down

Tasks:
- Domain boundaries identified
- Service catalog defined
- Target architecture documented
- ERDs created per service
- Sequence diagrams for key processes
- Technology stack decided
- Modernization roadmap created
- Risk analysis documented

### Phase 3: Convergence

Tasks:
- Feature matrix complete (100% mapped)
- Source→Target gaps resolved
- Target→Source gaps documented
- Boundary violations resolved
- Every BR-ID assigned to one service
- Test feasibility confirmed

### Phase 4: Specification Generation

Tasks (per service):
- Service <X> spec started
- Service <X> business rules extracted (N/M complete)
- Service <X> domain model complete
- Service <X> API design complete
- Service <X> quality gates passed
- Service <X> human sign-off

### Phase 4a: BA Review

Tasks (per service):
- BA review document generated for <service>
- BA review document completed by BA
- Decisions parsed back into specs
- Decision register updated
- Scope reduction report generated

### Phase 4b: Automatibility & Roadmap

Tasks:
- Initial scores calculated
- Improvement plan generated
- Iteration 1: items executed, specs updated, scores recalculated
- Iteration N: ...
- Roadmap finalized
- Team composition documented

### Phase 4c: Test Suite Generation

Tasks (per service):
- Test suite generated for <service>
- Test assertions validated against spec
- Test suite reviewed by human

### Phase 5: Implementation

Tasks (per service — tracked in separate file `tracking/phase5-implementation/<service>.md`):
- Kiro spec generated (requirements.md, design.md, tasks.md)
- Scaffolding complete
- Domain model implemented
- Repository layer complete
- Service layer: BR-IDs implemented (N/M)
- Controller layer complete
- Events implemented (if applicable)
- Unit tests written
- Compilation passes
- Container build succeeds
- Service starts
- Comprehensive test suite: N/M pass
- All tests pass (100%)
- CI/CD pipeline created
- Documentation generated
- Human sign-off

**Phase 5 tracking files include cross-references to:**
- SAAM spec file path and BR-ID group (`spec/microservices/<service>/01-business-rules.md`)
- SDD spec task reference (`.kiro/specs/<service-name>/tasks.md#Task-N`)
- Jira ticket IDs (if configured)

This interlinking ensures any tracking entry can be traced back to its source spec and forward to its Jira representation.

### Phase 6: Continuous Evolution

Phase 6 uses a SINGLE tracking file: `tracking/phase6-evolution.md` (not per-service).

Tasks (ongoing, append-only):
- Items received (classified: DEV-TEST / BUG / FEATURE / SPEC-DRIFT / DEPENDENCY)
- Spec updated (if needed)
- Tests updated (if needed)
- Implementation complete
- Validation passed
- Item resolved

**Phase 6 tracking model:** Same as Phase 5 (append-only log). Items are never deleted or reverted. New items are appended as they arrive. Resolved items stay in the log with completion date.

**Jira integration:** Each Phase 6 item can optionally create a Jira ticket. DEV-TEST items from the deviation log are processed by AI-DLC first (no ticket needed for trivial fixes). Only items requiring human decision or that remain unresolved after AI-DLC attempts become Jira tickets.

## Dual-Write Protocol (File + Jira)

When Jira is configured, the agent MUST maintain both systems in sync:

### On Task Creation
1. Write the task to the tracking file
2. Create the Jira ticket via MCP (`jira_create_issue`)
3. Write the Jira ticket ID back into the tracking file's Jira column

### On Status Change
1. Update the tracking file status
2. Transition the Jira ticket (`jira_transition_issue`)
3. Add a comment to the Jira ticket with context (what was done)

### On Task Completion
1. Mark IN_REVIEW in tracking file (agent has finished its work, PR submitted)
2. Transition Jira ticket to In Review
3. Add comment with PR link and deliverable summary
4. **DONE is set ONLY by human action** (after PR merge + tests pass)

### Sync Conflict Rule
**The tracking file is always the source of truth.** If Jira and the file disagree, the file wins. The agent never reads Jira to determine what to do next — it reads the tracking file.

## Jira Mapping File

When Jira is configured, `tracking/jira-mapping.md` maintains the complete mapping:

```markdown
# Jira Mapping

## Project: <PROJ>

| Phase | Epic ID | Epic Name |
|-------|---------|-----------|
| Phase 0 | PROJ-10 | SAAM Phase 0: Onboarding — <System> |
| Phase 1 | PROJ-11 | SAAM Phase 1: Bottom-Up — <System> |
| Phase 2 | PROJ-12 | SAAM Phase 2: Top-Down — <System> |
| ... | | |

## Task Mapping

| Tracking File | Line # | Jira ID | Summary |
|---------------|--------|---------|---------|
| phase0-onboarding.md | 1 | PROJ-20 | System identification |
| phase0-onboarding.md | 2 | PROJ-21 | Analysis mode selection |
| ... | | | |
```

## Agent Behavior Rules

### Starting a New Phase (MANDATORY — ALL phases including 0-4c)
1. Check if `tracking/phase<N>-*.md` exists
2. If NOT: create the tracking file with all tasks for that phase as PENDING (derive from the phase's Deliverables section)
3. If Jira configured: create Epic + Tasks in Jira, record IDs in tracking file
4. If YES: read the file, resume from first non-DONE task

**This is not optional.** Every phase execution MUST have a tracking file. If the agent starts work on any phase without first creating/reading the tracking file, it is a protocol violation.

### During Phase Execution
1. Before starting a task: mark it IN_PROGRESS in the file (+ Jira transition)
2. After completing a task: mark it DONE in the file (+ Jira transition)
3. If blocked: mark BLOCKED with explanation (+ Jira flag/comment)
4. Update Summary metrics after each status change

### Completing a Phase
1. Verify all tasks are IN_REVIEW or DONE (human-confirmed)
2. Set phase Status to COMPLETE only after human confirms all PRs merged
3. Record completion date
4. If Jira: transition Epic to Done (human action)

### Without Jira
- All tracking is file-based only
- Jira column shows "—" for all rows
- No MCP calls made
- Workflow is identical otherwise

## Initialization

The `tracking/` directory is created during Phase 0 (or when the agent first encounters a tracking need). The agent creates it automatically — no manual setup required.

Add to project structure:
```
├── tracking/                    # Task tracking per phase
```

## Phase Transition Protocol (MANDATORY — Tracing Enforcement)

Every phase boundary (P0→P1, P1→P3, P4→P4a, P4c→P5, etc.) requires a tracing checkpoint. This ensures telemetry can be derived from verifiable timestamps rather than retroactive estimates.

### At Phase Completion (Before Moving to Next Phase)

The agent MUST execute ALL of these before presenting the exit gate or activating the next phase:

1. **Tracking file:** Mark phase status COMPLETE with timestamp
2. **Graph:** Run `graph_run_inferences` + update any phase-specific graph nodes (PhaseEvent if configured)
3. **Commit:** `git commit` all phase artifacts with conventional message: `feat(phase<N>): <phase-name> complete — <key metrics>`
4. **Jira (if configured):** Transition phase Epic subtasks to DONE
5. **Telemetry file:** Create/update `.saam/telemetry/phase<N>-<name>.yaml` with:
   - `completed_at`: current ISO timestamp (from `date -u +"%Y-%m-%dT%H:%M:%SZ"`)
   - Key metrics for the phase
   - `timing_source: "machine_recorded"` (NOT "estimated" or "human_reported")

### At Subagent Return (Within a Phase)

Every time a subagent returns from delegated work (extraction, generation, etc.):

1. **Tracking file:** Update the specific task/service to reflect what was produced
2. **Commit:** `git add <produced-artifacts> && git commit -m "<conventional message>"`
3. **Graph (if applicable):** Update nodes for the work unit completed
4. **Jira (if configured):** Transition subtask

**Why commit per-subagent-return:** Each commit creates a git timestamp that telemetry can query later. Without commits between steps, the only timing data is the session's start/end — which is useless for per-service or per-phase calibration.

### Commit Message Convention (Telemetry-Friendly)

```
feat(phase<N>): <service-or-deliverable> — <key metric>

Examples:
  feat(phase1): identity-segment extraction — 28 rules, 16 files
  feat(phase4): team-service spec complete — 25 rules, 5 tables, 24 endpoints
  feat(phase4c): identity-service test suite — 92 assertions
  feat(phase5): gateway implementation — 11 BR-IDs, Mode 1
  chore(phase5): gateway post-service reconciliation
  fix(phase6): systemic P0 fix — ValidationPipe 422 (12 services)
```

This convention enables `git log --grep="feat(phase5)"` to extract all Phase 5 service implementations with timestamps for telemetry computation.

### What Gets Committed When (Quick Reference)

| Trigger | What to Commit | Conventional Message Pattern |
|---------|---------------|------------------------------|
| P1 segment extraction done | `assessment/<segment>-*.md` | `feat(phase1): <segment> — N rules` |
| P4 service spec done | `spec/microservices/<service>/*` | `feat(phase4): <service> spec — N rules` |
| P4c test suite done | `validation/<service>/*.sh` | `feat(phase4c): <service> test suite — N assertions` |
| P4c DTOs generated | `spec/microservices/<service>/08-dtos/*` | `feat(phase4c): <service> DTOs generated` |
| P5 service implementation | `sourcecode/<service>/*` | `feat(phase5): <service> — N BR-IDs, Mode N` |
| P5 post-service protocol | `tracking/`, graph updates | `chore(phase5): <service> tracking + graph` |
| P6 systemic fix | All affected services | `fix(phase6): <pattern> — N services` |
| P6 per-service fix | `sourcecode/<service>/*` | `fix(phase6): <service> — <what was fixed>` |
| Phase exit gate | `tracking/`, `.saam/telemetry/` | `chore: phase<N> complete — telemetry recorded` |

### Enforcement Rule

**If the tracking file shows a task as DONE but no corresponding git commit exists, the task is NOT actually done.** The commit is the verifiable proof that work was completed at a specific point in time. This is not optional overhead — it's the telemetry timestamp source.

**Exception:** If the user explicitly says "do not commit" — defer commits but continue tracking file updates. Batch-commit when the user gives permission. Note in tracking: `commit_deferred: true`.
