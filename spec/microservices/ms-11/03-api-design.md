# Content and Configuration — API Design

**Service ID:** MS-11  
**Service Name:** Content and Configuration  
**Port:** `8111`  
**Base path:** `/api/v1`  
**JSON naming:** camelCase  
**Path naming:** kebab-case  
**Status:** 🟡 In Progress — Phase 4 extraction  

## API boundary

MS-11 exposes merchant-scoped CMS content, localized descriptions, content-file metadata and storage orchestration, public configuration projection, merchant configuration state, and payment/shipping integration-module configuration metadata.

MS-11 does **not**:

- manage merchant or store lifecycle;
- write MS-10 store tables;
- charge or refund payments;
- calculate shipping quotes;
- execute payment or shipping provider operations;
- expose provider execution endpoints owned by MS-12;
- store CMS file bytes in PostgreSQL.

Payment and shipping configuration mutations invoke an MS-12 provider-validation boundary before MS-11 persists configuration state.

## Request context

### Required headers

The following headers are required on every target request:

| Header | Required | Description |
|---|---:|---|
| `x-tenant-id` | Yes | Tenant isolation key. Must be a valid UUID. |
| `x-store-id` | Yes for store-scoped operations | Store isolation key. Must belong to `x-tenant-id`; the store lifecycle is owned by MS-10. |
| `x-correlation-id` | Yes | Distributed tracing identifier. A UUID is recommended. |
| `Authorization` | Private operations | `Bearer <JWT>`. Required for administration and configuration mutations. |

Global module-definition replacement is tenant/platform scoped and requires `x-tenant-id`, `x-correlation-id`, and `Authorization`; `x-store-id` is optional for that operation because the persisted module definition is not store-owned.

### Language context

Localized operations accept the optional `x-language` header.

| Value | Meaning |
|---|---|
| ISO language code, for example `en` or `fr` | Return or process the selected language. |
| Header absent on a read | Return the all-language projection where the endpoint supports it. |
| Header absent on a mutation | No implicit language is selected; the request `descriptions` collection is authoritative. |

The legacy `lang` request context and `store` request context may be accepted by a migration adapter, but target clients must use `x-language` and `x-store-id`.

### Authorization

- Public content and public configuration reads do not require `Authorization`.
- Private content reads and mutations require an authenticated administrator authorized for `x-tenant-id` and `x-store-id`.
- Merchant configuration and payment/shipping module configuration require administrator authorization for the selected store.
- Global module-definition replacement requires a platform administrator.
- A caller must never select a store outside the tenant in `x-tenant-id`.

### Idempotency

| Operation class | Expectation |
|---|---|
| GET | Safe and repeatable; no `Idempotency-Key` required. |
| Create page or box | `Idempotency-Key` required. Replaying the same key and equivalent request returns the original result. |
| Update page or box | PUT is idempotent for the same tenant, store, content identifier, and request representation. |
| Content deletion | Repeating a deletion of an existing identifier returns the same successful deletion result; an unknown identifier returns `404`. |
| File upload | `Idempotency-Key` required. Same namespace and file name replace the stored object; a replay with the same key must not create a second object. |
| File rename | `Idempotency-Key` required. Replaying a completed key returns the original result. A new request after the original name has been removed returns `404`. |
| File deletion | The target contract defines absent-file deletion as idempotent success with `204`. |
| Folder creation | PUT semantics are idempotent for the same folder path. |
| Folder deletion | Repeating deletion of an absent folder returns `204`. |
| Configuration update | PUT is idempotent for the same store, configuration key, and request representation. |
| Module replacement | `Idempotency-Key` required. Replaying the same key returns the original replacement result. |

The legacy implementation did not consume an idempotency key. The target requirement is explicit because uploads, rename, configuration replacement, and module replacement can otherwise be retried ambiguously.

## Common response and error shapes

### `ContentDescription`

```json
{
  "id": "8e6f9b1f-8c61-4db4-99a7-3ab7c42a2f30",
  "language": "en",
  "name": "About us",
  "title": "About Us",
  "description": "Our company",
  "friendlyUrl": "about-us",
  "metaKeywords": "company,about",
  "metaTitle": "About Us",
  "metaDescription": "Information about our company"
}
```

`id`, `language`, `name`, `title`, `description`, and `friendlyUrl` are the fields required for normal localized projections. Metadata fields are nullable.

### `ContentItem`

```json
{
  "id": "5f2a6d79-6d6f-4c03-82e6-68e7a44e0ed7",
  "code": "about-us",
  "contentType": "Page",
  "visible": true,
  "linkToMenu": true,
  "sortOrder": 10,
  "contentPosition": "LEFT",
  "productGroup": null,
  "description": {
    "id": "8e6f9b1f-8c61-4db4-99a7-3ab7c42a2f30",
    "language": "en",
    "name": "About us",
    "title": "About Us",
    "description": "Our company",
    "friendlyUrl": "about-us",
    "metaKeywords": null,
    "metaTitle": "About Us",
    "metaDescription": null
  }
}
```

`description` is present for a language-specific projection. `descriptions` is present for an all-language projection.

### `ContentListResponse`

```json
{
  "items": [],
  "page": 0,
  "count": 20,
  "number": 0,
  "totalPages": 1,
  "recordsTotal": 0,
  "recordsFiltered": 0
}
```

| Field | Type | Description |
|---|---|---|
| `items` | array | Content items for the requested page. |
| `page` | integer | Zero-based persistence page index. |
| `count` | integer | Requested page size. |
| `number` | integer | Number of records in the current page. |
| `totalPages` | integer | Total persistence pages. |
| `recordsTotal` | integer | Total matching records. |
| `recordsFiltered` | integer | Total records after filtering; equal to `recordsTotal` when no additional filter is applied. |

### `EntityIdResponse`

```json
{
  "id": "5f2a6d79-6d6f-4c03-82e6-68e7a44e0ed7"
}
```

### `EntityExistsResponse`

```json
{
  "exists": true
}
```

### `ErrorResponse`

```json
{
  "error": "CONTENT_CODE_CONFLICT",
  "message": "Content code [about-us] already exists for store [DEFAULT].",
  "statusCode": 409,
  "timestamp": "2026-09-01T18:45:13.939+04:00",
  "correlationId": "9f9b4bc7-86c7-4db7-8590-2270e84d70b8"
}
```

Error codes include:

| Error code | HTTP status | Meaning |
|---|---:|---|
| `INVALID_REQUEST_CONTEXT` | 400 | Missing or malformed tenant, store, language, or correlation context. |
| `INVALID_CONTENT_REQUEST` | 400 | Invalid content payload shape. |
| `INVALID_FILENAME` | 422 | File-manager filename validation failed. |
| `INVALID_FOLDER_PATH` | 422 | Folder path is not valid Linux-style directory syntax. |
| `LANGUAGE_NOT_FOUND` | 422 | Submitted language code does not resolve in shared reference data. |
| `CONTENT_CODE_CONFLICT` | 409 | Content code already exists within the merchant store. |
| `CONTENT_NOT_FOUND` | 404 | Content identifier or code is not present in the selected store. |
| `FILE_NOT_FOUND` | 404 | Requested file is not present in the selected namespace. |
| `FOLDER_NOT_FOUND` | 404 | Requested folder does not exist. |
| `MODULE_NOT_FOUND` | 404 | Requested payment or shipping module is unavailable to the store. |
| `CONFIGURATION_NOT_FOUND` | 404 | Requested merchant configuration key does not exist. |
| `CONFIGURATION_UNAVAILABLE` | 503 | No usable merchant configuration is available. |
| `MODULE_CONFIGURATION_INVALID` | 422 | Provider validation rejected integration keys or options. |
| `PROVIDER_UNAVAILABLE` | 503 | Selected CMS or provider-validation boundary is unavailable. |
| `PROVIDER_CAPABILITY_UNSUPPORTED` | 501 | Selected provider does not support the requested operation. |
| `LEGACY_OPERATION_RETIRED` | 410 | Deprecated legacy operation was explicitly nonfunctional or has been removed. |
| `FORBIDDEN_STORE_SCOPE` | 403 | Caller is not authorized for the selected store. |

## Endpoint inventory

| # | Method | Path | Scope | Purpose | Driving rules |
|---:|---|---|---|---|---|
| 1 | GET | `/content/pages` | Public | List page summaries. | BR-MER-016, BR-MER-019, BR-MER-020 |
| 2 | GET | `/private/content/pages` | Private | List page summaries for administration. | BR-MER-016, BR-MER-019, BR-MER-020 |
| 3 | GET | `/content/pages/{code}` | Public | Read a page by content code. | BR-MER-016, BR-MER-018, BR-MER-020 |
| 4 | GET | `/private/content/pages/{code}` | Private | Read a page by content code for administration. | BR-MER-016, BR-MER-020 |
| 5 | GET | `/content/pages/name/{name}` | Public | Read visible page content by localized friendly URL. | BR-MER-017, BR-MER-020 |
| 6 | GET | `/private/content/boxes` | Private | List content boxes. | BR-MER-016, BR-MER-019, BR-MER-020 |
| 7 | GET | `/content/boxes` | Public | Read content-box list projection. | BR-MER-016, BR-MER-019, BR-MER-020 |
| 8 | GET | `/private/content/boxes/{code}` | Private | Read one content box by code. | BR-MER-016, BR-MER-020 |
| 9 | GET | `/content/boxes/{code}` | Public | Read one content box by code. | BR-MER-016, BR-MER-018, BR-MER-020 |
| 10 | POST | `/private/content/page` | Private | Create a page. | BR-MER-013, BR-MER-014, BR-MER-015 |
| 11 | PUT | `/private/content/page/{contentId}` | Private | Replace a page and its submitted descriptions. | BR-MER-014, BR-MER-015 |
| 12 | DELETE | `/private/content/page/{contentId}` | Private | Delete a page in the owning store. | BR-MER-021 |
| 13 | GET | `/private/content/page/{code}/exists` | Private | Check page-code existence. | BR-MER-013 |
| 14 | POST | `/private/content/box` | Private | Create a content box. | BR-MER-013, BR-MER-014, BR-MER-015 |
| 15 | PUT | `/private/content/box/{contentId}` | Private | Replace a content box and its submitted descriptions. | BR-MER-014, BR-MER-015 |
| 16 | DELETE | `/private/content/box/{contentId}` | Private | Delete a content box in the owning store. | BR-MER-021 |
| 17 | GET | `/private/content/box/{code}/exists` | Private | Check box-code existence. | BR-MER-013 |
| 18 | GET | `/private/content/files` | Private | List files in a store/type/path namespace. | BR-MER-024, BR-EXT-021, BR-EXT-022, BR-EXT-023 |
| 19 | GET | `/private/content/list` | Private | File-manager image listing projection. | BR-MER-028, BR-EXT-021, BR-EXT-022 |
| 20 | GET | `/private/content/folder` | Private | Read an image folder projection. | BR-MER-028, BR-MER-026, BR-EXT-023 |
| 21 | POST | `/private/content/files` | Private | Upload a generic content file. | BR-MER-022, BR-MER-024, BR-EXT-021, BR-EXT-022, BR-EXT-023 |
| 22 | POST | `/private/content/images/add` | Private | Upload a file-manager image. | BR-MER-022, BR-MER-023, BR-MER-024, BR-EXT-021, BR-EXT-022 |
| 23 | POST | `/private/file` | Private | Legacy single-file upload alias. | BR-MER-022, BR-MER-024, BR-EXT-021, BR-EXT-022 |
| 24 | POST | `/private/files` | Private | Legacy multi-file upload alias. | BR-MER-022, BR-MER-024, BR-EXT-021, BR-EXT-022 |
| 25 | GET | `/content/images/download` | Public | Download an image object. | BR-MER-027, BR-MER-024, BR-EXT-021, BR-EXT-022, BR-EXT-023 |
| 26 | GET | `/private/content/files/{fileName}/download` | Private | Download a typed content file. | BR-MER-024, BR-EXT-021, BR-EXT-022, BR-EXT-023 |
| 27 | POST | `/private/content/images/rename` | Private | Rename an image while retaining metadata. | BR-MER-025, BR-EXT-021, BR-EXT-022, BR-EXT-029 |
| 28 | POST | `/private/content/files/rename` | Private | Rename a typed content file. | BR-MER-025, BR-EXT-021, BR-EXT-022, BR-EXT-029 |
| 29 | DELETE | `/private/content/images/remove` | Private | Delete an image from the image namespace. | BR-MER-024, BR-EXT-021, BR-EXT-022, BR-EXT-030 |
| 30 | DELETE | `/private/content/files/{fileName}` | Private | Delete one typed content file. | BR-MER-024, BR-EXT-021, BR-EXT-022, BR-EXT-030 |
| 31 | POST | `/private/content/folders` | Private | Create a folder. | BR-MER-026, BR-EXT-021, BR-EXT-023 |
| 32 | GET | `/private/content/folders` | Private | List folders when the selected provider supports it. | BR-MER-026, BR-EXT-021, BR-EXT-023 |
| 33 | DELETE | `/private/content/folders` | Private | Remove a folder when the selected provider supports it. | BR-MER-026, BR-EXT-021, BR-EXT-023 |
| 34 | GET | `/config` | Public | Return the public configuration projection. | BR-CF-003, BR-CF-004, BR-CF-005, BR-CF-015 |
| 35 | GET | `/private/configuration` | Private | Read typed merchant configuration. | BR-CF-001, BR-CF-002, BR-CF-015 |
| 36 | PUT | `/private/configuration` | Private | Save typed merchant configuration. | BR-CF-001, BR-CF-002 |
| 37 | GET | `/private/configurations/{key}` | Private | Read one merchant configuration record. | BR-CF-001, BR-CF-006, BR-EXT-025 |
| 38 | PUT | `/private/configurations/{key}` | Private | Save one merchant configuration record. | BR-CF-001, BR-CF-006, BR-EXT-025 |
| 39 | GET | `/private/modules/payment` | Private | Discover available payment modules with merchant status. | BR-CF-011, BR-CF-012, BR-CF-014, BR-EXT-024, BR-EXT-026, BR-EXT-027 |
| 40 | GET | `/private/modules/payment/{code}` | Private | Read payment module metadata and merchant configuration. | BR-CF-012, BR-CF-013, BR-CF-014, BR-CF-006, BR-EXT-027, BR-EXT-028 |
| 41 | PUT | `/private/modules/payment/{code}` | Private | Validate and save payment module configuration state. | BR-CF-006, BR-CF-007, BR-CF-013, BR-EXT-025 |
| 42 | GET | `/private/modules/shipping` | Private | Discover available shipping modules with merchant status. | BR-CF-011, BR-CF-012, BR-CF-014, BR-EXT-026, BR-EXT-027 |
| 43 | GET | `/private/modules/shipping/{code}` | Private | Read shipping module metadata and merchant configuration. | BR-CF-012, BR-CF-013, BR-CF-014, BR-CF-006, BR-EXT-027, BR-EXT-028 |
| 44 | PUT | `/private/modules/shipping/{code}` | Private | Validate and save shipping module configuration state. | BR-CF-006, BR-CF-007, BR-CF-013, BR-EXT-025 |
| 45 | POST | `/services/private/system/module` | Platform private | Replace a global integration-module definition by code. | BR-CF-008, BR-CF-009, BR-CF-010, BR-EXT-026, BR-EXT-028 |

