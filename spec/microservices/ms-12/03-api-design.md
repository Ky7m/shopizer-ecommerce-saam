# Platform Integrations — API Design

**Version:** 2.0  
**Base URL:** `/api/v1/integrations`  
**Port:** `8112`  
**JSON fields:** camelCase  
**URL paths:** kebab-case  
**Authentication:** JWT plus tenant/store context

Every operation requires `x-tenant-id`, `x-store-id`, `x-correlation-id`, and `Authorization`.
`04-api-contract.yaml` is authoritative for all field names, required fields, enum values,
schemas, and status codes below.

## Endpoint Inventory

| # | Method | Path | Purpose | Success | Driving rules |
|---:|---|---|---|---:|---|
| 1 | GET | `/adapters` | List safe adapter projections | 200 | BR-INT-MS12-001 |
| 2 | POST | `/adapters/refresh` | Validate and atomically publish one adapter projection | 200 | BR-INT-MS12-002–004, BR-INT-MS12-005–006 |
| 3 | POST | `/carrier-quotes/ups` | Build, execute, and normalize a UPS rating request | 200 | BR-INT-MS12-007–009 |
| 4 | POST | `/carrier-quotes/usps` | Build, execute, and normalize USPS rating | 200 | BR-INT-MS12-010–011 |
| 5 | POST | `/maps/distance` | Enrich an eligible destination with coordinates and distance | 200 | BR-INT-MS12-012 |
| 6 | POST | `/geolocation/ip` | Resolve coarse location from an IP address | 200 | BR-INT-MS12-013 |
| 7 | POST | `/files` | Upload one asset with a durable operation key | 201 | BR-INT-MS12-018–019, BR-INT-MS12-021 |
| 8 | POST | `/files/batch` | Upload multiple assets under one durable operation key | 201 | BR-INT-MS12-018–019, BR-INT-MS12-021 |
| 9 | GET | `/files` | List direct files in a store/content namespace | 200 | BR-INT-MS12-018–019 |
| 10 | GET | `/files/{fileName}` | Read one asset as bytes and metadata | 200 | BR-INT-MS12-018–019 |
| 11 | DELETE | `/files/{fileName}` | Delete one asset | 204 | BR-INT-MS12-018–019 |
| 12 | DELETE | `/files` | Delete a store namespace | 204 | BR-INT-MS12-018–019 |
| 13 | POST | `/files/folders` | Create a provider-selected folder | 201 | BR-INT-MS12-020 |
| 14 | GET | `/files/folders` | List folders on a provider-selected adapter | 200 | BR-INT-MS12-020 |
| 15 | DELETE | `/files/folders` | Remove a provider-selected folder | 204 | BR-INT-MS12-020 |
| 16 | POST | `/emails` | Persist and queue an email delivery | 202 | BR-INT-MS12-014–017, BR-INT-MS12-022–023 |
| 17 | GET | `/delivery-attempts/{attemptId}` | Inspect durable delivery state | 200 | BR-INT-MS12-022–023 |
| 18 | POST | `/delivery-attempts/{attemptId}/replay` | Queue a replay of a failed delivery | 202 | BR-INT-MS12-023 |

## Contract-locked operation matrix

This matrix is the design-side record of the exact contract binding. The OpenAPI document
contains the same method/path, request schema, response schema, and status set.

| Method | Path | Request schema | Success response | Response statuses |
|---|---|---|---|---|
| GET | `/adapters` | query `moduleType`, `environment`, `page`, `pageSize` | `200 AdapterListResponse` | 200, 401, 500 |
| POST | `/adapters/refresh` | `RefreshAdapterRequest` | `200 Adapter` | 200, 400, 401, 409, 422, 500 |
| POST | `/carrier-quotes/ups` | `CarrierQuoteRequest` | `200 CarrierQuoteResponse` | 200, 400, 401, 422, 502, 503 |
| POST | `/carrier-quotes/usps` | `CarrierQuoteRequest` | `200 CarrierQuoteResponse` | 200, 400, 401, 422, 502, 503 |
| POST | `/maps/distance` | `DistanceRequest` | `200 DistanceResponse` | 200, 400, 401, 422, 502 |
| POST | `/geolocation/ip` | `IpGeolocationRequest` | `200 IpGeolocationResponse` | 200, 400, 401, 500 |
| POST | `/files` | `UploadFileRequest` | `201 UploadedFileAsset` | 201, 400, 401, 409, 422, 502, 503 |
| POST | `/files/batch` | `BatchUploadFileRequest` | `201 FileBatchResponse` | 201, 400, 401, 409, 422, 502, 503 |
| GET | `/files` | query `storeCode`, `contentType`, `folderPath` | `200 FileListResponse` | 200, 400, 401, 500 |
| GET | `/files/{fileName}` | path `fileName`; query `storeCode`, `contentType`, `folderPath` | `200 FileContentResponse` | 200, 400, 401, 404, 501 |
| DELETE | `/files/{fileName}` | path `fileName`; query `storeCode`, `contentType`, `folderPath` | bodyless `204` | 204, 400, 401, 404, 502 |
| DELETE | `/files` | query `storeCode`, `folderPath` | bodyless `204` | 204, 400, 401, 404, 502 |
| POST | `/files/folders` | `FolderRequest` | `201 FolderResponse` | 201, 400, 401, 422, 501 |
| GET | `/files/folders` | query `storeCode`, `provider`, `folderPath` | `200 FolderListResponse` | 200, 400, 401, 501 |
| DELETE | `/files/folders` | query `storeCode`, `provider`, `folderPath`, `folderName` | bodyless `204` | 204, 400, 401, 404, 501 |
| POST | `/emails` | `QueueEmailRequest` | `202 EmailMessage` | 202, 400, 401, 409, 422, 502, 503 |
| GET | `/delivery-attempts/{attemptId}` | path `attemptId` | `200 DeliveryAttempt` | 200, 400, 401, 404 |
| POST | `/delivery-attempts/{attemptId}/replay` | path `attemptId`; `ReplayRequest` | `202 DeliveryAttempt` | 202, 400, 401, 404, 409, 422 |

