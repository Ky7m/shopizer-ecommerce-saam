# MS-08 Tax Completion Summary

**Version:** 1.0  
**Date:** 2026-09-01  
**Service:** MS-08 Tax  
**Status:** 🟡 In Progress — extraction complete; BA review and graph ingestion pending

## Service overview

| Attribute | Value |
|---|---|
| Service name | Tax |
| Service ID | MS-08 |
| Target port | 8008 |
| Database schema | `tax_schema` |
| Priority | 2 — business service |
| Automation potential | 84% provisional |
| Implementation | 0% |
| Tests | 0% |
| External tax provider | None identified; none fabricated |

## Rule decomposition outcome

The final rule count is **20**. This is a decomposition outcome, not a target-count inflation exercise.

| Rule group | Count | Rules |
|---|---:|---|
| Configuration | 2 | BR-TAX-CFG-001..002 |
| Tax-class administration | 3 | BR-TAX-CLS-001..003 |
| Tax-rate administration | 5 | BR-TAX-RAT-001..005 |
| Tax calculation | 10 | BR-TAX-CAL-001..010 |
| **Total** | **20** | **20 unique business constraints** |

The eight P1 tax rules were re-extracted at P4 depth:

| P1 rule | P4 rule |
|---|---|
| BR-PRC-014 | BR-TAX-CAL-002 |
| BR-PRC-015 | BR-TAX-CAL-003 |
| BR-PRC-016 | BR-TAX-CAL-004 |
| BR-PRC-017 | BR-TAX-CAL-006 |
| BR-PRC-018 | BR-TAX-CAL-007 |
| BR-PRC-019 | BR-TAX-CAL-009 |
| BR-PRC-020 | BR-TAX-CAL-010 |
| BR-PRC-021 | BR-TAX-CAL-008 |

## Net-new findings from the deep read

1. `TaxConfiguration.toJSONString()` persists only the tax-basis field; both geographic policy booleans are omitted.
2. `TaxRateServiceImpl` accepts a `TaxClass` parameter, but the repository methods do not use it in their filtering predicates.
3. `PersistableTaxRateMapper` assigns `source.zone` to `stateProvince`, conflating two distinct geographic fields.
4. `existsTaxRate` calls a lookup that throws `ResourceNotFoundException` for absence, so the advertised boolean false result is unreachable.
5. Shipping/handling taxation uses the default tax class but does not insert that class into the `taxClasses` lookup map.
6. `ReadableTaxRateMapper` assumes a non-null zone even though the repository permits `ZONE_ID IS NULL`.
7. Same-code consolidation computes an aggregate amount without assigning it back to the retained tax item.
8. The different-country boolean has behavior opposite to its most natural reading: `true` replaces customer jurisdiction with store jurisdiction.
9. The legacy list endpoints ignore their `count` and `page` arguments and always report one page.
10. The tax-rate response mapper exposes only one localized description, selected by request language.

## Domain model

| Entity/table | Purpose | Lifecycle |
|---|---|---|
| `tax_classes` | Store-scoped tax classifications | Present → Deleted |
| `tax_rates` | Geographic percentage rates and compound behavior | Present → Deleted |
| `tax_rate_descriptions` | Localized rate labels | Owned by tax rate |
| `tax_configurations` | Store tax policy | Upsert reference data |
| `tax_quotes` | Calculation result and replay boundary | Calculated or Failed |
| `tax_quote_items` | Per-code tax result | Owned by quote |

All cross-service identifiers are non-FK boundary values. Internal FKs exist only between MS-08 tables.

## API coverage

| Method | Endpoint | Status | Driving BR-IDs |
|---|---|---|---|
| POST | `/api/v1/tax-classes` | COVERED | BR-TAX-CLS-001 |
| GET | `/api/v1/tax-classes` | COVERED | BR-TAX-CLS-002 |
| GET | `/api/v1/tax-classes/exists` | COVERED | BR-TAX-CLS-001 |
| GET | `/api/v1/tax-classes/{id}` | COVERED | BR-TAX-CLS-002 |
| PUT | `/api/v1/tax-classes/{id}` | COVERED | BR-TAX-CLS-003 |
| DELETE | `/api/v1/tax-classes/{id}` | COVERED | BR-TAX-CLS-003 |
| POST | `/api/v1/tax-rates` | COVERED | BR-TAX-RAT-001 |
| GET | `/api/v1/tax-rates` | COVERED | BR-TAX-RAT-003 |
| GET | `/api/v1/tax-rates/exists` | COVERED | BR-TAX-RAT-005 |
| GET | `/api/v1/tax-rates/{id}` | COVERED | BR-TAX-RAT-004 |
| PUT | `/api/v1/tax-rates/{id}` | COVERED | BR-TAX-RAT-002 |
| DELETE | `/api/v1/tax-rates/{id}` | COVERED | BR-TAX-RAT-004 |
| GET | `/api/v1/tax-configuration` | COVERED | BR-TAX-CFG-001 |
| PUT | `/api/v1/tax-configuration` | COVERED | BR-TAX-CFG-002 |
| POST | `/api/v1/tax-calculations` | COVERED | BR-TAX-CAL-001..010 |

