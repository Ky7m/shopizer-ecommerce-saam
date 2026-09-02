# Cart and Checkout — Business Rules

**Service ID:** MS-04  
**Version:** 1.0  
**Date:** 2026-09-01  
**Analysis mode:** Hybrid — CAST structural discovery plus direct source reading  
**Ownership:** Cart mutation, cart hydration, checkout-session orchestration, quote coordination, checkout freezing, idempotent submission, and `OrderSubmitted` publication.

MS-04 does not own product facts or inventory, price and promotion algorithms, tax calculation, shipping-provider algorithms, payment-provider state, order lifecycle, or download entitlements. Those capabilities remain at the MS-02, MS-07, MS-08, MS-09, MS-06, MS-05, and MS-12 boundaries respectively.

## Rule Conventions

- Statements are architecture-independent business declarations.
- Logic is source-derived pseudocode and retains legacy names only for traceability.
- Target-only rules are explicitly identified where the legacy implementation had no equivalent.
- All monetary values in the target contract use decimal strings with currency metadata.
- All cart and checkout reads and writes are tenant- and store-scoped.

---

## Cart creation and mutation

### BR-SC-CRE-001: A cart receives a unique store-scoped client code

**Source Reference:** `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/controller/shoppingCart/facade/ShoppingCartFacadeImpl.java` : `createCartModel()` lines 440-456; `uniqueShoppingCartCode()` lines 1080-1082; `addToCart()` lines 756-779  
**Cross-Reference:** `ShoppingCartApi.addToCart()` lines 75-85  
**Discovery Method:** Direct Source Read | CAST Imaging (Hybrid)  
**CAST Reference:** Transaction 244210 `/api/v1/cart`; objects 30430 and 30045

**Statement:** Every newly created cart must belong to exactly one tenant and store, may optionally be associated with a customer, and must expose a unique opaque client code when the caller does not supply one.

**Intent:** State Transition / Authorization
**Classification:** Core
**Weight:** High

**Logic:**
```pseudocode
customerId = customer != null ? customer.id : null
cart = new ShoppingCart()

IF shoppingCartCode is not blank:
    cart.shoppingCartCode = shoppingCartCode
ELSE:
    cart.shoppingCartCode = UUID.randomUUID().toString().replace("-", "")

cart.merchantStore = store
cart.customerId = customerId
persist cart
```

**Data Dependencies:**
- Reads: `SHOPPING_CART.SHP_CART_CODE`, `SHOPPING_CART.MERCHANT_ID`, `SHOPPING_CART.CUSTOMER_ID`
- Writes: `SHOPPING_CART.SHP_CART_CODE`, `SHOPPING_CART.MERCHANT_ID`, `SHOPPING_CART.CUSTOMER_ID`

**Side Effects:**
- Calls the cart persistence service.
- Target: records tenant and store context before persistence.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 2 | 2 | OK |
| Data-flow | 4 | 4 | OK |
| Constants | 1 | 1 | OK |
| State transitions | 1 | 1 | OK |
| Outcomes | 2 | 2 | OK |
| Data writes | 3 | 3 | OK |
| Integrations | 1 | 1 | OK |
| Error paths | 1 | 1 | OK |

**Preservation:** OK

**Concrete Example:**
- API Input: `POST /api/v1/cart`  
  `{"product":"SKU-RED-42","quantity":1,"attributes":[]}`
- Success Output: `201`  
  `{"cart":{"code":"a91b8c7d2e","status":"Open","customerId":null,"items":[{"sku":"SKU-RED-42","quantity":1}]}}`
- Error Input: same request with `x-store-id` belonging to another tenant
- Error Output: `403`  
  `{"error":"STORE_SCOPE_MISMATCH","message":"Store is not available in the requested tenant","statusCode":403}`

---

### BR-SC-SEL-002: A cart line must reference a sellable product in the active store

**Source Reference:** `ShoppingCartFacadeImpl.createCartItem()` lines 254-342 and `createCartItems()` lines 346-429  
**Cross-Reference:** `ShoppingCartApi.addToCart()` lines 80-84; `ShoppingCartApi.modifyCart()` lines 91-115  
**Discovery Method:** Direct Source Read | CAST Imaging (Hybrid)  
**CAST Reference:** Transaction 244210 `/api/v1/cart`; Transaction 244211 `/api/v1/cart/{code}`; object 30045

**Statement:** A cart can contain a product only when the product is found by SKU in the requested store, belongs to that store, is marked available, has configured inventory, and is available for sale at the current time.

**Intent:** Validation / Authorization
**Classification:** Core
**Weight:** High

**Logic:**
```pseudocode
product = productService.getBySku(request.product, store, store.defaultLanguage)

IF product == null:
    REJECT "Product with sku ... does not exist"

IF product.merchantStore.id != store.id:
    REJECT "Item ... does not belong to merchant ..."

IF NOT product.available:
    REJECT "Product ... is not available"

IF product.dateAvailable > now:
    REJECT "Item ... is not available"

availabilities = product.availabilities
IF product has variants AND first variant has availabilities:
    availabilities = first variant.availabilities

IF availabilities is empty:
    REJECT "Item ... contains no inventory"

FOR availability IN availabilities:
    IF availability.productQuantity IS NULL OR availability.productQuantity == 0:
        REJECT "Product ... is not available"

cartItem = populateShoppingCartItem(product, store)
cartItem.quantity = request.quantity
cartItem.sku = product.sku
cartItem.variant = first variant id when present
```

**Data Dependencies:**
- Reads: `product.PRODUCT_ID`, `product.SKU`, `product.MERCHANT_ID`, `product.AVAILABLE`, `product.DATE_AVAILABLE`, `product_availability.PRODUCT_QUANTITY`, `product_availability.REGION`
- Writes: `SHOPPING_CART_ITEM.PRODUCT_ID`, `SHOPPING_CART_ITEM.SKU`, `SHOPPING_CART_ITEM.PRODUCT_VARIANT`, `SHOPPING_CART_ITEM.QUANTITY`

**Side Effects:**
- Calls MS-02 product, attribute, and availability services.
- Does not write product availability.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 12 | 10 | OK |
| Data-flow | 10 | 10 | OK |
| Constants | 2 | 2 | OK |
| State transitions | 1 | 1 | OK |
| Outcomes | 5 | 5 | OK |
| Data writes | 4 | 4 | OK |
| Integrations | 3 | 3 | OK |
| Error paths | 5 | 5 | OK |

**Preservation:** OK

**Concrete Example:**
- API Input: `POST /api/v1/cart`  
  `{"product":"SKU-RED-42","quantity":2,"attributes":[]}`
- Success Output: `201`  
  `{"cart":{"code":"a91b8c7d2e","items":[{"sku":"SKU-RED-42","quantity":2}]}}`
- Error Input: `{"product":"SKU-DISCONTINUED","quantity":1,"attributes":[]}`
- Error Output: `422`  
  `{"error":"PRODUCT_NOT_SELLABLE","message":"Product SKU-DISCONTINUED is not available for sale","statusCode":422}`

---

### BR-SC-ATR-003: Selected attributes must belong to the requested product

**Source Reference:** `ShoppingCartFacadeImpl.createCartItem()` lines 323-340; `createCartItems()` lines 408-425; `ShoppingCartServiceImpl.getPopulatedItem()` lines 309-353  
**Cross-Reference:** `OrderProductPopulator.populate()` lines 117-144  
**Discovery Method:** Direct Source Read  
**CAST Reference:** Transaction 244210 cart-add graph; Transaction 244089 checkout graph

