# Content and Configuration Business Rules

**Version**: 1.0  
**Date**: 2026-09-01  
**Status**: 🟡 In Progress — Phase 4 extraction  
**Service ID**: MS-11  
**Service Name**: Content and Configuration

## Extraction Scope

MS-11 owns merchant-scoped CMS content, localized descriptions, content-file metadata and storage orchestration, merchant configuration projection, integration-module metadata and discovery, and configuration hand-off to payment/shipping provider boundaries. Store lifecycle remains MS-10. Payment/shipping execution remains outside MS-11.

Source references use the repository-relative path required by the SAAM extraction protocol. CAST references identify the transaction or call-graph evidence that located the source behavior. Provider adapter implementations are treated as infrastructure evidence except where their behavior changes the MS-11 contract or exposes a target-only defect.

## Business Rules

### BR-MER-013: Content codes are unique within a merchant store

**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/facade/content/ContentFacadeImpl.java` : `saveContentPage()` lines 693-714; `saveContentBox()` lines 717-738  
**Cross-Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/content/ContentRepository.java` : `findByCode()` lines 32-34; `findByCodeAndType()` lines 20-22  
**Discovery Method:** Direct Source Read | CAST Imaging (Hybrid)  
**CAST Reference:** Transaction 244054 `POST api/v1/private/content/page/`; transaction 244051 `POST api/v1/private/content/box/`; transaction 244052/244053 uniqueness checks

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 2 | 2 | OK |
| Data-flow | 3 | 3 | OK |
| Constants | 2 | 2 | OK (`PAGE`, `BOX`) |
| State transitions | 0 | 0 | OK |
| Outcomes | 2 | 2 | OK |
| Data writes | 1 | 1 | OK |
| Integrations | 0 | 0 | OK |
| Error paths | 1 | 1 | OK |

**Preservation:** OK

**Statement:** A merchant store cannot contain two CMS items with the same content code, regardless of whether the existing item is a page or a content box. The uniqueness scope is the merchant store, not the global platform.

**Intent:** Validation

**Acceptance Criteria:** Given an existing `about-us` item in store `DEFAULT`, when a page or box with code `about-us` is created in that store, then creation is rejected. The same code may be created in a different merchant store.

**Logic:**
```pseudocode
existingContent = contentService.getByCode(page.getCode(), merchantStore)
IF existingContent != null:
    THROW ConstraintException(
        "Page with code [" + page.getCode() +
        "] already exist for store [" + merchantStore.getCode() + "]"
    )

existingContent = contentService.getByCode(box.getCode(), merchantStore)
IF existingContent != null:
    THROW ConstraintException(
        "Content box with code [" + box.getCode() +
        "] already exist for store [" + merchantStore.getCode() + "]"
    )

// Database evidence also declares UNIQUE(MERCHANT_ID, CODE).
```

**Data Dependencies:**
- Reads: `content.code`, `content.merchant_id`, `content.content_type`
- Writes: `content.content_id`, `content.code`, `content.merchant_id`, `content.content_type`
- Constraint: `content(MERCHANT_ID, CODE)` unique constraint

**Side Effects:**
- Calls `ContentService.getByCode()`
- On success, persists a new `content` row and its localized `content_description` rows
- On conflict, no content write is intended

**Concrete Example:**
- **API Input:** `POST /api/v1/private/content/page`  
  `{"code":"about-us","linkToMenu":true,"visible":true,"descriptions":[{"language":"en","name":"About us","friendlyUrl":"about-us","title":"About Us","description":"Our company"}]}`
- **Success Output:** `201 {"id":1201}` when store `DEFAULT` has no `about-us` item
- **Error Input:** The same request when store `DEFAULT` already contains a box with `code:"about-us"`
- **Error Output:** `500 {"message":"Page with code [about-us] already exist for store [DEFAULT]"}` from the wrapped `ConstraintException`
- **Cross-store example:** The same request succeeds for store `CA-STORE` if that store has no `about-us` item

---

### BR-MER-014: Page and box operations assign their content type from the operation

**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/facade/content/ContentFacadeImpl.java` : `convertContentPageToContent()` lines 218-236; `convertContentBoxToContent()` lines 238-258  
**Cross-Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/content/ContentApi.java` : `createBox()` lines 176-191; `createPage()` lines 231-246; `updatePage()` lines 291-306; `updateBox()` lines 308-323  
**Discovery Method:** Direct Source Read | CAST Imaging (Hybrid)  
**CAST Reference:** Transaction 244054 page create; transaction 244051 box create; transaction 244057 page update; transaction 244058 box update

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 2 | 2 | OK |
| Data-flow | 7 | 7 | OK |
| Constants | 2 | 2 | OK (`PAGE`, `BOX`) |
| State transitions | 2 | 2 | OK |
| Outcomes | 2 | 2 | OK |
| Data writes | 6 | 6 | OK |
| Integrations | 0 | 0 | OK |
| Error paths | 0 | 0 | OK |

**Preservation:** OK

**Statement:** A page endpoint creates or updates an item classified as `PAGE`, and a box endpoint creates or updates an item classified as `BOX`. The requested operation determines the type; a client cannot change a page into a box by supplying another content type.

**Intent:** State Transition

**Acceptance Criteria:** Given a page request containing any client-supplied content type, when it is processed through the page operation, then the persisted item has type `PAGE`. The equivalent box operation persists type `BOX`.

**Logic:**
```pseudocode
convertContentPageToContent(store, model, content):
    contentModel = model if model != null else new Content()
    descriptions = buildDescriptions(contentModel, content.getDescriptions())

    contentModel.setCode(content.getCode())
    contentModel.setContentType(ContentType.PAGE)
    contentModel.setMerchantStore(store)
    contentModel.setLinkToMenu(content.isLinkToMenu())
    contentModel.setVisible(content.isVisible())
    contentModel.setDescriptions(descriptions)
    contentModel.setId(content.getId())
    RETURN contentModel

convertContentBoxToContent(store, model, content):
    contentModel = model if model != null else new Content()
    descriptions = buildDescriptions(contentModel, content.getDescriptions())

    FOR cd IN descriptions:
        cd.setContent(contentModel)

    contentModel.setCode(content.getCode())
    contentModel.setContentType(ContentType.BOX)
    contentModel.setMerchantStore(store)
    contentModel.setVisible(content.isVisible())
    contentModel.setDescriptions(descriptions)
    contentModel.setId(content.getId())
    RETURN contentModel
```

**Data Dependencies:**
- Reads: `content.code`, `content.visible`, `content.link_to_menu`, request `descriptions`
- Writes: `content.content_type`, `content.code`, `content.merchant_id`, `content.visible`, `content.link_to_menu`, `content_description.content_id`

**Side Effects:**
- Reuses the existing `Content` entity during update
- Rebuilds the localized description collection
- Persists through `ContentService.saveOrUpdate()`

**Concrete Example:**
- **API Input:** `POST /api/v1/private/content/page`  
  `{"code":"returns","contentType":"Box","linkToMenu":false,"visible":true,"descriptions":[{"language":"en","name":"Returns","friendlyUrl":"returns","title":"Returns","description":"Return policy"}]}`
- **Success Output:** `201 {"id":"00000000-0000-0000-0000-000000001202"}` and a persisted page
- **Error Input:** `PUT /api/v1/private/content/box/00000000-0000-0000-0000-000000001202` with `{"code":"returns","contentType":"Page","visible":true,"descriptions":[{"language":"en","name":"Returns","friendlyUrl":"returns","title":"Returns","description":"Updated"}]}`
- **Error Output:** No type-validation error is produced by the legacy converter; the target contract must ignore the conflicting `contentType` field or reject it explicitly rather than persist the wrong type. This is a target contract requirement derived from the source behavior.

---

### BR-MER-015: Localized descriptions are upserted by language code during content mutation

**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/facade/content/ContentFacadeImpl.java` : `buildDescriptions()` lines 349-393  
**Cross-Reference:** `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/content/ContentDescription.java` : fields and setters lines 20-76; `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/common/description/Description.java` : localized fields lines 24-91  
**Discovery Method:** Direct Source Read | CAST Imaging (Hybrid)  
**CAST Reference:** Transaction 244057 `PUT api/v1/private/content/page/{}`; transaction 244058 `PUT api/v1/private/content/box/{}`; CAST method 30117 `buildDescriptions`

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 5 | 5 | OK |
| Data-flow | 11 | 11 | OK |
| Constants | 0 | 0 | OK |
| State transitions | 1 | 1 | OK |
| Outcomes | 2 | 2 | OK |
| Data writes | 8 | 8 | OK |
| Integrations | 1 | 1 | OK (`LanguageService`) |
| Error paths | 1 | 1 | OK |

**Preservation:** OK

**Statement:** Each submitted localized description is matched to an existing description for the same language code. A matching description is updated in place; a language not already present creates a new localized description. The submitted description collection becomes the content item's localized description collection, so omitted languages are not retained by this conversion path.

**Intent:** State Transition

**Acceptance Criteria:** Given an English description already exists, when an update supplies English content, then the existing English row is updated rather than duplicated. When French is supplied for the first time, a French row is added. When a previously stored German description is omitted from the submitted list, the target must make the replacement semantics explicit; the legacy conversion replaces the collection.

**Logic:**
```pseudocode
buildDescriptions(contentModel, persistableDescriptions):
    descriptions = new ArrayList()

    FOR objectContent IN persistableDescriptions:
        lang = languageService.getByCode(objectContent.getLanguage())
        Validate.notNull(lang, "language cannot be null")

        contentDescription = null

        IF contentModel.getDescriptions() is not empty:
            FOR descriptionModel IN contentModel.getDescriptions():
                IF descriptionModel.getLanguage().getCode().equals(lang.getCode()):
                    contentDescription = descriptionModel
                    BREAK

        IF contentDescription == null:
            contentDescription = new ContentDescription()

        contentDescription.setMetatagDescription(objectContent.getMetaDescription())
        contentDescription.setTitle(objectContent.getTitle())
        contentDescription.setName(objectContent.getName())
        contentDescription.setSeUrl(objectContent.getFriendlyUrl())
        contentDescription.setDescription(objectContent.getDescription())
        contentDescription.setMetatagTitle(objectContent.getTitle())
        contentDescription.setContent(contentModel)
        contentDescription.setLanguage(lang)

        descriptions.add(contentDescription)

    RETURN descriptions
```

**Data Dependencies:**
- Reads: `content_description.content_id`, `language.language_id`, `language.code`, request `language`, `name`, `title`, `description`, `friendlyUrl`, `metaDescription`
- Writes: `content_description.name`, `content_description.title`, `content_description.description`, `content_description.sef_url`, `content_description.meta_title`, `content_description.meta_description`, `content_description.language_id`, `content_description.content_id`
- Constraint: `content_description(CONTENT_ID, LANGUAGE_ID)` unique constraint

**Side Effects:**
- Calls `LanguageService.getByCode()`
- Updates or creates `content_description` entities
- Replaces the `Content.descriptions` collection before persistence

**Concrete Example:**
- **API Input:** `PUT /api/v1/private/content/page/1203`  
  `{"code":"shipping","linkToMenu":true,"visible":true,"descriptions":[{"language":"en","name":"Shipping","friendlyUrl":"shipping","title":"Shipping","description":"Updated English text"},{"language":"fr","name":"Livraison","friendlyUrl":"livraison","title":"Livraison","description":"Texte français"}]}`
- **Success Output:** `200` with an empty body; the English description is updated and a French description is created
- **Error Input:** `{"code":"shipping","visible":true,"descriptions":[{"language":"xx","name":"Shipping","friendlyUrl":"shipping","title":"Shipping","description":"Text"}]}`
- **Error Output:** `500 {"message":"language cannot be null"}` when `LanguageService.getByCode("xx")` returns no language
- **Replacement example:** If German existed before but is omitted from the request, the legacy `setDescriptions(descriptions)` path does not include German in the submitted collection

---

### BR-MER-016: Language-specific and all-language reads use different projections

**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/facade/content/ContentFacadeImpl.java` : `getContentPage()` lines 414-436; `getContentBox()` lines 622-684; `convertContentToReadableContentPage()` lines 557-583; `convertContentToReadableContentBox()` lines 519-543  
**Cross-Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/content/ContentRepository.java` : language-filtered queries lines 8-18 and 38-44; `PageContentRepository.java` : language-filtered query lines 14-18  
**Discovery Method:** Direct Source Read | CAST Imaging (Hybrid)  
**CAST Reference:** Transaction 244049 `GET api/v1/content/pages/{}`; transaction 244062 `GET api/v1/content/boxes/{}`; transaction 244061 private box read

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 5 | 5 | OK |
| Data-flow | 9 | 9 | OK |
| Constants | 0 | 0 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 0 | 0 | OK |
| Integrations | 0 | 0 | OK |
| Error paths | 2 | 2 | OK |

**Preservation:** OK

**Statement:** A read with a requested language returns a single-language projection containing the matching localized description. A read without a language returns an all-language projection containing the available descriptions. A missing content code is an error in both modes.

**Intent:** Routing

**Acceptance Criteria:** Given a page with English and French descriptions, when English is requested, then the response contains the English description projection. When no language is supplied through the internal read path, then the response contains both descriptions.

**Logic:**
```pseudocode
getContentPage(code, store, language):
    IF language == null:
        content = contentService.getByCode(code, store)
    ELSE:
        content = contentService.getByCode(code, store, language)

    IF content == null:
        THROW ResourceNotFoundException("No page found : " + code)

    RETURN convertContentToReadableContentPage(store, language, content)

convertContentToReadableContentPage(store, language, content):
    IF language != null:
        page = new ReadableContentPage()
        description = findAppropriateContentDescription(content.getDescriptions(), language)
        IF description is present:
            page.setDescription(contentDescription(description))
        page.setCode(content.getCode())
        page.setId(content.getId())
        page.setVisible(content.isVisible())
        RETURN page
    ELSE:
        page = new ReadableContentPageFull()
        page.setDescriptions(content.getDescriptions().map(contentDescription))
        page.setCode(content.getCode())
        page.setId(content.getId())
        page.setVisible(content.isVisible())
        RETURN page
```

**Data Dependencies:**
- Reads: `content.content_id`, `content.code`, `content.visible`, `content_description.language_id`, `content_description.name`, `content_description.title`, `content_description.description`, `content_description.sef_url`
- Writes: none

**Side Effects:**
- Calls `ContentRepository.findByCode()` with or without `language_id`
- Produces either `ReadableContentPage` or `ReadableContentPageFull`
- No persistence side effect

**Concrete Example:**
- **API Input:** `GET /api/v1/content/pages/about-us?lang=en&store=DEFAULT`
- **Success Output:** `200 {"id":1204,"code":"about-us","visible":true,"description":{"language":"en","name":"About us","title":"About Us","description":"English text","friendlyUrl":"about-us"}}`
- **Error Input:** `GET /api/v1/content/pages/missing-page?lang=en&store=DEFAULT`
- **Error Output:** `500 {"message":"No page found : missing-page"}`
- **All-language internal projection:** `GET /api/v1/private/content/boxes/promo?store=DEFAULT` with no resolved language returns a `ReadableContentBoxFull` containing `descriptions:[...]`

---

### BR-MER-017: Public friendly-URL lookup exposes only visible content

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/content/ContentRepositoryImpl.java` : `getBySeUrl()` lines 42-76  
**Cross-Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/facade/content/ContentFacadeImpl.java` : `getContentPageByName()` lines 811-826; `contentDescriptionToReadableContent()` lines 152-172  
**Discovery Method:** Direct Source Read | CAST Imaging (Hybrid)  
**CAST Reference:** Transaction 244050 `GET api/v1/content/pages/name/{}`; CAST query for `content_description.seUrl` and `content.visible`

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 4 | 4 | OK |
| Data-flow | 6 | 6 | OK |
| Constants | 0 | 0 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 2 | 2 | OK |
| Data writes | 0 | 0 | OK |
| Integrations | 0 | 0 | OK |
| Error paths | 2 | 2 | OK |

**Preservation:** OK

**Statement:** A public page lookup by friendly URL is eligible only when the localized friendly URL belongs to the requested merchant store and the content item is visible. An invisible item must not be published through this lookup.

**Intent:** Authorization

**Acceptance Criteria:** Given a visible page with friendly URL `terms`, when `GET /content/pages/name/terms` is requested for its store, then the page is returned. If the same page is invisible, the public lookup returns no page.

**Logic:**
```pseudocode
getBySeUrl(store, seUrl):
    HQL =
        "select c from Content c " +
        "left join fetch c.descriptions cd " +
        "join fetch c.merchantStore cm " +
        "where cm.id = :cm " +
        "and c.visible = true " +
        "and cd.seUrl = :se"

    bind :cm = store.getId()
    bind :se = seUrl
    content = q.getSingleResult()

    IF content != null:
        RETURN content.getDescription()

    results = q.getResultList()
    IF results.isEmpty():
        RETURN null
    ELSE:
        content = results.get(0)

    IF content != null:
        RETURN content.getDescription()
    RETURN null

getContentPageByName(name, store, language):
    contentDescription = contentService.getBySeUrl(store, name)
    IF contentDescription == null:
        THROW ResourceNotFoundException("No page found : " + name)

    RETURN contentDescriptionToReadableContent(
        store,
        contentDescription.getContent(),
        contentDescription
    )
