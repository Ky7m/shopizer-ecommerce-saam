# Dependencies: Order Management

**Service ID:** MS-05

## Services Consumed

### Cart and Checkout (MS-04) (async events)

#### Consumes: `OrderSubmitted`
- **Triggered by:** BR-OR-SUB-001, BR-OR-SUB-002, BR-OR-SUB-003, BR-OR-SUB-004
- **Channel:** RabbitMQ domain-events exchange; source MS-04
- **Schema:** `spec/shared/event-schemas/order-submitted-v1.yaml`
- **Action:** create the immutable order aggregate and snapshots.
- **Idempotency:** inbox uniqueness on `eventId` and `submissionId`.

### Payments (MS-06) (async events)

#### Consumes: `PaymentAuthorized.v1`, `PaymentCaptured.v1`, `PaymentFailed.v1`, `PaymentRefunded.v1`, `PaymentVoided.v1`
- **Triggered by:** BR-OR-PAY-001, BR-OR-PAY-002, BR-OR-REF-001, BR-OR-CAN-001
- **Channel:** RabbitMQ domain-events exchange; source MS-06
- **Schemas:** corresponding files under `spec/shared/event-schemas/`
- **Action:** apply only authenticated payment outcomes to the order projection and legal lifecycle.
- **Idempotency:** inbox uniqueness on `eventId`; stale/duplicate outcomes cannot reverse a later legal state.

### MS-09/MS-12 (async event)

#### Consumes: `ShipmentStatusUpdated`
- **Triggered by:** BR-OR-FUL-001
- **Channel:** RabbitMQ domain-events exchange; source MS-09/MS-12
- **Schema:** `spec/shared/event-schemas/shipment-status-updated.yaml`
- **Action:** update fulfillment state and, where legal, order delivery state.
- **Idempotency:** inbox uniqueness on `eventId` and shipment reference.
- **Status:** GAP — neither provider contract defines this payload.

### Catalog and Product (MS-02) (async event)

#### Consumes: `InventoryReservationReleased`
- **Triggered by:** BR-OR-CAN-001 and BR-OR-FAIL-001
- **Channel:** RabbitMQ domain-events exchange; source MS-02
- **Schema:** `spec/shared/event-schemas/inventory-reservation-released.yaml`
- **Action:** reconcile cancellation/compensation tracking.
- **Idempotency:** inbox uniqueness on `eventId` and reservation ID.
- **Status:** GAP — MS-02 API design names reservation operations but does not define this event.

## Events Published

The following events are declared by MS-05 `01-business-rules.md` and `03-api-design.md`; each
uses the common metadata stated in that service (`eventId`, `eventType`, `eventVersion`,
`tenantId`, `storeId`, `orderId`, `occurredAt`) and is at-least-once via an outbox.

| Event | Triggered by | Schema | Consumers |
|---|---|---|---|
| `PaymentRequested.v1` | BR-OR-PAY-001 | `payment-requested-v1.yaml` | MS-06 |
| `OrderAccepted` | BR-OR-SUB-001 | `order-accepted.yaml` | MS-04, MS-12, analytics |
| `OrderStatusChanged` | BR-OR-LIFE-001 | `order-status-changed.yaml` | MS-09/MS-12, notifications |
| `DownloadEntitlementGranted` | BR-OR-DIG-001 | `download-entitlement-granted.yaml` | MS-12 |
| `OrderCanceled` | BR-OR-CAN-001 | `order-canceled.yaml` | MS-02, MS-06, MS-12 |
| `OrderRefundApplied` | BR-OR-REF-001 | `order-refund-applied.yaml` | MS-06, MS-12 |
| `OrderPaymentFailed` | BR-OR-PAY-002 | `order-payment-failed.yaml` | MS-06, MS-12 |
| `OrderProcessingFailed` | BR-OR-FAIL-001 | `order-processing-failed.yaml` | MS-02, MS-06, MS-12 |
| `RefundReconciliationFailed` | BR-OR-REF-001 | `refund-reconciliation-failed.yaml` | MS-06, MS-12 |
| `FulfillmentRequested` | BR-OR-FUL-001 | `fulfillment-requested.yaml` | MS-09/MS-12 |
| `InvoiceGenerationRequested` | BR-OR-INV-001 | `invoice-generation-requested.yaml` | MS-12 |
| `OrderCompensationRequired` | BR-OR-FAIL-001 | `order-compensation-required.yaml` | MS-02, MS-06, MS-12 |

#### `BusinessIntegrationDeliveryRequested`
- **Triggered by:** BR-OR-INV-001 and BR-OR-DIG-001
- **Channel:** RabbitMQ integration-delivery exchange
- **Schema:** `spec/shared/event-schemas/business-integration-delivery-requested.yaml`
- **Consumers:** MS-12
- **Status:** The provider schema is authoritative and defines required `idempotencyKey`,
  `deliveryType`, `endpointCode`, and `payload`, plus common `EventMetadata`.

## Resilience

All event publication and consumption uses transactional outbox/inbox, at-least-once delivery,
event-ID deduplication, bounded exponential backoff, and dead-letter handling. Payment and
fulfillment gaps above are explicit because the producer artifacts do not yet define payloads.
