# Search Service — API Design

**Service ID:** MS-03  
**Port:** 8103  
**Base path:** `/api/v1`  
**JSON naming:** camelCase  
**Context:** `x-tenant-id`, `x-store-id`, and `x-correlation-id` are required on every operation.

## Endpoint inventory

| # | Method | Path | Purpose | Driving rules |
|---:|---|---|---|---|
| 1 | POST | `/search` | Search indexed products | BR-CAT-020, BR-CAT-021, BR-CAT-022, BR-CAT-034, BR-EXT-024 |
| 2 | POST | `/search/autocomplete` | Return keyword suggestions | BR-CAT-020, BR-CAT-024, BR-EXT-024 |
| 3 | POST | `/private/system/search/index` | Start an asynchronous store rebuild | BR-CAT-020, BR-CAT-032, BR-EXT-023, BR-EXT-024 |

## POST /api/v1/search

Searches the tenant/store product projection.

**Request body:**
```json
{"query":"blue mug","count":20,"start":0}
```

- `query` is required and non-blank.
- `count` defaults to `100` and is between `1` and `100`.
- `start` defaults to `0` and is non-negative.
- Locale is resolved from authenticated request context and store code is normalized for provider lookup.

**Responses:** `200` `SearchResultsResponse`; `400` malformed context; `422` validation failure; `503` disabled/unavailable provider; `500` unexpected failure.

## POST /api/v1/search/autocomplete

Returns at most fifteen provider-derived suggestions. Category facets are not returned.

**Request body:**
```json
{"query":"blu"}
```

**Responses:** `200` `AutocompleteResponse`; `400` malformed context; `422` blank query/locale; `503` disabled/unavailable provider; `500` unexpected failure.

## POST /api/v1/private/system/search/index

Starts a store-scoped asynchronous full rebuild.

**Headers:** required context plus `Authorization: Bearer <token>` and `idempotency-key`.

**Authorization:** `SUPERADMIN`, `ADMIN`, `ADMIN_CATALOGUE`, or `ADMIN_RETAIL`, with access to the requested store.

**Processing:** validate context and authorization, reject disabled indexing or an active duplicate, create a `Requested` rebuild job, enqueue asynchronous processing, index each MS-02 product projection, and transition the job to `Succeeded`, `Failed`, or `Cancelled`.

**Responses:** `202` `RebuildAcceptedResponse`; `400` malformed context; `401` unauthenticated; `403` out of scope; `409` disabled or active duplicate; `500` scheduling failure.

## Event contracts

### Consumed

- `ProductChanged.v1` from MS-02: refresh or remove localized product projections.
- `ContentPublished.v1` from MS-11: reindex searchable published content when enabled by configuration.

### Published

- `SearchIndexingFailed.v1`: emitted after retry exhaustion or terminal projection failure.
- `SearchRebuildCompleted.v1`: emitted when a rebuild reaches `Succeeded`.

Events require `eventId`, `eventVersion`, `tenantId`, `storeId`, correlation ID, source version, and a product/rebuild identifier where applicable. Consumers must tolerate replay.

## Boundary rules

- MS-03 consumes MS-02 projections/events and never writes MS-02 tables.
- Inventory and price values in search documents are display projections, not authoritative state.
- Provider-specific search infrastructure is external and accessed through a neutral adapter.
