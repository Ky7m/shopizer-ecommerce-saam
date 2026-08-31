---
name: saam-human-guidance-protocol
description: "Decision checkpoints, escalation criteria, and protocols for human-in-the-loop guidance during modernization."
copyright: "Copyright 2024-2026 SoftServe Inc. All rights reserved."
authors: "Max Kozinenko, Roman Kalita (SoftServe)"
---

# SAAM Human Guidance Protocol

## Prompt Categories

### 🔴 BLOCKING (Must stop and wait for response)
- DOMAIN_CONFIRM: Business domain assignment
- BOUNDARY_APPROVE: Service boundary decisions
- PRIORITY_RANK: Feature criticality ranking
- AMBIGUOUS_LOGIC: Unclear code behavior
- EXTERNAL_SYSTEM: Integration point clarification
- DATA_OWNERSHIP: Shared data resolution
- SPEC_REVIEW: Specification sign-off
- MODE_SELECT: Analysis mode confirmation
- SPEC_VALIDATION: Phase 4 independent validation — after spec generation, 5 random rules must pass implementability test before completion is accepted
- PLACEMENT_REVIEW: Phase 4b tier placement (Layer C) — decide app-tier vs DB-tier per flagged candidate (architecture/performance). Skipped only when zero candidates were flagged.

### 🟡 INFORMATIONAL (Proceed with stated assumption)
- NAMING_CLARIFY: Naming convention meanings
- VOLUME_ESTIMATE: Sizing assumptions
- FREQUENCY_CONFIRM: Batch/job scheduling
- STACK_CONFIRM: Technology choice validation

### 🟢 NOTIFICATION (No response needed)
- PHASE_COMPLETE: Phase milestone reached
- PROGRESS_UPDATE: Segment completion
- SPEC_READY: Specification available for review

## Prompt Format

```
🔴 HUMAN INPUT REQUIRED: [CATEGORY]

Context: [Where in the analysis we are]
Question: [Clear, specific question]
Options:
  (a) [Option with implications]
  (b) [Option with implications]
  (c) Other — please specify
Impact if skipped: [Consequence of guessing]
Current assumption: [What agent will assume if no response]
```

## Decision Log

All decisions recorded in `assessment/assessment_agenda.md`:
| # | Decision | Date | Options | Choice | Rationale |

## Agent Rules
1. NEVER assume business intent without code evidence
2. ALWAYS flag confidence < High
3. NEVER split transactions across services without approval
4. ALWAYS present alternatives for ambiguous boundaries
5. NEVER proceed past a phase gate without human confirmation
6. TRACK all assumptions for human review
7. When using CAST: query one segment at a time, never dump entire application
8. NEVER self-assess at 100% completion — all completion scores are PROVISIONAL until independently validated
9. After Phase 4: MUST run independent validation (5 random rules, implementability test) before declaring completion

## Phase 4 Validation Checkpoint

**🔴 BLOCKING**: After Phase 4 specification generation, BEFORE accepting completion:

1. Select 5 rules at RANDOM from the generated specs (across different services)
2. For each rule, attempt to answer: "Can I write a unit test from this Statement + DDL alone?"
3. Check for template patterns: same Statement structure across >3 rules = template = FAIL
4. Check DDL: columns must make domain sense for the entity (no `amount_total` on identity tables)
5. Check examples: must use real domain fields, not generic envelopes

**Pass criteria:** ≥4 of 5 rules pass the implementability test AND no template patterns detected

**If FAILED:** Reject the specs. Instruct re-extraction using Purpose-First method. Do NOT accept self-assessed scores.
