# Screen Inventory: Content and Shipping

## Content

| Screen | Layout/data on load | Actions and URL state | Capability |
|---|---|---|---|
| Pages list/edit | content table using `ContentListResponse`, editor using `ContentItem` and request schemas | list query; add/edit by code/content ID; save/delete | contract-backed |
| Boxes list/edit | same list/editor pattern for content boxes | add/edit/delete; code uniqueness | contract-backed |
| Images list | file-manager toolbar, folder view, image grid | search/folder/upload/rename/delete; query folder | contract-backed |
| Files list | file table with name/type/size/status | upload/download/rename/delete | contract-backed |
| Promotion | legacy promotion editor route | no published CRUD operation | deferred contract-gap screen |

Editor loading preserves the legacy content-editor sequence, but rich text/HTML sanitization
and provider field mapping require a security review before implementation. Gallery selection
uses the common image picker.

## Shipping

| Screen | Layout/data on load | Actions and URL state | Capability |
|---|---|---|---|
| Expedition/configuration | store selector, shipping type/countries, tax toggle where exact schema supports it | load/save configuration | contract-backed |
| Methods | module summary table | select module, configure | contract-backed |
| Method configure | provider module detail form | `{id}`; save/cancel | contract-backed |
| Origin | address form with country/state dependent selectors | load/save | contract-backed except shared lookup values |
| Packaging | package table | add/edit/delete | contract-backed |
| Package add/edit | code, dimensions, weight, type, units from exact schema | save/cancel | contract-backed |
| Rules list/editor | search, rule form, criteria/actions/result configuration | add/edit/configure | deferred; no MS-09 rule contract |

Shipping screens retain the legacy transfer-list and form sequence. State/country option
controls show a lookup-gap message until an owning contract exists rather than silently
substituting browser data.
