# Merchant and Store Administration — Phase 4 Completion Summary

**Service:** MS-10 Merchant and Store Administration  
**Schema:** `merchant_store`  
**Status:** 🟡 Complete for Phase 4 extraction; BA validation, Phase 4a classification, and shared-contract reconciliation remain required.

## Decomposition outcome

The package contains **21 rules**: 13 carry-forward rules (`BR-MER-001..012` and `BR-UI-007`) plus 8 net-new rules identified from the CAST-bounded deep read. The rules cover store identity and contact validation, uniqueness, defaults, hierarchy, context resolution, authorization, pagination, branding-provider separation, supported languages, and transactional creation.

The large CAST create and update transactions were treated as orchestration flows rather than CRUD-only endpoints. Store configuration and CMS/file-provider behavior was kept outside the MS-10 ownership boundary and recorded as an MS-11 or external dependency.

## Actual artifact counts

| Artifact | Actual count |
|---|---:|
| Business-rule IDs | 21 |
| PostgreSQL tables | 2 |
| PostgreSQL enum types | 0 |
| API endpoint methods | 17 |
| API path items | 11 |
| CAST-listed source files read | 17 |
| Semantic preservation tables | 21 |

## Endpoint coverage

| Endpoint family | Methods | Status | Driving rules |
|---|---:|---|---|
| `/stores` | GET, POST | COVERED | BR-MER-001..004, BR-MSA-VAL-001, BR-MSA-VAL-003, BR-MSA-READ-001 |
| `/stores/{storeCode}` | GET, PUT, DELETE | COVERED | BR-MER-002, BR-MER-005..006, BR-MER-011, BR-MSA-VAL-002 |
| `/stores/uniqueness` | GET | COVERED | BR-MER-003, BR-MSA-VAL-001 |
| `/stores/names` | GET | COVERED | BR-MSA-LST-001, BR-UI-007 |
| `/merchants/{merchantCode}/stores` | GET | COVERED | BR-MER-008, BR-MSA-AUTH-001, BR-MSA-READ-001 |
| `/merchants/{merchantCode}/children` | GET | COVERED | BR-MER-008..009, BR-MSA-AUTH-001 |
| `/stores/{storeCode}/languages` | GET, PUT | COVERED | BR-MER-012, BR-MSA-LANG-001 |
| `/stores/{storeCode}/branding` | GET, PUT | COVERED | BR-MSA-BRD-001, BR-MER-011 |
| `/stores/{storeCode}/branding/logo` | POST, DELETE | COVERED | BR-MSA-BRD-001 |
| `/stores/signup` | POST | COVERED | BR-MSA-VAL-003, BR-MER-001..004 |
| `/stores/{storeCode}/signup/{token}` | GET | COVERED | BR-MSA-VAL-003 |

## Semantic preservation

| Source component group | Status | Evidence |
|---|---|---|
| Store identity and contact mapping | OK with target validation tightening | Required fields, country resolution, and code constraints are explicit. |
| Store hierarchy and deletion | FLAGGED → target invariant | Parent existence, self-parent rejection, child handling, and default-store protection are explicit. |
| Store context and authorization | FLAGGED → target security boundary | Default resolution and URI/store hierarchy authorization are explicit. |
| Store collection and names | OK | Pagination, deterministic ordering, and authorized selector results are explicit. |
| Branding and providers | GAP → boundary handoff | Store metadata is owned here; binary storage and configuration remain provider/MS-11 concerns. |
| Languages and defaults | FLAGGED → target invariant | Default-language inclusion and fallback behavior are explicit. |

Every rule contains a source reference, discovery method, CAST reference, semantic statement, intent, logic, data dependencies, side effects, concrete success/error examples, and all eight preservation dimensions.

## Dependencies and events

- **Upstream:** MS-01 administrator identity and store-scope authorization; shared country, zone, currency, language, and measurement-unit reference contracts.
- **Downstream:** MS-11 for CMS/configuration and file-provider operations; store context consumed by catalog, order, payment, and content services.
- **External:** configured object/file storage provider for logo bytes.
- **No direct cross-service table reads:** store identifiers and reference codes remain opaque at service boundaries.
- **Events:** store-created, store-updated, and store-deleted integration events are candidate contracts for Stage 1.5 reconciliation; no event schema is claimed by this package.

## Unresolved gaps

1. The exact default-store configuration source and lifecycle for changing the default store require BA confirmation.
2. Parent deletion policy must be selected explicitly: restrictive rejection or atomic cascade.
3. The reference-data contracts for country, zone, currency, language, and measurement units must be reconciled in Stage 1.5.
4. Logo content type, size limits, virus scanning, and provider failure semantics remain external integration decisions.
5. Signup token persistence, expiry, and activation side effects need a dedicated identity/merchant workflow decision.
6. The requested six-file package intentionally omits `05-dependencies.md`, DTOs, workflows, tests, and graph import; those belong to later SAAM stages.

## Readiness

The six requested files exist and are implementation-oriented: semantic rules with CAST/source evidence, executable PostgreSQL DDL, a 17-operation OpenAPI 3.1 contract, endpoint coverage, boundary notes, and explicit gaps. Human review remains required before Phase 4a sign-off.
