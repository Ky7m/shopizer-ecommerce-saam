# Platform Integrations — Business Rules

**Version:** 2.0  
**Date:** 2026-09-01  
**Service ID:** MS-12  
**Analysis mode:** Hybrid CAST + direct Java source reading

MS-12 owns adapter dispatch, external provider execution, storage-provider execution, and
durable integration delivery. MS-11 owns merchant/module configuration persistence, MS-09 owns
shipping policy and quote persistence, MS-06 owns payment execution, and MS-02 owns product/media
metadata. The source-derived rules below describe behavior found in the targeted Java files.
BR-INT-MS12-022 and BR-INT-MS12-023 are target-architecture reliability rules; the source read
found no durable operation, outbox, retry, or replay store.

## Rule Index

| ID | Name | Classification |
|---|---|---|
| BR-INT-MS12-001 | Category-scoped adapter projection | Source-derived |
| BR-INT-MS12-002 | Atomic active adapter replacement | Source-derived plus target integrity |
| BR-INT-MS12-003 | Environment-specific endpoint projection | Source-derived |
| BR-INT-MS12-004 | Independent supplemental settings | Source-derived defect finding |
| BR-INT-MS12-005 | UPS configuration validation | Source-derived |
| BR-INT-MS12-006 | USPS configuration validation | Source-derived |
| BR-INT-MS12-007 | UPS eligibility and endpoint selection | Source-derived |
| BR-INT-MS12-008 | UPS request construction | Source-derived |
| BR-INT-MS12-009 | UPS response normalization | Source-derived |
| BR-INT-MS12-010 | USPS route and package normalization | Source-derived |
| BR-INT-MS12-011 | USPS response normalization | Source-derived |
| BR-INT-MS12-012 | Distance eligibility and enrichment | Source-derived |
| BR-INT-MS12-013 | IP location lookup | Source-derived |
| BR-INT-MS12-014 | Configured email sender dispatch | Source-derived |
| BR-INT-MS12-015 | SMTP and SES message rendering | Source-derived |
| BR-INT-MS12-016 | Order confirmation projection | Source-derived |
| BR-INT-MS12-017 | Account and operational notification projection | Source-derived |
| BR-INT-MS12-018 | Provider-neutral storage addressing | Source-derived |
| BR-INT-MS12-019 | Storage file operation semantics | Source-derived |
| BR-INT-MS12-020 | Storage folder capability handling | Source-derived |
| BR-INT-MS12-021 | Storage operation idempotency association | Target architecture, source-faithful reliability |
| BR-INT-MS12-022 | Durable delivery retry policy | Target-only reliability |
| BR-INT-MS12-023 | Outbox, replay, and dead-letter handling | Target-only reliability |

## Adapter registry and loading

### BR-INT-MS12-001: Category-scoped adapter projection

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/system/ModuleConfigurationServiceImpl.java:ModuleConfigurationServiceImpl.getIntegrationModules:62-160`  
**Discovery Method:** Hybrid (CAST transaction path + Direct Source Read)  
**CAST Reference:** Object `13381`, complexity `17`; transaction `244013`.

**Statement:** Adapter discovery returns the definitions for the requested integration category, reusing that category’s cached projection and loading the repository only when the projection is absent.
**Intent:** Routing
**Classification:** Core
**Weight:** High

**Logic:**
```pseudocode
cacheKey = "INTEGRATION_M" + module
modules = cache.getFromCache(cacheKey)
IF modules == null:
    modules = moduleConfigurationRepository.findByModule(module)
    FOR each mod:
        parse mod.regions JSON into mod.regionsSet
        parse mod.configDetails JSON into mod.details
        parse mod.configuration JSON into mod.moduleConfigs keyed by env
    IF payments is not null:
        append one IntegrationModule for each ModuleStarter
    cache.putInCache(modules, cacheKey)
RETURN modules
```

**Data Dependencies:**
- Reads: `MODULE_CONFIGURATION.module`, `MODULE_CONFIGURATION.regions`, `MODULE_CONFIGURATION.config_details`, `MODULE_CONFIGURATION.configuration`, `MODULE_CONFIGURATION.code`
- Writes: integration-category cache projection

**Side Effects:** Reads MS-11’s configuration repository on a cache miss and writes the category projection to the cache. No merchant configuration is changed.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 7 | 7 | OK |
| Data-flow | 8 | 8 | OK |
| Constants | 1 | 1 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 2 | 2 | OK |
| Data writes | 1 | 1 | OK |
| Integrations | 3 | 3 | OK |
| Error paths | 1 | 1 | OK |

**Preservation:** OK

**Concrete Example:**
- **API Input:** `GET /api/v1/integrations/adapters?moduleType=Shipping&page=1&pageSize=20`
- **Success:** `200 {"items":[{"endpointId":"4fd4f7f6-0f9a-4903-965e-4bb4cd1d9e85","integrationType":"Shipping","provider":"UPS","code":"ups","environment":"PROD","status":"Active","configurationRef":"ms11://module/ups/prod","capabilities":{"rating":true},"timeoutMs":10000,"maxAttempts":3}],"pagination":{"page":1,"pageSize":20,"totalItems":1,"totalPages":1}}`
- **Error Input:** `GET /api/v1/integrations/adapters?moduleType=Shipping&environment=QA&page=1&pageSize=20`
- **Error Output:** `500 {"error":"INTERNAL_ERROR","message":"Adapter projection could not be loaded","statusCode":500,"timestamp":"2026-09-01T17:49:37Z"}`

### BR-INT-MS12-002: Atomic active adapter replacement

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/system/ModuleConfigurationServiceImpl.java:ModuleConfigurationServiceImpl.createOrUpdateModule:166-182`; `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v0/system/SystemRESTController.java:SystemRESTController.createOrUpdateModule:58-84`  
**Discovery Method:** Hybrid (CAST transaction path + Direct Source Read)  
**CAST Reference:** Object `13382`; transaction `244013`.

**Statement:** A tenant and store can have only one active adapter projection for a category, code, and environment, and replacing it publishes either the complete new projection or leaves the previous projection active.
**Intent:** State Transition
**Classification:** Core
**Weight:** High

**Logic:**
```pseudocode
module = integrationModulesLoader.loadModule(mapper.readValue(json, Map.class))
IF module != null:
    existing = getByCode(module.code)
    replace existing and module in one target transaction
    invalidate the affected category cache
IF module == null OR parsing/replacement fails:
    do not publish a partial projection
```

**Data Dependencies:**
- Reads: `MODULE_CONFIGURATION.code`, `MODULE_CONFIGURATION.module`, `MODULE_CONFIGURATION.configuration`
- Writes: target `integration_endpoint` replacement and category cache invalidation

**Side Effects:** A successful replacement retires the previous projection. The legacy implementation deletes before creating; the target transaction closes that failure window.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 4 | 5 | FLAGGED |
| Data-flow | 3 | 4 | FLAGGED |
| Constants | 0 | 0 | OK |
| State transitions | 2 | 3 | FLAGGED |
| Outcomes | 3 | 3 | OK |
| Data writes | 2 | 3 | FLAGGED |
| Integrations | 2 | 2 | OK |
| Error paths | 1 | 2 | FLAGGED |

**Preservation:** FLAGGED — the source uses delete-then-create; atomic replacement is a justified target reliability correction.

**Concrete Example:**
- **API Input:** `POST /api/v1/integrations/adapters/refresh` `{"moduleType":"Shipping","code":"ups","provider":"UPS","environment":"PROD","configurationRef":"ms11://module/ups/prod","capabilities":{"rating":true},"timeoutMs":10000,"maxAttempts":3}`
- **Success:** `200 {"endpointId":"4fd4f7f6-0f9a-4903-965e-4bb4cd1d9e85","integrationType":"Shipping","provider":"UPS","code":"ups","environment":"PROD","status":"Active","capabilities":{"rating":true},"configurationRef":"ms11://module/ups/prod","timeoutMs":10000,"maxAttempts":3}`
- **Error Input:** `POST /api/v1/integrations/adapters/refresh` `{"moduleType":"Shipping","code":"ups","provider":"UPS","environment":"PROD","configurationRef":"ms11://module/ups/prod","capabilities":{"rating":true},"timeoutMs":10000,"maxAttempts":3}` while another replacement holds the adapter version.
- **Error Output:** `409 {"error":"ADAPTER_UPDATE_CONFLICT","message":"Adapter 'ups' was modified concurrently","statusCode":409,"timestamp":"2026-09-01T17:49:37Z"}`

### BR-INT-MS12-003: Environment-specific endpoint projection

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/reference/loader/IntegrationModulesLoader.java:IntegrationModulesLoader.loadModule:25-185`; `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/system/ModuleConfigurationServiceImpl.java:ModuleConfigurationServiceImpl.getIntegrationModules:88-133`  
**Discovery Method:** Hybrid (CAST object path + Direct Source Read)  
**CAST Reference:** Object `13191`, complexity `17`; transaction `244013`.

**Statement:** Each adapter environment retains its own protocol, host, port, URI, and supplemental settings, and an operation uses only the projection for its requested environment.
**Intent:** Routing
**Classification:** Active
**Weight:** Medium

**Logic:**
```pseudocode
FOR each values in object["configuration"]:
    env = values["env"]
    config.scheme = values["scheme"]
    config.host = values["host"]
    config.port = values["port"]
    config.uri = values["uri"]
    config.config1 = values["config1"] when present
    config.config2 = values["config2"] when present
    moduleConfigs.put(env, config)