**Statement:** A selected product option may be attached to a cart line only when its product association matches the line product; unrelated or removed options must not become part of the cart or checkout snapshot.

**Intent:** Validation
**Classification:** Core
**Weight:** High

**Logic:**
```pseudocode
FOR requestedAttribute IN request.attributes:
    productAttribute = productAttributeService.getById(requestedAttribute.id)

    IF productAttribute != null
       AND productAttribute.product.id == product.id:
        cartItem.attributes.add(new ShoppingCartAttributeItem(cartItem, productAttribute))
    ELSE:
        ignore the unrelated attribute during cart construction

FOR persistedAttribute IN cartItem.attributes:
    IF persistedAttribute.productAttributeId is not present in product.attributes:
        shoppingCartAttributeItemRepository.delete(persistedAttribute)
```

**Data Dependencies:**
- Reads: `SHOPPING_CART_ATTR_ITEM.PRODUCT_ATTR_ID`, `SHOPPING_CART_ATTR_ITEM.SHP_CART_ITEM_ID`, `product_attribute.PRODUCT_ID`
- Writes: `SHOPPING_CART_ATTR_ITEM.PRODUCT_ATTR_ID`, `SHOPPING_CART_ATTR_ITEM.SHP_CART_ITEM_ID`
- Deletes: `SHOPPING_CART_ATTR_ITEM`

**Side Effects:**
- Calls MS-02 product-attribute lookup.
- Deletes orphaned attribute rows during hydration.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 7 | 7 | OK |
| Data-flow | 6 | 6 | OK |
| Constants | 0 | 0 | N/A |
| State transitions | 1 | 1 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 2 | 2 | OK |
| Integrations | 2 | 2 | OK |
| Error paths | 2 | 2 | OK |

**Preservation:** OK

**Concrete Example:**
- API Input: `POST /api/v1/cart`  
  `{"product":"SKU-RED-42","quantity":1,"attributes":[{"id":701}]}`
- Success Output: `201`  
  `{"cart":{"items":[{"sku":"SKU-RED-42","quantity":1,"attributes":[{"id":701}]}]}}`
- Error Input: attribute `701` belongs to `SKU-BLUE-7`
- Error Output: `422`  
  `{"error":"ATTRIBUTE_PRODUCT_MISMATCH","message":"Attribute 701 is not valid for SKU-RED-42","statusCode":422}`

---

### BR-SC-MRG-004: Duplicate attribute-free physical lines merge by quantity

**Source Reference:** `ShoppingCartFacadeImpl.readableShoppingCart()` lines 830-868; `addItemsToShoppingCart()` lines 150-171; `ShoppingCartServiceImpl.mergeShoppingCarts()` lines 390-438  
**Cross-Reference:** `ShoppingCartFacadeImpl.addToCart(Customer,...)` lines 1024-1043  
**Discovery Method:** Direct Source Read | CAST Imaging (Hybrid)  
**CAST Reference:** Transaction 244210 cart-add; Transaction 244213 multi-cart update

**Statement:** When a physical product without selected attributes is added to a cart that already contains the same product without attributes, the existing line absorbs the new quantity instead of creating a second line. Attribute-bearing lines remain distinct.

**Intent:** Calculation / State Transition
**Classification:** Core
**Weight:** High

**Logic:**
```pseudocode
duplicateFound = false

IF request.attributes is empty:
    FOR existingItem IN cart.lineItems:
        IF existingItem.product.sku == request.product
           AND existingItem.attributes is empty
           AND duplicateFound == false:
            IF newItem.productVirtual == false:
                existingItem.quantity =
                    existingItem.quantity + newItem.quantity
            duplicateFound = true
            BREAK

IF duplicateFound == false:
    cart.lineItems.add(newItem)
```

**Data Dependencies:**
- Reads: `SHOPPING_CART_ITEM.SKU`, `SHOPPING_CART_ITEM.QUANTITY`, `SHOPPING_CART_ATTR_ITEM.PRODUCT_ATTR_ID`, product virtual flag
- Writes: `SHOPPING_CART_ITEM.QUANTITY`, `SHOPPING_CART_ITEM.SHP_CART_ID`

**Side Effects:**
- Persists the updated cart.
- Recalculates the cart after persistence.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 8 | 8 | OK |
| Data-flow | 6 | 6 | OK |
| Constants | 0 | 0 | N/A |
| State transitions | 2 | 2 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 2 | 2 | OK |
| Integrations | 1 | 1 | OK |
| Error paths | 1 | 1 | OK |

**Preservation:** OK

**Concrete Example:**
- API Input: first `POST /api/v1/cart` with `{"product":"SKU-RED-42","quantity":2,"attributes":[]}`, followed by a second identical request with quantity `3`
- Success Output: `201`  
  `{"cart":{"items":[{"sku":"SKU-RED-42","quantity":5}]}}`
- Error Input: request contains attributes from a different product
- Error Output: `422`  
  `{"error":"ATTRIBUTE_PRODUCT_MISMATCH","message":"Selected attributes cannot be merged with the requested product","statusCode":422}`

---

### BR-SC-UPD-005: Updating a line to zero removes the line and its attributes

**Source Reference:** `ShoppingCartFacadeImpl.modifyCart()` lines 877-927; `modifyCartMulti()` lines 968-1022; `ShoppingCartServiceImpl.deleteShoppingCartItem()` lines 491-511  
**Cross-Reference:** `ShoppingCartApi.modifyCart()` lines 87-115 and lines 149-173  
**Discovery Method:** Direct Source Read | CAST Imaging (Hybrid)  
**CAST Reference:** Transaction 244211 `/api/v1/cart/{code}`; Transaction 244213 `/multi`; object 30046

**Statement:** A cart update with quantity zero means removal, and removal must delete the selected-attribute rows before deleting the cart-line row. Positive quantities replace the existing line quantity.

**Intent:** State Transition
**Classification:** Core
**Weight:** High

**Logic:**
```pseudocode
FOR incomingItem IN request.items:
    oldItem = existingItems.findBySku(incomingItem.product)

    IF oldItem exists:
        IF incomingItem.quantity == oldItem.quantity:
            CONTINUE

        IF incomingItem.quantity == 0:
            shoppingCartService.deleteShoppingCartItem(oldItem.id)
            cart.lineItems.remove(oldItem)
        ELSE:
            oldItem.quantity = incomingItem.quantity
    ELSE:
        IF incomingItem.quantity > 0:
            cart.lineItems.add(incomingItem)

deleteShoppingCartItem(itemId):
    item = shoppingCartItemRepository.findOne(itemId)
    IF item != null:
        FOR attribute IN item.attributes:
            shoppingCartAttributeItemRepository.deleteById(attribute.id)
        item.attributes.clear()
        shoppingCartItemRepository.deleteById(itemId)
```

**Data Dependencies:**
- Reads: `SHOPPING_CART_ITEM.SHP_CART_ITEM_ID`, `SHOPPING_CART_ITEM.QUANTITY`, `SHOPPING_CART_ATTR_ITEM.SHP_CART_ITEM_ID`
- Writes: `SHOPPING_CART_ITEM.QUANTITY`
- Deletes: `SHOPPING_CART_ATTR_ITEM`, `SHOPPING_CART_ITEM`

**Side Effects:**
- Persists cart line deletion or quantity replacement.
- Recalculates the returned cart.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 10 | 10 | OK |
| Data-flow | 7 | 7 | OK |
| Constants | 1 | 1 | OK |
| State transitions | 3 | 3 | OK |
| Outcomes | 4 | 4 | OK |
| Data writes | 3 | 3 | OK |
| Integrations | 1 | 1 | OK |
| Error paths | 2 | 2 | OK |

