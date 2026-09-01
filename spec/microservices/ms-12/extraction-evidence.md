# MS-12 Platform Integrations — Extraction Evidence

## Extraction status

- **Pass:** Independent Phase 4 re-extraction, 2026-09-01
- **Method:** Hybrid CAST bounds plus direct Java source reading
- **Files listed by CAST brief:** 16
- **Files actually read:** 16
- **Rules extracted:** 23 (20 source-derived, 3 target-only reliability)
- **Preservation tables:** 23/23, with all eight dimensions per rule
- **Large-file protocol:** UPS and USPS read in two passes; all other files read completely
- **Dead code excluded:** CAST object `ModulesApi` `29894`, zero transaction reachability

## Source files read

The vector columns are direct-read source counts in the order required by the Java reading guide:
control-flow, data-flow, constants, state transitions, outcomes, data writes, integrations, and
error paths. Counts include infrastructure branches in the source; rule-level preservation
vectors in `01-business-rules.md` record the business semantics retained in the target.

| # | Exact source file | Lines/sections read | Purpose and source vector (CF/DF/C/S/O/W/I/E) | Rules |
|---:|---|---|---|---|
| 1 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/system/EmailServiceImpl.java` | 1–67: configuration lookup, JSON parse, sender dispatch, configuration save | Store email configuration lookup and sender handoff; **6/8/1/0/3/1/2/2** | 014 |
| 2 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/email/DefaultEmailSenderImpl.java` | 1–188: SMTP setup, recipient/from mapping, text and HTML template rendering, multipart assembly | SMTP provider message construction; **10/10/3/0/4/0/3/3** | 015 |
| 3 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/email/SESEmailSenderImpl.java` | 1–99: region validation, SES client/request, HTML rendering, UTF-8 fallback | SES provider message construction; **7/8/3/0/3/0/3/2** | 015 |
| 4 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/utils/EmailTemplatesUtils.java` | 1–250: order addresses, lines, totals, payment, shipping, status; 251–497: registration, contact, status, download, password messages | Notification payload and recipient projection; **31/32/8/0/8/0/5/4** | 016, 017 |
| 5 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/shipping/impl/UPSShippingQuote.java` | 1–350: validation, eligibility, endpoint, XML request; 351–692: HTTP response, XML parsing, normalization, errors | UPS rating adapter; **35/30/8/0/7/0/5/7**; CAST object `29070`, complexity 35 | 005, 007, 008, 009 |
| 6 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/shipping/impl/USPSShippingQuote.java` | 1–370: validation, US-origin check, units, domestic/international XML; 371–744: encoded request, response parsing, errors | USPS rating adapter; **34/31/10/0/7/0/5/7**; CAST object `29071`, complexity 34 | 006, 010, 011 |
| 7 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/integration/shipping/impl/ShippingDistancePreProcessorImpl.java` | 1–92: module identity/configuration; 93–204: zone/postal eligibility, address construction, geocoding, matrix, quote enrichment; 205–226: accessors | Google Maps distance enrichment; **17/17/3/0/4/2/4/4**; CAST object `30319`, complexity 17 | 012 |
| 8 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/utils/GeoLocationImpl.java` | 1–62: lazy GeoLite reader, IP lookup, address mapping, unknown-address path | Local coarse IP lookup; **6/7/1/0/3/4/1/3** | 013 |
| 9 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/cms/content/StaticContentFileManagerImpl.java` | 1–158: provider-neutral facade delegation for file and folder operations | Provider dispatch boundary; **2/8/0/0/6/4/4/1** | 018 |
| 10 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/cms/content/local/CmsStaticContentFileManagerImpl.java` | 1–250: local setup, single/batch upload, path construction; 251–484: unsupported reads, deletes, listing, folder operations | Local filesystem capabilities and failures; **18/18/5/0/7/6/3/6** | 019, 020 |
| 11 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/cms/content/infinispan/CmsStaticContentFileManagerImpl.java` | 1–240: cache setup, single/batch upload, reads; 241–477: deletes, keys/listing, folder creation and TODOs | Infinispan byte storage and incomplete folders; **20/19/4/0/7/6/3/7** | 019, 020 |
| 12 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/cms/content/aws/S3StaticContentAssetsManagerImpl.java` | 1–153: object reads/listing; 154–228: upload/deletion; 229–322: bucket/client resolution and folder TODOs | S3 object storage and capability gaps; **17/18/5/0/7/5/5/6** | 019, 020 |
| 13 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/modules/cms/content/gcp/GCPStaticContentAssetsManagerImpl.java` | 1–104: blob reads/listing; 105–192: batch read/upload/deletion; 193–224: accessors and folder TODOs | GCP object storage, byte reads, and listing defect; **15/17/4/0/6/5/5/5** | 019, 020 |
| 14 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/system/ModuleConfigurationServiceImpl.java` | 1–61: service and code lookup; 62–165: cache, JSON projections, payment starters; 166–187: delete/create replacement | Category cache and replacement boundary; **17/15/5/1/5/3/4/3**; CAST object `13381`, complexity 17 | 001–004 |
| 15 | `initial-source/shopizer-3.2.7/sm-core/src/main/java/com/salesmanager/core/business/services/reference/loader/IntegrationModulesLoader.java` | 1–24: loader setup; 25–60: resource JSON; 61–119: module identity/details; 120–185: environment and supplemental settings | JSON module projection; **17/16/6/0/4/3/2/4**; CAST object `13191`, complexity 17 | 003, 004 |
| 16 | `initial-source/shopizer-3.2.7/sm-shop/src/main/java/com/salesmanager/shop/store/api/v0/system/SystemRESTController.java` | 1–57: controller/service boundary; 58–84: module route; 85–152: opt-in placeholder routes; 153–156: closing declarations | Boundary evidence only; **9/5/2/0/5/0/1/4** | No BR; opt-in routes excluded as placeholders |

