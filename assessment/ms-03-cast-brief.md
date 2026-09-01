# MS-03 Search — CAST-Guided Phase 4 Brief

## Scope

- **Service:** MS-03 Search
- **CAST application:** `Shopizer-Backend`
- **Analysis mode:** Hybrid — CAST transaction and component selection followed by direct source reading
- **Ownership:** Search queries, autocomplete, index documents, index rebuild state, and the configured search-provider boundary
- **Inputs:** Product, category, availability, media, and catalog events from MS-02; tenant/store and locale context
- **Outputs:** Search results, autocomplete suggestions, localized search documents, and index-status responses
- **Out of scope:** Product/catalog persistence (MS-02), commercial promotion calculation (MS-07), and provider-specific search infrastructure implementation (MS-12 boundary)

## CAST entry points

| Transaction ID | Entry point | Reduced/full size | CAST evidence |
|---|---|---:|---|
| `243993` | `POST /api/v1/search/` | 1 / 64 | `SearchApi.search` |
| `243994` | `POST /api/v1/search/autocomplete/` | 1 / 54 | `SearchApi.autocomplete` |
| `243998` | `POST /api/v1/private/system/search/index/` | 23 / 536 | `SearchToolsApi.index` and asynchronous full-index path |

The full-index transaction is the primary complexity hotspot. Its reduced graph includes `SearchServiceImpl`, `SearchFacadeImpl`, `IndexProductEventListener`, `SearchModule`, product projection reads, and the configured search-provider call path.

## Source files to read

| Priority | Local path | Why it is in scope |
|---:|---|---|
| 1 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/search/SearchServiceImpl.java` | Search enablement gates, localized document construction, product indexing, provider delegation, and index lifecycle |
| 2 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/search/facade/SearchFacadeImpl.java` | Search request mapping, autocomplete limit, result shaping, and full-index orchestration |
| 3 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/search/SearchApi.java` | Public search and autocomplete endpoint contracts and request context |
| 4 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/system/SearchToolsApi.java` | Private index rebuild endpoint and administrative authorization boundary |
| 5 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/search/facade/SearchFacade.java` | Facade operations and caller-facing search abstraction |
| 6 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/search/SearchService.java` | Search service interface and operation boundary |
| 7 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/configuration/events/products/listeners/IndexProductEventListener.java` | Product, variant, attribute, and media event-to-index dispatch |
| 8 | `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/common/Criteria.java` | Pagination and offset semantics used by search requests |
| 9 | `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/model/catalog/SearchProductRequest.java` | Search request fields and filter semantics |
| 10 | `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/model/catalog/SearchProductList.java` | Search response envelope and pagination fields |
| 11 | `initial-source/shopizer-3.2.7/sm-core/src/main/resources/shopizer-core.properties` | Search provider and indexing configuration defaults |
| 12 | `initial-source/shopizer-3.2.7/sm-core/src/main/resources/profiles/local/shopizer-core.properties` | Local profile search configuration overrides |

## Source files to skip

| Path/family | Reason |
|---|---|
| `modules.commons.search.*` extracted library classes | External provider SPI; inspect only through the local `SearchModule` interface and target adapter boundary |
| Hibernate/JPA framework classes and generated metadata | Infrastructure, not search business behavior |
| Product/catalog persistence classes already assigned to MS-02 | Search reads their projection/source data but does not own their mutations |
| Frontend search components | Frontend specification stage; this brief targets the backend search service |

## Data and integration signals

- CAST identifies product projection reads and configured search-module calls in the full-index graph.
- Search behavior is feature-gated by `search.noindex`, `INDEX_PRODUCTS`, and provider availability.
- A product description produces a localized search document; locale and store context must remain part of the document identity.
- Product, variant, attribute, and media events trigger index refresh or deletion paths.
- Autocomplete is a distinct read operation with a bounded suggestion count.
- The private index endpoint is an administrative operation and must not be exposed as a storefront mutation.

## Phase 1 rules requiring Phase 4 deep extraction

- `BR-CAT-020` — Search/indexing is bypassed when no-index, `INDEX_PRODUCTS=false`, or the search module is absent.
- `BR-CAT-021` — One localized search document is built per product description/language.
- `BR-CAT-022` — Product, variant, attribute, and image events refresh the search index.
- `BR-CAT-023` — Image/attribute event filters contain an ID-comparison defect candidate and require source confirmation.
- `BR-CAT-024` — Autocomplete returns at most 15 suggestions and category facets are effectively disabled.
- `BR-EXT-023` — Search indexing is event-driven and globally disableable.
- `BR-EXT-024` — Search builds localized documents through the configured search-provider boundary.

## Hidden-engine check

The three search entry points are not CRUD-only. The full-index path has 536 objects (23 in the reduced graph), far above a small endpoint-and-repository baseline. The residual contains indexing orchestration, localized document projection, event dispatch, feature gates, and an external provider SPI. This is a search-indexing engine and requires a complete extraction of `SearchServiceImpl`, `SearchFacadeImpl`, and `IndexProductEventListener`, not a thin CRUD specification.

## Cross-service dependencies

- **MS-02:** consumes product/catalog/availability/media change events and reads an approved product projection contract; never writes MS-02 tables.
- **MS-10:** consumes opaque tenant/store context and validates administrative scope through the shared request context.
- **MS-12:** provider-neutral search adapter boundary for the configured external index implementation.
- **MS-04/MS-05:** may consume search results or product projection events indirectly; no direct database coupling.

## CAST limitations

Numeric object IDs are available for the transactions above, but source references should use resolved local paths. External `SearchModule` implementation files are not in the local source tree and must not be fabricated as local evidence.
