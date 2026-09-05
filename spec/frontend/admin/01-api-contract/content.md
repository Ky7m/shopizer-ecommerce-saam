# BFF Contract: Content, File Manager, Gallery, Configuration

## Content pages and boxes

| Frontend BFF method/path | Provider | Exact provider method/path | Request -> response |
|---|---|---|---|
| GET `/api/admin/v1/content/pages` | MS-11 | GET `/private/content/pages` | query -> `ContentListResponse` |
| GET `/api/admin/v1/content/pages/{code}` | MS-11 | GET `/private/content/pages/{code}` | path -> `ContentItem` |
| POST `/api/admin/v1/content/pages` | MS-11 | POST `/private/content/page` | `CreatePageRequest` -> `EntityIdResponse` |
| PUT `/api/admin/v1/content/pages/{contentId}` | MS-11 | PUT `/private/content/page/{contentId}` | `UpdatePageRequest` -> `204` (no body) |
| DELETE `/api/admin/v1/content/pages/{contentId}` | MS-11 | DELETE `/private/content/page/{contentId}` | path -> `204` (no body) |
| GET `/api/admin/v1/content/pages/exists/{code}` | MS-11 | GET `/private/content/page/{code}/exists` | path -> `EntityExistsResponse` |
| GET `/api/admin/v1/content/boxes` | MS-11 | GET `/private/content/boxes` | query -> `ContentListResponse` |
| GET `/api/admin/v1/content/boxes/{code}` | MS-11 | GET `/private/content/boxes/{code}` | path -> `ContentItem` |
| POST `/api/admin/v1/content/boxes` | MS-11 | POST `/private/content/box` | `CreateBoxRequest` -> `EntityIdResponse` |
| PUT `/api/admin/v1/content/boxes/{contentId}` | MS-11 | PUT `/private/content/box/{contentId}` | `UpdateBoxRequest` -> `204` (no body) |
| DELETE `/api/admin/v1/content/boxes/{contentId}` | MS-11 | DELETE `/private/content/box/{contentId}` | path -> `204` (no body) |
| GET `/api/admin/v1/content/boxes/exists/{code}` | MS-11 | GET `/private/content/box/{code}/exists` | path -> `EntityExistsResponse` |

Page and box editors preserve the legacy code/title/content/description/localization layout,
but bind only exact `ContentItem`, `ContentDescriptionInput`, `CreatePageRequest`,
`UpdatePageRequest`, `CreateBoxRequest`, and `UpdateBoxRequest` fields.

## Files, images, folders, and gallery

| Frontend BFF method/path | Provider | Exact provider method/path | Request -> response |
|---|---|---|---|
| GET `/api/admin/v1/content/files` | MS-11 | GET `/private/content/files` | query -> `FileListResponse` |
| POST `/api/admin/v1/content/files` | MS-11 | POST `/private/content/files` | upload -> `FileResponse` |
| GET `/api/admin/v1/content/files/{fileName}/download` | MS-11 | GET `/private/content/files/{fileName}/download` | path -> `FileDownload` |
| DELETE `/api/admin/v1/content/files/{fileName}` | MS-11 | DELETE `/private/content/files/{fileName}` | path -> `204` (no body) |
| POST `/api/admin/v1/content/files/rename` | MS-11 | POST `/private/content/files/rename` | `ContentFileRenameRequest` -> `FileStatusResponse` |
| GET `/api/admin/v1/content/images` | MS-11 | GET `/private/content/list` | query -> `ImageFileListResponse` |
| GET `/api/admin/v1/content/images/folder` | MS-11 | GET `/private/content/folder` | query -> `ContentFolderResponse` |
| POST `/api/admin/v1/content/images` | MS-11 | POST `/private/content/images/add` | `ImageUploadRequest` -> `FileStatusResponse` |
| POST `/api/admin/v1/content/images/rename` | MS-11 | POST `/private/content/images/rename` | `ImageRenameRequest` -> `FileStatusResponse` |
| DELETE `/api/admin/v1/content/images` | MS-11 | DELETE `/private/content/images/remove` | request -> `204` (no body) |
| POST `/api/admin/v1/content/folders` | MS-11 | POST `/private/content/folders` | `CreateFolderRequest` -> `FolderResponse` |
| GET `/api/admin/v1/content/folders` | MS-11 | GET `/private/content/folders` | query -> `FolderListResponse` |
| DELETE `/api/admin/v1/content/folders` | MS-11 | DELETE `/private/content/folders` | request -> `204` (no body) |

The gallery is a selection dialog, not a separate provider domain: it uses the image manager
list, binds exact `ImageFile` fields, previews only provider-returned asset values, and closes
with the selected value. File manager upload/delete/rename actions use the exact published
request schemas and show progress, empty folder, conflict, and retry states.

## Configuration and payment modules

| Frontend BFF method/path | Provider | Exact provider method/path | Request -> response |
|---|---|---|---|
| GET `/api/admin/v1/configuration` | MS-11 | GET `/private/configuration` | context -> `MerchantConfigurationPayload` |
| PUT `/api/admin/v1/configuration` | MS-11 | PUT `/private/configuration` | `MerchantConfigurationPayload` -> `MerchantConfigurationPayload` |
| GET `/api/admin/v1/payment-modules` | MS-11 | GET `/private/modules/payment` | context -> `PaymentModuleSummaryListResponse` |
| GET `/api/admin/v1/payment-modules/{code}` | MS-11 | GET `/private/modules/payment/{code}` | path -> `PaymentModuleDetail` |
| PUT `/api/admin/v1/payment-modules/{code}` | MS-11 | PUT `/private/modules/payment/{code}` | `ModuleConfigurationRequest` -> `PaymentModuleDetail` |

The legacy payment methods screen and configure screen bind only the exact module summary,
detail, and configuration fields. Provider-specific JSON is not fabricated in the frontend.
401/403/409/422 and 5xx handling follows the common rules in `INDEX.md`.

## Open decisions / gaps

- The legacy upload component and image browser have different historical file shapes; use
  the published MS-11 schemas and resolve any BFF transformation explicitly.
- Content publish sequencing has no backend workflow authority: `GAP-WF-ADMIN-005`.