```

**Data Dependencies:**
- Reads: `content.content_id`, `content.merchant_id`, `content.visible`, `content_description.content_id`, `content_description.sef_url`, `content_description.name`, `content_description.title`, `content_description.description`
- Writes: none

**Side Effects:**
- Executes a merchant-scoped query
- Builds a public page path using `content_description.sef_url`
- No writes

**Concrete Example:**
- **API Input:** `GET /api/v1/content/pages/name/terms?store=DEFAULT&lang=en`
- **Success Output:** `200 {"id":"00000000-0000-0000-0000-000000001205","code":"terms","contentType":"Page","linkToMenu":false,"path":"/DEFAULT/terms","description":{"name":"Terms","friendlyUrl":"terms","title":"Terms","description":"Terms text"}}`
- **Error Input:** The same request after `content.visible` is set to `false`
- **Error Output:** `500 {"message":"No page found : terms"}`
- **Cross-store error:** A `terms` friendly URL in store `CA-STORE` is not returned when the request scope is store `DEFAULT`

---

### BR-MER-018: Visibility and menu linkage are independent content policies

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/content/Content.java` : fields `visible` and `linkToMenu` lines 65-72, 130-151; `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/facade/content/ContentFacadeImpl.java` : projections lines 519-583 and 622-684  
**Cross-Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/content/ContentRepositoryImpl.java` : visibility predicate lines 42-51; `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/content/ContentApi.java` : content mutation endpoints lines 176-323  
**Discovery Method:** Direct Source Read | CAST Imaging (Hybrid)  
**CAST Reference:** Transactions 244047/244048 content lists; transactions 244049/244062 public reads

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 3 | 3 | OK |
| Data-flow | 6 | 6 | OK |
| Constants | 0 | 0 | OK |
| State transitions | 2 | 2 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 2 | 2 | OK |
| Integrations | 0 | 0 | OK |
| Error paths | 1 | 1 | OK |

**Preservation:** OK

**Statement:** Content visibility and menu association are independent publication policies. Changing one does not automatically change the other, and a public friendly-URL lookup must exclude content that is not visible.

**Intent:** Compliance

**Acceptance Criteria:** A visible page may be excluded from menu navigation, and a page linked to the menu may be invisible. The target must preserve these as separate fields and define publication behavior explicitly rather than treating menu linkage as publication.

**Logic:**
```pseudocode
convertContentPageToContent(...):
    contentModel.setLinkToMenu(content.isLinkToMenu())
    contentModel.setVisible(content.isVisible())

convertContentToReadableContentPage(...):
    page.setVisible(content.isVisible())
    // linkToMenu is retained by the page model where that projection is used

getBySeUrl(...):
    query includes "and c.visible = true"

getByCode(...):
    ContentRepository.findByCode(code, store[, language])
    // query has no c.visible predicate
```

**Data Dependencies:**
- Reads: `content.visible`, `content.link_to_menu`, `content.code`, `content.merchant_id`
- Writes: `content.visible`, `content.link_to_menu`
- Reads for publication lookup: `content_description.sef_url`

**Side Effects:**
- Persists both flags independently
- Exposes `visible` and, in applicable projections, menu linkage
- Public friendly-URL lookup filters invisible content; code lookup does not

**Concrete Example:**
- **API Input:** `PUT /api/v1/private/content/page/1206`  
  `{"code":"help","linkToMenu":false,"visible":true,"descriptions":[{"language":"en","name":"Help","friendlyUrl":"help","title":"Help","description":"Help text"}]}`
- **Success Output:** `200`; the page is visible but not linked to the menu
- **Error Input:** `{"code":"help","linkToMenu":true,"visible":false,"descriptions":[{"language":"en","name":"Help","friendlyUrl":"help","title":"Help","description":"Help text"}]}`
- **Error Output:** No validation error is raised by the legacy mutation path. Target publication must prevent invisible `help` from public friendly-URL output while retaining `linkToMenu:true` as stored metadata

---

### BR-MER-019: Content lists are merchant-scoped, type-scoped, ordered, and paginated

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/content/PageContentRepository.java` : queries lines 9-22; `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/content/ContentServiceImpl.java` : paginated methods lines 522-534; `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/facade/content/ContentFacadeImpl.java` : `getContentPages()` lines 125-150 and `getContentBoxes()` lines 458-484  
**Cross-Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/content/ContentApi.java` : `pages()` lines 86-97; `boxes()` lines 118-130  
**Discovery Method:** Direct Source Read | CAST Imaging (Hybrid)  
**CAST Reference:** Transactions 244045 `GET api/v1/content/pages/`; 244047 `GET api/v1/content/boxes/`

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 3 | 3 | OK |
| Data-flow | 8 | 8 | OK |
| Constants | 1 | 1 | OK (`sortOrder ASC`) |
| State transitions | 0 | 0 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 0 | 0 | OK |
| Integrations | 0 | 0 | OK |
| Error paths | 1 | 1 | OK |

**Preservation:** OK

**Statement:** Page and box list operations return only the requested content type for the requested merchant store, order results by ascending `sortOrder`, and apply the supplied page and count values through the persistence paging query.

**Intent:** Routing

**Acceptance Criteria:** Given three pages with sort orders `20`, `5`, and `10`, when page `0` with count `2` is requested, then the first page contains the items ordered `5`, `10` and reports the persistence page totals.

**Logic:**
```pseudocode
listByType(contentType, store, page, count):
    pageRequest = PageRequest.of(page, count)
    RETURN pageContentRepository.findByContentType(
        contentType,
        store.getId(),
        pageRequest
    )

PageContentRepository.findByContentType:
    select c
    from Content c
    left join fetch c.descriptions cd
    join fetch c.merchantStore cm
    where c.contentType = ?1
      and cm.id = ?2
    order by c.sortOrder asc

getContentPages(...):
    contentPages = contentService.listByType(ContentType.PAGE, store, page, count)
    items.totalPages = contentPages.getTotalPages()
    items.number = contentPages.getContent().size()
    items.recordsTotal = contentPages.getNumberOfElements()
    items.items = contentPages.content.map(convertContentToReadableContentBox)
```

**Data Dependencies:**
- Reads: `content.content_type`, `content.merchant_id`, `content.sort_order`, `content.content_id`, `content_description.content_id`
- Writes: none

**Side Effects:**
- Executes a paginated database query
- Builds `ReadableEntityList` metadata
- **Defect:** `getContentPages()` maps page records through `convertContentToReadableContentBox()` rather than a page converter

**Concrete Example:**
- **API Input:** `GET /api/v1/content/pages?page=0&count=2&store=DEFAULT&lang=en`
- **Success Output:** `200 {"items":[{"id":1210,"code":"home","visible":true},{"id":1211,"code":"about","visible":true}],"totalPages":2,"number":2,"recordsTotal":3}` ordered by `sortOrder`
- **Error Input:** `GET /api/v1/content/pages?page=0&count=2&store=DEFAULT` with a nonnumeric paging parameter
- **Error Output:** `400` request-binding error
- **Target-only correction:** The page-list target projection should use page fields and not emit a box-specific `contentType` default merely because the legacy mapper does so

---

### BR-MER-020: Localized content projection preserves domain fields but applies endpoint-specific formatting

**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/facade/content/ContentFacadeImpl.java` : `contentDescription()` lines 585-596; `getContentBox()` lines 622-684; `convertContentToReadableContentPage()` lines 557-583; `fixContentDescription()` lines 686-690  
**Cross-Reference:** `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/model/content/common/ContentDescription.java` : inherited fields from `NamedEntity`; `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/model/content/page/ReadableContentPage.java` : response fields lines 1-31  
**Discovery Method:** Direct Source Read | CAST Imaging (Hybrid)  
**CAST Reference:** Transaction 244062 box read; transaction 244049 page read; CAST call graph from `contentDescription`

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 4 | 4 | OK |
| Data-flow | 8 | 8 | OK |
| Constants | 3 | 3 | OK (`<![CDATA[`, CR/LF removal, tab removal) |
| State transitions | 0 | 0 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 8 | 8 | OK |
| Integrations | 0 | 0 | OK |
| Error paths | 1 | 1 | OK |

**Preservation:** OK

**Statement:** A localized response exposes the language code, name, title, description, friendly URL, and identifier of the selected description. The language-specific content-box read wraps its description in CDATA after removing carriage-return/newline and tab characters; the page projection returns the description without that CDATA transformation.

**Intent:** Routing

**Acceptance Criteria:** A box containing `"\r\nPromo\ttext"` returns `<![CDATA[Promo text]]>` through the language-specific box endpoint. The page endpoint returns the source description value without the box-specific wrapper.

**Logic:**
```pseudocode
contentDescription(description):
    Validate.notNull(description, "ContentDescription cannot be null")
    desc.description = description.getDescription()
    desc.name = description.getName()
    desc.title = description.getTitle()
    desc.friendlyUrl = description.getSeUrl()
    desc.id = description.getId()
    desc.language = description.getLanguage().getCode()
    RETURN desc

getContentBox(code, store, language):
    content = contentService.getByCode(code, store, language)
    selected = findAppropriateContentDescription(content.getDescriptions(), language)

    IF selected.present:
        desc = contentDescription(selected)
        desc.description = fixContentDescription(desc.description)
        box.description = desc

fixContentDescription(description):
    RETURN "<![CDATA[" +
        description.replaceAll("\r\n", "").replaceAll("\t", "") +
        "]]>"

convertContentToReadableContentPage(...):
    selected = findAppropriateContentDescription(...)
    IF selected.present:
        page.description = contentDescription(selected)
    // no fixContentDescription call
```

**Data Dependencies:**
- Reads: `content_description.description_id`, `content_description.language_id`, `content_description.name`, `content_description.title`, `content_description.description`, `content_description.sef_url`, `content_description.content_id`
- Writes: none

**Side Effects:**
- Produces endpoint-specific DTO projections
- Applies CDATA transformation only in the language-specific box path

**Concrete Example:**
- **API Input:** `GET /api/v1/content/boxes/promo?store=DEFAULT&lang=en`
- **Success Output:** `200 {"id":1212,"code":"promo","visible":true,"description":{"id":4012,"language":"en","name":"Promo","title":"Promo","friendlyUrl":"promo","description":"<![CDATA[Save today]]>"}}`
- **Error Input:** A box description with source value `"Save\r\n today\t"`
- **Error Output:** Not an error; legacy success output is `description:"<![CDATA[Save today]]>"`
- **Target-only requirement:** CDATA wrapping must be an explicit presentation policy, not a hidden mutation of stored `content_description.description`

---

### BR-MER-021: Content deletion is restricted to the owning merchant store

**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/facade/content/ContentFacadeImpl.java` : `delete(MerchantStore, Long)` lines 749-768; `deleteContent()` lines 879-899  
**Cross-Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/content/ContentServiceImpl.java` : `getById(Long, MerchantStore)` lines 446-457; `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/content/ContentApi.java` : delete endpoints lines 255-289  
**Discovery Method:** Direct Source Read | CAST Imaging (Hybrid)  
**CAST Reference:** Transactions 244055 page delete; 244056 box delete; 244068 deprecated generic delete

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 5 | 5 | OK |
| Data-flow | 5 | 5 | OK |
| Constants | 0 | 0 | OK |
| State transitions | 1 | 1 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 1 | 1 | OK |
| Integrations | 0 | 0 | OK |
| Error paths | 2 | 2 | OK |

**Preservation:** OK

**Statement:** A content item may be deleted only through the merchant-store scope to which it belongs. An identifier belonging to another merchant store must be treated as not found and must not be deleted.

**Intent:** Authorization

**Acceptance Criteria:** Given content ID `1213` belongs to `DEFAULT`, a delete scoped to `DEFAULT` succeeds. The same ID scoped to `CA-STORE` is rejected without a delete.

**Logic:**
```pseudocode
delete(store, id):
    content = contentService.getById(id)

    IF content != null:
        IF content.getMerchantStore().getId().intValue()
             != store.getId().intValue():
            THROW ResourceNotFoundException(
                "No content found with id [" + id +
                "] for store [" + store.getCode() + "]"
            )

    contentService.delete(content)

deleteContent(id, merchantStore):
    content = contentService.getById(id, merchantStore)
    IF content == null:
        THROW ConstraintException(
            "Content with id [" + id +
            "] does not exist for store [" + merchantStore.getCode() + "]"
        )
    contentService.delete(content)
```

**Data Dependencies:**
- Reads: `content.content_id`, `content.merchant_id`
- Writes: delete from `content`; cascaded delete of associated `content_description` rows
- Scope comparison: `merchant_store.merchant_id`

**Side Effects:**
- Calls `ContentService.delete()`
- Removes the content entity and its cascade-managed descriptions
- Cross-store access produces a not-found or constraint exception

**Concrete Example:**
- **API Input:** `DELETE /api/v1/private/content/page/1213?store=DEFAULT`
- **Success Output:** `200` with an empty body and content ID `1213` removed
- **Error Input:** `DELETE /api/v1/private/content/page/1213?store=CA-STORE` when ID `1213` belongs to `DEFAULT`
- **Error Output:** `500 {"message":"No content found with id [1213] for store [CA-STORE]"}`

---

### BR-MER-022: Uploaded files are classified by the MIME-type major component

**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/facade/content/ContentFacadeImpl.java` : `addContentFile()` lines 487-509; `getFileContentType()` lines 510-517  
**Cross-Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/content/ContentServiceImpl.java` : `addContentFile()` lines 146-169  
**Discovery Method:** Direct Source Read | CAST Imaging (Hybrid)  
**CAST Reference:** Transactions 244039 `POST api/v1/private/content/images/add`; 244013 file/module call graph

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 6 | 6 | OK |
| Data-flow | 7 | 7 | OK |
| Constants | 4 | 4 | OK (`image`, `API_IMAGE`, `API_FILE`, `STATIC_FILE`) |
| State transitions | 2 | 2 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 3 | 3 | OK |
| Integrations | 1 | 1 | OK (`ContentService`) |
| Error paths | 1 | 1 | OK |

**Preservation:** OK

**Statement:** An upload submitted through the content facade is classified as an API image when the MIME-type major component is `image`; all other MIME-type major components are classified as a static file. The service converts `API_IMAGE` to `IMAGE` and `API_FILE` to `STATIC_FILE` before storage.

**Intent:** Routing

**Acceptance Criteria:** `hero.png` with MIME type `image/png` is stored in the `IMAGE` category. `manual.pdf` with MIME type `application/pdf` is stored in the `STATIC_FILE` category.

**Logic:**
```pseudocode
addContentFile(file, merchantStoreCode):
    payload = file.getFile()
    fileName = file.getName()

    type = file.getContentType().split("/")[0]
    fileType = getFileContentType(type)

    cmsContent = new InputContentFile()
    cmsContent.setFileName(fileName)
    cmsContent.setMimeType(file.getContentType())
    cmsContent.setFile(new ByteArrayInputStream(payload))
    cmsContent.setFileContentType(fileType)

    contentService.addContentFile(merchantStoreCode, cmsContent)

getFileContentType(type):
    fileType = FileContentType.STATIC_FILE
    IF type.equals("image"):
        fileType = FileContentType.API_IMAGE
    RETURN fileType

ContentService.addContentFile(...):
    contentFile.setMimeType(URLConnection.guessContentTypeFromName(fileName))

    IF fileContentType == IMAGE OR fileContentType == STATIC_FILE:
        addFile(...)
    ELSE IF fileContentType == API_IMAGE:
        contentFile.setFileContentType(IMAGE)
        addImage(...)
    ELSE IF fileContentType == API_FILE:
        contentFile.setFileContentType(STATIC_FILE)
        addFile(...)
    ELSE:
        addImage(...)
```

**Data Dependencies:**
- Reads: upload `file.name`, `file.content_type`, `file.bytes`; `merchant_store.code`
- Writes: provider object under the classified file type namespace
- No relational content row is created for an uploaded content file

**Side Effects:**
- Calls `ContentService.addContentFile()`
- Calls the configured `StaticContentFileManager`
- Closes the input stream in `addImage()` or `addFile()`

**Concrete Example:**
- **API Input:** `POST /api/v1/private/content/images/add` multipart fields `qqfilename=hero.png`, `qqfile` with MIME `image/png`, store code `DEFAULT`
- **Success Output:** `201 {"success":true,"error":null,"preventRetry":true}` and object stored under the store's `IMAGE` namespace
- **Error Input:** `POST /api/v1/private/file` with `file=manual.pdf`, MIME `application/pdf`, and bytes readable
- **Error Output:** No classification error; success is `201` and the object is stored under `STATIC_FILE`. A storage failure is surfaced as a service exception

---

### BR-MER-023: File-manager image uploads validate the submitted filename before storage

**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/content/ContentAdministrationApi.java` : `upload()` lines 123-162  
**Cross-Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/content/ContentApi.java` : generic `upload()` lines 329-353 and `uploadMultipleFiles()` lines 355-379  
**Discovery Method:** Direct Source Read | CAST Imaging (Hybrid)  
**CAST Reference:** Transaction 244039 `POST api/v1/private/content/images/add`

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 4 | 4 | OK |
| Data-flow | 8 | 8 | OK |
| Constants | 1 | 1 | OK (`Invalid filename`) |
| State transitions | 1 | 1 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 1 | 1 | OK |
| Integrations | 1 | 1 | OK (`FileNameUtils`) |
| Error paths | 2 | 2 | OK |

**Preservation:** OK

**Statement:** The administrative image-upload operation accepts only safe basenames. An invalid filename returns a failed upload result and does not call content storage. The separate generic file-upload endpoints do not apply this stricter image-upload validation.

**Intent:** Validation

**Acceptance Criteria:** An upload named `hero.png` passes filename validation and is sent to storage. An upload named `../hero.png` is rejected with `success:false` and `error:"Invalid filename"`.

