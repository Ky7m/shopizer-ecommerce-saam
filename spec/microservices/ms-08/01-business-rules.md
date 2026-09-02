# Tax Service Business Rules

**Service:** MS-08 Tax  
**Version:** 1.0  
**Date:** 2026-09-01  
**Analysis mode:** Hybrid — CAST transaction/data-graph scope plus direct source reading  
**Rule count:** 20 decomposition rules

## Scope and target corrections

MS-08 owns tax-class and tax-rate administration, jurisdiction selection, tax-rate resolution, tax calculation, and tax-calculation results. It consumes tenant/store context, product tax classifications, customer/address snapshots, item amounts, and shipping/handling amounts. It does not write cart, checkout, order, product, customer, shipping, or merchant tables.

The target specification explicitly corrects or resolves these legacy behaviors:

- Tax configuration serialization currently omits both province and country-policy booleans.
- Tax-rate lookup service methods receive a tax class but repository queries do not filter by it.
- The persistable-rate mapper assigns the request zone to the state/province field.
- Tax-rate uniqueness currently raises not-found instead of returning `false` when no rate exists.
- A default tax class is used for shipping but is not inserted into the class lookup map.
- Tax-rate output assumes a non-null zone although repository queries permit country-wide rates.
- Same-code tax consolidation calculates an aggregate without writing it back to the retained item.
- Tax configuration's “different country” flag replaces the selected customer jurisdiction with the store jurisdiction; the target API names this behavior explicitly.
- Target APIs use tenant/store/correlation headers instead of trusting store values supplied in request bodies.

### BR-TAX-CFG-001: Resolve tax configuration with a shipping-address default

**Statement:** Each store has a tax configuration. If no configuration has been saved, tax calculation uses the shipping address as its jurisdiction basis, permits collection in other provinces, and does not replace a foreign customer jurisdiction with the store jurisdiction.

**Intent:** Routing / Compliance
**Classification:** Core
**Weight:** Critical

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/tax/TaxServiceImpl.java:41-70,108-137`

**Cross-Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/tax/TaxService.java:17-24`

**Discovery Method:** Hybrid — CAST object `13426` plus direct source read

**CAST Reference:** Object `TaxServiceImpl` (`13426`); calculation component used by 21 CAST transactions. No dedicated calculation transaction detail was available.

**Logic:**
```pseudocode
configuration = merchantConfigurationService.getMerchantConfiguration("TAX_CONFIG", store)

IF configuration exists:
    taxConfiguration = parseJson(configuration.value)
    IF parsing fails:
        raise ServiceException("Cannot parse json string " + configuration.value)
ELSE:
    taxConfiguration.taxBasisCalculation = SHIPPINGADDRESS
    taxConfiguration.collectTaxIfDifferentProvinceOfStoreCountry = true
    taxConfiguration.collectTaxIfDifferentCountryOfStoreCountry = false

return taxConfiguration
```

**Data Dependencies:**
- Reads target `tax_schema.tax_configurations.tenant_id`
- Reads target `tax_schema.tax_configurations.store_id`
- Reads target `tax_schema.tax_configurations.tax_basis`
- Reads target `tax_schema.tax_configurations.collect_tax_if_different_province`
- Reads target `tax_schema.tax_configurations.different_country_behavior`
- Legacy source: `MerchantConfiguration.value` for key `TAX_CONFIG`

**Side Effects:**
- Calls the merchant-configuration provider in the legacy implementation.
- Target implementation reads MS-08-owned configuration storage.

**Concrete Example:**
- API Input: `GET /api/v1/tax-configuration` with `x-tenant-id: tenant-001`, `x-store-id: store-001`, `x-correlation-id: corr-001`
- Success Output: `200 {"taxBasis":"ShippingAddress","collectTaxIfDifferentProvince":true,"differentCountryBehavior":"UseCustomerJurisdiction"}`
- Error Input: Stored configuration value is malformed JSON: `{"taxBasisCalculation":`
- Error Output: `422 {"error":"TAX_CONFIGURATION_INVALID","message":"Tax configuration cannot be parsed","statusCode":422}`

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 4 | 4 | OK |
| Data-flow | 5 | 5 | OK |
| Constants | 4 | 4 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 0 | 0 | OK |
| Integrations | 1 | 1 | OK |
| Error paths | 1 | 1 | OK |

**Preservation:** OK

---

### BR-TAX-CFG-002: Persist every configurable tax-policy field

**Statement:** Saving a store's tax configuration must preserve the jurisdiction basis, province policy, and different-country behavior so that a subsequent calculation observes the values that an administrator saved.

**Intent:** Configuration / Compliance
**Classification:** Core
**Weight:** Critical

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/tax/TaxServiceImpl.java:74-89`; `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/tax/TaxConfiguration.java:13-53`

**Cross-Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/tax/TaxService.java:26-34`

**Discovery Method:** Hybrid — CAST object `13426` plus direct source read

**CAST Reference:** Object `TaxServiceImpl` (`13426`); data-graph scope includes configuration access through the calculation component.

**Logic:**
```pseudocode
configuration = load MerchantConfiguration where key = "TAX_CONFIG" and store = current_store

IF configuration does not exist:
    configuration.store = current_store
    configuration.key = "TAX_CONFIG"

configuration.value = serialize(
    taxBasisCalculation,
    collectTaxIfDifferentProvinceOfStoreCountry,
    collectTaxIfDifferentCountryOfStoreCountry
)

saveOrUpdate(configuration)
```

**Target correction:** The legacy `TaxConfiguration.toJSONString()` serializes only `taxBasisCalculation`; the two policy booleans are omitted. The target serializer must persist all three fields and expose the different-country behavior with an explicit enum.

**Data Dependencies:**
- Writes `tax_schema.tax_configurations.tax_basis`
- Writes `tax_schema.tax_configurations.collect_tax_if_different_province`
- Writes `tax_schema.tax_configurations.different_country_behavior`
- Writes `tax_schema.tax_configurations.updated_at`
- Legacy write target: `MerchantConfiguration.value`

