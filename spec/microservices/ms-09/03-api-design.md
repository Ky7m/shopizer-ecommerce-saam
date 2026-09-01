# Shipping Specification — API Design

**Version**: 1.0  
**Date**: 2026-09-01  
**Service ID**: MS-09  
**Contract**: `04-api-contract.yaml`  
**Operation count**: 16

## API Conventions

- Base path: `/api/v1`
- JSON fields: camelCase
- URL paths: kebab-case
- Store and tenant scope are required on every request.
- Public quote operations consume cart/customer/address context from MS-01/MS-04.
- Administrative operations require bearer authorization and shipping administration roles.
- HTTP/XML carrier calls and Google Maps calls are not exposed by MS-09; they are internal
  MS-12 adapter operations.
- Configuration endpoints are compatibility façades over MS-11-owned configuration projections.
- MS-09 never changes order state or writes product, cart, merchant, or module-configuration
  tables.

## Endpoint Catalogue

| # | Method | Endpoint | Operation ID | Purpose | Success | Primary rules |
|---:|---|---|---|---|---|---|
| 1 | GET | `/auth/cart/{cart}/shipping` | `getAuthenticatedCartShipping` | Calculate shipping for an authenticated customer cart | 200 | BR-PRC-022..028 |
| 2 | POST | `/cart/{cart}/shipping` | `calculateCartShipping` | Calculate shipping for an anonymous or checkout cart address | 200 | BR-PRC-022..036 |
| 3 | GET | `/private/configurations/shipping` | `getShippingConfiguration` | Read effective store shipping configuration | 200 | BR-PRC-029, BR-PRC-026, BR-UI-008 |
| 4 | GET | `/private/modules/shipping` | `listShippingModules` | List region-eligible provider modules and active state | 200 | BR-PRC-024 |
| 5 | GET | `/private/modules/shipping/{module}` | `getShippingModule` | Read one provider configuration projection | 200 | BR-PRC-024, BR-EXT-012 |
| 6 | POST | `/private/modules/shipping` | `configureShippingModule` | Validate and save one provider configuration projection | 200 | BR-PRC-024 |
| 7 | GET | `/private/shipping/origin` | `getShippingOrigin` | Read configured shipping origin | 200 | BR-PRC-022 |
| 8 | POST | `/private/shipping/origin` | `saveShippingOrigin` | Create or update configured origin | 200 | BR-PRC-022 |
| 9 | GET | `/private/shipping/packages` | `listShippingPackages` | List configured package definitions | 200 | BR-PRC-029..032 |
| 10 | GET | `/private/shipping/package/{package}` | `getShippingPackage` | Read one package definition | 200 | BR-PRC-029..032 |
| 11 | POST | `/private/shipping/package` | `createShippingPackage` | Add a package definition to configuration | 200 | BR-PRC-029..032 |
| 12 | PUT | `/private/shipping/package/{package}` | `updateShippingPackage` | Replace a package definition | 200 | BR-PRC-029..032 |
| 13 | DELETE | `/private/shipping/package/{package}` | `deleteShippingPackage` | Delete a package definition | 200 | BR-PRC-029..032 |
| 14 | GET | `/private/shipping/expedition` | `getExpeditionConfiguration` | Read international/tax/destination configuration | 200 | BR-PRC-023 |
| 15 | POST | `/private/shipping/expedition` | `saveExpeditionConfiguration` | Save national/international and supported-country configuration | 200 | BR-PRC-023 |
| 16 | GET | `/shipping/country` | `listShippingCountries` | Return translated eligible destination countries | 200 | BR-PRC-023 |

## Request and Response Behavior

### Quote calculation

The POST request requires a cart identifier and address with `countryCode` and
`postalCode`. The service:

1. Resolves store and customer/cart context.
2. Resolves the effective origin.
3. Validates destination eligibility.
4. Determines whether shipment is required.
5. Calculates merchandise total and package facts.
6. Applies the free-shipping threshold.
7. Runs preprocessors.
8. Selects or replaces the provider.
9. Invokes the normalized MS-12 adapter or in-process policy module.
10. Applies option selection.
11. Runs postprocessors.
12. Persists final option snapshots where applicable.
13. Returns a readable shipping summary.

### Administrative authorization

The following roles are required for administrative operations:

- `SUPERADMIN`
- `ADMIN`
- `SHIPPING`
- `ADMIN_RETAIL`

### Configuration ownership

The package, module, and expedition endpoints are retained for API compatibility. Their target
writes must be routed to MS-11 configuration ownership. The resulting normalized policy
projection is consumed by MS-09.

## Error Catalogue

| Code | HTTP status | Meaning |
|---|---:|---|
| `INVALID_REQUEST` | 400 | Required body or field is malformed |
| `UNAUTHORIZED` | 401 | Authentication is missing or invalid |
| `FORBIDDEN` | 403 | Caller lacks shipping administration permission |
| `CART_NOT_FOUND` | 404 | Cart does not exist or is not owned by the authenticated customer |
| `PACKAGE_NOT_FOUND` | 404 | Package code is not configured |
| `MODULE_NOT_FOUND` | 404 | Provider module is not available for the store |
| `RULE_CODE_EXISTS` | 409 | Shipping configuration code already exists |
| `DESTINATION_NOT_SUPPORTED` | 422 | Destination fails national or international policy |
| `NO_SHIPPING_MODULE_CONFIGURED` | 422 | No eligible active provider exists |
| `PACKAGE_DOES_NOT_FIT` | 422 | Product cannot fit configured box |
| `DISTANCE_UNAVAILABLE` | 422 | Required distance fact is absent |
| `ORIGIN_UNAVAILABLE` | 422 | No usable shipping origin exists |
| `PROVIDER_ERROR` | 502 | MS-12 adapter or carrier failed |
| `INTERNAL_ERROR` | 500 | Unexpected service failure |

## Downstream and External Boundaries

| Dependency | Direction | Protocol | Data |
|---|---|---|---|
| MS-01 | Upstream | REST/context | Customer identity and validated address |
| MS-02 | Upstream | REST/event projection | Product dimensions, weight, virtual/shippable status |
| MS-04 | Upstream/downstream | REST | Cart and checkout context; quote selection |
| MS-08 | Downstream | REST/event | Shipping and handling facts for tax |
| MS-10 | Upstream | REST/context | Store and tenant scope |
| MS-11 | Upstream | Event/projection | Shipping configuration, package, and module projections |
| MS-12 | Downstream | Internal REST/event | Carrier and Maps adapter execution |
| MS-05 | Downstream | Event/REST | Quote snapshot consumption; no order transition |
