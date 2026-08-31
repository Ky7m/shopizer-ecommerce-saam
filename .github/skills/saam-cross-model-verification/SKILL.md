---
name: saam-cross-model-verification
description: "Independent cross-model verification protocol to detect and eliminate LLM extraction errors."
copyright: "Copyright 2024-2026 SoftServe Inc. All rights reserved."
authors: "Max Kozinenko, Roman Kalita (SoftServe)"
---

# SAAM Cross-Model Verification

## Purpose

Cross-model verification is an OPTIONAL Phase 4 quality gate that uses a different AI model (or dramatically different prompt) to independently extract rules from the same source code. Disagreements between the primary and verification extractions are strong signals of potential correlated error.

**When to use:**
- High-complexity legacy systems (>30 cyclomatic complexity per component average)
- Systems with Critical business rules where extraction error is unacceptable
- Engagements where BA review is unavailable or cursory (Mode A defaults)
- When the primary extraction has many `extractionRisk: High` rules

**When to skip:**
- BA review (Phase 4a Mode B) provides thorough human validation
- System is well-documented (requirements docs available as ground truth)
- Complexity preservation shows clean results across all dimensions
- Time/budget constraints don't permit the extra verification pass

## Setup (One-Time Per Engagement)

### Step 1: Select Verification Model

**🔴 PROMPT HUMAN**: "Cross-model verification is available for this engagement. I'll use a different AI model to independently extract rules from the same source and compare the results.

Which verification model should I use?

Options:
- **Different model via API** — e.g., if primary extraction used Claude, verify with GPT-4 (or vice versa). Requires API key.
- **Same model, different prompt strategy** — I use a fundamentally different extraction approach (e.g., test-first: 'what tests would you write for this code?' then derive rules from tests). No extra API needed.
- **Same model, adversarial reviewer** — I prompt the model to actively look for errors in the primary extraction. No extra API needed.

Which approach? (Or skip cross-model verification entirely)"

### Step 2: Configure

Based on the human's choice:

**If different model via API:**
- Record the model name and access method
- The agent will send source code + a simplified extraction prompt to the verification model
- Compare results against primary extraction

**If same model, different prompt strategy:**
- No external configuration needed
- The agent uses the "test-first extraction" approach (see below)

**If adversarial reviewer:**
- No external configuration needed
- The agent prompts itself to challenge the primary extraction (see below)

Record the choice in `modernization/cross-model-config.md`:

```markdown
# Cross-Model Verification Configuration

- Mode: <different_model | test_first | adversarial>
- Verification model: <model name or "same model">
- Scope: <all_critical | high_complexity_only | specific_services>
- Configured: <date>
```

## Verification Protocols

### Protocol A: Different Model Extraction

Use when a second model is available (API access to a different LLM).

**For each service (or high-complexity component):**

1. **Prepare the verification prompt:**

```
You are analyzing legacy source code to extract business rules.
For the following source file, identify ALL business rules — decisions,
calculations, validations, state transitions, and routing logic.

For each rule, provide:
- A semantic statement (what the business means, not what the code does)
- The exact source location (file:function:lines)
- Key values: constants, thresholds, rates
- Conditions: when does this rule fire?
- Outcomes: what are the distinct results?

Source code:
<insert source file content>
```

2. **Send to verification model** (via API or separate session)

3. **Compare extractions:**

| Comparison | Meaning | Action |
|-----------|---------|--------|
| Both models extracted same rule with same semantics | High confidence — likely correct | None |
| Both extracted same rule but different constants/conditions | Potential value hallucination | Flag for human review |
| Primary extracted, verification missed | Could be over-extraction by primary | Review if rule is real |
| Verification extracted, primary missed | Potential omission by primary | Add the missing rule |
| Models disagree on rule meaning | Ambiguous source — correlated error risk | Human must decide |

4. **Record results** in `assessment/cross-model-verification-report.md`

### Protocol B: Test-First Extraction (Same Model, Different Approach)

Use when no second model is available. This breaks correlation by approaching the source from a completely different angle.

Instead of "extract business rules from this code," the agent asks:

```
You are a QA engineer writing acceptance tests for a legacy system.
Looking at this source code, what test scenarios would you write to
fully validate its behavior? For each scenario:
- What input would you provide?
- What output do you expect?
- What boundary conditions matter?
- What error cases should be tested?

Do NOT think about "business rules" or "specifications."
Think ONLY about: "what would I test to know this code works correctly?"

Source code:
<insert source file content>
```

Then:
1. The agent generates test scenarios from the source (different cognitive angle)
2. Each test scenario is compared against the primary extraction's Concrete Examples
3. Scenarios that DON'T map to any extracted BR-ID are potential omissions
4. Scenarios that contradict extracted BR-ID examples are potential extraction errors