selected = moduleConfigs.get(configuration.environment)
providerUri = selected.scheme + "://" + selected.host + ":" + selected.port + selected.uri
```

**Data Dependencies:**
- Reads: `MODULE_CONFIGURATION.configuration`, `ModuleConfig.env`, `ModuleConfig.scheme`, `ModuleConfig.host`, `ModuleConfig.port`, `ModuleConfig.uri`, `ModuleConfig.config1`, `ModuleConfig.config2`
- Writes: `integration_endpoint.environment`, `integration_endpoint.endpoint_uri`

**Side Effects:** The selected environment controls the external endpoint; no external call is made until a complete environment projection exists.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 8 | 8 | OK |
| Data-flow | 10 | 10 | OK |
| Constants | 0 | 0 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 1 | 1 | OK |
| Integrations | 2 | 2 | OK |
| Error paths | 2 | 2 | OK |

**Preservation:** OK

**Concrete Example:**
- **API Input:** `POST /api/v1/integrations/adapters/refresh` `{"moduleType":"Shipping","code":"ups","provider":"UPS","environment":"PROD","configurationRef":"ms11://module/ups/prod","resolvedEndpointUri":"https://onlinetools.ups.com:443/ups.app/xml/Rate","capabilities":{"rating":true},"timeoutMs":10000,"maxAttempts":3}`
- **Success:** `200 {"endpointId":"4fd4f7f6-0f9a-4903-965e-4bb4cd1d9e85","integrationType":"Shipping","provider":"UPS","code":"ups","environment":"PROD","status":"Active","capabilities":{"rating":true},"configurationRef":"ms11://module/ups/prod","endpointUri":"https://onlinetools.ups.com:443/ups.app/xml/Rate","timeoutMs":10000,"maxAttempts":3}`
- **Error Input:** `POST /api/v1/integrations/adapters/refresh` `{"moduleType":"Shipping","code":"ups","provider":"UPS","environment":"STAGE","configurationRef":"","capabilities":{"rating":true},"timeoutMs":10000,"maxAttempts":3}`
- **Error Output:** `422 {"error":"ADAPTER_CONFIGURATION_INVALID","message":"No complete endpoint projection exists for environment 'STAGE'","statusCode":422,"timestamp":"2026-09-01T17:49:37Z"}`

### BR-INT-MS12-004: Independent supplemental settings

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/reference/loader/IntegrationModulesLoader.java:IntegrationModulesLoader.loadModule:136-145`; `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/system/ModuleConfigurationServiceImpl.java:ModuleConfigurationServiceImpl.getIntegrationModules:111-120`  
**Discovery Method:** Hybrid (CAST object path + Direct Source Read)  
**CAST Reference:** Object `13381`, complexity `17`.

**Statement:** Two supplemental adapter settings remain independently addressable, so supplying a second setting cannot replace the first setting.
**Intent:** Validation
**Classification:** Active
**Weight:** Medium

**Logic:**
```pseudocode
IF values["config1"] != null: config.setConfig1(values["config1"])
IF values["config2"] != null: config.setConfig2(values["config2"])
SOURCE DEFECT in ModuleConfigurationServiceImpl:
    the second branch calls config.setConfig1(values["config2"])
TARGET:
    retain config1 and config2 as separate projection values
```

**Data Dependencies:**
- Reads: `MODULE_CONFIGURATION.configuration`, `ModuleConfig.config1`, `ModuleConfig.config2`
- Writes: `integration_endpoint.supplemental_configuration`

**Side Effects:** A projection containing both settings is rejected if the values cannot be represented distinctly.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 3 | 3 | OK |
| Data-flow | 4 | 4 | OK |
| Constants | 0 | 0 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 2 | 3 | FLAGGED |
| Data writes | 2 | 2 | OK |
| Integrations | 0 | 0 | OK |
| Error paths | 0 | 1 | OK |

**Preservation:** FLAGGED — direct reading found the `config2`-to-`config1` corruption defect; target behavior corrects it.

**Concrete Example:**
- **API Input:** `POST /api/v1/integrations/adapters/refresh` `{"moduleType":"Storage","code":"s3","provider":"S3","environment":"PROD","configurationRef":"ms11://module/s3/prod","capabilities":{"read":true},"config1":"bucket=shopizer-content","config2":"prefix=tenant","timeoutMs":10000,"maxAttempts":3}`
- **Success:** `200 {"endpointId":"c7b6b2f4-0a58-4f43-b7d6-9bd8c3f0a211","integrationType":"Storage","provider":"S3","code":"s3","environment":"PROD","status":"Active","capabilities":{"read":true},"configurationRef":"ms11://module/s3/prod","supplementalConfiguration":{"config1":"bucket=shopizer-content","config2":"prefix=tenant"},"timeoutMs":10000,"maxAttempts":3}`
- **Error Input:** `POST /api/v1/integrations/adapters/refresh` `{"moduleType":"Storage","code":"s3","provider":"S3","environment":"PROD","configurationRef":"ms11://module/s3/prod","capabilities":{"read":true},"config1":"bucket=shopizer-content","config2":"bucket=other-bucket","timeoutMs":10000,"maxAttempts":3}` with both supplemental values mapped to one target key.
- **Error Output:** `422 {"error":"ADAPTER_CONFIGURATION_INVALID","message":"Supplemental settings must remain distinct","statusCode":422,"timestamp":"2026-09-01T17:49:37Z"}`

## Carrier adapters

### BR-INT-MS12-005: UPS configuration validation

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/shipping/impl/UPSShippingQuote.java:UPSShippingQuote.validateModuleConfiguration:56-119`  
**Discovery Method:** Hybrid (CAST transaction path + Direct Source Read)  
**CAST Reference:** Object `29070`, complexity `35`; transactions `244101`, `244102`.

**Statement:** UPS quoting is available only when the access key, user identifier, password, and at least one package type are configured.
**Intent:** Validation
**Classification:** Core
**Weight:** High

**Logic:**
```pseudocode
keys = integrationConfiguration.getIntegrationKeys()
options = integrationConfiguration.getIntegrationOptions()
missing = []
IF keys == null OR blank(keys["accessKey"]): append "accessKey"
IF keys == null OR blank(keys["userId"]): append "userId"
IF keys == null OR blank(keys["password"]): append "password"
IF options == null OR options["packages"] == null OR options["packages"].size == 0:
    append "packages"
IF missing is not empty: throw IntegrationException(ERROR_VALIDATION_SAVE, missing)
```

**Data Dependencies:**
- Reads: `IntegrationConfiguration.integrationKeys.accessKey`, `.userId`, `.password`, `.integrationOptions.packages`
- Writes: adapter activation decision

**Side Effects:** No UPS provider call is attempted when validation fails.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 9 | 9 | OK |
| Data-flow | 6 | 6 | OK |
| Constants | 1 | 1 | OK |
| State transitions | 1 | 1 | OK |
| Outcomes | 2 | 2 | OK |
| Data writes | 1 | 1 | OK |
| Integrations | 1 | 1 | OK |
| Error paths | 4 | 4 | OK |

**Preservation:** OK

**Concrete Example:**
- **API Input:** `POST /api/v1/integrations/adapters/refresh` `{"moduleType":"Shipping","code":"ups","provider":"UPS","environment":"PROD","configurationRef":"ms11://module/ups/prod","capabilities":{"rating":true},"credentials":{"accessKey":"AK-9041","userId":"shopizer-prod","password":"redacted"},"packageTypes":["02"],"timeoutMs":10000,"maxAttempts":3}`
- **Success:** `200 {"endpointId":"4fd4f7f6-0f9a-4903-965e-4bb4cd1d9e85","integrationType":"Shipping","provider":"UPS","code":"ups","environment":"PROD","status":"Active","capabilities":{"rating":true},"configurationRef":"ms11://module/ups/prod","timeoutMs":10000,"maxAttempts":3}`
- **Error Input:** `POST /api/v1/integrations/adapters/refresh` `{"moduleType":"Shipping","code":"ups","provider":"UPS","environment":"PROD","configurationRef":"ms11://module/ups/prod","capabilities":{"rating":true},"credentials":{"accessKey":"AK-9041","userId":"","password":"redacted"},"packageTypes":[],"timeoutMs":10000,"maxAttempts":3}`.
- **Error Output:** `422 {"error":"ADAPTER_CONFIGURATION_INVALID","message":"UPS requires userId and at least one package type","statusCode":422,"timestamp":"2026-09-01T17:49:37Z"}`

### BR-INT-MS12-006: USPS configuration validation

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/shipping/impl/USPSShippingQuote.java:USPSShippingQuote.validateModuleConfiguration:70-123`  
**Discovery Method:** Hybrid (CAST transaction path + Direct Source Read)  
**CAST Reference:** Object `29071`, complexity `34`; transactions `244101`, `244102`.

**Statement:** USPS quoting is available only when the account identifier and at least one package or mail type are configured.
**Intent:** Validation
**Classification:** Core
**Weight:** High

**Logic:**
```pseudocode
keys = integrationConfiguration.getIntegrationKeys()
options = integrationConfiguration.getIntegrationOptions()
missing = []
IF keys == null OR blank(keys["account"]): append "account"
IF options == null OR options["packages"] == null OR options["packages"].size == 0:
    append "packages"
IF missing is not empty: throw IntegrationException(ERROR_VALIDATION_SAVE, missing)
```

**Data Dependencies:**
- Reads: `IntegrationConfiguration.integrationKeys.account`, `.integrationOptions.packages`
- Writes: adapter activation decision

**Side Effects:** No USPS provider call is attempted when validation fails.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 7 | 7 | OK |
| Data-flow | 5 | 5 | OK |
| Constants | 1 | 1 | OK |
| State transitions | 1 | 1 | OK |
| Outcomes | 2 | 2 | OK |
| Data writes | 1 | 1 | OK |
| Integrations | 1 | 1 | OK |
| Error paths | 3 | 3 | OK |

**Preservation:** OK

**Concrete Example:**
- **API Input:** `POST /api/v1/integrations/adapters/refresh` `{"moduleType":"Shipping","code":"usps","provider":"USPS","environment":"PROD","configurationRef":"ms11://module/usps/prod","capabilities":{"rating":true},"credentials":{"account":"USPS-7712"},"packageTypes":["Package"],"timeoutMs":10000,"maxAttempts":3}`
- **Success:** `200 {"endpointId":"7fd4f7f6-0f9a-4903-965e-4bb4cd1d9e85","integrationType":"Shipping","provider":"USPS","code":"usps","environment":"PROD","status":"Active","capabilities":{"rating":true},"configurationRef":"ms11://module/usps/prod","timeoutMs":10000,"maxAttempts":3}`
- **Error Input:** `POST /api/v1/integrations/adapters/refresh` `{"moduleType":"Shipping","code":"usps","provider":"USPS","environment":"PROD","configurationRef":"ms11://module/usps/prod","capabilities":{"rating":true},"credentials":{"account":""},"packageTypes":[],"timeoutMs":10000,"maxAttempts":3}`.
- **Error Output:** `422 {"error":"ADAPTER_CONFIGURATION_INVALID","message":"USPS requires account and at least one package type","statusCode":422,"timestamp":"2026-09-01T17:49:37Z"}`

