# Cross-Service Contract Reconciliation

The rows below cover every synchronous REST call resolved from the graph's REST `CALLS` edges.
Endpoint paths, methods, request schema names, response schema names, and statuses are copied from
the provider contracts. `RECONCILED` means the consumer binding is explicitly tied to the
provider schema; it does not mean that unrelated convention drift has been normalized.

| Consumer | Provider | Endpoint | Request Shape | Response Shape | Status |
|----------|----------|----------|---------------|----------------|--------|
| MS-02 | MS-10 | GET /stores/{storeCode} | path `storeCode: string`; no body | `#/components/schemas/Store` (200) | RECONCILED |
| MS-04 | MS-01 | GET /customers/me | no body | `#/components/schemas/Customer` (200) | RECONCILED |
| MS-04 | MS-01 | POST /customer-auth/registrations | `#/components/schemas/CreateCustomerRequest` (required: emailAddress, password, billing) | `#/components/schemas/AuthenticationResponse` (201) | RECONCILED |
| MS-04 | MS-02 | GET /products/sku/{sku} | path `sku: string`; no body | `#/components/schemas/Product` (200) | RECONCILED |
| MS-04 | MS-02 | GET /products/{productId}/availability | path `productId: uuid`; no body | `#/components/schemas/AvailabilityListResponse` (200; items array) | RECONCILED |
| MS-04 | MS-02 | POST /products/{productId}/reservations | `#/components/schemas/CreateReservationRequest` (required: reservationKey, quantity, expiresAt) | `#/components/schemas/InventoryReservation` (201) | RECONCILED |
| MS-04 | MS-07 | POST /pricing/quotes | `#/components/schemas/PricingQuoteRequest` (required: currency, items) | `#/components/schemas/PricingQuoteResponse` (200) | RECONCILED |
| MS-04 | MS-07 | POST /pricing/promotions/evaluate | `#/components/schemas/PromotionEvaluationRequest` (required: promoCode, items) | `#/components/schemas/PromotionEvaluationResponse` (200) | RECONCILED |
| MS-04 | MS-08 | POST /tax-calculations | `#/components/schemas/CalculateTaxRequest` (required: currencyCode, billingAddress, items) | `#/components/schemas/TaxCalculationResponse` (200) | RECONCILED |
| MS-04 | MS-09 | POST /cart/{cart}/shipping | path `cart: string`; `#/components/schemas/ShippingAddressRequest` (required: countryCode, postalCode) | `#/components/schemas/ShippingSummary` (200) | RECONCILED |
| MS-10 | MS-01 | GET /users/me | no body | `#/components/schemas/Administrator` (200) | RECONCILED |

No synchronous graph edge is hidden as an event. The MS-09 -> MS-12 graph edge is protocol
`Event`, and its missing internal event contract is recorded as a GAP in the MS-09 dependency
artifact rather than placed in this synchronous table.

