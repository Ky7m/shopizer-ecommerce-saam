# Shipping Specification — Business Rules

**Version**: 1.0  
**Date**: 2026-09-01  
**Status**: 🟢 100% COMPLETE  
**Service ID**: MS-09  
**Rule count**: 24

## Scope and Ownership

MS-09 owns provider-independent shipping policy: destination eligibility, origin fallback,
packaging facts, free-shipping evaluation, option selection, quote persistence, and shipping
configuration projections.

MS-09 does not own product facts, customer identity/address validation, merchant/store
identity, module configuration storage, carrier credentials, carrier HTTP/XML protocols,
Google Maps calls, retries, or external-response normalization. Those responsibilities are
consumed from MS-02, MS-01/MS-04, MS-10, and MS-11, or delegated to MS-12.

The `BR-PRC-*`, `BR-EXT-*`, and `BR-UI-*` identifiers retain their Phase 1 identity for
traceability. Rules with overlapping evidence are deliberately separated by behavioral seam
rather than duplicated implementation.

---

### BR-PRC-022: Resolve the effective shipping origin

**Source Reference:**  
`initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/shipping/ShippingServiceImpl.java:399-414`  
`initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/shipping/ShippingOriginServiceImpl.java:33-35`  
`initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/shipping/facade/ShippingFacadeImpl.java:120-171`

**Discovery Method:** Hybrid (CAST transaction path + Direct Source Read)  
**CAST Reference:** Transactions 244101 and 244102; `ShippingServiceImpl.getShippingQuote`, complexity 69.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 5 | 5 | OK |
| Data-flow | 8 | 8 | OK |
| Constants | 0 | 0 | OK |
| State transitions | 2 | 2 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 0 | 0 | OK |
| Integrations | 1 | 1 | OK |
| Error paths | 1 | 1 | OK |

**Preservation:** OK

**Statement:** An active origin configured for the store is used for shipping calculations. If
no active origin exists, the store’s registered address, country, postal code, state, and zone
are used as the effective origin.

**Intent:** Routing

**Logic:**
```text
origin = shippingOriginService.getByStore(store)
IF origin IS null OR origin.active IS false:
    origin.address = store.storeaddress
    origin.city = store.storecity
    origin.country = store.country
    origin.postalCode = store.storepostalcode
    origin.state = store.storestateprovince
    origin.zone = store.zone
RETURN origin
```

**Data:** `SHIPING_ORIGIN.MERCHANT_ID`, `ACTIVE`, `STREET_ADDRESS`, `CITY`, `POSTCODE`,
`STATE`, `COUNTRY_ID`, `ZONE_ID`; store address fields.

**Side Effects:** None during resolution. Origin administration writes the origin aggregate.

**Concrete Example:**
- **Input:** `POST /api/v1/cart/CART-100/shipping` with destination `{ "countryCode": "CA", "postalCode": "H2X1Y4" }`; configured origin is active in `M5V1E6`.
- **Success:** `200` returns a quote calculated from origin postal code `M5V1E6`.
- **Error Input:** Store has no address and no configured origin.
- **Error Output:** `422 { "error": "ORIGIN_UNAVAILABLE", "message": "A shipping origin is required" }`.

---

### BR-PRC-023: Enforce national and international destination eligibility

**Source Reference:**  
`initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/shipping/ShippingServiceImpl.java:426-453`  
`initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/shipping/ShippingServiceImpl.java:757-823`

**Discovery Method:** Hybrid (CAST transaction path + Direct Source Read)  
**CAST Reference:** Transactions 244101, 244102, and 244208.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 7 | 7 | OK |
| Data-flow | 6 | 6 | OK |
| Constants | 3 | 3 | OK |
| State transitions | 1 | 1 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 0 | 0 | OK |
| Integrations | 1 | 1 | OK |
| Error paths | 2 | 2 | OK |

**Preservation:** OK

**Statement:** A national shipping policy accepts only destinations in the store’s country. An
international policy accepts only destination countries explicitly enabled for that store.
Unsupported destinations produce no shipping quote and do not invoke a provider.

**Intent:** Validation

**Logic:**
```text
IF shippingType = NATIONAL AND delivery.country.isoCode != store.country.isoCode:
    return quote(NO_SHIPPING_TO_SELECTED_COUNTRY)

IF shippingType = INTERNATIONAL:
    supported = getSupportedCountries(store)
    IF delivery.country.isoCode NOT IN supported:
        return quote(NO_SHIPPING_TO_SELECTED_COUNTRY)
```

**Data:** Merchant shipping configuration; supported-country configuration; country ISO codes.

**Side Effects:** None.

**Concrete Example:**
- **Input:** `POST /api/v1/cart/CART-100/shipping` with `{ "countryCode": "US", "postalCode": "10001" }`; store country is `CA`; policy is `NATIONAL`.
- **Success:** `200 { "shippingOptions": [], "shippingReturnCode": "NO_SHIPPING_TO_SELECTED_COUNTRY US" }`.
- **Error Input:** International policy with destination `JP` absent from the store’s supported-country list.
- **Error Output:** `422 { "error": "DESTINATION_NOT_SUPPORTED", "message": "Shipping is not available to JP" }`.

---

### BR-PRC-024: Select the first active configured provider

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/shipping/ShippingServiceImpl.java:449-486`

**Discovery Method:** Hybrid (CAST transaction path + Direct Source Read)  
**CAST Reference:** Transactions 244101 and 244102; provider-selection segment of `getShippingQuote`.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 8 | 8 | OK |
| Data-flow | 7 | 7 | OK |
| Constants | 2 | 2 | OK |
| State transitions | 1 | 1 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 0 | 0 | OK |
| Integrations | 3 | 3 | OK |
| Error paths | 2 | 2 | OK |

**Preservation:** OK

**Statement:** Provider selection scans configured shipping modules in registry iteration order
and chooses the first active module that is not itself a pre/postprocessor. If no eligible active
provider is available, the quote ends with a no-provider result.

**Intent:** Routing

**Logic:**
```text
modules = getShippingModulesConfigured(store)
IF modules IS null:
    return quote(NO_SHIPPING_MODULE_CONFIGURED)