**Side Effects:**
- Updates one configuration row for the tenant/store scope.
- No event is published.

**Concrete Example:**
- API Input: `PUT /api/v1/tax-configuration {"taxBasis":"BillingAddress","collectTaxIfDifferentProvince":false,"differentCountryBehavior":"UseStoreJurisdiction"}`
- Success Output: `200 {"taxBasis":"BillingAddress","collectTaxIfDifferentProvince":false,"differentCountryBehavior":"UseStoreJurisdiction"}`
- Error Input: `{"taxBasis":"UnknownBasis","collectTaxIfDifferentProvince":false,"differentCountryBehavior":"UseStoreJurisdiction"}`
- Error Output: `422 {"error":"INVALID_TAX_BASIS","message":"taxBasis must be StoreAddress, ShippingAddress, or BillingAddress","statusCode":422}`

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 3 | 3 | OK |
| Data-flow | 5 | 5 | OK |
| Constants | 1 | 2 | FLAGGED — target adds explicit policy enum |
| State transitions | 0 | 0 | OK |
| Outcomes | 2 | 2 | OK |
| Data writes | 2 | 4 | FLAGGED — target repairs omitted persisted fields |
| Integrations | 1 | 1 | OK |
| Error paths | 0 | 1 | FLAGGED — target validates enum explicitly |

**Preservation:** FLAGGED — target correction required

## Phase 4b inferred clarifications

The following assumptions were applied in Mode A and are not validated by a domain expert:

- `[Inferred in Phase 4b — Mode A]` An optional external tax provider receives destination,
  tax-class, taxable-line, and currency data and must return tax amount, rate, jurisdiction,
  and provider reference.
- `[Inferred in Phase 4b — Mode A]` Provider calls use a bounded timeout; when no approved
  fallback is configured, timeout or provider rejection returns a typed provider error.
- `[Inferred in Phase 4b — Mode A]` A quote with no matching rate returns a successful zero-tax
  result only when the jurisdiction policy explicitly permits zero tax; otherwise it is a
  typed validation failure.

---

### BR-TAX-CLS-001: Create a tax class unique within a tenant and store

**Statement:** A tax class code may be created only once within the requesting tenant and store. The store scope is taken from authenticated request context, not from a client-supplied store value.

**Intent:** Validation / Authorization
**Classification:** Core
**Weight:** Critical

**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/facade/tax/TaxFacadeImpl.java:59-83`; `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/tax/TaxClassRepository.java:18-19`; `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/tax/taxclass/TaxClass.java:28-32,45-67`

**CAST Reference:** Object `TaxFacadeImpl` (`20996`); transaction `243999` POST tax class.

**Logic:**
```pseudocode
assert request exists
assert store context exists

IF taxClassRepository.findByStoreAndCode(store.id, request.code) exists:
    reject with TAX_CLASS_ALREADY_EXISTS

request.store = store.code
model = PersistableTaxClassMapper.convert(request, store, language)
saved = taxClassService.saveOrUpdate(model)

return saved.id
```

**Data Dependencies:**
- Reads `tax_schema.tax_classes.tenant_id`
- Reads `tax_schema.tax_classes.store_id`
- Reads `tax_schema.tax_classes.code`
- Writes `tax_schema.tax_classes.code`
- Writes `tax_schema.tax_classes.title`

**Concrete Example:**
- API Input: `POST /api/v1/tax-classes {"code":"REDUCED","title":"Reduced rate"}`
- Success Output: `201 {"id":"9c8f6b2a-89d5-4f7f-a4e0-1f3dc7b1f100","code":"REDUCED","title":"Reduced rate","storeId":"store-001"}`
- Error Input: `{"code":"REDUCED","title":"Second reduced rate"}`
- Error Output: `409 {"error":"TAX_CLASS_ALREADY_EXISTS","message":"Tax class code REDUCED already exists for store store-001","statusCode":409}`

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 3 | 3 | OK |
| Data-flow | 5 | 5 | OK |
| Constants | 2 | 2 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 2 | 2 | OK |
| Integrations | 0 | 0 | OK |
| Error paths | 1 | 1 | OK |

**Preservation:** OK

---

### BR-TAX-CLS-002: Expose only store-owned tax classes

**Statement:** Tax-class lists and lookups must return only tax classes belonging to the authenticated tenant and store; a class belonging to another store is not visible.

**Intent:** Authorization / Routing
**Classification:** Core
**Weight:** Critical

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/tax/TaxClassServiceImpl.java:29-42`; `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/tax/TaxClassRepository.java:12-19`; `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/facade/tax/TaxFacadeImpl.java:108-129,158-181`

**CAST Reference:** Transactions `244002`, `244000`, and `244003`; object `TaxClassServiceImpl` (`13397`).

**Logic:**
```pseudocode
list:
    models = find TaxClass where merchantStore.id = current_store.id
    return map each model to response

get by code:
    model = find TaxClass where merchantStore.id = current_store.id and code = requested_code
    IF model is absent:
        return TAX_CLASS_NOT_FOUND
    IF model.merchantStore.code != current_store.code:
        return UNAUTHORIZED
    return mapped model
```

**Data Dependencies:**
- Reads `tax_schema.tax_classes.tenant_id`
- Reads `tax_schema.tax_classes.store_id`
- Reads `tax_schema.tax_classes.code`
- Reads `tax_schema.tax_classes.title`

