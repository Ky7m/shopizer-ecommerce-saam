# MS-08 Tax API Design

**Version:** 1.0  
**Base URL:** `/api/v1`  
**Field naming:** camelCase  
**Path naming:** kebab-case  
**Authentication:** Bearer token  
**Required headers on every operation:**

| Header | Required | Purpose |
|---|---:|---|
| `x-tenant-id` | Yes | Tenant isolation |
| `x-store-id` | Yes | Store isolation |
| `x-correlation-id` | Yes | Distributed trace correlation |
| `Authorization` | Yes | Authenticated caller identity |

Request body store identifiers are not accepted as ownership authority. The authenticated `x-store-id` controls all reads and writes.

## Standard error model

```json
{
  "error": "TAX_RATE_NOT_FOUND",
  "message": "Tax rate was not found for store store-001",
  "statusCode": 404,
  "timestamp": "2026-09-01T15:38:48Z",
  "correlationId": "corr-001",
  "details": {}
}
```

## Tax-class endpoints

### POST `/tax-classes`

- **Purpose:** Create a tax class for the authenticated tenant/store.
- **Driven by:** `BR-TAX-CLS-001`
- **Request:** `CreateTaxClassRequest`
- **Success:** `201 Created`, `TaxClass`
- **Errors:** `400`, `401`, `403`, `409`, `422`, `500`
- **Duplicate behavior:** `409 TAX_CLASS_ALREADY_EXISTS`

### GET `/tax-classes`

- **Purpose:** List tax classes belonging to the authenticated tenant/store.
- **Driven by:** `BR-TAX-CLS-002`
- **Query parameters:** `page` default `1`, `pageSize` default `20`
- **Success:** `200`, `TaxClassListResponse`
- **Errors:** `400`, `401`, `500`

### GET `/tax-classes/{id}`

- **Purpose:** Retrieve a store-owned tax class.
- **Driven by:** `BR-TAX-CLS-002`
- **Path parameter:** `id`, UUID
- **Success:** `200`, `TaxClass`
- **Errors:** `401`, `404`, `500`

### PUT `/tax-classes/{id}`

- **Purpose:** Update a store-owned tax class.
- **Driven by:** `BR-TAX-CLS-003`
- **Request:** `UpdateTaxClassRequest`
- **Success:** `200`, `TaxClass`
- **Errors:** `400`, `401`, `403`, `404`, `409`, `422`, `500`

### DELETE `/tax-classes/{id}`

- **Purpose:** Delete a store-owned tax class.
- **Driven by:** `BR-TAX-CLS-003`
- **Success:** `200`, `DeleteResponse`
- **Errors:** `401`, `403`, `404`, `409`, `500`

### GET `/tax-classes/exists`

- **Purpose:** Return whether a tax-class code exists in the authenticated tenant/store.
- **Driven by:** `BR-TAX-CLS-001`
- **Query parameter:** `code`
- **Success:** `200`, `ExistsResponse`
- **Errors:** `400`, `401`, `500`

## Tax-rate endpoints

### POST `/tax-rates`

- **Purpose:** Create a tax rate with geography, tax class, priority, compound behavior, and localized descriptions.
- **Driven by:** `BR-TAX-RAT-001`
- **Request:** `CreateTaxRateRequest`
- **Success:** `201`, `TaxRate`
- **Errors:** `400`, `401`, `403`, `409`, `422`, `500`

### GET `/tax-rates`

- **Purpose:** List store-owned tax rates in requested language and priority order.
- **Driven by:** `BR-TAX-RAT-003`
- **Query parameters:** `languageCode` default `en`, `page` default `1`, `pageSize` default `20`
- **Success:** `200`, `TaxRateListResponse`
- **Errors:** `400`, `401`, `422`, `500`

### GET `/tax-rates/{id}`

- **Purpose:** Retrieve a store-owned tax rate.
- **Driven by:** `BR-TAX-RAT-004`
- **Success:** `200`, `TaxRate`
- **Errors:** `401`, `404`, `500`

### PUT `/tax-rates/{id}`

- **Purpose:** Update a store-owned tax rate and its localized descriptions.
- **Driven by:** `BR-TAX-RAT-002`
- **Request:** `UpdateTaxRateRequest`
- **Success:** `200`, `TaxRate`
- **Errors:** `400`, `401`, `403`, `404`, `409`, `422`, `500`

### DELETE `/tax-rates/{id}`

