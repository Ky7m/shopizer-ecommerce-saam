# Shared Event Schemas

All events use RabbitMQ and the architecture-level metadata defined in
`modernization/modernized-architecture.md` where the source artifact defines it:
`eventId`, `eventType`, `eventVersion`, `occurredAt`, `tenantId`, and `correlationId`.
The schemas below are compiled from the service rules, API designs, contracts, and approved
sequence/composition artifacts. `GAP` means that the event name or consumer is known but the
publisher payload/routing contract is not yet defined; no generic payload has been invented.

| Event | Publisher | Consumers | Coverage |
|---|---|---|---|
| `CustomerRegistered` | MS-01 | none confirmed | DEAD/UNCONSUMED — approved capability has no consumer contract |
| `ProductChanged.v1` | MS-02 | MS-03, MS-04, MS-07 | OK |
| `CategoryChanged.v1` | MS-02 | MS-03 | OK |
| `AvailabilityChanged.v1` | MS-02 | MS-04, MS-05 | OK |
| `MediaChanged.v1` | MS-02 | MS-03, MS-12 | OK |
| `InventoryReservationChanged.v1` | MS-02 | MS-04, MS-05 | OK |
| `SearchIndexingFailed.v1` | MS-03 | none confirmed | DEAD/UNCONSUMED — operational event |
| `SearchRebuildCompleted.v1` | MS-03 | none confirmed | DEAD/UNCONSUMED — operational event |
| `OrderSubmitted` | MS-04 | MS-05, MS-06 | OK |
| `OrderAccepted` | MS-05 | MS-04, MS-12 | GAP — publisher payload incomplete |
| `OrderStatusChanged` | MS-05 | MS-09/MS-12 | GAP — publisher payload incomplete |
| `DownloadEntitlementGranted` | MS-05 | MS-12 | GAP — publisher payload incomplete |
| `OrderCanceled` | MS-05 | MS-02, MS-06, MS-12 | GAP — MS-02/MS-06 consumer contracts incomplete |
| `OrderRefundApplied` | MS-05 | MS-06, MS-12 | GAP — publisher payload incomplete |
| `OrderPaymentFailed` | MS-05 | MS-06, MS-12 | GAP — rules-only publisher event |
| `OrderProcessingFailed` | MS-05 | MS-02, MS-06, MS-12 | GAP — rules-only publisher event |
| `RefundReconciliationFailed` | MS-05 | MS-06, MS-12 | GAP — rules-only publisher event |
| `FulfillmentRequested` | MS-05 | MS-09/MS-12 | GAP — publisher payload incomplete |
| `InvoiceGenerationRequested` | MS-05 | MS-12 | GAP — publisher payload incomplete |
| `OrderCompensationRequired` | MS-05 | MS-02, MS-06, MS-12 | GAP — consumer payload contracts incomplete |
| `BusinessIntegrationDeliveryRequested` | MS-05 | MS-12 | OK — MS-12 contract defines payload |
| `PaymentRequested.v1` | MS-05 | MS-06 | GAP — publisher payload incomplete |
| `PaymentAuthorizationStarted` | MS-06 | none confirmed | DEAD/UNCONSUMED — sequence-only status event |
| `PaymentAuthorized.v1` | MS-06 | MS-05 | GAP — publisher payload incomplete |
| `PaymentCaptured.v1` | MS-06 | MS-05 | GAP — publisher payload incomplete |
| `PaymentRefunded.v1` | MS-06 | MS-05 | GAP — publisher payload incomplete |
| `PaymentFailed.v1` | MS-06 | MS-05, operations | GAP — publisher payload incomplete |
| `PaymentVoided.v1` | MS-06 | MS-05 | GAP — publisher payload incomplete |
| `PaymentReconciliationRequired.v1` | MS-06 | MS-05, MS-12 | GAP — publisher payload incomplete |
| `ProviderConfigurationChanged.v1` | MS-11? | MS-06 | GAP — MS-11 does not define this event |
| `PriceChanged.v1` | MS-07 | MS-04, MS-05, optionally MS-03 | GAP — publisher payload incomplete |
| `PromotionChanged.v1` | MS-07 | MS-04, MS-05, optionally MS-03 | GAP — publisher payload incomplete |
| `ShippingQuoteCalculated.v1` | MS-09 | MS-04, MS-05, MS-08 | GAP/PROPOSED — recommended only |
| `ShippingConfigurationChanged.v1` | MS-09 | MS-11, MS-12, MS-04 | GAP/PROPOSED — recommended only |
| `StoreCreated` | MS-10 | none confirmed | GAP — candidate event |
| `StoreConfigured` | MS-10 | MS-11, MS-02 | GAP — sequence-only name conflicts with candidate naming |
| `StoreUpdated` | MS-10 | none confirmed | GAP — candidate event |
| `StoreDeleted` | MS-10 | none confirmed | GAP — candidate event |
| `ContentPublished` | MS-11 | MS-03 | GAP — no publisher BR side effect or payload |
| `ConfigurationReferenceChanged` | MS-11 | MS-12 | GAP — required by MS-12 contract, absent from MS-11 contract |
| `IntegrationDeliveryQueued` | MS-12 | none confirmed | DEAD/UNCONSUMED — operational event |
| `IntegrationDeliveryDeadLettered` | MS-12 | operations | OK — schema is in MS-12 contract |
| `IntegrationDeliveryReplayRequested` | operator/MS-12 | MS-12 | OK — schema is in MS-12 contract |
| `PaymentRefundFailed` | MS-06? | none confirmed | GAP — rules-only variant, not API event catalog |
| `PaymentPendingManualSettlement` | MS-06? | none confirmed | GAP — rules-only variant, not API event catalog |
| `ShipmentStatusUpdated` | MS-09/MS-12 | MS-05 | GAP — no provider payload contract |
| `InventoryReservationReleased` | MS-02 | MS-05 | GAP — consumer is named but event payload is absent |

`MS-09 -> MS-12` is a graph event edge without a named event. It is not represented by a
fabricated schema; the missing internal request/response event is explicitly recorded as a GAP
in `spec/microservices/ms-09/05-dependencies.md`.
