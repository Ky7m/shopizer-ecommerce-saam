# Merchant and Store Administration — Business Rules

**Service ID:** MS-10  
**Extraction mode:** Hybrid (CAST transaction bounds + direct Java source read)  
**Scope:** Merchant/store identity, hierarchy, store context, defaults, supported languages, branding metadata, and store-scoped administration. CMS content, configuration, and binary file providers remain MS-11 or external dependencies.

## Carry-forward rules

### BR-MER-001: Store code format and presence
**Source Reference:** `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/merchant/MerchantStore.java:93-95`; `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/store/facade/StoreFacadeImpl.java:174-187`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transactions `244225` and `244233`

**Statement:** A store identifier is mandatory, uses only letters, digits, and underscores, and is at most 100 characters long.
**Intent:** Validation
**Logic:** Reject blank or overlong codes; reject characters outside `[A-Za-z0-9_]`; otherwise persist the supplied code.
**Data Dependencies:** Reads/writes `MERCHANT_STORE.STORE_CODE`.
**Side Effects:** Calls store uniqueness lookup.
**Concrete Example:** Success `POST /api/v1/stores {"code":"north_01"}` → `201 {"code":"north_01"}`. Error `{"code":"north-america"}` → `422 VALIDATION_ERROR`.
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 5 | 5 | OK |
| Data-flow | 2 | 2 | OK |
| Constants | 2 | 2 | OK |
| State transitions | 0 | 0 | N/A |
| Outcomes | 2 | 2 | OK |
| Data writes | 1 | 1 | OK |
| Integrations | 0 | 0 | N/A |
| Error paths | 2 | 2 | OK |

### BR-MER-002: Store identity and contact validation
**Source Reference:** `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/merchant/MerchantStore.java:89-114`; `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/populator/store/PersistableMerchantStorePopulator.java:80-85,135-148`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transactions `244225` and `244226`

**Statement:** A store must have a name, phone, city, postal code, syntactically valid email, and a resolvable country before persistence.
**Intent:** Validation
**Logic:** Validate required contact fields; resolve `request.address.country` through the country reference service; map resolved country and contact values to the store.
**Data Dependencies:** Reads `MERCHANT_STORE.*`, `COUNTRY.ID`; writes store contact and country fields.
**Side Effects:** Calls shared country reference service.
**Concrete Example:** Success `POST /api/v1/stores` with `{"name":"Toronto","email":"ops@example.com","phone":"+14165550199","address":{"city":"Toronto","postalCode":"M5E1W7","country":"CA"}}` → `201`. Error country `ZZ` → `422 COUNTRY_NOT_FOUND`.
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 7 | 7 | OK |
| Data-flow | 9 | 9 | OK |
| Constants | 3 | 3 | OK |
| State transitions | 0 | 0 | N/A |
| Outcomes | 2 | 2 | OK |
| Data writes | 7 | 7 | OK |
| Integrations | 1 | 1 | OK |
| Error paths | 3 | 3 | OK |

### BR-MER-003: Store-code uniqueness
**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/store/facade/StoreFacadeImpl.java:174-187`; `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/merchant/MerchantRepository.java:30-32,38-39`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transactions `244233` and `244225`

**Statement:** A store code identifies at most one store within a tenant; a duplicate create request is rejected.
**Intent:** Validation
**Logic:** Query by code before creation and reject an existing result; retain a database unique constraint for race-free enforcement.
**Data Dependencies:** Reads/writes `MERCHANT_STORE.STORE_CODE`.
**Side Effects:** Performs a uniqueness query.
**Concrete Example:** `GET /api/v1/stores/uniqueness?code=toronto_01` → `200 {"exists":true}`; a second create → `409 STORE_CODE_CONFLICT`.
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 3 | 3 | OK |
| Data-flow | 2 | 2 | OK |
| Constants | 1 | 1 | OK |
| State transitions | 0 | 0 | N/A |
| Outcomes | 3 | 3 | OK |
| Data writes | 1 | 1 | OK |
| Integrations | 0 | 0 | N/A |
| Error paths | 1 | 1 | OK |

### BR-MER-004: New stores receive measurement defaults
**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/populator/store/PersistableMerchantStorePopulator.java:101-122`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transaction `244225`