**Preservation:** OK

**Concrete Example:**
- API Input: `PUT /api/v1/cart/a91b8c7d2e`  
  `{"product":"SKU-RED-42","quantity":0,"attributes":[]}`
- Success Output: `201`  
  `{"cart":{"code":"a91b8c7d2e","items":[],"status":"Obsolete"}}`
- Error Input: `{"product":"SKU-RED-42","quantity":-1,"attributes":[]}`
- Error Output: `422`  
  `{"error":"INVALID_QUANTITY","message":"Quantity must be zero or greater for an update","statusCode":422}`

---

### BR-SC-HYD-006: Cart hydration refreshes product facts and prices before use

**Source Reference:** `ShoppingCartServiceImpl.getByCode()` lines 182-205; `getPopulatedShoppingCart()` lines 230-274; `getPopulatedItem()` lines 292-359  
**Cross-Reference:** `ShoppingCartFacadeImpl.readableShoppingCart()` lines 860-866; `getCart()` lines 734-751  
**Discovery Method:** Direct Source Read | CAST Imaging (Hybrid)  
**CAST Reference:** Transaction 244214 `/api/v1/cart/{code}`; object `getPopulatedItem` complexity 15

**Statement:** Every cart read used for display, total calculation, or checkout must re-resolve each SKU, restore valid attributes, calculate the current price, calculate the line subtotal, and mark the cart obsolete when no usable line remains.

**Intent:** Calculation / State Transition
**Classification:** Core
**Weight:** High

**Logic:**
```pseudocode
IF cart.lineItems is null OR cart.lineItems is empty:
    cart.obsolete = true
    RETURN cart

cartIsObsolete = false

FOR item IN cart.lineItems:
    product = productService.getBySku(item.sku, store, store.defaultLanguage)

    IF product == null:
        item.obsolete = true
        cartIsObsolete = true
        CONTINUE

    item.product = product
    item.sku = product.sku
    item.productVirtual = product.productVirtual

    validAttributes = []
    orphanAttributes = []

    FOR cartAttribute IN item.attributes:
        matchingAttribute = product.attributes.findById(cartAttribute.productAttributeId)
        IF matchingAttribute exists:
            cartAttribute.productAttribute = matchingAttribute
            validAttributes.add(matchingAttribute)
        ELSE:
            orphanAttributes.add(cartAttribute)

    FOR orphan IN orphanAttributes:
        shoppingCartAttributeItemRepository.delete(orphan)

    IF validAttributes is empty:
        item.attributes = null

    finalPrice = pricingService.calculateProductPrice(product, validAttributes)
    item.itemPrice = finalPrice.finalPrice
    item.finalPrice = finalPrice
    item.subTotal = item.itemPrice * item.quantity

update(cart)

IF cartIsObsolete:
    cart.obsolete = true
```

**Data Dependencies:**
- Reads: `SHOPPING_CART.SHP_CART_CODE`, `SHOPPING_CART.ORDER_ID`, `SHOPPING_CART_ITEM.SKU`, `SHOPPING_CART_ITEM.QUANTITY`, `SHOPPING_CART_ATTR_ITEM.PRODUCT_ATTR_ID`, product and price data
- Writes: transient `SHOPPING_CART_ITEM.ITEM_PRICE`, `SHOPPING_CART_ITEM.SUB_TOTAL`, `SHOPPING_CART_ITEM.OBSOLETE`; deletes orphan attribute rows; updates cart

**Side Effects:**
- Calls MS-02 and MS-07.
- Mutates persistence during a read.
- Removes obsolete carts through the caller.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 15 | 14 | OK |
| Data-flow | 12 | 12 | OK |
| Constants | 0 | 0 | N/A |
| State transitions | 4 | 4 | OK |
| Outcomes | 4 | 4 | OK |
| Data writes | 4 | 4 | OK |
| Integrations | 3 | 3 | OK |
| Error paths | 3 | 3 | OK |

**Preservation:** OK

**Concrete Example:**
- API Input: `GET /api/v1/cart/a91b8c7d2e`
- Success Output: `200`  
  `{"cart":{"items":[{"sku":"SKU-RED-42","quantity":2,"unitPrice":"19.99","subTotal":"39.98"}],"status":"Open"}}`
- Error Input: cart contains only `SKU-DELETED`
- Error Output: `404`  
  `{"error":"CART_NOT_AVAILABLE","message":"Cart a91b8c7d2e has no sellable items","statusCode":404}`

---

### BR-SC-MRG-007: Anonymous and customer carts are merged only within the same tenant and store

**Source Reference:** `ShoppingCartServiceImpl.mergeShoppingCarts()` lines 390-438; `getShoppingCartItems()` lines 440-489  
**Cross-Reference:** `ShoppingCartApi.getByCustomer()` lines 251-282; `ShoppingCartFacadeImpl.addToCart(Customer,...)` lines 1024-1043  
**Discovery Method:** Direct Source Read | CAST Imaging (Hybrid)  
**CAST Reference:** Transaction 244217 authenticated cart; Transaction 244210 anonymous cart

**Statement:** When an authenticated customer adopts an anonymous cart, only carts from the same tenant and store may be merged; existing customer lines are preserved, compatible duplicates are combined, and the anonymous cart is removed after a successful merge.

**Intent:** Authorization / State Transition
**Classification:** Core
**Weight:** High

**Logic:**
```pseudocode
ASSERT sessionCart.storeId == customerCart.storeId
ASSERT sessionCart.tenantId == customerCart.tenantId

IF sessionCart.customerId == customerCart.customerId
   AND both carts have items:
    RETURN customerCart

sessionItems = rehydrateSessionItems(sessionCart, store)

FOR sessionItem IN sessionItems:
    duplicateFound = false

    FOR customerItem IN customerCart.lineItems:
        IF customerItem.product.id == sessionItem.product.id
           AND customerItem.attributes are compatible:
            customerItem.quantity += sessionItem.quantity
            duplicateFound = true
            BREAK

    IF duplicateFound == false:
        customerCart.lineItems.add(sessionItem)

persist customerCart
delete sessionCart
RETURN customerCart
```

**Data Dependencies:**
- Reads: `SHOPPING_CART.CUSTOMER_ID`, `SHOPPING_CART.MERCHANT_ID`, `SHOPPING_CART_ITEM.SKU`, `SHOPPING_CART_ITEM.QUANTITY`, cart attributes
- Writes: customer cart line quantities and associations
- Deletes: anonymous `SHOPPING_CART` after successful merge

**Side Effects:**
- Calls MS-01 for authenticated identity context.
- Rehydrates products through MS-02.
- Deletes the consumed anonymous cart.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 9 | 9 | OK |
| Data-flow | 8 | 8 | OK |
| Constants | 0 | 0 | N/A |
| State transitions | 3 | 3 | OK |
| Outcomes | 4 | 4 | OK |
| Data writes | 3 | 3 | OK |
| Integrations | 2 | 2 | OK |
| Error paths | 2 | 2 | OK |

**Preservation:** OK

**Concrete Example:**
- API Input: authenticated `GET /api/v1/auth/customer/cart?anonymousCartCode=a91b8c7d2e`
- Success Output: `200`  
  `{"cart":{"customerId":"cust-1001","items":[{"sku":"SKU-RED-42","quantity":5}]}}`
- Error Input: anonymous cart belongs to `tenant-2`, authenticated request carries `x-tenant-id: tenant-1`
- Error Output: `403`  
  `{"error":"CART_SCOPE_MISMATCH","message":"Cart cannot be merged across tenant or store boundaries","statusCode":403}`