**Concrete Example:**
- API Input: `GET /api/v1/tax-classes/REDUCED` for `store-001`
- Success Output: `200 {"id":"9c8f6b2a-89d5-4f7f-a4e0-1f3dc7b1f100","code":"REDUCED","title":"Reduced rate","storeId":"store-001"}`
- Error Input: Same request for `store-002`, where `REDUCED` exists only for `store-001`
- Error Output: `404 {"error":"TAX_CLASS_NOT_FOUND","message":"Tax class REDUCED was not found for store store-002","statusCode":404}`

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 5 | 5 | OK |
| Data-flow | 4 | 4 | OK |
| Constants | 0 | 0 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 4 | 4 | OK |
| Data writes | 0 | 0 | OK |
| Integrations | 0 | 0 | OK |
| Error paths | 2 | 2 | OK |

**Preservation:** OK

---

### BR-TAX-CLS-003: Permit mutation only for the owning store

**Statement:** A tax class can be updated or deleted only when the identified class belongs to the authenticated tenant and store. Missing classes return not-found; classes owned by another store return unauthorized.

**Intent:** Authorization / State Transition
**Classification:** Core
**Weight:** High

**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/facade/tax/TaxFacadeImpl.java:85-106,135-156`; `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/tax/TaxClassServiceImpl.java:44-50,66-74`

**CAST Reference:** Transactions `244001` PUT and `244004` DELETE; object `TaxFacadeImpl` (`20996`).

**Logic:**
```pseudocode
model = load TaxClass by id

IF model is absent:
    reject TAX_CLASS_NOT_FOUND

IF model.store_id != current_store.id:
    reject UNAUTHORIZED

IF operation = UPDATE:
    replace code and title
    retain current tenant/store ownership
    save model

IF operation = DELETE:
    delete model
```

**Data Dependencies:**
- Reads `tax_schema.tax_classes.id`
- Reads `tax_schema.tax_classes.tenant_id`
- Reads `tax_schema.tax_classes.store_id`
- Writes `tax_schema.tax_classes.code`
- Writes `tax_schema.tax_classes.title`
- Deletes from `tax_schema.tax_classes`

**Concrete Example:**
- API Input: `PUT /api/v1/tax-classes/9c8f6b2a-89d5-4f7f-a4e0-1f3dc7b1f100 {"code":"REDUCED","title":"Reduced VAT"}`
- Success Output: `200 {"id":"9c8f6b2a-89d5-4f7f-a4e0-1f3dc7b1f100","code":"REDUCED","title":"Reduced VAT","storeId":"store-001"}`
- Error Input: `DELETE /api/v1/tax-classes/other-store-class-id` under `store-001`
- Error Output: `403 {"error":"TAX_CLASS_STORE_MISMATCH","message":"Tax class is not owned by store store-001","statusCode":403}`

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 7 | 7 | OK |
| Data-flow | 6 | 6 | OK |
| Constants | 0 | 0 | OK |
| State transitions | 2 | 2 | OK |
| Outcomes | 5 | 5 | OK |
| Data writes | 3 | 3 | OK |
| Integrations | 0 | 0 | OK |
| Error paths | 3 | 3 | OK |

**Preservation:** OK

---

### BR-TAX-RAT-001: Create a tax rate unique within a tenant and store

**Statement:** A tax-rate code may be created only once within the requesting tenant and store. The rate must identify a tax class, country, percentage, and localized description.

**Intent:** Validation / Compliance
**Classification:** Core
**Weight:** Critical

**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/facade/tax/TaxFacadeImpl.java:260-291`; `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/tax/TaxRateRepository.java:18-19`; `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/tax/taxrate/TaxRate.java:54-110`

**CAST Reference:** Transaction `244156` POST tax rate; object `TaxFacadeImpl` (`20996`).

**Logic:**
```pseudocode
existing = find TaxRate where store_id = current_store.id and code = request.code

IF existing exists:
    reject TAX_RATE_ALREADY_EXISTS

resolve request.countryCode
resolve request.zoneCode if present
resolve request.taxClassCode within current store
create rate with current tenant/store
persist descriptions
save rate
return rate.id
```

**Data Dependencies:**
- Reads `tax_schema.tax_rates.tenant_id`
- Reads `tax_schema.tax_rates.store_id`
- Reads `tax_schema.tax_rates.code`
- Writes `tax_schema.tax_rates.code`
- Writes `tax_schema.tax_rates.rate_percent`
- Writes `tax_schema.tax_rates.tax_class_id`
- Writes `tax_schema.tax_rate_descriptions.name`
- Writes `tax_schema.tax_rate_descriptions.language_code`

**Concrete Example:**
- API Input: `POST /api/v1/tax-rates {"code":"CA-REDUCED","rate":7.25,"priority":1,"piggyback":false,"taxClassCode":"REDUCED","countryCode":"CA","zoneCode":"CA-ON","descriptions":[{"languageCode":"en","name":"Ontario reduced tax"}]}`
- Success Output: `201 {"id":"e2f5a32e-0e6e-4be0-8d38-1b62fc29bf20","code":"CA-REDUCED","rate":7.25,"priority":1,"piggyback":false,"taxClassCode":"REDUCED","countryCode":"CA","zoneCode":"CA-ON","stateProvince":null}`
- Error Input: Same code `CA-REDUCED` in the same store
- Error Output: `409 {"error":"TAX_RATE_ALREADY_EXISTS","message":"Tax rate code CA-REDUCED already exists for store store-001","statusCode":409}`

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 3 | 3 | OK |
| Data-flow | 8 | 8 | OK |
| Constants | 0 | 0 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 5 | 5 | OK |
| Integrations | 0 | 0 | OK |
| Error paths | 1 | 1 | OK |

**Preservation:** OK

---

### BR-TAX-RAT-002: Update a rate without changing its store ownership

**Statement:** Updating a tax rate replaces its editable rate, priority, classification, geography, compound flag, and descriptions while retaining the rate identifier and authenticated tenant/store ownership.

**Intent:** State Transition / Validation
**Classification:** Core
**Weight:** High

**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/facade/tax/TaxFacadeImpl.java:293-314`; `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/mapper/tax/PersistableTaxRateMapper.java:38-75`

**CAST Reference:** Transaction `244239` PUT tax rate; object `TaxFacadeImpl` (`20996`).

**Logic:**
```pseudocode
model = find TaxRate by id and current_store.id