**Statement:** When a new store omits measurement settings, the system assigns the platform defaults for dimension and weight units.
**Intent:** Validation
**Logic:** If dimension or weight is absent, resolve the configured default unit and assign it before persistence; preserve explicit valid values.
**Data Dependencies:** Reads store request and unit reference data; writes `MERCHANT_STORE.DIMENSION` and `MERCHANT_STORE.WEIGHT`.
**Side Effects:** Calls shared unit/reference services.
**Concrete Example:** Create without units → `201` with `{"dimension":"CM","weight":"KG"}`; unsupported unit → `422 UNSUPPORTED_UNIT`.
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 4 | 4 | OK |
| Data-flow | 4 | 4 | OK |
| Constants | 2 | 2 | OK |
| State transitions | 0 | 0 | N/A |
| Outcomes | 2 | 2 | OK |
| Data writes | 2 | 2 | OK |
| Integrations | 1 | 1 | OK |
| Error paths | 1 | 1 | OK |

### BR-MER-005: Store updates merge into the existing store
**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/store/facade/StoreFacadeImpl.java:215-248`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transaction `244226`

**Statement:** A store modification changes the identified store while preserving fields not supplied by the request.
**Intent:** State Transition
**Logic:** Load the store by identifier, map supplied editable fields onto the existing entity, validate the merged result, then save it.
**Data Dependencies:** Reads/writes `MERCHANT_STORE` identity, contact, language, currency, and branding fields.
**Side Effects:** Emits a store-updated integration event in the target.
**Concrete Example:** `PUT /api/v1/stores/toronto_01 {"phone":"+14165550111"}` → `200` with the prior name retained; unknown store → `404 STORE_NOT_FOUND`.
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 6 | 6 | OK |
| Data-flow | 8 | 8 | OK |
| Constants | 1 | 1 | OK |
| State transitions | 1 | 1 | OK |
| Outcomes | 2 | 2 | OK |
| Data writes | 4 | 4 | OK |
| Integrations | 0 | 1 | GAP |
| Error paths | 2 | 2 | OK |

### BR-MER-006: The default store cannot be deleted
**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/store/facade/StoreFacadeImpl.java:283-298`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transaction `244234`

**Statement:** The platform’s designated default store is protected from deletion.
**Intent:** Compliance
**Logic:** Compare the target code with the configured default-store code; reject deletion when they match, otherwise delete the store.
**Data Dependencies:** Reads `MERCHANT_STORE.STORE_CODE` and default-store configuration; writes/deletes store record.
**Side Effects:** May cascade child-store deletion after authorization.
**Concrete Example:** `DELETE /api/v1/stores/DEFAULT` → `409 DEFAULT_STORE_PROTECTED`; deleting `north_01` → `204`.
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 3 | 3 | OK |
| Data-flow | 2 | 2 | OK |
| Constants | 1 | 1 | OK |
| State transitions | 1 | 1 | OK |
| Outcomes | 2 | 2 | OK |
| Data writes | 1 | 1 | OK |
| Integrations | 0 | 0 | N/A |
| Error paths | 1 | 1 | OK |

### BR-MER-007: Parent stores must exist and cannot be themselves
**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/populator/store/PersistableMerchantStorePopulator.java:126-150`; `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/store/facade/StoreFacadeImpl.java:188-214`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transactions `244225` and `244226`

**Statement:** A child store may reference only an existing different parent store.
**Intent:** Validation
**Logic:** Resolve the requested parent; reject a missing parent or a parent whose identifier equals the child identifier; persist the hierarchy link only after validation.
**Data Dependencies:** Reads/writes `MERCHANT_STORE.PARENT_ID`.
**Side Effects:** Calls store lookup.
**Concrete Example:** `POST /api/v1/stores {"code":"toronto_02","parentCode":"retail_group"}` → `201`; `parentCode:"toronto_02"` → `422 INVALID_PARENT`.
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 5 | 5 | OK |
| Data-flow | 3 | 3 | OK |
| Constants | 0 | 0 | N/A |
| State transitions | 1 | 1 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 1 | 1 | OK |
| Integrations | 0 | 0 | N/A |
| Error paths | 2 | 2 | OK |

### BR-MER-008: Child retrieval requires retailer status
**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/merchant/MerchantStoreServiceImpl.java:55-72,137-166`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transaction `244228`