- **Purpose:** Delete a store-owned tax rate.
- **Driven by:** `BR-TAX-RAT-004`
- **Success:** `200`, `DeleteResponse`
- **Errors:** `401`, `403`, `404`, `409`, `500`

### GET `/tax-rates/exists`

- **Purpose:** Return whether a tax-rate code exists in the authenticated tenant/store.
- **Driven by:** `BR-TAX-RAT-005`
- **Query parameter:** `code`
- **Success:** `200`, `ExistsResponse`
- **Errors:** `400`, `401`, `500`

## Tax configuration endpoints

### GET `/tax-configuration`

- **Purpose:** Retrieve the store's tax configuration, applying the shipping-address default when no configuration exists.
- **Driven by:** `BR-TAX-CFG-001`
- **Success:** `200`, `TaxConfiguration`
- **Errors:** `401`, `422`, `500`

### PUT `/tax-configuration`

- **Purpose:** Save all tax-basis and geographic policy settings for the authenticated tenant/store.
- **Driven by:** `BR-TAX-CFG-002`
- **Request:** `UpdateTaxConfigurationRequest`
- **Success:** `200`, `TaxConfiguration`
- **Errors:** `400`, `401`, `422`, `500`

## Tax calculation endpoint

### POST `/tax-calculations`

- **Purpose:** Calculate tax from item amounts, tax-class codes, customer address snapshots, store configuration, and shipping/handling amounts.
- **Driven by:** `BR-TAX-CAL-001` through `BR-TAX-CAL-010`
- **Request:** `CalculateTaxRequest`
- **Success:** `200`, `TaxCalculationResponse`
- **Errors:** `400`, `401`, `422`, `500`
- **Ownership:** MS-08 returns tax results; MS-04/MS-05 retain ownership of cart/order totals.
- **External provider:** None is assumed or fabricated. A future provider adapter must be introduced through an explicit contract.

## Cross-service boundary

The calculation endpoint accepts snapshots and identifiers rather than directly reading:

- product tables owned by MS-02;
- cart/checkout tables owned by MS-04;
- order tables owned by MS-05;
- customer/address tables owned by MS-01;
- store/merchant tables owned by MS-10;
- shipping tables owned by MS-09.

## Endpoint coverage

| Endpoint | Status | Driving BR-IDs |
|---|---|---|
| POST `/tax-classes` | COVERED | BR-TAX-CLS-001 |
| GET `/tax-classes` | COVERED | BR-TAX-CLS-002 |
| GET `/tax-classes/{id}` | COVERED | BR-TAX-CLS-002 |
| PUT `/tax-classes/{id}` | COVERED | BR-TAX-CLS-003 |
| DELETE `/tax-classes/{id}` | COVERED | BR-TAX-CLS-003 |
| GET `/tax-classes/exists` | COVERED | BR-TAX-CLS-001 |
| POST `/tax-rates` | COVERED | BR-TAX-RAT-001 |
| GET `/tax-rates` | COVERED | BR-TAX-RAT-003 |
| GET `/tax-rates/{id}` | COVERED | BR-TAX-RAT-004 |
| PUT `/tax-rates/{id}` | COVERED | BR-TAX-RAT-002 |
| DELETE `/tax-rates/{id}` | COVERED | BR-TAX-RAT-004 |
| GET `/tax-rates/exists` | COVERED | BR-TAX-RAT-005 |
| GET `/tax-configuration` | COVERED | BR-TAX-CFG-001 |
| PUT `/tax-configuration` | COVERED | BR-TAX-CFG-002 |
| POST `/tax-calculations` | COVERED | BR-TAX-CAL-001 through BR-TAX-CAL-010 |

## Events

No tax events are published or consumed in the legacy implementation. Tax calculation is synchronous. A future event contract requires explicit architecture approval.

## Phase 4b inferred provider and failure clarifications

- `[Inferred in Phase 4b — Mode A]` When configured, the external provider request contains
  destination, tax class, taxable lines, and currency; the response must contain rate,
  amount, jurisdiction, and provider reference.
- `[Inferred in Phase 4b — Mode A]` Provider timeout or rejection returns a typed provider
  error unless an explicitly configured fallback can calculate the quote locally.
- `[Inferred in Phase 4b — Mode A]` A no-rate result is zero tax only when the jurisdiction
  policy allows zero tax; otherwise the API returns a typed validation error.