---

## Shipping and totals

### BR-SC-SHP-008: Only physical shippable lines enter shipping calculation

**Source Reference:** `ShoppingCartServiceImpl.createShippingProduct()` lines 361-383; `OrderFacadeImpl.getShippingQuote()` lines 1151-1189  
**Cross-Reference:** `OrderShippingApi.shipping()` lines 79-177 and 190-289  
**Discovery Method:** Direct Source Read | CAST Imaging (Hybrid)  
**CAST Reference:** Transaction 244101 authenticated shipping; Transaction 244102 anonymous shipping

**Statement:** A shipping request must include only lines that are both non-virtual and shippable; a cart containing no such lines must not request a carrier quote.

**Intent:** Routing
**Classification:** Core
**Weight:** High

**Logic:**
```pseudocode
shippingProducts = null

FOR item IN cart.lineItems:
    product = item.product
    IF product.productVirtual == false
       AND product.productShipeable == true:
        IF shippingProducts == null:
            shippingProducts = []
        shippingProducts.add(
            ShippingProduct(
                product = product,
                quantity = item.quantity,
                finalPrice = item.finalPrice
            )
        )

IF shippingProducts is empty:
    RETURN no-shipping-required
```

**Data Dependencies:**
- Reads: `SHOPPING_CART_ITEM.QUANTITY`, product virtual and shippable flags, calculated price
- Writes: target quote request only; no cart table write

**Side Effects:**
- Calls MS-09 shipping quote orchestration.
- Does not call shipping providers directly.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 5 | 5 | OK |
| Data-flow | 5 | 5 | OK |
| Constants | 0 | 0 | N/A |
| State transitions | 1 | 1 | OK |
| Outcomes | 2 | 2 | OK |
| Data writes | 1 | 1 | OK |
| Integrations | 1 | 1 | OK |
| Error paths | 1 | 1 | OK |

**Preservation:** OK

**Concrete Example:**
- API Input: `POST /api/v1/cart/a91b8c7d2e/shipping`  
  `{"postalCode":"H2Y 1C6","countryCode":"CA"}`
- Success Output: `200`  
  `{"shippingRequired":false,"options":[],"quoteId":null}`
- Error Input: no physical line exists but a carrier option is submitted to checkout
- Error Output: `422`  
  `{"error":"SHIPPING_NOT_REQUIRED","message":"A shipping option cannot be selected for a digital-only cart","statusCode":422}`

---

### BR-SC-SHP-009: Shipping address selection uses delivery data and falls back to billing data

**Source Reference:** `OrderFacadeImpl.getShippingQuote(Customer, ShoppingCart, MerchantStore, Language)` lines 1151-1189; legacy v0 path lines 974-1004; `OrderShippingApi.shipping()` lines 94-120 and 207-232  
**Discovery Method:** Direct Source Read | CAST Imaging (Hybrid)  
**CAST Reference:** Transactions 244101 and 244102; shipping object `getShippingQuote` complexity 69

**Statement:** For a physical cart, the quote request must use the customer's delivery address when it has a postal code; otherwise it must use the billing address, and an anonymous request must construct delivery data from the supplied postal and country values.

**Intent:** Routing / Validation
**Classification:** Core
**Weight:** High

**Logic:**
```pseudocode
shippingProducts = createShippingProduct(cart)
IF shippingProducts is empty:
    RETURN null

IF customer.delivery != null
   AND customer.delivery.postalCode is not blank:
    delivery = customer.delivery
ELSE:
    billing = customer.billing when present
    delivery = copy address, city, company, postalCode, state, country, zone from billing

IF anonymous request:
    delivery.postalCode = request.address.postalCode
    delivery.country = countryService.getByCode(request.address.countryCode)
    IF delivery.country == null:
        delivery.country = store.country

quote = shippingService.getShippingQuote(
    cart.id, store, delivery, shippingProducts, language
)
```

**Data Dependencies:**
- Reads: customer billing/delivery fields, `SHOPPING_CART.SHP_CART_ID`, product shipping facts
- Writes: `cart_quote_reference` and quote snapshot in target
- Reads external quote result from MS-09

**Side Effects:**
- Calls MS-09.
- Hidden rules in `ShippingDecision.drl`, `PriceByDistance.drl`, and `PriceByDistance2.drl` remain MS-09/MS-12 responsibilities.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 9 | 9 | OK |
| Data-flow | 9 | 9 | OK |
| Constants | 1 | 1 | OK |
| State transitions | 1 | 1 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 1 | 1 | OK |
| Integrations | 3 | 3 | OK |
| Error paths | 2 | 2 | OK |

**Preservation:** OK

**Concrete Example:**
- API Input: `POST /api/v1/cart/a91b8c7d2e/shipping`  
  `{"postalCode":"H2Y 1C6","countryCode":"CA"}`
- Success Output: `200`  
  `{"quoteId":"quote-701","delivery":{"postalCode":"H2Y 1C6","countryCode":"CA"},"options":[{"code":"canadapost","price":"12.00"}]}`
- Error Input: `{"postalCode":"","countryCode":"ZZ"}`
- Error Output: `422`  
  `{"error":"SHIPPING_ADDRESS_INVALID","message":"A postal code and supported country are required for shipping quotes","statusCode":422}`

---

### BR-SC-TOT-010: Cart totals are recomputed from current line prices and service allocations

**Source Reference:** `OrderServiceImpl.caculateOrder()` lines 217-394; `ShoppingCartCalculationServiceImpl.calculate()` lines 65-109  
**Cross-Reference:** `OrderTotalApi.calculateTotal()` lines 165-223; `OrderFacadeImpl.processOrder()` lines 1261-1294  
**Discovery Method:** Direct Source Read | CAST Imaging (Hybrid)  
**CAST Reference:** Transaction 244105 totals; Transaction 244106 totals; object `caculateOrder` complexity 22

**Statement:** The current checkout total is the sum of line subtotals, applicable price variations, shipping, eligible handling, tax, and the final total; the result must be returned with each component identified.

**Intent:** Calculation
**Classification:** Core
**Weight:** High

**Logic:**
```pseudocode
subTotal = 0
grandTotal = 0
variationLines = []
otherPriceTotals = map()

FOR item IN summary.products:
    lineSubtotal = item.itemPrice * item.quantity
    item.subTotal = lineSubtotal
    subTotal += lineSubtotal

    FOR price IN item.finalPrice.additionalPrices:
        IF price.defaultPrice == false:
            otherPriceTotals[price.productPrice.code] += price.finalPrice
            IF price.productPrice.productPriceType == ONE_TIME:
                subTotal += price.finalPrice

IF summary.type IN {ORDERTOTAL, SHOPPINGCART}:
    variations = orderTotalService.findOrderTotalVariation(...)
    sortOrder = 10
    FOR variation IN variations:
        variation.sortOrder = sortOrder
        sortOrder += 1
        variationLines.add(variation)
        subTotal -= variation.value

grandTotal = subTotal
append subtotal line

IF summary.shippingSummary != null:
    IF shippingSummary.freeShipping:
        shippingValue = 0
    ELSE:
        shippingValue = shippingSummary.shipping
    grandTotal += shippingValue
    append shipping line

    IF shippingSummary.handling > 0
       AND shippingConfiguration.handlingFees > 0:
        grandTotal += shippingSummary.handling
        append handling line

taxes = taxService.calculateTax(summary, customer, store, language)
totalTaxes = sum(tax.itemPrice for tax in taxes)
grandTotal += totalTaxes
append each tax line

append grand-total line with grandTotal
RETURN subtotal, taxTotal, totals, total
```

