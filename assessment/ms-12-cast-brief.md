# MS-12 Platform Integrations - CAST Scout Brief

**Phase:** 4 CAST Scout
**Service:** MS-12 Platform Integrations
**Analysis mode:** Hybrid
**CAST application:** `Shopizer-Backend`
**CAST delivery:** `Onboarding-202511171247`
**Local source root:** `initial-source/shopizer-3.2.7/`

## Scope and ownership

MS-12 owns integration endpoint references, adapter dispatch, durable delivery
attempts, email and notification adapters, carrier adapters, maps/geolocation
calls, external file/content storage adapters, and event-driven delivery.

Provider-specific payment execution remains MS-06. Shipping policy, packaging,
rate selection, thresholds, and quote persistence remain MS-09. Merchant and
module configuration state remains MS-11. Product/image metadata remains MS-02.
Order lifecycle and entitlement decisions remain MS-05. MS-12 must tolerate
replayed events and must not write another service's schema.

## CAST queries executed

- Application inventory and domain transaction searches for order, content,
  file, shipping, system, and module flows.
- Full transaction call graphs with nodes and links.
- Complexity-ranked objects and inward/outward call graphs.
- Data graphs for configuration, shipping, content, system configuration, and
  S3 storage.
- Source-file resolution and transaction reachability for email, carrier,
  module, and storage adapters.

CAST returned no separately named integration, delivery, email, carrier,
webhook, or notification transactions. These capabilities are embedded in
order, customer, content, file, shipping, and system transactions.

## Entry points and critical transaction graphs

### Email and notification flows

| CAST transaction | Legacy operation | Nodes | Links | Observation |
|---:|---|---:|---:|---|
| 244089 | POST api/v1/auth/cart/{}/checkout/ | 3245 | 8112 | sendOrderEmail complexity 21 |
| 244090 | POST api/v1/cart/{}/checkout/ | 3262 | 8173 | Same notification path |
| 244235 | POST api/v1/contact/ | 240 | 402 | Default/SES sender dispatch |
| 244079 | POST api/v1/customer/password/reset/request/ | 326 | 575 | Password-reset email |
| 244245 | POST api/v1/user/password/reset/request/ | 298 | 526 | Password-reset email |
| 244012 | POST services/public/{}/contact/ | 292 | 498 | Public contact notification |

`EmailServiceImpl.sendHtmlEmail` and both default and SES senders are
reachable through the email transactions. Sender selection is configuration
driven and there is no durable legacy delivery-attempt table.

### Carrier and maps flows

| CAST transaction | Legacy operation | Nodes | Links | Observation |
|---:|---|---:|---:|---|
| 244101 | GET api/v1/auth/cart/{}/shipping/ | 1202 | 2783 | UPS/USPS and maps path |
| 244102 | POST api/v1/cart/{}/shipping/ | 1192 | 2778 | Same adapter path |
| 244208 | GET api/v1/shipping/country/ | 3020 | - | MS-09 policy context |

UPS `getShippingQuotes` has complexity 35 and USPS has complexity 34.
`ShippingDistancePreProcessorImpl` has complexity 17 and invokes external
Google Maps library objects. No local Google implementation exists.

### File and storage flows

| CAST transaction | Legacy operation | Nodes | Links | Scope |
|---:|---|---:|---:|---|
| 244042 | POST api/v1/private/content/images/rename/ | 236 | 585 | Storage adapter execution |
| 244065 | POST api/v1/private/file/ | 219 | 433 | Upload adapter path |
| 244066 | POST api/v1/private/files/ | 219 | - | Related upload path |
| 244293 | admin/files/downloads/{}/{}/ | 170 | - | Download read path |
| 244292 | static/files/{}/{}/ | 162 | - | Static file read |
| 244289 | static/files/{}/{}/{}/ | 156 | - | Static image/file read |

Rename policy and database metadata remain MS-11 concerns; provider execution
belongs behind the MS-12 adapter boundary.

### Module and adapter registry flow

