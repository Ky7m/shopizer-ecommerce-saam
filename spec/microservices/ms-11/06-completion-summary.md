# MS-11 Content and Configuration — Completion Summary

**Version:** 1.0  
**Date:** 2026-09-01  
**Status:** Phase 4 extraction complete — ready for Phase 4a BA review  
**Service ID:** MS-11  
**Analysis mode:** Hybrid CAST plus direct source extraction

## Actual counts

| Artifact | Count |
|---|---:|
| Business rules | 41 |
| Semantic preservation tables | 41 |
| Semantic preservation dimensions | 328 |
| Target database tables | 5 |
| Data invariants | 24 |
| Entity lifecycle models | 4 |
| API design operation rows | 45 |
| OpenAPI paths | 46 |
| OpenAPI operations | 58 |
| OpenAPI schemas | 54 |

Counts were derived from the current artifacts, not fitted to a target. The
rules decompose into 16 CMS/content rules (`BR-MER-013` through `BR-MER-028`),
15 configuration/module rules (`BR-CF-001` through `BR-CF-015`), and 10
provider/extensibility boundary rules (`BR-EXT-021` through `BR-EXT-030`).

## Decomposition outcome

The deep extraction identified store-scoped content identity, localized
description replacement and projection, visibility/publication behavior,
provider-backed file orchestration, merchant configuration serialization,
module discovery and replacement, cache behavior, provider validation
boundaries, and explicit legacy no-op/defect behavior. MS-11 is not CRUD-only.

Net-new findings beyond the CAST/P1 grouping include:

- Public page listing routes records through a box converter instead of the
  page converter.
- The public image-download path returns `null` after extracting a filename.
- Module replacement deletes and recreates by code without invalidating module
  discovery cache.
- Hydration can assign persisted `config2` into `config1`.
- Administrative module detail can copy integration keys without redaction.
- File rename removes the original before recreating the new object and is not
  atomic.

## CAST and source coverage

The package uses the `Shopizer-Backend` CAST application and delivery
`Onboarding-202511171247`. Critical full graphs covered page update (244057),
image rename (244042), payment module configuration (244108), shipping module
configuration (244206), and integration-module replacement (244013).

The extractor evidence records direct reads and multi-pass reads of the
principal content, configuration, repository, domain, provider-neutral, and
module-loader sources. It records 41 rules with source references, CAST
references, concrete examples, and eight-dimensional preservation tables.
Missing or corrected source paths are recorded explicitly in
`extraction-evidence.md`; no source read is silently fabricated.

## Domain model and boundaries

MS-11 owns `content`, `content_description`, `content_file`,
`merchant_configuration`, and `module_configuration`. Store lifecycle remains
MS-10. Language, country, currency, and geozone are shared/reference data.
Payment, shipping, and CMS provider execution remains behind MS-12 boundaries;
MS-11 owns configuration state and may invoke validation/discovery contracts.
Published-content indexing is an MS-03 integration boundary to reconcile in
Stage 1.5.

The domain model includes closed lifecycle models for content, configuration,
module metadata, and provider-backed file state, plus invariants for
merchant-scoped uniqueness, secret handling, cache freshness, and provider
operation state. Database logic objects are included only where backed by
executable DDL and a real rule or invariant.

## Endpoint coverage

The API design contains 45 operation rows. The OpenAPI contract contains 46
paths and 58 operation IDs because it documents compatibility, retired, and
explicitly defective legacy routes in addition to the active target surface.
All API-design operations have corresponding contract operations. Coverage
includes public/private content reads, page and box mutations, localization,
file upload/list/download/remove/rename/folder operations, public and
merchant configuration, module discovery/detail/save, and global module
replacement. Provider execution endpoints are excluded.

## Semantic preservation

All 41 rules contain all eight required dimensions:
Control-flow, Data-flow, Constants, State transitions, Outcomes, Data writes,
Integrations, and Error paths. Thirty-five rules have no GAP dimension and six
rules carry explicit, intentional `GAP` deltas where the target hardens or
normalizes legacy behavior: BR-MER-026, BR-CF-015, BR-EXT-023,
BR-EXT-026, BR-EXT-029, and BR-EXT-030. No rule is `CRITICAL` or
`UNRESOLVED`; the six GAPs remain subject to BA review.

## Known BA decisions

Phase 4a must decide the target treatment for the page-list mapper defect,
public download and folder no-op routes, missing merchant configuration,
options parsing, `config2` compatibility, non-atomic rename, provider
capability differences, module-cache invalidation, secret redaction,
idempotency for retry-sensitive mutations, and the MS-03 reindexing contract.
No classification, weighting, obsolescence, or human sign-off has been
approved yet.

## Contract validation status

- OpenAPI version is 3.1.0.
- Operation IDs are unique and component references resolve.
- Arrays define item schemas and error responses use named schemas.
- Tenant, store, authorization, and correlation headers are represented.
- Contract exceptions are documented for intentionally empty legacy success
  bodies, retired operations, and the optional store scope on global module
  replacement.

**Overall:** MS-11's six-file Phase 4 extraction package is complete and ready
for Phase 4a business-rule validation. `05-dependencies.md`, DTOs, workflows,
tests, and shared contract reconciliation remain deferred to their defined
later stages.