**Statement:** Only a retailer store may expose its child-store collection.
**Intent:** Authorization
**Logic:** Load the requested store; return children only when its retailer flag is enabled; otherwise reject the hierarchy query.
**Data Dependencies:** Reads `MERCHANT_STORE.RETAILER` and `MERCHANT_STORE.PARENT_ID`.
**Side Effects:** None.
**Concrete Example:** `GET /api/v1/merchants/retail_group/children` → `200 {"items":[...]}`; a non-retailer store → `403 RETAILER_REQUIRED`.
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 4 | 4 | OK |
| Data-flow | 4 | 4 | OK |
| Constants | 0 | 0 | N/A |
| State transitions | 0 | 0 | N/A |
| Outcomes | 2 | 2 | OK |
| Data writes | 0 | 0 | N/A |
| Integrations | 0 | 0 | N/A |
| Error paths | 1 | 1 | OK |

### BR-MER-009: Parent deletion handles child stores consistently
**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/model/store/MerchantStoreEntity.java:20-48`; `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/store/facade/StoreFacadeImpl.java:283-298`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transaction `244234`

**Statement:** Deleting a parent store must not leave child stores orphaned; the configured deletion policy is applied atomically.
**Intent:** State Transition
**Logic:** Resolve children before parent deletion and either cascade the deletion or reject the parent deletion according to the target policy; never commit a dangling parent reference.
**Data Dependencies:** Reads/writes `MERCHANT_STORE.ID` and `MERCHANT_STORE.PARENT_ID`.
**Side Effects:** May delete multiple store rows in one transaction.
**Concrete Example:** Deleting `retail_group` with children → `204` and all child rows removed under cascade policy; restrictive policy → `409 CHILD_STORES_EXIST`.
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 5 | 5 | OK |
| Data-flow | 4 | 4 | OK |
| Constants | 0 | 0 | N/A |
| State transitions | 2 | 2 | OK |
| Outcomes | 2 | 2 | OK |
| Data writes | 2 | 2 | OK |
| Integrations | 0 | 0 | N/A |
| Error paths | 1 | 1 | OK |

### BR-MER-010: Missing store context defaults to the default store
**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/application/config/MerchantStoreArgumentResolver.java:43-64`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transactions `244219`, `244220`, and `244224`

**Statement:** A request without an explicit store context is resolved against the configured default store.
**Intent:** Routing
**Logic:** Read the request store parameter; when absent, resolve the configured default code and load that store; reject when the configured default cannot be found.
**Data Dependencies:** Reads request context and `MERCHANT_STORE.STORE_CODE`.
**Side Effects:** None.
**Concrete Example:** `GET /api/v1/store` without `x-store-id` → default store response; invalid explicit store → `404 STORE_NOT_FOUND`.
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 4 | 4 | OK |
| Data-flow | 3 | 3 | OK |
| Constants | 1 | 1 | OK |
| State transitions | 0 | 0 | N/A |
| Outcomes | 2 | 2 | OK |
| Data writes | 0 | 0 | N/A |
| Integrations | 0 | 0 | N/A |
| Error paths | 1 | 1 | OK |

### BR-MER-011: Store context is authorized against the request URI
**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/application/config/MerchantStoreArgumentResolver.java:65-93`; `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/facade/user/UserFacadeImpl.java:78-118`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transactions `244220`, `244226`, and `244234`

**Statement:** An authenticated administrator may operate only on a store permitted by the request context and the administrator’s store hierarchy.
**Intent:** Authorization
**Logic:** Resolve the target store from URI/context; load the authenticated administrator’s permitted stores; reject a target outside that set.
**Data Dependencies:** Reads administrator/store membership and `MERCHANT_STORE` hierarchy.
**Side Effects:** Security rejection is audited.
**Concrete Example:** Admin scoped to `north_01` calling `PUT /api/v1/stores/south_01` → `403 STORE_ACCESS_DENIED`; permitted store → `200`.
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 8 | 8 | OK |
| Data-flow | 7 | 7 | OK |
| Constants | 0 | 0 | N/A |
| State transitions | 0 | 0 | N/A |
| Outcomes | 2 | 2 | OK |
| Data writes | 0 | 1 | GAP |
| Integrations | 0 | 0 | N/A |
| Error paths | 2 | 2 | OK |

### BR-MER-012: Language resolution falls back through store and system defaults
**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/populator/store/ReadableMerchantStorePopulator.java:58-100`; `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/merchant/MerchantStoreServiceImpl.java:95-123`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transactions `244219` and `244224`

