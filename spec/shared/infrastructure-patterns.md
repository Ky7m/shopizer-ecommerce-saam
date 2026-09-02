# Infrastructure Patterns — ASP.NET Core / .NET 10

This document is the single source of truth for cross-cutting HTTP, runtime, tenancy,
messaging, and observability behavior across all services.

## Health endpoints

- `/health` returns a simple JSON `200` response.
- `/health/alive` is the liveness endpoint and does not require dependencies.
- `/health/ready` is the readiness endpoint and checks PostgreSQL and RabbitMQ connectivity.
- Response format: `{ "status": "Healthy|Degraded|Unhealthy" }`.

## Error handling middleware

- Validation errors return HTTP 422 with `{ "errors": [{ "field": "...", "message": "..." }] }`.
- Authentication failures return HTTP 401 with `{ "error": "Unauthorized" }`.
- Authorization failures return HTTP 403 with `{ "error": "Forbidden" }`.
- Not found returns HTTP 404 with `{ "error": "Resource not found" }`.
- Unhandled exceptions return HTTP 500 with `{ "error": "Internal server error", "correlationId": "..." }`.
- Services never return stack traces or framework-default HTML error pages.
- Service-specific error codes remain in the shared error envelope defined by
  `spec/shared/common-schemas.yaml`.

## Tenant extraction

- Header name: `x-tenant-id`.
- Format: UUID; malformed values are rejected with HTTP 400.
- Store-scoped operations may also require `x-store-id`; path identifiers remain authoritative
  only after ownership validation.
- Tenant context is a request-lifetime scoped DI service available to controllers, application
  services, and repositories.
- Every tenant-owned query applies the tenant filter automatically; cross-tenant access is
  rejected rather than returning an empty success.

## Request and response conventions

- All responses are JSON with camelCase field names matching `04-api-contract.yaml`.
- Correlation header: `x-correlation-id`; generate one when absent and return it in the response
  header and unhandled-error body.
- Idempotent commands accept `Idempotency-Key` where the service contract declares it.
- Pagination format: `{ "items": [...], "total": N, "page": N, "pageSize": N }`.
- Valid empty collections return HTTP 200 with an empty `items` array and `total: 0`, not 404.
- Monetary values use decimal strings and explicit currency metadata where required by the
  service contract.

## Messaging and integration runtime

- RabbitMQ events use `eventId`, `eventType`, `eventVersion`, `occurredAt`, `tenantId`,
  `storeId` when applicable, `correlationId`, and a typed payload.
- Services use a transactional outbox when a database mutation and event publication must be
  coordinated.
- Consumers use an inbox/idempotency store and acknowledge only after successful processing.
- Retries use bounded exponential backoff; poison messages move to a dead-letter exchange with
  an operator-visible reason and correlation ID.
- Consumers tolerate replayed events and deduplicate by event ID or contract-specific key.

## Startup and initialization

- Database migrations run as a controlled startup/release step; production uses forward-only
  migrations.
- Test mode applies deterministic seed data and deterministic provider/message adapters.
- Messaging connects after the database is ready; `/health/ready` waits for both dependencies.
- Graceful shutdown drains in-flight requests, stops consumers, flushes telemetry, and exits.

## Logging and observability

- Structured logs use JSON in production and readable console output in development.
- Correlation ID, causation ID, tenant ID, service, operation, and outcome are included where
  available.
- Sensitive credentials, tokens, payment data, and secrets are redacted at the logging
  boundary.
- OpenTelemetry traces cover HTTP requests, database queries, RabbitMQ publish/consume
  operations, and external provider calls.
- Metrics include request rate/latency/errors, queue depth, dead-letter count, outbox age,
  projection lag, checkout conversion, payment failures, and quote latency.