FOR (moduleCode, configuration) IN modules:
    IF configuration.active:
        candidate = shippingModules[moduleCode]
        IF candidate implements ShippingQuotePrePostProcessModule:
            continue
        provider = candidate
        selectedModuleCode = moduleCode
        break

IF provider IS null:
    return quote(NO_SHIPPING_MODULE_CONFIGURED)
```

**Data:** Merchant module configuration; `IntegrationConfiguration.active`; provider registry;
`IntegrationModule.code`.

**Side Effects:** None.

**Concrete Example:**
- **Input:** Store has active `storePickUp` followed by active `usps`; pickup is a
  preprocessor and USPS is a quote provider.
- **Success:** `200` invokes USPS and returns its normalized options.
- **Error Input:** Store has no active non-processor module.
- **Error Output:** `422 { "error": "NO_SHIPPING_MODULE_CONFIGURED", "message": "No active shipping provider is configured" }`.

---

### BR-PRC-025: Execute preprocessors before provider quotation and allow replacement

**Source Reference:**  
`initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/shipping/ShippingServiceImpl.java:522-551`  
`initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/shipping/impl/ShippingDecisionPreProcessorImpl.java:53-164`  
`initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/shipping/impl/StorePickupShippingQuote.java:117-169`

**Discovery Method:** Hybrid (CAST transaction path + Direct Source Read)  
**CAST Reference:** Transactions 244101 and 244102; preprocessor and provider-replacement path.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 11 | 10 | GAP |
| Data-flow | 10 | 10 | OK |
| Constants | 3 | 3 | OK |
| State transitions | 2 | 2 | OK |
| Outcomes | 4 | 4 | OK |
| Data writes | 1 | 1 | OK |
| Integrations | 4 | 4 | OK |
| Error paths | 2 | 2 | OK |

**Preservation:** FLAGGED (control-flow)

**Statement:** Configured preprocessors run in their injected order before the selected provider
is called. A preprocessor may select a different configured provider; the replacement is used
only when it is active and resolvable.

**Intent:** Routing

**Logic:**
```text
FOR processor IN preProcessors:
    processor.prePostProcessShippingQuotes(...)

    IF quote.currentShippingModule != null
       AND quote.currentShippingModule.code != shippingModule.code:
        shippingModule = quote.currentShippingModule
        configuration = modules[shippingModule.code]
        IF configuration != null AND configuration.active:
            provider = shippingModules[shippingModule.code]
            moduleName = shippingModule.code

provider.getShippingQuotes(...)
```

**Data:** Processor registry, configured module map, current module code, quote current module,
shipping options.

**Side Effects:** Preprocessors may append options, populate distance facts, or set the current
provider. No external adapter call is owned by MS-09.

**Concrete Example:**
- **Input:** `POST /api/v1/cart/CART-100/shipping`; decision processor selects
  `priceByDistance` for a Quebec destination.
- **Success:** `200` contains a `priceByDistance` option and the selected module is
  `priceByDistance`.
- **Error Input:** Processor selects a module that is not configured or inactive.
- **Error Output:** `422 { "error": "PROVIDER_REPLACEMENT_UNAVAILABLE", "message": "Selected shipping provider is not active" }`.

---

### BR-PRC-026: Apply the exclusive free-shipping threshold

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/shipping/ShippingServiceImpl.java:496-520`

**Discovery Method:** Hybrid (CAST transaction path + Direct Source Read)  
**CAST Reference:** Transactions 244101 and 244102; free-shipping segment.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 7 | 7 | OK |
| Data-flow | 6 | 6 | OK |
| Constants | 1 | 1 | OK |
| State transitions | 1 | 1 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 1 | 1 | OK |
| Integrations | 0 | 0 | OK |
| Error paths | 1 | 1 | OK |

**Preservation:** OK

**Statement:** When free shipping is enabled and the merchandise total is strictly greater than
the configured threshold, shipping is free. A national free-shipping policy applies only when
the destination country equals the store country; an international policy applies to all
eligible destinations.

**Intent:** Calculation

**Logic:**
```text
orderTotal = SUM(product.finalPrice * product.quantity)

IF configuration.freeShippingEnabled
   AND configuration.orderTotalFreeShipping != null
   AND orderTotal > configuration.orderTotalFreeShipping:
    IF configuration.freeShippingType = NATIONAL:
        free = store.country.isoCode = delivery.country.isoCode
    ELSE:
        free = true

    IF free:
        quote.freeShipping = true
        quote.freeShippingAmount = threshold
        return quote
```

**Data:** Product final price and quantity; shipping configuration threshold and free-shipping
type; store and delivery country.

**Side Effects:** Provider quotation is bypassed. The current source does not persist a free
quote because it returns before final option persistence.

**Concrete Example:**
- **Input:** `POST /api/v1/cart/CART-100/shipping`; merchandise total `100.01`, threshold `100.00`,
  eligible destination.
- **Success:** `200 { "freeShipping": true, "shipping": "0.00", "shippingOptions": [] }`.
- **Error Input:** Merchandise total exactly `100.00` with threshold `100.00`.
- **Error Output:** `200 { "freeShipping": false, "shippingReturnCode": "PROVIDER_REQUIRED" }`.

---

### BR-PRC-027: Select highest, least, or all shipping options

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/shipping/ShippingServiceImpl.java:570-655`

**Discovery Method:** Hybrid (CAST transaction path + Direct Source Read)  
**CAST Reference:** Transactions 244101 and 244102; option filtering segment.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 13 | 12 | GAP |
| Data-flow | 11 | 11 | OK |
| Constants | 4 | 4 | OK |
| State transitions | 2 | 2 | OK |
| Outcomes | 5 | 5 | OK |
| Data writes | 2 | 2 | OK |
| Integrations | 1 | 1 | OK |
| Error paths | 1 | 1 | OK |

**Preservation:** FLAGGED (control-flow)

**Statement:** The `HIGHEST` policy retains the highest-priced option, `LEAST` retains the
lowest-priced option, and `ALL` returns every option while recording the lowest-priced option
as selected. Comparisons use truncated integer values in the legacy behavior; the target must
replace this with exact decimal comparison unless BA review explicitly approves the defect.

**Intent:** Calculation

**Logic:**
```text
selected = null
FOR option IN providerOptions:
    IF selected == null:
        selected = option

    option.priceText = pricingService.getDisplayAmount(option.price)
    option.moduleCode = selectedModuleCode
    IF option.name is blank:
        option.name = translated country name or country ISO code

    IF priceType = HIGHEST AND option.price.longValue > selected.price.longValue:
        selected = option
    IF priceType = LEAST AND option.price.longValue < selected.price.longValue:
        selected = option
    IF priceType = ALL AND option.price.longValue < selected.price.longValue:
        selected = option