IF model is absent:
    reject TAX_RATE_NOT_FOUND

model.code = request.code
model.taxPriority = request.priority
model.country = resolve(request.country)
model.zone = resolve(request.zone)
model.stateProvince = request.stateProvince
model.merchantStore = current_store
model.taxClass = find tax class request.taxClassCode within current_store
model.taxRate = request.rate
merge descriptions by languageCode
save model
```

**Target correction:** The legacy mapper sets `stateProvince` from `source.zone`; the target uses the explicit `stateProvince` field and preserves a nullable independent zone.

**Data Dependencies:**
- Reads `tax_schema.tax_rates.id`
- Reads `tax_schema.tax_rates.store_id`
- Writes `tax_schema.tax_rates.code`
- Writes `tax_schema.tax_rates.rate_percent`
- Writes `tax_schema.tax_rates.priority`
- Writes `tax_schema.tax_rates.zone_code`
- Writes `tax_schema.tax_rates.state_province`
- Writes `tax_schema.tax_rates.tax_class_id`
- Writes `tax_schema.tax_rate_descriptions.*`

**Concrete Example:**
- API Input: `PUT /api/v1/tax-rates/e2f5a32e-0e6e-4be0-8d38-1b62fc29bf20 {"code":"CA-REDUCED","rate":8.25,"priority":2,"piggyback":false,"taxClassCode":"REDUCED","countryCode":"CA","zoneCode":null,"stateProvince":"Ontario","descriptions":[{"languageCode":"en","name":"Ontario reduced tax"}]}`
- Success Output: `200 {"id":"e2f5a32e-0e6e-4be0-8d38-1b62fc29bf20","rate":8.25,"zoneCode":null,"stateProvince":"Ontario","taxClassCode":"REDUCED"}`
- Error Input: Rate identifier belongs to `store-002`, request context is `store-001`
- Error Output: `404 {"error":"TAX_RATE_NOT_FOUND","message":"Tax rate was not found for store store-001","statusCode":404}`

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 5 | 5 | OK |
| Data-flow | 10 | 10 | OK |
| Constants | 0 | 0 | OK |
| State transitions | 1 | 1 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 8 | 8 | OK |
| Integrations | 0 | 0 | OK |
| Error paths | 2 | 2 | OK |

**Preservation:** OK with mapper defect corrected

---

### BR-TAX-RAT-003: List localized tax rates in priority order

**Statement:** A store's tax-rate list is limited to the requested tenant/store and language, and rates are returned in ascending priority order.

**Intent:** Routing / Compliance
**Classification:** Core
**Weight:** Critical

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/tax/TaxRateServiceImpl.java:31-41`; `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/tax/TaxRateRepository.java:15-16`; `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/facade/tax/TaxFacadeImpl.java:330-352`

**CAST Reference:** Transaction `244240` GET tax rates; object `TaxRateServiceImpl` (`13411`).

**Logic:**
```pseudocode
rates = find rates where store_id = current_store.id
                     and description.language_code = requested_language
                     order by priority ascending

map each rate to the localized response
return items with total count and one-page legacy-compatible pagination
```

**Data Dependencies:**
- Reads `tax_schema.tax_rates.store_id`
- Reads `tax_schema.tax_rates.priority`
- Reads `tax_schema.tax_rate_descriptions.language_code`
- Reads `tax_schema.tax_rate_descriptions.name`

**Concrete Example:**
- API Input: `GET /api/v1/tax-rates?languageCode=en`
- Success Output: `200 {"items":[{"code":"CA-GENERAL","priority":1},{"code":"CA-REDUCED","priority":2}],"pagination":{"page":1,"pageSize":20,"totalItems":2,"totalPages":1}}`
- Error Input: `languageCode=xx` when no supported language exists
- Error Output: `422 {"error":"LANGUAGE_NOT_SUPPORTED","message":"Language xx is not supported","statusCode":422}`

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 3 | 3 | OK |
| Data-flow | 5 | 5 | OK |
| Constants | 2 | 2 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 2 | 2 | OK |
| Data writes | 0 | 0 | OK |
| Integrations | 0 | 0 | OK |
| Error paths | 1 | 1 | OK |

**Preservation:** OK

---

### BR-TAX-RAT-004: Read and delete only store-owned rates

**Statement:** A tax rate can be read or deleted only through a store-scoped lookup. The operation must never expose or delete a rate belonging to another store.

**Intent:** Authorization / State Transition
**Classification:** Core
**Weight:** High

**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/facade/tax/TaxFacadeImpl.java:220-258`; `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/tax/TaxRateServiceImpl.java:77-80`; `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/tax/TaxRateRepository.java:21-22`

**CAST Reference:** Transactions `244241` GET and `244242` DELETE tax rate.

**Logic:**
```pseudocode
model = find TaxRate where id = requested_id and store_id = current_store.id

IF model is absent:
    reject TAX_RATE_NOT_FOUND

IF operation = GET:
    return mapped model

IF operation = DELETE:
    delete model
    return deleted = true
```

**Data Dependencies:**
- Reads `tax_schema.tax_rates.id`
- Reads `tax_schema.tax_rates.tenant_id`
- Reads `tax_schema.tax_rates.store_id`
- Deletes from `tax_schema.tax_rates`

**Concrete Example:**
- API Input: `DELETE /api/v1/tax-rates/e2f5a32e-0e6e-4be0-8d38-1b62fc29bf20`
- Success Output: `200 {"deleted":true,"id":"e2f5a32e-0e6e-4be0-8d38-1b62fc29bf20"}`
- Error Input: `GET /api/v1/tax-rates/missing-rate-id`
- Error Output: `404 {"error":"TAX_RATE_NOT_FOUND","message":"Tax rate was not found for store store-001","statusCode":404}`

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 4 | 4 | OK |
| Data-flow | 5 | 5 | OK |
| Constants | 0 | 0 | OK |
| State transitions | 1 | 1 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 1 | 1 | OK |
| Integrations | 0 | 0 | OK |
| Error paths | 2 | 2 | OK |

**Preservation:** OK

---

### BR-TAX-RAT-005: Rate uniqueness checks return a boolean

**Statement:** A rate uniqueness check returns `true` when the store contains the requested code and `false` when it does not; absence is not a not-found error.

**Intent:** Validation
**Classification:** Core
**Weight:** Critical

**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/facade/tax/TaxFacadeImpl.java:316-328`; `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/facade/tax/TaxFacadeImpl.java:195-218`

