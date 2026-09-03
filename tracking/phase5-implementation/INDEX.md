# Phase 5 Implementation — All Services

## Execution model

- Model: A — Pure GitHub Copilot
- Sub-mode: A-direct
- Solution structure: Microsoft Aspire Shop-aligned
- Target runtime: C#/.NET 10+, ASP.NET Core

## Service status

| # | Service | Project | Stage | Status |
|---|---|---|---|---|
| 1 | Customer and Identity (MS-01) | `Shopizer.CustomerIdentity` | Scaffold | IN_REVIEW |
| 2 | Merchant and Store Administration (MS-10) | `Shopizer.MerchantAdministration` | Pending | PENDING |
| 3 | Catalog and Product (MS-02) | `Shopizer.CatalogProduct` | Pending | PENDING |
| 4 | Pricing and Promotions (MS-07) | `Shopizer.PricingPromotions` | Pending | PENDING |
| 5 | Tax (MS-08) | `Shopizer.Tax` | Pending | PENDING |
| 6 | Shipping (MS-09) | `Shopizer.Shipping` | Pending | PENDING |
| 7 | Content and Configuration (MS-11) | `Shopizer.ContentConfiguration` | Pending | PENDING |
| 8 | Cart and Checkout (MS-04) | `Shopizer.CartCheckout` | Pending | PENDING |
| 9 | Search (MS-03) | `Shopizer.Search` | Implemented | IN_REVIEW |
| 10 | Order Management (MS-05) | `Shopizer.OrderManagement` | Pending | PENDING |
| 11 | Payments (MS-06) | `Shopizer.Payments` | Pending | PENDING |
| 12 | Platform Integrations (MS-12) | `Shopizer.PlatformIntegrations` | Pending | PENDING |

## Shared implementation rules

- Specifications drive implementation; validation suites verify behavior.
- `04-api-contract.yaml` is the API naming authority.
- DTOs under each service's `08-dtos/` directory are copied verbatim.
- Every implemented business rule is annotated with its `BR-ID`.
- No stubs, shell implementations, or algorithm simplifications.
- Validation uses `sourcecode/Shopizer.IntegrationTests/Shopizer.IntegrationTests.csproj`.
