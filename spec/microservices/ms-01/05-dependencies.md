# Dependencies: Customer and Identity

**Service ID:** MS-01

## Graph CALLS edges

This service has no outgoing service-to-service `CALLS` edge in the Phase 2 graph. The OIDC
provider and password-reset email sender are external boundaries mentioned by the rules, but
neither has a provider contract in this repository.

## Events Published

### `CustomerRegistered`
- **Triggered by:** BR-CUS-002 (registration persists a customer and starts authentication)
- **Channel:** RabbitMQ domain-events exchange; routing key `CustomerRegistered`
- **Schema:** `spec/shared/event-schemas/customer-registered.yaml`
- **Payload:** `customerId`, `loginName`, `emailAddress`, and `status`, plus shared event metadata;
  the fields are copied from the MS-01 `Customer` contract.
- **Status:** DEAD/UNCONSUMED — no consumer is confirmed in the approved service composition.
- **Guarantees:** transactional outbox, at-least-once delivery, `eventId` deduplication by consumers
- **Ordering:** by customer aggregate

## External Dependencies

### OIDC provider
- **Triggered by:** BR-CUS-NN-005 and BR-CUS-NN-009
- **Contract:** no repository provider contract; endpoint, request/response shape, status mapping,
  timeout, and retry policy are unresolved and must be supplied by the OIDC deployment owner.

### Password-reset email delivery
- **Triggered by:** BR-CUS-NN-001 and BR-CUS-NN-017
- **Contract:** no repository provider contract. Delivery is asynchronous and must use the approved
  MS-12 integration event boundary; the exact event and payload are unresolved.

## Resilience

Internal event publication uses the confirmed RabbitMQ outbox/inbox pattern. Delivery is
at-least-once with bounded exponential backoff and dead-letter handling. External OIDC and email
parameters remain a preservation/spec GAP because no provider contract exists.
