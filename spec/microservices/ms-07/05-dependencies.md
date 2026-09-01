# Dependencies: Pricing and Promotions

**Service ID:** MS-07

## Graph CALLS edges

MS-07 has no outgoing `CALLS` edge in the Phase 2 graph. The API-design artifact describes MS-02
catalog references and MS-10 scope as caller-supplied context or unresolved consumed dependencies,
not as graph-backed calls. No endpoint is invented here.

## Events Published

### `PriceChanged.v1`
- **Triggered by:** BR-PRC-002 and BR-PRC-013
- **Channel:** RabbitMQ domain-events exchange
- **Schema:** `spec/shared/event-schemas/price-changed-v1.yaml`
- **Consumers:** MS-04, MS-05, optionally MS-03 for display projection
- **Guarantees:** transactional outbox, at-least-once delivery

### `PromotionChanged.v1`
- **Triggered by:** BR-PRC-009, BR-PRC-010, and BR-PRC-013
- **Channel:** RabbitMQ domain-events exchange
- **Schema:** `spec/shared/event-schemas/promotion-changed-v1.yaml`
- **Consumers:** MS-04, MS-05, optionally MS-03 for display projection
- **Guarantees:** transactional outbox, at-least-once delivery

## Events Consumed

No event consumer is declared by the MS-07 API design or rules. Product/variant availability
resolution is represented as a data dependency in the pricing request and must remain
contract-mediated.

## Resilience

Published pricing events use outbox/inbox, at-least-once delivery, bounded exponential backoff,
and dead-letter handling. Synchronous quote calls are documented in MS-04's dependency artifact
and use the exact MS-07 provider paths.
