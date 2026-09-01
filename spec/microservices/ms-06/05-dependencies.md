# Dependencies: Payments

**Service ID:** MS-06

## Services Consumed

### Order Management (MS-05) (async events)

#### Consumes: `OrderSubmitted.v1`
- **Triggered by:** BR-ORD-015 and BR-PA-020
- **Channel:** RabbitMQ domain-events exchange; source MS-04 via MS-05 order acceptance
- **Schema:** `spec/shared/event-schemas/order-submitted-v1.yaml`
- **Action:** bind immutable amount/currency/order context to the payment intent.
- **Idempotency:** inbox uniqueness on `eventId` and submission ID.
- **Status:** The graph edge is MS-06 -> MS-05 with protocol `Event`; the event source is documented as MS-04/MS-05 in the approved sequence and API design.

#### Consumes: `PaymentRequested.v1`
- **Triggered by:** BR-EXT-001
- **Channel:** RabbitMQ payment-commands exchange; source MS-05
- **Schema:** `spec/shared/event-schemas/payment-requested-v1.yaml`
- **Action:** begin the configured authorization or capture workflow.
- **Idempotency:** payment operation idempotency key and inbox uniqueness on `eventId`.

#### Consumes: `ConfigurationReferenceChanged`
- **Triggered by:** BR-EXT-001
- **Channel:** RabbitMQ configuration exchange; source MS-11
- **Schema:** `spec/shared/event-schemas/configuration-reference-changed.yaml`
- **Action:** refresh future-operation configuration; never mutate configuration pinned to an existing intent.
- **Idempotency:** configuration version guard.
- **Status:** RECONCILED — the MS-11 publication and the MS-12 adapter projection use the same
  event name and schema. MS-06 consumes the canonical name rather than the retired alias.

## Events Published

| Event | Triggered by | Schema | Consumers |
|---|---|---|---|
| `PaymentAuthorized.v1` | BR-ORD-015, BR-EXT-002 | `payment-authorized-v1.yaml` | MS-05 |
| `PaymentCaptured.v1` | BR-ORD-016, BR-EXT-002 | `payment-captured-v1.yaml` | MS-05 |
| `PaymentRefunded.v1` | BR-ORD-017, BR-EXT-003 | `payment-refunded-v1.yaml` | MS-05 |
| `PaymentFailed.v1` | BR-EXT-004, BR-PA-020, BR-PA-023 | `payment-failed-v1.yaml` | MS-05, operations |
| `PaymentReconciliationRequired.v1` | BR-ORD-015, BR-PA-023 | `payment-reconciliation-required-v1.yaml` | MS-05, MS-12 |

The rules additionally mention `PaymentRefundFailed` and `PaymentPendingManualSettlement` without
placing them in the MS-06 API-design event catalog. They are listed as unconfirmed event
variants in the shared index and are not silently treated as confirmed contracts.

### Unconfirmed rule-only event variants

| Event | Triggered by | Schema | Status |
|---|---|---|---|
| `PaymentRefundFailed` | BR-EXT-003 | `payment-refund-failed.yaml` | GAP — rules-only variant; consumer and final publication policy not defined |
| `PaymentPendingManualSettlement` | BR-EXT-009 | `payment-pending-manual-settlement.yaml` | GAP — rules-only variant; consumer and final publication policy not defined |

## External Dependencies

Provider-specific REST/webhook calls are external to the graph and are covered by BR-EXT-004
through BR-EXT-009 and BR-PA-023. No external provider contract is present in this repository;
provider paths, request/response fields, signature headers, and status mappings remain a GAP.

## Resilience

Payment event publication uses an outbox and at-least-once delivery. Non-idempotent provider
operations are not automatically retried unless the adapter supplies a provider-safe request key.
The API design confirms a 10s provider timeout and a per-provider/store circuit breaker after five
failures in one minute with 50% half-open sampling. Consumers use inbox deduplication.