## CAST discovery evidence

| Area | CAST evidence used |
|---|---|
| Email | Transactions `244089`, `244090`, `244235`, `244079`, `244245`, `244012`; `sendHtmlEmail` complexity 21 |
| Carrier | UPS object `29070`, complexity 35/fan-out 123; USPS object `29071`, complexity 34/fan-out 126 |
| Maps | `ShippingDistancePreProcessorImpl.prePostProcessShippingQuotes`, object `30319`, complexity 17/fan-out 68 |
| Registry | `ModuleConfigurationServiceImpl.getIntegrationModules`, object `13381`; `IntegrationModulesLoader.loadModule`, object `13191` |
| Storage | Transactions `244042`, `244065`, `244066`, `244289`, `244292`, `244293` |
| Configuration | `module_configuration` observed in 57 CAST nodes and 334 links; ownership remains MS-11 |
| External storage | `shopizer-content` S3 resource observed by CAST; execution is behind MS-12 |

## Source-derived rule crosswalk

| Rule family | Direct source files | Source behavior reconstructed |
|---|---|---|
| 001–004 | ModuleConfigurationServiceImpl, IntegrationModulesLoader | Category cache, delete/create replacement, environment projection, and the `config2` assignment defect |
| 005, 007–009 | UPSShippingQuote | Credentials/packages, destination and region gating, endpoint URI, rounded XML request, response parsing and option normalization |
| 006, 010–011 | USPSShippingQuote | Account/packages, US-origin rule, domestic/international branches, inch/pound aggregation, size brackets, and response mapping |
| 012 | ShippingDistancePreProcessorImpl | Zone/postal suppression, address strings, two geocodes, matrix conversion to kilometers |
| 013 | GeoLocationImpl | Lazy GeoLite reader, `InetAddress` lookup, coarse fields, unknown-address behavior |
| 014–017 | EmailServiceImpl, DefaultEmailSenderImpl, SESEmailSenderImpl, EmailTemplatesUtils | Sender selection, SMTP/SES rendering, order projection, and notification-specific payloads |
| 018–020 | StaticContentFileManagerImpl and four provider managers | Logical delegation, provider keys, byte reads, MIME listing, delete behavior, and folder capability differences |

## Target-only findings and rationale

| Rule | Legacy evidence | Target decision |
|---|---|---|
| 021 | Upload methods overwrite provider keys and have no durable operation key or attempt association; batch methods loop over items | Require one idempotency key for each operation and link every single/batch item to an attempt |
| 022 | Email/carrier/storage calls throw or log provider errors; no retry schedule or durable attempt table exists | Add bounded attempt states and retry scheduling |
| 023 | No outbox, replay, or dead-letter record exists; async email methods log and swallow failures | Add transactional outbox, replay lineage, and terminal dead-letter events |

## Independent-pass findings

This re-extraction was not a validator patch cycle. The complete source set was reread from the
CAST brief and rules were reassembled by behavioral seam. The independent pass retained source
mechanics in `Logic`, elevated statements to architecture-neutral business language, and
identified the following details as net-new or newly confirmed: `config2` overwrites `config1`
in `ModuleConfigurationServiceImpl`, SMTP HTML reads `textWriter`, local reads throw
unsupported errors, folder methods are incomplete across providers, GCP uses metageneration
`42`, USPS requires a US-origin store, and the source has no durable delivery identity.

## Explicit exclusions

- `ModulesApi` CAST object `29894`: zero transaction reachability.
- Payment providers: MS-06.
- Shipping policy, packaging policy, quote selection, and quote persistence: MS-09.
- Merchant/module configuration persistence: MS-11.
- Product/media metadata and orchestration: MS-02.
- DTO-only classes, JPA/framework classes, logging, `FilesController`, external `<LISA>` objects,
  JavaMail, SES, AWS SDK, Google Maps SDK, and JDK classes.
- No local FedEx adapter or local Google Maps implementation was found in the targeted scope.