quote.selectedOption = selected
IF priceType != ALL:
    quote.options = [selected]
ELSE:
    quote.options = providerOptions
```

**Data:** `ShippingOption.optionPrice`, option code/name/module, configured price-selection type.

**Side Effects:** Mutates provider options and quote selection; no database write until final
quote persistence.

**Concrete Example:**
- **Input:** Provider options priced `10.25` and `10.75`; selection type `LEAST`.
- **Success:** `200` returns only `10.25`.
- **Error Input:** Selection type `ALL`; options `10.25` and `10.75`.
- **Error Output:** `200` returns both options but legacy-selected option is `10.25`; exact
  target behavior must document decimal precision.

---

### BR-PRC-028: Persist one quote row for each final option

**Source Reference:**  
`initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/shipping/ShippingServiceImpl.java:681-748`  
`initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/shipping/ShippingQuoteServiceImpl.java:39-73`  
`initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/repositories/shipping/ShippingQuoteRepository.java:9-13`

**Discovery Method:** Hybrid (CAST data graph + Direct Source Read)  
**CAST Reference:** Transactions 244101 and 244102; table `salesmanager.shipping_quote`, CAST ID 369,
graph 243933.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 10 | 10 | OK |
| Data-flow | 15 | 15 | OK |
| Constants | 5 | 5 | OK |
| State transitions | 1 | 1 | OK |
| Outcomes | 4 | 4 | OK |
| Data writes | 7 | 7 | OK |
| Integrations | 1 | 1 | OK |
| Error paths | 3 | 3 | OK |

**Preservation:** OK

**Statement:** After provider processing and option filtering, each final shipping option is
stored as an immutable quote snapshot containing the cart, destination, provider, option,
price, handling, estimated days, quote time, and optional customer/order references.

**Intent:** State Transition

**Logic:**
```text
FOR option IN quote.shippingOptions:
    persisted.cartId = shoppingCartId
    persisted.delivery = delivery
    persisted.ipAddress = currentUser.ipAddress
    persisted.estimatedNumberOfDays = parseInteger(option.estimatedNumberOfDays)
    persisted.module = option.shippingModuleCode
    persisted.optionCode = option.optionCode
    persisted.optionName = option.optionName
    persisted.optionShippingDate = now()
    persisted.price = option.optionPrice
    persisted.handling = handlingFees
    persisted.quoteDate = now()
    shippingQuoteService.save(persisted)
    option.shippingQuoteOptionId = persisted.id
```

**Data:** `SHIPPING_QUOTE` fields `CART_ID`, `ORDER_ID`, `CUSTOMER_ID`, `MODULE`,
`OPTION_CODE`, `OPTION_NAME`, `OPTION_DELIVERY_DATE`, `OPTION_SHIPPING_DATE`, `QUOTE_DATE`,
`SHIPPING_NUMBER_DAYS`, `QUOTE_PRICE`, `QUOTE_HANDLING`, `FREE_SHIPPING`, `IP_ADDRESS`, and
embedded delivery fields.

**Side Effects:** Inserts quote rows. Retrieval by order uses `ORDER_ID`.

**Concrete Example:**
- **Input:** `POST /api/v1/cart/CART-100/shipping` returns provider option `GROUND`, price `12.50`,
  estimated days `"3"`.
- **Success:** `201 { "quoteId": "9e4d...", "moduleCode": "usps", "optionCode": "GROUND", "price": "12.50", "estimatedDays": 3 }`.
- **Error Input:** Provider returns `estimatedDays: "three"`.
- **Error Output:** `200` quote is persisted without a numeric estimated-day value and an error is
  logged; target contract returns `422 ESTIMATED_DAYS_INVALID`.

---

### BR-PRC-029: Choose ITEM or BOX packaging, defaulting to ITEM

**Source Reference:**  
`initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/shipping/ShippingServiceImpl.java:870-892`  
`initial-source/shopizer-3.2.7/sm-core-model/src/main/java/com/salesmanager/core/model/shipping/ShippingConfiguration.java:19-47,110-155`

**Discovery Method:** Direct Source Read  
**CAST Reference:** Transactions 244101 and 244102; packaging segment.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 4 | 4 | OK |
| Data-flow | 5 | 5 | OK |
| Constants | 2 | 2 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 0 | 0 | OK |
| Integrations | 1 | 1 | OK |
| Error paths | 1 | 1 | OK |

**Preservation:** OK

**Statement:** Shipping calculations use item-level package facts when the configured packaging
mode is `ITEM`, and box-fit packaging when it is `BOX`. Missing shipping configuration defaults
to item mode at the orchestration layer.

**Intent:** Routing

**Logic:**
```text
packageType = ITEM
IF shippingConfiguration != null:
    packageType = shippingConfiguration.shippingPackageType

IF packageType = BOX:
    return packaging.getBoxPackagesDetails(products, store)
ELSE:
    return packaging.getItemPackagesDetails(products, store)