### BR-INT-MS12-007: UPS eligibility and endpoint selection

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/shipping/impl/UPSShippingQuote.java:UPSShippingQuote.getShippingQuotes:122-233`  
**Discovery Method:** Hybrid (CAST transaction path + Direct Source Read)  
**CAST Reference:** Object `29070`, complexity `35`; transactions `244101`, `244102`.

**Statement:** UPS quoting is offered only for a US or Canadian destination with a postal code, a store country accepted by the adapter, and a configured endpoint for the requested environment.
**Intent:** Routing
**Classification:** Active
**Weight:** Medium

**Logic:**
```pseudocode
IF configuration == null: reject
IF blank(delivery.getPostalCode()): return null
IF packages == null: return null
IF delivery.country.isoCode is not "US" and not "CA": return null
IF module.getRegionsSet() does not contain store.country.isoCode: throw IntegrationException
FOR each moduleConfig:
    IF moduleConfig.env == configuration.environment:
        select scheme, host, port, uri
```

**Data Dependencies:**
- Reads: `Delivery.postalCode`, `Delivery.country.isoCode`, `MerchantStore.country.isoCode`, `IntegrationConfiguration.environment`, `IntegrationModule.regions`, `IntegrationModule.configuration`
- Writes: none

**Side Effects:** Unsupported destinations return no quote and do not call UPS. A store-region or endpoint failure is surfaced as an integration failure.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 11 | 11 | OK |
| Data-flow | 9 | 9 | OK |
| Constants | 5 | 5 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 4 | 4 | OK |
| Data writes | 0 | 0 | OK |
| Integrations | 2 | 2 | OK |
| Error paths | 3 | 3 | OK |

**Preservation:** OK

**Concrete Example:**
- **API Input:** `POST /api/v1/integrations/carrier-quotes/ups` `{"environment":"PROD","origin":{"city":"Montreal","zoneCode":"QC","countryCode":"CA","postalCode":"H2Y1C6"},"destination":{"city":"Los Angeles","zoneCode":"CA","countryCode":"US","postalCode":"90210"},"packages":[{"weight":2.4,"weightUnit":"KG","length":30,"width":20,"height":10,"dimensionUnit":"CM"}]}`
- **Success:** `200 {"provider":"UPS","requestType":"Rate","options":[{"provider":"UPS","code":"03","name":"UPS Ground","price":18.75,"currency":"USD","estimatedDays":"3"}]}`
- **Error Input:** `POST /api/v1/integrations/carrier-quotes/ups` `{"environment":"PROD","origin":{"city":"Montreal","zoneCode":"QC","countryCode":"CA","postalCode":"H2Y1C6"},"destination":{"city":"Berlin","zoneCode":"BE","countryCode":"DE","postalCode":"10115"},"packages":[{"weight":2.4,"weightUnit":"KG","length":30,"width":20,"height":10,"dimensionUnit":"CM"}]}`.
- **Error Output:** `200 {"provider":"UPS","requestType":"Suppressed","options":[],"suppressedReason":"DESTINATION_NOT_SUPPORTED"}`

### BR-INT-MS12-008: UPS request construction

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/shipping/impl/UPSShippingQuote.java:UPSShippingQuote.getShippingQuotes:234-369`  
**Discovery Method:** Hybrid (CAST object path + Direct Source Read)  
**CAST Reference:** Object `29070`, complexity `35`.

**Statement:** A UPS rating request carries authenticated origin and destination addresses, one provider package element for each package, a configured package type, provider weight units, and dimensions rounded to the provider precision.
**Intent:** Calculation
**Classification:** Active
**Weight:** Medium

**Logic:**
```pseudocode
weightCode = "KGS" when store.weightunitcode == "KG", otherwise "LBS"
append AccessLicenseNumber(accessKey), UserId(userId), Password(password)
append shipper city, zone code when present, country ISO, trimPostalCode(store.storepostalcode)
append recipient city, zone code when present, country ISO, trimPostalCode(delivery.postalCode)
FOR each packageDetail:
    weight = BigDecimal(shippingWeight).setScale(1, ROUND_HALF_UP)
    length = BigDecimal(shippingLength).setScale(2, ROUND_HALF_UP)
    width = BigDecimal(shippingWidth).setScale(2, ROUND_HALF_UP)
    height = BigDecimal(shippingHeight).setScale(2, ROUND_HALF_UP)
    append Package(pack, weightCode, weight, measureCode, length, width, height)
POST XML to protocol + "://" + host + ":" + port + uri
```

**Data Dependencies:**
- Reads: `MerchantStore.weightunitcode`, `.storecity`, `.storepostalcode`, `.zone.code`, `.country.isoCode`, `Delivery.city`, `.postalCode`, `.zone.code`, `.country.isoCode`, `PackageDetails.shippingWeight`, `.shippingLength`, `.shippingWidth`, `.shippingHeight`
- Writes: none

**Side Effects:** Sends one XML request to UPS; quote persistence remains with MS-09.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 8 | 8 | OK |
| Data-flow | 18 | 18 | OK |
| Constants | 7 | 7 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 2 | 2 | OK |
| Data writes | 0 | 0 | OK |
| Integrations | 2 | 2 | OK |
| Error paths | 2 | 2 | OK |

**Preservation:** OK

**Concrete Example:**
- **API Input:** `POST /api/v1/integrations/carrier-quotes/ups` `{"environment":"PROD","origin":{"city":"Montreal","zoneCode":"QC","countryCode":"CA","postalCode":"H2Y1C6"},"destination":{"city":"Los Angeles","zoneCode":"CA","countryCode":"US","postalCode":"90210"},"packages":[{"weight":2.46,"weightUnit":"KG","length":30.125,"width":20.126,"height":10.129,"dimensionUnit":"CM"}]}`
- **Success:** `200 {"provider":"UPS","requestType":"Rate","options":[{"provider":"UPS","code":"03","name":"UPS Ground","price":18.75,"currency":"USD","estimatedDays":"3"}]}`
- **Error Input:** `POST /api/v1/integrations/carrier-quotes/ups` with the same contract-valid package and address fields as the success case, but no `PROD` UPS endpoint is configured for the selected store.
- **Error Output:** `502 {"error":"CARRIER_PROVIDER_ERROR","message":"UPS request could not be constructed","statusCode":502,"timestamp":"2026-09-01T17:49:37Z"}`

### BR-INT-MS12-009: UPS response normalization

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/shipping/impl/UPSShippingQuote.java:UPSShippingQuote.getShippingQuotes:370-617`  
**Discovery Method:** Hybrid (CAST object path + Direct Source Read)  
**CAST Reference:** Object `29070`, complexity `35`.

**Statement:** Each successful UPS rated shipment becomes a normalized carrier option with its service code, configured display name, monetary price, and estimated delivery days; provider errors and empty results are failures.
**Intent:** Calculation
**Classification:** Active
**Weight:** Medium

**Logic:**
```pseudocode
parse RatingServiceSelectionResponse:
    Error/ErrorDescription -> parsed.error
    ResponseStatusCode -> parsed.statusCode
    RatedShipment/Service/Code -> option.optionId and option.optionCode
    TotalCharges/MonetaryValue -> option.optionPriceText
    GuaranteedDaysToDelivery -> option.estimatedNumberOfDays
IF parsed.errorCode is not blank: throw IntegrationException(parsed.error)
IF parsed.statusCode is not blank AND parsed.statusCode != "1": throw IntegrationException(parsed.error)
IF parsed.options is null OR empty: throw IntegrationException("No shipping options available")
FOR each option:
    option.name = module.details[option.optionCode]
    IF option.price is null: option.price = new BigDecimal(option.optionPriceText)
RETURN options
```

**Data Dependencies:**
- Reads: `IntegrationModule.details`, provider response `RatedShipment.Service.Code`, `.TotalCharges.MonetaryValue`, `.GuaranteedDaysToDelivery`
- Writes: normalized quote option values in the response

**Side Effects:** Records provider outcome through the target delivery attempt; does not save an MS-09 shipping quote.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 13 | 13 | OK |
| Data-flow | 9 | 9 | OK |
| Constants | 1 | 1 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 5 | 5 | OK |
| Data writes | 0 | 0 | OK |
| Integrations | 2 | 2 | OK |
| Error paths | 4 | 4 | OK |

**Preservation:** OK

**Concrete Example:**
- **API Input:** `POST /api/v1/integrations/carrier-quotes/ups` `{"environment":"PROD","origin":{"city":"Montreal","zoneCode":"QC","countryCode":"CA","postalCode":"H2Y1C6"},"destination":{"city":"Los Angeles","zoneCode":"CA","countryCode":"US","postalCode":"90210"},"packages":[{"weight":2.46,"weightUnit":"KG","length":30.125,"width":20.126,"height":10.129,"dimensionUnit":"CM"}]}`
- **Success:** `200 {"provider":"UPS","requestType":"Rate","options":[{"provider":"UPS","code":"03","name":"UPS Ground","price":18.75,"currency":"USD","estimatedDays":"3"}]}`
- **Error Input:** `POST /api/v1/integrations/carrier-quotes/ups` `{"environment":"PROD","origin":{"city":"Montreal","zoneCode":"QC","countryCode":"CA","postalCode":"H2Y1C6"},"destination":{"city":"Los Angeles","zoneCode":"CA","countryCode":"US","postalCode":"90210"},"packages":[{"weight":2.46,"weightUnit":"KG","length":30.125,"width":20.126,"height":10.129,"dimensionUnit":"CM"}]}` when the provider returns XML error description `"Invalid postal code"`.
- **Error Output:** `502 {"error":"CARRIER_PROVIDER_ERROR","message":"UPS provider rejected the rating request: Invalid postal code","statusCode":502,"timestamp":"2026-09-01T17:49:37Z"}`

### BR-INT-MS12-010: USPS route and package normalization

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/shipping/impl/USPSShippingQuote.java:USPSShippingQuote.getShippingQuotes:126-370`  
**Discovery Method:** Hybrid (CAST transaction path + Direct Source Read)  
**CAST Reference:** Object `29071`, complexity `34`; transactions `244101`, `244102`.

