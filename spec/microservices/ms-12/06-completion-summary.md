# Platform Integrations — Completion Summary

**Version:** 2.0  
**Date:** 2026-09-01  
**Service ID:** MS-12  
**Status:** 🟡 Phase 4 re-extraction complete; BA validation and Phase 4b placement remain  
**Port:** 8112  
**PostgreSQL schema:** `platform_integrations`  
**Analysis mode:** Hybrid CAST + direct Java source reading

## Artifact counts

| Artifact | Actual count |
|---|---:|
| Business rules | 23 |
| Source-derived rules | 20 |
| Target-only reliability rules | 3 |
| Domain tables | 5 |
| API operations | 18 |
| Consumed event definitions | 3 |
| Published event definitions | 2 |
| Event definitions total | 5 |
| Source files fully read | 16 |
| CAST transactions used as discovery evidence | 15 |
| CAST complexity hotspots | 6 |
| Preservation tables | 23 |

The rule count is a decomposition outcome, not a target yield: **23 rules = 20 source-derived
behavioral seams across 16 targeted Java files + 3 target reliability decisions**. The source
read added net-new findings that were not present in the prior surface extraction: the
`config2` assignment corrupts `config1`, the SMTP HTML data source reads the text writer, local
filesystem reads are unsupported, provider folder methods are incomplete, and GCP listing uses
the literal metageneration `42`. The three reliability rules are justified by the absence of a
durable operation, attempt, outbox, retry, and replay store in the source.

## Source coverage

All 16 files listed in `assessment/ms-12-cast-brief.md` were read. Files over 500 lines were
read in two passes; files at or below 500 lines were read completely in one pass. Exact line
ranges, section summaries, direct-read vectors, and CAST references are recorded in
`extraction-evidence.md`.

## Preservation assessment

| Area | Rules | Status | Explanation |
|---|---|---|---|
| Adapter registry and loading | 001–004 | FLAGGED for target correction | Source delete-then-create and `config2` corruption are documented; target uses an atomic projection and distinct settings. |
| UPS | 005, 007–009 | OK | Credential/package validation, US/CA eligibility, endpoint selection, XML mechanics, rounding, parsing, and failures are retained. |
| USPS | 006, 010–011 | OK | US-origin restriction, domestic/international XML branches, conversion, size thresholds, and response mapping are retained. |
| Maps and geolocation | 012–013 | OK | Zone/postal suppression, address construction, geocoding, kilometer conversion, GeoLite lookup, and unknown-address behavior are retained. |
| Email | 014–017 | FLAGGED for target correction | Sender selection and payload mapping are retained; target removes plaintext passwords, fixes HTML-body selection, and makes async outcome durable. |
| Storage | 018–020 | FLAGGED for target capability handling | Provider key mapping, byte reads, overwrite behavior, name filtering, and incomplete folder capabilities are explicit. |
| Reliability | 021–023 | Target-only | Idempotency, retry, outbox, replay, and dead-letter behavior has no legacy equivalent and is explicitly marked target-only. |

## Domain model result

The executable DDL declares five tables, each with a primary key:
`integration_endpoint`, `delivery_idempotency`, `email_message`, `outbox_event`, and
`delivery_attempt`. The operation table and the `operation_id`/`operation_item_key` fields make
the idempotency key and durable attempt association explicit for both single and batch uploads.
All source-derived columns are either projections of source fields or justified by a BR-ID;
tenant/store and timestamps are infrastructure annotations.

The database-logic catalog uses the required fixed order
`Name | Kind | Implements | Enforces Invariant | Migration Order | Binding | Placement`.
Function names and trigger names are separate rows. Every catalog row has executable DDL and
uses function orders `10`–`13` followed by trigger orders `30`–`33`.

## API result

The API design and OpenAPI contract contain the same 18 method/path pairs. File reads use
`FileContentResponse.contentBase64` (`format: byte`) so supported provider reads return bytes.
Single and batch uploads require `idempotencyKey`; batch responses and each item carry the
operation/attempt association. Folder operations require caller-supplied `provider` in both
design and contract. All delete operations return bodyless `204` responses.

## Endpoint coverage

| Method | Path | Status | Driving rules |
|---|---|---|---|
| GET | `/adapters` | COVERED | 001 |
| POST | `/adapters/refresh` | COVERED | 002–006 |
| POST | `/carrier-quotes/ups` | COVERED | 007–009 |
| POST | `/carrier-quotes/usps` | COVERED | 010–011 |
| POST | `/maps/distance` | COVERED | 012 |
| POST | `/geolocation/ip` | COVERED | 013 |
| POST | `/files` | COVERED | 018, 019, 021 |
| POST | `/files/batch` | COVERED | 018, 019, 021 |
| GET | `/files` | COVERED | 018, 019 |
| GET | `/files/{fileName}` | COVERED | 018, 019 |
| DELETE | `/files/{fileName}` | COVERED | 018, 019 |
| DELETE | `/files` | COVERED | 018, 019 |
| POST | `/files/folders` | COVERED | 020 |
| GET | `/files/folders` | COVERED | 020 |
| DELETE | `/files/folders` | COVERED | 020 |
| POST | `/emails` | COVERED | 014–017, 022–023 |
| GET | `/delivery-attempts/{attemptId}` | COVERED | 022–023 |
| POST | `/delivery-attempts/{attemptId}/replay` | COVERED | 023 |

## Placement candidates for Phase 4b

Default placement is application tier. These are evidence for Phase 4b, not final placement
decisions.

| Candidate | Legacy tier | Volume/set-vs-row | Frequency | App-tier risk | Default |
|---|---|---|---|---|---|
| UPS XML rating | Application integration | One request with one XML element per package | Interactive checkout | Provider latency directly delays quote response | App tier |
| USPS XML rating | Application integration | One request aggregating package dimensions and weight | Interactive checkout | Rebuilding provider requests in a batch worker delays quote response | App tier |
| Maps enrichment | Application integration | Two geocodes plus one distance matrix per eligible request | Interactive checkout | Sequential provider calls increase latency and quota pressure | App tier |
| Email delivery | Async application | One rendered message per notification | Event-driven | Synchronous provider calls can block the owning workflow | App tier with queue |
| Batch storage upload | Provider adapter | N files, row-at-a-time provider writes | Admin/content operation | Unbounded buffering or partial failure risks memory and consistency | App tier with bounded batch |
| Retry/replay worker | Target-only | Potentially unbounded historical attempts, queue sweep | Asynchronous | Request threads cannot own durable retry and dead-letter work | App worker |

## Open BA decisions

1. Confirm provider timeout, retry classification, and circuit-breaker thresholds.
2. Confirm whether email failure is always non-blocking for order workflows.
3. Confirm namespace deletion semantics for object stores: one prefix or recursively enumerated objects.
4. Confirm whether empty rendered HTML is a validation error.
5. Confirm the secret-reference and rotation contract with MS-11.
6. Confirm whether public object access is permitted; the source S3 adapter requests `PublicRead`.
7. Confirm currency normalization ownership for carrier options with MS-09.
8. Confirm retention policy for coarse geolocation results.

## Deliberately not generated

Tests, workflows, dependencies, DTOs, payment execution, shipping policy, merchant/module
configuration persistence, and product/media metadata remain outside this re-extraction scope.

## Phase 4a BA disposition

Mode A agent defaults were approved on 2026-09-02. 23 rules remain active after 0 approved obsolete-rule removal(s). Retained rules carry explicit Classification and Weight metadata; no rules were deferred, merged, or simplified without BA-specific guidance.
