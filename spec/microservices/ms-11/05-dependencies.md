# Dependencies: Content and Configuration

**Service ID:** MS-11

## Services Consumed

### Search (MS-03) (async event boundary)

MS-11 publishes content publication changes for search reindexing; the graph records an MS-11 ->
MS-03 `Event` edge. The approved rules and contract do not define an event name or payload.

#### Event dependency: published-content indexing
- **Triggered by:** BR-MER-018 (visibility/publication policy)
- **Channel:** RabbitMQ domain-events exchange
- **Action:** MS-03 reindexes the published content projection.
- **Schema:** `spec/shared/event-schemas/content-published-v1.yaml`
- **Status:** GAP — event name is supplied by the sequence/reconciliation artifacts, but no
  publisher BR-ID side effect, routing key, or payload fields are defined.

## Events Published

### `ContentPublished.v1` (unconfirmed)
- **Triggered by:** BR-MER-018
- **Channel:** RabbitMQ domain-events exchange
- **Schema:** `spec/shared/event-schemas/content-published-v1.yaml`
- **Consumers:** MS-03
- **Guarantees:** target event behavior is not yet approved; no implementation guarantee is claimed.

### `ConfigurationReferenceChanged`
- **Triggered by:** BR-EXT-025 and BR-EXT-026
- **Channel:** RabbitMQ configuration exchange
- **Schema:** `spec/shared/event-schemas/configuration-reference-changed.yaml`
- **Consumers:** MS-12
- **Status:** GAP — this event is required by the authoritative MS-12 contract, but MS-11's
  contract does not declare its publisher operation or event catalog.

## Events Consumed

No inbound event is declared for MS-11. The store scope and provider-validation references in
the rules are request/context boundaries, not approved graph calls.

## Reconciliation note

The graph edge and MS-12 provider contract prove an intended configuration hand-off, but the
MS-11 publisher contract is absent. This is a real preservation/spec gap, not a missing endpoint
that can be filled from a service-local contract.