```

**Data:** Shipping configuration `shippingPackageType`; product dimensions and weights.

**Side Effects:** Creates in-memory `PackageDetails`; no package table is written.

**Concrete Example:**
- **Input:** `POST /api/v1/cart/CART-100/shipping`; configuration `{ "packageType": "ITEM" }`.
- **Success:** `200` provider receives one package fact per shippable unit.
- **Error Input:** Configuration requests `PALLET`, which is outside the supported enum.
- **Error Output:** `422 { "error": "PACKAGE_TYPE_INVALID", "message": "packageType must be ITEM or BOX" }`.

---

### BR-PRC-030: Normalize shippable products into package facts

**Source Reference:**  
`initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/shipping/impl/DefaultPackagingImpl.java:78-139`  
`initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/shipping/impl/DefaultPackagingImpl.java:319-395`

**Discovery Method:** Hybrid (CAST complexity candidate + Direct Source Read)  
**CAST Reference:** `DefaultPackagingImpl.getBoxPackagesDetails`, complexity 32; transactions 244101 and 244102.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 17 | 16 | GAP |
| Data-flow | 18 | 18 | OK |
| Constants | 4 | 4 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 4 | 4 | OK |
| Data writes | 0 | 0 | OK |
| Integrations | 1 | 1 | OK |
| Error paths | 2 | 2 | OK |

**Preservation:** FLAGGED (control-flow)

**Statement:** Virtual products are excluded from shipment calculations. Missing product weight
defaults to `1`, and missing height, length, or width defaults to `4`. Product quantity is
expanded into separate package facts. Product-attribute weight is added where the attribute
provides shipping weight.

**Intent:** Calculation

**Logic:**
```text
FOR shippingProduct IN products:
    product = shippingProduct.product
    IF product.productVirtual:
        continue

    weight = product.productWeight OR 1
    height = product.productHeight OR 4
    length = product.productLength OR 4
    width = product.productWidth OR 4

    FOR attribute IN product.attributes:
        IF attribute.productAttributeWeight != null:
            weight += attribute.productAttributeWeight

    repeat shippingProduct.quantity times:
        append PackageDetails(height, length, width, weight, quantity=1)
```

**Data:** Product virtual flag, weight, height, length, width, quantity, product attributes,
localized description name.

**Side Effects:** Creates in-memory package objects.

**Concrete Example:**
- **Input:** Cart line `{ "sku": "BOOK-001", "quantity": 2, "weight": null, "height": 10, "length": 20, "width": 3, "virtual": false }`.
- **Success:** Provider receives two package facts with weight `1`, dimensions `10×20×3`.
- **Error Input:** Cart contains only `{ "sku": "EBOOK-001", "virtual": true }`.
- **Error Output:** `200 { "shippingRequired": false, "packages": [] }`.

---

### BR-PRC-031: Fit products into boxes using the seventy-five-percent volume rule

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/shipping/impl/DefaultPackagingImpl.java:43-75`  
`initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/shipping/impl/DefaultPackagingImpl.java:141-289`

**Discovery Method:** Hybrid (CAST complexity candidate + Direct Source Read)  
**CAST Reference:** `DefaultPackagingImpl.getBoxPackagesDetails`, complexity 32.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 19 | 19 | OK |
| Data-flow | 17 | 17 | OK |
| Constants | 5 | 5 | OK |
| State transitions | 2 | 2 | OK |
| Outcomes | 5 | 5 | OK |
| Data writes | 4 | 4 | OK |
| Integrations | 1 | 1 | OK |
| Error paths | 5 | 5 | OK |

**Preservation:** OK

**Statement:** A product fits an existing box only when each dimension is within the box,
product weight is within remaining capacity, and product volume is no greater than seventy-five
percent of the box’s remaining volume. Otherwise a new box is created. Zero or invalid box
capacity causes the calculation to fail.

**Intent:** Calculation

**Logic:**
```text
boxVolume = boxWidth * boxLength * boxHeight
IF boxVolume = 0 OR maxWeight = 0:
    log configuration error
    throw "Product configuration exceeds box configuration"

FOR productFact:
    IF any product dimension > corresponding box dimension:
        throw configuration error
    IF productWeight > maxWeight OR productVolume = 0 OR productVolume > boxVolume:
        throw configuration error

    FOR box IN boxes:
        IF box.volumeLeft * 0.75 >= productVolume
           AND box.weightLeft >= productWeight:
            subtract volume and weight
            add productWeight
            assign product
            break

    IF not assigned:
        create box with full capacity
        subtract product volume and weight
```

**Data:** Shipping box dimensions, empty box weight, maximum weight, product dimensions and
weights.

**Side Effects:** Logs merchant shipping errors; creates in-memory box facts.

**Concrete Example:**
- **Input:** Box `40×40×40`, max weight `20`, product `10×10×10`, weight `2`.
- **Success:** Product is assigned when remaining volume times `.75` is at least `1000` and
  remaining weight is at least `2`.
- **Error Input:** Product width `45` against box width `40`.
- **Error Output:** `422 { "error": "PACKAGE_DOES_NOT_FIT", "message": "Product dimensions exceed configured box" }`.

---

### BR-PRC-032: Preserve generated box-weight behavior as a flagged defect

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/shipping/impl/DefaultPackagingImpl.java:294-310`

**Discovery Method:** Direct Source Read  
**CAST Reference:** Transaction 244101; package-construction result path.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 2 | 2 | OK |
| Data-flow | 6 | 5 | GAP |
| Constants | 1 | 1 | OK |
| State transitions | 1 | 1 | OK |
| Outcomes | 2 | 2 | OK |
| Data writes | 1 | 1 | OK |
| Integrations | 0 | 0 | OK |
| Error paths | 0 | 0 | OK |

**Preservation:** FLAGGED (data-flow)

**Statement:** Each generated box fact should report that box’s own accumulated product weight
plus empty-box weight. The legacy implementation instead uses the last-created box’s accumulated
weight while constructing every returned box fact; this is a fidelity defect requiring BA
classification before implementation.

**Intent:** Calculation

**Logic:**
```text
FOR pb IN boxesList:
    details.shippingWeight = configuredEmptyBoxWeight + box.getWeight()
    // `box` is the method's last-created local variable, not `pb`
```

**Data:** Box list, empty box weight, accumulated box weight.

**Side Effects:** Returns potentially incorrect package weights to providers.

**Concrete Example:**
- **Input:** Two boxes with product weights `4` and `7`; empty-box weight `1`.
- **Success target behavior:** Returned weights `5` and `8`.
- **Error Input:** Legacy loop reaches second box last, then constructs both details.
- **Error Output:** Legacy result may report `8` for both boxes; target returns
  `PACKAGE_WEIGHT_CONSTRUCTION_DEFECT` until BA decides whether to correct it.

---

### BR-PRC-033: Calculate distance-based shipping within the service radius

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/shipping/impl/PriceByDistanceShippingQuoteRules.java:59-128`

