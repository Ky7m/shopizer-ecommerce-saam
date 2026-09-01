# Shared Event Schemas

All events use RabbitMQ. Every schema composes the shared `EventMetadata` form from
`spec/shared/common-schemas.yaml`: `eventId`, `eventType`, `eventVersion`, `occurredAt`,
`tenantId`, `storeId`, and `correlationId`. Routing names use the `.vN` suffix where the
published name is versioned; the integer `eventVersion` is present in every event.

`OK` means the publisher, concrete payload, and named consumer binding are present.
`GAP` means the payload is concrete but repository evidence is still missing for the exact
publisher/consumer binding stated in the row. `DEAD/UNCONSUMED` means the artifact is retained
for traceability but is intentionally not consumed.

| Event | Publisher | Consumers | Schema | Coverage |
|---|---|---|---|---|
| `CustomerRegistered` | MS-01 | none confirmed | customer-registered.yaml | DEAD/UNCONSUMED — no consumer contract |
| `ProductChanged.v1` | MS-02 | MS-03, MS-04, MS-07 | product-changed-v1.yaml | OK |
| `CategoryChanged.v1` | MS-02 | MS-03 | category-changed-v1.yaml | OK |
| `AvailabilityChanged.v1` | MS-02 | MS-04, MS-05 | availability-changed-v1.yaml | OK |
| `MediaChanged.v1` | MS-02 | MS-03, MS-12 | media-changed-v1.yaml | OK |
| `InventoryReservationChanged.v1` | MS-02 | MS-04, MS-05 | inventory-reservation-changed-v1.yaml | OK |
| `InventoryReservationReleased.v1` | MS-02 | MS-05 | inventory-reservation-released.yaml | OK |
| `SearchIndexingFailed.v1` | MS-03 | none confirmed | search-indexing-failed-v1.yaml | DEAD/UNCONSUMED — operational |
| `SearchRebuildCompleted.v1` | MS-03 | none confirmed | search-rebuild-completed-v1.yaml | DEAD/UNCONSUMED — operational |
| `OrderSubmitted.v1` | MS-04 | MS-05, MS-06 | order-submitted-v1.yaml | OK |
| `OrderAccepted` | MS-05 | none confirmed | order-accepted.yaml | GAP — named consumers have no consumer artifacts |
| `OrderStatusChanged` | MS-05 | MS-09/MS-12 | order-status-changed.yaml | GAP — provider payload is concrete; consumer transport binding is absent |
| `DownloadEntitlementGranted` | MS-05 | MS-12 | download-entitlement-granted.yaml | GAP — MS-12 consumer handler is not defined |
| `OrderCanceled` | MS-05 | MS-02, MS-06, MS-12 | order-canceled.yaml | GAP — named consumers have no inbound contracts |
| `OrderRefundApplied` | MS-05 | MS-06, MS-12 | order-refund-applied.yaml | GAP — named consumers have no inbound contracts |
| `OrderPaymentFailed` | MS-05 | MS-06, MS-12 | order-payment-failed.yaml | GAP — named consumers have no inbound contracts |
| `OrderProcessingFailed` | MS-05 | MS-02, MS-06, MS-12 | order-processing-failed.yaml | GAP — named consumers have no inbound contracts |
| `RefundReconciliationFailed` | MS-05 | MS-06, MS-12 | refund-reconciliation-failed.yaml | GAP — named consumers have no inbound contracts |
| `FulfillmentRequested` | MS-05 | MS-09/MS-12 | fulfillment-requested.yaml | GAP — named consumers have no inbound contracts |
| `InvoiceGenerationRequested` | MS-05 | MS-12 | invoice-generation-requested.yaml | GAP — named consumer has no inbound contract |
| `OrderCompensationRequired` | MS-05 | MS-02, MS-06, MS-12 | order-compensation-required.yaml | GAP — named consumers have no inbound contracts |
| `BusinessIntegrationDeliveryRequested` | MS-05 | MS-12 | business-integration-delivery-requested.yaml | OK |
| `PaymentRequested.v1` | MS-05 | MS-06 | payment-requested-v1.yaml | OK |
| `PaymentAuthorizationStarted` | MS-06 | none confirmed | payment-authorization-started.yaml | DEAD/UNCONSUMED — sequence-only |
| `PaymentAuthorized.v1` | MS-06 | MS-04, MS-05 | payment-authorized-v1.yaml | OK |
| `PaymentCaptured.v1` | MS-06 | MS-04, MS-05 | payment-captured-v1.yaml | OK |
| `PaymentRefunded.v1` | MS-06 | MS-04, MS-05 | payment-refunded-v1.yaml | OK |
| `PaymentFailed.v1` | MS-06 | MS-04, MS-05, operations | payment-failed-v1.yaml | OK |
| `PaymentVoided.v1` | MS-06 | MS-04, MS-05 | payment-voided-v1.yaml | OK |
| `PaymentReconciliationRequired.v1` | MS-06 | MS-05, MS-12 | payment-reconciliation-required-v1.yaml | GAP — MS-12 consumer binding is absent |
| `PaymentRefundFailed` | MS-06? | none confirmed | payment-refund-failed.yaml | GAP — rules-only variant needs publication policy and consumer |
| `PaymentPendingManualSettlement` | MS-06? | none confirmed | payment-pending-manual-settlement.yaml | GAP — rules-only variant needs publication policy and consumer |
| `ConfigurationReferenceChanged` | MS-11 | MS-06, MS-12 | configuration-reference-changed.yaml | OK |
| `ProviderConfigurationChanged.v1` | MS-11 alias | none | provider-configuration-changed-v1.yaml | DEAD/UNCONSUMED — retired in favor of ConfigurationReferenceChanged |
| `PriceChanged.v1` | MS-07 | MS-04, MS-05, optionally MS-03 | price-changed-v1.yaml | GAP — target publisher payload is concrete; consumer handlers are absent |
| `PromotionChanged.v1` | MS-07 | MS-04, MS-05, optionally MS-03 | promotion-changed-v1.yaml | GAP — target publisher payload is concrete; consumer handlers are absent |
| `ShippingAdapterExecutionRequested.v1` | MS-09 | MS-12 | shipping-adapter-execution-requested-v1.yaml | OK |
| `ShippingQuoteCalculated.v1` | MS-09 | MS-04, MS-05, MS-08 | shipping-quote-calculated-v1.yaml | GAP — named consumer handlers are absent |
| `ShippingConfigurationChanged.v1` | MS-09 | MS-11, MS-12, MS-04 | shipping-configuration-changed-v1.yaml | GAP — named consumer handlers are absent |
| `StoreCreated` | MS-10 | none confirmed | store-created.yaml | GAP — canonical publisher exists; consumer is not confirmed |
| `StoreConfigured` | MS-10 sequence alias | none | store-configured.yaml | DEAD/UNCONSUMED — retired in favor of StoreCreated |
| `StoreUpdated` | MS-10 candidate | none confirmed | store-updated.yaml | GAP — no approved publisher or consumer |
| `StoreDeleted` | MS-10 candidate | none confirmed | store-deleted.yaml | GAP — no approved publisher or consumer |
| `ContentPublished.v1` | MS-11 | MS-03 | content-published-v1.yaml | OK |
| `IntegrationDeliveryQueued` | MS-12 | internal worker | integration-delivery-queued.yaml | DEAD/UNCONSUMED — handled by the local durable worker |
| `IntegrationDeliveryDeadLettered` | MS-12 | operations | integration-delivery-dead-lettered.yaml | GAP — operator consumer is not a service contract |
| `IntegrationDeliveryReplayRequested` | MS-12/operator | MS-12 | integration-delivery-replay-requested.yaml | OK |
| `ShipmentStatusUpdated` | MS-09/MS-12? | MS-05 | shipment-status-updated.yaml | GAP — publisher and transport mapping are not evidenced |

## Remaining gaps

The remaining `GAP` rows are not generic payload placeholders: each has a typed schema and the
exact missing evidence is stated in this table. Closing them requires the named consumer
handler/provider transport contracts (or an architecture decision to remove the unconfirmed
consumer). Dependency-version pins remain separately blocked in
`spec/shared/09-dependency-versions.md`.
