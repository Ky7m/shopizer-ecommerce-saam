---
name: saam-ba-review-template
description: "Template and structure for Business Analyst reviews, business rule validation, and optimization sign-offs."
copyright: "Copyright 2024-2026 SoftServe Inc. All rights reserved."
authors: "Max Kozinenko, Roman Kalita (SoftServe)"
---

# SAAM BA Review Document Template

## Purpose

This template defines the document the agent GENERATES for the Business Analyst to review. The BA works through this document, makes decisions, and returns it. The agent then parses the completed document back into updated service specs.

**Audience:** Business Analyst / Domain Expert (non-technical)
**Format:** Markdown — editable in any text editor, trackable in git
**One document per:** Service or domain (agent decides based on rule count)

## Generation Instructions (for the agent)

When generating a BA review document:

1. Use the template structure below
2. Replace all `<placeholders>` with actual extracted data
3. Pre-populate the `Classification` and `Weight` columns with agent recommendations
4. Mark rows needing BA attention with `[!!]` indicator
5. Group rules by business function, NOT by source file
6. Write all rule descriptions in business language (no code, no variable names)
7. Include the "Quick Stats" section so the BA sees scope at a glance
8. Save to: `assessment/ba-review-<service-name>.md`

## Document Template

---

```markdown
# Business Rule Review: <Service Name>

## For: <BA Name / Role>
## Prepared: <Date>
## Prepared By: SAAM Extraction Agent

---

## How to Use This Document

This document contains all business rules extracted from the legacy system for the **<Service Name>** service. Your job is to review each rule and make decisions about what should carry forward into the modernized system.

**For each rule (or group), you need to:**
1. Confirm the classification is correct (or change it)
2. Assign a business impact weight for Core/Active rules
3. Add notes where something needs correction or simplification

**Classification options:**
| Tag | Meaning | What Happens |
|-----|---------|--------------|
| `CORE` | Essential — system breaks without this | Implemented first, highest test coverage |
| `ACTIVE` | Needed — standard business logic | Implemented as extracted |
| `SIMPLIFY` | Correct intent but can be streamlined | You describe the simpler version, we rewrite |
| `OBSOLETE` | No longer needed — safe to drop | Moved to appendix, not implemented |
| `DEFERRED` | Valid but not needed for initial release | Moved to backlog for future phase |
| `MERGE` | Combine with another rule (specify which) | We consolidate and renumber |

**Weight options (for CORE and ACTIVE rules):**
| Weight | Meaning |
|--------|---------|
| `CRITICAL` | Failure = financial loss, compliance breach, or data corruption |
| `HIGH` | Failure = major operational disruption |
| `MEDIUM` | Failure = inconvenience, workarounds exist |
| `LOW` | Failure = minimal impact |

**Markers in this document:**
- `[!!]` = Agent flagged this for your attention (possible obsolete, ambiguous, or complex)
- `[OK]` = Agent is confident this is correct and active
- `[??]` = Agent is uncertain — needs your domain knowledge

---

## Quick Stats

| Metric | Value |
|--------|-------|
| Total rules extracted | <N> |
| Pre-classified as Core | <N> |
| Pre-classified as Active | <N> |
| Flagged for BA attention | <N> |
| Obsolete candidates | <N> |
| Optimization candidates | <N> |
| Estimated implementation effort (all rules) | <N> days |

---

## Section 1: Rules Requiring Your Attention

These rules were flagged because they may be obsolete, overly complex, or reference things that won't exist in the modernized system. **Please review each one carefully.**

**Complexity-flagged rules** (marked with `[COMPLEXITY]`): These rules have source algorithm complexity significantly higher than what was captured in the spec. After 2-3 extraction attempts, the agent could not fully decompose them. This may mean: (a) the remaining complexity is infrastructure/boilerplate that SHOULD be dropped in modernization, or (b) business decision paths were genuinely lost. **Your job:** confirm whether the condensed spec captures the full business intent, or identify what's missing.

### <Business Function Group Name>

#### BR-<ID>: <Rule Name>

| Field | Value |
|-------|-------|
| **What it does** | <Plain English description of the business rule — NO code> |
| **Why it was flagged** | <Reason: references retired system / date-bounded / workaround / complex / uncertain / COMPLEXITY: source=X, spec=Y, ratio=Z> |
| **Current behavior** | <What happens today when this rule fires> |
| **If dropped** | <What would change if we don't implement this in the new system> |
| **Agent recommendation** | <Obsolete / Simplify / Keep — with brief rationale> |

**Your decision:**

| Classification | Weight | Notes |
|----------------|--------|-------|
| _________ | _________ | _________ |

---

*Repeat for each flagged rule*

---

## Section 2: Core Business Rules (Pre-Validated)

These rules are clearly essential. The agent is confident they should carry forward. **You only need to assign weights and correct anything that looks wrong.**

### <Business Function Group Name>

| BR-ID | Rule Name | What It Does | Classification | Weight | Notes |
|-------|-----------|--------------|----------------|--------|-------|
| BR-XX-001 | <name> | <plain English> | CORE | _____ | |
| BR-XX-002 | <name> | <plain English> | CORE | _____ | |

---

## Section 3: Standard Business Rules

These are active rules that the agent believes are straightforward and correct. **Scan for anything that looks wrong, assign weights where you can.**

### <Business Function Group Name>

| BR-ID | Rule Name | What It Does | Classification | Weight | Notes |
|-------|-----------|--------------|----------------|--------|-------|
| BR-XX-010 | <name> | <plain English> | ACTIVE | _____ | |
| BR-XX-011 | <name> | <plain English> | ACTIVE | _____ | |

*If a rule looks wrong, change its Classification or add Notes explaining what should change.*

---

## Section 4: Simplification Opportunities

These rules work correctly but are more complex than they need to be. The modernized system is an opportunity to simplify. **For each one: keep as-is, or describe the simpler version.**

### <Rule Group>

#### BR-<ID>: <Rule Name>

**Current logic (simplified):**
<Plain English description of what the rule currently does, including all the branches and conditions>

**Why it's complex:**
<What makes this a candidate for simplification — e.g., "handles 3 edge cases that are no longer possible in the new architecture">

**Possible simplification:**
<Agent's suggestion for a simpler version>

**Your decision:**
- [ ] Keep as-is (complexity is justified)
- [ ] Simplify as suggested
- [ ] Simplify differently: _________________________________

---

## Section 5: Cross-Cutting Observations

Space for the BA to note patterns, concerns, or questions that apply across multiple rules:

**General observations:**

_________________________________________________________

**Rules that seem to conflict with each other:**

_________________________________________________________

**Business context the extraction may have missed:**

_________________________________________________________

**Questions for the development team:**

_________________________________________________________

**Workflow journey completeness (MANDATORY — trace end-to-end user tasks):**

For each major user task in this service (e.g., "pay a vendor," "apply a cash receipt," "close a period"), trace the complete journey through the documented workflows:

| User Task | Workflow Sequence | Gap Between Workflows? | What User Does in the Gap |
|-----------|------------------|------------------------|---------------------------|
| _________ | WF-XX-NNN → WF-XX-NNN | Yes / No | _________________________ |
| _________ | WF-XX-NNN → WF-XX-NNN | Yes / No | _________________________ |
| _________ | WF-XX-NNN → WF-XX-NNN | Yes / No | _________________________ |

If you find gaps where the user does work (selecting records, adjusting parameters, reviewing lists, releasing holds) that isn't covered by any workflow — please describe what happens there. These are the steps we need to add.

_________________________________________________________

---

## Section 6: Sign-Off

| Field | Value |
|-------|-------|
| Reviewer name | _________ |
| Review date | _________ |
| Services covered | _________ |
| Confidence in decisions | High / Medium / Low |
| Outstanding questions | _________ |
| Ready for implementation? | Yes / Yes with caveats / No — needs another pass |

**Caveats (if any):**

_________________________________________________________

```