**Data Dependencies:**
- Reads: `SHOPPING_CART_ITEM.ITEM_PRICE`, `SHOPPING_CART_ITEM.QUANTITY`, price allocations, shipping quote, tax quote
- Writes: calculated line subtotals and `checkout_total_snapshot`
- No direct writes to MS-05 `ORDER_TOTAL`

**Side Effects:**
- Calls MS-07, MS-08, and MS-09.
- `ProcessorsConfiguration` activates `PromoCodeCalculatorModule`.
- `PromoCoupon.drl` currently grants 10% for `Test1234` before `31-Oct-2025`; this is recorded as provider-owned promotion behavior, not duplicated in MS-04.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 18 | 17 | OK |
| Data-flow | 15 | 15 | OK |
| Constants | 8 | 8 | OK |
| State transitions | 1 | 1 | OK |
| Outcomes | 7 | 7 | OK |
| Data writes | 3 | 3 | OK |
| Integrations | 4 | 4 | OK |
| Error paths | 2 | 2 | OK |

**Preservation:** OK

**Concrete Example:**
- API Input: `GET /api/v1/cart/a91b8c7d2e/total?quote=quote-701`
- Success Output: `200`  
  `{"currency":"CAD","subTotal":"39.98","discountTotal":"4.00","shipping":"12.00","handling":"2.00","tax":"6.50","grandTotal":"56.48","quoteVersion":3}`
- Error Input: quote belongs to another store
- Error Output: `404`  
  `{"error":"QUOTE_NOT_FOUND","message":"Shipping quote quote-701 is not available in this store","statusCode":404}`

---

### BR-SC-PRO-011: A cart promotion is retained for less than one calendar day

**Source Reference:** `ShoppingCartFacadeImpl.modifyCart(String,String,...)` lines 1150-1160; `getByCode()` lines 1107-1132; `OrderServiceImpl.caculateShoppingCart()` lines 430-461  
**Cross-Reference:** `ShoppingCartFacadeImpl.modifyCart()` lines 931-935 and `ShoppingCartApi` lines 118-146  
**Discovery Method:** Direct Source Read | CAST Imaging (Hybrid)  
**CAST Reference:** Transaction 244212 `/api/v1/cart/{code}/promo/{code}`; hidden engine `PromoCoupon.drl`

**Statement:** A promotion code is associated with the cart at the time it is added and is eligible for calculation only while its recorded calendar date is before tomorrow; an expired code must be cleared before the next cart total is calculated.

**Intent:** Validation / State Transition
**Classification:** Core
**Weight:** High

**Logic:**
```pseudocode
cart.promoCode = request.promoCode
cart.promoAdded = now
persist cart

promoDateAdded = cart.promoAdded
IF promoDateAdded == null:
    promoDateAdded = now

dateAdded = localDate(promoDateAdded)
tomorrow = localDate(now) + 1 day

IF dateAdded < tomorrow:
    orderSummary.promoCode = cart.promoCode
ELSE:
    cart.promoCode = null
    saveOrUpdate(cart)
```

**Data Dependencies:**
- Reads: `SHOPPING_CART.PROMO_CODE`, `SHOPPING_CART.PROMO_ADDED`
- Writes: `SHOPPING_CART.PROMO_CODE`, `SHOPPING_CART.PROMO_ADDED`
- Reads promotion allocation from MS-07

**Side Effects:**
- Cart calculation may mutate the cart by clearing an expired promotion.
- Does not own promotion eligibility or discount-rate algorithms.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 7 | 7 | OK |
| Data-flow | 4 | 4 | OK |
| Constants | 2 | 2 | OK |
| State transitions | 2 | 2 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 3 | 3 | OK |
| Integrations | 2 | 2 | OK |
| Error paths | 1 | 1 | OK |

**Preservation:** OK

**Concrete Example:**
- API Input: `POST /api/v1/cart/a91b8c7d2e/promo/Test1234`
- Success Output: `201`  
  `{"cart":{"promoCode":"Test1234","promoAdded":"2026-09-01T12:00:00Z"}}`
- Error Input: a cart whose `promoAdded` date is `2026-08-30`
- Error Output: `422`  
  `{"error":"PROMOTION_EXPIRED","message":"Promotion Test1234 is no longer valid for this cart","statusCode":422}`

---

## Checkout orchestration

### BR-CO-AUT-012: Authenticated checkout may use only the authenticated customer's cart

**Source Reference:** `OrderApi.checkout()` lines 343-392; `OrderTotalApi.payment()` lines 83-144; `OrderShippingApi.shipping()` lines 87-177; `OrderPaymentApi.init()` lines 120-181  
**Discovery Method:** Direct Source Read | CAST Imaging (Hybrid)  
**CAST Reference:** Transactions 244089, 244094, 244101, and 244105

**Statement:** An authenticated operation must resolve the customer from the authenticated principal and must not expose or mutate a cart whose customer association differs from that principal.

**Intent:** Authorization
**Classification:** Core
**Weight:** High

**Logic:**
```pseudocode
principal = request.userPrincipal
customer = customerService.getByNick(principal.name)

IF customer == null:
    REJECT 401

cart = shoppingCartService.getByCode(code, merchantStore)

IF cart == null:
    REJECT 404

IF cart.customerId is null OR cart.customerId != customer.id:
    REJECT 404
```

**Data Dependencies:**
- Reads: `SHOPPING_CART.SHP_CART_CODE`, `SHOPPING_CART.CUSTOMER_ID`, `SHOPPING_CART.MERCHANT_ID`
- Reads identity from MS-01
- Writes only after authorization succeeds

**Side Effects:**
- No cart mutation on authorization failure.
- Target additionally verifies `tenant_id` and `store_id`.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 7 | 7 | OK |
| Data-flow | 5 | 5 | OK |
| Constants | 2 | 2 | OK |
| State transitions | 0 | 0 | N/A |
| Outcomes | 4 | 4 | OK |
| Data writes | 0 | 0 | N/A |
| Integrations | 1 | 1 | OK |
| Error paths | 3 | 3 | OK |

**Preservation:** OK

**Concrete Example:**
- API Input: `POST /api/v1/auth/cart/a91b8c7d2e/checkout` with a valid authenticated principal for customer `cust-1001`
- Success Output: `202`  
  `{"submissionId":"sub-9001","state":"Submitted"}`
- Error Input: same request where the cart belongs to `cust-2002`
- Error Output: `404`  
  `{"error":"CART_NOT_FOUND","message":"Cart is not available to this customer","statusCode":404}`

---

### BR-CO-CUS-013: Anonymous checkout must construct customer and address context from the request

**Source Reference:** `OrderApi.checkout()` lines 402-471; `PersistableOrderApiPopulator.populate()` lines 69-157  
**Cross-Reference:** `OrderFacadeImpl.processOrder()` lines 1196-1217  
**Discovery Method:** Direct Source Read | CAST Imaging (Hybrid)  
**CAST Reference:** Transaction 244090 anonymous checkout

**Statement:** An anonymous checkout must provide customer details and may create a customer account only when credentials are supplied and the email is not already registered in the store.

**Intent:** Validation / Authorization
**Classification:** Core
**Weight:** High

**Logic:**
```pseudocode
ASSERT request.customer exists

IF request.customer.password is not blank:
    credentialsService.validateCredentials(
        request.customer.password,
        request.customer.repeatPassword,
        merchantStore,
        language
    )

customer = customerFacade.populateCustomerModel(
    new Customer(),
    request.customer,
    merchantStore,
    language
)

IF request.customer.password is not blank:
    customer.anonymous = false
    customer.nick = customer.emailAddress

    IF customerFacadev1.checkIfUserExists(customer.nick, merchantStore):
        REJECT 409 "Customer with email ... is already registered"

order.shoppingCartId = cart.id
process checkout using customer context
```