**CAST Reference:** Transaction `244238` GET unique tax rate.

**Logic:**
```pseudocode
rate = find TaxRate where store_id = current_store.id and code = requested_code

IF rate exists:
    return exists = true
ELSE:
    return exists = false
```

**Target correction:** The legacy `existsTaxRate` delegates to `taxRateByCode`, which raises `ResourceNotFoundException` when no row exists; the target returns the declared boolean result.

**Data Dependencies:**
- Reads `tax_schema.tax_rates.store_id`
- Reads `tax_schema.tax_rates.code`

**Concrete Example:**
- API Input: `GET /api/v1/tax-rates/exists?code=CA-NEW`
- Success Output: `200 {"exists":false}`
- Error Input: `GET /api/v1/tax-rates/exists` without `code`
- Error Output: `400 {"error":"MISSING_CODE","message":"Query parameter code is required","statusCode":400}`

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 3 | 3 | OK |
| Data-flow | 3 | 3 | OK |
| Constants | 0 | 0 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 2 | 2 | FLAGGED — source defect corrected |
| Data writes | 0 | 0 | OK |
| Integrations | 0 | 0 | OK |
| Error paths | 1 | 1 | FLAGGED — absence becomes valid false result |

**Preservation:** FLAGGED — target correction required

---

### BR-TAX-CAL-001: Return defined empty results for absent calculation inputs

**Statement:** A tax calculation without a customer context produces no tax result, while a calculation with an order summary but no product items produces an empty tax-item list. The target API rejects structurally incomplete requests before domain execution.

**Intent:** Validation / Calculation
**Classification:** Active
**Weight:** Medium

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/tax/TaxServiceImpl.java:91-105`

**CAST Reference:** Object `TaxServiceImpl` (`13426`), calculation callers resolved through 21 CAST transactions.

**Logic:**
```pseudocode
IF customer is null:
    legacy return null

items = orderSummary.products

IF items is null:
    legacy return empty list

target:
    reject missing customerSnapshot or items with 422
```

**Data Dependencies:**
- Reads calculation request customer snapshot
- Reads calculation request item collection
- Writes no tax tables before successful calculation

**Concrete Example:**
- API Input: `POST /api/v1/tax-calculations {"currencyCode":"CAD","customer":null,"items":[]}`
- Success Output: `422 {"error":"CUSTOMER_CONTEXT_REQUIRED","message":"Customer address context is required for tax calculation","statusCode":422}`
- Error Input: `POST /api/v1/tax-calculations {"currencyCode":"CAD","items":[]}` with no customer
- Error Output: `422 {"error":"CUSTOMER_CONTEXT_REQUIRED","message":"Customer address context is required for tax calculation","statusCode":422}`

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 3 | 3 | OK |
| Data-flow | 3 | 3 | OK |
| Constants | 0 | 0 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 2 | 2 | FLAGGED — target uses explicit 422 |
| Data writes | 0 | 0 | OK |
| Integrations | 0 | 0 | OK |
| Error paths | 1 | 1 | FLAGGED — target validates before execution |

**Preservation:** FLAGGED — target API correction

---

### BR-TAX-CAL-002: Select the configured jurisdiction basis

**Statement:** Tax jurisdiction is selected from the store address, shipping address, or billing address according to the store's configured basis. Shipping is the default basis; if shipping address data is absent, the existing billing-derived jurisdiction is retained.

**Intent:** Routing / Compliance
**Classification:** Core
**Weight:** Critical

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/tax/TaxServiceImpl.java:107-137`

**CAST Reference:** Object `TaxServiceImpl` (`13426`); tax calculation callers from 21 transactions.

**Logic:**
```pseudocode
country = customer.billing.country
zone = customer.billing.zone
stateProvince = customer.billing.state

basis = taxConfiguration.taxBasisCalculation

IF basis = SHIPPINGADDRESS AND customer.delivery exists:
    country = customer.delivery.country
    zone = customer.delivery.zone
    stateProvince = customer.delivery.state

ELSE IF basis = BILLINGADDRESS AND customer.billing exists:
    country = customer.billing.country
    zone = customer.billing.zone
    stateProvince = customer.billing.state

ELSE IF basis = STOREADDRESS:
    country = store.country
    zone = store.zone
    stateProvince = store.storestateprovince
```

**Target correction:** The target validates billing/address snapshots before dereferencing them and represents the fallback explicitly.

**Data Dependencies:**
- Reads `customer.billing.countryCode`
- Reads `customer.billing.zoneCode`
- Reads `customer.billing.stateProvince`
- Reads `customer.delivery.countryCode`
- Reads `customer.delivery.zoneCode`
- Reads `customer.delivery.stateProvince`
- Reads store address context
- Reads `tax_schema.tax_configurations.tax_basis`