## Content page and box endpoints

### GET `/api/v1/content/pages`

### GET `/api/v1/private/content/pages`

Lists page items for the selected tenant and store.

**Auth:**

- Public route: anonymous or authenticated.
- Private route: authenticated administrator authorized for the store.

**Query parameters:**

| Parameter | Required | Type | Default | Description |
|---|---:|---|---:|---|
| `page` | No | integer | `0` | Zero-based page index. |
| `count` | No | integer | `20` | Number of items per page; must be positive. |
| `language` | No | string | absent | Target language. If absent, the response contains all available descriptions. |

**Response:** `200 ContentListResponse`.

The query is store-scoped, restricted to `contentType=PAGE`, ordered by ascending `sortOrder`, and paginated.

**Errors:** `400`, `401`, `403`, `422`, `500`.

**CRUD-only:** No. Store scope, content type, ordering, pagination, and localized projection are business behavior.

**Driving rules:** BR-MER-016, BR-MER-019, BR-MER-020.

### GET `/api/v1/content/pages/{code}`

Reads a page by content code.

**Path parameters:**

| Parameter | Type | Description |
|---|---|---|
| `code` | string | Content code within the selected store. |

**Response:** `200 ContentItem`.

With `x-language`, the response contains the matching localized `description`. Without `x-language`, the response contains `descriptions` for all available languages.

The code lookup preserves the source behavior in which the public code route returns the stored `visible` value but does not independently apply the friendly-URL visibility predicate.

**Errors:** `400`, `403`, `404 CONTENT_NOT_FOUND`, `500`.

**CRUD-only:** No. Language-specific projection and store scoping apply.

**Driving rules:** BR-MER-016, BR-MER-018, BR-MER-020.

### GET `/api/v1/private/content/pages/{code}`

Reads a page by content code for administration.

**Auth:** Authenticated administrator authorized for the selected store.

**Response:** `200 ContentItem`.

The private route has the same language-specific and all-language projection rules as the public code route.

**Errors:** `400`, `401`, `403`, `404`, `500`.

**CRUD-only:** No.

**Driving rules:** BR-MER-016, BR-MER-020.

### GET `/api/v1/content/pages/name/{name}`

Reads a page by localized friendly URL.

**Path parameters:**

| Parameter | Type | Description |
|---|---|---|
| `name` | string | Localized friendly URL. |

**Headers:** `x-language` is required because friendly-URL lookup is language-specific.

**Response:** `200 ContentItem`.

The lookup is restricted to:

1. the selected tenant and store;
2. the selected language;
3. a localized description whose `friendlyUrl` equals `name`;
4. a content item with `visible=true`.

An invisible item is returned as `404 CONTENT_NOT_FOUND`.

**Errors:** `400`, `403`, `404`, `500`.

**CRUD-only:** No. Publication eligibility is enforced.

**Driving rules:** BR-MER-017, BR-MER-020.

### GET `/api/v1/content/boxes`

### GET `/api/v1/private/content/boxes`

Lists content boxes for the selected store.

**Query parameters:** `page`, `count`, and optional `language`, with the same meanings as the page-list endpoint.

**Response:** `200 ContentListResponse`.

The query is restricted to `contentType=BOX`, ordered by ascending `sortOrder`, and store-scoped. The response contains one selected localized description when `language` is supplied and all descriptions when it is absent.

The language-specific box projection applies the legacy formatting rule:

- remove carriage returns, newlines, and tab characters;
- wrap the resulting description in `<![CDATA[...]]>`.

Page projections do not apply this box-specific CDATA transformation.

**Errors:** `400`, `401` for the private route, `403`, `422`, `500`.

**CRUD-only:** No.

**Driving rules:** BR-MER-016, BR-MER-019, BR-MER-020.

### GET `/api/v1/content/boxes/{code}`

### GET `/api/v1/private/content/boxes/{code}`

Reads one content box by code.

**Path parameters:** `code`.

**Response:** `200 ContentItem` with `contentType=BOX`.

The public code route returns the stored visibility value but does not apply the friendly-URL visibility predicate. The private route is restricted to an authorized administrator.

When `x-language` is supplied, the response contains the selected description with box-specific CDATA formatting. When it is absent, the response contains all descriptions without applying the single-language CDATA wrapper to each all-language entry.

**Errors:** `400`, `401` for the private route, `403`, `404`, `500`.

**CRUD-only:** No.

**Driving rules:** BR-MER-016, BR-MER-018, BR-MER-020.

### POST `/api/v1/private/content/page`

Creates a page.

**Auth:** Authenticated administrator authorized for the selected store.

**Headers:** `Idempotency-Key` required.

**Request body:**

```json
{
  "code": "about-us",
  "visible": true,
  "linkToMenu": true,
  "sortOrder": 10,
  "contentPosition": "LEFT",
  "productGroup": null,
  "descriptions": [
    {
      "language": "en",
      "name": "About us",
      "title": "About Us",
      "description": "Our company",
      "friendlyUrl": "about-us",
      "metaKeywords": "company,about",
      "metaDescription": "Information about our company"
    }
  ]
}
```

| Field | Required | Description |
|---|---:|---|
| `code` | Yes | Non-blank store-scoped content code. |
| `visible` | Yes | Whether the content item is visible. |
| `linkToMenu` | Yes | Whether the page is linked to menu presentation. Independent of `visible`. |
| `sortOrder` | No | Ordering value; defaults to `0`. |
| `contentPosition` | No | `LEFT` or `RIGHT`. |
| `productGroup` | No | Opaque catalog product-group reference. |
| `descriptions` | Yes | Submitted localized descriptions. |
| `descriptions[].language` | Yes | Existing shared language code. |
| `descriptions[].name` | Yes | Non-blank localized name. |
| `descriptions[].title` | No | Localized title. |
| `descriptions[].description` | No | Localized body. |
| `descriptions[].friendlyUrl` | No | Localized friendly URL. |
| `descriptions[].metaKeywords` | No | Localized metadata keywords. |
| `descriptions[].metaDescription` | No | Localized metadata description. |

`contentType` is not accepted as a type-selection field. If supplied for compatibility, it is ignored and the operation persists `PAGE`.