## Semantic preservation

| Source component | CAST control | Direct-read business vector | Rules | Preservation |
|---|---:|---|---:|---|
| `TaxFacadeImpl` | 81 | Admin validation, ownership, mapping, and response branches | 8 | FLAGGED only where target repairs source behavior |
| `TaxServiceImpl` | 52 | Basis, geography, grouping, shipping, rate application, consolidation | 10 | FLAGGED target corrections documented |
| `TaxClassApi` | 3 | Endpoint routing and facade delegation | 3 | OK |
| `TaxRatesApi` | 3 | Endpoint routing and facade delegation | 5 | OK |
| `TaxClassServiceImpl` | 3 | Store-scoped CRUD orchestration | 3 | OK |
| `TaxRateServiceImpl` | 3 | Store, language, geography, and rate lookup orchestration | 5 | FLAGGED tax-class predicate correction |
| **Total** | **145** | **All eight dimensions reviewed** | **20** | **No unresolved business dimension** |

### Eight-dimension preservation status

| Dimension | Result |
|---|---|
| Control-flow | Preserved; infrastructure null/error branches were not promoted to independent business rules |
| Data-flow | Preserved, including tax classes, rates, descriptions, configuration, address snapshots, and shipping inputs |
| Constants | Preserved: `DEFAULT`, shipping-address default, rounding scale 2, half-up rounding, and rate bounds |
| State transitions | Administration and quote lifecycle explicitly modeled |
| Outcomes | Success, no-tax, empty-result, duplicate, not-found, unauthorized, and validation outcomes modeled |
| Data writes | Tax-class/rate/configuration/quote writes separated from cross-service totals |
| Integrations | Merchant configuration and reference-data resolutions documented; no external provider invented |
| Error paths | Source exceptions and target explicit diagnostics documented |

## Automation assessment

| Dimension | Score | Notes |
|---|---:|---|
| Statement clarity | 90% | Statements use business terms and avoid legacy identifiers |
| Algorithm completeness | 86% | Calculation ordering, grouping, compounding, and rounding are explicit |
| Data-model readiness | 88% | Executable PostgreSQL DDL, constraints, indexes, and ownership boundaries included |
| API contract readiness | 90% | Named schemas, exact paths, headers, errors, and response shapes locked |
| Edge-case coverage | 78% | Null inputs, missing geography, missing rates, country/province policies, and mapper defects covered |
| Source traceability | 95% | Mandatory business and contract files have exact line references |
| Cross-service boundary clarity | 88% | Snapshot and identifier boundaries are explicit; provider contract remains future work |
| Implementation readiness | 84% | Ready for BA review and DTO generation after contract approval |
| **Overall provisional score** | **84%** | Not a human sign-off |

## Placement candidates

| Candidate | Legacy tier | Data-volume signal | Set-vs-row | Call frequency | App-tier risk | Default |
|---|---|---|---|---|---|---|
| Tax-class aggregation | Application service | Number of cart/order lines per calculation | Row loop over request lines | Per calculation | Large carts increase in-process iteration but no database round-trip is required | App |
| Rate resolution | Application/repository | Rates per store and geographic scope | Set-based repository query | Per calculation | Incorrect filtering can apply another class's rate; app fallback must not bypass query predicates | App |
| Compound calculation | Application service | Number of rates per class | Row-at-a-time ordered loop | Per calculation | Ordering and monetary rounding can drift if delegated to generic SQL aggregation | App |
| Quote persistence | Target-only | One quote plus several items per calculation | One header plus batch item insert | Optional per calculation | Naive per-item writes can increase latency | App initially |

## Dependencies

- **Consumes:** MS-01 customer/address snapshots.
- **Consumes:** MS-02 product tax-class identifiers.
- **Consumes:** MS-04 cart/checkout item and shipping inputs.
- **Consumes:** MS-05 order context where order-linked calculations are required.
- **Consumes:** MS-09 shipping and handling amounts.
- **Consumes:** MS-10 store and merchant configuration context.
- **Reference data:** country, zone, and language identifiers through declared contracts.
- **External tax provider:** none identified; no provider is assumed.

## Explicit exclusions

- No order-total persistence.
- No cart or checkout mutation.
- No product tax-class mutation.
- No customer/address mutation.
- No shipping quote or provider mutation.
- No direct cross-service foreign keys.
- No Drools rules, carrier providers, frontend Angular components, or external tax provider implementation.
- No tests or dependency files generated.

## Completion status

- [x] Business rules extracted
- [x] Exact source references included
- [x] Concrete examples included for every rule
- [x] Eight-dimension preservation tables included for every rule
- [x] Executable PostgreSQL DDL included
- [x] API design completed
- [x] OpenAPI contract completed
- [x] Required tenant/store/correlation headers locked
- [x] Source defects and ambiguities explicitly documented
- [ ] BA validation and classification
- [ ] Neo4j graph ingestion
- [ ] DTO generation
- [ ] Implementation
- [ ] Test generation
