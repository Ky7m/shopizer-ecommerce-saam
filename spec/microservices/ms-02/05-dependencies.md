# Dependencies: Catalog and Product

**Service ID:** MS-02

## Services Consumed

### Merchant and Store Administration (MS-10) (sync REST)

#### Call: `getStore`
- **Triggered by:** BR-CAT-028 (catalog mutations require privileged groups) and the service boundary rule at `01-business-rules.md:9`
- **Method:** GET
- **Path:** `/stores/{storeCode}` (MS-10 `04-api-contract.yaml`, `getStore`)
- **Path parameter:** `storeCode` is the store identifier from the catalog request context.
- **Headers:** `x-tenant-id`, `x-store-id`, and `x-correlation-id` are the shared request-context
  parameters. MS-10's provider contract now exposes the shared tenant/correlation components;
  `storeCode` remains the provider path identifier.
- **Request body:** none
- **Success response:** `200`, `#/components/schemas/Store`
- **Response shape:** `Store` with required `id`, `tenantId`, `code`, `name`, `emailAddress`, `phone`, `city`, `postalCode`, `countryCode`, `defaultLanguageCode`, `currencyCode`, `dimensionUnit`, `weightUnit`, `retailer`, and `status`.
- **Error handling:**
  | Status | Meaning | Action |
  |---|---|---|
  | 404 | Store is not present | Fail closed; reject the catalog operation as out of scope |
  | 503 | Store-context dependency unavailable | Do not mutate catalog data; retry through the REST policy |
- **Resilience:** 10s timeout; 3 retries at 2s/4s/8s; circuit opens after 5 failures and half-opens after 30s; no stale-store fallback.

## Events Published

### `ProductChanged.v1`
- **Triggered by:** BR-CAT-001, BR-CAT-002, BR-CAT-017, BR-CAT-019, BR-CAT-036
- **Channel:** RabbitMQ domain-events exchange; routing key `ProductChanged.v1`
- **Schema:** `spec/shared/event-schemas/product-changed-v1.yaml`
- **Guarantees:** transactional outbox, at-least-once delivery
- **Ordering:** by product aggregate and `aggregateVersion`

### `CategoryChanged.v1`
- **Triggered by:** BR-CAT-006, BR-CAT-007, BR-UI-006
- **Channel:** RabbitMQ domain-events exchange; routing key `CategoryChanged.v1`
- **Schema:** `spec/shared/event-schemas/category-changed-v1.yaml`
- **Guarantees:** transactional outbox, at-least-once delivery
- **Ordering:** by category aggregate and `aggregateVersion`

### `AvailabilityChanged.v1`
- **Triggered by:** BR-ORD-012, BR-CAT-037, BR-CAT-039
- **Channel:** RabbitMQ domain-events exchange; routing key `AvailabilityChanged.v1`
- **Schema:** `spec/shared/event-schemas/availability-changed-v1.yaml`
- **Guarantees:** transactional outbox, at-least-once delivery
- **Ordering:** by product/availability aggregate

### `MediaChanged.v1`
- **Triggered by:** BR-EXT-019, BR-CAT-017, BR-CAT-035
- **Channel:** RabbitMQ domain-events exchange; routing key `MediaChanged.v1`
- **Schema:** `spec/shared/event-schemas/media-changed-v1.yaml`
- **Guarantees:** transactional outbox, at-least-once delivery
- **Ordering:** by product aggregate

### `InventoryReservationChanged.v1`
- **Triggered by:** BR-ORD-012, BR-CAT-037, BR-CAT-039
- **Channel:** RabbitMQ domain-events exchange; routing key `InventoryReservationChanged.v1`
- **Schema:** `spec/shared/event-schemas/inventory-reservation-changed-v1.yaml`
- **Guarantees:** transactional outbox, at-least-once delivery
- **Ordering:** by reservation aggregate

### `InventoryReservationReleased.v1`
- **Triggered by:** BR-CAT-039 and the reservation release operation
- **Channel:** RabbitMQ domain-events exchange; routing key `InventoryReservationReleased.v1`
- **Schema:** `spec/shared/event-schemas/inventory-reservation-released.yaml`
- **Consumers:** MS-05
- **Guarantees:** transactional outbox, at-least-once delivery

## Events Consumed

`OrderCanceled` and `OrderCompensationRequired` remain unbound to an MS-02 consumer contract.
The MS-05 event table names MS-02, but no MS-02 rule or API artifact defines an inbound handler;
this is retained as a GAP rather than inventing one.

## Integration reconciliation

The rules' integration dimensions identify MS-10 scope validation and downstream product,
availability, and event boundaries. The graph contributes one REST edge, resolved above. The
MS-05 cancellation/compensation references require a graph edge and payload contract before they
can be treated as implemented dependencies.