**Discovery Method:** Hybrid (CAST complexity candidate + Direct Source Read)  
**CAST Reference:** `PriceByDistanceShippingQuoteRules.getShippingQuotes`, complexity 10.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 9 | 9 | OK |
| Data-flow | 8 | 8 | OK |
| Constants | 4 | 4 | OK |
| State transitions | 1 | 1 | OK |
| Outcomes | 4 | 4 | OK |
| Data writes | 1 | 1 | OK |
| Integrations | 1 | 1 | OK |
| Error paths | 3 | 3 | OK |

**Preservation:** OK with a flagged minimum-distance defect

**Statement:** Distance pricing requires a computed distance and postal code. Destinations over
150 km are ineligible. Distances up to and including 20 km use a rate of `2` per kilometre;
greater eligible distances use `3` per kilometre. The current source attempts to clamp distances
below 1 km after calculating the total, so the clamp does not affect the returned price.

**Intent:** Calculation

**Logic:**
```text
IF delivery.postalCode blank OR quote.distance absent:
    return null
IF distance > 150:
    return null

rate = distance <= 20 ? 2 : 3
total = distance * rate
IF distance < 1:
    distance = 1  // occurs after total; no price recalculation
append option(price=total, moduleCode="priceByDistance")
```

**Data:** Delivery postal code; quote distance; option price and module code.

**Side Effects:** Appends an in-memory shipping option.

**Concrete Example:**
- **Input:** `POST /api/v1/cart/CART-100/shipping`; distance `18.5 km`.
- **Success:** `200` returns price `37.00`.
- **Error Input:** Distance `151 km`.
- **Error Output:** `200 { "shippingOptions": [], "shippingReturnCode": "NO_SHIPPING_TO_SELECTED_COUNTRY" }`.

---

### BR-PRC-034: Prepare distance facts through the Maps adapter

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/shipping/impl/ShippingDistancePreProcessorImpl.java:93-209`

**Discovery Method:** Hybrid (CAST transaction path + Direct Source Read)  
**CAST Reference:** Transactions 244101 and 244102; Google distance preprocessing path.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 12 | 12 | OK |
| Data-flow | 18 | 18 | OK |
| Constants | 3 | 3 | OK |
| State transitions | 1 | 1 | OK |
| Outcomes | 4 | 4 | OK |
| Data writes | 2 | 2 | OK |
| Integrations | 4 | 3 | GAP |
| Error paths | 3 | 3 | OK |

**Preservation:** FLAGGED (integration ownership)

**Statement:** Distance enrichment runs only for allowed destination zones with a nonblank postal
code. The Maps adapter geocodes the effective origin and destination, stores destination
coordinates on the delivery snapshot, and records route distance in kilometres. Maps API keys,
HTTP calls, retries, and provider response handling belong to MS-12.

**Intent:** Calculation

**Logic:**
```text
IF delivery.zone is null OR delivery.zone.code not in allowedZones:
    return
IF delivery.postalCode blank:
    return
require apiKey

originAddress = origin.address + origin.city + origin.postalCode
              + optional state + optional zone + origin.country.isoCode
destinationAddress = delivery.address + optional city + delivery.postalCode
                   + optional state + optional zone + delivery.country.isoCode

originResult = Maps.geocode(originAddress).await()
destinationResult = Maps.geocode(destinationAddress).await()

IF both have results:
    delivery.latitude = destinationResult[0].lat
    delivery.longitude = destinationResult[0].lng
    matrix = Maps.distanceMatrix(originResult[0], destinationResult[0]).awaitIgnoreError()
    IF matrix != null:
        quote.informations[DISTANCE_KEY] = matrix.distance.inMeters * 0.001
```

**Data:** Origin/delivery address fields, allowed zone codes, transient delivery coordinates,
quote distance information.

**Side Effects:** Writes transient delivery latitude/longitude and quote distance facts.
External calls are delegated to MS-12.

**Concrete Example:**
- **Input:** Destination zone `QC`, postal code `H2X1Y4`, allowed zones include `QC`.
- **Success:** `200` quote contains `distanceKm: 12.4`, delivery contains coordinates.
- **Error Input:** Destination zone `BC`, not in allowed zones.
- **Error Output:** `200` returns without distance enrichment; distance-based providers return
  `DISTANCE_UNAVAILABLE`.

---

### BR-PRC-035: Aggregate package facts and execute shipping-decision rules

**Source Reference:**  
`initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/shipping/impl/ShippingDecisionPreProcessorImpl.java:53-164`  
`initial-source/shopizer-3.2.7/sm-core/src/main/resources/com/salesmanager/drools/rules/ShippingDecision.drl:1-25`

**Discovery Method:** Hybrid (CAST complexity candidate + Direct Source Read)  
**CAST Reference:** Transactions 244101 and 244102; KIE decision preprocessing path.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 16 | 16 | OK |
| Data-flow | 18 | 18 | OK |
| Constants | 5 | 5 | OK |
| State transitions | 2 | 2 | OK |
| Outcomes | 4 | 4 | OK |
| Data writes | 2 | 2 | OK |
| Integrations | 2 | 2 | OK |
| Error paths | 4 | 4 | OK |

**Preservation:** OK

**Statement:** The decision engine receives total package weight, the largest package volume,
largest package dimension, destination country, province, and optional distance. For a Canadian
destination, a shipment below 62 weight units and below 66 size units selects Canada Post.
A Canadian Quebec shipment over either threshold selects distance pricing. If no rule fires,
the provider remains unchanged.

**Intent:** Routing

**Logic:**
```text
weight = SUM(package.shippingWeight)
volume = MAX(package.height * package.length * package.width)
size = MAX(package.height, package.length, package.width)
province = delivery.zone.code OR delivery.state
distance = quote.informations[DISTANCE_KEY] truncated to long when present

insert ShippingInputParameters(weight, volume, size, country, province, distance)
set global decision = DecisionResponse
fireAllRules()

input.moduleName = decision.moduleName
IF moduleName not blank:
    FOR module IN allModules:
        IF module.code = moduleName:
            quote.currentShippingModule = module
```

Rules:
```text
IF weight < 62 AND size < 66 AND country = "CA":
    decision.moduleName = "canadapost"

IF (weight > 62 OR size > 66) AND country = "CA" AND province = "QC":
    decision.moduleName = "priceByDistance"