**Statement:** USPS uses domestic rating for a US-origin shipment whose destination is also US and international rating otherwise; every package is converted to inches and pounds, and the aggregate length-plus-girth selects Regular, Large, or Oversize.
**Intent:** Routing
**Classification:** Active
**Weight:** Medium

**Logic:**
```pseudocode
require packages != null and delivery.postalCode not blank
require store.country.isoCode == "US"
domestic = store.country.isoCode == delivery.country.isoCode
FOR each detail:
    w = getMeasure(shippingWidth, store, "IN")
    h = getMeasure(shippingHeight, store, "IN")
    l = getMeasure(shippingLength, store, "IN")
    totalL += l; totalW += w; totalH += h
    totalG += l + (w * 2) + (h * 2)
    totalP += getWeight(shippingWeight, store, "LB")
size = "REGULAR" when totalL + totalG <= 64
     else "LARGE" when totalL + totalG <= 108
     else "OVERSIZE"
shipDate = today + 3 calendar days, formatted DEFAULT_DATE_FORMAT
IF domestic: build RateV3Request with ZipOrigination, ZipDestination, Pounds, Ounces, Container, Size, Machinable=true, ShipDate
ELSE: build IntlRateRequest with Pounds, Ounces, MailType, ValueOfContents, Country
```

**Data Dependencies:**
- Reads: `MerchantStore.country.isoCode`, `.storepostalcode`, `.defaultLanguage`, `Delivery.country.isoCode`, `.postalCode`, `PackageDetails.shippingWidth`, `.shippingHeight`, `.shippingLength`, `.shippingWeight`, `orderTotal`
- Writes: none

**Side Effects:** Sends either a domestic `RateV3` request or an international `IntlRate` request to the selected USPS endpoint.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 17 | 17 | OK |
| Data-flow | 17 | 17 | OK |
| Constants | 9 | 9 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 4 | 4 | OK |
| Data writes | 0 | 0 | OK |
| Integrations | 2 | 2 | OK |
| Error paths | 3 | 3 | OK |

**Preservation:** OK

**Concrete Example:**
- **API Input:** `POST /api/v1/integrations/carrier-quotes/usps` `{"environment":"PROD","origin":{"city":"Los Angeles","zoneCode":"CA","countryCode":"US","postalCode":"90001"},"destination":{"city":"New York","zoneCode":"NY","countryCode":"US","postalCode":"10001"},"packages":[{"weight":4.2,"weightUnit":"LB","length":20,"width":12,"height":8,"dimensionUnit":"IN"}],"orderTotal":250.00}`
- **Success:** `200 {"provider":"USPS","requestType":"Domestic","packageSize":"REGULAR","options":[{"provider":"USPS","code":"1","name":"Priority Mail","price":14.20,"currency":"USD","estimatedDays":"2"}]}`
- **Error Input:** `POST /api/v1/integrations/carrier-quotes/usps` `{"environment":"PROD","origin":{"city":"Toronto","zoneCode":"ON","countryCode":"CA","postalCode":"M5V2T6"},"destination":{"city":"New York","zoneCode":"NY","countryCode":"US","postalCode":"10001"},"packages":[{"weight":4.2,"weightUnit":"LB","length":20,"width":12,"height":8,"dimensionUnit":"IN"}],"orderTotal":250.00}`.
- **Error Output:** `422 {"error":"ADAPTER_CONFIGURATION_INVALID","message":"USPS requires a US-origin store","statusCode":422,"timestamp":"2026-09-01T17:49:37Z"}`

### BR-INT-MS12-011: USPS response normalization

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/shipping/impl/USPSShippingQuote.java:USPSShippingQuote.getShippingQuotes:371-665`  
**Discovery Method:** Hybrid (CAST object path + Direct Source Read)  
**CAST Reference:** Object `29071`, complexity `34`.

**Statement:** USPS domestic and international response formats are exposed as the same normalized option shape, while provider errors and an empty option collection fail the rating operation.
**Intent:** Calculation
**Classification:** Active
**Weight:** Medium

**Logic:**
```pseudocode
IF domestic:
    RateV3Response/Package/Postage CLASSID -> optionId
    MailService -> optionName and optionCode
    Rate -> optionPriceText
ELSE:
    IntlRateResponse/Package/Service ID -> optionId
    SvcDescription -> optionName and optionCode
    Postage -> optionPriceText
parse Error/Description and branch-specific Package/Error/Description -> parsed.error
IF parsed.error is not blank: throw IntegrationException(parsed.error)
IF parsed.options is null OR empty: throw IntegrationException(parsed.error)
RETURN parsed.options
```

**Data Dependencies:**
- Reads: USPS `RateV3Response.Package.Postage`, `IntlRateResponse.Package.Service`, and error descriptions
- Writes: normalized `CarrierOption` response

**Side Effects:** Records provider outcome through the target delivery attempt; no quote persistence.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 9 | 9 | OK |
| Data-flow | 11 | 11 | OK |
| Constants | 0 | 0 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 4 | 4 | OK |
| Data writes | 0 | 0 | OK |
| Integrations | 2 | 2 | OK |
| Error paths | 3 | 3 | OK |

**Preservation:** OK

**Concrete Example:**
- **API Input:** `POST /api/v1/integrations/carrier-quotes/usps` `{"environment":"PROD","origin":{"city":"Los Angeles","zoneCode":"CA","countryCode":"US","postalCode":"90001"},"destination":{"city":"New York","zoneCode":"NY","countryCode":"US","postalCode":"10001"},"packages":[{"weight":4.2,"weightUnit":"LB","length":20,"width":12,"height":8,"dimensionUnit":"IN"}],"orderTotal":250.00}`
- **Success:** `200 {"provider":"USPS","requestType":"Domestic","packageSize":"REGULAR","options":[{"provider":"USPS","code":"1","name":"Priority Mail","price":14.20,"currency":"USD","estimatedDays":"2"}]}`
- **Error Input:** `POST /api/v1/integrations/carrier-quotes/usps` `{"environment":"PROD","origin":{"city":"Los Angeles","zoneCode":"CA","countryCode":"US","postalCode":"90001"},"destination":{"city":"New York","zoneCode":"NY","countryCode":"US","postalCode":"10001"},"packages":[{"weight":4.2,"weightUnit":"LB","length":20,"width":12,"height":8,"dimensionUnit":"IN"}],"orderTotal":250.00}` when the provider returns `"Invalid Country Name"`.
- **Error Output:** `502 {"error":"CARRIER_PROVIDER_ERROR","message":"USPS provider rejected the rating request: Invalid Country Name","statusCode":502,"timestamp":"2026-09-01T17:49:37Z"}`

## Maps and geolocation

### BR-INT-MS12-012: Distance eligibility and enrichment

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/shipping/impl/ShippingDistancePreProcessorImpl.java:ShippingDistancePreProcessorImpl.prePostProcessShippingQuotes:93-204`  
**Discovery Method:** Hybrid (CAST transaction path + Direct Source Read)  
**CAST Reference:** Object `30319`, complexity `17`; transactions `244101`, `244102`.

**Statement:** Distance enrichment is suppressed when the destination has no configured zone or postal code, or when its zone is not allowed; eligible destinations are geocoded and receive destination coordinates and route distance in kilometers.
**Intent:** Calculation
**Classification:** Active
**Weight:** Medium

**Logic:**
```pseudocode
IF delivery.zone == null: return
zoneAllowed = any allowedZonesCodes value equals delivery.zone.code
IF not zoneAllowed: return
IF blank(delivery.postalCode): return
Validate.notNull(apiKey, "Requires the configuration of google apiKey")
originAddress = origin.address + " " + origin.city + " " + origin.postalCode + " "
                + optional origin.state + " " + optional origin.zone.code + " " + origin.country.isoCode
destinationAddress = delivery.address + optional " " + delivery.city + " "
                     + delivery.postalCode + " " + optional state + " "
                     + optional zone.code + " " + delivery.country.isoCode
originResult = GeocodingApi.geocode(context, originAddress).await()
destinationResult = GeocodingApi.geocode(context, destinationAddress).await()
IF both result arrays have length > 0:
    delivery.latitude = destination location latitude
    delivery.longitude = destination location longitude
    distance = DistanceMatrixApi.newRequest(context).origins(origin).destinations(destination).awaitIgnoreError()
    IF distance != null: quote.informations["distance"] = 0.001 * distance.inMeters
    ELSE log provider response error
```

**Data Dependencies:**
- Reads: `ShippingOrigin.address`, `.city`, `.postalCode`, `.state`, `.zone.code`, `.country.isoCode`, `Delivery.address`, `.city`, `.postalCode`, `.state`, `.zone.code`, `.country.isoCode`, `allowedZonesCodes`, `apiKey`
- Writes: `Delivery.latitude`, `Delivery.longitude`, quote information key `distance`

**Side Effects:** Calls Google Geocoding twice and Distance Matrix once for an eligible address. Exceptions are logged and do not escape the method.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 12 | 12 | OK |
| Data-flow | 17 | 17 | OK |
| Constants | 3 | 3 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 4 | 4 | OK |
| Data writes | 2 | 2 | OK |
| Integrations | 3 | 3 | OK |
| Error paths | 4 | 4 | OK |

**Preservation:** OK

