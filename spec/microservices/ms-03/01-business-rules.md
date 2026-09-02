# Search Service — Business Rules

**Service:** MS-03 Search  
**Version:** 1.0  
**Analysis mode:** Hybrid — CAST-guided transaction scope plus direct Java source read  
**Boundary:** MS-03 owns searchable projections, localized documents, autocomplete, rebuild orchestration, and the provider-neutral search boundary. Product/catalog authority remains MS-02.

### BR-CAT-020: Search availability requires an enabled provider

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/search/SearchServiceImpl.java:122-129,378-383,395-402,411-418,428-434`; `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/search/facade/SearchFacadeImpl.java:85-115,175-200`  
**Discovery Method:** Hybrid — CAST transactions 243993, 243994, 243998 plus direct source read  
**Statement:** Product search, autocomplete, and indexing operate only when indexing is enabled for the deployment and a configured search provider is available. Disabled or unavailable search must return a defined service outcome without issuing provider calls or dereferencing an absent response.  
**Intent:** Validation; Routing  
**Classification:** Active
**Weight:** Medium
**Logic:**
```text
enabled = configuration.INDEX_PRODUCTS
providerAvailable = searchModule != null
deploymentAllowsIndexing = !configuration.search.noindex
IF enabled is false OR providerAvailable is false:
    index, delete, search, autocomplete, and document lookup do not call the provider
IF deploymentAllowsIndexing is false:
    provider initialization and event dispatch are skipped
target maps unavailable reads to 503 SEARCH_UNAVAILABLE
```
**Data Dependencies:** Reads `INDEX_PRODUCTS`, `search.noindex`, provider availability, tenant/store context, and search index state.  
**Side Effects:** No provider mutation or query when unavailable; emit an operational availability metric.  
**Concrete Example:** Success: `POST /api/v1/search` for `{"query":"blue mug","count":20,"start":0}` returns `200` results when enabled. Error: the same request with indexing disabled returns `503 {"error":"SEARCH_UNAVAILABLE","statusCode":503}`.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 4 | 4 | OK |
| Data-flow | 5 | 5 | OK |
| Constants | 2 | 2 | OK |
| State transitions | 0 | 1 | GAP |
| Outcomes | 5 | 3 | GAP |
| Data writes | 0 | 0 | OK |
| Integrations | 1 | 1 | OK |
| Error paths | 4 | 4 | OK |

**Preservation:** FLAGGED — target makes legacy null/unavailable outcomes explicit as HTTP responses.

### BR-CAT-021: A product has one search document per store and locale

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/search/SearchServiceImpl.java:131-159,212-249,443-445`  
**Discovery Method:** Hybrid — CAST transaction 243998 plus direct source read  
**Statement:** Each searchable product description produces one localized document identified by tenant, store, product, and locale. English and French descriptions for one product therefore remain separate searchable documents.  
**Intent:** Calculation; State Transition  
**Classification:** Core
**Weight:** High
**Logic:**
```text
languages = product.descriptions.map(description.language.code)
existing = provider.getDocument(product.id, languages, DO_NOT_FAIL_ON_NOT_FOUND)
IF existing is present: provider.delete(languages, product.id)
FOR EACH description:
    item = buildIndexItem(product, description)
    item.language = description.language.code
    item.store = lowerCase(product.merchantStore.code)
    provider.index(item)
```
**Data Dependencies:** Product ID, store, descriptions, locale code, localized text, and provider document identity.  
**Side Effects:** Existing locale documents are removed before the current locale set is indexed.  
**Concrete Example:** Success: product `74021` with `en` and `fr` descriptions yields two active documents. Error: a missing locale returns `422 {"error":"DOCUMENT_LOCALE_REQUIRED","statusCode":422}` and writes no active document.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 3 | 3 | OK |
| Data-flow | 7 | 7 | OK |
| Constants | 1 | 1 | OK |
| State transitions | 1 | 1 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 1 | 1 | OK |
| Integrations | 1 | 1 | OK |
| Error paths | 2 | 2 | OK |

**Preservation:** OK

### BR-CAT-022: Search documents contain the localized product projection

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/search/SearchServiceImpl.java:184-249,305-369,443-487`  
**Discovery Method:** Hybrid — CAST transaction 243998 plus direct source read  
**Statement:** A localized search document includes the searchable product text, store context, product and variant inventory/price entries, and available localized merchandising information such as brand, category, attributes, image, reviews, and product link.  
**Intent:** Calculation  
**Classification:** Core
**Weight:** High
**Logic:**
```text
image = default product image OR first image
inventoryEntries = inventory(product)
IF product.variants exist:
    append inventory and option/value mappings for each variant