```

**Data:** Package dimensions/weights, delivery country/province, quote distance, configured
module registry.

**Side Effects:** KIE rule execution; quote current-module mutation.

**Concrete Example:**
- **Input:** Canada destination, province `ON`, weight `40`, largest dimension `50`.
- **Success:** `200` selects `canadapost`.
- **Error Input:** Canada destination, province `QC`, weight `70`, largest dimension `50`, but
  `priceByDistance` is not configured.
- **Error Output:** `422 { "error": "DECISION_PROVIDER_UNAVAILABLE", "message": "priceByDistance is not configured" }`.

---

### BR-PRC-036: Preserve distance-rule bands and overlap behavior

**Source Reference:**  
`initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/shipping/impl/CustomShippingQuoteRules.java:59-168`  
`initial-source/shopizer-3.2.7/sm-core/src/main/resources/com/salesmanager/drools/rules/PriceByDistance.drl:1-21`  
`initial-source/shopizer-3.2.7/sm-core/src/main/resources/com/salesmanager/drools/rules/PriceByDistance2.drl:1-28`

**Discovery Method:** Hybrid (CAST complexity candidate + Direct Source Read)  
**CAST Reference:** Transaction 244101; custom rules provider and Drools pricing path.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 10 | 10 | OK |
| Data-flow | 9 | 9 | OK |
| Constants | 8 | 8 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 4 | 4 | OK |
| Data writes | 1 | 1 | OK |
| Integrations | 1 | 1 | OK |
| Error paths | 2 | 2 | OK |

**Preservation:** OK with rule-order risk

**Statement:** Custom distance rules translate integer distance facts into configured shipping
prices. The first rule set contains overlapping bands (`<=530` and `<=3550`) with no explicit
salience, while the second rule set contains non-overlapping bands (`<=40`, `41–80`,
`81–2550`). Rule ordering must be made deterministic in the target.

**Intent:** Calculation

**Logic:**
```text
input.distance = quote.distance truncated to long
fire PriceByDistance.drl

PriceByDistance.drl:
    IF distance <= 530: decision.customPrice = "75"
    IF distance <= 3550: decision.customPrice = "140"

PriceByDistance2.drl:
    IF distance <= 40: decision.customPrice = "75"
    IF distance > 40 AND distance <= 80: decision.customPrice = "120"
    IF distance > 80 AND distance <= 2550: decision.customPrice = "140"

IF customPrice != null:
    append option(price=BigDecimal(customPrice), code="customQuotesRules")
```

**Data:** Quote distance; package weight, largest volume, country, province, module code;
custom-price response.

**Side Effects:** KIE execution; appends a custom shipping option.

**Concrete Example:**
- **Input:** Distance `500`, custom rule set `PriceByDistance`.
- **Success:** Deterministic target response identifies the selected rule and price.
- **Error Input:** Distance `500` under the legacy overlapping rule set.
- **Error Output:** `409 { "error": "DISTANCE_RULE_ORDER_UNDEFINED", "message": "Overlapping distance rules require explicit precedence" }`.

---

### BR-EXT-010: Apply the destination gate before adapter invocation

**Source Reference:**  
`initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/shipping/ShippingServiceImpl.java:426-453`  
`initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/shipping/impl/UPSShippingQuote.java:121-153`  
`initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/shipping/impl/USPSShippingQuote.java:125-153`

**Discovery Method:** Hybrid  
**CAST Reference:** Transactions 244101 and 244102; provider invocation paths.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 8 | 8 | OK |
| Data-flow | 7 | 7 | OK |
| Constants | 4 | 4 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 0 | 0 | OK |
| Integrations | 2 | 2 | OK |
| Error paths | 2 | 2 | OK |

**Preservation:** OK

**Statement:** A carrier adapter is never called for a destination rejected by MS-09 policy.
Carrier-specific country restrictions remain adapter capabilities, but they cannot widen the
MS-09 eligibility decision.

**Intent:** Compliance

**Logic:**
```text
validate destination in MS-09
IF rejected:
    return no-shipping result
ELSE:
    invoke normalized adapter contract
```

**Data:** Store country, delivery country, supported-country configuration.

**Side Effects:** Prevents an external call.

**Concrete Example:**
- **Input:** National store `CA`, destination `US`.
- **Success:** `200` no-shipping result; no carrier request.
- **Error Input:** Adapter called despite rejected destination.
- **Error Output:** `500 { "error": "BOUNDARY_VIOLATION", "message": "Carrier invocation occurred before destination eligibility" }`.

---

### BR-EXT-011: Bypass providers for free shipping

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/shipping/ShippingServiceImpl.java:496-520`

**Discovery Method:** Hybrid  
**CAST Reference:** Transactions 244101 and 244102.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 4 | 4 | OK |
| Data-flow | 5 | 5 | OK |
| Constants | 1 | 1 | OK |
| State transitions | 1 | 1 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 0 | 1 | GAP |
| Integrations | 1 | 0 | GAP |
| Error paths | 1 | 1 | OK |

**Preservation:** FLAGGED (free-quote persistence is absent in source)

**Statement:** An eligible free-shipping result terminates provider quotation and returns zero
shipping cost. The target must explicitly decide whether the zero-cost quote is persisted for
checkout reproducibility.

**Intent:** Routing

**Logic:**
```text
IF eligibleFreeShipping:
    quote.freeShipping = true
    quote.shipping = 0
    return quote
```

**Data:** Merchandise total, threshold, free-shipping type, destination eligibility.

**Side Effects:** No carrier/provider call. Legacy returns before quote-row persistence.

**Concrete Example:**
- **Input:** Eligible cart total `250.00`, threshold `200.00`.
- **Success:** `200 { "freeShipping": true, "shipping": "0.00" }`.
- **Error Input:** Checkout requests the quote ID after the legacy free-shipping response.
- **Error Output:** `404 { "error": "FREE_QUOTE_NOT_PERSISTED", "message": "No persisted quote snapshot exists" }`.

---

### BR-EXT-012: Expose provider replacement as policy, not adapter state

**Source Reference:**  
`initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/shipping/ShippingServiceImpl.java:522-551`  
`initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/shipping/impl/ShippingDecisionPreProcessorImpl.java:147-164`

**Discovery Method:** Hybrid  
**CAST Reference:** Transactions 244101 and 244102.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 8 | 8 | OK |
| Data-flow | 9 | 9 | OK |
| Constants | 2 | 2 | OK |
| State transitions | 2 | 2 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 1 | 1 | OK |
| Integrations | 2 | 2 | OK |
| Error paths | 2 | 2 | OK |