---

## Parse-Back Instructions (for the agent)

When the BA returns the completed document, the agent processes it as follows:

### Reading Decisions

1. **Section 1 (Flagged rules):** Read the "Your decision" table for each rule. Extract Classification, Weight, and Notes.
2. **Section 2 (Core rules):** Read the Weight column. If Classification was changed from CORE, respect the override.
3. **Section 3 (Standard rules):** Read Weight column and any changed Classifications or Notes.
4. **Section 4 (Simplification):** Check which option was selected. If "Simplify differently," read the free-text description.
5. **Section 5 (Cross-cutting):** Parse for actionable items — conflicts need resolution, missing context needs incorporation.
6. **Section 6 (Sign-off):** Record reviewer, date, confidence. If "No — needs another pass," do NOT proceed to Phase 4b.

### Applying Decisions

For each BR-ID in the service spec:

| BA Decision | Agent Action |
|-------------|-------------|
| Classification = CORE or ACTIVE | Add `**Weight:** <value>` field to the BR-ID entry |
| Classification = SIMPLIFY | Rewrite Statement and Logic per BA notes, add `[Simplified per BA review — <date>]` |
| Classification = OBSOLETE | Move entire BR-ID entry to `07-obsolete-rules-appendix.md` with drop rationale from BA notes |
| Classification = DEFERRED | Move entire BR-ID entry to `06-deferred-rules.md` with defer reason |
| Classification = MERGE + target BR-ID | Combine logic into target BR-ID, add cross-reference to original IDs |
| Notes contain corrections | Update Statement/Logic/Examples accordingly |
| Weight = CRITICAL | Flag for exhaustive test coverage in Phase 4c |

### Handling Incomplete Reviews

If the BA left some rules without decisions:
- Rules without Classification → keep as pre-classified
- Rules without Weight → assign MEDIUM as default
- Rules with "??" in Notes → flag for follow-up in Phase 4b improvement plan

### Validation After Parse-Back

After applying all decisions:
1. Verify no Core rules were accidentally dropped (cross-check with pre-classification)
2. Count remaining rules vs. original → calculate scope reduction %
3. Verify rule numbering is still sequential (renumber if merges created gaps)
4. Update `06-completion-summary.md` with new counts
5. Generate scope reduction report

## Multiple Services

If the engagement has multiple services:
- Generate ONE review document per service (not one giant document)
- BA can review them in any order
- Agent processes each completed document independently
- Decision register aggregates across all services

## Document Naming

```
assessment/ba-review-<service-name>.md
assessment/ba-review-<service-name>-completed.md  (BA's returned version)
assessment/ba-decision-register.md                (generated after parse-back)
```