**Concrete Example:**
- **API Input:** `POST /api/v1/integrations/maps/distance` `{"origin":{"address":"100 Main St","city":"Toronto","zoneCode":"ON","countryCode":"CA","postalCode":"M5V2T6"},"destination":{"address":"200 King St","city":"Toronto","zoneCode":"ON","countryCode":"CA","postalCode":"M5H1K5"},"allowedZoneCodes":["ON"]}`
- **Success:** `200 {"enriched":true,"destination":{"latitude":43.6487,"longitude":-79.3854},"distanceKm":2.4}`
- **Error Input:** `POST /api/v1/integrations/maps/distance` `{"origin":{"address":"100 Main St","city":"Toronto","zoneCode":"ON","countryCode":"CA","postalCode":"M5V2T6"},"destination":{"address":"200 King St","city":"Toronto","zoneCode":"ON","countryCode":"CA","postalCode":"M5H1K5"},"allowedZoneCodes":["QC"]}`.
- **Error Output:** `200 {"enriched":false,"suppressedReason":"DESTINATION_ZONE_NOT_ALLOWED"}`

### BR-INT-MS12-013: IP location lookup

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/utils/GeoLocationImpl.java:GeoLocationImpl.getAddress:25-58`  
**Discovery Method:** Hybrid (CAST source reachability + Direct Source Read)

**Statement:** An IP address is resolved to coarse country, postal code, subdivision, and city data when the local GeoLite database contains it; an address absent from that database returns an unresolved result.
**Intent:** Calculation
**Classification:** Active
**Weight:** Medium

**Logic:**
```pseudocode
IF reader == null:
    inputFile = classLoader.getResourceAsStream("reference/GeoLite2-City.mmdb")
    TRY reader = new DatabaseReader.Builder(inputFile).build()
    CATCH exception: log "Cannot instantiate IP database"
address = new Address()
TRY:
    response = reader.city(InetAddress.getByName(ipAddress))
    address.country = response.country.isoCode
    address.postalCode = response.postal.code
    address.zone = response.mostSpecificSubdivision.isoCode
    address.city = response.city.name
CATCH AddressNotFoundException: log debug and return empty address
CATCH other exception: throw ServiceException
RETURN address
```

**Data Dependencies:**
- Reads: GeoLite resource `reference/GeoLite2-City.mmdb`, `ipAddress`
- Writes: response location fields

**Side Effects:** Lazily opens the local GeoLite reader. No external geolocation call is made.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 6 | 6 | OK |
| Data-flow | 7 | 7 | OK |
| Constants | 1 | 1 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 4 | 4 | OK |
| Integrations | 1 | 1 | OK |
| Error paths | 3 | 3 | OK |

**Preservation:** OK

**Concrete Example:**
- **API Input:** `POST /api/v1/integrations/geolocation/ip` `{"ipAddress":"8.8.8.8"}`
- **Success:** `200 {"resolved":true,"countryCode":"US","postalCode":"94043","zoneCode":"CA","city":"Mountain View"}`
- **Error Input:** `POST /api/v1/integrations/geolocation/ip` `{"ipAddress":"192.0.2.1"}`
- **Error Output:** `200 {"resolved":false,"countryCode":null,"postalCode":null,"zoneCode":null,"city":null}`

## Email delivery

### BR-INT-MS12-014: Configured email sender dispatch

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/system/EmailServiceImpl.java:EmailServiceImpl.sendHtmlEmail:24-32`; `EmailServiceImpl.getEmailConfiguration:35-48`  
**Discovery Method:** Hybrid (CAST transaction path + Direct Source Read)  
**CAST Reference:** Transactions `244089`, `244090`, `244235`, `244079`, `244245`, `244012`.

**Statement:** Email delivery parses the store’s email configuration and passes that configuration to the sender implementation selected by the runtime application wiring.
**Intent:** Routing
**Classification:** Active
**Weight:** Medium

**Logic:**
```pseudocode
configuration = merchantConfigurationService.getMerchantConfiguration(EMAIL_CONFIG, store)
IF configuration != null:
    TRY emailConfig = ObjectMapper.readValue(configuration.value, EmailConfig.class)
    CATCH exception: throw ServiceException("Cannot parse json string " + value)
sender.setEmailConfig(emailConfig)
sender.send(email)
```

**Data Dependencies:**
- Reads: `MERCHANT_CONFIGURATION.key`, `.value`, `EmailConfig.protocol`, `.host`, `.port`, `.username`, `.password`, `.smtpAuth`, `.starttls`
- Writes: target `integration_endpoint` sender association

**Side Effects:** Calls the selected SMTP or SES sender. The target stores only an opaque configuration reference and secret-free endpoint projection.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 4 | 4 | OK |
| Data-flow | 8 | 8 | OK |
| Constants | 1 | 1 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 2 | 2 | OK |
| Data writes | 0 | 1 | FLAGGED |
| Integrations | 1 | 1 | OK |
| Error paths | 2 | 2 | OK |

**Preservation:** FLAGGED — durable sender association is target architecture, while source configuration remains owned by MS-11.

**Concrete Example:**
- **API Input:** `POST /api/v1/integrations/emails` `{"idempotencyKey":"order-10482-confirmation-v1","templateKey":"order-confirmation","locale":"en-CA","recipientEmail":"maya@example.net","senderEmail":"orders@example.ca","senderName":"Demo Store","subject":"Order 10482","tokenPayload":{"orderNumber":"10482","customerFirstName":"Maya"},"orderReference":"10482"}`
- **Success:** `202 {"messageId":"f4d4f7f6-0f9a-4903-965e-4bb4cd1d9e85","operationId":"1d8a7f0e-c6b5-4b65-a8de-7cb5c8c3d410","endpointId":"4fd4f7f6-0f9a-4903-965e-4bb4cd1d9e85","idempotencyKey":"order-10482-confirmation-v1","templateKey":"order-confirmation","locale":"en-CA","recipientEmail":"maya@example.net","senderEmail":"orders@example.ca","subject":"Order 10482","status":"Queued","queuedAt":"2026-09-01T17:49:37Z"}`
- **Error Input:** The same request with malformed store email configuration.
- **Error Output:** `422 {"error":"EMAIL_CONFIGURATION_INVALID","message":"Store email configuration is not valid JSON","statusCode":422,"timestamp":"2026-09-01T17:49:37Z"}`

### BR-INT-MS12-015: SMTP and SES message rendering

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/email/DefaultEmailSenderImpl.java:DefaultEmailSenderImpl.send:38-167`; `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/email/SESEmailSenderImpl.java:SESEmailSenderImpl.send:37-88`  
**Discovery Method:** Hybrid (CAST reachable sender methods + Direct Source Read)

**Statement:** SMTP delivery sends a UTF-8 text alternative and rendered HTML alternative from the selected template, while SES sends the rendered HTML with its UTF-8 text fallback; template preparation failures prevent provider submission.
**Intent:** Routing
**Classification:** Active
**Weight:** Medium

**Logic:**
```pseudocode
SMTP:
    apply emailConfig protocol, host, port, username, password, smtp.auth, starttls.enable
    load text template templates/email/<tmpl>; process templateTokens into textWriter
    add text/plain DataSource from textWriter UTF-8 bytes
    load HTML template templates/email/<tmpl>; process templateTokens into htmlWriter
    SOURCE DEFECT: HTML DataSource getInputStream returns textWriter bytes
    TARGET: HTML DataSource returns htmlWriter UTF-8 bytes
    send MimeMessage through JavaMailSender
SES:
    Validate.notNull(region, "AWS region is null")
    build AmazonSimpleEmailService client for Regions.valueOf(region.toUpperCase())
    prepare HTML from templates/email/<templateName>
    send UTF-8 HTML and TEXTBODY through client.sendEmail