**Response:** `201 EntityIdResponse`.

The operation:

- rejects an existing code anywhere in the selected store, including a box with the same code;
- assigns `contentType=PAGE`;
- resolves every submitted language code;
- updates matching localized descriptions by language code;
- creates a localized description when the language is new;
- replaces the submitted description collection, so omitted languages are not retained.

**Errors:** `400`, `401`, `403`, `409 CONTENT_CODE_CONFLICT`, `422 LANGUAGE_NOT_FOUND`, `500`.

**Driving rules:** BR-MER-013, BR-MER-014, BR-MER-015.

### PUT `/api/v1/private/content/page/{contentId}`

Replaces an existing page.

**Auth:** Authenticated administrator authorized for the selected store.

**Request body:** Same as page creation. `contentType`, if present, is ignored; the operation always persists `PAGE`.

**Response:** `204` with an empty body.

The identifier must belong to the selected store. The submitted descriptions replace the existing localized description collection using language-code matching.

**Errors:** `400`, `401`, `403`, `404 CONTENT_NOT_FOUND`, `422 LANGUAGE_NOT_FOUND`, `500`.

**CRUD-only:** No. Type enforcement, localized upsert, and replacement semantics apply.

**Driving rules:** BR-MER-014, BR-MER-015.

### DELETE `/api/v1/private/content/page/{contentId}`

Deletes a page.

**Auth:** Authenticated administrator authorized for the selected store.

**Response:** `204`.

Deletion is permitted only when the content identifier belongs to the selected store. A content identifier from another store is treated as not found and is not deleted.

**Errors:** `400`, `401`, `403`, `404 CONTENT_NOT_FOUND`, `500`.

**CRUD-only:** No. Store ownership authorization applies.

**Driving rules:** BR-MER-021.

### GET `/api/v1/private/content/page/{code}/exists`

Checks whether a page code exists in the selected store.

**Response:** `200 EntityExistsResponse`.

The check is store-scoped and restricted to page content.

**Errors:** `400`, `401`, `403`, `500`.

**CRUD-only:** Yes — standard store-scoped existence read with the uniqueness rule applied.

**Driving rules:** BR-MER-013.

### POST `/api/v1/private/content/box`

Creates a content box.

**Auth:** Authenticated administrator authorized for the selected store.

**Headers:** `Idempotency-Key` required.

**Request body:** Same localized description structure as page creation, with `code`, `visible`, `sortOrder`, and `productGroup` as applicable. `linkToMenu` is not a box publication control.

`contentType`, if supplied, is ignored and the operation always persists `BOX`.

**Response:** `201 EntityIdResponse`.

**Errors:** `400`, `401`, `403`, `409`, `422 LANGUAGE_NOT_FOUND`, `500`.

**Driving rules:** BR-MER-013, BR-MER-014, BR-MER-015.

### PUT `/api/v1/private/content/box/{contentId}`

Replaces a content box.

**Response:** `204` with an empty body.

The identifier must belong to the selected store. The operation always persists `BOX`, updates or creates descriptions by language code, and replaces the submitted description collection.

**Errors:** `400`, `401`, `403`, `404`, `422 LANGUAGE_NOT_FOUND`, `500`.

**Driving rules:** BR-MER-014, BR-MER-015.

### DELETE `/api/v1/private/content/box/{contentId}`

Deletes a content box in the owning store.

**Response:** `204`.

**Errors:** `400`, `401`, `403`, `404`, `500`.

**Driving rules:** BR-MER-021.

### GET `/api/v1/private/content/box/{code}/exists`

Checks whether a box code exists in the selected store.

**Response:** `200 EntityExistsResponse`.

**CRUD-only:** Yes — standard store-scoped existence read with content-code uniqueness applied.

**Driving rules:** BR-MER-013.

## Content-file endpoints

### File namespace

Every file request uses:

- `contentType`: one of `StaticFile`, `Image`, `Logo`, `Product`, `ProductLg`, `Property`, `Variant`, `Manufacturer`, `ProductDigital`, `ApiImage`, or `ApiFile`;
- `path`: optional Linux-style folder path;
- `fileName`: basename only, without `/`, `\`, or `..`.

`API_IMAGE` is normalized to `IMAGE` before storage. `API_FILE` is normalized to `STATIC_FILE`. An upload whose MIME major component is `image` is classified as an API image; all other MIME major components are classified as a static file.

Provider object keys use the content root and merchant store code. Non-base types receive an additional content-type namespace. `IMAGE` and `STATIC_FILE` use the base store namespace. The same key-generation rule is used for upload, retrieval, listing, rename, and deletion.

### GET `/api/v1/private/content/files`

Lists content files.

**Query parameters:**

| Parameter | Required | Type | Default | Description |
|---|---:|---|---|---|
| `contentType` | Yes | enum | — | File-content namespace. |
| `path` | No | string | `/` | Folder path. |
| `page` | No | integer | `0` | Zero-based page index. |
| `count` | No | integer | `100` | Page size. |

**Response:** `200 FileListResponse`.

```json
{
  "items": [
    {
      "fileName": "hero.png",
      "mimeType": "image/png",
      "contentType": "Image",
      "path": "/",
      "provider": "default",
      "state": "AVAILABLE",
      "downloadPath": "/api/v1/private/content/files/hero.png/download"
    }
  ],
  "page": 0,
  "count": 100,
  "number": 1,
  "totalPages": 1,
  "recordsTotal": 1,
  "recordsFiltered": 1
}
```

The selected provider must support listing. A provider that cannot list files returns `501 PROVIDER_CAPABILITY_UNSUPPORTED`; it must not return a false successful empty list.

**Errors:** `400`, `401`, `403`, `422`, `501`, `503`.

**CRUD-only:** No. Provider selection, namespace isolation, and capability behavior apply.

**Driving rules:** BR-MER-024, BR-EXT-021, BR-EXT-022, BR-EXT-023.

### GET `/api/v1/private/content/list`

Returns the file-manager image listing.

**Query parameters:**

| Parameter | Required | Type | Default | Description |
|---|---:|---|---|---|
| `parentPath` | No | string | `/` | URL-encoded image path. Blank values and paths containing `/images` resolve to `/`. |

**Response:** `200 ImageFile[]`.

```json
[
  {
    "url": "/static/images/DEFAULT/hero.png",
    "name": "hero.png",
    "size": null,
    "dir": false,
    "path": "image.png",
    "id": "/static/images/DEFAULT/hero.png"
  }
]
```

The listing is restricted to the selected store and `IMAGE` namespace. Each entry includes the generated static-image path derived from the store and file name.

**Errors:** `400`, `401`, `403`, `501`, `503`.

**CRUD-only:** No. Store/type isolation and generated path projection apply.

**Driving rules:** BR-MER-028, BR-EXT-021, BR-EXT-022.

### GET `/api/v1/private/content/folder`

Returns the legacy image-folder projection.

**Query parameters:**

| Parameter | Required | Type | Default |
|---|---:|---|---|
| `path` | No | string | `/` |

**Response:** `200 ContentFolderResponse`.

```json
{
  "path": "/",
  "content": [
    {
      "name": "hero.png",
      "path": "/static/images/DEFAULT/hero.png"
    }
  ]
}
```

The selected provider must support folder/file listing. The target must not claim portable folder enumeration where the provider does not implement it.

**Errors:** `400`, `401`, `403`, `501 PROVIDER_CAPABILITY_UNSUPPORTED`, `503`.

**Driving rules:** BR-MER-026, BR-MER-028, BR-EXT-023.

### POST `/api/v1/private/content/files`

Uploads one generic content file.

**Auth:** Authenticated administrator authorized for the selected store.

**Headers:** `Idempotency-Key` required.

**Content type:** `multipart/form-data`.

| Part | Required | Type | Description |
|---|---:|---|---|
| `file` | Yes | binary | File bytes. |
| `fileName` | No | string | Original filename; defaults to multipart filename. |
| `contentType` | No | enum | Target file-content type; inferred from MIME major component when omitted. |
| `path` | No | string | Target folder path; defaults to `/`. |

**Response:** `201 FileResponse`.

```json
{
  "fileName": "manual.pdf",
  "mimeType": "application/pdf",
  "contentType": "StaticFile",
  "path": "/",
  "provider": "default",
  "state": "AVAILABLE"
}
```

Generic file upload does not apply the file-manager-specific `validFileName(qqfilename)` rule, but the target still rejects path traversal, separators, blank names, and invalid folder paths.

Same-name writes in the same store/type/path namespace replace the existing provider object.

**Errors:** `400`, `401`, `403`, `409`, `422`, `501`, `503`.

**Driving rules:** BR-MER-022, BR-MER-024, BR-EXT-021, BR-EXT-022, BR-EXT-023.

### POST `/api/v1/private/content/images/add`

Uploads a file-manager image.

**Content type:** `multipart/form-data`.

| Part | Required | Type | Description |
|---|---:|---|---|
| `qqfile` | Yes | binary | File bytes. |
| `qquuid` | Yes | string | File-manager upload identifier. |
| `qqfilename` | Yes | string | Filename validated before storage. |
| `qqtotalfilesize` | No | integer | Declared total size. |
| `parentPath` | No | string | File-manager path context. The target validates it; legacy storage used the root path. |
| `qqpartindex` | No | integer | Chunk index. |
| `qqtotalparts` | No | integer | Total chunk count. |

**Response:** `201 FileStatusResponse`.

```json
{
  "success": true,
  "error": null,
  "preventRetry": true
}
```

The upload is rejected before the facade/storage call when filename validation fails.

Invalid filename response:

```json
{
  "success": false,
  "error": "Invalid filename",
  "preventRetry": true
}
```

**Errors:** `400`, `401`, `403`, `422 INVALID_FILENAME`, `501`, `503`.

**Driving rules:** BR-MER-022, BR-MER-023, BR-MER-024, BR-EXT-021, BR-EXT-022.

### POST `/api/v1/private/file`

Legacy single-file upload alias.

**Request:** multipart part `file`.

The target preserves the source MIME-based classification and store scope. The operation is not subject to `qqfilename` validation because the legacy generic upload did not perform that validation.

**Response:** `201 FileResponse`.

**Driving rules:** BR-MER-022, BR-MER-024, BR-EXT-021, BR-EXT-022.

### POST `/api/v1/private/files`

Legacy multi-file upload alias.

**Request:** multipart parts `file[]`.

Each file is independently classified and written to the selected store namespace.

**Response:** `201 FileBatchResponse`.

```json
{
  "items": [
    {
      "fileName": "one.png",
      "mimeType": "image/png",
      "contentType": "Image",
      "state": "AVAILABLE"
    }
  ]
}
```

**Driving rules:** BR-MER-022, BR-MER-024, BR-EXT-021, BR-EXT-022.

### GET `/api/v1/content/images/download`

Downloads an image by the legacy public path parameter.

**Query parameters:**

| Parameter | Required | Type | Description |
|---|---:|---|---|
| `path` | Yes | string | Image path. The basename identifies the image file. |

The operation is store-scoped through `x-store-id` and uses `contentType=IMAGE`.

**Response:** `200` with the file bytes and the stored `mimeType`.

The source controller returned `null`; the target closes that defect by returning bytes or an explicit error.

**Errors:** `400`, `403`, `404 FILE_NOT_FOUND`, `501 PROVIDER_CAPABILITY_UNSUPPORTED`, `503`.

**Driving rules:** BR-MER-027, BR-MER-024, BR-EXT-021, BR-EXT-022, BR-EXT-023.

### GET `/api/v1/private/content/files/{fileName}/download`

Downloads a typed content file.

**Query parameters:**

| Parameter | Required | Type | Description |
|---|---:|---|---|
| `contentType` | Yes | enum | File-content namespace. |
| `path` | No | string | Folder path; defaults to `/`. |

**Response:** `200` binary content with the stored MIME type.

A missing file is `404 FILE_NOT_FOUND`. A selected provider that cannot retrieve files returns `501 PROVIDER_CAPABILITY_UNSUPPORTED`.

**Driving rules:** BR-MER-024, BR-EXT-021, BR-EXT-022, BR-EXT-023.

### POST `/api/v1/private/content/images/rename`

Renames an image.

**Headers:** `Idempotency-Key` required.

**Request parameters:**

| Parameter | Required | Type |
|---|---:|---|
| `path` | Yes | string |
| `newName` | Yes | string |

The original image name is extracted from `path`. The target operation:

1. reads the original object;
2. retains its bytes, original MIME type, and `IMAGE` content type;
3. removes the original provider object;
4. recreates the object under `newName`.

The target must use an atomic provider operation where supported or expose a recoverable failure if recreation fails after removal.

**Response:** `200 FileStatusResponse`.

**Errors:** `400`, `401`, `403`, `404 FILE_NOT_FOUND`, `409`, `422`, `503`.

**Driving rules:** BR-MER-025, BR-EXT-021, BR-EXT-022, BR-EXT-029.

### POST `/api/v1/private/content/files/rename`

Renames a typed content file.

**Request body:**

```json
{
  "fileName": "hero.jpeg",
  "newName": "hero.bin",
  "contentType": "Image",
  "path": "/"
}
```

The original `contentType` and MIME metadata are retained. The new extension does not cause the target to infer a different content classification.

**Response:** `200 FileStatusResponse`.

**Errors:** `400`, `401`, `403`, `404`, `409`, `422`, `503`.

**Driving rules:** BR-MER-025, BR-EXT-021, BR-EXT-022, BR-EXT-029.

### DELETE `/api/v1/private/content/images/remove`

Deletes an image.

**Query parameters:**

| Parameter | Required | Type |
|---|---:|---|
| `path` | Yes | string |

The basename identifies the file in the `IMAGE` namespace.

**Response:** `204`.

Deletion is scoped to the selected tenant, store, provider namespace, and image type. Deleting an absent image is idempotent success.

**Errors:** `400`, `401`, `403`, `422`, `503`.

**Driving rules:** BR-MER-024, BR-EXT-021, BR-EXT-022, BR-EXT-030.

### DELETE `/api/v1/private/content/files/{fileName}`

Deletes one typed content file.

**Query parameters:** `contentType` required; `path` optional and defaults to `/`.

**Response:** `204`.

The operation affects exactly one store, content type, path, and filename. It does not affect the same filename in another store or namespace.

**Errors:** `400`, `401`, `403`, `422`, `503`.

**Driving rules:** BR-MER-024, BR-EXT-021, BR-EXT-022, BR-EXT-030.

## Folder endpoints

### POST `/api/v1/private/content/folders`

Creates a folder.

**Request body:**

```json
{
  "path": "/marketing",
  "folderName": "summer-2026"
}
```

| Field | Required | Description |
|---|---:|---|
| `path` | No | Parent path; defaults to `/`. |
| `folderName` | Yes | Single folder segment without `/`, `\`, or `..`. |

Valid paths are `/` or slash-prefixed segments containing only letters, digits, underscores, and hyphens. `/marketing/summer-2026` is valid; `/marketing/summer 2026` is invalid.

**Response:** `201 FolderResponse`.

Folder creation is idempotent for the same store and resulting path.

**Errors:** `400`, `401`, `403`, `422 INVALID_FOLDER_PATH`, `501`, `503`.

**Driving rules:** BR-MER-026, BR-EXT-021, BR-EXT-023.

### GET `/api/v1/private/content/folders`

Lists folders.

**Query parameter:** optional `path`, default `/`.

**Response:** `200 FolderListResponse`.

```json
{
  "path": "/marketing",
  "folders": [
    "/marketing/summer-2026"
  ]
}
```

If the selected provider does not implement folder listing, the response is `501 PROVIDER_CAPABILITY_UNSUPPORTED`; the endpoint must not return a successful fabricated list.

**Errors:** `400`, `401`, `403`, `501`, `503`.

**Driving rules:** BR-MER-026, BR-EXT-021, BR-EXT-023.

### DELETE `/api/v1/private/content/folders`

Removes a folder.

**Query parameters:**

| Parameter | Required | Type |
|---|---:|---|
| `path` | No | string |
| `folderName` | Yes | string |

**Response:** `204`.

The target must document whether provider removal is recursive. Because the source provider implementations are incomplete or provider-dependent, the target returns `501 PROVIDER_CAPABILITY_UNSUPPORTED` when recursive or portable removal is unavailable.

**Errors:** `400`, `401`, `403`, `404`, `422`, `501`, `503`.

**Driving rules:** BR-MER-026, BR-EXT-021, BR-EXT-023.

## Public and merchant configuration endpoints

### GET `/api/v1/config`

Returns the public configuration projection for the selected store.

**Auth:** Public.

**Response:** `200 PublicConfiguration`.

```json
{
  "facebook": "https://facebook.com/example",
  "pinterest": "https://pinterest.com/example",
  "ga": "G-ABCDE12345",
  "instagram": "https://instagram.com/example",
  "allowOnlinePurchase": true,
  "displaySearchBox": true,
  "displayContactUs": false,
  "displayShipping": false,
  "displayCustomerSection": false,
  "displayAddToCartOnFeaturedItems": false,
  "displayCustomerAgreement": false,
  "displayPagesMenu": true
}
```

The response includes only public fields:

- `allowOnlinePurchase`;
- `displaySearchBox`;
- `displayContactUs`;
- `displayShipping`;
- `displayCustomerSection`;
- `displayAddToCartOnFeaturedItems`;
- `displayCustomerAgreement`;
- `displayPagesMenu`;
- `facebook`;
- `ga`;
- `instagram`;
- `pinterest`.

Internal fields such as `testMode`, `debugMode`, search configuration paths, encrypted values, payment credentials, and shipping credentials are never returned.

Named social values are resolved from the selected store's dedicated configuration keys. Missing keys are omitted or represented as `null`; no value from another store is used.

`displayShipping` defaults to `false`. A nonblank platform property overrides the default when it parses as a boolean. A blank or malformed property leaves the value `false`.

When the store has no `CONFIG` merchant configuration record, the target returns an explicit platform-default projection with `200`; it does not return a null body and does not permit an unhandled null dereference.

**Errors:** `400`, `403`, `503 CONFIGURATION_UNAVAILABLE`, `500`.

**CRUD-only:** No. Projection, precedence, redaction, and absence policy apply.

**Driving rules:** BR-CF-003, BR-CF-004, BR-CF-005, BR-CF-015.

### GET `/api/v1/private/configuration`

Reads the typed merchant `CONFIG` payload.

**Auth:** Authenticated administrator authorized for the selected store.

**Response:** `200 MerchantConfigurationPayload`.

```json
{
  "displayCustomerSection": false,
  "displayContactUs": false,
  "displayStoreAddress": false,
  "displayAddToCartOnFeaturedItems": false,
  "displayCustomerAgreement": false,
  "displayPagesMenu": true,
  "allowPurchaseItems": true,
  "displaySearchBox": true,
  "testMode": false,
  "debugMode": false,
  "useDefaultSearchConfig": {
    "en": true
  },
  "defaultSearchConfigPath": {
    "en": "/search/default.json"
  }
}
```

Search configuration maps are keyed by language code. Null values are omitted. Blank search paths are omitted. Commerce-display flags are serialized as JSON booleans.

**Response:** `200 MerchantConfigurationPayload`.

**Errors:** `401`, `403`, `404 CONFIGURATION_NOT_FOUND`, `422 CONFIGURATION_PARSE_ERROR`, `500`.

**CRUD-only:** No. Typed serialization and store/key resolution apply.

**Driving rules:** BR-CF-001, BR-CF-002, BR-CF-015.

### PUT `/api/v1/private/configuration`

Saves the typed merchant `CONFIG` payload.

**Auth:** Authenticated administrator authorized for the selected store.

**Request body:** `MerchantConfigurationPayload`.

**Response:** `200 MerchantConfigurationPayload`.

The target serializes boolean flags as JSON booleans and omits null search-map values and blank search paths. The record is created when absent or updated when present, always under the selected store and key `CONFIG`.

**Errors:** `400`, `401`, `403`, `422`, `500`.

**Driving rules:** BR-CF-001, BR-CF-002.

### GET `/api/v1/private/configurations/{key}`

Reads one merchant configuration record.

**Response:** `200 MerchantConfigurationRecord`.

```json
{
  "id": "7f58d11e-b0a3-4e2e-9181-962a970f7965",
  "key": "PAYMENT_MODULES",
  "type": "INTEGRATION",
  "active": true,
  "value": null,
  "valueState": "ENCRYPTED"
}
```

Encrypted values are never returned as plaintext. `valueState` is `ENCRYPTED`, `PRESENT`, or `ABSENT`; sensitive payloads are write-only.

**Errors:** `401`, `403`, `404`, `500`.

**CRUD-only:** Yes for record retrieval; store/key isolation and redaction are mandatory.

**Driving rules:** BR-CF-001, BR-CF-006, BR-EXT-025.

### PUT `/api/v1/private/configurations/{key}`

Creates or replaces one merchant configuration record.

**Request body:**

```json
{
  "type": "INTEGRATION",
  "active": true,
  "value": {
    "moduleCode": "stripe",
    "environment": "Production",
    "integrationKeys": {
      "secretKey": "write-only-secret",
      "publishableKey": "pk_live_example"
    },
    "integrationOptions": {}
  }
}
```

The target serializes and encrypts sensitive integration payloads before persistence. The record identity is `(tenantId, storeId, key)`.

**Response:** `200 MerchantConfigurationRecord` with sensitive values redacted.

**Errors:** `400`, `401`, `403`, `409`, `422`, `500`.

**Driving rules:** BR-CF-001, BR-CF-006, BR-EXT-025.

## Payment and shipping module endpoints

Module discovery and configuration endpoints manage configuration state and metadata only. They do not charge payments, calculate shipping quotes, or invoke provider execution operations.

### GET `/api/v1/private/modules/payment`

Lists payment modules available to the selected store.

**Auth:** Authenticated administrator authorized for the selected store.

**Response:** `200 PaymentModuleSummary[]`.

```json
[
  {
    "code": "stripe",
    "active": false,
    "configured": true,
    "image": "stripe.png",
    "binaryImage": null,
    "requiredKeys": [],
    "configurable": "true"
  }
]
```

A module is available when its region set contains the store country ISO code or `*`. Persisted module metadata is hydrated with regions, display details, and environment configuration. Runtime payment starters are appended before the result is cached.

`configured=true` means a store configuration record exists for the module code. `active=true` is possible only when `configured=true` and the stored configuration is active.

The target invalidates or versions the affected family cache after module replacement.

**Errors:** `401`, `403`, `503`.

**CRUD-only:** No. Country filtering, runtime starter discovery, cache hydration, and configured/active projection apply.

**Driving rules:** BR-CF-011, BR-CF-012, BR-CF-014, BR-EXT-024, BR-EXT-026, BR-EXT-027.

### GET `/api/v1/private/modules/payment/{code}`

Reads payment module metadata and the selected store's configuration state.

**Response:** `200 PaymentModuleDetail`.

```json
{
  "code": "stripe",
  "configurable": "true",
  "active": true,
  "defaultSelected": true,
  "requiredKeys": [
    "secretKey",
    "publishableKey"
  ],
  "integrationKeys": {
    "secretKey": null,
    "publishableKey": "pk_live_example"
  },
  "integrationOptions": {},
  "environment": "Production",
  "secretsPresent": true
}
```

Sensitive values are masked or write-only. The encrypted merchant configuration value is never serialized.

An unavailable module or a module restricted from the selected store returns `404 MODULE_NOT_FOUND`.

**Errors:** `401`, `403`, `404`, `503`.

**Driving rules:** BR-CF-006, BR-CF-012, BR-CF-013, BR-CF-014, BR-EXT-027, BR-EXT-028.

### PUT `/api/v1/private/modules/payment/{code}`

Validates and saves payment module configuration state.

**Request body:**

```json
{
  "active": true,
  "defaultSelected": true,
  "integrationKeys": {
    "secretKey": "write-only-secret",
    "publishableKey": "pk_live_example"
  },
  "integrationOptions": {
    "captureMode": [
      "automatic"
    ]
  },
  "environment": "Production"
}
```

Processing order:

1. verify that the module code is available to the selected store;
2. invoke the MS-12 provider-validation boundary;
3. reject invalid keys or options without persistence;
4. merge the selected module into the store's existing payment configuration set;
5. serialize and encrypt the configuration set;
6. persist it under the store-scoped `PAYMENT_MODULES` configuration key.

This endpoint does not authorize, capture, or otherwise execute a payment.

**Response:** `200 PaymentModuleDetail` with sensitive values redacted.

**Errors:** `400`, `401`, `403`, `404 MODULE_NOT_FOUND`, `409`, `422 MODULE_CONFIGURATION_INVALID`, `503 PROVIDER_UNAVAILABLE`, `500`.

**Driving rules:** BR-CF-006, BR-CF-007, BR-CF-013, BR-EXT-025.

### GET `/api/v1/private/modules/shipping`

Lists shipping modules available to the selected store.

**Response:** `200 ShippingModuleSummary[]`.

The response uses `code`, `active`, `configured`, `image`, `requiredKeys`, and `configurable` fields. Availability uses the module's country region set or wildcard `*`.

Shipping discovery does not append payment starters.

**Errors:** `401`, `403`, `503`.

**CRUD-only:** No. Country filtering, cache hydration, and configured/active projection apply.

**Driving rules:** BR-CF-011, BR-CF-012, BR-CF-014, BR-EXT-026, BR-EXT-027.

### GET `/api/v1/private/modules/shipping/{code}`

Reads shipping module metadata and the selected store's configuration.

**Response:** `200 ShippingModuleDetail`.

The response includes active/default-selection state, module code, environment, integration options, and masked configuration-key state. It never exposes encrypted merchant configuration or provider credentials in plaintext.

**Errors:** `401`, `403`, `404 MODULE_NOT_FOUND`, `503`.

**Driving rules:** BR-CF-006, BR-CF-012, BR-CF-013, BR-CF-014, BR-EXT-027, BR-EXT-028.

### PUT `/api/v1/private/modules/shipping/{code}`

Validates and saves shipping module configuration state.

**Request body:** Same structure as payment module configuration, with shipping-provider keys and options.

Processing invokes the MS-12 shipping provider-validation boundary before persisting encrypted configuration state under `SHIPPING_MODULES`. The endpoint does not calculate a shipping quote or execute a carrier operation.

**Response:** `200 ShippingModuleDetail` with sensitive values redacted.

**Errors:** `400`, `401`, `403`, `404 MODULE_NOT_FOUND`, `409`, `422 MODULE_CONFIGURATION_INVALID`, `503`, `500`.

**Driving rules:** BR-CF-006, BR-CF-007, BR-CF-013, BR-EXT-025.

## Global integration-module definition endpoint

### POST `/api/v1/services/private/system/module`

Replaces a global integration-module definition by module code.

**Auth:** Platform administrator.

**Headers:** `x-tenant-id`, `x-correlation-id`, `Authorization`, and `Idempotency-Key`. `x-store-id` is optional.

**Legacy request media type:** `text/plain;charset=UTF-8` containing a JSON object. The target also accepts `application/json`.

**Request body:**

```json
{
  "module": "PAYMENT",
  "code": "paypal-express-checkout",
  "type": "paypal",
  "image": "icon-paypal.png",
  "customModule": false,
  "regions": [
    "*"
  ],
  "details": {
    "displayName": "PayPal Express Checkout"
  },
  "configuration": [
    {
      "env": "Test",
      "scheme": "https",
      "host": "sandbox.paypal.com",
      "port": "443",
      "uri": "/checkout",
      "config1": "test-url",
      "config2": "test-token"
    },
    {
      "env": "Prod",
      "scheme": "https",
      "host": "paypal.com",
      "port": "443",
      "uri": "/checkout",
      "config1": "production-url",
      "config2": "production-token"
    }
  ]
}
```

| Field | Required | Description |
|---|---:|---|
| `module` | Yes | Module family, for example `PAYMENT` or `SHIPPING`. |
| `code` | Yes | Replacement identity. |
| `type` | No | Provider/module type. |
| `image` | No | Display image path. |
| `customModule` | No | Boolean custom-module indicator. String boolean values are accepted for legacy compatibility. |
| `regions` | No | Country ISO codes or wildcard `*`. |
| `details` | No | Arbitrary display metadata. |
| `configuration` | No | Environment-specific connection metadata. |
| `configuration[].env` | Yes when an environment entry exists | `Test`, `Prod`, or compatibility alias `Production`. |
| `configuration[].scheme` | No | Descriptive connection scheme. |
| `configuration[].host` | No | Descriptive provider host. |
| `configuration[].port` | No | Descriptive provider port. |
| `configuration[].uri` | No | Descriptive provider URI. |
| `configuration[].config1` | No | First environment-specific configuration value. |
| `configuration[].config2` | No | Second environment-specific configuration value. |

The operation:

1. parses the JSON definition;
2. loads the module metadata;
3. finds an existing record by `code`;
4. deletes the existing record only when the code matches;
5. creates the replacement record;
6. invalidates or versions the affected payment/shipping discovery cache.

A module with a different code is not replaced even when it belongs to the same module family.

`config1` and `config2` remain distinct fields. The target does not reproduce the legacy discovery defect that assigned persisted `config2` into `config1`.

This endpoint stores metadata only. It does not execute a payment or shipping provider call.

**Response:** `200 ModuleReplacementResponse`.

```json
{
  "status": 200,
  "code": "paypal-express-checkout",
  "replaced": true,
  "cacheInvalidated": true
}
```

**Errors:** `400`, `401`, `403`, `409`, `422`, `503`, `500`.

**Driving rules:** BR-CF-008, BR-CF-009, BR-CF-010, BR-EXT-026, BR-EXT-028.

## Deprecated and explicitly nonfunctional legacy operations

The following source operations are documented so that migration clients do not mistake a legacy null or empty response for a valid target contract.

### Deprecated active compatibility reads

| Legacy route | Target behavior | Response | Rules |
|---|---|---|---|
| `GET /api/v1/private/content/any/{code}` | Return `DeprecatedContentFull` containing `id`, `code`, `contentType`, `visible`, `displayedInMenu`, and all descriptions. | `200` | BR-MER-016, BR-MER-018 |
| `GET /api/v1/private/contents/any` | Return a list of deprecated full content projections for page, box, and section items. | `200 DeprecatedContentFull[]` | BR-MER-016, BR-MER-019 |
| `GET /api/v1/content/boxes/{code}` | Retained as the public box code read described above. | `200 ContentItem` | BR-MER-016, BR-MER-018, BR-MER-020 |

### Retired no-op or defective operations

| Legacy route | Source-proven behavior | Target behavior |
|---|---|---|
| `GET /api/v1/content/summary` | Deprecated method returns `null`. | `410 LEGACY_OPERATION_RETIRED`; no null success body. |
| `DELETE /api/v1/content/folder` | Misnamed folder-create method has an empty implementation and returns `201`. | `410 LEGACY_OPERATION_RETIRED`; use `POST /private/content/folders`. |
| `PUT /api/v1/private/content/{id}` | Deprecated method sets the request identifier and performs no persistence. | `410 LEGACY_OPERATION_RETIRED`; use typed page or box PUT. |
| `GET /api/v1/content/images/download` | Source extracts a filename and returns `null`. | Route retained and fixed to return bytes or `404`/`503`. |
| `POST /api/v1/private/configurations/payment` | Source returns `null` and performs no configuration action. | `410 LEGACY_OPERATION_RETIRED`; use `PUT /private/modules/payment/{code}`. |
| `GET /api/v1/private/configurations/payment` | Source returns `null`. | `410 LEGACY_OPERATION_RETIRED`; use `GET /private/modules/payment`. |
| `GET /api/v1/private/configurations/shipping` | Source returns `null`. | `410 LEGACY_OPERATION_RETIRED`; use `GET /private/modules/shipping`. |
| `POST /api/v1/private/configurations/shipping` | No implemented source operation is exposed. | `410 LEGACY_OPERATION_RETIRED`; use `PUT /private/modules/shipping/{code}`. |
| `POST /api/v1/services/private/system/optin` | Source returns a successful status without creating an opt-in. | `410 LEGACY_OPERATION_RETIRED`. |
| `DELETE /api/v1/services/private/system/optin/{code}` | Source returns a successful status without deleting an opt-in. | `410 LEGACY_OPERATION_RETIRED`. |
| `POST /api/v1/services/private/system/optin/{code}/customer` | Source returns a successful status without creating a customer opt-in. | `410 LEGACY_OPERATION_RETIRED`. |

Every retired operation returns:

```json
{
  "error": "LEGACY_OPERATION_RETIRED",
  "message": "This legacy operation was explicitly nonfunctional and is not part of the target contract.",
  "statusCode": 410,
  "timestamp": "2026-09-01T18:45:13.939+04:00",
  "correlationId": "9f9b4bc7-86c7-4db7-8590-2270e84d70b8"
}
```

## Source-to-operation coverage

| Source component | Covered operations |
|---|---|
| `ContentApi` | Page and box reads, creates, updates, deletes, existence checks, generic uploads, deprecated content update/delete, image listing, folder compatibility route. |
| `ContentAdministrationApi` | File-manager listing, folder projection, image upload, image rename, image removal, defective download route. |
| `ContentFacadeImpl` | Store-scoped content conversion, type assignment, localized description construction, CDATA box formatting, friendly-URL lookup, file classification, rename orchestration, download delegation. |
| `ContentServiceImpl` | Content persistence, language/type queries, MIME-based file classification, provider delegation, folder path validation, file namespace operations. |
| `ContentRepository` and `PageContentRepository` | Store/type/language filtering, ascending `sortOrder`, pagination, code and friendly-URL lookup. |
| `PublicConfigsApi` and `MerchantConfigurationFacadeImpl` | Public configuration projection, social-key lookup, platform shipping-display precedence, explicit missing-configuration policy. |
| `ConfigurationsApi` | Explicitly retired payment/shipping configuration no-op routes. |
| `PaymentApi` | Payment module discovery, summary/detail projections, configured/active flags, configuration mutation boundary. |
| `ShippingConfigurationApi` | Shipping module discovery, summary/detail projections, configuration mutation boundary. |
| `ModuleConfigurationServiceImpl` | Module cache lookup, metadata hydration, runtime payment starters, module replacement by code. |
| `IntegrationModulesLoader` | Module metadata, regions, details, TEST/PROD environment entries, `config1`/`config2` preservation. |
| `StaticContentFileManager` and provider-neutral content managers | Provider selection, upload, retrieval, listing, removal, rename, and folder capability boundary. |

## API-to-rule coverage

Every non-CRUD business operation has one or more driving rules:

- CMS identity, type, localization, visibility, ordering, and ownership: BR-MER-013 through BR-MER-021.
- File classification, validation, isolation, rename, folders, download, and provider behavior: BR-MER-022 through BR-MER-028 and BR-EXT-021 through BR-EXT-023, BR-EXT-029, and BR-EXT-030.
- Merchant configuration and public projection: BR-CF-001 through BR-CF-007 and BR-CF-015.
- Module metadata, discovery, validation, cache, redaction, and environment configuration: BR-CF-008 through BR-CF-014 and BR-EXT-024 through BR-EXT-028.

Explicitly CRUD-only operations are limited to standard store-scoped merchant-configuration record retrieval and content existence reads. All other target endpoints apply store/type/language policy, serialization, validation, visibility, provider capability, authorization, caching, or cross-service validation behavior.