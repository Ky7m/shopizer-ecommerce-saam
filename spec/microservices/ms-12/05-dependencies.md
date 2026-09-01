# Dependencies: Platform Integrations

**Service ID:** MS-12

## Services Consumed

### Order Management (MS-05) (async events)

#### Consumes: `BusinessIntegrationDeliveryRequested`
- **Triggered by:** BR-INT-MS12-016, BR-INT-MS12-017, and the MS-05 invoice/entitlement boundaries
- **Channel:** RabbitMQ integration-delivery exchange
- **Schema:** `#/components/schemas/BusinessIntegrationDeliveryRequested` in the MS-12 contract and `spec/shared/event-schemas/business-integration-delivery-requested.yaml`
- **Required shape:** common `EventMetadata` (`eventId`, `eventType`, `occurredAt`, `tenantId`, `storeId`, `correlationId`) plus required `idempotencyKey`, `deliveryType`, `endpointCode`, and `payload`; optional `businessReference`.
- **Action:** resolve the configured endpoint and create a durable integration operation/attempt.
- **Idempotency:** unique `idempotencyKey`; inbox/event ID deduplication.

### Content and Configuration (MS-11) (async events)

#### Consumes: `ConfigurationReferenceChanged`
- **Triggered by:** BR-INT-MS12-001 and BR-INT-MS12-003
- **Channel:** RabbitMQ configuration exchange
- **Schema:** `#/components/schemas/ConfigurationReferenceChanged` and `configuration-reference-changed.yaml`
- **Required shape:** `EventMetadata` plus required `moduleType`, `code`, `environment`, `configurationRef`, and `version`.
- **Action:** refresh the safe adapter projection; existing operation configuration remains pinned.
- **Idempotency:** configuration version guard and inbox event ID.
- **Status:** GAP — MS-11 does not publish the event in its contract.

### MS-12 (async event)

#### Consumes: `IntegrationDeliveryReplayRequested`
- **Triggered by:** BR-INT-MS12-023
- **Channel:** RabbitMQ integration-delivery exchange
- **Schema:** `#/components/schemas/IntegrationDeliveryReplayRequested` and `integration-delivery-replay-requested.yaml`
- **Required shape:** `EventMetadata` plus `originalAttemptId` and `reason`.
- **Action:** create a new attempt for the original operation after validating replay policy.
- **Idempotency:** replay command key and original attempt state.

## Events Published

### `IntegrationDeliveryQueued`
- **Triggered by:** BR-INT-MS12-021 and BR-INT-MS12-023
- **Channel:** `integration-delivery`
- **Schema:** `#/components/schemas/IntegrationDeliveryQueued` and `integration-delivery-queued.yaml`
- **Required shape:** `EventMetadata` plus `operationId`, `attemptId`, `endpointId`, `idempotencyKey`, `availableAt`, and `requestPayload`.
- **Guarantees:** at-least-once
- **Ordering:** by operation ID

### `IntegrationDeliveryDeadLettered`
- **Triggered by:** BR-INT-MS12-022 and BR-INT-MS12-023
- **Channel:** `integration-delivery-dead-letter`
- **Schema:** `#/components/schemas/IntegrationDeliveryDeadLettered` and `integration-delivery-dead-lettered.yaml`
- **Required shape:** `EventMetadata` plus `operationId`, `attemptId`, `endpointId`, `idempotencyKey`, `attemptNumber`, and `deadLetteredAt`; optional `messageId`.
- **Guarantees:** at-least-once publication with operator-visible reason in the durable attempt record.

## External dependencies

MS-12's exact external-facing provider operations are defined in its own contract:
`POST /carrier-quotes/ups`, `POST /carrier-quotes/usps`, `POST /maps/distance`, `GET /geolocation/ip`,
the `/files` family, and `POST /emails`. Those are external provider adapter calls, not graph
service dependencies. Their exact request/response schemas and statuses remain authoritative in
MS-12 `04-api-contract.yaml`.

## Resilience

Durable operations use the MS-12 contract's operation/attempt state, idempotency key, replay, and
dead-letter model. Provider workers use bounded retries and per-provider circuit breakers. A
completed attempt is never resubmitted as the same attempt.

