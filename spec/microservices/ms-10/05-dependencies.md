# Dependencies: Merchant and Store Administration

**Service ID:** MS-10

## Services Consumed

### Customer and Identity (MS-01) (sync REST)

#### Call: `getCurrentAdministrator`
- **Triggered by:** BR-MER-011, BR-MSA-AUTH-001, and BR-UI-007
- **Method:** GET
- **Path:** `/users/me`
- **Headers:** `x-tenant-id`, `x-store-id`, `x-correlation-id` required by the MS-01 contract; `Authorization` is required by the authenticated operation even though the provider's global extension marks it optional.
- **Request body:** none
- **Success response:** `200`, `#/components/schemas/Administrator`
- **Error handling:** `401` reject the administrative operation; `503` retry then fail closed. No identity fallback is permitted.
- **Resilience:** 10s timeout; 3 retries at 2s/4s/8s only for transport failure; circuit opens after 5 failures and half-opens after 30s.

## Events Published

The completion summary names store-created, store-updated, and store-deleted events as
**candidates**, while the approved sequence diagram names `StoreConfigured`. None has a
publisher contract or payload in the MS-10 contract.

| Event | Triggered by | Schema | Status |
|---|---|---|---|
| `StoreCreated` | BR-MSA-VAL-003 | `store-created.yaml` | GAP — candidate only |
| `StoreConfigured` | BR-MER-005 and BR-MSA-VAL-003 | `store-configured.yaml` | GAP — sequence-only name; conflicts with candidate naming |
| `StoreUpdated` | BR-MER-005 | `store-updated.yaml` | GAP — candidate only |
| `StoreDeleted` | BR-MER-006 and BR-MER-009 | `store-deleted.yaml` | GAP — candidate only |

## External/reference dependencies

Rules mention country, zone, currency, language, measurement-unit, and file storage boundaries.
No provider contracts or graph `CALLS` edges for those references exist in the repository; they
remain explicit preservation/spec gaps. Branding binary storage is not silently mapped to an
MS-11 endpoint.

## Resilience

The MS-01 identity call uses the REST policy above. Event publication, if approved, must use the
transactional outbox and at-least-once delivery; exact event routing and payloads require human
reconciliation.

