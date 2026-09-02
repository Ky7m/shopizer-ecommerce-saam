# Merchant and Store Administration — API Design

**Base path:** `/api/v1`  
**JSON:** camelCase; paths: kebab-case  
**Global context:** `x-tenant-id`, `x-store-id`, `x-correlation-id`; administrator bearer token is required for private operations.

## Store lifecycle and lookup

### POST `/stores`
- **Purpose:** Create a store with validated identity, contact data, references, hierarchy, language, and measurement defaults.
- **Auth:** administrator with tenant-level store-management permission.
- **Response:** `201 Store`
- **Driving rules:** BR-MER-001..004, BR-MER-007, BR-MER-012, BR-MSA-VAL-001, BR-MSA-VAL-003, BR-MSA-LANG-001

### GET `/stores/{storeCode}`
- **Purpose:** Read one store in the active tenant.
- **Auth:** public when context permits; private metadata is protected.
- **Response:** `200 Store`
- **Driving rules:** BR-MER-010..012, BR-UI-007

### PUT `/stores/{storeCode}`
- **Purpose:** Merge editable store metadata while preserving immutable identity.
- **Auth:** authorized administrator.
- **Response:** `200 Store`
- **Driving rules:** BR-MER-002, BR-MER-005, BR-MER-011, BR-MER-012, BR-MSA-VAL-002, BR-MSA-VAL-003, BR-MSA-LANG-001

### DELETE `/stores/{storeCode}`
- **Purpose:** Delete a store according to its child-store policy.
- **Auth:** authorized administrator.
- **Response:** `204`
- **Driving rules:** BR-MER-006, BR-MER-009, BR-MER-011

### GET `/stores/uniqueness`
- **Purpose:** Check whether a store code exists in the active tenant.
- **Auth:** authorized administrator.
- **Response:** `200 EntityExistsResponse`
- **Driving rules:** BR-MER-003, BR-MSA-VAL-001

### GET `/stores`
- **Purpose:** Return a paginated, deterministically ordered store collection.
- **Auth:** authorized administrator.
- **Response:** `200 StoreListResponse`
- **Driving rules:** BR-MSA-READ-001, BR-MER-010..012

### GET `/stores/names`
- **Purpose:** Return authorized code/name pairs for selectors.
- **Auth:** authorized administrator.
- **Response:** `200 StoreNameListResponse`
- **Driving rules:** BR-MSA-LST-001, BR-UI-007

## Hierarchy and language

### GET `/merchants/{merchantCode}/stores`
- **Purpose:** List stores belonging to an authorized merchant hierarchy.
- **Auth:** authorized retailer administrator.
- **Response:** `200 StoreListResponse`
- **Driving rules:** BR-MER-008, BR-MSA-AUTH-001, BR-MSA-READ-001

### GET `/merchants/{merchantCode}/children`
- **Purpose:** List descendants of a retailer store.
- **Auth:** authorized retailer administrator.
- **Response:** `200 StoreListResponse`
- **Driving rules:** BR-MER-008, BR-MER-009, BR-MSA-AUTH-001

### GET `/stores/{storeCode}/languages`
- **Purpose:** List supported languages for one store.
- **Auth:** public read or authorized administrator.
- **Response:** `200 LanguageListResponse`
- **Driving rules:** BR-MER-012, BR-MSA-LANG-001

### PUT `/stores/{storeCode}/languages`
- **Purpose:** Replace supported languages while retaining a valid default.
- **Auth:** authorized administrator.
- **Response:** `200 Store`
- **Driving rules:** BR-MER-012, BR-MSA-LANG-001, BR-MER-011

## Branding metadata and provider boundary

### GET `/stores/{storeCode}/branding`
- **Purpose:** Read store template and logo metadata.
- **Auth:** public read or authorized administrator.
- **Response:** `200 Branding`
- **Driving rules:** BR-MSA-BRD-001, BR-UI-007

### PUT `/stores/{storeCode}/branding`
- **Purpose:** Update store-scoped branding metadata.
- **Auth:** authorized administrator.
- **Response:** `200 Branding`
- **Driving rules:** BR-MSA-BRD-001, BR-MER-011

### POST `/stores/{storeCode}/branding/logo`
- **Purpose:** Store logo bytes through the configured file-provider boundary and persist its URI.
- **Auth:** authorized administrator.
- **Response:** `201 Branding`
- **Driving rules:** BR-MSA-BRD-001, BR-MER-011

### DELETE `/stores/{storeCode}/branding/logo`
- **Purpose:** Remove the store logo through the configured file-provider boundary.
- **Auth:** authorized administrator.
- **Response:** `204`
- **Driving rules:** BR-MSA-BRD-001, BR-MER-011

## Compatibility operations

### POST `/stores/signup`
- **Purpose:** Create a store signup request and issue a verification token.
- **Auth:** anonymous.
- **Response:** `202 SignupResponse`
- **Driving rules:** BR-MSA-VAL-003, BR-MER-001..004

### GET `/stores/{storeCode}/signup/{token}`
- **Purpose:** Verify a store signup token before activation.
- **Auth:** anonymous with token.
- **Response:** `200 SignupVerificationResponse`
- **Driving rules:** BR-MSA-VAL-003

## API-to-rule coverage

All non-CRUD operations have driving rules. `GET /stores/{storeCode}` is a standard scoped read but retains context rules; no endpoint is intentionally left without a rule or explicit CRUD classification.

## Events

### Published

`StoreCreated` is published after the store and store-language transaction commits. Its payload
is defined by `spec/shared/event-schemas/store-created.yaml`. `StoreConfigured` is a retired
sequence alias and is not published.

## Phase 4b inferred lifecycle clarifications

- `[Inferred in Phase 4b — Mode A]` Child-store creation requires an active parent; deleting a
  protected default store or a store with active children returns `409`.
- `[Inferred in Phase 4b — Mode A]` Language updates are idempotent and reject a request that
  removes the current default language from the supported set.
- `[Inferred in Phase 4b — Mode A]` Signup verification tokens are single-use, store-bound,
  valid for 24 hours, and return `410` after expiry or consumption.