**Logic:**
```pseudocode
upload(qqfile, qquuid, qqfilename, ...):
    IF NOT fileNameUtils.validFileName(qqfilename):
        fs = new FileStatus()
        fs.setError("Invalid filename")
        fs.setSuccess(false)
        RETURN fs

    cf = new ContentFile()
    cf.setContentType(qqfile.getContentType())
    cf.setName(qqfilename)

    TRY:
        cf.setFile(qqfile.getBytes())
        contentFacade.addContentFile(cf, merchantStore.getCode())
        RETURN new FileStatus()
    CATCH IOException e:
        fs = new FileStatus()
        fs.setError(e.getMessage())
        fs.setSuccess(false)
        RETURN fs
```

**Data Dependencies:**
- Reads: multipart `qqfilename`, `qqfile.content_type`, `qqfile.bytes`, `merchant_store.code`
- Writes: provider content object only after filename validation succeeds

**Side Effects:**
- Calls `FileNameUtils.validFileName()`
- Calls `ContentFacade.addContentFile()` only for valid names
- Returns `FileStatus.preventRetry = true` by default

**Concrete Example:**
- **API Input:** `POST /api/v1/private/content/images/add` with multipart `qqfilename=hero.png`, `qqfile=image/png`
- **Success Output:** `201 {"success":true,"error":null,"preventRetry":true}`
- **Error Input:** Same endpoint with `qqfilename=../hero.png`
- **Error Output:** `201 {"success":false,"error":"Invalid filename","preventRetry":true}`; the legacy method returns a failure object while retaining the endpoint's `201` annotation
- **Target-only requirement:** The target should use a semantically correct `400` or `422` for invalid filename while preserving the exact error reason

---

### BR-MER-024: Content files are isolated by merchant and file-content type

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/cms/content/ContentAssetsManager.java` : `nodePath()` lines 31-52; `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/cms/content/infinispan/CmsStaticContentFileManagerImpl.java` : `addFile()` lines 108-128; `getFile()` lines 198-233; `removeFile()` lines 280-300  
**Cross-Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/content/ContentServiceImpl.java` : file delegation lines 192-244, 358-410  
**Discovery Method:** Direct Source Read | CAST Imaging (Hybrid)  
**CAST Reference:** Transaction 244064 image listing; transaction 244040 download; transaction 244043 image removal

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 4 | 4 | OK |
| Data-flow | 8 | 8 | OK |
| Constants | 4 | 4 | OK (`files`, store, type namespace) |
| State transitions | 0 | 0 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 2 | 2 | OK |
| Integrations | 1 | 1 | OK (`StaticContentFileManager`) |
| Error paths | 2 | 2 | OK |

**Preservation:** OK

**Statement:** A stored content file is addressed by merchant store code, file-content type, and file name. Files with the same name in different stores or different content-type namespaces are distinct. Writing the same name into the same namespace replaces the existing object in the cache and local providers.

**Intent:** Routing

**Acceptance Criteria:** `logo.png` for store `DEFAULT` is not returned for store `CA-STORE`. `logo.png` in `IMAGE` is distinct from `logo.png` in `LOGO`. Re-uploading `hero.png` to the same store and namespace replaces the existing bytes.

**Logic:**
```pseudocode
ContentAssetsManager.nodePath(store, type):
    root = nodePath(store) // "files/" + store + "/"
    IF type != null
       AND type != IMAGE
       AND type != STATIC_FILE:
        root = root + type.name() + "/"
    RETURN root

Infinispan.addFile(merchantStoreCode, path, input):
    nodePath = getNodePath(
        merchantStoreCode,
        input.getFileContentType()
    )
    merchantNode = getNode(nodePath)
    merchantNode.put(
        input.getFileName(),
        IOUtils.toByteArray(input.getFile())
    )

Infinispan.getFile(store, path, fileType, fileName):
    merchantNode = getNode(getNodePath(store, fileType))
    fileBytes = merchantNode.get(fileName)
    IF fileBytes == null:
        RETURN null
    output.file = fileBytes
    output.mimeType = URLConnection.getFileNameMap()
        .getContentTypeFor(fileName)
    output.fileName = fileName
    output.fileContentType = fileType
    RETURN output
```

**Data Dependencies:**
- Reads: `merchant_store.code`; provider key `files/<merchantStoreCode>/<fileContentType>/<fileName>`
- Writes: provider object bytes at the same key
- No relational file table is used by the listed CMS file path

**Side Effects:**
- Calls provider-neutral `StaticContentFileManager`
- Cache provider `Node.put()` replaces an existing same-name value
- Retrieval returns `null` for absent cache bytes

**Concrete Example:**
- **API Input:** `POST /api/v1/private/content/images/add` with `qqfilename=hero.png`, store `DEFAULT`
- **Success Output:** `201 {"success":true,"error":null,"preventRetry":true}`; key is equivalent to `files/DEFAULT/IMAGE/hero.png`
- **Error Input:** `GET /api/v1/content/images/download?path=/DEFAULT/hero.png&store=CA-STORE`
- **Error Output:** The store-scoped lookup must not return `DEFAULT` bytes; target response should be `404 {"message":"File hero.png was not found for store CA-STORE"}`
- **Replacement example:** A second upload of `hero.png` to `DEFAULT` replaces the first provider value

---

### BR-MER-025: File rename is a read-remove-recreate sequence and is not atomic

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/content/ContentServiceImpl.java` : `renameFile()` lines 495-520  
**Cross-Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/facade/content/ContentFacadeImpl.java` : `renameFile()` lines 829-838; `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/content/ContentAdministrationApi.java` : `rename()` lines 186-211  
**Discovery Method:** Direct Source Read | CAST Imaging (Hybrid)  
**CAST Reference:** Transaction 244042 `POST api/v1/private/content/images/rename`; CAST method 30127 `renameFile`

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 3 | 3 | OK |
| Data-flow | 8 | 8 | OK |
| Constants | 1 | 1 | OK (not-found message) |
| State transitions | 1 | 1 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 2 | 2 | OK |
| Integrations | 1 | 1 | OK |
| Error paths | 2 | 2 | OK |

**Preservation:** OK

**Statement:** Renaming a file preserves its bytes and media metadata under the new name. The operation must either be atomic or expose a recoverable failure that prevents an apparent success when the original has already been removed.

**Intent:** State Transition

**Acceptance Criteria:** Renaming `hero.png` to `banner.png` preserves bytes and MIME type. Renaming a missing file returns a not-found error. A provider failure during recreation must not leave an unexplained successful response.

**Logic:**
```pseudocode
renameFile(merchantStoreCode, fileContentType, path, originalName, newName):
    file = contentFileManager.getFile(
        merchantStoreCode,
        path,
        fileContentType,
        originalName
    )

    IF file == null:
        THROW ServiceException(
            "File name [" + originalName +
            "] not found for merchant [" + merchantStoreCode + "]"
        )

    os = file.getFile()
    is = new ByteArrayInputStream(os.toByteArray())

    contentFileManager.removeFile(
        merchantStoreCode,
        fileContentType,
        originalName,
        path
    )

    inputFile = new InputContentFile()
    inputFile.setFileContentType(fileContentType)
    inputFile.setFileName(newName)
    inputFile.setMimeType(file.getMimeType())
    inputFile.setFile(is)

    contentFileManager.addFile(
        merchantStoreCode,
        path,
        inputFile
    )
```

**Data Dependencies:**
- Reads: provider key for `originalName`, original bytes, original MIME type
- Writes: delete provider key for `originalName`; create provider key for `newName`
- Scope: `merchant_store.code`

**Side Effects:**
- Calls provider `getFile()`, `removeFile()`, and `addFile()`
- Removes the old object before adding the new object
- Returns a `FileStatus` from the controller

**Concrete Example:**
- **API Input:** `POST /api/v1/private/content/images/rename?path=%2Fhero.png&newName=banner.png&store=DEFAULT`
- **Success Output:** `200 {"success":true,"error":null,"preventRetry":true}`; `hero.png` is absent and `banner.png` contains the original bytes
- **Error Input:** `path=/missing.png&newName=banner.png&store=DEFAULT`
- **Error Output:** `200 {"success":false,"error":"File name [missing.png] not found for merchant [DEFAULT]","preventRetry":true}`
- **Target-only failure case:** If removal succeeds and recreation fails, target response must be an error and must preserve or restore `hero.png`; the legacy sequence does not provide that guarantee

---

### BR-MER-026: Folder paths use Linux-style directory syntax, but folder enumeration and deletion are incomplete

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/content/ContentServiceImpl.java` : `addFolder()` lines 459-472; `isValidLinuxDirectory()` lines 490-493; `listFolders()` lines 474-479; `removeFolder()` lines 481-488  
**Cross-Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/cms/content/infinispan/CmsStaticContentFileManagerImpl.java` : `addFolder()` lines 427-458; `removeFolder()` and `listFolders()` lines 460-470; `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/cms/content/local/CmsStaticContentFileManagerImpl.java` : folder methods lines 399-477  
**Discovery Method:** Direct Source Read | CAST Imaging (Hybrid)  
**CAST Reference:** Transaction 244038 `GET api/v1/private/content/folder`; transaction 244063 `DELETE api/v1/content/folder`

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 5 | 5 | OK |
| Data-flow | 5 | 5 | OK |
| Constants | 2 | 3 | GAP (target rejects path characters accepted by the legacy pattern) |
| State transitions | 2 | 2 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 1 | 1 | OK |
| Integrations | 1 | 1 | OK |
| Error paths | 2 | 2 | OK |

**Preservation:** GAP — the target intentionally tightens path validation beyond the legacy regular expression; this policy change is forwarded to Phase 4a.

**Statement:** The target accepts only `/` or slash-prefixed folder segments containing letters, digits, underscores, and hyphens. Folder creation, listing, and deletion capabilities must be explicit because storage backends do not provide identical folder semantics; the legacy implementation accepted some broader paths.

**Intent:** Validation

**Acceptance Criteria:** `/marketing/summer-2026` is a valid folder path. `/marketing/summer 2026` is rejected by the target validation policy, even though the legacy regular expression accepted it. The target must not claim portable folder listing or recursive deletion unless those capabilities are implemented.

**Logic:**
```pseudocode
isValidLinuxDirectory(path):
    linuxDirectoryPattern = "^/|(/[a-zA-Z0-9_-]+)+$"
    RETURN path != null
       AND NOT path.trim().isEmpty()
       AND linuxDirectoryPattern.matcher(path).matches()

addFolder(store, path, folderName):
    Validate.notNull(store, "MerchantStore cannot be null")
    Validate.notNull(folderName, "Folder name cannot be null")

    IF path.isPresent():
        IF NOT isValidLinuxDirectory(path.get()):
            THROW ServiceException(
                "Path format [" + path.get() +
                "] not a valid directory format"
            )

    contentFileManager.addFolder(store.getCode(), folderName, path)

Infinispan.addFolder(...):
    nodePath = getNodePath(store, FileContentType.IMAGE)
    append optional path
    create parent node if absent
    append folderName
    add child folder node

Infinispan.removeFolder(...):
    // TODO — no removal behavior

Infinispan.listFolders(...):
    // TODO — returns null
```

**Data Dependencies:**
- Reads: `merchant_store.code`; provider folder path
- Writes: provider folder node/directory under the image namespace
- No relational folder table is used

**Side Effects:**
- Calls provider folder operations
- Invalid paths produce a service exception
- Infinispan `removeFolder()` and `listFolders()` are TODO implementations
- Local `listFolders()` returns `null`

**Concrete Example:**
- **API Input:** Target folder operation equivalent to `POST /api/v1/private/content/folders`  
  `{"path":"/marketing/summer-2026","folderName":"banners","store":"DEFAULT"}`
- **Success Output:** `201 {"path":"/marketing/summer-2026/banners"}`
- **Error Input:** `{"path":"/marketing/summer 2026","folderName":"banners","store":"DEFAULT"}`
- **Error Output:** `400 {"message":"Path format [/marketing/summer 2026] not a valid directory format"}`
- **Legacy defect:** `GET /api/v1/private/content/folder?path=/marketing&store=DEFAULT` can return a folder containing files but provider `listFolders()` itself is `null`; target must specify a real folder-list contract rather than copy the null behavior

---

### BR-MER-027: Legacy download and folder controller operations contain explicit nonfunctional behavior

**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/content/ContentAdministrationApi.java` : `download()` lines 164-184; `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/content/ContentApi.java` : `addFolder()` lines 324-328; deprecated `pagesSummary()` lines 99-109  
**Cross-Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/facade/content/ContentFacadeImpl.java` : `download()` lines 840-850; `getContentBoxes(ContentType,String,...)` lines 439-456  
**Discovery Method:** Direct Source Read | CAST Imaging (Hybrid)  
**CAST Reference:** Transaction 244040 `GET api/v1/content/images/download`; transaction 244063 `DELETE api/v1/content/folder`; transaction 244046 `GET api/v1/content/summary`

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 4 | 4 | OK |
| Data-flow | 4 | 4 | OK |
| Constants | 2 | 2 | OK (`null`, error prefix) |
| State transitions | 0 | 0 | OK |
| Outcomes | 4 | 4 | OK |
| Data writes | 0 | 0 | OK |
| Integrations | 2 | 2 | OK (`ContentFacade.download`, content-box prefix path) |
| Error paths | 3 | 3 | OK |

**Preservation:** OK — defect behavior recorded explicitly

**Statement:** A content API must not represent an unimplemented download, folder, summary, or compatibility operation as a successful content response with an empty value. Each exposed operation must either perform its declared function or return an explicit unsupported/retired error.

**Intent:** Routing

**Acceptance Criteria:** A target download endpoint must either return file bytes or a documented not-found/error response. A target folder-create/list endpoint must implement its declared operation or be removed from the public contract.

**Logic:**
```pseudocode
ContentAdministrationApi.download(path, store, language):
    fileName = path.substring(path.lastIndexOf("/") + 1, path.length())
    TRY:
        // contentFacade.download(...) is commented out
        RETURN null
    CATCH Exception e:
        THROW ServiceRuntimeException(
            "Error while downloading file [" + fileName + "]"
        )

ContentApi.addFolder(parent, folder, store, language):
    // empty method body; no facade call and no storage write

ContentApi.pagesSummary(...):
    // contentFacade.getContentBoxes(...) is commented out
    RETURN null

ContentFacadeImpl.getContentBoxes(type, codePrefix, store, language, page, count):
    // intended contentService.getByCodeLike(...) is commented out
    RETURN null
```

**Data Dependencies:**
- Download would read provider file bytes but the active controller does not do so
- Prefix listing would read `content.code`, `content.content_type`, `content.merchant_id`, `content_description.language_id`, but the active implementation does not query them
- Writes: none for these inactive paths

**Side Effects:**
- Legacy download returns a null body
- Legacy folder endpoint produces no side effect
- Deprecated summary and prefix operations return null
- Target must expose explicit `404`, `501`, or a real implementation rather than silent null success

**Concrete Example:**
- **API Input:** `GET /api/v1/content/images/download?path=%2Fhero.png&store=DEFAULT`
- **Success Output:** **Legacy defect:** `200` with a `null` body; **target-only required output:** `200` with `image/png` bytes when found
- **Error Input:** The same request for `missing.png`
- **Error Output:** **Target-only required output:** `404 {"message":"File missing.png was not found for store DEFAULT"}`
- **Legacy folder input:** `DELETE /api/v1/content/folder?parent=/&folder=banners&store=DEFAULT`
- **Legacy output:** `201` with an empty body and no folder operation; target must not preserve this status/behavior

---

### BR-MER-028: Image listings are store-scoped and expose generated static-image paths

**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/facade/content/ContentFacadeImpl.java` : `getContentFolder()` lines 75-95; `convertToContentImage()` lines 97-103; `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/content/ContentAdministrationApi.java` : `list()` lines 83-96 and `convertToImageFile()` lines 240-249  
**Cross-Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/content/ContentServiceImpl.java` : `getContentFilesNames()` lines 402-410  
**Discovery Method:** Direct Source Read | CAST Imaging (Hybrid)  
**CAST Reference:** Transaction 244064 `GET api/v1/content/images/`; transaction 244038 `GET api/v1/private/content/folder`

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 3 | 3 | OK |
| Data-flow | 7 | 7 | OK |
| Constants | 3 | 3 | OK (`IMAGE`, encoded path, generated static path) |
| State transitions | 0 | 0 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 0 | 0 | OK |
| Integrations | 1 | 1 | OK (`ImageFilePath`) |
| Error paths | 1 | 1 | OK |

**Preservation:** OK

**Statement:** An image listing is scoped to the merchant store and retrieves names from the `IMAGE` file namespace. Each returned image includes the file name and a generated static-image path derived from the store and file name. The requested folder path is URL-decoded for the administration list, except blank paths and paths containing `/images`, which resolve to `/`.

**Intent:** Routing

**Acceptance Criteria:** Listing images for store `DEFAULT` returns only that store's image names and generated URLs. `parentPath=%2Fimages` resolves to the root path `/`.

**Logic:**
```pseudocode
decodeContentPath(path):
    IF StringUtils.isBlank(path) OR path.contains("/images"):
        RETURN "/"
    RETURN URLDecoder.decode(path.replaceAll(",", ""), "UTF-8")

getContentFolder(folder, store):
    imageNames = Optional.ofNullable(
        contentService.getContentFilesNames(
            store.getCode(),
            FileContentType.IMAGE
        )
    ).orElseThrow(
        ResourceNotFoundException("No Folder found for path : " + folder)
    )

    contentImages = imageNames.map(
        name -> convertToContentImage(name, store)
    )

    contentFolder = new ContentFolder()
    IF NOT StringUtils.isBlank(folder):
        contentFolder.path = URLEncoder.encode(folder, "UTF-8")
    contentFolder.content.addAll(contentImages)
    RETURN contentFolder

convertToContentImage(name, store):
    contentImage.name = name
    contentImage.path = absolutePath(store, null)
```

