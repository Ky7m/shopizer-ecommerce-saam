# Phase 1 Table Write-Coverage Reconciliation

This is the reverse-direction CAST/Hybrid check: every business table with a legacy write path must
have at least one extracted writer. `EXTRACT` rows are proposed gaps requiring human confirmation before
Phase 1 can exit. `INFRA` rows are technical/audit/configuration writes proposed for exclusion.

| Business table | Writers observed | Extracted writer represented by BR | Proposed classification | Evidence / notes |
|---|---|---:|---|---|
| `PRODUCT` | `ProductServiceImpl`, product mapper/API | Yes | EXTRACT | Catalog rules BR-CAT-004, BR-CAT-017, BR-CAT-019 |
| `PRODUCT_AVAILABILITY` | `ProductServiceImpl`, inventory service, order decrement | Yes | EXTRACT | BR-CAT-004, BR-CAT-011; BR-ORD-012 |
| `PRODUCT_PRICE` | price services/product mapper | Yes | EXTRACT | BR-CAT-012-016; BR-PRC-001-005 |
| `PRODUCT_VARIANT` | variant service/API/mapper | Yes | EXTRACT | BR-CAT-002, BR-CAT-029-030 |
| `PRODUCT_CATEGORY` | product/category services | Yes | EXTRACT | BR-CAT-005, BR-CAT-008 |
| `PRODUCT_IMAGE` | product image service/content manager | Yes | EXTRACT | BR-CAT-017-019; BR-EXT-019-020 |
| `PRODUCT_RELATIONSHIP` | product service | Yes | EXTRACT | BR-CAT-019 |
| `SHOPPING_CART` | cart facade/service | Yes | EXTRACT | BR-ORD-001, BR-ORD-005, BR-ORD-007 |
| `SHOPPING_CART_ITEM` | cart facade/service | Yes | EXTRACT | BR-ORD-003-005 |
| `SHOPPING_CART_ATTR_ITEM` | cart service/attribute repository | Yes | EXTRACT | BR-ORD-004-005 |
| `ORDERS` | order facade/service | Yes | EXTRACT | BR-ORD-010-013 |
| `ORDER_PRODUCT` | order product populator/order service | Yes | EXTRACT | BR-ORD-011 |
| `ORDER_PRODUCT_ATTRIBUTE` | order product populator | Yes | EXTRACT | BR-ORD-011 |
| `ORDER_PRODUCT_PRICE` | order product populator | Yes | EXTRACT | BR-ORD-011 |
| `ORDER_PRODUCT_DOWNLOAD` | order product populator/download service | Yes | EXTRACT | BR-ORD-018 |
| `ORDER_STATUS_HISTORY` | order service/facade | Yes | EXTRACT | BR-ORD-013, BR-ORD-016-017 |
| `ORDER_TOTAL` | order total service/payment service | Yes | EXTRACT | BR-ORD-008-009, BR-ORD-017 |
| `SM_TRANSACTION` | payment/transaction services | Yes | EXTRACT | BR-ORD-014-017; BR-EXT-001-009 |
| `TAX_CLASS` | tax administration/service | Yes | EXTRACT | BR-PRC-014-021 |
| `TAX_RATE` | tax administration/service | Yes | EXTRACT | BR-PRC-014-021 |
| `SHIPPING_CONFIGURATION` | shipping configuration service | Yes | EXTRACT | BR-PRC-022, BR-PRC-029 |
| `SHIPPING_ORIGIN` | shipping facade/service | Yes | EXTRACT | BR-PRC-022 |
| `SHIPPING_QUOTE` | shipping service/quote service | Yes | EXTRACT | BR-PRC-027-028 |
| `SHIPPING_BOX` | packaging service/configuration | Yes | EXTRACT | BR-PRC-029-032 |
| `MERCHANT_STORE` | merchant store service/facade | Yes | EXTRACT | BR-MER-001-012 |
| `MERCHANT_LANGUAGE` | store populator/service | Yes | EXTRACT | BR-MER-004, BR-MER-012 |
| `CONTENT` | content service/facade | Yes | EXTRACT | BR-MER-013-018 |
| `CONTENT_DESCRIPTION` | content service/facade | Yes | EXTRACT | BR-MER-014-016 |
| `MERCHANT_CONFIGURATION` | configuration service/facade | Yes | EXTRACT | BR-MER-019-022 |
| `MODULE_CONFIGURATION` | module configuration service | Yes | EXTRACT | BR-MER-023-027 |
| `CUSTOMER` | customer service/facade | Yes | EXTRACT | BR-CUS-001-015 |
| `CUSTOMER_ATTRIBUTE` | customer populator/service | Yes | EXTRACT | BR-CUS-012-013 |
| `USER` | user facade/service | Yes | EXTRACT | BR-CUS-016-023 |
| `GROUP` / `PERMISSION` | group/user services | Yes | EXTRACT | BR-CUS-006-007, BR-CUS-016-022 |
| `CREDENTIALS_RESET` | customer reset flow | Yes | EXTRACT | BR-CUS-014-015 |
| Search index documents | search service/listener | Yes | EXTRACT | BR-CAT-020-024; BR-EXT-023-024 |
| CMS file/object storage | local/Infinispan/S3/GCP providers | Yes | EXTRACT | BR-EXT-019-021; BR-MER-017-018, BR-MER-027 |
| Email delivery | SMTP/SES sender | Yes | EXTRACT | BR-EXT-022; BR-ORD-018 |
| Audit/log tables | framework/service logging | No | INFRA | Technical/audit writes; exclude after human confirmation |

## Producer/Consumer Pairing Notes

- Product availability, cart items, order products, order totals, transactions, shipping quotes, content,
  and configuration all have both extracted producers and consumers in the current assessment set.
- Search documents and CMS files are external/non-relational write surfaces and are represented by integration rules.
- No business table is currently proposed as `DEAD`; CAST dead-code evidence was not available for individual writers.

## Human Confirmation Required

Please confirm each `EXTRACT`/`INFRA` classification before Phase 1 exit. Any row changed to `EXTRACT`
without an extracted writer remains an open Phase 1 gap and blocks the exit gate.