## Common request context

The four required headers are repeated on every operation in the OpenAPI contract. The service
uses `x-tenant-id` and `x-store-id` for isolation and `x-correlation-id` for traceability.

## Adapter operations

### GET `/adapters`

Optional query parameters are `moduleType` (`Email`, `Shipping`, `Maps`, `Storage`, or
`Adapter`), `environment`, `page` (default `1`), and `pageSize` (default `20`, maximum `100`).
The response is `200 AdapterListResponse`; it contains safe endpoint projections and never
returns raw MS-11 configuration or credentials. Invalid authentication returns `401` and an
unexpected projection failure returns `500`.

### POST `/adapters/refresh`

The required body fields are `moduleType`, `code`, `provider`, `environment`, and
`configurationRef`. `resolvedEndpointUri`, `capabilities`, `credentials`, `packageTypes`,
`config1`, `config2`, `timeoutMs`, and `maxAttempts` are optional. The response is
`200 Adapter`. It returns `400`, `401`, `409`, `422`, or `500` for malformed, unauthorized,
concurrent, invalid, or unexpected failures. Credentials are accepted only for resolution and
are never returned or persisted by MS-12.

## Carrier operations

### POST `/carrier-quotes/ups`

The required body fields are `environment`, `origin`, `destination`, and `packages`.
`origin` and `destination` require `countryCode` and `postalCode`; each package requires
`weight`, `weightUnit` (`KG` or `LB`), `length`, `width`, `height`, and `dimensionUnit`
(`CM` or `IN`). `orderTotal` is optional. The response is `200 CarrierQuoteResponse` and
contains normalized options; MS-09 owns quote policy and persistence. Errors are `400`, `401`,
`422`, `502`, or `503`.

### POST `/carrier-quotes/usps`

The request shape is the same as UPS. The selected store must have country code `US`.
Domestic and international routing is selected from origin/destination country equality. The
response is `200 CarrierQuoteResponse`; errors are `400`, `401`, `422`, `502`, or `503`.

## Maps and geolocation

### POST `/maps/distance`

The required fields are `origin`, `destination`, and `allowedZoneCodes`. Address fields use
`address`, `city`, `state`, `zoneCode`, `countryCode`, and `postalCode`. A destination outside
the allowed zone or without a postal code returns `200 DistanceResponse` with `enriched:false`
and `suppressedReason`; an eligible destination returns coordinates and `distanceKm`. Other
errors are `400`, `401`, `422`, or `502`.

### POST `/geolocation/ip`

The required field is `ipAddress`, accepting IPv4 or IPv6 syntax. A known address returns
`200 IpGeolocationResponse` with `resolved:true`; an address absent from GeoLite returns `200`
with `resolved:false` and null location fields. Other failures are `400`, `401`, or `500`.

## File operations

`storeCode`, `contentType`, `fileName`, `mimeType`, and `contentBase64` are required for a
single upload. `contentType` is one of `Image`, `File`, `Css`, `Js`, `Pdf`, or `Digital`.
`idempotencyKey` is also required for every upload, including batch uploads. A batch has required
`storeCode`, `idempotencyKey`, and `files`; each file has the required content fields. The one
operation record is linked to one attempt per batch item, so batch retries cannot lose item
association.

File names cannot contain path separators or traversal segments. The logical key is based on
store, content type, optional folder path, and file name. Uploads replace an existing object at
the same key, as the source providers do. Upload responses return `201` and an attempt reference.
Listings return `200` file metadata only. Supported reads return `200 FileContentResponse` with
`contentBase64` bytes; local filesystem reads that are unsupported return `501`. Deletes return
bodyless `204` and may return `401`, `404`, or `502`.

### Folder provider selection