| CAST transaction | Legacy operation | Nodes | Links | Observation |
|---:|---|---:|---:|---|
| 244013 | services/private/system/module/ | 113 | 158 | Adapter registry/loading |
| 244107 | GET api/v1/private/modules/payment/ | 205 | - | Discovery/configuration |
| 244108 | POST api/v1/private/modules/payment/ | 284 | - | Discovery/configuration |
| 244109 | GET api/v1/private/modules/payment/{}/ | 211 | - | Configuration read |
| 244204 | GET api/v1/private/modules/shipping/ | 200 | - | Adapter discovery |
| 244206 | POST api/v1/private/modules/shipping/ | 284 | - | Adapter configuration |
| 244205 | GET api/v1/private/modules/shipping/{}/ | 197 | - | Configuration read |

## Complexity-ranked hotspots

| Object | CAST ID | Complexity | Fan-out | Classification |
|---|---:|---:|---:|---|
| UPSShippingQuote.getShippingQuotes | 29070 | 35 | 123 | MS-12 carrier adapter |
| USPSShippingQuote.getShippingQuotes | 29071 | 34 | 126 | MS-12 carrier adapter |
| ShippingDistancePreProcessorImpl.prePostProcessShippingQuotes | 30319 | 17 | 68 | MS-12 maps adapter |
| ModuleConfigurationServiceImpl.getIntegrationModules | 13381 | 17 | 61 | MS-12 registry boundary |
| IntegrationModulesLoader.loadModule | 13191 | 17 | 46 | MS-12 module loading |
| EmailTemplatesUtils.sendOrderEmail | 30294 | 21 | - | MS-12 email mapping |
| ModuleConfigurationServiceImpl.createOrUpdateModule | 13382 | 5 | 18 | MS-12 replacement boundary |

## Source files to read

### Email and notification

- `sm-core/src/main/java/com/salesmanager/core/business/services/system/EmailServiceImpl.java` - configuration lookup and sender dispatch; 67 LOC.
- `sm-core/src/main/java/com/salesmanager/core/business/modules/email/DefaultEmailSenderImpl.java` - SMTP/FreeMarker sender; 188 LOC.
- `sm-core/src/main/java/com/salesmanager/core/business/modules/email/SESEmailSenderImpl.java` - AWS SES sender; 99 LOC.
- `sm-shop/src/main/java/com/salesmanager/shop/utils/EmailTemplatesUtils.java` - order/contact/password payload and template mapping; 497 LOC.

### Carrier and maps

- `sm-core/src/main/java/com/salesmanager/core/business/modules/integration/shipping/impl/UPSShippingQuote.java` - 692 LOC; HTTP/XML adapter and normalization.
- `sm-core/src/main/java/com/salesmanager/core/business/modules/integration/shipping/impl/USPSShippingQuote.java` - 744 LOC; HTTP/XML adapter and normalization.
- `sm-core/src/main/java/com/salesmanager/core/business/modules/integration/shipping/impl/ShippingDistancePreProcessorImpl.java` - 226 LOC; maps boundary.
- `sm-core/src/main/java/com/salesmanager/core/business/modules/utils/GeoLocationImpl.java` - local geolocation implementation.

### File and storage adapters

- `sm-core/src/main/java/com/salesmanager/core/business/modules/cms/content/StaticContentFileManagerImpl.java`
- `sm-core/src/main/java/com/salesmanager/core/business/modules/cms/content/local/CmsStaticContentFileManagerImpl.java`
- `sm-core/src/main/java/com/salesmanager/core/business/modules/cms/content/infinispan/CmsStaticContentFileManagerImpl.java`
- `sm-core/src/main/java/com/salesmanager/core/business/modules/cms/content/aws/S3StaticContentAssetsManagerImpl.java`
- `sm-core/src/main/java/com/salesmanager/core/business/modules/cms/content/gcp/GCPStaticContentAssetsManagerImpl.java`

These files are read for adapter contracts, key/path mapping, capabilities,
and failure behavior, not for MS-11 content policy.

### Adapter registry and loading

