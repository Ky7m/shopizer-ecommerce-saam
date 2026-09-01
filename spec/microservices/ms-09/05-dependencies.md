# Dependencies: Shipping

**Service ID:** MS-09

## Services Consumed

### Platform Integrations (MS-12) (async event boundary)

#### Event dependency: carrier, maps, and adapter execution
- **Triggered by:** BR-PRC-034, BR-EXT-012, BR-EXT-013, BR-EXT-018
- **Channel:** RabbitMQ integration exchange
- **Action:** MS-12 executes the selected carrier/maps adapter and returns normalized facts for the shipping quote.
- **Event:** `ShippingAdapterExecutionRequested.v1`
- **Schema:** `spec/shared/event-schemas/shipping-adapter-execution-requested-v1.yaml`
- **Payload:** `requestType`, `providerCode`, and the typed `carrierQuote` or `distance` request
  fields copied from the MS-12 adapter contract.
- **Resilience:** bounded retries, per-provider circuit breaking, and dead-letter handling are owned by MS-12.
- **Routing key:** `ShippingAdapterExecutionRequested.v1`
- **Status:** RECONCILED — the graph edge now has a named request event and concrete typed payload.

## Events Published

### `ShippingAdapterExecutionRequested.v1`
- **Triggered by:** BR-PRC-034, BR-EXT-012, BR-EXT-013, and BR-EXT-018
- **Channel:** RabbitMQ integration exchange; routing key `ShippingAdapterExecutionRequested.v1`
- **Schema:** `spec/shared/event-schemas/shipping-adapter-execution-requested-v1.yaml`
- **Consumers:** MS-12
- **Status:** RECONCILED

### `ShippingQuoteCalculated.v1`
- **Triggered by:** BR-PRC-028 and BR-UI-008
- **Schema:** `spec/shared/event-schemas/shipping-quote-calculated-v1.yaml`
- **Consumers:** MS-04, MS-05, MS-08
- **Status:** RECONCILED — approved event compilation promotes the target quote boundary and
  preserves the numeric money fields already fixed by the MS-09 contract.

### `ShippingConfigurationChanged.v1`
- **Triggered by:** BR-PRC-022, BR-PRC-028, and BR-UI-008
- **Schema:** `spec/shared/event-schemas/shipping-configuration-changed-v1.yaml`
- **Consumers:** MS-11, MS-12, MS-04
- **Status:** RECONCILED — configuration type, identifier, change type, and service-owned
  configuration projection are defined in the shared schema.

## Events Consumed

No named inbound event is defined for MS-09. The shipping summary's product, customer, store,
and configuration items are request/context data, not event contracts.

## Reconciliation note

MS-09 owns shipping decisions and publishes the typed adapter request; MS-12 owns adapter
execution and provider resilience. The event is an internal request boundary, not a replacement
for the external MS-12 provider operations.