**Data Dependencies:**
- Reads: request customer fields; `customer.EMAIL_ADDRESS`; store registration data
- Writes: target customer reference in checkout session; customer creation remains MS-01-owned

**Side Effects:**
- Calls MS-01 customer and credential services.
- Does not create customer records directly in MS-04.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 8 | 8 | OK |
| Data-flow | 7 | 7 | OK |
| Constants | 2 | 2 | OK |
| State transitions | 2 | 2 | OK |
| Outcomes | 4 | 4 | OK |
| Data writes | 2 | 2 | OK |
| Integrations | 2 | 2 | OK |
| Error paths | 3 | 3 | OK |

**Preservation:** OK

**Concrete Example:**
- API Input: `POST /api/v1/cart/a91b8c7d2e/checkout`  
  `{"customer":{"email":"new@example.test","password":"Secret123!","repeatPassword":"Secret123!","billing":{"firstName":"Ada","lastName":"Lovelace","address":"1 Main St","city":"Montreal","country":"CA","postalCode":"H2Y 1C6","phone":"5145550100"}},"currency":"CAD","payment":{"paymentType":"CREDITCARD","paymentModule":"stripe","amount":"56.48"}}`
- Success Output: `202`  
  `{"submissionId":"sub-9001","state":"Submitted","customerId":"cust-3001"}`
- Error Input: same request with an email already registered
- Error Output: `409`  
  `{"error":"CUSTOMER_ALREADY_REGISTERED","message":"Customer with email new@example.test is already registered","statusCode":409}`

---

### BR-CO-SNP-014: Checkout freezes a server-derived snapshot before submission

**Source Reference:** `OrderFacadeImpl.processOrder()` lines 1208-1328; `PersistableOrderApiPopulator.populate()` lines 82-157; `OrderProductPopulator.populate()` lines 60-153  
**Cross-Reference:** `OrderApi.checkout()` lines 348-380 and 407-454  
**Discovery Method:** Direct Source Read | CAST Imaging (Hybrid)  
**CAST Reference:** Transaction 244089 checkout graph; object `processOrder` complexity 24; object `populate` complexity 46

**Statement:** Checkout must freeze the current cart into an immutable snapshot containing the resolved SKU, product name, quantity, selected attributes, current price allocations, currency, addresses, shipping reference, and calculated totals before publication to MS-05.

**Intent:** State Transition / Compliance
**Classification:** Core
**Weight:** Critical

**Logic:**
```pseudocode
modelOrder = populate request-level order fields
cart = shoppingCartService.getById(request.shoppingCartId, store)

IF cart == null:
    REJECT "Shopping cart ... does not exist"

FOR cartItem IN cart.lineItems:
    orderProduct = new OrderProduct()
    orderProduct = orderProductPopulator.populate(cartItem, orderProduct, store, language)
    orderProduct.order = modelOrder
    snapshot.lines.add(orderProduct)

IF request.shippingQuote exists:
    shippingSummary = shippingQuoteService.getShippingSummary(request.shippingQuote, store)
    modelOrder.shippingModuleCode = shippingSummary.shippingModule

orderSummary.products = current cart line items
orderSummary.shippingSummary = shippingSummary
calculatedTotal = orderService.caculateOrderTotal(orderSummary, customer, store, language)

persist checkout_line_snapshot
persist checkout_total_snapshot
transition checkout_session from Quoted to Frozen
```

**Data Dependencies:**
- Reads: `SHOPPING_CART`, `SHOPPING_CART_ITEM`, `SHOPPING_CART_ATTR_ITEM`, product, price, shipping quote
- Writes: `checkout_session`, `checkout_line_snapshot`, `checkout_total_snapshot`
- Does not write `ORDERS` or `ORDER_PRODUCT` in target architecture

**Side Effects:**
- Calls MS-02, MS-07, MS-08, and MS-09.
- Publishes no event until the local freeze and submission transaction succeeds.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 14 | 14 | OK |
| Data-flow | 16 | 16 | OK |
| Constants | 4 | 4 | OK |
| State transitions | 4 | 4 | OK |
| Outcomes | 4 | 4 | OK |
| Data writes | 7 | 7 | OK |
| Integrations | 5 | 5 | OK |
| Error paths | 3 | 3 | OK |

**Preservation:** OK

**Concrete Example:**
- API Input: `POST /api/v1/cart/a91b8c7d2e/checkout`  
  `{"currency":"CAD","shippingQuoteId":"quote-701","payment":{"paymentType":"CREDITCARD","paymentModule":"stripe","amount":"56.48"},"customer":{"email":"ada@example.test"}}`
- Success Output: `202`  
  `{"submissionId":"sub-9001","state":"Submitted","snapshot":{"lineCount":1,"grandTotal":"56.48","currency":"CAD"}}`
- Error Input: cart price changed after the quote was created
- Error Output: `409`  
  `{"error":"QUOTE_STALE","message":"Cart totals changed; recalculate before submitting checkout","statusCode":409}`

---

### BR-CO-TOT-015: The submitted payment amount must equal the server calculation

**Source Reference:** `OrderFacadeImpl.processOrder()` lines 1261-1294; `PersistablePaymentPopulator.populate()` lines 25-50  
**Cross-Reference:** `OrderApi.checkout()` lines 348-380 and 407-454  
**Discovery Method:** Direct Source Read | CAST Imaging (Hybrid)  
**CAST Reference:** Transaction 244089 checkout; Transaction 244090 anonymous checkout

**Statement:** Checkout submission is accepted only when the caller's amount, normalized to the store's currency precision, exactly equals the server-calculated grand total for the current cart and selected shipping quote.

**Intent:** Validation / Compliance
**Classification:** Core
**Weight:** Critical

**Logic:**
```pseudocode
IF request.payment.amount is null:
    REJECT "Requires Payment.amount"

submittedAmount = productPriceUtils.getAmount(request.payment.amount)
calculatedAmount = orderTotalSummary.total

IF calculatedAmount.compareTo(submittedAmount) != 0:
    REJECT:
      "Payment.amount does not match what the system has calculated
       {calculatedAmount} (received {submittedAmount})"

checkout_total_snapshot.grand_total = calculatedAmount
```

**Data Dependencies:**
- Reads: request payment amount, `checkout_total_snapshot.GRAND_TOTAL`, currency precision from MS-07/MS-11
- Writes: frozen total snapshot
- No client amount is persisted as authoritative

**Side Effects:**
- Calls MS-07 amount normalization.
- Blocks payment handoff when values differ.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 4 | 4 | OK |
| Data-flow | 5 | 5 | OK |
| Constants | 1 | 1 | OK |
| State transitions | 1 | 1 | OK |
| Outcomes | 3 | 3 | OK |
| Data writes | 1 | 1 | OK |
| Integrations | 1 | 1 | OK |
| Error paths | 2 | 2 | OK |

**Preservation:** OK

**Concrete Example:**
- API Input: `POST /api/v1/cart/a91b8c7d2e/checkout` with payment amount `"56.48"` and server total `"56.48"`
- Success Output: `202`  
  `{"submissionId":"sub-9001","state":"Submitted","amount":"56.48","currency":"CAD"}`
- Error Input: same payload with payment amount `"55.48"`
- Error Output: `409`  
  `{"error":"AMOUNT_MISMATCH","message":"Submitted amount 55.48 does not match calculated total 56.48","statusCode":409}`