**Concrete Example:**
- API Input: `POST /api/v1/tax-calculations {"currencyCode":"CAD","customer":{"billing":{"countryCode":"CA","zoneCode":"ON","stateProvince":"Ontario"},"shipping":{"countryCode":"CA","zoneCode":"QC","stateProvince":"Quebec"}},"items":[{"productId":"p-100","quantity":1,"unitAmount":100,"taxClassCode":"STANDARD"}]}`
- Success Output: `200 {"jurisdiction":{"countryCode":"CA","zoneCode":"QC","stateProvince":"Quebec"},"totalTaxAmount":14.98}`
- Error Input: Configuration requests `BillingAddress` but billing snapshot is absent
- Error Output: `422 {"error":"BILLING_ADDRESS_REQUIRED","message":"Billing address is required by the configured tax basis","statusCode":422}`

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 8 | 8 | OK |
| Data-flow | 9 | 9 | OK |
| Constants | 4 | 4 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 0 | 0 | OK |
| Integrations | 0 | 0 | OK |
| Error paths | 1 | 1 | FLAGGED — target exposes implicit null failure |

**Preservation:** FLAGGED — target makes implicit address failure explicit

---

### BR-TAX-CAL-003: Suppress tax for disallowed province differences

**Statement:** When the store configuration disallows tax collection in other provinces, a customer jurisdiction is taxable only when its zone or province matches the store's configured jurisdiction; otherwise the calculation returns no tax.

**Intent:** Compliance / Routing
**Classification:** Core
**Weight:** Critical

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/tax/TaxServiceImpl.java:139-158`

**CAST Reference:** Object `TaxServiceImpl` (`13426`).

**Logic:**
```pseudocode
IF collectTaxIfDifferentProvinceOfStoreCountry = false:
    IF customer.zone and store.zone exist AND customer.zone.id != store.zone.id:
        return null

    IF stateProvince is nonblank:
        IF store.zone exists AND store.zone.name != stateProvince:
            return null
        ELSE IF store.zone absent
                AND store.storestateprovince nonblank
                AND store.storestateprovince != stateProvince:
            return null
```

**Data Dependencies:**
- Reads `tax_schema.tax_configurations.collect_tax_if_different_province`
- Reads customer jurisdiction snapshot
- Reads store jurisdiction snapshot
- Reads tax-rate tables only after the province guard passes

**Concrete Example:**
- API Input: Customer in Quebec, store in Ontario, configuration `collectTaxIfDifferentProvince=false`
- Success Output: `200 {"jurisdiction":{"countryCode":"CA","zoneCode":"QC"},"taxItems":[],"totalTaxAmount":0}`
- Error Input: Same request interpreted as a taxable calculation
- Error Output: `422 {"error":"TAX_NOT_APPLICABLE","message":"Tax is not collected for the selected province","statusCode":422}`

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 8 | 8 | OK |
| Data-flow | 7 | 7 | OK |
| Constants | 0 | 0 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 2 | 2 | OK |
| Data writes | 0 | 0 | OK |
| Integrations | 0 | 0 | OK |
| Error paths | 1 | 1 | OK |

**Preservation:** OK

---

### BR-TAX-CAL-004: Apply explicit different-country behavior

**Statement:** For a customer in a different country, the configured country behavior determines whether tax uses the customer's jurisdiction, the store jurisdiction, or produces no tax; the behavior must not be inferred from a boolean with ambiguous naming.

**Intent:** Compliance / Routing
**Classification:** Core
**Weight:** Critical

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/tax/TaxServiceImpl.java:160-166`; `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/tax/TaxConfiguration.java:36-51`

**CAST Reference:** Object `TaxServiceImpl` (`13426`).

**Logic:**
```pseudocode
IF differentCountryBehavior = UseStoreJurisdiction:
    country = store.country
    zone = store.zone
    stateProvince = store.storestateprovince

ELSE IF differentCountryBehavior = UseCustomerJurisdiction:
    retain selected customer jurisdiction

ELSE IF differentCountryBehavior = NoTax:
    return no tax
```

**Target correction:** The source boolean `collectTaxIfDifferentCountryOfStoreCountry` causes store-address substitution when `true`; the target retains compatibility mapping but exposes the behavior explicitly.

**Data Dependencies:**
- Reads `tax_schema.tax_configurations.different_country_behavior`
- Reads customer country and jurisdiction
- Reads store country and jurisdiction

**Concrete Example:**
- API Input: Customer country `US`, store country `CA`, configuration `differentCountryBehavior=UseStoreJurisdiction`
- Success Output: `200 {"jurisdiction":{"countryCode":"CA","zoneCode":"ON"},"totalTaxAmount":13.00}`
- Error Input: Customer country `US`, configuration `differentCountryBehavior=NoTax`
- Error Output: `200 {"jurisdiction":{"countryCode":"US"},"taxItems":[],"totalTaxAmount":0}`

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 3 | 3 | OK |
| Data-flow | 6 | 6 | OK |
| Constants | 1 | 3 | FLAGGED — target names all supported behaviors |
| State transitions | 0 | 0 | OK |
| Outcomes | 2 | 3 | FLAGGED — target adds explicit no-tax outcome |
| Data writes | 0 | 0 | OK |
| Integrations | 0 | 0 | OK |
| Error paths | 0 | 0 | OK |

**Preservation:** FLAGGED — ambiguous legacy policy resolved in target

---

### BR-TAX-CAL-005: Require a usable tax jurisdiction

**Statement:** Tax calculation proceeds only when the selected jurisdiction has either a zone or a nonblank province/state; without either geographic discriminator, the result contains no tax.

**Intent:** Validation / Compliance
**Classification:** Core
**Weight:** Critical

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/tax/TaxServiceImpl.java:168-170`

**CAST Reference:** Object `TaxServiceImpl` (`13426`).

**Logic:**
```pseudocode
IF selected_zone is null AND selected_stateProvince is blank:
    return null