**Why this works:** The "extract business rules" prompt and the "write tests for this code" prompt activate different reasoning paths. The first focuses on semantic meaning. The second focuses on observable behavior. Disagreements reveal where interpretation diverged from behavior.

### Protocol C: Adversarial Reviewer (Same Model, Critic Mode)

Use as the lightest-weight verification option. The agent reviews its own extraction with an explicitly adversarial mindset.

**For each Critical BR-ID, the agent asks itself:**

```
You are reviewing this business rule extraction for errors.
Your job is to find problems — not to confirm correctness.

Source code (original):
<source section for this BR-ID>

Extracted rule:
<BR-ID statement, logic, examples>

Challenge questions:
1. Are there ANY conditions in the source that are NOT in the extracted Logic?
2. Are there ANY constants/thresholds in the source that are NOT in the extraction?
3. Does the source have edge cases (boundary conditions, null checks, overflow) that the extraction doesn't cover?
4. Could the Statement be interpreted differently? Is it ambiguous?
5. Are the Concrete Examples complete? Could the source produce an output NOT covered by the examples?
6. Is there any code path that, if removed, would NOT be detected by the extracted test cases?

If you find ANY problem, report it. Do NOT confirm correctness.
```

**If the adversarial review identifies issues:** Flag them in the same format as Protocol A disagreements.

**Limitation:** This is the weakest decorrelation method — the same model reviewing itself has biases. But it's free and catches obvious oversights (especially omissions of boundary conditions).

## Scope Selection

Not every rule needs cross-model verification. Prioritize:

| Priority | Scope | Rationale |
|----------|-------|-----------|
| 1 (always verify) | Critical BR-IDs in complex components (>20 cyclomatic) | Highest risk: complex source + high business impact |
| 2 (verify if time) | All Critical BR-IDs regardless of complexity | All critical rules benefit |
| 3 (optional) | High-complexity components with `extractionRisk: High` | Even non-critical rules in complex code may be wrong |
| 4 (skip) | Simple rules in low-complexity components | Cost/benefit doesn't justify |

The human chooses scope at configuration time. Default recommendation: Priority 1 + 2.

## Output

After verification completes, produce:

```markdown
# Cross-Model Verification Report — <service>

## Summary
- Protocol: <A / B / C>
- Rules verified: N
- Agreements: X
- Disagreements: Y
- Potential omissions found: Z

## Disagreements (require human resolution)

### BR-OR-VAL-003: Order credit limit check
- **Primary extraction:** "Reject if total > credit_limit"
- **Verification says:** "Reject if total >= credit_limit" (boundary condition)
- **Source evidence:** Line 42: `if (total >= limit) reject()`
- **Resolution:** Primary extraction has boundary error. Fix to `>=`.

### BR-PA-CAL-007: Late fee calculation
- **Primary extraction:** "fee = base_rate * 1.5 for Gold tier"
- **Verification says:** "fee = base_rate * tier_multiplier where Gold=1.5, Silver=1.25, Bronze=1.0"
- **Source evidence:** Lines 88-92 show all three tiers
- **Resolution:** Primary missed Silver and Bronze tiers (omission). Add 2 rules.

## Potential Omissions (rules found by verification but not in primary)

### New: Error handling for null customer
- **Verification found:** Source checks for null customer at line 15, throws specific error
- **Primary missed:** No BR-ID covers this rejection path
- **Resolution:** Add BR-OR-VAL-NNN for null customer validation

## Confidence Adjustments

Based on this verification:
- Rules where both agree: provenanceConfidence remains as-is
- Rules where disagrement was resolved: provenanceConfidence → 0.95 (human-confirmed)
- Rules where omission was found: graph updated with new BR-IDs
```

## Integration with Phase 4

Cross-model verification runs AFTER primary Phase 4 extraction is complete but BEFORE Phase 4a (BA review):

```
Phase 4 extraction complete (all services specified)
    ↓
Cross-model verification (if configured)
    ↓
Fixes applied to specs (boundary corrections, omissions added)
    ↓
Phase 4a BA review (validates everything — including verification fixes)
```

This means BA review sees the corrected specs — not the raw primary extraction.

## Telemetry

Cross-model verification results feed telemetry:

```yaml
# In phase4-specs.yaml:
cross_model_verification:
  protocol: "test_first"
  rules_verified: 67
  agreements: 61
  disagreements: 4
  omissions_found: 2
  boundary_errors_fixed: 3
  verification_useful: true   # did it find real issues?
```

This data enables calibration: "is cross-model verification worth the cost?" After enough engagements, you'll know whether it consistently finds real errors or just produces noise.
