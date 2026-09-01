# MS-03 Search — Extraction Evidence

## Source files processed

| # | File | Lines | Sections read | Primary rule evidence | Vectors counted |
|---:|---|---:|---|---:|---|
| 1 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/search/SearchServiceImpl.java` | 519 | 1-200: configuration, availability, document lookup, and index entry; 201-400: projection mapping and provider operations; 401-519: search, pagination, and failure handling | BR-CAT-020, BR-CAT-021, BR-CAT-022, BR-CAT-032, BR-CAT-034, BR-EXT-023, BR-EXT-024 | ✅ |
| 2 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/search/facade/SearchFacadeImpl.java` | 206 | 1-100: search and rebuild facade entry points; 101-206: result shaping, autocomplete, and error mapping | BR-CAT-020, BR-CAT-024, BR-CAT-032, BR-CAT-034 | ✅ |
| 3 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/search/SearchApi.java` | 73 | 1-73: public search and autocomplete request/context contract | BR-CAT-020, BR-CAT-024 | ✅ |
| 4 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/system/SearchToolsApi.java` | 86 | 1-86: administrative authorization and asynchronous rebuild endpoint | BR-CAT-032 | ✅ |
| 5 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/search/facade/SearchFacade.java` | 53 | 1-53: facade operation boundary | — | ✅ |
| 6 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/search/SearchService.java` | 49 | 1-49: search service operation boundary | — | ✅ |
| 7 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/configuration/events/products/listeners/IndexProductEventListener.java` | 319 | 1-120: event gating and product events; 121-220: variant events; 221-319: image/attribute events and component merge behavior | BR-CAT-023, BR-CAT-033 | ✅ |
| 8 | `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/common/Criteria.java` | 139 | 1-139: offset, limit, and criteria semantics | BR-CAT-034 | ✅ |
| 9 | `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/model/catalog/SearchProductRequest.java` | 49 | 1-49: search term, language, store, count, and start fields | BR-CAT-024, BR-CAT-034 | ✅ |
| 10 | `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/model/catalog/SearchProductList.java` | 27 | 1-27: search result envelope and pagination fields | BR-CAT-034 | ✅ |
| 11 | `initial-source/shopizer-3.2.7/sm-core/src/main/resources/shopizer-core.properties` | 111 | 1-111: provider, indexing, locale, host, and mapping defaults | BR-EXT-023, BR-EXT-024 | ✅ |
| 12 | `initial-source/shopizer-3.2.7/sm-core/src/main/resources/profiles/local/shopizer-core.properties` | 106 | 1-106: local profile provider and indexing overrides | BR-EXT-023, BR-EXT-024 | ✅ |

## External and excluded evidence

`modules.commons.search.SearchModule` is an external provider SPI identified by CAST. Its implementation is not present in the local source tree, so no local source reference is fabricated. Hibernate/JPA infrastructure, generated metadata, MS-02 catalog persistence, and frontend components were excluded according to `assessment/ms-03-cast-brief.md`.

## Extraction status

- **Files total:** 12
- **Files processed:** 12
- **Rules extracted:** 10
- **Source vectors complete:** yes
- **CAST transactions covered:** 243993, 243994, 243998
- **CAST mode:** Hybrid

## Session log

| Session | Files processed | Rules added | Notes |
|---:|---:|---:|---|
| 1 | 12 | 10 | Deep read of all CAST-selected business and contract files; confirmed indexing engine, event defects, provider boundary, and pagination behavior |
