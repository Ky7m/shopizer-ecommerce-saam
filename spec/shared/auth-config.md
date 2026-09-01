# Shared Authentication and Request Context

**Status:** RECONCILED — SAAM Phase 4 Stage 1.5  
**Authority:** `spec/shared/common-schemas.yaml` and the service OpenAPI contracts

The gateway supplies tenant and store context after validating the authenticated request.
Services validate the context again against the token claims and their owned data. Protected
operations validate the OAuth2/OIDC bearer token locally for issuer, audience, expiry, scopes,
and tenant claims.

## Shared header forms

| Header | Requiredness | Meaning |
|---|---|---|
| `x-tenant-id` | Every service operation | Tenant isolation context. The value is opaque unless a service contract explicitly preserves a UUID constraint. |
| `x-store-id` | Store-scoped operations | Store context within the tenant. MS-11 retains operation-level optionality for unscoped content/configuration compatibility operations. |
| `x-correlation-id` | Internal and authenticated operations | Trace and request correlation identifier. Public operations may omit it only where the service contract explicitly says so. |
| `Authorization` | Protected operations | `Bearer <OAuth2 access token>`. Public signup, lookup, callback, and storefront operations may omit it when their contract says so. |
| `Idempotency-Key` | Retryable commands | Client-supplied replay key. Requiredness is operation-specific; the spelling and wire casing are fixed. |

The machine-readable parameter components are `TenantId`, `StoreId`, `CorrelationId`,
`Authorization`, `AuthorizationOptional`, `IdempotencyKey`, and `OptionalIdempotencyKey`.

## Security

The shared `bearerAuth` scheme describes OIDC access-token validation. It does not grant
permissions: each service operation retains its own scopes and role checks. Callback endpoints
that use provider-specific verification keep that service-specific authentication mechanism.

## Compatibility decisions

- MS-03 keeps its search-provider `count`/`start` request and offset/limit result pagination.
- MS-11 keeps the legacy content envelope (`page`, `count`, `number`, `totalPages`,
  `recordsTotal`, and `recordsFiltered`) and operation-level store/header optionality where
  required by its compatibility surface.
- Identifier constraints remain service-owned where the draft contracts distinguish opaque
  values from UUIDs.

## Idempotency and events

HTTP commands use `Idempotency-Key`. Events use `eventId` for delivery deduplication, an
event type ending in `.vN` for routing, integer `eventVersion`, and the shared metadata fields
`eventId`, `eventType`, `eventVersion`, `occurredAt`, `tenantId`, `storeId`, and
`correlationId`. Domain payloads are defined by each event schema; no generic payload is implied.