Folder requests explicitly include `provider` (`Local`, `Infinispan`, `S3`, or `GCP`) because
folder capability is provider-specific. `POST /files/folders` requires `storeCode`, `provider`,
and `folderName`; `GET` requires `storeCode` and `provider`; `DELETE` requires `storeCode`,
`provider`, and `folderName`. `folderPath` is optional on all three. Unsupported operations
return `501 STORAGE_OPERATION_UNSUPPORTED`. Folder deletion is bodyless `204`.

## Email and delivery operations

### POST `/emails`

The required fields are `idempotencyKey`, `templateKey`, `locale`, `recipientEmail`,
`senderEmail`, `subject`, and `tokenPayload`; `senderName` and `orderReference` are optional.
The endpoint resolves sender configuration, renders the requested template, stores an
`email_message`, stores its operation and attempt association, and creates an outbox event.
It returns `202 EmailMessage`, not provider delivery confirmation. Errors are `400`, `401`,
`409`, `422`, `502`, or `503`.

### GET `/delivery-attempts/{attemptId}`

The path requires a UUID `attemptId`. It returns `200 DeliveryAttempt` or `401`/`404`.
Provider errors, retry time, operation item key, and replay lineage are included when available.

### POST `/delivery-attempts/{attemptId}/replay`

The path requires a UUID `attemptId` and the body requires a non-empty `reason`. Only `FAILED`
and `DEAD_LETTERED` attempts can be replayed. The original attempt remains terminal, a new
attempt is linked through `replayOfAttemptId`, and the endpoint returns `202 DeliveryAttempt`.
It returns `400`, `401`, `404`, `409`, or `422` for invalid, unauthorized, absent, disallowed,
or malformed requests.

## Events

### Consumed

| Event | Source | Action | Delivery |
|---|---|---|---|
| `BusinessIntegrationDeliveryRequested` | MS-05 | Deduplicate and create operation/attempt state, then enqueue | At-least-once by `eventId` and tenant/idempotency key |
| `ConfigurationReferenceChanged` | MS-11 | Refresh the opaque endpoint projection | At-least-once; latest version wins |
| `IntegrationDeliveryReplayRequested` | MS-12 | Create a new attempt linked to the original | At-least-once |
| `ShippingAdapterExecutionRequested.v1` | MS-09 | Execute the typed carrier or Maps adapter request and return normalized facts to MS-09 | At-least-once |

### Published

| Event | Trigger | Consumers |
|---|---|---|
| `IntegrationDeliveryQueued` | Operation and initial attempt are committed | MS-12 delivery worker |
| `IntegrationDeliveryDeadLettered` | Retry budget is exhausted or failure is terminal | MS-05 and operations |

Every event contains shared `eventId`, `eventType`, `eventVersion`, `occurredAt`, `tenantId`,
`storeId`, and `correlationId`. Credentials, passwords, and unredacted secret material are
excluded.

## Endpoint-to-rule coverage

| Method | Path | Status | Driving BR-IDs |
|---|---|---|---|
| GET | `/adapters` | COVERED | BR-INT-MS12-001 |
| POST | `/adapters/refresh` | COVERED | BR-INT-MS12-002, BR-INT-MS12-003, BR-INT-MS12-004, BR-INT-MS12-005, BR-INT-MS12-006 |
| POST | `/carrier-quotes/ups` | COVERED | BR-INT-MS12-007, BR-INT-MS12-008, BR-INT-MS12-009 |
| POST | `/carrier-quotes/usps` | COVERED | BR-INT-MS12-010, BR-INT-MS12-011 |
| POST | `/maps/distance` | COVERED | BR-INT-MS12-012 |
| POST | `/geolocation/ip` | COVERED | BR-INT-MS12-013 |
| POST | `/files` | COVERED | BR-INT-MS12-018, BR-INT-MS12-019, BR-INT-MS12-021 |
| POST | `/files/batch` | COVERED | BR-INT-MS12-018, BR-INT-MS12-019, BR-INT-MS12-021 |
| GET | `/files` | COVERED | BR-INT-MS12-018, BR-INT-MS12-019 |
| GET | `/files/{fileName}` | COVERED | BR-INT-MS12-018, BR-INT-MS12-019 |
| DELETE | `/files/{fileName}` | COVERED | BR-INT-MS12-018, BR-INT-MS12-019 |
| DELETE | `/files` | COVERED | BR-INT-MS12-018, BR-INT-MS12-019 |
| POST | `/files/folders` | COVERED | BR-INT-MS12-020 |
| GET | `/files/folders` | COVERED | BR-INT-MS12-020 |
| DELETE | `/files/folders` | COVERED | BR-INT-MS12-020 |
| POST | `/emails` | COVERED | BR-INT-MS12-014, BR-INT-MS12-015, BR-INT-MS12-016, BR-INT-MS12-017, BR-INT-MS12-022, BR-INT-MS12-023 |
| GET | `/delivery-attempts/{attemptId}` | COVERED | BR-INT-MS12-022, BR-INT-MS12-023 |
| POST | `/delivery-attempts/{attemptId}/replay` | COVERED | BR-INT-MS12-023 |