item.name = description.name
item.description = description.description
item.brand = localized manufacturer when present
item.category = localized first category when present
item.attributes = localized option/value map when present
item.image = selected image when present
item.reviews = review average when present
item.link = description.seUrl
provider.index(item)
```
**Data Dependencies:** Product descriptions, variants, inventory, prices, manufacturer, categories, options, images, reviews, and friendly URL.  
**Side Effects:** One provider-neutral projection is stored/submitted; MS-02 entities are not mutated.  
**Concrete Example:** Success: product `74021` indexes name `Blue Ceramic Mug`, quantity `18`, price `14.95`, brand `Northwind`, and image `/images/mug-blue.jpg`. Error: an attribute lacks a localized value and returns `422 {"error":"LOCALIZATION_REQUIRED_FOR_ATTRIBUTE","statusCode":422}`.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 11 | 11 | OK |
| Data-flow | 21 | 21 | OK |
| Constants | 6 | 6 | OK |
| State transitions | 0 | 1 | GAP |
| Outcomes | 4 | 4 | OK |
| Data writes | 1 | 1 | OK |
| Integrations | 2 | 2 | OK |
| Error paths | 4 | 4 | OK |

**Preservation:** FLAGGED — the target adds durable projection state around the provider write.

### BR-CAT-023: Product projection changes trigger refresh or removal

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/configuration/events/products/listeners/IndexProductEventListener.java:55-96,124-200,203-311`  
**Discovery Method:** Hybrid — CAST transaction 243998 plus direct source read  
**Statement:** Product, variant, image, or attribute changes that can affect search visibility or displayed data refresh the parent product projection. Product deletion removes all localized documents, and variant deletion refreshes the parent without the deleted variant.  
**Intent:** Routing; State Transition  
**Classification:** Core
**Weight:** High
**Logic:**
```text
IF indexing disabled: ignore event
ELSE IF product saved: reload full product and index
ELSE IF product deleted: delete all product locale documents
ELSE IF variant saved: reload parent, replace changed variant, index parent
ELSE IF variant deleted: reload parent, remove variant by ID, index parent
ELSE IF image/attribute saved: reload parent, merge component, index parent
ELSE IF image/attribute deleted: reload parent, remove component, index parent
```
**Data Dependencies:** Event type, tenant/store, product/component IDs, MS-02 product projection, and existing documents.  
**Side Effects:** Provider index/delete operation and retryable indexing outcome; no direct catalog mutation.  
**Concrete Example:** Success: a variant change for product `74021` reindexes the complete parent projection. Error: provider outage during `ProductDeleted` leaves a retryable `INDEX_DELETE_FAILED` outcome.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 9 | 9 | OK |
| Data-flow | 12 | 12 | OK |
| Constants | 8 | 8 | OK |
| State transitions | 4 | 4 | OK |
| Outcomes | 8 | 8 | OK |
| Data writes | 0 | 1 | GAP |
| Integrations | 2 | 2 | OK |
| Error paths | 8 | 8 | OK |

**Preservation:** FLAGGED — target makes local projection and retry state durable.

### BR-CAT-024: Autocomplete returns no more than fifteen suggestions