```

**Data Dependencies:**
- Reads: `Email.from`, `.fromEmail`, `.to`, `.subject`, `.templateName`, `.templateTokens`, `EmailConfig.*`, template resources `templates/email/*`, SES region
- Writes: provider message

**Side Effects:** Calls JavaMail/SMTP or AWS SES. The target records provider outcome on `delivery_attempt`.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 10 | 10 | OK |
| Data-flow | 10 | 10 | OK |
| Constants | 3 | 3 | OK |
| State transitions | 0 | 1 | FLAGGED |
| Outcomes | 4 | 4 | OK |
| Data writes | 0 | 1 | FLAGGED |
| Integrations | 3 | 3 | OK |
| Error paths | 3 | 3 | OK |

**Preservation:** FLAGGED — target delivery state records the asynchronous outcome; source sender has no durable attempt state.

**Concrete Example:**
- **API Input:** `POST /api/v1/integrations/emails` `{"idempotencyKey":"contact-20260901-001","templateKey":"contact","locale":"en-CA","recipientEmail":"store@example.ca","senderEmail":"store@example.ca","senderName":"Demo Store","subject":"Customer contact","tokenPayload":{"contactName":"Maya Chen","contactEmail":"maya@example.net","comment":"Please call me"} }`
- **Success:** `202 {"messageId":"a3d4f7f6-0f9a-4903-965e-4bb4cd1d9e85","operationId":"2d8a7f0e-c6b5-4b65-a8de-7cb5c8c3d410","endpointId":"4fd4f7f6-0f9a-4903-965e-4bb4cd1d9e85","idempotencyKey":"contact-20260901-001","templateKey":"contact","locale":"en-CA","recipientEmail":"store@example.ca","senderEmail":"store@example.ca","subject":"Customer contact","status":"Queued","queuedAt":"2026-09-01T17:49:37Z"}`
- **Error Input:** `POST /api/v1/integrations/emails` `{"idempotencyKey":"contact-20260901-001","templateKey":"contact","locale":"en-CA","recipientEmail":"store@example.ca","senderEmail":"store@example.ca","senderName":"Demo Store","subject":"Customer contact","tokenPayload":{"contactName":"Maya Chen","contactEmail":"maya@example.net","comment":"Please call me"}}` with an unavailable template key.
- **Error Output:** `422 {"error":"EMAIL_CONFIGURATION_INVALID","message":"Email template 'contact' could not be rendered","statusCode":422,"timestamp":"2026-09-01T17:49:37Z"}`

### BR-INT-MS12-016: Order confirmation projection

**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/utils/EmailTemplatesUtils.java:EmailTemplatesUtils.sendOrderEmail:89-259`  
**Discovery Method:** Hybrid (CAST transaction path + Direct Source Read)  
**CAST Reference:** `sendOrderEmail`, complexity `21`; transactions `244089`, `244090`.

**Statement:** An order-confirmation message contains localized billing information, optional delivery information, every product line’s name, SKU, quantity, and displayed price, every order total, payment information, shipping information when present, and the current order status.
**Intent:** Calculation
**Classification:** Core
**Weight:** High

**Logic:**
```pseudocode
load zones = zoneService.getZones(language)
load countries = countryService.getCountriesMap(language)
format billing from order.billing company OR first/last name, address, city, zone/state, country, postalCode
IF order.delivery exists AND delivery.firstName not blank:
    format delivery using the same address branches
IF shippingModuleCode not blank AND shipping is null: shipping = billing
FOR each orderProduct:
    append productName, " - ", sku, localized quantity label, productQuantity,
           pricingService.getDisplayAmount(oneTimeCharge, merchantStore)
FOR each orderTotal:
    append localized total label (tax uses total.text), displayed total value
tokens include billing, product table, payment type, optional shipping, order number/date/status
email.to = toEmail; email.templateName = EMAIL_ORDER_TPL
emailService.sendHtmlEmail(merchantStore, email)
```

**Data Dependencies:**
- Reads: `Order.billing.*`, `.delivery.*`, `.orderProducts.productName`, `.sku`, `.productQuantity`, `.oneTimeCharge`, `.orderTotal.module`, `.orderTotal.text`, `.orderTotal.value`, `.paymentType`, `.shippingModuleCode`, `.status`, `.datePurchased`, `MerchantStore.storename`, `.storeEmailAddress`, localized country/zone/label data
- Writes: rendered email message tokens

**Side Effects:** Calls pricing, country, zone, localization, and email services. Source exceptions are logged by the asynchronous method; target records the failure durably.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 17 | 17 | OK |
| Data-flow | 25 | 25 | OK |
| Constants | 7 | 7 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 2 | 2 | OK |
| Data writes | 1 | 1 | OK |
| Integrations | 5 | 5 | OK |
| Error paths | 1 | 1 | OK |

**Preservation:** OK

**Concrete Example:**
- **API Input:** `POST /api/v1/integrations/emails` `{"idempotencyKey":"order-10482-confirmation-v1","templateKey":"order-confirmation","locale":"en-CA","recipientEmail":"maya@example.net","senderEmail":"orders@example.ca","senderName":"Demo Store","subject":"Order 10482","tokenPayload":{"orderNumber":"10482","customerFirstName":"Maya","billingAddress":"10 Queen St, Toronto, ON M5V2T6","items":[{"productName":"Canvas Tote","sku":"TOTE-01","quantity":2,"displayPrice":"CAD 24.00"}],"orderTotal":"CAD 48.00","paymentMethod":"Credit Card","shippingMethod":"UPS Ground","orderStatus":"CONFIRMED"},"orderReference":"10482"}`
- **Success:** `202 {"messageId":"f4d4f7f6-0f9a-4903-965e-4bb4cd1d9e85","operationId":"1d8a7f0e-c6b5-4b65-a8de-7cb5c8c3d410","endpointId":"4fd4f7f6-0f9a-4903-965e-4bb4cd1d9e85","idempotencyKey":"order-10482-confirmation-v1","templateKey":"order-confirmation","locale":"en-CA","recipientEmail":"maya@example.net","senderEmail":"orders@example.ca","subject":"Order 10482","status":"Queued","queuedAt":"2026-09-01T17:49:37Z"}`
- **Error Input:** `POST /api/v1/integrations/emails` `{"idempotencyKey":"order-10482-confirmation-v1","templateKey":"order-confirmation","locale":"en-CA","recipientEmail":"maya@example.net","senderEmail":"orders@example.ca","senderName":"Demo Store","subject":"Order 10482","tokenPayload":{"orderNumber":"10482","customerFirstName":"Maya"},"orderReference":"10482"}` with an unavailable template.
- **Error Output:** `422 {"error":"EMAIL_CONFIGURATION_INVALID","message":"Email template 'order-confirmation' could not be rendered","statusCode":422,"timestamp":"2026-09-01T17:49:37Z"}`

### BR-INT-MS12-017: Account and operational notification projection

**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/utils/EmailTemplatesUtils.java:EmailTemplatesUtils.sendRegistrationEmail:269-308`; `sendContactEmail:311-349`; `sendUpdateOrderStatusEmail:360-404`; `sendOrderDownloadEmail:415-452`; `changePasswordNotificationEmail:462-496`  
**Discovery Method:** Hybrid (CAST transaction paths + Direct Source Read)  
**CAST Reference:** Transactions `244235`, `244079`, `244245`, `244012`.

**Statement:** Each operational notification selects its recipient, subject, locale, template, and payload by notification type: registration addresses the customer, contact sends to the store address, status uses the latest comment or localized status, downloads use the configured download period, and password-change notices use the change date without exposing credentials.
**Intent:** Routing
**Classification:** Active
**Weight:** Medium

**Logic:**
```pseudocode
registration:
    recipient = customer.emailAddress
    tokens include customer billing first/last name, userName, customer access URL
    SOURCE includes customer.password; TARGET excludes plaintext passwords
contact:
    fromEmail = merchantStore.storeEmailAddress
    recipient = merchantStore.storeEmailAddress
    tokens include contact.name, contact.email, contact.comment, localized subject
order status:
    comments = lastHistory.comments
    IF comments blank: comments = localized current order.status
    recipient = customer.emailAddress
download:
    tokens include MAX_DOWNLOAD_DAYS, order.id, customer URL, store email
password changed:
    tokens include DateUtil.formatLongDate(new Date())
all branches create Email with branch template and call emailService.sendHtmlEmail
```

**Data Dependencies:**
- Reads: `Customer.emailAddress`, `.billing.firstName`, `.billing.lastName`, `.userName`, `.password`, `PersistableCustomer.*`, `ContactForm.name`, `.email`, `.comment`, `.subject`, `Order.id`, `.status`, `OrderStatusHistory.comments`, `.dateAdded`, `ApplicationConstants.MAX_DOWNLOAD_DAYS`, `MerchantStore.storename`, `.storeEmailAddress`
- Writes: rendered notification message

**Side Effects:** Calls email delivery asynchronously in the source; target queues a durable `email_message`. Plaintext passwords are not copied into target payloads.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 14 | 14 | OK |
| Data-flow | 24 | 24 | OK |
| Constants | 1 | 1 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 5 | 5 | OK |
| Data writes | 1 | 1 | OK |
| Integrations | 3 | 3 | OK |
| Error paths | 1 | 1 | OK |

**Preservation:** FLAGGED — target deliberately removes the source password token as a security correction.

**Concrete Example:**
- **API Input:** `POST /api/v1/integrations/emails` `{"idempotencyKey":"password-change-10482-v1","templateKey":"password-changed","locale":"en-CA","recipientEmail":"maya@example.net","senderEmail":"orders@example.ca","senderName":"Demo Store","subject":"Your password changed","tokenPayload":{"customerFirstName":"Maya","changedAt":"September 1, 2026"} }`
- **Success:** `202 {"messageId":"b4d4f7f6-0f9a-4903-965e-4bb4cd1d9e85","operationId":"3d8a7f0e-c6b5-4b65-a8de-7cb5c8c3d410","endpointId":"4fd4f7f6-0f9a-4903-965e-4bb4cd1d9e85","idempotencyKey":"password-change-10482-v1","templateKey":"password-changed","locale":"en-CA","recipientEmail":"maya@example.net","senderEmail":"orders@example.ca","subject":"Your password changed","status":"Queued","queuedAt":"2026-09-01T17:49:37Z"}`
- **Error Input:** `POST /api/v1/integrations/emails` `{"idempotencyKey":"password-change-10482-v1","templateKey":"password-changed","locale":"en-CA","recipientEmail":"","senderEmail":"orders@example.ca","senderName":"Demo Store","subject":"Your password changed","tokenPayload":{"customerFirstName":"Maya","changedAt":"September 1, 2026"}}`.
- **Error Output:** `422 {"error":"EMAIL_CONFIGURATION_INVALID","message":"recipientEmail must be a valid email address","statusCode":422,"timestamp":"2026-09-01T17:49:37Z"}`

## Storage adapters

### BR-INT-MS12-018: Provider-neutral storage addressing

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/cms/content/StaticContentFileManagerImpl.java:StaticContentFileManagerImpl.addFile:25-35`; `addFiles:47-58`; `getFile:72-77`; `getFiles:79-84`; `removeFile:63-68`; `removeFiles:86-89`  
**Discovery Method:** Hybrid (CAST storage transactions + Direct Source Read)  
**CAST Reference:** Transactions `244042`, `244065`, `244066`, `244289`, `244292`, `244293`.

**Statement:** A storage asset is addressed by store, content type, optional folder path, and file name, while the facade delegates that logical address to the selected filesystem, cache, S3, or GCP implementation without taking ownership of content metadata.
**Intent:** Routing
**Classification:** Core
**Weight:** High

**Logic:**
```pseudocode
addFile(storeCode, path, input): uploadFile.addFile(storeCode, path, input)
addFiles(storeCode, path, inputs): uploadFile.addFiles(storeCode, path, inputs)
getFile(storeCode, path, contentType, name): getFile.getFile(storeCode, path, contentType, name)
getFiles(...): getFile.getFiles(...)
removeFile(...): removeFile.removeFile(...)
removeFiles(...): removeFile.removeFiles(...)
```

**Data Dependencies:**
- Reads: `merchantStoreCode`, `Optional.path`, `InputContentFile.fileContentType`, `.fileName`, `FileContentType`, `contentName`
- Writes: selected provider namespace

**Side Effects:** Delegates to exactly one configured provider adapter. Product/media metadata remains outside MS-12.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 0 | 0 | OK |
| Data-flow | 8 | 8 | OK |
| Constants | 0 | 0 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 6 | 6 | OK |
| Data writes | 4 | 4 | OK |
| Integrations | 4 | 4 | OK |
| Error paths | 0 | 0 | OK |

**Preservation:** OK

**Concrete Example:**
- **API Input:** `POST /api/v1/integrations/files` `{"storeCode":"demo-ca","contentType":"Image","fileName":"hero.png","mimeType":"image/png","contentBase64":"iVBORw0KGgo=","idempotencyKey":"asset-hero-v1"}`
- **Success:** `201 {"operationId":"4d8a7f0e-c6b5-4b65-a8de-7cb5c8c3d410","fileName":"hero.png","contentType":"Image","mimeType":"image/png","providerKey":"demo-ca/Image/hero.png","status":"Available","deliveryAttemptId":"5d8a7f0e-c6b5-4b65-a8de-7cb5c8c3d410"}`
- **Error Input:** The same request with `"fileName":"../hero.png"`.
- **Error Output:** `422 {"error":"STORAGE_KEY_INVALID","message":"fileName must not contain path traversal or path separators","statusCode":422,"timestamp":"2026-09-01T17:49:37Z"}`

### BR-INT-MS12-019: Storage file operation semantics

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/cms/content/local/CmsStaticContentFileManagerImpl.java:1-484`; `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/cms/content/infinispan/CmsStaticContentFileManagerImpl.java:1-477`; `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/cms/content/aws/S3StaticContentAssetsManagerImpl.java:1-322`; `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/cms/content/gcp/GCPStaticContentAssetsManagerImpl.java:1-224`  
**Discovery Method:** Hybrid (CAST storage transactions + Direct Source Read)

**Statement:** Upload writes the asset at its logical key, supported reads return the asset bytes with metadata, name listings expose recognized file types at the selected namespace, deletion targets one asset or a store namespace, and provider failures are surfaced rather than reported as success.
**Intent:** Routing
**Classification:** Active
**Weight:** Medium

**Logic:**
```pseudocode
LOCAL:
    copy input file to root/files/store/contentType/fileName with REPLACE_EXISTING
    getFile and getFiles throw ServiceException("Not implemented for httpd image manager")
    getFileNames lists directory entries and filters IMAGE entries by URLConnection MIME family "image"
    removeFile Files.deleteIfExists(root/files/store/contentType/fileName)
    removeFiles Files.deleteIfExists(root/files/store)
INFINISPAN:
    put IOUtils.toByteArray(input.file) under rootName + store + contentType + fileName
    getFile reads byte[] and returns OutputContentFile with ByteArrayOutputStream and MIME
    getFiles copies every cached byte[] to an OutputContentFile
    getFileNames returns node keys; removeFile removes key; removeFiles removes store node
S3/GCP:
    read object/blob bytes; list by store/contentType prefix; filter direct names by MIME
    upload object/blob at the same key; delete object/blob at the same key or store prefix
```

**Data Dependencies:**
- Reads: `InputContentFile.file`, `.fileName`, `.mimeType`, `.fileContentType`, `OutputContentFile.file`, `.mimeType`, `.fileName`, provider object/blob/cache keys
- Writes: filesystem paths, Infinispan node values, S3 objects, GCP blobs

**Side Effects:** Calls filesystem, Infinispan, S3, or GCP APIs. Local read operations remain an explicit unsupported capability; supported provider reads return bytes.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 24 | 24 | OK |
| Data-flow | 28 | 28 | OK |
| Constants | 12 | 12 | OK |
| State transitions | 0 | 1 | FLAGGED |
| Outcomes | 8 | 8 | OK |
| Data writes | 12 | 12 | OK |
| Integrations | 8 | 8 | OK |
| Error paths | 8 | 8 | OK |

**Preservation:** FLAGGED — the target makes unsupported local reads explicit with `501` and records operation state.

**Concrete Example:**
- **API Input:** `GET /api/v1/integrations/files/hero.png?storeCode=demo-ca&contentType=Image`
- **Success:** `200 {"fileName":"hero.png","contentType":"Image","mimeType":"image/png","providerKey":"demo-ca/Image/hero.png","contentBase64":"iVBORw0KGgo="}`
- **Error Input:** `GET /api/v1/integrations/files/hero.png?storeCode=demo-ca&contentType=Image` against a provider without read capability.
- **Error Output:** `501 {"error":"STORAGE_OPERATION_UNSUPPORTED","message":"The selected storage provider does not support file reads","statusCode":501,"timestamp":"2026-09-01T17:49:37Z"}`

### BR-INT-MS12-020: Storage folder capability handling

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/cms/content/StaticContentFileManagerImpl.java:StaticContentFileManagerImpl.addFolder:91-105`; `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/cms/content/StaticContentFileManagerImpl.java:StaticContentFileManagerImpl.removeFolder:108-113`; `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/cms/content/StaticContentFileManagerImpl.java:StaticContentFileManagerImpl.listFolders:115-129`; `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/cms/content/local/CmsStaticContentFileManagerImpl.java:369-484`; `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/cms/content/infinispan/CmsStaticContentFileManagerImpl.java:429-477`; `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/cms/content/aws/S3StaticContentAssetsManagerImpl.java:229-322`; `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/cms/content/gcp/GCPStaticContentAssetsManagerImpl.java:193-224`  
**Discovery Method:** Hybrid (CAST storage transaction paths + Direct Source Read)

**Statement:** Folder creation, deletion, and listing are available only when the caller-selected storage provider supports the requested capability; an unsupported capability returns an explicit unsupported response.
**Intent:** Validation
**Classification:** Active
**Weight:** Medium

**Logic:**
```pseudocode
facade.addFolder(storeCode, folderName, path) delegates to addFolder adapter
LOCAL addFolder creates the merchant path and folder if absent
LOCAL removeFolder deletes the folder path when it exists
LOCAL listFolders returns null
INFINISPAN addFolder creates the image node and child folder Fqn
INFINISPAN removeFolder and listFolders are TODO
S3 addFolder, removeFolder, listFolders are TODO
GCP addFolder, removeFolder, listFolders are TODO
TARGET:
    inspect provider capability map
    execute supported operation
    return STORAGE_OPERATION_UNSUPPORTED for unsupported operation
```

**Data Dependencies:**
- Reads: `merchantStoreCode`, `folderName`, `Optional.folderPath`, provider capability map
- Writes: local directory or Infinispan folder node when supported

**Side Effects:** Creates or removes a provider folder only where implemented. The folder endpoint receives `provider` explicitly so capability selection is unambiguous.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 8 | 8 | OK |
| Data-flow | 6 | 6 | OK |
| Constants | 2 | 2 | OK |
| State transitions | 0 | 1 | FLAGGED |
| Outcomes | 4 | 4 | OK |
| Data writes | 3 | 3 | OK |
| Integrations | 4 | 4 | OK |
| Error paths | 4 | 4 | OK |

**Preservation:** FLAGGED — target converts null/TODO provider behavior into explicit capability outcomes.

**Concrete Example:**
- **API Input:** `POST /api/v1/integrations/files/folders` `{"storeCode":"demo-ca","provider":"Local","folderPath":"campaigns","folderName":"2026"}`
- **Success:** `201 {"path":"demo-ca/Image/campaigns/2026","provider":"Local","capability":"CreateFolder","status":"Created"}`
- **Error Input:** `POST /api/v1/integrations/files/folders` `{"storeCode":"demo-ca","provider":"GCP","folderPath":"campaigns","folderName":"2026"}` when folder creation is not supported.
- **Error Output:** `501 {"error":"STORAGE_OPERATION_UNSUPPORTED","message":"GCP does not support folder creation","statusCode":501,"timestamp":"2026-09-01T17:49:37Z"}`

## Durable delivery reliability

### BR-INT-MS12-021: Storage operation idempotency association

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/cms/content/StaticContentFileManagerImpl.java:StaticContentFileManagerImpl.addFile:25-35`; `addFiles:47-58`; `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/cms/content/aws/S3StaticContentAssetsManagerImpl.java:S3StaticContentAssetsManagerImpl.addFiles:178-187`; `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/cms/content/gcp/GCPStaticContentAssetsManagerImpl.java:GCPStaticContentAssetsManagerImpl.addFiles:145-153`  
**Discovery Method:** Hybrid source read plus target architecture analysis

**Statement:** Each single or batch storage upload carries a caller-provided idempotency key, and the durable operation record links that key to every provider attempt so a replayed request cannot submit the same logical item twice.
**Intent:** Compliance
**Classification:** Core
**Weight:** Critical

**Logic:**
```pseudocode
operation = find delivery_idempotency by tenant_id + idempotencyKey
IF operation exists AND requestHash differs: return IDEMPOTENCY_KEY_REUSED
IF operation exists AND requestHash matches: return the existing operation result
ELSE create operation(operationId, operationType, idempotencyKey, requestHash)
FOR one upload: create one delivery_attempt(operationId, operationItemKey=fileName)
FOR batch upload: create one delivery_attempt per item, all linked to operationId
provider write uses the logical provider key; successful retry does not create a second logical operation
```

**Data Dependencies:**
- Reads: upload request `storeCode`, `contentType`, `fileName`, `folderPath`, `contentBase64`, `idempotencyKey`
- Writes: `delivery_idempotency.operation_id`, `delivery_idempotency.request_hash`, `delivery_attempt.operation_id`, provider object/key

**Side Effects:** Creates durable operation and attempt associations before provider submission. This is target-only because source uploads have no idempotency or attempt store.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 4 | 7 | FLAGGED |
| Data-flow | 8 | 8 | OK |
| Constants | 0 | 1 | OK |
| State transitions | 0 | 3 | FLAGGED |
| Outcomes | 2 | 3 | FLAGGED |
| Data writes | 2 | 4 | FLAGGED |
| Integrations | 2 | 2 | OK |
| Error paths | 1 | 2 | FLAGGED |

**Preservation:** FLAGGED — all durable idempotency and attempt behavior is target-only; the source-faithful alternative is same-key replacement with durable duplicate suppression.

**Concrete Example:**
- **API Input:** `POST /api/v1/integrations/files/batch` `{"storeCode":"demo-ca","idempotencyKey":"campaign-2026-assets-v1","files":[{"contentType":"Image","fileName":"hero.png","mimeType":"image/png","contentBase64":"iVBORw0KGgo="},{"contentType":"Pdf","fileName":"terms.pdf","mimeType":"application/pdf","contentBase64":"JVBERi0xLjQ="}]}`
- **Success:** `201 {"operationId":"4d8a7f0e-c6b5-4b65-a8de-7cb5c8c3d410","items":[{"operationId":"4d8a7f0e-c6b5-4b65-a8de-7cb5c8c3d410","fileName":"hero.png","contentType":"Image","mimeType":"image/png","providerKey":"demo-ca/Image/hero.png","status":"Available","deliveryAttemptId":"5d8a7f0e-c6b5-4b65-a8de-7cb5c8c3d410"},{"operationId":"4d8a7f0e-c6b5-4b65-a8de-7cb5c8c3d410","fileName":"terms.pdf","contentType":"Pdf","mimeType":"application/pdf","providerKey":"demo-ca/Pdf/terms.pdf","status":"Available","deliveryAttemptId":"6d8a7f0e-c6b5-4b65-a8de-7cb5c8c3d410"}],"acceptedCount":2,"failedCount":0}`
- **Error Input:** `POST /api/v1/integrations/files/batch` `{"storeCode":"demo-ca","idempotencyKey":"campaign-2026-assets-v1","files":[{"contentType":"Image","fileName":"hero.png","mimeType":"image/png","contentBase64":"iVBORw0KGgo="},{"contentType":"Pdf","fileName":"terms.pdf","mimeType":"application/pdf","contentBase64":"JVBERi0xLjU="}]}` repeats the operation key with a changed content byte sequence.
- **Error Output:** `409 {"error":"IDEMPOTENCY_KEY_REUSED","message":"idempotencyKey is already associated with a different upload request","statusCode":409,"timestamp":"2026-09-01T17:49:37Z"}`

### BR-INT-MS12-022: Durable delivery retry policy

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/system/EmailServiceImpl.java:EmailServiceImpl.sendHtmlEmail:24-32`; `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/shipping/impl/UPSShippingQuote.java:UPSShippingQuote.getShippingQuotes:370-617`; `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/cms/content/StaticContentFileManagerImpl.java:StaticContentFileManagerImpl.addFile:25-35`  
**Discovery Method:** Hybrid source read plus target architecture analysis

**Statement:** An external delivery has a bounded attempt budget, records each provider outcome, and schedules retryable failures with increasing delay without reopening a completed attempt.
**Intent:** State Transition
**Classification:** Core
**Weight:** High

**Logic:**
```pseudocode
create delivery_attempt(status=PENDING, attemptNumber=1)
worker changes PENDING -> STARTED
IF provider succeeds:
    change STARTED -> SUCCEEDED; set completedAt and provider outcome
ELSE IF provider failure is retryable AND attemptNumber < endpoint.maxAttempts:
    change STARTED -> FAILED; nextAttemptAt = now + configured exponential backoff
    create next attempt with attemptNumber + 1
ELSE:
    change STARTED -> DEAD_LETTERED; retain provider error details
```

**Data Dependencies:**
- Reads: `integration_endpoint.max_attempts`, `.timeout_ms`, delivery operation payload
- Writes: `delivery_attempt.status`, `.attempt_number`, `.provider_outcome_code`, `.provider_error_code`, `.next_attempt_at`, `.completed_at`

**Side Effects:** Provider calls are retried by a worker; a completed attempt is never resubmitted as the same attempt.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 3 | 8 | FLAGGED |
| Data-flow | 5 | 7 | FLAGGED |
| Constants | 0 | 2 | FLAGGED |
| State transitions | 0 | 6 | FLAGGED |
| Outcomes | 3 | 5 | FLAGGED |
| Data writes | 0 | 7 | FLAGGED |
| Integrations | 1 | 1 | OK |
| Error paths | 3 | 4 | OK |

**Preservation:** FLAGGED — bounded retry and durable states are target-only reliability requirements justified by asynchronous provider delivery.

**Concrete Example:**
- **API Input:** `GET /api/v1/integrations/delivery-attempts/5d8a7f0e-c6b5-4b65-a8de-7cb5c8c3d410`
- **Success:** `200 {"attemptId":"5d8a7f0e-c6b5-4b65-a8de-7cb5c8c3d410","operationId":"4d8a7f0e-c6b5-4b65-a8de-7cb5c8c3d410","endpointId":"4fd4f7f6-0f9a-4903-965e-4bb4cd1d9e85","operationItemKey":"hero.png","attemptNumber":2,"status":"Failed","providerErrorCode":"TIMEOUT","providerErrorMessage":"Provider did not respond within 10000 ms","nextAttemptAt":"2026-09-01T17:50:17Z","createdAt":"2026-09-01T17:49:37Z","updatedAt":"2026-09-01T17:49:37Z"}`
- **Error Input:** `GET /api/v1/integrations/delivery-attempts/aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa`
- **Error Output:** `404 {"error":"DELIVERY_ATTEMPT_NOT_FOUND","message":"Delivery attempt was not found","statusCode":404,"timestamp":"2026-09-01T17:49:37Z"}`

### BR-INT-MS12-023: Outbox, replay, and dead-letter handling

**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/utils/EmailTemplatesUtils.java:EmailTemplatesUtils.sendOrderEmail:89-259`; `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v0/system/SystemRESTController.java:SystemRESTController.createOrUpdateModule:58-84`  
**Discovery Method:** Hybrid source read plus target architecture analysis

**Statement:** A delivery request is recorded before its queue event is published, replay creates a new attempt linked to a failed or dead-lettered attempt, and exhausted delivery failures remain observable without changing the owning business record.
**Intent:** State Transition
**Classification:** Core
**Weight:** High

**Logic:**
```pseudocode
transaction:
    persist delivery_idempotency and initial delivery_attempt
    persist outbox_event(status=PENDING, payload=redacted request)
after commit:
    publish IntegrationDeliveryQueued
    mark outbox_event PUBLISHED with publishedAt
on publish failure:
    mark outbox_event FAILED with the error details
    transition FAILED -> PENDING when the worker schedules another publication attempt
replay(attemptId):
    require original.status in {FAILED, DEAD_LETTERED}
    create new attempt(replayOfAttemptId=original.attemptId)
    publish IntegrationDeliveryQueued for new attempt
    original remains terminal
on exhausted retry:
    mark attempt and operation DEAD_LETTERED
    publish IntegrationDeliveryDeadLettered
```

**Data Dependencies:**
- Reads: `delivery_attempt.status`, `.attempt_id`, `.operation_id`, `.provider_error_code`, `.provider_error_message`
- Writes: `outbox_event.event_type`, `.payload`, `.status`, `.published_at`, `delivery_attempt.replay_of_attempt_id`, terminal attempt state

**Side Effects:** Publishes `IntegrationDeliveryQueued` and `IntegrationDeliveryDeadLettered`; consumes delivery requests and replay commands; leaves MS-05/MS-09/MS-11/MS-02-owned records unchanged.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 2 | 8 | FLAGGED |
| Data-flow | 5 | 7 | FLAGGED |
| Constants | 0 | 1 | OK |
| State transitions | 0 | 7 | FLAGGED |
| Outcomes | 2 | 5 | FLAGGED |
| Data writes | 0 | 8 | FLAGGED |
| Integrations | 1 | 3 | FLAGGED |
| Error paths | 2 | 4 | OK |

**Preservation:** FLAGGED — outbox, replay, and dead-letter persistence are target-only because no equivalent durable source implementation exists.

**Concrete Example:**
- **API Input:** `POST /api/v1/integrations/delivery-attempts/5d8a7f0e-c6b5-4b65-a8de-7cb5c8c3d410/replay` `{"reason":"Provider recovered"}`
- **Success:** `202 {"attemptId":"7d8a7f0e-c6b5-4b65-a8de-7cb5c8c3d410","operationId":"4d8a7f0e-c6b5-4b65-a8de-7cb5c8c3d410","endpointId":"4fd4f7f6-0f9a-4903-965e-4bb4cd1d9e85","operationItemKey":"hero.png","attemptNumber":3,"status":"Pending","replayOfAttemptId":"5d8a7f0e-c6b5-4b65-a8de-7cb5c8c3d410","createdAt":"2026-09-01T17:49:37Z","updatedAt":"2026-09-01T17:49:37Z"}`
- **Error Input:** `POST /api/v1/integrations/delivery-attempts/8d8a7f0e-c6b5-4b65-a8de-7cb5c8c3d410/replay` `{"reason":"Retry successful delivery"}`
- **Error Output:** `409 {"error":"DELIVERY_REPLAY_NOT_ALLOWED","message":"Only failed or dead-lettered attempts can be replayed","statusCode":409,"timestamp":"2026-09-01T17:49:37Z"}`

## Events

### Events consumed

| Event | Source | Action |
|---|---|---|
| `BusinessIntegrationDeliveryRequested` | MS-05 | Create or reuse the durable operation and attempt, then enqueue delivery. |
| `ConfigurationReferenceChanged` | MS-11 | Refresh an opaque adapter endpoint projection. |
| `IntegrationDeliveryReplayRequested` | MS-12 operator/API | Create a new attempt linked to the original terminal attempt. |

### Events published

| Event | Trigger | Consumers |
|---|---|---|
| `IntegrationDeliveryQueued` | Operation and first attempt committed | MS-12 delivery worker |
| `IntegrationDeliveryDeadLettered` | Retry budget exhausted or terminal provider failure | MS-05 and operations |

All event payloads include `eventId`, `eventType`, `occurredAt`, `tenantId`, `storeId`, and
`correlationId`. Provider credentials and plaintext passwords are excluded. Consumers deduplicate
by `eventId`; delivery operations additionally deduplicate by tenant plus idempotency key.