```

**Data Dependencies:**
- Reads selected jurisdiction `zone_code`
- Reads selected jurisdiction `state_province`

**Concrete Example:**
- API Input: Customer address `{countryCode:"CA",zoneCode:null,stateProvince:null}`
- Success Output: `200 {"jurisdiction":{"countryCode":"CA"},"taxItems":[],"totalTaxAmount":0}`
- Error Input: Same address submitted where the client expects a rate
- Error Output: `422 {"error":"JURISDICTION_INCOMPLETE","message":"A zone or stateProvince is required to calculate tax","statusCode":422}`

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 2 | 2 | OK |
| Data-flow | 2 | 2 | OK |
| Constants | 0 | 0 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 1 | 1 | OK |
| Data writes | 0 | 0 | OK |
| Integrations | 0 | 0 | OK |
| Error paths | 0 | 1 | FLAGGED — target can expose diagnostic code |

**Preservation:** FLAGGED — target diagnostic is an intentional improvement

---

### BR-TAX-CAL-006: Aggregate taxable amounts by tax class

**Statement:** Each line contributes unit amount multiplied by quantity to the subtotal of its tax class. A line without a tax class is assigned to the store's `DEFAULT` tax class.

**Intent:** Calculation / Compliance
**Classification:** Core
**Weight:** Critical

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/tax/TaxServiceImpl.java:172-195`; `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/tax/taxclass/TaxClass.java:35-38`

**CAST Reference:** Object `TaxServiceImpl` (`13426`).

**Logic:**
```pseudocode
FOR each item in orderSummary.products:
    lineAmount = item.itemPrice * item.quantity
    taxClass = item.product.taxClass

    IF taxClass is null:
        taxClass = taxClassService.getByCode("DEFAULT")

    taxableAmountByClass[taxClass.id] += lineAmount
    taxClassById[taxClass.id] = taxClass
```

**Target correction:** The default class must be present in both the amount map and class map before shipping or item taxation.

**Data Dependencies:**
- Reads calculation item `unit_amount`
- Reads calculation item `quantity`
- Reads calculation item `tax_class_code`
- Reads `tax_schema.tax_classes.id`
- Reads `tax_schema.tax_classes.code`
- Writes no order or product tables

**Concrete Example:**
- API Input: `{"items":[{"productId":"p-1","quantity":2,"unitAmount":25,"taxClassCode":"REDUCED"},{"productId":"p-2","quantity":1,"unitAmount":100,"taxClassCode":null}]}`
- Success Output: `200 {"taxableAmountsByClass":{"REDUCED":50,"DEFAULT":100},"totalTaxAmount":15.00}`
- Error Input: `{"items":[{"productId":"p-1","quantity":0,"unitAmount":25,"taxClassCode":"REDUCED"}]}`
- Error Output: `422 {"error":"INVALID_QUANTITY","message":"quantity must be greater than zero","statusCode":422}`

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 5 | 5 | OK |
| Data-flow | 7 | 7 | OK |
| Constants | 2 | 2 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 2 | 2 | OK |
| Data writes | 0 | 0 | OK |
| Integrations | 1 | 1 | OK |
| Error paths | 0 | 1 | FLAGGED — target validates quantity |

**Preservation:** FLAGGED — target input invariant added

---

### BR-TAX-CAL-007: Tax positive shipping and handling under the default class

**Statement:** When shipping is positive, shipping is taxable under the default tax class; positive handling is added to the same default-class taxable amount. Zero or negative shipping and handling are excluded.

**Intent:** Calculation / Compliance
**Classification:** Core
**Weight:** Critical

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/tax/TaxServiceImpl.java:197-220`

**CAST Reference:** Object `TaxServiceImpl` (`13426`).

**Logic:**
```pseudocode
defaultClass = taxClassService.getByCode("DEFAULT")
amount = taxableAmountByClass[defaultClass.id] or 0

IF shippingSummary exists
   AND shippingSummary.shipping exists
   AND shippingSummary.shipping > 0:
    amount += shippingSummary.shipping

    IF shippingSummary.handling exists AND shippingSummary.handling > 0:
        amount += shippingSummary.handling

taxableAmountByClass[defaultClass.id] = amount
```

**Target correction:** The target explicitly inserts `DEFAULT` into the class map when shipping creates the class subtotal.

**Data Dependencies:**
- Reads shipping input `shipping_amount`
- Reads shipping input `handling_amount`
- Reads `tax_schema.tax_classes.code`
- Reads/writes in-memory default-class taxable subtotal

**Concrete Example:**
- API Input: `{"items":[{"quantity":1,"unitAmount":100,"taxClassCode":"DEFAULT"}],"shipping":{"shippingAmount":10,"handlingAmount":2}}`
- Success Output: `200 {"taxableAmountsByClass":{"DEFAULT":112},"taxItems":[{"taxClassCode":"DEFAULT","taxAmount":14.56}],"totalTaxAmount":14.56}`
- Error Input: `{"shipping":{"shippingAmount":-5,"handlingAmount":2}}`
- Error Output: `422 {"error":"INVALID_SHIPPING_AMOUNT","message":"shippingAmount cannot be negative","statusCode":422}`

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 6 | 6 | OK |
| Data-flow | 5 | 5 | OK |
| Constants | 0 | 0 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 2 | 2 | OK |
| Data writes | 0 | 0 | OK |
| Integrations | 0 | 0 | OK |
| Error paths | 0 | 1 | FLAGGED — target validates negative values |

**Preservation:** FLAGGED — target input invariant added

---

### BR-TAX-CAL-008: Resolve rates by store, country, geography, language, and tax class

**Statement:** A tax calculation may apply only rates belonging to the current tenant/store, selected country and geographic basis, requested language, and line tax class. Rates are evaluated in ascending priority.

**Intent:** Routing / Compliance
**Classification:** Core
**Weight:** Critical

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/tax/TaxServiceImpl.java:223-237`; `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/tax/TaxRateServiceImpl.java:50-58`; `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/tax/TaxRateRepository.java:27-31`

**CAST Reference:** Object `TaxRateServiceImpl` (`13411`); data graph `243924`; calculation object `TaxServiceImpl` (`13426`).