**Data Dependencies:**
- Reads: `merchant_store.code`; provider image names
- Writes: none
- Generated path uses `merchant_store.code` and `content file name`

**Side Effects:**
- Calls `ContentService.getContentFilesNames()`
- Calls `ImageFilePath.buildStaticImageUtils()`
- Returns `ContentFolder` or administration `ImageFile` projections

**Concrete Example:**
- **API Input:** `GET /api/v1/private/content/list?parentPath=%2Fimages&store=DEFAULT`
- **Success Output:** `200 [{"dir":false,"name":"hero.png","id":"/static/DEFAULT/hero.png","url":"/static/DEFAULT/hero.png","path":"image.png","size":null}]`
- **Error Input:** `GET /api/v1/private/content/list?parentPath=%2Fmissing%2Cpath&store=DEFAULT` when the provider fails
- **Error Output:** `500 {"message":"Error while getting folder /missingpath"}`
- **Isolation example:** An image stored for `CA-STORE` is not included in a `DEFAULT` listing

---

## Merchant Configuration Rules

### BR-CF-001: Merchant configuration records are keyed by merchant store and configuration key

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/system/MerchantConfigurationRepository.java` : queries lines 10-23; `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/system/MerchantConfiguration.java` : table and fields lines 35-88  
**Cross-Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/system/MerchantConfigurationServiceImpl.java` : `getMerchantConfiguration()` lines 30-33; `saveOrUpdate()` lines 45-56  
**Discovery Method:** Direct Source Read | CAST Imaging (Hybrid)  
**CAST Reference:** Transactions 244237 public configuration; 244108 payment configuration; 244206 shipping configuration

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 3 | 3 | OK |
| Data-flow | 6 | 6 | OK |
| Constants | 1 | 1 | OK (merchant key) |
| State transitions | 1 | 1 | OK |
| Outcomes | 2 | 2 | OK |
| Data writes | 2 | 2 | OK |
| Integrations | 0 | 0 | OK |
| Error paths | 0 | 0 | OK |

**Preservation:** OK

**Statement:** A merchant configuration is selected by the combination of merchant store and configuration key. The same key may exist for different stores, but a store has one configuration record for a given key.

**Intent:** Routing

**Acceptance Criteria:** Reading key `PAYMENT_MODULES` for store `DEFAULT` must never return the encrypted configuration belonging to `CA-STORE`. Saving a known key updates the existing store-scoped record rather than creating an unrelated record.

**Logic:**
```pseudocode
getMerchantConfiguration(key, store):
    RETURN merchantConfigurationRepository
        .findByMerchantStoreAndKey(store.getId(), key)

findByMerchantStoreAndKey:
    select m
    from MerchantConfiguration m
    join fetch m.merchantStore ms
    where ms.id = ?1
      and m.key = ?2

saveOrUpdate(entity):
    IF entity.getId() != null AND entity.getId() > 0:
        super.update(entity)
    ELSE:
        super.create(entity)
```

**Data Dependencies:**
- Reads: `merchant_configuration.merchant_id`, `merchant_configuration.config_key`, `merchant_configuration.merchant_config_id`
- Writes: `merchant_configuration.merchant_id`, `merchant_configuration.config_key`, `merchant_configuration.value`, `merchant_configuration.type`, `merchant_configuration.active`
- Constraint: `merchant_configuration(MERCHANT_ID, CONFIG_KEY)` unique constraint

**Side Effects:**
- Reads and writes merchant configuration records
- Preserves the merchant-store relationship on new configuration creation

**Concrete Example:**
- **API Input:** Internal configuration save for store `DEFAULT`:  
  `{"key":"PAYMENT_MODULES","value":"<encrypted-json>","type":"INTEGRATION"}`
- **Success Output:** `200 {"merchantConfigId":501,"key":"PAYMENT_MODULES","merchantStore":"DEFAULT"}`
- **Error Input:** A read request for `key:"PAYMENT_MODULES"` under store `CA-STORE` when only `DEFAULT` has that key
- **Error Output:** `404 {"message":"Configuration PAYMENT_MODULES was not found for store CA-STORE"}`

---

### BR-CF-002: Merchant configuration JSON uses typed flags and language-keyed search settings

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/system/MerchantConfig.java` : `toJSONString()` lines 31-70 and fields lines 18-29  
**Cross-Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/system/MerchantConfigurationServiceImpl.java` : `getMerchantConfig()` lines 68-85; `saveMerchantConfig()` lines 87-108  
**Discovery Method:** Direct Source Read | CAST Imaging (Hybrid)  
**CAST Reference:** CAST method 13363 `getMerchantConfig`; transaction 244237 public config

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 5 | 5 | OK |
| Data-flow | 15 | 15 | OK |
| Constants | 0 | 0 | OK |
| State transitions | 1 | 1 | OK |
| Outcomes | 2 | 2 | OK |
| Data writes | 12 | 12 | OK |
| Integrations | 1 | 1 | OK (`ObjectMapper`) |
| Error paths | 1 | 1 | OK |

**Preservation:** OK

**Statement:** Merchant configuration serializes the commerce-display flags as JSON booleans. Search defaults are represented as language-code-to-boolean and language-code-to-path maps. Null search maps are omitted; blank search paths are omitted; nonblank paths and non-null booleans are retained.

**Intent:** Calculation

**Acceptance Criteria:** A configuration with `allowPurchaseItems:true`, `displaySearchBox:false`, and `useDefaultSearchConfig.en:true` serializes those values as booleans. A blank search path is not emitted.

**Logic:**
```pseudocode
MerchantConfig.toJSONString():
    data["displayCustomerSection"] = isDisplayCustomerSection()
    data["displayContactUs"] = isDisplayContactUs()
    data["displayStoreAddress"] = isDisplayStoreAddress()
    data["displayAddToCartOnFeaturedItems"] =
        isDisplayAddToCartOnFeaturedItems()
    data["displayPagesMenu"] = isDisplayPagesMenu()
    data["displayCustomerAgreement"] = isDisplayCustomerAgreement()
    data["allowPurchaseItems"] = isAllowPurchaseItems()
    data["displaySearchBox"] = displaySearchBox
    data["testMode"] = isTestMode()
    data["debugMode"] = isDebugMode()

    IF useDefaultSearchConfig != null:
        obj = {}
        FOR key IN useDefaultSearchConfig.keySet():
            val = useDefaultSearchConfig.get(key)
            IF val != null:
                obj[key] = val
        data["useDefaultSearchConfig"] = obj

    IF defaultSearchConfigPath != null:
        obj = {}
        FOR key IN defaultSearchConfigPath.keySet():
            val = defaultSearchConfigPath.get(key)
            IF NOT StringUtils.isBlank(val):
                obj[key] = val
        data["defaultSearchConfigPath"] = obj

    RETURN data.toJSONString()

saveMerchantConfig(config, store):
    value = config.toJSONString()
    configuration.value = value
    merchantConfigurationService.saveOrUpdate(configuration)
```

**Data Dependencies:**
- Reads/writes: `merchant_configuration.value`
- JSON fields: `displayCustomerSection`, `displayContactUs`, `displayStoreAddress`, `displayAddToCartOnFeaturedItems`, `displayPagesMenu`, `displayCustomerAgreement`, `allowPurchaseItems`, `displaySearchBox`, `testMode`, `debugMode`, `useDefaultSearchConfig`, `defaultSearchConfigPath`

**Side Effects:**
- Serializes a `MerchantConfig` object
- Persists JSON into `merchant_configuration.value`
- Omits null booleans and blank search paths from the nested maps

**Concrete Example:**
- **API Input:** Internal save for store `DEFAULT`:  
  `{"allowPurchaseItems":true,"displaySearchBox":false,"useDefaultSearchConfig":{"en":true},"defaultSearchConfigPath":{"en":"search/default-en.json"}}`
- **Success Output:** `200 {"storedValue":"{\"allowPurchaseItems\":true,\"displaySearchBox\":false,\"useDefaultSearchConfig\":{\"en\":true},\"defaultSearchConfigPath\":{\"en\":\"search/default-en.json\"}}"}``
- **Error Input:** `{"useDefaultSearchConfig":{"en":null},"defaultSearchConfigPath":{"en":"   "}}`
- **Error Output:** No exception; the nested values are omitted from serialized JSON

---

### BR-CF-003: Public configuration projects selected merchant flags into the public response

**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/system/MerchantConfigurationFacadeImpl.java` : `getMerchantConfig()` lines 40-78  
**Cross-Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/system/PublicConfigsApi.java` : `getConfig()` lines 20-49; `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/model/system/Configs.java` : public configuration DTO  
**Discovery Method:** Direct Source Read | CAST Imaging (Hybrid)  
**CAST Reference:** Transaction 244237 `GET api/v1/config`; CAST method 13363 `getMerchantConfig`

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 2 | 2 | OK |
| Data-flow | 14 | 14 | OK |
| Constants | 0 | 0 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 2 | 2 | OK |
| Data writes | 14 | 14 | OK |
| Integrations | 1 | 1 | OK (`MerchantConfigurationService`) |
| Error paths | 1 | 1 | OK |

**Preservation:** OK

**Statement:** The public configuration response exposes the merchant's purchase, search, contact, customer, featured-item cart, customer-agreement, and pages-menu display choices. Internal configuration fields such as `testMode`, `debugMode`, and search configuration paths are not copied into this public projection.

**Intent:** Routing

**Acceptance Criteria:** A stored `allowPurchaseItems:false` is returned as `allowOnlinePurchase:false`. Internal credentials or raw configuration JSON must not appear in the public response.

**Logic:**
```pseudocode
getMerchantConfig(merchantStore, language):
    configs = getMerchantConfig(merchantStore)

    readableConfig.allowOnlinePurchase =
        configs.isAllowPurchaseItems()
    readableConfig.displaySearchBox =
        configs.isDisplaySearchBox()
    readableConfig.displayContactUs =
        configs.isDisplayContactUs()
    readableConfig.displayCustomerSection =
        configs.isDisplayCustomerSection()
    readableConfig.displayAddToCartOnFeaturedItems =
        configs.isDisplayAddToCartOnFeaturedItems()
    readableConfig.displayCustomerAgreement =
        configs.isDisplayCustomerAgreement()
    readableConfig.displayPagesMenu =
        configs.isDisplayPagesMenu()

    RETURN readableConfig