**Statement:** A store’s effective language is the requested supported language, otherwise its configured default, otherwise the platform default.
**Intent:** Routing
**Logic:** Accept a requested language only when associated with the store; fall back to store default and then system default; reject an unsupported explicit language.
**Data Dependencies:** Reads store-language associations and `LANGUAGE.CODE`; writes no store data during reads.
**Side Effects:** Calls shared language reference service.
**Concrete Example:** `GET /api/v1/stores/north_01?language=fr` → French when supported; `language=xx` → `422 UNSUPPORTED_LANGUAGE`.
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 6 | 6 | OK |
| Data-flow | 5 | 5 | OK |
| Constants | 1 | 1 | OK |
| State transitions | 0 | 0 | N/A |
| Outcomes | 3 | 3 | OK |
| Data writes | 0 | 0 | N/A |
| Integrations | 1 | 1 | OK |
| Error paths | 1 | 1 | OK |

### BR-UI-007: Store context controls administration screens
**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/application/config/MerchantStoreArgumentResolver.java:43-93`; `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/store/MerchantStoreApi.java:70-180`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)  
**CAST Reference:** Transactions `244219`, `244220`, and `244226`

**Statement:** Store administration screens and API calls display and modify only the store selected by the active store context.
**Intent:** Authorization
**Logic:** Resolve the active store before controller execution and bind all read/write operations to that resolved store.
**Data Dependencies:** Reads request context and store identity; writes only the selected store.
**Side Effects:** None.
**Concrete Example:** Admin UI selected `north_01` cannot display `south_01` data; a matching context returns `200`.
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 4 | 4 | OK |
| Data-flow | 4 | 4 | OK |
| Constants | 0 | 0 | N/A |
| State transitions | 0 | 0 | N/A |
| Outcomes | 2 | 2 | OK |
| Data writes | 1 | 1 | OK |
| Integrations | 0 | 0 | N/A |
| Error paths | 1 | 1 | OK |

## Net-new rules from the Phase 4 deep read

### BR-MSA-VAL-001: Store names are normalized before uniqueness checks
**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/store/facade/StoreFacadeImpl.java:174-214`
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)
**CAST Reference:** Transactions `244225` and `244233`

**Statement:** Store codes are normalized consistently before duplicate detection so equivalent identifiers cannot bypass uniqueness.
**Intent:** Validation
**Logic:** Trim the submitted code, apply the target case policy, then perform lookup and persistence using the normalized value.
**Data Dependencies:** Reads/writes `MERCHANT_STORE.STORE_CODE`.
**Side Effects:** None.
**Concrete Example:** `north_01` and ` NORTH_01 ` follow the configured case policy and cannot create two equivalent stores; duplicate → `409`.
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 3 | 3 | OK |
| Data-flow | 2 | 2 | OK |
| Constants | 1 | 1 | OK |
| State transitions | 0 | 0 | N/A |
| Outcomes | 2 | 2 | OK |
| Data writes | 1 | 1 | OK |
| Integrations | 0 | 0 | N/A |
| Error paths | 1 | 1 | OK |