- `sm-core/src/main/java/com/salesmanager/core/business/services/system/ModuleConfigurationServiceImpl.java`
- `sm-core/src/main/java/com/salesmanager/core/business/services/reference/loader/IntegrationModulesLoader.java`
- `sm-shop/src/main/java/com/salesmanager/shop/store/api/v0/system/SystemRESTController.java`

## Source files to skip or treat as context-only

- MS-09 shipping policy, packaging, Drools rules, and quote selection classes.
- MS-06 payment provider implementations and payment lifecycle.
- MS-11 content, merchant configuration, and module configuration persistence.
- MS-02 product media metadata and product-media orchestration.
- DTO-only models, logging, JPA framework objects, `FilesController`, and
  generic infrastructure.
- External CAST `<LISA>` objects such as Google Maps, AWS, JavaMail, SES, and
  JDK classes. Do not fabricate local implementations.

## Data access and ownership

| Resource | CAST evidence | Target disposition |
|---|---|---|
| module_configuration | 57 nodes, 334 links | Owned by MS-11; consume API/event projection |
| merchant_configuration | 236 nodes, 1803 links | Owned by MS-11; consume references/secrets |
| shipping_quote | 4 nodes, 6 links | Owned by MS-09 |
| shiping_origin | 18 nodes, 77 links | Owned by MS-09 |
| content | 187 nodes, 985 links | Owned by MS-11/MS-02 boundary |
| shopizer-content S3 bucket | 87 nodes, 528 links | External resource behind MS-12 |

Target-only MS-12 tables are `integration_endpoint`, `delivery_attempt`, and
`email_message`. No legacy durable delivery or email-message table was found.

## Cross-service dependencies

| Direction | Boundary |
|---|---|
| MS-05 -> MS-12 | Order, invoice, fulfillment, and notification delivery events |
| MS-11 -> MS-12 | Configuration changes and adapter references |
| MS-09 -> MS-12 | Carrier quote request and response normalization |
| MS-08 -> MS-12 | Optional external tax-provider execution |
| MS-02 -> MS-12 | Product/media storage adapter requests |
| MS-12 -> SMTP/SES | Email delivery |
| MS-12 -> UPS/USPS | Carrier HTTP/XML APIs |
| MS-12 -> Google Maps | Distance/geolocation adapter |
| MS-12 -> filesystem/S3/GCP/Infinispan | File/content storage |
| MS-12 -> RabbitMQ/event infrastructure | Event consumption, outbox, replay, retry, dead letter |

## Existing P1 rules requiring P4 upgrade

`BR-MER-023` cache by category, `BR-MER-024` replacement by module code,
`BR-MER-025` JSON environment configuration, `BR-MER-026` config2 defect,
`BR-EXT-017` UPS/USPS endpoint translation, and `BR-EXT-022` email sender
selection require deep extraction of cache scope, replacement atomicity,
environment validation, provider timeout/retry, template rendering, durable
attempt state, and failure events.

## Dead-code exclusions

`ModulesApi` CAST object 29894 had zero transaction reachability and is excluded.
`SESEmailSenderImpl.send` remains in scope despite class-level reachability
noise because its method is reachable through seven email transactions. No
carrier or email adapter is excluded as dead. No local FedEx adapter was found.

## Hidden-engine check

MS-12 is not CRUD-only. The integration path contains 254 CAST objects across
19 files; UPS and USPS adapters have complexity 35/34 and more than 120
outgoing dependencies each; the maps adapter has complexity 17; registry and
module loading fan out to 61 and 46 objects; checkout email graphs contain
more than 3,200 nodes; and no legacy delivery-attempt table exists. The hidden
engine is adapter dispatch plus delivery reliability: retries, idempotency,
dead letters, replay, and provider outcome normalization.

## Extraction handoff constraints

Read all targeted files fully, using multi-pass reads for files over 500 LOC.
Extract provider selection, endpoint resolution, request/response mapping,
validation, timeout/retry, idempotency, delivery outcomes, and event payloads.
Keep MS-09, MS-06, MS-11, and MS-02 policies in their owning services. Record
CAST references for every rule and mark target-only delivery tables explicitly.