**Logic:**
```pseudocode
FOR each taxClassId:
    IF stateProvince is nonblank AND zone is null:
        rates = find by store, country, stateProvince, language, taxClassId
    ELSE:
        rates = find by store, country, zone, language, taxClassId

    order rates by priority ascending
```

**Target correction:** The legacy repository query joins `TaxRate.taxClass` but does not constrain it, and the service ignores its `taxClass` argument. The target query includes `tax_class_id`.

**Data Dependencies:**
- Reads `tax_schema.tax_rates.tenant_id`
- Reads `tax_schema.tax_rates.store_id`
- Reads `tax_schema.tax_rates.country_code`
- Reads `tax_schema.tax_rates.zone_code`
- Reads `tax_schema.tax_rates.state_province`
- Reads `tax_schema.tax_rates.tax_class_id`
- Reads `tax_schema.tax_rate_descriptions.language_code`
- Reads `tax_schema.tax_rates.priority`

**Concrete Example:**
- API Input: Item class `REDUCED`, country `CA`, zone `ON`, language `en`
- Success Output: `200 {"appliedRates":[{"code":"CA-ON-REDUCED","taxClassCode":"REDUCED","rate":7.25}]}`
- Error Input: Only a `STANDARD` rate exists for `CA/ON`
- Error Output: `200 {"appliedRates":[],"taxItems":[],"totalTaxAmount":0}`

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 5 | 5 | OK |
| Data-flow | 12 | 12 | OK |
| Constants | 0 | 0 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 0 | 0 | OK |
| Integrations | 0 | 0 | OK |
| Error paths | 0 | 0 | OK |

**Preservation:** OK with repository defect corrected

---

### BR-TAX-CAL-009: Apply rates sequentially with compound-rate semantics

**Statement:** A non-compound rate is calculated from the original tax-class taxable amount. A compound rate is calculated from the running amount after earlier tax has been added. Each tax amount is rounded to two decimal places using half-up rounding.

**Intent:** Calculation / Compliance
**Classification:** Core
**Weight:** Critical

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/tax/TaxServiceImpl.java:239-265`; `initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/tax/taxrate/TaxRate.java:73-85`

**CAST Reference:** Object `TaxServiceImpl` (`13426`).

**Logic:**
```pseudocode
beforeTaxAmount = taxableAmountByClass[taxClassId]
runningTaxedAmount = 0

FOR rate ordered by priority:
    IF rate.piggyback = true AND runningTaxedAmount > 0:
        calculationBase = runningTaxedAmount
    ELSE:
        calculationBase = beforeTaxAmount

    taxAmount = roundHalfUp(calculationBase * rate.ratePercent / 100, 2)
    runningTaxedAmount = calculationBase + taxAmount

    emit TaxItem(
        taxCode = rate.code,
        taxRatePercent = rate.ratePercent,
        taxAmount = taxAmount,
        label = localized description name
    )
```

**Data Dependencies:**
- Reads `tax_schema.tax_rates.rate_percent`
- Reads `tax_schema.tax_rates.piggyback`
- Reads `tax_schema.tax_rates.priority`
- Reads `tax_schema.tax_rate_descriptions.name`
- Reads tax-class taxable amount

**Concrete Example:**
- API Input: Taxable amount `100.00`; rate A `5%`, non-compound; rate B `2%`, compound
- Success Output: `200 {"taxItems":[{"taxCode":"A","taxAmount":5.00},{"taxCode":"B","taxAmount":2.10}],"totalTaxAmount":7.10}`
- Error Input: Rate percent `-1`
- Error Output: `422 {"error":"INVALID_TAX_RATE","message":"rate must be between 0 and 100","statusCode":422}`

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 7 | 7 | OK |
| Data-flow | 6 | 6 | OK |
| Constants | 3 | 3 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 2 | 2 | OK |
| Data writes | 0 | 0 | OK |
| Integrations | 0 | 0 | OK |
| Error paths | 0 | 1 | FLAGGED — target validates rate range |

**Preservation:** FLAGGED — target invariant added

---

### BR-TAX-CAL-010: Consolidate same-code tax items without losing amounts

**Statement:** Tax items with the same tax code are returned as one item whose tax amount equals the sum of all contributing items. If no applicable rate produces a tax item, the calculation returns an empty tax result rather than a fabricated tax line.

**Intent:** Calculation / Compliance
**Classification:** Core
**Weight:** Critical

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/tax/TaxServiceImpl.java:269-297`

**CAST Reference:** Object `TaxServiceImpl` (`13426`).

**Logic:**
```pseudocode
taxItemsByCode = ordered map

FOR each taxItem:
    IF code not present:
        taxItemsByCode[code] = taxItem
    ELSE:
        taxItemsByCode[code].taxAmount += taxItem.taxAmount

IF taxItemsByCode is empty:
    return empty tax result

return values(taxItemsByCode)
```

**Target correction:** The legacy implementation calculates `amount = amount + taxItem.itemPrice` but does not assign the result back to the retained item. The target writes the aggregate amount to the retained tax item.

**Data Dependencies:**
- Reads `tax_schema.tax_rates.code`
- Reads tax item `tax_amount`
- Returns an in-memory tax result
- Does not write order totals

**Concrete Example:**
- API Input: Two tax-class subtotals both resolve to tax code `CA-GENERAL`, with amounts `5.00` and `3.50`
- Success Output: `200 {"taxItems":[{"taxCode":"CA-GENERAL","taxAmount":8.50}],"totalTaxAmount":8.50}`
- Error Input: No rate matches the selected store/country/geography/class
- Error Output: `200 {"taxItems":[],"totalTaxAmount":0}`

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 5 | 5 | OK |
| Data-flow | 3 | 3 | OK |
| Constants | 0 | 0 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 2 | 2 | OK |
| Data writes | 1 | 1 | FLAGGED — target writes missing aggregate mutation |
| Integrations | 0 | 0 | OK |
| Error paths | 1 | 1 | OK |

**Preservation:** FLAGGED — target correction required