**Preservation:** OK

**Statement:** MS-09 decides which provider policy applies. MS-12 adapters receive the
resulting provider code and must not select or replace providers themselves.

**Intent:** Routing

**Logic:**
```text
selectedModule = preprocessors.reduce(initialModule, policy)
configuration = configuredModules[selectedModule.code]
adapter = integrationGateway.forProvider(selectedModule.code)
adapter.quote(normalizedRequest)
```

**Data:** Provider code, active configuration, module registry.

**Side Effects:** Provider selection is recorded in the quote snapshot.

**Concrete Example:**
- **Input:** Decision facts select `priceByDistance`.
- **Success:** Adapter gateway receives `{ "providerCode": "priceByDistance" }`.
- **Error Input:** Carrier adapter changes provider code to `usps`.
- **Error Output:** `500 { "error": "ADAPTER_POLICY_VIOLATION", "message": "Adapters cannot replace MS-09 provider policy" }`.

---

### BR-EXT-013: Treat distance data as an adapter prerequisite

**Source Reference:**  
`initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/shipping/impl/ShippingDistancePreProcessorImpl.java:105-189`  
`initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/shipping/impl/PriceByDistanceShippingQuoteRules.java:81-92`

**Discovery Method:** Hybrid  
**CAST Reference:** Transactions 244101 and 244102.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 7 | 7 | OK |
| Data-flow | 9 | 9 | OK |
| Constants | 2 | 2 | OK |
| State transitions | 1 | 1 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 1 | 1 | OK |
| Integrations | 3 | 2 | GAP |
| Error paths | 2 | 2 | OK |

**Preservation:** FLAGGED (external ownership)

**Statement:** Distance-based policy can produce a quote only when MS-12 supplies a usable
distance fact. Missing, disallowed, or failed geocoding does not result in a fabricated
distance-based price.

**Intent:** Validation

**Logic:**
```text
IF quote.distance is null:
    return no distance option
IF quote.distance > 150:
    return no distance option
```

**Data:** Quote distance fact, delivery zone and postal code.

**Side Effects:** None.

**Concrete Example:**
- **Input:** Allowed zone `QC`, distance fact `22`.
- **Success:** Distance policy produces a quote.
- **Error Input:** Maps adapter returns no matrix.
- **Error Output:** `422 { "error": "DISTANCE_UNAVAILABLE", "message": "Distance-based shipping cannot be calculated" }`.

---

### BR-EXT-014: Make distance-rule precedence explicit

**Source Reference:**  
`initial-source/shopizer-3.2.7/sm-core/src/main/resources/com/salesmanager/drools/rules/PriceByDistance.drl:7-19`  
`initial-source/shopizer-3.2.7/sm-core/src/main/resources/com/salesmanager/drools/rules/PriceByDistance2.drl:7-26`

**Discovery Method:** Direct Source Read  
**CAST Reference:** Transaction 244101; custom-rule execution path.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 6 | 6 | OK |
| Data-flow | 4 | 4 | OK |
| Constants | 6 | 6 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 2 | 2 | OK |
| Integrations | 0 | 0 | OK |
| Error paths | 1 | 1 | OK |

**Preservation:** OK

**Statement:** Distance bands must be mutually exclusive or carry explicit priority. A distance
that satisfies multiple rules cannot produce an implementation-dependent price.

**Intent:** Compliance

**Logic:**
```text
validate every active band:
    lowerBound < upperBound
    no overlap unless priority is present
IF overlap exists without priority:
    reject rule set
```

**Data:** Distance rule lower/upper bounds, price, priority.

**Side Effects:** Rule-set validation failure prevents activation.

**Concrete Example:**
- **Input:** Bands `0–530 => 75` and `0–3550 => 140`, no priority.
- **Success:** `422` on activation with `DISTANCE_RULE_OVERLAP`.
- **Error Input:** Bands `0–530 => 75 priority 10`, `531–3550 => 140 priority 10`.
- **Error Output:** `200 { "status": "validated" }`.

---

### BR-EXT-015: Select the first matching configured weight bracket

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/shipping/impl/CustomWeightBasedShippingQuote.java:89-153`

**Discovery Method:** Hybrid (CAST complexity candidate + Direct Source Read)  
**CAST Reference:** `CustomWeightBasedShippingQuote.getShippingQuotes`, complexity 11.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 9 | 9 | OK |
| Data-flow | 10 | 10 | OK |
| Constants | 2 | 2 | OK |
| State transitions | 0 | 0 | OK |
| Outcomes | 4 | 4 | OK |
| Data writes | 1 | 1 | OK |
| Integrations | 1 | 1 | OK |
| Error paths | 3 | 3 | OK |

**Preservation:** OK

**Statement:** For a destination in a configured custom region, total package weight is compared
with region brackets in configuration order. The first bracket whose maximum weight is greater
than or equal to the shipment weight supplies the price. No matching region or bracket produces
no option.

**Intent:** Calculation

**Logic:**
```text
IF delivery.postalCode blank:
    return null

FOR region IN customConfiguration.regions:
    IF delivery.country.isoCode IN region.countries:
        weight = SUM(package.shippingWeight)
        FOR bracket IN region.quoteItems:
            IF weight <= bracket.maximumWeight:
                return option(
                    code="CUSTOM_WEIGHT",
                    id="CUSTOM_WEIGHT_" + region.name,
                    price=bracket.price)
