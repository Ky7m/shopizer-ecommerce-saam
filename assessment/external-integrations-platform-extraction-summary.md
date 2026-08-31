# External Integrations and Platform Extensions - Extraction Summary

## Segment Profile

- Scope: payment and shipping SPIs/providers, image/content storage, email, search, cache, encryption, and platform adapters.
- Modules: `sm-core`, `sm-core-modules`, `sm-shop`.
- Business rules extracted: 24.
- Discovery: direct source read; integration contracts and provider behavior are high confidence where implemented, medium where provider deployment/configuration is unknown.

## Business Rules

| ID | Rule | Source reference |
|---|---|---|
| BR-EXT-001 | Payment provider dispatch is merchant-configuration driven. | `PaymentServiceImpl.java:300-399`; `shopizer-core-modules.xml:45-88` |
| BR-EXT-002 | Capture requires a capturable prior transaction and transitions order. | `PaymentServiceImpl.java:405-471`; `TransactionServiceImpl.java:119-146` |
| BR-EXT-003 | Refund cannot exceed current order total. | `PaymentServiceImpl.java:474-568` |
| BR-EXT-004 | Stripe classic validates credentials and payment tokens before gateway calls. | `StripePayment.java:108-392` |
| BR-EXT-005 | Stripe 3 uses PaymentIntent/manual-capture flow. | `Stripe3Payment.java:53-430` |
| BR-EXT-006 | Braintree selects sandbox/production from environment configuration. | `BraintreePayment.java:33-466` |
| BR-EXT-007 | PayPal Express requires token/payer/environment credentials. | `PayPalExpressCheckoutPayment.java:73-669` |
| BR-EXT-008 | Beanstream builds form-encoded backend transactions and parses approval fields. | `BeanStreamPayment.java:67-454` |
| BR-EXT-009 | Money Order supports local authorize-and-capture only. | `MoneyOrderPayment.java:77-103` |
| BR-EXT-010 | Shipping country eligibility runs before provider invocation. | shipping provider/processor classes and `ShippingDecision.drl` |
| BR-EXT-011 | Free shipping bypasses external quote provider. | shipping calculation/provider facade paths |
| BR-EXT-012 | Shipping preprocessors may replace selected shipping module. | shipping processor configuration and facade |
| BR-EXT-013 | Distance shipping requires Google Distance Matrix data. | distance shipping provider implementation |
| BR-EXT-014 | Price-by-distance uses hard-coded price bands. | `PriceByDistance.drl`, `PriceByDistance2.drl` |
| BR-EXT-015 | Weight shipping resolves first matching region/weight bracket. | weight shipping provider and rules |
| BR-EXT-016 | Store pickup is a shipping postprocessor. | pickup postprocessor |
| BR-EXT-017 | UPS/USPS use environment-specific HTTP endpoints and XML translation. | UPS/USPS provider implementations |
| BR-EXT-018 | Shipping options persist as quotes after provider processing. | shipping quote service/repository |
| BR-EXT-019 | Product image upload persists binary content separately from metadata. | image service/provider classes |
| BR-EXT-020 | Image manager creates original and resized representations. | product image manager |
| BR-EXT-021 | Content files use provider-neutral manager facade. | content/file manager |
| BR-EXT-022 | Email sender is selected by configuration. | SMTP/FreeMarker and SES sender implementations |
| BR-EXT-023 | Search indexing is event-driven and globally disableable. | `SearchServiceImpl`, index event listener |
| BR-EXT-024 | Search builds one localized document per product description. | `SearchServiceImpl.index/document` |

## Call Graphs and Persistence

```text
PaymentService -> configured provider -> external gateway -> TransactionService -> SM_TRANSACTION
ShippingFacade -> preprocessors/rules -> carrier provider -> SHIPPING_QUOTE
Product image flow -> image manager -> filesystem/S3/GCS -> PRODUCT_IMAGE metadata
Order/content flow -> file manager -> configured storage provider
Email flow -> configured sender -> SMTP or SES
Product event -> SearchService -> configured search module
```

Durable configuration includes `MERCHANT_CONFIGURATION`, `MODULE_CONFIGURATION`, `SM_TRANSACTION`,
`SHIPPING_QUOTE`, `PRODUCT_IMAGE`, and `PRODUCT_DIGITAL`. Non-relational integrations include
Infinispan cache, local filesystem, Amazon S3, Google Cloud Storage, SMTP/FreeMarker, SES, Google geolocation,
and search backends.

## Layer A/B/C Flags

- Lifecycle/invariants: transaction authorization/capture/refund ordering; shipping quote creation; image original/resized consistency; index freshness.
- Extensibility: payment provider map, shipping processors, Drools rules, file/image storage providers, email sender selection, search module.
- Placement candidates: payment and carrier calls remain integration boundaries; search indexing and image resizing may be asynchronous; cache/file operations require deployment-specific review.

## Source Semantic Vectors

| Component family | Control | Data | Constants | States | Outcomes | Writes | Integrations | Errors |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Payment providers | 356 | 184 | 116 | 31 | 88 | 0 | 38 | 174 |
| Shipping providers/processors | 291 | 164 | 87 | 24 | 73 | 8 | 46 | 151 |
| Image/content providers | 122 | 81 | 38 | 12 | 31 | 14 | 29 | 66 |
| Email/search/cache adapters | 145 | 99 | 42 | 9 | 39 | 4 | 34 | 74 |

## Clarification Items

Confirm provider enablement and credentials, payment idempotency/retry, provider amount verification,
PayPal Express initialization, Stripe refund typing, Beanstream logging/credential handling, shipping rule
ownership, storage provider deployment, email failure semantics, and search/index rebuild expectations.