```

**Data Dependencies:**
- Reads: `merchant_configuration.merchant_id`, `merchant_configuration.config_key`, `merchant_configuration.value`
- Deserializes: `allowPurchaseItems`, `displaySearchBox`, `displayContactUs`, `displayCustomerSection`, `displayAddToCartOnFeaturedItems`, `displayCustomerAgreement`, `displayPagesMenu`
- Writes: public `Configs` DTO only

**Side Effects:**
- Calls `MerchantConfigurationService.getMerchantConfig()`
- Returns a filtered public projection
- Does not expose raw `merchant_configuration.value`

**Concrete Example:**
- **API Input:** `GET /api/v1/config?store=DEFAULT&lang=en`
- **Success Output:** `200 {"allowOnlinePurchase":true,"displaySearchBox":true,"displayContactUs":false,"displayCustomerSection":true,"displayAddToCartOnFeaturedItems":true,"displayCustomerAgreement":false,"displayPagesMenu":true,"displayShipping":false}`
- **Error Input:** Stored configuration contains malformed JSON `{"allowPurchaseItems":`
- **Error Output:** `500 {"message":"Cannot parse json string {\"allowPurchaseItems\":"}`

---

### BR-CF-004: Public social values are resolved by named merchant configuration keys

**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/system/MerchantConfigurationFacadeImpl.java` : constants and social lookups lines 3-6, 55-66; `getConfigValue()` lines 88-91  
**Cross-Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/system/PublicConfigsApi.java` : `getConfig()` lines 30-49  
**Discovery Method:** Direct Source Read | CAST Imaging (Hybrid)  
**CAST Reference:** Transaction 244237 `GET api/v1/config`; CAST call graph through `getMerchantConfiguration()`

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 5 | 5 | OK |
| Data-flow | 8 | 8 | OK |
| Constants | 4 | 4 | OK (four social keys) |
| State transitions | 0 | 0 | OK |
| Outcomes | 2 | 2 | OK |
| Data writes | 4 | 4 | OK |
| Integrations | 1 | 1 | OK |
| Error paths | 0 | 0 | OK |

**Preservation:** OK

**Statement:** Public configuration reads the merchant-scoped values for Facebook, Google Analytics, Instagram, and Pinterest using their dedicated configuration keys. A missing key is omitted from the public projection rather than replaced with an arbitrary value.

**Intent:** Routing

**Acceptance Criteria:** If the Facebook configuration exists for store `DEFAULT`, the public response contains its value under `facebook`. If the Instagram key is absent, `instagram` is not populated by this lookup.

**Logic:**
```pseudocode
facebookConfigValue =
    getConfigValue(KEY_FACEBOOK_PAGE_URL, merchantStore)
facebookConfigValue.ifPresent(readableConfig::setFacebook)

googleConfigValue =
    getConfigValue(KEY_GOOGLE_ANALYTICS_URL, merchantStore)
googleConfigValue.ifPresent(readableConfig::setGa)

instagramConfigValue =
    getConfigValue(KEY_INSTAGRAM_URL, merchantStore)
instagramConfigValue.ifPresent(readableConfig::setInstagram)

pinterestConfigValue =
    getConfigValue(KEY_PINTEREST_PAGE_URL, merchantStore)
pinterestConfigValue.ifPresent(readableConfig::setPinterest)

getConfigValue(keyConstant, merchantStore):
    configuration = merchantConfigurationService
        .getMerchantConfiguration(keyConstant, merchantStore)
    RETURN Optional.ofNullable(configuration)
        .map(MerchantConfiguration::getValue)
```

**Data Dependencies:**
- Reads: `merchant_configuration.merchant_id`, `merchant_configuration.config_key`, `merchant_configuration.value`
- Keys: `KEY_FACEBOOK_PAGE_URL`, `KEY_GOOGLE_ANALYTICS_URL`, `KEY_INSTAGRAM_URL`, `KEY_PINTEREST_PAGE_URL`
- Writes: public `Configs.facebook`, `Configs.ga`, `Configs.instagram`, `Configs.pinterest`

**Side Effects:**
- Performs up to four merchant-scoped configuration reads
- Omits absent optional social values
- Does not expose configuration record metadata

**Concrete Example:**
- **API Input:** `GET /api/v1/config?store=DEFAULT`
- **Success Output:** `200 {"facebook":"https://facebook.com/shopizer","ga":"UA-123","displayShipping":false}`
- **Error Input:** Store `DEFAULT` has no `KEY_INSTAGRAM_URL` record
- **Error Output:** No error; the response omits `instagram`

---

### BR-CF-005: Shipping display is controlled by a platform property and defaults to false

**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/system/MerchantConfigurationFacadeImpl.java` : property and display logic lines 37-38, 68-75  
**Cross-Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/system/PublicConfigsApi.java` : `getConfig()` lines 30-49  
**Discovery Method:** Direct Source Read | CAST Imaging (Hybrid)  
**CAST Reference:** Transaction 244237 `GET api/v1/config`

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 4 | 4 | OK |
| Data-flow | 2 | 2 | OK |
| Constants | 2 | 2 | OK (`false`, property value) |
| State transitions | 1 | 1 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 1 | 1 | OK |
| Integrations | 0 | 0 | OK |
| Error paths | 1 | 1 | OK |

**Preservation:** OK

**Statement:** The public `displayShipping` value starts as false. When the platform property is nonblank, its Boolean representation overrides the default. A blank property leaves the value false, and any value other than `"true"` resolves to false rather than enabling shipping.

**Intent:** Routing

**Acceptance Criteria:** A property value of `"true"` returns `displayShipping:true`. An absent or blank property returns false. A value such as `"not-a-boolean"` also returns false because `Boolean.valueOf()` treats every value other than `"true"` as false.

**Logic:**
```pseudocode
readableConfig.setDisplayShipping(false)

TRY:
    IF NOT StringUtils.isBlank(displayShipping):
        readableConfig.setDisplayShipping(
            Boolean.valueOf(displayShipping)
        )
CATCH Exception e:
    LOGGER.error("Unexpected failure while reading " + displayShipping)

RETURN readableConfig
```

**Data Dependencies:**
- Reads: platform property `config.displayShipping`
- Writes: public `Configs.displayShipping`

**Side Effects:**
- Logs a parsing error for an invalid property
- Does not read a merchant configuration record for shipping display

**Concrete Example:**
- **API Input:** `GET /api/v1/config?store=DEFAULT`
- **Success Output:** `200 {"displayShipping":true}` when `config.displayShipping=true`
- **Error Input:** Platform property `config.displayShipping=""`
- **Error Output:** `200 {"displayShipping":false}`
- **Malformed property:** `config.displayShipping="not-a-boolean"` is logged; Java `Boolean.valueOf()` yields false rather than throwing

---

### BR-CF-006: Payment and shipping module configurations are decrypted before parsing and encrypted before persistence

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/payments/PaymentServiceImpl.java` : `getPaymentModulesConfigured()` lines 184-206; `savePaymentModuleConfiguration()` lines 225-250; `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/shipping/ShippingServiceImpl.java` : `saveShippingQuoteModuleConfiguration()` lines 252-260 and continuation lines 261-275  
**Cross-Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/reference/loader/ConfigurationModulesLoader.java` : `loadIntegrationConfigurations()` lines 50-103; `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/utils/EncryptionImpl.java` : `encrypt()` lines 23-40 and `decrypt()` lines 42-61  
**Discovery Method:** Direct Source Read | CAST Imaging (Hybrid)  
**CAST Reference:** Transaction 244108 payment configuration; transaction 244206 shipping configuration; CAST method 13186 `loadIntegrationConfigurations`

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 7 | 7 | OK |
| Data-flow | 10 | 10 | OK |
| Constants | 5 | 5 | OK (`AES/CBC/PKCS5Padding`, fixed IV, key names) |
| State transitions | 2 | 2 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 3 | 3 | OK |
| Integrations | 3 | 3 | OK (`Encryption`, JSON loader, repository) |
| Error paths | 3 | 3 | OK |

**Preservation:** OK

**Statement:** Stored payment and shipping module configuration is encrypted at rest. Reads decrypt the merchant configuration value before converting the JSON array into module configuration objects. Saves merge the selected module into the existing configuration set, serialize the set, encrypt the serialized value, and persist it under the store-scoped integration key.

**Intent:** Compliance

**Acceptance Criteria:** A stored encrypted payment configuration is never parsed as plaintext. A successful save writes ciphertext rather than the raw `integrationKeys` map. A blank encrypted value is treated as no configured module set.

**Logic:**
```pseudocode
getPaymentModulesConfigured(store):
    modules = {}
    merchantConfiguration =
        merchantConfigurationService.getMerchantConfiguration(
            Constants.PAYMENT_MODULES,
            store
        )

    IF merchantConfiguration != null
       AND NOT StringUtils.isBlank(merchantConfiguration.getValue()):
        decrypted = encryption.decrypt(
            merchantConfiguration.getValue()
        )
        modules =
            ConfigurationModulesLoader.loadIntegrationConfigurations(
                decrypted
            )

    RETURN modules

savePaymentModuleConfiguration(configuration, store):
    validate module and provider first

    modules = {}
    merchantConfiguration =
        merchantConfigurationService.getMerchantConfiguration(
            Constants.PAYMENT_MODULES,
            store
        )

    IF merchantConfiguration != null
       AND NOT StringUtils.isBlank(merchantConfiguration.getValue()):
        decrypted = encryption.decrypt(
            merchantConfiguration.getValue()
        )
        modules =
            ConfigurationModulesLoader.loadIntegrationConfigurations(
                decrypted
            )
    ELSE IF merchantConfiguration == null:
        merchantConfiguration = new MerchantConfiguration()
        merchantConfiguration.setMerchantStore(store)
        merchantConfiguration.setKey(Constants.PAYMENT_MODULES)

    modules.put(configuration.getModuleCode(), configuration)
    configs = ConfigurationModulesLoader.toJSONString(modules)
    encrypted = encryption.encrypt(configs)
    merchantConfiguration.setValue(encrypted)
    merchantConfigurationService.saveOrUpdate(merchantConfiguration)
```

**Data Dependencies:**
- Reads: `merchant_configuration.merchant_id`, `merchant_configuration.config_key`, `merchant_configuration.value`
- JSON fields: `moduleCode`, `active`, `defaultSelected`, `environment`, `integrationKeys`, `integrationOptions`
- Writes: encrypted `merchant_configuration.value`

**Side Effects:**
- Calls `Encryption.decrypt()` and `Encryption.encrypt()`
- Calls `ConfigurationModulesLoader`
- Persists encrypted payment or shipping configuration
- A decryption, JSON, or encryption failure prevents the save

**Concrete Example:**
- **API Input:** `PUT /api/v1/private/modules/payment/stripe`  
  `{"code":"stripe","active":true,"defaultSelected":true,"integrationKeys":{"secretKey":"sk_test_123","publishableKey":"pk_test_123"},"integrationOptions":{}}`
- **Success Output:** `200` with an empty body; `merchant_configuration.value` contains AES ciphertext, not the JSON body
- **Error Input:** Existing `merchant_configuration.value="not-hex-ciphertext"`
- **Error Output:** `500 {"message":"Error saving payment module"}` caused by decrypt/parse failure
- **Security example:** A read response may contain provider configuration according to the existing API, but the persisted value remains encrypted

---

### BR-CF-007: Integration configuration parsing has an options-field defect

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/reference/loader/ConfigurationModulesLoader.java` : `loadIntegrationConfigurations()` lines 61-99  
**Cross-Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/payments/PaymentServiceImpl.java` : configured-module read lines 184-206; `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/payment/PaymentApi.java` : module detail projection lines 156-175  
**Discovery Method:** Direct Source Read | CAST Imaging (Hybrid)  
**CAST Reference:** Transaction 244108 payment configuration; transaction 244206 shipping configuration; CAST method 13186

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 3 | 3 | OK |
| Data-flow | 7 | 7 | OK |
| Constants | 0 | 0 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 3 | 3 | OK |
| Integrations | 1 | 1 | OK |
| Error paths | 2 | 2 | OK |

**Preservation:** OK — defect behavior recorded explicitly

**Statement:** Module configuration must preserve provider options independently from credential values. A configuration containing options but no credentials must still parse and validate its options without failing because the credential collection is absent.

**Intent:** Validation

**Acceptance Criteria:** A target parser must load `integrationOptions` whenever that field is present, independently of `integrationKeys`. A configuration with only options must not fail because keys are absent.

**Logic:**
```pseudocode
FOR object IN mapper.readValue(value, Map[].class):
    configuration = new IntegrationConfiguration()
    moduleCode = (String)object.get("moduleCode")

    IF object.get("active") != null:
        configuration.setActive((Boolean)object.get("active"))

    IF object.get("defaultSelected") != null:
        configuration.setDefaultSelected(
            (Boolean)object.get("defaultSelected")
        )

    IF object.get("environment") != null:
        configuration.setEnvironment(
            (String)object.get("environment")
        )

    configuration.setModuleCode(moduleCode)
    modules.put(moduleCode, configuration)

    IF object.get("integrationKeys") != null:
        confs = (Map<String,String>)object.get("integrationKeys")
        configuration.setIntegrationKeys(confs)

    // Legacy defect: condition checks integrationKeys again.
    IF object.get("integrationKeys") != null:
        options =
            (Map<String,List<String>>)object.get("integrationOptions")
        configuration.setIntegrationOptions(options)
```

**Data Dependencies:**
- Reads: `merchant_configuration.value`
- JSON fields: `moduleCode`, `active`, `defaultSelected`, `environment`, `integrationKeys`, `integrationOptions`
- Writes: in-memory `IntegrationConfiguration` map keyed by `moduleCode`

**Side Effects:**
- Parses stored JSON into a map
- Omits `integrationOptions` when the legacy payload contains options without `integrationKeys`
- Affects the configuration returned to payment/shipping management APIs

**Concrete Example:**
- **API Input:** Stored configuration represented by  
  `[{"moduleCode":"ups","active":true,"integrationOptions":{"packages":["03","07"]}}]`
- **Success Output:** **Target-only required output:** `{"moduleCode":"ups","active":true,"integrationKeys":{},"integrationOptions":{"packages":["03","07"]}}`
- **Error Input:** The same JSON through the legacy loader
- **Error Output:** Legacy defect: the second `IF` is false because `integrationKeys` is absent, so the response omits `integrationOptions`; target must preserve the submitted options instead

---

### BR-CF-008: Integration-module metadata preserves module, region, detail, and environment configuration

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/reference/loader/IntegrationModulesLoader.java` : `loadIntegrationModules()` lines 25-50; `loadModule()` lines 60-185  
**Cross-Reference:** `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/system/IntegrationModule.java` : persisted and transient fields lines 24-83; `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/system/ModuleConfig.java` : environment fields lines 3-48  
**Discovery Method:** Direct Source Read | CAST Imaging (Hybrid)  
**CAST Reference:** Transaction 244013 `POST services/private/system/module`; CAST method 13191 `loadModule`

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 12 | 12 | OK |
| Data-flow | 17 | 17 | OK |
| Constants | 3 | 3 | OK (`module`, `code`, `configuration`) |
| State transitions | 1 | 1 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 10 | 10 | OK |
| Integrations | 1 | 1 | OK (`ObjectMapper`) |
| Error paths | 3 | 3 | OK |

**Preservation:** OK

**Statement:** An integration-module definition may contain a module family, code, type, image, custom-module flag, supported regions, arbitrary display details, and environment-specific connection configuration. The loader preserves these values in both structured transient objects and JSON strings used for persistence.

**Intent:** Routing

**Acceptance Criteria:** A module definition for `paypal-express-checkout` retains its module family `PAYMENT`, code, type, regions, image, and `TEST`/`PROD` configuration entries. A string `"true"` custom-module value is interpreted as boolean true.

**Logic:**
```pseudocode
loadModule(object):
    module = new IntegrationModule()
    module.setModule((String)object.get("module"))
    module.setCode((String)object.get("code"))
    module.setImage((String)object.get("image"))

    IF object.get("type") != null:
        module.setType((String)object.get("type"))

    IF object.get("customModule") != null:
        o = object.get("customModule")
        b = false
        IF o instanceof Boolean:
            b = (Boolean)object.get("customModule")
        ELSE:
            TRY:
                b = Boolean.valueOf(
                    (String)object.get("customModule")
                )
            CATCH Exception e:
                LOGGER.error("Cannot cast " + o.getClass() +
                    " tp a boolean value")
        module.setCustomModule(b)

    IF object.get("details") != null:
        details = (Map<String,String>)object.get("details")
        module.setDetails(details)
        module.setConfigDetails(
            JSON string reconstructed from each details key/value
        )

    confs = (List)object.get("configuration")
    IF confs != null:
        moduleConfigs = {}
        FOR values IN confs:
            config = new ModuleConfig()
            config.setScheme((String)values.get("scheme"))
            config.setHost((String)values.get("host"))
            config.setPort((String)values.get("port"))
            config.setUri((String)values.get("uri"))
            config.setEnv((String)values.get("env"))
            IF values.get("config1") != null:
                config.setConfig1((String)values.get("config1"))
            IF values.get("config2") != null:
                config.setConfig2((String)values.get("config2"))
            moduleConfigs.put((String)values.get("env"), config)
        module.setModuleConfigs(moduleConfigs)
        module.setConfiguration(JSON array of serialized ModuleConfig values)

    regions = (List<String>)object.get("regions")
    IF regions != null:
        FOR region IN regions:
            module.getRegionsSet().add(region)
        module.setRegions(JSON array of region values)

    RETURN module
```

**Data Dependencies:**
- Reads/writes: `module_configuration.module_conf_id`, `module_configuration.module`, `module_configuration.code`, `module_configuration.regions`, `module_configuration.configuration`, `module_configuration.details`, `module_configuration.type`, `module_configuration.image`, `module_configuration.custom_ind`
- JSON fields: `module`, `code`, `type`, `customModule`, `details`, `configuration`, `regions`

**Side Effects:**
- Constructs `IntegrationModule` and `ModuleConfig` objects
- Serializes details, environments, and regions for persistence
- Invalid JSON or incompatible value types produce a service exception or logged conversion failure

**Concrete Example:**
- **API Input:** `POST /services/private/system/module` with text JSON  
  `{"module":"SHIPPING","code":"ups","regions":["US","CA"],"image":"ups.jpg","configuration":[{"env":"TEST","scheme":"https","host":"wwwcie.ups.com","port":"443","uri":"/ups.app/xml/Rate","config1":"rate-test"}]}`
- **Success Output:** `200 {"status":200}` and a `module_configuration` row with `module:"SHIPPING"`, `code:"ups"`, serialized regions and configuration
- **Error Input:** `{"module":"SHIPPING","code":"ups","regions":"US"}`
- **Error Output:** `500`/`503` from JSON conversion failure because `regions` is expected to be a list

---

### BR-CF-009: Environment configuration distinguishes TEST and PROD and exposes a config2 compatibility defect

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/reference/loader/IntegrationModulesLoader.java` : environment mapping lines 120-158; `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/system/ModuleConfigurationServiceImpl.java` : database hydration lines 94-124  
**Cross-Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/resources/reference/integrationmodules.json` : examples of `TEST` and `PROD` entries lines 1-17, 64-70, 110-116  
**Discovery Method:** Direct Source Read | CAST Imaging (Hybrid)  
**CAST Reference:** Transaction 244013 module creation; module discovery path from CAST method 13191

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 4 | 4 | OK |
| Data-flow | 9 | 9 | OK |
| Constants | 2 | 2 | OK (`TEST`, `PROD`) |
| State transitions | 0 | 0 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 2 | 2 | OK |
| Integrations | 0 | 0 | OK |
| Error paths | 1 | 1 | OK |

**Preservation:** OK — target correction required for defect

**Statement:** Each module environment may define separate primary and secondary connection values in addition to its protocol, host, port, and URI. The two connection values must remain distinct when module metadata is loaded from configuration files or persistence.

**Intent:** Routing

**Acceptance Criteria:** A module with `TEST.config1="test-url"` and `TEST.config2="test-token"` returns those values in their respective fields after discovery. `config2` must not overwrite `config1`.

**Logic:**
```pseudocode
IntegrationModulesLoader.loadModule(values):
    IF values.get("config1") != null:
        config.setConfig1((String)values.get("config1"))
    IF values.get("config2") != null:
        config.setConfig2((String)values.get("config2"))
    moduleConfigs.put(env, config)

ModuleConfigurationServiceImpl.getIntegrationModules(module):
    FOR arrayConfig IN JSONValue.parse(mod.getConfiguration()):
        config.setScheme((String)values.get("scheme"))
        config.setHost((String)values.get("host"))
        config.setPort((String)values.get("port"))
        config.setUri((String)values.get("uri"))
        config.setEnv((String)values.get("env"))
        IF values.get("config1") != null:
            config.setConfig1((String)values.get("config1"))
        IF values.get("config2") != null:
            // Legacy defect:
            config.setConfig1((String)values.get("config2"))
```

**Data Dependencies:**
- Reads: `module_configuration.configuration`
- JSON fields: `env`, `scheme`, `host`, `port`, `uri`, `config1`, `config2`
- Writes: transient `ModuleConfig.env`, `.scheme`, `.host`, `.port`, `.uri`, `.config1`, `.config2`

**Side Effects:**
- Builds environment-keyed module configuration map
- Affects provider discovery and module detail responses
- Legacy hydration can lose `config1` when `config2` is present

**Concrete Example:**
- **API Input:** Module configuration  
  `{"env":"TEST","scheme":"https","host":"gateway.test","port":"443","uri":"/rate","config1":"client-id","config2":"tenant-id"}`
- **Success Output:** `200 {"code":"gateway","moduleConfigs":{"TEST":{"env":"TEST","scheme":"https","host":"gateway.test","port":"443","uri":"/rate","config1":"client-id","config2":"tenant-id"}}}`
- **Error Input:** The same module after database reload through the legacy hydration path
- **Error Output:** Legacy defect: `config1` becomes `"tenant-id"` and `config2` remains unset; target must correct the mapping

---

### BR-CF-010: Module replacement is performed by code, not by module family

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/system/ModuleConfigurationServiceImpl.java` : `createOrUpdateModule()` lines 166-185; `getByCode()` lines 55-58  
**Cross-Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v0/system/SystemRESTController.java` : `createOrUpdateModule()` lines 35-68  
**Discovery Method:** Direct Source Read | CAST Imaging (Hybrid)  
**CAST Reference:** Transaction 244013 `POST services/private/system/module`; CAST method 13382 `createOrUpdateModule`

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 4 | 4 | OK |
| Data-flow | 6 | 6 | OK |
| Constants | 0 | 0 | OK |
| State transitions | 2 | 2 | OK |
| Outcomes | 2 | 2 | OK |
| Data writes | 2 | 2 | OK |
| Integrations | 2 | 2 | OK (`ObjectMapper`, repository) |
| Error paths | 2 | 2 | OK |

**Preservation:** OK

**Statement:** Creating or updating an integration module identifies the module by its code. If a record with that code already exists, it is deleted and the newly loaded definition is created. Modules with different codes are not replaced even when they belong to the same payment or shipping family.

**Intent:** State Transition

**Acceptance Criteria:** Submitting code `ups` replaces the existing `ups` metadata record. Submitting code `usps` does not replace `ups`.

**Logic:**
```pseudocode
createOrUpdateModule(json):
    object = mapper.readValue(json, Map.class)
    module = integrationModulesLoader.loadModule(object)

    IF module != null:
        existing = this.getByCode(module.getCode())
        IF existing != null:
            this.delete(existing)
        this.create(module)
```

**Data Dependencies:**
- Reads: `module_configuration.code`, request JSON `code`
- Writes: delete and insert in `module_configuration`
- Key: `module_configuration.code`

**Side Effects:**
- Parses module JSON
- Deletes an existing module record by code
- Creates a replacement record
- No explicit cache invalidation is performed in this method

**Concrete Example:**
- **API Input:** `POST /services/private/system/module`  
  `{"module":"SHIPPING","code":"ups","regions":["US","CA"],"image":"ups-new.jpg"}`
- **Success Output:** `200 {"status":200}`; the stored `ups` record contains the new image
- **Error Input:** Malformed body `{"module":"SHIPPING","regions":["US"]}`
- **Error Output:** `503 {"message":"Exception while creating or updating the module ..."}`
- **Nonreplacement example:** Submitting `{"module":"SHIPPING","code":"usps"}` leaves the `ups` record untouched

---

### BR-CF-011: Module discovery hydrates cached metadata and appends payment starters

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/system/ModuleConfigurationServiceImpl.java` : `getIntegrationModules()` lines 60-160  
**Cross-Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/system/ModuleConfigurationRepository.java` : `findByModule()` lines 10-14; `initial-source/shopizer-3.2.7/sm-core/src/main/resources/spring/shopizer-core-modules.xml` : payment and shipping module maps lines 1-61  
**Discovery Method:** Direct Source Read | CAST Imaging (Hybrid)  
**CAST Reference:** Transactions 244107/244204 module lists; CAST method 13381 `getIntegrationModules`, fan-out 61

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 13 | 13 | OK |
| Data-flow | 18 | 18 | OK |
| Constants | 3 | 3 | OK (`INTEGRATION_M`, module family) |
| State transitions | 1 | 1 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 8 | 8 | OK |
| Integrations | 3 | 3 | OK (`CacheUtils`, repository, `ModuleStarter`) |
| Error paths | 2 | 2 | OK |

**Preservation:** OK

**Statement:** Module discovery first checks the cached result for the requested module family. On a cache miss it loads persisted module records, hydrates regions, details, and environment configurations, appends available runtime payment implementations, stores the result in cache, and returns the module list.

**Intent:** Routing

**Acceptance Criteria:** A cached payment module list is returned without a database reload. On a miss, persisted payment modules and discovered payment starters are combined. Shipping discovery does not append payment starters because starters are explicitly marked as payment modules.

**Logic:**
```pseudocode
getIntegrationModules(module):
    modules = cache.getFromCache("INTEGRATION_M" + module)

    IF modules == null:
        modules = moduleConfigurationRepository.findByModule(module)

        FOR mod IN modules:
            IF mod.getRegions() != null:
                arrayRegions = JSONValue.parse(mod.getRegions())
                FOR arrayRegion IN arrayRegions:
                    mod.getRegionsSet().add((String)arrayRegion)

            IF mod.getConfigDetails() != null:
                objDetails = JSONValue.parse(mod.getConfigDetails())
                mod.setDetails(objDetails)

            IF mod.getConfiguration() != null:
                arrayConfigs = JSONValue.parse(mod.getConfiguration())
                moduleConfigs = {}
                FOR values IN arrayConfigs:
                    env = (String)values.get("env")
                    config = new ModuleConfig()
                    config.setScheme((String)values.get("scheme"))
                    config.setHost((String)values.get("host"))
                    config.setPort((String)values.get("port"))
                    config.setUri((String)values.get("uri"))
                    config.setEnv((String)values.get("env"))
                    IF values.get("config1") != null:
                        config.setConfig1((String)values.get("config1"))
                    IF values.get("config2") != null:
                        config.setConfig1((String)values.get("config2"))
                    moduleConfigs.put(env, config)
                mod.setModuleConfigs(moduleConfigs)

        IF payments != null:
            FOR mod IN payments:
                m = new IntegrationModule()
                m.setCode(mod.getUniqueCode())
                m.setModule(Constants.PAYMENT_MODULES)
                IF mod.getSupportedCountry() is not empty:
                    m.setRegions(mod.getSupportedCountry().toString())
                    m.setRegionsSet(new HashSet(mod.getSupportedCountry()))
                IF mod.getLogo() is not blank:
                    m.setBinaryImage(mod.getLogo())
                IF mod.getConfigurable() is not blank:
                    m.setConfigurable(mod.getConfigurable())
                modules.add(m)

        cache.putInCache(modules, "INTEGRATION_M" + module)

    RETURN modules
```

**Data Dependencies:**
- Reads: `module_configuration.module`, `.code`, `.regions`, `.details`, `.configuration`, `.image`, `.custom_ind`
- Reads from runtime starters: unique code, supported countries, logo, configurable metadata
- Writes: cache key `INTEGRATION_M<module>`

**Side Effects:**
- Reads/writes module discovery cache
- Reads `module_configuration`
- Appends payment starter metadata
- Logs and returns the current `modules` value when discovery handling catches an exception

**Concrete Example:**
- **API Input:** `GET /api/v1/private/modules/payment?store=DEFAULT`
- **Success Output:** `200 [{"code":"stripe","image":"stripe.png","configured":true,"active":true},{"code":"shopizer-payment-starter","binaryImage":"data:image/png;base64,...","configured":false,"active":false}]`
- **Error Input:** Stored `module_configuration.configuration` contains malformed JSON for `ups`
- **Error Output:** Legacy discovery logs `getIntegrationModules()` failure; target should return an explicit `503 {"message":"Payment module discovery failed"}` rather than a misleading empty success list

---

### BR-CF-012: Provider module availability is filtered by the merchant store country or wildcard region

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/payments/PaymentServiceImpl.java` : `getPaymentMethods()` lines 82-96; `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/shipping/ShippingServiceImpl.java` : `getShippingMethods()` lines 221-233  
**Cross-Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/resources/reference/integrationmodules.json` : region declarations lines 3-8, 11-17, 20-27, 55-70, 75-116  
**Discovery Method:** Direct Source Read | CAST Imaging (Hybrid)  
**CAST Reference:** Transactions 244107 payment modules and 244204 shipping modules

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 4 | 4 | OK |
| Data-flow | 5 | 5 | OK |
| Constants | 1 | 1 | OK (`*`) |
| State transitions | 0 | 0 | OK |
| Outcomes | 2 | 2 | OK |
| Data writes | 0 | 0 | OK |
| Integrations | 1 | 1 | OK (`ModuleConfigurationService`) |
| Error paths | 0 | 0 | OK |

**Preservation:** OK

**Statement:** A payment or shipping module is available to a merchant store when the module declares the store country's ISO code or declares the wildcard region `*`. A module restricted to another country is excluded from the available-module list.

**Intent:** Routing

**Acceptance Criteria:** A store in `US` sees `usps`, `ups`, and wildcard modules. A store in `CA` sees `canadapost`, `ups`, and wildcard modules. A `US`-only module is not offered to a store in `FR`.

**Logic:**
```pseudocode
getPaymentMethods(store):
    modules = moduleConfigurationService
        .getIntegrationModules(Constants.PAYMENT_MODULES)
    returnModules = []

    FOR module IN modules:
        IF module.getRegionsSet().contains(
                store.getCountry().getIsoCode()
           )
           OR module.getRegionsSet().contains("*"):
            returnModules.add(module)

    RETURN returnModules

getShippingMethods(store):
    modules = moduleConfigurationService
        .getIntegrationModules(SHIPPING_MODULES)
    returnModules = []

    FOR module IN modules:
        IF module.getRegionsSet().contains(
                store.getCountry().getIsoCode()
           )
           OR module.getRegionsSet().contains("*"):
            returnModules.add(module)

    RETURN returnModules
```

**Data Dependencies:**
- Reads: `merchant_store.country_id`; `country.iso_code`; `module_configuration.module`; `module_configuration.regions`
- Writes: none
- Runtime projection: `IntegrationModule.regionsSet`

**Side Effects:**
- Calls module discovery
- Filters modules before API projection or provider selection
- Country lookup is owned by shared/reference services

**Concrete Example:**
- **API Input:** `GET /api/v1/private/modules/shipping?store=CA-STORE`, where store country ISO is `CA`
- **Success Output:** `200 [{"code":"canadapost","image":"canadapost.jpg"},{"code":"ups","image":"ups.jpg"},{"code":"weightBased","configured":false}]`
- **Error Input:** `GET /api/v1/private/modules/shipping?store=FR-STORE` for a module whose regions are only `["US"]`
- **Error Output:** `200 []` for that module; it is filtered out rather than returned as available

---

### BR-CF-013: Provider configuration must validate against the selected provider before persistence

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/payments/PaymentServiceImpl.java` : `savePaymentModuleConfiguration()` lines 208-223; `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/shipping/ShippingServiceImpl.java` : `saveShippingQuoteModuleConfiguration()` lines 236-250  
**Cross-Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/payment/PaymentApi.java` : configure lines 83-124; `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/shipping/ShippingConfigurationApi.java` : configure lines 246-288; provider validation examples `StripePayment.java` lines 51-74 and `USPSShippingQuote.java` lines 70-91  
**Discovery Method:** Direct Source Read | CAST Imaging (Hybrid)  
**CAST Reference:** Transaction 244108 payment configuration; transaction 244206 shipping configuration; CAST method 11669 `validateModuleConfiguration`

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 8 | 8 | OK |
| Data-flow | 11 | 11 | OK |
| Constants | 7 | 7 | OK (provider key names and validation code) |
| State transitions | 2 | 2 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 3 | 3 | OK |
| Integrations | 3 | 3 | OK (`PaymentModule`/`ShippingQuoteModule`, encryption, repository) |
| Error paths | 3 | 3 | OK |

**Preservation:** OK

**Statement:** A payment or shipping configuration may be persisted only when the requested module is available to the merchant store and the provider-specific validation boundary accepts its integration keys and options. Provider execution and provider-specific validation remain outside MS-11, but MS-11 must invoke that boundary before saving configuration state.

**Intent:** Validation

**Acceptance Criteria:** Stripe configuration must contain both `secretKey` and `publishableKey`. USPS configuration must provide the provider-required account and package options. A module code that is not available for the store is rejected before persistence.

**Logic:**
```pseudocode
PaymentApi.configure(configuration, merchantStore):
    modules = paymentService.getPaymentMethods(merchantStore)
    map = modules.toMap(IntegrationModule::getCode)
    configModule = map.get(configuration.getCode())

    IF configModule == null:
        THROW ResourceNotFoundException(
            "Payment module [" + configuration.getCode() +
            "] not found"
        )

    integrationConfiguration =
        paymentService.getPaymentModulesConfigured(merchantStore)
            .get(configuration.getCode())

    IF integrationConfiguration == null:
        integrationConfiguration = new IntegrationConfiguration()
        integrationConfiguration.setModuleCode(
            configuration.getCode()
        )

    copy active, defaultSelected, integrationKeys,
         integrationOptions
    paymentService.savePaymentModuleConfiguration(
        integrationConfiguration,
        merchantStore
    )

PaymentServiceImpl.savePaymentModuleConfiguration(configuration, store):
    moduleCode = configuration.getModuleCode()
    module = paymentModules.get(moduleCode)

    IF module == null:
        THROW ServiceException(
            "Payment module " + moduleCode + " does not exist"
        )

    module.validateModuleConfiguration(configuration, store)
    // Only after validation:
    merge configuration
    encrypt and persist

StripePayment.validateModuleConfiguration(configuration, store):
    keys = configuration.getIntegrationKeys()
    IF keys == null OR blank(keys.get("secretKey")):
        errorFields.add("secretKey")
    IF keys == null OR blank(keys.get("publishableKey")):
        errorFields.add("publishableKey")
    IF errorFields != null:
        THROW IntegrationException(ERROR_VALIDATION_SAVE)
```

**Data Dependencies:**
- Reads: `merchant_store.country_id`, `module_configuration.code`, `module_configuration.module`, merchant integration configuration JSON
- Reads provider fields: `integrationKeys`, `integrationOptions`
- Writes: `merchant_configuration.value` only after provider validation

**Side Effects:**
- Calls payment/shipping provider validation modules
- Validation failure prevents encryption and persistence
- Provider-specific error fields are returned through the integration exception boundary

**Concrete Example:**
- **API Input:** `PUT /api/v1/private/modules/payment/stripe`  
  `{"code":"stripe","active":true,"defaultSelected":false,"integrationKeys":{"secretKey":"","publishableKey":"pk_test_123"},"integrationOptions":{}}`
- **Success Output:** Not successful; no merchant configuration write
- **Error Output:** `422 {"error":"ERROR_VALIDATION_SAVE","fields":["secretKey"]}` at the target integration-validation boundary
- **Unavailable module input:** `{"code":"canadapost","active":true,"integrationKeys":{}}` to the payment endpoint
- **Error Output:** `404 {"message":"Payment module [canadapost] not found"}`

---

### BR-CF-014: Module summary responses distinguish configured from active

**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/payment/PaymentApi.java` : `integrationModule()` lines 184-202; `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/shipping/ShippingConfigurationApi.java` : `integrationModule()` lines 290-305  
**Cross-Reference:** `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/model/system/IntegrationModuleSummaryEntity.java` : fields lines 5-59  
**Discovery Method:** Direct Source Read | CAST Imaging (Hybrid)  
**CAST Reference:** Transactions 244107 and 244204 module list projections

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 3 | 3 | OK |
| Data-flow | 7 | 7 | OK |
| Constants | 0 | 0 | OK |
| State transitions | 2 | 2 | OK |
| Outcomes | 2 | 2 | OK |
| Data writes | 4 | 4 | OK |
| Integrations | 0 | 0 | OK |
| Error paths | 0 | 0 | OK |

**Preservation:** OK

**Statement:** A module summary marks `configured:true` when a merchant configuration entry exists for the module code. It marks `active:true` only when that configured entry has `active:true`. An available but unconfigured module is neither configured nor active.

**Intent:** Routing

**Acceptance Criteria:** A Stripe module with a stored inactive configuration returns `configured:true, active:false`. A module with no stored configuration returns `configured:false, active:false`.

**Logic:**
```pseudocode
integrationModule(module, configuredModules):
    readable = new IntegrationModuleSummaryEntity()
    readable.setCode(module.getCode())
    readable.setImage(module.getImage())
    readable.setBinaryImage(module.getBinaryImage())
    readable.setConfigurable(module.getConfigurable())

    IF configuredModules.containsKey(module.getCode()):
        readable.setConfigured(true)
        IF configuredModules.get(module.getCode()).isActive():
            readable.setActive(true)

    RETURN readable
```

**Data Dependencies:**
- Reads: `module_configuration.code`, `module_configuration.image`; merchant integration configuration `moduleCode`, `active`
- Writes: response `code`, `image`, `binaryImage`, `configured`, `active`, `configurable`

**Side Effects:**
- Combines discovered modules with decrypted merchant configuration
- Does not enable a module merely because it exists in metadata

**Concrete Example:**
- **API Input:** `GET /api/v1/private/modules/payment?store=DEFAULT`
- **Success Output:** `200 [{"code":"stripe","image":"stripe.png","configured":true,"active":false},{"code":"moneyorder","image":"moneyorder.gif","configured":false,"active":false}]`
- **Error Input:** Stored configuration has `moduleCode:"stripe"` and `active:"yes"` as a non-boolean value
- **Error Output:** `500 {"message":"Cannot parse payment module configuration"}` during JSON conversion; target must reject invalid boolean types

---

### BR-CF-015: Missing merchant configuration is a distinct state and must not become an implicit public configuration

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/system/MerchantConfigurationServiceImpl.java` : `getMerchantConfig()` lines 68-85; `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/system/MerchantConfigurationFacadeImpl.java` : `getMerchantConfig()` lines 40-48  
**Cross-Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/system/PublicConfigsApi.java` : `getConfig()` lines 30-49  
**Discovery Method:** Direct Source Read | CAST Imaging (Hybrid)  
**CAST Reference:** Transaction 244237 public configuration; CAST call graph method 13363

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 2 | 2 | OK |
| Data-flow | 4 | 4 | OK |
| Constants | 1 | 1 | OK (`CONFIG`) |
| State transitions | 0 | 0 | OK |
| Outcomes | 2 | 1 | GAP (target selects documented defaults instead of preserving the legacy null/error alternative) |
| Data writes | 0 | 0 | OK |
| Integrations | 1 | 1 | OK |
| Error paths | 1 | 1 | OK |

**Preservation:** GAP — target defaulting intentionally corrects the legacy missing-record behavior

**Statement:** A store with no public-configuration record receives the documented platform-default public configuration. The public API must never expose an accidental null response or an unhandled configuration lookup failure.

**Intent:** Validation

**Acceptance Criteria:** A store without a configuration record receives the platform-default public configuration. It must not receive an accidental null body or an unhandled null dereference.

**Logic:**
```pseudocode
MerchantConfigurationServiceImpl.getMerchantConfig(store):
    configuration =
        merchantConfigurationRepository
            .findByMerchantStoreAndKey(
                store.getId(),
                MerchantConfigurationType.CONFIG.name()
            )

    config = null
    IF configuration != null:
        TRY:
            config = mapper.readValue(
                configuration.getValue(),
                MerchantConfig.class
            )
        CATCH Exception e:
            THROW ServiceException(
                "Cannot parse json string " +
                configuration.getValue()
            )

    RETURN config

MerchantConfigurationFacadeImpl.getMerchantConfig(store, language):
    configs = getMerchantConfig(store)
    // Legacy code dereferences configs immediately:
    readableConfig.setAllowOnlinePurchase(
        configs.isAllowPurchaseItems()
    )
```

**Data Dependencies:**
- Reads: `merchant_configuration.merchant_id`, `merchant_configuration.config_key`, `merchant_configuration.value`
- Key: `MerchantConfigurationType.CONFIG.name()` = `CONFIG`
- Writes: none on read

**Side Effects:**
- Missing record returns `null` from the service
- Legacy facade can throw a null-pointer failure
- Target should return explicit defaults or a controlled `503`/`404`

**Concrete Example:**
- **API Input:** `GET /api/v1/config?store=NEW-STORE`
- **Success Output:** **Target-only required output:** `200 {"allowOnlinePurchase":true,"displaySearchBox":true,"displayPagesMenu":true,"displayShipping":false}` when defaults are selected as policy
- **Error Input:** The same request with no `CONFIG` record and no target default policy
- **Error Output:** **Legacy defect:** `500` caused by dereferencing `configs == null`; target must replace this with a controlled response

---

## Extensibility and Provider Boundary Rules

### BR-EXT-021: CMS provider selection is configuration-driven and has no automatic fallback

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/resources/spring/shopizer-core-cms.xml` : provider reference `${config.cms.method}` lines 48-69 and provider beans lines 91-161  
**Cross-Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/cms/content/StaticContentFileManagerImpl.java` : delegation methods lines 33-85; `initial-source/shopizer-3.2.7/sm-core/src/main/resources/spring/shopizer-core-cms.xml` : comment defining `default | http | aws` lines 20-26  
**Discovery Method:** Direct Source Read | CAST Imaging (Hybrid)  
**CAST Reference:** Transaction 244042 file rename; transaction 244039 upload; transaction 244064 image listing

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 3 | 3 | OK |
| Data-flow | 8 | 8 | OK |
| Constants | 3 | 3 | OK (`default`, `httpd`, `aws`) |
| State transitions | 0 | 0 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 1 | 1 | OK |
| Integrations | 4 | 4 | OK (provider implementations) |
| Error paths | 2 | 2 | OK |

**Preservation:** OK

**Statement:** The deployment's configured content-storage provider supplies file upload, retrieval, removal, folder, and listing capabilities through one provider-neutral boundary. Unsupported capabilities or provider initialization failures must be reported explicitly; the service must not silently switch providers.

**Intent:** Routing

**Acceptance Criteria:** With `config.cms.method=default`, content operations use the default cache-backed provider. With `httpd`, they use the local file provider. With `aws`, they use the S3 provider. An unavailable selected provider fails the operation rather than silently switching storage backends.

**Logic:**
```pseudocode
provider = extensionResolver.resolve("EXT-CMS-021", context)
Spring configuration:
    contentFileManager.uploadFile =
        bean("${config.cms.method}ContentAssetsManager")
    contentFileManager.getFile =
        bean("${config.cms.method}ContentAssetsManager")
    contentFileManager.removeFile =
        bean("${config.cms.method}ContentAssetsManager")
    contentFileManager.addFolder =
        bean("${config.cms.method}ContentAssetsManager")
    contentFileManager.removeFolder =
        bean("${config.cms.method}ContentAssetsManager")
    contentFileManager.listFolder =
        bean("${config.cms.method}ContentAssetsManager")

StaticContentFileManagerImpl.addFile(...):
    uploadFile.addFile(...)

StaticContentFileManagerImpl.getFile(...):
    RETURN getFile.getFile(...)

StaticContentFileManagerImpl.removeFile(...):
    removeFile.removeFile(...)

StaticContentFileManagerImpl.listFolders(...):
    RETURN listFolder.listFolders(...)
```

**Data Dependencies:**
- Reads: platform property `config.cms.method`; provider-specific CMS configuration
- Writes: selected provider storage
- Provider beans: `defaultContentAssetsManager`, `httpdContentAssetsManager`, `awsContentAssetsManager`

**Side Effects:**
- Binds all content operations to one selected provider at application configuration time
- Provider failures propagate through `ServiceException`
- No fallback or cross-provider replication is defined

**Extension Point:** `EXT-CMS-021` — pluggable CMS storage provider selected by deployment configuration

**Concrete Example:**
- **API Input:** `POST /api/v1/private/content/images/add` with `qqfilename=hero.png`, store `DEFAULT`, and `config.cms.method=aws`
- **Success Output:** `201 {"success":true,"error":null,"preventRetry":true}`; object is sent to `awsContentAssetsManager`
- **Error Input:** Same upload with `config.cms.method=azure` and no `azureContentAssetsManager` bean
- **Error Output:** Application/provider startup or operation failure; target should return `503 {"message":"CMS provider azure is not configured"}` rather than silently using local storage

---

### BR-EXT-022: Provider object keys preserve merchant and content-type namespaces

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/cms/content/ContentAssetsManager.java` : `nodePath(String, FileContentType)` lines 31-52 and `nodePath(String)` lines 54-60  
**Cross-Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/cms/content/aws/S3StaticContentAssetsManagerImpl.java` : object access lines 59-109 and 155-179; `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/cms/content/gcp/GCPStaticContentAssetsManagerImpl.java` : object access lines 62-101 and 132-165  
**Discovery Method:** Direct Source Read | CAST Imaging (Hybrid)  
**CAST Reference:** Transaction 244042 file rename; transaction 244064 image listing; CAST data graphs for provider object paths

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 4 | 4 | OK |
| Data-flow | 8 | 8 | OK |
| Constants | 5 | 5 | OK (`files`, slash, type names) |
| State transitions | 0 | 0 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 2 | 2 | OK |
| Integrations | 3 | 3 | OK (S3/GCP/cache object stores) |
| Error paths | 2 | 2 | OK |

**Preservation:** OK

**Statement:** Provider object keys begin with the content root and merchant store code. Nondefault file-content types receive an additional type segment; `IMAGE` and `STATIC_FILE` use the base store namespace. The same key-generation rule must be used for upload, retrieval, listing, rename, and removal.

**Intent:** Routing

**Acceptance Criteria:** The `LOGO` object `brand.png` for store `DEFAULT` is addressed separately from the `IMAGE` object `brand.png`. Retrieval with a different store code cannot access the original object.

**Logic:**
```pseudocode
keyStrategy = extensionResolver.resolve("EXT-CMS-021", context)
nodePath(store, type):
    root = nodePath(store) // "files/" + store + "/"

    IF type != null
       AND NOT IMAGE.name().equals(type.name())
       AND NOT STATIC_FILE.name().equals(type.name()):
        root = root + type.name() + "/"

    RETURN root

S3.addFile(store, folderPath, input):
    bucket = bucketName()
    nodePath = nodePath(
        store,
        input.getFileContentType()
    )
    key = nodePath + input.getFileName()
    s3.putObject(bucket, key, input.getFile(), metadata)

GCP.addFile(store, folderPath, input):
    blobId = BlobId.of(
        bucketName(),
        nodePath(store, input.getFileContentType()) +
            input.getFileName()
    )
    storage.create(blobId, bytes)
```

**Data Dependencies:**
- Reads: provider configuration bucket/root; `merchant_store.code`; `FileContentType`; file name
- Writes: provider object key `files/<store>/<type>/<name>` according to namespace rules

**Side Effects:**
- Provider-specific object creation, retrieval, listing, or deletion
- S3 upload sets `PublicRead` ACL in the legacy implementation
- GCP and S3 failures become `ServiceException`

**Extension Point:** `EXT-CMS-021` — provider-neutral object-key strategy

**Concrete Example:**
- **API Input:** Upload `brand.png` as `FileContentType.LOGO` for store `DEFAULT`
- **Success Output:** `201 {"success":true,"error":null,"preventRetry":true}`; target key `files/DEFAULT/LOGO/brand.png`
- **Error Input:** Retrieve `brand.png` as `IMAGE` for store `CA-STORE`
- **Error Output:** `404 {"message":"File brand.png was not found for store CA-STORE and type IMAGE"}`

---

### BR-EXT-023: Provider capabilities and failure semantics must be explicit

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/cms/content/local/CmsStaticContentFileManagerImpl.java` : retrieval methods lines 250-264, removal lines 266-285, listing lines 319-390; `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/cms/content/aws/S3StaticContentAssetsManagerImpl.java` : retrieval/listing lines 59-153 and folder TODOs lines 304-322; `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/cms/content/gcp/GCPStaticContentAssetsManagerImpl.java` : retrieval/listing lines 62-130 and folder TODOs lines 207-224  
**Cross-Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/cms/content/infinispan/CmsStaticContentFileManagerImpl.java` : retrieval and missing-file behavior lines 198-233  
**Discovery Method:** Direct Source Read | CAST Imaging (Hybrid)  
**CAST Reference:** Transaction 244042 rename full graph; transaction 244064 image listing; provider fan-out from CAST

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 9 | 9 | OK |
| Data-flow | 14 | 14 | OK |
| Constants | 4 | 4 | OK (`Not implemented`, null result, provider keys) |
| State transitions | 1 | 1 | OK |
| Outcomes | 5 | 5 | OK |
| Data writes | 3 | 3 | OK |
| Integrations | 5 | 5 | OK |
| Error paths | 4 | 5 | GAP (target adds an explicit unsupported-capability outcome) |

**Preservation:** GAP — target capability errors are more explicit than the legacy provider outcomes.

**Statement:** Configured content-storage backends may differ in retrieval, listing, folder, and deletion capabilities. The target contract must advertise only supported operations and normalize missing objects, unsupported capabilities, and provider failures into explicit API outcomes.

**Intent:** Validation

**Acceptance Criteria:** A missing file is a not-found result, not an empty successful file. A provider that cannot retrieve files must return a capability error. Folder operations must not return success when the selected provider did not perform the operation.

**Logic:**
```pseudocode
capabilities = extensionResolver.resolve("EXT-CMS-021", context)
Infinispan.getFile(...):
    fileBytes = merchantNode.get(contentFileName)
    IF fileBytes == null:
        RETURN null

Local.getFile(...):
    THROW ServiceException(
        "Not implemented for httpd image manager"
    )

Local.getFiles(...):
    THROW ServiceException(
        "Not implemented for httpd image manager"
    )

S3.listFolders(...):
    // TODO — returns null

GCP.listFolders(...):
    // TODO — returns null

Local.listFolders(...):
    // TODO — returns null

S3/GCP/Local provider exceptions:
    CATCH Exception e
    THROW ServiceException(e)
```

**Data Dependencies:**
- Reads: provider object key, provider bucket/path, `merchant_store.code`, `FileContentType`
- Writes: provider objects only for mutating operations
- No relational table represents provider capability status

**Side Effects:**
- Provider calls may return null, throw `ServiceException`, or perform no operation
- Target API must translate:
  - missing object → `404`
  - unsupported capability → `501`
  - provider outage/failure → `503`

**Extension Point:** `EXT-CMS-021` — provider capability registry

**Concrete Example:**
- **API Input:** `GET /api/v1/content/images/download?path=%2Fhero.png&store=DEFAULT` with `config.cms.method=http`
- **Success Output:** **Target-only:** `200 image/png` bytes if local retrieval is implemented
- **Error Input:** Same request against the legacy local provider
- **Error Output:** `501 {"message":"File retrieval is not implemented for the local CMS provider"}`
- **Missing-file example:** Cache provider returns `null` for `missing.png`; target response is `404 {"message":"File missing.png was not found"}`

---

### BR-EXT-024: Runtime payment starters extend discovered payment metadata

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/system/ModuleConfigurationServiceImpl.java` : payment starter append logic lines 129-152  
**Cross-Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/payments/PaymentServiceImpl.java` : country filtering lines 82-96; `initial-source/shopizer-3.2.7/sm-shop-model/src/main/java/com/salesmanager/shop/model/system/IntegrationModuleSummaryEntity.java` : binary image/configurable fields lines 21-59  
**Discovery Method:** Direct Source Read | CAST Imaging (Hybrid)  
**CAST Reference:** Transactions 244107 payment module list; CAST method 13381 `getIntegrationModules`

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 7 | 7 | OK |
| Data-flow | 9 | 9 | OK |
| Constants | 1 | 1 | OK (`PAYMENT_MODULES`) |
| State transitions | 0 | 0 | OK |
| Outcomes | 2 | 2 | OK |
| Data writes | 5 | 5 | OK |
| Integrations | 2 | 2 | OK (`ModuleStarter`, cache) |
| Error paths | 0 | 0 | OK |

**Preservation:** OK

**Statement:** Runtime payment implementations are projected into the payment module catalog with their unique code, payment family, supported countries, logo, and configurable metadata. They are appended to persisted payment module metadata before the result is cached.

**Intent:** Routing

**Acceptance Criteria:** A runtime payment starter with unique code `acme-pay`, supported country `US`, logo data, and configuration metadata appears in the payment module list even if it is not present in the reference JSON file.

**Logic:**
```pseudocode
paymentImplementations = extensionResolver.resolve("EXT-PAY-024", context)
IF payments != null:
    FOR mod IN payments:
        m = new IntegrationModule()
        m.setCode(mod.getUniqueCode())
        m.setModule(Constants.PAYMENT_MODULES)

        IF CollectionUtils.isNotEmpty(mod.getSupportedCountry()):
            m.setRegions(mod.getSupportedCountry().toString())
            m.setRegionsSet(
                new HashSet<String>(mod.getSupportedCountry())
            )

        IF NOT StringUtils.isBlank(mod.getLogo()):
            m.setBinaryImage(mod.getLogo())

        IF StringUtils.isNotBlank(mod.getConfigurable()):
            m.setConfigurable(mod.getConfigurable())

        modules.add(m)

cache.putInCache(
    modules,
    "INTEGRATION_M" + module
)
```

**Data Dependencies:**
- Reads: runtime `ModuleStarter.uniqueCode`, `.supportedCountry`, `.logo`, `.configurable`
- Writes: transient `IntegrationModule.code`, `.module`, `.regions`, `.regionsSet`, `.binaryImage`, `.configurable`
- Writes cache key `INTEGRATION_MPAYMENT`

**Side Effects:**
- Extends the payment catalog at runtime
- Adds starter metadata to the cached module list
- Country filtering later applies to the starter's supported-country set

**Extension Point:** `EXT-PAY-024` — runtime payment starter discovery

**Concrete Example:**
- **API Input:** `GET /api/v1/private/modules/payment?store=US-STORE`
- **Success Output:** `200 [{"code":"acme-pay","configured":false,"active":false,"binaryImage":"data:image/png;base64,...","configurable":"{\"fields\":[\"apiKey\"]}"}]`
- **Error Input:** Runtime starter has an empty unique code or malformed supported-country metadata
- **Error Output:** Target discovery must reject the invalid starter with `503 {"message":"Payment starter metadata is invalid"}` rather than publish an unusable module code

---

### BR-EXT-025: Target configuration storage must separate configuration state from provider execution

**Source Reference:** `assessment/ms-11-cast-brief.md` : scope and cross-service boundaries; `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/payment/PaymentApi.java` : configuration orchestration lines 83-124; `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/shipping/ShippingConfigurationApi.java` : configuration orchestration lines 246-288  
**Cross-Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/payments/PaymentServiceImpl.java` : validation and persistence lines 208-250; `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/shipping/ShippingServiceImpl.java` : validation and persistence lines 236-275  
**Discovery Method:** Direct Source Read | CAST Imaging (Hybrid)  
**CAST Reference:** Transactions 244108 and 244206; CAST full graphs with 284 nodes each

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 8 | 8 | OK |
| Data-flow | 12 | 12 | OK |
| Constants | 5 | 5 | OK |
| State transitions | 2 | 2 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 3 | 3 | OK |
| Integrations | 4 | 4 | OK |
| Error paths | 3 | 3 | OK |

**Preservation:** OK

**Statement:** MS-11 owns merchant-scoped module configuration state, including active/default-selection flags and integration data, but provider execution remains behind payment and shipping service boundaries. MS-11 must not execute payment charges, calculate shipping quotes, or write provider-owned operational tables; it may invoke provider validation and discovery before persisting configuration state.

**Intent:** Routing

**Acceptance Criteria:** Saving Stripe credentials persists configuration state after Stripe validation, but does not charge a card. Saving UPS configuration persists module state after UPS validation, but does not calculate a shipping quote.

**Logic:**
```pseudocode
providerBoundary = extensionResolver.resolve("EXT-PROVIDER-025", context)
PaymentApi.configure(configuration, merchantStore):
    discover available payment modules
    reject configuration.code when unavailable
    copy active, defaultSelected,
         integrationKeys, integrationOptions
    paymentService.savePaymentModuleConfiguration(
        integrationConfiguration,
        merchantStore
    )

PaymentServiceImpl.savePaymentModuleConfiguration(...):
    providerModule.validateModuleConfiguration(...)
    encrypt configuration JSON
    merchantConfigurationService.saveOrUpdate(...)

ShippingConfigurationApi.configure(...):
    discover available shipping modules
    reject configuration.code when unavailable
    copy active, defaultSelected,
         integrationKeys, integrationOptions
    shippingService.saveShippingQuoteModuleConfiguration(...)

MS-11 target boundary:
    persist module configuration state
    invoke provider validation/discovery boundary
    DO NOT execute payment authorization/capture
    DO NOT calculate or quote shipping
    DO NOT write payment/shipping execution tables
```

**Data Dependencies:**
- Reads: `merchant_configuration.merchant_id`, `merchant_configuration.config_key`, `merchant_configuration.value`, `module_configuration.code`, `.module`, `.regions`
- Writes: `merchant_configuration.value` for payment/shipping module configuration
- Explicitly not owned: payment transaction tables, shipment quote tables, provider execution records

**Side Effects:**
- Calls provider discovery and validation boundaries
- Encrypts and persists merchant configuration
- Does not publish an execution result or perform provider business execution

**Extension Point:** `EXT-PROVIDER-025` — provider validation/discovery boundary

**Concrete Example:**
- **API Input:** `PUT /api/v1/private/modules/payment/stripe`  
  `{"code":"stripe","active":true,"defaultSelected":true,"integrationKeys":{"secretKey":"sk_test_123","publishableKey":"pk_test_123"},"integrationOptions":{}}`
- **Success Output:** `200` with empty body; encrypted module state is stored for the merchant
- **Error Input:** `PUT /api/v1/private/modules/payment/stripe` with `{"code":"stripe","active":true,"integrationKeys":{"secretKey":"sk_test_123"}}`
- **Error Output:** `422 {"error":"ERROR_VALIDATION_SAVE","fields":["publishableKey"]}`; no payment charge is attempted
- **Boundary example:** A checkout later charging Stripe belongs to the payment execution service, not this rule or MS-11 persistence

---

### BR-EXT-026: Module discovery cache requires invalidation after module replacement

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/system/ModuleConfigurationServiceImpl.java` : cache read/write lines 70-73 and 154; replacement lines 166-180  
**Cross-Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/system/ModuleConfigurationServiceImpl.java` : `createOrUpdateModule()` lines 166-185  
**Discovery Method:** Direct Source Read | CAST Imaging (Hybrid)  
**CAST Reference:** Transaction 244013 module replacement; transactions 244107/244204 cached module discovery

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 4 | 4 | OK |
| Data-flow | 7 | 7 | OK |
| Constants | 2 | 2 | OK (`INTEGRATION_M`) |
| State transitions | 2 | 2 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 2 | 2 | OK |
| Integrations | 2 | 3 | GAP (target adds explicit cache invalidation/versioning absent from legacy replacement) |
| Error paths | 1 | 1 | OK |

**Preservation:** GAP — target cache invalidation is a required correction to the legacy behavior

**Statement:** Module discovery results must reflect a completed module replacement on the next read. Replacing a module therefore requires invalidating or versioning the affected family’s cached discovery result.

**Intent:** State Transition

**Acceptance Criteria:** After replacing `ups` image metadata, the next shipping module discovery must return the new image rather than an old cached record.

**Logic:**
```pseudocode
cachePolicy = extensionResolver.resolve("EXT-MODULE-CACHE-026", context)
getIntegrationModules(module):
    modules = cache.getFromCache("INTEGRATION_M" + module)
    IF modules == null:
        modules = repository.findByModule(module)
        hydrate modules
        cache.putInCache(
            modules,
            "INTEGRATION_M" + module
        )
    RETURN modules

createOrUpdateModule(json):
    module = integrationModulesLoader.loadModule(
        mapper.readValue(json, Map.class)
    )
    IF module != null:
        existing = getByCode(module.getCode())
        IF existing != null:
            delete(existing)
        create(module)
        // Legacy defect: no cache.remove("INTEGRATION_M" + module.getModule())
```

**Data Dependencies:**
- Reads: `module_configuration.code`, `.module`, `.image`, `.regions`, `.configuration`, `.details`
- Writes: `module_configuration` replacement and cache key `INTEGRATION_M<module>`

**Side Effects:**
- Legacy replacement can leave stale module metadata in cache
- Target must remove or version the affected cache entry after successful replacement

**Extension Point:** `EXT-MODULE-CACHE-026` — versioned or invalidated module-discovery cache

**Concrete Example:**
- **API Input:** `POST /services/private/system/module`  
  `{"module":"SHIPPING","code":"ups","image":"ups-v2.jpg","regions":["US","CA"]}`
- **Success Output:** `200 {"status":200}` followed by `GET /api/v1/private/modules/shipping` returning `"image":"ups-v2.jpg"`
- **Error Input:** Same replacement while cache contains the old `ups` object
- **Error Output:** **Legacy defect:** discovery may return `"image":"ups.jpg"` until cache expiry/restart; target must invalidate and return the replacement deterministically

---

### BR-EXT-027: Public module detail reads must not expose encrypted merchant configuration

**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/payment/PaymentApi.java` : `paymentModule()` lines 134-180; `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/shipping/ShippingConfigurationApi.java` : `shippingModule()` lines 203-244  
**Cross-Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/payments/PaymentServiceImpl.java` : decryption and configuration lookup lines 162-206; `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/reference/loader/ConfigurationModulesLoader.java` : parsed fields lines 61-95  
**Discovery Method:** Direct Source Read | CAST Imaging (Hybrid)  
**CAST Reference:** Transactions 244109 payment detail and 244205 shipping detail

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 5 | 5 | OK |
| Data-flow | 10 | 10 | OK |
| Constants | 1 | 1 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 0 | 0 | OK |
| Integrations | 2 | 2 | OK |
| Error paths | 2 | 2 | OK |

**Preservation:** OK — target security strengthening required

**Statement:** Module detail responses may expose identity, activation state, and non-sensitive configuration metadata needed by administration, but must never expose encrypted storage values or provider credentials to public callers. Sensitive fields must be classified and masked or omitted.

**Intent:** Compliance

**Acceptance Criteria:** A payment detail response may indicate that Stripe is active and configurable, but must return masked or write-only secret fields. The encrypted `merchant_configuration.value` must never be serialized into an API response.

**Logic:**
```pseudocode
redactionPolicy = extensionResolver.resolve("EXT-CONFIG-027", context)
PaymentApi.paymentModule(code, merchantStore, language):
    integrationModule =
        paymentService.getPaymentMethodByCode(
            merchantStore,
            code
        )
    IF integrationModule == null:
        THROW ResourceNotFoundException(
            "Payment module [" + code + "] not found"
        )

    returnConfig = new IntegrationModuleConfiguration()
    returnConfig.setConfigurable(
        integrationModule.getConfigurable()
    )
    returnConfig.setActive(false)
    returnConfig.setDefaultSelected(false)
    returnConfig.setCode(code)

    config = paymentService.getPaymentConfiguration(
        code,
        merchantStore
    )
    IF config == null:
        RETURN returnConfig

    returnConfig.setActive(config.isActive())
    returnConfig.setDefaultSelected(
        config.isDefaultSelected()
    )
    returnConfig.setIntegrationKeys(
        config.getIntegrationKeys()
    )
    returnConfig.setIntegrationOptions(
        config.getIntegrationOptions()
    )
    RETURN returnConfig
```

**Data Dependencies:**
- Reads: `module_configuration.code`, `.configurable`; decrypted merchant integration configuration fields
- Writes: response DTO only
- Sensitive fields: `integrationKeys` values such as Stripe `secretKey`

**Side Effects:**
- Decrypts and parses stored merchant configuration
- Returns administrative module detail
- Target must mask, omit, or represent secret values as configured-but-not-readable

**Extension Point:** `EXT-CONFIG-027` — secret-field classification and redaction policy

**Concrete Example:**
- **API Input:** `GET /api/v1/private/modules/payment/stripe?store=DEFAULT`
- **Success Output:** Target response  
  `200 {"code":"stripe","active":true,"defaultSelected":true,"integrationKeys":{"secretKey":"***","publishableKey":"pk_test_123"},"integrationOptions":{}}`
- **Error Input:** A public, unauthenticated caller requests the same endpoint
- **Error Output:** `401 {"error":"UNAUTHORIZED","message":"Authentication required","statusCode":401}`
- **Legacy defect:** The source copies `integrationKeys` without redaction; target must not expose `sk_live_...` in a response

---

### BR-EXT-028: Reference module definitions support wildcard and provider-specific environment endpoints

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/resources/reference/integrationmodules.json` : shipping entries lines 1-53; payment entries lines 55-118  
**Cross-Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/reference/loader/IntegrationModulesLoader.java` : environment and region loading lines 114-183  
**Discovery Method:** Direct Source Read | CAST Imaging (Hybrid)  
**CAST Reference:** Transactions 244107/244204 module discovery; CAST data graph for reference JSON

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 3 | 3 | OK |
| Data-flow | 8 | 8 | OK |
| Constants | 7 | 7 | OK (`US`, `CA`, `*`, `TEST`, `PROD`, schemes, paths) |
| State transitions | 0 | 0 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 3 | 3 | OK |
| Integrations | 1 | 1 | OK |
| Error paths | 1 | 1 | OK |

**Preservation:** OK

**Statement:** Module metadata may declare wildcard availability or explicit country availability and may provide distinct TEST and PROD connection endpoints. Environment endpoint metadata is descriptive configuration for downstream provider use; selecting an environment or executing a provider call is outside MS-11 content persistence.

**Intent:** Routing

**Acceptance Criteria:** `weightBased` is available in all regions because it declares `*`. USPS exposes separate testing and production hosts. The target preserves both environment entries without replacing one with the other.

**Logic:**
```pseudocode
metadataPolicy = extensionResolver.resolve("EXT-MODULE-METADATA-028", context)
reference JSON:
    {
      "module":"SHIPPING",
      "code":"usps",
      "regions":["US"],
      "configuration":[
        {
          "env":"TEST",
          "scheme":"http",
          "host":"testing.shippingapis.com",
          "port":"80",
          "uri":"/ShippingAPI.dll"
        },
        {
          "env":"PROD",
          "scheme":"http",
          "host":"production.shippingapis.com",
          "port":"80",
          "uri":"/ShippingAPI.dll"
        }
      ]
    }

loadModule(object):
    FOR region IN object.get("regions"):
        module.getRegionsSet().add(region)

    FOR values IN object.get("configuration"):
        config.env = values["env"]
        config.scheme = values["scheme"]
        config.host = values["host"]
        config.port = values["port"]
        config.uri = values["uri"]
        module.moduleConfigs.put(config.env, config)
```

**Data Dependencies:**
- Reads: reference JSON `module`, `code`, `regions`, `configuration.env`, `.scheme`, `.host`, `.port`, `.uri`
- Writes: `module_configuration.regions`, `.configuration`; transient module environment map

**Side Effects:**
- Loads reference metadata into module definitions
- Enables country filtering and downstream endpoint construction
- Does not make an external provider call

**Extension Point:** `EXT-MODULE-METADATA-028` — environment-aware module definition

**Concrete Example:**
- **API Input:** `GET /api/v1/private/modules/shipping?store=US-STORE`
- **Success Output:** `200 [{"code":"usps","configured":false,"image":"usps.jpg","moduleConfigs":{"TEST":{"host":"testing.shippingapis.com","port":"80","uri":"/ShippingAPI.dll"},"PROD":{"host":"production.shippingapis.com","port":"80","uri":"/ShippingAPI.dll"}}}]`
- **Error Input:** A module definition with duplicate environment entries both using `env:"TEST"`
- **Error Output:** Target validation should return `422 {"message":"Module usps contains duplicate environment TEST entries"}`; the legacy map would silently retain the last value

---

### BR-EXT-029: File rename must preserve MIME metadata across provider boundaries

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/content/ContentServiceImpl.java` : `renameFile()` lines 495-520  
**Cross-Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/cms/content/aws/S3StaticContentAssetsManagerImpl.java` : `addFile()` lines 155-179; `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/cms/content/infinispan/CmsStaticContentFileManagerImpl.java` : `getFile()` lines 224-227  
**Discovery Method:** Direct Source Read | CAST Imaging (Hybrid)  
**CAST Reference:** Transaction 244042 file rename full graph

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 2 | 2 | OK |
| Data-flow | 7 | 7 | OK |
| Constants | 0 | 0 | OK |
| State transitions | 1 | 1 | OK |
| Outcomes | 2 | 2 | OK |
| Data writes | 2 | 2 | OK |
| Integrations | 2 | 2 | OK |
| Error paths | 1 | 2 | GAP (target adds a deterministic incompatible-extension rejection) |

**Preservation:** GAP — target rename validation is stricter than the legacy implementation

**Statement:** A renamed content file retains the original file-content type and MIME metadata when recreated under the new name. The target must not infer a different content type solely from the new extension after the rename operation has selected the original file.

**Intent:** State Transition

**Acceptance Criteria:** Renaming `hero.jpeg` to `hero.bin` through the image rename operation retains the image content classification and original MIME metadata unless the target explicitly rejects the incompatible new name.

**Logic:**
```pseudocode
metadataPolicy = extensionResolver.resolve("EXT-CMS-021", context)
file = contentFileManager.getFile(
    merchantStoreCode,
    path,
    fileContentType,
    originalName
)

mimeType = file.getMimeType()
fileBytes = file.getFile().toByteArray()

contentFileManager.removeFile(
    merchantStoreCode,
    fileContentType,
    originalName,
    path
)

inputFile.setFileContentType(fileContentType)
inputFile.setFileName(newName)
inputFile.setMimeType(mimeType)
inputFile.setFile(new ByteArrayInputStream(fileBytes))

contentFileManager.addFile(
    merchantStoreCode,
    path,
    inputFile
)
```

**Data Dependencies:**
- Reads: provider original object bytes and MIME metadata
- Writes: provider new object bytes and MIME metadata; removes original key
- Scope: `merchant_store.code`, original `FileContentType`

**Side Effects:**
- Provider-specific MIME metadata may be applied during recreation
- S3 sets object metadata from `inputStaticContentData.getMimeType()`
- Cache provider derives MIME type on retrieval from the original name

**Extension Point:** `EXT-CMS-021` — provider metadata preservation strategy

**Concrete Example:**
- **API Input:** `POST /api/v1/private/content/images/rename?path=%2Fhero.jpeg&newName=hero-banner.jpeg&store=DEFAULT`
- **Success Output:** `200 {"success":true,"error":null,"preventRetry":true}`; new object retains MIME `image/jpeg`
- **Error Input:** `newName=hero-banner.exe`
- **Error Output:** Target validation returns `422 {"message":"New file name extension is incompatible with image/jpeg"}`; when the rename is accepted, the original MIME metadata is preserved regardless of the new extension

---

### BR-EXT-030: Provider-backed file deletion must be scoped and idempotency must be explicit

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/content/ContentServiceImpl.java` : `removeFile()` lines 296-308 and `removeFiles()` lines 334-342; `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/cms/content/aws/S3StaticContentAssetsManagerImpl.java` : removal lines 196-224; `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/cms/content/gcp/GCPStaticContentAssetsManagerImpl.java` : removal lines 166-205  
**Cross-Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v1/content/ContentAdministrationApi.java` : `remove()` lines 213-237  
**Discovery Method:** Direct Source Read | CAST Imaging (Hybrid)  
**CAST Reference:** Transaction 244043 `DELETE api/v1/private/content/images/remove`; file removal provider graphs

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 3 | 3 | OK |
| Data-flow | 7 | 7 | OK |
| Constants | 2 | 2 | OK (`IMAGE`, file-not-found behavior) |
| State transitions | 1 | 1 | OK |
| Outcomes | 3 | 2 | GAP (target normalizes already-absent deletion to idempotent success) |
| Data writes | 1 | 1 | OK |
| Integrations | 2 | 2 | OK |
| Error paths | 2 | 2 | OK |

**Preservation:** GAP — target deletion semantics are intentionally normalized across providers

**Statement:** File deletion must be scoped to one merchant store, content category, and file name, while bulk removal must be scoped to that merchant’s content namespace. The API must define one consistent outcome for deleting an object that is already absent.

**Intent:** Compliance

**Acceptance Criteria:** Deleting `hero.png` for `DEFAULT` and type `IMAGE` must not affect `hero.png` for another store or type. Bulk store cleanup must not delete another store's namespace.

**Logic:**
```pseudocode
deletionPolicy = extensionResolver.resolve("EXT-CMS-DELETE-030", context)
ContentFacade.delete(store, fileName, fileType):
    Validate.notNull(store, "MerchantStore cannot be null")
    Validate.notNull(fileName, "File name cannot be null")
    t = FileContentType.valueOf(fileType)
    contentService.removeFile(
        store.getCode(),
        t,
        fileName
    )

ContentService.removeFile(storeCode, fileType, fileName):
    path = Optional.empty()
    contentFileManager.removeFile(
        storeCode,
        fileType,
        fileName,
        path
    )

S3.removeFile(storeCode, type, fileName, path):
    s3.deleteObject(
        bucketName(),
        nodePath(storeCode, type) + fileName
    )

GCP.removeFile(storeCode, type, fileName, path):
    storage.delete(
        bucketName(),
        nodePath(storeCode, type) + fileName
    )
```

**Data Dependencies:**
- Reads: merchant store code, file name, `FileContentType`
- Writes/deletes: provider object at `files/<store>/<type>/<fileName>`
- Bulk deletes provider store root

**Side Effects:**
- Calls selected provider deletion operation
- Returns `FileStatus` from image removal controller
- Provider absence semantics are not normalized in the legacy source

**Extension Point:** `EXT-CMS-DELETE-030` — provider-neutral idempotent deletion policy

**Concrete Example:**
- **API Input:** `DELETE /api/v1/private/content/images/remove?path=%2Fhero.png&store=DEFAULT`
- **Success Output:** `200 {"success":true,"error":null,"preventRetry":true}` when the provider removes the object
- **Error Input:** Same request after `hero.png` is already absent
- **Error Output:** `204` with no body when `hero.png` is already absent; deletion is idempotent across local, cache, S3, and GCP providers

## Extraction Coverage Notes

- Content source files were read in full or in multi-pass ranges according to file size:
  - `ContentFacadeImpl.java`: 930 lines, read in multiple ranges covering content mapping, localization, file orchestration, projections, and mutations.
  - `ContentServiceImpl.java`: 542 lines, read in two passes covering persistence, file routing, folders, rename, and paging.
  - `ContentApi.java`: 520 lines, read in multiple ranges covering public/private content endpoints, deprecated operations, uploads, and deletes.
  - `ContentAdministrationApi.java`: 363 lines, read in multiple ranges covering folder listing, upload, download, rename, removal, and DTO conversion.
  - `CmsStaticContentFileManagerImpl.java` local provider: 484 lines, read in multiple ranges covering upload, removal, listing, and folder operations.
  - `CmsStaticContentFileManagerImpl.java` Infinispan provider: 477 lines, read in multiple ranges covering upload, retrieval, removal, folder operations, and namespace construction.
  - `S3StaticContentAssetsManagerImpl.java`: 322 lines, read in two passes covering object operations, listing, bucket access, and folder TODOs.
  - `GCPStaticContentAssetsManagerImpl.java`: 224 lines, read in full covering object operations, listing, deletion, and folder TODOs.
  - `ModuleConfigurationServiceImpl.java`: 187 lines, read in full covering cache hydration, starter discovery, and replacement.
  - `IntegrationModulesLoader.java`: 190 lines, read in full covering reference-module loading, JSON preservation, regions, details, and environments.
- Explicit defects retained for target review: null public download, empty folder endpoint, null deprecated summary/prefix operation, missing merchant-config null policy, options parser guard defect, `config2` hydration defect, stale module-discovery cache, provider capability gaps, non-atomic rename, and unredacted administrative integration keys.