return null
```

**Data:** Custom regions, country codes, maximum weights, prices, package weights.

**Side Effects:** Returns an in-memory option.

**Concrete Example:**
- **Input:** Region `CA`, brackets `0–5 kg => 8.00`, `5–20 kg => 14.00`; shipment `4.5 kg`.
- **Success:** `200` returns price `8.00`.
- **Error Input:** Shipment `25 kg`, no bracket covers it.
- **Error Output:** `200 { "shippingOptions": [], "shippingReturnCode": "NO_WEIGHT_BRACKET" }`.

---

### BR-EXT-016: Add store pickup through the preprocessor pipeline

**Source Reference:** `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/shipping/impl/StorePickupShippingQuote.java:31-43`  
`initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/shipping/impl/StorePickupShippingQuote.java:117-169`

**Discovery Method:** Hybrid  
**CAST Reference:** Transactions 244101 and 244102; pickup preprocessor path.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 8 | 8 | OK |
| Data-flow | 8 | 8 | OK |
| Constants | 4 | 4 | OK |
| State transitions | 1 | 1 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 0 | 0 | OK |
| Integrations | 0 | 0 | OK |
| Error paths | 3 | 3 | OK |

**Preservation:** OK

**Statement:** Active store pickup configuration contributes a pickup option during preprocessing.
Its price comes from configured module data, its identifier includes the destination region, and
it becomes the initial selected option when no option has previously been selected.

**Intent:** Calculation

**Logic:**
```text
require globalShippingConfiguration
IF not active:
    return

region = delivery.zone.code OR delivery.state
price = parse(globalShippingConfiguration.integrationKeys["price"])
option.code = "storePickUp"
option.id = "storePickUp_" + region
option.price = price
append quote.options
IF quote.selectedOption is null:
    quote.selectedOption = option
```

**Data:** Pickup module active flag, configured price, delivery zone/state, quote options.

**Side Effects:** Appends an option; may set selected option.

**Concrete Example:**
- **Input:** Active pickup price `0.00`, destination zone `QC`.
- **Success:** `200` contains `{ "optionCode": "storePickUp", "optionId": "storePickUp_QC", "price": "0.00" }`.
- **Error Input:** Active pickup configuration lacks a numeric price.
- **Error Output:** `422 { "error": "PICKUP_PRICE_INVALID", "message": "Pickup price must be numeric" }`.

---

### BR-EXT-018: Expose quote snapshots to downstream services

**Source Reference:**  
`initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/shipping/ShippingServiceImpl.java:681-748`  
`initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/shipping/ShippingQuoteServiceImpl.java:45-73`

**Discovery Method:** Hybrid  
**CAST Reference:** Transactions 244101 and 244102; `salesmanager.shipping_quote`, CAST ID 369.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 5 | 5 | OK |
| Data-flow | 12 | 12 | OK |
| Constants | 1 | 1 | OK |
| State transitions | 1 | 1 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 4 | 4 | OK |
| Integrations | 2 | 2 | OK |
| Error paths | 2 | 2 | OK |

**Preservation:** OK

**Statement:** Checkout and order services receive a stable shipping-method and price snapshot.
MS-09 supplies shipping and handling facts but never changes cart or order lifecycle state.

**Intent:** State Transition

**Logic:**
```text
quoteSnapshot = persisted quote row
checkout consumes quoteSnapshot.module, optionCode, price, handling, delivery, freeShipping
MS-09 performs no order status update
```

**Data:** Quote snapshot fields and delivery address.

**Side Effects:** Readback of persisted quote data.

**Concrete Example:**
- **Input:** `GET /api/v1/cart/CART-100/shipping/quotes/QUOTE-1`.
- **Success:** `200` returns provider, option, price, handling, destination, and quote timestamp.
- **Error Input:** Caller attempts to set order status through MS-09.
- **Error Output:** `405 { "error": "ORDER_LIFECYCLE_NOT_OWNED", "message": "MS-09 does not transition orders" }`.

---

### BR-UI-008: Serialize shipping-rule definitions as configuration commands

**Source Reference:**  
`initial-source/shopizer-admin-main/src/app/pages/shipping/rules/rules.component.ts:158-207`  
`initial-source/shopizer-admin-main/src/app/pages/shipping/services/shared.service.ts:50-84`

**Discovery Method:** Hybrid (CAST/API scope + Direct Source Read)  
**CAST Reference:** Shipping administration dependency of transaction scope; rule configuration flow.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 10 | 10 | OK |
| Data-flow | 13 | 13 | OK |
| Constants | 4 | 4 | OK |
| State transitions | 2 | 2 | OK |
| Outcomes | 4 | 4 | OK |
| Data writes | 2 | 2 | OK |
| Integrations | 2 | 2 | OK |
| Error paths | 2 | 2 | OK |

**Preservation:** OK

**Statement:** An administration user can define a shipping rule with a name, unique code,
store scope, enabled flag, UTC start/end dates, ordered actions, and one or more criteria
expressions. String criterion values are serialized as single-element arrays; non-string values
retain their original representation.

**Intent:** Validation

**Logic:**
```text
actions = actionsData.map(a => { code: a.code, value: a.value })
ruleSets = query.rules.map(q =>
    typeof q.value == string
      ? { field: q.field, operator: q.operator, value: [q.value] }
      : { field: q.field, operator: q.operator, value: q.value })

payload = {
    name, code, store, enabled,
    startDate: UTC(startDate),
    endDate: UTC(endDate),
    actions,
    ruleSets: [{ condition: query.condition, rules: ruleSets }]
}
POST or PUT shipping rule endpoint
```

**Data:** Rule name/code/store/enabled/dates, criteria fields/operators/values, actions.

**Side Effects:** Configuration command sent to shipping-rule administration API.

**Concrete Example:**
- **Input:** `{ "code": "CA_FREE", "store": "DEFAULT", "enabled": true, "ruleSets": [{ "condition": "and", "rules": [{ "field": "country", "operator": "=", "value": ["CA"] }] }], "actions": [{ "code": "freeShipping", "value": true }] }`.
- **Success:** `201 { "status": "created", "code": "CA_FREE" }`.
- **Error Input:** Same code already exists for the store.
- **Error Output:** `409 { "error": "RULE_CODE_EXISTS", "message": "Shipping rule code already exists for this store" }`.

---

## API and Configuration Rules

The following API-facing behavior is included in the 16-operation contract:

- Quote calculation is exposed publicly for anonymous checkout and authenticated carts.
- Administrative origin, expedition, package, and provider operations require authorization.
- Module configuration and package definitions are compatibility projections over MS-11-owned
  configuration data; MS-09 does not create ownership for `module_configuration` or merchant
  configuration records.
- Carrier and Maps calls are represented as MS-12 adapter dependencies, not MS-09 endpoints.
- `ShippingConfigurationFacadeImpl` is a stub and is excluded from active behavior:
  `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/facade/shipping/ShippingConfigurationFacadeImpl.java:13-36`.
