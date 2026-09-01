# Dependencies: Shipping

**Service ID:** MS-09

## Services Consumed

### Platform Integrations (MS-12) (async event boundary)

#### Event dependency: carrier, maps, and adapter execution
- **Triggered by:** BR-PRC-034, BR-EXT-012, BR-EXT-013, BR-EXT-018
- **Channel:** RabbitMQ integration exchange
- **Action:** MS-12 executes the selected carrier/maps adapter and returns normalized facts for the shipping quote.
- **Resilience:** bounded retries, per-provider circuit breaking, and dead-letter handling are owned by MS-12.
- **Status:** GAP — the graph has an MS-09 -> MS-12 edge with protocol `Event`, but no event name,
  routing key, request schema, response schema, or BR-specific event contract is present in the
  approved artifacts. The MS-12 contract only exposes synchronous external-facing adapter
  operations (`/carrier-quotes/ups`, `/carrier-quotes/usps`, `/maps/distance`), not an internal
  MS-09 event contract. No unnamed provider event has been invented.

## Events Published

### `ShippingQuoteCalculated.v1` (proposed, unconfirmed)
- **Triggered by:** BR-PRC-028 and BR-UI-008
- **Schema:** `spec/shared/event-schemas/shipping-quote-calculated-v1.yaml`
- **Consumers:** MS-04, MS-05, MS-08
- **Status:** GAP — MS-09's summary explicitly calls this a recommended target event, not an
  approved publisher contract.

### `ShippingConfigurationChanged.v1` (proposed, unconfirmed)
- **Triggered by:** BR-PRC-022, BR-PRC-028, and BR-UI-008
- **Schema:** `spec/shared/event-schemas/shipping-configuration-changed-v1.yaml`
- **Consumers:** MS-11, MS-12, MS-04
- **Status:** GAP — recommended target event only; no approved payload.

## Events Consumed

No named inbound event is defined for MS-09. The shipping summary's product, customer, store,
and configuration items are request/context data, not event contracts.

## Reconciliation note

The rules correctly identify MS-12 ownership for carrier and Maps calls. The graph protocol
classification and the MS-12 REST contract are currently inconsistent; this is a real
cross-service contract gap requiring a human architecture decision.