**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/search/facade/SearchFacadeImpl.java:62-64,175-200`; `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/search/SearchApi.java:62-71`; `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/model/catalog/SearchProductRequest.java:18-23`  
**Discovery Method:** Hybrid — CAST transaction 243994 plus direct source read  
**Statement:** Autocomplete accepts a non-empty term with store and locale context, delegates a keyword search, and returns at most fifteen suggestion strings. Category facets are not included in this response.  
**Intent:** Validation; Calculation  
**Classification:** Active
**Weight:** Low
**Logic:**
```text
require word, language, and store
request.searchString = word
request.language = language.code
request.store = lowerCase(store.code)
response = searchKeywords(request, AUTOCOMPLETE_ENTRIES_COUNT = 15)
return response.items.map(item.suggestions)
```
**Data Dependencies:** Search term, store, locale, provider keyword response, and suggestion values.  
**Side Effects:** Provider keyword query only; no local mutation or category lookup.  
**Concrete Example:** Success: `POST /api/v1/search/autocomplete` with `{"query":"blu"}` returns `200 {"suggestions":["blue ceramic mug","blue bowl"]}`. Error: blank query returns `422 {"error":"QUERY_REQUIRED","statusCode":422}`.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 3 | 3 | OK |
| Data-flow | 5 | 5 | OK |
| Constants | 15 | 15 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 2 | 3 | GAP |
| Data writes | 0 | 0 | OK |
| Integrations | 1 | 1 | OK |
| Error paths | 2 | 3 | GAP |

**Preservation:** FLAGGED — target defines typed empty/provider-failure outcomes.

### BR-EXT-023: Indexing can be globally disabled

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/search/SearchServiceImpl.java:104-120`; `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/configuration/events/products/listeners/IndexProductEventListener.java:48-96`; `initial-source/shopizer-3.2.7/sm-core/src/main/resources/shopizer-core.properties:28-39`; `initial-source/shopizer-3.2.7/sm-core/src/main/resources/profiles/local/shopizer-core.properties:22-37`  
**Discovery Method:** Hybrid — CAST full-index/event paths plus direct source read  
**Statement:** Deployment configuration can disable provider initialization and event-driven indexing without disabling catalog operations. Catalog events are acknowledged without search mutation while the feature is disabled.  
**Intent:** Routing; Compliance  
**Classification:** Core
**Weight:** Critical
**Logic:**
```text
noIndex = configuration.search.noindex OR false
IF noIndex is false AND provider exists: configure provider
ELSE: skip provider configuration
ON product event:
    IF noIndex: do nothing
    ELSE: dispatch event to indexing handler
```
**Data Dependencies:** `search.noindex`, provider configuration, supported locales, provider hosts, and product event stream.  
**Side Effects:** No provider calls while disabled; expose an operational disabled signal.  
**Concrete Example:** Success: startup with `search.noindex=true` acknowledges a product event without indexing. Error: rebuild while disabled returns `409 {"error":"INDEXING_DISABLED","statusCode":409}`.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 3 | 3 | OK |
| Data-flow | 6 | 6 | OK |
| Constants | 1 | 1 | OK |
| State transitions | 0 | 1 | GAP |
| Outcomes | 3 | 4 | GAP |
| Data writes | 0 | 0 | OK |
| Integrations | 1 | 1 | OK |
| Error paths | 1 | 1 | OK |

**Preservation:** FLAGGED — target exposes disabled state explicitly.

### BR-EXT-024: Provider configuration uses a neutral boundary

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/search/SearchServiceImpl.java:104-120,256-301,395-441,490-517`; `initial-source/shopizer-3.2.7/sm-core/src/main/resources/shopizer-core.properties:20-39`; `initial-source/shopizer-3.2.7/sm-core/src/main/resources/profiles/local/shopizer-core.properties:22-37`  
**Discovery Method:** Hybrid — CAST provider path plus direct source read  
**Statement:** MS-03 configures the selected search provider with deployment hosts, credentials, supported locales, and locale-specific mappings, then delegates product, keyword, and document operations through a provider-neutral adapter.  
**Intent:** Routing; Validation  
**Classification:** Core
**Weight:** High
**Logic:**
```text
configuration = clusterName + hosts + credentials + searchLanguages
FOR EACH configured language:
    load product mapping
    load keyword mapping
    load English settings for en, default settings otherwise
provider.configure(configuration)
searchProducts -> provider.searchProducts
searchKeywords -> provider.searchKeywords
getDocument -> provider.getDocument
```
**Data Dependencies:** Cluster name, hosts, credentials, locales, mapping resources, and settings resources.  
**Side Effects:** Provider configuration and query calls; target records configuration version and health.  
**Concrete Example:** Success: valid `search.internal:9200` configuration for `en,fr` enables product search. Error: missing French mapping records `CONFIGURATION_FAILED` and search returns `503 SEARCH_PROVIDER_UNAVAILABLE`.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 7 | 7 | OK |
| Data-flow | 9 | 9 | OK |
| Constants | 4 | 4 | OK |
| State transitions | 1 | 2 | GAP |
| Outcomes | 5 | 5 | OK |
| Data writes | 0 | 1 | GAP |
| Integrations | 2 | 2 | OK |
| Error paths | 6 | 6 | OK |

**Preservation:** FLAGGED — target persists provider configuration state.

### BR-CAT-032: Reindexing replaces the current product projection

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/search/SearchServiceImpl.java:133-180`; `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/search/facade/SearchFacadeImpl.java:66-83`; `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/system/SearchToolsApi.java:51-83`  
**Discovery Method:** Hybrid — CAST transaction 243998 plus direct source read  
**Statement:** Reindexing replaces all current localized documents for a product with the latest catalog projection. A store-wide rebuild runs asynchronously and records requested, running, and terminal outcomes.  
**Intent:** State Transition; Calculation  
**Classification:** Core
**Weight:** High
**Logic:**
```text
ON product index:
    lookup existing locale documents without failing when absent
    delete existing documents when present
    build and index each current description
ON rebuild(store):
    create rebuild job
    asynchronously list store products
    index each product
    transition job to Succeeded, Failed, or Cancelled
```
**Data Dependencies:** Store, product projection, descriptions, existing documents, rebuild identity, and idempotency key.  
**Side Effects:** Provider delete/index operations and rebuild-job writes.  
**Concrete Example:** Success: `POST /api/v1/private/system/search/index` returns `202` with rebuild ID and `Requested`. Error: a duplicate active idempotency key returns `409 REBUILD_ALREADY_RUNNING` with the existing ID.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 6 | 6 | OK |
| Data-flow | 8 | 8 | OK |
| Constants | 2 | 2 | OK |
| State transitions | 2 | 4 | GAP |
| Outcomes | 4 | 6 | GAP |
| Data writes | 2 | 3 | GAP |
| Integrations | 3 | 3 | OK |
| Error paths | 4 | 6 | GAP |

