# Specification deviation log

## DEV-CODE entries

| ID | Date | Service | Deviation/fix | Authority | Resolution |
|---|---|---|---|---|---|
| DEV-CODE-MS02-001 | 2026-09-03 | MS-02 | Initial live startup could race PostgreSQL database creation and terminate before health became ready. | Reference startup/runtime behavior | Added bounded retry around schema initialization; DDL and migrations remain idempotent. |
| DEV-CODE-MS02-002 | 2026-09-03 | MS-02 | Listing fetch used `SELECT DISTINCT` with an unselected sort expression, which PostgreSQL rejects. | MS-02 BR-CAT-026 and PostgreSQL query semantics | Reused the listing predicate and grouped by product ID/sort order so count and fetch remain distinct and consistent. |
| DEV-CODE-MS02-003 | 2026-09-03 | MS-02 | Nullable UUID exclusion parameters were untyped when used in `IS NULL` predicates. | PostgreSQL parameter typing | Generate the exclusion predicate only when an exclusion ID is supplied. |

## Validation environment notes

The native MTP command now runs successfully after the `sourcecode/global.json` fix:

`cd sourcecode && dotnet test --project
Shopizer.IntegrationTests/Shopizer.IntegrationTests.csproj
--filter-class '*CatalogProductComprehensiveTests*'`

The latest run executed all 111 tests with 28 passed, 83 failed, and 0 skipped after
the approved contract change moved the MS-02 base path to `/api/v1`. All test paths now
use the explicit `/api/v1/...` form. Remaining failures use placeholder resource IDs
and duplicate create payloads without fixture isolation; the first actionable failure
is an expected-success category create receiving `PARENT_CATEGORY_NOT_FOUND`. These
assumptions must be corrected without weakening the contract's uniqueness and
not-found behavior.

## MS-02 final validation updates

The final native MTP run passed all 111 tests (`111 passed, 0 failed, 0 skipped`).
The following test-harness adaptations were required to make generated scenarios
independent and deterministic; they do not relax service-side uniqueness,
authorization, tenancy, or not-found behavior.

| ID | Type | Deviation | Spec Says | Service Does | Fix Recommendation |
|---|---|---|---|---|---|
| DEV-TEST-MS02-001 | DEV-TEST | Generated success scenarios needed catalog prerequisites. | Category/product relationships must resolve according to the contract and business rules. | Correctly rejects references that do not exist. | Seed the required tenant/store catalog graph before each scenario; no service change. |
| DEV-TEST-MS02-002 | DEV-TEST | Generated create scenarios reused unique fields across facts. | Uniqueness constraints remain enforced for catalog codes, SKUs, and slugs. | Correctly rejects duplicates. | Generate unique test values per scenario; no service change. |
| DEV-TEST-MS02-003 | DEV-TEST | Generated negative scenarios reused IDs created by other facts. | Missing resources must return the contract-defined not-found response. | Correctly returns not-found only for absent resources. | Use deterministic missing IDs/SKUs/slugs for negative cases; no service change. |

## Final summary statistics

| Category | Count |
|---|---:|
| DEV-CODE (bugs fixed) | 4 |
| DEV-TEST (test adapted) | 3 |
| SPEC-DRIFT (needs decision) | 0 |

## Governance tooling update

| ID | Type | Deviation | Resolution |
|---|---|---|---|
| DEV-CODE-MS02-004 | DEV-CODE | The fidelity audit did not recognize valid flat BR-IDs or service files whose declared type name differed from the filename stem. | Updated the audit to support both BR-ID forms and declared-type reachability matching; MS-02 now audits as 41 reachable claims with no dead-code flags. |
