# Obsolete Rules Appendix — MS-11

**Approved:** Mode A agent defaults, 2026-09-02
**Status:** Excluded from the active implementation scope.

The following rules were explicitly approved for removal during Phase 4a. Their source evidence is retained here for traceability.

## Drop decision: BR-MER-026
**Drop rationale:** Legacy folder enumeration/deletion is incomplete and backend-dependent; the approved target scope removes this incomplete capability.

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

## Drop decision: BR-MER-027
**Drop rationale:** The rule documents explicitly nonfunctional legacy download/folder/compatibility operations; the approved target scope removes these defective compatibility behaviors.

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
