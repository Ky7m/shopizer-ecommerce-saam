# Dependencies: Content and Configuration

**Service ID:** MS-11

## Services Consumed

### Search (MS-03) (async event boundary)

MS-11 publishes content publication changes for search reindexing; the graph records an MS-11 ->
MS-03 `Event` edge. The compiled event carries the published content identity and localized
descriptions from the MS-11 content contract.

#### Event dependency: published-content indexing
- **Triggered by:** BR-MER-018 (visibility/publication policy)
- **Channel:** RabbitMQ domain-events exchange
- **Action:** MS-03 reindexes the published content projection.
- **Schema:** `spec/shared/event-schemas/content-published-v1.yaml`
- **Status:** RECONCILED — `ContentPublished.v1` is the canonical routing key and its payload
  is derived from `ContentItem`.

## Events Published

### `ContentPublished.v1`
- **Triggered by:** BR-MER-018
- **Channel:** RabbitMQ domain-events exchange
- **Schema:** `spec/shared/event-schemas/content-published-v1.yaml`
- **Consumers:** MS-03
- **Guarantees:** transactional outbox and at-least-once delivery; consumers deduplicate by `eventId`.

### `ConfigurationReferenceChanged`
- **Triggered by:** BR-EXT-025 and BR-EXT-026
- **Channel:** RabbitMQ configuration exchange
- **Schema:** `spec/shared/event-schemas/configuration-reference-changed.yaml`
- **Consumers:** MS-12
- **Status:** RECONCILED — the MS-11 configuration publication and MS-12 projection use the
  same shared event schema.

## Events Consumed

No inbound event is declared for MS-11. The store scope and provider-validation references in
the rules are request/context boundaries, not approved graph calls.

## Reconciliation note

Configuration publication is a target side effect of the MS-11 module/configuration mutation
boundary. The provider-specific settings remain opaque; only the reference, environment, module
type, code, and version cross the service boundary.