### BR-MSA-VAL-002: Store updates preserve immutable identity
**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/store/facade/StoreFacadeImpl.java:215-248`; `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/populator/store/PersistableMerchantStorePopulator.java:80-150`
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)
**CAST Reference:** Transaction `244226`

**Statement:** A store modification cannot silently change its identity or move it to another tenant; identity changes use a separate controlled operation.
**Intent:** Compliance
**Logic:** Load the existing store, ignore or reject immutable code/tenant changes, merge editable metadata, and save under the original identity.
**Data Dependencies:** Reads/writes `MERCHANT_STORE.ID`, `STORE_CODE`, and tenant/store ownership.
**Side Effects:** Emits an audit record for rejected identity changes.
**Concrete Example:** `PUT /api/v1/stores/north_01 {"code":"south_01"}` → `409 STORE_IDENTITY_IMMUTABLE`; phone-only change → `200`.
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 5 | 5 | OK |
| Data-flow | 5 | 5 | OK |
| Constants | 0 | 0 | N/A |
| State transitions | 1 | 1 | OK |
| Outcomes | 2 | 2 | OK |
| Data writes | 2 | 2 | OK |
| Integrations | 0 | 0 | N/A |
| Error paths | 1 | 1 | OK |

### BR-MSA-VAL-003: Store creation is transactional across defaults and hierarchy
**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/store/facade/StoreFacadeImpl.java:174-214`; `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/populator/store/PersistableMerchantStorePopulator.java:80-150`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)
**CAST Reference:** Transaction `244225`

**Statement:** A store is created only when identity, references, hierarchy, and defaults all validate; a failure leaves no partially created store.
**Intent:** State Transition
**Logic:** Validate code/contact, resolve country/language/units/parent, persist the store and language links in one transaction, roll back on any service exception.
**Data Dependencies:** Reads reference data and `MERCHANT_STORE`; writes store and store-language rows.
**Side Effects:** Publishes `StoreCreated` after commit.
**Concrete Example:** Invalid country during create → `422` and no store row; valid request → `201` plus `StoreCreated`.
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 9 | 9 | OK |
| Data-flow | 8 | 8 | OK |
| Constants | 1 | 1 | OK |
| State transitions | 2 | 2 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 3 | 3 | OK |
| Integrations | 2 | 2 | OK |
| Error paths | 3 | 3 | OK |

### BR-MSA-READ-001: Store collections are paginated and deterministically ordered
**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/merchant/MerchantStoreServiceImpl.java:95-166`; `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/merchant/PageableMerchantRepository.java:1-80`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)
**CAST Reference:** Transactions `244221`, `244222`, and `244223`

**Statement:** Store collection endpoints return bounded pages with stable ordering and report the total matching store count.
**Intent:** Calculation
**Logic:** Apply tenant/store hierarchy criteria, create a page request from `page` and `pageSize`, order by normalized store code, and return items plus pagination metadata.
**Data Dependencies:** Reads `MERCHANT_STORE` identity and hierarchy fields.
**Side Effects:** None.
**Concrete Example:** `GET /api/v1/stores?page=1&pageSize=20` → `200 {"items":[...],"pagination":{"page":1,"pageSize":20,"totalItems":42,"totalPages":3}}`.
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 6 | 6 | OK |
| Data-flow | 6 | 6 | OK |
| Constants | 2 | 2 | OK |
| State transitions | 0 | 0 | N/A |
| Outcomes | 2 | 2 | OK |
| Data writes | 0 | 0 | N/A |
| Integrations | 0 | 0 | N/A |
| Error paths | 1 | 1 | OK |

### BR-MSA-AUTH-001: Retailer hierarchy expansion is bounded to authorized stores
**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/merchant/MerchantStoreServiceImpl.java:55-72,137-166`; `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/facade/user/UserFacadeImpl.java:78-118`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)
**CAST Reference:** Transactions `244228` and `244221`

**Statement:** Retailer hierarchy queries include only descendants that the requesting administrator is authorized to administer.
**Intent:** Authorization
**Logic:** Resolve the retailer root, traverse child relationships, intersect the result with the administrator’s permitted stores, then paginate.
**Data Dependencies:** Reads store parent/retailer fields and administrator store permissions.
**Side Effects:** Security denials are auditable.
**Concrete Example:** A retailer admin sees `north_01` and `north_02` but not `south_01`; unauthorized child request → `403`.
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 8 | 8 | OK |
| Data-flow | 7 | 7 | OK |
| Constants | 0 | 0 | N/A |
| State transitions | 0 | 0 | N/A |
| Outcomes | 2 | 2 | OK |
| Data writes | 0 | 1 | GAP |
| Integrations | 0 | 0 | N/A |
| Error paths | 2 | 2 | OK |