---

### BR-CO-PAY-016: Payment handoff uses the configured active payment method

**Source Reference:** `OrderPaymentApi.init()` lines 88-181; `PersistablePaymentPopulator.populate()` lines 25-50; `PaymentServiceImpl.processPayment()` lines 299-393  
**Cross-Reference:** `OrderFacadeImpl.processOrder()` lines 1308-1328  
**Discovery Method:** Direct Source Read | CAST Imaging (Hybrid)  
**CAST Reference:** Transactions 244093 and 244094 payment initialization; payment configuration graph

**Statement:** MS-04 may hand off a payment request only after the selected payment method is configured and active for the store; provider selection, provider credentials, transaction state, and authorization results belong to MS-06.

**Intent:** Routing / Authorization
**Classification:** Core
**Weight:** Critical

**Logic:**
```pseudocode
payment.amount = pricingService.getAmount(request.amount)
payment.moduleName = request.paymentModule
payment.paymentType = PaymentType.valueOf(request.paymentType)
payment.transactionType = TransactionType.valueOf(request.transactionType)
payment.paymentMetaData["paymentToken"] = request.paymentToken

configuration = paymentConfiguration(store, payment.moduleName)

IF configuration map is null:
    REJECT "No payment module configured"

IF configuration[payment.moduleName] is null:
    REJECT "Payment module ... is not configured"

IF configuration[payment.moduleName].active == false:
    REJECT "Payment module ... is not active"

publish PaymentRequested with:
    submissionId, amount, currency, paymentType,
    paymentModule, token reference, tenantId, storeId
```

**Data Dependencies:**
- Reads: store payment configuration; `checkout_total_snapshot.GRAND_TOTAL`; payment request fields
- Writes: `checkout_submission.PAYMENT_METHOD`, `checkout_submission.PAYMENT_AMOUNT`
- Does not write `SM_TRANSACTION`

**Side Effects:**
- Calls MS-06 through `PaymentRequested`.
- Payment tokens must be redacted from logs and stored only as provider-safe references.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 11 | 11 | OK |
| Data-flow | 9 | 9 | OK |
| Constants | 3 | 3 | OK |
| State transitions | 2 | 2 | OK |
| Outcomes | 5 | 5 | OK |
| Data writes | 3 | 3 | OK |
| Integrations | 3 | 3 | OK |
| Error paths | 4 | 4 | OK |

**Preservation:** OK

**Concrete Example:**
- API Input: `POST /api/v1/cart/a91b8c7d2e/payment/init`  
  `{"amount":"56.48","paymentModule":"stripe","paymentType":"CREDITCARD","transactionType":"INIT","paymentToken":"tok_test_123"}`
- Success Output: `202`  
  `{"submissionId":"sub-9001","paymentState":"Pending","providerReference":"payref-701"}`
- Error Input: `{"amount":"56.48","paymentModule":"disabled-provider","paymentType":"CREDITCARD","transactionType":"INIT","paymentToken":"tok_test_123"}`
- Error Output: `422`  
  `{"error":"PAYMENT_METHOD_INACTIVE","message":"Payment module disabled-provider is not active for this store","statusCode":422}`

---

### BR-CO-IDM-017: Checkout submission must be idempotent

**Source Reference:** No legacy implementation found after search for `idempot`, `Idempot`, request-key headers, and idempotency tables across the inspected Java/configuration source. The legacy checkout path is evidenced by `OrderFacadeImpl.processOrder()` lines 1196-1359 and `OrderServiceImpl.process()` lines 127-214.  
**Discovery Method:** Direct Source Read plus source/configuration search  
**CAST Reference:** Transactions 244089 and 244090; hidden-engine check in `assessment/ms-04-cast-brief.md`

**Statement:** Every payment-sensitive checkout submission must carry a caller-supplied idempotency key scoped to tenant, store, customer, cart, and operation; a retry with the same key must return the original result without repeating payment, inventory, order-submission, or notification side effects.

**Intent:** Compliance / State Transition
**Classification:** Core
**Weight:** Critical

**Logic:**
```pseudocode
key = request.header["idempotency-key"]
scope = (tenantId, storeId, customerId, cartId, "checkout")

existing = checkout_idempotency_key.find(scope, key)

IF existing exists:
    IF existing.request_hash != hash(canonicalRequest):
        REJECT 409 "Idempotency key was reused with a different request"
    RETURN existing.original_status, existing.original_response

INSERT checkout_idempotency_key(scope, key, request_hash, state = InProgress)

execute local freeze and submission transaction

UPDATE checkout_idempotency_key
SET state = Completed,
    original_status = response.status,
    original_response = response.body

RETURN original response
```

**Data Dependencies:**
- Reads: `checkout_idempotency_key.TENANT_ID`, `STORE_ID`, `CUSTOMER_ID`, `CART_ID`, `IDEMPOTENCY_KEY`, `REQUEST_HASH`
- Writes: idempotency record and original response

**Side Effects:**
- Target-only rule; no equivalent legacy mechanism was found.
- Prevents duplicate calls to MS-05, MS-06, and MS-02.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 0 | 6 | Target addition |
| Data-flow | 0 | 6 | Target addition |
| Constants | 0 | 1 | Target addition |
| State transitions | 0 | 3 | Target addition |
| Outcomes | 0 | 3 | Target addition |
| Data writes | 0 | 3 | Target addition |
| Integrations | 0 | 3 | Target addition |
| Error paths | 0 | 2 | Target addition |

**Preservation:** Target addition — not legacy-preserved; required for safe modernization.

**Concrete Example:**
- API Input: first `POST /api/v1/cart/a91b8c7d2e/checkout` with header `idempotency-key: checkout-20260901-001`
- Success Output: `202`  
  `{"submissionId":"sub-9001","state":"Submitted"}`
- Error Input: retry with the same key but a different amount
- Error Output: `409`  
  `{"error":"IDEMPOTENCY_KEY_REUSED","message":"The idempotency key was already used with a different request","statusCode":409}`

---

### BR-CO-STA-018: Checkout uses an explicit closed lifecycle

**Source Reference:** Legacy cart completion is represented by `ShoppingCart.orderId` assignment in `OrderFacadeImpl.processOrder()` lines 1327-1336; legacy cart obsolescence is represented by `ShoppingCartServiceImpl.getPopulatedShoppingCart()` lines 235-264 and `getByCode()` lines 182-205. No explicit checkout-session state machine was found.  
**Discovery Method:** Direct Source Read plus CAST hidden-engine review  
**CAST Reference:** Checkout transactions 244089 and 244090

**Statement:** A checkout session must progress only through `Open`, `Quoted`, `Frozen`, `Submitted`, `Failed`, or `Expired`; `Submitted`, `Failed`, and `Expired` are terminal states and a terminal session cannot be reused.

**Intent:** State Transition
**Classification:** Core
**Weight:** High

**Logic:**
```pseudocode
Open -> Quoted:
    valid cart exists
    totals and any shipping quote are current

Quoted -> Frozen:
    cart version, quote versions, and total hash still match
    immutable line and total snapshots persisted

Frozen -> Submitted:
    idempotency record accepted
    OrderSubmitted outbox record persisted

Frozen -> Failed:
    permanent validation, payment-handoff, or dependency rejection

Open or Quoted -> Expired:
    now >= checkout_session.expires_at

Submitted, Failed, Expired:
    no outgoing transitions
    reject all further mutation and submission attempts
```

**Data Dependencies:**
- Reads: `checkout_session.STATE`, `checkout_session.EXPIRES_AT`, cart version, quote references
- Writes: `checkout_session.STATE`, `checkout_session.SUBMITTED_AT`, `checkout_session.FAILURE_CODE`

