# Obsolete Rules Appendix — MS-07

**Approved:** Mode A agent defaults, 2026-09-02
**Status:** Excluded from the active implementation scope.

The following rules were explicitly approved for removal during Phase 4a. Their source evidence is retained here for traceability.

## Drop decision: BR-PRC-010
**Drop rationale:** The hardcoded Test1234 campaign ended on 2025-10-31 and is expired at the review date.

### BR-PRC-010: The extracted promotion rule grants ten percent for `Test1234` before 31 October 2025

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/resources/com/salesmanager/drools/rules/PromoCoupon.drl:1-16`  
**Discovery Method:** Direct Source Read  
**CAST Reference:** Promotion rule resource reached through `PromoCodeCalculatorModule`; no distinct promotion transaction is present in CAST.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 3 | 4 | GAP |
| Data-flow | 2 | 2 | OK |
| Constants | 3 | 3 | OK |
| State transitions | 0 | 1 | GAP |
| Outcomes | 2 | 3 | GAP |
| Data writes | 1 | 0 | GAP |
| Integrations | 0 | 0 | OK |
| Error paths | 1 | 3 | GAP |

**Preservation:** FLAGGED — the source rule’s visible date-bounded behavior is preserved, while the target exposes the expired state and avoids treating a global rule response as durable promotion state.

**Statement:** The extracted rule named `Bam0520` matches promotion code `Test1234` only when the evaluation date is earlier than `31 October 2025`, and assigns a discount rate of `10%`. Because the current analysis date is `2026-09-01`, this extracted rule is expired and must not produce a discount for a current-time evaluation. The code and date are preserved as source evidence, not as an assertion that the campaign remains commercially active.

**Intent:** Calculation; Validation; Compliance

**Logic:**
```text
IF input.promoCode = 'Test1234'
   AND input.evaluationDate < 2025-10-31:
    response.discount = 0.10
ELSE:
    response.discount remains null
```

**Data Dependencies:** Promotion code, evaluation timestamp, rule identifier, discount rate, and promotion evaluation response.

**Side Effects:** The rule sets the in-memory promotion response used by the promotion processor. It does not create a coupon record, redemption, or durable campaign mutation.

**Concrete Example:**
- **Input:** `POST /api/v1/pricing/promotions/evaluate` with `{"promoCode":"Test1234","items":[{"sku":"SKU-MUG-BLUE","quantity":1}],"evaluationDate":"2025-10-30T23:59:59Z"}`
- **Success:** `200 {"promoCode":"Test1234","matched":true,"discountRate":0.10,"reduction":2.00}`
- **Error Input:** The same request with `evaluationDate:"2026-09-01T12:00:00Z"`.
- **Error Output:** `422 {"error":"PROMOTION_EXPIRED","message":"Promotion code Test1234 has no active rule at the requested evaluation time","statusCode":422}`
