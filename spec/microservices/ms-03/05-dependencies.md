# Dependencies: Search

**Service ID:** MS-03

## Services Consumed

### Catalog and Product (MS-02) (async events)

#### Consumes: `ProductChanged.v1`
- **Triggered by:** BR-CAT-022 and BR-CAT-023
- **Channel:** RabbitMQ domain-events exchange; source MS-02
- **Schema:** `spec/shared/event-schemas/product-changed-v1.yaml`
- **Action:** Reload the complete product projection, then upsert or remove localized search documents.
- **Idempotency:** Inbox uniqueness on `eventId` plus product aggregate/version guard.

#### Consumes: `CategoryChanged.v1`
- **Triggered by:** BR-CAT-021 and BR-CAT-023
- **Channel:** RabbitMQ domain-events exchange; source MS-02
- **Schema:** `spec/shared/event-schemas/category-changed-v1.yaml`
- **Action:** Refresh category-derived search metadata.
- **Idempotency:** Inbox uniqueness on `eventId`; replay-safe projection upsert.

#### Consumes: `MediaChanged.v1`
- **Triggered by:** BR-CAT-022 and BR-CAT-023
- **Channel:** RabbitMQ domain-events exchange; source MS-02
- **Schema:** `spec/shared/event-schemas/media-changed-v1.yaml`
- **Action:** Refresh or remove media references in the product projection.
- **Idempotency:** Inbox uniqueness on `eventId`; replay-safe projection upsert/delete.

## Events Consumed

### `ContentPublished.v1` from MS-11
- **Triggered by:** MS-11 publication boundary; no corresponding MS-11 BR-ID or payload is
  present in the approved rules.
- **Channel:** RabbitMQ domain-events exchange
- **Schema:** `spec/shared/event-schemas/content-published-v1.yaml`
- **Action:** Reindex published searchable content when enabled.
- **Idempotency:** Inbox uniqueness on `eventId`.
- **Status:** RECONCILED — the MS-11 contract publishes `ContentPublished.v1` with content
  identity, type, visibility, event occurrence time, and localized descriptions.

## Events Published

### `SearchIndexingFailed.v1`
- **Triggered by:** BR-CAT-023 and BR-EXT-024 after retry exhaustion or terminal projection failure
- **Channel:** RabbitMQ operational-events exchange
- **Schema:** `spec/shared/event-schemas/search-indexing-failed-v1.yaml`
- **Guarantees:** at-least-once; operational consumers must deduplicate by `eventId`

### `SearchRebuildCompleted.v1`
- **Triggered by:** BR-CAT-032 after a rebuild reaches `Succeeded`
- **Channel:** RabbitMQ operational-events exchange
- **Schema:** `spec/shared/event-schemas/search-rebuild-completed-v1.yaml`
- **Guarantees:** at-least-once; operational consumers must deduplicate by `eventId`

## Resilience

Event handlers use inbox deduplication, bounded exponential backoff, and dead-letter handling.
The source/API artifacts do not identify any business consumer for either published search event;
both are explicitly listed as intentionally unconsumed operational events in
`spec/shared/event-schemas/index.md`.
