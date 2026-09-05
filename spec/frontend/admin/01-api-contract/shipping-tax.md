# BFF Contract: Shipping and Tax

## Shipping

| Frontend BFF method/path | Provider | Exact provider method/path | Request -> response |
|---|---|---|---|
| GET `/api/admin/v1/shipping/configuration` | MS-09 | GET `/private/configurations/shipping` | context -> `ShippingConfiguration` |
| GET `/api/admin/v1/shipping/modules` | MS-09 | GET `/private/modules/shipping` | context -> `ShippingModuleSummary` list |
| POST `/api/admin/v1/shipping/modules` | MS-09 | POST `/private/modules/shipping` | `ShippingModuleConfigurationRequest` -> `ShippingModuleConfiguration` |
| GET `/api/admin/v1/shipping/modules/{module}` | MS-09 | GET `/private/modules/shipping/{module}` | path -> `ShippingModuleConfiguration` |
| GET `/api/admin/v1/shipping/origin` | MS-09 | GET `/private/shipping/origin` | context -> `ShippingOrigin` |
| POST `/api/admin/v1/shipping/origin` | MS-09 | POST `/private/shipping/origin` | `ShippingOriginRequest` -> `ShippingOrigin` |
| GET `/api/admin/v1/shipping/packages` | MS-09 | GET `/private/shipping/packages` | context -> list of `ShippingPackage` |
| GET `/api/admin/v1/shipping/packages/{package}` | MS-09 | GET `/private/shipping/package/{package}` | path -> `ShippingPackage` |
| POST `/api/admin/v1/shipping/packages` | MS-09 | POST `/private/shipping/package` | `ShippingPackageRequest` -> `ShippingPackage` |
| PUT `/api/admin/v1/shipping/packages/{package}` | MS-09 | PUT `/private/shipping/package/{package}` | `ShippingPackageRequest` -> `ShippingPackage` |
| DELETE `/api/admin/v1/shipping/packages/{package}` | MS-09 | DELETE `/private/shipping/package/{package}` | path -> `ActionResult` |
| GET `/api/admin/v1/shipping/expedition` | MS-09 | GET `/private/shipping/expedition` | context -> `ExpeditionConfiguration` |
| POST `/api/admin/v1/shipping/expedition` | MS-09 | POST `/private/shipping/expedition` | `ExpeditionConfigurationRequest` -> `ExpeditionConfiguration` |

Configuration binds exact `ShippingConfiguration`, module, origin, package, and expedition
fields. Store switching reloads every read. The legacy methods screens preserve the selected
provider/module sequence but are read/configure only where the provider operations support it.

## Tax

| Frontend BFF method/path | Provider | Exact provider method/path | Request -> response |
|---|---|---|---|
| GET `/api/admin/v1/tax/classes` | MS-08 | GET `/tax-classes` | query -> `TaxClassListResponse` |
| POST `/api/admin/v1/tax/classes` | MS-08 | POST `/tax-classes` | `CreateTaxClassRequest` -> `TaxClass` |
| GET `/api/admin/v1/tax/classes/{id}` | MS-08 | GET `/tax-classes/{id}` | path -> `TaxClass` |
| PUT `/api/admin/v1/tax/classes/{id}` | MS-08 | PUT `/tax-classes/{id}` | `UpdateTaxClassRequest` -> `TaxClass` |
| DELETE `/api/admin/v1/tax/classes/{id}` | MS-08 | DELETE `/tax-classes/{id}` | path -> `DeleteResponse` |
| GET `/api/admin/v1/tax/rates` | MS-08 | GET `/tax-rates` | query -> `TaxRateListResponse` |
| POST `/api/admin/v1/tax/rates` | MS-08 | POST `/tax-rates` | `CreateTaxRateRequest` -> `TaxRate` |
| GET `/api/admin/v1/tax/rates/{id}` | MS-08 | GET `/tax-rates/{id}` | path -> `TaxRate` |
| PUT `/api/admin/v1/tax/rates/{id}` | MS-08 | PUT `/tax-rates/{id}` | `UpdateTaxRateRequest` -> `TaxRate` |
| DELETE `/api/admin/v1/tax/rates/{id}` | MS-08 | DELETE `/tax-rates/{id}` | path -> `DeleteResponse` |

Tax lists preserve provider pagination envelopes. Tax form country/zone selectors are deferred
until shared lookup contracts exist; they must not call the legacy country/zone paths.

## CONTRACT GAP

Legacy shipping rules (`/pages/shipping/rules` and `/pages/shipping/rules/add`) have no
matching MS-09 operation. The rules list/editor stays routable but is deferred. There is also
no provider operation for the legacy dynamic criteria/action lookup used by the editor.