**Side Effects:**
- Target-only explicit lifecycle.
- Keeps order status transitions in MS-05 and payment state transitions in MS-06.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 2 | 8 | Target expansion |
| Data-flow | 3 | 7 | FLAGGED — explicit target state |
| Constants | 1 | 6 | Target expansion |
| State transitions | 2 | 8 | FLAGGED — legacy had implicit state |
| Outcomes | 3 | 5 | OK |
| Data writes | 2 | 5 | Target expansion |
| Integrations | 2 | 3 | OK |
| Error paths | 2 | 4 | OK |

**Preservation:** FLAGGED (state transitions, constants, data writes) — target lifecycle replaces implicit legacy markers; Phase 4a review required.

**Concrete Example:**
- API Input: `POST /api/v1/cart/a91b8c7d2e/checkout` with a current quote
- Success Output: `202`  
  `{"sessionId":"cs-7001","state":"Submitted"}`
- Error Input: resubmission using a session already in `Submitted`
- Error Output: `409`  
  `{"error":"CHECKOUT_TERMINAL","message":"Checkout session cs-7001 has already been submitted","statusCode":409}`

---

### BR-CO-ORC-019: Order submission is published only after local checkout persistence succeeds

**Source Reference:** `OrderFacadeImpl.processOrder()` lines 1300-1359; `OrderServiceImpl.process()` lines 127-214; `notify()` lines 1361-1380  
**Discovery Method:** Direct Source Read | CAST Imaging (Hybrid)  
**CAST Reference:** Transaction 244089 checkout graph, 3,245 nodes and 8,112 links

**Statement:** MS-04 must commit its cart/checkout freeze, total snapshot, idempotency record, and outbox entry locally before publishing the immutable order submission; MS-05, MS-06, and MS-02 then perform their owned work asynchronously with retries and compensation.

**Classification:** Core
**Weight:** High
**Logic:**
```pseudocode
BEGIN local MS-04 transaction

freeze cart and checkout snapshots
validate amount
create checkout_submission(state = Submitted)
create outbox_event(type = OrderSubmitted, payload = immutable snapshot)
mark checkout_session.state = Submitted
mark cart.status = Completed
store idempotency response

COMMIT

outbox publisher sends OrderSubmitted

MS-05 consumes OrderSubmitted and creates the order
MS-05 publishes PaymentRequested
MS-06 owns provider processing and payment events
MS-02 owns inventory reservation/decrement
MS-12 owns email/download delivery
```

**Data Dependencies:**
- Reads: cart, checkout session, line snapshot, total snapshot
- Writes: `checkout_submission`, `checkout_session`, `checkout_idempotency_key`, `ms04_outbox`
- Does not write `ORDERS`, `SM_TRANSACTION`, or product availability

**Side Effects:**
- Publishes `OrderSubmitted`.
- Consumes payment and inventory outcome events.
- Legacy provider call, order persistence, inventory mutation, cart completion, and email dispatch were not enclosed by one visible transaction; the target deliberately separates them.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 13 | 13 | OK |
| Data-flow | 18 | 18 | OK |
| Constants | 2 | 2 | OK |
| State transitions | 6 | 6 | OK |
| Outcomes | 6 | 6 | OK |
| Data writes | 9 | 9 | OK |
| Integrations | 7 | 7 | OK |
| Error paths | 4 | 4 | OK |

**Preservation:** OK with target boundary split

**Concrete Example:**
- API Input: `POST /api/v1/auth/cart/a91b8c7d2e/checkout` with valid frozen total and idempotency key
- Success Output: `202`  
  `{"submissionId":"sub-9001","state":"Submitted","eventId":"evt-9001"}`
- Error Input: outbox database unavailable during local transaction
- Error Output: `503`  
  `{"error":"CHECKOUT_UNAVAILABLE","message":"Checkout was not submitted because the durable submission record could not be stored","statusCode":503}`

---

## Boundary rules and exclusions

### BR-CO-BND-020: MS-04 does not own downstream order, payment, pricing, tax, shipping, or inventory state

**Source Reference:** `OrderFacadeImpl.processOrder()` lines 1222-1328; `OrderServiceImpl.process()` lines 146-210; `PaymentServiceImpl.processPayment()` lines 299-393  
**Discovery Method:** Direct Source Read | CAST Imaging (Hybrid)  
**CAST Reference:** Full checkout call graph 244089 / 244090

**Statement:** MS-04 may coordinate downstream capabilities, but it must not create or transition order records, mutate payment-provider transactions, calculate provider-owned prices or taxes, call shipping providers directly, or decrement inventory directly.

**Intent:** Authorization / Routing
**Classification:** Core
**Weight:** Critical

**Logic:**
```pseudocode
MS-04:
    owns cart and checkout tables
    calls provider contracts
    writes opaque quote references and immutable submission snapshots
    publishes OrderSubmitted

MS-05:
    owns ORDERS, ORDER_PRODUCT, ORDER_TOTAL, ORDER_STATUS_HISTORY

MS-06:
    owns SM_TRANSACTION and provider payment state

MS-07:
    owns product pricing and promotion allocation

MS-08:
    owns tax calculation

MS-09/MS-12:
    own shipping packaging, carrier selection, and provider calls

MS-02:
    owns product availability and atomic inventory reservation/decrement
```

**Data Dependencies:**
- Reads provider results through versioned contracts.
- Writes only MS-04-owned tables.

**Side Effects:**
- Prevents cross-service foreign keys and direct cross-schema writes.

**Semantic Preservation:**

| Dimension | Source | Spec | Status |
|---|---:|---:|---|
| Control-flow | 8 | 8 | OK |
| Data-flow | 17 | 17 | OK |
| Constants | 1 | 1 | OK |
| State transitions | 6 | 6 | OK |
| Outcomes | 5 | 5 | OK |
| Data writes | 9 | 9 | OK |
| Integrations | 8 | 8 | OK |
| Error paths | 3 | 3 | OK |

**Preservation:** OK with ownership correction

**Concrete Example:**
- API Input: `POST /api/v1/auth/cart/a91b8c7d2e/checkout`
- Success Output: `202`  
  `{"submissionId":"sub-9001","state":"Submitted","downstream":{"order":"Pending","payment":"Pending","inventory":"Pending"}}`
- Error Input: implementation attempts to write `SM_TRANSACTION`
- Error Output: `403`  
  `{"error":"OWNERSHIP_VIOLATION","message":"MS-04 cannot write payment-provider state","statusCode":403}`

---

## Source-derived preservation summary

| Rule | Source preservation |
|---|---|
| BR-SC-CRE-001 | OK |
| BR-SC-SEL-002 | OK |
| BR-SC-ATR-003 | OK |
| BR-SC-MRG-004 | OK |
| BR-SC-UPD-005 | OK |
| BR-SC-HYD-006 | OK |
| BR-SC-MRG-007 | OK |
| BR-SC-SHP-008 | OK |
| BR-SC-SHP-009 | OK |
| BR-SC-TOT-010 | OK |
| BR-SC-PRO-011 | OK |
| BR-CO-AUT-012 | OK |
| BR-CO-CUS-013 | OK |
| BR-CO-SNP-014 | OK |
| BR-CO-TOT-015 | OK |
| BR-CO-PAY-016 | OK with ownership boundary |
| BR-CO-IDM-017 | Target addition |
| BR-CO-STA-018 | FLAGGED for Phase 4a |
| BR-CO-ORC-019 | OK with target boundary split |
| BR-CO-BND-020 | OK with ownership correction |