### BR-MSA-BRD-001: Branding metadata is store-scoped while binary storage is delegated
**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/store/MerchantStoreApi.java:180-360`; `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/model/store/MerchantStoreEntity.java:12-105`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)
**CAST Reference:** Transactions `244227`, `244229`, `244230`, and `244231`

**Statement:** Branding metadata belongs to one store, while logo bytes are stored through the configured file provider and are not part of the store database record.
**Intent:** State Transition
**Logic:** Authorize the store, persist template/branding metadata, and call the file-provider boundary for logo upload or deletion; do not store provider bytes in the store row.
**Data Dependencies:** Reads/writes store branding metadata and provider object references.
**Side Effects:** Calls external file storage.
**Concrete Example:** `POST /api/v1/stores/north_01/branding/logo` with a PNG → `201 {"logoUrl":"..."}`; provider unavailable → `503 STORAGE_UNAVAILABLE`.
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 6 | 6 | OK |
| Data-flow | 5 | 5 | OK |
| Constants | 2 | 2 | OK |
| State transitions | 1 | 1 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 2 | 2 | OK |
| Integrations | 2 | 2 | OK |
| Error paths | 2 | 2 | OK |

### BR-MSA-LANG-001: Supported languages must include the default language
**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/populator/store/PersistableMerchantStorePopulator.java:101-122`; `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/model/store/MerchantStoreEntity.java:40-80`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)
**CAST Reference:** Transactions `244224`, `244225`, and `244226`

**Statement:** A store’s configured default language must be one of its supported languages.
**Intent:** Validation
**Logic:** Resolve requested supported language codes; reject a default code not present in the association set; persist the default and associations together.
**Data Dependencies:** Reads/writes store-language associations and `LANGUAGE.CODE`.
**Side Effects:** Calls shared language reference service.
**Concrete Example:** Supported `[en,fr]`, default `fr` → `201`; default `de` absent from supported list → `422 DEFAULT_LANGUAGE_UNSUPPORTED`.
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 5 | 5 | OK |
| Data-flow | 5 | 5 | OK |
| Constants | 0 | 0 | N/A |
| State transitions | 1 | 1 | OK |
| Outcomes | 2 | 2 | OK |
| Data writes | 2 | 2 | OK |
| Integrations | 1 | 1 | OK |
| Error paths | 1 | 1 | OK |

### BR-MSA-LST-001: Store name lookup returns only authorized store names
**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/merchant/MerchantStoreServiceImpl.java:137-166`; `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/store/MerchantStoreApi.java:400-520`  
**Discovery Method:** Hybrid (CAST transaction + Direct Source Read)
**CAST Reference:** Transactions `244222` and `244223`

**Statement:** Lightweight store-name lists are restricted to the administrator’s permitted hierarchy and exclude unauthorized tenants.
**Intent:** Authorization
**Logic:** Resolve permitted store IDs from the active administrator context, query only those stores, order by display name, and return code/name pairs.
**Data Dependencies:** Reads `MERCHANT_STORE.ID`, `STORE_CODE`, and `STORE_NAME`.
**Side Effects:** None.
**Concrete Example:** `GET /api/v1/stores/names` for a retailer admin → only permitted names; missing authorization → `403 STORE_ACCESS_DENIED`.
**Semantic Preservation:**
| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 5 | 5 | OK |
| Data-flow | 5 | 5 | OK |
| Constants | 0 | 0 | N/A |
| State transitions | 0 | 0 | N/A |
| Outcomes | 2 | 2 | OK |
| Data writes | 0 | 0 | N/A |
| Integrations | 0 | 0 | N/A |
| Error paths | 1 | 1 | OK |

## Rule coverage notes

The 13 assigned Phase 3 rules are re-extracted above. Eight net-new rules capture normalization, immutable identity, transactional creation, deterministic pagination, bounded hierarchy expansion, branding-provider separation, language-set coherence, and authorized name lookup discovered during the Phase 4 deep read.