**Preservation:** FLAGGED — target makes asynchronous rebuild state and idempotency explicit.

### BR-CAT-033: Component event merges preserve unchanged components

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/configuration/events/products/listeners/IndexProductEventListener.java:203-311`  
**Discovery Method:** Hybrid — CAST transaction 243998 plus direct source read  
**Statement:** When an image or attribute changes, the refreshed product projection retains unrelated components and replaces or removes only the component identified by the event. A component event with no usable identity is rejected rather than corrupting the projection.  
**Intent:** Validation; State Transition  
**Classification:** Core
**Weight:** High
**Logic:**
```text
legacy save filters compare each component ID to itself and therefore discard existing components
legacy image deletion returns before refresh
target reloads the full product
target retains components whose ID differs from the event ID
target adds a saved component or omits a deleted component
target indexes the resulting parent projection
```
**Data Dependencies:** Full product projection, component identifiers, event payload, and image/attribute collections.  
**Side Effects:** Product refresh and provider write; failed merges are retried or dead-lettered.  
**Concrete Example:** Success: updating `img-side` on product `74021` preserves `img-main`. Error: null image ID returns `422 COMPONENT_IDENTIFIER_REQUIRED` and leaves the existing projection unchanged.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 4 | 4 | OK |
| Data-flow | 8 | 8 | OK |
| Constants | 0 | 0 | OK |
| State transitions | 1 | 1 | OK |
| Outcomes | 4 | 4 | OK |
| Data writes | 1 | 1 | OK |
| Integrations | 2 | 2 | OK |
| Error paths | 2 | 3 | GAP |

**Preservation:** FLAGGED — target corrects confirmed self-comparison and image-delete defects.

### BR-CAT-034: Search pagination is applied at the service boundary

**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/model/catalog/SearchProductRequest.java:18-23,33-47`; `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/search/facade/SearchFacadeImpl.java:85-115`; `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/search/SearchServiceImpl.java:411-425`  
**Discovery Method:** Hybrid — CAST transaction 243993 plus direct source read  
**Statement:** Product search honors a non-negative offset and a result limit from one through one hundred. Response pagination reports the applied values even when the external provider does not enforce them.  
**Intent:** Validation; Calculation  
**Classification:** Core
**Weight:** High
**Logic:**
```text
default count = 100
default start = 0
validate start >= 0 and 1 <= count <= 100
pass offset and limit to provider when supported
defensively trim results to [start, start + count)
return applied pagination metadata
```
**Data Dependencies:** Query, count, start, provider result list, optional total, and index scope.  
**Side Effects:** Provider query only; calculate response pagination metadata.  
**Concrete Example:** Success: `POST /api/v1/search` with `{"query":"mug","count":20,"start":40}` returns at most 20 results from offset 40. Error: count `250` returns `422 {"error":"INVALID_LIMIT","statusCode":422}`.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 3 | 5 | GAP |
| Data-flow | 4 | 4 | OK |
| Constants | 2 | 3 | GAP |
| State transitions | 0 | 0 | OK |
| Outcomes | 2 | 4 | GAP |
| Data writes | 0 | 0 | OK |
| Integrations | 1 | 1 | OK |
| Error paths | 2 | 4 | GAP |

**Preservation:** FLAGGED — target closes the ignored count/start gap.
